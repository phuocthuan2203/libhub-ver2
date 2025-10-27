# Task 6.4: Implement Admin Pages

**Phase**: 6 - Frontend Implementation  
**Estimated Time**: 3 hours  
**Dependencies**: Task 6.3 (Loan pages)

---

## Objective

Build admin dashboard for managing books and viewing all loans (Admin role only).

---

## Key Implementation Points

### 1. Pages to Create

frontend/
├── admin-catalog.html
├── admin-add-book.html
├── admin-edit-book.html
└── admin-loans.html

text

### 2. Admin Access Check

// At top of every admin page
function requireAdmin() {
const role = localStorage.getItem('user_role');
if (role !== 'Admin') {
alert('Access denied. Admin only.');
window.location.href = 'index.html';
}
}

requireAdmin(); // Call immediately

text

### 3. admin-catalog.html

<h2>Manage Books</h2> <button onclick="location.href='admin-add-book.html'">Add New Book</button> <table id="bookTable"> <thead> <tr> <th>ISBN</th> <th>Title</th> <th>Author</th> <th>Genre</th> <th>Total</th> <th>Available</th> <th>Actions</th> </tr> </thead> <tbody id="bookList"></tbody> </table> <script> async function loadBooks() { const response = await fetch('http://localhost:5000/api/books'); const books = await response.json(); document.getElementById('bookList').innerHTML = books.map(book => ` <tr> <td>${book.isbn}</td> <td>${book.title}</td> <td>${book.author}</td> <td>${book.genre}</td> <td>${book.totalCopies}</td> <td>${book.availableCopies}</td> <td> <button onclick="editBook(${book.bookId})">Edit</button> <button onclick="deleteBook(${book.bookId})">Delete</button> </td> </tr> `).join(''); } async function deleteBook(bookId) { if (!confirm('Delete this book?')) return; const token = localStorage.getItem('jwt_token'); const response = await fetch( `http://localhost:5000/api/books/${bookId}`, { method: 'DELETE', headers: { 'Authorization': `Bearer ${token}` } } ); if (response.ok) { loadBooks(); // Refresh } else { alert('Cannot delete book (may have active loans)'); } } </script>
text

### 4. admin-add-book.html

<h2>Add New Book</h2> <form id="addBookForm"> <label>ISBN (13 digits):</label> <input type="text" id="isbn" pattern="[0-9]{13}" required>
text
<label>Title:</label>
<input type="text" id="title" required>

<label>Author:</label>
<input type="text" id="author" required>

<label>Genre:</label>
<select id="genre" required>
    <option value="Fiction">Fiction</option>
    <option value="Non-Fiction">Non-Fiction</option>
    <!-- Add more -->
</select>

<label>Description:</label>
<textarea id="description"></textarea>

<label>Total Copies:</label>
<input type="number" id="totalCopies" min="0" required>

<button type="submit">Add Book</button>
<button type="button" onclick="location.href='admin-catalog.html'">Cancel</button>
</form> <script> document.getElementById('addBookForm').addEventListener('submit', async (e) => { e.preventDefault(); const token = localStorage.getItem('jwt_token'); const bookData = { isbn: document.getElementById('isbn').value, title: document.getElementById('title').value, author: document.getElementById('author').value, genre: document.getElementById('genre').value, description: document.getElementById('description').value, totalCopies: parseInt(document.getElementById('totalCopies').value) }; const response = await fetch('http://localhost:5000/api/books', { method: 'POST', headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' }, body: JSON.stringify(bookData) }); if (response.ok) { alert('Book added successfully!'); location.href = 'admin-catalog.html'; } else { const error = await response.json(); alert(`Error: ${error.message}`); } }); </script>
text

### 5. admin-edit-book.html

<!-- Similar to add-book but: - Load existing book data on page load (GET /api/books/{id}) - Pre-fill form fields - Use PUT instead of POST - ISBN should be read-only (can't change) -->
text

### 6. admin-loans.html

<h2>All System Loans</h2> <div id="filters"> <button onclick="filterLoans('all')">All</button> <button onclick="filterLoans('active')">Active</button> <button onclick="filterLoans('overdue')">Overdue</button> <button onclick="filterLoans('returned')">Returned</button> </div> <table id="loansTable"> <thead> <tr> <th>Loan ID</th> <th>User ID</th> <th>Book ID</th> <th>Status</th> <th>Checkout Date</th> <th>Due Date</th> <th>Return Date</th> </tr> </thead> <tbody id="loansList"></tbody> </table> <script> async function loadAllLoans() { const token = localStorage.getItem('jwt_token'); const response = await fetch('http://localhost:5000/api/loans', { headers: { 'Authorization': `Bearer ${token}` } }); const loans = await response.json(); renderLoans(loans); } function filterLoans(filter) { // Filter and re-render based on status } </script>
text

---

## Key Features

- **Role Protection**: All admin pages check for Admin role
- **Book Management**: Full CRUD operations
- **Loan Monitoring**: View all system loans with filters
- **ISBN Validation**: Client-side validation for 13 digits
- **Delete Protection**: Warn about books with active loans
- **Responsive Tables**: Display data clearly

---

## Acceptance Criteria

- [ ] All admin pages require Admin role
- [ ] Non-admins redirected to home
- [ ] Can add new books with validation
- [ ] Can edit existing books
- [ ] Can delete books (if no active loans)
- [ ] Can view all system loans
- [ ] Can filter loans by status
- [ ] ISBN validation (13 digits)
- [ ] Success/error messages displayed
- [ ] Admin navigation between pages

---

## After Completion

Update **PROJECT_STATUS.md**:
Phase Status Overview
| Phase 6: Frontend | ✅ COMPLETE | 100% (4/4) | Full application complete! |

Completed Tasks
✅ Task 6.4: Admin Pages (date)

Complete admin dashboard

Book CRUD operations

System-wide loan monitoring

Phase 6 Complete! 🎉

🚀 LibHub Project COMPLETE! 🚀

Overall Progress
Overall Progress: 100% (20/20 tasks complete)

🎉 PROJECT COMPLETE! 🎉
All phases implemented:

✅ Phase 1: Database Setup (3 services)

✅ Phase 2: UserService (authentication & JWT)

✅ Phase 3: CatalogService (book catalog)

✅ Phase 4: LoanService (Saga pattern)

✅ Phase 5: API Gateway (Ocelot integration)

✅ Phase 6: Frontend (complete UI)

Working LibHub Application:

User registration and login

Book browsing and search

Borrow and return books

Admin book management

Distributed transactions (Saga)

Microservices architecture

Clean Architecture in all services

text

**Git commit** → **Move task file** → **PROJECT COMPLETE!** 🎉