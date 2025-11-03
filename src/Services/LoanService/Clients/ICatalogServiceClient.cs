using LibHub.LoanService.Models.Responses;

namespace LibHub.LoanService.Clients;

public interface ICatalogServiceClient
{
    Task<BookResponse> GetBookAsync(int bookId);
    Task DecrementStockAsync(int bookId);
    Task IncrementStockAsync(int bookId);
}

