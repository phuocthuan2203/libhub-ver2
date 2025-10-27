# LibHub - Windows Quick Start

## 🚀 Get Started in 3 Steps

### Step 1: Install Docker Desktop
Download and install from: https://www.docker.com/products/docker-desktop

### Step 2: Clone Repository
```cmd
git clone <repository-url> LibHub
cd LibHub
```

### Step 3: Run Setup
```cmd
setup-windows.bat
```

**That's it!** Wait 3-5 minutes for first-time setup.

## 🌐 Access Points

- **Frontend**: http://localhost:8080
- **API Gateway**: http://localhost:5000
- **Consul Dashboard**: http://localhost:8500

## 📋 Common Commands

| Action | Command |
|--------|---------|
| **Start** | `docker compose up -d` |
| **Stop** | `docker compose down` |
| **Restart** | `docker compose restart` |
| **View Logs** | `docker compose logs -f` |
| **Rebuild** | `docker compose up -d --build` |
| **Test** | `scripts\test-docker-containers.bat` |

## 🔧 Troubleshooting

### Services not starting?
Wait 60 seconds, then run:
```cmd
scripts\test-docker-containers.bat
```

### Port conflicts?
Check what's using ports:
```cmd
netstat -ano | findstr :5000
```

### Need to clean up?
```cmd
docker compose down -v
docker system prune -a
```

## 📚 More Help

- Full guide: `WINDOWS_SETUP.md`
- Docker commands: `DOCKER_QUICK_START.md`
- Troubleshooting: `ai-docs/deployment/DOCKER_TROUBLESHOOTING.md`

## ✅ What's Running?

After setup, you'll have:
- ✅ MySQL Database
- ✅ Consul Service Discovery
- ✅ User Service (Authentication)
- ✅ Catalog Service (Books)
- ✅ Loan Service (Borrowing)
- ✅ API Gateway
- ✅ Frontend UI

All running in Docker containers!
