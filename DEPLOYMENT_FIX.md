# LibHub Deployment Fix - November 2, 2025

## Problem Identified

When accessing the LibHub application at `http://192.168.1.4:8080/`, the browser console showed:
```
GET http://192.168.1.4:8080/api/books net::ERR_ABORTED 404 (Not Found)
```

## Root Cause

The three microservices (UserService, CatalogService, LoanService) were crashing with exit code 139 due to MySQL database permission issues:

```
Access denied for user 'libhub_user'@'%' to database 'catalog_db'
Access denied for user 'libhub_user'@'%' to database 'user_db'
Access denied for user 'libhub_user'@'%' to database 'loan_db'
```

The MySQL container created the `libhub_user` but didn't automatically create the databases or grant permissions.

## Solution Applied

### Step 1: Created Databases and Granted Permissions

Connected to MySQL container and executed:

```sql
CREATE DATABASE IF NOT EXISTS user_db;
CREATE DATABASE IF NOT EXISTS catalog_db;
CREATE DATABASE IF NOT EXISTS loan_db;

GRANT ALL PRIVILEGES ON user_db.* TO 'libhub_user'@'%';
GRANT ALL PRIVILEGES ON catalog_db.* TO 'libhub_user'@'%';
GRANT ALL PRIVILEGES ON loan_db.* TO 'libhub_user'@'%';

FLUSH PRIVILEGES;
```

### Step 2: Restarted Microservices

```bash
docker compose up -d userservice catalogservice loanservice
```

## Verification

All services are now running successfully:

```bash
docker compose ps
```

Expected output: All services showing "Up" status

Test API endpoint:
```bash
curl http://192.168.1.4:8080/api/books
```

Expected: JSON array of books

## Architecture Overview

The application uses the following architecture:

```
Browser (192.168.1.4:8080)
    ↓
Frontend (nginx:8080)
    ↓ /api/* requests proxied to
Gateway (Ocelot:5000)
    ↓ routes to
Microservices:
    - UserService:5002
    - CatalogService:5001
    - LoanService:5003
    ↓ connect to
MySQL (3306)
    - user_db
    - catalog_db
    - loan_db
```

## Firewall Configuration

The following ports are open:
- 8080 (Frontend)
- 5000 (API Gateway)
- 8500 (Consul UI)

## Future Deployment

To prevent this issue in future deployments, consider adding a MySQL initialization script:

1. Create `mysql-init/init.sql`:
```sql
CREATE DATABASE IF NOT EXISTS user_db;
CREATE DATABASE IF NOT EXISTS catalog_db;
CREATE DATABASE IF NOT EXISTS loan_db;

GRANT ALL PRIVILEGES ON user_db.* TO 'libhub_user'@'%';
GRANT ALL PRIVILEGES ON catalog_db.* TO 'libhub_user'@'%';
GRANT ALL PRIVILEGES ON loan_db.* TO 'libhub_user'@'%';

FLUSH PRIVILEGES;
```

2. Update `docker-compose.yml` MySQL service:
```yaml
mysql:
  volumes:
    - mysql-data:/var/lib/mysql
    - ./mysql-init:/docker-entrypoint-initdb.d
```

## Troubleshooting Commands

Check service status:
```bash
docker compose ps
```

View service logs:
```bash
docker compose logs [service-name] --tail=50
```

Restart specific service:
```bash
docker compose restart [service-name]
```

Rebuild and restart all:
```bash
docker compose down
docker compose up -d --build
```

## Status: RESOLVED ✓

The application is now fully functional and accessible at:
- Frontend: http://192.168.1.4:8080
- API Gateway: http://192.168.1.4:5000
- Consul UI: http://192.168.1.4:8500

