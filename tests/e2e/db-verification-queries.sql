-- =====================================================
-- LibHub E2E Test - Database Verification Queries
-- =====================================================

-- =====================================================
-- SCENARIO 1: Customer Happy Path
-- =====================================================

-- 1.1: Verify user registration
SELECT UserId, Username, Email, Role, CreatedAt
FROM user_db.Users
WHERE Email = 'e2e@test.com';

-- 1.2: Verify loan created with CheckedOut status
SELECT LoanId, UserId, BookId, Status, CheckoutDate, DueDate, ReturnDate
FROM loan_db.Loans
WHERE UserId = <userId> AND BookId = <bookId>
ORDER BY LoanId DESC
LIMIT 1;

-- 1.3: Verify stock decremented in catalog_db
SELECT BookId, Title, TotalCopies, AvailableCopies
FROM catalog_db.Books
WHERE BookId = <bookId>;

-- 1.4: Verify loan returned (status changed to Returned)
SELECT LoanId, Status, ReturnDate
FROM loan_db.Loans
WHERE LoanId = <loanId>;

-- 1.5: Verify stock incremented after return
SELECT BookId, Title, AvailableCopies
FROM catalog_db.Books
WHERE BookId = <bookId>;

-- 1.6: Count active loans for user
SELECT COUNT(*) AS ActiveLoans
FROM loan_db.Loans
WHERE UserId = <userId> AND Status = 'CheckedOut';


-- =====================================================
-- SCENARIO 2: Admin Workflow
-- =====================================================

-- 2.1: Verify admin user exists
SELECT UserId, Username, Email, Role
FROM user_db.Users
WHERE Role = 'Admin' AND Email = 'admin@test.com';

-- 2.2: Verify book added by admin
SELECT BookId, Isbn, Title, Author, Genre, TotalCopies, AvailableCopies, CreatedAt
FROM catalog_db.Books
WHERE Isbn = '<newIsbn>'
ORDER BY BookId DESC
LIMIT 1;

-- 2.3: Verify book updated
SELECT BookId, Title, Author, UpdatedAt
FROM catalog_db.Books
WHERE BookId = <bookId>;

-- 2.4: Check if book has active loans (before delete attempt)
SELECT COUNT(*) AS ActiveLoans
FROM loan_db.Loans
WHERE BookId = <bookId> AND Status = 'CheckedOut';

-- 2.5: Verify book deleted (should return 0 rows if successful)
SELECT BookId, Title
FROM catalog_db.Books
WHERE BookId = <bookId>;

-- 2.6: View all loans (admin privilege)
SELECT l.LoanId, l.UserId, u.Username, l.BookId, b.Title, l.Status, l.CheckoutDate, l.DueDate
FROM loan_db.Loans l
JOIN user_db.Users u ON l.UserId = u.UserId
JOIN catalog_db.Books b ON l.BookId = b.BookId
ORDER BY l.LoanId DESC
LIMIT 20;


-- =====================================================
-- SCENARIO 3: Saga - Book Unavailable
-- =====================================================

-- 3.1: Check book stock before borrow attempt
SELECT BookId, Title, AvailableCopies
FROM catalog_db.Books
WHERE BookId = <bookId>;

-- 3.2: Verify loan status is FAILED (compensating transaction)
SELECT LoanId, UserId, BookId, Status, CheckoutDate
FROM loan_db.Loans
WHERE UserId = <userId> AND BookId = <bookId>
ORDER BY LoanId DESC
LIMIT 1;

-- 3.3: Verify stock NOT decremented (should remain 0)
SELECT BookId, AvailableCopies
FROM catalog_db.Books
WHERE BookId = <bookId>;

-- 3.4: Count FAILED loans for debugging
SELECT COUNT(*) AS FailedLoans
FROM loan_db.Loans
WHERE Status = 'FAILED';


-- =====================================================
-- SCENARIO 4: Max Loans Limit
-- =====================================================

-- 4.1: Count active loans for user
SELECT COUNT(*) AS ActiveLoans
FROM loan_db.Loans
WHERE UserId = <userId> AND Status = 'CheckedOut';

-- 4.2: List all active loans for user
SELECT LoanId, BookId, CheckoutDate, DueDate
FROM loan_db.Loans
WHERE UserId = <userId> AND Status = 'CheckedOut'
ORDER BY CheckoutDate DESC;

-- 4.3: Verify 6th loan NOT created
SELECT COUNT(*) AS TotalLoans
FROM loan_db.Loans
WHERE UserId = <userId>;

-- 4.4: Check if max limit error was logged (optional)
SELECT LoanId, Status
FROM loan_db.Loans
WHERE UserId = <userId>
ORDER BY LoanId DESC
LIMIT 1;


-- =====================================================
-- SCENARIO 5: Overdue Loans
-- =====================================================

-- 5.1: Find overdue loans
SELECT LoanId, UserId, BookId, CheckoutDate, DueDate, 
       DATEDIFF(CURDATE(), DueDate) AS DaysOverdue
FROM loan_db.Loans
WHERE Status = 'CheckedOut' AND DueDate < CURDATE()
ORDER BY DueDate ASC;

-- 5.2: Set loan to overdue (for testing)
UPDATE loan_db.Loans
SET DueDate = '2025-10-01'
WHERE LoanId = <loanId>;

-- 5.3: Verify overdue loan details
SELECT LoanId, UserId, BookId, DueDate, 
       DATEDIFF(CURDATE(), DueDate) AS DaysOverdue,
       Status
FROM loan_db.Loans
WHERE LoanId = <loanId>;

-- 5.4: Count all overdue loans in system
SELECT COUNT(*) AS OverdueLoans
FROM loan_db.Loans
WHERE Status = 'CheckedOut' AND DueDate < CURDATE();

-- 5.5: List all overdue loans with user and book details
SELECT l.LoanId, u.Username, u.Email, b.Title, l.DueDate,
       DATEDIFF(CURDATE(), l.DueDate) AS DaysOverdue
FROM loan_db.Loans l
JOIN user_db.Users u ON l.UserId = u.UserId
JOIN catalog_db.Books b ON l.BookId = b.BookId
WHERE l.Status = 'CheckedOut' AND l.DueDate < CURDATE()
ORDER BY l.DueDate ASC;


-- =====================================================
-- GENERAL VERIFICATION QUERIES
-- =====================================================

-- Check database health
SELECT 'user_db' AS Database, COUNT(*) AS UserCount FROM user_db.Users
UNION ALL
SELECT 'catalog_db', COUNT(*) FROM catalog_db.Books
UNION ALL
SELECT 'loan_db', COUNT(*) FROM loan_db.Loans;

-- Check loan status distribution
SELECT Status, COUNT(*) AS Count
FROM loan_db.Loans
GROUP BY Status;

-- Check stock levels
SELECT BookId, Title, TotalCopies, AvailableCopies,
       (TotalCopies - AvailableCopies) AS BorrowedCopies
FROM catalog_db.Books
ORDER BY AvailableCopies ASC;

-- Find books with no available copies
SELECT BookId, Title, TotalCopies, AvailableCopies
FROM catalog_db.Books
WHERE AvailableCopies = 0;

-- Check for data inconsistencies (stock should never be negative)
SELECT BookId, Title, AvailableCopies
FROM catalog_db.Books
WHERE AvailableCopies < 0;

-- Verify Saga integrity: Count active loans vs borrowed copies
SELECT 
    (SELECT COUNT(*) FROM loan_db.Loans WHERE Status = 'CheckedOut') AS ActiveLoans,
    (SELECT SUM(TotalCopies - AvailableCopies) FROM catalog_db.Books) AS BorrowedCopies;
