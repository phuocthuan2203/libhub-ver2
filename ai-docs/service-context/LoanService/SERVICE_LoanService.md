# LoanService - Clean Architecture Design

**Domain**: Borrowing & Returns Management  
**Port**: 5003  
**Database**: loan_db  
**Responsibilities**: Loan creation, returns, **Saga orchestration** for distributed transactions  
**Special**: Makes HTTP calls to CatalogService

---

## Clean Architecture Layers

### 1. Domain Layer (`LibHub.LoanService.Domain`)

**Zero external dependencies - pure C# only**

#### Loan Entity (Aggregate Root)
```
public class Loan
{
    public int LoanId { get; private set; }
    public int UserId { get; private set; }              // FK reference (ID only, no navigation)
    public int BookId { get; private set; }              // FK reference (ID only, no navigation)
    public DateTime CheckoutDate { get; private set; }
    public DateTime DueDate { get; private set; }
    public DateTime? ReturnDate { get; private set; }
    public string Status { get; private set; }
    
    public bool IsOverdue => Status == "CheckedOut" && DateTime.UtcNow > DueDate;
    
    // Constructor - creates PENDING loan (Saga step 2)
    public Loan(int userId, int bookId)
    {
        if (userId <= 0) throw new ArgumentException("Invalid user ID");
        if (bookId <= 0) throw new ArgumentException("Invalid book ID");
        
        UserId = userId;
        BookId = bookId;
        CheckoutDate = DateTime.UtcNow;
        DueDate = CheckoutDate.AddDays(14);  // Business rule: 14-day loan period
        Status = "PENDING";
        ReturnDate = null;
    }
    
    // Saga step 5a - success path
    public void MarkAsCheckedOut()
    {
        if (Status != "PENDING")
            throw new InvalidOperationException("Can only mark PENDING loans as checked out");
        
        Status = "CheckedOut";
    }
    
    // Saga step 5b - compensating transaction (failure path)
    public void MarkAsFailed()
    {
        if (Status != "PENDING")
            throw new InvalidOperationException("Can only mark PENDING loans as failed");
        
        Status = "FAILED";
    }
    
    // Return book operation
    public void MarkAsReturned()
    {
        if (Status != "CheckedOut")
            throw new InvalidOperationException("Can only return loans that are checked out");
        
        Status = "Returned";
        ReturnDate = DateTime.UtcNow;
    }
    
    public int DaysUntilDue() => Status == "CheckedOut" ? (DueDate - DateTime.UtcNow).Days : 0;
    public int DaysOverdue() => IsOverdue ? (DateTime.UtcNow - DueDate).Days : 0;
    public bool IsActive() => Status == "CheckedOut";
}
```

**Status State Machine**:
```
PENDING → CheckedOut → Returned
   ↓
 FAILED
```

**Valid Status Values**: "PENDING", "CheckedOut", "Returned", "FAILED"

#### Repository Interface
```
public interface ILoanRepository
{
    Task<Loan> GetByIdAsync(int loanId);
    Task<List<Loan>> GetActiveLoansForUserAsync(int userId);
    Task<List<Loan>> GetAllLoansForUserAsync(int userId);
    Task<List<Loan>> GetAllActiveLoansAsync();
    Task<List<Loan>> GetOverdueLoansAsync();
    Task<int> CountActiveLoansForUserAsync(int userId);
    Task AddAsync(Loan loan);
    Task UpdateAsync(Loan loan);
}
```

---

### 2. Application Layer (`LibHub.LoanService.Application`)

**Depends on Domain Layer only**

#### DTOs
```
public class CreateLoanDto
{
    public int BookId { get; set; }
    // UserId extracted from JWT claims in controller
}

public class LoanDto
{
    public int LoanId { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public string Status { get; set; }
    public DateTime CheckoutDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysUntilDue { get; set; }
}

public class BookAvailabilityDto
{
    public int BookId { get; set; }
    public bool IsAvailable { get; set; }
    public int AvailableCopies { get; set; }
}
```

#### Infrastructure Interface (defined in Application)
```
public interface ICatalogServiceClient
{
    Task<BookAvailabilityDto> GetBookAsync(int bookId);
    Task DecrementStockAsync(int bookId);
    Task IncrementStockAsync(int bookId);
}
```

#### Application Service - THE SAGA ORCHESTRATOR
```
public class LoanApplicationService
{
    private readonly ILoanRepository _loanRepository;
    private readonly ICatalogServiceClient _catalogService;
    
    // CRITICAL: This implements the Saga pattern for "Borrow Book"
    public async Task<LoanDto> BorrowBookAsync(int userId, CreateLoanDto dto)
    {
        // SAGA STEP 1: Check user's active loan count (max 5)
        var activeLoansCount = await _loanRepository.CountActiveLoansForUserAsync(userId);
        if (activeLoansCount >= 5)
            throw new Exception("Maximum loan limit reached (5 active loans)");
        
        // SAGA STEP 2: Create PENDING loan record in loan_db
        var loan = new Loan(userId, dto.BookId);
        await _loanRepository.AddAsync(loan);
        
        try
        {
            // SAGA STEP 3: Verify book availability with CatalogService
            var book = await _catalogService.GetBookAsync(dto.BookId);
            if (!book.IsAvailable)
            {
                loan.MarkAsFailed();
                await _loanRepository.UpdateAsync(loan);
                throw new Exception("Book is not available");
            }
            
            // SAGA STEP 4: Execute distributed transaction - decrement stock
            await _catalogService.DecrementStockAsync(dto.BookId);
        }
        catch (Exception ex)
        {
            // SAGA STEP 5b: COMPENSATING TRANSACTION - mark loan as failed
            loan.MarkAsFailed();
            await _loanRepository.UpdateAsync(loan);
            throw new Exception($"Failed to borrow book: {ex.Message}", ex);
        }
        
        // SAGA STEP 5a: SUCCESS - finalize local transaction
        loan.MarkAsCheckedOut();
        await _loanRepository.UpdateAsync(loan);
        
        return MapToDto(loan);
    }
    
    public async Task ReturnBookAsync(int loanId)
    {
        // 1. Get loan
        var loan = await _loanRepository.GetByIdAsync(loanId);
        if (loan == null)
            throw new Exception("Loan not found");
        
        // 2. Mark as returned
        loan.MarkAsReturned();
        await _loanRepository.UpdateAsync(loan);
        
        // 3. Increment stock in CatalogService
        try
        {
            await _catalogService.IncrementStockAsync(loan.BookId);
        }
        catch (Exception ex)
        {
            // Log error but don't fail - book is already returned in our system
            // This could be handled by eventual consistency / retry mechanism
            Console.WriteLine($"Warning: Failed to increment stock for book {loan.BookId}: {ex.Message}");
        }
    }
    
    public async Task<List<LoanDto>> GetUserLoansAsync(int userId)
    {
        var loans = await _loanRepository.GetAllLoansForUserAsync(userId);
        return loans.Select(MapToDto).ToList();
    }
    
    public async Task<List<LoanDto>> GetAllLoansAsync()
    {
        var loans = await _loanRepository.GetAllActiveLoansAsync();
        return loans.Select(MapToDto).ToList();
    }
    
    private LoanDto MapToDto(Loan loan) => new LoanDto
    {
        LoanId = loan.LoanId,
        UserId = loan.UserId,
        BookId = loan.BookId,
        Status = loan.Status,
        CheckoutDate = loan.CheckoutDate,
        DueDate = loan.DueDate,
        ReturnDate = loan.ReturnDate,
        IsOverdue = loan.IsOverdue,
        DaysUntilDue = loan.DaysUntilDue()
    };
}
```

---

### 3. Infrastructure Layer (`LibHub.LoanService.Infrastructure`)

**Implements interfaces from Domain and Application layers**

#### EfLoanRepository
```
public class EfLoanRepository : ILoanRepository
{
    private readonly LoanDbContext _context;
    
    public async Task<Loan> GetByIdAsync(int loanId)
    {
        return await _context.Loans.FindAsync(loanId);
    }
    
    public async Task<List<Loan>> GetActiveLoansForUserAsync(int userId)
    {
        return await _context.Loans
            .Where(l => l.UserId == userId && l.Status == "CheckedOut")
            .ToListAsync();
    }
    
    public async Task<int> CountActiveLoansForUserAsync(int userId)
    {
        return await _context.Loans
            .CountAsync(l => l.UserId == userId && l.Status == "CheckedOut");
    }
    
    public async Task<List<Loan>> GetOverdueLoansAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.Loans
            .Where(l => l.Status == "CheckedOut" && l.DueDate < now)
            .ToListAsync();
    }
    
    public async Task AddAsync(Loan loan)
    {
        await _context.Loans.AddAsync(loan);
        await _context.SaveChangesAsync();
    }
    
    public async Task UpdateAsync(Loan loan)
    {
        _context.Loans.Update(loan);
        await _context.SaveChangesAsync();
    }
}
```

#### CatalogServiceHttpClient - CRITICAL FOR SAGA
```
public class CatalogServiceHttpClient : ICatalogServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogServiceHttpClient> _logger;
    
    public CatalogServiceHttpClient(HttpClient httpClient, ILogger<CatalogServiceHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public async Task<BookAvailabilityDto> GetBookAsync(int bookId)
    {
        var response = await _httpClient.GetAsync($"/api/books/{bookId}");
        response.EnsureSuccessStatusCode();
        
        var book = await response.Content.ReadFromJsonAsync<BookAvailabilityDto>();
        return book;
    }
    
    public async Task DecrementStockAsync(int bookId)
    {
        var stockDto = new { ChangeAmount = -1 };
        var response = await _httpClient.PutAsJsonAsync($"/api/books/{bookId}/stock", stockDto);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError($"Failed to decrement stock for book {bookId}: {response.StatusCode}");
            throw new Exception($"Failed to decrement stock: {response.StatusCode}");
        }
    }
    
    public async Task IncrementStockAsync(int bookId)
    {
        var stockDto = new { ChangeAmount = 1 };
        var response = await _httpClient.PutAsJsonAsync($"/api/books/{bookId}/stock", stockDto);
        
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning($"Failed to increment stock for book {bookId}: {response.StatusCode}");
            // Don't throw - log and continue (eventual consistency)
        }
    }
}
```

#### DbContext
```
public class LoanDbContext : DbContext
{
    public DbSet<Loan> Loans { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Loan>(entity =>
        {
            entity.HasKey(e => e.LoanId);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.BookId).IsRequired();
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
            entity.Property(e => e.CheckoutDate).IsRequired();
            entity.Property(e => e.DueDate).IsRequired();
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.BookId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DueDate);
        });
    }
}
```

---

### 4. Presentation Layer (`LibHub.LoanService.Api`)

**Thin controllers - delegate to Application Layer**

#### LoansController
```
[ApiController]
[Route("api/loans")]
[Authorize]
public class LoansController : ControllerBase
{
    private readonly LoanApplicationService _loanService;
    
    [HttpPost]
    public async Task<IActionResult> BorrowBook(CreateLoanDto dto)
    {
        // Extract userId from JWT claims
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        
        var loan = await _loanService.BorrowBookAsync(userId, dto);
        return CreatedAtAction(nameof(GetLoanById), new { id = loan.LoanId }, loan);
    }
    
    [HttpPut("{id}/return")]
    public async Task<IActionResult> ReturnBook(int id)
    {
        await _loanService.ReturnBookAsync(id);
        return NoContent();
    }
    
    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserLoans(int userId)
    {
        // Optionally verify user can only see their own loans
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var isAdmin = User.IsInRole("Admin");
        
        if (!isAdmin && currentUserId != userId)
            return Forbid();
        
        var loans = await _loanService.GetUserLoansAsync(userId);
        return Ok(loans);
    }
    
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllLoans()
    {
        var loans = await _loanService.GetAllLoansAsync();
        return Ok(loans);
    }
    
    [HttpGet("{id}")]
    public async Task<IActionResult> GetLoanById(int id)
    {
        // Implementation
    }
}
```

#### Dependency Injection (Program.cs)
```
// Database
builder.Services.AddDbContext<LoanDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// Repositories
builder.Services.AddScoped<ILoanRepository, EfLoanRepository>();

// Application Services
builder.Services.AddScoped<LoanApplicationService>();

// HTTP Client for CatalogService
builder.Services.AddHttpClient<ICatalogServiceClient, CatalogServiceHttpClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ExternalServices:CatalogServiceBaseUrl"]);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
        };
    });
```

#### appsettings.json Addition
```
{
  "ExternalServices": {
    "CatalogServiceBaseUrl": "http://localhost:5001"
  }
}
```

---

## Required NuGet Packages

```
<!-- Domain (no packages) -->

<!-- Application (no packages) -->

<!-- Infrastructure -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.*" />
<PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="8.0.*" />

<!-- Presentation -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.*" />
```

---

## Saga Pattern Implementation - Critical Notes

### The 5-Step Saga for "Borrow Book"

1. **Check Preconditions**: Verify user hasn't reached max loan limit (5 active loans)
2. **Create PENDING Loan**: Insert loan record in loan_db with status "PENDING"
3. **Verify Availability**: HTTP GET to CatalogService `/api/books/{id}`
4. **Execute Remote Transaction**: HTTP PUT to CatalogService `/api/books/{id}/stock` with `ChangeAmount: -1`
5. **Finalize**:
   - **Success Path**: Update loan status to "CheckedOut"
   - **Failure Path**: Update loan status to "FAILED" (compensating transaction)

### Key Saga Principles

- **Orchestration-Based**: LoanService controls the workflow
- **Compensating Transactions**: If step 4 fails, mark loan as FAILED
- **Idempotency**: Saga steps should be idempotent where possible
- **Error Handling**: Catch exceptions from HTTP calls, don't let them crash the service
- **Logging**: Log all saga steps for debugging distributed transactions

---

## Key Implementation Notes

1. **Saga is Critical**: BorrowBookAsync implements distributed transaction - test thoroughly
2. **HTTP Client Configuration**: Use IHttpClientFactory, set base URL in appsettings.json
3. **Status State Machine**: Enforce valid transitions in Loan entity methods
4. **14-Day Loan Period**: Hardcoded business rule in Loan constructor
5. **Max 5 Active Loans**: Check before creating loan
6. **Extract UserId from JWT**: Use `ClaimTypes.NameIdentifier` in controller
7. **Authorization**: Users can only see their own loans unless they're Admin
8. **Return Operation**: Simpler than borrow - increment stock, less critical if fails
9. **Overdue Tracking**: Use IsOverdue property, calculate on-demand
10. **No Foreign Keys**: UserId and BookId are just integers, no navigation properties

