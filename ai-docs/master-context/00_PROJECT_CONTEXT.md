# LibHub - Project Context

## What is LibHub?

LibHub is a **web-based library management system** built using microservices architecture to digitize and streamline the operations of a community library. This is an academic project demonstrating distributed systems design, Service-Oriented Architecture (SOA) principles, and Clean Architecture implementation.

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
| **Database** | MySQL 8.0 (using `caching_sha2_password` authentication) |
| **ORM** | Entity Framework Core 8.0 |
| **API Gateway** | Ocelot |
| **Authentication** | JWT (JSON Web Tokens) |
| **Password Hashing** | BCrypt (work factor: 11) |
| **Frontend** | Vanilla JavaScript (ES6+), HTML5, CSS3 |
| **Development Platform** | Ubuntu 25.10, .NET 8 SDK, VSCode with C# Dev Kit |

## Architecture Patterns

### Microservices Architecture
The system is decomposed into **4 independent microservices** + 1 API Gateway:

1. **UserService** (Port 5002)
   - Domain: Identity & Access Management
   - Database: `user_db`
   - Responsibilities: User registration, authentication, JWT token generation

2. **CatalogService** (Port 5001)
   - Domain: Book Inventory Management
   - Database: `catalog_db`
   - Responsibilities: Book CRUD operations, search, inventory tracking

3. **LoanService** (Port 5003)
   - Domain: Borrowing & Returns Management
   - Database: `loan_db`
   - Responsibilities: Loan creation, returns, saga orchestration
   - **Special**: Implements distributed transaction using Saga pattern

4. **API Gateway** (Port 5000)
   - Technology: Ocelot
   - Responsibilities: Request routing, JWT validation, single entry point for frontend

### Key Architectural Principles

**Database per Service Pattern**: Each microservice owns its private database. No foreign key constraints exist between services. Data relationships managed through API calls, not database joins.

**Clean Architecture** (per microservice): Four-layer structure with strict dependency rules (dependencies point inward only):
- **Domain Layer**: Entities, business rules, repository interfaces (zero external dependencies)
- **Application Layer**: Use case orchestration, DTOs, application services
- **Infrastructure Layer**: EF Core implementations, HTTP clients, external integrations
- **Presentation Layer**: API Controllers, middleware, dependency injection

**Saga Pattern**: Used for distributed transactions (e.g., "Borrow Book" workflow coordinating LoanService and CatalogService). LoanService acts as orchestrator with compensating transactions on failure.

**Synchronous Communication**: Services communicate via HTTP/REST calls. Configuration-based service discovery (development) with hardcoded URLs in `appsettings.json`.

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
- Critical system errors and exceptions

Log format: Timestamp, user ID, event type, relevant details

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
- Clean Architecture properly implemented in all services
- Database per Service pattern enforced (no cross-database FK constraints)
- API Gateway routing all requests correctly
- JWT authentication working end-to-end

### Functional Success
- Users can register, login, browse books, and borrow/return books
- Admins can add books and view loan records
- All business rules enforced (loan period, borrowing limits, stock validation)
- All non-functional requirements met (performance, security, logging)

### Quality Success
- Unit test coverage >70%
- Integration tests passing for all services
- No critical bugs in core workflows
- Clean, maintainable code following SOLID principles

## Project Scope

### In Scope
- 4 microservices + API Gateway backend
- Simple HTML/CSS/JavaScript frontend
- JWT-based authentication and authorization
- MySQL databases with EF Core
- Basic CRUD operations and search
- Distributed transaction (Saga) for borrowing
- Audit logging
- Local development deployment

### Out of Scope
- Production deployment infrastructure
- Advanced notification system (email/SMS)
- Fine/penalty management for overdue books
- Book recommendations or analytics
- Mobile app development
- Payment processing
- Advanced search filters (faceted search)
- Book reservations/holds
- Containerization (Docker) - optional bonus
- CI/CD pipelines
- Service discovery with Consul - using config-based instead

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
- **Project Location**: `/home/thuannp4/development/LibHub/`
- **MySQL**: localhost:3306
- **Databases**: user_db, catalog_db, loan_db
- **MySQL User**: `libhub_user` / `LibHub@Dev2025`
- **Authentication Method**: `caching_sha2_password` (MySQL 8.4+)
- **IDE**: Visual Studio Code with C# Dev Kit
- **Version Control**: Git

## Project Timeline

Estimated total development time: **6-8 weeks** across 6 phases:
- Phase 1: Database Implementation (2-3 days)
- Phase 2: UserService (5-7 days)
- Phase 3: CatalogService (5-7 days)
- Phase 4: LoanService (7-10 days)
- Phase 5: API Gateway (3-4 days)
- Phase 6: Frontend (7-10 days)

## Important Notes for Implementation

1. **Always use Clean Architecture**: Every service follows the 4-layer structure
2. **Database isolation is mandatory**: Never create FK constraints between service databases
3. **JWT must be validated at Gateway**: Services trust the Gateway's validation
4. **Saga pattern is critical for UC-09**: Borrow Book requires careful orchestration with compensating transactions
5. **All admin operations must be logged**: Audit trail is a mandatory requirement
6. **Password hashing is non-negotiable**: Never store plaintext passwords
7. **Service ports are fixed**: Don't change port assignments (5000-5003)
8. **Connection strings use libhub_user**: Don't use root user in application code
