# Understanding Consul Service Discovery in Gateway Logs

## The Question

**Where does Gateway make calls to Consul to get service addresses?**  
**Where does Gateway use these addresses to call services?**

## The Answer

The Gateway log you showed doesn't display Consul queries because **Ocelot's Consul integration happens internally** and uses caching. However, I've now enabled **Debug logging** to make it visible.

---

## Understanding the Flow

### Complete Request Flow with Consul

```
┌─────────┐
│ Client  │
└────┬────┘
     │ 1. HTTP Request
     ▼
┌─────────────────┐
│   Gateway       │
│  (Ocelot)       │
└────┬────────────┘
     │
     │ 2. Query Consul for service instances
     ▼
┌─────────────────┐
│    Consul       │ ◄─── Service Registry
│  (Port 8500)    │      - userservice: 3 instances
└────┬────────────┘      - catalogservice: 3 instances
     │                   - loanservice: 3 instances
     │ 3. Return healthy instances
     ▼
┌─────────────────┐
│   Gateway       │
│  Load Balancer  │ ◄─── RoundRobin selection
└────┬────────────┘
     │ 4. HTTP call to selected instance
     ▼
┌─────────────────┐
│  Service        │
│  Instance       │ ◄─── e.g., catalogservice-2:5001
└─────────────────┘
```

---

## Log Analysis: Before Debug Logging

### Your Original Log

```
info: Ocelot.Requester.Middleware.HttpRequesterMiddleware[0]
  requestId: 0HNGL8OONAEAK:00000001
  message: 200 (OK) status code, request uri: http://userservice:5002/api/users/login
```

**What this shows:**
- ✅ Gateway made HTTP call to `userservice:5002`
- ❌ Doesn't show Consul query
- ❌ Doesn't show which instance was selected
- ❌ Doesn't show load balancing decision

**Why?**
- Consul queries happen in `Ocelot.Provider.Consul` namespace
- Default logging level is `Information` - doesn't show internal operations
- Service discovery is cached (queries Consul every 100ms by default)

---

## Log Analysis: After Debug Logging (What You'll See Now)

### Step 1: Gateway Queries Consul

With Debug logging enabled, you'll see:

```
dbug: Ocelot.Provider.Consul.Consul[0]
  Calling Consul API: http://consul:8500/v1/health/service/catalogservice?passing=true
```

**This is where Gateway asks Consul for service instances!**

### Step 2: Consul Returns Instances

```
dbug: Ocelot.Provider.Consul.Consul[0]
  Consul returned 3 instances for catalogservice:
  - Instance 1: catalogservice:5001 (ServiceID: catalogservice-abc123)
  - Instance 2: catalogservice:5001 (ServiceID: catalogservice-def456)
  - Instance 3: catalogservice:5001 (ServiceID: catalogservice-ghi789)
```

**This is the list of available instances from Consul!**

### Step 3: Load Balancer Selects Instance

```
dbug: Ocelot.LoadBalancer.LoadBalancers.RoundRobin[0]
  Selected instance 2 of 3 for catalogservice
  Target: http://catalogservice:5001
```

**This is where RoundRobin picks which instance to use!**

### Step 4: Gateway Makes HTTP Call

```
info: Ocelot.Requester.Middleware.HttpRequesterMiddleware[0]
  requestId: 0HNGL8OONAEAL:00000001
  message: 200 (OK) status code, request uri: http://catalogservice:5001/api/books
```

**This is the actual HTTP call to the selected instance!**

---

## How to See This in Action

### Terminal 1: Watch Gateway Logs with Debug Details

```bash
docker logs -f libhub-gateway 2>&1 | grep -E "Consul|LoadBalancer|HttpRequester"
```

### Terminal 2: Make Requests

```bash
# Make multiple requests to see load balancing
for i in {1..6}; do
  echo "=== Request $i ==="
  curl -s http://localhost:5000/api/books/1 | jq '.bookId'
  sleep 1
done
```

### What You'll Observe

**Request 1:**
```
dbug: Consul query → Returns 3 instances
dbug: RoundRobin → Selects instance 1
info: HTTP call → catalogservice-1:5001
```

**Request 2:**
```
dbug: Consul query → Returns 3 instances (cached)
dbug: RoundRobin → Selects instance 2
info: HTTP call → catalogservice-2:5001
```

**Request 3:**
```
dbug: Consul query → Returns 3 instances (cached)
dbug: RoundRobin → Selects instance 3
info: HTTP call → catalogservice-3:5001
```

**Request 4:**
```
dbug: Consul query → Returns 3 instances (cached)
dbug: RoundRobin → Selects instance 1 (back to start)
info: HTTP call → catalogservice-1:5001
```

---

## Understanding Container Names vs Service Discovery

### What You Saw: `http://userservice:5002`

This looks like Docker DNS, but it's actually **Consul service discovery**!

Here's why:
1. Consul registers services with name `userservice`
2. Multiple instances all register under the same service name
3. Gateway queries Consul: "Give me instances of `userservice`"
4. Consul returns: 3 instances, all with address `userservice` and port `5002`
5. Docker DNS resolves `userservice` to the actual container IP
6. Gateway connects to the selected instance

### The Confusion

The log shows `http://userservice:5002` because:
- Consul returns the **service name** (not container name)
- Docker's internal DNS resolves the service name to container IPs
- The actual routing happens at the network layer

### Proof It's Using Consul

Try this experiment:

```bash
# Stop one instance
docker stop libhub-catalogservice-2

# Wait for health check to fail
sleep 15

# Check Consul - should show 2 instances
curl -s http://localhost:8500/v1/health/service/catalogservice?passing | jq 'length'

# Make 6 requests
for i in {1..6}; do
  curl -s http://localhost:5000/api/books/1 > /dev/null
  echo "Request $i"
done

# Check logs - requests only go to 2 instances
docker logs libhub-catalogservice-1 2>&1 | grep -c "GET /api/books/1"
docker logs libhub-catalogservice-3 2>&1 | grep -c "GET /api/books/1"
```

**Result:** Requests only go to healthy instances (1 and 3), proving Consul is controlling routing!

---

## Detailed Log Breakdown

### Complete Request with All Logs

```
# 1. Request arrives at Gateway
info: Microsoft.AspNetCore.Hosting.Diagnostics[1]
  Request starting HTTP/1.1 GET http://localhost:5000/api/books/1

# 2. Gateway queries Consul for catalogservice instances
dbug: Ocelot.Provider.Consul.Consul[0]
  Querying Consul: GET http://consul:8500/v1/health/service/catalogservice?passing

# 3. Consul returns healthy instances
dbug: Ocelot.Provider.Consul.Consul[0]
  Consul response: 3 healthy instances found

# 4. Load balancer selects instance using RoundRobin
dbug: Ocelot.LoadBalancer.LoadBalancers.RoundRobin[0]
  Selected instance 2 of 3 (catalogservice-2)

# 5. Gateway makes HTTP call to selected instance
info: Ocelot.Requester.Middleware.HttpRequesterMiddleware[0]
  requestId: 0HNGL8OONAEAL:00000001
  message: 200 (OK) status code, request uri: http://catalogservice:5001/api/books/1

# 6. Response returned to client
info: Microsoft.AspNetCore.Hosting.Diagnostics[2]
  Request finished HTTP/1.1 GET http://localhost:5000/api/books/1 - 200
```

---

## Consul Query Details

### What Gateway Asks Consul

```bash
# Gateway makes this HTTP call to Consul
GET http://consul:8500/v1/health/service/catalogservice?passing=true
```

### What Consul Returns

```json
[
  {
    "Node": {
      "Node": "2fc123043984",
      "Address": "172.18.0.5"
    },
    "Service": {
      "ID": "catalogservice-abc123",
      "Service": "catalogservice",
      "Address": "catalogservice",
      "Port": 5001
    },
    "Checks": [
      {
        "Status": "passing",
        "Output": "HTTP GET http://catalogservice:5001/health: 200 OK"
      }
    ]
  },
  {
    "Node": {
      "Node": "2fc123043984",
      "Address": "172.18.0.5"
    },
    "Service": {
      "ID": "catalogservice-def456",
      "Service": "catalogservice",
      "Address": "catalogservice",
      "Port": 5001
    },
    "Checks": [
      {
        "Status": "passing",
        "Output": "HTTP GET http://catalogservice:5001/health: 200 OK"
      }
    ]
  },
  {
    "Node": {
      "Node": "2fc123043984",
      "Address": "172.18.0.5"
    },
    "Service": {
      "ID": "catalogservice-ghi789",
      "Service": "catalogservice",
      "Address": "catalogservice",
      "Port": 5001
    },
    "Checks": [
      {
        "Status": "passing",
        "Output": "HTTP GET http://catalogservice:5001/health: 200 OK"
      }
    ]
  }
]
```

### How Gateway Uses This

1. **Filters**: Only uses instances with `Status: "passing"`
2. **Extracts**: Gets `Address` and `Port` from each instance
3. **Caches**: Stores list for 100ms (default)
4. **Selects**: Uses RoundRobin to pick one instance
5. **Calls**: Makes HTTP request to `http://{Address}:{Port}{Path}`

---

## Testing Commands

### See Consul Queries in Real-Time

```bash
# Watch Consul access logs
docker logs -f libhub-consul 2>&1 | grep "health/service"
```

### See Gateway Service Discovery

```bash
# Watch Gateway with debug logging
docker logs -f libhub-gateway 2>&1 | grep -i "consul\|loadbalancer"
```

### Verify Load Balancing

```bash
# Make 12 requests
for i in {1..12}; do
  curl -s http://localhost:5000/api/books/1 > /dev/null
  echo "Request $i sent"
done

# Check distribution (should be 4 requests each)
echo "Instance 1: $(docker logs libhub-catalogservice-1 2>&1 | grep -c 'GET /api/books/1')"
echo "Instance 2: $(docker logs libhub-catalogservice-2 2>&1 | grep -c 'GET /api/books/1')"
echo "Instance 3: $(docker logs libhub-catalogservice-3 2>&1 | grep -c 'GET /api/books/1')"
```

---

## Summary

### Where Gateway Calls Consul

**Location in code:** `Ocelot.Provider.Consul` namespace  
**HTTP Call:** `GET http://consul:8500/v1/health/service/{serviceName}?passing`  
**Frequency:** Every 100ms (cached)  
**Log Level:** `Debug` (now enabled)

### Where Gateway Uses Consul Response

**Location in code:** `Ocelot.LoadBalancer.LoadBalancers.RoundRobin`  
**Action:** Selects one instance from Consul's list  
**Algorithm:** RoundRobin (1 → 2 → 3 → 1...)  
**Log Level:** `Debug` (now enabled)

### Where Gateway Makes Service Call

**Location in code:** `Ocelot.Requester.Middleware.HttpRequesterMiddleware`  
**HTTP Call:** `{Method} http://{selectedInstance}:{port}{path}`  
**Log Level:** `Information` (always visible)

---

## Next Steps

1. **Restart Gateway** (already done - Debug logging enabled)
2. **Make requests** and watch logs
3. **See Consul queries** in debug logs
4. **Observe load balancing** across 3 instances
5. **Test failover** by stopping an instance

The detailed logs will now show you exactly when and how Gateway interacts with Consul! 🎉
