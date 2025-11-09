namespace LibHub.UserService.Middleware;

public class HealthCheckLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HealthCheckLoggingMiddleware> _logger;

    public HealthCheckLoggingMiddleware(RequestDelegate next, ILogger<HealthCheckLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only log if it's a health check request
        if (context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase))
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            _logger.LogDebug("💓 [HEALTH-CHECK] Health check called by {ClientIp}", clientIp);
        }

        await _next(context);
    }
}

public static class HealthCheckLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseHealthCheckLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<HealthCheckLoggingMiddleware>();
    }
}
