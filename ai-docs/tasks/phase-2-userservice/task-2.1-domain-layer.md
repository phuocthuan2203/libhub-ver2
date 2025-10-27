# Task 2.1: Implement UserService Domain Layer

**Phase**: 2 - UserService Implementation  
**Layer**: Domain  
**Estimated Time**: 1-2 hours  
**Dependencies**: Task 1.1 (UserService DB)

---

## Objective

Implement the complete Domain Layer for UserService with User entity, business rules, repository interface, and password hashing logic.

---

## Prerequisites

- [ ] Task 1.1 completed (UserService database schema exists)
- [ ] Basic User entity created during Task 1.1
- [ ] Understanding of Clean Architecture Domain Layer principles

---

## What You'll Implement

1. Complete User entity with all business logic and validation
2. IUserRepository interface with all query methods
3. Password verification method in User entity
4. Domain-level validation and business rules

---

## Step-by-Step Instructions

### Step 1: Complete User Entity

**File**: `LibHub.UserService.Domain/User.cs`

Replace the existing basic entity with this complete implementation:

namespace LibHub.UserService.Domain;

public class User
{
public int UserId { get; private se
; } public string Username { get; private set; } = strin
.Empty; public string Email { get; private set; } = s
ring.Empty; public string HashedPassword { get; private set; }
= string.Empty; public string Role { get; private se
; } = string.Empty; public DateTime Created
text
// EF Core requires parameterless constructor
private User() { }

// Constructor for creating new users
public User(string username, string email, string hashedPassword, string role)
{
    ValidateUsername(username);
    ValidateEmail(email);
    ValidateRole(role);
    ValidateHashedPassword(hashedPassword);

    Username = username;
    Email = email.ToLowerInvariant();
    HashedPassword = hashedPassword;
    Role = role;
    CreatedAt = DateTime.UtcNow;
}

// Business logic methods
public bool VerifyPassword(string plainPassword, IPasswordHasher passwordHasher)
{
    if (string.IsNullOrWhiteSpace(plainPassword))
        return false;

    return passwordHasher.Verify(HashedPassword, plainPassword);
}

public void UpdateProfile(string username, string email)
{
    ValidateUsername(username);
    ValidateEmail(email);

    Username = username;
    Email = email.ToLowerInvariant();
    UpdatedAt = DateTime.UtcNow;
}

// Private validation methods
private static void ValidateUsername(string username)
{
    if (string.IsNullOrWhiteSpace(username))
        throw new ArgumentException("Username is required", nameof(username));

    if (username.Length < 2)
        throw new ArgumentException("Username must be at least 2 characters", nameof(username));

    if (username.Length > 100)
        throw new ArgumentException("Username cannot exceed 100 characters", nameof(username));
}

private static void ValidateEmail(string email)
{
    if (string.IsNullOrWhiteSpace(email))
        throw new ArgumentException("Email is required", nameof(email));

    // Basic email format validation
    if (!email.Contains('@') || !email.Contains('.'))
        throw new ArgumentException("Email format is invalid", nameof(email));

    if (email.Length > 255)
        throw new ArgumentException("Email cannot exceed 255 characters", nameof(email));
}

private static void ValidateRole(string role)
{
    if (string.IsNullOrWhiteSpace(role))
        throw new ArgumentException("Role is required", nameof(role));

    if (role != "Customer" && role != "Admin")
        throw new ArgumentException("Role must be either 'Customer' or 'Admin'", nameof(role));
}

private static void ValidateHashedPassword(string hashedPassword)
{
    if (string.IsNullOrWhiteSpace(hashedPassword))
        throw new ArgumentException("Hashed password is required", nameof(hashedPassword));

    // BCrypt hashes are typically 60 characters
    if (hashedPassword.Length < 50)
        throw new ArgumentException("Invalid hashed password format", nameof(hashedPassword));
}

// Query methods
public bool IsAdmin() => Role == "Admin";
public bool IsCustomer() => Role == "Customer";
}

text

### Step 2: Create IPasswordHasher Interface

**File**: `LibHub.UserService.Domain/IPasswordHasher.cs`

namespace LibHub.UserService.Domain;

/// <summary>
/// Interface for password hashing operations.
/// Implementation will be in Infrastructure layer using BCrypt.
</summary>
public interface IPasswordHasher
{
<summary>
/// Hash a plain text passw
</summary>
<param name="password">Plain text password</param>
<returns>Hashed password</returns>
string Hash(string passw

text
/// <summary>
/// Verify a plain text password against a hashed password.
/// </summary>
/// <param name="hash">Hashed password</param>
/// <param name="password">Plain text password to verify</param>
/// <returns>True if password matches, false otherwise</returns>
bool Verify(string hash, string password);
}

text

### Step 3: Create IUserRepository Interface

**File**: `LibHub.UserService.Domain/IUserRepository.cs`

namespace LibHub.UserService.Domain;

/// <summary>
/// Repository interface for User entity.
/// Implementation will be in Infrastructure layer using EF Core.
</summary>
public interface IUserRepository
{
<summary>
/// Get user by
</summary>
Task<User?> GetByIdAsync(int use

text
/// <summary>
/// Get user by email (case-insensitive).
/// </summary>
Task<User?> GetByEmailAsync(string email);

/// <summary>
/// Check if email already exists (case-insensitive).
/// </summary>
Task<bool> EmailExistsAsync(string email);

/// <summary>
/// Add new user to database.
/// </summary>
Task AddAsync(User user);

/// <summary>
/// Update existing user.
/// </summary>
Task UpdateAsync(User user);

/// <summary>
/// Get all users (admin operation).
/// </summary>
Task<List<User>> GetAllAsync();
}

text

### Step 4: Create Domain Exceptions (Optional but Recommended)

**File**: `LibHub.UserService.Domain/Exceptions/DomainException.cs`

namespace LibHub.UserService.Domain.Exceptions;

/// <summary>
/// Base exception for domain-level errors.
</summary>
public class DomainException : Exception
{
public DomainException(string message) : base(mess
g
text
public DomainException(string message, Exception innerException) 
    : base(message, innerException)
{
}
}

text

**File**: `LibHub.UserService.Domain/Exceptions/ValidationException.cs`

namespace LibHub.UserService.Domain.Exceptions;

/// <summary>
/// Exception thrown when domain validation fails.
</summary>
public class ValidationException : DomainException
{
public ValidationException(string message) : base(mess
g
)
text

### Step 5: Verify Domain Layer

cd ~/Projects/LibHub/src/Services/UserService/LibHub.UserService.Domain

List all files
ls -la

Build the project
dotnet build

text

**Expected files**:
- `User.cs`
- `IUserRepository.cs`
- `IPasswordHasher.cs`
- `Exceptions/DomainException.cs`
- `Exceptions/ValidationException.cs`

---

## Acceptance Criteria

- [ ] User entity has complete business logic and validation
- [ ] All private setters for encapsulation
- [ ] VerifyPassword method delegates to IPasswordHasher
- [ ] UpdateProfile method with validation
- [ ] IUserRepository interface with all CRUD operations
- [ ] IPasswordHasher interface defined
- [ ] Domain exceptions created
- [ ] No external dependencies (only .NET primitives)
- [ ] All validation throws ArgumentException with clear messages
- [ ] Email stored as lowercase
- [ ] Role validated (only "Customer" or "Admin")
- [ ] No compilation errors

---

## Verification Commands

Build domain project
cd ~/Projects/LibHub/src/Services/UserService/LibHub.UserService.Domain
Verify no external package dependencies
dotnet list package

Expected: No packages (pure C#)
text

---

## Testing (Manual Verification)

Create a simple test file to verify logic:

**File**: `LibHub.UserService.Domain/Test.cs` (temporary, delete after testing)

namespace LibHub.UserService.Domain;

public static class DomainTest
{
public static void RunTes
s
) { // Test val
d u
e
test@example.com", "hashedpassword123456789012345678901234567890123456789012345", "Customer");
Console.WriteLine("✓ Valid use

reated"); }

catch (Exception ex) { Console.
text
    // Test invalid username
    try
    {
        var user = new User("", "test@example.com", "hashedpassword123456789012345678901234567890123456789012345", "Customer");
        Console.WriteLine("✗ Empty username should throw");
    }
    catch (ArgumentException)
    {
        Console.WriteLine("✓ Empty username rejected");
    }

    // Test invalid role
    try
    {
        var user = new User("test", "test@example.com", "hashedpassword123456789012345678901234567890123456789012345", "InvalidRole");
        Console.WriteLine("✗ Invalid role should throw");
    }
    catch (ArgumentException)
    {
        Console.WriteLine("✓ Invalid role rejected");
    }

    // Test email lowercase
    var user2 = new User("test", "TEST@EXAMPLE.COM", "hashedpassword123456789012345678901234567890123456789012345", "Customer");
    if (user2.Email == "test@example.com")
        Console.WriteLine("✓ Email converted to lowercase");
    else
        Console.WriteLine("✗ Email not lowercase");
}
}

text

Run: Create a console app temporarily to test, then delete.

---

## After Completion

### Update PROJECT_STATUS.md

Add to **Completed Tasks**:
Phase 2: UserService
✅ Task 2.1: UserService Domain Layer implemented (2025-10-27)

Files Created:

User.cs (complete with validation)

IUserRepository.cs

IPasswordHasher.cs

Domain exceptions

Verification: Domain layer has zero external dependencies

Business Rules: Email uniqueness, role validation, password verification

text

Update **Service Readiness Status**:
| UserService | ✅ | ✅ | ⚪ | 🟡 | ⚪ | ⚪ | ❌ |

text

Update **Current Phase**:
Current Phase: Phase 2 - UserService Implementation
Overall Progress: 20% (4/20 tasks complete)

text

### Git Commit
git add src/Services/UserService/LibHub.UserService.Domain/
git commit -m "✅ Task 2.1: Implement UserService Domain Layer"
git add ai-docs/PROJECT_STATUS.md
text

### Move Task File
mv ai-docs/tasks/phase-2-userservice/task-2.1-domain-layer.md ai-docs/completed-artifacts/

text

---

## Next Task

**Task 2.2**: Implement UserService Application Layer (DTOs, IdentityApplicationService)