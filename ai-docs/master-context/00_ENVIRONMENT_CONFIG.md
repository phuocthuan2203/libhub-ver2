# LibHub - Environment Configuration

## Development Environment Setup

**Platform**: Ubuntu 25.10  
**Project Root**: `/home/thuannp4/development/LibHub/`  
**.NET Version**: 8.0  
**MySQL Version**: 8.0+ (using `caching_sha2_password`)

---

## MySQL Configuration

### Database Credentials
```
Host: localhost
Port: 3306
User: libhub_user
Password: LibHub@Dev2025
Auth Plugin: caching_sha2_password
```

### Connection Strings (Copy-Paste Ready)

**UserService** (user_db):
```
Server=localhost;Port=3306;Database=user_db;User=libhub_user;Password=LibHub@Dev2025;
```

**CatalogService** (catalog_db):
```
Server=localhost;Port=3306;Database=catalog_db;User=libhub_user;Password=LibHub@Dev2025;
```

**LoanService** (loan_db):
```
Server=localhost;Port=3306;Database=loan_db;User=libhub_user;Password=LibHub@Dev2025;
```

---

## Service Ports (Fixed - Do Not Change)

| Service | Port | URL |
|---------|------|-----|
| **API Gateway** | 5000 | `http://localhost:5000` |
| **CatalogService** | 5001 | `http://localhost:5001` |
| **UserService** | 5002 | `http://localhost:5002` |
| **LoanService** | 5003 | `http://localhost:5003` |

---

## JWT Configuration

```
{
  "Jwt": {
    "SecretKey": "LibHub_SuperSecret_256BitKey_ForDevelopment_ChangeInProduction!",
    "Issuer": "LibHub.UserService",
    "Audience": "LibHub.Clients",
    "ExpiryInHours": 1
  }
}
```

**⚠️ Important**: 
- Secret key must be at least 256 bits (32+ characters)
- Same key used across all services for JWT validation
- Change in production!

---

## Project Structure

```
~/Projects/LibHub/
├── src/
│   ├── Services/
│   │   ├── UserService/
│   │   │   ├── LibHub.UserService.Domain/
│   │   │   ├── LibHub.UserService.Application/
│   │   │   ├── LibHub.UserService.Infrastructure/
│   │   │   └── LibHub.UserService.Api/
│   │   ├── CatalogService/
│   │   │   ├── LibHub.CatalogService.Domain/
│   │   │   ├── LibHub.CatalogService.Application/
│   │   │   ├── LibHub.CatalogService.Infrastructure/
│   │   │   └── LibHub.CatalogService.Api/
│   │   └── LoanService/
│   │       ├── LibHub.LoanService.Domain/
│   │       ├── LibHub.LoanService.Application/
│   │       ├── LibHub.LoanService.Infrastructure/
│   │       └── LibHub.LoanService.Api/
│   └── Gateway/
│       └── LibHub.Gateway.Api/
├── frontend/
├── ai-docs/
└── LibHub.sln
```

---

## Service-to-Service Communication

**LoanService → CatalogService** (HTTP Client):
```
{
  "ExternalServices": {
    "CatalogServiceBaseUrl": "http://localhost:5001"
  }
}
```

---

## NuGet Packages (Common)

```
<!-- All services -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.*" />
<PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="8.0.*" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.*" />

<!-- UserService only -->
<PackageReference Include="BCrypt.Net-Next" Version="4.0.*" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="7.0.*" />

<!-- Gateway only -->
<PackageReference Include="Ocelot" Version="20.0.*" />
```

---

## Quick Commands

### Create Database Migration
```
cd ~/Projects/LibHub/src/Services/UserService/LibHub.UserService.Infrastructure
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Run Service
```
cd ~/Projects/LibHub/src/Services/UserService/LibHub.UserService.Api
dotnet run
```

### Test MySQL Connection
```
mysql -u libhub_user -p
# Password: LibHub@Dev2025
```

---

## Common appsettings.json Template

```
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=SERVICE_DB_NAME;User=libhub_user;Password=LibHub@Dev2025;"
  },
  "Jwt": {
    "Issuer": "LibHub.UserService",
    "Audience": "LibHub.Clients",
    "SecretKey": "LibHub_SuperSecret_256BitKey_ForDevelopment_ChangeInProduction!",
    "ExpiryInHours": 1
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
        "Url": "http://localhost:PORT_NUMBER"
      }
    }
  }
}
