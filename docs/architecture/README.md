# LibHub Architecture Documentation

## Overview

This directory contains comprehensive architecture documentation for the LibHub project. The documentation is structured in 5 progressive layers, each building on the previous one to help you understand how the system works from high-level concepts down to specific code implementation.

## Reading Order

Read these documents in sequence for the best learning experience:

### Layer 0: Foundation
**File:** `../../ai-docs/master-context/00_PROJEC$$T_CONTEXT.md`

Start here to understand the project's purpose, technology stack, and high-level architecture patterns.

### Layer 1: System Overview
**File:** `ARCHITECTURE_LAYER1_SYSTEM_OVERVIEW.md`

Learn about:
- All 5 system components (Frontend, Gateway, 3 microservices)
- Network architecture and port mapping
- Basic request flow diagrams
- Database per service pattern
- Docker deployment structure

**Time to read:** 15-20 minutes

### Layer 2: UserService Deep Dive
**File:** `ARCHITECTURE_LAYER2_USERSERVICE_DEEP_DIVE.md`

Understand Clean Architecture through UserService:
- 4-layer architecture (Domain, Application, Infrastructure, Presentation)
- How each layer works with code examples
- Data flow through layers
- Why dependencies point inward
- Register and Login workflows

**Time to read:** 30-40 minutes

### Layer 3: Dependency Flow & Injection
**File:** `ARCHITECTURE_LAYER3_DEPENDENCY_FLOW.md`

Learn how everything wires together:
- Dependency injection in ASP.NET Core
- Service lifetimes (Scoped, Singleton, Transient)
- Request lifecycle from HTTP to database
- Middleware pipeline
- Configuration management

**Time to read:** 25-35 minutes

### Layer 4: Inter-Service Communication
**File:** `ARCHITECTURE_LAYER4_INTER_SERVICE_COMMUNICATION.md`

Explore distributed systems patterns:
- HTTP-based service communication
- Saga pattern for distributed transactions
- Compensating transactions
- Service discovery with Consul
- Error handling strategies

**Time to read:** 35-45 minutes

### Layer 5: Complete Data Flow Examples
**File:** `ARCHITECTURE_LAYER5_DATA_FLOW_EXAMPLES.md`

Walk through real scenarios:
- User registration (complete code trace)
- Borrow book with Saga pattern
- Saga failure with rollback
- Data transformation at each layer
- JWT token flow

**Time to read:** 40-50 minutes

## Total Learning Time

**Estimated:** 2.5 - 3 hours for complete understanding

## Quick Reference

### For Understanding Specific Topics

| Topic | Document | Section |
|-------|----------|---------|
| What is LibHub? | Layer 0 | Problem Statement |
| System components | Layer 1 | Component Breakdown |
| Clean Architecture | Layer 2 | Clean Architecture Overview |
| Domain entities | Layer 2 | Layer 1: Domain Layer |
| Use case orchestration | Layer 2 | Layer 2: Application Layer |
| Database access | Layer 2 | Layer 3: Infrastructure Layer |
| HTTP API | Layer 2 | Layer 4: Presentation Layer |
| Dependency injection | Layer 3 | Service Registration |
| Service lifetimes | Layer 3 | Service Lifetimes Explained |
| Middleware | Layer 3 | Middleware Pipeline |
| Service communication | Layer 4 | HTTP Client Implementation |
| Saga pattern | Layer 4 | The Saga Pattern |
| Consul | Layer 4 | Service Discovery with Consul |
| User registration flow | Layer 5 | Scenario 1 |
| Borrow book flow | Layer 5 | Scenario 2 |
| Saga failure | Layer 5 | Scenario 3 |

## Visual Diagrams

All documents include Mermaid diagrams that render in most markdown viewers:
- System architecture diagrams
- Sequence diagrams for request flows
- State machines for domain entities
- Dependency injection graphs
- Saga orchestration flows

## Code References

Documents reference actual source code with file paths:
- `src/Services/UserService/` - UserService implementation
- `src/Services/CatalogService/` - CatalogService implementation
- `src/Services/LoanService/` - LoanService with Saga pattern
- `src/Gateway/` - Ocelot API Gateway
- `frontend/` - Vanilla JavaScript frontend

## Key Architectural Patterns

### Microservices Architecture
- Independent services with separate databases
- HTTP/REST communication
- Service discovery with Consul

### Clean Architecture (per service)
- Domain Layer (business rules, zero dependencies)
- Application Layer (use case orchestration)
- Infrastructure Layer (technical implementation)
- Presentation Layer (HTTP API)

### Saga Pattern
- Distributed transactions across services
- Compensating transactions for rollback
- PENDING → CheckedOut or FAILED state flow

### Database Per Service
- Each service owns its database
- No foreign key constraints between services
- Data consistency via Saga pattern

## Learning Objectives

After reading all 5 layers, you will be able to:

1. **Explain** the overall system architecture and how components interact
2. **Trace** a request from frontend through all layers to database and back
3. **Understand** why Clean Architecture separates concerns into layers
4. **Implement** new features following established patterns
5. **Debug** issues by identifying which layer is responsible
6. **Appreciate** trade-offs in distributed systems design
7. **Recognize** how Saga pattern maintains consistency across services

## Additional Resources

### Project Documentation
- `../../README.md` - Project overview and quick start
- `../../ai-docs/PROJECT_STATUS.md` - Implementation status
- `../../ai-docs/master-context/` - Domain models and context

### Testing Documentation
- `../../tests/e2e/README.md` - End-to-end testing guide
- `../../ai-docs/testing-phase/` - Testing strategies

### Deployment Documentation
- `../../docs/deployment/` - Docker and deployment guides
- `../../FEDORA_DEPLOYMENT_GUIDE.md` - Linux deployment
- `../../docs/windows/` - Windows setup guides

## Feedback and Questions

If you find any part of the documentation unclear or have suggestions for improvement, please note them for future updates.

## Document Metadata

- **Created:** November 2025
- **Last Updated:** November 2025
- **Target Audience:** Developers learning LibHub architecture
- **Prerequisites:** Basic understanding of C#, ASP.NET Core, and microservices concepts
- **Difficulty Level:** Intermediate to Advanced

## Next Steps After Reading

1. **Run the application** using `docker compose up -d`
2. **Explore the code** starting with UserService
3. **Try modifying** a feature (e.g., change loan period from 14 to 21 days)
4. **Add a new feature** following Clean Architecture patterns
5. **Write tests** for your changes
6. **Review** the Saga pattern implementation in LoanService

Happy learning! 🚀


