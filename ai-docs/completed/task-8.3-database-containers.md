# Task 8.3: Database Containers and Migration Automation for LibHub

This document provides instructions for automating database migrations and setting up persistent database containers for LibHub microservices.

## 1. Update Program.cs for Auto-Applying Migrations

Update `Program.cs` in UserService, CatalogService, and LoanService to automatically apply migrations on startup. Add the following code before `app.Run()`:

### UserService (`src/Services/UserService/LibHub.UserService.Api/Program.cs`)
```csharp
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
    try
    {
        dbContext.Database.Migrate();
        app.Logger.LogInformation("Database migrations applied successfully for UserService.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to apply database migrations for UserService.");
        throw; // Stop the application if migrations fail
    }
}
```

### CatalogService (`src/Services/CatalogService/LibHub.CatalogService.Api/Program.cs`)
```csharp
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    try
    {
        dbContext.Database.Migrate();
        app.Logger.LogInformation("Database migrations applied successfully for CatalogService.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to apply database migrations for CatalogService.");
        throw;
    }
}
```

### LoanService (`src/Services/LoanService/LibHub.LoanService.Api/Program.cs`)
```csharp
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LoanDbContext>();
    try
    {
        dbContext.Database.Migrate();
        app.Logger.LogInformation("Database migrations applied successfully for LoanService.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Failed to apply database migrations for LoanService.");
        throw;
    }
}
```

## 2. Error Handling for Migration Failures

- The `try-catch` blocks log errors if migrations fail and stop the application to prevent inconsistent states.
- Logs will appear in container output for debugging (`docker compose logs -f <service>`).

## 3. Create scripts/seed-data.sql for Sample Data

Create an optional file at `/home/thuannp4/development/LibHub/scripts/seed-data.sql` with sample data:

```sql
-- Admin User (hashed password using BCrypt, work factor 11)
INSERT INTO user_db.Users (Username, Email, HashedPassword, Role, CreatedAt, UpdatedAt)
VALUES ('admin', 'admin@libhub.com', '$2a$11$5vz6V5W4Mz5B0jL0j5Qvqe5L8a8b5N5B5V5W4Mz5B0jL0j5Qvqe', 'Admin', NOW(), NOW());

-- Test Customer User
INSERT INTO user_db.Users (Username, Email, HashedPassword, Role, CreatedAt, UpdatedAt)
VALUES ('testuser', 'test@libhub.com', '$2a$11$5vz6V5W4Mz5B0jL0j5Qvqe5L8a8b5N5B5V5W4Mz5B0jL0j5Qvqe', 'Customer', NOW(), NOW());

-- Sample Books for Catalog
INSERT INTO catalog_db.Books (Isbn, Title, Author, Genre, Description, TotalCopies, AvailableCopies, CreatedAt, UpdatedAt)
VALUES 
('9780132350884', 'Clean Code', 'Robert C. Martin', 'Technology', 'A handbook of agile software craftsmanship.', 5, 5, NOW(), NOW()),
('9780596007126', 'Head First Design Patterns', 'Eric Freeman', 'Technology', 'A brain-friendly guide to design patterns.', 3, 3, NOW(), NOW()),
('9780743273565', 'The Great Gatsby', 'F. Scott Fitzgerald', 'Fiction', 'A story of the fabulously wealthy Jay Gatsby.', 4, 4, NOW(), NOW()),
('9780141439518', 'Pride and Prejudice', 'Jane Austen', 'Romance', 'A classic novel of manners and marriage.', 6, 6, NOW(), NOW());
```

To use, mount this script in `docker-compose.yml` under MySQL service: `./scripts/seed-data.sql:/docker-entrypoint-initdb.d/seed-data.sql`.

## 4. Configure MySQL Volume in docker-compose.yml for Persistence

Ensure the `docker-compose.yml` at project root includes the following for MySQL persistence:

```yaml
services:
  mysql:
    image: mysql:8.0
    volumes:
      - mysql-data:/var/lib/mysql

volumes:
  mysql-data:
```

- **Named Volume**: `mysql-data` ensures data persists across container restarts.
- **Mount Point**: `/var/lib/mysql` is the MySQL data directory inside the container.

## 5. Verification Commands for Migrations

After starting containers with `docker compose up -d`, verify migrations:

- Check logs for migration success:
  ```bash
  docker compose logs -f userservice | grep "migrations applied"
  docker compose logs -f catalogservice | grep "migrations applied"
  docker compose logs -f loanservice | grep "migrations applied"
  ```
- Access MySQL to confirm databases and tables:
  ```bash
  docker exec -it libhub-mysql mysql -u libhub_user -p
  # Password: LibHub@Dev2025
  SHOW DATABASES;
  USE user_db; SHOW TABLES;
  USE catalog_db; SHOW TABLES;
  USE loan_db; SHOW TABLES;
  ```
