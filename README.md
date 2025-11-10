# LibHub - Library Management System

A modern microservices-based library management system built with ASP.NET Core 8.0, featuring service discovery, distributed transactions, and centralized logging.

## 🏗️ Architecture Overview

LibHub implements a complete microservices architecture with:
- **4 Independent Services** (User, Catalog, Loan, Gateway)
- **Consul** for service discovery and health monitoring
- **Saga Pattern** for distributed transactions
- **Serilog + Seq** for centralized logging and request tracing
- **JWT Authentication** with role-based access control
- **Docker Compose** for easy deployment

## 📁 Project Structure

```
libhub-ver2/
├── src/
│   ├── Gateway/
│   │   └── LibHub.Gateway.Api/          # Ocelot API Gateway (Port 5000)
│   └── Services/
│       ├── UserService/                  # Authentication & Identity (Port 5001)
│       ├── CatalogService/               # Book Inventory (Port 5002)
│       └── LoanService/                  # Borrowing & Returns (Port 5003)
├── frontend/                             # Vanilla JavaScript UI (Port 8080)
├── tests/                                # Unit & Integration Tests
│   ├── LibHub.UserService.Tests/
│   ├── LibHub.CatalogService.Tests/
│   ├── LibHub.LoanService.Tests/
│   └── e2e/                              # End-to-End Tests
├── scripts/                              # Testing & Deployment Scripts
├── mysql-init/                           # Database Initialization
├── docs/                                 # Complete Documentation
└── docker-compose.yml                    # Container Orchestration
```

## 🛠️ Tech Stack

| Component | Technology |
|-----------|-----------|
| **Backend** | ASP.NET Core 8.0 Web API |
| **Database** | MySQL 8.0 (3 isolated databases) |
| **ORM** | Entity Framework Core 9.0 |
| **API Gateway** | Ocelot 23.0 |
| **Service Discovery** | Consul 1.15 |
| **Logging** | Serilog + Seq |
| **Authentication** | JWT (JSON Web Tokens) |
| **Password Hashing** | BCrypt (work factor: 11) |
| **Frontend** | Vanilla JavaScript (ES6+), HTML5, CSS3 |
| **Containerization** | Docker & Docker Compose |

## 🚀 Quick Start - Clone and Run

### Prerequisites

**Required:**
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) installed and running
- Git (for cloning)
- **Minimum 4GB RAM** available
- **Available Ports**: 3307, 5000, 5001, 5002, 5003, 8080, 8500, 5341

**Note**: You don't need .NET SDK, MySQL, or any other tools installed - Docker handles everything!

### Step 1: Clone the Repository

```bash
git clone https://github.com/phuocthuan2203/libhub-ver2.git
cd libhub-ver2
```

### Step 2: Start All Services

**Linux/Mac:**
```bash
docker compose up -d --build
```

**Windows (PowerShell):**
```powershell
docker compose up -d --build
```

**Windows (Command Prompt):**
```cmd
docker compose up -d --build
```

### Step 3: Wait for Services to Start

The system needs **60-90 seconds** to:
- Build all service images
- Start MySQL and create databases
- Register services with Consul
- Seed initial data (15 books)

**Monitor startup progress:**
```bash
# Watch all logs
docker compose logs -f

# Watch specific service
docker compose logs -f gateway
docker compose logs -f catalogservice
```

### Step 4: Verify Everything is Running

**Check container status:**
```bash
docker compose ps
```

You should see 8 containers running:
- `libhub-mysql` (Database)
- `libhub-consul` (Service Discovery)
- `libhub-seq` (Log Aggregation)
- `libhub-userservice`
- `libhub-catalogservice`
- `libhub-loanservice`
- `libhub-gateway`
- `libhub-frontend`

**Run automated tests (Linux/Mac):**
```bash
./scripts/test-docker-containers.sh
./scripts/test-consul-discovery.sh
./scripts/test-gateway-integration.sh
```

**Run automated tests (Windows):**
```cmd
scripts\test-docker-containers.bat
scripts\test-consul-discovery.bat
scripts\test-gateway-integration.bat
```

### Step 5: Access the Application

Once all services are running, access:

| Service | URL | Description |
|---------|-----|-------------|
| **Frontend** | http://localhost:8080 | User interface (Register, Login, Browse Books) |
| **API Gateway** | http://localhost:5000 | Main API endpoint |
| **Consul UI** | http://localhost:8500 | Service discovery dashboard |
| **Seq Logs** | http://localhost:5341 | Centralized log viewer |
| **MySQL** | `localhost:3307` | Database (user: `libhub_user`, password: `LibHub@Dev2025`) |

### Step 6: Try It Out!

**Open the frontend:** http://localhost:8080

1. **Register a new account** (or use test account: `test@libhub.com` / `Test123!`)
2. **Browse books** - 15 technical books are pre-loaded
3. **Borrow a book** - Experience the distributed transaction (Saga pattern)
4. **View your loans** - See active borrowing records

**Check the logs in Seq:** http://localhost:5341
- Filter by `CorrelationId` to trace a single request across all services
- Search for `[SAGA-START]` to see borrow transactions
- Search for `[CONSUL-DISCOVERY]` to see service discovery in action

## 🧪 Testing the System

### Manual Testing

**Test User Registration:**
```bash
curl -X POST http://localhost:5000/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "testuser@example.com",
    "password": "Test123!"
  }'
```

**Test Login:**
```bash
curl -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "testuser@example.com",
    "password": "Test123!"
  }'
```

**Test Book Listing (No Auth Required):**
```bash
curl http://localhost:5000/api/books
```

**Test Borrowing (Requires JWT):**
```bash
# Get token first
TOKEN=$(curl -s -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@libhub.com","password":"Test123!"}' \
  | grep -o '"token":"[^"]*' | cut -d'"' -f4)

# Borrow a book
curl -X POST http://localhost:5000/api/loans \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"bookId": 1}'
```

### Automated Testing

**Run all tests:**
```bash
# Linux/Mac
./scripts/test-docker-containers.sh      # Container health checks
./scripts/test-consul-discovery.sh       # Service discovery
./scripts/test-gateway-integration.sh    # API Gateway routing

# Windows
scripts\test-docker-containers.bat
scripts\test-consul-discovery.bat
scripts\test-gateway-integration.bat
```

## 📊 Monitoring & Debugging

### View Logs

**All services:**
```bash
docker compose logs -f
```

**Specific service:**
```bash
docker compose logs -f gateway
docker compose logs -f catalogservice
docker compose logs -f loanservice
```

**Filter logs by pattern:**
```bash
docker compose logs gateway | grep "CONSUL-DISCOVERY"
docker compose logs loanservice | grep "SAGA"
```

### Centralized Logging (Seq)

Open **http://localhost:5341** and use these queries:

**Track a single request across all services:**
```sql
CorrelationId = 'req-xxx-xxx'
```

**View all service discovery events:**
```sql
@MessageTemplate like '%CONSUL-DISCOVERY%'
```

**View all saga orchestrations (borrow transactions):**
```sql
@MessageTemplate like '%SAGA%'
```

**View JWT validation events:**
```sql
@MessageTemplate like '%JWT%'
```

**View all errors:**
```sql
@Level = 'Error'
```

### Service Discovery (Consul)

Open **http://localhost:8500** to:
- View all registered services
- Check health status
- See service instances and endpoints
- Monitor service availability

## 🛑 Managing the Application

### Stop All Services
```bash
docker compose down
```

### Stop and Remove All Data
```bash
docker compose down -v
```

### Restart Services
```bash
docker compose restart
```

### Restart Specific Service
```bash
docker compose restart gateway
docker compose restart catalogservice
```

### Rebuild and Restart
```bash
docker compose up -d --build
```

### View Container Status
```bash
docker compose ps
```

### Access Container Shell
```bash
docker exec -it libhub-gateway bash
docker exec -it libhub-mysql bash
```

### View Container Resource Usage
```bash
docker stats
```

## 🏗️ System Architecture

### Microservices

**1. UserService (Port 5001)**
- User registration and authentication
- JWT token generation
- Password hashing with BCrypt
- Role-based access control (Customer/Admin)

**2. CatalogService (Port 5002)**
- Book CRUD operations
- Inventory management
- Book search and filtering
- Auto-seed with 15 technical books

**3. LoanService (Port 5003)**
- Borrow and return operations
- **Saga orchestration** for distributed transactions
- Business rules enforcement (14-day period, 5 loan limit)
- Inter-service communication with CatalogService

**4. API Gateway (Port 5000)**
- Single entry point for all clients
- JWT validation and authentication
- Route-based authorization
- Service discovery via Consul
- Request/response logging with correlation tracking

### Infrastructure

**Consul (Port 8500)**
- Service registration and discovery
- Health check monitoring
- Dynamic service location resolution

**Seq (Port 5341)**
- Centralized log aggregation
- Real-time log streaming
- Structured query and filtering
- Request correlation tracking

**MySQL (Port 3307)**
- Three isolated databases (Database per Service pattern)
  - `user_db` - User data
  - `catalog_db` - Book inventory
  - `loan_db` - Borrowing records
- No cross-database foreign keys

### Key Design Patterns

- **Microservices Architecture**: Independent, deployable services
- **Database per Service**: Each service owns its data
- **Saga Pattern**: Distributed transaction with compensating actions
- **API Gateway**: Single entry point with routing
- **Service Discovery**: Dynamic service location via Consul
- **Correlation ID**: Request tracing across services
- **Structured Logging**: Semantic logging with Serilog + Seq

## 📚 Features

### For Users (Customers)
✅ Register and create account  
✅ Login with JWT authentication  
✅ Browse book catalog (public access)  
✅ Search books by title, author, ISBN, genre  
✅ Borrow available books  
✅ View borrowing history  
✅ Automatic loan period (14 days)  
✅ Maximum 5 active loans per user  

### For Administrators
✅ Add new books to catalog  
✅ Update book information  
✅ Manage inventory (total/available copies)  
✅ View all loans system-wide  
✅ Identify overdue loans  
✅ Complete audit logging  

### Technical Features
✅ Distributed transactions with Saga pattern  
✅ Service discovery and health monitoring  
✅ Request correlation tracking  
✅ Centralized log aggregation  
✅ JWT-based authentication  
✅ Role-based authorization  
✅ Password hashing with BCrypt  
✅ Auto-seeded test data  
✅ Docker containerization  
✅ Automated testing scripts  

## 🧰 Troubleshooting

## 🧰 Troubleshooting

### Services Won't Start

**Problem**: Containers fail to start or exit immediately

**Solution**:
```bash
# Check logs for errors
docker compose logs

# Rebuild from scratch
docker compose down -v
docker compose up -d --build
```

### Port Already in Use

**Problem**: Error "port is already allocated"

**Solution**:
```bash
# Check what's using the port (Linux/Mac)
sudo lsof -i :5000
sudo lsof -i :3307

# Check what's using the port (Windows PowerShell)
netstat -ano | findstr :5000
netstat -ano | findstr :3307

# Kill the process or change port in docker-compose.yml
```

### Services Not Registering with Consul

**Problem**: Services show as unhealthy in Consul UI

**Solution**:
```bash
# Wait longer (services need 30-60s to register)
sleep 30

# Check service logs
docker compose logs userservice
docker compose logs catalogservice

# Restart specific service
docker compose restart userservice
```

### Database Connection Errors

**Problem**: Services can't connect to MySQL

**Solution**:
```bash
# Check MySQL is healthy
docker compose ps mysql

# Restart MySQL
docker compose restart mysql

# Check database was created
docker exec -it libhub-mysql mysql -ulibhub_user -pLibHub@Dev2025 -e "SHOW DATABASES;"
```

### Frontend Can't Reach API

**Problem**: Frontend shows connection errors

**Solution**:
```bash
# Check Gateway is running
docker compose ps gateway

# Verify Gateway routing
curl http://localhost:5000/api/books

# Check frontend configuration
docker compose logs frontend
```

### Logs Not Appearing in Seq

**Problem**: No logs in Seq UI

**Solution**:
```bash
# Check Seq is running
docker compose ps seq

# Restart Seq
docker compose restart seq

# Verify services can reach Seq
docker compose logs gateway | grep "Seq"
```

## 📖 Documentation

### Quick Reference
- **Main Documentation**: [`ai-docs/master-context/00_PROJECT_CONTEXT.md`](ai-docs/master-context/00_PROJECT_CONTEXT.md)
- **Docker Guide**: [`docs/deployment/DOCKER_QUICK_START.md`](docs/deployment/DOCKER_QUICK_START.md)
- **Service Discovery**: [`docs/deployment/CONSUL_SERVICE_DISCOVERY.md`](docs/deployment/CONSUL_SERVICE_DISCOVERY.md)
- **Testing Guide**: [`docs/deployment/TESTING_CONSUL_AND_SAGA.md`](docs/deployment/TESTING_CONSUL_AND_SAGA.md)

### For Developers
- **Architecture Details**: [`ai-docs/master-context/`](ai-docs/master-context/)
- **Git Workflow**: [`docs/development/GIT_COMMANDS.md`](docs/development/GIT_COMMANDS.md)
- **Project Status**: [`ai-docs/PROJECT_STATUS.md`](ai-docs/PROJECT_STATUS.md)

### For Windows Users
- **Quick Start**: [`docs/windows/SHARE_WITH_FRIENDS.md`](docs/windows/SHARE_WITH_FRIENDS.md)
- **Complete Guide**: [`docs/windows/FOR_WINDOWS_USERS.md`](docs/windows/FOR_WINDOWS_USERS.md)
- **Setup Reference**: [`docs/windows/WINDOWS_SETUP.md`](docs/windows/WINDOWS_SETUP.md)

## 🤝 Contributing

This is an academic project demonstrating microservices architecture and distributed systems design.

**To contribute**:
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'feat: add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📝 License

Academic project - All rights reserved

## 🙏 Acknowledgments

Built as part of Service-Oriented Architecture coursework, demonstrating:
- Microservices design patterns
- Distributed transaction management (Saga)
- Service discovery and orchestration
- Event-driven architecture principles
- Container orchestration with Docker

## 📧 Contact

**Author**: Phuoc Thuan  
**Repository**: https://github.com/phuocthuan2203/libhub-ver2  
**Branch**: `feat/logging-feature`

---

⭐ **If you found this project helpful, please give it a star!** ⭐
