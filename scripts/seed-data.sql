USE user_db;
INSERT INTO Users (Username, Email, HashedPassword, Role, CreatedAt, UpdatedAt)
VALUES 
('admin', 'admin@libhub.com', '$2a$11$5vz6V5W4Mz5B0jL0j5Qvqe5L8a8b5N5B5V5W4Mz5B0jL0j5Qvqe', 'Admin', NOW(), NOW()),
('testuser', 'test@libhub.com', '$2a$11$5vz6V5W4Mz5B0jL0j5Qvqe5L8a8b5N5B5V5W4Mz5B0jL0j5Qvqe', 'Customer', NOW(), NOW())
ON DUPLICATE KEY UPDATE UpdatedAt=NOW();

USE catalog_db;
INSERT INTO Books (Isbn, Title, Author, Genre, Description, TotalCopies, AvailableCopies, CreatedAt, UpdatedAt)
VALUES 
('9780132350884', 'Clean Code', 'Robert C. Martin', 'Technology', 'A handbook of agile software craftsmanship.', 5, 5, NOW(), NOW()),
('9780596007126', 'Head First Design Patterns', 'Eric Freeman', 'Technology', 'A brain-friendly guide to design patterns.', 3, 3, NOW(), NOW()),
('9780743273565', 'The Great Gatsby', 'F. Scott Fitzgerald', 'Fiction', 'A story of the fabulously wealthy Jay Gatsby.', 4, 4, NOW(), NOW()),
('9780141439518', 'Pride and Prejudice', 'Jane Austen', 'Romance', 'A classic novel of manners and marriage.', 6, 6, NOW(), NOW()),
('9780451524935', '1984', 'George Orwell', 'Fiction', 'A dystopian social science fiction novel.', 7, 7, NOW(), NOW()),
('9780061120084', 'To Kill a Mockingbird', 'Harper Lee', 'Fiction', 'A gripping tale of racial injustice and childhood innocence.', 5, 5, NOW(), NOW()),
('9780316769174', 'The Catcher in the Rye', 'J.D. Salinger', 'Fiction', 'A story about teenage rebellion and alienation.', 4, 4, NOW(), NOW())
ON DUPLICATE KEY UPDATE UpdatedAt=NOW();
