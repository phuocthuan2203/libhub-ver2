# Task 5.1: Setup Ocelot API Gateway Project

**Phase**: 5 - API Gateway Implementation  
**Estimated Time**: 1 hour  
**Dependencies**: Phase 2-4 complete (all services ready)

---

## Objective

Create and configure basic Ocelot API Gateway project structure.

---

## Key Steps

### 1. Create Gateway Project

cd ~/Projects/LibHub/src/Gateway
dotnet new web -n LibHub.Gateway.Api
cd ~/Projects/LibHub
dotnet sln add src/Gateway/LibHub.Gateway.Api

text

### 2. Install Ocelot Package

cd src/Gateway/LibHub.Gateway.Api
dotnet add package Ocelot --version 20.0.*
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.*

text

### 3. Create ocelot.json

{
"Routes": [], // Will populate in Task 5.2
"GlobalConfiguration": {
"BaseUrl": "http://localhost:5000"
}
}

text

### 4. Basic Program.cs

using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add ocelot.json to configuration
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

// Add Ocelot
builder.Services.AddOcelot(builder.Configuration);

var app = builder.Build();

// Use Ocelot middleware
await app.UseOcelot();

app.Run();

text

### 5. Configure Port in appsettings.json

{
"Kestrel": {
"Endpoints": {
"Http": { "Url": "http://localhost:5000" }
}
}
}

text

---

## Verification

dotnet build
dotnet run

Should start on port 5000 without errors
text

---

## After Completion

Update **PROJECT_STATUS.md**:
✅ Task 5.1: Ocelot Gateway Setup (date)

Basic project structure created

Ocelot package installed

Runs on port 5000

text

**Next: Task 5.2** (Configure routing for all services)