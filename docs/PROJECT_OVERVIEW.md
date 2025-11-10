# PROJECT_OVERVIEW.md

NOTE: This document is formatted for Google Docs. Lines marked with "H1 -", "H2 -", etc. should be highlighted and formatted as headers in Google Docs.

---

H1 - LibHub - Library Management System

H2 - Table of Contents

1. Project Architecture
   1.1 Architecture Components
       - API Gateway (Port 5000)
       - User Service (Port 5002)
       - Catalog Service (Port 5001)
       - Loan Service (Port 5003)
       - Consul (Port 8500)
       - Seq (Port 5341)
       - MySQL Database (Port 3307)
       - Frontend (Port 8080)
   1.2 Database-Per-Service Pattern
   1.3 Communication Flow

2. Saga Pattern: Orchestration
   2.1 What is the Saga Pattern?
   2.2 Implementation in BorrowBook Operation
   2.3 Compensating Transactions
   2.4 Example Saga Execution
   2.5 Saga Logging and Traceability
   2.6 Saga Pattern Benefits

3. Communication Style: Synchronous
   3.1 Synchronous Communication Overview
   3.2 Communication Patterns
       - Gateway to Services
       - Service to Service (Loan to Catalog)
   3.3 Benefits of Synchronous Communication
   3.4 Handling Synchronous Communication Challenges
       - Timeouts
       - Retry and Circuit Breaker
       - Service Discovery
       - Token Propagation
       - Correlation ID Propagation
   3.5 Trade-offs

4. Logging: Structured Logging with Serilog and Seq
   4.1 Structured Logging Overview
   4.2 Serilog Configuration
   4.3 Seq Centralized Logging
   4.4 Correlation ID Tracking
   4.5 Logging Patterns in LibHub
       - Request Lifecycle Logging
       - Saga Execution Logging
       - Inter-Service Communication Logging
       - Error Logging
   4.6 Benefits of Structured Logging with Serilog and Seq
   4.7 Logging Best Practices Demonstrated

---

H2 - Project Overview

LibHub is a modern microservices-based library management system built with ASP.NET Core 8.0. The system demonstrates enterprise-level architecture patterns including microservices decomposition, distributed transactions using Saga pattern, service discovery, API gateway, and centralized logging. The application allows users to browse books, borrow and return books, and manage their library accounts through a clean web interface.

---

H2 - 1. Project Architecture

The LibHub system follows a microservices architecture pattern with clear separation of concerns. The architecture consists of several independent services that communicate through well-defined APIs, managed by a central gateway, and supported by infrastructure components for service discovery and logging.

H3 - 1.1 Architecture Components

H4 - API Gateway (Port 5000)

The API Gateway serves as the single entry point for all client requests. Built using Ocelot, it provides:

ROLE: Routes incoming HTTP requests to appropriate microservices based on URL patterns
- Request routing and aggregation
- JWT token validation and authentication
- Load balancing using Round Robin algorithm
- Circuit breaker pattern for fault tolerance (breaks after 3 failures for 5 seconds)
- Service discovery integration with Consul
- Centralized CORS policy management

The gateway eliminates the need for clients to know about individual service locations and provides a unified API interface. All client requests go through http://localhost:5000 and are automatically routed to the correct service.

H4 - User Service (Port 5002)

The User Service handles all authentication and user management operations.

ROLE: Manages user identity, authentication, and authorization
- User registration with email validation and password hashing (BCrypt with work factor 11)
- User login with JWT token generation (24-hour expiration)
- Password security using BCrypt hashing algorithm
- Role-based access control (Member, Librarian roles)
- User profile management
- Independent MySQL database (user_db)

This service is the foundation of the security layer, issuing JWT tokens that other services validate for authenticated requests.

H4 - Catalog Service (Port 5001)

The Catalog Service manages the library's book inventory.

ROLE: Maintains book information and availability status
- Book CRUD operations (Create, Read, Update, Delete)
- Book search and filtering capabilities
- Stock management (available copies tracking)
- Book availability checking
- ISBN validation and book metadata management
- Independent MySQL database (catalog_db)
- Seeded with 15 sample books on startup

This service acts as the single source of truth for book information and stock levels across the system.

H4 - Loan Service (Port 5003)

The Loan Service orchestrates the book borrowing and return process.

ROLE: Coordinates distributed transactions between users and book inventory
- Book borrowing with Saga pattern orchestration
- Book return processing with automatic stock restoration
- Loan history tracking
- Due date management (14-day loan period)
- Active loan limit enforcement (maximum 5 active loans per user)
- Inter-service communication with Catalog Service
- Independent MySQL database (loan_db)

This service implements the core business logic and demonstrates distributed transaction management across multiple databases.

H4 - Consul (Port 8500)

Consul provides service discovery and health monitoring capabilities.

ROLE: Service registry and health check coordinator
- Dynamic service registration on startup
- Periodic health checks every 10 seconds
- Service instance tracking
- Automatic failover to healthy instances
- Web UI for monitoring at http://localhost:8500
- DNS-based service discovery

Consul eliminates hardcoded service addresses, enabling dynamic scaling and automatic service discovery.

H4 - Seq (Port 5341)

Seq is a centralized log aggregation and analysis platform.

ROLE: Collects, stores, and visualizes structured logs from all services
- Real-time log ingestion from all microservices
- Structured log querying and filtering
- Request correlation tracking across services
- Performance monitoring and debugging
- Web UI for log exploration at http://localhost:5341
- 7-day log retention by default

Seq provides a unified view of system behavior, making debugging distributed transactions much easier.

H4 - MySQL Database (Port 3307)

MySQL hosts three isolated databases, one for each business service.

ROLE: Provides data persistence with database-per-service pattern
- user_db: User accounts, credentials, and profiles
- catalog_db: Book inventory and availability data
- loan_db: Loan records and transaction history
- Each service has exclusive access to its own database
- Data isolation ensures loose coupling between services
- Automatic schema creation via Entity Framework migrations

This database-per-service pattern ensures that services remain independently deployable and scalable.

H4 - Frontend (Port 8080)

The frontend is a vanilla JavaScript single-page application.

ROLE: Provides user interface for library operations
- User registration and login
- Book browsing with search and pagination
- Book detail view
- Book borrowing interface
- Loan history management
- JWT token management in localStorage
- Responsive design with custom CSS
- Nginx web server for static file serving

The frontend communicates exclusively through the API Gateway, abstracting away the microservices architecture from end users.

H3 - 1.2 Database-Per-Service Pattern

Each microservice maintains its own dedicated MySQL database:

- User Service manages user_db (users table)
- Catalog Service manages catalog_db (books table)  
- Loan Service manages loan_db (loans table)

This pattern provides:
- Service autonomy: Each service can evolve its schema independently
- Fault isolation: Database issues in one service don't affect others
- Technology flexibility: Services can potentially use different database technologies
- Clear ownership: Each service owns its data exclusively

The trade-off is increased complexity in maintaining data consistency across services, which is addressed through the Saga pattern.

H3 - 1.3 Communication Flow

A typical book borrowing request flows through the system as follows:

1. User clicks "Borrow" in the frontend (Port 8080)
2. Frontend sends POST request to Gateway (Port 5000)
3. Gateway validates JWT token
4. Gateway queries Consul for Loan Service location
5. Gateway routes request to Loan Service (Port 5003)
6. Loan Service executes Saga orchestration:
   - Creates PENDING loan in loan_db
   - Queries Consul for Catalog Service location
   - Calls Catalog Service to check availability
   - Calls Catalog Service to decrement stock
   - Updates loan status to CHECKED_OUT
7. Each service logs to Seq with correlation ID
8. Response flows back through Gateway to frontend
9. All logs are queryable in Seq by correlation ID

This architecture ensures loose coupling while maintaining data consistency through orchestrated workflows.

---

H2 - 2. Saga Pattern: Orchestration

LibHub implements the Saga orchestration pattern to handle distributed transactions across multiple databases. Since traditional ACID transactions cannot span multiple microservices, the Saga pattern provides a way to maintain data consistency through a sequence of local transactions with compensating actions.

H3 - 2.1 What is the Saga Pattern?

The Saga pattern is a design pattern for managing distributed transactions in microservices architectures. Instead of a single ACID transaction, a saga is a sequence of local transactions where each service updates its own database and publishes an event or message to trigger the next step.

ORCHESTRATION vs CHOREOGRAPHY:
- Orchestration: A central coordinator (orchestrator) tells each service what to do
- Choreography: Each service listens to events and decides what to do next

LibHub uses orchestration where the Loan Service acts as the saga orchestrator.

H3 - 2.2 Implementation in BorrowBook Operation

The book borrowing operation requires updates to two databases:
- loan_db: Create a loan record
- catalog_db: Decrement book stock

The LoanService orchestrates this 5-step saga workflow:

STEP 1 - Validate Business Rules:
Check if user has reached maximum loan limit (5 active loans). This is a local operation in loan_db. If limit exceeded, reject immediately without starting the saga.

STEP 2 - Create PENDING Loan:
Create a loan record with status "PENDING" in loan_db. This is the first local transaction. The loan is not yet active but reserves the intent to borrow.

STEP 3 - Verify Book Availability:
Make synchronous HTTP call to Catalog Service to check if book is available. This is a read-only operation. If book is not available, trigger compensation.

STEP 4 - Decrement Stock:
Make synchronous HTTP call to Catalog Service to decrement the book's available copies. This is the critical distributed operation that modifies catalog_db. If this fails, trigger compensation.

STEP 5 - Complete Saga:
Update loan status from "PENDING" to "CHECKED_OUT" in loan_db. This finalizes the successful saga execution.

H3 - 2.3 Compensating Transactions

If any step after STEP 2 fails, the saga executes compensating transactions to maintain consistency:

COMPENSATION ACTION:
If STEP 3 or STEP 4 fails, mark the loan as "FAILED" in loan_db. This prevents the loan from being considered active while no stock was actually decremented.

The beauty of this approach is that we never leave the system in an inconsistent state:
- If stock decrement fails, we mark the loan as failed
- If the entire operation fails before stock decrement, no harm is done
- The loan record provides an audit trail of all attempts

H3 - 2.4 Example Saga Execution

SUCCESSFUL SAGA:
1. User 123 requests to borrow Book 456
2. LoanService checks: User has 3 active loans (OK)
3. LoanService creates Loan #789 with status "PENDING"
4. LoanService calls CatalogService: Book 456 has 5 copies available (OK)
5. LoanService calls CatalogService: Decrement Book 456 stock (now 4 copies)
6. LoanService updates Loan #789 status to "CHECKED_OUT"
7. Success response returned to user

FAILED SAGA WITH COMPENSATION:
1. User 123 requests to borrow Book 456
2. LoanService checks: User has 3 active loans (OK)
3. LoanService creates Loan #789 with status "PENDING"
4. LoanService calls CatalogService: Book 456 has 0 copies available (FAILED)
5. COMPENSATION: LoanService marks Loan #789 as "FAILED"
6. Error response returned to user: "Book is not available"

The compensation ensures loan_db accurately reflects that no book was borrowed, even though a loan record exists for audit purposes.

H3 - 2.5 Saga Logging and Traceability

Every saga step is logged with structured logging for full traceability:

- [SAGA-START] Initial saga trigger
- [SAGA-STEP-1] Loan record created
- [SAGA-STEP-2] Checking book availability
- [SAGA-STEP-2-SUCCESS] or [SAGA-STEP-2-FAILED]
- [SAGA-STEP-3] Decrementing book stock
- [SAGA-STEP-3-SUCCESS] or failure
- [SAGA-COMPENSATION] if saga failed
- [SAGA-SUCCESS] final success

All logs include correlation IDs, allowing developers to trace a single user request across all services and database operations in Seq.

H3 - 2.6 Saga Pattern Benefits

CONSISTENCY WITHOUT DISTRIBUTED TRANSACTIONS:
Maintains data consistency across loan_db and catalog_db without requiring distributed ACID transactions (which don't work across HTTP boundaries).

CLEAR FAILURE SEMANTICS:
Every failure mode has a defined compensation strategy. Failed loans are marked explicitly rather than left in an unknown state.

AUDITABILITY:
Every saga execution is fully logged, providing complete audit trail of all borrowing attempts, successes, and failures.

SCALABILITY:
Services remain independently deployable and don't hold locks across service boundaries, enabling better scalability than distributed transactions.

---

H2 - 3. Communication Style: Synchronous

LibHub uses synchronous HTTP/REST communication for all inter-service interactions. This communication style was chosen for its simplicity, request-response nature, and suitability for the library management domain.

H3 - 3.1 Synchronous Communication Overview

In synchronous communication, the calling service sends a request and blocks waiting for a response from the called service. All microservice communication in LibHub follows this pattern using HTTP/REST APIs.

CHARACTERISTICS:
- Request-Response pattern: Caller waits for response
- Blocking operation: Caller thread is blocked until response arrives
- Direct coupling: Services communicate via direct HTTP calls
- Immediate feedback: Caller immediately knows success or failure

H3 - 3.2 Communication Patterns

H4 - Gateway to Services

The API Gateway communicates synchronously with all backend services:

PATTERN: HTTP Request Forwarding
1. Frontend sends HTTP request to Gateway (e.g., POST /api/loans)
2. Gateway validates JWT token synchronously
3. Gateway queries Consul for service location
4. Gateway forwards request to target service
5. Gateway waits for service response
6. Gateway returns response to frontend

This is implemented through Ocelot's routing configuration, which handles the synchronous forwarding transparently.

H4 - Service to Service (Loan to Catalog)

The Loan Service calls the Catalog Service synchronously during saga execution:

PATTERN: HTTP Client Communication
1. LoanService needs to check book availability
2. LoanService queries Consul for CatalogService URL
3. LoanService creates HTTP request to CatalogService
4. LoanService sends GET /api/books/{id} and waits
5. CatalogService processes request and queries catalog_db
6. CatalogService returns book data as JSON
7. LoanService receives response and continues saga

This is implemented using HttpClient with service discovery:

```csharp
// Discover service URL from Consul
var catalogServiceUrl = await _serviceDiscovery.GetServiceUrlAsync("catalogservice");

// Make synchronous HTTP call
var response = await _httpClient.GetAsync($"{catalogServiceUrl}/api/books/{bookId}");
var book = await response.Content.ReadFromJsonAsync<BookResponse>();
```

H3 - 3.3 Benefits of Synchronous Communication

SIMPLICITY:
The request-response model is intuitive and easy to reason about. Developers can trace a single request through the system linearly.

IMMEDIATE CONSISTENCY:
The caller immediately knows if the operation succeeded or failed, simplifying error handling and user feedback.

TRANSACTIONAL WORKFLOWS:
Synchronous calls work naturally with the saga orchestration pattern where each step must complete before proceeding to the next.

DEBUGGING:
Easier to debug with standard HTTP debugging tools. Request flow is sequential and can be traced in logs with correlation IDs.

NO MESSAGE BROKER REQUIRED:
Eliminates the complexity and operational overhead of message brokers like RabbitMQ or Kafka.

H3 - 3.4 Handling Synchronous Communication Challenges

H4 - Timeouts

Each HTTP client is configured with appropriate timeouts:

- HttpClient timeout: 30 seconds
- Ocelot timeout (QoSOptions): 10 seconds per request
- Prevents indefinite blocking if a service becomes unresponsive

H4 - Retry and Circuit Breaker

Ocelot provides fault tolerance:

- Circuit Breaker: Opens after 3 consecutive failures
- Break Duration: 5 seconds before attempting again
- Prevents cascading failures when a service is down

H4 - Service Discovery

Consul eliminates hardcoded URLs:

- Services register their location dynamically
- Clients query Consul for current service locations
- Enables dynamic scaling and zero-downtime deployments

H4 - Token Propagation

JWT tokens are propagated across service boundaries:

```csharp
// Get token from incoming request
var token = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"];

// Forward token to downstream service
_httpClient.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(token);
```

This ensures authentication context is maintained across the entire request chain.

H4 - Correlation ID Propagation

Every request is tagged with a correlation ID:

```csharp
// Extract correlation ID from incoming request
var correlationId = _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"];

// Propagate to downstream service
_httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
```

This enables end-to-end request tracing in Seq, making it easy to see all logs related to a single user action across all services.

H3 - 3.5 Trade-offs

ADVANTAGES in LibHub context:
- Simple library management workflows fit request-response pattern
- Immediate user feedback for borrow/return operations
- Saga orchestration requires synchronous coordination
- Small number of services makes latency acceptable

LIMITATIONS acknowledged:
- Tight temporal coupling: Caller blocked until response
- Reduced availability: Failure in downstream service fails entire request
- Latency accumulation: Each synchronous hop adds latency
- Not suitable for long-running operations

For the library management domain with human-initiated, short-duration operations, synchronous communication provides the right balance of simplicity and functionality.

---

H2 - 4. Logging: Structured Logging with Serilog and Seq

LibHub implements comprehensive structured logging using Serilog for log generation and Seq for centralized log aggregation and analysis. This logging infrastructure provides complete visibility into system behavior, request flows, and distributed transactions.

H3 - 4.1 Structured Logging Overview

Structured logging differs from traditional text logging by treating log entries as data structures rather than plain text strings.

TRADITIONAL LOGGING:
"User 123 borrowed book 456 at 2025-11-10 14:30:00"

STRUCTURED LOGGING:
{
  "Message": "User borrowed book",
  "UserId": 123,
  "BookId": 456,
  "Timestamp": "2025-11-10T14:30:00Z",
  "ServiceName": "LoanService",
  "CorrelationId": "abc-123-def"
}

The structured approach enables powerful querying: "Show me all failed saga operations for book 456 in the last hour" becomes a simple query rather than complex text parsing.

H3 - 4.2 Serilog Configuration

Each microservice is configured with Serilog at startup:

CONFIGURATION ELEMENTS:

Minimum Log Level:
- Information level for application logs
- Warning level for ASP.NET Core framework logs (reduces noise)
- Information level for Entity Framework SQL queries (for debugging)

Enrichment:
- ServiceName: Identifies which service produced the log (UserService, CatalogService, etc.)
- MachineName: Container hostname for distributed deployments
- ThreadId: Tracks concurrent request processing
- CorrelationId: Tracks requests across service boundaries

Console Sink:
Outputs formatted logs to console for local development and Docker logs. Template includes timestamp, level, service name, correlation ID, and message.

Seq Sink:
Sends structured logs to Seq server at http://seq:80 (port 5341 externally). Logs are sent in real-time as JSON objects preserving all structure.

H3 - 4.3 Seq Centralized Logging

Seq is a specialized log server designed for structured logs from modern applications.

CAPABILITIES:

Centralized Storage:
All logs from all services (Gateway, UserService, CatalogService, LoanService) are stored in one place, making system-wide analysis possible.

Structured Querying:
Query logs using SQL-like syntax:
- "ServiceName = 'LoanService' AND Level = 'Error'"
- "CorrelationId = 'abc-123-def'" (trace entire request)
- "BookId = 456 AND Message LIKE '%SAGA%'" (find all saga operations for a book)

Real-time Monitoring:
Live tail of logs as they arrive, with filtering and highlighting.

Signal Creation:
Define alerts for specific patterns (e.g., saga failures, authentication errors).

Retention Policy:
Configured for 7-day retention to balance storage and debugging needs.

Web UI:
Accessible at http://localhost:5341 for developers and operators.

H3 - 4.4 Correlation ID Tracking

Correlation IDs are the key to tracing distributed requests across service boundaries.

FLOW:

1. Gateway receives request and generates correlation ID (GUID)
2. Gateway adds X-Correlation-ID header to downstream requests
3. Each service extracts correlation ID from headers
4. Each service enriches all logs with this correlation ID
5. Logs from all services share the same correlation ID

USAGE:

A single user action "Borrow book 456" might generate logs in:
- Gateway: Request received, routing to LoanService
- LoanService: Saga started, checking availability
- CatalogService: Book availability checked, stock decremented
- LoanService: Saga completed successfully
- Gateway: Response returned

In Seq, querying for that specific correlation ID shows all these logs in chronological order, providing a complete picture of the distributed transaction.

H3 - 4.5 Logging Patterns in LibHub

H4 - Request Lifecycle Logging

Every API request is logged:

ENTRY:
[HTTP-IN] Method, path, correlation ID logged when request enters the service.

EXIT:
[HTTP-OUT] Status code, duration logged when response is sent.

This provides performance metrics and request audit trail.

H4 - Saga Execution Logging

Saga steps are explicitly logged with semantic markers:

- [SAGA-START]: Saga initiation with user and book details
- [SAGA-STEP-1], [SAGA-STEP-2], etc.: Each saga step
- [SAGA-STEP-X-SUCCESS] or [SAGA-STEP-X-FAILED]: Step outcomes
- [SAGA-COMPENSATION]: Compensating transactions
- [SAGA-SUCCESS] or [SAGA-FAILED]: Final saga outcome

This enables tracking saga success rates and identifying failure points.

H4 - Inter-Service Communication Logging

All HTTP calls between services are logged:

OUTBOUND:
[INTER-SERVICE] Calling CatalogService at {url}: GET /api/books/{id}

RESPONSE:
[INTER-SERVICE] CatalogService response: {statusCode} for GET /api/books/{id}

This helps identify inter-service communication issues and performance bottlenecks.

H4 - Error Logging

Exceptions are logged with full context:

```csharp
_logger.LogError(ex, 
    "Failed to borrow book | UserId: {UserId} | BookId: {BookId}", 
    userId, bookId);
```

The exception details, stack trace, and contextual properties are all captured in Seq.

H3 - 4.6 Benefits of Structured Logging with Serilog and Seq

DEBUGGING DISTRIBUTED TRANSACTIONS:
Correlation IDs make it trivial to trace a request across all services, turning a distributed system into a traceable sequence of operations.

PERFORMANCE ANALYSIS:
Query all requests taking longer than 1 second, identify slow database queries, find bottlenecks in inter-service communication.

OPERATIONAL VISIBILITY:
Monitor service health, error rates, saga success rates in real-time without deploying additional monitoring tools.

AUDIT TRAIL:
Every user action, every saga execution, every state change is logged with full context for compliance and debugging.

DEVELOPER PRODUCTIVITY:
Instead of tailing multiple log files or container logs, developers query Seq to find exactly the logs they need in seconds.

PRODUCTION TROUBLESHOOTING:
When a user reports an issue, operators can search by user ID, book ID, or time range to find exactly what happened without reproducing the issue.

H3 - 4.7 Logging Best Practices Demonstrated

SEMANTIC LOG LEVELS:
- Information: Normal operations (saga steps, API calls)
- Warning: Recoverable issues (max loan limit reached)
- Error: Failures requiring attention (saga failed)

CONSISTENT MESSAGE TEMPLATES:
Using templates like "User borrowed book | UserId: {UserId} | BookId: {BookId}" ensures consistent property names across all services, enabling reliable querying.

CORRELATION CONTEXT:
Every log related to a single user action shares a correlation ID, making distributed debugging as easy as single-service debugging.

PERFORMANCE AWARENESS:
Logs include timing information (request duration, saga execution time) without requiring separate APM tools.

---

H2 - Conclusion

LibHub demonstrates a production-ready microservices architecture with:

ARCHITECTURE: Four independent services (User, Catalog, Loan, Gateway) with database-per-service pattern, managed through Consul service discovery.

DISTRIBUTED TRANSACTIONS: Saga orchestration pattern in LoanService coordinates operations across multiple databases with clear compensation strategies.

COMMUNICATION: Synchronous HTTP/REST communication with proper timeout handling, circuit breakers, and token/correlation propagation.

OBSERVABILITY: Structured logging with Serilog and centralized log aggregation in Seq provides complete visibility into distributed operations.

The system showcases how modern architectural patterns can be combined to build scalable, maintainable, and observable distributed systems. The comprehensive logging and clear saga implementation make the system debuggable and auditable, while service discovery and API gateway patterns ensure operational flexibility.

---

END OF DOCUMENT
