# LibHub - Domain Reference

## Overview

LibHub uses **Domain-Driven Design (DDD)** principles with three bounded contexts, each mapping to a microservice. This document provides quick reference for all domain entities, business rules, and database schemas.

---

## Bounded Context 1: Identity & Access (UserService)

### Aggregate Root: User

**Purpose**: Represents a library user (patron or administrator)

#### Properties

```
public class User
{
    public int UserId { get; private set; }              // PK, auto-increment
    public string Username { get; private set; }          // Display name
    public string Email { get; private set; }             // Unique, login identifier
    public string HashedPassword { get; private set; }    // BCrypt hashed
    public string Role { get; private set; }              // "Customer" or "Admin"
    public DateTime CreatedAt { get; private set; }       // Registration timestamp
    public DateTime? UpdatedAt { get; private set; }      // Last modification
}
```

#### Business Rules

1. **Email Uniqueness**: Email must be unique across all users
2. **Email Format**: Must be valid email format (validated at Application layer)
3. **Password Complexity**:
   - Minimum 8 characters
   - At least one uppercase letter
   - At least one lowercase letter
   - At least one digit
   - At least one special character
4. **Password Hashing**: Always use BCrypt with work factor 11, never store plaintext
5. **Role Constraint**: Must be either "Customer" or "Admin" (case-sensitive)
6. **Username Required**: Cannot be null or whitespace
7. **Email Case**: Store email in lowercase for consistency

#### Key Entity Methods

```
// Constructor for new user registration
public User(string username, string email, string plainPassword, string role)
{
    // Validates username, email format, role
    // Hashes password using BCrypt
    Username = username;
    Email = email.ToLowerInvariant();
    HashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 11);
    Role = role;
    CreatedAt = DateTime.UtcNow;
}

// Verify password during login
public bool VerifyPassword(string plainPassword)
{
    return BCrypt.Net.BCrypt.Verify(plainPassword, HashedPassword);
}

// Update profile information
public void UpdateProfile(string username, string email)
{
    // Validates inputs
    Username = username;
    Email = email.ToLowerInvariant();
    UpdatedAt = DateTime.UtcNow;
}
```

#### Repository Interface

```
public interface IUserRepository
{
    Task<User> GetByIdAsync(int userId);
    Task<User> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}
```

#### Database Schema (user_db)

```
CREATE TABLE Users (
    UserId INT PRIMARY KEY AUTO_INCREMENT,
    Username VARCHAR(100) NOT NULL,
    Email VARCHAR(255) NOT NULL UNIQUE,
    HashedPassword VARCHAR(255) NOT NULL,
    Role VARCHAR(20) NOT NULL,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NULL,
    INDEX IX_Users_Email (Email)
);
```

#### Ubiquitous Language

- **User**: A person with an account in the system
- **Customer**: User with "Customer" role who can borrow books
- **Admin**: User with "Admin" role who manages catalog and loans
- **HashedPassword**: BCrypt-hashed password with embedded salt
- **Authentication**: Verifying user identity via email/password
- **Authorization**: Determining access based on role

---

## Bounded Context 2: Catalog (CatalogService)

### Aggregate Root: Book

**Purpose**: Represents a book title in the library's catalog with inventory tracking

#### Properties

```
public class Book
{
    public int BookId { get; private set; }              // PK, auto-increment
    public string Isbn { get; private set; }             // 13-digit unique identifier
    public string Title { get; private set; }            // Book title
    public string Author { get; private set; }           // Primary author
    public string Genre { get; private set; }            // Category
    public string Description { get; private set; }      // Optional summary
    public int TotalCopies { get; private set; }         // Total physical copies owned
    public int AvailableCopies { get; private set; }     // Currently available
    public DateTime CreatedAt { get; private set; }      // When added to catalog
    public DateTime? UpdatedAt { get; private set; }     // Last modification
}
```

#### Business Rules

1. **ISBN Format**: Must be exactly 13 digits (validated as string)
2. **ISBN Uniqueness**: Must be unique across all books
3. **Required Fields**: Title, Author, Genre are mandatory
4. **TotalCopies Constraint**: Must be >= 0
5. **AvailableCopies Constraint**: Must be >= 0 AND <= TotalCopies
6. **Stock Cannot Go Negative**: DecrementStock fails if AvailableCopies = 0
7. **Stock Cannot Exceed Total**: IncrementStock fails if AvailableCopies = TotalCopies

#### Invariants (MUST ALWAYS BE TRUE)

- `AvailableCopies <= TotalCopies`
- `AvailableCopies >= 0`
- `TotalCopies >= 0`

#### Key Entity Methods

```
// Constructor for adding new book
public Book(string isbn, string title, string author, string genre, 
            string description, int totalCopies)
{
    if (string.IsNullOrWhiteSpace(isbn) || isbn.Length != 13)
        throw new ArgumentException("ISBN must be exactly 13 digits");
    
    Isbn = isbn;
    Title = title;
    Author = author;
    Genre = genre;
    Description = description;
    TotalCopies = totalCopies;
    AvailableCopies = totalCopies; // All copies available initially
    CreatedAt = DateTime.UtcNow;
}

// Called when a book is borrowed (by LoanService via API)
public void DecrementStock()
{
    if (AvailableCopies <= 0)
        throw new InvalidOperationException("No copies available to borrow");
    
    AvailableCopies--;
    UpdatedAt = DateTime.UtcNow;
}

// Called when a book is returned (by LoanService via API)
public void IncrementStock()
{
    if (AvailableCopies >= TotalCopies)
        throw new InvalidOperationException("Cannot increment beyond total copies");
    
    AvailableCopies++;
    UpdatedAt = DateTime.UtcNow;
}

// Update catalog information (admin operation)
public void UpdateDetails(string title, string author, string genre, string description)
{
    Title = title;
    Author = author;
    Genre = genre;
    Description = description;
    UpdatedAt = DateTime.UtcNow;
}

// Adjust total inventory (admin operation)
public void AdjustTotalCopies(int newTotal)
{
    if (newTotal < 0)
        throw new ArgumentException("Total copies cannot be negative");
    
    int onLoan = TotalCopies - AvailableCopies;
    if (newTotal < onLoan)
        throw new InvalidOperationException(
            $"Cannot set total to {newTotal} when {onLoan} copies are on loan");
    
    int difference = newTotal - TotalCopies;
    TotalCopies = newTotal;
    AvailableCopies += difference;
    UpdatedAt = DateTime.UtcNow;
}

// Query methods
public bool IsAvailable() => AvailableCopies > 0;
public int CopiesOnLoan() => TotalCopies - AvailableCopies;
```

#### Repository Interface

```
public interface IBookRepository
{
    Task<Book> GetByIdAsync(int bookId);
    Task<Book> GetByIsbnAsync(string isbn);
    Task<List<Book>> SearchAsync(string searchTerm, string genre = null);
    Task<List<Book>> GetAllAsync();
    Task AddAsync(Book book);
    Task UpdateAsync(Book book);
    Task DeleteAsync(int bookId);
}
```

#### Database Schema (catalog_db)

```
CREATE TABLE Books (
    BookId INT PRIMARY KEY AUTO_INCREMENT,
    Isbn VARCHAR(13) NOT NULL UNIQUE,
    Title VARCHAR(255) NOT NULL,
    Author VARCHAR(255) NOT NULL,
    Genre VARCHAR(100) NOT NULL,
    Description TEXT NULL,
    TotalCopies INT NOT NULL DEFAULT 0,
    AvailableCopies INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL,
    UpdatedAt DATETIME NULL,
    INDEX IX_Books_Isbn (Isbn),
    INDEX IX_Books_Title (Title),
    INDEX IX_Books_Author (Author),
    INDEX IX_Books_Genre (Genre)
);
```

#### Ubiquitous Language

- **Book**: A title in the catalog (not a physical copy)
- **ISBN**: International Standard Book Number (13 digits)
- **Copy**: Physical instance of a book (represented by TotalCopies count)
- **Available**: Copy currently on shelf, can be borrowed
- **On Loan**: Copy currently borrowed (TotalCopies - AvailableCopies)
- **Stock**: Inventory of physical copies
- **Catalog**: Complete collection of book titles

---

## Bounded Context 3: Loan (LoanService)

### Aggregate Root: Loan

**Purpose**: Represents a book borrowing transaction

#### Properties

```
public class Loan
{
    public int LoanId { get; private set; }              // PK, auto-increment
    public int UserId { get; private set; }              // FK reference (ID only)
    public int BookId { get; private set; }              // FK reference (ID only)
    public DateTime CheckoutDate { get; private set; }   // When borrowed
    public DateTime DueDate { get; private set; }        // When due (14 days)
    public DateTime? ReturnDate { get; private set; }    // When returned (null if active)
    public string Status { get; private set; }           // Workflow state
    
    // Calculated property
    public bool IsOverdue => Status == "CheckedOut" && DateTime.UtcNow > DueDate;
}
```

#### Status State Machine

```
PENDING → CheckedOut → Returned
   ↓
 FAILED
```

**Valid Status Values**:
- **"PENDING"**: Loan created, waiting for CatalogService confirmation (Saga step 2)
- **"CheckedOut"**: Book successfully borrowed, currently on loan (Saga success)
- **"Returned"**: Book has been returned to library (UC-C2)
- **"FAILED"**: Borrow operation failed, compensating transaction executed (Saga failure)

#### Business Rules

1. **Loan Period**: Exactly 14 days from checkout date
2. **Borrowing Limit**: Customer can have maximum 5 active loans simultaneously
3. **Status Transitions**: Must follow state machine (no arbitrary status changes)
4. **ReturnDate Constraint**: Must be null for PENDING/CheckedOut loans
5. **ReturnDate Required**: Must be set when status changes to "Returned"
6. **Cannot Return Twice**: Cannot return a loan that's already returned
7. **Foreign Key Pattern**: UserId and BookId are references only (no navigation properties)
8. **Overdue Definition**: CheckedOut status AND current time > DueDate

#### Key Entity Methods

```
// Constructor - creates PENDING loan (Saga step 2)
public Loan(int userId, int bookId)
{
    if (userId <= 0) throw new ArgumentException("Invalid user ID");
    if (bookId <= 0) throw new ArgumentException("Invalid book ID");
    
    UserId = userId;
    BookId = bookId;
    CheckoutDate = DateTime.UtcNow;
    DueDate = CheckoutDate.AddDays(14);  // Fixed 14-day loan period
    Status = "PENDING";
    ReturnDate = null;
}

// Saga step 5a - mark as successfully checked out
public void MarkAsCheckedOut()
{
    if (Status != "PENDING")
        throw new InvalidOperationException("Can only mark PENDING loans as checked out");
    
    Status = "CheckedOut";
}

// Saga step 5b - mark as failed (compensating transaction)
public void MarkAsFailed()
{
    if (Status != "PENDING")
        throw new InvalidOperationException("Can only mark PENDING loans as failed");
    
    Status = "FAILED";
}

// Return book operation (UC-C2)
public void MarkAsReturned()
{
    if (Status != "CheckedOut")
        throw new InvalidOperationException("Can only return loans that are checked out");
    
    Status = "Returned";
    ReturnDate = DateTime.UtcNow;
}

// Query methods
public int DaysUntilDue()
{
    if (Status != "CheckedOut") return 0;
    return (DueDate - DateTime.UtcNow).Days;
}

public int DaysOverdue()
{
    if (!IsOverdue) return 0;
    return (DateTime.UtcNow - DueDate).Days;
}

public bool IsActive() => Status == "CheckedOut";
```

#### Repository Interface

```
public interface ILoanRepository
{
    Task<Loan> GetByIdAsync(int loanId);
    Task<List<Loan>> GetActiveLoansForUserAsync(int userId);
    Task<List<Loan>> GetAllLoansForUserAsync(int userId);
    Task<List<Loan>> GetAllActiveLoansAsync();
    Task<List<Loan>> GetOverdueLoansAsync();
    Task<int> CountActiveLoansForUserAsync(int userId);
    Task AddAsync(Loan loan);
    Task UpdateAsync(Loan loan);
}
```

#### Domain Service Interface (External Dependency)

```
public interface ICatalogServiceClient
{
    Task<BookDto> GetBookAsync(int bookId);
    Task<bool> CheckAvailabilityAsync(int bookId);
    Task DecrementStockAsync(int bookId);
    Task IncrementStockAsync(int bookId);
}
```

#### Database Schema (loan_db)

```
CREATE TABLE Loans (
    LoanId INT PRIMARY KEY AUTO_INCREMENT,
    UserId INT NOT NULL,
    BookId INT NOT NULL,
    CheckoutDate DATETIME NOT NULL,
    DueDate DATETIME NOT NULL,
    ReturnDate DATETIME NULL,
    Status VARCHAR(20) NOT NULL,
    INDEX IX_Loans_UserId (UserId),
    INDEX IX_Loans_BookId (BookId),
    INDEX IX_Loans_Status (Status),
    INDEX IX_Loans_DueDate (DueDate)
);
```

**IMPORTANT**: No foreign key constraints to Users or Books tables (different databases).

#### Ubiquitous Language

- **Loan**: Record of a book being borrowed by a customer
- **Active Loan**: Loan with status "CheckedOut"
- **Checkout**: Act of borrowing a book
- **Return**: Act of bringing a borrowed book back
- **Due Date**: Date by which book must be returned (14 days from checkout)
- **Overdue**: Loan past due date that hasn't been returned
- **Saga**: Distributed transaction orchestration for borrowing
- **Compensating Transaction**: Rolling back PENDING loan to FAILED if catalog operation fails

---

## Cross-Context Relationships

### Important: No Database Foreign Keys

In microservices architecture, contexts reference each other **by ID only**, not by entity navigation properties or database foreign keys.

```
Loan Context                    Catalog Context
+---------+                     +---------+
|  Loan   |  --BookId (int)->   |  Book   |
|         |  (API call)         |         |
+---------+                     +---------+
    |
    | UserId (int)
    | (API call)
    v
+---------+
|  User   |   Identity & Access Context
+---------+
```

### Data Consistency Models

**Within a Context**: 
- ACID transactions via Entity Framework Core
- Example: Creating a Book in catalog_db is atomic

**Across Contexts**:
- Eventual consistency via Saga pattern
- Example: Borrow operation coordinates loan_db and catalog_db via HTTP calls

### Example: Borrow Book Saga Workflow

```
1. User clicks "Borrow" (Frontend)
2. POST /api/loans → API Gateway → LoanService
3. LoanService (Orchestrator):
   a. Create PENDING Loan in loan_db
   b. HTTP GET /api/books/{id} to CatalogService (check availability)
   c. HTTP PUT /api/books/{id}/stock to CatalogService (decrement)
   d. If success: Update Loan to "CheckedOut" in loan_db
   e. If failure: Update Loan to "FAILED" in loan_db (compensating transaction)
4. Return success/failure to Frontend
```

---

## Common DTOs (Data Transfer Objects)

DTOs are used for API communication and Application Layer operations.

### User Context DTOs

```
public class RegisterUserDto
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}

public class LoginDto
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class UserDto
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
}

public class TokenDto
{
    public string AccessToken { get; set; }
    public int ExpiresIn { get; set; }
}
```

### Catalog Context DTOs

```
public class CreateBookDto
{
    public string Isbn { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string Genre { get; set; }
    public string Description { get; set; }
    public int TotalCopies { get; set; }
}

public class BookDto
{
    public int BookId { get; set; }
    public string Isbn { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string Genre { get; set; }
    public string Description { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public bool IsAvailable { get; set; }
}

public class UpdateStockDto
{
    public int ChangeAmount { get; set; }  // -1 for borrow, +1 for return
}
```

### Loan Context DTOs

```
public class CreateLoanDto
{
    public int BookId { get; set; }
    // UserId comes from JWT claims
}

public class LoanDto
{
    public int LoanId { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public string Status { get; set; }
    public DateTime CheckoutDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysUntilDue { get; set; }
}
```

---

## Validation Rules Summary

### Email Validation
- Regex: `^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$`
- Must be unique in Users table

### Password Validation
- Minimum 8 characters
- At least one uppercase: `[A-Z]`
- At least one lowercase: `[a-z]`
- At least one digit: `[0-9]`
- At least one special character: `[!@#$%^&*(),.?":{}|<>]`

### ISBN Validation
- Exactly 13 digits
- Numeric only
- Must be unique in Books table

### Status Validation (Loan)
- Only allowed values: "PENDING", "CheckedOut", "Returned", "FAILED"
- Transitions must follow state machine

---

## Quick Reference: Entity Operations

| Entity | Create | Read | Update | Delete | Special Operations |
|--------|--------|------|--------|--------|--------------------|
| **User** | Register | GetById, GetByEmail | UpdateProfile | ❌ No | VerifyPassword, GenerateJWT |
| **Book** | AddBook (Admin) | GetById, Search, GetAll | UpdateDetails, AdjustTotal | Delete (Admin) | DecrementStock, IncrementStock, IsAvailable |
| **Loan** | BorrowBook | GetById, GetByUser, GetAll | MarkReturned | ❌ No | MarkAsCheckedOut, MarkAsFailed, IsOverdue |
