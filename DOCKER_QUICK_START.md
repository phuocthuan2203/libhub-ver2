# LibHub Docker Quick Start Guide

## Prerequisites
- Docker and Docker Compose installed
- Ports available: 3306, 5000, 5001, 5002, 5003, 8080

## Quick Start

### 1. Start All Services
```bash
docker compose up -d
```

Wait 30-60 seconds for all services to initialize.

### 2. Verify Services
```bash
docker ps
```

You should see 6 containers running:
- libhub-mysql (healthy)
- libhub-userservice
- libhub-catalogservice
- libhub-loanservice
- libhub-gateway
- libhub-frontend

### 3. Access the Application
- **Frontend**: http://localhost:8080
- **API Gateway**: http://localhost:5000
- **Swagger Docs**:
  - UserService: http://localhost:5002/swagger
  - CatalogService: http://localhost:5001/swagger
  - LoanService: http://localhost:5003/swagger

## Testing

### Run Comprehensive Tests
```bash
./scripts/test-docker-containers.sh
```

### Verify Network
```bash
./scripts/verify-network.sh
```

### Test Data Persistence
```bash
./scripts/verify-persistence.sh
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

### Stop All Services
```bash
docker compose down
```

### Restart a Service
```bash
docker compose restart userservice
```

### Rebuild and Restart
```bash
docker compose up -d --build
```

### View Service Status
```bash
docker compose ps
```

### Remove All Data (⚠️ Destructive)
```bash
docker compose down -v
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

## Architecture

### Services
- **MySQL**: Database server (port 3306)
- **UserService**: User authentication and management (port 5002)
- **CatalogService**: Book catalog management (port 5001)
- **LoanService**: Loan operations with Saga pattern (port 5003)
- **Gateway**: Ocelot API Gateway (port 5000)
- **Frontend**: Nginx-served static files (port 8080)

### Network
All services communicate via `libhub-network` bridge network using container names for service discovery.

### Data Persistence
MySQL data persists in the `mysql-data` Docker volume across container restarts.

## Features

- ✅ Automatic database migrations on startup
- ✅ Health checks for MySQL
- ✅ Service dependencies managed
- ✅ Environment-based configuration
- ✅ Volume persistence for data
- ✅ Multi-stage Docker builds for optimization
- ✅ Comprehensive logging

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
