# Connection Strings Template## MySQL Connection Strings
### UserService
Server=localhost;Port=3306;Database=user_db;User=libhub_user;Password=LibHub@Dev2025;

### CatalogService
Server=localhost;Port=3306;Database=catalog_db;User=libhub_user;Password=LibHub@Dev2025;

### LoanService
Server=localhost;Port=3306;Database=loan_db;User=libhub_user;Password=LibHub@Dev2025;

## JWT Configuration
### Secret Key (256-bit minimum)
YourSuperSecretKeyHere_MustBe256BitsOrLonger!_LibHub2025

**IMPORTANT**: Change this in production!

### Settings
- Issuer: `LibHub.UserService`
- Audience: `LibHub.Clients`
- Expiry: 1 hour (3600 seconds)

## Service URLs (Development)

- API Gateway: `http://localhost:5000`
- UserService: `http://localhost:5002`
- CatalogService: `http://localhost:5001`
- LoanService: `http://localhost:5003`
