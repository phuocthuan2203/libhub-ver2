# Task 4.6: Write LoanService Tests

**Phase**: 4 - LoanService Implementation  
**Type**: Testing  
**Estimated Time**: 2-3 hours  
**Dependencies**: Task 4.5 (Presentation Layer)

---

## Objective

Write comprehensive tests focusing on **Saga orchestration scenarios** and state machine transitions.

---

## Key Test Categories

### 1. Domain Layer Tests

[Fact]
public void Loan_Constructor_ShouldCreatePendingLoanWith14DayDueDate()
{
var loan = new Loan(userId: 1, bookId: 1);

text
loan.Status.Should().Be("PENDING");
loan.DueDate.Should().BeCloseTo(
    DateTime.UtcNow.AddDays(14), TimeSpan.FromSeconds(1));
}

[Fact]
public void MarkAsCheckedOut_FromPending_ShouldSucceed()
{
var loan = new Loan(1, 1);
loan.MarkAsCheckedOut();
loan.Status.Should().Be("CheckedOut");
}

[Fact]
public void MarkAsCheckedOut_FromFailed_ShouldThrowInvalidOperationException()
{
var loan = new Loan(1, 1);
loan.MarkAsFailed();

text
Action act = () => loan.MarkAsCheckedOut();
act.Should().Throw<InvalidOperationException>();
}

text

### 2. Saga Tests (Most Important!)

[Fact]
public async Task BorrowBookAsync_HappyPath_ShouldCreateCheckedOutLoan()
{
// Arrange: Mock repository returns count < 5
// Mock CatalogService returns available book
// Mock stock decrement succeeds

text
// Act: Call BorrowBookAsync

// Assert: Loan status is "CheckedOut"
// Verify: AddAsync called once, UpdateAsync called once
// Verify: DecrementStockAsync called once
}

[Fact]
public async Task BorrowBookAsync_MaxLoansReached_ShouldThrowBeforeCreatingLoan()
{
// Arrange: Mock repository returns count = 5

text
// Act & Assert: Should throw InvalidOperationException
// Verify: AddAsync never called (short-circuit)
}

[Fact]
public async Task BorrowBookAsync_BookUnavailable_ShouldMarkAsFailed()
{
// Arrange: Mock book.IsAvailable = false

text
// Act: Call BorrowBookAsync

// Assert: Exception thrown
// Verify: Loan created as PENDING, then marked FAILED
// Verify: DecrementStockAsync never called
}

[Fact]
public async Task BorrowBookAsync_StockDecrementFails_ShouldMarkAsFailed()
{
// Arrange: Mock DecrementStockAsync throws exception

text
// Act: Call BorrowBookAsync

// Assert: Exception thrown
// Verify: Loan status is "FAILED" (compensating transaction)
}

text

### 3. Return Operation Tests

[Fact]
public async Task ReturnBookAsync_WithCheckedOutLoan_ShouldMarkReturned()
{
// Verify: Status changed to "Returned"
// Verify: IncrementStockAsync called
}

text

---

## Critical Test Coverage

1. **State Machine Transitions**: All valid and invalid transitions
2. **Saga Happy Path**: End-to-end success
3. **Saga Compensating Transactions**: All failure scenarios
4. **Max Loan Limit**: Boundary testing (4 loans OK, 5 loans rejected)
5. **Authorization**: Users can't access other users' loans
6. **Overdue Calculation**: IsOverdue property logic

---

## Acceptance Criteria

- [ ] Domain layer tests cover state machine
- [ ] Saga tests cover all 5 failure scenarios
- [ ] Mock ICatalogServiceClient for unit tests
- [ ] Test coverage > 70%
- [ ] All tests pass
- [ ] Tests run quickly (< 10 seconds)

---

## After Completion

Update **PROJECT_STATUS.md**:
Phase Status Overview
| Phase 4: LoanService | ✅ COMPLETE | 100% (6/6) | Saga pattern implemented & tested! |

Completed Tasks
✅ Task 4.6: LoanService Tests written (date)

Comprehensive Saga scenario tests

State machine transition tests

Test coverage >70%

Phase 4 Complete! 🎉

Service Readiness Status
| LoanService | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |

Implementation Notes
Saga pattern successfully implemented for distributed transactions

Compensating transactions handle all failure scenarios

Three services now complete: UserService, CatalogService, LoanService

text

Update **Overall Progress**: `70% (14/20 tasks complete)`

**Next: Phase 5** - API Gateway (integrate all services!)