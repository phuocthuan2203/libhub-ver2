# Task 1.1: Setup UserService Database Schema

**Phase**: 1 - Database Setup  
**Service**: UserService  
**Estimated Time**: 45 minutes  
**Dependencies**: None

---

## Objective

Create the UserService database schema using Entity Framework Core migrations for the `user_db` database.

---

## Prerequisites

- [ ] Phase 0 completed (MySQL installed, user_db created)
- [ ] .NET 8 SDK installed
- [ ] LibHub.sln exists in `~/Projects/LibHub/`

---

## What You'll Create

1. Four class library projects following Clean Architecture
2. `UserDbContext` with Entity Framework Core configuration
3. Initial migration for Users table
4. Applied migration to `user_db` database

---

## Step-by-Step Instructions

### Step 1: Create UserService Project Structure

cd ~/Projects/LibHub/src/Services/UserService

Create four class libraries
dotnet new classlib -n LibHub.UserService.Domain
dotnet new classlib -n LibHub.UserService.Application
dotnet new classlib -n LibHub.UserService.Infrastructure
dotnet new webapi -n LibHub.UserService.Api

Add to solution
cd ~/Projects/LibHub
dotnet sln add src/Services/UserService/LibHub.UserService.Domain
dotnet sln add src/Services/UserService/LibHub.UserService.Application
dotnet sln add src/Services/UserService/LibHub.UserService.Infrastructure
dotnet sln add src/Services/UserService/LibHub.UserService.Api

Set up project references
dotnet add src/Services/UserService/LibHub.UserService.Application reference src/Services/UserService/LibHub.UserService.Domain
dotnet add src/Services/UserService/LibHub.UserService.Infrastructure reference src/Services/UserService/LibHub.UserService.Application
dotnet add src/Services/UserService/LibHub.UserService.Api reference src/Services/UserService/LibHub.UserService.Application
dotnet add src/Services/UserService/LibHub.UserService.Api reference src/Services/UserService/LibHub.UserService.Infrastructure

text

### Step 2: Add NuGet Packages to Infrastructure

cd ~/Projects/LibHub/src/Services/UserService/LibHub.UserService.Infrastructure

dotnet add package Microsoft.EntityFrameworkCore --version 8.0.*
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.*
dotnet add package Pomelo.EntityFrameworkCore.MySql --version 8.0.*

text

### Step 3: Create User Entity (Domain Layer)

**File**: `LibHub.UserService.Domain/User.cs`

namespace LibHub.UserService.Domain;

public class User
{
public int UserId { get; private set; }
public string Username { get; private set; } = string.Empty;
public string Email { get; private set; } = string.Empty;
public string HashedPassword { get; private set; } = string.Empty;
public string Role { get; private set; } = string.Empty;
public DateTime CreatedAt { get; private set; }
public DateTime? UpdatedAt { get; private set; }

text
// EF Core requires parameterless constructor
private User() { }

// Constructor for creating new users
public User(string username, string email, string hashedPassword, string role)
{
    if (string.IsNullOrWhiteSpace(username))
        throw new ArgumentException("Username is required", nameof(username));
    if (string.IsNullOrWhiteSpace(email))
        throw new ArgumentException("Email is required", nameof(email));
    if (role != "Customer" && role != "Admin")
        throw new ArgumentException("Invalid role", nameof(role));

    Username = username;
    Email = email.ToLowerInvariant();
    HashedPassword = hashedPassword;
    Role = role;
    CreatedAt = DateTime.UtcNow;
}
}

text

### Step 4: Create UserDbContext (Infrastructure Layer)

**File**: `LibHub.UserService.Infrastructure/UserDbContext.cs`

using Microsoft.EntityFrameworkCore;
using LibHub.UserService.Domain;

namespace LibHub.UserService.Infrastructure;

public class UserDbContext : DbContext
{
public DbSet<User> Users { get; set; }

text
public UserDbContext(DbContextOptions<UserDbContext> options) 
    : base(options)
{
}

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<User>(entity =>
    {
        entity.HasKey(e => e.UserId);
        
        entity.Property(e => e.Username)
            .IsRequired()
            .HasMaxLength(100);
        
        entity.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(255);
        
        entity.HasIndex(e => e.Email)
            .IsUnique();
        
        entity.Property(e => e.HashedPassword)
            .IsRequired()
            .HasMaxLength(255);
        
        entity.Property(e => e.Role)
            .IsRequired()
            .HasMaxLength(20);
        
        entity.Property(e => e.CreatedAt)
            .IsRequired();
    });
}
}

text

### Step 5: Create Design-Time Factory

**File**: `LibHub.UserService.Infrastructure/DesignTimeDbContextFactory.cs`

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LibHub.UserService.Infrastructure;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
{
public UserDbContext CreateDbContext(string[] args)
{
var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();

text
    var connectionString = "Server=localhost;Port=3306;Database=user_db;User=libhub_user;Password=LibHub@Dev2025;";
    
    optionsBuilder.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    );

    return new UserDbContext(optionsBuilder.Options);
}
}

text

### Step 6: Create and Apply Migration

cd ~/Projects/LibHub/src/Services/UserService/LibHub.UserService.Infrastructure

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
USE user_db;
SHOW TABLES;
DESC Users;
EXIT;

text

**Expected output**:
- Table `Users` exists
- Columns: UserId, Username, Email, HashedPassword, Role, CreatedAt, UpdatedAt
- Unique index on Email

---

## Acceptance Criteria

- [ ] Four UserService projects created and added to solution
- [ ] User entity created with private setters
- [ ] UserDbContext configured with entity mappings
- [ ] Initial migration created in `Migrations/` folder
- [ ] Migration applied successfully to `user_db`
- [ ] Users table exists in database with correct schema
- [ ] Unique constraint on Email column verified
- [ ] No compilation errors

---

## Verification Commands

Verify projects exist
ls ~/Projects/LibHub/src/Services/UserService/

Verify migration files
ls ~/Projects/LibHub/src/Services/UserService/LibHub.UserService.Infrastructure/Migrations/

Verify database
mysql -u libhub_user -pLibHub@Dev2025 -e "USE user_db; SHOW TABLES;"

Build solution
cd ~/Projects/LibHub
dotnet build

text

---

## Troubleshooting

### Issue: Migration command not found
**Solution**: Install EF Core tools globally
dotnet tool install --global dotnet-ef

text

### Issue: Cannot connect to database
**Solution**: Verify MySQL is running and credentials are correct
sudo systemctl status mysql
mysql -u libhub_user -p

text

### Issue: Build errors
**Solution**: Verify all project references are correct
cd ~/Projects/LibHub
dotnet restore

text

---

## After Completion

### Update PROJECT_STATUS.md

Add to **Completed Tasks** section:
Phase 1: Database Setup
✅ Task 1.1: UserService database schema created (2025-10-27)

Files Created:

LibHub.UserService.Domain/User.cs

LibHub.UserService.Infrastructure/UserDbContext.cs

LibHub.UserService.Infrastructure/DesignTimeDbContextFactory.cs

LibHub.UserService.Infrastructure/Migrations/[timestamp]_InitialCreate.cs

Verification: Users table exists in user_db

Connection String: Working with libhub_user credentials

text

Update **Service Readiness Status**:
| Service | Database | Domain | Application | Infrastructure | Presentation | Tests | Ready? |
| UserService | ✅ | ⚪ | ⚪ | 🟡 (DbContext only) | ⚪ | ⚪ | ❌ |

text

### Git Commit
git add src/Services/UserService/
git commit -m "✅ Task 1.1: Setup UserService database schema"
git add ai-docs/PROJECT_STATUS.md
git commit -m "docs: Update status after Task 1.1"

text

### Move Task File
mv ai-docs/tasks/phase-1-database/task-1.1-setup-userservice-db.md ai-docs/completed-artifacts/

text

---

## Next Task

**Task 1.2**: Setup CatalogService Database Schema