# Task 2.5: Write UserService Tests

**Phase**: 2 - UserService Implementation  
**Type**: Testing  
**Estimated Time**: 2-3 hours  
**Dependencies**: Task 2.4 (Presentation Layer)

---

## Objective

Write comprehensive unit and integration tests for UserService to ensure code quality and reliability.

---

## Prerequisites

- [ ] Task 2.1-2.4 completed (all UserService layers)
- [ ] UserService running successfully on port 5002
- [ ] Understanding of xUnit and Moq

---

## What You'll Implement

1. Unit tests for User entity validation
2. Unit tests for PasswordValidator
3. Unit tests for IdentityApplicationService (with mocks)
4. Integration tests for UsersController
5. Integration tests for complete registration/login workflow

---

## Step-by-Step Instructions

### Step 1: Create Test Project

cd ~/Projects/LibHub/tests

Create xUnit test project
dotnet new xunit -n LibHub.UserService.Tests

Add to solution
cd ~/Projects/LibHub
dotnet sln add tests/LibHub.UserService.Tests

text

### Step 2: Add NuGet Packages for Testing

cd ~/Projects/LibHub/tests/LibHub.UserService.Tests

Add required test packages
dotnet add package Moq --version 4.20.*
dotnet add package FluentAssertions --version 6.12.*
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 8.0.*
dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 8.0.*

Add project references
cd ~/Projects/LibHub
dotnet add tests/LibHub.UserService.Tests reference src/Services/UserService/LibHub.UserService.Domain
dotnet add tests/LibHub.UserService.Tests reference src/Services/UserService/LibHub.UserService.Application
dotnet add tests/LibHub.UserService.Tests reference src/Services/UserService/LibHub.UserService.Infrastructure
dotnet add tests/LibHub.UserService.Tests reference src/Services/UserService/LibHub.UserService.Api

text

### Step 3: Domain Layer Unit Tests

**File**: `LibHub.UserService.Tests/Domain/UserTests.cs`

using FluentAssertions;
using LibHub.UserService.Domain;
using Xunit;

namespace LibHub.UserService.Tests.Domain;

public class UserTests
{
[Fact]
public void Constructor_WithValidInputs_ShouldCreateUser()
{
// Arrange
var username = "testuser";
var email = "TEST@EXAMPLE.COM";
var hashedPassword = "hashedpassword123456789012345678901234567890123456789012345";
var role = "Customer";

text
    // Act
    var user = new User(username, email, hashedPassword, role);

    // Assert
    user.Username.Should().Be(username);
    user.Email.Should().Be("test@example.com"); // Should be lowercase
    user.HashedPassword.Should().Be(hashedPassword);
    user.Role.Should().Be(role);
    user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
}

[Theory]
[InlineData("")]
[InlineData("  ")]
[InlineData(null)]
public void Constructor_WithInvalidUsername_ShouldThrowArgumentException(string invalidUsername)
{
    // Arrange & Act
    Action act = () => new User(
        invalidUsername, 
        "test@example.com", 
        "hashedpassword123456789012345678901234567890123456789012345", 
        "Customer");

    // Assert
    act.Should().Throw<ArgumentException>()
        .WithMessage("*Username*");
}

[Theory]
[InlineData("")]
[InlineData("invalid-email")]
[InlineData("noemail")]
public void Constructor_WithInvalidEmail_ShouldThrowArgumentException(string invalidEmail)
{
    // Arrange & Act
    Action act = () => new User(
        "testuser", 
        invalidEmail, 
        "hashedpassword123456789012345678901234567890123456789012345", 
        "Customer");

    // Assert
    act.Should().Throw<ArgumentException>()
        .WithMessage("*Email*");
}

[Theory]
[InlineData("SuperUser")]
[InlineData("Guest")]
[InlineData("")]
public void Constructor_WithInvalidRole_ShouldThrowArgumentException(string invalidRole)
{
    // Arrange & Act
    Action act = () => new User(
        "testuser", 
        "test@example.com", 
        "hashedpassword123456789012345678901234567890123456789012345", 
        invalidRole);

    // Assert
    act.Should().Throw<ArgumentException>()
        .WithMessage("*Role*");
}

[Fact]
public void IsCustomer_WithCustomerRole_ShouldReturnTrue()
{
    // Arrange
    var user = new User(
        "testuser", 
        "test@example.com", 
        "hashedpassword123456789012345678901234567890123456789012345", 
        "Customer");

    // Act & Assert
    user.IsCustomer().Should().BeTrue();
    user.IsAdmin().Should().BeFalse();
}

[Fact]
public void IsAdmin_WithAdminRole_ShouldReturnTrue()
{
    // Arrange
    var user = new User(
        "admin", 
        "admin@example.com", 
        "hashedpassword123456789012345678901234567890123456789012345", 
        "Admin");

    // Act & Assert
    user.IsAdmin().Should().BeTrue();
    user.IsCustomer().Should().BeFalse();
}
}

text

### Step 4: Application Layer Unit Tests

**File**: `LibHub.UserService.Tests/Application/PasswordValidatorTests.cs`

using FluentAssertions;
using LibHub.UserService.Application.Validation;
using Xunit;

namespace LibHub.UserService.Tests.Application;

public class PasswordValidatorTests
{
[Fact]
public void Validate_WithValidPassword_ShouldReturnTrue()
{
// Arrange
var password = "ValidPass123!";

text
    // Act
    var (isValid, errorMessage) = PasswordValidator.Validate(password);

    // Assert
    isValid.Should().BeTrue();
    errorMessage.Should().BeEmpty();
}

[Theory]
[InlineData("short1!")]
[InlineData("Short1!")]
public void Validate_WithShortPassword_ShouldReturnFalse(string password)
{
    // Act
    var (isValid, errorMessage) = PasswordValidator.Validate(password);

    // Assert
    isValid.Should().BeFalse();
    errorMessage.Should().Contain("8 characters");
}

[Fact]
public void Validate_WithoutUppercase_ShouldReturnFalse()
{
    // Arrange
    var password = "validpass123!";

    // Act
    var (isValid, errorMessage) = PasswordValidator.Validate(password);

    // Assert
    isValid.Should().BeFalse();
    errorMessage.Should().Contain("uppercase");
}

[Fact]
public void Validate_WithoutLowercase_ShouldReturnFalse()
{
    // Arrange
    var password = "VALIDPASS123!";

    // Act
    var (isValid, errorMessage) = PasswordValidator.Validate(password);

    // Assert
    isValid.Should().BeFalse();
    errorMessage.Should().Contain("lowercase");
}

[Fact]
public void Validate_WithoutDigit_ShouldReturnFalse()
{
    // Arrange
    var password = "ValidPass!";

    // Act
    var (isValid, errorMessage) = PasswordValidator.Validate(password);

    // Assert
    isValid.Should().BeFalse();
    errorMessage.Should().Contain("digit");
}

[Fact]
public void Validate_WithoutSpecialCharacter_ShouldReturnFalse()
{
    // Arrange
    var password = "ValidPass123";

    // Act
    var (isValid, errorMessage) = PasswordValidator.Validate(password);

    // Assert
    isValid.Should().BeFalse();
    errorMessage.Should().Contain("special character");
}
}

text

**File**: `LibHub.UserService.Tests/Application/IdentityApplicationServiceTests.cs`

using FluentAssertions;
using LibHub.UserService.Application.DTOs;
using LibHub.UserService.Application.Interfaces;
using LibHub.UserService.Application.Services;
using LibHub.UserService.Domain;
using Moq;
using Xunit;

namespace LibHub.UserService.Tests.Application;

public class IdentityApplicationServiceTests
{
private readonly Mock<IUserRepository> _mockUserRepository;
private readonly Mock<IPasswordHasher> _mockPasswordHasher;
private readonly Mock<IJwtTokenGenerator> _mockTokenGenerator;
private readonly IdentityApplicationService _service;

text
public IdentityApplicationServiceTests()
{
    _mockUserRepository = new Mock<IUserRepository>();
    _mockPasswordHasher = new Mock<IPasswordHasher>();
    _mockTokenGenerator = new Mock<IJwtTokenGenerator>();
    
    _service = new IdentityApplicationService(
        _mockUserRepository.Object,
        _mockPasswordHasher.Object,
        _mockTokenGenerator.Object);
}

[Fact]
public async Task RegisterUserAsync_WithValidData_ShouldCreateUser()
{
    // Arrange
    var dto = new RegisterUserDto
    {
        Username = "testuser",
        Email = "test@example.com",
        Password = "ValidPass123!"
    };

    _mockUserRepository
        .Setup(r => r.EmailExistsAsync(It.IsAny<string>()))
        .ReturnsAsync(false);

    _mockPasswordHasher
        .Setup(h => h.Hash(It.IsAny<string>()))
        .Returns("hashedpassword123456789012345678901234567890123456789012345");

    // Act
    var result = await _service.RegisterUserAsync(dto);

    // Assert
    result.Should().NotBeNull();
    result.Username.Should().Be(dto.Username);
    result.Email.Should().Be(dto.Email.ToLower());
    result.Role.Should().Be("Customer");

    _mockUserRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
}

[Fact]
public async Task RegisterUserAsync_WithExistingEmail_ShouldThrowInvalidOperationException()
{
    // Arrange
    var dto = new RegisterUserDto
    {
        Username = "testuser",
        Email = "existing@example.com",
        Password = "ValidPass123!"
    };

    _mockUserRepository
        .Setup(r => r.EmailExistsAsync(It.IsAny<string>()))
        .ReturnsAsync(true);

    // Act
    Func<Task> act = async () => await _service.RegisterUserAsync(dto);

    // Assert
    await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*already exists*");
}

[Fact]
public async Task LoginUserAsync_WithValidCredentials_ShouldReturnToken()
{
    // Arrange
    var dto = new LoginDto
    {
        Email = "test@example.com",
        Password = "ValidPass123!"
    };

    var user = new User(
        "testuser",
        dto.Email,
        "hashedpassword123456789012345678901234567890123456789012345",
        "Customer");

    _mockUserRepository
        .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
        .ReturnsAsync(user);

    _mockPasswordHasher
        .Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
        .Returns(true);

    _mockTokenGenerator
        .Setup(t => t.GenerateToken(It.IsAny<User>()))
        .Returns("jwt-token-here");

    // Act
    var result = await _service.LoginUserAsync(dto);

    // Assert
    result.Should().NotBeNull();
    result.AccessToken.Should().Be("jwt-token-here");
    result.ExpiresIn.Should().Be(3600);
}

[Fact]
public async Task LoginUserAsync_WithInvalidPassword_ShouldThrowUnauthorizedAccessException()
{
    // Arrange
    var dto = new LoginDto
    {
        Email = "test@example.com",
        Password = "WrongPass123!"
    };

    var user = new User(
        "testuser",
        dto.Email,
        "hashedpassword123456789012345678901234567890123456789012345",
        "Customer");

    _mockUserRepository
        .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
        .ReturnsAsync(user);

    _mockPasswordHasher
        .Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
        .Returns(false);

    // Act
    Func<Task> act = async () => await _service.LoginUserAsync(dto);

    // Assert
    await act.Should().ThrowAsync<UnauthorizedAccessException>();
}
}

text

### Step 5: Infrastructure Layer Tests

**File**: `LibHub.UserService.Tests/Infrastructure/PasswordHasherTests.cs`

using FluentAssertions;
using LibHub.UserService.Infrastructure.Security;
using Xunit;

namespace LibHub.UserService.Tests.Infrastructure;

public class PasswordHasherTests
{
private readonly PasswordHasher _hasher;

text
public PasswordHasherTests()
{
    _hasher = new PasswordHasher();
}

[Fact]
public void Hash_WithValidPassword_ShouldReturnHashedPassword()
{
    // Arrange
    var password = "TestPassword123!";

    // Act
    var hash = _hasher.Hash(password);

    // Assert
    hash.Should().NotBeNullOrEmpty();
    hash.Should().NotBe(password);
    hash.Length.Should().BeGreaterThan(50); // BCrypt hashes are typically 60 chars
}

[Fact]
public void Verify_WithCorrectPassword_ShouldReturnTrue()
{
    // Arrange
    var password = "TestPassword123!";
    var hash = _hasher.Hash(password);

    // Act
    var result = _hasher.Verify(hash, password);

    // Assert
    result.Should().BeTrue();
}

[Fact]
public void Verify_WithIncorrectPassword_ShouldReturnFalse()
{
    // Arrange
    var password = "TestPassword123!";
    var wrongPassword = "WrongPassword123!";
    var hash = _hasher.Hash(password);

    // Act
    var result = _hasher.Verify(hash, wrongPassword);

    // Assert
    result.Should().BeFalse();
}

[Fact]
public void Hash_DifferentPasswordInstances_ShouldProduceDifferentHashes()
{
    // Arrange
    var password = "TestPassword123!";

    // Act
    var hash1 = _hasher.Hash(password);
    var hash2 = _hasher.Hash(password);

    // Assert
    hash1.Should().NotBe(hash2); // BCrypt uses random salt
    _hasher.Verify(hash1, password).Should().BeTrue();
    _hasher.Verify(hash2, password).Should().BeTrue();
}
}

text

### Step 6: Run All Tests

cd ~/Projects/LibHub/tests/LibHub.UserService.Tests

Run all tests
dotnet test

Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

Run tests with coverage (optional)
dotnet test --collect:"XPlat Code Coverage"

text

---

## Acceptance Criteria

- [ ] All domain unit tests pass
- [ ] All application unit tests pass
- [ ] All infrastructure tests pass
- [ ] Password validation tests cover all scenarios
- [ ] IdentityApplicationService tests use Moq
- [ ] BCrypt hasher tests verify hashing and verification
- [ ] Test coverage > 70% (measure with coverage tool)
- [ ] No failing tests
- [ ] All tests run in under 5 seconds

---

## Verification Commands

Run all tests
cd ~/Projects/LibHub
dotnet test

Run only UserService tests
dotnet test tests/LibHub.UserService.Tests

Run with verbosity
dotnet test tests/LibHub.UserService.Tests --logger "console;verbosity=detailed"

Count test results
dotnet test tests/LibHub.UserService.Tests --logger "console;verbosity=minimal"

text

**Expected output**: All tests passing (green)

---

## After Completion

### Update PROJECT_STATUS.md

Update **Phase Status Overview**:
| Phase 2: UserService | ✅ COMPLETE | 100% (5/5) | All layers implemented and tested |
| Phase 3: CatalogService | ⚪ READY TO START | 0% | No blockers |

text

Add to **Completed Tasks**:
✅ Task 2.5: UserService Tests written (2025-10-27)

Files Created:

UserTests.cs (domain validation tests)

PasswordValidatorTests.cs (password validation tests)

IdentityApplicationServiceTests.cs (mocked service tests)

PasswordHasherTests.cs (BCrypt tests)

Verification: All tests passing, coverage >70%

Test Count: 15+ unit tests

Phase 2 Complete: UserService fully implemented and tested! 🎉

text

Update **Service Readiness Status**:
| Service | Database | Domain | Application | Infrastructure | Presentation | Tests | Ready? |
| UserService | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| CatalogService | ✅ | ⚪ | ⚪ | 🟡 | ⚪ | ⚪ | ❌ |
| LoanService | ✅ | ⚪ | ⚪ | 🟡 | ⚪ | ⚪ | ❌ |

text

Update **Overall Progress**:
Overall Progress: 40% (8/20 tasks complete)

text

### Git Commit
git add tests/LibHub.UserService.Tests/
git commit -m "✅ Task 2.5: Write UserService tests"
git add ai-docs/PROJECT_STATUS.md
git commit -m "docs: Phase 2 complete! UserService ready 🎉"

text

### Move Task File
mv ai-docs/tasks/phase-2-userservice/task-2.5-testing.md ai-docs/completed-artifacts/

text

---

## 🎉 Phase 2 Complete!

**UserService is now fully implemented, tested, and ready for integration!**

All layers complete:
- ✅ Domain Layer (entities, interfaces, business rules)
- ✅ Application Layer (DTOs, services, validation)
- ✅ Infrastructure Layer (EF Core, BCrypt, JWT)
- ✅ Presentation Layer (API controllers, Swagger)
- ✅ Tests (unit and infrastructure tests)

**Next**: Phase 3 - Implement CatalogService (similar structure)

---

## Next Task

**Task 3.1**: Implement CatalogService Domain Layer