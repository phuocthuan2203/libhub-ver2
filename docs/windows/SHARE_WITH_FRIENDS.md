# 🚀 Run LibHub on Your Windows PC

## What You'll Get

A complete library management system with:
- User authentication
- Book catalog with 15 pre-loaded books
- Loan management system
- Modern web interface
- Microservices architecture with service discovery

All running locally on your machine!

## Prerequisites (Install These First)

### 1. Docker Desktop
- **Download**: https://www.docker.com/products/docker-desktop
- **Size**: ~500MB
- **Time**: 5-10 minutes to install
- **Important**: Restart your PC after installation

### 2. Git for Windows
- **Download**: https://git-scm.com/download/win
- **Size**: ~50MB
- **Time**: 2-3 minutes to install

## Installation (3 Commands)

### 1. Clone the Repository

Open **Command Prompt** (`Win + R`, type `cmd`, Enter):

```cmd
cd %USERPROFILE%\Documents
git clone <REPOSITORY_URL_HERE> LibHub
cd LibHub
```

### 2. Run Setup

```cmd
setup-windows.bat
```

**Wait 3-5 minutes** (first time only - downloading images and building)

### 3. Open Your Browser

Go to: **http://localhost:8080**

🎉 **Done!** You're running the entire application!

## Daily Usage

### Start the Application
```cmd
cd %USERPROFILE%\Documents\LibHub
docker compose up -d
```
Wait 30 seconds, then go to http://localhost:8080

### Stop the Application
```cmd
docker compose down
```

### View What's Running
```cmd
docker compose ps
```

## Access Points

| Service | URL |
|---------|-----|
| **Main App** | http://localhost:8080 |
| **API Gateway** | http://localhost:5000 |
| **Service Dashboard** | http://localhost:8500 |

## Test Everything Works

```cmd
scripts\test-docker-containers.bat
```

You should see green "[OK]" messages.

## Troubleshooting

### Docker Not Running?
1. Start Docker Desktop from Start menu
2. Wait for whale icon in system tray
3. Try again

### Ports Already in Use?
```cmd
netstat -ano | findstr :5000
```
Close the program using that port, or restart your PC.

### Still Not Working?
Clean everything and start fresh:
```cmd
docker compose down -v
docker system prune -a
setup-windows.bat
```

## What's Inside?

When running, you have:
- ✅ MySQL Database
- ✅ Consul (Service Discovery)
- ✅ User Service (Login/Register)
- ✅ Catalog Service (Books)
- ✅ Loan Service (Borrowing)
- ✅ API Gateway
- ✅ Web Frontend

All in Docker containers, automatically connected!

## System Requirements

- Windows 10/11 (64-bit)
- 4GB RAM available (8GB recommended)
- 10GB free disk space
- Internet (for initial download)

## Need More Help?

Read these files in the project:
- `FOR_WINDOWS_USERS.md` - Detailed guide
- `WINDOWS_QUICK_START.md` - Quick reference
- `WINDOWS_SETUP.md` - Complete setup guide
- `DOCKER_QUICK_START.md` - All Docker commands

## Commands Cheat Sheet

```cmd
docker compose up -d              Start all services
docker compose down               Stop all services
docker compose restart            Restart all services
docker compose logs -f            View logs
docker compose ps                 See what's running
scripts\test-docker-containers.bat  Run tests
```

## That's It!

**To run LibHub:**
1. Install Docker Desktop + Git
2. Clone repo
3. Run `setup-windows.bat`
4. Open http://localhost:8080

**Questions?** Check the documentation files or ask me!

---

**Repository**: <REPOSITORY_URL_HERE>

**Made with**: ASP.NET Core, MySQL, Docker, Consul, Microservices Architecture
