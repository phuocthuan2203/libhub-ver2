# Task 4.3: Implement LoanService Infrastructure Layer

**Phase**: 4 - LoanService Implementation  
**Layer**: Infrastructure  
**Estimated Time**: 2 hours  
**Dependencies**: Task 4.2 (Application Layer)

---

## Objective

Implement Infrastructure Layer with EF Core repository and **HTTP client for CatalogService**.

---

## Key Implementation Points

### 1. EfLoanRepository

public class EfLoanRepository : ILoanRepository
{
private readonly LoanDbContext _context;

text
public async Task<List<Loan>> GetActiveLoansForUserAsync(int userId)
{
    return await _context.Loans
        .Where(l => l.UserId == userId && l.Status == "CheckedOut")
        .ToListAsync();
}

public async Task<int> CountActiveLoansForUserAsync(int userId)
{
    // Count for max loan limit check (5 active loans)
}

// Implement other methods...
}

text

### 2. CatalogServiceHttpClient (CRITICAL for Saga)

public class CatalogServiceHttpClient : ICatalogServiceClient
{
private readonly HttpClient _httpClient;
private readonly ILogger _logger;

text
public async Task DecrementStockAsync(int bookId)
{
    var dto = new { ChangeAmount = -1 };
    var response = await _httpClient.PutAsJsonAsync(
        $"/api/books/{bookId}/stock", dto);
    
    response.EnsureSuccessStatusCode(); // Throws if fails
}

public async Task IncrementStockAsync(int bookId)
{
    // Similar but ChangeAmount = +1
    // Log warning if fails but don't throw (eventual consistency)
}
}

text

### 3. Dependency Injection Setup

In Program.cs of Api project:
builder.Services.AddHttpClient<ICatalogServiceClient, CatalogServiceHttpClient>(
client => {
client.BaseAddress = new Uri(
builder.Configuration["ExternalServices:CatalogServiceBaseUrl"]);
});

text

appsettings.json:
{
"ExternalServices": {
"CatalogServiceBaseUrl": "http://localhost:5001"
}
}

text

---

## Critical Points

- **HTTP Client Configuration**: Use IHttpClientFactory
- **Error Handling**: DecrementStock should throw if fails (for Saga rollback)
- **IncrementStock**: Log error but don't throw (return operation is less critical)
- **Base URL**: CatalogService at http://localhost:5001

---

## Acceptance Criteria

- [ ] EfLoanRepository with all methods
- [ ] CatalogServiceHttpClient implements ICatalogServiceClient
- [ ] HTTP client registered with DI
- [ ] appsettings.json has CatalogServiceBaseUrl
- [ ] No compilation errors

---

## After Completion

Update **PROJECT_STATUS.md**:
✅ Task 4.3: LoanService Infrastructure Layer (date)

EfLoanRepository with EF Core

CatalogServiceHttpClient for inter-service calls

HTTP client configured with base URL

text

**Next: Task 4.4** (Saga implementation - the core challenge!)