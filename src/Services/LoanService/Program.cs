using System.Text;
using LibHub.LoanService.Data;
using LibHub.LoanService.Clients;
using LibHub.LoanService.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

// Configure Serilog BEFORE creating the builder
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", "LoanService")
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{ServiceName}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq("http://seq:5341")
    .CreateLogger();

try
{
    Log.Information("Starting {ServiceName}", "LoanService");
    
    var builder = WebApplication.CreateBuilder(args);
    
    // USE SERILOG instead of default logging
    builder.Host.UseSerilog();

builder.Services.AddDbContext<LoanDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

builder.Services.AddScoped<LoanRepository>();
builder.Services.AddScoped<LibHub.LoanService.Services.LoanService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient<ICatalogServiceClient, CatalogServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExternalServices:CatalogServiceBaseUrl"]!);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

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
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddConsulServiceRegistration(builder.Configuration);

builder.Services.AddHealthChecks();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "LibHub LoanService API", 
        Version = "v1",
        Description = "Loan management service with Saga pattern for distributed transactions"
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LoanDbContext>();
    try
    {
        dbContext.Database.EnsureCreated();
        app.Logger.LogInformation("Database created successfully for LoanService.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to create database for LoanService.");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.UseConsulServiceRegistration(app.Configuration, app.Lifetime);

app.Lifetime.ApplicationStarted.Register(() =>
{
    var urls = app.Urls.Any() ? string.Join(", ", app.Urls) : "default URLs";
    Log.Information("{ServiceName} started successfully listening on {Urls}", "LoanService", urls);
});

app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "{ServiceName} failed to start", "LoanService");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
