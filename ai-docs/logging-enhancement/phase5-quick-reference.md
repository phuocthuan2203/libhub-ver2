# Phase 5: Consul Service Discovery - Quick Reference

**Feature:** Dynamic inter-service communication via Consul  
**Date:** November 9, 2025

---

## What Changed

### Before
```csharp
// Hardcoded URL
client.BaseAddress = new Uri("http://localhost:5001");
```

### After
```csharp
// Dynamic discovery via Consul
var url = await _serviceDiscovery.GetServiceUrlAsync("catalogservice");
var response = await _httpClient.GetAsync($"{url}/api/books/{id}");
```

---

## Files Created

1. **IServiceDiscovery.cs** - Service discovery interface
2. **ConsulServiceDiscovery.cs** - Consul implementation with logging

## Files Modified

1. **CatalogServiceClient.cs** - Uses dynamic discovery
2. **Program.cs** - Registers service discovery
3. **appsettings.json** - Removed hardcoded URL

---

## Log Tags

| Emoji | Tag | Meaning |
|-------|-----|---------|
| 🔍 | `[SERVICE-DISCOVERY]` | Querying Consul |
| ✅ | `[SERVICE-DISCOVERY]` | Service found |
| ❌ | `[SERVICE-DISCOVERY]` | Service not found |
| 🔗 | `[INTER-SERVICE]` | Calling downstream service |
| 📨 | `[INTER-SERVICE]` | Response received |

---

## Quick Test

```bash
# 1. Get token
TOKEN=$(curl -s -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin123!"}' \
  | grep -o '"token":"[^"]*' | sed 's/"token":"//')

# 2. Borrow book (triggers service discovery)
curl -X POST http://localhost:5000/api/loans \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"bookId": 1}'

# 3. View logs
docker logs libhub-loanservice-1 --tail 20 | grep "SERVICE-DISCOVERY\|INTER-SERVICE"
```

---

## Expected Log Output

```
🔍 [SERVICE-DISCOVERY] Querying Consul for service: catalogservice
✅ [SERVICE-DISCOVERY] Discovered service: catalogservice at http://catalogservice:5001 | ServiceId: catalogservice-xxx
🔗 [INTER-SERVICE] Calling CatalogService at http://catalogservice:5001: GET /api/books/1
📨 [INTER-SERVICE] CatalogService response: 200 for GET /api/books/1
```

---

## Seq Filters

**All service discovery:**
```
@Message like '%SERVICE-DISCOVERY%'
```

**Inter-service calls:**
```
@Message like '%INTER-SERVICE%'
```

**Errors only:**
```
@Level = 'Error' and ServiceName = 'LoanService'
```

**Specific correlation:**
```
CorrelationId = 'YOUR_ID_HERE'
```

---

## Verification Checklist

- [ ] Services registered in Consul (http://localhost:8500)
- [ ] Service discovery logs in console
- [ ] Service discovery logs in Seq (http://localhost:5341)
- [ ] Loan creation works successfully
- [ ] Error handling works when service is down
- [ ] No hardcoded URLs in logs

---

## Common Commands

**Check Consul services:**
```bash
curl http://localhost:8500/v1/catalog/services
```

**Check service health:**
```bash
curl http://localhost:8500/v1/health/service/catalogservice
```

**Watch logs:**
```bash
docker logs -f libhub-loanservice-1
```

**Restart service:**
```bash
docker compose restart catalogservice
```

---

## Benefits

✅ No hardcoded URLs  
✅ Dynamic service resolution  
✅ Fault tolerant  
✅ Ready for scaling  
✅ Full observability  

---

## Implementation Summary

```
ConsulServiceDiscovery
    ↓ (queries)
Consul Health API
    ↓ (returns)
http://catalogservice:5001
    ↓ (used by)
CatalogServiceClient
    ↓ (makes)
HTTP GET/PUT to CatalogService
```

---

**Status:** ✅ Complete and tested
