@echo off
echo Applying seed data to LibHub databases...
echo.

docker ps | findstr libhub-mysql >nul 2>&1
if errorlevel 1 (
    echo Error: MySQL container is not running
    echo Please start the containers first with: docker compose -f docker-compose.windows.yml up -d
    exit /b 1
)

echo Waiting for MySQL to be ready...
timeout /t 5 /nobreak >nul

echo Applying seed data...
docker exec -i libhub-mysql mysql -u libhub_user -pLibHub@Dev2025 < scripts\seed-data.sql

if errorlevel 1 (
    echo.
    echo [X] Failed to apply seed data
    echo Check if the services have created the tables first
    echo Wait a minute and try again
    exit /b 1
)

echo.
echo [OK] Seed data applied successfully!
echo.
echo You can now log in with:
echo   - Username: admin / Email: admin@libhub.com
echo   - Username: testuser / Email: test@libhub.com
