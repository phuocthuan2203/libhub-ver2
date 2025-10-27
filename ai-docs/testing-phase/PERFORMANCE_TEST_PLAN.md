# Performance Testing Plan

**Requirements**: SRS specifies 200 concurrent users, search <2s response time

---

## Tools

**Primary**: Apache JMeter or k6  
**Alternative**: Artillery, Gatling

---

## Test Scenarios

### Test 1: Search Endpoint Load

**Objective**: Verify search responds in <2 seconds under load

**Configuration**:
- Endpoint: GET /api/books?search=fiction
- Users: 200 concurrent
- Duration: 5 minutes
- Ramp-up: 30 seconds

**JMeter Test Plan**:
<ThreadGroup> <numThreads>200</numThreads> <rampUp>30</rampUp> <duration>300</duration> </ThreadGroup> ```
Success Criteria:

Average response time < 2000ms

95th percentile < 3000ms

Error rate < 1%

Test 2: Borrow Book Workflow
Objective: Test Saga pattern under concurrent load

Configuration:

Workflow: Login → Search → Borrow

Users: 50 concurrent

Duration: 3 minutes

k6 Script:

text
export default function() {
  // Login
  let loginRes = http.post('http://localhost:5000/api/users/login', payload);
  let token = loginRes.json('accessToken');
  
  // Borrow
  let borrowRes = http.post('http://localhost:5000/api/loans',
    JSON.stringify({bookId: 1}),
    {headers: {'Authorization': `Bearer ${token}`}}
  );
  
  check(borrowRes, {
    'borrow successful': (r) => r.status === 201,
  });
}
Success Criteria:

No database deadlocks

All Saga transactions complete successfully

No "CheckedOut" loans created when book unavailable

Test 3: Database Connection Pool
Objective: Verify connection pool handles load

Configuration:

Mixed requests across all endpoints

Users: 200 concurrent

Duration: 10 minutes

Monitor:

text
# MySQL connection count
mysql -e "SHOW STATUS LIKE 'Threads_connected';"
Success Criteria:

No "Too many connections" errors

Connection pool size adequate

Test 4: Gateway Throughput
Objective: Measure max requests/second

Configuration:

Endpoint: GET /api/books

Users: Gradually increase from 50 to 500

Measure throughput

Success Criteria:

Identify max RPS before degradation

No 503 Service Unavailable errors

Metrics to Collect
Response time (avg, p95, p99)

Throughput (requests/second)

Error rate (%)

Database connection count

CPU and memory usage

Network latency

Performance Baselines
Metric	Target	Measured	Status
Search response time	<2s	TBD	Pending
Concurrent users	200	TBD	Pending
Borrow success rate	>99%	TBD	Pending
Error rate	<1%	TBD	Pending
Performance Issues Resolution
If response time > 2s:

Add database indexes on searched fields

Implement caching (Redis)

Optimize SQL queries

If error rate > 1%:

Increase connection pool size

Add retry logic with exponential backoff

Scale services horizontally

Load Test Execution
text
# Install JMeter
wget https://dlcdn.apache.org//jmeter/binaries/apache-jmeter-5.6.3.tgz
tar -xzf apache-jmeter-5.6.3.tgz

# Run test
./jmeter -n -t libhub-load-test.jmx -l results.jtl

# Generate report
./jmeter -g results.jtl -o report/
text

***

## **SECURITY_TEST_CHECKLIST.md**

```markdown
# Security Testing Checklist

---

## Authentication & Authorization

### JWT Token Security
- [ ] Token has expiration (1 hour)
- [ ] Token signed with strong secret key (256-bit)
- [ ] Token includes user ID, role, email in claims
- [ ] Expired tokens rejected with 401
- [ ] Invalid tokens rejected with 401
- [ ] Token validated at Gateway level

**Test**:
Use expired token
curl http://localhost:5000/api/users/me
-H "Authorization: Bearer <EXPIRED_TOKEN>"

Expected: 401 Unauthorized
text

---

### Role-Based Access Control (RBAC)
- [ ] Customer cannot access admin endpoints
- [ ] Admin can access all endpoints
- [ ] Unauthenticated users can browse books
- [ ] Protected endpoints require authentication

**Test**:
Customer tries admin endpoint
curl -X POST http://localhost:5000/api/books
-H "Authorization: Bearer <CUSTOMER_TOKEN>"

Expected: 403 Forbidden
text

---

## Password Security

### Password Hashing
- [ ] Passwords hashed with BCrypt
- [ ] Work factor = 11 (or higher)
- [ ] Plain passwords never stored
- [ ] Passwords never logged

**Verification**:
SELECT HashedPassword FROM user_db.Users LIMIT 1;
text

---

### Password Validation
- [ ] Minimum 8 characters
- [ ] Requires uppercase letter
- [ ] Requires lowercase letter
- [ ] Requires digit
- [ ] Requires special character
- [ ] Client-side validation implemented
- [ ] Server-side validation implemented

**Test**:
curl -X POST http://localhost:5000/api/users/register
-d '{"username":"test","email":"test@test.com","password":"weak"}'

Expected: 400 Bad Request
text

---

## SQL Injection Prevention

### Test Endpoints with SQL Injection Payloads
- [ ] Search: `' OR '1'='1`
- [ ] Email: `admin'--`
- [ ] BookId: `1; DROP TABLE Books;--`

**Test**:
curl "http://localhost:5000/api/books?search=' OR '1'='1"

Expected: Should NOT return all books or cause error
text

**Verification**: EF Core parameterized queries prevent SQL injection

---

## Cross-Site Scripting (XSS)

### Test XSS in Input Fields
- [ ] Username: `<script>alert('XSS')</script>`
- [ ] Book title: `<img src=x onerror=alert('XSS')>`

**Mitigation**: HTML encoding in frontend

---

## CORS Configuration

- [ ] CORS allows frontend origin
- [ ] CORS does NOT allow all origins in production
- [ ] Preflight requests handled correctly

**Test**:
curl -X OPTIONS http://localhost:5000/api/books
-H "Origin: http://malicious-site.com"

Should handle gracefully
text

---

## Sensitive Data Exposure

- [ ] JWT secret not hardcoded in code
- [ ] Database password in config, not code
- [ ] Passwords never returned in API responses
- [ ] Error messages don't expose sensitive info

**Check**:
curl http://localhost:5000/api/users/1

Response should NOT include HashedPassword field
text

---

## API Rate Limiting (Optional)

- [ ] Gateway implements rate limiting
- [ ] 100 requests/minute per IP
- [ ] Returns 429 Too Many Requests

---

## HTTPS/TLS (Production)

- [ ] All communication over HTTPS
- [ ] Valid SSL certificate
- [ ] TLS 1.2 or higher
- [ ] HSTS header present

**Note**: Development uses HTTP (localhost:5000)

---

## Security Headers

- [ ] Content-Security-Policy
- [ ] X-Content-Type-Options: nosniff
- [ ] X-Frame-Options: DENY
- [ ] X-XSS-Protection: 1; mode=block

---

## Logging & Monitoring

- [ ] Failed login attempts logged
- [ ] Suspicious activity logged
- [ ] Logs don't contain passwords or tokens
- [ ] Security events alerting configured

---

## Vulnerability Scan

**Tool**: OWASP ZAP or Burp Suite

**Scan endpoints**:
- http://localhost:5000/api/users/login
- http://localhost:5000/api/books
- http://localhost:5000/api/loans

**Check for**:
- SQL injection
- XSS
- CSRF
- Insecure authentication
- Sensitive data exposure

---

## Security Test Results

| Test | Result | Severity | Notes |
|------|--------|----------|-------|
| JWT expiration | ✅ Pass | - | Tokens expire after 1 hour |
| RBAC enforcement | ✅ Pass | - | Admin endpoints protected |
| Password hashing | ✅ Pass | - | BCrypt work factor 11 |
| SQL injection | ✅ Pass | - | EF Core parameterization |

---

## Security Sign-off

**Tested by**: [Your Name]  
**Date**: [Date]  
**Critical Issues**: 0  
**High Issues**: 0  
**Medium Issues**: 0  
**Status**: ✅ APPROVED FOR DEPLOYMENT