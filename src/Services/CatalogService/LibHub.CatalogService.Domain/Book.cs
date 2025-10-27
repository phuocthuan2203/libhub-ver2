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
        if (string.IsNullOrWhiteSpace(isbn) || isbn.Length != 13)
            throw new ArgumentException("ISBN must be exactly 13 digits", nameof(isbn));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author is required", nameof(author));
        if (totalCopies < 0)
            throw new ArgumentException("Total copies cannot be negative", nameof(totalCopies));

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
            throw new InvalidOperationException("Cannot increment beyond total");
        
        AvailableCopies++;
        UpdatedAt = DateTime.UtcNow;
    }
}
