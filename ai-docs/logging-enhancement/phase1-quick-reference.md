# Quick Reference: Using Serilog in LibHub Services

## Overview
Phase 1 is complete! All services now use Serilog for structured logging. This guide shows you how to use the new logging capabilities.

---

## Basic Usage

### 1. Existing ILogger Code Still Works! ✅

**No changes needed to your existing code:**
```csharp
// This still works exactly as before
_logger.LogInformation("User {UserId} logged in", userId);
_logger.LogError(ex, "Failed to process order {OrderId}", orderId);
```

Serilog automatically captures structured properties from your log messages!

---

## Log Output Format

### What You'll See in Console

**Before (default ASP.NET Core):**
```
info: LibHub.UserService.Controllers.AuthController[0]
      User registered successfully
```

**Now (Serilog structured):**
```
[20:59:12 INF] [UserService] User registered successfully
```

### Format Breakdown
```
[HH:mm:ss LVL] [ServiceName] Message
```
- `HH:mm:ss` - Time (24-hour format)
- `LVL` - Log level (INF, WRN, ERR, FTL, DBG, VRB)
- `ServiceName` - Which service logged this (UserService, CatalogService, LoanService, Gateway)
- `Message` - The actual log message

---

## Log Levels

```csharp
_logger.LogTrace("Very detailed trace");       // [VRB] - Rarely shown
_logger.LogDebug("Debug information");         // [DBG] - Development only
_logger.LogInformation("Normal operation");    // [INF] - Default minimum
_logger.LogWarning("Something unusual");       // [WRN] - Potential issues
_logger.LogError(ex, "Error occurred");        // [ERR] - Recoverable errors
_logger.LogCritical(ex, "Critical failure");   // [FTL] - Fatal errors
```

---

## Structured Logging Examples

### Good: Use Structured Properties
```csharp
// ✅ GOOD - Properties are captured
_logger.LogInformation("User {UserId} borrowed book {BookId}", userId, bookId);

// Output: [20:59:12 INF] [LoanService] User 5 borrowed book 123
// Properties available: UserId=5, BookId=123
```

### Bad: String Interpolation
```csharp
// ❌ BAD - Properties are lost
_logger.LogInformation($"User {userId} borrowed book {bookId}");

// Output: [20:59:12 INF] [LoanService] User 5 borrowed book 123
// Properties NOT available for searching
```

**Why it matters:** In Seq (Phase 4), you'll be able to search by `UserId = 5` or `BookId = 123` only if you use structured properties!

---

## Common Logging Patterns

### 1. Operation Start/End
```csharp
_logger.LogInformation("Processing loan request for User {UserId}, Book {BookId}", userId, bookId);

// ... do work ...

_logger.LogInformation("Loan created successfully: LoanId {LoanId}, DueDate {DueDate}", 
    loan.LoanId, loan.DueDate);
```

### 2. Error Handling
```csharp
try
{
    // ... operation ...
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to create loan for User {UserId}, Book {BookId}", 
        userId, bookId);
    throw;
}
```

### 3. Performance Tracking
```csharp
using var activity = _logger.BeginScope("Processing order {OrderId}", orderId);
_logger.LogInformation("Starting database query");
// ... query ...
_logger.LogInformation("Query completed in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
```

### 4. HTTP Client Calls
```csharp
_logger.LogInformation("Calling CatalogService: GET /api/books/{BookId}", bookId);

var response = await _httpClient.GetAsync($"/api/books/{bookId}");

_logger.LogInformation("CatalogService responded: {StatusCode}", response.StatusCode);
```

---

## Entity Framework Core Logging

### SQL Queries Are Now Logged!

**Configuration (already done in appsettings.json):**
```json
"Microsoft.EntityFrameworkCore.Database.Command": "Information"
```

**What you'll see:**
```
[20:59:12 INF] [UserService] Executed DbCommand (8ms) [Parameters=[], CommandType='Text', CommandTimeout='30']
SELECT * FROM Users WHERE UserId = @p0
```

**To hide SQL queries** (if too verbose):
Change to `"Microsoft.EntityFrameworkCore.Database.Command": "Warning"`

---

## Filtering Logs

### In Console (grep)
```bash
# Show only UserService logs
docker logs libhub-userservice | grep "[UserService]"

# Show only errors
docker logs libhub-userservice | grep "[ERR]"

# Show specific user's actions
docker logs libhub-userservice | grep "UserId 5"
```

### In Seq (Phase 4 - when container is added)
```
ServiceName = 'UserService'
UserId = 5
@Level = 'Error'
@MessageTemplate like '%loan%'
```

---

## Current Limitations (Fixed in Future Phases)

### ❌ Cannot Track Requests Across Services Yet
```
Gateway → LoanService → CatalogService
   ❓         ❓            ❓
```
**Solution:** Phase 2 will add Correlation IDs

### ❌ Logs Disappear on Container Restart
**Solution:** Phase 4 will add Seq for persistent storage

### ❌ Hard to Search Through Logs
**Solution:** Phase 4 will add Seq UI with powerful search

---

## Expected Seq Connection Errors (Normal!)

You'll see this until Phase 4:
```
[ERR] [UserService] Failed to send events to Seq (http://seq:5341)
System.Net.Http.HttpRequestException: Connection refused
```

**This is normal!** Seq container doesn't exist yet.
- Logs still go to console ✅
- Services run normally ✅
- Error goes away in Phase 4 ✅

---

## Configuration Files

### Minimum Log Levels (appsettings.json)

All services have this configured:
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",        // Reduce ASP.NET noise
        "Microsoft.EntityFrameworkCore": "Information",
        "Microsoft.EntityFrameworkCore.Database.Command": "Information",  // Show SQL
        "System.Net.Http.HttpClient": "Information"  // Show HTTP calls
      }
    }
  }
}
```

### To Change Log Levels

**Show more detail (Development):**
```json
"Default": "Debug"
```

**Show less noise (Production):**
```json
"Microsoft.AspNetCore": "Warning",
"Microsoft.EntityFrameworkCore": "Warning"
```

**Hide SQL queries:**
```json
"Microsoft.EntityFrameworkCore.Database.Command": "Warning"
```

---

## Testing Your Logging

### 1. Run a Service
```bash
cd src/Services/UserService
dotnet run
```

### 2. Check Log Format
Look for:
```
[20:59:12 INF] [UserService] Starting UserService
[20:59:12 INF] [UserService] Database created successfully for UserService
[20:59:12 INF] [UserService] UserService started successfully on port 5002
```

✅ If you see `[ServiceName]` in brackets, it's working!

### 3. Test API Endpoints
```bash
# Register a user
curl -X POST http://localhost:5002/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"username":"test","email":"test@test.com","password":"Test123!"}'

# Check logs - you should see structured output
```

---

## Troubleshooting

### Issue: No [ServiceName] in Logs
**Cause:** Old build artifacts  
**Fix:** Clean and rebuild
```bash
dotnet clean
dotnet build
```

### Issue: Logs Look Same as Before
**Cause:** Not using updated Program.cs  
**Fix:** Verify using statements:
```csharp
using Serilog;
using Serilog.Events;
```

### Issue: Build Errors
**Cause:** Missing NuGet packages  
**Fix:** Restore packages
```bash
dotnet restore --force-evaluate
```

### Issue: Seq Connection Errors
**Cause:** Seq container not running (expected until Phase 4)  
**Fix:** Ignore or disable Seq sink temporarily:
```csharp
// Comment out in Program.cs
// .WriteTo.Seq("http://seq:5341")
```

---

## Benefits You Get Right Now

### 1. ✅ Easy Service Identification
```
[UserService] User logged in        ← Clearly from UserService
[CatalogService] Book retrieved     ← Clearly from CatalogService
[LoanService] Loan created          ← Clearly from LoanService
```

### 2. ✅ Consistent Timestamps
```
[20:59:12 INF] [UserService] Request started
[20:59:12 INF] [LoanService] Processing loan
[20:59:13 INF] [CatalogService] Stock updated
[20:59:13 INF] [LoanService] Loan completed
```
Easy to see timing!

### 3. ✅ Structured Properties
```csharp
_logger.LogInformation("User {UserId} borrowed {BookTitle}", userId, book.Title);
```
Properties captured: `UserId`, `BookTitle`

### 4. ✅ Clean Output
No more verbose default ASP.NET Core logs cluttering your console!

---

## Next Steps

**Phase 1 (Current):** ✅ Structured logging with Serilog
- You are here!

**Phase 2 (Next):** Correlation ID Middleware
- Track requests across services
- Add X-Correlation-ID header
- See complete request journey

**Phase 3:** Enhanced logging for specific events
- Consul registration logging
- JWT validation logging
- Saga orchestration logging

**Phase 4:** Seq Container
- Persistent log storage
- Web UI for searching logs
- Real-time log streaming
- No more lost logs on restart!

---

## Quick Command Reference

```bash
# Build all services
dotnet build LibHub.sln

# Run individual service
cd src/Services/UserService && dotnet run

# View Docker logs
docker logs -f libhub-userservice

# Grep for specific service
docker logs libhub-userservice | grep "[UserService]"

# Show only errors
docker logs libhub-userservice | grep "\[ERR\]"
```

---

## Questions?

**Q: Do I need to change my existing logging code?**  
A: No! Existing `ILogger` calls work as-is. Just keep using structured properties ({PropertyName}).

**Q: Why am I seeing Seq connection errors?**  
A: Normal! Seq container will be added in Phase 4. Services work fine without it.

**Q: How do I search logs?**  
A: Currently use grep. In Phase 4, Seq UI will have powerful search.

**Q: Can I disable Seq sink?**  
A: Yes, comment out `.WriteTo.Seq()` in Program.cs if errors bother you.

**Q: Where do logs go now?**  
A: Console only. Phase 4 adds persistent storage in Seq.

---

## Summary

🎉 **You now have structured logging!**

Key points:
- ✅ Use existing `ILogger` code - no changes needed
- ✅ Always use structured properties: `{PropertyName}`
- ✅ Look for `[ServiceName]` in log output
- ✅ Ignore Seq errors (until Phase 4)
- ✅ SQL queries are logged automatically
- ✅ Consistent format across all services

**Ready for Phase 2: Correlation IDs!** 🚀
