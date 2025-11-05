# Phase 3: Enhanced Event Logging - Key Operations Visibility

## Goal
Add rich, detailed logging for critical system events with emojis and structured data to make important operations easily identifiable. Focus on: Consul registration, JWT validation, Saga orchestration, and inter-service communication.

## Success Criteria
- [ ] Consul service registration logs clearly show success/failure/retry
- [ ] Health check endpoint logs every incoming request from Consul
- [ ] Gateway logs show Consul service discovery queries
- [ ] JWT validation events are logged with user context (success/failure)
- [ ] Saga orchestration steps are logged with clear stage indicators
- [ ] Inter-service HTTP calls are logged with timing information
- [ ] All critical logs use emojis for quick visual scanning
- [ ] All logs include relevant structured properties (UserId, BookId, ServiceName, etc.)

## Estimated Time
2-3 hours

---

## Implementation Steps

### Step 1: Enhanced Consul Registration Logging

**Location:** 
- `src/Services/UserService/Extensions/ConsulServiceRegistration.cs`
- `src/Services/CatalogService/Extensions/ConsulServiceRegistration.cs`
- `src/Services/LoanService/Extensions/ConsulServiceRegistration.cs`

**Current logs exist but can be enhanced with:**
- Emojis for visual identification
- More structured properties
- Better retry attempt visibility

#### Update RegisterWithRetryAsync Method

```csharp
private static async Task RegisterWithRetryAsync(
    IConsulClient consulClient,
    AgentServiceRegistration registration,
    ILogger logger,
    string serviceId,
    string serviceName)
{
    for (int attempt = 0; attempt < RetryDelays.Length; attempt++)
    {
        try
        {
            logger.LogInformation(
                "🔌 [CONSUL-REGISTER] Service: {ServiceName} | ID: {ServiceId} | Address: {Address}:{Port} | Attempt: {Attempt}/{MaxAttempts}",
                serviceName, serviceId, registration.Address, registration.Port, attempt + 1, RetryDelays.Length);

            await consulClient.Agent.ServiceRegister(registration);
            
            logger.LogInformation(
                "✅ [CONSUL-SUCCESS] Service {ServiceName} registered | ID: {ServiceId} | Health: {HealthUrl}",
                serviceName, serviceId, registration.Check.HTTP);
            
            return; // Success!
        }
        catch (Exception ex)
        {
            var isLastAttempt = attempt == RetryDelays.Length - 1;
            
            if (isLastAttempt)
            {
                logger.LogError(ex,
                    "❌ [CONSUL-FAILED] Service {ServiceName} registration FAILED after {MaxAttempts} attempts",
                    serviceName, RetryDelays.Length);
                return;
            }
            
            var nextDelay = RetryDelays[attempt];
            logger.LogWarning(ex,
                "⚠️ [CONSUL-RETRY] Service {ServiceName} registration failed | Attempt: {Attempt}/{MaxAttempts} | Retrying in {DelaySeconds}s",
                serviceName, attempt + 1, RetryDelays.Length, nextDelay.TotalSeconds);
            
            await Task.Delay(nextDelay);
        }
    }
}
```

#### Update Deregistration Logging

```csharp
lifetime.ApplicationStopping.Register(() =>
{
    try
    {
        logger.LogInformation("🔌 [CONSUL-DEREGISTER] Removing service {ServiceId}", serviceId);
        consulClient.Agent.ServiceDeregister(serviceId).Wait(TimeSpan.FromSeconds(5));
        logger.LogInformation("✅ [CONSUL-DEREGISTER-SUCCESS] Service {ServiceId} removed", serviceId);
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "⚠️ [CONSUL-DEREGISTER-FAILED] Failed to remove service {ServiceId}", serviceId);
    }
});
```

**Apply to all 3 services:** UserService, CatalogService, LoanService

---

### Step 1b: Health Check Endpoint Logging

**Location:** 
- `src/Services/UserService/Program.cs`
- `src/Services/CatalogService/Program.cs`
- `src/Services/LoanService/Program.cs`

**Add a custom health check endpoint with logging to track Consul's health check calls:**

```csharp
// In Program.cs, BEFORE app.MapHealthChecks("/health")
// Replace the simple MapHealthChecks with a custom endpoint

app.MapGet("/health", (ILogger<Program> logger) =>
{
    // Log health check requests (useful to see Consul polling)
    // Use Debug level to avoid too much noise, or Information if you want to see it
    logger.LogDebug("💓 [HEALTH-CHECK] Health check called by Consul");
    
    return Results.Ok(new 
    { 
        status = "healthy",
        service = "CatalogService", // Change per service
        timestamp = DateTime.UtcNow
    });
});

// Remove or comment out the old:
// app.MapHealthChecks("/health");
```

**Alternative: Use middleware to log ALL requests to /health**

Create a custom middleware (more flexible):

**Create:** `Middlewares/HealthCheckLoggingMiddleware.cs` (in each service)

```csharp
namespace LibHub.CatalogService.Middleware; // Change namespace per service

public class HealthCheckLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HealthCheckLoggingMiddleware> _logger;

    public HealthCheckLoggingMiddleware(RequestDelegate next, ILogger<HealthCheckLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only log if it's a health check request
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            
            // Log at Debug level (less noise) or Information level
            _logger.LogDebug(
                "💓 [HEALTH-CHECK] Request from {ClientIp} | Path: {Path}",
                clientIp, context.Request.Path);
        }

        await _next(context);
    }
}

public static class HealthCheckLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseHealthCheckLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<HealthCheckLoggingMiddleware>();
    }
}
```

**Register in Program.cs:**

```csharp
app.UseHealthCheckLogging(); // Add before app.MapHealthChecks()
app.MapHealthChecks("/health");
```

**Note:** These logs will only appear **while the service is running**. When service is stopped, you won't see the failed health check attempts (because the service isn't running to log them).

---

### Step 2: Gateway JWT Validation Logging

**Location:** `src/Gateway/LibHub.Gateway.Api/Program.cs`

**Find the JWT authentication configuration and enhance the events:**

```csharp
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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!))
        };
        
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var userId = context.Principal?.FindFirst("sub")?.Value ?? "unknown";
                var role = context.Principal?.FindFirst("role")?.Value ?? "unknown";
                var path = context.HttpContext.Request.Path;
                var method = context.HttpContext.Request.Method;
                
                logger.LogInformation(
                    "✅ [JWT-SUCCESS] User authenticated | UserId: {UserId} | Role: {Role} | {Method} {Path}",
                    userId, role, method, path);
                
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var path = context.HttpContext.Request.Path;
                var method = context.HttpContext.Request.Method;
                var reason = context.Exception.Message;
                
                logger.LogWarning(
                    "❌ [JWT-FAILED] Authentication failed | Reason: {Reason} | {Method} {Path}",
                    reason, method, path);
                
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var path = context.HttpContext.Request.Path;
                var method = context.HttpContext.Request.Method;
                
                logger.LogWarning(
                    "⚠️ [JWT-CHALLENGE] Token required but missing/invalid | {Method} {Path}",
                    method, path);
                
                return Task.CompletedTask;
            }
        };
    });
```

---

### Step 3: Enhanced Saga Orchestration Logging

**Location:** `src/Services/LoanService/Services/LoanService.cs`

**Update BorrowBookAsync method with detailed saga logging:**

```csharp
public async Task<LoanResponse> BorrowBookAsync(int userId, BorrowBookRequest request)
{
    _logger.LogInformation(
        "🚀 [SAGA-START] BorrowBook | UserId: {UserId} | BookId: {BookId}", 
        userId, request.BookId);

    // Check loan limit
    var activeLoansCount = await _loanRepository.CountActiveLoansForUserAsync(userId);
    if (activeLoansCount >= 5)
    {
        _logger.LogWarning(
            "❌ [SAGA-ABORT] Max loan limit reached | UserId: {UserId} | ActiveLoans: {ActiveLoans}/5",
            userId, activeLoansCount);
        throw new InvalidOperationException("Maximum loan limit reached (5 active loans)");
    }

    // Create pending loan
    var loan = new Loan(userId, request.BookId);
    await _loanRepository.AddAsync(loan);
    _logger.LogInformation(
        "📝 [SAGA-STEP-1] Loan record created | LoanId: {LoanId} | Status: PENDING", 
        loan.LoanId);

    try
    {
        // Step 2: Check availability
        _logger.LogInformation(
            "🔍 [SAGA-STEP-2] Checking book availability | BookId: {BookId}", 
            request.BookId);
        
        var book = await _catalogService.GetBookAsync(request.BookId);
        
        _logger.LogInformation(
            "📚 [SAGA-STEP-2-RESULT] Book details retrieved | BookId: {BookId} | Available: {IsAvailable} | Stock: {AvailableCopies}/{TotalCopies}",
            book.BookId, book.IsAvailable, book.AvailableCopies, book.TotalCopies);
        
        if (!book.IsAvailable)
        {
            _logger.LogWarning(
                "❌ [SAGA-STEP-2-FAILED] Book unavailable | BookId: {BookId} | Stock: {AvailableCopies}",
                request.BookId, book.AvailableCopies);
            throw new InvalidOperationException("Book is not available");
        }

        // Step 3: Decrement stock
        _logger.LogInformation(
            "📉 [SAGA-STEP-3] Decrementing stock | BookId: {BookId}",
            request.BookId);
        
        await _catalogService.DecrementStockAsync(request.BookId);
        
        _logger.LogInformation(
            "✅ [SAGA-STEP-3-RESULT] Stock decremented | BookId: {BookId}",
            request.BookId);
    }
    catch (Exception ex)
    {
        // Compensating transaction
        _logger.LogError(ex,
            "💥 [SAGA-FAILED] Saga execution failed | BookId: {BookId} | Reason: {Reason} | Executing compensating transaction",
            request.BookId, ex.Message);
        
        if (loan.Status == "PENDING")
        {
            loan.MarkAsFailed();
            await _loanRepository.UpdateAsync(loan);
            _logger.LogInformation(
                "🔄 [SAGA-COMPENSATION] Loan marked as FAILED | LoanId: {LoanId}",
                loan.LoanId);
        }
        
        throw new InvalidOperationException($"Failed to borrow book: {ex.Message}", ex);
    }

    // Mark as checked out
    loan.MarkAsCheckedOut();
    await _loanRepository.UpdateAsync(loan);
    
    _logger.LogInformation(
        "🎉 [SAGA-SUCCESS] Borrow completed | LoanId: {LoanId} | UserId: {UserId} | BookId: {BookId} | DueDate: {DueDate}",
        loan.LoanId, userId, request.BookId, loan.DueDate?.ToString("yyyy-MM-dd"));

    return MapToResponse(loan);
}
```

**Also update ReturnBookAsync:**

```csharp
public async Task ReturnBookAsync(int loanId)
{
    _logger.LogInformation("📚 [RETURN-START] Processing return | LoanId: {LoanId}", loanId);

    var loan = await _loanRepository.GetByIdAsync(loanId);
    if (loan == null)
    {
        _logger.LogWarning("❌ [RETURN-FAILED] Loan not found | LoanId: {LoanId}", loanId);
        throw new InvalidOperationException("Loan not found");
    }

    loan.MarkAsReturned();
    await _loanRepository.UpdateAsync(loan);
    
    _logger.LogInformation("✅ [RETURN-UPDATED] Loan marked as returned | LoanId: {LoanId}", loanId);

    try
    {
        await _catalogService.IncrementStockAsync(loan.BookId);
        _logger.LogInformation("📈 [RETURN-STOCK] Stock incremented | BookId: {BookId}", loan.BookId);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, 
            "⚠️ [RETURN-STOCK-FAILED] Failed to increment stock | BookId: {BookId} | Loan still marked as returned",
            loan.BookId);
    }
    
    _logger.LogInformation("🎉 [RETURN-SUCCESS] Return completed | LoanId: {LoanId} | BookId: {BookId}", loanId, loan.BookId);
}
```

---

### Step 4: Inter-Service HTTP Call Logging

**Location:** `src/Services/LoanService/Clients/CatalogServiceClient.cs`

**Add timing and detailed logging for each HTTP call:**

```csharp
public async Task<BookResponse> GetBookAsync(int bookId)
{
    // Propagate correlation ID (already done in Phase 2)
    var correlationId = _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault();
    if (!string.IsNullOrEmpty(correlationId))
    {
        _httpClient.DefaultRequestHeaders.Remove("X-Correlation-ID");
        _httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
    }

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    _logger.LogInformation("🔗 [HTTP-CALL] CatalogService | GET /api/books/{BookId}", bookId);

    var response = await _httpClient.GetAsync($"/api/books/{bookId}");
    
    stopwatch.Stop();
    
    _logger.LogInformation(
        "📨 [HTTP-RESPONSE] CatalogService | {StatusCode} | {ElapsedMs}ms | GET /api/books/{BookId}",
        (int)response.StatusCode, stopwatch.ElapsedMilliseconds, bookId);

    if (!response.IsSuccessStatusCode)
    {
        _logger.LogWarning(
            "⚠️ [HTTP-ERROR] CatalogService returned {StatusCode} | GET /api/books/{BookId}",
            (int)response.StatusCode, bookId);
    }

    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<BookResponse>() 
           ?? throw new InvalidOperationException("Failed to deserialize book response");
}

// Apply same pattern to DecrementStockAsync and IncrementStockAsync
public async Task DecrementStockAsync(int bookId)
{
    // Propagate correlation ID
    var correlationId = _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault();
    if (!string.IsNullOrEmpty(correlationId))
    {
        _httpClient.DefaultRequestHeaders.Remove("X-Correlation-ID");
        _httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
    }

    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    _logger.LogInformation("🔗 [HTTP-CALL] CatalogService | PUT /api/books/{BookId}/stock (decrement)", bookId);

    var request = new StockUpdateRequest { ChangeAmount = -1 };
    var response = await _httpClient.PutAsJsonAsync($"/api/books/{bookId}/stock", request);
    
    stopwatch.Stop();
    
    _logger.LogInformation(
        "📨 [HTTP-RESPONSE] CatalogService | {StatusCode} | {ElapsedMs}ms | PUT /api/books/{BookId}/stock",
        (int)response.StatusCode, stopwatch.ElapsedMilliseconds, bookId);

    response.EnsureSuccessStatusCode();
}
```

---

### Step 5: CatalogService Stock Operation Logging

**Location:** `src/Services/CatalogService/Controllers/BooksController.cs`

**Enhance stock update endpoint logging:**

```csharp
[HttpPut("{id}/stock")]
public async IActionResult UpdateStock(int id, [FromBody] StockUpdateRequest request)
{
    try
    {
        _logger.LogInformation(
            "📊 [STOCK-UPDATE-START] BookId: {BookId} | Change: {ChangeAmount}",
            id, request.ChangeAmount);

        var book = await _bookService.UpdateStockAsync(id, request.ChangeAmount);
        
        _logger.LogInformation(
            "✅ [STOCK-UPDATE-SUCCESS] BookId: {BookId} | Change: {ChangeAmount} | NewStock: {AvailableCopies}/{TotalCopies}",
            id, request.ChangeAmount, book.AvailableCopies, book.TotalCopies);

        return Ok(book);
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogWarning(
            "⚠️ [STOCK-UPDATE-FAILED] BookId: {BookId} | Change: {ChangeAmount} | Reason: {Reason}",
            id, request.ChangeAmount, ex.Message);
        return BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex,
            "❌ [STOCK-UPDATE-ERROR] BookId: {BookId} | Change: {ChangeAmount}",
            id, request.ChangeAmount);
        return StatusCode(500, new { message = "Error updating stock" });
    }
}
```

---

### Step 6: User Authentication Logging (UserService)

**Location:** `src/Services/UserService/Controllers/UsersController.cs`

**Enhance login endpoint:**

```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    try
    {
        _logger.LogInformation("🔐 [LOGIN-ATTEMPT] Email: {Email}", request.Email);

        var user = await _userService.AuthenticateAsync(request.Email, request.Password);
        
        if (user == null)
        {
            _logger.LogWarning("❌ [LOGIN-FAILED] Invalid credentials | Email: {Email}", request.Email);
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var token = _userService.GenerateJwtToken(user);
        
        _logger.LogInformation(
            "✅ [LOGIN-SUCCESS] User authenticated | UserId: {UserId} | Email: {Email} | Role: {Role}",
            user.UserId, user.Email, user.Role);

        return Ok(new LoginResponse
        {
            Token = token,
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ [LOGIN-ERROR] Exception during login | Email: {Email}", request.Email);
        return StatusCode(500, new { message = "An error occurred during login" });
    }
}
```

**Enhance register endpoint:**

```csharp
[HttpPost("register")]
public async Task<IActionResult> Register([FromBody] RegisterRequest request)
{
    try
    {
        _logger.LogInformation("📝 [REGISTER-ATTEMPT] Email: {Email} | Username: {Username}", 
            request.Email, request.Username);

        var user = await _userService.RegisterUserAsync(request);
        
        _logger.LogInformation(
            "✅ [REGISTER-SUCCESS] User created | UserId: {UserId} | Email: {Email} | Role: {Role}",
            user.UserId, user.Email, user.Role);

        var token = _userService.GenerateJwtToken(user);

        return Ok(new LoginResponse
        {
            Token = token,
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role
        });
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogWarning("⚠️ [REGISTER-FAILED] {Reason} | Email: {Email}", ex.Message, request.Email);
        return BadRequest(new { message = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ [REGISTER-ERROR] Email: {Email}", request.Email);
        return StatusCode(500, new { message = "An error occurred during registration" });
    }
}
```

---

## Verification Checklist

After implementation, verify these scenarios:

### Consul Registration
- [ ] Service startup shows: 🔌 [CONSUL-REGISTER] attempts
- [ ] Success shows: ✅ [CONSUL-SUCCESS]
- [ ] Failures show: ⚠️ [CONSUL-RETRY] and ❌ [CONSUL-FAILED]
- [ ] Shutdown shows: 🔌 [CONSUL-DEREGISTER]

### Health Checks
- [ ] Health check endpoint logs: 💓 [HEALTH-CHECK] when called
- [ ] Can see Consul polling every 10 seconds (when service is running)
- [ ] Logs show client IP (Consul container IP)

### JWT Validation (Gateway)
- [ ] Valid token shows: ✅ [JWT-SUCCESS] with UserId and Role
- [ ] Invalid token shows: ❌ [JWT-FAILED] with reason
- [ ] Missing token shows: ⚠️ [JWT-CHALLENGE]

### Saga Orchestration (Borrow Book)
- [ ] Start shows: 🚀 [SAGA-START]
- [ ] Each step shows: 📝 [SAGA-STEP-X]
- [ ] Success shows: 🎉 [SAGA-SUCCESS]
- [ ] Failure shows: 💥 [SAGA-FAILED] and 🔄 [SAGA-COMPENSATION]

### Inter-Service Calls
- [ ] HTTP calls show: 🔗 [HTTP-CALL] with target URL
- [ ] Responses show: 📨 [HTTP-RESPONSE] with status and timing

### User Operations
- [ ] Login shows: 🔐 [LOGIN-ATTEMPT], ✅ [LOGIN-SUCCESS] or ❌ [LOGIN-FAILED]
- [ ] Register shows: 📝 [REGISTER-ATTEMPT], ✅ [REGISTER-SUCCESS]

---

## Common Issues & Solutions

### Issue: Too many emojis make logs hard to read
**Solution:** Emojis are optional - can remove them but keep the structured tags like [SAGA-START]

### Issue: Logs too verbose
**Solution:** Filter by log level or specific tags in Seq (Phase 4)

### Issue: Performance impact from logging
**Solution:** Logging overhead is minimal (<5ms per log). If concerned, use async logging.

---

## Files Modified in This Phase

- `src/Services/UserService/Extensions/ConsulServiceRegistration.cs` ✏️
- `src/Services/CatalogService/Extensions/ConsulServiceRegistration.cs` ✏️
- `src/Services/LoanService/Extensions/ConsulServiceRegistration.cs` ✏️
- `src/Services/UserService/Program.cs` ✏️ (health check logging)
- `src/Services/CatalogService/Program.cs` ✏️ (health check logging)
- `src/Services/LoanService/Program.cs` ✏️ (health check logging)
- `src/Services/UserService/Middleware/HealthCheckLoggingMiddleware.cs` ✨ (optional)
- `src/Services/CatalogService/Middleware/HealthCheckLoggingMiddleware.cs` ✨ (optional)
- `src/Services/LoanService/Middleware/HealthCheckLoggingMiddleware.cs` ✨ (optional)
- `src/Gateway/LibHub.Gateway.Api/Program.cs` ✏️
- `src/Services/LoanService/Services/LoanService.cs` ✏️
- `src/Services/LoanService/Clients/CatalogServiceClient.cs` ✏️
- `src/Services/CatalogService/Controllers/BooksController.cs` ✏️
- `src/Services/UserService/Controllers/UsersController.cs` ✏️

Total: ~11-14 files (depending on middleware approach)

---

## Implementation Status

### ❌ Not Started

**Agent Instructions:**
1. Start with one service (e.g., LoanService saga logging) as proof of concept
2. Test to ensure emojis display correctly in terminal/Docker logs
3. Apply pattern to other services
4. Verify each enhanced logging area works independently
5. Test end-to-end borrow flow to see all logs together

---

## Completion Report

**Date Completed:** _[Agent fills this in]_

**Components Implemented:**
- [ ] Consul registration logging (3 services)
- [ ] Health check endpoint logging (3 services)
- [ ] JWT validation logging (Gateway)
- [ ] Saga orchestration logging (LoanService)
- [ ] Inter-service HTTP logging (CatalogServiceClient)
- [ ] Stock update logging (CatalogService)
- [ ] Authentication logging (UserService)

**What Was Done:**
_[Agent describes implementation details, any modifications to proposed structure]_

**Test Results:**
_[Agent shows example log output demonstrating the enhanced logging in action]_

**Next Steps for Phase 4:**
- Ready to add Seq container for log aggregation
- All structured logging is in place
- Correlation IDs are working
- Enhanced event logging provides rich context
- Seq will make all this searchable and filterable

**Notes:**
_[Any observations about log volume, readability, performance]_
