# Task 5.2: Configure Ocelot Routing

**Phase**: 5 - API Gateway Implementation  
**Estimated Time**: 1.5 hours  
**Dependencies**: Task 5.1 (Ocelot setup)

---

## Objective

Configure ocelot.json with routes for UserService, CatalogService, and LoanService.

---

## Key Configuration

### ocelot.json Structure

{
"Routes": [
{
"UpstreamPathTemplate": "/api/users/{everything}",
"UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete" ],
"DownstreamPathTemplate": "/api/users/{everything}",
"DownstreamScheme": "http",
"DownstreamHostAndPorts": [
{ "Host": "localhost", "Port": 5002 }
],
"ServiceName": "UserService"
},
{
"UpstreamPathTemplate": "/api/books/{everything}",
"UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete" ],
"DownstreamPathTemplate": "/api/books/{everything}",
"DownstreamScheme": "http",
"DownstreamHostAndPorts": [
{ "Host": "localhost", "Port": 5001 }
],
"ServiceName": "CatalogService"
},
{
"UpstreamPathTemplate": "/api/loans/{everything}",
"UpstreamHttpMethod": [ "Get", "Post", "Put", "Delete" ],
"DownstreamPathTemplate": "/api/loans/{everything}",
"DownstreamScheme": "http",
"DownstreamHostAndPorts": [
{ "Host": "localhost", "Port": 5003 }
],
"ServiceName": "LoanService"
}
],
"GlobalConfiguration": {
"BaseUrl": "http://localhost:5000"
}
}

text

---

## Routing Concepts

- **Upstream**: What client calls (Gateway URL)
- **Downstream**: Where Gateway forwards request (Service URL)
- **{everything}**: Catch-all parameter - matches any path after base

---

## Testing

Start all services first
Terminal 1: UserService on 5002
Terminal 2: CatalogService on 5001
Terminal 3: LoanService on 5003
Terminal 4: Gateway on 5000
Test routing
curl http://localhost:5000/api/books # Should hit CatalogService
curl http://localhost:5000/api/users/1 # Should hit UserService

text

---

## After Completion

Update **PROJECT_STATUS.md**:
✅ Task 5.2: Ocelot Routing Configuration (date)

Routes configured for all 3 services

Verified routing with curl tests

text

**Next: Task 5.3** (Add JWT authentication middleware)