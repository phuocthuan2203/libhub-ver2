# LibHub Documentation

Complete documentation for the LibHub Library Management System.

## 📖 Quick Navigation

### 🪟 Windows Users
Start here if you're using Windows:
- **[Share With Friends](windows/SHARE_WITH_FRIENDS.md)** - Simplest guide to share
- **[For Windows Users](windows/FOR_WINDOWS_USERS.md)** - Comprehensive friendly guide
- **[Windows Quick Start](windows/WINDOWS_QUICK_START.md)** - Quick reference card
- **[Windows Setup Guide](windows/WINDOWS_SETUP.md)** - Complete technical setup
- **[Instructions for Sharing](windows/INSTRUCTIONS_FOR_SHARING.md)** - How to share this project
- **[Windows Setup Summary](windows/WINDOWS_SETUP_SUMMARY.md)** - Implementation details

### 🐳 Deployment & Docker
Docker and deployment guides:
- **[Docker Quick Start](deployment/DOCKER_QUICK_START.md)** - Main Docker guide (all platforms)
- **[Consul Service Discovery](deployment/CONSUL_SERVICE_DISCOVERY.md)** - Service discovery setup
- **[Testing Consul and Saga](deployment/TESTING_CONSUL_AND_SAGA.md)** - Testing distributed systems
- **[Consul Discovery Logs Guide](deployment/CONSUL_DISCOVERY_LOGS_GUIDE.md)** - Log analysis
- **[Log Analysis Summary](deployment/LOG_ANALYSIS_SUMMARY.md)** - System logs overview

### 💻 Development
Developer resources:
- **[Git Commands](development/GIT_COMMANDS.md)** - Git workflow and commands
- **[Connection Strings Template](connection-strings-template.md)** - Database configuration

### 🏗️ Architecture & Context
Deep dive into system architecture:
- **[Master Context](../ai-docs/master-context/)** - Project context and domain reference
- **[Service Context](../ai-docs/service-context/)** - Individual service documentation
- **[Completed Tasks](../ai-docs/completed/)** - Implementation history

## 🚀 Getting Started

### First Time Setup

#### Windows
```cmd
setup-windows.bat
```

#### Linux/Mac
```bash
docker compose up -d --build
```

### Access Points
- **Frontend**: http://localhost:8080
- **API Gateway**: http://localhost:5000
- **Consul UI**: http://localhost:8500

## 📂 Documentation Structure

```
docs/
├── README.md (this file)
├── windows/                    # Windows-specific guides
│   ├── SHARE_WITH_FRIENDS.md
│   ├── FOR_WINDOWS_USERS.md
│   ├── WINDOWS_QUICK_START.md
│   ├── WINDOWS_SETUP.md
│   ├── INSTRUCTIONS_FOR_SHARING.md
│   └── WINDOWS_SETUP_SUMMARY.md
├── deployment/                 # Docker & deployment
│   ├── DOCKER_QUICK_START.md
│   ├── CONSUL_SERVICE_DISCOVERY.md
│   ├── TESTING_CONSUL_AND_SAGA.md
│   ├── CONSUL_DISCOVERY_LOGS_GUIDE.md
│   └── LOG_ANALYSIS_SUMMARY.md
├── development/                # Developer guides
│   └── GIT_COMMANDS.md
└── connection-strings-template.md
```

## 🎯 Use Cases

### "I want to run this project on Windows"
→ Start with [windows/SHARE_WITH_FRIENDS.md](windows/SHARE_WITH_FRIENDS.md)

### "I want to understand Docker setup"
→ Read [deployment/DOCKER_QUICK_START.md](deployment/DOCKER_QUICK_START.md)

### "I want to learn about service discovery"
→ Check [deployment/CONSUL_SERVICE_DISCOVERY.md](deployment/CONSUL_SERVICE_DISCOVERY.md)

### "I want to contribute/develop"
→ See [development/GIT_COMMANDS.md](development/GIT_COMMANDS.md)

### "I want to understand the architecture"
→ Explore [../ai-docs/master-context/](../ai-docs/master-context/)

### "Something is not working"
→ Check troubleshooting in [windows/FOR_WINDOWS_USERS.md](windows/FOR_WINDOWS_USERS.md#-common-issues-and-solutions) or [deployment/DOCKER_QUICK_START.md](deployment/DOCKER_QUICK_START.md#troubleshooting)

## 🔧 Common Tasks

### Start the Application
```bash
docker compose up -d
```

### Stop the Application
```bash
docker compose down
```

### View Logs
```bash
docker compose logs -f
```

### Run Tests
**Windows:**
```cmd
scripts\test-docker-containers.bat
```

**Linux/Mac:**
```bash
./scripts/test-docker-containers.sh
```

### Scale Services
```bash
docker compose up -d --scale userservice=3 --scale catalogservice=2
```

## 📊 System Overview

LibHub consists of:
- **MySQL Database** - Data persistence
- **Consul** - Service discovery and health checks
- **UserService** - Authentication and user management
- **CatalogService** - Book catalog (15 books pre-loaded)
- **LoanService** - Loan operations with Saga pattern
- **API Gateway** - Request routing (Ocelot)
- **Frontend** - Web interface (Nginx)

All services run in Docker containers with automatic service discovery.

## 🆘 Getting Help

1. Check the relevant documentation above
2. Look at troubleshooting sections
3. Check logs: `docker compose logs -f`
4. Verify Docker is running: `docker ps`

## 📝 Contributing

See [development/GIT_COMMANDS.md](development/GIT_COMMANDS.md) for Git workflow.

## 🔗 External Resources

- [Docker Documentation](https://docs.docker.com/)
- [Consul Documentation](https://www.consul.io/docs)
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core/)
- [Ocelot Documentation](https://ocelot.readthedocs.io/)

---

**Need quick help?** Jump to [windows/WINDOWS_QUICK_START.md](windows/WINDOWS_QUICK_START.md) or [deployment/DOCKER_QUICK_START.md](deployment/DOCKER_QUICK_START.md)
