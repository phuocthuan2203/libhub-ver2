# Task 3.1: Implement CatalogService Domain Layer

**Phase**: 3 - CatalogService Implementation  
**Layer**: Domain  
**Estimated Time**: 1-2 hours  
**Dependencies**: Task 1.2 (CatalogService DB), Task 2.5 (UserService complete)

---

## Objective

Implement the complete Domain Layer for CatalogService with Book entity, stock management business logic, and repository interface.

---

## Prerequisites

- [ ] Task 1.2 completed (CatalogService database schema exists)
- [ ] Basic Book entity created during Task 1.2
- [ ] Understanding of inventory management business rules

---

## What You'll Implement

1. Complete Book entity with stock management methods
2. IBookRepository interface with search capabilities
3. Stock increment/decrement business logic
4. Domain-level validation for ISBN, inventory constraints

---

## Step-by-Step Instructions

### Step 1: Complete Book Entity

**File**: `LibHub.CatalogService.Domain/Book.cs`

Replace the existing basic entity with this complete implementation:

namespace LibHub.CatalogService.Domain;

public class Book
{
public int BookId { get; private se
; } public string Isbn { get; private set; } = strin
.Empty; public string Title { get; private set; } = s
ring.Empty; public string Author { get; private set; }
= string.Empty; public string Genre { get; private se
; } = string.Empty; public string? Descripti
n { get; private set; } public int Total
opies { get; private set; } public int Avail
bleCopies { get; private set; } public Date
text
// EF Core requires parameterless constructor
private Book() { }

// Constructor for creating new books
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
    AvailableCopies = totalCopies; // All copies available initially
    CreatedAt = DateTime.UtcNow;
}

// CRITICAL: Stock management methods for Saga pattern
public void DecrementStock()
{
    if (AvailableCopies <= 0)
        throw new InvalidOperationException("No copies available to borrow");
    
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

// Update book details (admin operation)
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

// Adjust total inventory (admin operation)
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

// Query methods
public bool IsAvailable() => AvailableCopies > 0;
public int CopiesOnLoan() => TotalCopies - AvailableCopies;

// Private validation methods
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

text

### Step 2: Create IBookRepository Interface

**File**: `LibHub.CatalogService.Domain/IBookRepository.cs`

namespace LibHub.CatalogService.Domain;

/// <summary>
/// Repository interface for Book entity.
/// Implementation will be in Infrastructure layer using EF Core.
</summary>
public interface IBookRepository
{
<summary>
/// Get book by
ID. </summary>
Task<Book?> GetByIdAsync(int boo

text
/// <summary>
/// Get book by ISBN.
/// </summary>
Task<Book?> GetByIsbnAsync(string isbn);

/// <summary>
/// Search books by title, author, or ISBN.
/// </summary>
/// <param name="searchTerm">Search term (optional)</param>
/// <param name="genre">Filter by genre (optional)</param>
Task<List<Book>> SearchAsync(string? searchTerm = null, string? genre = null);

/// <summary>
/// Get all books.
/// </summary>
Task<List<Book>> GetAllAsync();

/// <summary>
/// Add new book to catalog.
/// </summary>
Task AddAsync(Book book);

/// <summary>
/// Update existing book.
/// </summary>
Task UpdateAsync(Book book);

/// <summary>
/// Delete book from catalog.
/// </summary>
Task DeleteAsync(int bookId);
}

text

### Step 3: Verify Domain Layer

cd ~/Projects/LibHub/src/Services/CatalogService/LibHub.CatalogService.Domain

Build the project
dotnet build

text

---

## Acceptance Criteria

- [ ] Book entity with complete validation and stock management
- [ ] DecrementStock and IncrementStock methods enforce business rules
- [ ] ISBN validation (13 digits, numeric only)
- [ ] Invariant maintained: AvailableCopies <= TotalCopies
- [ ] UpdateDetails method for admin operations
- [ ] AdjustTotalCopies prevents reducing below on-loan count
- [ ] IBookRepository with search capability
- [ ] No external dependencies (only .NET primitives)
- [ ] All validation throws ArgumentException with clear messages
- [ ] No compilation errors

---

## Verification Commands

Build domain project
cd ~/Projects/LibHub/src/Services/CatalogService/LibHub.CatalogService.Domain
Verify no external package dependencies
dotnet list package

Expected: No packages (pure C#)
text

---

## After Completion

### Update PROJECT_STATUS.md

Add to **Completed Tasks**:
Phase 3: CatalogService
✅ Task 3.1: CatalogService Domain Layer implemented (2025-10-27)

Files Created:

Book.cs (with stock management)

IBookRepository.cs

Verification: Domain layer has zero external dependencies

Business Rules: ISBN validation, stock constraints, inventory management

text

Update **Service Readiness Status**:
| CatalogService | ✅ | ✅ | ⚪ | 🟡 | ⚪ | ⚪ | ❌ |

text

Update **Overall Progress**:
Overall Progress: 45% (9/20 tasks complete)

text

### Git Commit
git add src/Services/CatalogService/LibHub.CatalogService.Domain/
git commit -m "✅ Task 3.1: Implement CatalogService Domain Layer"
git add ai-docs/PROJECT_STATUS.md
text

### Move Task File
mv ai-docs/tasks/phase-3-catalogservice/task-3.1-domain-layer.md ai-docs/completed-artifacts/

text

---

## Next Task

**Task 3.2**: Implement CatalogService Application Layer (DTOs, BookApplicationService)
