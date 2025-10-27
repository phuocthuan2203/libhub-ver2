# LibHub API Gateway

API Gateway for LibHub microservices using Ocelot.

## Port
- **5000** (http://localhost:5000)

## Features
- Request routing to all microservices
- JWT authentication validation
- CORS support for frontend
- Centralized entry point

## Routes

### Public Routes (No Authentication)
- `POST /api/users/register` → UserService
- `POST /api/users/login` → UserService
- `GET /api/books` → CatalogService
- `GET /api/books/{id}` → CatalogService

### Protected Routes (Requires JWT)
- `GET /api/users/{everything}` → UserService
- `POST/PUT/DELETE /api/books/{everything}` → CatalogService (Admin only)
- `ALL /api/loans/{everything}` → LoanService

## Running the Gateway

```bash
cd src/Gateway/LibHub.Gateway.Api
dotnet run
```

## Testing

Run all services first:
1. UserService (port 5002)
2. CatalogService (port 5001)
3. LoanService (port 5003)
4. Gateway (port 5000)

Then run integration test:
```bash
cd /home/thuannp4/development/LibHub
./test-gateway-integration.sh
```

## Configuration

- `ocelot.json` - Route configuration
- `appsettings.json` - JWT and port settings
