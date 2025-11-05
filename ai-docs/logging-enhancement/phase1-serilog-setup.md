# Phase 1: Serilog Setup - Structured Logging Foundation

## Goal
Replace ASP.NET Core's default logging with Serilog to enable structured logging with rich context and multiple output sinks (Console + Seq). This provides the foundation for all subsequent logging enhancements.

## Success Criteria
- [ ] All 4 services (UserService, CatalogService, LoanService, Gateway) use Serilog
- [ ] Logs include structured properties (ServiceName, Timestamp, LogLevel)
- [ ] Console output is formatted consistently across all services
- [ ] Seq sink is configured (ready for Phase 4 when Seq container is added)
- [ ] Existing ILogger code continues to work (backward compatible)
- [ ] Application starts successfully with new logging configuration

## Estimated Time
2-3 hours

---

## Implementation Steps

### Step 1: Install Serilog NuGet Packages

**For each service** (UserService, CatalogService, LoanService, Gateway):

```bash
# Navigate to service directory
cd src/Services/UserService  # or CatalogService, LoanService, or Gateway/LibHub.Gateway.Api

# Install packages
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.Seq
dotnet add package Serilog.Enrichers.Environment
dotnet add package Serilog.Enrichers.Thread
dotnet add package Serilog.Settings.Configuration
```

### Step 2: Update Program.cs for Each Service

**Location:** `src/Services/UserService/Program.cs` (repeat for all services)

**Required changes:**
1. Add `using Serilog;` and `using Serilog.Events;` at the top
2. Create Serilog logger BEFORE building the app
3. Replace default logger with Serilog using `builder.Host.UseSerilog()`
4. Add try-catch-finally block for proper startup/shutdown logging
5. Configure enrichers: ServiceName, MachineName, ThreadId

**Code structure:**
```csharp
using Serilog;
using Serilog.Events;

// Configure Serilog FIRST (before CreateBuilder)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("ServiceName", "UserService") // Change per service
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{ServiceName}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.Seq("http://seq:5341") // Will connect when Seq container is added
    .CreateLogger();

try
{
    Log.Information("Starting {ServiceName}", "UserService");
    
    var builder = WebApplication.CreateBuilder(args);
    
    // USE SERILOG instead of default logging
    builder.Host.UseSerilog();
    
    // ... rest of existing configuration (don't change) ...
    
    var app = builder.Build();
    
    // ... rest of existing middleware and startup code ...
    
    Log.Information("{ServiceName} started successfully on port {Port}", "UserService", 5002);
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "{ServiceName} failed to start", "UserService");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
```

**Service-specific values to change:**
- UserService: `ServiceName = "UserService"`, Port = 5002
- CatalogService: `ServiceName = "CatalogService"`, Port = 5001
- LoanService: `ServiceName = "LoanService"`, Port = 5003
- Gateway: `ServiceName = "Gateway"`, Port = 5000

### Step 3: Update appsettings.json for Each Service

**Location:** `src/Services/UserService/appsettings.json` (repeat for all services)

**Replace the existing "Logging" section with:**
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Information",
        "Microsoft.EntityFrameworkCore.Database.Command": "Information",
        "System.Net.Http.HttpClient": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] [{ServiceName}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://seq:5341"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"],
    "Properties": {
      "Application": "LibHub"
    }
  }
}
```

**Note:** Keep all other configuration sections (ConnectionStrings, Jwt, etc.) unchanged.

### Step 5: Test Each Service

**Test plan:**
1. Build the service: `dotnet build`
2. Run the service: `dotnet run`
3. Check console output - should see Serilog formatted logs
4. Verify structured format: `[HH:mm:ss INF] [ServiceName] Message`
5. Test existing functionality (register, login, browse books, etc.)
6. Verify no errors in startup

**Expected console output:**
```
[10:30:45 INF] [UserService] Starting UserService
[10:30:46 INF] [UserService] Database created successfully for UserService
[10:30:46 INF] [UserService] UserService started successfully on port 5002
```

### Step 6: Update Docker Configuration

**If using Docker, update Dockerfile to ensure logs are visible:**

No changes needed - Serilog writes to stdout by default, which Docker captures.

**Verify with:**
```bash
docker compose up -d
docker logs libhub-ver2-userservice-1
```

Should see Serilog formatted logs.

---

## Verification Checklist

After implementing all services, verify:

- [ ] UserService starts and shows Serilog logs
- [ ] CatalogService starts and shows Serilog logs
- [ ] LoanService starts and shows Serilog logs
- [ ] Gateway starts and shows Serilog logs
- [ ] All services show `[ServiceName]` in logs
- [ ] EF Core SQL queries are logged (check CatalogService)
- [ ] Existing functionality works (login, browse, borrow)
- [ ] No breaking changes to business logic
- [ ] Docker logs show structured format

---

## Common Issues & Solutions

### Issue: "Serilog.Log not found"
**Solution:** Ensure `using Serilog;` is at the top of Program.cs

### Issue: "Seq connection errors in console"
**Solution:** This is expected! Seq container doesn't exist yet. Serilog will retry and continue logging to console.

### Issue: "Logs not showing in Docker"
**Solution:** Check `docker logs <container>` - Serilog writes to stdout by default

### Issue: "Build fails with package conflicts"
**Solution:** Make sure all Serilog packages are the same version. Run `dotnet restore --force-evaluate`

---

## Files Modified in This Phase

Expected file changes:
- `src/Services/UserService/Program.cs` ✏️
- `src/Services/UserService/appsettings.json` ✏️
- `src/Services/UserService/UserService.csproj` (NuGet packages) ✏️
- `src/Services/CatalogService/Program.cs` ✏️
- `src/Services/CatalogService/appsettings.json` ✏️
- `src/Services/CatalogService/CatalogService.csproj` ✏️
- `src/Services/LoanService/Program.cs` ✏️
- `src/Services/LoanService/appsettings.json` ✏️
- `src/Services/LoanService/LoanService.csproj` ✏️
- `src/Gateway/LibHub.Gateway.Api/Program.cs` ✏️
- `src/Gateway/LibHub.Gateway.Api/appsettings.json` ✏️
- `src/Gateway/LibHub.Gateway.Api/LibHub.Gateway.Api.csproj` ✏️

Total: ~12 files

---

## Implementation Status

### ❌ Not Started

**Agent Instructions:**
1. Start with UserService as proof of concept
2. Test thoroughly before moving to other services
3. Once UserService works, apply same changes to CatalogService, LoanService, Gateway
4. After completing all services, update this section with completion status
5. Document any issues encountered and how they were resolved

---

## Completion Report

**Date Completed:** _[Agent fills this in]_

**Services Implemented:**
- [ ] UserService
- [ ] CatalogService
- [ ] LoanService
- [ ] Gateway

**What Was Done:**
_[Agent describes the implementation details, any deviations from plan, issues encountered]_

**Test Results:**
_[Agent reports test outcomes, verification checklist results]_

**Next Steps for Phase 2:**
- Ready to implement Correlation ID middleware
- Serilog foundation is in place for context enrichment
- All services can now push additional properties to LogContext

**Notes:**
_[Any additional observations, recommendations, or warnings for next phase]_
