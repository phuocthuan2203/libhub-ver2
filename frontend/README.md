# LibHub Frontend

Modern library management system frontend built with vanilla JavaScript, HTML5, and CSS3.

## Quick Start

1. **Start the backend services** (in separate terminals):
   ```bash
   cd src/Services/UserService/LibHub.UserService.Api
   dotnet run
   
   cd src/Services/CatalogService/LibHub.CatalogService.Api
   dotnet run
   
   cd src/Services/LoanService/LibHub.LoanService.Api
   dotnet run
   
   cd src/Gateway/LibHub.Gateway.Api
   dotnet run
   ```

2. **Open the frontend**:
   - Simply open `index.html` in your web browser
   - Or use a local web server:
     ```bash
     cd frontend
     python3 -m http.server 8080
     ```
   - Then navigate to `http://localhost:8080`

## Pages

### Public Pages (No Authentication)
- **index.html** - Book catalog with search and filters
- **book-detail.html** - Individual book details
- **login.html** - User login
- **register.html** - New user registration

### Authenticated Pages (Customer)
- **my-loans.html** - View active loans and history, return books

### Admin Pages (Admin Role Only)
- **admin-catalog.html** - Manage books (view, edit, delete)
- **admin-add-book.html** - Add new books to catalog
- **admin-edit-book.html** - Edit existing book details
- **admin-loans.html** - View all system loans with filters

## Features

- JWT-based authentication
- Role-based access control (Customer/Admin)
- Book search and filtering
- Borrow and return books
- Overdue loan tracking
- Admin book management (CRUD)
- Responsive design

## API Configuration

The frontend connects to the API Gateway at:
```javascript
const API_BASE_URL = 'http://localhost:5000';
```

To change the API URL, edit `js/api-client.js`.

## User Roles

### Customer
- Browse and search books
- Borrow books (max 5 active loans)
- View loan history
- Return books

### Admin
- All Customer features
- Add/edit/delete books
- View all system loans
- Manage book inventory

## Testing Accounts

After starting the services, you can:
1. Register a new account (defaults to Customer role)
2. For Admin access, manually update the database:
   ```sql
   UPDATE user_db.Users SET Role = 'Admin' WHERE Email = 'your@email.com';
   ```

## Browser Compatibility

- Chrome/Edge (recommended)
- Firefox
- Safari
- Any modern browser with ES6+ support

## Project Structure

```
frontend/
├── index.html              # Book catalog
├── book-detail.html        # Book details
├── login.html              # Login page
├── register.html           # Registration
├── my-loans.html          # User loans
├── admin-catalog.html     # Admin book management
├── admin-add-book.html    # Add book form
├── admin-edit-book.html   # Edit book form
├── admin-loans.html       # All loans view
├── css/
│   └── styles.css         # Global styles
└── js/
    ├── api-client.js      # API wrapper
    └── auth.js            # Auth utilities
```

## Development Notes

- No build process required (vanilla JavaScript)
- JWT tokens stored in localStorage
- All API calls go through the Gateway (port 5000)
- Client-side validation before API calls
- Error handling with user-friendly messages

## Security Notes

- Always use HTTPS in production
- JWT tokens expire after 1 hour
- Passwords must meet complexity requirements:
  - Minimum 8 characters
  - At least one uppercase letter
  - At least one lowercase letter
  - At least one digit
  - At least one special character

## Troubleshooting

**Login fails**: Ensure all backend services are running
**CORS errors**: Check Gateway CORS configuration
**Token expired**: Login again to get a new token
**Admin pages not accessible**: Verify your account has Admin role in database
