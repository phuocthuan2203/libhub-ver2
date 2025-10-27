# Task 1.3: Setup LoanService Database Schema

**Phase**: 1 - Database Setup  
**Service**: LoanService  
**Estimated Time**: 45 minutes  
**Dependencies**: Task 1.1, 1.2 (for reference)

---

## Objective

Create the LoanService database schema using Entity Framework Core migrations for the `loan_db` database. This completes Phase 1.

---

## Prerequisites

- [ ] Task 1.1 completed (UserService DB)
- [ ] Task 1.2 completed (CatalogService DB)
- [ ] loan_db exists in MySQL

---

## What You'll Create

1. Four LoanService class library projects
2. `LoanDbContext` with Loan entity configuration
3. Initial migration for Loans table
4. Applied migration to `loan_db` database

---

## Step-by-Step Instructions

### Step 1: Create LoanService Project Structure

cd ~/Projects/LibHub/src/Services/LoanService

Create four class libraries
dotnet new classlib -n LibHub.LoanService.Domain
dotnet new classlib -n LibHub.LoanService.Application
dotnet new classlib -n LibHub.LoanService.Infrastructure
dotnet new webapi -n LibHub.LoanService.Api

Add to solution
cd ~/Projects/LibHub
dotnet sln add src/Services/LoanService/LibHub.LoanService.Domain
dotnet sln add src/Services/LoanService/LibHub.LoanService.Application
dotnet sln add src/Services/LoanService/LibHub.LoanService.Infrastructure
dotnet sln add src/Services/LoanService/LibHub.LoanService.Api

Set up project references
dotnet add src/Services/LoanService/LibHub.LoanService.Application reference src/Services/LoanService/LibHub.LoanService.Domain
dotnet add src/Services/LoanService/LibHub.LoanService.Infrastructure reference src/Services/LoanService/LibHub.LoanService.Application
dotnet add src/Services/LoanService/LibHub.LoanService.Api reference src/Services/LoanService/LibHub.LoanService.Application
dotnet add src/Services/LoanService/LibHub.LoanService.Api reference src/Services/LoanService/LibHub.LoanService.Infrastructure

text

### Step 2: Add NuGet Packages to Infrastructure

cd ~/Projects/LibHub/src/Services/LoanService/LibHub.LoanService.Infrastructure

dotnet add package Microsoft.EntityFrameworkCore --version 8.0.*
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.*
dotnet add package Pomelo.EntityFrameworkCore.MySql --version 8.0.*

text

### Step 3: Create Loan Entity (Domain Layer)

**File**: `LibHub.LoanService.Domain/Loan.cs`

namespace LibHub.LoanService.Domain;

public class Loan
{
public int LoanId { get; private set; }
public int UserId { get; private set; }
public int BookId { get; private set; }
public DateTime CheckoutDate { get; private set; }
public DateTime DueDate { get; private set; }
public DateTime? ReturnDate { get; private set; }
public string Status { get; private set; } = string.Empty;

text
public bool IsOverdue => Status == "CheckedOut" && DateTime.UtcNow > DueDate;

// EF Core requires parameterless constructor
private Loan() { }

public Loan(int userId, int bookId)
{
    if (userId <= 0) 
        throw new ArgumentException("Invalid user ID", nameof(userId));
    if (bookId <= 0) 
        throw new ArgumentException("Invalid book ID", nameof(bookId));

    UserId = userId;
    BookId = bookId;
    CheckoutDate = DateTime.UtcNow;
    DueDate = CheckoutDate.AddDays(14);
    Status = "PENDING";
}

public void MarkAsCheckedOut()
{
    if (Status != "PENDING")
        throw new InvalidOperationException("Can only mark PENDING loans as checked out");
    
    Status = "CheckedOut";
}

public void MarkAsFailed()
{
    if (Status != "PENDING")
        throw new InvalidOperationException("Can only mark PENDING loans as failed");
    
    Status = "FAILED";
}

public void MarkAsReturned()
{
    if (Status != "CheckedOut")
        throw new InvalidOperationException("Can only return checked out loans");
    
    Status = "Returned";
    ReturnDate = DateTime.UtcNow;
}
}

text

### Step 4: Create LoanDbContext (Infrastructure Layer)

**File**: `LibHub.LoanService.Infrastructure/LoanDbContext.cs`

using Microsoft.EntityFrameworkCore;
using LibHub.LoanService.Domain;

namespace LibHub.LoanService.Infrastructure;

public class LoanDbContext : DbContext
{
public DbSet<Loan> Loans { get; set; }

text
public LoanDbContext(DbContextOptions<LoanDbContext> options) 
    : base(options)
{
}

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Loan>(entity =>
    {
        entity.HasKey(e => e.LoanId);
        
        entity.Property(e => e.UserId)
            .IsRequired();
        
        entity.Property(e => e.BookId)
            .IsRequired();
        
        entity.Property(e => e.CheckoutDate)
            .IsRequired();
        
        entity.Property(e => e.DueDate)
            .IsRequired();
        
        entity.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(20);
        
        // Create indexes for query performance
        entity.HasIndex(e => e.UserId);
        entity.HasIndex(e => e.BookId);
        entity.HasIndex(e => e.Status);
        entity.HasIndex(e => e.DueDate);
    });
}
}

text

### Step 5: Create Design-Time Factory

**File**: `LibHub.LoanService.Infrastructure/DesignTimeDbContextFactory.cs`

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LibHub.LoanService.Infrastructure;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LoanDbContext>
{
public LoanDbContext CreateDbContext(string[] args)
{
var optionsBuilder = new DbContextOptionsBuilder<LoanDbContext>();

text
    var connectionString = "Server=localhost;Port=3306;Database=loan_db;User=libhub_user;Password=LibHub@Dev2025;";
    
    optionsBuilder.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    );

    return new LoanDbContext(optionsBuilder.Options);
}
}

text

### Step 6: Create and Apply Migration

cd ~/Projects/LibHub/src/Services/LoanService/LibHub.LoanService.Infrastructure

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
USE loan_db;
SHOW TABLES;
DESC Loans;
SHOW INDEX FROM Loans;
EXIT;

text

**Expected output**:
- Table `Loans` exists
- Columns: LoanId, UserId, BookId, CheckoutDate, DueDate, ReturnDate, Status
- Indexes on UserId, BookId, Status, DueDate
- **NOTE**: No foreign key constraints (Database per Service pattern)

---

## Acceptance Criteria

- [ ] Four LoanService projects created
- [ ] Loan entity with state machine methods
- [ ] LoanDbContext with all indexes configured
- [ ] Initial migration created
- [ ] Migration applied to loan_db
- [ ] Loans table exists with correct schema
- [ ] All four indexes created (UserId, BookId, Status, DueDate)
- [ ] **No foreign keys** to Users or Books (different databases)
- [ ] No compilation errors
- [ ] Phase 1 is 100% complete

---

## Verification Commands

Build entire solution
cd ~/Projects/LibHub
dotnet build

Verify all three databases
mysql -u libhub_user -pLibHub@Dev2025 << EOF
SELECT 'UserService DB:' AS Info;
USE user_db; SHOW TABLES;
SELECT 'CatalogService DB:' AS Info;
USE catalog_db; SHOW TABLES;
SELECT 'LoanService DB:' AS Info;
USE loan_db; SHOW TABLES;
EOF

text

---

## After Completion

### Update PROJECT_STATUS.md

Update **Phase Status Overview**:
| Phase 1: Database Setup | ✅ COMPLETE | 100% (3/3) | All databases ready |
| Phase 2: UserService | ⚪ READY TO START | 0% | No blockers |

text

Add to **Completed Tasks**:
✅ Task 1.3: LoanService database schema created (2025-10-27)

Files Created: Loan.cs, LoanDbContext.cs, InitialCreate migration

Verification: Loans table exists with all indexes, no FK constraints

Phase 1 Complete: All three service databases ready

text

Update **Service Readiness Status**:
| Service | Database | Domain | Application | Infrastructure | Presentation | Tests | Ready? |
| UserService | ✅ | ⚪ | ⚪ | 🟡 | ⚪ | ⚪ | ❌ |
| CatalogService | ✅ | ⚪ | ⚪ | 🟡 | ⚪ | ⚪ | ❌ |
| LoanService | ✅ | ⚪ | ⚪ | 🟡 | ⚪ | ⚪ | ❌ |

text

Update **Overall Progress**:
Overall Progress: 15% (3/20 tasks complete)

text

Update **Next Steps**:
Immediate Next Task
Task 2.1: Implement UserService Domain Layer

What to do:

Implement complete User entity with all business logic

Implement IUserRepository interface

Add BCrypt password hashing

Add unit tests for entity validation

Phase 2 is now unblocked and ready to start!

text

### Git Commit
git add src/Services/LoanService/
git commit -m "✅ Task 1.3: Setup LoanService database schema"
git add ai-docs/PROJECT_STATUS.md
git commit -m "docs: Update status - Phase 1 complete! 🎉"

text

### Move Task File
mv ai-docs/tasks/phase-1-database/task-1.3-setup-loanservice-db.md ai-docs/completed-artifacts/

text

---

## 🎉 Phase 1 Complete!

All three service databases are now ready:
- ✅ UserService → user_db
- ✅ CatalogService → catalog_db  
- ✅ LoanService → loan_db

**Next**: Phase 2 - Implement UserService business logic

---

## Next Task

**Task 2.1**: Implement UserService Domain Layer (Password hashing, repository interface)