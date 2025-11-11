# Correlation ID - Distributed Tracing Guide

## Overview

The Correlation ID is a unique identifier that tracks a single user request across all microservices in LibHub, enabling complete end-to-end distributed tracing.

**Format**: `req-{timestamp}-{random-string}`  
**Example**: `req-1699123930-abc123`

## How It Works

### 1. ID Generation (Frontend)

**File**: `frontend/js/api-client.js`

```javascript
generateCorrelationId() {
    return `req-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
}

async post(endpoint, data, requiresAuth = false) {
    const headers = { 'Content-Type': 'application/json' };
    
    const correlationId = this.generateCorrelationId();
    headers['X-Correlation-ID'] = correlationId;
    
    console.log(`🔍 Track request: ${correlationId} - POST ${endpoint}`);
}
```

**Generated Headers**:
```
POST /api/loans
X-Correlation-ID: req-1699123930-abc123
Authorization: Bearer eyJhbGc...
```

### 2. Gateway Capture

**File**: `src/Gateway/LibHub.Gateway.Api/Middleware/CorrelationIdMiddleware.cs`

```csharp
public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
        ?? Guid.NewGuid().ToString();

    context.Request.Headers["X-Correlation-ID"] = correlationId;

    using (logger.BeginScope(new Dictionary<string, object>
    {
        ["CorrelationId"] = correlationId
    }))
    {
        logger.LogInformation("📨 [REQUEST] {Method} {Path}", 
            context.Request.Method, 
            context.Request.Path);

        await _next(context);
    }
}
```

**Log Output**:
```
[15:32:10 INF] [Gateway] 📨 [REQUEST] POST /api/loans | CorrelationId: req-1699123930-abc123
```

### 3. Ocelot Auto-Propagation

Ocelot automatically forwards the `X-Correlation-ID` header to downstream services.

**Outgoing Request**:
```
POST http://loanservice:5003/api/loans
X-Correlation-ID: req-1699123930-abc123
Authorization: Bearer eyJhbGc...
```

### 4. Service Capture

**File**: `src/Services/LoanService/Middleware/CorrelationIdMiddleware.cs`

```csharp
public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() 
        ?? Guid.NewGuid().ToString();

    context.Items["CorrelationId"] = correlationId;

    using (logger.BeginScope(new Dictionary<string, object>
    {
        ["CorrelationId"] = correlationId
    }))
    {
        await _next(context);
    }
}
```

### 5. Service-to-Service Propagation

**File**: `src/Services/LoanService/Clients/CatalogServiceClient.cs`

```csharp
private readonly IHttpContextAccessor _httpContextAccessor;

private void PropagateCorrelationId()
{
    var correlationId = _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault();
    
    if (!string.IsNullOrEmpty(correlationId))
    {
        _httpClient.DefaultRequestHeaders.Remove("X-Correlation-ID");
        _httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
    }
}

public async Task<BookResponse> GetBookAsync(int bookId)
{
    PropagateCorrelationId();
    
    var catalogServiceUrl = await _serviceDiscovery.GetServiceUrlAsync("catalogservice");
    var response = await _httpClient.GetAsync($"{catalogServiceUrl}/api/books/{bookId}");
    
    return await response.Content.ReadFromJsonAsync<BookResponse>();
}
```

## Complete Flow Example: Borrow Book

```
┌─────────────┐
│  Frontend   │  Generate ID: req-1699123930-abc123
└──────┬──────┘
       │ POST /api/loans
       │ X-Correlation-ID: req-1699123930-abc123
       ▼
┌─────────────────────┐
│   API Gateway       │  Log: 📨 [REQUEST] POST /api/loans
│                     │  CorrelationId: req-1699123930-abc123
└──────┬──────────────┘
       │ POST http://loanservice:5003/api/loans
       │ X-Correlation-ID: req-1699123930-abc123
       ▼
┌─────────────────────┐
│   LoanService       │  Log: 📖 [SAGA-START] Starting borrow book saga
│                     │  CorrelationId: req-1699123930-abc123
└──────┬──────────────┘
       │ GET http://catalogservice:5001/api/books/5
       │ X-Correlation-ID: req-1699123930-abc123
       ▼
┌─────────────────────┐
│  CatalogService     │  Log: 📖 [GET-BOOK] Retrieving book | BookId: 5
│                     │  CorrelationId: req-1699123930-abc123
└─────────────────────┘
```

## Seq Log Aggregation

All logs are sent to Seq at `http://localhost:5341`.

**Filter by Correlation ID**:
```sql
CorrelationId == "req-1699123930-abc123"
```

**Example Output**:
```
[15:32:10 INF] [Gateway] 📨 [REQUEST] POST /api/loans | CorrelationId: req-1699123930-abc123
[15:32:10 INF] [Gateway] 🔍 [CONSUL-QUERY] Querying Consul for service: loanservice | CorrelationId: req-1699123930-abc123
[15:32:10 INF] [LoanService] 📨 [REQUEST] POST /api/loans | CorrelationId: req-1699123930-abc123
[15:32:10 INF] [LoanService] 📖 [SAGA-START] Starting borrow book saga | CorrelationId: req-1699123930-abc123
[15:32:10 INF] [LoanService] 🔍 [SERVICE-DISCOVERY] Querying Consul for catalogservice | CorrelationId: req-1699123930-abc123
[15:32:10 INF] [LoanService] 🔗 [INTER-SERVICE] Calling CatalogService: GET /api/books/5 | CorrelationId: req-1699123930-abc123
[15:32:10 INF] [CatalogService] 📨 [REQUEST] GET /api/books/5 | CorrelationId: req-1699123930-abc123
[15:32:10 INF] [CatalogService] 📖 [GET-BOOK] Retrieving book | BookId: 5 | CorrelationId: req-1699123930-abc123
[15:32:10 INF] [LoanService] ✅ [SAGA-SUCCESS] Borrow book saga completed | CorrelationId: req-1699123930-abc123
```

## Implementation Checklist

### Frontend
- [x] Generate unique Correlation ID
- [x] Add `X-Correlation-ID` header to all requests
- [x] Console log for debugging

### API Gateway
- [x] Capture Correlation ID from request
- [x] Add to logging scope
- [x] Auto-propagate via Ocelot

### Microservices
- [x] Capture Correlation ID from request
- [x] Add to logging scope
- [x] Register `CorrelationIdMiddleware`
- [x] Register `IHttpContextAccessor`

### Service-to-Service
- [x] Implement `PropagateCorrelationId()` method
- [x] Call before each HTTP request
- [x] Use `IHttpContextAccessor` to access incoming headers

### Logging
- [x] Configure Serilog with `Enrich.FromLogContext()`
- [x] Send logs to Seq
- [x] Include Correlation ID in all log messages

## Code Locations

| Component | File Path |
|-----------|-----------|
| **Generation** | `frontend/js/api-client.js` |
| **Gateway Middleware** | `src/Gateway/LibHub.Gateway.Api/Middleware/CorrelationIdMiddleware.cs` |
| **Gateway Registration** | `src/Gateway/LibHub.Gateway.Api/Program.cs` |
| **Service Middleware** | `src/Services/*/Middleware/CorrelationIdMiddleware.cs` |
| **Service Registration** | `src/Services/*/Program.cs` |
| **Service Propagation** | `src/Services/LoanService/Clients/CatalogServiceClient.cs` |
| **Serilog Config** | `src/Services/*/Program.cs` |

## Best Practices

### DO ✅
- Generate ID in frontend for all requests
- Capture ID in every middleware
- Propagate ID in all service-to-service calls
- Include ID in all log messages
- Use Seq to filter logs by Correlation ID

### DON'T ❌
- Hardcode Correlation ID
- Skip propagation in service-to-service calls
- Remove `IHttpContextAccessor` registration
- Forget to call `PropagateCorrelationId()` before HTTP requests
- Use different header names across services

## Troubleshooting

### Issue: Correlation ID missing in downstream service

**Cause**: Forgot to call `PropagateCorrelationId()` before HTTP request

**Solution**:
```csharp
public async Task<BookResponse> GetBookAsync(int bookId)
{
    PropagateCorrelationId();
    
    var response = await _httpClient.GetAsync($"{serviceUrl}/api/books/{bookId}");
}
```

### Issue: Correlation ID not in logs

**Cause**: Middleware not registered

**Solution**:
```csharp
app.UseMiddleware<CorrelationIdMiddleware>();
```

### Issue: Cannot access HttpContext in client

**Cause**: `IHttpContextAccessor` not registered

**Solution**:
```csharp
builder.Services.AddHttpContextAccessor();
```

## Related Documentation

- [CODEBASE_DEEP_DIVE.md](./CODEBASE_DEEP_DIVE.md) - Complete system flow with Correlation ID
- [LOGGING_CONCEPTS.md](../LOGGING_CONCEPTS.md) - Logging patterns and conventions
- [ARCHITECTURE_LAYER4_INTER_SERVICE_COMMUNICATION.md](../architecture/ARCHITECTURE_LAYER4_INTER_SERVICE_COMMUNICATION.md) - Service-to-service patterns