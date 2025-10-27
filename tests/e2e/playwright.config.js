const { defineConfig, devices } = require('@playwright/test');

module.exports = defineConfig({
  testDir: './',
  
  fullyParallel: false,
  
  forbidOnly: !!process.env.CI,
  
  retries: process.env.CI ? 2 : 0,
  
  workers: 1,
  
  reporter: [
    ['html', { outputFolder: 'playwright-report' }],
    ['list']
  ],
  
  use: {
    baseURL: 'http://localhost:4200',
    
    trace: 'on-first-retry',
    
    screenshot: 'only-on-failure',
    
    video: 'retain-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  webServer: {
    command: 'echo "Please ensure frontend is running on http://localhost:4200"',
    url: 'http://localhost:4200',
    reuseExistingServer: true,
    timeout: 5000,
  },
});
