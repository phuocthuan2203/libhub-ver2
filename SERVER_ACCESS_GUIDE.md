# LibHub Server Access Guide

## Application URLs

Access the LibHub application using these URLs:

### Frontend (Web Interface)
```
http://192.168.1.4:8080
```

### API Gateway (Direct API Access)
```
http://192.168.1.4:5000
```

### Consul Service Discovery UI
```
http://192.168.1.4:8500
```

## Quick Test Commands

### Test Books API
```bash
curl http://192.168.1.4:8080/api/books
```

### Test User Registration
```bash
curl -X POST http://192.168.1.4:8080/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "test@example.com",
    "password": "Test@123"
  }'
```

### Test User Login
```bash
curl -X POST http://192.168.1.4:8080/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "Test@123"
  }'
```

## Service Architecture

```
Client Browser
    ↓
http://192.168.1.4:8080 (Frontend - nginx)
    ↓
/api/* → http://gateway:5000 (API Gateway - Ocelot)
    ↓
    ├── /api/users → UserService:5002
    ├── /api/books → CatalogService:5001
    └── /api/loans → LoanService:5003
```

## Container Status

Check all running containers:
```bash
docker compose ps
```

Expected output: All services should show "Up" status

## Common Management Commands

### View logs for specific service
```bash
docker compose logs [service-name] --tail=50
```

Service names:
- `frontend`
- `gateway`
- `userservice`
- `catalogservice`
- `loanservice`
- `mysql`
- `consul`

### Restart specific service
```bash
docker compose restart [service-name]
```

### Restart all services
```bash
docker compose restart
```

### Stop all services
```bash
docker compose down
```

### Start all services
```bash
docker compose up -d
```

### Rebuild and restart
```bash
docker compose down
docker compose up -d --build
```

## Firewall Ports

The following ports are open on the server:

| Port | Service | Purpose |
|------|---------|---------|
| 8080 | Frontend | Web interface |
| 5000 | Gateway | API Gateway |
| 8500 | Consul | Service discovery UI |

## Database Access

MySQL is accessible within the Docker network:
- Host: `mysql`
- Port: `3306` (internal), `3307` (external)
- User: `libhub_user`
- Password: `LibHub@Dev2025`
- Databases: `user_db`, `catalog_db`, `loan_db`

### Connect to MySQL from host
```bash
docker exec -it libhub-mysql mysql -ulibhub_user -pLibHub@Dev2025
```

## Troubleshooting

### If services are not responding

1. Check container status:
```bash
docker compose ps
```

2. Check logs for errors:
```bash
docker compose logs --tail=100
```

3. Restart services:
```bash
docker compose restart
```

### If database connection fails

1. Check MySQL is healthy:
```bash
docker compose ps mysql
```

2. Verify databases exist:
```bash
docker exec -it libhub-mysql mysql -uroot -pLibHub@2025 -e "SHOW DATABASES;"
```

3. Verify user permissions:
```bash
docker exec -it libhub-mysql mysql -uroot -pLibHub@2025 -e "SHOW GRANTS FOR 'libhub_user'@'%';"
```

### Clear browser cache

If you see old errors in the browser:
- **Hard Refresh**: `Ctrl + Shift + R` (Linux/Windows) or `Cmd + Shift + R` (Mac)
- **Incognito Mode**: Open in private/incognito window
- **Clear Cache**: Browser Settings → Clear browsing data

## Default Test Accounts

After first deployment, you can register new accounts or use these if seeded:

### Admin Account (if seeded)
- Email: `admin@libhub.com`
- Password: `Admin@123`

### Customer Account (if seeded)
- Email: `customer@libhub.com`
- Password: `Customer@123`

## Application Features

### Public Features (No Login Required)
- Browse book catalog
- Search books by title, author, ISBN, or genre
- View book details

### Customer Features (Login Required)
- Borrow books (max 5 active loans)
- Return books
- View loan history
- View active loans

### Admin Features (Admin Role Required)
- Add new books
- Edit book details
- Delete books
- View all system loans
- View overdue loans

## Support

For issues or questions, check:
1. Container logs: `docker compose logs [service-name]`
2. Gateway logs: `docker compose logs gateway`
3. MySQL logs: `docker compose logs mysql`
4. Browser console (F12 → Console tab)

## Last Updated
November 2, 2025 - All services verified working

