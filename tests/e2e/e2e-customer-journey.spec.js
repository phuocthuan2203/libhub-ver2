const { test, expect } = require('@playwright/test');
const mysql = require('mysql2/promise');

const BASE_URL = 'http://localhost:4200';
const API_URL = 'http://localhost:5000';

let dbConnection;

test.describe('E2E Scenario 1: Customer Happy Path', () => {
  let testUserId;
  let testBookId;
  let testLoanId;
  let authToken;

  test.beforeAll(async () => {
    dbConnection = await mysql.createConnection({
      host: 'localhost',
      user: 'libhub_user',
      password: 'libhub_password',
      multipleStatements: true
    });
  });

  test.afterAll(async () => {
    if (dbConnection) {
      await dbConnection.end();
    }
  });

  test('Step 1: Register new user', async ({ page }) => {
    await page.goto(`${BASE_URL}/register.html`);
    
    await page.fill('#username', 'e2etest');
    await page.fill('#email', 'e2e@test.com');
    await page.fill('#password', 'Test@1234');
    await page.fill('#confirmPassword', 'Test@1234');
    
    await page.click('button[type="submit"]');
    
    await page.waitForURL(`${BASE_URL}/login.html`, { timeout: 5000 });
    
    const [rows] = await dbConnection.execute(
      'SELECT UserId FROM user_db.Users WHERE Email = ?',
      ['e2e@test.com']
    );
    expect(rows.length).toBe(1);
    testUserId = rows[0].UserId;
    console.log(`✅ User created with ID: ${testUserId}`);
  });

  test('Step 2: Login with credentials', async ({ page }) => {
    await page.goto(`${BASE_URL}/login.html`);
    
    await page.fill('#email', 'e2e@test.com');
    await page.fill('#password', 'Test@1234');
    
    await page.click('button[type="submit"]');
    
    await page.waitForURL(`${BASE_URL}/index.html`, { timeout: 5000 });
    
    authToken = await page.evaluate(() => localStorage.getItem('token'));
    expect(authToken).toBeTruthy();
    console.log('✅ Login successful, JWT token received');
  });

  test('Step 3: Browse books and search "Fiction"', async ({ page }) => {
    await page.goto(`${BASE_URL}/index.html`);
    
    await page.fill('#searchInput', 'Fiction');
    await page.click('button:has-text("Search")');
    
    await page.waitForSelector('.book-card', { timeout: 5000 });
    
    const bookCards = await page.locator('.book-card').count();
    expect(bookCards).toBeGreaterThan(0);
    console.log(`✅ Found ${bookCards} Fiction books`);
  });

  test('Step 4: Click book and view details', async ({ page }) => {
    await page.goto(`${BASE_URL}/index.html`);
    
    const firstBook = page.locator('.book-card').first();
    const bookTitle = await firstBook.locator('h3').textContent();
    
    await firstBook.click();
    
    await page.waitForURL(/book-detail\.html\?id=\d+/, { timeout: 5000 });
    
    const detailTitle = await page.locator('h1').textContent();
    expect(detailTitle).toBe(bookTitle);
    
    const url = page.url();
    testBookId = new URL(url).searchParams.get('id');
    console.log(`✅ Viewing book details for BookId: ${testBookId}`);
  });

  test('Step 5: Borrow book', async ({ page }) => {
    await page.goto(`${BASE_URL}/book-detail.html?id=${testBookId}`);
    
    await page.evaluate((token) => {
      localStorage.setItem('token', token);
    }, authToken);
    await page.reload();
    
    const [beforeStock] = await dbConnection.execute(
      'SELECT AvailableCopies FROM catalog_db.Books WHERE BookId = ?',
      [testBookId]
    );
    const stockBefore = beforeStock[0].AvailableCopies;
    
    await page.click('button:has-text("Borrow Book")');
    
    await page.waitForURL(`${BASE_URL}/my-loans.html`, { timeout: 5000 });
    
    await page.waitForTimeout(1000);
    
    const [loanRows] = await dbConnection.execute(
      'SELECT LoanId, Status FROM loan_db.Loans WHERE UserId = ? AND BookId = ? ORDER BY LoanId DESC LIMIT 1',
      [testUserId, testBookId]
    );
    expect(loanRows.length).toBe(1);
    expect(loanRows[0].Status).toBe('CheckedOut');
    testLoanId = loanRows[0].LoanId;
    
    const [afterStock] = await dbConnection.execute(
      'SELECT AvailableCopies FROM catalog_db.Books WHERE BookId = ?',
      [testBookId]
    );
    const stockAfter = afterStock[0].AvailableCopies;
    
    expect(stockAfter).toBe(stockBefore - 1);
    console.log(`✅ Loan created (ID: ${testLoanId}), stock decremented: ${stockBefore} → ${stockAfter}`);
  });

  test('Step 6: Navigate to My Loans and verify loan appears', async ({ page }) => {
    await page.goto(`${BASE_URL}/my-loans.html`);
    
    await page.evaluate((token) => {
      localStorage.setItem('token', token);
    }, authToken);
    await page.reload();
    
    await page.waitForSelector('.loan-card', { timeout: 5000 });
    
    const loanCard = page.locator('.loan-card').first();
    const status = await loanCard.locator('.status').textContent();
    
    expect(status).toContain('CheckedOut');
    console.log('✅ Loan appears in My Loans with status: CheckedOut');
  });

  test('Step 7: Return book', async ({ page }) => {
    await page.goto(`${BASE_URL}/my-loans.html`);
    
    await page.evaluate((token) => {
      localStorage.setItem('token', token);
    }, authToken);
    await page.reload();
    
    const [beforeStock] = await dbConnection.execute(
      'SELECT AvailableCopies FROM catalog_db.Books WHERE BookId = ?',
      [testBookId]
    );
    const stockBefore = beforeStock[0].AvailableCopies;
    
    const returnButton = page.locator('button:has-text("Return Book")').first();
    await returnButton.click();
    
    await page.waitForTimeout(1000);
    
    const [loanRows] = await dbConnection.execute(
      'SELECT Status, ReturnDate FROM loan_db.Loans WHERE LoanId = ?',
      [testLoanId]
    );
    expect(loanRows[0].Status).toBe('Returned');
    expect(loanRows[0].ReturnDate).not.toBeNull();
    
    const [afterStock] = await dbConnection.execute(
      'SELECT AvailableCopies FROM catalog_db.Books WHERE BookId = ?',
      [testBookId]
    );
    const stockAfter = afterStock[0].AvailableCopies;
    
    expect(stockAfter).toBe(stockBefore + 1);
    console.log(`✅ Book returned, stock incremented: ${stockBefore} → ${stockAfter}`);
  });

  test('Step 8: Verify loan status changed to Returned', async ({ page }) => {
    await page.goto(`${BASE_URL}/my-loans.html`);
    
    await page.evaluate((token) => {
      localStorage.setItem('token', token);
    }, authToken);
    await page.reload();
    
    await page.waitForSelector('.loan-card', { timeout: 5000 });
    
    const loanCard = page.locator('.loan-card').first();
    const status = await loanCard.locator('.status').textContent();
    
    expect(status).toContain('Returned');
    console.log('✅ Loan status updated to Returned in UI');
  });
});
