# Logging Concepts - Quick Reference

This document provides concise explanations of key logging concepts used in LibHub for presentation reference.

---

## What is Serilog?

**Serilog** is a structured logging library for .NET applications.

### Key Features

**Structured Logging**: Instead of writing plain text logs, Serilog writes logs as structured data (JSON-like format) with named properties.

Example:
```csharp
// Traditional logging (plain text)
logger.Log("User 123 borrowed book 456");

// Serilog (structured)
logger.LogInformation("User borrowed book | UserId: {UserId} | BookId: {BookId}", 123, 456);
```

**Multiple Sinks**: Can send logs to multiple destinations simultaneously (Console, Seq, File, Database, etc.).

**Enrichment**: Automatically adds contextual information to every log:
- ServiceName: Which microservice generated the log
- MachineName: Which server/container
- ThreadId: Which thread was processing
- Timestamp: When it happened

**Filtering**: Different log levels (Information, Warning, Error) with easy configuration.

### Why We Use Serilog

- Easy to query logs by specific properties (UserId, BookId, CorrelationId)
- Better debugging in distributed systems
- Industry-standard library for .NET microservices
- Integrates seamlessly with Seq for log analysis

---

## What is Seq?

**Seq** is a centralized log server designed specifically for structured logs.

### Key Features

**Centralized Storage**: All logs from all microservices (Gateway, UserService, CatalogService, LoanService) are stored in one place.

**Powerful Querying**: Query logs using SQL-like syntax:
```
ServiceName = 'LoanService' AND Level = 'Error'
CorrelationId = 'abc-123-def'
BookId = 456 AND Message LIKE '%SAGA%'
```

**Real-time Monitoring**: Watch logs appear in real-time as they happen across all services.

**Web UI**: Browser-based interface at http://localhost:5341 for easy access.

**Signal Alerts**: Set up notifications for specific error patterns or conditions.

**Retention Policy**: Automatically manages log storage (configured for 7 days in LibHub).

### Why We Use Seq

- Single place to see what's happening across all microservices
- Find specific user actions or errors in seconds
- Trace distributed transactions across service boundaries
- No need to SSH into containers or read raw log files
- Free for development, affordable for production

---

## What is Correlation ID?

**Correlation ID** is a unique identifier (usually a GUID) that tracks a single request as it flows through multiple services.

### The Problem It Solves

In a microservices architecture, one user action might touch multiple services:

1. User clicks "Borrow Book" in Frontend
2. Request goes to Gateway
3. Gateway routes to LoanService
4. LoanService calls CatalogService (twice)
5. Response returns through Gateway to Frontend

Without correlation IDs, logs from these 4 services are scattered and hard to connect.

### How It Works

**Generation**: API Gateway generates a unique ID when request enters the system (e.g., "a1b2c3d4-e5f6-7890")

**Propagation**: ID is passed in HTTP header (X-Correlation-ID) to every downstream service

**Logging**: Every service includes this ID in all its log entries

**Querying**: Search Seq for that ID to see ALL logs related to that one user action

### Example Flow

```
[Gateway]      CorrelationId: a1b2c3d4 - Received POST /api/loans
[LoanService]  CorrelationId: a1b2c3d4 - SAGA-START BorrowBook
[LoanService]  CorrelationId: a1b2c3d4 - SAGA-STEP-1 Loan created
[CatalogService] CorrelationId: a1b2c3d4 - GET /api/books/456
[LoanService]  CorrelationId: a1b2c3d4 - SAGA-STEP-2 Book available
[CatalogService] CorrelationId: a1b2c3d4 - PUT /api/books/456/stock
[LoanService]  CorrelationId: a1b2c3d4 - SAGA-SUCCESS
[Gateway]      CorrelationId: a1b2c3d4 - Response 200 OK
```

In Seq, one query `CorrelationId = 'a1b2c3d4'` shows all 8 logs in chronological order.

### Why We Use Correlation ID

- Trace a single user request across all microservices
- Debug distributed transactions easily
- Understand the complete flow of any operation
- Identify where failures occur in the request chain
- Essential for microservices observability

---

## How They Work Together in LibHub

**Serilog** generates structured logs with correlation IDs and sends them to **Seq**. 

**Seq** stores and indexes these logs, making it easy to query by **Correlation ID** to trace distributed requests.

This combination makes debugging microservices as easy as debugging a single application.

### Quick Demo Scenario

Problem: User reports "I tried to borrow a book but got an error"

Traditional approach: Check logs in 4 different services, try to match timestamps, guess which logs are related.

With Serilog + Seq + Correlation ID:
1. Get correlation ID from Gateway logs (filter by timestamp and user)
2. Query Seq: `CorrelationId = 'xyz'`
3. See complete flow across all services
4. Identify exact step where saga failed
5. Fix the issue

Time saved: Hours → Minutes

---

## Summary

| Concept | What It Is | Why It Matters |
|---------|-----------|----------------|
| **Serilog** | Structured logging library | Makes logs queryable as data |
| **Seq** | Centralized log server | One place to see all microservice logs |
| **Correlation ID** | Unique request identifier | Traces requests across services |

Together, they provide **complete observability** in a distributed microservices architecture.
