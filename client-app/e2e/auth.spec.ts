import { expect, test } from '@playwright/test';

import { AUTH_STORAGE_KEY, adminSession, bidderSession, createMockState, setupMockApp } from './helpers/mock-app';

test('guest is redirected from protected routes to login', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state);

  await page.goto('/my-bids');

  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('heading', { name: 'Welcome Back' })).toBeVisible();
});

test('expired stored sessions are cleared and treated as signed out', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, {
    session: {
      ...bidderSession,
      expiresAtUtc: '2020-01-01T00:00:00Z'
    }
  });

  await page.goto('/my-bids');

  await expect(page).toHaveURL(/\/login$/);
  await expect.poll(() => page.evaluate((storageKey) => localStorage.getItem(storageKey), AUTH_STORAGE_KEY)).toBe(null);
});

test('bidder can sign in and land on the marketplace', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state);

  await page.goto('/login');
  await page.getByLabel('Email').fill(bidderSession.email);
  await page.getByLabel('Password').fill('Secret123!');
  await page.getByRole('button', { name: 'Sign In' }).click();

  await expect(page).toHaveURL('/');
  await expect(page.getByRole('heading', { name: 'Active Auctions' })).toBeVisible();
});

test('login surfaces backend credential errors', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state);
  await page.route('**/api/auth/login', async (route) => {
    await route.fulfill({
      status: 403,
      contentType: 'application/json',
      body: JSON.stringify({ details: 'Invalid credentials.' })
    });
  });

  await page.goto('/login');
  await page.getByLabel('Email').fill('wrong@example.com');
  await page.getByLabel('Password').fill('WrongPassword!');
  await page.getByRole('button', { name: 'Sign In' }).click();

  await expect(page.getByText('Invalid credentials.')).toBeVisible();
});

test('guest can register and is routed into the app', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state);

  await page.goto('/register');
  await page.getByLabel('Full Name').fill('Pat Collector');
  await page.getByLabel('Email').fill('collector@example.com');
  await page.getByLabel('Password', { exact: true }).fill('Secret123!');
  await page.getByLabel('Confirm Password').fill('Secret123!');
  await page.getByRole('checkbox').check();
  await page.getByRole('button', { name: 'Sign Up' }).click();

  await expect(page).toHaveURL('/');
  await expect(page.getByRole('heading', { name: 'Active Auctions' })).toBeVisible();
});

test('non-admin sessions are redirected away from admin routes', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, { session: bidderSession });

  await page.goto('/admin/dashboard');

  await expect(page).toHaveURL('/');
  await expect(page.getByRole('heading', { name: 'Active Auctions' })).toBeVisible();
});

test('admin login target is the dashboard', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state);

  await page.goto('/login');
  await page.getByLabel('Email').fill(adminSession.email);
  await page.getByLabel('Password').fill('Secret123!');
  await page.getByRole('button', { name: 'Sign In' }).click();

  await expect(page).toHaveURL('/admin/dashboard');
  await expect(page.getByRole('heading', { name: 'Dashboard Overview' })).toBeVisible();
});

test('authenticated user can log out from the user menu', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, { session: bidderSession });

  await page.goto('/');
  await page.getByRole('button', { name: 'User menu' }).click();
  await page.getByRole('menuitem', { name: 'Log Out' }).click();

  await expect(page.getByRole('link', { name: 'Sign up' })).toBeVisible();
  await expect.poll(() => page.evaluate((storageKey) => localStorage.getItem(storageKey), AUTH_STORAGE_KEY)).toBe(null);
});
