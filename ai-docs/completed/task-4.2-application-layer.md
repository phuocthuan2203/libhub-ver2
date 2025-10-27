# Task 4.2: Implement LoanService Application Layer

**Phase**: 4 - LoanService Implementation  
**Layer**: Application  
**Estimated Time**: 1.5 hours  
**Dependencies**: Task 4.1 (Domain Layer)

---

## Objective

Implement Application Layer with DTOs and prepare for Saga orchestration (actual Saga in Task 4.4).

---

## Key Implementation Points

### 1. DTOs

public class CreateLoanDto { public int BookId { get; set; } }
public class LoanDto

// LoanId, UserId, BookId, Status, CheckoutDate, DueDate, Retu
nDate // IsOverdue, DaysUntilDue (calculat
text

### 2. ICatalogServiceClient Interface

// Define in Application layer, implement in Infrastructure
public interface ICatalogServiceClient
{
<BookDto> GetBookAsync(int bookId);
Task DecrementStockAsync(int book
d); Task IncrementStockAsync(int
text

### 3. LoanApplicationService (Basic Structure)

public class LoanApplicationService
{
private readonly ILoanRepository _loanReposit
text
public async Task<LoanDto> BorrowBookAsync(int userId, CreateLoanDto dto)
{
    // Placeholder - full Saga implementation in Task 4.4
    // For now: just create PENDING loan, return DTO
}

public async Task ReturnBookAsync(int loanId)
{
    // Mark as returned, call CatalogService.IncrementStockAsync
}

public async Task<List<LoanDto>> GetUserLoansAsync(int userId) { }
}

text

---

## Important Notes

- **BorrowBookAsync** will be completed in Task 4.4 (Saga implementation)
- ReturnBookAsync is simpler - just mark returned and increment stock
- ICatalogServiceClient is interface only - HTTP implementation in Task 4.3

---

## Acceptance Criteria

- [ ] All DTOs created
- [ ] ICatalogServiceClient interface defined
- [ ] LoanApplicationService with placeholder methods
- [ ] Reference to Domain layer only
- [ ] No compilation errors

---

## After Completion

Update **PROJECT_STATUS.md**:
✅ Task 4.2: LoanService Application Layer (date)

DTOs and ICatalogServiceClient interface

LoanApplicationService ready for Saga (Task 4.4)

text

**Next: Task 4.3** (Infrastructure with HTTP client)
