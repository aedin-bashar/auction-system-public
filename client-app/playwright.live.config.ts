import { defineConfig, devices } from '@playwright/test';

const baseURL = process.env.PLAYWRIGHT_LIVE_BASE_URL ?? 'http://127.0.0.1:4200';
const shouldStartFrontend = process.env.PLAYWRIGHT_LIVE_START_FRONTEND !== '0';

export default defineConfig({
  testDir: './e2e/live',
  fullyParallel: false,
  timeout: 60_000,
  expect: {
    timeout: 10_000
  },
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure'
  },
  projects: [
    {
      name: 'chromium-live',
      use: { ...devices['Desktop Chrome'] }
    }
  ],
  webServer: shouldStartFrontend
    ? {
        command: 'npm start -- --host 127.0.0.1 --port 4200',
        url: baseURL,
        reuseExistingServer: true,
        timeout: 120_000,
        cwd: __dirname
      }
    : undefined
});