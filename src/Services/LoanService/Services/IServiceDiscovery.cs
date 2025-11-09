namespace LibHub.LoanService.Services;

/// <summary>
/// Service discovery interface for dynamically resolving service URLs from Consul
/// </summary>
public interface IServiceDiscovery
{
    /// <summary>
    /// Gets the URL of a healthy service instance from Consul
    /// </summary>
    /// <param name="serviceName">The name of the service registered in Consul</param>
    /// <returns>The full URL (scheme + host + port) of the service</returns>
    Task<string> GetServiceUrlAsync(string serviceName);
}
