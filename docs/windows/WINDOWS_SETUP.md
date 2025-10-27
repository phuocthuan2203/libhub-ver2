# LibHub - Windows Setup Guide

Complete guide for setting up and running LibHub on Windows.

## Prerequisites

### Required Software

1. **Docker Desktop for Windows**
   - Download: https://www.docker.com/products/docker-desktop
   - Minimum version: 20.10 or higher
   - Enable WSL 2 backend (recommended)

2. **Git for Windows**
   - Download: https://git-scm.com/download/win
   - Or use GitHub Desktop: https://desktop.github.com/

3. **System Requirements**
   - Windows 10/11 (64-bit)
   - 4GB RAM minimum (8GB recommended)
   - 10GB free disk space

### Optional (for development)

- Visual Studio 2022 or Visual Studio Code
- .NET 8 SDK (if you want to build locally without Docker)

## Installation Steps

### 1. Install Docker Desktop

1. Download Docker Desktop from the link above
2. Run the installer
3. Follow the installation wizard
4. **Important**: Enable WSL 2 when prompted
5. Restart your computer if required
6. Start Docker Desktop
7. Wait for Docker to be fully running (whale icon in system tray)

### 2. Verify Docker Installation

Open **Command Prompt** or **PowerShell** and run:

```cmd
docker --version
docker compose version
```

You should see version information for both commands.

### 3. Clone the Repository

**Using Command Prompt:**
```cmd
cd %USERPROFILE%\Documents
git clone <repository-url> LibHub
cd LibHub
```

**Using PowerShell:**
```powershell
cd $HOME\Documents
git clone <repository-url> LibHub
cd LibHub
```

## Quick Start

### Method 1: Batch Script (Easiest)

Open **Command Prompt** in the LibHub directory and run:

```cmd
setup-windows.bat
```

This script will:
- Check if Docker is running
- Build all Docker images
- Start all services
- Wait for initialization
- Run health checks
- Display access URLs

**First run takes 3-5 minutes** to download images and build services.

### Method 2: PowerShell Script

Open **PowerShell** in the LibHub directory and run:

```powershell
.\setup-windows.ps1
```

If you get an execution policy error, run:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

Then try again.

### Method 3: Manual Docker Compose

```cmd
docker compose up -d --build
```

Wait 60 seconds, then test:
```cmd
scripts\test-docker-containers.bat
```

## Accessing the Application

Once setup is complete, open your browser:

- **Frontend**: http://localhost:8080
- **API Gateway**: http://localhost:5000
- **Consul UI**: http://localhost:8500

## Testing

### Run All Tests (Batch)

```cmd
scripts\test-docker-containers.bat
scripts\test-consul-discovery.bat
scripts\test-gateway-integration.bat
```

### Run Tests (PowerShell)

```powershell
.\scripts\test-docker-containers.ps1
```

## Common Commands

### View Running Containers

```cmd
docker compose ps
```

### View Logs

**All services:**
```cmd
docker compose logs -f
```

**Specific service:**
```cmd
docker compose logs -f userservice
docker compose logs -f catalogservice
docker compose logs -f loanservice
docker compose logs -f gateway
```

Press `Ctrl+C` to stop viewing logs.

### Stop Services

**Stop all (keeps data):**
```cmd
docker compose down
```

**Stop and remove data:**
```cmd
docker compose down -v
```

### Restart Services

```cmd
docker compose restart
```

### Rebuild After Code Changes

```cmd
docker compose up -d --build
```

### Scale Services

```cmd
docker compose up -d --scale userservice=3 --scale catalogservice=2
```

## Troubleshooting

### Docker Desktop Not Starting

1. Check if Hyper-V or WSL 2 is enabled
2. Restart Docker Desktop
3. Restart your computer
4. Check Docker Desktop logs: Settings → Troubleshoot → Show logs

### Port Already in Use

If you get "port already allocated" error:

1. Check what's using the port:
   ```cmd
   netstat -ano | findstr :5000
   ```

2. Stop the process or change the port in `docker-compose.yml`

### Services Not Healthy

Wait longer (services need 60-90 seconds to start):
```cmd
timeout /t 30
scripts\test-docker-containers.bat
```

### Cannot Connect to MySQL

```cmd
docker exec -it libhub-mysql mysql -u libhub_user -pLibHub@Dev2025
```

If this fails, check MySQL logs:
```cmd
docker compose logs mysql
```

### Build Failures

Clean everything and rebuild:
```cmd
docker compose down -v
docker system prune -a
docker compose up -d --build
```

### WSL 2 Issues

If Docker Desktop shows WSL 2 errors:

1. Open PowerShell as Administrator
2. Run:
   ```powershell
   wsl --update
   wsl --set-default-version 2
   ```
3. Restart Docker Desktop

## File Paths on Windows

When working with scripts:

- Use backslashes: `scripts\test.bat`
- Or forward slashes work too: `scripts/test.bat`
- For PowerShell: `.\scripts\test.ps1`

## Performance Tips

### Improve Docker Performance

1. **Allocate more resources** to Docker:
   - Docker Desktop → Settings → Resources
   - Increase CPUs to 4
   - Increase Memory to 6-8GB

2. **Use WSL 2 backend** (faster than Hyper-V)

3. **Exclude from antivirus**:
   - Add Docker directories to Windows Defender exclusions
   - Add your project directory to exclusions

### Faster Rebuilds

Use BuildKit for faster builds:
```cmd
set DOCKER_BUILDKIT=1
docker compose build
```

## Development Workflow

### 1. Start Services
```cmd
setup-windows.bat
```

### 2. Make Code Changes
Edit files in `src/` directory

### 3. Rebuild Changed Service
```cmd
docker compose up -d --build userservice
```

### 4. View Logs
```cmd
docker compose logs -f userservice
```

### 5. Test Changes
```cmd
scripts\test-docker-containers.bat
```

### 6. Stop When Done
```cmd
docker compose down
```

## Database Management

### Connect to MySQL

```cmd
docker exec -it libhub-mysql mysql -u libhub_user -pLibHub@Dev2025
```

### View Databases

```sql
SHOW DATABASES;
USE user_db;
SHOW TABLES;
```

### Backup Database

```cmd
docker exec libhub-mysql mysqldump -u libhub_user -pLibHub@Dev2025 user_db > backup.sql
```

### Restore Database

```cmd
docker exec -i libhub-mysql mysql -u libhub_user -pLibHub@Dev2025 user_db < backup.sql
```

## Useful Docker Commands

### Remove All Containers
```cmd
docker rm -f $(docker ps -aq)
```

### Remove All Images
```cmd
docker rmi -f $(docker images -q)
```

### View Disk Usage
```cmd
docker system df
```

### Clean Up Everything
```cmd
docker system prune -a --volumes
```

## Getting Help

### Check Service Health

```cmd
docker compose ps
docker compose logs [service-name]
```

### Verify Network

```cmd
docker network ls
docker network inspect libhub_libhub-network
```

### Check Consul

Visit http://localhost:8500 to see:
- Registered services
- Health checks
- Service instances

## Next Steps

1. Read `DOCKER_QUICK_START.md` for more Docker commands
2. Check `ai-docs/deployment/DEPLOYMENT_GUIDE.md` for architecture details
3. Explore the Consul UI at http://localhost:8500
4. Test the API using the Frontend at http://localhost:8080

## Support

For issues:
1. Check `DOCKER_TROUBLESHOOTING.md` in `ai-docs/deployment/`
2. View logs: `docker compose logs -f`
3. Check Docker Desktop logs
4. Verify all prerequisites are installed

## Summary

**To run LibHub on Windows:**

1. Install Docker Desktop
2. Clone the repository
3. Run `setup-windows.bat`
4. Access http://localhost:8080

That's it! The entire microservices architecture runs in Docker containers.
