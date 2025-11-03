using System.Linq;
using LibHub.CatalogService.Models.Entities;
using LibHub.CatalogService.Models.Requests;
using LibHub.CatalogService.Models.Responses;
using LibHub.CatalogService.Data;

namespace LibHub.CatalogService.Services;

public class BookService
{
    private readonly BookRepository _bookRepository;

    public BookService(BookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<BookResponse> CreateBookAsync(CreateBookRequest request)
    {
        var existingBook = await _bookRepository.GetByIsbnAsync(request.Isbn);
        if (existingBook != null)
            throw new InvalidOperationException("A book with this ISBN already exists");

        var book = new Book(
            request.Isbn,
            request.Title,
            request.Author,
            request.Genre,
            request.Description,
            request.TotalCopies);

        await _bookRepository.AddAsync(book);

        return MapToResponse(book);
    }

    public async Task<BookResponse> GetBookByIdAsync(int bookId)
    {
        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book == null)
            throw new KeyNotFoundException($"Book with ID {bookId} not found");

        return MapToResponse(book);
    }

    public async Task<List<BookResponse>> SearchBooksAsync(string? searchTerm = null, string? genre = null)
    {
        var books = await _bookRepository.SearchAsync(searchTerm, genre);
        return books.Select(MapToResponse).ToList();
    }

    public async Task<List<BookResponse>> GetAllBooksAsync()
    {
        var books = await _bookRepository.GetAllAsync();
        return books.Select(MapToResponse).ToList();
    }

    public async Task UpdateBookAsync(int bookId, UpdateBookRequest request)
    {
        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book == null)
            throw new KeyNotFoundException($"Book with ID {bookId} not found");

        book.UpdateDetails(request.Title, request.Author, request.Genre, request.Description);
        await _bookRepository.UpdateAsync(book);
    }

    public async Task DeleteBookAsync(int bookId)
    {
        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book == null)
            throw new KeyNotFoundException($"Book with ID {bookId} not found");

        if (book.CopiesOnLoan() > 0)
            throw new InvalidOperationException(
                $"Cannot delete book with {book.CopiesOnLoan()} active loans");

        await _bookRepository.DeleteAsync(bookId);
    }

    public async Task UpdateStockAsync(int bookId, UpdateStockRequest request)
    {
        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book == null)
            throw new KeyNotFoundException($"Book with ID {bookId} not found");

        if (request.ChangeAmount < 0)
        {
            book.DecrementStock();
        }
        else if (request.ChangeAmount > 0)
        {
            book.IncrementStock();
        }

        await _bookRepository.UpdateAsync(book);
    }

    private static BookResponse MapToResponse(Book book)
    {
        return new BookResponse
        {
            BookId = book.BookId,
            Isbn = book.Isbn,
            Title = book.Title,
            Author = book.Author,
            Genre = book.Genre,
            Description = book.Description,
            TotalCopies = book.TotalCopies,
            AvailableCopies = book.AvailableCopies,
            IsAvailable = book.IsAvailable(),
            CreatedAt = book.CreatedAt
        };
    }
}

