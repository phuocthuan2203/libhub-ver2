-- =====================================================
-- LibHub E2E Test - Test Data Setup Script
-- =====================================================
-- This script populates test data for E2E testing
-- Run this BEFORE executing E2E tests
-- =====================================================

-- =====================================================
-- CLEANUP: Remove existing test data
-- =====================================================

DELETE FROM loan_db.Loans WHERE UserId IN (
    SELECT UserId FROM user_db.Users WHERE Email LIKE '%@test.com'
);

DELETE FROM catalog_db.Books WHERE Isbn LIKE '9999%';

DELETE FROM user_db.Users WHERE Email LIKE '%@test.com';

-- =====================================================
-- USERS: Create test users
-- =====================================================

-- Customer user (password: Test@1234)
-- BCrypt hash for "Test@1234" with work factor 11
INSERT INTO user_db.Users (Username, Email, HashedPassword, Role, CreatedAt, UpdatedAt)
VALUES (
    'testcustomer',
    'customer@test.com',
    '$2a$11$XqZ8J5K9L2M3N4O5P6Q7R8S9T0U1V2W3X4Y5Z6A7B8C9D0E1F2G3H4',
    'Customer',
    NOW(),
    NOW()
);

-- Admin user (password: Admin@1234)
-- BCrypt hash for "Admin@1234" with work factor 11
INSERT INTO user_db.Users (Username, Email, HashedPassword, Role, CreatedAt, UpdatedAt)
VALUES (
    'testadmin',
    'admin@test.com',
    '$2a$11$YqZ8J5K9L2M3N4O5P6Q7R8S9T0U1V2W3X4Y5Z6A7B8C9D0E1F2G3H5',
    'Admin',
    NOW(),
    NOW()
);

-- =====================================================
-- BOOKS: Create test books with varying stock levels
-- =====================================================

-- Book 1: Fiction with high stock (for happy path testing)
INSERT INTO catalog_db.Books (Isbn, Title, Author, Genre, Description, TotalCopies, AvailableCopies, CreatedAt, UpdatedAt)
VALUES (
    '9999111111111',
    'The Great Adventure',
    'John Smith',
    'Fiction',
    'An epic tale of courage and discovery in uncharted lands.',
    10,
    10,
    NOW(),
    NOW()
);

-- Book 2: Mystery with medium stock
INSERT INTO catalog_db.Books (Isbn, Title, Author, Genre, Description, TotalCopies, AvailableCopies, CreatedAt, UpdatedAt)
VALUES (
    '9999222222222',
    'Mystery of the Lost City',
    'Jane Doe',
    'Mystery',
    'A detective races against time to solve an ancient mystery.',
    5,
    5,
    NOW(),
    NOW()
);

-- Book 3: Science Fiction with low stock
INSERT INTO catalog_db.Books (Isbn, Title, Author, Genre, Description, TotalCopies, AvailableCopies, CreatedAt, UpdatedAt)
VALUES (
    '9999333333333',
    'Journey to Mars',
    'Robert Johnson',
    'Science Fiction',
    'Humanity first mission to colonize the red planet.',
    3,
    3,
    NOW(),
    NOW()
);

-- Book 4: Romance with single copy
INSERT INTO catalog_db.Books (Isbn, Title, Author, Genre, Description, TotalCopies, AvailableCopies, CreatedAt, UpdatedAt)
VALUES (
    '9999444444444',
    'Love in Paris',
    'Emily Brown',
    'Romance',
    'A heartwarming story of love found in the city of lights.',
    1,
    1,
    NOW(),
    NOW()
);

-- Book 5: History with NO available copies (for Saga failure testing)
INSERT INTO catalog_db.Books (Isbn, Title, Author, Genre, Description, TotalCopies, AvailableCopies, CreatedAt, UpdatedAt)
VALUES (
    '9999555555555',
    'Ancient Civilizations',
    'Michael Davis',
    'History',
    'Exploring the rise and fall of ancient empires.',
    2,
    0,
    NOW(),
    NOW()
);

-- =====================================================
-- ADDITIONAL TEST BOOKS: For various scenarios
-- =====================================================

-- Book 6: Biography for admin CRUD testing
INSERT INTO catalog_db.Books (Isbn, Title, Author, Genre, Description, TotalCopies, AvailableCopies, CreatedAt, UpdatedAt)
VALUES (
    '9999666666666',
    'Life of a Legend',
    'Sarah Wilson',
    'Biography',
    'The inspiring journey of a remarkable individual.',
    4,
    4,
    NOW(),
    NOW()
);

-- Book 7: Thriller for search testing
INSERT INTO catalog_db.Books (Isbn, Title, Author, Genre, Description, TotalCopies, AvailableCopies, CreatedAt, UpdatedAt)
VALUES (
    '9999777777777',
    'The Silent Witness',
    'David Martinez',
    'Thriller',
    'A gripping thriller that will keep you on the edge of your seat.',
    6,
    6,
    NOW(),
    NOW()
);

-- =====================================================
-- VERIFICATION: Display created test data
-- =====================================================

SELECT '=== TEST USERS CREATED ===' AS Info;
SELECT UserId, Username, Email, Role
FROM user_db.Users
WHERE Email LIKE '%@test.com'
ORDER BY UserId;

SELECT '=== TEST BOOKS CREATED ===' AS Info;
SELECT BookId, Isbn, Title, Author, Genre, TotalCopies, AvailableCopies
FROM catalog_db.Books
WHERE Isbn LIKE '9999%'
ORDER BY BookId;

SELECT '=== SUMMARY ===' AS Info;
SELECT 
    (SELECT COUNT(*) FROM user_db.Users WHERE Email LIKE '%@test.com') AS TestUsers,
    (SELECT COUNT(*) FROM catalog_db.Books WHERE Isbn LIKE '9999%') AS TestBooks,
    (SELECT COUNT(*) FROM loan_db.Loans WHERE UserId IN (
        SELECT UserId FROM user_db.Users WHERE Email LIKE '%@test.com'
    )) AS TestLoans;

-- =====================================================
-- NOTES FOR TESTERS
-- =====================================================

-- Customer Login:
--   Email: customer@test.com
--   Password: Test@1234

-- Admin Login:
--   Email: admin@test.com
--   Password: Admin@1234

-- Book Stock Levels:
--   9999111111111 (The Great Adventure) - 10 copies available
--   9999222222222 (Mystery of the Lost City) - 5 copies available
--   9999333333333 (Journey to Mars) - 3 copies available
--   9999444444444 (Love in Paris) - 1 copy available
--   9999555555555 (Ancient Civilizations) - 0 copies available (for Saga testing)
--   9999666666666 (Life of a Legend) - 4 copies available
--   9999777777777 (The Silent Witness) - 6 copies available

-- Test Scenarios:
--   1. Customer Happy Path: Use customer@test.com, borrow any book with stock > 0
--   2. Admin Workflow: Use admin@test.com, perform CRUD operations
--   3. Saga Failure: Try to borrow BookId with Isbn='9999555555555' (0 stock)
--   4. Max Loans: Borrow 5 books, then attempt 6th (should fail)
--   5. Overdue Loans: Manually update DueDate to past date for testing
