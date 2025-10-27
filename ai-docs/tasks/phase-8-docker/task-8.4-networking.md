# Task 8.4: Networking Setup and Verification for LibHub

This document provides instructions for verifying and configuring networking for LibHub microservices using Docker's bridge network.

## 1. Verify Services on libhub-network

Ensure all services are connected to the `libhub-network` bridge network as defined in `docker-compose.yml`. After starting containers with `docker compose up -d`, run:

```bash
docker network inspect libhub_libhub-network
```

- **Expected Output**: All services (mysql, userservice, catalogservice, loanservice, gateway, frontend) should be listed under `Containers` with their respective IP addresses.

## 2. Document Service Name Resolution

- **Inside Docker**: Services communicate using container names as DNS entries resolved by Docker's built-in DNS server.
  - Examples: `mysql`, `userservice`, `catalogservice`, `loanservice`
- **From Host**: Access services using `localhost` with mapped ports.
  - Examples: `localhost:3306` (MySQL), `localhost:5002` (UserService), `localhost:5000` (Gateway)

## 3. Update LoanService Configuration

Update the `CatalogServiceBaseUrl` in LoanService to use the container name instead of localhost. Modify `appsettings.json` at `/home/thuannp4/development/LibHub/src/Services/LoanService/LibHub.LoanService.Api/appsettings.json`:

```json
"ExternalServices": {
  "CatalogServiceBaseUrl": "http://catalogservice:5001"
}
```

Alternatively, set it as an environment variable in `docker-compose.yml` under the `loanservice` section:

```yaml
environment:
  - ExternalServices__CatalogServiceBaseUrl=http://catalogservice:5001
```

## 4. Test Inter-Service Communication

Verify communication between services inside the network:

- Access LoanService container and test connection to CatalogService:
  ```bash
  docker exec -it libhub-loanservice bash
  curl http://catalogservice:5001/health
  ```
  - **Expected Output**: Should return a health status (e.g., `OK` or `200` response).

- Test MySQL connection from a service container (e.g., UserService):
  ```bash
  docker exec -it libhub-userservice bash
  apt-get update && apt-get install -y mysql-client
  mysql -h mysql -u libhub_user -p
  # Password: LibHub@Dev2025
  SHOW DATABASES;
  ```
  - **Expected Output**: Should list `user_db`, `catalog_db`, and `loan_db`.

## 5. Verify Gateway Routing to Backend Services

Ensure Gateway routes requests correctly to backend services:

- Test Gateway routing from host:
  ```bash
  curl http://localhost:5000/api/books
  curl http://localhost:5000/api/users/register
  curl http://localhost:5000/api/loans
  ```
  - **Expected Output**: Responses should match the respective service endpoints or return appropriate status codes.

- Check Gateway logs for routing confirmation:
  ```bash
  docker compose logs -f gateway | grep "routing"
  ```

## 6. Network Inspection Commands

Use these commands for deeper network troubleshooting:

- Inspect network details:
  ```bash
  docker network inspect libhub_libhub-network
  ```
- List all containers and their networks:
  ```bash
  docker ps -a --format "{{.Names}} - {{.Networks}}"
  ```
- Ping between containers to confirm connectivity:
  ```bash
  docker exec -it libhub-loanservice ping catalogservice -c 4
  docker exec -it libhub-userservice ping mysql -c 4
  ```
  - **Expected Output**: Successful ping responses with no packet loss.
