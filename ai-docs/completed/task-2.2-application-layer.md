# Task 2.2: Implement UserService Application Layer

**Phase**: 2 - UserService Implementation  
**Layer**: Application  
**Estimated Time**: 1.5-2 hours  
**Dependencies**: Task 2.1 (Domain Layer)

---

## Objective

Implement the Application Layer for UserService with DTOs, application services, and business workflow orchestration.

---

## Prerequisites

- [ ] Task 2.1 completed (Domain Layer with User entity and interfaces)
- [ ] Understanding of Application Layer responsibility (use case orchestration)

---

## What You'll Implement

1. Data Transfer Objects (DTOs) for all operations
2. IdentityApplicationService for user registration and authentication
3. JWT token generation interface
4. Password validation logic
5. Email uniqueness checking workflow

---

## Step-by-Step Instructions

### Step 1: Create DTOs

**File**: `LibHub.UserService.Application/DTOs/RegisterUserDto.cs`

namespace LibHub.UserService.Application.DTOs;

public class RegisterUserDto
{
public string Username { get; set; } = string.Em
ty; public string Email { get; set; } = strin
.Empty; public string Password { get; set; } = s
text

**File**: `LibHub.UserService.Application/DTOs/LoginDto.cs`

namespace LibHub.UserService.Application.DTOs;

public class LoginDto
{
public string Email { get; set; } = string.Em
ty; public string Password { get; set; } = strin
text

**File**: `LibHub.UserService.Application/DTOs/UserDto.cs`

namespace LibHub.UserService.Application.DTOs;

public class UserDto
{
public int UserId { get; se
; } public string Username { get; set; } = strin
.Empty; public string Email { get; set; } = s
ring.Empty; public string Role { get; set; }
= string.Empty; public DateTime Cre
text

**File**: `LibHub.UserService.Application/DTOs/TokenDto.cs`

namespace LibHub.UserService.Application.DTOs;

public class TokenDto
{
public string AccessToken { get; set; } = string.Em
ty; public int ExpiresIn { get; set; } //
text

### Step 2: Create IJwtTokenGenerator Interface

**File**: `LibHub.UserService.Application/Interfaces/IJwtTokenGenerator.cs`

using LibHub.UserService.Domain;

namespace LibHub.UserService.Application.Interfaces;

/// <summary>
/// Interface for JWT token generation.
/// Implementation will be in Infrastructure layer.
</summary>
public interface IJwtTokenGenerator
{
<summary>
/// Generate JWT token for authenticated u
</summary>
<param name="user">User entity</param>
<returns>JWT token string</returns>
string GenerateToken(User us
text

### Step 3: Create Password Validation

**File**: `LibHub.UserService.Application/Validation/PasswordValidator.cs`

namespace LibHub.UserService.Application.Validation;

public static class PasswordValidator
{
<summary>
/// Validate password meets security requireme
</summary>
public static (bool IsValid, string ErrorMessage) Validate(string passw
r
) { if (string.IsNullOrWhite
text
    if (password.Length < 8)
        return (false, "Password must be at least 8 characters long");

    if (!password.Any(char.IsUpper))
        return (false, "Password must contain at least one uppercase letter");

    if (!password.Any(char.IsLower))
        return (false, "Password must contain at least one lowercase letter");

    if (!password.Any(char.IsDigit))
        return (false, "Password must contain at least one digit");

    if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
        return (false, "Password must contain at least one special character");

    return (true, string.Empty);
}
}

text

### Step 4: Create IdentityApplicationService

**File**: `LibHub.UserService.Application/Services/IdentityApplicationService.cs`

using LibHub.UserService.Application.DTOs;
using LibHub.UserService.Application.Interfaces;
using LibHub.UserService.Application.Validation;
namespace LibHub.UserService.Application.Services;

public class IdentityApplicationService
{
private readonly IUserRepository _userReposit
ry; private readonly IPasswordHasher _passwor
text
public IdentityApplicationService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator)
{
    _userRepository = userRepository;
    _passwordHasher = passwordHasher;
    _jwtTokenGenerator = jwtTokenGenerator;
}

/// <summary>
/// Register a new user account.
/// </summary>
public async Task<UserDto> RegisterUserAsync(RegisterUserDto dto)
{
    // Validate password
    var (isValid, errorMessage) = PasswordValidator.Validate(dto.Password);
    if (!isValid)
        throw new ArgumentException(errorMessage);

    // Check if email already exists
    if (await _userRepository.EmailExistsAsync(dto.Email))
        throw new InvalidOperationException("Email already exists");

    // Hash password
    var hashedPassword = _passwordHasher.Hash(dto.Password);

    // Create user entity (default role: Customer)
    var user = new User(dto.Username, dto.Email, hashedPassword, "Customer");

    // Persist
    await _userRepository.AddAsync(user);

    // Return DTO
    return MapToDto(user);
}

/// <summary>
/// Authenticate user and generate JWT token.
/// </summary>
public async Task<TokenDto> LoginUserAsync(LoginDto dto)
{
    // Get user by email
    var user = await _userRepository.GetByEmailAsync(dto.Email);
    if (user == null)
        throw new UnauthorizedAccessException("Invalid email or password");

    // Verify password
    if (!user.VerifyPassword(dto.Password, _passwordHasher))
        throw new UnauthorizedAccessException("Invalid email or password");

    // Generate JWT token
    var token = _jwtTokenGenerator.GenerateToken(user);

    return new TokenDto
    {
        AccessToken = token,
        ExpiresIn = 3600 // 1 hour
    };
}

/// <summary>
/// Get user by ID.
/// </summary>
public async Task<UserDto?> GetUserByIdAsync(int userId)
{
    var user = await _userRepository.GetByIdAsync(userId);
    return user != null ? MapToDto(user) : null;
}

/// <summary>
/// Get user by email.
/// </summary>
public async Task<UserDto?> GetUserByEmailAsync(string email)
{
    var user = await _userRepository.GetByEmailAsync(email);
    return user != null ? MapToDto(user) : null;
}

private static UserDto MapToDto(User user)
{
    return new UserDto
    {
        UserId = user.UserId,
        Username = user.Username,
        Email = user.Email,
        Role = user.Role,
        CreatedAt = user.CreatedAt
    };
}
}

text

### Step 5: Add Project Reference

cd ~/Projects/LibHub

Application layer depends on Domain layer
dotnet add src/Services/UserService/LibHub.UserService.Application reference src/Services/UserService/LibHub.UserService.Domain

text

### Step 6: Verify Application Layer

cd ~/Projects/LibHub/src/Services/UserService/LibHub.UserService.Application

Build the project
dotnet build

text

---

## Acceptance Criteria

- [ ] All DTOs created (RegisterUserDto, LoginDto, UserDto, TokenDto)
- [ ] PasswordValidator with comprehensive validation
- [ ] IJwtTokenGenerator interface defined
- [ ] IdentityApplicationService with RegisterUserAsync and LoginUserAsync
- [ ] Email uniqueness checking in RegisterUserAsync
- [ ] Password verification in LoginUserAsync
- [ ] Clear exception messages for validation failures
- [ ] MapToDto helper method
- [ ] Project reference to Domain layer
- [ ] No compilation errors
- [ ] No dependencies on Infrastructure or Presentation layers

---

## Verification Commands

Build application project
cd ~/Projects/LibHub/src/Services/UserService/LibHub.UserService.Application
Verify project references
dotnet list reference

Expected: Only Domain layer reference
text

---

## Key Implementation Notes

1. **Application Layer Orchestrates**: It doesn't contain business logic, just orchestrates domain entities
2. **Password Validation**: Done at application layer before domain entity creation
3. **Email Uniqueness**: Checked at application layer via repository
4. **JWT Generation**: Delegated to Infrastructure via interface
5. **Default Role**: New users get "Customer" role automatically
6. **Exception Handling**: Use clear, user-friendly error messages
7. **DTO Mapping**: Always return DTOs, never domain entities

---

## After Completion

### Update PROJECT_STATUS.md

Add to **Completed Tasks**:
✅ Task 2.2: UserService Application Layer implemented (2025-10-27)

Files Created:

DTOs (RegisterUserDto, LoginDto, UserDto, TokenDto)

IdentityApplicationService

PasswordValidator

IJwtTokenGenerator interface

Verification: Application layer orchestrates use cases correctly

text

Update **Service Readiness Status**:
| UserService | ✅ | ✅ | ✅ | 🟡 | ⚪ | ⚪ | ❌ |

text

Update **Overall Progress**:
Overall Progress: 25% (5/20 tasks complete)

text

### Git Commit
git add src/Services/UserService/LibHub.UserService.Application/
git commit -m "✅ Task 2.2: Implement UserService Application Layer"
git add ai-docs/PROJECT_STATUS.md
text

### Move Task File
mv ai-docs/tasks/phase-2-userservice/task-2.2-application-layer.md ai-docs/completed-artifacts/

text

---

## Next Task

**Task 2.3**: Implement UserService Infrastructure Layer (EF Core repository, BCrypt, JWT)