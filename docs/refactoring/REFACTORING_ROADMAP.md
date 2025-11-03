# LibHub Microservices Refactoring Roadmap

> **Version:** 1.0  
> **Date:** November 3, 2025  
> **Branch:** `refactor/simplify-architecture`  
> **Estimated Time:** 8-12 hours total

---

## 🎯 Refactoring Goals

### **Primary Objectives**
1. **Simplify Architecture** - From 4 projects per service to 1 project per service
2. **Reduce Complexity** - Remove unnecessary abstractions and interfaces
3. **Improve Maintainability** - Easier to navigate, understand, and modify
4. **Maintain Stability** - Zero breaking changes to frontend API contracts
5. **Preserve Functionality** - All features work exactly as before

### **Success Criteria**
- ✅ All services build successfully
- ✅ All services run in Docker
- ✅ Frontend works without any modifications
- ✅ API contracts remain unchanged
- ✅ Consul service discovery works
- ✅ 30-40% reduction in code complexity
- ✅ Single project per service

---

## 📋 Overall Strategy

### **Refactoring Order**
```
Phase 1: UserService       (Simplest, 4-6 hours)
    ↓
Phase 2: CatalogService    (Simple, 4-6 hours)
    ↓
Phase 3: LoanService       (Complex, 6-8 hours)
    ↓
Phase 4: Gateway           (Minimal, 1-2 hours)
    ↓
Phase 5: Integration       (Testing, 2-3 hours)
```

### **Working Principle**
> Refactor one service at a time, test thoroughly, commit, then move to next service.

---

## 🔧 Phase 1: UserService Refactoring

**Status:** 🎯 Ready to Start  
**Complexity:** ⭐⭐☆☆☆ (Simple)  
**Estimated Time:** 4-6 hours  
**Branch:** `refactor/simplify-architecture`

### **Current State Analysis**

**Current Structure:**
```
UserService/
├── LibHub.UserService.Domain/
│   ├── User.cs
│   ├── IUserRepository.cs
│   ├── IPasswordHasher.cs
│   └── Exceptions/
├── LibHub.UserService.Application/
│   ├── Services/
│   │   └── IdentityApplicationService.cs
│   ├── DTOs/
│   │   ├── RegisterUserDto.cs
│   │   ├── LoginDto.cs
│   │   ├── UserDto.cs
│   │   └── TokenDto.cs
│   ├── Validation/
│   │   └── PasswordValidator.cs
│   └── Interfaces/
│       └── IJwtTokenGenerator.cs
├── LibHub.UserService.Infrastructure/
│   ├── Repositories/
│   │   └── EfUserRepository.cs
│   ├── Security/
│   │   ├── PasswordHasher.cs
│   │   └── JwtTokenGenerator.cs
│   ├── UserDbContext.cs
│   └── DesignTimeDbContextFactory.cs
└── LibHub.UserService.Api/
    ├── Controllers/
    │   └── UsersController.cs
    ├── Extensions/
    │   └── ConsulServiceRegistration.cs
    ├── Program.cs
    ├── appsettings.json
    └── Dockerfile
```

**Total Files:** 20 files across 4 projects

### **Target State**

**New Structure:**
```
UserService/
├── LibHub.UserService.csproj          ← Single project
├── Program.cs
├── appsettings.json
├── Dockerfile
│
├── Controllers/
│   └── UsersController.cs
│
├── Models/
│   ├── Entities/
│   │   └── User.cs
│   ├── Requests/
│   │   ├── RegisterRequest.cs
│   │   └── LoginRequest.cs
│   └── Responses/
│       ├── UserResponse.cs
│       └── TokenResponse.cs
│
├── Services/
│   ├── UserService.cs
│   └── PasswordValidator.cs
│
├── Data/
│   ├── UserDbContext.cs
│   ├── UserRepository.cs
│   └── DesignTimeDbContextFactory.cs
│
├── Security/
│   ├── PasswordHasher.cs
│   └── JwtTokenGenerator.cs
│
└── Extensions/
    └── ConsulServiceRegistration.cs
```

**Total Files:** 15 files in 1 project

### **API Contract to Preserve**

**Critical Endpoints (MUST NOT CHANGE):**

```http
POST /api/users/register
Content-Type: application/json
{
  "username": "string",
  "email": "string",
  "password": "string"
}
Response 201: {
  "userId": 1,
  "username": "string",
  "email": "string",
  "role": "Customer",
  "createdAt": "2025-11-03T..."
}

POST /api/users/login
Content-Type: application/json
{
  "email": "string",
  "password": "string"
}
Response 200: {
  "accessToken": "eyJ...",
  "expiresIn": 3600
}

GET /api/users/{id}
Authorization: Bearer {token}
Response 200: {
  "userId": 1,
  "username": "string",
  "email": "string",
  "role": "Customer",
  "createdAt": "2025-11-03T..."
}

GET /api/users/me
Authorization: Bearer {token}
Response 200: {
  "userId": 1,
  "username": "string",
  "email": "string",
  "role": "Customer",
  "createdAt": "2025-11-03T..."
}
```

### **Step-by-Step Implementation**

#### **Step 1.1: Create New Project Structure**
```bash
# Create new single project
cd /home/thuannp4/libhub-ver2/src/Services
mkdir UserService-New
cd UserService-New

# Create folder structure
mkdir -p Controllers
mkdir -p Models/Entities
mkdir -p Models/Requests
mkdir -p Models/Responses
mkdir -p Services
mkdir -p Data
mkdir -p Security
mkdir -p Extensions
mkdir -p Properties
```

**Files to Create:**
- `LibHub.UserService.csproj` - Single project file with all dependencies
- Empty folders as above

**Time:** 15 minutes

---

#### **Step 1.2: Create Project File**

**File:** `LibHub.UserService.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.*" />
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.21" />
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.*" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.*" />
    <PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="9.0.*" />
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
    <PackageReference Include="Consul" Version="1.7.14.3" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
  </ItemGroup>
</Project>
```

**Time:** 10 minutes

---

#### **Step 1.3: Migrate Models**

**Priority:** High (Foundation for everything else)

**Files to Create:**

1. **`Models/Entities/User.cs`**
   - Copy from `Domain/User.cs`
   - Keep all validation logic
   - Update namespace to `LibHub.UserService.Models.Entities`
   - **Change:** Remove `IPasswordHasher` interface parameter, use concrete class

2. **`Models/Requests/RegisterRequest.cs`**
   - Copy from `Application/DTOs/RegisterUserDto.cs`
   - Rename class
   - Update namespace to `LibHub.UserService.Models.Requests`

3. **`Models/Requests/LoginRequest.cs`**
   - Copy from `Application/DTOs/LoginDto.cs`
   - Rename class
   - Update namespace

4. **`Models/Responses/UserResponse.cs`**
   - Copy from `Application/DTOs/UserDto.cs`
   - Rename class
   - Update namespace

5. **`Models/Responses/TokenResponse.cs`**
   - Copy from `Application/DTOs/TokenDto.cs`
   - Rename class
   - Update namespace

**Code Changes Required:**

```csharp
// User.cs - Update password verification
// OLD
public bool VerifyPassword(string plainPassword, IPasswordHasher passwordHasher)

// NEW
public bool VerifyPassword(string plainPassword, PasswordHasher passwordHasher)
```

**Testing:**
```bash
dotnet build
# Should compile without errors
```

**Time:** 30 minutes

---

#### **Step 1.4: Migrate Security Classes**

**Files to Create:**

1. **`Security/PasswordHasher.cs`**
   - Copy from `Infrastructure/Security/PasswordHasher.cs`
   - Update namespace to `LibHub.UserService.Security`
   - **No interface** - Use as concrete class

2. **`Security/JwtTokenGenerator.cs`**
   - Copy from `Infrastructure/Security/JwtTokenGenerator.cs`
   - Update namespace
   - **No interface** - Use as concrete class

**Code Example:**

```csharp
// Security/PasswordHasher.cs
namespace LibHub.UserService.Security;

public class PasswordHasher
{
    private const int WorkFactor = 11;

    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty");
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool Verify(string hash, string password)
    {
        if (string.IsNullOrWhiteSpace(hash) || string.IsNullOrWhiteSpace(password))
            return false;
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }
}
```

**Testing:**
```bash
dotnet build
# Should compile without errors
```

**Time:** 20 minutes

---

#### **Step 1.5: Migrate Data Layer**

**Files to Create:**

1. **`Data/UserDbContext.cs`**
   - Copy from `Infrastructure/UserDbContext.cs`
   - Update namespace to `LibHub.UserService.Data`
   - Update entity reference to `Models.Entities.User`

2. **`Data/UserRepository.cs`**
   - Copy from `Infrastructure/Repositories/EfUserRepository.cs`
   - Rename class (remove `Ef` prefix)
   - Update namespace
   - **Remove interface** - Just concrete class

3. **`Data/DesignTimeDbContextFactory.cs`**
   - Copy from `Infrastructure/DesignTimeDbContextFactory.cs`
   - Update namespace
   - Update DbContext reference

**Code Example:**

```csharp
// Data/UserRepository.cs
namespace LibHub.UserService.Data;

using LibHub.UserService.Models.Entities;
using Microsoft.EntityFrameworkCore;

public class UserRepository
{
    private readonly UserDbContext _context;

    public UserRepository(UserDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email.ToLower());
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users
            .AnyAsync(u => u.Email == email.ToLower());
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<List<User>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }
}
```

**Testing:**
```bash
dotnet build
# Should compile without errors
```

**Time:** 30 minutes

---

#### **Step 1.6: Migrate Business Logic**

**Files to Create:**

1. **`Services/UserService.cs`**
   - Copy from `Application/Services/IdentityApplicationService.cs`
   - Rename class
   - Update namespace to `LibHub.UserService.Services`
   - Update all DTO references to new Request/Response models
   - **Remove interface dependencies** - Use concrete classes

2. **`Services/PasswordValidator.cs`**
   - Copy from `Application/Validation/PasswordValidator.cs`
   - Update namespace

**Code Example:**

```csharp
// Services/UserService.cs
namespace LibHub.UserService.Services;

using LibHub.UserService.Models.Entities;
using LibHub.UserService.Models.Requests;
using LibHub.UserService.Models.Responses;
using LibHub.UserService.Data;
using LibHub.UserService.Security;

public class UserService
{
    private readonly UserRepository _repository;
    private readonly PasswordHasher _passwordHasher;
    private readonly JwtTokenGenerator _tokenGenerator;

    public UserService(
        UserRepository repository,
        PasswordHasher passwordHasher,
        JwtTokenGenerator tokenGenerator)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<UserResponse> RegisterAsync(RegisterRequest request)
    {
        // Validate password
        var (isValid, errorMessage) = PasswordValidator.Validate(request.Password);
        if (!isValid)
            throw new ArgumentException(errorMessage);

        // Check if email exists
        if (await _repository.EmailExistsAsync(request.Email))
            throw new InvalidOperationException("Email already exists");

        // Hash password
        var hashedPassword = _passwordHasher.Hash(request.Password);

        // Create user
        var user = new User(request.Username, request.Email, hashedPassword, "Customer");

        // Save to database
        await _repository.AddAsync(user);

        // Return response
        return MapToResponse(user);
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request)
    {
        var user = await _repository.GetByEmailAsync(request.Email);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid email or password");

        if (!user.VerifyPassword(request.Password, _passwordHasher))
            throw new UnauthorizedAccessException("Invalid email or password");

        var token = _tokenGenerator.GenerateToken(user);

        return new TokenResponse
        {
            AccessToken = token,
            ExpiresIn = 3600
        };
    }

    public async Task<UserResponse?> GetByIdAsync(int userId)
    {
        var user = await _repository.GetByIdAsync(userId);
        return user != null ? MapToResponse(user) : null;
    }

    public async Task<UserResponse?> GetByEmailAsync(string email)
    {
        var user = await _repository.GetByEmailAsync(email);
        return user != null ? MapToResponse(user) : null;
    }

    private static UserResponse MapToResponse(User user)
    {
        return new UserResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }
}
```

**Testing:**
```bash
dotnet build
# Should compile without errors
```

**Time:** 45 minutes

---

#### **Step 1.7: Migrate Controller**

**File to Create:** `Controllers/UsersController.cs`

- Copy from `Api/Controllers/UsersController.cs`
- Update namespace to `LibHub.UserService.Controllers`
- Update service reference from `IdentityApplicationService` to `UserService`
- Update DTO references to Request/Response models
- **Keep exact same endpoints and response formats**

**Code Example:**

```csharp
// Controllers/UsersController.cs
namespace LibHub.UserService.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LibHub.UserService.Models.Requests;
using LibHub.UserService.Models.Responses;
using LibHub.UserService.Services;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(UserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var user = await _userService.RegisterAsync(request);
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

    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var token = await _userService.LoginAsync(request);
            _logger.LogInformation("User logged in: {Email}", request.Email);
            
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

    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        
        if (user == null)
            return NotFound(new { message = "User not found" });
        
        return Ok(user);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
            return Unauthorized();

        var userId = int.Parse(userIdClaim.Value);
        var user = await _userService.GetByIdAsync(userId);
        
        if (user == null)
            return NotFound();
        
        return Ok(user);
    }
}
```

**Testing:**
```bash
dotnet build
# Should compile without errors
```

**Time:** 30 minutes

---

#### **Step 1.8: Create Program.cs**

**File to Create:** `Program.cs`

- Copy from `Api/Program.cs`
- Update namespace references
- **Simplify DI registration** - Remove interface registrations
- Keep JWT configuration
- Keep Swagger configuration
- Keep Consul registration

**Code Example:**

```csharp
// Program.cs
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using LibHub.UserService.Data;
using LibHub.UserService.Security;
using LibHub.UserService.Services;
using LibHub.UserService.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger configuration
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "UserService API", 
        Version = "v1", 
        Description = "LibHub User Management Service" 
    });
    
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

// Database configuration
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] 
    ?? throw new InvalidOperationException("JWT Key not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Register application services (SIMPLIFIED - No interfaces!)
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<PasswordHasher>();
builder.Services.AddScoped<JwtTokenGenerator>();
builder.Services.AddScoped<UserService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader());
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Register with Consul
app.RegisterWithConsul(app.Lifetime, builder.Configuration);

app.Run();
```

**Testing:**
```bash
dotnet build
# Should compile without errors
```

**Time:** 30 minutes

---

#### **Step 1.9: Copy Supporting Files**

**Files to Copy:**

1. **`appsettings.json`**
   - Copy from `Api/appsettings.json`
   - Keep all settings unchanged

2. **`Extensions/ConsulServiceRegistration.cs`**
   - Copy from `Api/Extensions/ConsulServiceRegistration.cs`
   - Update namespace

3. **`Properties/launchSettings.json`** (if exists)
   - Copy from `Api/Properties/`

**Time:** 15 minutes

---

#### **Step 1.10: Update Dockerfile**

**File to Create:** `Dockerfile`

**New Simplified Version:**

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy single project file
COPY ["src/Services/UserService/LibHub.UserService.csproj", "src/Services/UserService/"]
RUN dotnet restore "src/Services/UserService/LibHub.UserService.csproj"

# Copy all source files
COPY src/Services/UserService/ src/Services/UserService/

# Build
WORKDIR "/src/src/Services/UserService"
RUN dotnet build "LibHub.UserService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LibHub.UserService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LibHub.UserService.dll"]
```

**Testing:**
```bash
docker build -t userservice-test .
# Should build successfully
```

**Time:** 20 minutes

---

#### **Step 1.11: Test Build and Functionality**

**Build Test:**
```bash
cd /home/thuannp4/libhub-ver2/src/Services/UserService-New
dotnet build
# Expected: Build succeeded. 0 Error(s)
```

**Run Test (without Docker first):**
```bash
dotnet run
# Expected: Service starts on port 5001
# Check: http://localhost:5001/swagger
```

**API Tests:**
```bash
# Test registration
curl -X POST http://localhost:5001/api/users/register \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","email":"test@example.com","password":"Test123!@#"}'

# Expected: 201 Created with user data

# Test login
curl -X POST http://localhost:5001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!@#"}'

# Expected: 200 OK with JWT token
```

**Time:** 30 minutes

---

#### **Step 1.12: Replace Old Service**

**After all tests pass:**

```bash
# Rename old service
cd /home/thuannp4/libhub-ver2/src/Services
mv UserService UserService-Old

# Rename new service
mv UserService-New UserService

# Update docker-compose.yml if service name changed
# (Usually no change needed if folder name stays the same)
```

**Time:** 10 minutes

---

#### **Step 1.13: Test in Docker**

```bash
# Build and start service
cd /home/thuannp4/libhub-ver2
docker compose up -d userservice

# Check logs
docker compose logs -f userservice

# Wait for service to register with Consul
sleep 30

# Test endpoints
./scripts/test-gateway-integration.sh
```

**Expected Results:**
- Service builds successfully
- Service starts without errors
- Registers with Consul
- All API endpoints work
- Frontend can login/register

**Time:** 30 minutes

---

#### **Step 1.14: Commit Changes**

```bash
git add src/Services/UserService/
git add docs/refactoring/
git commit -m "refactor(UserService): Simplify from 4 projects to 1

- Merged Domain, Application, Infrastructure, Api into single project
- Removed unnecessary interfaces (IUserRepository, IPasswordHasher, IJwtTokenGenerator)
- Renamed DTOs to Request/Response pattern
- Simplified dependency injection
- Maintained 100% API compatibility
- Reduced files from 20 to 15
- Simplified Dockerfile

BREAKING CHANGES: None
Frontend compatibility: 100%"

# Push to feature branch
git push origin refactor/simplify-architecture
```

**Time:** 15 minutes

---

### **Phase 1 Summary**

**Total Time:** ~5-6 hours  
**Files Reduced:** 20 → 15 (25% reduction)  
**Projects:** 4 → 1 (75% reduction)  
**Lines of Code:** ~2000 → ~1500 (25% reduction)  
**Breaking Changes:** 0  
**API Compatibility:** 100%

---

## 🔧 Phase 2: CatalogService Refactoring

**Status:** 🔜 After UserService  
**Complexity:** ⭐⭐☆☆☆ (Simple)  
**Estimated Time:** 4-6 hours

### **Similar to UserService**

Follow the same steps as Phase 1, but for CatalogService:

1. Create new single project structure
2. Migrate models (Book entity, request/response DTOs)
3. Migrate data layer (BookRepository, CatalogDbContext)
4. Migrate business logic (BookService)
5. Migrate controller (BooksController)
6. Create Program.cs with simplified DI
7. Update Dockerfile
8. Test thoroughly
9. Replace old service
10. Commit changes

### **Key Differences**

- Entity is `Book` instead of `User`
- No password hashing or JWT generation
- Simpler business logic (CRUD operations)
- May have book availability checking

### **API Contracts to Preserve**

```http
GET /api/books - List all books
GET /api/books/{id} - Get book details
POST /api/books - Create book (Admin only)
PUT /api/books/{id} - Update book (Admin only)
DELETE /api/books/{id} - Delete book (Admin only)
GET /api/books/search?query={query} - Search books
```

---

## 🔧 Phase 3: LoanService Refactoring

**Status:** 🔜 After CatalogService  
**Complexity:** ⭐⭐⭐⭐☆ (Complex)  
**Estimated Time:** 6-8 hours

### **Why More Complex?**

LoanService has:
- **Saga Pattern** for distributed transactions
- **Inter-service communication** with UserService and CatalogService
- **HTTP clients** for calling other services
- **Compensation logic** for rollback
- **More complex business rules**

### **Special Considerations**

**Keep Some Interfaces:**
- `ISagaOrchestrator` - Needed for testing complex saga logic
- `IUserServiceClient` - Needed for mocking external service
- `ICatalogServiceClient` - Needed for mocking external service

**Rationale:** Complex business logic and external dependencies benefit from interfaces for unit testing.

### **Structure After Refactoring**

```
LoanService/
├── LibHub.LoanService.csproj
├── Program.cs
│
├── Controllers/
│   └── LoansController.cs
│
├── Models/
│   ├── Entities/
│   │   └── Loan.cs
│   ├── Requests/
│   └── Responses/
│
├── Services/
│   ├── LoanService.cs
│   └── Saga/
│       ├── ISagaOrchestrator.cs          ← Keep interface
│       ├── LoanSagaOrchestrator.cs
│       └── SagaStep.cs
│
├── Data/
│   ├── LoanDbContext.cs
│   └── LoanRepository.cs
│
├── Clients/                              ← HTTP clients
│   ├── IUserServiceClient.cs             ← Keep interface
│   ├── UserServiceClient.cs
│   ├── ICatalogServiceClient.cs          ← Keep interface
│   └── CatalogServiceClient.cs
│
└── Extensions/
    └── ConsulServiceRegistration.cs
```

### **API Contracts to Preserve**

```http
POST /api/loans/borrow - Borrow a book
POST /api/loans/return - Return a book
GET /api/loans - List all loans
GET /api/loans/user/{userId} - Get user's loans
GET /api/loans/{id} - Get loan details
```

### **Saga Pattern Preservation**

Must maintain the saga steps:
1. Validate user exists
2. Validate book exists and available
3. Create loan record
4. Update book availability
5. If any step fails, rollback previous steps

---

## 🔧 Phase 4: Gateway Refactoring

**Status:** 🔜 After LoanService  
**Complexity:** ⭐☆☆☆☆ (Minimal)  
**Estimated Time:** 1-2 hours

### **Why Minimal?**

Gateway (Ocelot) is already simple:
- Single project
- Just configuration files
- No complex business logic
- Mostly routing and JWT validation

### **Possible Improvements**

1. Clean up `ocelot.json` configuration
2. Add better error handling
3. Improve logging
4. Update documentation

### **Critical Rule**

**DO NOT CHANGE ROUTES!** Frontend depends on exact paths.

---

## 🔧 Phase 5: Integration Testing

**Status:** 🔜 Final Phase  
**Complexity:** ⭐⭐⭐☆☆ (Medium)  
**Estimated Time:** 2-3 hours

### **Complete System Test**

**Docker Test:**
```bash
# Stop all services
docker compose down

# Rebuild all services
docker compose build

# Start all services
docker compose up -d

# Wait for services to start
sleep 60

# Check all services are running
docker compose ps

# Check Consul registration
curl http://localhost:8500/v1/catalog/services

# Test all endpoints
./scripts/test-docker-containers.sh
./scripts/test-consul-discovery.sh
./scripts/test-gateway-integration.sh
```

### **Frontend Integration Test**

**Manual Testing Checklist:**

1. **Open:** http://localhost:8080
2. **Test Registration:**
   - Fill registration form
   - Submit
   - ✅ Should succeed with redirect to login
3. **Test Login:**
   - Enter credentials
   - Submit
   - ✅ Should succeed with redirect to home
4. **Test Browse Books:**
   - View book list
   - ✅ Should display all books
5. **Test Book Details:**
   - Click on a book
   - ✅ Should show book details with borrow button
6. **Test Borrow Book:**
   - Click "Borrow"
   - ✅ Should succeed with confirmation
7. **Test My Loans:**
   - Navigate to "My Loans"
   - ✅ Should show borrowed books
8. **Test Return Book:**
   - Click "Return"
   - ✅ Should succeed with confirmation

### **Load Testing** (Optional)

```bash
# Simple load test
for i in {1..100}; do
  curl -X POST http://localhost:5000/api/users/login \
    -H "Content-Type: application/json" \
    -d '{"email":"test@example.com","password":"Test123!@#"}' &
done
wait

# Check: All requests should succeed
# Check: Services should handle load without errors
```

### **Log Analysis**

```bash
# Check for errors in logs
docker compose logs | grep -i error
docker compose logs | grep -i exception

# Expected: No errors related to refactoring
```

---

## 📊 Final Validation Checklist

Before merging to main:

### **Build & Deploy**
- [ ] All services build without errors
- [ ] All services start in Docker
- [ ] No errors in service logs
- [ ] All services register with Consul

### **API Compatibility**
- [ ] All UserService endpoints work
- [ ] All CatalogService endpoints work
- [ ] All LoanService endpoints work
- [ ] Gateway routes correctly
- [ ] JWT authentication works

### **Frontend Integration**
- [ ] User registration works
- [ ] User login works
- [ ] Browse books works
- [ ] Book details display correctly
- [ ] Borrow book works
- [ ] Return book works
- [ ] My loans page works
- [ ] No console errors in browser

### **Database**
- [ ] Migrations work
- [ ] Seed data loads correctly
- [ ] Data persists across restarts

### **Documentation**
- [ ] README updated
- [ ] Architecture docs updated
- [ ] Deployment guides updated
- [ ] Change log created

---

## 🚀 Deployment to Production

### **Merge Strategy**

```bash
# 1. Ensure all tests pass
./scripts/test-gateway-integration.sh

# 2. Merge to main
git checkout main
git pull origin main
git merge refactor/simplify-architecture

# 3. Resolve any conflicts

# 4. Final test on main branch
docker compose down
docker compose build
docker compose up -d
# Wait and test

# 5. Push to GitHub
git push origin main

# 6. Deploy to production server
# SSH to server
ssh user@production-server

# Pull latest changes
cd /path/to/libhub-ver2
git pull origin main

# Rebuild and restart
docker compose down
docker compose build
docker compose up -d

# Monitor logs
docker compose logs -f
```

### **Rollback Plan**

If something goes wrong:

```bash
# 1. Revert to previous commit
git revert HEAD

# 2. Or reset to previous tag
git reset --hard <previous-commit-hash>

# 3. Force push (only on feature branch!)
git push origin refactor/simplify-architecture --force

# 4. Rebuild
docker compose down
docker compose build
docker compose up -d
```

---

## 📈 Success Metrics

After completion, measure:

### **Quantitative**
- **Projects:** 12 → 3 (75% reduction)
- **Files:** ~80 → ~50 (37% reduction)
- **Lines of Code:** ~8,000 → ~6,000 (25% reduction)
- **Build Time:** Reduced by ~30%
- **Docker Image Size:** Similar or smaller

### **Qualitative**
- **Easier Navigation:** Find code faster
- **Simpler Dependencies:** Fewer project references
- **Better Maintainability:** Junior devs understand structure
- **Reduced Complexity:** Less over-engineering
- **Standard Patterns:** More familiar to ASP.NET developers

---

## 🎓 Lessons Learned (To be updated after completion)

*Document challenges, solutions, and best practices discovered during refactoring*

---

**Last Updated:** November 3, 2025  
**Next Review:** After Phase 1 completion  
**Maintained By:** Development Team
