# Task 7.4: Security Testing

**Phase**: 7 - System Integration & Testing  
**Type**: Security Audit  
**Estimated Time**: 3-4 hours  
**Dependencies**: Task 7.3 complete

---

## Objective

Verify security controls protect the system from common vulnerabilities.

---

## Security Test Categories

1. Authentication & Authorization (JWT, RBAC)
2. Password Security (BCrypt, validation)
3. SQL Injection Prevention
4. Cross-Site Scripting (XSS)
5. Sensitive Data Exposure

---

## Test Checklist

### JWT Token Security
- [ ] Token expires after 1 hour
- [ ] Expired tokens rejected (401)
- [ ] Invalid tokens rejected (401)
- [ ] Token signature validated
- [ ] Token includes correct claims

**Test**:
Use expired token
curl http://localhost:5000/api/users/me
-H "Authorization: Bearer EXPIRED_TOKEN"

Expected: 401 Unauthorized
text

---

### RBAC Enforcement
- [ ] Customer cannot access admin endpoints
- [ ] Admin can access all endpoints
- [ ] Unauthenticated users can browse books
- [ ] Protected endpoints require authentication

**Test**:
Customer tries admin endpoint
curl -X POST http://localhost:5000/api/books
-H "Authorization: Bearer CUSTOMER_TOKEN"

Expected: 403 Forbidden
text

---

### Password Security
- [ ] Passwords hashed with BCrypt (work factor 11)
- [ ] Plain passwords never stored
- [ ] Password validation enforced (8 chars, uppercase, lowercase, digit, special)

**Verify**:
SELECT HashedPassword FROM user_db.Users LIMIT 1;
-- Should start with "$2a$11$" or "$2b$11$"

text

---

### SQL Injection Prevention
- [ ] Search with `' OR '1'='1` doesn't return all books
- [ ] Email with `admin'--` doesn't bypass authentication
- [ ] EF Core parameterization prevents injection

**Test**:
curl "http://localhost:5000/api/books?search=' OR '1'='1"

Should return empty or error, not all books
text

---

### XSS Prevention
- [ ] Input sanitization implemented
- [ ] HTML encoding in frontend
- [ ] Script tags rejected

**Test**: Try registering with username `<script>alert('XSS')</script>`

---

### Sensitive Data Exposure
- [ ] JWT secret not hardcoded
- [ ] Database passwords in config files
- [ ] HashedPassword never returned in API responses
- [ ] Error messages don't expose system details

**Verify**:
curl http://localhost:5000/api/users/1

Response should NOT include HashedPassword field
text

---

## AI Agent Task

**Generate security test checklist**:

**Prompt**:
Based on SECURITY_TEST_CHECKLIST.md, generate a comprehensive
security testing script with all test cases for JWT, RBAC,
password security, and SQL injection. Include verification commands.

text

---

## Optional: Vulnerability Scanning

**Tool**: OWASP ZAP or Burp Suite

**Scan targets**:
- http://localhost:5000/api/users/login
- http://localhost:5000/api/books
- http://localhost:5000/api/loans

---

## Security Test Results

| Test | Result | Issues | Notes |
|------|--------|--------|-------|
| JWT expiration | ✅ Pass | None | Tokens expire correctly |
| RBAC | ✅ Pass | None | Admin endpoints protected |
| BCrypt | ✅ Pass | None | Work factor 11 |
| SQL injection | ✅ Pass | None | EF Core parameterization |
| Password in API | ✅ Pass | None | Never exposed |

---

## Acceptance Criteria

- [ ] All JWT tests pass
- [ ] RBAC enforced correctly
- [ ] Passwords properly hashed
- [ ] SQL injection prevented
- [ ] No sensitive data exposed
- [ ] No critical security vulnerabilities

---

## After Completion

Update **PROJECT_STATUS.md**:
✅ Task 7.4: Security Testing (date)

All security tests passed

0 critical vulnerabilities

JWT and RBAC working correctly

System ready for deployment

text

**Next**: Task 7.5 (Resilience Testing - final task!)