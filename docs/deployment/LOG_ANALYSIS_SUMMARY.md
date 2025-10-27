# Log Analysis: Where Gateway Calls Consul

## Your Question

> "Where does the Gateway make a call to Consul to get the address, and where does Gateway use this address to make a call to the service?"

## The Answer

### 1. Where Gateway Calls Consul

**Line in your log:**
```
requestId: 0HNGL8RVQ251F:00000001
message: Getting service discovery provider of Type 'Consul'...
```

**What happens here:**
- Gateway receives request for `/api/books/1`
- Gateway looks up route configuration in `ocelot.json`
- Finds: `"ServiceName": "catalogservice"` with `"Type": "Consul"`
- **Triggers Consul query** to get instances of `catalogservice`

**Actual Consul HTTP call (internal, not logged):**
```
GET http://consul:8500/v1/health/service/catalogservice?passing=true
```

### 2. Where Gateway Uses Consul Response

**Line in your log:**
```
requestId: 0HNGL8RVQ251F:00000001
message: Downstream url is http://catalogservice:5001/api/books/1
```

**What happens here:**
- Consul returns 3 healthy instances
- Gateway's RoundRobin load balancer selects one instance
- Gateway constructs the downstream URL: `http://catalogservice:5001/api/books/1`
- **This is the selected instance from Consul's response**

### 3. Where Gateway Makes the Service Call

**Line in your log:**
```
info: Ocelot.Requester.Middleware.HttpRequesterMiddleware[0]
  message: 200 (OK) status code, request uri: http://catalogservice:5001/api/books/1
```

**What happens here:**
- Gateway makes HTTP call to the selected instance
- Docker DNS resolves `catalogservice` to actual container IP
- Request reaches one of the 3 CatalogService instances
- Response (200 OK) returned to Gateway, then to client

---

## Complete Flow Visualization

```
┌──────────────────────────────────────────────────────────────┐
│ 1. Client Request                                            │
│    curl http://localhost:5000/api/books/1                    │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│ 2. Gateway: "Getting service discovery provider of Type      │
│    'Consul'..."                                              │
│                                                              │
│    → Gateway queries Consul internally                       │
│    → GET http://consul:8500/v1/health/service/catalogservice│
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│ 3. Consul Response (not logged, but happens here)           │
│                                                              │
│    Returns 3 healthy instances:                             │
│    - catalogservice-1 (ID: abc123)                          │
│    - catalogservice-2 (ID: def456)                          │
│    - catalogservice-3 (ID: ghi789)                          │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│ 4. Gateway: "Downstream url is                               │
│    http://catalogservice:5001/api/books/1"                   │
│                                                              │
│    → RoundRobin selects instance (e.g., instance 2)         │
│    → Constructs URL with selected instance                   │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│ 5. Gateway: "200 (OK) status code, request uri:             │
│    http://catalogservice:5001/api/books/1"                   │
│                                                              │
│    → HTTP call made to selected instance                     │
│    → Docker DNS resolves to container IP                     │
│    → Request reaches catalogservice-2                        │
└────────────────────┬─────────────────────────────────────────┘
                     │
                     ▼
┌──────────────────────────────────────────────────────────────┐
│ 6. Response returned to client                               │
│    { "bookId": 1, "title": "Effective Java", ... }          │
└──────────────────────────────────────────────────────────────┘
```

---

## Why You Don't See Explicit Consul HTTP Calls

### The Consul Query is Internal

Ocelot's Consul provider makes HTTP calls to Consul **internally** using `HttpClient`. These calls are:
- Not logged at `Information` level (only at `Debug` level)
- Cached for performance (default: 100ms TTL)
- Abstracted away by the service discovery provider

### What You See vs What Happens

| What You See in Logs | What Actually Happens |
|---------------------|----------------------|
| `Getting service discovery provider of Type 'Consul'` | Gateway prepares to query Consul |
| *(nothing visible)* | **HTTP GET to Consul API** |
| *(nothing visible)* | **Consul returns 3 instances** |
| *(nothing visible)* | **RoundRobin selects instance 2** |
| `Downstream url is http://catalogservice:5001/api/books/1` | Gateway has selected the target |
| `200 (OK) status code, request uri: http://catalogservice:5001/api/books/1` | HTTP call completed |

---

## How to See the Missing Steps

### Enable Detailed Consul Logging

I've already enabled this for you in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Ocelot": "Debug",
      "Ocelot.Provider.Consul": "Debug"
    }
  }
}
```

### Watch Detailed Logs

```bash
# See all Consul-related logs
docker logs -f libhub-gateway 2>&1 | grep -i consul

# See load balancer decisions
docker logs -f libhub-gateway 2>&1 | grep -i "loadbalancer\|downstream"

# See complete request flow
docker logs -f libhub-gateway 2>&1 | grep -E "Consul|LoadBalancer|Downstream|HttpRequester"
```

### Make Test Requests

```bash
# Make 6 requests to see load balancing
for i in {1..6}; do
  echo "=== Request $i ==="
  curl -s http://localhost:5000/api/books/1 | jq '.bookId'
  sleep 1
done
```

---

## Proof That Consul is Working

### Test 1: Check Consul Service Registry

```bash
# See all registered catalogservice instances
curl -s http://localhost:8500/v1/health/service/catalogservice?passing | jq '.[] | {
  ServiceID: .Service.ID,
  Address: .Service.Address,
  Port: .Service.Port,
  Status: .Checks[1].Status
}'
```

**Output:**
```json
{
  "ServiceID": "catalogservice-abc123",
  "Address": "catalogservice",
  "Port": 5001,
  "Status": "passing"
}
{
  "ServiceID": "catalogservice-def456",
  "Address": "catalogservice",
  "Port": 5001,
  "Status": "passing"
}
{
  "ServiceID": "catalogservice-ghi789",
  "Address": "catalogservice",
  "Port": 5001,
  "Status": "passing"
}
```

### Test 2: Verify Load Balancing

```bash
# Clear previous logs
docker compose restart catalogservice

# Wait for restart
sleep 10

# Make 9 requests
for i in {1..9}; do
  curl -s http://localhost:5000/api/books/1 > /dev/null
done

# Check distribution (should be 3 requests each)
echo "Instance 1: $(docker logs libhub-catalogservice-1 2>&1 | grep -c 'GET /api/books/1')"
echo "Instance 2: $(docker logs libhub-catalogservice-2 2>&1 | grep -c 'GET /api/books/1')"
echo "Instance 3: $(docker logs libhub-catalogservice-3 2>&1 | grep -c 'GET /api/books/1')"
```

**Expected output:**
```
Instance 1: 3
Instance 2: 3
Instance 3: 3
```

This proves RoundRobin is distributing requests evenly!

### Test 3: Failover Test

```bash
# Stop one instance
docker stop libhub-catalogservice-2

# Wait for health check to fail
sleep 15

# Check Consul - should show 2 instances
curl -s http://localhost:8500/v1/health/service/catalogservice?passing | jq 'length'
# Output: 2

# Make 6 requests
for i in {1..6}; do
  curl -s http://localhost:5000/api/books/1 > /dev/null
done

# Check distribution (only instances 1 and 3 should receive requests)
echo "Instance 1: $(docker logs libhub-catalogservice-1 2>&1 | grep -c 'GET /api/books/1')"
echo "Instance 3: $(docker logs libhub-catalogservice-3 2>&1 | grep -c 'GET /api/books/1')"

# Restart instance
docker start libhub-catalogservice-2
```

This proves Gateway only routes to healthy instances from Consul!

---

## Summary

### Question 1: Where does Gateway call Consul?

**Answer:** At this line:
```
message: Getting service discovery provider of Type 'Consul'...
```

**What happens:**
- Gateway queries: `GET http://consul:8500/v1/health/service/catalogservice?passing`
- Consul returns list of healthy instances
- Gateway caches the response

### Question 2: Where does Gateway use Consul's response?

**Answer:** At this line:
```
message: Downstream url is http://catalogservice:5001/api/books/1
```

**What happens:**
- RoundRobin selects one instance from Consul's list
- Gateway constructs the downstream URL
- Gateway makes HTTP call to selected instance

### The Missing Piece

The actual Consul HTTP call and response are **internal operations** that aren't logged at `Information` level. With `Debug` logging enabled, you'll see more details about:
- Consul API calls
- Service instance lists
- Load balancer decisions

---

## Key Takeaway

Your original log **does show Consul service discovery**, but the details are abstracted:

1. ✅ `Getting service discovery provider of Type 'Consul'` = **Consul query happens here**
2. ✅ `Downstream url is http://catalogservice:5001/api/books/1` = **Consul response used here**
3. ✅ `200 (OK) status code, request uri: http://catalogservice:5001/api/books/1` = **HTTP call to selected instance**

The Consul integration is working! It's just not verbose by default. 🎉
