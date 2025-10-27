using Consul;

namespace LibHub.UserService.Api.Extensions;

public static class ConsulServiceRegistration
{
    public static IServiceCollection AddConsulServiceRegistration(this IServiceCollection services, IConfiguration configuration)
    {
        var consulHost = configuration["Consul:Host"] ?? "localhost";
        var consulPort = int.Parse(configuration["Consul:Port"] ?? "8500");

        services.AddSingleton<IConsulClient>(p => new ConsulClient(config =>
        {
            config.Address = new Uri($"http://{consulHost}:{consulPort}");
        }));

        return services;
    }

    public static IApplicationBuilder UseConsulServiceRegistration(this IApplicationBuilder app, IConfiguration configuration, IHostApplicationLifetime lifetime)
    {
        var consulClient = app.ApplicationServices.GetRequiredService<IConsulClient>();
        
        var serviceName = configuration["ServiceConfig:ServiceName"] ?? "userservice";
        var serviceHost = configuration["ServiceConfig:ServiceHost"] ?? "localhost";
        var servicePort = int.Parse(configuration["ServiceConfig:ServicePort"] ?? "5002");
        var serviceId = $"{serviceName}-{Guid.NewGuid()}";

        var registration = new AgentServiceRegistration
        {
            ID = serviceId,
            Name = serviceName,
            Address = serviceHost,
            Port = servicePort,
            Check = new AgentServiceCheck
            {
                HTTP = $"http://{serviceHost}:{servicePort}/health",
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5),
                DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1)
            }
        };

        consulClient.Agent.ServiceRegister(registration).Wait();

        lifetime.ApplicationStopping.Register(() =>
        {
            consulClient.Agent.ServiceDeregister(serviceId).Wait();
        });

        return app;
    }
}
