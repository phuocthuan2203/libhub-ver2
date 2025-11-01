# Architecture Layer 5: Complete Data Flow Examples

## What You'll Learn

This document walks through complete end-to-end scenarios showing how all architectural layers work together. You'll see:
- Complete code traces from frontend to database
- How data transforms at each layer
- Real examples of Clean Architecture in action
- The Saga pattern in a full workflow

## Scenario 1: User Registration

### Overview

User creates a new account, gets auto-logged in, and can start browsing books.

### Complete Flow Diagram

```mermaid
sequenceDiagram
    participant Browser
    participant HTML as register.html
    participant JS as JavaScript
    participant Nginx
    participant Gateway
    participant UserService
    participant AppService as IdentityApplicationService
    participant Domain as User Entity
    participant Hasher as PasswordHasher
    participant Repo as EfUserRepository
    participant DB as MySQL (user_db)
    
    Browser->>HTML: Fill form<br/>{username, email, password}
    Browser->>HTML: Click "Register"
    HTML->>JS: validatePassword(password)
    JS-->>HTML: Valid ✓
    
    HTML->>JS: apiClient.post('/api/users/register', data)
    JS->>Nginx: POST /api/users/register<br/>{username, email, password}
    Nginx->>Gateway: Forward to http://gateway:5000
    
    Gateway->>Gateway: No auth required (public endpoint)
    Gateway->>UserService: POST /api/users/register
    
    Note over UserService: Presentation Layer
    UserService->>AppService: RegisterUserAsync(dto)
    
    Note over AppService: Application Layer
    AppService->>AppService: PasswordValidator.Validate(password)
    AppService->>Repo: EmailExistsAsync(email)
    Repo->>DB: SELECT COUNT(*) WHERE email=?
    DB-->>Repo: 0 (not exists)
    Repo-->>AppService: false
    
    AppService->>Hasher: Hash(password)
    Hasher-->>AppService: $2a$11$hashed...
    
    Note over Domain: Domain Layer
    AppService->>Domain: new User(username, email, hashedPassword, "Customer")
    Domain->>Domain: ValidateUsername()
    Domain->>Domain: ValidateEmail()
    Domain->>Domain: ValidateRole()
    Domain-->>AppService: User instance
    
    Note over Repo: Infrastructure Layer
    AppService->>Repo: AddAsync(user)
    Repo->>DB: INSERT INTO Users VALUES (...)
    DB-->>Repo: Success (UserId=1)
    Repo-->>AppService: void
    
    AppService->>AppService: MapToDto(user)
    AppService-->>UserService: UserDto
    
    UserService-->>Gateway: 201 Created<br/>{userId, username, email, role}
    Gateway-->>Nginx: 201 Created
    Nginx-->>JS: 201 Created
    JS->>JS: Show success message
    JS-->>Browser: Redirect to login.html
```

### Code Trace

#### Step 1: Frontend Form Submission

**File:** `frontend/register.html`

```html
<form id="registerForm">
    <input type="text" id="username" required>
    <input type="email" id="email" required>
    <input type="password" id="password" required>
    <button type="submit">Register</button>
</form>

<script>
document.getElementById('registerForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const username = document.getElementById('username').value;
    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;
    
    // Validate password complexity
    if (!validatePassword(password)) {
        alert(getPasswordValidationMessage(password));
        return;
    }
    
    try {
        // Call API
        const response = await apiClient.post('/api/users/register', {
            username: username,
            email: email,
            password: password
        });
        
        alert('Registration successful! Please login.');
        window.location.href = 'login.html';
    } catch (error) {
        alert(`Registration failed: ${error.message}`);
    }
});
</script>
```

**Data at this point:**
```javascript
{
    username: "john_doe",
    email: "john@example.com",
    password: "Test@1234"  // Plain text
}
```

#### Step 2: API Client (JavaScript)

**File:** `frontend/js/api-client.js`

```javascript
async post(endpoint, data, requiresAuth = false) {
    const headers = { 'Content-Type': 'application/json' };
    
    // No auth required for registration
    
    const response = await fetch(`${this.baseUrl}${endpoint}`, {
        method: 'POST',
        headers,
        body: JSON.stringify(data)  // Convert to JSON string
    });
    
    if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
    }
    
    return await response.json();
}
```

**HTTP Request:**
```http
POST /api/users/register HTTP/1.1
Host: localhost:8080
Content-Type: application/json

{
    "username": "john_doe",
    "email": "john@example.com",
    "password": "Test@1234"
}
```

#### Step 3: Nginx Proxy

**File:** `frontend/nginx.conf`

```nginx
location /api/ {
    proxy_pass http://gateway:5000;
    proxy_set_header Host $host;
    proxy_set_header X-Real-IP $remote_addr;
}
```

**Action:** Forwards request to Gateway at `http://gateway:5000/api/users/register`

#### Step 4: Gateway Routing

**File:** `src/Gateway/LibHub.Gateway.Api/ocelot.json`

```json
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/api/users/{everything}",
      "DownstreamScheme": "http",
      "UpstreamPathTemplate": "/api/users/{everything}",
      "UpstreamHttpMethod": [ "Post" ],
      "ServiceName": "userservice"
    }
  ]
}
```

**Action:** 
1. Match route: `/api/users/register`
2. Query Consul: "Where is userservice?"
3. Forward to: `http://userservice:5002/api/users/register`

#### Step 5: Controller (Presentation Layer)

**File:** `src/Services/UserService/LibHub.UserService.Api/Controllers/UsersController.cs`

```csharp
[HttpPost("register")]
public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
{
    try
    {
        // Call application service
        var user = await _identityService.RegisterUserAsync(dto);
        
        // Log success
        _logger.LogInformation("User registered: {Email}", user.Email);
        
        // Return 201 Created
        return CreatedAtAction(nameof(GetUserById), 
            new { id = user.UserId }, user);
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}
```

**Data transformation:**
- **Input:** JSON string → ASP.NET model binding → `RegisterUserDto` object
- **Output:** `UserDto` object → JSON serialization → HTTP response

**RegisterUserDto at this point:**
```csharp
{
    Username = "john_doe",
    Email = "john@example.com",
    Password = "Test@1234"
}
```

#### Step 6: Application Service (Application Layer)

**File:** `src/Services/UserService/LibHub.UserService.Application/Services/IdentityApplicationService.cs`

```csharp
public async Task<UserDto> RegisterUserAsync(RegisterUserDto dto)
{
    // Step 1: Validate password complexity
    var (isValid, errorMessage) = PasswordValidator.Validate(dto.Password);
    if (!isValid)
        throw new ArgumentException(errorMessage);
    
    // Step 2: Check email uniqueness
    if (await _userRepository.EmailExistsAsync(dto.Email))
        throw new InvalidOperationException("Email already exists");
    
    // Step 3: Hash password
    var hashedPassword = _passwordHasher.Hash(dto.Password);
    // hashedPassword = "$2a$11$N9qo8uLOickgx2ZMRZoMye..."
    
    // Step 4: Create domain entity
    var user = new User(dto.Username, dto.Email, hashedPassword, "Customer");
    
    // Step 5: Persist to database
    await _userRepository.AddAsync(user);
    
    // Step 6: Map to DTO
    return MapToDto(user);
}
```

**Data transformation:**
- `dto.Password` (plain text) → `hashedPassword` (BCrypt hash)
- `RegisterUserDto` → `User` entity → `UserDto`

#### Step 7: Domain Entity (Domain Layer)

**File:** `src/Services/UserService/LibHub.UserService.Domain/User.cs`

```csharp
public User(string username, string email, string hashedPassword, string role)
{
    // Validate business rules
    ValidateUsername(username);      // 2-100 chars
    ValidateEmail(email);            // Must contain @ and .
    ValidateRole(role);              // Must be "Customer" or "Admin"
    ValidateHashedPassword(hashedPassword);  // Min 50 chars

    Username = username;
    Email = email.ToLowerInvariant();  // Normalize to lowercase
    HashedPassword = hashedPassword;
    Role = role;
    CreatedAt = DateTime.UtcNow;
}
```

**Data transformation:**
- `email` → `email.ToLowerInvariant()` (normalization)
- Validation ensures data integrity

**User entity at this point:**
```csharp
{
    UserId = 0,  // Not set yet (auto-increment)
    Username = "john_doe",
    Email = "john@example.com",  // Lowercase
    HashedPassword = "$2a$11$N9qo8uLOickgx2ZMRZoMye...",
    Role = "Customer",
    CreatedAt = DateTime.UtcNow
}
```

#### Step 8: Repository (Infrastructure Layer)

**File:** `src/Services/UserService/LibHub.UserService.Infrastructure/Repositories/EfUserRepository.cs`

```csharp
public async Task AddAsync(User user)
{
    await _context.Users.AddAsync(user);
    await _context.SaveChangesAsync();  // Execute SQL
}
```

**Generated SQL:**
```sql
INSERT INTO Users (Username, Email, HashedPassword, Role, CreatedAt, UpdatedAt)
VALUES ('john_doe', 'john@example.com', '$2a$11$N9qo8uLOickgx2ZMRZoMye...', 
        'Customer', '2025-11-01 10:30:00', NULL);

-- MySQL returns auto-increment ID
-- UserId = 1
```

**Database state after:**
```
Users table:
+--------+-----------+-------------------+---------------------+----------+---------------------+
| UserId | Username  | Email             | HashedPassword      | Role     | CreatedAt           |
+--------+-----------+-------------------+---------------------+----------+---------------------+
| 1      | john_doe  | john@example.com  | $2a$11$N9qo8uLO... | Customer | 2025-11-01 10:30:00 |
+--------+-----------+-------------------+---------------------+----------+---------------------+
```

#### Step 9: Response Journey Back

**Application Service → Controller:**
```csharp
UserDto {
    UserId = 1,
    Username = "john_doe",
    Email = "john@example.com",
    Role = "Customer",
    CreatedAt = DateTime.UtcNow
    // Note: HashedPassword NOT included (security)
}
```

**Controller → Gateway → Frontend:**
```http
HTTP/1.1 201 Created
Content-Type: application/json
Location: /api/users/1

{
    "userId": 1,
    "username": "john_doe",
    "email": "john@example.com",
    "role": "Customer",
    "createdAt": "2025-11-01T10:30:00Z"
}
```

**Frontend JavaScript:**
```javascript
// Success! Redirect to login
alert('Registration successful! Please login.');
window.location.href = 'login.html';
```

## Scenario 2: Borrow Book (Saga Pattern)

### Overview

Authenticated user borrows a book, triggering a distributed transaction across LoanService and CatalogService.

### Complete Flow Diagram

```mermaid
sequenceDiagram
    participant Browser
    participant Frontend
    participant Gateway
    participant LoanService
    participant LoanDB
    participant CatalogService
    participant CatalogDB
    
    Browser->>Frontend: Click "Borrow" on book detail page
    Frontend->>Frontend: Get JWT from localStorage
    Frontend->>Gateway: POST /api/loans<br/>Authorization: Bearer {JWT}<br/>{bookId: 5}
    
    Gateway->>Gateway: Validate JWT signature
    Gateway->>Gateway: Extract userId from claims
    Gateway->>LoanService: POST /api/loans<br/>Authorization: Bearer {JWT}
    
    Note over LoanService: SAGA STEP 1: Check loan limit
    LoanService->>LoanService: Extract userId from JWT claims
    LoanService->>LoanDB: SELECT COUNT(*) FROM Loans<br/>WHERE UserId=3 AND Status='CheckedOut'
    LoanDB-->>LoanService: 2 (< 5, OK)
    
    Note over LoanService: SAGA STEP 2: Create PENDING loan
    LoanService->>LoanService: new Loan(userId=3, bookId=5)
    LoanService->>LoanDB: INSERT INTO Loans<br/>(UserId, BookId, Status, CheckoutDate, DueDate)<br/>VALUES (3, 5, 'PENDING', NOW(), NOW()+14)
    LoanDB-->>LoanService: LoanId=101
    
    Note over LoanService: SAGA STEP 3: Check availability
    LoanService->>CatalogService: GET /api/books/5
    CatalogService->>CatalogDB: SELECT * FROM Books WHERE BookId=5
    CatalogDB-->>CatalogService: {BookId:5, AvailableCopies:3, ...}
    CatalogService-->>LoanService: BookDto {IsAvailable:true}
    
    Note over LoanService: SAGA STEP 4: Decrement stock
    LoanService->>CatalogService: PUT /api/books/5/stock<br/>{ChangeAmount: -1}
    CatalogService->>CatalogDB: UPDATE Books<br/>SET AvailableCopies=AvailableCopies-1<br/>WHERE BookId=5
    CatalogDB-->>CatalogService: Success (AvailableCopies now 2)
    CatalogService-->>LoanService: 200 OK
    
    Note over LoanService: SAGA STEP 5: Mark as CheckedOut
    LoanService->>LoanService: loan.MarkAsCheckedOut()
    LoanService->>LoanDB: UPDATE Loans<br/>SET Status='CheckedOut'<br/>WHERE LoanId=101
    LoanDB-->>LoanService: Success
    
    LoanService-->>Gateway: 201 Created<br/>LoanDto
    Gateway-->>Frontend: 201 Created
    Frontend->>Frontend: Update UI
    Frontend-->>Browser: Show "Book borrowed successfully!"
```

### Code Trace with Data Transformation

#### Step 1: Frontend Click Handler

**File:** `frontend/book-detail.html`

```javascript
async function borrowBook() {
    if (!isLoggedIn()) {
        alert('Please login to borrow books');
        window.location.href = 'login.html';
        return;
    }
    
    const bookId = new URLSearchParams(window.location.search).get('id');
    
    try {
        // API call with JWT authentication
        const loan = await apiClient.post('/api/loans', 
            { bookId: parseInt(bookId) }, 
            true  // requiresAuth = true
        );
        
        alert('Book borrowed successfully! Due date: ' + 
              new Date(loan.dueDate).toLocaleDateString());
        window.location.href = 'my-loans.html';
    } catch (error) {
        alert(`Failed to borrow book: ${error.message}`);
    }
}
```

**HTTP Request:**
```http
POST /api/loans HTTP/1.1
Host: localhost:8080
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
    "bookId": 5
}
```

**JWT Token Decoded:**
```json
{
  "sub": "3",
  "email": "john@example.com",
  "unique_name": "john_doe",
  "role": "Customer",
  "exp": 1698850800
}
```

#### Step 2: Gateway JWT Validation

**File:** `src/Gateway/LibHub.Gateway.Api/Program.cs`

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,  // Check expiration
            ValidateIssuerSigningKey = true,  // Verify signature
            ValidIssuer = "LibHub.UserService",
            ValidAudience = "LibHub.Client",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey)
            )
        };
    });
```

**Action:**
1. Extract token from `Authorization: Bearer {token}` header
2. Verify signature using secret key
3. Check expiration (exp claim)
4. Populate `HttpContext.User` with claims

#### Step 3: Loans Controller

**File:** `src/Services/LoanService/LibHub.LoanService.Api/Controllers/LoansController.cs`

```csharp
[HttpPost]
[Authorize]  // Requires JWT
public async Task<IActionResult> BorrowBook([FromBody] CreateLoanDto dto)
{
    try
    {
        // Extract userId from JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var userId = int.Parse(userIdClaim.Value);  // userId = 3
        
        // Call application service (Saga orchestrator)
        var loan = await _loanService.BorrowBookAsync(userId, dto);
        
        return CreatedAtAction(nameof(GetLoanById), 
            new { id = loan.LoanId }, loan);
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}
```

**Data at this point:**
```csharp
userId = 3  // From JWT claims
dto = { BookId = 5 }
```

#### Step 4: Saga Orchestration (Application Layer)

**File:** `src/Services/LoanService/LibHub.LoanService.Application/Services/LoanApplicationService.cs`

```csharp
public async Task<LoanDto> BorrowBookAsync(int userId, CreateLoanDto dto)
{
    _logger.LogInformation("Starting Saga: UserId={UserId}, BookId={BookId}", 
        userId, dto.BookId);

    // SAGA STEP 1: Check loan limit
    var activeLoansCount = await _loanRepository.CountActiveLoansForUserAsync(userId);
    if (activeLoansCount >= 5)
    {
        _logger.LogWarning("Saga aborted: Max loan limit reached");
        throw new InvalidOperationException("Maximum loan limit reached");
    }
    // activeLoansCount = 2 (OK)

    // SAGA STEP 2: Create PENDING loan
    var loan = new Loan(userId, dto.BookId);
    await _loanRepository.AddAsync(loan);
    _logger.LogInformation("Saga Step 2: Created PENDING loan {LoanId}", loan.LoanId);
    // loan.LoanId = 101, loan.Status = "PENDING"

    try
    {
        // SAGA STEP 3: Verify availability
        _logger.LogInformation("Saga Step 3: Verifying book availability");
        var book = await _catalogService.GetBookAsync(dto.BookId);
        // book = { BookId: 5, AvailableCopies: 3, IsAvailable: true }
        
        if (!book.IsAvailable)
        {
            _logger.LogWarning("Saga Step 3 failed: Book not available");
            throw new InvalidOperationException("Book is not available");
        }

        // SAGA STEP 4: Decrement stock
        _logger.LogInformation("Saga Step 4: Decrementing stock");
        await _catalogService.DecrementStockAsync(dto.BookId);
        _logger.LogInformation("Saga Step 4: Stock decremented successfully");
    }
    catch (Exception ex)
    {
        // COMPENSATING TRANSACTION
        _logger.LogError(ex, "Saga failed, executing compensating transaction");
        
        loan.MarkAsFailed();  // Status = "FAILED"
        await _loanRepository.UpdateAsync(loan);
        _logger.LogInformation("Compensating transaction: Marked loan as FAILED");
        
        throw new InvalidOperationException($"Failed to borrow book: {ex.Message}");
    }

    // SAGA STEP 5: Mark as CheckedOut
    loan.MarkAsCheckedOut();  // Status = "CheckedOut"
    await _loanRepository.UpdateAsync(loan);
    _logger.LogInformation("Saga Step 5: Loan marked as CheckedOut - Saga completed");

    return MapToDto(loan);
}
```

#### Step 5: HTTP Call to CatalogService

**File:** `src/Services/LoanService/LibHub.LoanService.Infrastructure/HttpClients/CatalogServiceHttpClient.cs`

```csharp
public async Task DecrementStockAsync(int bookId)
{
    SetAuthorizationHeader();  // Forward JWT token
    
    var stockDto = new { ChangeAmount = -1 };
    
    var response = await _httpClient.PutAsJsonAsync(
        $"/api/books/{bookId}/stock", 
        stockDto
    );

    if (!response.IsSuccessStatusCode)
    {
        throw new Exception($"Failed to decrement stock: {response.StatusCode}");
    }
}
```

**HTTP Request to CatalogService:**
```http
PUT /api/books/5/stock HTTP/1.1
Host: catalogservice:5001
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
    "changeAmount": -1
}
```

#### Step 6: CatalogService Updates Stock

**File:** `src/Services/CatalogService/LibHub.CatalogService.Api/Controllers/BooksController.cs`

```csharp
[HttpPut("{id}/stock")]
[Authorize]
public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockDto dto)
{
    var book = await _bookService.GetBookByIdAsync(id);
    if (book == null)
        return NotFound();

    await _bookService.UpdateStockAsync(id, dto.ChangeAmount);
    return NoContent();  // 204 No Content
}
```

**File:** `src/Services/CatalogService/LibHub.CatalogService.Application/Services/BookApplicationService.cs`

```csharp
public async Task UpdateStockAsync(int bookId, int changeAmount)
{
    var book = await _bookRepository.GetByIdAsync(bookId);
    if (book == null)
        throw new InvalidOperationException("Book not found");

    book.UpdateStock(changeAmount);  // Domain method
    await _bookRepository.UpdateAsync(book);
}
```

**File:** `src/Services/CatalogService/LibHub.CatalogService.Domain/Book.cs`

```csharp
public void UpdateStock(int changeAmount)
{
    var newStock = AvailableCopies + changeAmount;
    
    if (newStock < 0)
        throw new InvalidOperationException("Insufficient stock");
    
    if (newStock > TotalCopies)
        throw new InvalidOperationException("Cannot exceed total copies");
    
    AvailableCopies = newStock;
    UpdatedAt = DateTime.UtcNow;
}
```

**Generated SQL:**
```sql
UPDATE Books
SET AvailableCopies = AvailableCopies - 1,
    UpdatedAt = '2025-11-01 10:35:00'
WHERE BookId = 5;

-- Before: AvailableCopies = 3
-- After:  AvailableCopies = 2
```

**Database state after:**
```
Books table:
+--------+-------+--------------+-----------------+
| BookId | Title | TotalCopies  | AvailableCopies |
+--------+-------+--------------+-----------------+
| 5      | ...   | 5            | 2               |
+--------+-------+--------------+-----------------+

Loans table:
+--------+--------+--------+------------+------------+------------+
| LoanId | UserId | BookId | Status     | CheckoutDt | DueDate    |
+--------+--------+--------+------------+------------+------------+
| 101    | 3      | 5      | CheckedOut | 2025-11-01 | 2025-11-15 |
+--------+--------+--------+------------+------------+------------+
```

#### Step 7: Response Journey Back

**LoanService → Gateway → Frontend:**
```http
HTTP/1.1 201 Created
Content-Type: application/json
Location: /api/loans/101

{
    "loanId": 101,
    "userId": 3,
    "bookId": 5,
    "status": "CheckedOut",
    "checkoutDate": "2025-11-01T10:35:00Z",
    "dueDate": "2025-11-15T10:35:00Z",
    "returnDate": null,
    "isOverdue": false,
    "daysUntilDue": 14
}
```

**Frontend JavaScript:**
```javascript
alert('Book borrowed successfully! Due date: 11/15/2025');
window.location.href = 'my-loans.html';
```

## Scenario 3: Saga Failure with Compensating Transaction

### What Happens When Stock Decrement Fails?

```mermaid
sequenceDiagram
    participant LoanService
    participant LoanDB
    participant CatalogService
    participant CatalogDB
    
    Note over LoanService: SAGA STEP 2: Create PENDING loan
    LoanService->>LoanDB: INSERT Loan (Status='PENDING')
    LoanDB-->>LoanService: LoanId=102
    
    Note over LoanService: SAGA STEP 3: Check availability
    LoanService->>CatalogService: GET /api/books/5
    CatalogService->>CatalogDB: SELECT * FROM Books WHERE BookId=5
    CatalogDB-->>CatalogService: {AvailableCopies: 0}
    CatalogService-->>LoanService: BookDto {IsAvailable: false}
    
    Note over LoanService: SAGA STEP 3 FAILED!
    Note over LoanService: COMPENSATING TRANSACTION
    
    LoanService->>LoanService: loan.MarkAsFailed()
    LoanService->>LoanDB: UPDATE Loans<br/>SET Status='FAILED'<br/>WHERE LoanId=102
    LoanDB-->>LoanService: Success
    
    LoanService-->>Client: 400 Bad Request<br/>"Book is not available"
```

**Database state after failure:**
```
Loans table:
+--------+--------+--------+--------+------------+------------+
| LoanId | UserId | BookId | Status | CheckoutDt | DueDate    |
+--------+--------+--------+--------+------------+------------+
| 102    | 3      | 5      | FAILED | 2025-11-01 | 2025-11-15 |
+--------+--------+--------+--------+------------+------------+

Books table: (UNCHANGED)
+--------+-------+--------------+-----------------+
| BookId | Title | TotalCopies  | AvailableCopies |
+--------+-------+--------------+-----------------+
| 5      | ...   | 5            | 0               |
+--------+-------+--------------+-----------------+
```

**Key Points:**
1. PENDING loan was created (Step 2)
2. Stock check failed (Step 3)
3. Compensating transaction marked loan as FAILED
4. No stock was decremented (consistency maintained)
5. FAILED loan kept for audit trail

## Key Architectural Insights

### Data Transformation Journey

| Layer | Input Format | Output Format | Purpose |
|-------|--------------|---------------|---------|
| **Frontend** | User input (form) | JSON string | Collect data |
| **API Client** | JavaScript object | HTTP request | Send to server |
| **Gateway** | HTTP request | Routed HTTP request | Route & validate |
| **Controller** | JSON | DTO object | Deserialize |
| **Application Service** | DTO | Domain entity | Orchestrate |
| **Domain Entity** | Constructor params | Validated entity | Enforce rules |
| **Repository** | Domain entity | SQL query | Persist |
| **Database** | SQL query | Table rows | Store |

### Clean Architecture in Action

1. **Domain Layer** - Business rules enforced in entity constructors and methods
2. **Application Layer** - Orchestrates workflow, coordinates between layers
3. **Infrastructure Layer** - Handles technical concerns (HTTP, database, BCrypt)
4. **Presentation Layer** - HTTP concerns, serialization, authentication

### Saga Pattern Benefits

1. **Consistency** - Distributed transaction across services
2. **Auditability** - PENDING and FAILED states provide audit trail
3. **Resilience** - Compensating transactions handle failures gracefully
4. **Logging** - Comprehensive logging at each step for debugging

### JWT Token Flow

1. **User logs in** → UserService generates JWT
2. **Frontend stores** JWT in localStorage
3. **Every API call** includes `Authorization: Bearer {JWT}` header
4. **Gateway validates** JWT signature and expiration
5. **Services trust** Gateway's validation (no re-validation)
6. **LoanService forwards** JWT to CatalogService for authorization

## Key Takeaways

### Complete Request Flow
1. Frontend form → JavaScript → HTTP request
2. Nginx proxy → Gateway routing → Service discovery
3. Controller → Application Service → Domain Entity
4. Repository → Database → SQL execution
5. Response travels back through same layers

### Data Transformation
- Plain text password → BCrypt hash
- JavaScript object → JSON → DTO → Domain entity
- Domain entity → DTO → JSON → JavaScript object
- Email normalization (lowercase)
- Validation at multiple layers

### Saga Pattern
- 5-step orchestration for distributed transactions
- PENDING state during execution
- Compensating transactions on failure
- Comprehensive logging for debugging
- Audit trail with FAILED loans

### Clean Architecture Benefits
- Clear separation of concerns
- Business logic isolated in Domain layer
- Easy to test (mock interfaces)
- Flexible (swap implementations)
- Maintainable (changes localized)

## Conclusion

You now understand how LibHub works from top to bottom:
- **Layer 1**: System overview and component interaction
- **Layer 2**: Clean Architecture implementation in UserService
- **Layer 3**: Dependency injection and middleware pipeline
- **Layer 4**: Inter-service communication and Saga pattern
- **Layer 5**: Complete end-to-end data flows

This layered understanding allows you to:
- Debug issues by tracing through layers
- Add new features following established patterns
- Understand trade-offs in distributed systems
- Appreciate the value of Clean Architecture


