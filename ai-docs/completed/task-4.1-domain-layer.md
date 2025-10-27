# Task 4.1: Implement LoanService Domain Layer

**Phase**: 4 - LoanService Implementation  
**Layer**: Domain  
**Estimated Time**: 1-2 hours  
**Dependencies**: Task 1.3 (LoanService DB), Phase 2-3 complete

---

## Objective

Implement Domain Layer with Loan entity featuring **state machine pattern** for Saga orchestration.

---

## Key Implementation Points

### 1. Loan Entity with State Machine

public class Loan
{
// Properties: LoanId, UserId, BookId, CheckoutDate, DueDate, ReturnDate, St
text
public Loan(int userId, int bookId)
{
    // Creates PENDING loan with 14-day due date
    Status = "PENDING";
    DueDate = CheckoutDate.AddDays(14);
}

public void MarkAsCheckedOut() { /* PENDING → CheckedOut */ }
public void MarkAsFailed() { /* PENDING → FAILED (compensating tx) */ }
public void MarkAsReturned() { /* CheckedOut → Returned */ }

public bool IsOverdue => Status == "CheckedOut" && DateTime.UtcNow > DueDate;
}

text

### 2. ILoanRepository Interface

public interface ILoanRepository
{
Task<Loan?> GetByIdAsync(int loan
<Loan>> GetActiveLoansForUserAsync(int userId);
Task<int> CountActiveLoansForUserAsync(int userId);
<Loan>> GetOverdueLoansAsync();
Task AddAsync(Loan lo
n); Task UpdateAsync(Loa
text

---

## Critical Business Rules

1. **Loan Period**: Fixed 14 days
2. **Max Active Loans**: 5 per user
3. **Status Transitions**: Must follow state machine (no arbitrary changes)
4. **Foreign Keys**: UserId and BookId are IDs only (no navigation properties - different databases)

---

## Acceptance Criteria

- [ ] Loan entity with state machine methods
- [ ] Status validation in state transition methods
- [ ] IsOverdue calculated property
- [ ] ILoanRepository with all query methods
- [ ] No external dependencies
- [ ] No compilation errors

---

## After Completion

Update **PROJECT_STATUS.md**:
✅ Task 4.1: LoanService Domain Layer (date)

Loan entity with state machine for Saga

Status: PENDING, CheckedOut, Returned, FAILED

text

Update **Service Readiness**: `| LoanService | ✅ | ✅ | ⚪ | 🟡 | ⚪ | ⚪ | ❌ |`

**Git commit** → **Move task file** → **Next: Task 4.2**