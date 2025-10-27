using FluentAssertions;
using LibHub.CatalogService.Domain;
using Xunit;

namespace LibHub.CatalogService.Tests.Domain;

public class BookTests
{
    [Fact]
    public void Constructor_WithValidInputs_ShouldCreateBook()
    {
        var isbn = "9781234567890";
        var title = "Test Book";
        var author = "Test Author";
        var genre = "Fiction";
        var description = "Test description";
        var totalCopies = 5;

        var book = new Book(isbn, title, author, genre, description, totalCopies);

        book.Isbn.Should().Be(isbn);
        book.Title.Should().Be(title);
        book.Author.Should().Be(author);
        book.Genre.Should().Be(genre);
        book.Description.Should().Be(description);
        book.TotalCopies.Should().Be(totalCopies);
        book.AvailableCopies.Should().Be(totalCopies);
        book.IsAvailable().Should().BeTrue();
        book.CopiesOnLoan().Should().Be(0);
        book.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("12345678901234")]
    [InlineData("978123456789A")]
    [InlineData("")]
    public void Constructor_WithInvalidIsbn_ShouldThrowArgumentException(string invalidIsbn)
    {
        Action act = () => new Book(invalidIsbn, "Title", "Author", "Genre", null, 5);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*ISBN*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Constructor_WithInvalidTitle_ShouldThrowArgumentException(string invalidTitle)
    {
        Action act = () => new Book("9781234567890", invalidTitle, "Author", "Genre", null, 5);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Title*");
    }

    [Fact]
    public void DecrementStock_WithAvailableCopies_ShouldDecreaseCount()
    {
        var book = new Book("9781234567890", "Title", "Author", "Genre", null, 5);

        book.DecrementStock();

        book.AvailableCopies.Should().Be(4);
        book.CopiesOnLoan().Should().Be(1);
        book.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void DecrementStock_WithNoCopiesAvailable_ShouldThrowInvalidOperationException()
    {
        var book = new Book("9781234567890", "Title", "Author", "Genre", null, 1);
        book.DecrementStock();

        Action act = () => book.DecrementStock();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No copies available*");
    }

    [Fact]
    public void IncrementStock_WithCopiesOnLoan_ShouldIncreaseCount()
    {
        var book = new Book("9781234567890", "Title", "Author", "Genre", null, 5);
        book.DecrementStock();

        book.IncrementStock();

        book.AvailableCopies.Should().Be(5);
        book.CopiesOnLoan().Should().Be(0);
    }

    [Fact]
    public void IncrementStock_WhenAllCopiesAvailable_ShouldThrowInvalidOperationException()
    {
        var book = new Book("9781234567890", "Title", "Author", "Genre", null, 5);

        Action act = () => book.IncrementStock();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot increment beyond total*");
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateBook()
    {
        var book = new Book("9781234567890", "Old Title", "Old Author", "Old Genre", "Old desc", 5);

        book.UpdateDetails("New Title", "New Author", "New Genre", "New desc");

        book.Title.Should().Be("New Title");
        book.Author.Should().Be("New Author");
        book.Genre.Should().Be("New Genre");
        book.Description.Should().Be("New desc");
        book.Isbn.Should().Be("9781234567890");
        book.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void AdjustTotalCopies_IncreasingTotal_ShouldIncreaseAvailable()
    {
        var book = new Book("9781234567890", "Title", "Author", "Genre", null, 5);
        book.DecrementStock();

        book.AdjustTotalCopies(10);

        book.TotalCopies.Should().Be(10);
        book.AvailableCopies.Should().Be(9);
        book.CopiesOnLoan().Should().Be(1);
    }

    [Fact]
    public void AdjustTotalCopies_BelowOnLoanCount_ShouldThrowInvalidOperationException()
    {
        var book = new Book("9781234567890", "Title", "Author", "Genre", null, 5);
        book.DecrementStock();
        book.DecrementStock();

        Action act = () => book.AdjustTotalCopies(1);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cannot reduce total*when*on loan*");
    }

    [Fact]
    public void IsAvailable_WithAvailableCopies_ShouldReturnTrue()
    {
        var book = new Book("9781234567890", "Title", "Author", "Genre", null, 5);

        book.IsAvailable().Should().BeTrue();
    }

    [Fact]
    public void IsAvailable_WithNoCopies_ShouldReturnFalse()
    {
        var book = new Book("9781234567890", "Title", "Author", "Genre", null, 1);
        book.DecrementStock();

        book.IsAvailable().Should().BeFalse();
    }
}
