using System.Net.Http.Json;
using System.Net.Http.Headers;
using LibHub.LoanService.Models.Responses;
using LibHub.LoanService.Services;

namespace LibHub.LoanService.Clients;

public class CatalogServiceClient : ICatalogServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IServiceDiscovery _serviceDiscovery;
    private readonly ILogger<CatalogServiceClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CatalogServiceClient(
        HttpClient httpClient, 
        IServiceDiscovery serviceDiscovery,
        ILogger<CatalogServiceClient> logger, 
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _serviceDiscovery = serviceDiscovery;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    private void SetAuthorizationHeader()
    {
        var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(token);
        }
    }

    private void PropagateCorrelationId()
    {
        // Propagate Correlation ID to downstream service
        var correlationId = _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault();
        if (!string.IsNullOrEmpty(correlationId))
        {
            // Remove old header if exists, then add new one
            _httpClient.DefaultRequestHeaders.Remove("X-Correlation-ID");
            _httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        }
    }

    public async Task<BookResponse> GetBookAsync(int bookId)
    {
        try
        {
            PropagateCorrelationId();
            
            // Discover CatalogService URL from Consul
            var catalogServiceUrl = await _serviceDiscovery.GetServiceUrlAsync("catalogservice");
            
            _logger.LogInformation("🔗 [INTER-SERVICE] Calling CatalogService at {ServiceUrl}: GET /api/books/{BookId}", 
                catalogServiceUrl, bookId);

            var response = await _httpClient.GetAsync($"{catalogServiceUrl}/api/books/{bookId}");
            
            _logger.LogInformation("📨 [INTER-SERVICE] CatalogService response: {StatusCode} for GET /api/books/{BookId}", 
                response.StatusCode, bookId);
            
            response.EnsureSuccessStatusCode();

            var book = await response.Content.ReadFromJsonAsync<BookResponse>();
            if (book == null)
                throw new Exception($"Failed to deserialize book data for BookId {bookId}");

            return book;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [INTER-SERVICE] Failed to get book {BookId} from CatalogService", bookId);
            throw;
        }
    }

    public async Task DecrementStockAsync(int bookId)
    {
        try
        {
            SetAuthorizationHeader();
            PropagateCorrelationId();
            
            // Discover CatalogService URL from Consul
            var catalogServiceUrl = await _serviceDiscovery.GetServiceUrlAsync("catalogservice");
            
            _logger.LogInformation("🔗 [INTER-SERVICE] Calling CatalogService at {ServiceUrl}: PUT /api/books/{BookId}/stock (decrement)", 
                catalogServiceUrl, bookId);
            
            var stockDto = new { ChangeAmount = -1 };
            var response = await _httpClient.PutAsJsonAsync($"{catalogServiceUrl}/api/books/{bookId}/stock", stockDto);

            _logger.LogInformation("📨 [INTER-SERVICE] CatalogService response: {StatusCode} for PUT /api/books/{BookId}/stock", 
                response.StatusCode, bookId);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("❌ [INTER-SERVICE] Failed to decrement stock for book {BookId}: {StatusCode} - {Error}", 
                    bookId, response.StatusCode, errorContent);
                throw new Exception($"Failed to decrement stock: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [INTER-SERVICE] Failed to decrement stock for book {BookId}", bookId);
            throw;
        }
    }

    public async Task IncrementStockAsync(int bookId)
    {
        try
        {
            SetAuthorizationHeader();
            PropagateCorrelationId();
            
            // Discover CatalogService URL from Consul
            var catalogServiceUrl = await _serviceDiscovery.GetServiceUrlAsync("catalogservice");
            
            _logger.LogInformation("🔗 [INTER-SERVICE] Calling CatalogService at {ServiceUrl}: PUT /api/books/{BookId}/stock (increment)", 
                catalogServiceUrl, bookId);
            
            var stockDto = new { ChangeAmount = 1 };
            var response = await _httpClient.PutAsJsonAsync($"{catalogServiceUrl}/api/books/{bookId}/stock", stockDto);

            _logger.LogInformation("📨 [INTER-SERVICE] CatalogService response: {StatusCode} for PUT /api/books/{BookId}/stock", 
                response.StatusCode, bookId);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("⚠️ [INTER-SERVICE] Failed to increment stock for book {BookId}: {StatusCode} - {Error}", 
                    bookId, response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ [INTER-SERVICE] Failed to increment stock for book {BookId}", bookId);
        }
    }
}

