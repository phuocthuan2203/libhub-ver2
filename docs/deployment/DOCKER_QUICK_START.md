# LibHub Docker Quick Start Guide

## Prerequisites
- Docker and Docker Compose installed
- Ports available: 3306, 5000, 5001, 5002, 5003, 8080, 8500, 8600
- Minimum 4GB RAM recommended for all containers

## Quick Start

### Windows Users

**Easiest way:**
```cmd
setup-windows.bat
```

Or using PowerShell:
```powershell
.\setup-windows.ps1
```

### Linux/Mac Users

### 1. Start All Services
```bash
docker compose up -d
```

Wait 30-60 seconds for all services to initialize.

### 1.1 Start services with defined number of instances
```bash
docker compose up -d --scale userservice=3 --scale catalogservice=3 --scale loanservice=3
```

### 2. Verify Services
```bash
docker ps
```

You should see 7 containers running:
- libhub-consul (Consul service discovery)
- libhub-mysql (healthy)
- libhub-userservice
- libhub-catalogservice
- libhub-loanservice
- libhub-gateway
- libhub-frontend

### 3. Access the Application
- **Frontend**: http://localhost:8080
- **API Gateway**: http://localhost:5000
- **Consul UI**: http://localhost:8500 (Service discovery dashboard)
- **Swagger Docs**:
  - UserService: http://localhost:5002/swagger
  - CatalogService: http://localhost:5001/swagger
  - LoanService: http://localhost:5003/swagger

## Testing

### Windows (Batch Scripts)

```cmd
scripts\test-docker-containers.bat
scripts\test-consul-discovery.bat
scripts\test-gateway-integration.bat
```

### Windows (PowerShell)

```powershell
.\scripts\test-docker-containers.ps1
```

### Linux/Mac (Shell Scripts)

```bash
./scripts/test-docker-containers.sh
./scripts/verify-network.sh
./scripts/verify-persistence.sh
./scripts/test-consul-discovery.sh
./scripts/test-gateway-integration.sh
```

## Check Logs

### All Services
```bash
docker compose logs -f
```

### Specific Service
```bash
docker compose logs -f userservice
docker compose logs -f catalogservice
docker compose logs -f loanservice
docker compose logs -f gateway
```

### Check Migration Success
```bash
docker compose logs userservice | grep "migrations applied"
docker compose logs catalogservice | grep "migrations applied"
docker compose logs loanservice | grep "migrations applied"
```

## Database Access

### Connect to MySQL
```bash
docker exec -it libhub-mysql mysql -u libhub_user -pLibHub@Dev2025
```

### Verify Tables
```bash
docker exec libhub-mysql mysql -u libhub_user -pLibHub@Dev2025 \
  -e "SHOW TABLES FROM user_db; SHOW TABLES FROM catalog_db; SHOW TABLES FROM loan_db;"
```

## Management Commands

### Start Services
```bash
# Start all services
docker compose up -d

# Start specific service
docker compose up -d userservice

# Start with logs visible
docker compose up

# Start and rebuild
docker compose up -d --build
```

### Stop Services
```bash
# Stop all services (keeps data)
docker compose down

# Stop specific service
docker compose stop userservice

# Stop and remove volumes (⚠️ Deletes all data)
docker compose down -v
```

### Restart Services
```bash
# Restart all services
docker compose restart

# Restart specific service
docker compose restart userservice

# Restart with rebuild
docker compose up -d --build --force-recreate
```

### Rebuild Services
```bash
# Rebuild all services
docker compose build

# Rebuild specific service
docker compose build catalogservice

# Rebuild without cache
docker compose build --no-cache

# Rebuild and start
docker compose up -d --build
```

### View Status
```bash
# View running containers
docker compose ps

# View all containers (including stopped)
docker compose ps -a

# View resource usage
docker stats
```

### Scale Services (Load Balancing)
```bash
# Scale userservice to 3 instances
docker compose up -d --scale userservice=3

# Scale catalogservice to 2 instances
docker compose up -d --scale catalogservice=2

# Note: Consul will automatically discover all instances
```

### Clean Up
```bash
# Remove stopped containers
docker compose rm

# Remove all (⚠️ Destructive - removes data)
docker compose down -v

# Remove unused images
docker image prune -a

# Full cleanup (⚠️ Very Destructive)
docker system prune -a --volumes
```

## Troubleshooting

### Service Won't Start
```bash
docker compose logs [service-name]
```

### Network Issues
```bash
docker network inspect libhub_libhub-network
```

### Database Connection Issues
```bash
docker exec libhub-mysql mysqladmin ping -h localhost
```

### Rebuild Specific Service
```bash
docker compose up -d --build userservice
```

## Test Scripts Usage

The `scripts/` directory contains automated test and verification scripts:

### test-docker-containers.sh
Comprehensive test suite for all Docker containers.

```bash
./scripts/test-docker-containers.sh
```

**What it tests:**
- Container health status
- Database connectivity
- Service endpoints
- API Gateway routing
- Database migrations
- Network connectivity

### test-consul-discovery.sh
Tests Consul service discovery functionality.

```bash
./scripts/test-consul-discovery.sh
```

**What it tests:**
- Consul availability
- Service registration (all 3 microservices)
- Health check status
- Service endpoints
- Gateway routing through Consul
- Service instance details

### test-gateway-integration.sh
Tests API Gateway integration with all services.

```bash
./scripts/test-gateway-integration.sh
```

**What it tests:**
- Public endpoints (no auth required)
- User registration and login
- JWT token generation
- Protected endpoints (auth required)
- Complete user journey

### verify-network.sh
Verifies Docker network configuration.

```bash
./scripts/verify-network.sh
```

**What it checks:**
- Network existence
- Container connectivity
- Service name resolution
- Inter-service communication

### verify-persistence.sh
Tests data persistence across container restarts.

```bash
./scripts/verify-persistence.sh
```

**What it tests:**
- Data survives container restart
- Volume mounting
- Database persistence
- Migration re-application

### Running All Tests
```bash
# Make scripts executable (first time only)
chmod +x scripts/*.sh

# Run all tests in sequence
./scripts/test-docker-containers.sh && \
./scripts/test-consul-discovery.sh && \
./scripts/test-gateway-integration.sh && \
./scripts/verify-network.sh && \
./scripts/verify-persistence.sh
```

## Architecture

### Services
- **Consul**: Service discovery and health checking (ports 8500, 8600)
- **MySQL**: Database server (port 3306)
- **UserService**: User authentication and management (port 5002)
- **CatalogService**: Book catalog management with seed data (port 5001)
- **LoanService**: Loan operations with Saga pattern (port 5003)
- **Gateway**: Ocelot API Gateway with Consul integration (port 5000)
- **Frontend**: Nginx-served static files (port 8080)

### Network
All services communicate via `libhub-network` bridge network. Services are discovered dynamically through Consul, with automatic health checks and load balancing.

### Data Persistence
MySQL data persists in the `mysql-data` Docker volume across container restarts.

## Features

- ✅ Automatic database migrations on startup
- ✅ **Consul service discovery with health checks**
- ✅ **Dynamic service registration and load balancing**
- ✅ **Book seed data (15 books) for immediate testing**
- ✅ Health checks for all services
- ✅ Service dependencies managed
- ✅ Environment-based configuration
- ✅ Volume persistence for data
- ✅ Multi-stage Docker builds for optimization
- ✅ Comprehensive logging
- ✅ Horizontal scaling support

## Default Credentials

### Test Users (if seed data loaded)
- **Admin**: admin@libhub.com / [see seed-data.sql]
- **Customer**: test@libhub.com / [see seed-data.sql]

### Database
- **User**: libhub_user
- **Password**: LibHub@Dev2025
- **Databases**: user_db, catalog_db, loan_db

## Support

For detailed troubleshooting, see:
- `ai-docs/deployment/DOCKER_TROUBLESHOOTING.md`
- `ai-docs/completed/task-8.3-8.4-8.5-implementation.md`
