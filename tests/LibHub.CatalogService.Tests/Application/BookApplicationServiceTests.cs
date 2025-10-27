using FluentAssertions;
using LibHub.CatalogService.Application.DTOs;
using LibHub.CatalogService.Application.Services;
using LibHub.CatalogService.Domain;
using Moq;
using Xunit;

namespace LibHub.CatalogService.Tests.Application;

public class BookApplicationServiceTests
{
    private readonly Mock<IBookRepository> _mockBookRepository;
    private readonly BookApplicationService _service;

    public BookApplicationServiceTests()
    {
        _mockBookRepository = new Mock<IBookRepository>();
        _service = new BookApplicationService(_mockBookRepository.Object);
    }

    [Fact]
    public async Task CreateBookAsync_WithValidData_ShouldCreateBook()
    {
        var dto = new CreateBookDto
        {
            Isbn = "9781234567890",
            Title = "Test Book",
            Author = "Test Author",
            Genre = "Fiction",
            Description = "Test description",
            TotalCopies = 5
        };

        _mockBookRepository
            .Setup(r => r.GetByIsbnAsync(It.IsAny<string>()))
            .ReturnsAsync((Book?)null);

        var result = await _service.CreateBookAsync(dto);

        result.Should().NotBeNull();
        result.Isbn.Should().Be(dto.Isbn);
        result.Title.Should().Be(dto.Title);
        result.Author.Should().Be(dto.Author);
        result.TotalCopies.Should().Be(dto.TotalCopies);
        result.AvailableCopies.Should().Be(dto.TotalCopies);
        result.IsAvailable.Should().BeTrue();

        _mockBookRepository.Verify(r => r.AddAsync(It.IsAny<Book>()), Times.Once);
    }

    [Fact]
    public async Task CreateBookAsync_WithExistingIsbn_ShouldThrowInvalidOperationException()
    {
        var dto = new CreateBookDto
        {
            Isbn = "9781234567890",
            Title = "Test Book",
            Author = "Test Author",
            Genre = "Fiction",
            TotalCopies = 5
        };

        var existingBook = new Book("9781234567890", "Existing", "Author", "Genre", null, 3);

        _mockBookRepository
            .Setup(r => r.GetByIsbnAsync(It.IsAny<string>()))
            .ReturnsAsync(existingBook);

        Func<Task> act = async () => await _service.CreateBookAsync(dto);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ISBN already exists*");
    }

    [Fact]
    public async Task UpdateStockAsync_WithNegativeChange_ShouldDecrementStock()
    {
        var bookId = 1;
        var book = new Book("9781234567890", "Title", "Author", "Genre", null, 5);
        var dto = new UpdateStockDto { ChangeAmount = -1 };

        _mockBookRepository
            .Setup(r => r.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        await _service.UpdateStockAsync(bookId, dto);

        book.AvailableCopies.Should().Be(4);
        _mockBookRepository.Verify(r => r.UpdateAsync(book), Times.Once);
    }

    [Fact]
    public async Task UpdateStockAsync_WithPositiveChange_ShouldIncrementStock()
    {
        var bookId = 1;
        var book = new Book("9781234567890", "Title", "Author", "Genre", null, 5);
        book.DecrementStock();
        var dto = new UpdateStockDto { ChangeAmount = 1 };

        _mockBookRepository
            .Setup(r => r.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        await _service.UpdateStockAsync(bookId, dto);

        book.AvailableCopies.Should().Be(5);
        _mockBookRepository.Verify(r => r.UpdateAsync(book), Times.Once);
    }

    [Fact]
    public async Task DeleteBookAsync_WithActiveLoans_ShouldThrowInvalidOperationException()
    {
        var bookId = 1;
        var book = new Book("9781234567890", "Title", "Author", "Genre", null, 5);
        book.DecrementStock();

        _mockBookRepository
            .Setup(r => r.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        Func<Task> act = async () => await _service.DeleteBookAsync(bookId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*active loans*");
    }

    [Fact]
    public async Task DeleteBookAsync_WithNoLoans_ShouldDeleteBook()
    {
        var bookId = 1;
        var book = new Book("9781234567890", "Title", "Author", "Genre", null, 5);

        _mockBookRepository
            .Setup(r => r.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        await _service.DeleteBookAsync(bookId);

        _mockBookRepository.Verify(r => r.DeleteAsync(bookId), Times.Once);
    }

    [Fact]
    public async Task GetBookByIdAsync_WithExistingBook_ShouldReturnBookDto()
    {
        var bookId = 1;
        var book = new Book("9781234567890", "Title", "Author", "Genre", "Description", 5);

        _mockBookRepository
            .Setup(r => r.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        var result = await _service.GetBookByIdAsync(bookId);

        result.Should().NotBeNull();
        result.Isbn.Should().Be(book.Isbn);
        result.Title.Should().Be(book.Title);
        result.Author.Should().Be(book.Author);
        result.Genre.Should().Be(book.Genre);
        result.Description.Should().Be(book.Description);
        result.TotalCopies.Should().Be(book.TotalCopies);
        result.AvailableCopies.Should().Be(book.AvailableCopies);
    }

    [Fact]
    public async Task GetBookByIdAsync_WithNonExistingBook_ShouldThrowKeyNotFoundException()
    {
        var bookId = 999;

        _mockBookRepository
            .Setup(r => r.GetByIdAsync(bookId))
            .ReturnsAsync((Book?)null);

        Func<Task> act = async () => await _service.GetBookByIdAsync(bookId);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{bookId}*");
    }

    [Fact]
    public async Task SearchBooksAsync_ShouldReturnFilteredBooks()
    {
        var books = new List<Book>
        {
            new Book("9781234567890", "Book 1", "Author 1", "Fiction", null, 5),
            new Book("9781234567891", "Book 2", "Author 2", "Fiction", null, 3)
        };

        _mockBookRepository
            .Setup(r => r.SearchAsync("test", "Fiction"))
            .ReturnsAsync(books);

        var result = await _service.SearchBooksAsync("test", "Fiction");

        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Book 1");
        result[1].Title.Should().Be("Book 2");
    }

    [Fact]
    public async Task UpdateBookAsync_WithExistingBook_ShouldUpdateDetails()
    {
        var bookId = 1;
        var book = new Book("9781234567890", "Old Title", "Old Author", "Old Genre", "Old desc", 5);
        var dto = new UpdateBookDto
        {
            Title = "New Title",
            Author = "New Author",
            Genre = "New Genre",
            Description = "New desc"
        };

        _mockBookRepository
            .Setup(r => r.GetByIdAsync(bookId))
            .ReturnsAsync(book);

        await _service.UpdateBookAsync(bookId, dto);

        _mockBookRepository.Verify(r => r.UpdateAsync(book), Times.Once);
    }
}
