# Task 5.3: Configure JWT Middleware in Gateway

**Phase**: 5 - API Gateway Implementation  
**Estimated Time**: 1 hour  
**Dependencies**: Task 5.2 (Routing configured)

---

## Objective

Add JWT authentication middleware to Gateway and configure protected routes.

---

## Key Implementation

### 1. Update Program.cs with JWT

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Add JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options => {
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

// Add CORS
builder.Services.AddCors(options => {
options.AddDefaultPolicy(policy => {
policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
});
});

builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

app.UseCors();
await app.UseOcelot();

app.Run();

text

### 2. Add JWT Config to appsettings.json

{
"Jwt": {
"SecretKey": "LibHub_SuperSecret_256BitKey_ForDevelopment_ChangeInProduction!",
"Issuer": "LibHub.UserService",
"Audience": "LibHub.Clients"
}
}

text

### 3. Update ocelot.json for Protected Routes

{
"Routes": [
// Public route (no auth)
{
"UpstreamPathTemplate": "/api/users/register",
"UpstreamHttpMethod": [ "Post" ],
"DownstreamPathTemplate": "/api/users/register",
"DownstreamScheme": "http",
"DownstreamHostAndPorts": [
{ "Host": "localhost", "Port": 5002 }
]
},
// Protected route (requires JWT)
{
"UpstreamPathTemplate": "/api/loans/{everything}",
"UpstreamHttpMethod": [ "Get", "Post", "Put" ],
"DownstreamPathTemplate": "/api/loans/{everything}",
"DownstreamScheme": "http",
"DownstreamHostAndPorts": [
{ "Host": "localhost", "Port": 5003 }
],
"AuthenticationOptions": {
"AuthenticationProviderKey": "Bearer"
}
}
]
}

text

---

## Testing JWT

1. Login to get token
curl -X POST http://localhost:5000/api/users/login
-H "Content-Type: application/json"
-d '{"email":"test@example.com","password":"Password123!"}'

2. Use token to access protected route
curl http://localhost:5000/api/loans
-H "Authorization: Bearer YOUR_TOKEN_HERE"

text

---

## After Completion

Update **PROJECT_STATUS.md**:
✅ Task 5.3: JWT Middleware in Gateway (date)

JWT authentication configured

Protected routes require Bearer token

Public routes (register, login) accessible without token

text

**Next: Task 5.4** (Integration testing with all services)