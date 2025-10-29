USE user_db;
INSERT INTO Users (Username, Email, HashedPassword, Role, CreatedAt, UpdatedAt)
VALUES 
('admin', 'admin@libhub.com', '$2a$11$8Z7qZ8J5K9L2M3N4O5P6Q.uV3W4X5Y6Z7A8B9C0D1E2F3G4H5I6J7K', 'Admin', NOW(), NOW()),
('testuser', 'test@libhub.com', '$2a$11$9Z7qZ8J5K9L2M3N4O5P6Q.vW3X4Y5Z6A7B8C9D0E1F2G3H4I5J6K7L', 'Customer', NOW(), NOW())
ON DUPLICATE KEY UPDATE UpdatedAt=NOW();

USE catalog_db;
INSERT INTO Books (Isbn, Title, Author, Genre, Description, TotalCopies, AvailableCopies, CreatedAt, UpdatedAt)
VALUES 
('9780132350884', 'Clean Code', 'Robert C. Martin', 'Technology', 'A handbook of agile software craftsmanship.', 100, 100, NOW(), NOW()),
('9780596007126', 'Head First Design Patterns', 'Eric Freeman', 'Technology', 'A brain-friendly guide to design patterns.', 100, 100, NOW(), NOW()),
('9780743273565', 'The Great Gatsby', 'F. Scott Fitzgerald', 'Fiction', 'A story of the fabulously wealthy Jay Gatsby.', 100, 100, NOW(), NOW()),
('9780141439518', 'Pride and Prejudice', 'Jane Austen', 'Romance', 'A classic novel of manners and marriage.', 100, 100, NOW(), NOW()),
('9780451524935', '1984', 'George Orwell', 'Fiction', 'A dystopian social science fiction novel.', 100, 100, NOW(), NOW()),
('9780061120084', 'To Kill a Mockingbird', 'Harper Lee', 'Fiction', 'A gripping tale of racial injustice and childhood innocence.', 100, 100, NOW(), NOW()),
('9780316769174', 'The Catcher in the Rye', 'J.D. Salinger', 'Fiction', 'A story about teenage rebellion and alienation.', 100, 100, NOW(), NOW())
ON DUPLICATE KEY UPDATE UpdatedAt=NOW();
