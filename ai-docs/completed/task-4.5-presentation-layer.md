# Task 4.5: Implement LoanService Presentation Layer

**Phase**: 4 - LoanService Implementation  
**Layer**: Presentation (API)  
**Estimated Time**: 1.5-2 hours  
**Dependencies**: Task 4.4 (Saga implementation)

---

## Objective

Implement Presentation Layer with controllers, DI configuration, JWT authentication, and Swagger.

---

## Key Implementation Points

### 1. LoansController

[ApiController]
[Route("api/loans")]
[Authorize] // All endpoints require authentication
public class LoansController : ControllerBase
{
private readonly LoanApplicationService _loanService;

text
[HttpPost]
public async Task<IActionResult> BorrowBook(CreateLoanDto dto)
{
    // Extract userId from JWT claims
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
    
    try {
        var loan = await _loanService.BorrowBookAsync(userId, dto);
        return CreatedAtAction(nameof(GetLoanById), 
            new { id = loan.LoanId }, loan);
    }
    catch (InvalidOperationException ex) {
        return BadRequest(new { message = ex.Message });
    }
}

[HttpPut("{id}/return")]
public async Task<IActionResult> ReturnBook(int id)
{
    // Call ReturnBookAsync, handle exceptions
}

[HttpGet("user/{userId}")]
public async Task<IActionResult> GetUserLoans(int userId)
{
    // Check authorization: user can only see own loans (or admin)
}

[HttpGet]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> GetAllLoans() { }
}

text

### 2. Program.cs Configuration

// Database
builder.Services.AddDbContext<LoanDbContext>(...);

// Repositories
builder.Services.AddScoped<ILoanRepository, EfLoanRepository>();

// Application Services
builder.Services.AddScoped<LoanApplicationService>();

// HTTP Client for CatalogService (already configured in Task 4.3)
builder.Services.AddHttpClient<ICatalogServiceClient, CatalogServiceHttpClient>(...);

// JWT Authentication (same config as other services)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)...

// CORS
builder.Services.AddCors(...);

text

### 3. appsettings.json

{
"ConnectionStrings": {
"DefaultConnection": "Server=localhost;Port=3306;Database=loan_db;..."
},
"Jwt": { /* Same as UserService */ },
"ExternalServices": {
"CatalogServiceBaseUrl": "http://localhost:5001"
},
"Kestrel": {
"Endpoints": { "Http": { "Url": "http://localhost:5003" } }
}
}

text

---

## Important Points

- **Port 5003** for LoanService
- **UserId from JWT**: Extract from claims, don't trust client input
- **Authorization**: Users can only access their own loans unless Admin
- **Error Handling**: Return 400 for business rule violations, 500 for system errors
- **Swagger**: Document Saga behavior in endpoint descriptions

---

## Acceptance Criteria

- [ ] LoansController with all endpoints
- [ ] UserId extracted from JWT claims
- [ ] Authorization checks (own loans only)
- [ ] Program.cs with complete DI
- [ ] appsettings.json configured
- [ ] Port 5003 in Kestrel
- [ ] Service runs without errors
- [ ] Swagger accessible at http://localhost:5003/swagger
- [ ] Can borrow and return books via API

---

## After Completion

Update **PROJECT_STATUS.md**:
✅ Task 4.5: LoanService Presentation Layer (date)

LoansController with Saga-backed endpoints

JWT authentication and authorization

Running on port 5003

text

**Next: Task 4.6** (Testing - especially Saga scenarios!)
