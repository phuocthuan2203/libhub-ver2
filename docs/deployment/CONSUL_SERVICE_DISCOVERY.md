# Consul Service Discovery Flow

## Overview

This document explains how the LibHub Gateway uses Consul for service discovery. When a client makes a request, the Gateway queries Consul to find the address of the target service, then forwards the request to that address.

## Request Flow

```
Client Request → Gateway → Consul (Query Service) → Gateway (Get Address) → Target Service → Response
```

### Step-by-Step Process

1. **Client sends request** to Gateway (e.g., `GET http://localhost:5000/api/catalog/books`)
2. **Gateway receives request** and identifies target service (CatalogService)
3. **Gateway queries Consul** to discover CatalogService instances
4. **Consul returns service address** (e.g., `http://catalogservice:8081`)
5. **Gateway forwards request** to the discovered address
6. **Target service processes** and returns response
7. **Gateway returns response** to client

## Checking Gateway Logs

### View Real-Time Gateway Logs

```bash
docker logs -f libhub-gateway
```

### View Last 100 Lines

```bash
docker logs --tail 100 libhub-gateway
```

### View Logs with Timestamps

```bash
docker logs -f --timestamps libhub-gateway
```

## Expected Log Output

When a request flows through the Gateway with Consul service discovery, you should see logs similar to:

```
[2025-10-27 11:58:23] INFO: Received request: GET /api/catalog/books
[2025-10-27 11:58:23] INFO: Querying Consul for service: CatalogService
[2025-10-27 11:58:23] INFO: Consul returned service address: http://catalogservice:8081
[2025-10-27 11:58:23] INFO: Forwarding request to: http://catalogservice:8081/api/catalog/books
[2025-10-27 11:58:24] INFO: Received response from CatalogService: 200 OK
[2025-10-27 11:58:24] INFO: Returning response to client
```

### Key Log Indicators

- **Service Discovery**: Look for "Querying Consul for service"
- **Address Resolution**: Look for "Consul returned service address"
- **Request Forwarding**: Look for "Forwarding request to"
- **Service Response**: Look for "Received response from"

## Verifying Service Discovery

### 1. Check Consul UI

Access Consul UI at `http://localhost:8500/ui` to verify services are registered:
- CatalogService
- UserService
- LoanService

### 2. Query Consul API Directly

```bash
curl http://localhost:8500/v1/catalog/service/catalogservice
```

Expected response:
```json
[
  {
    "ServiceName": "catalogservice",
    "ServiceAddress": "catalogservice",
    "ServicePort": 8081,
    "ServiceID": "catalogservice-1"
  }
]
```

### 3. Test Gateway Service Discovery

```bash
curl http://localhost:5000/api/catalog/books
```

Then check Gateway logs to see the Consul query and address resolution.

## Troubleshooting

### Gateway Cannot Find Service

**Symptom**: Gateway logs show "Service not found in Consul"

**Solution**:
1. Verify service is registered in Consul UI
2. Check service health status in Consul
3. Restart the service container

```bash
docker restart libhub-catalogservice-1
```

### Gateway Uses Wrong Address

**Symptom**: Gateway forwards to incorrect address

**Solution**:
1. Check Consul service registration
2. Verify Docker network configuration
3. Ensure services use correct service names

```bash
docker network inspect libhub_default
```

### No Consul Query in Logs

**Symptom**: Logs don't show Consul queries

**Solution**:
1. Verify Gateway Consul configuration
2. Check Gateway environment variables
3. Ensure Consul client is initialized

```bash
docker exec libhub-gateway env | grep CONSUL
```

## Configuration

### Gateway Consul Settings

The Gateway should be configured with:

```yaml
Consul:
  Host: consul
  Port: 8500
  ServiceName: gateway
```

### Service Registration

Each service registers with Consul on startup:

```yaml
Consul:
  Host: consul
  Port: 8500
  ServiceName: catalogservice
  ServicePort: 8081
  HealthCheckInterval: 10s
```

## Monitoring Service Discovery

### Watch Consul Events

```bash
docker logs -f consul
```

### Monitor Gateway Performance

```bash
docker stats libhub-gateway
```

### Check All Service Logs

```bash
docker-compose logs -f gateway catalogservice userservice loanservice
```

## Best Practices

1. **Always check Gateway logs** when debugging request routing issues
2. **Verify Consul registration** before testing endpoints
3. **Use service names** (not IP addresses) in configuration
4. **Monitor health checks** to ensure services remain discoverable
5. **Check Docker network** if services cannot communicate

## Related Documentation

- [Consul Discovery Logs Guide](./CONSUL_DISCOVERY_LOGS_GUIDE.md)
- [Docker Quick Start](./DOCKER_QUICK_START.md)
