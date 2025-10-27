@echo off
setlocal enabledelayedexpansion

echo ========================================
echo LibHub - Windows Setup Script
echo ========================================
echo.

echo Checking prerequisites...
echo.

docker --version >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Docker is not installed or not in PATH
    echo Please install Docker Desktop from: https://www.docker.com/products/docker-desktop
    exit /b 1
)
echo [OK] Docker is installed

docker info >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Docker is not running
    echo Please start Docker Desktop and try again
    exit /b 1
)
echo [OK] Docker is running

echo.
echo ========================================
echo Starting LibHub Services...
echo ========================================
echo.

echo This will:
echo 1. Build all Docker images
echo 2. Start all services (Consul, MySQL, Microservices, Gateway, Frontend)
echo 3. Wait for services to be ready
echo 4. Run health checks
echo.
echo This may take 3-5 minutes on first run...
echo.

docker compose up -d --build

if errorlevel 1 (
    echo.
    echo [ERROR] Failed to start services
    echo Check the error messages above
    exit /b 1
)

echo.
echo ========================================
echo Waiting for services to initialize...
echo ========================================
echo.

echo Waiting 60 seconds for all services to be ready...
timeout /t 60 /nobreak >nul

echo.
echo ========================================
echo Running Health Checks...
echo ========================================
echo.

call scripts\test-docker-containers.bat

if errorlevel 1 (
    echo.
    echo [WARNING] Some health checks failed
    echo Services might still be starting up
    echo Wait a bit longer and run: scripts\test-docker-containers.bat
)

echo.
echo ========================================
echo Setup Complete!
echo ========================================
echo.
echo Your LibHub instance is running at:
echo.
echo   Frontend:  http://localhost:8080
echo   Gateway:   http://localhost:5000
echo   Consul UI: http://localhost:8500
echo.
echo To view logs:        docker compose logs -f
echo To stop services:    docker compose down
echo To restart:          docker compose restart
echo.
echo For more commands, see DOCKER_QUICK_START.md
echo.

endlocal
