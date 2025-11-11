# LibHub Codebase Deep Dive - Implementation Guide

This guide maps high-level architectural concepts to actual code implementations, helping you understand **which file and which code snippet** performs each function in the system.

## Table of Contents
1. [Frontend → API Gateway Communication](#1-frontend--api-gateway-communication)
2. [API Gateway → Consul Service Discovery](#2-api-gateway--consul-service-discovery)
3. [API Gateway → Downstream Services](#3-api-gateway--downstream-services)
4. [Service-to-Service Communication](#4-service-to-service-communication)
5. [Service Registration with Consul](#5-service-registration-with-consul)
6. [JWT Authentication Flow](#6-jwt-authentication-flow)
7. [Request Correlation & Tracing](#7-request-correlation--tracing)
8. [Complete End-to-End Flow Example](#8-complete-end-to-end-flow-example)

---

## 1. Frontend → API Gateway Communication

### Concept
Frontend (JavaScript) makes HTTP requests to the API Gateway at `http://localhost:5000` (or `http://nginx:8080` in Docker). The frontend has **no knowledge** of individual service addresses.

### Implementation

**File:** `frontend/js/api-client.js`

```javascript
const API_BASE_URL = '';

class ApiClient {
    constructor(baseUrl) {
        this.baseUrl = baseUrl;
    }
    
    async get(endpoint, requiresAuth = false) {
        const headers = { 'Content-Type': 'application/json' };
        
        const correlationId = this.generateCorrelationId();
        headers['X-Correlation-ID'] = correlationId;
        
        if (requiresAuth) {
            const token = localStorage.getItem('jwt_token');
            if (token) {
                headers['Authorization'] = `Bearer ${token}`;
            }
        }
        
        const response = await fetch(`${this.baseUrl}${endpoint}`, { 
            method: 'GET',
            headers 
        });
        
        return await response.json();
    }
    
    async post(endpoint, data, requiresAuth = false) {
        const headers = { 'Content-Type': 'application/json' };
        
        const correlationId = this.generateCorrelationId();
        headers['X-Correlation-ID'] = correlationId;
        
        if (requiresAuth) {
            const token = localStorage.getItem('jwt_token');
            if (token) {
                headers['Authorization'] = `Bearer ${token}`;
            }
        }
        
        const response = await fetch(`${this.baseUrl}${endpoint}`, {
            method: 'POST',
            headers,
            body: JSON.stringify(data)
        });
        
        return await response.json();
    }
}
```

**Key Points:**
- `API_BASE_URL = ''` → Relative path, resolved to API Gateway (via nginx reverse proxy in Docker)
- Adds `X-Correlation-ID` header for request tracing
- Adds `Authorization: Bearer <token>` header for authenticated requests
- Frontend calls endpoints like `/api/books`, `/api/users/login`, `/api/loans`

**Usage Example:** `frontend/js/auth.js`
```javascript
const apiClient = new ApiClient(API_BASE_URL);

async function login(email, password) {
    const response = await apiClient.post('/api/users/login', { email, password });
    if (response.token) {
        localStorage.setItem('jwt_token', response.token);
    }
}
```

---

## 2. API Gateway → Consul Service Discovery

### Concept
When the API Gateway receives a request (e.g., `GET /api/books`), it queries Consul to discover the actual address of the CatalogService.

### Implementation

**File:** `src/Gateway/LibHub.Gateway.Api/ocelot.json`

```json
{
  "Routes": [
    {
      "UpstreamPathTemplate": "/api/books",
      "UpstreamHttpMethod": [ "GET" ],
      "DownstreamPathTemplate": "/api/books",
      "DownstreamScheme": "http",
      "ServiceName": "catalogservice",
      "LoadBalancerOptions": {
        "Type": "RoundRobin"
      }
    }
  ],
  "GlobalConfiguration": {
    "ServiceDiscoveryProvider": {
      "Scheme": "http",
      "Host": "consul",
      "Port": 8500,
      "Type": "Consul"
    }
  }
}
```

**Key Points:**
- `UpstreamPathTemplate`: What the frontend sends (e.g., `/api/books`)
- `ServiceName`: The service name registered in Consul (e.g., `catalogservice`)
- `DownstreamPathTemplate`: The path to forward to the discovered service
- **NO hardcoded IP/port** for downstream services
- Ocelot queries Consul at `http://consul:8500` to resolve `catalogservice` to actual address

**File:** `src/Gateway/LibHub.Gateway.Api/ServiceDiscovery/LoggingConsulServiceDiscoveryProvider.cs`

This custom wrapper adds logging around Ocelot's Consul queries:

```csharp
public class LoggingConsulServiceDiscoveryProvider : IServiceDiscoveryProvider
{
    private readonly IServiceDiscoveryProvider _innerProvider;
    private readonly ILogger<LoggingConsulServiceDiscoveryProvider> _logger;
    private readonly string _serviceName;

    public async Task<List<Service>> GetAsync()
    {
        _logger.LogInformation(
            "🔍 [CONSUL-QUERY] Querying Consul for service: {ServiceName}",
            _serviceName);

        var services = await _innerProvider.GetAsync();

        if (services == null || services.Count == 0)
        {
            _logger.LogWarning(
                "⚠️ [CONSUL-RESPONSE] No instances found for service: {ServiceName}",
                _serviceName);
            return services ?? new List<Service>();
        }

        _logger.LogInformation(
            "📍 [CONSUL-RESPONSE] Found {Count} instance(s) for service: {ServiceName} | Instances: {Instances}",
            services.Count,
            _serviceName,
            string.Join(", ", services.Select(s => $"{s.HostAndPort.DownstreamHost}:{s.HostAndPort.DownstreamPort}")));

        return services;
    }
}
```

**Key Points:**
- Wraps Ocelot's default Consul provider
- Logs each Consul query and response
- Returns list of healthy service instances (host + port)

**File:** `src/Gateway/LibHub.Gateway.Api/Program.cs`

```csharp
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services
    .AddOcelot(builder.Configuration)
    .AddConsul()
    .AddPolly();

builder.Services.AddSingleton<IServiceDiscoveryProviderFactory, LoggingServiceDiscoveryProviderFactory>();

var app = builder.Build();
await app.UseOcelot();
```

**Key Points:**
- `AddConsul()`: Enables Consul-based service discovery
- `AddPolly()`: Adds retry/circuit breaker policies
- Custom factory injects logging provider

---

## 3. API Gateway → Downstream Services

### Concept
After discovering the service address from Consul, the API Gateway forwards the request to the actual downstream service (e.g., `http://catalogservice:5001/api/books`).

### Implementation

**File:** `src/Gateway/LibHub.Gateway.Api/ocelot.json` (continued)

```json
{
  "UpstreamPathTemplate": "/api/books",
  "DownstreamPathTemplate": "/api/books",
  "DownstreamScheme": "http",
  "ServiceName": "catalogservice",
  "LoadBalancerOptions": {
    "Type": "RoundRobin"
  },
  "QoSOptions": {
    "ExceptionsAllowedBeforeBreaking": 3,
    "DurationOfBreak": 5000,
    "TimeoutValue": 10000
  },
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  }
}
```

**Key Points:**
- `DownstreamScheme`: Protocol to use (http/https)
- `QoSOptions`: Circuit breaker and timeout configuration
- `AuthenticationOptions`: Validates JWT token before forwarding
- Ocelot automatically:
  1. Queries Consul for `catalogservice` address
  2. Gets `http://catalogservice:5001`
  3. Forwards request to `http://catalogservice:5001/api/books`
  4. Propagates headers (`Authorization`, `X-Correlation-ID`)

**File:** `src/Gateway/LibHub.Gateway.Api/Program.cs` (JWT Validation)

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
                var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var role = context.Principal?.FindFirst(ClaimTypes.Role)?.Value;
                
                logger.LogInformation(
                    "✅ [JWT-SUCCESS] Token validated | UserId: {UserId} | Role: {Role}",
                    userId, role);
                
                return Task.CompletedTask;
            }
        };
    });
```

**Key Points:**
- Gateway validates JWT **before** forwarding to downstream services
- Downstream services trust the gateway's validation
- JWT claims (UserId, Role) are available in the forwarded request

---

## 4. Service-to-Service Communication

### Concept
LoanService needs to call CatalogService to check book availability and decrement stock. It queries Consul to get CatalogService's address, then makes an HTTP call.

### Implementation

**Step 1: Define Service Discovery Interface**

**File:** `src/Services/LoanService/Services/IServiceDiscovery.cs`

```csharp
public interface IServiceDiscovery
{
    Task<string> GetServiceUrlAsync(string serviceName);
}
```

**Step 2: Implement Consul-based Service Discovery**

**File:** `src/Services/LoanService/Services/ConsulServiceDiscovery.cs`

```csharp
public class ConsulServiceDiscovery : IServiceDiscovery
{
    private readonly IConsulClient _consulClient;
    private readonly ILogger<ConsulServiceDiscovery> _logger;

    public ConsulServiceDiscovery(IConsulClient consulClient, ILogger<ConsulServiceDiscovery> logger)
    {
        _consulClient = consulClient;
        _logger = logger;
    }

    public async Task<string> GetServiceUrlAsync(string serviceName)
    {
        _logger.LogInformation("🔍 [SERVICE-DISCOVERY] Querying Consul for service: {ServiceName}", serviceName);

        var services = await _consulClient.Health.Service(serviceName, tag: null, passingOnly: true);

        if (services.Response == null || !services.Response.Any())
        {
            _logger.LogError("❌ [SERVICE-DISCOVERY] No healthy instances found for service: {ServiceName}", serviceName);
            throw new Exception($"Service '{serviceName}' not available in Consul");
        }

        var serviceEntry = services.Response.First();
        var service = serviceEntry.Service;
        var serviceUrl = $"http://{service.Address}:{service.Port}";

        _logger.LogInformation(
            "✅ [SERVICE-DISCOVERY] Discovered service: {ServiceName} at {ServiceUrl} | ServiceId: {ServiceId}",
            serviceName,
            serviceUrl,
            service.ID);

        return serviceUrl;
    }
}
```

**Key Points:**
- Queries Consul API: `_consulClient.Health.Service(serviceName, passingOnly: true)`
- `passingOnly: true` → Only returns healthy instances
- Returns first healthy instance URL (e.g., `http://catalogservice:5001`)

**Step 3: Create HTTP Client for CatalogService**

**File:** `src/Services/LoanService/Clients/CatalogServiceClient.cs`

```csharp
public class CatalogServiceClient : ICatalogServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly IServiceDiscovery _serviceDiscovery;
    private readonly ILogger<CatalogServiceClient> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CatalogServiceClient(
        HttpClient httpClient, 
        IServiceDiscovery serviceDiscovery,
        ILogger<CatalogServiceClient> logger, 
        IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _serviceDiscovery = serviceDiscovery;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

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
        try
        {
            PropagateCorrelationId();
            
            var catalogServiceUrl = await _serviceDiscovery.GetServiceUrlAsync("catalogservice");
            
            _logger.LogInformation("🔗 [INTER-SERVICE] Calling CatalogService at {ServiceUrl}: GET /api/books/{BookId}", 
                catalogServiceUrl, bookId);

            var response = await _httpClient.GetAsync($"{catalogServiceUrl}/api/books/{bookId}");
            
            response.EnsureSuccessStatusCode();
            
            var book = await response.Content.ReadFromJsonAsync<BookResponse>();
            
            _logger.LogInformation("✅ [INTER-SERVICE] Successfully retrieved book {BookId} from CatalogService", bookId);
            
            return book;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [INTER-SERVICE] Failed to get book {BookId} from CatalogService", bookId);
            throw;
        }
    }

    public async Task DecrementStockAsync(int bookId)
    {
        try
        {
            PropagateCorrelationId();
            
            var catalogServiceUrl = await _serviceDiscovery.GetServiceUrlAsync("catalogservice");
            
            _logger.LogInformation("🔗 [INTER-SERVICE] Calling CatalogService at {ServiceUrl}: PUT /api/books/{BookId}/decrement-stock", 
                catalogServiceUrl, bookId);

            var response = await _httpClient.PutAsync($"{catalogServiceUrl}/api/books/{bookId}/decrement-stock", null);
            
            response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("✅ [INTER-SERVICE] Successfully decremented stock for book {BookId}", bookId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [INTER-SERVICE] Failed to decrement stock for book {BookId}", bookId);
            throw;
        }
    }
}
```

**Key Points:**
- Calls `_serviceDiscovery.GetServiceUrlAsync("catalogservice")` to get address from Consul
- Constructs full URL: `{serviceUrl}/api/books/{bookId}`
- Propagates `X-Correlation-ID` header for tracing
- No hardcoded service addresses

**Step 4: Use in Saga Orchestrator**

**File:** `src/Services/LoanService/Services/LoanService.cs` (Saga implementation)

```csharp
public async Task<LoanResponse> BorrowBookAsync(int userId, BorrowBookRequest request)
{
    var loan = new Loan
    {
        UserId = userId,
        BookId = request.BookId,
        CheckoutDate = DateTime.UtcNow,
        DueDate = DateTime.UtcNow.AddDays(14),
        Status = LoanStatus.PENDING
    };

    await _loanRepository.AddAsync(loan);
    await _loanRepository.SaveChangesAsync();

    try
    {
        _logger.LogInformation("📖 [SAGA-START] Starting borrow book saga | LoanId: {LoanId} | BookId: {BookId}", 
            loan.LoanId, request.BookId);

        var book = await _catalogServiceClient.GetBookAsync(request.BookId);

        if (book.AvailableCopies <= 0)
        {
            throw new InvalidOperationException("Book is not available");
        }

        await _catalogServiceClient.DecrementStockAsync(request.BookId);

        loan.Status = LoanStatus.CheckedOut;
        await _loanRepository.SaveChangesAsync();

        _logger.LogInformation("✅ [SAGA-SUCCESS] Borrow book saga completed | LoanId: {LoanId}", loan.LoanId);

        return loan.ToResponse();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "❌ [SAGA-FAILED] Borrow book saga failed | LoanId: {LoanId}", loan.LoanId);

        loan.Status = LoanStatus.FAILED;
        await _loanRepository.SaveChangesAsync();

        throw;
    }
}
```

**Key Points:**
- Saga orchestrator uses `_catalogServiceClient` to call CatalogService
- Client internally queries Consul for service address
- Implements compensating transaction pattern (mark loan as FAILED on error)

---

## 5. Service Registration with Consul

### Concept
Each microservice registers itself with Consul on startup, including a health check endpoint. Consul periodically pings the health check to determine if the service is healthy.

### Implementation

**File:** `src/Services/UserService/Extensions/ConsulServiceRegistration.cs`

```csharp
public static class ConsulServiceRegistration
{
    public static IServiceCollection AddConsulServiceRegistration(this IServiceCollection services, IConfiguration configuration)
    {
        var consulHost = configuration["Consul:Host"] ?? "localhost";
        var consulPort = int.Parse(configuration["Consul:Port"] ?? "8500");

        services.AddSingleton<IConsulClient>(p => new ConsulClient(config =>
        {
            config.Address = new Uri($"http://{consulHost}:{consulPort}");
        }));

        return services;
    }

    public static IApplicationBuilder UseConsulServiceRegistration(this IApplicationBuilder app, IConfiguration configuration, IHostApplicationLifetime lifetime)
    {
        var consulClient = app.ApplicationServices.GetRequiredService<IConsulClient>();
        var logger = app.ApplicationServices.GetRequiredService<ILogger<IConsulClient>>();
        
        var serviceName = configuration["ServiceConfig:ServiceName"] ?? "userservice";
        var serviceHost = configuration["ServiceConfig:ServiceHost"] ?? "localhost";
        var servicePort = int.Parse(configuration["ServiceConfig:ServicePort"] ?? "5002");
        var serviceId = $"{serviceName}-{Guid.NewGuid()}";

        var registration = new AgentServiceRegistration
        {
            ID = serviceId,
            Name = serviceName,
            Address = serviceHost,
            Port = servicePort,
            Check = new AgentServiceCheck
            {
                HTTP = $"http://{serviceHost}:{servicePort}/health",
                Interval = TimeSpan.FromSeconds(10),
                Timeout = TimeSpan.FromSeconds(5),
                DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1)
            }
        };

        Task.Run(async () =>
        {
            await RegisterWithRetryAsync(consulClient, registration, logger, serviceId, serviceName);
        });

        return app;
    }

    private static async Task RegisterWithRetryAsync(IConsulClient consulClient, AgentServiceRegistration registration, ILogger logger, string serviceId, string serviceName)
    {
        for (int attempt = 0; attempt < RetryDelays.Length; attempt++)
        {
            try
            {
                await consulClient.Agent.ServiceRegister(registration);
                logger.LogInformation("✅ [CONSUL] Service registered successfully | ServiceId: {ServiceId} | ServiceName: {ServiceName}", 
                    serviceId, serviceName);
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "⚠️ [CONSUL] Registration attempt {Attempt} failed, retrying...", attempt + 1);
                await Task.Delay(RetryDelays[attempt]);
            }
        }
        
        logger.LogError("❌ [CONSUL] Failed to register service after all retry attempts");
    }
}
```

**Key Points:**
- Creates `IConsulClient` configured to talk to Consul at `http://consul:8500`
- Registers service with:
  - `Name`: Service name (e.g., `userservice`)
  - `Address`: Container hostname (e.g., `userservice`)
  - `Port`: Service port (e.g., `5002`)
  - `Check.HTTP`: Health check URL (e.g., `http://userservice:5002/health`)
  - `Check.Interval`: How often Consul checks health (10 seconds)
  - `Check.Timeout`: How long to wait for health response (5 seconds)
  - `Check.DeregisterCriticalServiceAfter`: Auto-remove service after 1 minute of failures
- Implements retry logic with exponential backoff

**File:** `src/Services/UserService/Program.cs`

```csharp
builder.Services.AddConsulServiceRegistration(builder.Configuration);

var app = builder.Build();

app.MapHealthChecks("/health");

app.UseConsulServiceRegistration(builder.Configuration, app.Lifetime);

app.Run();
```

**Key Points:**
- Registers Consul client in DI container
- Exposes `/health` endpoint for Consul health checks
- Registers service with Consul on startup

**File:** `src/Services/UserService/appsettings.json`

```json
{
  "Consul": {
    "Host": "consul",
    "Port": "8500"
  },
  "ServiceConfig": {
    "ServiceName": "userservice",
    "ServiceHost": "userservice",
    "ServicePort": "5002"
  }
}
```

**Key Points:**
- `ServiceHost`: Must match Docker container name for inter-container communication
- Same pattern used in CatalogService and LoanService

---

## 6. JWT Authentication Flow

### Concept
User logs in → UserService generates JWT → Frontend stores token → Frontend includes token in requests → API Gateway validates token → Downstream services trust gateway.

### Implementation

**Step 1: User Login**

**File:** `src/Services/UserService/Controllers/UsersController.cs`

```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
{
    try
    {
        _logger.LogInformation(
            "🔐 [LOGIN-ATTEMPT] Login attempt | Email: {Email}", 
            request.Email);
        
        var response = await _userService.LoginAsync(request);
        
        _logger.LogInformation(
            "✅ [LOGIN-SUCCESS] User logged in successfully | Email: {Email} | UserId: {UserId}", 
            request.Email, response.User.UserId);
        
        return Ok(response);
    }
    catch (UnauthorizedAccessException ex)
    {
        _logger.LogWarning(
            "❌ [LOGIN-FAILED] Invalid credentials | Email: {Email}", 
            request.Email);
        return Unauthorized(new { message = ex.Message });
    }
}
```

**Step 2: Generate JWT Token**

**File:** `src/Services/UserService/Security/JwtTokenGenerator.cs`

```csharp
public class JwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

**Key Points:**
- Creates JWT with claims: UserId, Email, Role
- Signs with HMAC-SHA256 using secret key
- Token expires in 1 hour

**Step 3: Frontend Stores Token**

**File:** `frontend/js/auth.js`

```javascript
async function handleLogin(event) {
    event.preventDefault();
    
    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;

    try {
        const response = await apiClient.post('/api/users/login', { email, password });
        
        if (response.token) {
            localStorage.setItem('jwt_token', response.token);
            localStorage.setItem('user_info', JSON.stringify(response.user));
            window.location.href = 'index.html';
        }
    } catch (error) {
        showError('Login failed. Please check your credentials.');
    }
}
```

**Step 4: Frontend Includes Token in Requests**

**File:** `frontend/js/api-client.js`

```javascript
async post(endpoint, data, requiresAuth = false) {
    const headers = { 'Content-Type': 'application/json' };
    
    if (requiresAuth) {
        const token = localStorage.getItem('jwt_token');
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }
    }
    
    const response = await fetch(`${this.baseUrl}${endpoint}`, {
        method: 'POST',
        headers,
        body: JSON.stringify(data)
    });
    
    return await response.json();
}
```

**Step 5: Gateway Validates Token**

**File:** `src/Gateway/LibHub.Gateway.Api/Program.cs`

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
    });
```

**File:** `src/Gateway/LibHub.Gateway.Api/ocelot.json`

```json
{
  "UpstreamPathTemplate": "/api/loans",
  "AuthenticationOptions": {
    "AuthenticationProviderKey": "Bearer"
  }
}
```

**Key Points:**
- Gateway validates JWT signature, issuer, audience, expiration
- Routes with `AuthenticationOptions` require valid JWT
- Invalid/missing token → 401 Unauthorized response

**Step 6: Downstream Services Extract Claims**

**File:** `src/Services/LoanService/Controllers/LoansController.cs`

```csharp
[ApiController]
[Route("api/loans")]
[Authorize]
public class LoansController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> BorrowBook(BorrowBookRequest request)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { message = "Invalid user token" });
        }

        var loan = await _loanService.BorrowBookAsync(userId, request);
        return CreatedAtAction(nameof(GetLoanById), new { id = loan.LoanId }, loan);
    }
}
```

**Key Points:**
- `[Authorize]` attribute requires authenticated user
- `User.FindFirstValue(ClaimTypes.NameIdentifier)` extracts UserId from JWT claims
- Downstream services don't re-validate JWT (trust the gateway)

---

## 7. Request Correlation & Tracing

### Concept
Every request gets a unique `X-Correlation-ID` header that is propagated across all service calls, allowing you to trace a single user request through the entire system in logs.

### Implementation

**Step 1: Frontend Generates Correlation ID**

**File:** `frontend/js/api-client.js`

```javascript
generateCorrelationId() {
    return `req-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
}

async get(endpoint, requiresAuth = false) {
    const headers = { 'Content-Type': 'application/json' };
    
    const correlationId = this.generateCorrelationId();
    headers['X-Correlation-ID'] = correlationId;
    console.log(`🔍 Track request: ${correlationId} - GET ${endpoint}`);
    
    const response = await fetch(`${this.baseUrl}${endpoint}`, { 
        method: 'GET',
        headers 
    });
}
```

**Step 2: Gateway Captures and Propagates**

**File:** `src/Gateway/LibHub.Gateway.Api/Middleware/CorrelationIdMiddleware.cs`

```csharp
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILogger<CorrelationIdMiddleware> logger)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault() 
            ?? Guid.NewGuid().ToString();

        context.Request.Headers[CorrelationIdHeader] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            logger.LogInformation("📨 [REQUEST] {Method} {Path} | CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                correlationId);

            await _next(context);
        }
    }
}
```

**Key Points:**
- Extracts `X-Correlation-ID` from request header
- If missing, generates new GUID
- Adds to logging scope (all subsequent logs include CorrelationId)
- Ocelot automatically propagates header to downstream services

**Step 3: Downstream Services Capture**

**File:** `src/Services/UserService/Middleware/CorrelationIdMiddleware.cs`

Same implementation as Gateway - extracts and adds to logging scope.

**Step 4: Service-to-Service Propagation**

**File:** `src/Services/LoanService/Clients/CatalogServiceClient.cs`

```csharp
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

**Key Points:**
- Extracts `X-Correlation-ID` from incoming request
- Adds to outgoing HTTP call to CatalogService
- Enables end-to-end tracing

**Step 5: View in Seq**

All services log with Serilog, sending logs to Seq at `http://seq:5341`. You can filter by `CorrelationId` to see all logs for a single request:

```
[15:32:10 INF] [Gateway] [req-1699123930-abc123] 📨 [REQUEST] POST /api/loans
[15:32:10 INF] [LoanService] [req-1699123930-abc123] 📖 [SAGA-START] Starting borrow book saga
[15:32:10 INF] [LoanService] [req-1699123930-abc123] 🔍 [SERVICE-DISCOVERY] Querying Consul for service: catalogservice
[15:32:10 INF] [LoanService] [req-1699123930-abc123] 🔗 [INTER-SERVICE] Calling CatalogService at http://catalogservice:5001: GET /api/books/5
[15:32:10 INF] [CatalogService] [req-1699123930-abc123] 📖 [GET-BOOK] Retrieving book | BookId: 5
[15:32:10 INF] [LoanService] [req-1699123930-abc123] ✅ [SAGA-SUCCESS] Borrow book saga completed
```

---

## 8. Complete End-to-End Flow Example

### Use Case: Borrow a Book

Let's trace the complete flow from frontend click to database update.

#### Step 1: User Clicks "Borrow" Button

**File:** `frontend/book-detail.html`

```html
<button id="borrow-button" onclick="borrowBook()">Borrow Book</button>
```

**File:** `frontend/js/book-detail.js` (hypothetical)

```javascript
async function borrowBook() {
    const bookId = getBookIdFromUrl();
    const apiClient = new ApiClient(API_BASE_URL);
    
    try {
        const response = await apiClient.post('/api/loans', 
            { bookId }, 
            requiresAuth: true);
        
        alert('Book borrowed successfully!');
    } catch (error) {
        alert('Failed to borrow book');
    }
}
```

**What happens:**
- JavaScript calls `apiClient.post('/api/loans', { bookId }, true)`
- `api-client.js` generates CorrelationId: `req-1699123930-abc123`
- Adds `Authorization: Bearer <token>` header
- Sends `POST http://localhost:5000/api/loans` with body `{ "bookId": 5 }`

#### Step 2: API Gateway Receives Request

**File:** `src/Gateway/LibHub.Gateway.Api/Middleware/CorrelationIdMiddleware.cs`

```
[15:32:10 INF] [Gateway] [req-1699123930-abc123] 📨 [REQUEST] POST /api/loans
```

**File:** `src/Gateway/LibHub.Gateway.Api/Program.cs` (JWT validation)

```
[15:32:10 INF] [Gateway] [req-1699123930-abc123] ✅ [JWT-SUCCESS] Token validated | UserId: 1 | Role: Customer
```

#### Step 3: Gateway Queries Consul

**File:** `src/Gateway/LibHub.Gateway.Api/ocelot.json`

```json
{
  "UpstreamPathTemplate": "/api/loans",
  "DownstreamPathTemplate": "/api/loans",
  "ServiceName": "loanservice"
}
```

**File:** `src/Gateway/LibHub.Gateway.Api/ServiceDiscovery/LoggingConsulServiceDiscoveryProvider.cs`

```
[15:32:10 INF] [Gateway] [req-1699123930-abc123] 🔍 [CONSUL-QUERY] Querying Consul for service: loanservice
[15:32:10 INF] [Gateway] [req-1699123930-abc123] 📍 [CONSUL-RESPONSE] Found 1 instance(s) for service: loanservice | Instances: loanservice:5003
```

**What happens:**
- Ocelot queries Consul HTTP API: `GET http://consul:8500/v1/health/service/loanservice?passing=true`
- Consul returns: `{ "Service": { "Address": "loanservice", "Port": 5003 } }`
- Ocelot resolves downstream URL: `http://loanservice:5003`

#### Step 4: Gateway Forwards to LoanService

**What happens:**
- Ocelot forwards: `POST http://loanservice:5003/api/loans`
- Includes headers:
  - `Authorization: Bearer <token>`
  - `X-Correlation-ID: req-1699123930-abc123`

#### Step 5: LoanService Receives Request

**File:** `src/Services/LoanService/Middleware/CorrelationIdMiddleware.cs`

```
[15:32:10 INF] [LoanService] [req-1699123930-abc123] 📨 [REQUEST] POST /api/loans
```

**File:** `src/Services/LoanService/Controllers/LoansController.cs`

```csharp
[HttpPost]
public async Task<IActionResult> BorrowBook(BorrowBookRequest request)
{
    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier); // "1"
    var userId = int.Parse(userIdClaim);
    
    var loan = await _loanService.BorrowBookAsync(userId, request);
    return CreatedAtAction(nameof(GetLoanById), new { id = loan.LoanId }, loan);
}
```

**What happens:**
- Extracts UserId from JWT claims: `1`
- Calls `_loanService.BorrowBookAsync(1, { BookId: 5 })`

#### Step 6: Saga Orchestration Begins

**File:** `src/Services/LoanService/Services/LoanService.cs`

```csharp
public async Task<LoanResponse> BorrowBookAsync(int userId, BorrowBookRequest request)
{
    var loan = new Loan
    {
        UserId = userId,
        BookId = request.BookId,
        CheckoutDate = DateTime.UtcNow,
        DueDate = DateTime.UtcNow.AddDays(14),
        Status = LoanStatus.PENDING
    };

    await _loanRepository.AddAsync(loan);
    await _loanRepository.SaveChangesAsync();
```

**Log:**
```
[15:32:10 INF] [LoanService] [req-1699123930-abc123] 📖 [SAGA-START] Starting borrow book saga | LoanId: 10 | BookId: 5
```

**What happens:**
- Creates Loan entity with status `PENDING`
- Saves to `loan_db` database

#### Step 7: LoanService Queries Consul for CatalogService

**File:** `src/Services/LoanService/Services/ConsulServiceDiscovery.cs`

```
[15:32:10 INF] [LoanService] [req-1699123930-abc123] 🔍 [SERVICE-DISCOVERY] Querying Consul for service: catalogservice
[15:32:10 INF] [LoanService] [req-1699123930-abc123] ✅ [SERVICE-DISCOVERY] Discovered service: catalogservice at http://catalogservice:5001
```

**What happens:**
- Calls Consul API: `GET http://consul:8500/v1/health/service/catalogservice?passing=true`
- Gets response: `http://catalogservice:5001`

#### Step 8: LoanService Calls CatalogService (Check Availability)

**File:** `src/Services/LoanService/Clients/CatalogServiceClient.cs`

```
[15:32:10 INF] [LoanService] [req-1699123930-abc123] 🔗 [INTER-SERVICE] Calling CatalogService at http://catalogservice:5001: GET /api/books/5
```

**What happens:**
- HTTP call: `GET http://catalogservice:5001/api/books/5`
- Includes `X-Correlation-ID: req-1699123930-abc123` header

#### Step 9: CatalogService Processes Request

**File:** `src/Services/CatalogService/Controllers/BooksController.cs`

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetBookById(int id)
{
    var book = await _bookService.GetBookByIdAsync(id);
    return Ok(book);
}
```

**Log:**
```
[15:32:10 INF] [CatalogService] [req-1699123930-abc123] 📖 [GET-BOOK] Retrieving book | BookId: 5
[15:32:10 INF] [CatalogService] [req-1699123930-abc123] ✅ [GET-BOOK-SUCCESS] Book retrieved | Title: Clean Code | AvailableCopies: 3
```

**What happens:**
- Queries `catalog_db`: `SELECT * FROM Books WHERE BookId = 5`
- Returns: `{ "bookId": 5, "title": "Clean Code", "availableCopies": 3 }`

#### Step 10: LoanService Validates Stock

**File:** `src/Services/LoanService/Services/LoanService.cs`

```csharp
var book = await _catalogServiceClient.GetBookAsync(request.BookId);

if (book.AvailableCopies <= 0)
{
    throw new InvalidOperationException("Book is not available");
}
```

**What happens:**
- Checks `availableCopies: 3` (> 0, so proceed)

#### Step 11: LoanService Calls CatalogService (Decrement Stock)

**File:** `src/Services/LoanService/Clients/CatalogServiceClient.cs`

```
[15:32:10 INF] [LoanService] [req-1699123930-abc123] 🔗 [INTER-SERVICE] Calling CatalogService at http://catalogservice:5001: PUT /api/books/5/decrement-stock
```

**What happens:**
- HTTP call: `PUT http://catalogservice:5001/api/books/5/decrement-stock`

#### Step 12: CatalogService Decrements Stock

**File:** `src/Services/CatalogService/Controllers/BooksController.cs`

```csharp
[HttpPut("{id}/decrement-stock")]
public async Task<IActionResult> DecrementStock(int id)
{
    await _bookService.DecrementStockAsync(id);
    return NoContent();
}
```

**File:** `src/Services/CatalogService/Services/BookService.cs`

```csharp
public async Task DecrementStockAsync(int bookId)
{
    var book = await _bookRepository.GetByIdAsync(bookId);
    book.AvailableCopies--;
    await _bookRepository.SaveChangesAsync();
}
```

**Log:**
```
[15:32:10 INF] [CatalogService] [req-1699123930-abc123] 📉 [DECREMENT-STOCK] Decrementing stock | BookId: 5 | Before: 3 | After: 2
```

**What happens:**
- Updates `catalog_db`: `UPDATE Books SET AvailableCopies = 2 WHERE BookId = 5`
- Returns `204 No Content`

#### Step 13: LoanService Completes Saga

**File:** `src/Services/LoanService/Services/LoanService.cs`

```csharp
await _catalogServiceClient.DecrementStockAsync(request.BookId);

loan.Status = LoanStatus.CheckedOut;
await _loanRepository.SaveChangesAsync();
```

**Log:**
```
[15:32:10 INF] [LoanService] [req-1699123930-abc123] ✅ [SAGA-SUCCESS] Borrow book saga completed | LoanId: 10
```

**What happens:**
- Updates `loan_db`: `UPDATE Loans SET Status = 'CheckedOut' WHERE LoanId = 10`
- Returns loan response to controller

#### Step 14: Response Flows Back to Frontend

**File:** `src/Services/LoanService/Controllers/LoansController.cs`

```csharp
return CreatedAtAction(nameof(GetLoanById), new { id = loan.LoanId }, loan);
```

**What happens:**
- Returns `201 Created` with body:
```json
{
  "loanId": 10,
  "userId": 1,
  "bookId": 5,
  "checkoutDate": "2025-11-11T15:32:10Z",
  "dueDate": "2025-11-25T15:32:10Z",
  "status": "CheckedOut"
}
```

#### Step 15: API Gateway Forwards Response to Frontend

**What happens:**
- Ocelot forwards `201 Created` response to frontend
- Frontend receives response and shows success message

#### Step 16: View Complete Flow in Seq

Open `http://localhost:5341` and filter by `CorrelationId == "req-1699123930-abc123"`:

```
[15:32:10 INF] [Gateway] [req-1699123930-abc123] 📨 [REQUEST] POST /api/loans
[15:32:10 INF] [Gateway] [req-1699123930-abc123] ✅ [JWT-SUCCESS] Token validated | UserId: 1
[15:32:10 INF] [Gateway] [req-1699123930-abc123] 🔍 [CONSUL-QUERY] Querying Consul for service: loanservice
[15:32:10 INF] [Gateway] [req-1699123930-abc123] 📍 [CONSUL-RESPONSE] Found 1 instance for loanservice
[15:32:10 INF] [LoanService] [req-1699123930-abc123] 📨 [REQUEST] POST /api/loans
[15:32:10 INF] [LoanService] [req-1699123930-abc123] 📖 [SAGA-START] Starting borrow book saga | LoanId: 10
[15:32:10 INF] [LoanService] [req-1699123930-abc123] 🔍 [SERVICE-DISCOVERY] Querying Consul for catalogservice
[15:32:10 INF] [LoanService] [req-1699123930-abc123] 🔗 [INTER-SERVICE] Calling CatalogService: GET /api/books/5
[15:32:10 INF] [CatalogService] [req-1699123930-abc123] 📖 [GET-BOOK] Retrieving book | BookId: 5
[15:32:10 INF] [CatalogService] [req-1699123930-abc123] ✅ [GET-BOOK-SUCCESS] AvailableCopies: 3
[15:32:10 INF] [LoanService] [req-1699123930-abc123] 🔗 [INTER-SERVICE] Calling CatalogService: PUT /api/books/5/decrement-stock
[15:32:10 INF] [CatalogService] [req-1699123930-abc123] 📉 [DECREMENT-STOCK] Before: 3 | After: 2
[15:32:10 INF] [LoanService] [req-1699123930-abc123] ✅ [SAGA-SUCCESS] Borrow book saga completed
```

---

## Learning Path Recommendation

### Level 1: Understand Request Flow (30 minutes)
1. Read `frontend/js/api-client.js` → How frontend makes API calls
2. Read `src/Gateway/LibHub.Gateway.Api/ocelot.json` → How routes are configured
3. Read `src/Services/UserService/Controllers/UsersController.cs` → How controllers handle requests
4. **Exercise:** Add a `console.log` in `api-client.js` and a breakpoint in `UsersController.cs`. Register a new user and trace the flow.

### Level 2: Understand Service Discovery (45 minutes)
1. Read `src/Services/UserService/Extensions/ConsulServiceRegistration.cs` → How services register
2. Read `src/Gateway/LibHub.Gateway.Api/ServiceDiscovery/LoggingConsulServiceDiscoveryProvider.cs` → How gateway discovers services
3. Read `src/Services/LoanService/Services/ConsulServiceDiscovery.cs` → How services discover each other
4. **Exercise:** Open Consul UI (`http://localhost:8500`), stop a service, watch it disappear from the registry.

### Level 3: Understand Inter-Service Communication (45 minutes)
1. Read `src/Services/LoanService/Clients/CatalogServiceClient.cs` → How HTTP client calls other services
2. Read `src/Services/LoanService/Services/LoanService.cs` → How saga orchestrates calls
3. Read correlation ID propagation in all `CorrelationIdMiddleware.cs` files
4. **Exercise:** Borrow a book, find the CorrelationId in logs, trace all service calls in Seq.

### Level 4: Understand Authentication (30 minutes)
1. Read `src/Services/UserService/Security/JwtTokenGenerator.cs` → Token generation
2. Read `src/Gateway/LibHub.Gateway.Api/Program.cs` (JWT section) → Token validation
3. Read `src/Services/LoanService/Controllers/LoansController.cs` → Claims extraction
4. **Exercise:** Login, copy JWT from localStorage, paste into https://jwt.io, inspect claims.

### Level 5: Understand Data Flow (45 minutes)
1. Read entity models in `Models/Entities/` across all services
2. Read repository implementations in `Data/` folders
3. Read service layer business logic in `Services/` folders
4. **Exercise:** Add a book (as admin), trace SQL queries in logs, verify in MySQL database.

### Level 6: Hands-On Debugging (60 minutes)
1. Run entire stack: `docker compose up -d --build`
2. Open Seq: `http://localhost:5341`
3. Open Consul: `http://localhost:8500`
4. Open Frontend: `http://localhost:8080`
5. Perform all 6 use cases, correlate frontend actions with backend logs
6. Stop a service mid-request, observe circuit breaker behavior
7. Introduce a bug (e.g., wrong Consul service name), watch service discovery fail

---

## Quick Reference: Key Files by Function

| Function | File Path |
|----------|-----------|
| **Frontend API calls** | `frontend/js/api-client.js` |
| **Gateway routing config** | `src/Gateway/LibHub.Gateway.Api/ocelot.json` |
| **Gateway JWT validation** | `src/Gateway/LibHub.Gateway.Api/Program.cs` |
| **Gateway Consul discovery** | `src/Gateway/LibHub.Gateway.Api/ServiceDiscovery/LoggingConsulServiceDiscoveryProvider.cs` |
| **Service registration** | `src/Services/UserService/Extensions/ConsulServiceRegistration.cs` |
| **Service discovery (service-to-service)** | `src/Services/LoanService/Services/ConsulServiceDiscovery.cs` |
| **Inter-service HTTP client** | `src/Services/LoanService/Clients/CatalogServiceClient.cs` |
| **Saga orchestration** | `src/Services/LoanService/Services/LoanService.cs` |
| **JWT token generation** | `src/Services/UserService/Security/JwtTokenGenerator.cs` |
| **Correlation ID middleware** | `src/Services/UserService/Middleware/CorrelationIdMiddleware.cs` |
| **User controller** | `src/Services/UserService/Controllers/UsersController.cs` |
| **Book controller** | `src/Services/CatalogService/Controllers/BooksController.cs` |
| **Loan controller** | `src/Services/LoanService/Controllers/LoansController.cs` |
| **Database context** | `src/Services/UserService/Data/UserDbContext.cs` |
| **Repository pattern** | `src/Services/UserService/Data/UserRepository.cs` |
| **Business logic** | `src/Services/UserService/Services/UserService.cs` |

---

## Summary

This guide maps **every architectural concept** to **specific code files and implementations**:

✅ **Frontend → Gateway**: `api-client.js` makes HTTP calls to routes defined in `ocelot.json`

✅ **Gateway → Consul**: Ocelot queries Consul via `LoggingConsulServiceDiscoveryProvider.cs`

✅ **Gateway → Services**: Ocelot forwards requests to discovered service addresses

✅ **Service-to-Service**: LoanService uses `ConsulServiceDiscovery.cs` + `CatalogServiceClient.cs`

✅ **Service Registration**: `ConsulServiceRegistration.cs` registers services with health checks

✅ **JWT Flow**: `JwtTokenGenerator.cs` creates tokens, gateway validates, controllers extract claims

✅ **Request Tracing**: `CorrelationIdMiddleware.cs` + propagation in HTTP clients + Seq aggregation

Now you can navigate the codebase with confidence, knowing exactly where each piece of the architecture is implemented!
