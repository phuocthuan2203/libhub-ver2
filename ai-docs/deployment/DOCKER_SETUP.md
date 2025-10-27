# Docker Setup Guide for LibHub

This guide explains the Docker containerization strategy for LibHub microservices application.

---

## Architecture

Docker Environment
├── mysql (container) - 3306
├── userservice (container) - 5002
├── catalogservice (container) - 5001
├── loanservice (container) - 5003
├── gateway (container) - 5000
├── frontend (container) - 8080
└── consul (container) - 8500 (Phase 9)

text

**Network**: `libhub-network` (bridge driver)

---

## Prerequisites

- Docker Engine 24.0+ installed
- Docker Compose 2.20+ installed
- 8GB RAM minimum
- 20GB disk space

**Install Docker on Ubuntu**:
Update packages
sudo apt update
sudo apt install -y apt-transport-https ca-certificates curl software-properties-common

Add Docker GPG key
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg

Add Docker repository
echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

Install Docker
sudo apt update
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

Add user to docker group
sudo usermod -aG docker $USER
newgrp docker

Verify installation
docker --version
docker compose version

text

---

## Project Structure

LibHub/
├── docker-compose.yml # Main orchestration file
├── .dockerignore # Exclude from Docker context
├── scripts/
│ ├── init-databases.sql # DB initialization
│ └── seed-data.sql # Optional seed data
├── src/
│ ├── Services/
│ │ ├── UserService/
│ │ │ └── LibHub.UserService.Api/
│ │ │ └── Dockerfile
│ │ ├── CatalogService/
│ │ │ └── LibHub.CatalogService.Api/
│ │ │ └── Dockerfile
│ │ └── LoanService/
│ │ └── LibHub.LoanService.Api/
│ │ └── Dockerfile
│ └── Gateway/
│ └── LibHub.Gateway.Api/
│ └── Dockerfile
└── frontend/
├── Dockerfile
└── nginx.conf

text

---

## Quick Start

### 1. Build All Images
docker compose build

text

**Expected output**: 5 images built successfully

### 2. Start All Services
docker compose up -d

text

**Expected output**: 6 containers running

### 3. Verify Health
Check container status
docker ps

Check logs
docker compose logs -f

Health check endpoints
curl http://localhost:5002/health # UserService
curl http://localhost:5001/health # CatalogService
curl http://localhost:5003/health # LoanService
curl http://localhost:5000/health # Gateway (if implemented)

text

### 4. Access Application
- **Frontend**: http://localhost:8080
- **Gateway API**: http://localhost:5000
- **MySQL**: localhost:3306

### 5. Stop Services
docker compose down

text

### 6. Stop and Remove Data
docker compose down -v # ⚠️ Deletes database data!

text

---

## Environment Variables

**Configured in docker-compose.yml**:

environment:

ASPNETCORE_ENVIRONMENT=Production

ConnectionStrings__DefaultConnection=Server=mysql;Port=3306;Database=user_db;User=libhub_user;Password=LibHub@Dev2025;

Jwt__SecretKey=LibHub_SuperSecret_256BitKey_ForDevelopment_ChangeInProduction!

Jwt__Issuer=LibHub.UserService

Jwt__Audience=LibHub.Clients

text

**Override with .env file** (optional):
Create .env file in project root
MYSQL_ROOT_PASSWORD=YourSecurePassword
MYSQL_USER_PASSWORD=YourDevPassword
JWT_SECRET_KEY=YourCustom256BitSecretKey

text

---

## Networking

### Docker Network: libhub-network

**Service communication**:
- Services use container names: `mysql`, `userservice`, `catalogservice`, `loanservice`
- Gateway routes use container names in ocelot.json
- Frontend calls Gateway via browser: `http://localhost:5000`

**Example** (in LoanService):
// Configuration in appsettings.json or environment variable
"ExternalServices": {
"CatalogServiceBaseUrl": "http://catalogservice:5001"
}

text

### Port Mapping

| Service | Internal Port | External Port | Access |
|---------|--------------|---------------|--------|
| MySQL | 3306 | 3306 | localhost:3306 |
| UserService | 5002 | 5002 | localhost:5002 |
| CatalogService | 5001 | 5001 | localhost:5001 |
| LoanService | 5003 | 5003 | localhost:5003 |
| Gateway | 5000 | 5000 | localhost:5000 |
| Frontend | 8080 | 8080 | localhost:8080 |

---

## Database Persistence

**Volume**: `mysql-data` (Docker named volume)

**Location**: `/var/lib/docker/volumes/libhub_mysql-data/_data`

**Backup database**:
docker exec libhub-mysql mysqldump -u root -p'LibHub@2025' --all-databases > backup.sql

text

**Restore database**:
docker exec -i libhub-mysql mysql -u root -p'LibHub@2025' < backup.sql

text

---

## Logs and Debugging

### View All Logs
docker compose logs -f

text

### View Specific Service
docker compose logs -f userservice

text

### Enter Container Shell
docker exec -it libhub-userservice bash

text

### Check MySQL
docker exec -it libhub-mysql mysql -u root -p

text

### Inspect Network
docker network inspect libhub_libhub-network

text

---

## Performance Optimization

### Build with Cache
docker compose build --parallel

text

### Resource Limits (optional)
Add to docker-compose.yml:
services:
userservice:
deploy:
resources:
limits:
cpus: '0.5'
memory: 512M
reservations:
cpus: '0.25'
memory: 256M

text

---

## CI/CD Integration

### Build for Production
docker compose -f docker-compose.yml -f docker-compose.prod.yml build

text

### Push to Registry
docker tag libhub-userservice:latest myregistry.com/libhub-userservice:v1.0
docker push myregistry.com/libhub-userservice:v1.0

text

---

## Next Steps

1. **Phase 9**: Add Consul for service discovery
2. **Monitoring**: Add Prometheus + Grafana
3. **Logging**: Add ELK stack (Elasticsearch, Logstash, Kibana)
4. **Deployment**: Deploy to Kubernetes cluster

---

## Related Documentation

- [Docker Troubleshooting](./DOCKER_TROUBLESHOOTING.md)
- [Deployment Guide](./DEPLOYMENT_GUIDE.md)
- [PROJECT_STATUS.md](../PROJECT_STATUS.md)