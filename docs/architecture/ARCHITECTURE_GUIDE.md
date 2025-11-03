# LibHub Architecture Learning Guide

## Quick Start

This guide helps you understand the LibHub architecture from high-level concepts to specific code implementation using a layered abstraction approach.

## Documentation Location

All architecture documentation is located in:
```
/home/thuannp4/development/LibHub/docs/**architecture**/
```$$

## Learning Path

### Step 1: Read the Foundation (5 minutes)
**File:** `ai-docs/master-context/00_PROJECT_CONTEXT.md`

Get familiar with:
- What LibHub does
- Technology stack
- Microservices overview

### Step 2: Follow the 5-Layer Deep Dive (2.5-3 hours)

Read these documents in order:

1. **Layer 1: System Overview** (15-20 min)
   - `docs/architecture/ARCHITECTURE_LAYER1_SYSTEM_OVERVIEW.md`
   - Understand all 5 components and how they connect

2. **Layer 2: UserService Deep Dive** (30-40 min)
   - `docs/architecture/ARCHITECTURE_LAYER2_USERSERVICE_DEEP_DIVE.md`
   - Learn Clean Architecture with real code examples

3. **Layer 3: Dependency Flow** (25-35 min)
   - `docs/architecture/ARCHITECTURE_LAYER3_DEPENDENCY_FLOW.md`
   - See how dependency injection wires everything together

4. **Layer 4: Inter-Service Communication** (35-45 min)
   - `docs/architecture/ARCHITECTURE_LAYER4_INTER_SERVICE_COMMUNICATION.md`
   - Explore Saga pattern and service discovery

5. **Layer 5: Complete Data Flow Examples** (40-50 min)
   - `docs/architecture/ARCHITECTURE_LAYER5_DATA_FLOW_EXAMPLES.md`
   - Walk through real scenarios with complete code traces

### Step 3: Explore the Code

After reading the documentation, explore the codebase:

```bash
# Start with UserService (simplest)
cd src/Services/UserService/

# View the 4-layer structure
ls -R
# LibHub.UserService.Domain/
# LibHub.UserService.Application/
# LibHub.UserService.Infrastructure/
# LibHub.UserService.Api/

# Then explore LoanService (most complex - has Saga pattern)
cd ../LoanService/
```

### Step 4: Run and Test

```bash
# Start the entire system
docker compose up -d

# Wait 60 seconds for services to start

# Test the system
./scripts/test-docker-containers.sh
./scripts/test-consul-discovery.sh
./scripts/test-gateway-integration.sh

# Access the application
# Frontend: http://localhost:8080
# Gateway: http://localhost:5000
# Consul UI: http://localhost:8500
```

## Quick Reference by Topic

### Understanding Clean Architecture
- **Document:** Layer 2
- **Key Sections:**
  - Clean Architecture Overview
  - Layer 1: Domain Layer
  - Layer 2: Application Layer
  - Layer 3: Infrastructure Layer
  - Layer 4: Presentation Layer

### Understanding Dependency Injection
- **Document:** Layer 3
- **Key Sections:**
  - Service Registration in Program.cs
  - Service Lifetimes Explained
  - Dependency Resolution Example

### Understanding Saga Pattern
- **Document:** Layer 4
- **Key Sections:**
  - The Saga Pattern
  - Saga Implementation: Borrow Book
  - Saga Flow Diagram

### Understanding Complete Flows
- **Document:** Layer 5
- **Key Sections:**
  - Scenario 1: User Registration
  - Scenario 2: Borrow Book (Saga Pattern)
  - Scenario 3: Saga Failure

## Visual Learning

All documents include Mermaid diagrams:
- System architecture diagrams
- Sequence diagrams for request flows
- State machines
- Dependency graphs

View these diagrams in:
- GitHub (renders Mermaid automatically)
- VS Code (with Mermaid extension)
- Any Mermaid-compatible markdown viewer

## Key Files to Understand

### UserService (Start Here)
```
src/Services/UserService/
├── LibHub.UserService.Domain/
│   ├── User.cs                    # Domain entity with business rules
│   └── IUserRepository.cs         # Repository interface
├── LibHub.UserService.Application/
│   └── Services/
│       └── IdentityApplicationService.cs  # Use case orchestration
├── LibHub.UserService.Infrastructure/
│   ├── Repositories/
│   │   └── EfUserRepository.cs    # EF Core implementation
│   └── Security/
│       ├── PasswordHasher.cs      # BCrypt hashing
│       └── JwtTokenGenerator.cs   # JWT token creation
└── LibHub.UserService.Api/
    ├── Controllers/
    │   └── UsersController.cs     # HTTP endpoints
    └── Program.cs                 # Dependency injection setup
```

### LoanService (Advanced - Saga Pattern)
```
src/Services/LoanService/
├── LibHub.LoanService.Domain/
│   └── Loan.cs                    # State machine (PENDING → CheckedOut → Returned)
├── LibHub.LoanService.Application/
│   └── Services/
│       └── LoanApplicationService.cs  # Saga orchestrator
└── LibHub.LoanService.Infrastructure/
    └── HttpClients/
        └── CatalogServiceHttpClient.cs  # HTTP calls to CatalogService
```

## Common Questions

### Q: Where do I start if I'm new to the project?
**A:** Read Layer 0 (PROJECT_CONTEXT.md), then Layer 1 (System Overview). This gives you the foundation.

### Q: I want to understand how a specific feature works. Where do I look?
**A:** 
- User registration/login → Layer 2 + Layer 5 Scenario 1
- Book borrowing → Layer 4 + Layer 5 Scenario 2
- Service communication → Layer 4
- Dependency injection → Layer 3

### Q: How do I understand the Saga pattern?
**A:** Read Layer 4 (Inter-Service Communication) then Layer 5 Scenario 2 (Borrow Book with complete code trace).

### Q: I want to add a new feature. What pattern should I follow?
**A:** Study UserService in Layer 2 to understand Clean Architecture, then follow the same 4-layer structure for your new feature.

### Q: How do services communicate?
**A:** Read Layer 4 (Inter-Service Communication) which explains HTTP clients, service discovery, and the Saga pattern.

### Q: What happens when a distributed transaction fails?
**A:** See Layer 5 Scenario 3 (Saga Failure) which shows compensating transactions in action.

## Learning Objectives

After completing this guide, you will:

✅ Understand the overall system architecture  
✅ Know how Clean Architecture works in practice  
✅ Trace requests from frontend to database  
✅ Understand dependency injection and service lifetimes  
✅ Comprehend inter-service communication patterns  
✅ Recognize the Saga pattern for distributed transactions  
✅ Be able to add new features following established patterns  
✅ Debug issues by identifying the responsible layer  

## Next Steps

1. ✅ Read all 5 architecture layers
2. ⬜ Run the application locally
3. ⬜ Explore the codebase
4. ⬜ Make a small change (e.g., modify loan period)
5. ⬜ Add a new feature (e.g., book reservation)
6. ⬜ Write tests for your changes

## Additional Resources

- **Project Status:** `ai-docs/PROJECT_STATUS.md`
- **API Contracts:** `ai-docs/shared-context/API_CONTRACTS.md`
- **Testing Guide:** `tests/e2e/README.md`
- **Deployment Guide:** `docs/deployment/DOCKER_QUICK_START.md`
- **Git Commands:** `docs/development/GIT_COMMANDS.md`

## Feedback

If you find any part of the documentation unclear or have suggestions, note them for future improvements.

---

**Happy Learning!** 🚀

Start with: `docs/architecture/README.md` for detailed navigation.


