using FluentAssertions;
using LibHub.LoanService.Application.DTOs;
using LibHub.LoanService.Application.Interfaces;
using LibHub.LoanService.Application.Services;
using LibHub.LoanService.Domain;
using Microsoft.Extensions.Logging;
using Moq;

namespace LibHub.LoanService.Tests.Application;

public class LoanApplicationServiceTests
{
    private readonly Mock<ILoanRepository> _mockLoanRepository;
    private readonly Mock<ICatalogServiceClient> _mockCatalogService;
    private readonly Mock<ILogger<LoanApplicationService>> _mockLogger;
    private readonly LoanApplicationService _service;

    public LoanApplicationServiceTests()
    {
        _mockLoanRepository = new Mock<ILoanRepository>();
        _mockCatalogService = new Mock<ICatalogServiceClient>();
        _mockLogger = new Mock<ILogger<LoanApplicationService>>();
        _service = new LoanApplicationService(
            _mockLoanRepository.Object,
            _mockCatalogService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task BorrowBookAsync_HappyPath_ShouldCreateCheckedOutLoan()
    {
        var userId = 1;
        var bookId = 100;
        var dto = new CreateLoanDto { BookId = bookId };

        _mockLoanRepository.Setup(r => r.CountActiveLoansForUserAsync(userId))
            .ReturnsAsync(2);

        _mockCatalogService.Setup(c => c.GetBookAsync(bookId))
            .ReturnsAsync(new BookDto
            {
                BookId = bookId,
                IsAvailable = true,
                AvailableCopies = 5
            });

        _mockCatalogService.Setup(c => c.DecrementStockAsync(bookId))
            .Returns(Task.CompletedTask);

        Loan? capturedLoan = null;
        _mockLoanRepository.Setup(r => r.AddAsync(It.IsAny<Loan>()))
            .Callback<Loan>(loan => capturedLoan = loan)
            .Returns(Task.CompletedTask);

        _mockLoanRepository.Setup(r => r.UpdateAsync(It.IsAny<Loan>()))
            .Returns(Task.CompletedTask);

        var result = await _service.BorrowBookAsync(userId, dto);

        result.Should().NotBeNull();
        result.Status.Should().Be("CheckedOut");
        result.UserId.Should().Be(userId);
        result.BookId.Should().Be(bookId);

        _mockLoanRepository.Verify(r => r.AddAsync(It.IsAny<Loan>()), Times.Once);
        _mockLoanRepository.Verify(r => r.UpdateAsync(It.IsAny<Loan>()), Times.Once);
        _mockCatalogService.Verify(c => c.DecrementStockAsync(bookId), Times.Once);

        capturedLoan.Should().NotBeNull();
        capturedLoan!.Status.Should().Be("CheckedOut");
    }

    [Fact]
    public async Task BorrowBookAsync_MaxLoansReached_ShouldThrowBeforeCreatingLoan()
    {
        var userId = 1;
        var dto = new CreateLoanDto { BookId = 100 };

        _mockLoanRepository.Setup(r => r.CountActiveLoansForUserAsync(userId))
            .ReturnsAsync(5);

        Func<Task> act = async () => await _service.BorrowBookAsync(userId, dto);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Maximum loan limit*");

        _mockLoanRepository.Verify(r => r.AddAsync(It.IsAny<Loan>()), Times.Never);
        _mockCatalogService.Verify(c => c.DecrementStockAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task BorrowBookAsync_BookUnavailable_ShouldMarkAsFailed()
    {
        var userId = 1;
        var bookId = 100;
        var dto = new CreateLoanDto { BookId = bookId };

        _mockLoanRepository.Setup(r => r.CountActiveLoansForUserAsync(userId))
            .ReturnsAsync(2);

        _mockCatalogService.Setup(c => c.GetBookAsync(bookId))
            .ReturnsAsync(new BookDto
            {
                BookId = bookId,
                IsAvailable = false,
                AvailableCopies = 0
            });

        Loan? capturedLoan = null;
        _mockLoanRepository.Setup(r => r.AddAsync(It.IsAny<Loan>()))
            .Callback<Loan>(loan => capturedLoan = loan)
            .Returns(Task.CompletedTask);

        _mockLoanRepository.Setup(r => r.UpdateAsync(It.IsAny<Loan>()))
            .Returns(Task.CompletedTask);

        Func<Task> act = async () => await _service.BorrowBookAsync(userId, dto);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Book is not available*");

        _mockLoanRepository.Verify(r => r.AddAsync(It.IsAny<Loan>()), Times.Once);
        _mockLoanRepository.Verify(r => r.UpdateAsync(It.IsAny<Loan>()), Times.Once);
        _mockCatalogService.Verify(c => c.DecrementStockAsync(It.IsAny<int>()), Times.Never);

        capturedLoan.Should().NotBeNull();
        capturedLoan!.Status.Should().Be("FAILED");
    }

    [Fact]
    public async Task BorrowBookAsync_StockDecrementFails_ShouldMarkAsFailed()
    {
        var userId = 1;
        var bookId = 100;
        var dto = new CreateLoanDto { BookId = bookId };

        _mockLoanRepository.Setup(r => r.CountActiveLoansForUserAsync(userId))
            .ReturnsAsync(2);

        _mockCatalogService.Setup(c => c.GetBookAsync(bookId))
            .ReturnsAsync(new BookDto
            {
                BookId = bookId,
                IsAvailable = true,
                AvailableCopies = 5
            });

        _mockCatalogService.Setup(c => c.DecrementStockAsync(bookId))
            .ThrowsAsync(new Exception("Network error"));

        Loan? capturedLoan = null;
        _mockLoanRepository.Setup(r => r.AddAsync(It.IsAny<Loan>()))
            .Callback<Loan>(loan => capturedLoan = loan)
            .Returns(Task.CompletedTask);

        _mockLoanRepository.Setup(r => r.UpdateAsync(It.IsAny<Loan>()))
            .Returns(Task.CompletedTask);

        Func<Task> act = async () => await _service.BorrowBookAsync(userId, dto);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to borrow book*");

        _mockLoanRepository.Verify(r => r.AddAsync(It.IsAny<Loan>()), Times.Once);
        _mockLoanRepository.Verify(r => r.UpdateAsync(It.IsAny<Loan>()), Times.Once);

        capturedLoan.Should().NotBeNull();
        capturedLoan!.Status.Should().Be("FAILED");
    }

    [Fact]
    public async Task ReturnBookAsync_WithCheckedOutLoan_ShouldMarkReturned()
    {
        var loanId = 1;
        var loan = new Loan(1, 100);
        loan.MarkAsCheckedOut();

        _mockLoanRepository.Setup(r => r.GetByIdAsync(loanId))
            .ReturnsAsync(loan);

        _mockLoanRepository.Setup(r => r.UpdateAsync(It.IsAny<Loan>()))
            .Returns(Task.CompletedTask);

        _mockCatalogService.Setup(c => c.IncrementStockAsync(100))
            .Returns(Task.CompletedTask);

        await _service.ReturnBookAsync(loanId);

        loan.Status.Should().Be("Returned");
        loan.ReturnDate.Should().NotBeNull();

        _mockLoanRepository.Verify(r => r.UpdateAsync(It.IsAny<Loan>()), Times.Once);
        _mockCatalogService.Verify(c => c.IncrementStockAsync(100), Times.Once);
    }

    [Fact]
    public async Task ReturnBookAsync_LoanNotFound_ShouldThrowInvalidOperationException()
    {
        var loanId = 999;

        _mockLoanRepository.Setup(r => r.GetByIdAsync(loanId))
            .ReturnsAsync((Loan?)null);

        Func<Task> act = async () => await _service.ReturnBookAsync(loanId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Loan not found*");

        _mockCatalogService.Verify(c => c.IncrementStockAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetUserLoansAsync_ShouldReturnUserLoans()
    {
        var userId = 1;
        var loans = new List<Loan>
        {
            new Loan(userId, 100),
            new Loan(userId, 101)
        };

        _mockLoanRepository.Setup(r => r.GetAllLoansForUserAsync(userId))
            .ReturnsAsync(loans);

        var result = await _service.GetUserLoansAsync(userId);

        result.Should().HaveCount(2);
        result.All(l => l.UserId == userId).Should().BeTrue();
    }

    [Fact]
    public async Task GetLoanByIdAsync_WithExistingLoan_ShouldReturnLoanDto()
    {
        var loanId = 1;
        var loan = new Loan(1, 100);

        _mockLoanRepository.Setup(r => r.GetByIdAsync(loanId))
            .ReturnsAsync(loan);

        var result = await _service.GetLoanByIdAsync(loanId);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(1);
        result.BookId.Should().Be(100);
    }

    [Fact]
    public async Task GetLoanByIdAsync_WithNonExistingLoan_ShouldReturnNull()
    {
        var loanId = 999;

        _mockLoanRepository.Setup(r => r.GetByIdAsync(loanId))
            .ReturnsAsync((Loan?)null);

        var result = await _service.GetLoanByIdAsync(loanId);

        result.Should().BeNull();
    }
}
