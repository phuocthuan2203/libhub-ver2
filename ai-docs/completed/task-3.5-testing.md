# Task 3.5: Write CatalogService Tests

**Phase**: 3 - CatalogService Implementation  
**Type**: Testing  
**Estimated Time**: 2-3 hours  
**Dependencies**: Task 3.4 (Presentation Layer)

---

## Objective

Write comprehensive unit and integration tests for CatalogService to ensure code quality and reliability.

---

## Prerequisites

- [ ] Task 3.1-3.4 completed (all CatalogService layers)
- [ ] CatalogService running successfully on port 5001
- [ ] Understanding of xUnit and Moq

---

## What You'll Implement

1. Unit tests for Book entity (stock management, validation)
2. Unit tests for BookApplicationService (with mocks)
3. Integration tests for search functionality
4. Tests for stock update operations (Saga integration)

---

## Step-by-Step Instructions

### Step 1: Create Test Project

cd ~/Projects/LibHub/tests

Create xUnit test project
dotnet new xunit -n LibHub.CatalogService.Tests

Add to solution
cd ~/Projects/LibHub
dotnet sln add tests/LibHub.CatalogService.Tests

text

### Step 2: Add NuGet Packages

cd ~/Projects/LibHub/tests/LibHub.CatalogService.Tests

Add required test packages
dotnet add package Moq --version 4.20.*
dotnet add package FluentAssertions --version 6.12.*

Add project references
cd ~/Projects/LibHub
dotnet add tests/LibHub.CatalogService.Tests reference src/Services/CatalogService/LibHub.CatalogService.Domain
dotnet add tests/LibHub.CatalogService.Tests reference src/Services/CatalogService/LibHub.CatalogService.Application

text

### Step 3: Domain Layer Unit Tests

**File**: `LibHub.CatalogService.Tests/Domain/BookTests.cs`

using FluentAssertions;
using LibHub.CatalogService.Domain;
using Xunit;

namespace LibHub.CatalogService.Tests.Domain;

public class BookTests
{
[Fact]
public void Constructor_WithValidInputs_ShouldCreateBook()
{
// Arrange
var isbn = "9781234567890";
var title = "Test Book";
var author = "Test Author";
var genre = "Fiction";
var description = "Test description";
var totalCopies = 5;

text
    // Act
    var book = new Book(isbn, title, author, genre, description, totalCopies);

    // Assert
    book.Isbn.Should().Be(isbn);
    book.Title.Should().Be(title);
    book.Author.Should().Be(author);
    book.Genre.Should().Be(genre);
    book.Description.Should().Be(description);
    book.TotalCopies.Should().Be(totalCopies);
    book.AvailableCopies.Should().Be(totalCopies);
    book.IsAvailable().Should().BeTrue();
    book.CopiesOnLoan().Should().Be(0);
    book.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
}

[Theory]
[InlineData("123")]           // Too short
[InlineData("12345678901234")] // Too long
[InlineData("978123456789A")]  // Contains letter
[InlineData("")]
public void Constructor_WithInvalidIsbn_ShouldThrowArgumentException(string invalidIsbn)
{
    // Arrange & Act
    Action act = () => new Book(invalidIsbn, "Title", "Author", "Genre", null, 5);

    // Assert
    act.Should().Throw<ArgumentException>()
        .WithMessage("*ISBN*");
}

[Theory]
[InlineData("")]
[InlineData("  ")]
[InlineData(null)]
public void Constructor_WithInvalidTitle_ShouldThrowArgumentException(string invalidTitle)
{
    // Arrange & Act
    Action act = () => new Book("9781234567890", invalidTitle, "Author", "Genre", null, 5);

    // Assert
    act.Should().Throw<ArgumentException>()
        .WithMessage("*Title*");
}

[Fact]
public void DecrementStock_WithAvailableCopies_ShouldDecreaseCount()
{
    // Arrange
    var book = new Book("9781234567890", "Title", "Author", "Genre", null, 5);

    // Act
    book.DecrementStock();

    // Assert
    book.AvailableCopies.Should().Be(4);
    book.CopiesOnLoan().Should().Be(1);
    book.UpdatedAt.Should().NotBeNull();
}

[Fact]
public void DecrementStock_WithNoCopiesAvailable_ShouldThrowInvalidOperationException()
{
    // Arrange
    var book = new Book("9781234567890", "Title", "Author", "Genre", null, 1);
    book.DecrementStock(); // Now AvailableCopies = 0

    // Act
    Action act = () => book.DecrementStock();

    // Assert
    act.Should().Throw<InvalidOperationException>()
        .WithMessage("*No copies available*");
}

[Fact]
public void IncrementStock_WithCopiesOnLoan_ShouldIncreaseCount()
{
    // Arrange
    var book = new Book("9781234567890", "Title", "Author", "Genre", null, 5);
    book.DecrementStock(); // AvailableCopies = 4

    // Act
    book.IncrementStock();

    // Assert
    book.AvailableCopies.Should().Be(5);
    book.CopiesOnLoan().Should().Be(0);
}

[Fact]
public void IncrementStock_WhenAllCopiesAvailable_ShouldThrowInvalidOperationException()
{
    // Arrange
    var book = new Book("9781234567890", "Title", "Author", "Genre", null, 5);
    // All 5 copies are available

    // Act
    Action act = () => book.IncrementStock();

    // Assert
    act.Should().Throw<InvalidOperationException>()
        .WithMessage("*Cannot increment beyond total*");
}

[Fact]
public void UpdateDetails_WithValidData_ShouldUpdateBook()
{
    // Arrange
    var book = new Book("9781234567890", "Old Title", "Old Author", "Old Genre", "Old desc", 5);

    // Act
    book.UpdateDetails("New Title", "New Author", "New Genre", "New desc");

    // Assert
    book.Title.Should().Be("New Title");
    book.Author.Should().Be("New Author");
    book.Genre.Should().Be("New Genre");
    book.Description.Should().Be("New desc");
    book.Isbn.Should().Be("9781234567890"); // ISBN unchanged
    book.UpdatedAt.Should().NotBeNull();
}

[Fact]
public void AdjustTotalCopies_IncreasingTotal_ShouldIncreaseAvailable()
{
    // Arrange
    var book = new Book("9781234567890", "Title", "Author", "Genre", null, 5);
    book.DecrementStock(); // AvailableCopies = 4, OnLoan = 1

    // Act
    book.AdjustTotalCopies(10);

    // Assert
    book.TotalCopies.Should().Be(10);
    book.AvailableCopies.Should().Be(9); // 4 + 5 difference
    book.CopiesOnLoan().Should().Be(1);
}

[Fact]
public void AdjustTotalCopies_BelowOnLoanCount_ShouldThrowInvalidOperationException()
{
    // Arrange
    var book = new Book("9781234567890", "Title", "Author", "Genre", null, 5);
    book.DecrementStock();
    book.DecrementStock(); // OnLoan = 2

    // Act
    Action act = () => book.AdjustTotalCopies(1); // Try to set total below on-loan count

    // Assert
    act.Should().Throw<InvalidOperationException>()
        .WithMessage("*Cannot reduce total*when*on loan*");
}

[Fact]
public void IsAvailable_WithAvailableCopies_ShouldReturnTrue()
{
    // Arrange
    var book = new Book("9781234567890", "Title", "Author", "Genre", null, 5);

    // Assert
    book.IsAvailable().Should().BeTrue();
}

[Fact]
public void IsAvailable_WithNoCopies_ShouldReturnFalse()
{
    // Arrange
    var book = new Book("9781234567890", "Title", "Author", "Genre", null, 1);
    book.DecrementStock();

    // Assert
    book.IsAvailable().Should().BeFalse();
}
}

text

### Step 4: Application Layer Unit Tests

**File**: `LibHub.CatalogService.Tests/Application/BookApplicationServiceTests.cs`

using FluentAssertions;
using LibHub.CatalogService.Application.DTOs;
using LibHub.CatalogService.Application.Services;
using LibHub.CatalogService.Domain;
using Moq;
using Xunit;

namespace LibHub.CatalogService.Tests.Application;

public class BookApplicationServiceTests
{
private readonly Mock<IBookRepository> _mockBookRepository;
private readonly BookApplicationService _service;

text
public BookApplicationServiceTests()
{
    _mockBookRepository = new Mock<IBookRepository>();
    _service = new BookApplicationService(_mockBookRepository.Object);
}

[Fact]
public async Task CreateBookAsync_WithValidData_ShouldCreateBook()
{
    // Arrange
    var dto = new CreateBookDto
    {
        Isbn = "9781234567890",
        Title = "Test Book",
        Author = "Test Author",
        Genre = "Fiction",
        Description = "Test description",
        TotalCopies = 5
    };

    _mockBookRepository
        .Setup(r => r.GetByIsbnAsync(It.IsAny<string>()))
        .ReturnsAsync((Book?)null);

    // Act
    var result = await _service.CreateBookAsync(dto);

    // Assert
    result.Should().NotBeNull();
    result.Isbn.Should().Be(dto.Isbn);
    result.Title.Should().Be(dto.Title);
    result.Author.Should().Be(dto.Author);
    result.TotalCopies.Should().Be(dto.TotalCopies);
    result.AvailableCopies.Should().Be(dto.TotalCopies);
    result.IsAvailable.Should().BeTrue();

    _mockBookRepository.Verify(r => r.AddAsync(It.IsAny<Book>()), Times.Once);
}

[Fact]
public async Task CreateBookAsync_WithExistingIsbn_ShouldThrowInvalidOperationException()
{
    // Arrange
    var dto = new CreateBookDto
    {
        Isbn = "9781234567890",
        Title = "Test Book",
        Author = "Test Author",
        Genre = "Fiction",
        TotalCopies = 5
    };

    var existingBook = new Book("9781234567890", "Existing", "Author", "Genre", null, 3);

    _mockBookRepository
        .Setup(r => r.GetByIsbnAsync(It.IsAny<string>()))
        .ReturnsAsync(existingBook);

    // Act
    Func<Task> act = async () => await _service.CreateBookAsync(dto);

    // Assert
    await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*ISBN already exists*");
}

[Fact]
public async Task UpdateStockAsync_WithNegativeChange_ShouldDecrementStock()
{
    // Arrange
    var bookId = 1;
    var book = new Book("9781234567890", "Title", "Author", "Genre", null, 5);
    var dto = new UpdateStockDto { ChangeAmount = -1 };

    _mockBookRepository
        .Setup(r => r.GetByIdAsync(bookId))
        .ReturnsAsync(book);

    // Act
    await _service.UpdateStockAsync(bookId, dto);

    // Assert
    book.AvailableCopies.Should().Be(4);
    _mockBookRepository.Verify(r => r.UpdateAsync(book), Times.Once);
}

[Fact]
public async Task UpdateStockAsync_WithPositiveChange_ShouldIncrementStock()
{
    // Arrange
    var bookId = 1;
    var book = new Book("9781234567890", "Title", "Author", "Genre", null, 5);
    book.DecrementStock(); // AvailableCopies = 4
    var dto = new UpdateStockDto { ChangeAmount = 1 };

    _mockBookRepository
        .Setup(r => r.GetByIdAsync(bookId))
        .ReturnsAsync(book);

    // Act
    await _service.UpdateStockAsync(bookId, dto);

    // Assert
    book.AvailableCopies.Should().Be(5);
    _mockBookRepository.Verify(r => r.UpdateAsync(book), Times.Once);
}

[Fact]
public async Task DeleteBookAsync_WithActiveLoans_ShouldThrowInvalidOperationException()
{
    // Arrange
    var bookId = 1;
    var book = new Book("9781234567890", "Title", "Author", "Genre", null, 5);
    book.DecrementStock(); // Now has 1 copy on loan

    _mockBookRepository
        .Setup(r => r.GetByIdAsync(bookId))
        .ReturnsAsync(book);

    // Act
    Func<Task> act = async () => await _service.DeleteBookAsync(bookId);

    // Assert
    await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*active loans*");
}

[Fact]
public async Task DeleteBookAsync_WithNoLoans_ShouldDeleteBook()
{
    // Arrange
    var bookId = 1;
    var book = new Book("9781234567890", "Title", "Author", "Genre", null, 5);
    // No loans, all copies available

    _mockBookRepository
        .Setup(r => r.GetByIdAsync(bookId))
        .ReturnsAsync(book);

    // Act
    await _service.DeleteBookAsync(bookId);

    // Assert
    _mockBookRepository.Verify(r => r.DeleteAsync(bookId), Times.Once);
}
}

text

### Step 5: Run All Tests

cd ~/Projects/LibHub/tests/LibHub.CatalogService.Tests

Run all tests
dotnet test

Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

text

---

## Acceptance Criteria

- [ ] All domain unit tests pass
- [ ] All application unit tests pass
- [ ] Stock management tests cover increment/decrement scenarios
- [ ] ISBN validation tests cover all edge cases
- [ ] BookApplicationService tests use Moq
- [ ] Delete protection test (cannot delete with active loans)
- [ ] Test coverage > 70%
- [ ] No failing tests
- [ ] All tests run quickly (under 5 seconds)

---

## Verification Commands

Run all tests
cd ~/Projects/LibHub
dotnet test

Run only CatalogService tests
dotnet test tests/LibHub.CatalogService.Tests

Count test results
dotnet test tests/LibHub.CatalogService.Tests --logger "console;verbosity=minimal"

text

**Expected output**: All tests passing (green)

---

## After Completion

### Update PROJECT_STATUS.md

Update **Phase Status Overview**:
| Phase 3: CatalogService | ✅ COMPLETE | 100% (5/5) | All layers implemented and tested |
| Phase 4: LoanService | ⚪ READY TO START | 0% | No blockers |

text

Add to **Completed Tasks**:
✅ Task 3.5: CatalogService Tests written (2025-10-27)

Files Created:

BookTests.cs (domain validation and stock management)

BookApplicationServiceTests.cs (service logic with mocks)

Verification: All tests passing, coverage >70%

Test Count: 15+ unit tests

Phase 3 Complete: CatalogService fully implemented and tested! 🎉

text

Update **Service Readiness Status**:
| Service | Database | Domain | Application | Infrastructure | Presentation | Tests | Ready? |
| UserService | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| CatalogService | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| LoanService | ✅ | ⚪ | ⚪ | 🟡 | ⚪ | ⚪ | ❌ |

text

Update **Overall Progress**:
Overall Progress: 65% (13/20 tasks complete)

text

### Git Commit
git add tests/LibHub.CatalogService.Tests/
git commit -m "✅ Task 3.5: Write CatalogService tests"
git add ai-docs/PROJECT_STATUS.md
git commit -m "docs: Phase 3 complete! CatalogService ready 🎉"

text

### Move Task File
mv ai-docs/tasks/phase-3-catalogservice/task-3.5-testing.md ai-docs/completed-artifacts/

text

---

## 🎉 Phase 3 Complete!

**CatalogService is now fully implemented, tested, and ready for integration!**

All layers complete:
- ✅ Domain Layer (Book entity with stock management)
- ✅ Application Layer (CRUD operations, search, stock updates)
- ✅ Infrastructure Layer (EF Core repository with search)
- ✅ Presentation Layer (API controllers with Swagger)
- ✅ Tests (comprehensive unit tests)

**Critical Integration Point**: The `PUT /api/books/{id}/stock` endpoint is ready for LoanService Saga integration in Phase 4!

---

## Next Task

**Task 4.1**: Implement LoanService Domain Layer (with Saga state machine)