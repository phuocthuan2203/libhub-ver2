# Consul Service Discovery Implementation

## Overview

This project now uses Consul for service discovery, replacing hardcoded service addresses with dynamic service registration and discovery.

## Architecture

### Components

1. **Consul Server** - Service registry and health check coordinator
2. **Microservices** - Register themselves with Consul on startup
3. **API Gateway** - Discovers services through Consul

### Service Registration Flow

1. Service starts up
2. Service registers with Consul (name, address, port, health check endpoint)
3. Consul performs periodic health checks on `/health` endpoint
4. If service becomes unhealthy, Consul marks it as unavailable
5. On shutdown, service deregisters from Consul

### Service Discovery Flow

1. Gateway receives request
2. Gateway queries Consul for service instances
3. Consul returns healthy service instances
4. Gateway routes request using load balancing (RoundRobin)

## Configuration

### Consul Container

```yaml
consul:
  image: consul:1.15
  ports:
    - "8500:8500"  # HTTP API and UI
    - "8600:8600/udp"  # DNS interface
```

### Service Registration

Each service has:
- **ServiceName**: Logical service name (e.g., `userservice`)
- **ServiceHost**: Container hostname
- **ServicePort**: Service port
- **Health Check**: HTTP endpoint at `/health`

### Gateway Configuration

Ocelot routes use `ServiceName` instead of hardcoded hosts:

```json
{
  "ServiceName": "userservice",
  "LoadBalancerOptions": {
    "Type": "RoundRobin"
  }
}
```

## Accessing Consul

### Consul UI
- URL: http://localhost:8500
- View registered services, health status, and service instances

### Consul API
```bash
curl http://localhost:8500/v1/catalog/services
curl http://localhost:8500/v1/health/service/userservice
```

## Health Checks

All services expose `/health` endpoint:
- **Interval**: 10 seconds
- **Timeout**: 5 seconds
- **Deregister**: After 1 minute of critical status

## Load Balancing

Gateway uses RoundRobin load balancing for multiple service instances.

To scale a service:
```bash
docker-compose up -d --scale userservice=3
```

## Benefits

1. **Dynamic Discovery** - No hardcoded service addresses
2. **Health Monitoring** - Automatic detection of unhealthy services
3. **Load Balancing** - Distribute traffic across instances
4. **Service Scaling** - Easy horizontal scaling
5. **Fault Tolerance** - Automatic failover to healthy instances

## Testing

### 1. Start all services
```bash
docker-compose up -d
```

### 2. Verify Consul registration
```bash
curl http://localhost:8500/v1/catalog/services
```

Expected output:
```json
{
  "consul": [],
  "userservice": [],
  "catalogservice": [],
  "loanservice": []
}
```

### 3. Check service health
```bash
curl http://localhost:8500/v1/health/service/userservice?passing
```

### 4. Test through Gateway
```bash
curl http://localhost:5000/api/books
```

## Troubleshooting

### Service not registering
- Check service logs: `docker logs libhub-userservice`
- Verify Consul is running: `docker ps | grep consul`
- Check health endpoint: `curl http://localhost:5002/health`

### Gateway cannot find service
- Verify service is registered in Consul UI
- Check Ocelot configuration has correct ServiceName
- Ensure Gateway has Consul connection configured

### Health check failing
- Check service is responding on `/health`
- Verify network connectivity between Consul and service
- Review health check configuration in service registration

## Files Modified

1. `docker-compose.yml` - Added Consul container and environment variables
2. `src/Gateway/LibHub.Gateway.Api/ocelot.json` - Updated for Consul discovery
3. `src/Gateway/LibHub.Gateway.Api/Program.cs` - Added Consul provider
4. `src/Services/*/LibHub.*.Api/Program.cs` - Added health checks and registration
5. `src/Services/*/LibHub.*.Api/Extensions/ConsulServiceRegistration.cs` - Registration logic
6. `src/Services/*/LibHub.*.Api/*.csproj` - Added Consul NuGet package

## Next Steps

- Monitor service health in Consul UI
- Scale services horizontally for load testing
- Configure custom health checks for database connectivity
- Implement circuit breaker pattern with Polly
