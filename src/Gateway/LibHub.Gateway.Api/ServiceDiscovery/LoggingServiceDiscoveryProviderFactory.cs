using Ocelot.Configuration;
using Ocelot.Responses;
using Ocelot.ServiceDiscovery;
using Ocelot.ServiceDiscovery.Providers;
using Ocelot.Values;

namespace LibHub.Gateway.Api.ServiceDiscovery;

public class LoggingServiceDiscoveryProviderFactory : IServiceDiscoveryProviderFactory
{
    private readonly IServiceDiscoveryProviderFactory _innerFactory;
    private readonly ILoggerFactory _loggerFactory;

    public LoggingServiceDiscoveryProviderFactory(
        IServiceDiscoveryProviderFactory innerFactory,
        ILoggerFactory loggerFactory)
    {
        _innerFactory = innerFactory;
        _loggerFactory = loggerFactory;
    }

    public Response<IServiceDiscoveryProvider> Get(ServiceProviderConfiguration serviceConfig, DownstreamRoute route)
    {
        var response = _innerFactory.Get(serviceConfig, route);
        
        if (response.IsError)
        {
            return response;
        }
        
        var serviceName = route.ServiceName ?? "unknown";
        
        var logger = _loggerFactory.CreateLogger<LoggingConsulServiceDiscoveryProvider>();
        
        var loggingProvider = new LoggingConsulServiceDiscoveryProvider(response.Data, logger, serviceName);
        
        return new OkResponse<IServiceDiscoveryProvider>(loggingProvider);
    }
}
