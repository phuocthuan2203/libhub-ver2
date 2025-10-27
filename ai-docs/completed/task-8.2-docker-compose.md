# Task 8.2: Docker Compose Setup for LibHub

This document provides instructions for setting up a `docker-compose.yml` file to orchestrate all LibHub services, ensuring proper networking, dependencies, and environment configuration.

## 1. Create docker-compose.yml at Project Root

Place the following content in `docker-compose.yml` at the project root (`/home/thuannp4/development/LibHub/`):

```yaml
version: '3.8'
services:
  mysql:
    image: mysql:8.0
    ports:
      - "3306:3306"
    volumes:
      - mysql-data:/var/lib/mysql
      - ./scripts/init-databases.sql:/docker-entrypoint-initdb.d/init-databases.sql
    environment:
      MYSQL_ROOT_PASSWORD: LibHub@2025
      MYSQL_USER: libhub_user
      MYSQL_PASSWORD: LibHub@Dev2025
    healthcheck:
      test: ["CMD", "mysqladmin", "ping", "-h", "localhost"]
      interval: 10s
      timeout: 5s
      retries: 3
    networks:
      - libhub-network

  userservice:
    build: ./src/Services/UserService/LibHub.UserService.Api
    ports:
      - "5002:5002"
    depends_on:
      mysql:
        condition: service_healthy
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=mysql;Port=3306;Database=user_db;User=libhub_user;Password=LibHub@Dev2025;
      - Jwt__SecretKey=LibHub_SuperSecret_256BitKey_ForDevelopment_ChangeInProduction!
      - Jwt__Issuer=LibHub.UserService
      - Jwt__Audience=LibHub.Clients
    networks:
      - libhub-network

  catalogservice:
    build: ./src/Services/CatalogService/LibHub.CatalogService.Api
    ports:
      - "5001:5001"
    depends_on:
      mysql:
        condition: service_healthy
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=mysql;Port=3306;Database=catalog_db;User=libhub_user;Password=LibHub@Dev2025;
      - Jwt__SecretKey=LibHub_SuperSecret_256BitKey_ForDevelopment_ChangeInProduction!
      - Jwt__Issuer=LibHub.UserService
      - Jwt__Audience=LibHub.Clients
    networks:
      - libhub-network

  loanservice:
    build: ./src/Services/LoanService/LibHub.LoanService.Api
    ports:
      - "5003:5003"
    depends_on:
      mysql:
        condition: service_healthy
      catalogservice:
        condition: service_started
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Server=mysql;Port=3306;Database=loan_db;User=libhub_user;Password=LibHub@Dev2025;
      - Jwt__SecretKey=LibHub_SuperSecret_256BitKey_ForDevelopment_ChangeInProduction!
      - Jwt__Issuer=LibHub.UserService
      - Jwt__Audience=LibHub.Clients
      - ExternalServices__CatalogServiceBaseUrl=http://catalogservice:5001
    networks:
      - libhub-network

  gateway:
    build: ./src/Gateway/LibHub.Gateway.Api
    ports:
      - "5000:5000"
    depends_on:
      - userservice
      - catalogservice
      - loanservice
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Jwt__SecretKey=LibHub_SuperSecret_256BitKey_ForDevelopment_ChangeInProduction!
      - Jwt__Issuer=LibHub.UserService
      - Jwt__Audience=LibHub.Clients
    networks:
      - libhub-network

  frontend:
    build: ./frontend
    ports:
      - "8080:8080"
    depends_on:
      - gateway
    networks:
      - libhub-network

networks:
  libhub-network:
    driver: bridge

volumes:
  mysql-data:
```

## 2. Network Configuration

- A custom bridge network named `libhub-network` is defined.
- All services are connected to this network for internal communication using container names.

## 3. Environment Variables

- **MySQL**: Configured with root password and user credentials.
- **Services**: Connection strings use `Server=mysql` to reference the MySQL container.
- **JWT**: Consistent secret key across all services for token validation.
- **LoanService**: Uses `http://catalogservice:5001` for inter-service communication.

## 4. Create scripts/init-databases.sql

Create the file at `/home/thuannp4/development/LibHub/scripts/init-databases.sql` with:

```sql
CREATE DATABASE IF NOT EXISTS user_db;
CREATE DATABASE IF NOT EXISTS catalog_db;
CREATE DATABASE IF NOT EXISTS loan_db;
CREATE USER IF NOT EXISTS 'libhub_user'@'%' IDENTIFIED BY 'LibHub@Dev2025';
GRANT ALL PRIVILEGES ON user_db.* TO 'libhub_user'@'%';
GRANT ALL PRIVILEGES ON catalog_db.* TO 'libhub_user'@'%';
GRANT ALL PRIVILEGES ON loan_db.* TO 'libhub_user'@'%';
FLUSH PRIVILEGES;
```

## 5. Update ocelot.json in Gateway

Update `/home/thuannp4/development/LibHub/src/Gateway/LibHub.Gateway.Api/ocelot.json` to use container names:

```json
"DownstreamHostAndPorts": [
  {
    "Host": "userservice",
    "Port": 5002
  }
],
"DownstreamHostAndPorts": [
  {
    "Host": "catalogservice",
    "Port": 5001
  }
],
"DownstreamHostAndPorts": [
  {
    "Host": "loanservice",
    "Port": 5003
  }
]
```

## 6. Docker Compose Commands

- Build all services: `docker compose build`
- Start services in detached mode: `docker compose up -d`
- View logs: `docker compose logs -f`
- Stop services: `docker compose down`
