# Database Setup Guide

## Initial Database Configuration

The LibHub application uses MySQL 8.0 with three separate databases:
- `user_db` - User authentication and profile data
- `catalog_db` - Book catalog information
- `loan_db` - Loan transaction records

## Database Permissions

The application uses a dedicated MySQL user `libhub_user` that requires proper permissions to access all three databases.

### Automatic Setup

The database initialization is handled by the `scripts/init-databases.sql` script, which:
1. Creates all three databases if they don't exist
2. Grants full privileges to `libhub_user` on all databases
3. Flushes privileges to apply changes

### Manual Setup (if needed)

If you encounter "Access denied" errors, run the following commands:

```bash
docker exec -i libhub-mysql mysql -uroot -pLibHub@2025 << 'EOF'
CREATE DATABASE IF NOT EXISTS user_db;
CREATE DATABASE IF NOT EXISTS catalog_db;
CREATE DATABASE IF NOT EXISTS loan_db;

GRANT ALL PRIVILEGES ON user_db.* TO 'libhub_user'@'%';
GRANT ALL PRIVILEGES ON catalog_db.* TO 'libhub_user'@'%';
GRANT ALL PRIVILEGES ON loan_db.* TO 'libhub_user'@'%';
FLUSH PRIVILEGES;
EOF
```

Then restart the services:

```bash
docker compose restart userservice catalogservice loanservice gateway
```

## Verification

To verify the database setup:

```bash
docker exec -i libhub-mysql mysql -uroot -pLibHub@2025 -e "SHOW GRANTS FOR 'libhub_user'@'%';"
```

Expected output should show grants for all three databases:
```
GRANT USAGE ON *.* TO `libhub_user`@`%`
GRANT ALL PRIVILEGES ON `catalog_db`.* TO `libhub_user`@`%`
GRANT ALL PRIVILEGES ON `loan_db`.* TO `libhub_user`@`%`
GRANT ALL PRIVILEGES ON `user_db`.* TO `libhub_user`@`%`
```

## Troubleshooting

### Services failing to start

If microservices fail with "Access denied" errors:
1. Check database permissions using the verification command above
2. Run the manual setup commands
3. Restart the affected services

### Connection refused errors

If you get connection refused errors:
1. Ensure MySQL container is healthy: `docker ps`
2. Check MySQL logs: `docker compose logs mysql`
3. Verify network connectivity: `docker compose exec userservice ping mysql`

