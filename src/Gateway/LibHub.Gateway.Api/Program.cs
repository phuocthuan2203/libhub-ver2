using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using System.Text;
using System.Text.Json;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Polly;
using Serilog;
using Serilog.Events;

// Configure Serilog BEFORE creating the builder
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", "Gateway")
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{ServiceName}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq("http://seq:5341")
    .CreateLogger();

try
{
    Log.Information("Starting {ServiceName}", "Gateway");
    
    var builder = WebApplication.CreateBuilder(args);
    
    // USE SERILOG instead of default logging
    builder.Host.UseSerilog();

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
        };
        
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Authentication failed: {Message}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Authentication challenge: {Error}", context.Error);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddOcelot(builder.Configuration)
    .AddConsul()
    .AddPolly();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var exception = exceptionHandlerPathFeature?.Error;

        logger.LogError(exception, "Unhandled exception occurred: {Message}", exception?.Message);

        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            message = "An error occurred while processing your request",
            statusCode = context.Response.StatusCode,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    });
});

app.UseCors();

app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var requestPath = context.Request.Path;
    var requestMethod = context.Request.Method;
    
    logger.LogInformation("Gateway request: {Method} {Path}", requestMethod, requestPath);
    
    await next();
    
    logger.LogInformation("Gateway response: {Method} {Path} - Status: {StatusCode}", 
        requestMethod, requestPath, context.Response.StatusCode);
});

await app.UseOcelot();

app.Lifetime.ApplicationStarted.Register(() =>
{
    var urls = app.Urls.Any() ? string.Join(", ", app.Urls) : "default URLs";
    Log.Information("{ServiceName} started successfully listening on {Urls}", "Gateway", urls);
});

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "{ServiceName} failed to start", "Gateway");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
