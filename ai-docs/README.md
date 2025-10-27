Here's the complete **README.md** for the `ai-docs/` folder:

```markdown
# LibHub AI Documentation

This folder contains all context files and task instructions for implementing LibHub with AI assistance. The structure is optimized for drag-and-drop workflows where each task is implemented in a separate conversation with an AI coding agent.

---

## 📁 Folder Structure

```
ai-docs/
│
├── README.md                       # This file - usage guide
├── PROJECT_STATUS.md               # ⭐ CENTRAL STATE TRACKER (always drag!)
│
├── master-context/                 # Core project context (always drag)
│   ├── 00_PROJECT_CONTEXT.md       # Project overview, architecture, use cases
│   ├── 00_DOMAIN_REFERENCE.md      # Domain model, entities, business rules
│   └── 00_ENVIRONMENT_CONFIG.md    # Database, ports, JWT config
│
├── service-context/                # Service-specific designs (drag when needed)
│   ├── UserService/
│   │   └── SERVICE_UserService.md
│   ├── CatalogService/
│   │   └── SERVICE_CatalogService.md
│   ├── LoanService/
│   │   └── SERVICE_LoanService.md
│   └── Gateway/
│       └── SERVICE_Gateway.md
│
├── shared-context/                 # Cross-cutting concerns (drag when needed)
│   ├── API_CONTRACTS.md            # All REST API endpoints
│   └── FRONTEND_SPECS.md           # UI wireframes and frontend specs
│
├── tasks/                          # Task instructions by phase
│   ├── phase-1-database/
│   │   ├── task-1.1-setup-userservice-db.md
│   │   ├── task-1.2-setup-catalogservice-db.md
│   │   └── task-1.3-setup-loanservice-db.md
│   ├── phase-2-userservice/
│   │   ├── task-2.1-domain-layer.md
│   │   ├── task-2.2-application-layer.md
│   │   ├── task-2.3-infrastructure-layer.md
│   │   ├── task-2.4-presentation-layer.md
│   │   └── task-2.5-testing.md
│   ├── phase-3-catalogservice/
│   ├── phase-4-loanservice/
│   ├── phase-5-gateway/
│   └── phase-6-frontend/
│
└── completed-artifacts/            # Archive of completed task files
    └── .gitkeep
```

---

## 🎯 Core Workflow Principles

### 1. **PROJECT_STATUS.md is Your Single Source of Truth**

**Always read this file first** in every new conversation. It tells you:
- What's been completed
- What's currently in progress
- What's ready to start next
- Known issues and blockers
- Service readiness status

### 2. **One Task Per Conversation**

Each task gets a fresh AI conversation:
- Provides clean context without conversation history bloat
- Allows focused implementation
- Makes debugging easier
- Keeps token usage manageable

### 3. **Drag-and-Drop Pattern**

**Always drag these 3 items** to every conversation:
1. ✅ `PROJECT_STATUS.md` (state tracker)
2. ✅ `master-context/` folder (entire folder)
3. ✅ Current task file from `tasks/phase-X/`

**Optionally drag** (based on task):
- Service-specific context from `service-context/`
- API contracts or frontend specs from `shared-context/`

### 4. **Update After Completion**

After AI completes a task:
1. **You manually update** `PROJECT_STATUS.md`
2. Move completed task file to `completed-artifacts/`
3. Git commit both changes
4. Ready for next task!

---

## 📋 Complete Workflow Example

### Example: Phase 1 - Task 1.1 (Setup UserService Database)

#### **Step 1: Prepare Files to Drag**

Open your file manager and prepare these items:

```
✓ ai-docs/PROJECT_STATUS.md
✓ ai-docs/master-context/ (entire folder)
✓ ai-docs/tasks/phase-1-database/task-1.1-setup-userservice-db.md
```

#### **Step 2: Start New AI Conversation**

Drag the 3 items above into your AI coding agent.

**Prompt the AI**:
```
Read PROJECT_STATUS.md first to understand current project state, 
then implement task-1.1-setup-userservice-db.md.
```

#### **Step 3: AI Implements Task**

The AI will:
1. Read `PROJECT_STATUS.md` to see Phase 0 is complete
2. Read master context to understand project architecture
3. Follow step-by-step instructions in task file
4. Generate all required code and migrations

#### **Step 4: Execute AI's Instructions**

Follow AI's output to:
- Create UserService projects
- Set up UserDbContext
- Create and apply migrations
- Verify database creation

#### **Step 5: Update PROJECT_STATUS.md** ⭐

**This is YOUR responsibility** - manually edit the file:

```
## Completed Tasks ✅

### Phase 1: Database Setup
- ✅ **Task 1.1**: UserService database schema created (2025-10-27)
  - **Files Created**: 
    - User.cs
    - UserDbContext.cs
    - DesignTimeDbContextFactory.cs
    - Migrations/InitialCreate.cs
  - **Verification**: Users table exists in user_db
  - **Connection String**: Working with libhub_user

## Service Readiness Status

| Service | Database | Domain | Application | Infrastructure | Presentation | Tests | Ready? |
|---------|----------|--------|-------------|----------------|--------------|-------|--------|
| UserService | ✅ | ⚪ | ⚪ | 🟡 | ⚪ | ⚪ | ❌ |
```

#### **Step 6: Move Task File**

```
mv ai-docs/tasks/phase-1-database/task-1.1-setup-userservice-db.md \
   ai-docs/completed-artifacts/
```

#### **Step 7: Git Commit**

```
git add src/Services/UserService/
git commit -m "✅ Task 1.1: Setup UserService database schema"

git add ai-docs/PROJECT_STATUS.md
git commit -m "docs: Update status after Task 1.1"
```

#### **Step 8: Ready for Next Task!**

Now repeat for Task 1.2 (CatalogService DB), Task 1.3 (LoanService DB).

---

## 🔄 Workflow for Different Phases

### Phase 1: Database Setup (Simple - No Service Context)

**What to Drag**:
```
✓ PROJECT_STATUS.md
✓ master-context/
✓ tasks/phase-1-database/task-1.X-XXXX.md
```

**Why**: Database tasks only need domain model reference, no service-specific logic yet.

---

### Phase 2: UserService Implementation (Add Service Context)

#### Task 2.1: Domain Layer

**What to Drag**:
```
✓ PROJECT_STATUS.md
✓ master-context/
✓ service-context/UserService/
✓ tasks/phase-2-userservice/task-2.1-domain-layer.md
```

**AI Prompt**:
```
Read PROJECT_STATUS.md to see Phase 1 complete and database ready.
Then implement task-2.1-domain-layer.md for UserService.
```

#### Task 2.2: Application Layer

**What to Drag**:
```
✓ PROJECT_STATUS.md
✓ master-context/
✓ service-context/UserService/
✓ tasks/phase-2-userservice/task-2.2-application-layer.md
```

**AI Prompt**:
```
Read PROJECT_STATUS.md to see Domain Layer complete.
Then implement task-2.2-application-layer.md for UserService.
```

#### Task 2.3: Infrastructure Layer

**What to Drag**:
```
✓ PROJECT_STATUS.md
✓ master-context/
✓ service-context/UserService/
✓ tasks/phase-2-userservice/task-2.3-infrastructure-layer.md
```

**AI Prompt**:
```
Read PROJECT_STATUS.md to see Application Layer complete.
Then implement task-2.3-infrastructure-layer.md with EF Core, BCrypt, and JWT.
```

#### Task 2.4: Presentation Layer

**What to Drag**:
```
✓ PROJECT_STATUS.md
✓ master-context/
✓ service-context/UserService/
✓ shared-context/API_CONTRACTS.md (for endpoint specs)
✓ tasks/phase-2-userservice/task-2.4-presentation-layer.md
```

**AI Prompt**:
```
Read PROJECT_STATUS.md to see Infrastructure complete.
Then implement task-2.4-presentation-layer.md with controllers and Swagger.
```

#### Task 2.5: Testing

**What to Drag**:
```
✓ PROJECT_STATUS.md
✓ master-context/
✓ service-context/UserService/
✓ tasks/phase-2-userservice/task-2.5-testing.md
```

**AI Prompt**:
```
Read PROJECT_STATUS.md to see all UserService layers complete.
Then write comprehensive tests per task-2.5-testing.md.
```

---

### Phase 4: LoanService (Complex - Multiple Services)

When implementing Task 4.4 (Saga pattern), you need context from **multiple services**:

**What to Drag**:
```
✓ PROJECT_STATUS.md
✓ master-context/
✓ service-context/LoanService/
✓ service-context/CatalogService/     ← Need this for HTTP calls!
✓ shared-context/API_CONTRACTS.md     ← Need endpoint specs!
✓ tasks/phase-4-loanservice/task-4.4-saga-implementation.md
```

**Why**: Saga orchestration in LoanService calls CatalogService endpoints.

---

## 📊 Reading PROJECT_STATUS.md

Before **every** task implementation, the AI should check:

### 1. Phase Status Overview
```
| Phase 1: Database Setup | ✅ **COMPLETE** | 100% (3/3) |
| Phase 2: UserService | 🟡 **IN PROGRESS** | 40% (2/5) |
```
**Tells you**: What's done, what's next.

### 2. Service Readiness
```
| Service | Database | Domain | Application | Infrastructure | Presentation | Tests | Ready? |
| UserService | ✅ | ✅ | 🟡 | ⚪ | ⚪ | ⚪ | ❌ |
```
**Tells you**: Which layers are complete, what's safe to build on.

### 3. Completed Tasks
```
- ✅ **Task 1.1**: UserService database (2025-10-27)
- ✅ **Task 2.1**: UserService Domain Layer (2025-10-27)
```
**Tells you**: Detailed history of what's been implemented.

### 4. Current Task
```
### Current Task
**Task 2.2**: Implement UserService Application Layer
**Started**: 2025-10-27
**Blocked By**: None
```
**Tells you**: What you're working on right now.

### 5. Implementation Notes
```
## Implementation Notes & Decisions

### Database
- ✅ Using `caching_sha2_password` (MySQL 8.4+)
- ✅ Three separate databases confirmed

### Authentication
- ✅ JWT configuration defined
- ✅ BCrypt work factor: 11
```
**Tells you**: Important technical decisions made.

---

## 🎓 Tips for Success

### For You (Developer)

1. **Always Update PROJECT_STATUS.md**: This is not optional - it's how continuity works
2. **Move Completed Tasks**: Keep `tasks/` folder clean with only pending work
3. **Git Commit Frequently**: After each task completion
4. **Read Before Starting**: Check PROJECT_STATUS.md yourself before each task
5. **One Task at a Time**: Don't skip ahead or combine tasks

### For AI Agent

1. **Read PROJECT_STATUS.md First**: Always start by understanding current state
2. **Check Dependencies**: Verify prerequisite tasks are marked complete
3. **Use Existing Patterns**: Look at completed tasks for code style consistency
4. **Explicit File Paths**: Always use full paths like `~/Projects/LibHub/src/...`
5. **Verify Commands**: Provide verification commands in output

---

## 🚀 Quick Start Checklist

Before implementing your first task:

- [ ] Phase 0 completed (MySQL installed, databases created)
- [ ] PROJECT_STATUS.md exists and is up-to-date
- [ ] All master-context files created
- [ ] All service-context files created
- [ ] Task files for Phase 1 created
- [ ] Git repository initialized

**Ready to start!** Begin with Task 1.1.

---

## 📝 Task File Template

Each task file follows this structure:

```
# Task X.Y: [Task Name]

**Phase**: X - [Phase Name]
**Estimated Time**: X hours
**Dependencies**: Task X.X

## Objective
[What needs to be accomplished]

## Prerequisites
- [ ] Checklist of required prior tasks

## Step-by-Step Instructions
[Detailed implementation steps]

## Acceptance Criteria
- [ ] Measurable completion criteria

## Verification Commands
[Commands to verify success]

## After Completion
### Update PROJECT_STATUS.md
[Specific sections to update]

### Git Commit
[Commit commands]

### Move Task File
[Archive command]

## Next Task
[What comes next]
```

---

## 🔧 Troubleshooting

### Issue: AI doesn't see latest changes

**Solution**: Make sure you dragged the **updated** PROJECT_STATUS.md, not an old cached version.

### Issue: AI suggests already-implemented code

**Solution**: PROJECT_STATUS.md probably not updated. Check "Completed Tasks" section.

### Issue: Task fails due to missing dependency

**Solution**: Check "Service Readiness Status" table - required layer might not be complete.

### Issue: Can't find what to drag for a task

**Solution**: Look at the task file's "Prerequisites" section - it lists required context files.

---

## 📞 Project Information

**Project**: LibHub - Library Management System  
**Architecture**: Microservices with Clean Architecture  
**Tech Stack**: ASP.NET Core 8, MySQL 8, EF Core, Ocelot, JWT  
**Development**: Ubuntu 25.10, VSCode  
**Total Tasks**: ~20 tasks across 6 phases  

---

## 📚 Additional Resources

- **LibHub_SRS.pdf**: Original Software Requirements Specification
- **Architecture Diagrams**: See `Artifacts_2_db_schema__high_level_architecture.pdf`
- **Domain Model**: See `Artifacts_1_domain_model__ubiquitous_language_glossary.pdf`
- **API Specs**: See `Artifacts_3_saga_sequence_diagram__openai_specification.pdf`

---

**Last Updated**: 2025-10-27  
**Current Phase**: Phase 1 - Database Setup  
**Status**: Ready to implement Task 1.1

---

## 🎉 Success Metrics

**Phase 1 Complete**: All 3 databases created with migrations ✅  
**Phase 2 Complete**: UserService fully implemented and tested ✅  
**Phase 3 Complete**: CatalogService fully implemented and tested ✅  
**Phase 4 Complete**: LoanService with Saga pattern implemented ✅  
**Phase 5 Complete**: API Gateway routing all services ✅  
**Phase 6 Complete**: Frontend connected and functional ✅  

**PROJECT COMPLETE**: Working LibHub application! 🚀
```

***

This README provides:
1. ✅ Complete folder structure explanation
2. ✅ Core workflow principles with PROJECT_STATUS.md as central tracker
3. ✅ Step-by-step example for Phase 1 Task 1.1
4. ✅ Detailed workflows for Phase 2 (all 5 UserService tasks)
5. ✅ What to drag for different phases
6. ✅ How to read and update PROJECT_STATUS.md
7. ✅ Tips for both you and the AI agent
8. ✅ Troubleshooting common issues
9. ✅ Quick reference tables and checklists
