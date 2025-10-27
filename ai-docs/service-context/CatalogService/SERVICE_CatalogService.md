# CatalogService - Clean Architecture Design

**Domain**: Book Inventory Management  
**Port**: 5001  
**Database**: catalog_db  
**Responsibilities**: Book CRUD operations, inventory tracking, stock management

---

## Clean Architecture Layers

### 1. Domain Layer (`LibHub.CatalogService.Domain`)

**Zero external dependencies - pure C# only**

#### Book Entity (Aggregate Root)
```
public class Book
{
    public int BookId { get; private set; }
    public string Isbn { get; private set; }              // 13 digits, unique
    public string Title { get; private set; }
    public string Author { get; private set; }
    public string Genre { get; private set; }
    public string Description { get; private set; }
    public int TotalCopies { get; private set; }
    public int AvailableCopies { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    
    public Book(string isbn, string title, string author, string genre, 
                string description, int totalCopies)
    {
        if (string.IsNullOrWhiteSpace(isbn) || isbn.Length != 13)
            throw new ArgumentException("ISBN must be exactly 13 digits");
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required");
        if (totalCopies < 0)
            throw new ArgumentException("Total copies cannot be negative");
        
        Isbn = isbn;
        Title = title;
        Author = author;
        Genre = genre;
        Description = description;
        TotalCopies = totalCopies;
        AvailableCopies = totalCopies;  // All available initially
        CreatedAt = DateTime.UtcNow;
    }
    
    // CRITICAL: Called by LoanService via API during borrow
    public void DecrementStock()
    {
        if (AvailableCopies <= 0)
            throw new InvalidOperationException("No copies available to borrow");
        
        AvailableCopies--;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // CRITICAL: Called by LoanService via API during return
    public void IncrementStock()
    {
        if (AvailableCopies >= TotalCopies)
            throw new InvalidOperationException("Cannot increment beyond total copies");
        
        AvailableCopies++;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateDetails(string title, string author, string genre, string description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required");
        
        Title = title;
        Author = author;
        Genre = genre;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void AdjustTotalCopies(int newTotal)
    {
        if (newTotal < 0)
            throw new ArgumentException("Total copies cannot be negative");
        
        int onLoan = TotalCopies - AvailableCopies;
        if (newTotal < onLoan)
            throw new InvalidOperationException(
                $"Cannot reduce total to {newTotal} when {onLoan} copies are on loan");
        
        int difference = newTotal - TotalCopies;
        TotalCopies = newTotal;
        AvailableCopies += difference;
        UpdatedAt = DateTime.UtcNow;
    }
    
    // Query methods
    public bool IsAvailable() => AvailableCopies > 0;
    public int CopiesOnLoan() => TotalCopies - AvailableCopies;
}
```

**Invariants (MUST ALWAYS BE TRUE)**:
- `AvailableCopies <= TotalCopies`
- `AvailableCopies >= 0`

#### Repository Interface
```
public interface IBookRepository
{
    Task<Book> GetByIdAsync(int bookId);
    Task<Book> GetByIsbnAsync(string isbn);
    Task<List<Book>> SearchAsync(string searchTerm, string genre = null);
    Task<List<Book>> GetAllAsync();
    Task AddAsync(Book book);
    Task UpdateAsync(Book book);
    Task DeleteAsync(int bookId);
}
```

---

### 2. Application Layer (`LibHub.CatalogService.Application`)

**Depends on Domain Layer only**

#### DTOs
```
public class CreateBookDto
{
    public string Isbn { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string Genre { get; set; }
    public string Description { get; set; }
    public int TotalCopies { get; set; }
}

public class UpdateBookDto
{
    public string Title { get; set; }
    public string Author { get; set; }
    public string Genre { get; set; }
    public string Description { get; set; }
}

public class BookDto
{
    public int BookId { get; set; }
    public string Isbn { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string Genre { get; set; }
    public string Description { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public bool IsAvailable { get; set; }
}

public class UpdateStockDto
{
    public int ChangeAmount { get; set; }  // -1 for borrow, +1 for return
}
```

#### Application Service
```
public class BookApplicationService
{
    private readonly IBookRepository _bookRepository;
    
    public async Task<BookDto> CreateBookAsync(CreateBookDto dto)
    {
        // 1. Check ISBN uniqueness
        var existingBook = await _bookRepository.GetByIsbnAsync(dto.Isbn);
        if (existingBook != null)
            throw new Exception("ISBN already exists");
        
        // 2. Create domain entity
        var book = new Book(dto.Isbn, dto.Title, dto.Author, 
                           dto.Genre, dto.Description, dto.TotalCopies);
        
        // 3. Persist
        await _bookRepository.AddAsync(book);
        
        // 4. Return DTO
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
            IsAvailable = book.IsAvailable()
        };
    }
    
    public async Task<BookDto> GetBookByIdAsync(int bookId)
    {
        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book == null)
            throw new Exception("Book not found");
        
        return MapToDto(book);
    }
    
    public async Task<List<BookDto>> SearchBooksAsync(string searchTerm, string genre)
    {
        var books = await _bookRepository.SearchAsync(searchTerm, genre);
        return books.Select(MapToDto).ToList();
    }
    
    // CRITICAL: Called by LoanService during Saga
    public async Task UpdateStockAsync(int bookId, UpdateStockDto dto)
    {
        // 1. Get book entity
        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book == null)
            throw new Exception("Book not found");
        
        // 2. Execute domain logic
        if (dto.ChangeAmount < 0)
            book.DecrementStock();  // Borrow
        else if (dto.ChangeAmount > 0)
            book.IncrementStock();  // Return
        
        // 3. Save changes
        await _bookRepository.UpdateAsync(book);
    }
    
    public async Task UpdateBookAsync(int bookId, UpdateBookDto dto)
    {
        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book == null)
            throw new Exception("Book not found");
        
        book.UpdateDetails(dto.Title, dto.Author, dto.Genre, dto.Description);
        await _bookRepository.UpdateAsync(book);
    }
    
    public async Task DeleteBookAsync(int bookId)
    {
        var book = await _bookRepository.GetByIdAsync(bookId);
        if (book == null)
            throw new Exception("Book not found");
        
        if (book.CopiesOnLoan() > 0)
            throw new Exception("Cannot delete book with active loans");
        
        await _bookRepository.DeleteAsync(bookId);
    }
    
    private BookDto MapToDto(Book book) => new BookDto 
    { 
        BookId = book.BookId,
        Isbn = book.Isbn,
        Title = book.Title,
        Author = book.Author,
        Genre = book.Genre,
        Description = book.Description,
        TotalCopies = book.TotalCopies,
        AvailableCopies = book.AvailableCopies,
        IsAvailable = book.IsAvailable()
    };
}
```

---

### 3. Infrastructure Layer (`LibHub.CatalogService.Infrastructure`)

**Implements interfaces from Domain and Application layers**

#### EfBookRepository
```
public class EfBookRepository : IBookRepository
{
    private readonly CatalogDbContext _context;
    
    public async Task<Book> GetByIdAsync(int bookId)
    {
        return await _context.Books.FindAsync(bookId);
    }
    
    public async Task<Book> GetByIsbnAsync(string isbn)
    {
        return await _context.Books
            .FirstOrDefaultAsync(b => b.Isbn == isbn);
    }
    
    public async Task<List<Book>> SearchAsync(string searchTerm, string genre)
    {
        var query = _context.Books.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(b => 
                b.Title.ToLower().Contains(searchTerm) ||
                b.Author.ToLower().Contains(searchTerm) ||
                b.Isbn.Contains(searchTerm));
        }
        
        if (!string.IsNullOrWhiteSpace(genre))
        {
            query = query.Where(b => b.Genre == genre);
        }
        
        return await query.ToListAsync();
    }
    
    public async Task<List<Book>> GetAllAsync()
    {
        return await _context.Books.ToListAsync();
    }
    
    public async Task AddAsync(Book book)
    {
        await _context.Books.AddAsync(book);
        await _context.SaveChangesAsync();
    }
    
    public async Task UpdateAsync(Book book)
    {
        _context.Books.Update(book);
        await _context.SaveChangesAsync();
    }
    
    public async Task DeleteAsync(int bookId)
    {
        var book = await GetByIdAsync(bookId);
        if (book != null)
        {
            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
        }
    }
}
```

#### DbContext
```
public class CatalogDbContext : DbContext
{
    public DbSet<Book> Books { get; set; }
    
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) 
        : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(e => e.BookId);
            entity.Property(e => e.Isbn).IsRequired().HasMaxLength(13);
            entity.HasIndex(e => e.Isbn).IsUnique();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Author).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Genre).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasColumnType("TEXT");
            entity.Property(e => e.TotalCopies).IsRequired();
            entity.Property(e => e.AvailableCopies).IsRequired();
        });
    }
}
```

---

### 4. Presentation Layer (`LibHub.CatalogService.Api`)

**Thin controllers - delegate to Application Layer**

#### BooksController
```
[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
    private readonly BookApplicationService _bookService;
    
    [HttpGet]
    public async Task<IActionResult> GetBooks([FromQuery] string search, [FromQuery] string genre)
    {
        var books = await _bookService.SearchBooksAsync(search, genre);
        return Ok(books);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBookById(int id)
    {
        var book = await _bookService.GetBookByIdAsync(id);
        return Ok(book);
    }
    
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateBook(CreateBookDto dto)
    {
        var book = await _bookService.CreateBookAsync(dto);
        return CreatedAtAction(nameof(GetBookById), new { id = book.BookId }, book);
    }
    
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateBook(int id, UpdateBookDto dto)
    {
        await _bookService.UpdateBookAsync(id, dto);
        return NoContent();
    }
    
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteBook(int id)
    {
        await _bookService.DeleteBookAsync(id);
        return NoContent();
    }
    
    // CRITICAL: Internal endpoint for LoanService during Saga
    [HttpPut("{id}/stock")]
    [Authorize]  // Authenticated service-to-service call
    public async Task<IActionResult> UpdateStock(int id, UpdateStockDto dto)
    {
        await _bookService.UpdateStockAsync(id, dto);
        return NoContent();
    }
}
```

#### Dependency Injection (Program.cs)
```
// Database
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// Repositories
builder.Services.AddScoped<IBookRepository, EfBookRepository>();

// Application Services
builder.Services.AddScoped<BookApplicationService>();

// JWT Authentication (validate tokens from UserService)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
        };
    });

// CORS (for frontend)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

---

## Required NuGet Packages

```
<!-- Domain (no packages) -->

<!-- Application (no packages) -->

<!-- Infrastructure -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.*" />
<PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="8.0.*" />

<!-- Presentation -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.*" />
```

---

## Key Implementation Notes

1. **Stock Management is Critical**: DecrementStock/IncrementStock methods enforce business rules preventing negative stock
2. **Invariants Must Hold**: Always maintain `AvailableCopies <= TotalCopies` and `AvailableCopies >= 0`
3. **PUT /api/books/{id}/stock**: Internal endpoint called by LoanService during Saga - must be secured with JWT
4. **ISBN Uniqueness**: Validate before creating books
5. **Search Functionality**: Case-insensitive search on Title, Author, and ISBN
6. **Cannot Delete Active Books**: Check for loans before deletion
7. **Logging**: Log all admin operations (create, update, delete)
8. **Authorization**: Only Admins can create/update/delete books; search is public

