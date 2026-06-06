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
  await page.getByRole('textbox', { name: 'Password', exact: true }).fill('Secret123!');
  await page.getByRole('textbox', { name: 'Confirm Password' }).fill('Secret123!');
  await page.getByRole('checkbox').check();
  await page.getByRole('button', { name: 'Sign Up' }).click();

  await expect(page).toHaveURL('/');
  await expect(page.getByRole('heading', { name: 'Active Auctions' })).toBeVisible();
});

test('register surfaces duplicate email errors and unlocks the form', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state);
  await page.route('**/api/auth/register', async (route) => {
    await route.fulfill({
      status: 400,
      contentType: 'application/json',
      body: JSON.stringify({
        details: 'A user with this email address is already registered.'
      })
    });
  });

  await page.goto('/register');
  await page.getByLabel('Full Name').fill('Pat Collector');
  await page.getByLabel('Email').fill('collector@example.com');
  await page.getByRole('textbox', { name: 'Password', exact: true }).fill('Secret123!');
  await page.getByRole('textbox', { name: 'Confirm Password' }).fill('Secret123!');
  await page.getByRole('checkbox').check();
  await page.getByRole('button', { name: 'Sign Up' }).click();

  await expect(page.getByText('A user with this email address is already registered.')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Sign Up' })).toBeEnabled();
});

test('register form can show and hide password fields', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state);

  await page.goto('/register');

  const password = page.getByRole('textbox', { name: 'Password', exact: true });
  const confirmPassword = page.getByRole('textbox', { name: 'Confirm Password' });

  await password.fill('Secret123!');
  await confirmPassword.fill('Secret123!');

  await expect(password).toHaveAttribute('type', 'password');
  await expect(confirmPassword).toHaveAttribute('type', 'password');

  await page.getByRole('button', { name: 'Show password' }).click();
  await page.getByRole('button', { name: 'Show confirm password' }).click();

  await expect(password).toHaveAttribute('type', 'text');
  await expect(confirmPassword).toHaveAttribute('type', 'text');

  await page.getByRole('button', { name: 'Hide password' }).click();
  await page.getByRole('button', { name: 'Hide confirm password' }).click();

  await expect(password).toHaveAttribute('type', 'password');
  await expect(confirmPassword).toHaveAttribute('type', 'password');
});

test('reset password form can show and hide password fields', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state);

  await page.goto('/reset-password?token=test-token');

  const newPassword = page.getByRole('textbox', { name: 'New Password', exact: true });
  const confirmPassword = page.getByRole('textbox', { name: 'Confirm New Password' });

  await newPassword.fill('Secret123!');
  await confirmPassword.fill('Secret123!');

  await expect(newPassword).toHaveAttribute('type', 'password');
  await expect(confirmPassword).toHaveAttribute('type', 'password');

  await page.getByRole('button', { name: 'Show new password' }).click();
  await page.getByRole('button', { name: 'Show confirm new password' }).click();

  await expect(newPassword).toHaveAttribute('type', 'text');
  await expect(confirmPassword).toHaveAttribute('type', 'text');

  await page.getByRole('button', { name: 'Hide new password' }).click();
  await page.getByRole('button', { name: 'Hide confirm new password' }).click();

  await expect(newPassword).toHaveAttribute('type', 'password');
  await expect(confirmPassword).toHaveAttribute('type', 'password');
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
