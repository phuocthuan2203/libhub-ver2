# LibHub - Windows Setup Guide

This guide will help you run the LibHub project on your Windows machine.

## Prerequisites

Before you begin, ensure you have:

1. **Docker Desktop for Windows** installed and running
   - Download from: https://www.docker.com/products/docker-desktop
   - Make sure Docker Desktop is running before proceeding
   
2. **Git** installed (to clone the repository)
   - Download from: https://git-scm.com/download/win

## Quick Start

### Step 1: Clone the Repository

Open PowerShell or Command Prompt and run:

```bash
git clone https://github.com/YOUR_USERNAME/LibHub.git
cd LibHub
```

### Step 2: Run the Setup Script

Choose one of the following methods:

#### Option A: Using Batch Script (Command Prompt)

```cmd
setup-windows.bat
```

#### Option B: Using PowerShell Script

```powershell
.\setup-windows.ps1
```

### Step 3: Wait for Services to Start

The script will:
- Check if Docker is installed and running
- Build all Docker images (this takes 3-5 minutes on first run)
- Start all services (Consul, MySQL, Microservices, Gateway, Frontend)
- Run health checks
- Display access URLs

### Step 4: Apply Seed Data (Optional)

After all services are running and healthy, apply the seed data to populate the database with sample books and users:

```cmd
scripts\apply-seed-data.bat
```

This will add:
- 7 sample books (each with 100 copies)
- Admin and test user accounts

### Step 5: Access the Application

Once setup is complete, you can access:

- **Frontend**: http://localhost:8080
- **API Gateway**: http://localhost:5000
- **Consul UI**: http://localhost:8500

## Important Notes

### MySQL Port Configuration

The project uses **port 3308** for MySQL instead of the default 3306. This prevents conflicts if you have MySQL installed locally on your machine.

### Default Credentials

Use these credentials to log in:

- **Admin Account**
  - Username: `admin`
  - Password: `admin123` (or check seed data)
  
- **Test User Account**
  - Username: `testuser`
  - Password: `test123` (or check seed data)

## Common Commands

### View Logs

To see what's happening in your containers:

```bash
docker compose -f docker-compose.windows.yml logs -f
```

To view logs for a specific service:

```bash
docker compose -f docker-compose.windows.yml logs -f [service-name]
```

Service names: `consul`, `mysql`, `userservice`, `catalogservice`, `loanservice`, `gateway`, `frontend`

### Stop Services

To stop all running services:

```bash
docker compose -f docker-compose.windows.yml down
```

### Restart Services

To restart all services:

```bash
docker compose -f docker-compose.windows.yml restart
```

To restart a specific service:

```bash
docker compose -f docker-compose.windows.yml restart [service-name]
```

### Rebuild and Restart

If you made code changes and want to rebuild:

```bash
docker compose -f docker-compose.windows.yml up -d --build
```

## Troubleshooting

### Docker is not running

**Error**: "Docker is not running"

**Solution**: 
1. Open Docker Desktop
2. Wait for it to fully start (whale icon in system tray should be steady)
3. Run the setup script again

### Port Already in Use

**Error**: "Port 8080 (or 5000, 8500) is already allocated"

**Solution**:
1. Check what's using the port:
   ```powershell
   netstat -ano | findstr :[PORT]
   ```
2. Stop the conflicting application or change the port in `docker-compose.windows.yml`

### Services Not Starting

**Error**: Services fail to start or health checks fail

**Solution**:
1. Check Docker Desktop has enough resources (Settings > Resources)
   - Recommended: 4GB RAM minimum, 2 CPUs
2. View logs to see specific errors:
   ```bash
   docker compose -f docker-compose.windows.yml logs
   ```
3. Try stopping and restarting:
   ```bash
   docker compose -f docker-compose.windows.yml down
   docker compose -f docker-compose.windows.yml up -d --build
   ```

### MySQL Connection Issues

**Error**: "Can't connect to MySQL server"

**Solution**:
1. Wait a bit longer - MySQL takes time to initialize on first run
2. Check MySQL container status:
   ```bash
   docker compose -f docker-compose.windows.yml ps
   ```
3. View MySQL logs:
   ```bash
   docker compose -f docker-compose.windows.yml logs mysql
   ```

### Build Failures

**Error**: Docker build fails

**Solution**:
1. Ensure you have a stable internet connection (downloads NuGet packages)
2. Clear Docker cache and rebuild:
   ```bash
   docker compose -f docker-compose.windows.yml down
   docker system prune -a
   docker compose -f docker-compose.windows.yml up -d --build
   ```

## Clean Up

To completely remove all containers, networks, and volumes:

```bash
docker compose -f docker-compose.windows.yml down -v
```

**Warning**: This will delete all data in the MySQL database!

## Getting Help

If you encounter issues:

1. Check the logs for error messages
2. Ensure all prerequisites are met
3. Try the troubleshooting steps above
4. Contact the project maintainer

## Architecture Overview

The LibHub project consists of:

- **Frontend**: Nginx serving HTML/CSS/JS
- **API Gateway**: Routes requests to microservices
- **User Service**: Handles authentication and user management
- **Catalog Service**: Manages book catalog
- **Loan Service**: Handles book borrowing/returning
- **Consul**: Service discovery and health checking
- **MySQL**: Database for all services

All services run in Docker containers and communicate through a Docker network.
