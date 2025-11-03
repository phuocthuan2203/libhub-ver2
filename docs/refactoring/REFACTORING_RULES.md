# LibHub Microservices Refactoring Rules

> **Version:** 1.0  
> **Date:** November 3, 2025  
> **Branch:** `refactor/simplify-architecture`  
> **Status:** Production-Safe Refactoring Guidelines

---

## ⚠️ CRITICAL PRODUCTION RULES

### 🔒 **Rule 0: Never Break the Contract**

**THE GOLDEN RULE:** Any refactoring MUST maintain 100% API compatibility with the frontend.

```
✅ ALLOWED: Change internal implementation
✅ ALLOWED: Reorganize folders and files
✅ ALLOWED: Simplify dependencies
✅ ALLOWED: Rename internal classes/methods

❌ FORBIDDEN: Change API endpoint URLs
❌ FORBIDDEN: Change request/response JSON structure
❌ FORBIDDEN: Change HTTP status codes
❌ FORBIDDEN: Remove or rename DTO properties used by frontend
❌ FORBIDDEN: Change authentication mechanism
```

### 📋 **API Contract Preservation Checklist**

Before any refactoring, document these and ensure they remain unchanged:

1. **Endpoint URLs** - Exact paths must remain identical
2. **HTTP Methods** - GET/POST/PUT/DELETE must stay the same
3. **Request Bodies** - JSON property names and types
4. **Response Bodies** - JSON property names and types
5. **Status Codes** - Success (200, 201) and error codes (400, 401, 404, 500)
6. **Headers** - Authorization, Content-Type requirements
7. **Query Parameters** - Names and expected values

### 🧪 **Testing Requirements**

After each service refactoring:

```bash
# 1. Service must build successfully
dotnet build

# 2. Service must start without errors
docker compose up -d [service-name]

# 3. All existing endpoints must respond
./scripts/test-gateway-integration.sh

# 4. Frontend must work without changes
# Open http://localhost:8080 and test all features:
# - User registration
# - User login
# - Browse books
# - Borrow books
# - Return books
# - View loan history
```

---

## 🎯 SOLID Principles Application

### **S - Single Responsibility Principle**

**Rule:** Each class should have ONE reason to change.

**Application in Refactoring:**

```csharp
// ❌ BAD: God class with multiple responsibilities
public class UserService 
{
    public void RegisterUser() { }
    public void LoginUser() { }
    public void HashPassword() { }      // Different responsibility
    public void GenerateToken() { }     // Different responsibility
    public void SendEmail() { }         // Different responsibility
}

// ✅ GOOD: Separated concerns
public class UserService           // User business logic
{
    public void RegisterUser() { }
    public void LoginUser() { }
}

public class PasswordHasher        // Password security
{
    public string Hash() { }
    public bool Verify() { }
}

public class JwtTokenGenerator     // Token generation
{
    public string GenerateToken() { }
}
```

**When to Apply:**
- Keep authentication logic separate from authorization
- Separate business logic from data access
- Extract validation into separate validators
- Keep security concerns in Security/ folder

---

### **O - Open/Closed Principle**

**Rule:** Open for extension, closed for modification.

**Application in Refactoring:**

```csharp
// ❌ BAD: Must modify class to add new validation
public class PasswordValidator
{
    public bool Validate(string password)
    {
        return password.Length >= 8 
            && HasUpperCase(password)
            && HasNumber(password);
        // Adding new rule requires modifying this method
    }
}

// ✅ GOOD: Configuration-based (if needed)
public class PasswordValidator
{
    private readonly PasswordRules _rules;
    
    public (bool isValid, string error) Validate(string password)
    {
        if (password.Length < _rules.MinLength)
            return (false, $"Password must be at least {_rules.MinLength} characters");
        
        if (_rules.RequireUppercase && !HasUpperCase(password))
            return (false, "Password must contain uppercase letter");
            
        return (true, string.Empty);
    }
}
```

**When to Apply:**
- Use configuration for validation rules
- Keep business rules data-driven when possible
- But DON'T over-engineer for simple services

---

### **L - Liskov Substitution Principle**

**Rule:** Derived classes must be substitutable for base classes.

**Application in Refactoring:**

```csharp
// For simple microservices, avoid inheritance hierarchies
// Prefer composition over inheritance

// ❌ AVOID: Complex inheritance
public abstract class BaseService { }
public class UserService : BaseService { }

// ✅ PREFER: Simple classes with composition
public class UserService
{
    private readonly UserRepository _repository;
    private readonly PasswordHasher _hasher;
    // Use composition
}
```

**When to Apply:**
- Generally avoid inheritance in simple microservices
- Use interfaces only when you need multiple implementations
- Prefer composition for dependency injection

---

### **I - Interface Segregation Principle**

**Rule:** Don't force clients to depend on methods they don't use.

**Application in Refactoring:**

```csharp
// ❌ BAD: Fat interface with many methods
public interface IUserRepository
{
    Task<User> GetById(int id);
    Task<User> GetByEmail(string email);
    Task<User> GetByUsername(string username);
    Task<List<User>> GetAll();
    Task<List<User>> GetActive();
    Task<List<User>> GetInactive();
    Task<List<User>> GetByRole(string role);
    // Controller only needs 2 methods but depends on all
}

// ✅ GOOD: For simple services, NO INTERFACE at all
public class UserRepository
{
    // Just concrete methods you actually need
    public async Task<User?> GetByIdAsync(int id) { }
    public async Task<User?> GetByEmailAsync(string email) { }
    public async Task AddAsync(User user) { }
}
```

**When to Apply:**
- **Simple services:** Skip interfaces, use concrete classes
- **Complex services:** Create small, focused interfaces
- Only add interfaces when you have multiple implementations

---

### **D - Dependency Inversion Principle**

**Rule:** Depend on abstractions, not concretions.

**Application in Refactoring:**

**For Simple Services (UserService, CatalogService):**
```csharp
// ✅ GOOD: Direct dependencies (simple)
public class UserService
{
    private readonly UserRepository _repository;
    private readonly PasswordHasher _hasher;
    
    public UserService(UserRepository repository, PasswordHasher hasher)
    {
        _repository = repository;
        _hasher = hasher;
    }
}

// DI Registration
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<PasswordHasher>();
builder.Services.AddScoped<UserService>();
```

**For Complex Services (LoanService with Saga):**
```csharp
// ✅ GOOD: Interface for testability (complex logic)
public interface ISagaOrchestrator
{
    Task<SagaResult> ExecuteAsync(LoanRequest request);
}

public class LoanService
{
    private readonly ISagaOrchestrator _saga;
    
    public LoanService(ISagaOrchestrator saga)
    {
        _saga = saga;  // Need interface for testing complex saga
    }
}
```

**When to Apply:**
- Simple CRUD? Use concrete classes
- Complex business logic? Use interfaces
- External dependencies (HTTP, message queues)? Use interfaces

---

## 📂 Folder Structure Standards

### **Standard Service Structure** (After Refactoring)

```
ServiceName/
├── LibHub.ServiceName.csproj       ← Single project file
├── Program.cs                       ← Startup & DI configuration
├── appsettings.json
├── Dockerfile
│
├── Controllers/                     ← HTTP endpoints
│   ├── MainController.cs
│   └── HealthController.cs
│
├── Models/                          ← All data models
│   ├── Entities/                    ← Database entities
│   │   └── EntityName.cs
│   ├── Requests/                    ← API request DTOs
│   │   ├── CreateRequest.cs
│   │   └── UpdateRequest.cs
│   └── Responses/                   ← API response DTOs
│       └── EntityResponse.cs
│
├── Services/                        ← Business logic
│   ├── MainService.cs               ← Primary service
│   ├── ValidationService.cs         ← Validation logic
│   └── Helpers/                     ← Helper classes
│
├── Data/                            ← Database layer
│   ├── AppDbContext.cs              ← EF Core DbContext
│   └── Repositories/                ← Data access (if complex)
│       └── EntityRepository.cs
│
├── Security/                        ← Auth & security (if needed)
│   ├── PasswordHasher.cs
│   └── TokenGenerator.cs
│
└── Extensions/                      ← Extension methods
    ├── ServiceRegistration.cs
    └── ConsulRegistration.cs
```

### **Naming Conventions**

| Type | Pattern | Example |
|------|---------|---------|
| **Entities** | Singular noun | `User`, `Book`, `Loan` |
| **Request DTOs** | `{Action}Request` | `RegisterRequest`, `CreateBookRequest` |
| **Response DTOs** | `{Entity}Response` | `UserResponse`, `BookResponse` |
| **Services** | `{Entity}Service` | `UserService`, `BookService` |
| **Repositories** | `{Entity}Repository` | `UserRepository`, `BookRepository` |
| **Controllers** | `{Entity}Controller` | `UsersController`, `BooksController` |
| **Validators** | `{Subject}Validator` | `PasswordValidator`, `EmailValidator` |

---

## 🔧 Code Quality Standards

### **1. Method Length**
```csharp
// ✅ GOOD: Short, focused methods (max 20-30 lines)
public async Task<UserResponse> RegisterAsync(RegisterRequest request)
{
    ValidateRequest(request);
    
    if (await _repository.EmailExistsAsync(request.Email))
        throw new InvalidOperationException("Email already exists");
    
    var hashedPassword = _hasher.Hash(request.Password);
    var user = new User(request.Username, request.Email, hashedPassword);
    
    await _repository.AddAsync(user);
    
    return MapToResponse(user);
}

// ❌ BAD: Long method doing too many things (100+ lines)
```

### **2. Error Handling**
```csharp
// ✅ GOOD: Specific exception handling
[HttpPost("register")]
public async Task<IActionResult> Register([FromBody] RegisterRequest request)
{
    try
    {
        var result = await _userService.RegisterAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = result.UserId }, result);
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Conflict(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Registration failed");
        return StatusCode(500, new { message = "An error occurred" });
    }
}

// ❌ BAD: Catching all exceptions the same way
catch (Exception ex) { return BadRequest(ex.Message); }
```

### **3. Async/Await**
```csharp
// ✅ GOOD: Consistent async all the way
public async Task<User?> GetByIdAsync(int id)
{
    return await _context.Users.FindAsync(id);
}

// ❌ BAD: Mixing sync and async
public User GetById(int id)
{
    return _context.Users.FindAsync(id).Result;  // Deadlock risk!
}
```

### **4. Null Handling**
```csharp
// ✅ GOOD: Nullable reference types
public async Task<UserResponse?> GetByIdAsync(int id)
{
    var user = await _repository.GetByIdAsync(id);
    return user != null ? MapToResponse(user) : null;
}

// Controller
var user = await _service.GetByIdAsync(id);
if (user == null)
    return NotFound();
return Ok(user);
```

### **5. Dependency Injection**
```csharp
// ✅ GOOD: Constructor injection
public class UserService
{
    private readonly UserRepository _repository;
    private readonly ILogger<UserService> _logger;
    
    public UserService(UserRepository repository, ILogger<UserService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
}

// ❌ BAD: Service locator pattern
public class UserService
{
    public void DoSomething()
    {
        var repo = ServiceLocator.Get<UserRepository>();  // Anti-pattern!
    }
}
```

---

## 🚫 Anti-Patterns to Avoid

### **1. God Classes**
```csharp
// ❌ BAD: One class doing everything
public class UserService
{
    public void Register() { }
    public void Login() { }
    public void ResetPassword() { }
    public void SendEmail() { }
    public void LogActivity() { }
    public void GenerateReport() { }
    // ... 50 more methods
}
```

### **2. Primitive Obsession**
```csharp
// ❌ BAD: Using primitives everywhere
public void CreateUser(string email, string password, string role) { }

// ✅ BETTER: Use request objects
public void CreateUser(RegisterRequest request) { }
```

### **3. Magic Strings/Numbers**
```csharp
// ❌ BAD: Magic values
if (user.Role == "admin") { }
var token = GenerateToken(user, 3600);

// ✅ GOOD: Named constants
public static class Roles
{
    public const string Admin = "Admin";
    public const string Customer = "Customer";
}

public static class JwtSettings
{
    public const int TokenExpirySeconds = 3600;
}

if (user.Role == Roles.Admin) { }
var token = GenerateToken(user, JwtSettings.TokenExpirySeconds);
```

### **4. Leaky Abstractions**
```csharp
// ❌ BAD: Exposing implementation details
public class UserService
{
    public DbSet<User> Users { get; set; }  // Exposing EF Core!
}

// ✅ GOOD: Hide implementation
public class UserService
{
    private readonly UserRepository _repository;
    
    public async Task<User?> GetByIdAsync(int id)
    {
        return await _repository.GetByIdAsync(id);
    }
}
```

---

## 🔄 Refactoring Process

### **Phase 1: Preparation**
1. ✅ Create feature branch
2. ✅ Document current API contracts
3. ✅ List all frontend dependencies
4. ✅ Backup current working state

### **Phase 2: Structural Changes**
1. Create new simplified folder structure
2. Copy files to new locations
3. Update namespaces
4. Update references

### **Phase 3: Code Simplification**
1. Remove unnecessary interfaces
2. Rename classes for clarity
3. Simplify dependency injection
4. Remove unused code

### **Phase 4: Validation**
1. Build service successfully
2. Run service in Docker
3. Test all API endpoints
4. Verify frontend still works
5. Check logs for errors

### **Phase 5: Documentation**
1. Update README if needed
2. Document breaking changes (if any)
3. Update architecture docs

### **Phase 6: Cleanup**
1. Remove old project folders
2. Update Dockerfile
3. Update docker-compose.yml
4. Commit changes

---

## ✅ Definition of Done

A service refactoring is complete when:

- [ ] Service builds without errors
- [ ] Service starts successfully in Docker
- [ ] All API endpoints return expected responses
- [ ] Frontend works without any modifications
- [ ] No new errors in logs
- [ ] Code follows naming conventions
- [ ] Folder structure matches standard
- [ ] Dockerfile is simplified
- [ ] docker-compose.yml is updated
- [ ] Changes are committed to feature branch
- [ ] Documentation is updated

---

## 🎯 Refactoring Priorities

### **Priority 1: Simple Services** (Do First)
- ✅ **UserService** - Simple CRUD, no dependencies
- ✅ **CatalogService** - Simple CRUD, no dependencies

**Why First:** Easiest to refactor, quick wins, test the process

### **Priority 2: Complex Service** (Do Second)
- 🔶 **LoanService** - Has Saga pattern, inter-service communication

**Why Second:** More complex, needs careful testing, may keep some interfaces

### **Priority 3: Gateway** (Do Last)
- 🔶 **API Gateway (Ocelot)** - Already simple, minimal changes

**Why Last:** Most critical, don't break if everything else works

---

## 📊 Success Metrics

After refactoring, measure:

1. **Lines of Code Reduced** - Target: 20-30% reduction
2. **Number of Projects** - Before: 4 per service, After: 1 per service
3. **Build Time** - Should be faster
4. **Complexity** - Fewer abstractions, easier navigation
5. **Maintainability** - Junior dev can understand in < 30 minutes

---

## 🛡️ Safety Checklist

Before merging to main:

- [ ] All services build successfully
- [ ] All services start in Docker
- [ ] Frontend login works
- [ ] Frontend can browse books
- [ ] Frontend can borrow books
- [ ] Frontend can return books
- [ ] Frontend can view loan history
- [ ] Consul service discovery works
- [ ] API Gateway routes correctly
- [ ] JWT authentication works
- [ ] No errors in service logs
- [ ] Database migrations work
- [ ] Seed data loads correctly

---

## 📝 Notes for AI Agents

When refactoring:

1. **Always read existing code first** before making changes
2. **Preserve API contracts** - Check controller endpoints carefully
3. **Update all references** - Don't leave broken imports
4. **Test incrementally** - Build after each major change
5. **Keep commits focused** - One service per commit
6. **Document breaking changes** - Even if none, say "No breaking changes"
7. **When in doubt, ask** - Better to clarify than break production

---

**Last Updated:** November 3, 2025  
**Maintained By:** Development Team  
**Review Frequency:** After each service refactoring
