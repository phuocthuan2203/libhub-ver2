using FluentAssertions;
using LibHub.LoanService.Domain;

namespace LibHub.LoanService.Tests.Domain;

public class LoanTests
{
    [Fact]
    public void Loan_Constructor_ShouldCreatePendingLoanWith14DayDueDate()
    {
        var loan = new Loan(userId: 1, bookId: 100);

        loan.Status.Should().Be("PENDING");
        loan.UserId.Should().Be(1);
        loan.BookId.Should().Be(100);
        loan.DueDate.Should().BeCloseTo(DateTime.UtcNow.AddDays(14), TimeSpan.FromSeconds(1));
        loan.ReturnDate.Should().BeNull();
    }

    [Fact]
    public void Loan_Constructor_WithInvalidUserId_ShouldThrowArgumentException()
    {
        Action act = () => new Loan(userId: 0, bookId: 100);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid user ID*");
    }

    [Fact]
    public void Loan_Constructor_WithInvalidBookId_ShouldThrowArgumentException()
    {
        Action act = () => new Loan(userId: 1, bookId: -1);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid book ID*");
    }

    [Fact]
    public void MarkAsCheckedOut_FromPending_ShouldSucceed()
    {
        var loan = new Loan(1, 100);

        loan.MarkAsCheckedOut();

        loan.Status.Should().Be("CheckedOut");
    }

    [Fact]
    public void MarkAsCheckedOut_FromFailed_ShouldThrowInvalidOperationException()
    {
        var loan = new Loan(1, 100);
        loan.MarkAsFailed();

        Action act = () => loan.MarkAsCheckedOut();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Can only mark PENDING loans*");
    }

    [Fact]
    public void MarkAsFailed_FromPending_ShouldSucceed()
    {
        var loan = new Loan(1, 100);

        loan.MarkAsFailed();

        loan.Status.Should().Be("FAILED");
    }

    [Fact]
    public void MarkAsFailed_FromCheckedOut_ShouldThrowInvalidOperationException()
    {
        var loan = new Loan(1, 100);
        loan.MarkAsCheckedOut();

        Action act = () => loan.MarkAsFailed();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkAsReturned_FromCheckedOut_ShouldSucceed()
    {
        var loan = new Loan(1, 100);
        loan.MarkAsCheckedOut();

        loan.MarkAsReturned();

        loan.Status.Should().Be("Returned");
        loan.ReturnDate.Should().NotBeNull();
        loan.ReturnDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkAsReturned_FromPending_ShouldThrowInvalidOperationException()
    {
        var loan = new Loan(1, 100);

        Action act = () => loan.MarkAsReturned();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Can only return checked out loans*");
    }

    [Fact]
    public void IsOverdue_WithCheckedOutLoanPastDueDate_ShouldReturnTrue()
    {
        var loan = new Loan(1, 100);
        loan.MarkAsCheckedOut();

        var loanType = typeof(Loan);
        var dueDateField = loanType.GetProperty("DueDate");
        dueDateField!.SetValue(loan, DateTime.UtcNow.AddDays(-1));

        loan.IsOverdue.Should().BeTrue();
    }

    [Fact]
    public void IsOverdue_WithCheckedOutLoanNotDue_ShouldReturnFalse()
    {
        var loan = new Loan(1, 100);
        loan.MarkAsCheckedOut();

        loan.IsOverdue.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WithCheckedOutLoan_ShouldReturnTrue()
    {
        var loan = new Loan(1, 100);
        loan.MarkAsCheckedOut();

        loan.IsActive().Should().BeTrue();
    }

    [Fact]
    public void IsActive_WithPendingLoan_ShouldReturnFalse()
    {
        var loan = new Loan(1, 100);

        loan.IsActive().Should().BeFalse();
    }

    [Fact]
    public void DaysUntilDue_WithCheckedOutLoan_ShouldReturnCorrectDays()
    {
        var loan = new Loan(1, 100);
        loan.MarkAsCheckedOut();

        var days = loan.DaysUntilDue();

        days.Should().BeInRange(13, 14);
    }

    [Fact]
    public void DaysUntilDue_WithPendingLoan_ShouldReturnZero()
    {
        var loan = new Loan(1, 100);

        var days = loan.DaysUntilDue();

        days.Should().Be(0);
    }
}
