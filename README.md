# LibHub - Library Management System

A microservices-based library management system built with ASP.NET Core 8.0.

## Project Structure

- `src/Services/UserService` - User authentication and identity management
- `src/Services/CatalogService` - Book catalog and inventory management
- `src/Services/LoanService` - Book borrowing and returns
- `src/Gateway` - API Gateway (Ocelot)
- `frontend` - HTML/CSS/JavaScript user interface
- `docs` - Project documentation
- `tests` - Unit and integration tests

## Tech Stack

- **Backend**: ASP.NET Core 8.0 Web API
- **Database**: MySQL 8.0
- **ORM**: Entity Framework Core 9.0
- **API Gateway**: Ocelot
- **Authentication**: JWT (JSON Web Tokens)
- **Frontend**: Vanilla JavaScript (ES6+)

## Development Environment

- Ubuntu 25.10
- .NET 8 SDK
- MySQL 8.0
- Visual Studio Code with C# Dev Kit

## Getting Started

### Prerequisites

- **Docker Desktop** (Windows/Mac/Linux)
- **Git** for cloning the repository
- Minimum 4GB RAM available
- Ports available: 3306, 5000, 5001, 5002, 5003, 8080, 8500, 8600

### Quick Start (All Platforms)

#### Windows Users

**Option 1: One-Command Setup (Batch Script)**
```cmd
setup-windows.bat
```

**Option 2: One-Command Setup (PowerShell)**
```powershell
.\setup-windows.ps1
```

#### Linux/Mac Users

```bash
docker compose up -d --build
```

Wait 60 seconds, then test:
```bash
./scripts/test-docker-containers.sh
```

### Access the Application

Once setup is complete, access:

- **Frontend**: http://localhost:8080
- **API Gateway**: http://localhost:5000
- **Consul UI**: http://localhost:8500 (Service Discovery Dashboard)

### Testing

**Windows (Batch)**:
```cmd
scripts\test-docker-containers.bat
scripts\test-consul-discovery.bat
scripts\test-gateway-integration.bat
```

**Windows (PowerShell)**:
```powershell
.\scripts\test-docker-containers.ps1
```

**Linux/Mac**:
```bash
./scripts/test-docker-containers.sh
./scripts/test-consul-discovery.sh
./scripts/test-gateway-integration.sh
```

### Common Commands

**View Logs**:
```bash
docker compose logs -f
```

**Stop Services**:
```bash
docker compose down
```

**Restart Services**:
```bash
docker compose restart
```

**Rebuild and Restart**:
```bash
docker compose up -d --build
```

For detailed documentation, see:
- **Windows Users**: `WINDOWS_SETUP.md`
- **All Users**: `DOCKER_QUICK_START.md`

## Architecture

This project follows:
- Microservices Architecture
- Clean Architecture (per microservice)
- Database per Service pattern
- Saga pattern for distributed transactions

## License

Academic project - All rights reserved
