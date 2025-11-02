CREATE DATABASE IF NOT EXISTS user_db;
CREATE DATABASE IF NOT EXISTS catalog_db;
CREATE DATABASE IF NOT EXISTS loan_db;

GRANT ALL PRIVILEGES ON user_db.* TO 'libhub_user'@'%';
GRANT ALL PRIVILEGES ON catalog_db.* TO 'libhub_user'@'%';
GRANT ALL PRIVILEGES ON loan_db.* TO 'libhub_user'@'%';

FLUSH PRIVILEGES;

