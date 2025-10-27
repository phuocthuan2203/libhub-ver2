# Task 2.3: Implement UserService Infrastructure Layer

**Phase**: 2 - UserService Implementation  
**Layer**: Infrastructure  
**Estimated Time**: 2-3 hours  
**Dependencies**: Task 2.2 (Application Layer)

---

## Objective

Implement the Infrastructure Layer for UserService with EF Core repository, BCrypt password hashing, and JWT token generation.

---

## Prerequisites

- [ ] Task 2.1 completed (Domain Layer)
- [ ] Task 2.2 completed (Application Layer)
- [ ] UserDbContext exists from Task 1.1

---

## What You'll Implement

1. EfUserRepository implementing IUserRepository
2. PasswordHasher using BCrypt
3. JwtTokenGenerator for authentication
4. Infrastructure service registrations

---

## Step-by-Step Instructions

### Step 1: Add NuGet Packages

cd ~/Projects/LibHub/src/Services/UserService/LibHub.UserService.Infrastructure

Add BCrypt for password hashing
dotnet add package BCrypt.Net-Next --version 4.0.*

Add JWT packages
dotnet add package System.IdentityModel.Tokens.Jwt --version 7.0.*
dotnet add package Microsof

EF Core packages should already be installed from Task 1.1
If not:
dotnet add package Microsoft.EntityFrameworkCore --version 8.0.*
dotnet add package Pomelo.EntityFrameworkCore.MySql --version 8.0.*
text

### Step 2: Add Project References

cd ~/Projects/LibHub

Infrastructure depends on Application and Domain
dotnet add src/Services/UserService/LibHub.UserService.Infrastructure reference src/Services/UserService/LibHub.UserService.Application
dotnet add src/Ser

text

### Step 3: Implement EfUserRepository

**File**: `LibHub.UserService.Infrastructure/Repositories/EfUserRepository.cs`

using Microsoft.EntityFrameworkCore;
using LibHub.UserService.D

namespace LibHub.UserService.Infrastructure.Repositories;

public class EfUserRepository : IUserRepository
{
private readonly Use

text
public EfUserRepository(UserDbContext context)
{
    _context = context;
}

public async Task<User?> GetByIdAsync(int userId)
{
    return await _context.Users.FindAsync(userId);
}

public async Task<User?> GetByEmailAsync(string email)
{
    return await _context.Users
        .FirstOrDefaultAsync(u => u.Email == email.ToLower());
}

public async Task<bool> EmailExistsAsync(string email)
{
    return await _context.Users
        .AnyAsync(u => u.Email == email.ToLower());
}

public async Task AddAsync(User user)
{
    await _context.Users.AddAsync(user);
    await _context.SaveChangesAsync();
}

public async Task UpdateAsync(User user)
{
    _context.Users.Update(user);
    await _context.SaveChangesAsync();
}

public async Task<List<User>> GetAllAsync()
{
    return await _context.Users.ToListAsync();
}
}

text

### Step 4: Implement PasswordHasher

**File**: `LibHub.UserService.Infrastructure/Security/PasswordHasher.cs`

using LibHub.UserService.Domain;

namespace LibHub.UserService.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
text
public string Hash(string password)
{
    if (string.IsNullOrWhiteSpace(password))
        throw new ArgumentException("Password cannot be empty", nameof(password));

    return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
}

public bool Verify(string hash, string password)
{
    if (string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(password))
        return false;

    try
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
    catch
    {
        return false;
    }
}
}

text

### Step 5: Implement JwtTokenGenerator

**File**: `LibHub.UserService.Infrastructure/Security/JwtTokenGenerator.cs`

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using LibHub.UserService.Application.Interfaces;
namespace LibHub.UserService.Infrastructure.Security;

public class JwtTokenGenerator : IJwtTokenGenerator
{
private rea

text
public JwtTokenGenerator(IConfiguration configuration)
{
    _configuration = configuration;
}

public string GenerateToken(User user)
{
    var secretKey = _configuration["Jwt:SecretKey"]
        ?? throw new InvalidOperationException("JWT SecretKey not configured");

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role),
        new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    var token = new JwtSecurityToken(
        issuer: _configuration["Jwt:Issuer"],
        audience: _configuration["Jwt:Audience"],
        claims: claims,
        expires: DateTime.UtcNow.AddHours(
            int.Parse(_configuration["Jwt:ExpiryInHours"] ?? "1")),
        signingCredentials: credentials
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}
}

text

### Step 6: Update UserDbContext (if needed)

Ensure UserDbContext is properly configured:

**File**: `LibHub.UserService.Infrastructure/UserDbContext.cs` (verify/update)

using Microsoft.EntityFrameworkCore;
namespace LibHub.UserService.Infrastructure;

public class UserDbContext : DbContext
{
<User> Users { get; set; }

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

### Step 7: Verify Infrastructure Layer

cd ~/Projects/LibHub/src/Services/UserService/LibHub.UserService.Infrastructure

Build the project
dotnet build

text

---

## Acceptance Criteria

- [ ] EfUserRepository implements all IUserRepository methods
- [ ] PasswordHasher uses BCrypt with work factor 11
- [ ] JwtTokenGenerator creates valid JWT tokens
- [ ] All NuGet packages installed correctly
- [ ] Project references to Application and Domain layers
- [ ] UserDbContext properly configured
- [ ] No compilation errors
- [ ] All infrastructure implementations use dependency injection

---

## Verification Commands

Build infrastructure project
cd ~/Projects/LibHub/src/Services/UserService/LibHub.UserService.Infrastructure
Verify packages
dotnet list package

Expected packages:
- Microsoft.EntityFrameworkCore
- Pomelo.EntityFrameworkCore.MySql
- BCrypt.Net-Next
- System.IdentityModel.Tokens.Jwt
- Microsoft.IdentityModel.Tokens
Verify project references
dotnet list reference

Expected: Application and Domain references
text

---

## Testing (Manual)

Create a test to verify BCrypt:

cd ~/Projects/LibHub/src/Services/UserService/LibHub.UserService.Infrastructure

Create temporary test file
cat > TestHasher.cs << 'EOF'
using LibHub.UserSer

var hasher = new PasswordHasher();
var password = "TestPassword123!";
Console.WriteLine($"Password: {password}");
Console.WriteLine($"Hash: {hash}");
Console.WriteLine($"Verify (correct): {hasher.Verify(hash, password)}");
Console.WriteLine($"Verify (wrong): {hasher.Verify(hash, "WrongPassword")}");
Run dotnet script or create console app to test
Then delete TestHasher.cs
text

---

## Key Implementation Notes

1. **BCrypt Work Factor**: 11 is secure but not too slow for production
2. **JWT Claims**: Include UserId, Email, Username, Role
3. **JWT Expiry**: 1 hour (3600 seconds)
4. **Case-Insensitive Email**: Always convert to lowercase in queries
5. **SaveChangesAsync**: Called in repository methods, not in domain
6. **Configuration**: JWT settings come from appsettings.json
7. **Error Handling**: PasswordHasher catches exceptions and returns false

---

## After Completion

### Update PROJECT_STATUS.md

Add to **Completed Tasks**:
✅ Task 2.3: UserService Infrastructure Layer implemented (2025-10-27)

Files Created:

EfUserRepository (EF Core implementation)

PasswordHasher (BCrypt with work factor 11)

JwtTokenGenerator (JWT with 1-hour expiry)

Verification: All interfaces implemented, packages installed

NuGet: BCrypt.Net-Next, System.IdentityModel.Tokens.Jwt

text

Update **Service Readiness Status**:
| UserService | ✅ | ✅ | ✅ | ✅ | ⚪ | ⚪ | ❌ |

text

Update **Overall Progress**:
Overall Progress: 30% (6/20 tasks complete)

text

### Git Commit
git add src/Services/UserService/LibHub.UserService.Infrastructure/
git commit -m "✅ Task 2.3: Implement UserService Infrastructure Layer"
git add ai-docs/PROJECT_STATUS.md
text

### Move Task File
mv ai-docs/tasks/phase-2-userservice/task-2.3-infrastructure-layer.md ai-docs/completed-artifacts/

text

---

## Next Task

**Task 2.4**: Implement UserService Presentation Layer (Controllers, DI, Swagger)