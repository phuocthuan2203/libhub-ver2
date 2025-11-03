# Git Workflow for Production Refactoring

> **Critical:** This guide is for working directly on a production server  
> **Current Branch:** `refactor/simplify-architecture`  
> **Production Branch:** `main`  
> **Remote:** `origin` (GitHub: phuocthuan2203/libhub-ver2)

---

## 🚨 Safety First: Production Server Rules

### **NEVER Work Directly on Main Branch**

```bash
# ❌ DANGEROUS: Working on main
git checkout main
# Make changes...
# If something breaks, production is down!

# ✅ SAFE: Always use feature branch
git checkout -b feature/my-changes
# Make changes...
# Test thoroughly before merging
```

### **Current Setup (Already Done)**

```bash
# Check current status
$ git branch
  main
* refactor/simplify-architecture

# We are safely on feature branch!
```

---

## 📋 Complete Git Workflow

### **Step 1: Daily Workflow (Making Changes)**

```bash
# 1. Start working (you're already here)
git status
# On branch refactor/simplify-architecture

# 2. Make your changes
# Edit files, create new files, etc.

# 3. Check what changed
git status
git diff

# 4. Stage specific files
git add src/Services/UserService/
git add docs/refactoring/

# Or stage everything (be careful!)
git add .

# 5. Commit with descriptive message
git commit -m "refactor(UserService): Simplify project structure

- Merged 4 projects into 1
- Removed unnecessary interfaces
- Updated folder structure
- Maintained API compatibility"

# 6. Push to GitHub (feature branch)
git push origin refactor/simplify-architecture
```

### **Step 2: Regular Backups to GitHub**

**Push frequently to avoid losing work:**

```bash
# After completing a logical unit of work, push
git push origin refactor/simplify-architecture

# If it's your first push on this branch
git push -u origin refactor/simplify-architecture

# Subsequent pushes
git push
```

### **Step 3: Viewing Your Changes**

```bash
# See what files changed
git status

# See detailed changes
git diff

# See commit history
git log --oneline

# See changes for specific file
git diff src/Services/UserService/Program.cs

# See all branches
git branch -a
```

### **Step 4: Testing Before Merge**

**Before merging to main, thoroughly test:**

```bash
# 1. Build all services
docker compose build

# 2. Start all services
docker compose up -d

# 3. Wait for startup
sleep 60

# 4. Run tests
./scripts/test-docker-containers.sh
./scripts/test-consul-discovery.sh
./scripts/test-gateway-integration.sh

# 5. Manual testing
# Open http://localhost:8080
# Test registration, login, borrow, return

# 6. Check logs for errors
docker compose logs | grep -i error

# 7. If everything works, proceed to merge
# If not, fix issues and test again
```

---

## 🔀 Merging to Production (Main Branch)

### **Option A: Direct Merge (Simple)**

**Use when:** You're confident and have tested thoroughly

```bash
# 1. Ensure you're on feature branch with latest changes
git checkout refactor/simplify-architecture
git status
# Should be clean (no uncommitted changes)

# 2. Switch to main branch
git checkout main

# 3. Pull latest changes from GitHub (in case team made changes)
git pull origin main

# 4. Merge feature branch into main
git merge refactor/simplify-architecture

# 5. If merge conflicts occur, resolve them
# Git will tell you which files have conflicts
# Edit the files, remove conflict markers
git add <resolved-files>
git commit -m "merge: Resolve conflicts from refactoring"

# 6. Test on main branch (IMPORTANT!)
docker compose down
docker compose build
docker compose up -d
# Wait and test thoroughly

# 7. If everything works, push to GitHub
git push origin main

# 8. Your production is now updated!
```

### **Option B: Squash Merge (Clean History)**

**Use when:** You have many small commits and want cleaner history

```bash
# 1. Switch to main
git checkout main
git pull origin main

# 2. Merge with squash (combines all commits into one)
git merge --squash refactor/simplify-architecture

# 3. Create single commit
git commit -m "refactor: Simplify microservices architecture

Major changes:
- UserService: Merged 4 projects into 1
- CatalogService: Merged 4 projects into 1
- LoanService: Merged 4 projects into 1
- Removed unnecessary interfaces
- Simplified dependency injection
- Maintained 100% API compatibility

BREAKING CHANGES: None
Frontend compatibility: 100%"

# 4. Test thoroughly
docker compose down
docker compose build
docker compose up -d

# 5. Push to production
git push origin main
```

### **Option C: Pull Request on GitHub (Safest)**

**Use when:** You want code review or extra safety

```bash
# 1. Push feature branch to GitHub
git push origin refactor/simplify-architecture

# 2. Go to GitHub
# https://github.com/phuocthuan2203/libhub-ver2

# 3. Click "Compare & pull request" button

# 4. Fill in PR details:
# Title: "Refactor: Simplify microservices architecture"
# Description: Describe changes, testing done, etc.

# 5. Create Pull Request

# 6. Review changes on GitHub web interface

# 7. If satisfied, click "Merge pull request"

# 8. On server, pull updated main
git checkout main
git pull origin main

# 9. Rebuild and deploy
docker compose down
docker compose build
docker compose up -d
```

---

## 🔙 Rolling Back Changes

### **If Something Goes Wrong After Merge**

#### **Quick Rollback (Revert Last Commit)**

```bash
# 1. This creates a new commit that undoes the merge
git revert HEAD

# 2. Push the revert
git push origin main

# 3. Redeploy
docker compose down
docker compose build
docker compose up -d

# Feature branch is still safe, fix issues there
```

#### **Hard Rollback (Go Back to Previous State)**

```bash
# 1. Find the commit before your merge
git log --oneline
# Example output:
# abc123 (HEAD -> main) refactor: Simplify architecture
# def456 Previous working version
# ...

# 2. Reset to previous commit (⚠️ DESTRUCTIVE)
git reset --hard def456

# 3. Force push (⚠️ USE WITH CAUTION)
# This rewrites history on GitHub
git push origin main --force

# 4. Redeploy
docker compose down
docker compose build
docker compose up -d
```

**⚠️ Warning:** `git reset --hard` and `--force` delete history. Use only as last resort!

---

## 📦 Creating Backup Points

### **Tagging Important Versions**

```bash
# Before major changes, tag the working version
git tag -a v1.0-before-refactor -m "Stable version before refactoring"
git push origin v1.0-before-refactor

# After successful refactoring
git tag -a v1.1-after-refactor -m "Refactored architecture"
git push origin v1.1-after-refactor

# To rollback to a tag
git checkout v1.0-before-refactor
git checkout -b rollback-branch
# Test, then merge to main if needed
```

---

## 🔄 Syncing with GitHub

### **Daily Sync Pattern**

```bash
# Morning: Get latest changes
git checkout refactor/simplify-architecture
git pull origin refactor/simplify-architecture

# Make your changes throughout the day
# ... work work work ...

# Afternoon: Push your changes
git add .
git commit -m "progress: Completed UserService migration"
git push origin refactor/simplify-architecture

# Evening: Final push
git push origin refactor/simplify-architecture
```

### **Handling Conflicts**

```bash
# If push fails with conflict
$ git push
! [rejected]        refactor/simplify-architecture -> refactor/simplify-architecture (non-fast-forward)

# Solution: Pull first, resolve conflicts, then push
git pull origin refactor/simplify-architecture

# If conflicts occur during pull
Auto-merging src/Services/UserService/Program.cs
CONFLICT (content): Merge conflict in src/Services/UserService/Program.cs

# Edit the file, look for:
<<<<<<< HEAD
your changes
=======
remote changes
>>>>>>> branch-name

# Choose what to keep, remove markers, then:
git add src/Services/UserService/Program.cs
git commit -m "merge: Resolve conflicts"
git push origin refactor/simplify-architecture
```

---

## 🛡️ Production Safety Checklist

### **Before Every Push to Main:**

```bash
# 1. All changes committed
git status
# Should show "nothing to commit, working tree clean"

# 2. All services build
docker compose build

# 3. All services start
docker compose up -d

# 4. All tests pass
./scripts/test-gateway-integration.sh

# 5. Frontend works
# Open browser, test manually

# 6. No errors in logs
docker compose logs | grep -i error | grep -v "Error 0"

# 7. Services register with Consul
curl http://localhost:8500/v1/catalog/services

# ✅ Only if all above pass, merge to main
```

---

## 📊 Git Status Quick Reference

### **Check Your Status**

```bash
# What branch am I on?
git branch
# * shows current branch

# What files changed?
git status

# What changed in files?
git diff

# What commits were made?
git log --oneline -10

# Where am I compared to GitHub?
git status -sb
# Shows ahead/behind remote
```

### **Undo Changes (Before Commit)**

```bash
# Undo changes to specific file
git checkout -- filename.cs

# Undo all unstaged changes
git checkout -- .

# Unstage a file (but keep changes)
git reset HEAD filename.cs

# Discard ALL local changes (⚠️ CAREFUL)
git reset --hard HEAD
```

### **Undo Commits**

```bash
# Undo last commit but keep changes
git reset --soft HEAD~1

# Undo last commit and discard changes
git reset --hard HEAD~1

# Undo last 3 commits but keep changes
git reset --soft HEAD~3
```

---

## 🎯 Common Scenarios

### **Scenario 1: Working on Refactoring Daily**

```bash
# Start of day
git status
git pull origin refactor/simplify-architecture

# Make changes
# ... edit files ...

# Save progress
git add .
git commit -m "progress: Migrated UserService models"
git push origin refactor/simplify-architecture

# Continue...
```

### **Scenario 2: Need to Switch to Fix Production Bug**

```bash
# You're on feature branch, but production has a bug!

# 1. Save current work
git stash save "WIP: UserService refactoring"

# 2. Switch to main
git checkout main

# 3. Create hotfix branch
git checkout -b hotfix/critical-bug

# 4. Fix the bug
# ... edit files ...

# 5. Test and commit
git add .
git commit -m "fix: Critical bug in login"

# 6. Merge to main
git checkout main
git merge hotfix/critical-bug
git push origin main

# 7. Deploy
docker compose down
docker compose build
docker compose up -d

# 8. Return to refactoring
git checkout refactor/simplify-architecture
git stash pop

# 9. Continue working
```

### **Scenario 3: Want to Test Main While Keeping Feature Branch**

```bash
# Currently on feature branch
git checkout refactor/simplify-architecture

# Switch to main to test
git checkout main

# Test production version
docker compose up -d

# Switch back to continue refactoring
git checkout refactor/simplify-architecture
```

---

## 🔐 Keeping GitHub Token/Auth

### **If Using HTTPS (Username/Token)**

```bash
# Store credentials
git config --global credential.helper store

# Next push/pull will ask for credentials once
git push
# Username: phuocthuan2203
# Password: <your-github-token>

# Future operations won't ask
```

### **If Using SSH Keys**

```bash
# Check if SSH key exists
ls -la ~/.ssh/

# If not, generate one
ssh-keygen -t ed25519 -C "your-email@example.com"

# Copy public key
cat ~/.ssh/id_ed25519.pub

# Add to GitHub: Settings → SSH Keys → New SSH Key

# Change remote to SSH
git remote set-url origin git@github.com:phuocthuan2203/libhub-ver2.git
```

---

## 📝 Commit Message Best Practices

### **Format**

```
<type>(<scope>): <subject>

<body>

<footer>
```

### **Types**
- `feat`: New feature
- `fix`: Bug fix
- `refactor`: Code restructuring
- `docs`: Documentation
- `test`: Adding tests
- `chore`: Maintenance

### **Examples**

```bash
# Good commit messages
git commit -m "refactor(UserService): Merge 4 projects into 1"
git commit -m "fix(Gateway): Correct JWT validation logic"
git commit -m "docs: Update refactoring roadmap"

# Detailed commit
git commit -m "refactor(UserService): Simplify architecture

- Merged Domain, Application, Infrastructure, Api projects
- Removed unnecessary interfaces
- Simplified dependency injection
- Maintained 100% API compatibility

BREAKING CHANGES: None"
```

---

## 🎓 Summary: Your Safe Workflow

```bash
# Daily routine
1. git pull origin refactor/simplify-architecture   # Get latest
2. # ... make changes ...                           # Work
3. git add .                                         # Stage
4. git commit -m "descriptive message"               # Commit
5. git push origin refactor/simplify-architecture    # Backup to GitHub

# When ready to deploy
1. # Test thoroughly on feature branch
2. git checkout main                                 # Switch to main
3. git pull origin main                              # Get latest main
4. git merge refactor/simplify-architecture          # Merge
5. # Test on main branch                             # Test again!
6. git push origin main                              # Deploy
7. docker compose down && docker compose up -d       # Restart services

# If something breaks
1. git revert HEAD                                   # Undo
2. git push origin main                              # Deploy rollback
3. docker compose down && docker compose up -d       # Restart
```

---

## 🆘 Emergency Commands

```bash
# Show me everything!
git status
git log --oneline -5
git branch -a

# I want to undo everything and start over!
git reset --hard HEAD
git clean -fd

# I pushed something wrong to main!
git revert HEAD
git push origin main

# Help, I'm lost!
git reflog
# Shows all actions, find the good one
git reset --hard <good-commit>
```

---

**Remember:** 
- ✅ Always work on feature branch
- ✅ Test before merging to main
- ✅ Push frequently to GitHub
- ✅ Production changes only after thorough testing
- ✅ Keep calm, Git has your back

**Last Updated:** November 3, 2025  
**For Questions:** Check Git documentation or ask for help
