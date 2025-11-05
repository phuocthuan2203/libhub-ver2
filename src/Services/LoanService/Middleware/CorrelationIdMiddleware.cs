using Serilog.Context;
using System.Diagnostics;

namespace LibHub.LoanService.Middleware;

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Get Correlation ID from request header, or generate new Guid
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault() 
                           ?? Guid.NewGuid().ToString();

        // 2. Add to response header (for debugging)
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // 3. Push to Serilog LogContext using LogContext.PushProperty()
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            var stopwatch = Stopwatch.StartNew();
            
            // 4. Log: "Request started: {Method} {Path}"
            _logger.LogInformation("Request started: {Method} {Path}", 
                context.Request.Method, context.Request.Path);

            try
            {
                // 5. Call next middleware
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                
                // 6. Log: "Request completed: {Method} {Path} - {StatusCode} ({ElapsedMs}ms)"
                _logger.LogInformation("Request completed: {Method} {Path} - {StatusCode} ({ElapsedMs}ms)",
                    context.Request.Method, 
                    context.Request.Path, 
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds);
            }
        }
        // 7. LogContext will be disposed automatically (using statement)
    }
}

// Extension method for easy registration
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
