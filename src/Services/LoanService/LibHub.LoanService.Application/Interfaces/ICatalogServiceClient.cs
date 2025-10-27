using LibHub.LoanService.Application.DTOs;

namespace LibHub.LoanService.Application.Interfaces;

public interface ICatalogServiceClient
{
    Task<BookDto> GetBookAsync(int bookId);
    Task DecrementStockAsync(int bookId);
    Task IncrementStockAsync(int bookId);
}
