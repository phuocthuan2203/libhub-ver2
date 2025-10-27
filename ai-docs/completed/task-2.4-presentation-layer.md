# Task 2.4: Implement UserService Presentation Layer

**Phase**: 2 - UserService Implementation  
**Layer**: Presentation (API)  
**Estimated Time**: 2-3 hours  
**Dependencies**: Task 2.3 (Infrastructure Layer)

---

## Objective

Implement the Presentation Layer (API) for UserService with controllers, dependency injection, JWT authentication middleware, and Swagger documentation.

---

## Prerequisites

- [ ] Task 2.1, 2.2, 2.3 completed (all other layers)
- [ ] Understanding of ASP.NET Core Web API
- [ ] Understanding of dependency injection

---

## What You'll Implement

1. UsersController with register, login, and profile endpoints
2. Program.cs with complete DI configuration
3. appsettings.json with JWT and database configuration
4. JWT authentication middleware
5. Swagger/OpenAPI documentation
6. CORS configuration

---

## Step-by-Step Instructions

### Step 1: Add NuGet Packages to Api Project

cd ~/Projects/LibHub/src/Services/UserService/LibHub.UserService.Api

dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.*
text

### Step 2: Add Project References

cd ~/Projects/LibHub

Api depends on Application and Infrastructure
dotnet add src/Services/UserService/LibHub.UserService.Api reference src/Services/UserService/LibHub.UserService.Application
do

text

### Step 3: Create UsersController

**File**: `LibHub.UserService.Api/Controllers/UsersController.cs`

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibHub.UserService.Application.DTOs;
namespace LibHub.UserService.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
private readonly IdentityApplicationService _identityServ
<UsersController> _logger;

text
public UsersController(
    IdentityApplicationService identityService,
    ILogger<UsersController> logger)
{
    _identityService = identityService;
    _logger = logger;
}

/// <summary>
/// Register a new user account.
/// </summary>
[HttpPost("register")]
[ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
{
    try
    {
        var user = await _identityService.RegisterUserAsync(dto);
        _logger.LogInformation("User registered: {Email}", user.Email);
        
        return CreatedAtAction(nameof(GetUserById), new { id = user.UserId }, user);
    }
    catch (ArgumentException ex)
    {
        _logger.LogWarning("Registration validation failed: {Message}", ex.Message);
        return BadRequest(new { message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogWarning("Registration failed: {Message}", ex.Message);
        return BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Registration error");
        return StatusCode(500, new { message = "An error occurred during registration" });
    }
}

/// <summary>
/// Authenticate user and receive JWT token.
/// </summary>
[HttpPost("login")]
[ProducesResponseType(typeof(TokenDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> Login([FromBody] LoginDto dto)
{
    try
    {
        var token = await _identityService.LoginUserAsync(dto);
        _logger.LogInformation("User logged in: {Email}", dto.Email);
        
        return Ok(token);
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogWarning("Login failed: {Message}", ex.Message);
        return Unauthorized(new { message = "Invalid email or password" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Login error");
        return StatusCode(500, new { message = "An error occurred during login" });
    }
}

/// <summary>
/// Get user by ID (requires authentication).
/// </summary>
[HttpGet("{id}")]
[Authorize]
[ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> GetUserById(int id)
{
    var user = await _identityService.GetUserByIdAsync(id);
    
    if (user == null)
        return NotFound(new { message = "User not found" });
    
    return Ok(user);
}

/// <summary>
/// Get current user profile (requires authentication).
/// </summary>
[HttpGet("me")]
[Authorize]
[ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> GetCurrentUser()
{
    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
    if (userIdClaim == null)
        return Unauthorized();

    var userId = int.Parse(userIdClaim.Value);
    var user = await _identityService.GetUserByIdAsync(userId);
    
    if (user == null)
        return NotFound(new { message = "User not found" });
    
    return Ok(user);
}
}

text

### Step 4: Configure Program.cs

**File**: `LibHub.UserService.Api/Program.cs`

using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using LibHub.UserService.Application.Interfaces;
using LibHub.UserService.Application.Services;
using LibHub.UserService.Domain;
using LibHub.UserService.Infrastructure;
using LibHub.UserService.Infrastructure.Repositories;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
c.SwaggerDoc("v1", new OpenApi
n
o { Title =
UserService API
, Version = "v1", Description = "LibHub User Man
text
c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
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
builder<UserDbContext>(options =>
options.UseMy
ql( builder.Configuration.GetConnectionString("DefaultC
nnection"), ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("
// Configure JWT Authentication
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
builder.Service<IdentityApplicationService>();

// Register Infrastructure Services
builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
app.UseSwagge
(); app.UseSwag
app.UseCors();

app.UseAuthentication();
app.UseAuthorization(

app.MapControllers();

app.Run();

text

### Step 5: Configure appsettings.json

**File**: `LibHub.UserService.Api/appsettings.json`

{
"ConnectionStrings":
{ "DefaultConnection": "Server=localhost;Port=3306;Database=user_db;User=libhub_user;Password=LibHub@Dev
02
;" },
"Jwt": { "SecretKey": "LibHub_SuperSecret_256BitKey_ForDevelopment_ChangeI
Production!", "Issuer": "Li
Hub.UserService", "Audien
e": "LibHub.Client
",
"ExpiryI
Hours": 1 }
"Logging": { "LogL
vel": { "Default": "Informat
on", "Microsoft.AspNetCore": "Warning", "Microsoft.
n
it
FrameworkCore.Databa
e.Command":
Information"
} },
http://localhost:5002"



}

text

**File**: `LibHub.UserService.Api/appsettings.Development.json`

{
"Logging":
{ "LogLev
l": { "Defaul
": "Debug", "Microsoft.AspNetCo
e
:
text

### Step 6: Build and Run UserService

cd ~/Projects/LibHub/src/Services/UserService/LibHub.UserService.Api

Build the project
dotnet build

Run the service
dotnet run

text

Open browser: `http://localhost:5002/swagger`

---

## Acceptance Criteria

- [ ] UsersController with register, login, GetUserById, GetCurrentUser
- [ ] Program.cs with complete DI configuration
- [ ] JWT authentication middleware configured
- [ ] Swagger/OpenAPI with JWT authorization UI
- [ ] appsettings.json with all configuration
- [ ] CORS enabled for frontend
- [ ] Port 5002 configured in Kestrel
- [ ] Logging configured
- [ ] Service runs without errors
- [ ] Swagger UI accessible at http://localhost:5002/swagger
- [ ] Can register user via Swagger
- [ ] Can login and receive JWT token
- [ ] Protected endpoints require Bearer token

---

## Verification Commands

Build entire UserService
cd ~/Projects/LibHub
dotnet build src/Services/UserService/L

Run UserService
cd src/Services/UserService/LibHub.UserService.Api
In another terminal, test endpoints
Register user
curl -X POST http://localhost:5002/api/users/register
-H "Content-Type: application/json"
-d '{"username":"testuser","email":"test@example.com","password":"TestPass123!"}'

Login
curl -X POST http://localhost:5002/api/users/login
-H "Content-Type: application/json"
-d '{"email":"test@example.com","password":"TestPass123!"}'

Copy token from response and test protected endpoint
curl -X GET http://localhost:5002/api/users/me
-H "Authorization: Bearer YOUR_TOKEN_HERE"

text

---

## Testing with Swagger

1. Open `http://localhost:5002/swagger`
2. Test POST `/api/users/register`:
   - Username: "testuser"
   - Email: "test@example.com"
   - Password: "TestPass123!"
3. Test POST `/api/users/login`:
   - Email: "test@example.com"
   - Password: "TestPass123!"
   - Copy the `accessToken` from response
4. Click "Authorize" button in Swagger
   - Enter: `Bearer YOUR_TOKEN_HERE`
5. Test GET `/api/users/me` (should work with token)
6. Test GET `/api/users/{id}` (should work with token)

---

## After Completion

### Update PROJECT_STATUS.md

Add to **Completed Tasks**:
✅ Task 2.4: UserService Presentation Layer implemented (2025-10-27)

Files Created:

UsersController (register, login, profile endpoints)

Program.cs (DI, JWT, Swagger configuration)

appsettings.json (database, JWT configuration)

Verification: Service runs on port 5002, Swagger accessible, JWT auth working

Endpoints Tested: Register, Login, GetUserById, GetCurrentUser

text

Update **Service Readiness Status**:
| UserService | ✅ | ✅ | ✅ | ✅ | ✅ | ⚪ | 🟡 |

text

Update **Overall Progress**:
Overall Progress: 35% (7/20 tasks complete)

text

Update **Current Task**:
Current Task
Task 2.5: Write UserService Tests
Started: 2025-10-27
Blocked By: None

text

### Git Commit
git add src/Services/UserService/LibHub.UserService.Api/
git commit -m "✅ Task 2.4: Implement UserService Presentation Layer"
git add ai-docs/PROJECT_STATUS.md
text

### Move Task File
mv ai-docs/tasks/phase-2-userservice/task-2.4-presentation-layer.md ai-docs/completed-artifacts/

text

---

## Next Task

**Task 2.5**: Write UserService Tests (unit and integration tests)