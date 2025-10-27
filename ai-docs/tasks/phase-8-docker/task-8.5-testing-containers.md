# Task 8.5: Testing Docker Containers for LibHub

This document provides instructions for testing the complete Docker containerized LibHub system, including health checks, API testing, and data persistence verification.

## 1. Complete Test Workflow

Start the entire system and verify all containers are running:

```bash
# Start all services
docker compose up -d

# Wait for health checks (30-60 seconds)
sleep 45

# Verify all containers are running
docker ps
```

- **Expected Output**: 6 containers running (mysql, userservice, catalogservice, loanservice, gateway, frontend).

## 2. Test Health Endpoints

Verify each service is healthy and responding:

```bash
# UserService health
curl http://localhost:5002/health

# CatalogService health
curl http://localhost:5001/health

# LoanService health
curl http://localhost:5003/health

# Gateway (no health endpoint, test root)
curl http://localhost:5000
```

- **Expected Output**: All services should return `200 OK` or health status messages.

## 3. Test Frontend Accessibility

Verify the frontend is accessible via browser:

```bash
# Open in browser (manual step)
# Navigate to: http://localhost:8080
```

- **Expected Output**: Login page loads correctly with styling and forms.

## 4. End-to-End API Testing via Gateway

Perform a complete user journey through the Gateway:

```bash
# 1. Register a new user
curl -X POST http://localhost:5000/api/users/register \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","email":"test@example.com","password":"Password123!"}'

# 2. Login to get JWT token
TOKEN=$(curl -s -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Password123!"}' | \
  grep -o '"token":"[^"]*"' | cut -d'"' -f4)

# 3. Browse books (public endpoint)
curl http://localhost:5000/api/books

# 4. Borrow a book (requires JWT)
curl -X POST http://localhost:5000/api/loans \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"bookId":1}'

# 5. Check user loans
curl http://localhost:5000/api/loans/user/1 \
  -H "Authorization: Bearer $TOKEN"
```

- **Expected Output**: Successful responses at each step, confirming Saga orchestration in Docker.

## 5. Verify Data Persistence

Confirm data survives container restarts:

```bash
# Stop all containers
docker compose down

# Start again
docker compose up -d

# Wait for services to be ready
sleep 30

# Verify user still exists
curl -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Password123!"}'

# Check database directly
docker exec -it libhub-mysql mysql -u libhub_user -pLibHub@Dev2025 \
  -e "SELECT COUNT(*) FROM user_db.Users;"
```

- **Expected Output**: Login succeeds and user count is preserved.

## 6. Test Container Restart

Verify individual service recovery:

```bash
# Restart LoanService
docker compose restart loanservice

# Wait for restart
sleep 15

# Verify LoanService is healthy
curl http://localhost:5003/health

# Test loan functionality still works
TOKEN=$(curl -s -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Password123!"}' | \
  grep -o '"token":"[^"]*"' | cut -d'"' -f4)

curl -X POST http://localhost:5000/api/loans \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"bookId":2}'
```

- **Expected Output**: Service restarts successfully and functionality is preserved.

## 7. Acceptance Criteria Checklist

Verify all requirements are met:

- [ ] All 6 containers start successfully
- [ ] All health endpoints return 200 OK
- [ ] Frontend loads at http://localhost:8080
- [ ] User registration works via Gateway
- [ ] JWT authentication works
- [ ] Book browsing works
- [ ] Book borrowing triggers Saga correctly
- [ ] Data persists after container restart
- [ ] Individual service restarts work
- [ ] MySQL data volume persists

## 8. Troubleshooting References

If tests fail, consult:

- **Container Issues**: Check `docker compose logs -f [service]`
- **Network Issues**: Run `docker network inspect libhub_libhub-network`
- **Database Issues**: Verify with `docker exec -it libhub-mysql mysql -u libhub_user -p`
- **Full Troubleshooting Guide**: See `DOCKER_TROUBLESHOOTING.md`

## 9. Cleanup Commands

After testing, clean up if needed:

```bash
# Stop and remove containers
docker compose down

# Remove volumes (⚠️ deletes all data)
docker compose down -v

# Remove images
docker compose down --rmi all
```

Use these commands only if you want to completely reset the environment.