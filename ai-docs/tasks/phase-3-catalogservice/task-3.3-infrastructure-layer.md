# Task 3.3: Implement CatalogService Infrastructure Layer

**Phase**: 3 - CatalogService Implementation  
**Layer**: Infrastructure  
**Estimated Time**: 1.5-2 hours  
**Dependencies**: Task 3.2 (Application Layer)

---

## Objective

Implement the Infrastructure Layer for CatalogService with EF Core repository and search functionality.

---

## Prerequisites

- [ ] Task 3.1 completed (Domain Layer)
- [ ] Task 3.2 completed (Application Layer)
- [ ] CatalogDbContext exists from Task 1.2

---

## What You'll Implement

1. EfBookRepository implementing IBookRepository
2. Search functionality with case-insensitive filtering
3. Infrastructure service registrations

---

## Step-by-Step Instructions

### Step 1: Add Project References

cd ~/Projects/LibHub

Infrastructure depends on Application and Domain
dotnet add src/Services/CatalogService/LibHub.CatalogService.Infrastructure reference src/Services/CatalogService/LibHub.CatalogService.Application
dotnet add src/Ser

text

### Step 2: Implement EfBookRepository

**File**: `LibHub.CatalogService.Infrastructure/Repositories/EfBookRepository.cs`

using Microsoft.EntityFrameworkCore;
using LibHub.CatalogServi

namespace LibHub.CatalogService.Infrastructure.Repositories;

public class EfBookRepository : IBookRepository
{
private readonly CatalogDbContext _c

text
public EfBookRepository(CatalogDbContext context)
{
    _context = context;
}

public async Task<Book?> GetByIdAsync(int bookId)
{
    return await _context.Books.FindAsync(bookId);
}

public async Task<Book?> GetByIsbnAsync(string isbn)
{
    return await _context.Books
        .FirstOrDefaultAsync(b => b.Isbn == isbn);
}

public async Task<List<Book>> SearchAsync(string? searchTerm = null, string? genre = null)
{
    var query = _context.Books.AsQueryable();

    // Apply search filter (case-insensitive)
    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        searchTerm = searchTerm.ToLower();
        query = query.Where(b =>
            b.Title.ToLower().Contains(searchTerm) ||
            b.Author.ToLower().Contains(searchTerm) ||
            b.Isbn.Contains(searchTerm));
    }

    // Apply genre filter
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

text

### Step 3: Verify CatalogDbContext

Ensure CatalogDbContext is properly configured from Task 1.2:

**File**: `LibHub.CatalogService.Infrastructure/CatalogDbContext.cs` (verify)

using Microsoft.EntityFrameworkCore;
usi

namespace LibHub.CatalogService.Infrastructure;

public class CatalogDbContext : DbContext
{
<Book> Books { get; set; }

text
public CatalogDbContext(DbContextOptions<CatalogDbContext> options) 
    : base(options)
{
}

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Book>(entity =>
    {
        entity.HasKey(e => e.BookId);
        
        entity.Property(e => e.Isbn)
            .IsRequired()
            .HasMaxLength(13);
        
        entity.HasIndex(e => e.Isbn)
            .IsUnique();
        
        entity.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(255);
        
        entity.Property(e => e.Author)
            .IsRequired()
            .HasMaxLength(255);
        
        entity.Property(e => e.Genre)
            .IsRequired()
            .HasMaxLength(100);
        
        entity.Property(e => e.Description)
            .HasColumnType("TEXT");
        
        entity.Property(e => e.TotalCopies)
            .IsRequired();
        
        entity.Property(e => e.AvailableCopies)
            .IsRequired();
        
        entity.Property(e => e.CreatedAt)
            .IsRequired();
    });
}
}

text

### Step 4: Verify Infrastructure Layer

cd ~/Projects/LibHub/src/Services/CatalogService/LibHub.CatalogService.Infrastructure

Build the project
dotnet build

text

---

## Acceptance Criteria

- [ ] EfBookRepository implements all IBookRepository methods
- [ ] SearchAsync with case-insensitive search on Title, Author, ISBN
- [ ] Genre filtering works independently or combined with search
- [ ] Project references to Application and Domain layers
- [ ] CatalogDbContext properly configured
- [ ] No compilation errors
- [ ] All infrastructure implementations use dependency injection

---

## Verification Commands

Build infrastructure project
cd ~/Projects/LibHub/src/Services/CatalogService/LibHub.CatalogService.Infrastructure
Verify packages
dotnet list package

Expected:
- Microsoft.EntityFrameworkCore
- Pomelo.EntityFrameworkCore.MySql
Verify project references
dotnet list reference

Expected: Application and Domain references
text

---

## Key Implementation Notes

1. **Case-Insensitive Search**: Use `.ToLower()` for search terms
2. **Search Flexibility**: Searches Title, Author, and ISBN simultaneously
3. **Genre Filtering**: Can be used alone or combined with search
4. **SaveChangesAsync**: Called in repository methods after each operation
5. **ISBN Uniqueness**: Enforced at database level via unique index
6. **TEXT Column**: Description uses TEXT type for long content

---

## After Completion

### Update PROJECT_STATUS.md

Add to **Completed Tasks**:
✅ Task 3.3: CatalogService Infrastructure Layer implemented (2025-10-27)

Files Created:

EfBookRepository (EF Core implementation with search)

Verification: Repository implements all CRUD and search operations

Search: Case-insensitive on Title, Author, ISBN

text

Update **Service Readiness Status**:
| CatalogService | ✅ | ✅ | ✅ | ✅ | ⚪ | ⚪ | ❌ |

text

Update **Overall Progress**:
Overall Progress: 55% (11/20 tasks complete)

text

### Git Commit
git add src/Services/CatalogService/LibHub.CatalogService.Infrastructure/
git commit -m "✅ Task 3.3: Implement CatalogService Infrastructure Layer"
git add ai-docs/PROJECT_STATUS.md
text

### Move Task File
mv ai-docs/tasks/phase-3-catalogservice/task-3.3-infrastructure-layer.md ai-docs/completed-artifacts/

text

---

## Next Task

**Task 3.4**: Implement CatalogService Presentation Layer (Controllers, DI, Swagger)