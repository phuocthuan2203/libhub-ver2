using Microsoft.Extensions.DependencyInjection.Extensions;
using Ocelot.DependencyInjection;
using Ocelot.ServiceDiscovery;

namespace LibHub.Gateway.Api.ServiceDiscovery;

public static class LoggingServiceDiscoveryExtensions
{
    public static IOcelotBuilder AddLoggingServiceDiscovery(this IOcelotBuilder builder)
    {
        builder.Services.Decorate<IServiceDiscoveryProviderFactory>((inner, serviceProvider) =>
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            return new LoggingServiceDiscoveryProviderFactory(inner, loggerFactory);
        });

        return builder;
    }
}
