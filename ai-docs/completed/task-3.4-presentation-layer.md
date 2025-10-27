# Task 3.4: Implement CatalogService Presentation Layer

**Phase**: 3 - CatalogService Implementation  
**Layer**: Presentation (API)  
**Estimated Time**: 2-3 hours  
**Dependencies**: Task 3.3 (Infrastructure Layer)

---

## Objective

Implement the Presentation Layer (API) for CatalogService with controllers, dependency injection, JWT authentication middleware, and Swagger documentation.

---

## Prerequisites

- [ ] Task 3.1, 3.2, 3.3 completed (all other layers)
- [ ] Understanding of REST API design
- [ ] JWT configuration (same as UserService)

---

## What You'll Implement

1. BooksController with CRUD and search endpoints
2. Program.cs with complete DI configuration
3. appsettings.json with JWT and database configuration
4. JWT authentication middleware (validates tokens from UserService)
5. Swagger/OpenAPI documentation
6. CORS configuration

---

## Step-by-Step Instructions

### Step 1: Add NuGet Packages

cd ~/Projects/LibHub/src/Services/CatalogService/LibHub.CatalogService.Api

dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.*
dotnet add pa

text

### Step 2: Add Project References

cd ~/Projects/LibHub

Api depends on Application and Infrastructure
dotnet add src/Services/CatalogService/LibHub.CatalogService.Api reference src/Services/CatalogService/LibHub.CatalogService.Application
do

text

### Step 3: Create BooksController

**File**: `LibHub.CatalogService.Api/Controllers/BooksController.cs`

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibHub.CatalogService.Application.DTOs;
namespace LibHub.CatalogService.Api.Controllers;

[ApiController]
[Route("api/books")]
public class BooksController : ControllerBase
{
private readonly BookApplicationService _bookServ
<BooksController> _logger;

text
public BooksController(
    BookApplicationService bookService,
    ILogger<BooksController> logger)
{
    _bookService = bookService;
    _logger = logger;
}

/// <summary>
/// Search/list books (public access, no authentication required).
/// </summary>
[HttpGet]
[ProducesResponseType(typeof(List<BookDto>), StatusCodes.Status200OK)]
public async Task<IActionResult> GetBooks(
    [FromQuery] string? search, 
    [FromQuery] string? genre)
{
    try
    {
        var books = await _bookService.SearchBooksAsync(search, genre);
        return Ok(books);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error searching books");
        return StatusCode(500, new { message = "An error occurred while searching books" });
    }
}

/// <summary>
/// Get book by ID (public access).
/// </summary>
[HttpGet("{id}")]
[ProducesResponseType(typeof(BookDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetBookById(int id)
{
    try
    {
        var book = await _bookService.GetBookByIdAsync(id);
        return Ok(book);
    }
    catch (KeyNotFoundException)
    {
        return NotFound(new { message = "Book not found" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error retrieving book {BookId}", id);
        return StatusCode(500, new { message = "An error occurred" });
    }
}

/// <summary>
/// Add new book to catalog (admin only).
/// </summary>
[HttpPost]
[Authorize(Roles = "Admin")]
[ProducesResponseType(typeof(BookDto), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<IActionResult> CreateBook([FromBody] CreateBookDto dto)
{
    try
    {
        var book = await _bookService.CreateBookAsync(dto);
        _logger.LogInformation("Book created: {BookId} - {Title}", book.BookId, book.Title);
        
        return CreatedAtAction(nameof(GetBookById), new { id = book.BookId }, book);
    }
    catch (ArgumentException ex)
    {
        _logger.LogWarning("Book creation validation failed: {Message}", ex.Message);
        return BadRequest(new { message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogWarning("Book creation failed: {Message}", ex.Message);
        return BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating book");
        return StatusCode(500, new { message = "An error occurred" });
    }
}

/// <summary>
/// Update book details (admin only).
/// </summary>
[HttpPut("{id}")]
[Authorize(Roles = "Admin")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<IActionResult> UpdateBook(int id, [FromBody] UpdateBookDto dto)
{
    try
    {
        await _bookService.UpdateBookAsync(id, dto);
        _logger.LogInformation("Book updated: {BookId}", id);
        
        return NoContent();
    }
    catch (KeyNotFoundException)
    {
        return NotFound(new { message = "Book not found" });
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error updating book {BookId}", id);
        return StatusCode(500, new { message = "An error occurred" });
    }
}

/// <summary>
/// Delete book from catalog (admin only).
/// </summary>
[HttpDelete("{id}")]
[Authorize(Roles = "Admin")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<IActionResult> DeleteBook(int id)
{
    try
    {
        await _bookService.DeleteBookAsync(id);
        _logger.LogInformation("Book deleted: {BookId}", id);
        
        return NoContent();
    }
    catch (KeyNotFoundException)
    {
        return NotFound(new { message = "Book not found" });
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogWarning("Cannot delete book {BookId}: {Message}", id, ex.Message);
        return BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error deleting book {BookId}", id);
        return StatusCode(500, new { message = "An error occurred" });
    }
}

/// <summary>
/// Update book stock (internal endpoint for LoanService Saga).
/// </summary>
[HttpPut("{id}/stock")]
[Authorize]  // Any authenticated request (from LoanService)
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockDto dto)
{
    try
    {
        await _bookService.UpdateStockAsync(id, dto);
        _logger.LogInformation("Stock updated for book {BookId}: {ChangeAmount}", id, dto.ChangeAmount);
        
        return NoContent();
    }
    catch (KeyNotFoundException)
    {
        return NotFound(new { message = "Book not found" });
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogWarning("Stock update failed for book {BookId}: {Message}", id, ex.Message);
        return BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error updating stock for book {BookId}", id);
        return StatusCode(500, new { message = "An error occurred" });
    }
}
}

text

### Step 4: Configure Program.cs

**File**: `LibHub.CatalogService.Api/Program.cs`

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using LibHub.CatalogService.Application.Services;
using LibHub.CatalogService.Domain;
using LibHub.CatalogService.Infrastructure;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
c.SwaggerDoc("v1", new OpenApi
n
o { Title = "Ca
alogService API
, Version = "v1", Description = "LibH
text
c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    Description = "JWT Authorization header using the Bearer scheme.",
    Name = "Authorization",
    In = ParameterLocation.Header,
    Type = SecuritySchemeType.ApiKey,
    Scheme = "Bearer"
});

c.AddSecurityRequirement(new OpenApiSecurityRequirement
{
    {
        new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        },
        Array.Empty<string>()
    }
});
});

// Configure Database
builder<CatalogDbContext>(options =>
options.UseMy
ql( builder.Configuration.GetConnectionString("DefaultC
nnection"), ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("
// Configure JWT Authentication (validates tokens from UserService)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(option

> { options.TokenValidationParameters = new TokenVali
a
ionParameters
ValidateIss
er = true, V
lidateAudience = true,
ValidateLifetime = true, ValidateIss
erSigningKey = true, ValidIssuer = builder
Configuration["Jwt:Issuer"], Val
dAudience = builder.Configuration["Jwt:Audience"], I
su
builder.Services.AddAuthorization();

// Configure CORS
builder.Services.AddCors(options =>
{
options.AddDefaultPolicy(polic

> { policy.
llowAnyOrigin()
.Allow
nyM
// Register Application Services
builder.Service<BookApplicationService>();

// Register Infrastructure Services
builder.Services.AddScoped<I

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
app.UseSwagge
(); app.UseSwag
app.UseCors();

app.UseAuthentication();
a

app.MapControllers();

app.Run();

text

### Step 5: Configure appsettings.json

**File**: `LibHub.CatalogService.Api/appsettings.json`

{
"ConnectionStrings":
{ "DefaultConnection": "Server=localhost;Port=3306;Database=catalog_db;User=libhub_user;Password=LibHub@Dev
02
;" },
"Jwt": { "SecretKey": "LibHub_SuperSecret_256BitKey_ForDevelopment_ChangeI
Production!", "Issuer": "Li
Hub.UserService", "Audie
ce
: "LibHub.Cl
ents" },
Logging": { "LogLevel
: { "Default": "Information"
"Microsoft.AspNetCore": "Warning", "Microsoft.Enti
y
ra
eworkCore.Database.C
mmand": "Inf
rmation" }
}, "A
http://localhost:5001"



text

### Step 6: Build and Run CatalogService

cd ~/Projects/LibHub/src/Services/CatalogService/LibHub.CatalogService.Api

Build the project
dotnet build

Run the service
dotnet run

text

Open browser: `http://localhost:5001/swagger`

---

## Acceptance Criteria

- [ ] BooksController with GET (search), GET by ID, POST, PUT, DELETE
- [ ] PUT /api/books/{id}/stock endpoint for Saga integration
- [ ] Program.cs with complete DI configuration
- [ ] JWT authentication middleware configured (validates UserService tokens)
- [ ] Swagger/OpenAPI with JWT authorization UI
- [ ] appsettings.json with catalog_db connection
- [ ] CORS enabled
- [ ] Port 5001 configured in Kestrel
- [ ] Service runs without errors
- [ ] Swagger UI accessible
- [ ] Public endpoints (GET) work without token
- [ ] Admin endpoints (POST/PUT/DELETE) require Admin role
- [ ] Stock endpoint requires any valid token

---

## Verification Commands

Build CatalogService
cd ~/Projects/LibHub
dotnet build src/Services/CatalogService/LibH

Run CatalogService
cd src/Services/CatalogService/LibHub.CatalogService.Api
In another terminal, test public endpoint
curl http://localhost:5001/api/books

Test with admin token (get token from UserService first)
curl -X POST http://localhost:5001/api/books
-H "Authorization: Bearer YOUR_ADMIN_TOKEN"
-H "Content-Type: application/json"
-d '{"isbn":"9781234567890","title":"Test Book","author":"Test Author","genre":"Fiction","totalCopies":5}'

text

---

## After Completion

### Update PROJECT_STATUS.md

Add to **Completed Tasks**:
✅ Task 3.4: CatalogService Presentation Layer implemented (2025-10-27)

Files Created:

BooksController (CRUD + search + stock endpoints)

Program.cs (DI, JWT validation, Swagger)

appsettings.json

Verification: Service runs on port 5001, Swagger accessible

Endpoints Tested: Search, Create, Update, Delete, UpdateStock

Authorization: Public search, Admin CRUD, Authenticated stock updates

text

Update **Service Readiness Status**:
| CatalogService | ✅ | ✅ | ✅ | ✅ | ✅ | ⚪ | 🟡 |

text

Update **Overall Progress**:
Overall Progress: 60% (12/20 tasks complete)

text

### Git Commit
git add src/Services/CatalogService/LibHub.CatalogService.Api/
git commit -m "✅ Task 3.4: Implement CatalogService Presentation Layer"
git add ai-docs/PROJECT_STATUS.md
text

### Move Task File
mv ai-docs/tasks/phase-3-catalogservice/task-3.4-presentation-layer.md ai-docs/completed-artifacts/

text

---

## Next Task

**Task 3.5**: Write CatalogService Tests