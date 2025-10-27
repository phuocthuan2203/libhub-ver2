# API Gateway - Ocelot Configuration

**Technology**: Ocelot API Gateway  
**Port**: 5000  
**Responsibilities**: Request routing, JWT validation, single entry point for frontend  
**Note**: Gateway is configuration-driven - NO Clean Architecture layers (no business logic)

---

## Project Structure

```
LibHub.Gateway.Api/
├── Program.cs
├── ocelot.json
└── appsettings.json
```

**Simple ASP.NET Core Empty project** - just middleware configuration.

---

## Program.cs Setup

```
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add Ocelot configuration
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Add Ocelot services
builder.Services.AddOcelot(builder.Configuration);

// Add JWT Authentication
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
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]))
        };
    });

// Add CORS for frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Use CORS
app.UseCors();

// Use Ocelot middleware
await app.UseOcelot();

app.Run();
```

---

## ocelot.json Configuration

```
{
  "Routes": [
    {
      "UpstreamPathTemplate": "/api/users/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete" ],
      "DownstreamPathTemplate": "/api/users/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "localhost",
          "Port": 5002
        }
      ],
      "ServiceName": "UserService",
      "RouteIsCaseSensitive": false
    },
    {
      "UpstreamPathTemplate": "/api/books/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete" ],
      "DownstreamPathTemplate": "/api/books/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "localhost",
          "Port": 5001
        }
      ],
      "ServiceName": "CatalogService",
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Bearer"
      },
      "RouteIsCaseSensitive": false
    },
    {
      "UpstreamPathTemplate": "/api/loans/{everything}",
      "UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete" ],
      "DownstreamPathTemplate": "/api/loans/{everything}",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "localhost",
          "Port": 5003
        }
      ],
      "ServiceName": "LoanService",
      "AuthenticationOptions": {
        "AuthenticationProviderKey": "Bearer"
      },
      "RouteIsCaseSensitive": false
    }
  ],
  "GlobalConfiguration": {
    "BaseUrl": "http://localhost:5000"
  }
}
```

---

## appsettings.json

```
{
  "Jwt": {
    "Issuer": "LibHub.UserService",
    "Audience": "LibHub.Clients",
    "SecretKey": "LibHub_SuperSecret_256BitKey_ForDevelopment_ChangeInProduction!"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  }
}
```

---

## Required NuGet Packages

```
<PackageReference Include="Ocelot" Version="20.0.*" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.*" />
```

---

## Routing Logic Explained

### Upstream vs Downstream

**Upstream**: What the client (frontend) calls
- Example: `http://localhost:5000/api/books/1`

**Downstream**: Where Gateway routes the request
- Example: `http://localhost:5001/api/books/1`

### Route Template Syntax

- `{everything}`: Catch-all parameter, matches any path after the base
- Example: `/api/books/{everything}` matches:
  - `/api/books` (empty)
  - `/api/books/1`
  - `/api/books/1/stock`
  - `/api/books?search=fiction`

### Authentication Options

**Routes WITHOUT AuthenticationOptions**: Public access (no JWT required)
- UserService routes (register, login)

**Routes WITH AuthenticationOptions**: JWT required
- CatalogService routes (except GET which should be public - handled by service)
- LoanService routes (all require authentication)

---

## How Gateway Works

### Request Flow

1. **Frontend** sends request to Gateway: `http://localhost:5000/api/loans`
2. **Gateway** checks `ocelot.json` for matching route
3. **Gateway** validates JWT token (if route requires authentication)
4. **Gateway** forwards request to downstream service: `http://localhost:5003/api/loans`
5. **Downstream service** processes request and returns response
6. **Gateway** forwards response back to frontend

### JWT Validation

Gateway validates:
- Token signature using secret key
- Token expiration
- Issuer matches configuration
- Audience matches configuration

If validation fails → **401 Unauthorized** returned to client

If validation succeeds → Request forwarded with JWT in Authorization header

---

## Advanced Configuration (Optional)

### Add Correlation ID Middleware

```
// In Program.cs, before app.UseOcelot()
app.Use(async (context, next) =>
{
    var correlationId = Guid.NewGuid().ToString();
    context.Request.Headers.Add("X-Correlation-ID", correlationId);
    context.Response.Headers.Add("X-Correlation-ID", correlationId);
    await next();
});
```

### Add Request/Response Logging

```
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation(
        "Gateway routing: {Method} {Path} to {Service}",
        context.Request.Method,
        context.Request.Path,
        context.Request.Headers["X-Forwarded-For"]);
    
    await next();
});
```

### Rate Limiting (Optional)

Add to route configuration:
```
{
  "RateLimitOptions": {
    "ClientWhitelist": [],
    "EnableRateLimiting": true,
    "Period": "1s",
    "PeriodTimespan": 1,
    "Limit": 10
  }
}
```

---

## Service Discovery (Production Enhancement)

For production, replace hardcoded ports with Consul:

```
{
  "DownstreamScheme": "http",
  "ServiceName": "UserService",
  "UseServiceDiscovery": true
}
```

And in GlobalConfiguration:
```
{
  "GlobalConfiguration": {
    "BaseUrl": "http://localhost:5000",
    "ServiceDiscoveryProvider": {
      "Host": "localhost",
      "Port": 8500,
      "Type": "Consul"
    }
  }
}
```

**Note**: Consul not required for this project - configuration-based is sufficient.

---

## Testing the Gateway

### Start All Services First
```
# Terminal 1 - UserService
cd ~/Projects/LibHub/src/Services/UserService/LibHub.UserService.Api
dotnet run

# Terminal 2 - CatalogService
cd ~/Projects/LibHub/src/Services/CatalogService/LibHub.CatalogService.Api
dotnet run

# Terminal 3 - LoanService
cd ~/Projects/LibHub/src/Services/LoanService/LibHub.LoanService.Api
dotnet run

# Terminal 4 - Gateway
cd ~/Projects/LibHub/src/Gateway/LibHub.Gateway.Api
dotnet run
```

### Test Routing with cURL

```
# Test public route (no JWT)
curl http://localhost:5000/api/books

# Test register (no JWT)
curl -X POST http://localhost:5000/api/users/register \
  -H "Content-Type: application/json" \
  -d '{"username":"test","email":"test@test.com","password":"Password123!"}'

# Test login and get token
curl -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@test.com","password":"Password123!"}'

# Test protected route (with JWT)
curl http://localhost:5000/api/loans \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

---

## Common Issues & Solutions

### Issue 1: 404 Not Found
**Cause**: Route not matching in ocelot.json  
**Solution**: Check UpstreamPathTemplate matches your request path

### Issue 2: 401 Unauthorized
**Cause**: JWT validation failing  
**Solution**: 
- Verify JWT secret key matches UserService
- Check token hasn't expired
- Ensure Issuer and Audience match

### Issue 3: Service Not Responding
**Cause**: Downstream service not running or wrong port  
**Solution**: 
- Verify service is running: `curl http://localhost:5001/api/books`
- Check DownstreamHostAndPorts configuration

### Issue 4: CORS Error in Frontend
**Cause**: CORS policy not configured  
**Solution**: Ensure `app.UseCors()` is called in Program.cs

---

## Key Implementation Notes

1. **Gateway Has No Business Logic**: It's purely routing and authentication
2. **JWT Secret Must Match**: Same key across Gateway, UserService, CatalogService, LoanService
3. **Start Services Before Gateway**: Gateway will fail health checks if services aren't running
4. **Upstream Path = Frontend URL**: This is what the frontend calls
5. **Downstream Path = Service URL**: This is where Gateway forwards the request
6. **{everything} Catches All**: Use this for flexible routing
7. **Public Routes**: Don't add AuthenticationOptions
8. **Protected Routes**: Add AuthenticationOptions with "Bearer" key
9. **CORS is Critical**: Frontend can't call Gateway without CORS enabled
10. **Configuration Hot Reload**: Changes to ocelot.json reload automatically

