# Universal Refactoring Workflow for AI Agents

> **Purpose:** Step-by-step workflow for refactoring any LibHub microservice  
> **Version:** 1.1  
> **Date:** November 3, 2025  
> **Last Updated:** November 3, 2025 (Added lessons from UserService refactoring)

---

## 🎯 Core Principle

**Understand → Plan → Execute → Validate → Document**

---

## 📋 Universal Workflow Steps

### **Phase 1: Understanding (MUST DO FIRST)**

1. **Read Current Service Structure**
   - Explore the service folder to understand current architecture
   - Identify all projects, files, and dependencies
   - Note API endpoints and their contracts

2. **Read Refactoring Goal**
   - Open `REFACTORING_ROADMAP.md`
   - Find the target service section
   - Understand the desired end state and structure

3. **Check Progress Status**
   - Open `REFACTORING_PROGRESS.md`
   - Find the service phase
   - Identify completed tasks and blockers
   - Determine starting point

4. **Review Rules**
   - Read `REFACTORING_RULES.md` for constraints
   - Remember: NEVER break API contracts
   - Follow SOLID principles appropriately

---

### **Phase 2: Execution (Based on User Prompt)**

**User will specify number of tasks to complete (e.g., "do tasks 1.1 to 1.5")**

For each task:

1. **Before Starting**
   - Read task description from `REFACTORING_PROGRESS.md`
   - Check for any blockers from previous attempts
   - Understand dependencies on previous tasks

2. **During Execution**
   - Follow folder structure standards from `REFACTORING_RULES.md`
   - Copy files from old structure to new structure
   - Update namespaces and references
   - Remove interfaces where appropriate (simple services)
   - Keep interfaces where needed (complex services, external dependencies)

3. **After Each Major Step**
   - Check for linter errors using `read_lints` tool
   - Fix any errors immediately
   - Don't proceed if errors exist

---

### **Phase 3: Validation (After Completing Tasks)**

1. **Build Validation**
   - **IMPORTANT:** If `dotnet` CLI is not available on server, use Docker for building
   - Run `docker compose build [service-name]` to verify compilation
   - Check build logs for errors (look for "Build succeeded" or "Build FAILED")
   - Ensure zero compilation errors

2. **API Contract Validation**
   - Compare controller endpoints with original
   - Verify request/response DTOs match exactly
   - Check HTTP methods and routes unchanged

3. **Docker Validation** (if completing final tasks)
   - Build Docker image
   - Run service in Docker
   - Test endpoints through gateway
   - Verify Consul registration

---

### **Phase 4: Documentation (MUST DO BEFORE FINISHING)**

1. **Update Progress File**
   - Open `REFACTORING_PROGRESS.md`
   - Check completed tasks with [x]
   - Update progress percentage
   - Update status (Not Started → In Progress → Completed)

2. **Document Blockers**
   - If encountered issues, add blocker notes
   - Describe what failed and why
   - Suggest resolution approach
   - Mark task status appropriately

3. **Update Workflow File (CRITICAL - DO NOT SKIP)**
   - **MANDATORY:** If you encountered ANY new issue during refactoring, you MUST update this workflow file
   - Open `REFACTORING_WORKFLOW.md`
   - Add the new issue to "Common Issues & Solutions" section
   - Follow the format: Problem → Solution → Prevention
   - Include code examples showing wrong vs. right approach
   - Number the issue sequentially (Issue 5, Issue 6, etc.)
   - This ensures future refactoring avoids the same mistakes

4. **Clean Up Backup Folders (BEFORE COMMIT)**
   - **MANDATORY:** Remove temporary backup folders before committing
   - Delete `ServiceName-New` folder (if exists)
   - Delete `ServiceName-Old` folder (if exists)
   - These are only for testing, should not be committed to repository
   - Command: `rm -rf src/Services/ServiceName-New src/Services/ServiceName-Old`

5. **Document Changes**
   - List files created/modified
   - Note any deviations from plan
   - Highlight important decisions made

---

## 🚫 Critical Rules (NEVER VIOLATE)

1. **API Contracts**
   - NEVER change endpoint URLs
   - NEVER change request/response JSON structure
   - NEVER change HTTP status codes

2. **Testing**
   - ALWAYS build after major changes
   - ALWAYS verify API contracts preserved
   - NEVER skip validation phase

3. **Git Workflow**
   - Work on `refactor/simplify-architecture` branch
   - Commit only when user explicitly requests
   - Follow commit message format from `GIT_WORKFLOW.md`

4. **Progress Tracking**
   - ALWAYS update `REFACTORING_PROGRESS.md` after completing tasks
   - ALWAYS document blockers encountered
   - NEVER leave progress file outdated

5. **Workflow Updates**
   - ALWAYS update `REFACTORING_WORKFLOW.md` if you encounter new issues
   - ALWAYS add lessons learned to "Common Issues & Solutions" section
   - NEVER skip workflow updates - this is how the system learns and improves

---

## 📝 Task Execution Pattern

```
FOR each task assigned by user:
  
  1. READ task description from REFACTORING_PROGRESS.md
  
  2. CHECK for blockers from previous attempts
  
  3. EXECUTE task:
     - Create/copy files as needed
     - Update namespaces
     - Remove unnecessary abstractions
     - Maintain API contracts
  
  4. VALIDATE:
     - Build service
     - Check for errors
     - Fix issues immediately
  
  5. CONTINUE to next task if build succeeds
  
AFTER all assigned tasks:
  
  6. UPDATE REFACTORING_PROGRESS.md:
     - Mark completed tasks [x]
     - Update progress percentage
     - Document any blockers
     - Update status
  
  7. UPDATE REFACTORING_WORKFLOW.md (IF NEW ISSUES FOUND):
     - Add any new issues to "Common Issues & Solutions" section
     - Follow the standard format with Problem/Solution/Prevention
     - Include code examples
     - This is MANDATORY, not optional
  
  8. CLEAN UP BACKUP FOLDERS (BEFORE COMMIT):
     - Remove ServiceName-New folder (if exists)
     - Remove ServiceName-Old folder (if exists)
     - Command: rm -rf src/Services/ServiceName-New src/Services/ServiceName-Old
  
  9. SUMMARIZE:
     - List completed tasks
     - List files created/modified
     - Note any issues or blockers
     - Mention if workflow file was updated
     - Suggest next steps
```

---

## 🎯 Decision Matrix

### When to Remove Interfaces?

| Scenario | Decision | Reason |
|----------|----------|--------|
| Simple repository (CRUD only) | Remove | No multiple implementations needed |
| Password hasher | Remove | Single implementation, no testing complexity |
| JWT token generator | Remove | Single implementation, straightforward |
| Saga orchestrator | Keep | Complex logic, needs testing with mocks |
| HTTP service clients | Keep | External dependencies, needs mocking |
| Simple business service | Remove | Direct dependencies sufficient |

### When to Keep Interfaces?

- Complex business logic requiring unit tests with mocks
- External service dependencies (HTTP clients)
- Multiple implementations exist or planned
- Testing requires isolation from real implementations

---

## 📊 Success Criteria

A task is complete when:

- [ ] Code builds without errors
- [ ] Namespaces updated correctly
- [ ] References resolved
- [ ] API contracts unchanged (if applicable)
- [ ] Progress file updated
- [ ] Blockers documented (if any)

A service refactoring is complete when:

- [ ] All tasks in progress file checked
- [ ] Service builds successfully
- [ ] Service runs in Docker
- [ ] All API endpoints work
- [ ] Frontend integration tested
- [ ] Progress file shows 100%

---

## 🔄 Handling Blockers

If you encounter a blocker:

1. **Stop immediately** - Don't proceed with broken code
2. **Document the blocker** in `REFACTORING_PROGRESS.md`
3. **Describe the issue** clearly
4. **Suggest resolution** if possible
5. **Mark task status** as blocked
6. **Inform user** in summary

Example blocker documentation:
```
- [⚠️] **1.5 Migrate Data Layer** - BLOCKED
  - Issue: Missing MySQL connection string in appsettings.json
  - Impact: Cannot test database context
  - Resolution: Need to copy connection string from old service
  - Next step: Copy appsettings.json first, then retry this task
```

---

## 📋 Quick Reference

### Files to Read Before Starting
1. `REFACTORING_ROADMAP.md` - Target structure and goals
2. `REFACTORING_PROGRESS.md` - Current status and tasks
3. `REFACTORING_RULES.md` - Constraints and standards
4. `GIT_WORKFLOW.md` - Git commands and safety

### Files to Update After Completion
1. `REFACTORING_PROGRESS.md` - Mark tasks complete, add blockers
2. `REFACTORING_WORKFLOW.md` - Add new issues/lessons learned (MANDATORY if encountered)

### Commands to Run
```bash
# Build service (use Docker if dotnet CLI not available)
docker compose build [service-name]

# Start dependencies first (MySQL, Consul)
docker compose up -d mysql consul
sleep 10

# Start and test service
docker compose up -d [service-name]
sleep 5
docker compose logs [service-name] --tail=50

# Check service health
docker compose ps [service-name]
```

---

## 🎯 Summary Template

After completing tasks, provide this summary:

```
## Refactoring Summary

### Completed Tasks
- [x] Task X.Y: Description
- [x] Task X.Z: Description

### Files Created/Modified
- Created: path/to/new/file.cs
- Modified: path/to/existing/file.cs

### Build Status
- ✅ Service builds successfully
- ✅ Zero compilation errors

### Blockers Encountered
- None / [Description of any blockers]

### Workflow Updates
- ✅ Updated REFACTORING_WORKFLOW.md with Issue N: [Description]
- OR No new issues encountered, workflow file unchanged

### Next Steps
- Continue with tasks X.N to X.M
- OR Service refactoring complete, ready for testing
```

---

## ⚠️ Common Issues & Solutions (Lessons Learned)

### **Issue 1: Namespace Conflicts**

**Problem:** When service name matches namespace (e.g., `UserService.Services.UserService`), C# compiler gets confused.

**Solution:**
```csharp
// ❌ BAD - Causes ambiguity
builder.Services.AddScoped<UserService.Services.UserService>();

// ✅ GOOD - Use fully qualified namespace
builder.Services.AddScoped<LibHub.UserService.Services.UserService>();
```

**Prevention:** Always use fully qualified namespaces in DI registration when there's potential naming conflict.

---

### **Issue 2: No dotnet CLI Available**

**Problem:** Server doesn't have `dotnet` command in PATH.

**Solution:** Use Docker for all build and test operations:
```bash
# Instead of: dotnet build
docker compose build [service-name]

# Check build success in logs
docker compose build [service-name] 2>&1 | grep -E "Build (succeeded|FAILED)"
```

**Prevention:** Always assume Docker-based workflow. Don't rely on local dotnet CLI.

---

### **Issue 3: Docker Compose Path References**

**Problem:** After renaming service folder, docker-compose.yml still references old paths.

**Solution:** Update all docker-compose files:
```yaml
# Update dockerfile path
dockerfile: src/Services/UserService/Dockerfile  # Not /LibHub.UserService.Api/Dockerfile
```

**Files to check:**
- `docker-compose.yml`
- `docker-compose.windows.yml`

**Prevention:** After renaming folders, search and update all docker-compose references.

---

### **Issue 4: Dockerfile Paths After Simplification**

**Problem:** Dockerfile still references multi-project structure.

**Solution:** Update COPY and WORKDIR paths:
```dockerfile
# ✅ GOOD - Single project
COPY ["src/Services/UserService/LibHub.UserService.csproj", "src/Services/UserService/"]
WORKDIR "/src/src/Services/UserService"
```

**Prevention:** Update Dockerfile immediately after creating single project structure.

---

### **Issue 5: Backup Folders Committed to Repository**

**Problem:** Temporary backup folders (ServiceName-New, ServiceName-Old) were committed to git repository.

**Solution:** Remove backup folders before committing:
```bash
# Remove backup folders
rm -rf src/Services/UserService-New
rm -rf src/Services/UserService-Old

# Then commit
git add -A
git commit -m "..."
```

**Prevention:** 
- Always clean up temporary folders before committing
- Add cleanup step to Phase 4: Documentation
- Backup folders are only for testing during refactoring
- Once testing is successful and service is replaced, delete them immediately

---

### **Issue 6: Ocelot QoS Requires Polly Package**

**Problem:** When adding QoS (Quality of Service) options to ocelot.json, Gateway fails to start with error: "Unable to start Ocelot because either a Route or GlobalConfiguration are using QoSOptions but no QosDelegatingHandlerDelegate has been registered".

**Solution:**
```xml
<PackageReference Include="Ocelot.Provider.Polly" Version="20.0.*" />
```

```csharp
using Ocelot.Provider.Polly;

builder.Services.AddOcelot(builder.Configuration)
    .AddConsul()
    .AddPolly();
```

**Prevention:**
- When adding QoS options (circuit breaker, timeout, etc.) to Ocelot routes, always add Polly package
- Polly provides the resilience and transient-fault-handling capabilities
- QoS options include: ExceptionsAllowedBeforeBreaking, DurationOfBreak, TimeoutValue
- Remember to add both the NuGet package AND the using directive

---

## 📝 Self-Improvement Instructions for AI Agents

**⚠️ CRITICAL - THIS IS MANDATORY, NOT OPTIONAL ⚠️**

After completing ANY refactoring task, you **MUST** update this workflow file if you encountered new issues.

### **When to Update This File**

Update `REFACTORING_WORKFLOW.md` immediately when you encounter:
- ❌ Compilation errors not documented here
- ❌ Namespace conflicts or ambiguities
- ❌ Docker build issues
- ❌ Configuration problems
- ❌ Path reference errors
- ❌ Any issue that took more than 1 attempt to fix

### **How to Update**

1. **Document New Issues**
   - If you encounter ANY issue not listed in "Common Issues & Solutions", add it
   - Follow the format: Problem → Solution → Prevention
   - Be specific about error messages and fixes
   - Add code examples showing wrong vs. right approach

2. **Update This Workflow File**
   - Add the issue to "Common Issues & Solutions" section above
   - Number it sequentially (next available number)
   - Include WHY the issue happened
   - Explain how to prevent it in future refactoring

3. **Update Naming Conventions**
   - If you discover better naming patterns, document them
   - Add to the "Naming Conventions" section

4. **Format for New Issues:**
   ```markdown
   ### **Issue N: [Brief Description]**
   
   **Problem:** [What went wrong]
   
   **Solution:**
   ```[language]
   // Show the fix
   ```
   
   **Prevention:** [How to avoid in future]
   ```

5. **Where to Add:**
   - Add new issues to "Common Issues & Solutions" section
   - Keep issues numbered sequentially
   - Most recent issues at the bottom

**Example Update:**
```markdown
### **Issue 5: Missing Using Statements**

**Problem:** After moving files, forgot to update using statements causing compilation errors.

**Solution:**
```csharp
// Add missing namespace
using LibHub.UserService.Models.Entities;
```

**Prevention:** After copying files, immediately check and update all using statements.
```

---

## 🎯 Naming Conventions (Universal for All Services)

### **Folder Structure**
```
ServiceName/
├── LibHub.ServiceName.csproj
├── Program.cs
├── Controllers/
├── Models/
│   ├── Entities/
│   ├── Requests/
│   └── Responses/
├── Services/
├── Data/
├── Security/        (if needed)
├── Clients/         (if needed - for HTTP clients)
└── Extensions/
```

### **File Naming**
- **Entities:** Singular noun (e.g., `User.cs`, `Book.cs`, `Loan.cs`)
- **Requests:** `{Action}Request.cs` (e.g., `RegisterRequest.cs`, `CreateBookRequest.cs`)
- **Responses:** `{Entity}Response.cs` (e.g., `UserResponse.cs`, `BookResponse.cs`)
- **Services:** `{Entity}Service.cs` (e.g., `UserService.cs`, `BookService.cs`)
- **Repositories:** `{Entity}Repository.cs` (e.g., `UserRepository.cs`, `BookRepository.cs`)
- **Controllers:** `{Entity}Controller.cs` (e.g., `UsersController.cs`, `BooksController.cs`)

### **Namespace Pattern**
```csharp
LibHub.{ServiceName}.{FolderName}

Examples:
- LibHub.UserService.Controllers
- LibHub.UserService.Models.Entities
- LibHub.UserService.Models.Requests
- LibHub.UserService.Services
- LibHub.CatalogService.Data
```

### **Class Naming**
- **Avoid naming conflicts:** Don't name service class same as namespace
  - ❌ BAD: `namespace UserService; class UserService {}`
  - ✅ GOOD: `namespace LibHub.UserService.Services; class UserService {}`

### **DI Registration**
```csharp
// Use fully qualified names to avoid ambiguity
builder.Services.AddScoped<LibHub.UserService.Services.UserService>();
builder.Services.AddScoped<UserRepository>();  // OK if no conflict
builder.Services.AddScoped<PasswordHasher>();
```

---

**Remember:** Quality over speed. Better to do fewer tasks correctly than rush and break things.

**Last Updated:** November 3, 2025

