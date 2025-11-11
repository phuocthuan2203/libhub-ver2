using Consul;

namespace LibHub.LoanService.Services;

/// <summary>
/// Consul-based service discovery implementation
/// </summary>
public class ConsulServiceDiscovery : IServiceDiscovery
{
    private readonly IConsulClient _consulClient;
    private readonly ILogger<ConsulServiceDiscovery> _logger;

    public ConsulServiceDiscovery(IConsulClient consulClient, ILogger<ConsulServiceDiscovery> logger)
    {
        _consulClient = consulClient;
        _logger = logger;
    }

    public async Task<string> GetServiceUrlAsync(string serviceName)
    {
        _logger.LogInformation("🔍 [SERVICE-DISCOVERY] Querying Consul for service: {ServiceName}", serviceName);

        try
        {
            // Query Consul for healthy instances of the service
            var services = await _consulClient.Health.Service(serviceName, tag: null, passingOnly: true);

            if (services.Response == null || !services.Response.Any())
            {
                _logger.LogError("❌ [SERVICE-DISCOVERY] No healthy instances found for service: {ServiceName}", serviceName);
                throw new Exception($"Service '{serviceName}' not available in Consul");
            }

            // Get the first healthy instance
            var serviceEntry = services.Response.First();
            var service = serviceEntry.Service;
            var serviceUrl = $"http://{service.Address}:{service.Port}";

            _logger.LogInformation(
                "✅ [SERVICE-DISCOVERY] Discovered service: {ServiceName} at {ServiceUrl} | ServiceId: {ServiceId} | HealthStatus: {HealthStatus}",
                serviceName,
                serviceUrl,
                service.ID,
                serviceEntry.Checks.FirstOrDefault()?.Status.ToString() ?? "unknown"
            );

            return serviceUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "❌ [SERVICE-DISCOVERY] Failed to discover service: {ServiceName} | Error: {ErrorMessage}",
                serviceName,
                ex.Message);
            throw;
        }
    }
}
