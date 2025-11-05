# Phase 2: Correlation ID Implementation - Request Tracing Across Services

## Goal
Implement Correlation ID middleware to track a single request as it flows through multiple services (Frontend → Gateway → LoanService → CatalogService). This enables complete request tracing and makes debugging distributed transactions dramatically easier.

## Success Criteria
- [ ] Every HTTP request gets a unique Correlation ID (generated or propagated)
- [ ] Correlation ID appears in all log messages within a request context
- [ ] Correlation ID is propagated across service boundaries via HTTP headers
- [ ] Gateway passes Correlation ID to downstream services
- [ ] Services pass Correlation ID to other services they call
- [ ] Frontend can optionally provide Correlation ID
- [ ] Can search Seq by Correlation ID to see complete request journey

## Estimated Time
1-2 hours

---

## Implementation Steps

### Step 1: Create Correlation ID Middleware (Shared Pattern)

**Location:** Create new file in each service  
`src/Services/UserService/Middleware/CorrelationIdMiddleware.cs`

**Purpose:** 
- Extract or generate Correlation ID from HTTP request header
- Add to response header for debugging
- Push to Serilog LogContext so it appears in all logs
- Log request start/end with correlation context

**Code structure:**
```csharp
using Serilog.Context;

namespace LibHub.UserService.Middleware; // Change namespace per service

public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        // Constructor: store dependencies
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Get Correlation ID from request header, or generate new Guid
        // 2. Add to response header (for debugging)
        // 3. Push to Serilog LogContext using LogContext.PushProperty()
        // 4. Log: "Request started: {Method} {Path}"
        // 5. Call next middleware: await _next(context)
        // 6. Log: "Request completed: {Method} {Path} - {StatusCode} ({ElapsedMs}ms)"
        // 7. LogContext will be disposed automatically (using statement)
    }
}

// Extension method for easy registration
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
```

**Implementation details:**
- Header name: `X-Correlation-ID`
- If header exists: use it (propagated from upstream)
- If header missing: generate new `Guid.NewGuid().ToString()`
- Use `LogContext.PushProperty("CorrelationId", correlationId)` to add to all logs
- Track request timing using `Stopwatch` or `DateTimeOffset`

**Create this file for:**
- UserService: `src/Services/UserService/Middleware/CorrelationIdMiddleware.cs`
- CatalogService: `src/Services/CatalogService/Middleware/CorrelationIdMiddleware.cs`
- LoanService: `src/Services/LoanService/Middleware/CorrelationIdMiddleware.cs`
- Gateway: `src/Gateway/LibHub.Gateway.Api/Middleware/CorrelationIdMiddleware.cs`

### Step 2: Register Middleware in Program.cs (All Services)

**Location:** `Program.cs` in each service

**Add middleware registration EARLY in pipeline** (before authentication):

```csharp
var app = builder.Build();

// ... database initialization code ...

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCorrelationId(); // ✅ ADD THIS - Must be early in pipeline

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
// ... rest of middleware
```

**Why early?** So all subsequent middleware and controllers have CorrelationId in logs.

### Step 3: Propagate Correlation ID in HTTP Clients

**Location:** `src/Services/LoanService/Clients/CatalogServiceClient.cs`

**Problem:** When LoanService calls CatalogService, the Correlation ID must be passed along.

**Solution:** Add `IHttpContextAccessor` to access current request's Correlation ID.

#### Step 3a: Register IHttpContextAccessor

**In LoanService/Program.cs:**
```csharp
// Add before builder.Build()
builder.Services.AddHttpContextAccessor();
```

#### Step 3b: Update CatalogServiceClient Constructor

```csharp
public class CatalogServiceClient : ICatalogServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogServiceClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CatalogServiceClient(
        HttpClient httpClient,
        ILogger<CatalogServiceClient> logger,
        IHttpContextAccessor httpContextAccessor) // ✅ ADD THIS
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }
    
    // ... methods below
}
```

#### Step 3c: Add Header Propagation to Each HTTP Call

```csharp
public async Task<BookResponse> GetBookAsync(int bookId)
{
    // Propagate Correlation ID to downstream service
    var correlationId = _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault();
    if (!string.IsNullOrEmpty(correlationId))
    {
        // Remove old header if exists, then add new one
        _httpClient.DefaultRequestHeaders.Remove("X-Correlation-ID");
        _httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
    }

    _logger.LogInformation("🔗 Calling CatalogService: GET /api/books/{BookId}", bookId);

    var response = await _httpClient.GetAsync($"/api/books/{bookId}");
    
    _logger.LogInformation("📨 CatalogService response: {StatusCode}", response.StatusCode);

    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<BookResponse>() 
           ?? throw new InvalidOperationException("Failed to deserialize book response");
}

// ✅ Apply same pattern to:
// - DecrementStockAsync()
// - IncrementStockAsync()
```

### Step 4: Update Serilog Output Template

**Location:** `Program.cs` in each service (Serilog configuration)

**Update Console output template to include CorrelationId:**

```csharp
.WriteTo.Console(
    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{ServiceName}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
```

**Before:** `[10:30:45 INF] [UserService] User registered: UserId=5`  
**After:** `[10:30:45 INF] [UserService] [req-abc-123] User registered: UserId=5`

### Step 5: Frontend - Generate Correlation ID (Optional but Recommended)

**Location:** `frontend/js/api.js` or wherever you make API calls

**Add Correlation ID header to all fetch requests:**

```javascript
// Generate correlation ID
function generateCorrelationId() {
    return `req-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
}

// Example: Borrow book
async function borrowBook(bookId, token) {
    const correlationId = generateCorrelationId();
    console.log(`🔍 Track request: ${correlationId}`);
    
    const response = await fetch('http://localhost:5000/api/loans', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`,
            'X-Correlation-ID': correlationId // ✅ Add this
        },
        body: JSON.stringify({ bookId })
    });
    
    // User can copy correlationId from console and search in Seq
    return response.json();
}
```

**Benefit:** Users can track their specific request through the entire system.

### Step 6: Test Correlation ID Propagation

**Test scenario:** Borrow a book (involves Gateway → LoanService → CatalogService)

#### Test Steps:
1. Start all containers: `docker compose up -d`
2. Open 3 terminals to watch logs:
   ```bash
   # Terminal 1
   docker logs -f libhub-gateway
   
   # Terminal 2
   docker logs -f libhub-ver2-loanservice-1
   
   # Terminal 3
   docker logs -f libhub-ver2-catalogservice-1
   ```
3. Borrow a book from UI
4. Look for the same `[req-...]` in all three logs

**Expected output:**

```
# Gateway logs:
[10:30:45 INF] [Gateway] [req-abc-123] Request started: POST /api/loans
[10:30:45 INF] [Gateway] [req-abc-123] JWT validated: UserId=5

# LoanService logs:
[10:30:45 INF] [LoanService] [req-abc-123] Request started: POST /api/loans
[10:30:45 INF] [LoanService] [req-abc-123] Starting Saga: BorrowBook UserId=5, BookId=10
[10:30:45 INF] [LoanService] [req-abc-123] 🔗 Calling CatalogService: GET /api/books/10

# CatalogService logs:
[10:30:45 INF] [CatalogService] [req-abc-123] Request started: GET /api/books/10
[10:30:45 INF] [CatalogService] [req-abc-123] Book found: BookId=10
[10:30:45 INF] [CatalogService] [req-abc-123] Request completed: GET /api/books/10 - 200
```

**✅ Same `req-abc-123` appears in all services! Perfect traceability!**

---

## Verification Checklist

- [ ] Correlation ID middleware created in all 4 services
- [ ] Middleware registered early in pipeline (Program.cs)
- [ ] IHttpContextAccessor registered in LoanService
- [ ] CatalogServiceClient propagates Correlation ID
- [ ] Serilog output template includes `[{CorrelationId}]`
- [ ] Test: Same Correlation ID appears in Gateway, LoanService, and CatalogService logs
- [ ] Test: Each new request gets a different Correlation ID
- [ ] Test: Frontend can provide custom Correlation ID
- [ ] Logs still work when no Correlation ID provided (falls back to new Guid)

---

## Common Issues & Solutions

### Issue: CorrelationId shows as empty in logs
**Solution:** Make sure you're using `LogContext.PushProperty()` in a `using` statement

### Issue: CorrelationId not propagating to CatalogService
**Solution:** Check that `IHttpContextAccessor` is registered and injected correctly

### Issue: Multiple CorrelationIds in same request
**Solution:** Don't generate new ID if one already exists in request header

### Issue: "IHttpContextAccessor is null"
**Solution:** Ensure `builder.Services.AddHttpContextAccessor()` is called before `builder.Build()`

---

## Files Modified in This Phase

New files created:
- `src/Services/UserService/Middleware/CorrelationIdMiddleware.cs` ✨
- `src/Services/CatalogService/Middleware/CorrelationIdMiddleware.cs` ✨
- `src/Services/LoanService/Middleware/CorrelationIdMiddleware.cs` ✨
- `src/Gateway/LibHub.Gateway.Api/Middleware/CorrelationIdMiddleware.cs` ✨

Files modified:
- `src/Services/UserService/Program.cs` ✏️
- `src/Services/CatalogService/Program.cs` ✏️
- `src/Services/LoanService/Program.cs` ✏️ (also add IHttpContextAccessor)
- `src/Gateway/LibHub.Gateway.Api/Program.cs` ✏️
- `src/Services/LoanService/Clients/CatalogServiceClient.cs` ✏️
- `frontend/js/api.js` ✏️ (optional)

Total: 4 new files, ~6 modified files

---

## Implementation Status

### ❌ Not Started

**Agent Instructions:**
1. Start with UserService middleware as template
2. Test UserService in isolation first
3. Copy pattern to other services
4. Test LoanService → CatalogService propagation specifically
5. Update this section with completion status

---

## Completion Report

**Date Completed:** _[Agent fills this in]_

**Components Implemented:**
- [ ] CorrelationIdMiddleware (all 4 services)
- [ ] Middleware registration (all 4 services)
- [ ] IHttpContextAccessor setup (LoanService)
- [ ] HTTP client propagation (CatalogServiceClient)
- [ ] Serilog template updated (all 4 services)
- [ ] Frontend integration (optional)

**What Was Done:**
_[Agent describes implementation, any challenges, solutions]_

**Test Results:**
_[Agent reports test outcomes - show example logs with CorrelationId tracking]_

**Next Steps for Phase 3:**
- Ready to add enhanced logging for specific events
- Correlation ID foundation is complete
- Can now add emojis and structured messages to key operations
- Consul registration, JWT validation, Saga steps ready for enhancement

**Notes:**
_[Any observations about correlation ID behavior, performance impact, recommendations]_
