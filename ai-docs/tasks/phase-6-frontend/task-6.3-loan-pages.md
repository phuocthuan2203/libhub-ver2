# Task 6.3: Implement Loan Pages

**Phase**: 6 - Frontend Implementation  
**Estimated Time**: 2 hours  
**Dependencies**: Task 6.2 (Catalog pages)

---

## Objective

Build user loan history and return functionality pages.

---

## Key Implementation Points

### 1. my-loans.html

<nav> <a href="index.html">Browse Books</a> <a href="my-loans.html">My Loans</a> <button onclick="logout()">Logout</button> </nav> <h2>My Loans</h2> <div id="activeLoans"> <h3>Active Loans</h3> <div id="activeList"></div> </div> <div id="loanHistory"> <h3>Loan History</h3> <div id="historyList"></div> </div> <script> // Call requireAuth() on page load requireAuth(); async function loadMyLoans() { const token = localStorage.getItem('jwt_token'); const userId = localStorage.getItem('user_id'); const response = await fetch( `http://localhost:5000/api/loans/user/${userId}`, { headers: { 'Authorization': `Bearer ${token}` } } ); const loans = await response.json(); const active = loans.filter(l => l.status === 'CheckedOut'); const history = loans.filter(l => l.status !== 'CheckedOut'); renderActiveLoans(active); renderHistory(history); } function renderActiveLoans(loans) { const container = document.getElementById('activeList'); container.innerHTML = loans.map(loan => ` <div class="loan-card ${loan.isOverdue ? 'overdue' : ''}"> <h4>Book ID: ${loan.bookId}</h4> <p>Due: ${new Date(loan.dueDate).toLocaleDateString()}</p> <p>${loan.isOverdue ? 'OVERDUE!' : `${loan.daysUntilDue} days left`}</p> <button onclick="returnBook(${loan.loanId})">Return Book</button> </div> `).join(''); } async function returnBook(loanId) { const token = localStorage.getItem('jwt_token'); const response = await fetch( `http://localhost:5000/api/loans/${loanId}/return`, { method: 'PUT', headers: { 'Authorization': `Bearer ${token}` } } ); if (response.ok) { alert('Book returned successfully!'); loadMyLoans(); // Refresh } } </script>
text

---

## Key Features

- **Active Loans**: Display all currently borrowed books
- **Overdue Indicator**: Highlight overdue loans in red
- **Return Button**: One-click return with confirmation
- **Loan History**: Show past loans (returned or failed)
- **Real-time Updates**: Refresh after return action
- **Book Details Link**: Click to see book info (optional enhancement)

---

## Styling Suggestions

.loan-card {
border: 1px solid #ddd;
padding: 1rem;
margin: 0.5rem 0;
}

.loan-card.overdue {
border-color: #dc3545;
background-color: #ffe5e5;
}

text

---

## Acceptance Criteria

- [ ] Page requires authentication
- [ ] Displays user's active loans
- [ ] Displays loan history
- [ ] Overdue loans highlighted
- [ ] Return button works
- [ ] Success message on return
- [ ] Page refreshes after return
- [ ] Days until due calculated correctly
- [ ] Handles empty loan list gracefully

---

## After Completion

Update **PROJECT_STATUS.md**:
✅ Task 6.3: Loan Pages (date)

My loans page with active and history

Return functionality

Overdue indicators

text

**Next: Task 6.4** (Admin pages - final task!)