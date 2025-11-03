using Consul;

namespace LibHub.LoanService.Extensions;

public static class ConsulServiceRegistration
{
    private static readonly TimeSpan[] RetryDelays = 
    {
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30)
    };
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
        var logger = app.ApplicationServices.GetRequiredService<ILogger<IConsulClient>>();
        
        var serviceName = configuration["ServiceConfig:ServiceName"] ?? "loanservice";
        var serviceHost = configuration["ServiceConfig:ServiceHost"] ?? "localhost";
        var servicePort = int.Parse(configuration["ServiceConfig:ServicePort"] ?? "5003");
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

        Task.Run(async () =>
        {
            await RegisterWithRetryAsync(consulClient, registration, logger, serviceId, serviceName);
        });

        lifetime.ApplicationStopping.Register(() =>
        {
            try
            {
                logger.LogInformation("Deregistering service {ServiceId} from Consul", serviceId);
                consulClient.Agent.ServiceDeregister(serviceId).Wait(TimeSpan.FromSeconds(5));
                logger.LogInformation("Successfully deregistered service {ServiceId} from Consul", serviceId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to deregister service {ServiceId} from Consul", serviceId);
            }
        });

        return app;
    }

    private static async Task RegisterWithRetryAsync(
        IConsulClient consulClient,
        AgentServiceRegistration registration,
        ILogger logger,
        string serviceId,
        string serviceName)
    {
        for (int attempt = 0; attempt < RetryDelays.Length; attempt++)
        {
            try
            {
                logger.LogInformation(
                    "Attempting to register service {ServiceName} (ID: {ServiceId}) with Consul (Attempt {Attempt}/{MaxAttempts})",
                    serviceName, serviceId, attempt + 1, RetryDelays.Length);

                await consulClient.Agent.ServiceRegister(registration);
                
                logger.LogInformation(
                    "Successfully registered service {ServiceName} (ID: {ServiceId}) with Consul at {Address}:{Port}",
                    serviceName, serviceId, registration.Address, registration.Port);
                
                return;
            }
            catch (Exception ex)
            {
                var isLastAttempt = attempt == RetryDelays.Length - 1;
                
                if (isLastAttempt)
                {
                    logger.LogError(ex,
                        "Failed to register service {ServiceName} (ID: {ServiceId}) with Consul after {Attempts} attempts. Service will continue without Consul registration.",
                        serviceName, serviceId, RetryDelays.Length);
                }
                else
                {
                    logger.LogWarning(ex,
                        "Failed to register service {ServiceName} with Consul (Attempt {Attempt}/{MaxAttempts}). Retrying in {Delay} seconds...",
                        serviceName, attempt + 1, RetryDelays.Length, RetryDelays[attempt].TotalSeconds);
                    
                    await Task.Delay(RetryDelays[attempt]);
                }
            }
        }
    }
}

