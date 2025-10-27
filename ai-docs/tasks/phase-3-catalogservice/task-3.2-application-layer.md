# Task 3.2: Implement CatalogService Application Layer

**Phase**: 3 - CatalogService Implementation  
**Layer**: Application  
**Estimated Time**: 1.5-2 hours  
**Dependencies**: Task 3.1 (Domain Layer)

---

## Objective

Implement the Application Layer for CatalogService with DTOs, application services for CRUD operations, and business workflow orchestration.

---

## Prerequisites

- [ ] Task 3.1 completed (Domain Layer with Book entity)
- [ ] Understanding of catalog management workflows

---

## What You'll Implement

1. Data Transfer Objects (DTOs) for all CRUD operations
2. BookApplicationService for catalog management
3. Search and filtering logic
4. Stock update operations (for Saga integration)

---

## Step-by-Step Instructions

### Step 1: Create DTOs

**File**: `LibHub.CatalogService.Application/DTOs/CreateBookDto.cs`

namespace LibHub.CatalogService.Application.DTOs;

public class CreateBookDto
{
public string Isbn { get; set; } = string.Em
ty; public string Title { get; set; } = strin
.Empty; public string Author { get; set; } = s
ring.Empty; public string Genre { get; set; }
= string.Empty; public string? Descr
ption { get; set; } public int T
text

**File**: `LibHub.CatalogService.Application/DTOs/UpdateBookDto.cs`

namespace LibHub.CatalogService.Application.DTOs;

public class UpdateBookDto
{
public string Title { get; set; } = string.Em
ty; public string Author { get; set; } = strin
.Empty; public string Genre { get; set; } = s
ring.Empty; public string? Descripti
text

**File**: `LibHub.CatalogService.Application/DTOs/BookDto.cs`

namespace LibHub.CatalogService.Application.DTOs;

public class BookDto
{
public int BookId { get; se
; } public string Isbn { get; set; } = strin
.Empty; public string Title { get; set; } = s
ring.Empty; public string Author { get; set; }
= string.Empty; public string Genre { get; se
; } = string.Empty; public string? D
scription { get; set; } public i
t TotalCopies { get; set; } public i
t AvailableCopies { get; set; } p
blic bool IsAvailable { get; set; }
text

**File**: `LibHub.CatalogService.Application/DTOs/UpdateStockDto.cs`

namespace LibHub.CatalogService.Application.DTOs;

/// <summary>
/// DTO for updating book stock (used by LoanService via Saga).
</summary>
public class UpdateStockDto
{
public int ChangeAmount { get; set; } // -1 for borrow, +1 for re
text

### Step 2: Create BookApplicationService

**File**: `LibHub.CatalogService.Application/Services/BookApplicationService.cs`

using LibHub.CatalogService.Application.DTOs;
namespace LibHub.CatalogService.Application.Services;

public class BookApplicationService
{
private

text
public BookApplicationService(IBookRepository bookRepository)
{
    _bookRepository = bookRepository;
}

/// <summary>
/// Create a new book in the catalog (admin operation).
/// </summary>
public async Task<BookDto> CreateBookAsync(CreateBookDto dto)
{
    // Check if ISBN already exists
    var existingBook = await _bookRepository.GetByIsbnAsync(dto.Isbn);
    if (existingBook != null)
        throw new InvalidOperationException("A book with this ISBN already exists");

    // Create domain entity
    var book = new Book(
        dto.Isbn,
        dto.Title,
        dto.Author,
        dto.Genre,
        dto.Description,
        dto.TotalCopies);

    // Persist
    await _bookRepository.AddAsync(book);

    // Return DTO
    return MapToDto(book);
}

/// <summary>
/// Get book by ID.
/// </summary>
public async Task<BookDto> GetBookByIdAsync(int bookId)
{
    var book = await _bookRepository.GetByIdAsync(bookId);
    if (book == null)
        throw new KeyNotFoundException($"Book with ID {bookId} not found");

    return MapToDto(book);
}

/// <summary>
/// Search books by title, author, ISBN, or genre.
/// </summary>
public async Task<List<BookDto>> SearchBooksAsync(string? searchTerm = null, string? genre = null)
{
    var books = await _bookRepository.SearchAsync(searchTerm, genre);
    return books.Select(MapToDto).ToList();
}

/// <summary>
/// Get all books in catalog.
/// </summary>
public async Task<List<BookDto>> GetAllBooksAsync()
{
    var books = await _bookRepository.GetAllAsync();
    return books.Select(MapToDto).ToList();
}

/// <summary>
/// Update book details (admin operation).
/// </summary>
public async Task UpdateBookAsync(int bookId, UpdateBookDto dto)
{
    var book = await _bookRepository.GetByIdAsync(bookId);
    if (book == null)
        throw new KeyNotFoundException($"Book with ID {bookId} not found");

    book.UpdateDetails(dto.Title, dto.Author, dto.Genre, dto.Description);
    await _bookRepository.UpdateAsync(book);
}

/// <summary>
/// Delete book from catalog (admin operation).
/// </summary>
public async Task DeleteBookAsync(int bookId)
{
    var book = await _bookRepository.GetByIdAsync(bookId);
    if (book == null)
        throw new KeyNotFoundException($"Book with ID {bookId} not found");

    // Business rule: Cannot delete book with active loans
    if (book.CopiesOnLoan() > 0)
        throw new InvalidOperationException(
            $"Cannot delete book with {book.CopiesOnLoan()} active loans");

    await _bookRepository.DeleteAsync(bookId);
}

/// <summary>
/// Update stock (called by LoanService during Saga).
/// CRITICAL: This is used for distributed transactions.
/// </summary>
public async Task UpdateStockAsync(int bookId, UpdateStockDto dto)
{
    var book = await _bookRepository.GetByIdAsync(bookId);
    if (book == null)
        throw new KeyNotFoundException($"Book with ID {bookId} not found");

    // Execute domain logic based on change amount
    if (dto.ChangeAmount < 0)
    {
        // Decrement (borrow operation)
        book.DecrementStock();
    }
    else if (dto.ChangeAmount > 0)
    {
        // Increment (return operation)
        book.IncrementStock();
    }

    await _bookRepository.UpdateAsync(book);
}

// Helper method to map entity to DTO
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

text

### Step 3: Add Project Reference

cd ~/Projects/LibHub

Application layer depends on Domain layer
dotnet add src/Services/CatalogService/LibHub.CatalogService.Application reference src/Services/CatalogService/LibHub.CatalogService.Domain

text

### Step 4: Verify Application Layer

cd ~/Projects/LibHub/src/Services/CatalogService/LibHub.CatalogService.Application

Build the project
dotnet build

text

---

## Acceptance Criteria

- [ ] All DTOs created (CreateBookDto, UpdateBookDto, BookDto, UpdateStockDto)
- [ ] BookApplicationService with complete CRUD operations
- [ ] ISBN uniqueness checking in CreateBookAsync
- [ ] UpdateStockAsync for Saga integration (critical!)
- [ ] DeleteBookAsync prevents deletion of books with active loans
- [ ] Search functionality with optional filters
- [ ] MapToDto helper method
- [ ] Project reference to Domain layer
- [ ] No compilation errors
- [ ] No dependencies on Infrastructure or Presentation layers

---

## Verification Commands

Build application project
cd ~/Projects/LibHub/src/Services/CatalogService/LibHub.CatalogService.Application
Verify project references
dotnet list reference

Expected: Only Domain layer reference
text

---

## Key Implementation Notes

1. **UpdateStockAsync is Critical**: LoanService calls this during Saga for distributed transactions
2. **ISBN Uniqueness**: Checked at application layer before creating book
3. **Delete Protection**: Cannot delete books with active loans (copies on loan > 0)
4. **Search Flexibility**: Supports search by any text (title/author/ISBN) + genre filter
5. **IsAvailable**: Calculated from domain entity, not stored
6. **Exception Handling**: Use KeyNotFoundException for not found, InvalidOperationException for business rule violations

---

## After Completion

### Update PROJECT_STATUS.md

Add to **Completed Tasks**:
✅ Task 3.2: CatalogService Application Layer implemented (2025-10-27)

Files Created:

DTOs (CreateBookDto, UpdateBookDto, BookDto, UpdateStockDto)

BookApplicationService

Verification: Application layer orchestrates catalog operations

Critical: UpdateStockAsync ready for Saga integration

text

Update **Service Readiness Status**:
| CatalogService | ✅ | ✅ | ✅ | 🟡 | ⚪ | ⚪ | ❌ |

text

Update **Overall Progress**:
Overall Progress: 50% (10/20 tasks complete)

text

### Git Commit
git add src/Services/CatalogService/LibHub.CatalogService.Application/
git commit -m "✅ Task 3.2: Implement CatalogService Application Layer"
git add ai-docs/PROJECT_STATUS.md
text

### Move Task File
mv ai-docs/tasks/phase-3-catalogservice/task-3.2-application-layer.md ai-docs/completed-artifacts/

text

---

## Next Task

**Task 3.3**: Implement CatalogService Infrastructure Layer (EF Core repository)