import { expect, test } from '@playwright/test';

const liveAdminEmail = process.env.PLAYWRIGHT_LIVE_ADMIN_EMAIL;
const liveAdminPassword = process.env.PLAYWRIGHT_LIVE_ADMIN_PASSWORD;

test('live backend profile supports register, profile access, logout, and login', async ({ page }) => {
  const uniqueSuffix = `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
  const email = `playwright.${uniqueSuffix}@example.com`;
  const password = 'Secret123!';
  const fullName = `Playwright ${uniqueSuffix}`;

  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Active Auctions' })).toBeVisible();

  await page.goto('/register');
  await page.getByLabel('Full Name').fill(fullName);
  await page.getByLabel('Email').fill(email);
  await page.getByRole('textbox', { name: 'Password', exact: true }).fill(password);
  await page.getByRole('textbox', { name: 'Confirm Password' }).fill(password);
  await page.getByRole('checkbox').check();
  await page.getByRole('button', { name: 'Sign Up' }).click();

  await expect(page).toHaveURL('/');
  await page.goto('/profile');
  await expect(page.getByRole('heading', { name: fullName })).toBeVisible();

  await page.goto('/');
  await page.getByRole('button', { name: 'User menu' }).click();
  await page.getByRole('menuitem', { name: /Log Out/ }).click();
  await expect(page.getByRole('link', { name: 'Sign up' })).toBeVisible();

  await page.goto('/login');
  await page.getByLabel('Email').fill(email);
  await page.getByLabel('Password').fill(password);
  await page.getByRole('button', { name: 'Sign In' }).click();

  await expect(page).toHaveURL('/');
  await page.goto('/my-bids');
  await expect(page.getByRole('heading', { name: 'My Bids' })).toBeVisible();
});

test('live backend profile can run optional admin smoke with real credentials', async ({ page }) => {
  test.skip(!liveAdminEmail || !liveAdminPassword, 'Set PLAYWRIGHT_LIVE_ADMIN_EMAIL and PLAYWRIGHT_LIVE_ADMIN_PASSWORD to enable admin smoke.');

  await page.goto('/login');
  await page.getByLabel('Email').fill(liveAdminEmail!);
  await page.getByLabel('Password').fill(liveAdminPassword!);
  await page.getByRole('button', { name: 'Sign In' }).click();

  await expect(page).toHaveURL('/admin/dashboard');
  await expect(page.getByRole('heading', { name: 'Dashboard Overview' })).toBeVisible();
});
