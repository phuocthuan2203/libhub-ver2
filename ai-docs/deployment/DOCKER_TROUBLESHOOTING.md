# Docker Troubleshooting Guide

Common issues and solutions for LibHub Docker deployment.

---

## Container Issues

### Problem: Container Exits Immediately

**Symptom**:
docker ps

libhub-userservice is missing
text

**Diagnosis**:
docker compose logs userservice

text

**Common Causes**:
1. **Database connection failure**
   - Solution: Check MySQL container is running and healthy
   - Verify connection string in environment variables

2. **Port already in use**
Check what's using the port
sudo lsof -i :5002

Kill the process or change port in docker-compose.yml
text

3. **Application crash on startup**
- Check logs for exceptions
- Verify appsettings.json is correct
- Check EF Core migrations ran successfully

---

### Problem: Container Restarts Continuously

**Diagnosis**:
docker compose logs -f userservice

text

**Solutions**:

1. **Health check failing**:
Remove or adjust healthcheck in docker-compose.yml
healthcheck:
test: ["CMD", "curl", "-f", "http://localhost:5002/health"]
interval: 30s # Increase interval
timeout: 10s # Increase timeout
retries: 5

text

2. **Out of memory**:
docker stats

If memory usage is 100%, increase limits
text

---

## Networking Issues

### Problem: Services Can't Communicate

**Symptom**: LoanService can't call CatalogService

**Diagnosis**:
Enter LoanService container
docker exec -it libhub-loanservice bash

Try to ping CatalogService
apt update && apt install -y iputils-ping curl
ping catalogservice
curl http://catalogservice:5001/health

text

**Solutions**:

1. **Wrong hostname**: Use container name, not `localhost`
// ❌ Wrong
"CatalogServiceBaseUrl": "http://localhost:5001"

// ✅ Correct
"CatalogServiceBaseUrl": "http://catalogservice:5001"

text

2. **Containers not on same network**:
docker network inspect libhub_libhub-network

Verify all containers are listed
text

3. **Firewall blocking**:
sudo ufw status
sudo ufw allow 5001/tcp

text

---

### Problem: Can't Access from Host Machine

**Symptom**: `curl http://localhost:5000` fails

**Solutions**:

1. **Port not mapped**:
Ensure ports section exists
ports:
- "5000:5000" # host:container

text

2. **Service binding to wrong interface**:
// In Dockerfile or Program.cs
ENV ASPNETCORE_URLS=http://+:5000 // Bind to all interfaces

text

3. **Container firewall**:
docker exec libhub-gateway netstat -tuln | grep 5000

Should show 0.0.0.0:5000 or :::5000
text

---

## Database Issues

### Problem: MySQL Container Won't Start

**Diagnosis**:
docker compose logs mysql

text

**Common Errors**:

1. **"Can't connect to MySQL server"**:
Wait for MySQL to fully initialize (30-60 seconds)
docker compose logs -f mysql

Look for: "ready for connections"
text

2. **Permission denied on volume**:
Remove volume and recreate
docker compose down -v
docker compose up -d

text

3. **Port 3306 already in use**:
Stop local MySQL
sudo systemctl stop mysql

Or change port in docker-compose.yml
ports:
- "3307:3306"

text

---

### Problem: Migrations Not Applied

**Symptom**: Tables don't exist in database

**Solutions**:

1. **Run migrations manually**:
docker exec -it libhub-userservice dotnet ef database update

text

2. **Check migration code in Program.cs**:
using (var scope = app.Services.CreateScope())
{
var db = scope.ServiceProvider.GetRequiredService<UserDbContext>();
db.Database.Migrate(); // Verify this line exists
}

text

3. **Check connection string**:
docker exec libhub-userservice printenv ConnectionStrings__DefaultConnection

text

---

### Problem: Database Connection Refused

**Error**: `Unable to connect to any of the specified MySQL hosts`

**Solutions**:

1. **MySQL not ready yet**:
Add depends_on with condition
depends_on:
mysql:
condition: service_healthy

text

2. **Wrong hostname**:
Use 'mysql', not 'localhost'
ConnectionStrings__DefaultConnection: "Server=mysql;Port=3306;..."

text

3. **Check MySQL health**:
docker exec libhub-mysql mysqladmin ping -h localhost -u root -p

text

---

## Build Issues

### Problem: Docker Build Fails

**Error**: `COPY failed: file not found`

**Solutions**:

1. **Build context issue**:
Build from project root, not service directory
cd ~/Projects/LibHub
docker compose build

text

2. **Check .dockerignore**:
Ensure these are NOT in .dockerignore
src/
*.csproj
*.cs
text

3. **Verify Dockerfile COPY paths**:
Paths relative to build context (project root)
COPY ["Services/UserService/LibHub.UserService.Api/...", "..."]

text

---

### Problem: Image Size Too Large

**Diagnosis**:
docker images | grep libhub

If images >500MB, optimize
text

**Solutions**:

1. **Use multi-stage builds** (already implemented)
2. **Clean build artifacts**:
RUN dotnet clean
RUN dotnet publish -c Release --no-restore

text

3. **Use Alpine images**:
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final

text

---

## Performance Issues

### Problem: Slow Startup

**Solutions**:

1. **Increase health check interval**:
healthcheck:
interval: 30s # Reduce frequency

text

2. **Disable logging verbosity**:
environment:
- Logging__LogLevel__Default=Warning

text

3. **Use cached builds**:
docker compose build --parallel

text

---

### Problem: High Memory Usage

**Diagnosis**:
docker stats

text

**Solutions**:

1. **Set memory limits**:
deploy:
resources:
limits:
memory: 512M

text

2. **Optimize garbage collection**:
// In Program.cs
builder.Services.Configure<GCSettings>(options =>
{
options.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
});

text

---

## Frontend Issues

### Problem: Frontend Can't Call Gateway

**Symptom**: CORS errors in browser console

**Solutions**:

1. **Check Gateway CORS config**:
builder.Services.AddCors(options => {
options.AddDefaultPolicy(policy => {
policy.WithOrigins("http://localhost:8080")
.AllowAnyMethod()
.AllowAnyHeader();
});
});

text

2. **Update frontend API calls**:
// Use Gateway URL
const API_BASE_URL = 'http://localhost:5000';

text

---

### Problem: Frontend Shows Blank Page

**Diagnosis**:
docker exec -it libhub-frontend ls /usr/share/nginx/html

Should show index.html, styles.css, etc.
text

**Solutions**:

1. **Check nginx logs**:
docker compose logs frontend

text

2. **Verify nginx config**:
docker exec libhub-frontend cat /etc/nginx/conf.d/default.conf

text

---

## Consul Issues (Phase 9)

### Problem: Services Not Registering with Consul

**Will be covered in Phase 9 with specific network solutions**

**Preview**: Use `network_mode: host` or register with `host.docker.internal`

---

## General Debugging Commands

View all containers
docker ps -a

Inspect container details
docker inspect libhub-userservice

View resource usage
docker stats

Clean up everything
docker system prune -a --volumes

Rebuild specific service
docker compose build --no-cache userservice

Restart specific service
docker compose restart userservice

View container filesystem
docker exec libhub-userservice ls -la /app

text

---

## Getting Help

1. Check logs: `docker compose logs -f [service]`
2. Inspect network: `docker network inspect libhub_libhub-network`
3. Check environment: `docker exec [container] printenv`
4. Verify configuration: Review docker-compose.yml and Dockerfiles

---

## Related Documentation

- [Docker Setup](./DOCKER_SETUP.md)
- [Deployment Guide](./DEPLOYMENT_GUIDE.md)