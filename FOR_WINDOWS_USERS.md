# 🎯 LibHub Setup Guide for Windows Users

Hey! This guide will help you run the entire LibHub project on your Windows machine with just a few commands.

## 📋 What You Need

1. **Windows 10 or 11** (64-bit)
2. **4GB RAM** available (8GB recommended)
3. **10GB free disk space**
4. **Internet connection** (for downloading Docker images)

## 🔧 Step-by-Step Setup

### Step 1: Install Docker Desktop (One-time setup)

1. Go to: https://www.docker.com/products/docker-desktop
2. Click "Download for Windows"
3. Run the installer (it's about 500MB)
4. Follow the installation wizard
5. **Restart your computer** when prompted
6. Start Docker Desktop from the Start menu
7. Wait for Docker to fully start (you'll see a whale icon in your system tray)

**First time?** Docker might ask you to enable WSL 2 - just click "Yes" and let it install.

### Step 2: Install Git (One-time setup)

1. Go to: https://git-scm.com/download/win
2. Download and install Git for Windows
3. Use default settings during installation

### Step 3: Clone the Project

1. Open **Command Prompt** (press `Win + R`, type `cmd`, press Enter)
2. Navigate to where you want the project:
   ```cmd
   cd %USERPROFILE%\Documents
   ```
3. Clone the repository:
   ```cmd
   git clone <repository-url> LibHub
   cd LibHub
   ```

### Step 4: Run the Setup Script

In the same Command Prompt window, run:

```cmd
setup-windows.bat
```

**What happens now?**
- Docker will download all necessary images (first time only, ~2GB)
- Build the application containers
- Start all services (MySQL, Consul, 3 microservices, Gateway, Frontend)
- Run health checks
- Show you the access URLs

**⏱️ Time:** First run takes 3-5 minutes. Subsequent runs take ~30 seconds.

### Step 5: Access the Application

Once the setup completes, open your browser and go to:

**🌐 http://localhost:8080**

You should see the LibHub frontend!

Other URLs:
- **API Gateway**: http://localhost:5000
- **Consul Dashboard**: http://localhost:8500 (see all running services)

## ✅ Verify Everything Works

Run the test script:

```cmd
scripts\test-docker-containers.bat
```

You should see green "[OK]" messages for all tests.

## 🎮 Using the Application

### Starting the Application (After First Setup)

```cmd
docker compose up -d
```

Wait 30 seconds, then access http://localhost:8080

### Stopping the Application

```cmd
docker compose down
```

This stops all services but keeps your data.

### Viewing Logs (If Something Goes Wrong)

```cmd
docker compose logs -f
```

Press `Ctrl + C` to stop viewing logs.

### Restarting Everything

```cmd
docker compose restart
```

## 🆘 Common Issues and Solutions

### Issue 1: "Docker is not running"

**Solution:** 
1. Look for the Docker whale icon in your system tray (bottom-right)
2. If it's not there, start Docker Desktop from the Start menu
3. Wait until the icon stops animating
4. Try the setup script again

### Issue 2: "Port already in use"

**Solution:**
Some other program is using the required ports. Check what's using port 5000:

```cmd
netstat -ano | findstr :5000
```

Either close that program or change the port in `docker-compose.yml`.

### Issue 3: Services Not Starting

**Solution:**
Wait a bit longer (services need time to start), then test:

```cmd
timeout /t 30
scripts\test-docker-containers.bat
```

### Issue 4: "Cannot connect to MySQL"

**Solution:**
MySQL might still be initializing. Wait 60 seconds and try again.

### Issue 5: Everything is Broken

**Nuclear option - Clean slate:**

```cmd
docker compose down -v
docker system prune -a
setup-windows.bat
```

This removes everything and starts fresh.

## 📚 What's Actually Running?

When you run the setup, Docker creates 7 containers:

1. **MySQL** - Database for all services
2. **Consul** - Service discovery (helps services find each other)
3. **UserService** - Handles user authentication and login
4. **CatalogService** - Manages the book catalog
5. **LoanService** - Handles book borrowing and returns
6. **Gateway** - Routes requests to the right service
7. **Frontend** - The web interface you see

All these services talk to each other automatically through Docker networking!

## 🔍 Exploring the System

### See All Running Containers

```cmd
docker compose ps
```

### Check Consul Dashboard

Go to http://localhost:8500 to see:
- All registered services
- Health status of each service
- How many instances are running

### Access the Database

```cmd
docker exec -it libhub-mysql mysql -u libhub_user -pLibHub@Dev2025
```

Then you can run SQL commands:
```sql
SHOW DATABASES;
USE user_db;
SHOW TABLES;
```

Type `exit` to leave MySQL.

## 🚀 Advanced: Scaling Services

Want to run multiple instances for load balancing?

```cmd
docker compose up -d --scale userservice=3 --scale catalogservice=2
```

This runs 3 instances of UserService and 2 of CatalogService. Consul automatically load-balances between them!

## 💡 Tips for Development

### After Making Code Changes

```cmd
docker compose up -d --build
```

This rebuilds and restarts the services.

### View Logs for Specific Service

```cmd
docker compose logs -f userservice
```

Replace `userservice` with `catalogservice`, `loanservice`, or `gateway`.

### Stop Everything and Remove Data

```cmd
docker compose down -v
```

**⚠️ Warning:** This deletes all data in the database!

## 📖 More Documentation

- **Quick reference**: `WINDOWS_QUICK_START.md`
- **Detailed guide**: `WINDOWS_SETUP.md`
- **All Docker commands**: `DOCKER_QUICK_START.md`
- **Troubleshooting**: `ai-docs/deployment/DOCKER_TROUBLESHOOTING.md`

## 🎓 Understanding the Architecture

This is a **microservices architecture**:
- Each service is independent
- They communicate through the API Gateway
- Consul helps them discover each other
- Everything runs in isolated Docker containers
- MySQL stores all the data

**Benefits:**
- ✅ Easy to scale individual services
- ✅ Services can be updated independently
- ✅ Fault isolation (one service failing doesn't crash everything)
- ✅ Each service can use different technologies

## 🤝 Getting Help

If you're stuck:

1. Check the logs: `docker compose logs -f`
2. Verify Docker is running: `docker ps`
3. Try the nuclear option (clean slate) above
4. Check the troubleshooting docs

## 🎉 You're Done!

That's it! You now have a complete microservices application running on your Windows machine.

**To summarize:**
1. Install Docker Desktop (one time)
2. Clone the repo
3. Run `setup-windows.bat`
4. Access http://localhost:8080

**To stop:** `docker compose down`

**To start again:** `docker compose up -d`

Happy coding! 🚀
