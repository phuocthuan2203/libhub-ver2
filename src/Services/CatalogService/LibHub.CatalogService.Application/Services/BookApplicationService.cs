using System.Linq;
using LibHub.CatalogService.Application.DTOs;
using LibHub.CatalogService.Domain;

namespace LibHub.CatalogService.Application.Services;

public class BookApplicationService
{
    private readonly IBookRepository _bookRepository;

    public BookApplicationService(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<BookDto> CreateBookAsync(CreateBookDto dto)
    {
        var existingBook = await _bookRepository.GetByIsbnAsync(dto.Isbn);
        if (existingBook != null)
            throw new InvalidOperationException("A book with this ISBN already exists");

        var book = new Book(
            dto.Isbn,
            dto.Title,
            dto.Author,
            dto.Genre,
            dto.Description,
            dto.TotalCopies);

        await _bookRepository.AddAsync(book);

        return MapToDto(book);
    }

    public async Task<BookDto> GetBookByIdAsync(int bookId)
    {
        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book == null)
            throw new KeyNotFoundException($"Book with ID {bookId} not found");

        return MapToDto(book);
    }

    public async Task<List<BookDto>> SearchBooksAsync(string? searchTerm = null, string? genre = null)
    {
        var books = await _bookRepository.SearchAsync(searchTerm, genre);
        return books.Select(MapToDto).ToList();
    }

    public async Task<List<BookDto>> GetAllBooksAsync()
    {
        var books = await _bookRepository.GetAllAsync();
        return books.Select(MapToDto).ToList();
    }

    public async Task UpdateBookAsync(int bookId, UpdateBookDto dto)
    {
        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book == null)
            throw new KeyNotFoundException($"Book with ID {bookId} not found");

        book.UpdateDetails(dto.Title, dto.Author, dto.Genre, dto.Description);
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

    public async Task UpdateStockAsync(int bookId, UpdateStockDto dto)
    {
        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book == null)
            throw new KeyNotFoundException($"Book with ID {bookId} not found");

        if (dto.ChangeAmount < 0)
        {
            book.DecrementStock();
        }
        else if (dto.ChangeAmount > 0)
        {
            book.IncrementStock();
        }

        await _bookRepository.UpdateAsync(book);
    }

    private static BookDto MapToDto(Book book)
    {
        return new BookDto
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
