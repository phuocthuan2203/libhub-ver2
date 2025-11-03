using System.Net.Http.Json;
using System.Net.Http.Headers;
using LibHub.LoanService.Models.Responses;

namespace LibHub.LoanService.Clients;

public class CatalogServiceClient : ICatalogServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogServiceClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CatalogServiceClient(HttpClient httpClient, ILogger<CatalogServiceClient> logger, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
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

    public async Task<BookResponse> GetBookAsync(int bookId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/books/{bookId}");
            response.EnsureSuccessStatusCode();

            var book = await response.Content.ReadFromJsonAsync<BookResponse>();
            if (book == null)
                throw new Exception($"Failed to deserialize book data for BookId {bookId}");

            return book;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get book {BookId} from CatalogService", bookId);
            throw;
        }
    }

    public async Task DecrementStockAsync(int bookId)
    {
        try
        {
            SetAuthorizationHeader();
            var stockDto = new { ChangeAmount = -1 };
            var response = await _httpClient.PutAsJsonAsync($"/api/books/{bookId}/stock", stockDto);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to decrement stock for book {BookId}: {StatusCode} - {Error}", 
                    bookId, response.StatusCode, errorContent);
                throw new Exception($"Failed to decrement stock: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrement stock for book {BookId}", bookId);
            throw;
        }
    }

    public async Task IncrementStockAsync(int bookId)
    {
        try
        {
            SetAuthorizationHeader();
            var stockDto = new { ChangeAmount = 1 };
            var response = await _httpClient.PutAsJsonAsync($"/api/books/{bookId}/stock", stockDto);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to increment stock for book {BookId}: {StatusCode} - {Error}", 
                    bookId, response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to increment stock for book {BookId}", bookId);
        }
    }
}

