# LibHub - Project Context

## What is LibHub?

LibHub is a **web-based library management system** built using microservices architecture to digitize and streamline the operations of a community library. This is an academic project demonstrating distributed systems design, Service-Oriented Architecture (SOA) principles, and pragmatic software architecture implementation.

## Problem Statement

The community library currently operates entirely manually, leading to three critical problems:

1. **Inefficient Inventory Management**: Staff lack real-time visibility into book availability. Manual tracking of books, copies, and overdue items is time-consuming and error-prone.

2. **Limited Patron Access**: Patrons must physically visit the library to search the catalog, check book availability, or view their borrowing history, creating barriers to access and reducing engagement.

3. **Poor User Experience**: No system exists for patrons to discover books online, manage accounts, or streamline checkout/return processes, leading to queues and delays.

## Solution Overview

LibHub provides a digital platform with two primary user interfaces:

### For Patrons (Customers)
- Online book catalog search and browsing
- Self-service borrowing and returns
- Personal borrowing history tracking
- Account management

### For Library Staff (Admins)
- Centralized book catalog management (CRUD operations)
- Real-time inventory visibility and stock management
- Loan tracking and overdue identification
- User account oversight

## Technology Stack

| Component | Technology |
|-----------|-----------|
| **Backend Framework** | ASP.NET Core 8.0 Web API |
| **Database** | MySQL 8.0 (using `mysql_native_password` authentication) |
| **ORM** | Entity Framework Core 9.0 |
| **API Gateway** | Ocelot |
| **Service Discovery** | Consul 1.15 |
| **Logging** | Serilog with Seq for centralized log aggregation |
| **Authentication** | JWT (JSON Web Tokens) |
| **Password Hashing** | BCrypt (work factor: 11) |
| **Frontend** | Vanilla JavaScript (ES6+), HTML5, CSS3 |
| **Containerization** | Docker & Docker Compose |
| **Development Platform** | Ubuntu 25.10, .NET 8 SDK, VSCode with C# Dev Kit |

## Architecture Patterns

### Microservices Architecture
The system is decomposed into **4 independent microservices** + 1 API Gateway + supporting infrastructure:

1. **UserService** (Port 5002)
   - Domain: Identity & Access Management
   - Database: `user_db`
   - Responsibilities: User registration, authentication, JWT token generation

2. **CatalogService** (Port 5001)
   - Domain: Book Inventory Management
   - Database: `catalog_db`
   - Responsibilities: Book CRUD operations, search, inventory tracking
   - Features: Auto-seed with 15 technical books on first startup

3. **LoanService** (Port 5003)
   - Domain: Borrowing & Returns Management
   - Database: `loan_db`
   - Responsibilities: Loan creation, returns, saga orchestration
   - **Special**: Implements distributed transaction using Saga pattern

4. **API Gateway** (Port 5000)
   - Technology: Ocelot
   - Responsibilities: Request routing, JWT validation, single entry point for frontend
   - Features: Consul integration for dynamic service discovery

**Supporting Infrastructure**:

5. **Consul** (Port 8500)
   - Service registry and discovery
   - Health check monitoring
   - Dynamic service location resolution

6. **Seq** (Port 5341)
   - Centralized log aggregation
   - Real-time log search and filtering
   - Request correlation tracking

7. **MySQL** (Port 3307 mapped from 3306)
   - Three isolated databases (user_db, catalog_db, loan_db)
   - Health checks for container orchestration

8. **Frontend** (Port 8080)
   - Nginx-served static HTML/CSS/JavaScript
   - Communicates exclusively through API Gateway

### Key Architectural Principles

**Database per Service Pattern**: Each microservice owns its private database. No foreign key constraints exist between services. Data relationships managed through API calls, not database joins.

**Simplified Folder Structure** (per microservice): Pragmatic organization for maintainability:
- **Models/**: Domain entities, request/response DTOs
  - `Entities/`: Domain entities (User, Book, Loan)
  - `Requests/`: Input DTOs for API endpoints
  - `Responses/`: Output DTOs for API responses
- **Data/**: Database context, repositories, seeders, migrations
- **Services/**: Business logic and application services
- **Security/**: Authentication and authorization utilities (JWT, BCrypt, password validation)
- **Clients/**: HTTP clients for inter-service communication (LoanService only)
- **Controllers/**: API endpoints and HTTP request handling
- **Middleware/**: Cross-cutting concerns (CorrelationId, health check logging)
- **Extensions/**: Service registration and configuration extensions (Consul)
- **Program.cs**: Application startup, DI configuration, middleware pipeline

**Example Service Structure (UserService)**:
```
UserService/
├── Controllers/
│   └── UsersController.cs
├── Data/
│   ├── UserDbContext.cs
│   ├── UserRepository.cs
│   └── DesignTimeDbContextFactory.cs
├── Models/
│   ├── Entities/
│   │   └── User.cs
│   ├── Requests/
│   │   ├── LoginRequest.cs
│   │   └── RegisterRequest.cs
│   └── Responses/
│       ├── TokenResponse.cs
│       └── UserResponse.cs
├── Security/
│   ├── JwtTokenGenerator.cs
│   └── PasswordHasher.cs
├── Services/
│   ├── UserService.cs
│   └── PasswordValidator.cs
├── Middleware/
│   ├── CorrelationIdMiddleware.cs
│   └── HealthCheckLoggingMiddleware.cs
├── Extensions/
│   └── ConsulServiceRegistration.cs
├── Program.cs
├── appsettings.json
└── Dockerfile
```

**Saga Pattern**: Used for distributed transactions (e.g., "Borrow Book" workflow coordinating LoanService and CatalogService). LoanService acts as orchestrator with compensating transactions on failure.

**Synchronous Communication**: Services communicate via HTTP/REST calls using dynamic service discovery through Consul. Services register themselves on startup and discover each other via Consul's service registry.

**Containerization**: Complete Docker Compose orchestration with:
- Multi-stage Dockerfiles for optimized image sizes
- Health checks for dependency management
- Bridge networking for service isolation
- Volume persistence for MySQL and Seq data
- Environment-based configuration
- Automatic service startup ordering

## Selected Use Cases for Implementation

This project implements **6 core use cases** (out of the full SRS):

### Member/Customer Use Cases

**UC-01: Create Account (User Registration)**
- Allow new users to register with username, email, password
- Auto-login after successful registration
- Password complexity validation and BCrypt hashing

**UC-02: Log In (User Authentication)**
- Authenticate existing users with email/password
- Issue JWT token valid for 1 hour
- Role-based claims (Customer or Admin)

**UC-05: View Books (View Book Catalog)**
- Public access (no authentication required)
- Search by title, author, ISBN, or genre
- View book details and availability status

**UC-09: Borrow Book**
- Authenticated customers can borrow available books
- 14-day loan period (automatically calculated)
- Maximum 5 active loans per customer
- **Implements Saga Pattern** (distributed transaction across LoanService and CatalogService)

### Administrative Use Cases

**UC-12: Add Book (Add New Book to Catalog)**
- Admin-only functionality
- Provide ISBN, title, author, genre, description, total copies
- All actions logged for audit trail

**UC-16: View All Loans (View Loan Records)**
- Admin-only functionality
- View all active loans system-wide
- Identify overdue loans (due date < current date)
- Filter by customer or view all

## Non-Functional Requirements

### Security (Mandatory)
- **Authentication**: All users must authenticate before accessing protected functions
- **Authorization**: Role-Based Access Control (Customer vs Admin)
- **Password Security**: BCrypt hashing with salt (work factor 11)
- **Token Security**: JWT with 1-hour expiration

### Logging & Auditing (Mandatory)
Events to log:
- User authentication events (successful and failed login attempts)
- All book management actions by admins (create, update, delete)
- All loan transactions (borrow, return)
- Service discovery and inter-service communication
- Critical system errors and exceptions

**Logging Implementation**:
- **Framework**: Serilog with structured logging
- **Centralized Aggregation**: Seq (accessible at http://localhost:5341)
- **Correlation**: CorrelationId header for request tracing across services
- **Enrichers**: ServiceName, Environment, Thread context
- **Format**: JSON-structured logs with timestamp, user ID, event type, correlation ID, relevant details

### Performance
- **Search Performance**: Book search results must display in **under 2 seconds**
- **Concurrency**: System must support **up to 200 concurrent users** without degradation

### Usability
- **Customer Interface**: Intuitive design allowing new users to find and borrow books with minimal guidance
- **Admin Interface**: Clear and efficient workflows for managing library data

## System Data Flow

### Example: Borrow Book Workflow
Customer clicks "Borrow" button (Frontend)

POST /api/loans sent to API Gateway (with JWT)

Gateway validates JWT, routes to LoanService

LoanService (Saga Orchestrator):
a. Creates PENDING Loan record in loan_db
b. Calls CatalogService: GET /api/books/{id} (check availability)
c. Calls CatalogService: PUT /api/books/{id}/stock (decrement stock)
d. If success: Update Loan to "CheckedOut" status
e. If failure: Update Loan to "FAILED", rollback (compensating transaction)

Return response to Frontend with success/failure message

text

## Key Success Criteria

### Technical Success
- All 6 use cases implemented and functional
- Saga pattern demonstrably working for distributed transactions
- Simplified folder structure properly organized in all services
- Database per Service pattern enforced (no cross-database FK constraints)
- API Gateway routing all requests correctly
- JWT authentication working end-to-end
- Consul service discovery operational
- Centralized logging with Serilog and Seq

### Functional Success
- Users can register, login, browse books, and borrow/return books
- Admins can add books and view loan records
- All business rules enforced (loan period, borrowing limits, stock validation)
- All non-functional requirements met (performance, security, logging)

### Quality Success
- Unit test coverage >70% across all services
- Integration tests passing for all services
- E2E test scripts for complete workflows
- No critical bugs in core workflows
- Clean, maintainable code following SOLID principles

**Test Implementation**:
- **UserService.Tests**: 26+ unit and infrastructure tests (xUnit, Moq, FluentAssertions)
- **CatalogService.Tests**: Domain, application, and infrastructure layer tests
- **LoanService.Tests**: Saga pattern and distributed transaction tests
- **E2E Tests**: Shell scripts for complete user journeys
- **Container Tests**: Automated Docker health check validation

## Project Scope

### In Scope ✅
- 4 microservices + API Gateway backend
- Simple HTML/CSS/JavaScript frontend
- JWT-based authentication and authorization
- MySQL databases with EF Core
- Basic CRUD operations and search
- Distributed transaction (Saga) for borrowing
- Structured audit logging with Serilog and Seq
- Docker containerization with Docker Compose
- Consul service discovery and health checks
- Seed data for development/testing
- E2E testing scripts
- Local and containerized deployment

### Out of Scope
- Production deployment infrastructure (Kubernetes)
- Advanced notification system (email/SMS)
- Fine/penalty management for overdue books
- Book recommendations or analytics
- Mobile app development
- Payment processing
- Advanced search filters (faceted search)
- Book reservations/holds
- CI/CD pipelines

## Domain Model Summary

### Three Bounded Contexts

1. **Identity & Access Context** (UserService)
   - Aggregate Root: **User**
   - Key Properties: UserId, Username, Email, HashedPassword, Role
   - Business Rules: Email uniqueness, password complexity, role validation

2. **Catalog Context** (CatalogService)
   - Aggregate Root: **Book**
   - Key Properties: BookId, ISBN, Title, Author, TotalCopies, AvailableCopies
   - Business Rules: ISBN uniqueness, AvailableCopies ≤ TotalCopies, stock cannot go negative

3. **Loan Context** (LoanService)
   - Aggregate Root: **Loan**
   - Key Properties: LoanId, UserId (reference), BookId (reference), Status, CheckoutDate, DueDate
   - Business Rules: 14-day loan period, max 5 active loans, status state machine
   - Status Flow: PENDING → CheckedOut → Returned (or PENDING → FAILED)

### Cross-Context Communication
- Services reference each other by ID only (no entity navigation properties)
- No foreign key constraints between databases
- Data consistency: ACID within service, eventual consistency across services (via Saga)

## Development Environment

- **Platform**: Ubuntu 25.10
- **Project Location**: `/home/thuannp4/development/libhub-ver2/`
- **MySQL**: 
  - Host: localhost (native) / mysql (Docker)
  - Port: 3306 (native) / 3307 (Docker mapped)
  - Databases: user_db, catalog_db, loan_db
  - User: `libhub_user` / `LibHub@Dev2025`
  - Authentication: `mysql_native_password`
- **Service Discovery**: Consul at http://localhost:8500
- **Log Aggregation**: Seq at http://localhost:5341
- **API Gateway**: http://localhost:5000
- **Frontend**: http://localhost:8080 (containerized)
- **IDE**: Visual Studio Code with C# Dev Kit
- **Version Control**: Git (branch: feat/logging-feature)

## Project Status

**Current Status**: ✅ **COMPLETE** (100% - 32/32 tasks)  
**Last Updated**: October 27, 2025  
**Current Branch**: feat/logging-feature

### Completed Phases

- ✅ **Phase 0**: Pre-Development Setup
- ✅ **Phase 1**: Database Implementation (3/3 tasks)
- ✅ **Phase 2**: UserService (5/5 tasks)
- ✅ **Phase 3**: CatalogService (5/5 tasks)
- ✅ **Phase 4**: LoanService (6/6 tasks)
- ✅ **Phase 5**: API Gateway (4/4 tasks)
- ✅ **Phase 6**: Frontend (4/4 tasks)
- ✅ **Phase 7**: E2E Testing (1/1 tasks)
- ✅ **Phase 8**: Docker Containerization (3/3 tasks)
- ✅ **Phase 9**: Service Discovery & Enhancements (2/2 tasks)

### Additional Enhancements Implemented

- ✅ **Serilog Integration**: Structured logging across all services
- ✅ **Seq Integration**: Centralized log aggregation and viewing
- ✅ **Correlation ID**: Request tracing across services
- ✅ **Consul Service Discovery**: Dynamic service registration and discovery
- ✅ **Book Seed Data**: 15 technical books auto-seeded on startup
- ✅ **Health Checks**: All services expose `/health` endpoints
- ✅ **Docker Networking**: Proper service isolation and communication

## Important Notes for Implementation

1. **Simplified folder structure**: Each service uses a pragmatic folder-based organization (Models, Data, Services, Controllers, etc.) instead of strict Clean Architecture layers
2. **Database isolation is mandatory**: Never create FK constraints between service databases
3. **JWT must be validated at Gateway**: Services trust the Gateway's validation
4. **Saga pattern is critical for UC-09**: Borrow Book requires careful orchestration with compensating transactions
5. **All admin operations must be logged**: Audit trail is a mandatory requirement
6. **Password hashing is non-negotiable**: Never store plaintext passwords
7. **Service ports are fixed**: Don't change port assignments (5000-5003)
8. **Connection strings use libhub_user**: Don't use root user in application code
9. **Use Consul for service discovery**: Services dynamically discover each other, no hardcoded URLs
10. **Correlation IDs for tracing**: Always propagate CorrelationId header across service calls
11. **Structured logging with Serilog**: Use semantic logging with proper context enrichment
12. **Health checks required**: All services expose `/health` endpoint for Consul

## Quick Start Guide

### Running the Application

**Using Docker Compose (Recommended)**:
```bash
docker compose up -d --build
```

**Access Points**:
- Frontend: http://localhost:8080
- API Gateway: http://localhost:5000
- Consul UI: http://localhost:8500
- Seq Logs: http://localhost:5341

**Test Scripts**:
```bash
./scripts/test-docker-containers.sh
./scripts/test-consul-discovery.sh
./scripts/test-gateway-integration.sh
```

**View Logs**:
```bash
docker compose logs -f [service-name]
```

**Default Credentials**:
- Admin: admin@libhub.com / Admin123!
- Test User: test@libhub.com / Test123!
