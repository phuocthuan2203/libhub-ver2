using System.Linq;

namespace LibHub.CatalogService.Domain;

public class Book
{
    public int BookId { get; private set; }
    public string Isbn { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Author { get; private set; } = string.Empty;
    public string Genre { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public int TotalCopies { get; private set; }
    public int AvailableCopies { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Book() { }

    public Book(string isbn, string title, string author, string genre, 
                string? description, int totalCopies)
    {
        ValidateIsbn(isbn);
        ValidateTitle(title);
        ValidateAuthor(author);
        ValidateGenre(genre);
        ValidateTotalCopies(totalCopies);

        Isbn = isbn;
        Title = title;
        Author = author;
        Genre = genre;
        Description = description;
        TotalCopies = totalCopies;
        AvailableCopies = totalCopies;
        CreatedAt = DateTime.UtcNow;
    }

    public void DecrementStock()
    {
        if (AvailableCopies <= 0)
            throw new InvalidOperationException("No copies available");
        
        AvailableCopies--;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementStock()
    {
        if (AvailableCopies >= TotalCopies)
            throw new InvalidOperationException("Cannot increment beyond total copies");
        
        AvailableCopies++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string title, string author, string genre, string? description)
    {
        ValidateTitle(title);
        ValidateAuthor(author);
        ValidateGenre(genre);

        Title = title;
        Author = author;
        Genre = genre;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AdjustTotalCopies(int newTotal)
    {
        ValidateTotalCopies(newTotal);

        int onLoan = TotalCopies - AvailableCopies;
        if (newTotal < onLoan)
            throw new InvalidOperationException(
                $"Cannot reduce total to {newTotal} when {onLoan} copies are on loan");

        int difference = newTotal - TotalCopies;
        TotalCopies = newTotal;
        AvailableCopies += difference;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsAvailable() => AvailableCopies > 0;
    public int CopiesOnLoan() => TotalCopies - AvailableCopies;

    private static void ValidateIsbn(string isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            throw new ArgumentException("ISBN is required", nameof(isbn));

        if (isbn.Length != 13)
            throw new ArgumentException("ISBN must be exactly 13 digits", nameof(isbn));

        if (!isbn.All(char.IsDigit))
            throw new ArgumentException("ISBN must contain only digits", nameof(isbn));
    }

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));

        if (title.Length > 255)
            throw new ArgumentException("Title cannot exceed 255 characters", nameof(title));
    }

    private static void ValidateAuthor(string author)
    {
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required", nameof(author));

        if (author.Length > 255)
            throw new ArgumentException("Author cannot exceed 255 characters", nameof(author));
    }

    private static void ValidateGenre(string genre)
    {
        if (string.IsNullOrWhiteSpace(genre))
            throw new ArgumentException("Genre is required", nameof(genre));

        if (genre.Length > 100)
            throw new ArgumentException("Genre cannot exceed 100 characters", nameof(genre));
    }

    private static void ValidateTotalCopies(int totalCopies)
    {
        if (totalCopies < 0)
            throw new ArgumentException("Total copies cannot be negative", nameof(totalCopies));
    }
}
