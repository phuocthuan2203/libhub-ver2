using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace LibHub.Gateway.Api.ServiceDiscovery;

public class LoggingConsulServiceDiscoveryProvider : IServiceDiscoveryProvider
{
    private readonly IServiceDiscoveryProvider _innerProvider;
    private readonly ILogger<LoggingConsulServiceDiscoveryProvider> _logger;
    private readonly string _serviceName;

    public LoggingConsulServiceDiscoveryProvider(
        IServiceDiscoveryProvider innerProvider,
        ILogger<LoggingConsulServiceDiscoveryProvider> logger,
        string serviceName)
    {
        _innerProvider = innerProvider;
        _logger = logger;
        _serviceName = serviceName;
    }

    public async Task<List<Service>> GetAsync()
    {
        _logger.LogInformation(
            "🔍 [CONSUL-QUERY] Querying Consul for service: {ServiceName}",
            _serviceName);

        var services = await _innerProvider.GetAsync();

        if (services == null || services.Count == 0)
        {
            _logger.LogWarning(
                "⚠️ [CONSUL-RESPONSE] No instances found for service: {ServiceName}",
                _serviceName);
            return services ?? new List<Service>();
        }

        _logger.LogInformation(
            "📍 [CONSUL-RESPONSE] Found {Count} instance(s) for service: {ServiceName} | Instances: {Instances}",
            services.Count,
            _serviceName,
            string.Join(", ", services.Select(s => $"{s.HostAndPort.DownstreamHost}:{s.HostAndPort.DownstreamPort}")));

        foreach (var service in services)
        {
            _logger.LogDebug(
                "📌 [CONSUL-INSTANCE] Service: {ServiceName} | Host: {Host} | Port: {Port} | Scheme: {Scheme}",
                _serviceName,
                service.HostAndPort.DownstreamHost,
                service.HostAndPort.DownstreamPort,
                service.HostAndPort.Scheme);
        }

        return services;
    }
}
