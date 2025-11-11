# Consul Service Discovery Logging Implementation

## Overview
This implementation adds comprehensive logging when Consul returns service addresses to the API Gateway, providing full visibility into service discovery and instance selection.

## What Was Implemented

### 1. Custom Service Discovery Provider
**File:** `ServiceDiscovery/LoggingConsulServiceDiscoveryProvider.cs`

Wraps Consul's service discovery to log:
- When Gateway queries Consul for a service
- Number of instances returned
- Actual IP:Port of all discovered instances
- Detailed instance information (host, port, scheme)

### 2. Service Discovery Provider Factory
**File:** `ServiceDiscovery/LoggingServiceDiscoveryProviderFactory.cs`

Factory pattern to inject logging into Ocelot's service discovery pipeline:
- Decorates the original Consul provider
- Handles Ocelot's Response<T> pattern
- Passes service name context to the provider

### 3. Extension Method
**File:** `ServiceDiscovery/LoggingServiceDiscoveryExtensions.cs`

Fluent API to add logging to Ocelot builder:
- Uses Scrutor's Decorate pattern
- Integrates seamlessly with Ocelot's DI container
- Easy to enable/disable

### 4. Enhanced Downstream Call Logging
**File:** `Handlers/ConsulDiscoveryLoggingHandler.cs`

Enhanced to log:
- The actual resolved URL (IP:Port) selected by load balancer
- Request duration
- Response status code
- Correlation ID for request tracking

## Log Output Examples

### When Gateway queries Consul:
```
[10:30:15 INF] [Gateway] [req-123] 🔍 [CONSUL-QUERY] Querying Consul for service: catalogservice
```

### When Consul returns instances:
```
[10:30:15 INF] [Gateway] [req-123] 📍 [CONSUL-RESPONSE] Found 2 instance(s) for service: catalogservice | Instances: 172.18.0.5:5001, 172.18.0.6:5001
```

### Selected instance (after load balancing):
```
[10:30:15 INF] [Gateway] [req-123] 📍 [CONSUL-RESOLVED] Selected instance | Service: catalogservice | URL: http://172.18.0.5:5001/api/books
```

### Downstream call result:
```
[10:30:15 INF] [Gateway] [req-123] 🎯 [DOWNSTREAM-CALL] GET http://172.18.0.5:5001/api/books → 200 | Duration: 45ms | CorrelationId: req-123
```

## Architecture

```
┌──────────────┐
│   Frontend   │
└──────┬───────┘
       │ X-Correlation-ID: req-123
       ▼
┌──────────────────────────────────────────────┐
│          API Gateway (Ocelot)                │
│                                              │
│  1. LoggingServiceDiscoveryProvider          │
│     ↓ Queries Consul                         │
│     📍 Logs: "Querying Consul for service"   │
│                                              │
│  2. Consul Returns Instances                 │
│     ↓ Receives list                          │
│     📍 Logs: "Found 2 instances: IP:Port"    │
│                                              │
│  3. Load Balancer Selects Instance           │
│     ↓ RoundRobin/LeastConnection             │
│                                              │
│  4. ConsulDiscoveryLoggingHandler            │
│     ↓ Before sending request                 │
│     📍 Logs: "Selected instance: http://..."  │
│     ↓ Sends request                          │
│     ↓ After receiving response               │
│     🎯 Logs: "Response: 200 | Duration: 45ms"│
└──────────────┬───────────────────────────────┘
               │
               ▼
       ┌───────────────┐
       │ CatalogService│
       │ 172.18.0.5    │
       └───────────────┘
```

## Dependencies Added

### NuGet Package:
- **Scrutor 4.2.2** - For service decoration pattern

## Configuration

### In Program.cs:
```csharp
builder.Services.AddOcelot(builder.Configuration)
    .AddConsul()
    .AddPolly()
    .AddLoggingServiceDiscovery()  // ← New extension method
    .AddDelegatingHandler<ConsulDiscoveryLoggingHandler>(true);
```

## Testing

### Run the test script:
```bash
./scripts/test-consul-discovery-logging.sh
```

### Manual testing:
```bash
# 1. Start services
docker compose up -d

# 2. Make a request with correlation ID
CORRELATION_ID="test-$(date +%s)"
curl -H "X-Correlation-ID: $CORRELATION_ID" http://localhost:5000/api/books

# 3. View logs
docker logs libhub-gateway 2>&1 | grep "$CORRELATION_ID"
```

### In Seq UI (http://localhost:5341):
```
# Search for Consul queries
@MessageTemplate like '%CONSUL%'

# Search for specific service
ServiceName = 'catalogservice'

# Track a specific request
CorrelationId = 'req-12345'
```

## Benefits

### ✅ Full Visibility
- See exactly which instances Consul returns
- Know which instance was selected by load balancer
- Track request from Gateway → Service with actual IP:Port

### ✅ Debugging Made Easy
- Identify load balancing issues
- Detect unhealthy instances
- Trace request routing problems

### ✅ Performance Monitoring
- Measure request duration to each instance
- Identify slow instances
- Optimize service distribution

### ✅ Request Tracing
- End-to-end tracking with Correlation ID
- Cross-service visibility
- Complete request journey in one search

## Log Levels

```
Information: Normal operations (all discovery events)
Debug: Detailed instance information
Warning: No instances found
Error: Discovery failures
```

## Next Steps

This implementation provides the foundation for:
1. Load balancing metrics
2. Service health monitoring
3. Automated instance failover detection
4. Performance analytics per instance
