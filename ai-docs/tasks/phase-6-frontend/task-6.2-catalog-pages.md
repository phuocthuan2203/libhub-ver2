# Task 6.2: Implement Catalog Pages

**Phase**: 6 - Frontend Implementation  
**Estimated Time**: 2-3 hours  
**Dependencies**: Task 6.1 (Auth pages)

---

## Objective

Build book browsing, search, and detail pages (public access).

---

## Key Implementation Points

### 1. Pages to Create

frontend/
├── index.html (book catalog/search)
├── book-detail.html
└── js/catalog.js

text

### 2. index.html - Book Catalog

<nav> <!-- Show "Login" or "My Loans" based on auth status --> <div id="navLinks"></div> </nav> <div id="searchSection"> <input type="text" id="searchInput" placeholder="Search by title, author, ISBN"> <select id="genreFilter"> <option value="">All Genres</option> <option value="Fiction">Fiction</option> <!-- Add more genres --> </select> <button onclick="searchBooks()">Search</button> </div> <div id="bookGrid"> <!-- Books rendered here dynamically --> </div> <script> async function loadBooks(search = '', genre = '') { const query = new URLSearchParams({ search, genre }); const response = await fetch(`http://localhost:5000/api/books?${query}`); const books = await response.json(); renderBooks(books); } function renderBooks(books) { const grid = document.getElementById('bookGrid'); grid.innerHTML = books.map(book => ` <div class="book-card"> <h3>${book.title}</h3> <p>by ${book.author}</p> <p>Genre: ${book.genre}</p> <p>${book.availableCopies} of ${book.totalCopies} available</p> <button onclick="viewDetails(${book.bookId})">View Details</button> </div> `).join(''); } </script>
text

### 3. book-detail.html

<div id="bookDetail"> <!-- Book info loaded dynamically --> </div> <button id="borrowBtn" onclick="borrowBook()" style="display:none"> Borrow This Book </button> <script> async function loadBookDetail(bookId) { const response = await fetch(`http://localhost:5000/api/books/${bookId}`); const book = await response.json(); // Render book details // Show borrow button only if logged in AND book available if (isLoggedIn() && book.isAvailable) { document.getElementById('borrowBtn').style.display = 'block'; } } async function borrowBook() { const token = localStorage.getItem('jwt_token'); const bookId = new URLSearchParams(window.location.search).get('id'); // POST to /api/loans with Authorization header // Handle success/error (max loans, book unavailable, etc.) } </script>
text

---

## Key Features

- **Public Access**: Catalog is viewable without login
- **Search**: Case-insensitive search on title, author, ISBN
- **Genre Filter**: Can combine with search
- **Availability Display**: Show available/total copies
- **Borrow Button**: Only visible if logged in and book available
- **Error Handling**: Display user-friendly messages for API errors

---

## Acceptance Criteria

- [ ] Book catalog displays all books on load
- [ ] Search functionality works
- [ ] Genre filter works (alone or with search)
- [ ] Book detail page shows full information
- [ ] Borrow button only shown to logged-in users
- [ ] Borrow button disabled if no copies available
- [ ] Successful borrow redirects to my-loans.html
- [ ] Error messages displayed for failed borrow

---

## After Completion

Update **PROJECT_STATUS.md**:
✅ Task 6.2: Catalog Pages (date)

Book browsing and search

Book detail page with borrow functionality

Public access catalog

text

**Next: Task 6.3** (Loan pages)