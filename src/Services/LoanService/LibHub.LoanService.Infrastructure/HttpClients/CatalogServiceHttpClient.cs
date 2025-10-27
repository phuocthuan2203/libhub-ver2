using System.Net.Http.Json;
using LibHub.LoanService.Application.DTOs;
using LibHub.LoanService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace LibHub.LoanService.Infrastructure.HttpClients;

public class CatalogServiceHttpClient : ICatalogServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogServiceHttpClient> _logger;

    public CatalogServiceHttpClient(HttpClient httpClient, ILogger<CatalogServiceHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<BookDto> GetBookAsync(int bookId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/books/{bookId}");
            response.EnsureSuccessStatusCode();

            var book = await response.Content.ReadFromJsonAsync<BookDto>();
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
