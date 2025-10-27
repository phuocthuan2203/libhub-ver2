# End-to-End Test Scenarios

---

## Scenario 1: Customer Happy Path

**Objective**: Verify complete customer journey works end-to-end

**Steps**:
1. Open frontend → Register new user (username: "e2etest", email: "e2e@test.com")
2. Login with credentials
3. Browse books → Search "Fiction"
4. Click book → View details
5. Click "Borrow Book"
6. Navigate to "My Loans" → Verify loan appears with status "CheckedOut"
7. Click "Return Book"
8. Verify loan status changed to "Returned"

**Expected Results**:
- ✅ Registration creates user in user_db
- ✅ Login returns JWT token
- ✅ Search returns filtered books
- ✅ Borrow creates loan in loan_db AND decrements stock in catalog_db (Saga!)
- ✅ Return updates loan status AND increments stock

**Verification**:
-- Check loan created
SELECT * FROM loan_<userId>;

-- Check stock decremented
SELECT AvailableCopies FROM cat<bookId>;

text

**Priority**: CRITICAL

---

## Scenario 2: Admin Workflow

**Objective**: Verify admin CRUD operations

**Steps**:
1. Login as admin (create admin user first if needed)
2. Navigate to "Admin Dashboard"
3. Click "Add Book" → Fill form → Submit
4. Verify book appears in catalog
5. Click "Edit" on book → Change title → Save
6. Verify changes saved
7. Attempt to delete book (should fail if has loans)
8. Delete book with no loans (should succeed)

**Expected Results**:
- ✅ Only Admin role can access admin pages
- ✅ Customer users redirected to home
- ✅ CRUD operations work correctly
- ✅ Cannot delete book with active loans

**Priority**: HIGH

---

## Scenario 3: Saga - Book Unavailable

**Objective**: Test Saga compensating transaction

**Setup**: Borrow book until AvailableCopies = 0

**Steps**:
1. User A attempts to borrow book with 0 copies
2. Check loan status in database

**Expected Results**:
- ✅ Loan status = "FAILED" (not "CheckedOut")
- ✅ Stock remains 0 (not decremented)
- ✅ User sees error message "Book not available"

**Verification**:
SELECT Status FROM loan_db.Loans WHERE LoanId=<loanId>;
-- Should be "FAILE

text

**Priority**: CRITICAL (validates Saga pattern)

---

## Scenario 4: Max Loans Limit

**Objective**: Verify 5 loan limit enforced

**Steps**:
1. User borrows 5 books
2. Attempt to borrow 6th book

**Expected Results**:
- ✅ Error message "Maximum 5 active loans reached"
- ✅ No 6th loan created in database

**Priority**: MEDIUM

---

## Scenario 5: Overdue Loans

**Objective**: Verify overdue detection

**Setup**:
UPDATE loan_db.Loans SET DueDate='2025-10-01' WHERE LoanId=<id>;

text

**Steps**:
1. Navigate to "My Loans"
2. Observe overdue loan

**Expected Results**:
- ✅ Overdue loan highlighted in red
- ✅ "OVERDUE!" message displayed
- ✅ Days overdue calculated correctly

**Priority**: LOW

---

## Test Data

**Create test users**:
INSERT INTO user_db.Users (Username, Email, HashedPassword, Role, CreatedAt)
VALUES
customer@test.com', '<bcrypt>', 'Customer', NOW()),
('testaadmin@test.com', '<bcrypt>', 'Admin', NOW());

text

**Create test books**:
INSERT INTO catalog_db.Books (Isbn, Title, Author, Genre, TotalCopies, AvailableCopies, CreatedAt)
VALUES
('9781111111111', 'Test Fiction', 'Author A', 'Fiction', 5, 5, NOW())
text

---

## Bug Template

| Bug ID | Scenario | Description | Steps to Reproduce | Expected | Actual | Severity | Status |
|--------|----------|-------------|-------------------|----------|--------|----------|--------|
| E2E-001 | Saga | Stock not decremented | 1. Borrow book... | Stock -1 | Stock same | Critical | Open |