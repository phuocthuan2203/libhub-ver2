# Task 1.2: Setup CatalogService Database Schema

**Phase**: 1 - Database Setup  
**Service**: CatalogService  
**Estimated Time**: 45 minutes  
**Dependencies**: Task 1.1 (for reference structure)

---

## Objective

Create the CatalogService database schema using Entity Framework Core migrations for the `catalog_db` database.

---

## Prerequisites

- [ ] Task 1.1 completed (UserService DB setup)
- [ ] catalog_db exists in MySQL
- [ ] Familiar with project structure from Task 1.1

---

## What You'll Create

1. Four CatalogService class library projects
2. `CatalogDbContext` with Book entity configuration
3. Initial migration for Books table
4. Applied migration to `catalog_db` database

---

## Step-by-Step Instructions

### Step 1: Create CatalogService Project Structure

cd ~/Projects/LibHub/src/Services/CatalogService

Create four class libraries
dotnet new classlib -n LibHub.CatalogService.Domain
dotnet new classlib -n LibHub.CatalogService.Application
dotnet new classlib -n LibHub.CatalogService.Infrastructure
dotnet new webapi -n LibHub.CatalogService.Api

Add to solution
cd ~/Projects/LibHub
dotnet sln add src/Services/CatalogService/LibHub.CatalogService.Domain
dotnet sln add src/Services/CatalogService/LibHub.CatalogService.Application
dotnet sln add src/Services/CatalogService/LibHub.CatalogService.Infrastructure
dotnet sln add src/Services/CatalogService/LibHub.CatalogService.Api

Set up project references
dotnet add src/Services/CatalogService/LibHub.CatalogService.Application reference src/Services/CatalogService/LibHub.CatalogService.Domain
dotnet add src/Services/CatalogService/LibHub.CatalogService.Infrastructure reference src/Services/CatalogService/LibHub.CatalogService.Application
dotnet add src/Services/CatalogService/LibHub.CatalogService.Api reference src/Services/CatalogService/LibHub.CatalogService.Application
dotnet add src/Services/CatalogService/LibHub.CatalogService.Api reference src/Services/CatalogService/LibHub.CatalogService.Infrastructure

text

### Step 2: Add NuGet Packages to Infrastructure

cd ~/Projects/LibHub/src/Services/CatalogService/LibHub.CatalogService.Infrastructure

dotnet add package Microsoft.EntityFrameworkCore --version 8.0.*
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.*
dotnet add package Pomelo.EntityFrameworkCore.MySql --version 8.0.*

text

### Step 3: Create Book Entity (Domain Layer)

**File**: `LibHub.CatalogService.Domain/Book.cs`

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

text
// EF Core requires parameterless constructor
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

text

### Step 4: Create CatalogDbContext (Infrastructure Layer)

**File**: `LibHub.CatalogService.Infrastructure/CatalogDbContext.cs`

using Microsoft.EntityFrameworkCore;
using LibHub.CatalogService.Domain;

namespace LibHub.CatalogService.Infrastructure;

public class CatalogDbContext : DbContext
{
public DbSet<Book> Books { get; set; }

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

### Step 5: Create Design-Time Factory

**File**: `LibHub.CatalogService.Infrastructure/DesignTimeDbContextFactory.cs`

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LibHub.CatalogService.Infrastructure;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
public CatalogDbContext CreateDbContext(string[] args)
{
var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();

text
    var connectionString = "Server=localhost;Port=3306;Database=catalog_db;User=libhub_user;Password=LibHub@Dev2025;";
    
    optionsBuilder.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    );

    return new CatalogDbContext(optionsBuilder.Options);
}
}

text

### Step 6: Create and Apply Migration

cd ~/Projects/LibHub/src/Services/CatalogService/LibHub.CatalogService.Infrastructure

Create initial migration
dotnet ef migrations add InitialCreate

Apply migration to database
dotnet ef database update

text

### Step 7: Verify Database Creation

mysql -u libhub_user -p

Password: LibHub@Dev2025
text

In MySQL:
USE catalog_db;
SHOW TABLES;
DESC Books;
SHOW INDEX FROM Books;
EXIT;

text

**Expected output**:
- Table `Books` exists
- Columns: BookId, Isbn, Title, Author, Genre, Description, TotalCopies, AvailableCopies, CreatedAt, UpdatedAt
- Unique index on Isbn

---

## Acceptance Criteria

- [ ] Four CatalogService projects created and added to solution
- [ ] Book entity with stock management methods
- [ ] CatalogDbContext configured with all constraints
- [ ] Initial migration created
- [ ] Migration applied to catalog_db
- [ ] Books table exists with correct schema
- [ ] Unique constraint on ISBN verified
- [ ] TEXT column type for Description
- [ ] No compilation errors

---

## Verification Commands

Build solution
cd ~/Projects/LibHub
dotnet build

Verify database
mysql -u libhub_user -pLibHub@Dev2025 -e "USE catalog_db; SHOW TABLES; DESC Books;"

text

---

## After Completion

### Update PROJECT_STATUS.md

Add to **Completed Tasks**:
✅ Task 1.2: CatalogService database schema created (2025-10-27)

Files Created: Book.cs, CatalogDbContext.cs, InitialCreate migration

Verification: Books table exists in catalog_db with ISBN unique constraint

text

Update **Service Readiness Status**:
| CatalogService | ✅ | ⚪ | ⚪ | 🟡 | ⚪ | ⚪ | ❌ |

text

Update **Phase Status**:
| Phase 1: Database Setup | 🟡 IN PROGRESS | 66% (2/3) | CatalogService DB done |

text

### Git Commit
git add src/Services/CatalogService/
git commit -m "✅ Task 1.2: Setup CatalogService database schema"
git add ai-docs/PROJECT_STATUS.md
git commit -m "docs: Update status after Task 1.2"

text

### Move Task File
mv ai-docs/tasks/phase-1-database/task-1.2-setup-catalogservice-db.md ai-docs/completed-artifacts/

text

---

## Next Task

**Task 1.3**: Setup LoanService Database Schema