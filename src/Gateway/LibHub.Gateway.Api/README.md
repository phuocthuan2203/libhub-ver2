# LibHub API Gateway

API Gateway for LibHub microservices using Ocelot with Consul service discovery.

## Overview

The Gateway serves as the single entry point for all client requests to the LibHub microservices architecture. It provides routing, authentication, load balancing, and quality of service features.

## Port
- **5000** (http://localhost:5000)

## Features

### Core Features
- **Request Routing** - Routes requests to appropriate microservices
- **Service Discovery** - Automatic service discovery via Consul
- **JWT Authentication** - Validates JWT tokens for protected routes
- **CORS Support** - Configured for frontend integration
- **Load Balancing** - Round-robin load balancing across service instances
- **Quality of Service** - Circuit breaker pattern with configurable timeouts
- **Error Handling** - Centralized error handling with structured responses
- **Request Logging** - Logs all incoming requests and responses

### Quality of Service Settings
- **Circuit Breaker**: Opens after 3 consecutive failures
- **Break Duration**: 5 seconds
- **Request Timeout**: 10 seconds

## API Routes

### UserService Routes

#### Public Routes (No Authentication)
- `POST /api/users/register` - Register new user
- `POST /api/users/login` - User login

#### Protected Routes (Requires JWT)
- `GET /api/users/{id}` - Get user by ID
- `GET /api/users/me` - Get current user profile
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

### CatalogService Routes

#### Public Routes (No Authentication)
- `GET /api/books` - List all books (supports search query parameter)
- `GET /api/books/{id}` - Get book details

#### Protected Routes (Requires JWT - Admin Only)
- `POST /api/books` - Create new book
- `PUT /api/books/{id}` - Update book
- `DELETE /api/books/{id}` - Delete book

### LoanService Routes

#### Protected Routes (All Require JWT)
- `POST /api/loans` - Borrow a book
- `GET /api/loans` - List all loans
- `GET /api/loans/user/{userId}` - Get user's loans
- `GET /api/loans/{id}` - Get loan details
- `PUT /api/loans/{id}/return` - Return a book
- `DELETE /api/loans/{id}` - Cancel loan

## Running the Gateway

### Local Development
```bash
cd src/Gateway/LibHub.Gateway.Api
dotnet run
```

### Docker
```bash
docker compose up -d gateway
```

## Configuration Files

### ocelot.json
Contains route definitions, service discovery settings, and QoS configuration.

**Key Sections:**
- `Routes` - Defines upstream/downstream path templates and HTTP methods
- `ServiceName` - Links to Consul service registry
- `AuthenticationOptions` - Specifies routes requiring JWT authentication
- `QoSOptions` - Circuit breaker and timeout settings
- `LoadBalancerOptions` - Load balancing strategy (RoundRobin)
- `GlobalConfiguration` - Consul connection and base URL

### appsettings.json
Contains logging configuration and JWT settings.

**Key Settings:**
- `Logging` - Log levels for different components
- `Jwt:SecretKey` - Secret key for JWT validation (must match UserService)
- `Jwt:Issuer` - Expected token issuer
- `Jwt:Audience` - Expected token audience

## Service Discovery

The Gateway uses Consul for service discovery:
- **Consul Host**: `consul` (Docker) or `localhost` (local)
- **Consul Port**: `8500`
- **Service Resolution**: Automatic via service names (userservice, catalogservice, loanservice)

## Error Handling

### Global Exception Handler
All unhandled exceptions return a structured JSON response:
```json
{
  "message": "An error occurred while processing your request",
  "statusCode": 500,
  "timestamp": "2025-11-03T10:30:00Z"
}
```

### Authentication Errors
- **401 Unauthorized** - Missing or invalid JWT token
- **403 Forbidden** - Valid token but insufficient permissions

### Service Errors
- **404 Not Found** - Resource not found in downstream service
- **503 Service Unavailable** - Downstream service is down or circuit breaker is open

## Logging

### Log Levels
- **Information** - Request/response logs, service discovery events
- **Warning** - Authentication failures, circuit breaker events
- **Error** - Unhandled exceptions, downstream service errors

### Log Format
Structured logging with timestamps and scopes enabled for better traceability.

## Testing

### Prerequisites
Ensure all services are running:
1. **Consul** (port 8500)
2. **MySQL** (port 3306)
3. **UserService** (port 5002)
4. **CatalogService** (port 5001)
5. **LoanService** (port 5003)
6. **Gateway** (port 5000)

### Start All Services
```bash
docker compose up -d
```

### Check Service Health
```bash
docker compose ps
docker compose logs gateway
```

### Test Endpoints
```bash
curl http://localhost:5000/api/books
curl -X POST http://localhost:5000/api/users/register \
  -H "Content-Type: application/json" \
  -d '{"username":"test","email":"test@example.com","password":"Test123!@#"}'
```

### Integration Tests
```bash
cd /home/thuannp4/libhub-ver2
./scripts/test-gateway-integration.sh
```

## Troubleshooting

### Gateway Not Starting
- Check if port 5000 is available
- Verify `ocelot.json` syntax is valid
- Check Consul is running and accessible

### Service Discovery Issues
- Verify Consul is running: `curl http://localhost:8500/v1/catalog/services`
- Check service registration in Consul UI: `http://localhost:8500/ui`
- Ensure service names in `ocelot.json` match Consul service names

### Authentication Issues
- Verify JWT secret key matches UserService configuration
- Check token expiration time
- Ensure `Authorization: Bearer <token>` header is included

### Timeout Issues
- Increase `TimeoutValue` in QoSOptions if needed
- Check downstream service performance
- Review circuit breaker settings

## Architecture

```
Frontend (React)
       ↓
API Gateway (Ocelot) ← You are here
       ↓
   Consul (Service Discovery)
       ↓
  ┌────┴────┬────────────┐
  ↓         ↓            ↓
UserService CatalogService LoanService
  ↓         ↓            ↓
MySQL     MySQL        MySQL
```

## Security

### JWT Validation
- All protected routes validate JWT tokens
- Token must be signed with the correct secret key
- Token must have valid issuer and audience
- Token must not be expired

### CORS Policy
- Currently allows all origins for development
- **Production**: Restrict to specific frontend domain

## Performance

### Load Balancing
- Round-robin distribution across service instances
- Automatic failover if instance is unavailable

### Circuit Breaker
- Prevents cascading failures
- Automatically opens after 3 consecutive failures
- Closes after 5-second break duration

## Maintenance

### Adding New Routes
1. Add route definition to `ocelot.json`
2. Specify service name matching Consul registration
3. Add authentication if required
4. Update this README with new route

### Updating Configuration
- Changes to `ocelot.json` reload automatically
- Changes to `appsettings.json` require service restart

---

**Last Updated**: November 3, 2025  
**Version**: 1.0  
**Maintained By**: Development Team
