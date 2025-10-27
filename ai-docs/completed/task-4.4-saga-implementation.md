# Task 4.4: Implement Saga Pattern for Borrow Book

**Phase**: 4 - LoanService Implementation  
**Type**: Business Logic / Orchestration  
**Estimated Time**: 2-3 hours  
**Dependencies**: Task 4.3 (Infrastructure Layer)

---

## Objective

Implement the **Saga orchestration pattern** in LoanApplicationService.BorrowBookAsync for distributed transaction across loan_db and catalog_db.

---

## The 5-Step Saga Workflow

public async Task<LoanDto> BorrowBookAsync(int userId, CreateLoanDto dto)
{
// STEP 1: Check max loan limit (5 active loans)
var activeCount = await _loanRepository.CountActiveLoansForUserAsync(userId);
if (activeCount >= 5) throw new InvalidOperationException("Max loans reached");

text
// STEP 2: Create PENDING loan in local DB (loan_db)
var loan = new Loan(userId, dto.BookId);
await _loanRepository.AddAsync(loan);

try
{
    // STEP 3: Verify availability via HTTP call
    var book = await _catalogService.GetBookAsync(dto.BookId);
    if (!book.IsAvailable)
    {
        // Compensating transaction
        loan.MarkAsFailed();
        await _loanRepository.UpdateAsync(loan);
        throw new InvalidOperationException("Book not available");
    }
    
    // STEP 4: Decrement stock via HTTP call (distributed transaction)
    await _catalogService.DecrementStockAsync(dto.BookId);
}
catch (Exception ex)
{
    // STEP 5b: COMPENSATING TRANSACTION - Saga failed
    loan.MarkAsFailed();
    await _loanRepository.UpdateAsync(loan);
    _logger.LogError(ex, "Saga failed for BookId {BookId}", dto.BookId);
    throw new InvalidOperationException($"Borrow failed: {ex.Message}", ex);
}

// STEP 5a: SUCCESS - Finalize local transaction
loan.MarkAsCheckedOut();
await _loanRepository.UpdateAsync(loan);

return MapToDto(loan);
}

text

---

## Critical Saga Principles

1. **Local State First**: Create PENDING loan before calling remote service
2. **Try-Catch for Compensation**: If remote call fails, mark loan as FAILED
3. **Idempotency**: Operations should be repeatable safely
4. **Error Logging**: Log all Saga failures for debugging
5. **Clear States**: PENDING (in-progress), CheckedOut (success), FAILED (rollback)

---

## Why This Pattern?

- **No 2PC**: Databases are separate, can't use distributed transactions
- **Eventual Consistency**: Systems eventually reach consistent state
- **Rollback via Compensation**: Can't undo remote calls, so mark local state as FAILED
- **Observability**: Status in DB shows transaction state

---

## Testing Scenarios

1. **Happy Path**: User borrows available book → CheckedOut
2. **Max Loans**: User has 5 active loans → Exception before PENDING
3. **Book Unavailable**: No copies → PENDING then FAILED
4. **Network Failure**: CatalogService unreachable → PENDING then FAILED
5. **Stock Decrement Fails**: No stock → PENDING then FAILED

---

## Acceptance Criteria

- [ ] BorrowBookAsync implements complete 5-step Saga
- [ ] Compensating transaction marks loan as FAILED
- [ ] Max loan limit checked before creating loan
- [ ] HTTP calls wrapped in try-catch
- [ ] Error logging for Saga failures
- [ ] Status transitions follow state machine
- [ ] All test scenarios handled

---

## After Completion

Update **PROJECT_STATUS.md**:
✅ Task 4.4: LoanService Saga Implementation (date)

5-step Saga orchestration in BorrowBookAsync

Compensating transactions for failure scenarios

Distributed transaction across loan_db and catalog_db

This is the core technical challenge of LoanService!

text

**Next: Task 4.5** (Presentation layer with Saga testing)