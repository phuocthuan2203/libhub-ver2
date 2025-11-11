namespace LibHub.Gateway.Api.Handlers;

public class ConsulDiscoveryLoggingHandler : DelegatingHandler
{
    private readonly ILogger<ConsulDiscoveryLoggingHandler> _logger;

    public ConsulDiscoveryLoggingHandler(ILogger<ConsulDiscoveryLoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var correlationId = request.Headers
            .Where(h => h.Key == "X-Correlation-ID")
            .SelectMany(h => h.Value)
            .FirstOrDefault() ?? "no-correlation-id";

        var serviceName = request.RequestUri?.Host ?? "unknown";
        var method = request.Method.ToString();
        var path = request.RequestUri?.PathAndQuery ?? "/";
        var actualUrl = request.RequestUri?.ToString() ?? "unknown";
        
        _logger.LogInformation(
            "� [CONSUL-RESOLVED] Selected instance | Service: {ServiceName} | URL: {ActualUrl} | CorrelationId: {CorrelationId}",
            serviceName, actualUrl, correlationId);

        var startTime = DateTime.UtcNow;
        
        var response = await base.SendAsync(request, cancellationToken);
        
        var duration = (DateTime.UtcNow - startTime).TotalMilliseconds;
        
        _logger.LogInformation(
            "🎯 [DOWNSTREAM-CALL] {Method} {ActualUrl} → {StatusCode} | Duration: {Duration}ms | CorrelationId: {CorrelationId}",
            method, actualUrl, (int)response.StatusCode, duration, correlationId);

        return response;
    }
}
