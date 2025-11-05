# Current Logging Implementation in LibHub

## Overview

LibHub currently uses **ASP.NET Core's built-in logging framework** (`Microsoft.Extensions.Logging`), which is the standard logging infrastructure provided by .NET. This document describes the current implementation and features.

## Logging Architecture

### 1. ILogger<T> Dependency Injection

Every service and controller injects `ILogger<T>` through constructor injection:

```csharp
private readonly ILogger<LoanService> _logger;

public LoanService(
    LoanRepository loanRepository,
    ICatalogServiceClient catalogService,
    ILogger<LoanService> logger)
{
    _logger = logger;
}
```

**Services Using ILogger:**
- `LoanService` - Saga orchestration logging
- `BooksController` (CatalogService) - Book management operations
- `Program.cs` (all services) - Application startup and database initialization

## Log Levels Used

The project implements three primary log levels:

### LogInformation
Used for normal flow events and successful operations:

**Examples:**
```csharp
_logger.LogInformation("Starting Saga: BorrowBook for UserId={UserId}, BookId={BookId}", userId, request.BookId);
_logger.LogInformation("Saga Step 2: Created PENDING loan {LoanId}", loan.LoanId);
_logger.LogInformation("Stock decremented successfully");
_logger.LogInformation("Book created: {BookId} - {Title}", book.BookId, book.Title);
_logger.LogInformation("Database created successfully for CatalogService.");
```

### LogWarning
Used for unusual but handled situations:

**Examples:**
```csharp
_logger.LogWarning("Saga aborted: User {UserId} has reached max loan limit", userId);
_logger.LogWarning("Book creation validation failed: {Message}", ex.Message);
_logger.LogWarning("Saga Step 3 failed: Book {BookId} is not available", request.BookId);
_logger.LogWarning("Cannot delete book {BookId}: {Message}", id, ex.Message);
_logger.LogWarning(ex, "Failed to increment stock for book {BookId}", loan.BookId);
```

### LogError
Used for failures and exceptions:

**Examples:**
```csharp
_logger.LogError(ex, "Saga failed for BookId={BookId}, executing compensating transaction", request.BookId);
_logger.LogError(ex, "Error creating book");
_logger.LogError(ex, "Error updating book {BookId}", id);
_logger.LogError(ex, "Failed to create database for CatalogService.");
```

## Configuration

### appsettings.json - LoanService
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### appsettings.json - CatalogService
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

### Configuration Breakdown

- **Default: Information** - Shows all Info, Warning, and Error logs from application code
- **Microsoft.AspNetCore: Warning** - Filters out verbose ASP.NET Core framework logs
- **Microsoft.EntityFrameworkCore.Database.Command: Information** - Shows SQL queries executed by EF Core (CatalogService only)

## Structured Logging

The project uses **structured logging** with named parameters for better searchability and filtering:

```csharp
// ✅ Good - Structured with named parameters
_logger.LogInformation("Saga Step 2: Created PENDING loan {LoanId}", loan.LoanId);
_logger.LogError(ex, "Error updating book {BookId}", id);

// ❌ Bad - String interpolation (not structured)
// _logger.LogInformation($"Saga Step 2: Created PENDING loan {loan.LoanId}");
```

**Benefits:**
- Parameters are indexed separately by log aggregation tools
- Easy to search logs by specific values (e.g., all logs for BookId=123)
- Better performance (no string allocation for logs that are filtered out)

## Borrow Book Saga - Logging Flow

When a user borrows a book, the following logs are generated in **LoanService**:

```
[Information] Starting Saga: BorrowBook for UserId=1, BookId=5
[Information] Saga Step 2: Created PENDING loan LoanId=42
[Information] Saga Step 3: Verifying book availability for BookId=5
[Information] Saga Step 4: Decrementing stock for BookId=5
[Information] Saga Step 4: Stock decremented successfully
[Information] Saga Step 5: Loan 42 marked as CheckedOut - Saga completed successfully
```

**Corresponding logs in CatalogService:**
```
[Information] GET /api/books/5 - Book found, AvailableCopies: 3
[Information] Stock updated for book BookId=5: ChangeAmount=-1
```

**On Failure (Compensating Transaction):**
```
[Error] Saga failed for BookId=5, executing compensating transaction
       Exception: Book is not available
[Information] Saga compensating transaction: Marked loan 42 as FAILED
```

## CatalogService Book Management Logging

### Book Creation
```
[Information] Book created: BookId=10 - Title="The Great Gatsby"
```

### Book Update
```
[Information] Book updated: BookId=10
```

### Book Deletion
```
[Information] Book deleted: BookId=10
```

### Stock Updates
```
[Information] Stock updated for book BookId=5: ChangeAmount=-1
```

### Validation Failures
```
[Warning] Book creation validation failed: Message="ISBN already exists"
[Warning] Stock update failed for book BookId=5: Message="Insufficient stock"
```

## Default Console Logger

ASP.NET Core automatically configures a **Console Logger** by default, which:
- Outputs to stdout (Information, Debug) and stderr (Warning, Error)
- Captured by Docker containers
- Viewable via `docker logs <container-name>`
- Includes timestamps, log level, category, and message

**Example Console Output:**
```
info: LibHub.LoanService.Services.LoanService[0]
      Starting Saga: BorrowBook for UserId=1, BookId=5
info: LibHub.LoanService.Services.LoanService[0]
      Saga Step 2: Created PENDING loan 42
```

## Current Limitations

### What is NOT Implemented

❌ **File Logging**
- No persistent log files
- Logs only exist in container console output
- Lost when container is removed

❌ **Log Aggregation**
- No centralized log storage (Seq, ELK, Application Insights)
- Cannot search across multiple services easily
- No log retention beyond container lifecycle

❌ **Custom Formatters**
- Using default console formatter
- No JSON structured output for machine parsing

❌ **Scoped Logging Contexts**
- No request correlation IDs across services
- Difficult to trace a single request through multiple services

❌ **Log Enrichment**
- No automatic metadata (machine name, environment, version)
- No user context enrichment

❌ **Advanced Features**
- No log sampling or filtering by environment
- No dynamic log level changes without restart
- No performance metrics or timing

## Viewing Logs

### Monitor Single Service
```bash
docker logs -f libhub-ver2-loanservice-1
```

### Monitor Multiple Services
```bash
docker-compose logs -f gateway loanservice catalogservice
```

### Filter Logs
```bash
# Saga-related logs only
docker logs libhub-ver2-loanservice-1 2>&1 | grep -i "saga"

# Errors only
docker logs libhub-ver2-loanservice-1 2>&1 | grep -i "error\|exception"

# Specific book operations
docker logs libhub-ver2-catalogservice-1 2>&1 | grep "BookId=5"
```

### View Recent Logs
```bash
# Last 50 lines
docker logs --tail 50 libhub-ver2-loanservice-1

# Logs since 10 minutes ago
docker logs --since 10m libhub-ver2-loanservice-1
```

## Log Categories by Service

### UserService
- User registration events
- Authentication (successful/failed login)
- JWT token generation
- Database initialization

### CatalogService
- Book CRUD operations (Create, Read, Update, Delete)
- Stock management
- Search operations
- Validation failures
- Database initialization and seed data

### LoanService
- Saga orchestration (detailed step-by-step)
- Loan creation (PENDING → CheckedOut)
- Compensating transactions (failures)
- Stock validation
- Return operations

### Gateway
- Request routing
- JWT validation
- HTTP status codes
- Service health checks

## Best Practices Followed

✅ **Structured Logging** - Using named parameters instead of string interpolation
✅ **Appropriate Log Levels** - Info for normal flow, Warning for handled errors, Error for exceptions
✅ **Exception Logging** - Including exception objects with LogError
✅ **Contextual Information** - Adding relevant IDs (UserId, BookId, LoanId)
✅ **Saga Step Tracking** - Clear step-by-step logging for distributed transactions
✅ **Business Event Logging** - Logging audit-worthy operations (book creation, deletions)

## Recommendations for Enhancement

### Priority 1 - Critical
1. **Add Correlation IDs** - Track requests across services
2. **Implement Serilog** - Structured logging with file sinks
3. **Add Request/Response Logging Middleware** - Automatic HTTP logging

### Priority 2 - Important
4. **Centralized Log Aggregation** - Seq or ELK stack for searching
5. **Log Enrichment** - Add environment, service name, version to all logs
6. **Performance Logging** - Add timing for key operations

### Priority 3 - Nice to Have
7. **Health Check Logging** - Log service health status changes
8. **Database Query Logging** - Enable for all services (currently only CatalogService)
9. **Log Sampling** - Reduce log volume in production with sampling

## Summary

The current logging implementation:
- ✅ Uses standard Microsoft.Extensions.Logging framework
- ✅ Implements proper log levels (Information, Warning, Error)
- ✅ Uses structured logging with named parameters
- ✅ Provides excellent saga orchestration visibility
- ✅ Logs all critical business operations
- ✅ Console output works well with Docker
- ⚠️ Lacks persistence beyond container lifecycle
- ⚠️ No centralized log aggregation
- ⚠️ No correlation IDs for distributed tracing

**Overall Assessment:** Good foundation for development, but needs enhancement for production use.
