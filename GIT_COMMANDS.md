# Git Commands for Consul & Seed Data Implementation

## Save Changes Locally

```bash
# Check current status
git status

# Add all changes
git add .

# Commit with descriptive message
git commit -m "feat: add Consul service discovery and book seed data

Phase 9 Implementation:
- Implement Consul service discovery pattern
- Add dynamic service registration for all microservices
- Configure Ocelot Gateway with Consul provider
- Add health check endpoints (/health) to all services
- Implement RoundRobin load balancing
- Add book seed data (15 technical books)
- Create BookSeeder for automatic data initialization
- Add test-consul-discovery.sh script
- Update PROJECT_STATUS.md with Phase 9 completion
- Update DOCKER_QUICK_START.md with comprehensive commands
- Add detailed script usage documentation

Features:
- Consul UI at http://localhost:8500
- Dynamic service discovery and health monitoring
- Automatic service registration/deregistration
- Load balancing support for horizontal scaling
- 15 books automatically seeded on startup
- Idempotent seeding (only if database empty)

Files Added:
- src/Services/*/Extensions/ConsulServiceRegistration.cs
- src/Services/CatalogService/Infrastructure/Data/BookSeeder.cs
- scripts/test-consul-discovery.sh
- ai-docs/completed/task-9.1-*.md
- ai-docs/completed/task-9.2-book-seed-data.md

Files Modified:
- docker-compose.yml (added Consul container)
- src/Gateway/LibHub.Gateway.Api/ocelot.json
- src/Gateway/LibHub.Gateway.Api/Program.cs
- All service Program.cs files
- All service .csproj files
- ai-docs/PROJECT_STATUS.md
- DOCKER_QUICK_START.md"
```

## Push to Remote Repository

```bash
# Push to feat/containerization branch
git push origin feat/containerization
```

## Alternative: Shorter Commit Message

If you prefer a shorter commit message:

```bash
git add .

git commit -m "feat: implement Consul service discovery and book seed data

- Add Consul container for service discovery
- Implement dynamic service registration
- Add health checks to all microservices
- Configure Ocelot with Consul provider
- Add book seed data (15 books)
- Update documentation and test scripts"

git push origin feat/containerization
```

## Verify Before Pushing

```bash
# View what will be committed
git status

# View changes in staged files
git diff --staged

# View commit history
git log --oneline -5

# View specific file changes
git diff HEAD docker-compose.yml
```

## If You Need to Amend

```bash
# Amend last commit (if you forgot something)
git add [forgotten-file]
git commit --amend --no-edit

# Amend with new message
git commit --amend -m "new message"

# Force push if already pushed (⚠️ use carefully)
git push origin feat/containerization --force
```

## Create Pull Request (After Push)

After pushing, you can create a Pull Request on GitHub:

1. Go to: https://github.com/phuocthuan2203/libhub-ver2
2. Click "Pull requests" → "New pull request"
3. Select: base: `main` ← compare: `feat/containerization`
4. Add title: "Phase 9: Consul Service Discovery & Book Seed Data"
5. Add description with key changes
6. Click "Create pull request"

## Branch Management

```bash
# View all branches
git branch -a

# Switch to main branch
git checkout main

# Pull latest changes
git pull origin main

# Switch back to feature branch
git checkout feat/containerization

# Merge main into feature branch (if needed)
git merge main
```
