# Task 8.1: Dockerfile Implementation for LibHub Services

## Overview
Create optimized multi-stage Dockerfiles for all LibHub services and frontend.

## Prerequisites
- Docker 24.0+
- .NET 8.0 SDK
- Node.js 18+ (for frontend build)
- MySQL 8.0+

## 1. .dockerignore
Create at project root (`.dockerignore`):

```dockerignore
**/bin/
**/obj/
**/node_modules/
**/.git/
**/.vs/
**/.vscode/
*.db
*.user
*.suo
*.userosscache
*.sln.docstates
```

## 2. Dockerfile for .NET Services
Create these files with the same structure, changing only the service name and port:

### UserService (`src/Services/UserService/LibHub.UserService.Api/Dockerfile`)
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["src/Services/UserService/LibHub.UserService.Api/LibHub.UserService.Api.csproj", "src/Services/UserService/LibHub.UserService.Api/"]
COPY ["src/Services/UserService/LibHub.UserService.Application/LibHub.UserService.Application.csproj", "src/Services/UserService/LibHub.UserService.Application/"]
COPY ["src/Services/UserService/LibHub.UserService.Domain/LibHub.UserService.Domain.csproj", "src/Services/UserService/LibHub.UserService.Domain/"]
COPY ["src/Services/UserService/LibHub.UserService.Infrastructure/LibHub.UserService.Infrastructure.csproj", "src/Services/UserService/LibHub.UserService.Infrastructure/"]

RUN dotnet restore "src/Services/UserService/LibHub.UserService.Api/LibHub.UserService.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/Services/UserService/LibHub.UserService.Api"
RUN dotnet build "LibHub.UserService.Api.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "LibHub.UserService.Api.csproj" -c Release -o /app/publish

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5002
ENV ASPNETCORE_URLS=http://+:5002
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LibHub.UserService.Api.dll"]
```

### CatalogService (`src/Services/CatalogService/LibHub.CatalogService.Api/Dockerfile`)
Same as above, but:
- Update file paths to use `CatalogService`
- Change `EXPOSE` to `5001`
- Update `ASPNETCORE_URLS` to `http://+:5001`
- Update `ENTRYPOINT` to use `LibHub.CatalogService.Api.dll`

### LoanService (`src/Services/LoanService/LibHub.LoanService.Api/Dockerfile`)
Same as above, but:
- Update file paths to use `LoanService`
- Change `EXPOSE` to `5003`
- Update `ASPNETCORE_URLS` to `http://+:5003`
- Update `ENTRYPOINT` to use `LibHub.LoanService.Api.dll`

### Gateway (`src/Gateway/LibHub.Gateway.Api/Dockerfile`)
Same as above, but:
- Update file paths to use `Gateway`
- Change `EXPOSE` to `5000`
- Update `ASPNETCORE_URLS` to `http://+:5000`
- Update `ENTRYPOINT` to use `LibHub.Gateway.Api.dll`

## 3. Frontend Dockerfile
Create `frontend/Dockerfile`:

```dockerfile
# Build stage
FROM node:18-alpine AS build
WORKDIR /app

# Copy package files
COPY ["frontend/package.json", "frontend/package-lock.json*", "./"]
RUN npm ci

# Copy and build app
COPY ["frontend/", "./"]
RUN npm run build

# Runtime stage
FROM nginx:alpine
COPY --from=build /app/dist /usr/share/nginx/html
COPY ["frontend/nginx.conf", "/etc/nginx/conf.d/default.conf"]
EXPOSE 8080
CMD ["nginx", "-g", "daemon off;"]
```

## 4. Nginx Configuration
Create `frontend/nginx.conf`:

```nginx
server {
    listen 8080;
    server_name localhost;
    
    location / {
        root /usr/share/nginx/html;
        try_files $uri $uri/ /index.html;
    }
    
    location /api/ {
        proxy_pass http://gateway:5000/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

## 5. Building and Testing

### Build Commands
```bash
# Build all services
docker build -t libhub/userservice -f src/Services/UserService/LibHub.UserService.Api/Dockerfile .
docker build -t libhub/catalogservice -f src/Services/CatalogService/LibHub.CatalogService.Api/Dockerfile .
docker build -t libhub/loanservice -f src/Services/LoanService/LibHub.LoanService.Api/Dockerfile .
docker build -t libhub/gateway -f src/Gateway/LibHub.Gateway.Api/Dockerfile .
docker build -t libhub/frontend -f frontend/Dockerfile .
```

### Test Commands
```bash
# Test UserService
docker run -d -p 5002:5002 --name userservice_test libhub/userservice
curl http://localhost:5002/health

docker stop userservice_test
docker rm userservice_test

# Test Frontend
docker run -d -p 8080:8080 --name frontend_test libhub/frontend
```

## 6. Docker Compose (Optional)
Create `docker-compose.yml` in project root:

```yaml
version: '3.8'

services:
  userservice:
    image: libhub/userservice
    ports:
      - "5002:5002"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=mysql;Port=3306;Database=user_db;User=libhub_user;Password=LibHub@Dev2025;
    depends_on:
      - mysql

  catalogservice:
    image: libhub/catalogservice
    ports:
      - "5001:5001"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=mysql;Port=3306;Database=catalog_db;User=libhub_user;Password=LibHub@Dev2025;
    depends_on:
      - mysql

  loanservice:
    image: libhub/loanservice
    ports:
      - "5003:5003"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=mysql;Port=3306;Database=loan_db;User=libhub_user;Password=LibHub@Dev2025;
    depends_on:
      - mysql

  gateway:
    image: libhub/gateway
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    depends_on:
      - userservice
      - catalogservice
      - loanservice

  frontend:
    image: libhub/frontend
    ports:
      - "8080:8080"
    depends_on:
      - gateway

  mysql:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: root
      MYSQL_DATABASE: libhub
      MYSQL_USER: libhub_user
      MYSQL_PASSWORD: LibHub@Dev2025
    ports:
      - "3306:3306"
    volumes:
      - mysql_data:/var/lib/mysql

volumes:
  mysql_data:
```

## 7. Next Steps
1. Run `docker-compose up -d` to start all services
2. Access the application at http://localhost:8080
3. API Gateway will be available at http://localhost:5000
4. Individual services:
   - UserService: http://localhost:5002
   - CatalogService: http://localhost:5001
   - LoanService: http://localhost:5003

## Notes
- All services use multi-stage builds for smaller image sizes
- Environment variables should be moved to `.env` files in production
- Database connection strings are configured for Docker Compose networking
- Frontend is configured to proxy API requests to the gateway service
- Health check endpoints are available at `/health` for each service