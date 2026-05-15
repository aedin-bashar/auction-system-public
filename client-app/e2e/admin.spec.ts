import { expect, test } from '@playwright/test';

import { adminSession, createMockState, ids, setupMockApp } from './helpers/mock-app';

test('admin dashboard, reports, and settings load and submit correctly', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, { session: adminSession });

  await page.goto('/admin/dashboard');
  await expect(page.getByRole('heading', { name: 'Dashboard Overview' })).toBeVisible();
  await expect(page.getByText('Recent Activity')).toBeVisible();

  await page.goto('/admin/reports');
  await expect(page.getByRole('heading', { name: 'Reports' })).toBeVisible();
  const reportRequest = page.waitForRequest((request) => request.method() === 'POST' && request.url().endsWith('/api/admin/reports/generate'));
  await page.getByLabel('Range').selectOption({ index: 1 });
  await reportRequest;

  await page.goto('/admin/settings');
  await expect(page.getByRole('heading', { name: 'System Settings' })).toBeVisible();
  await page.getByLabel('Maintenance Mode').check();
  await page.getByLabel('Min Bid Increment ($)').fill('2');
  await page.getByRole('button', { name: 'Save Settings' }).click();
  await expect(page.getByText(/^Saved /)).toBeVisible();
});

test('admin auction filters narrow list results by status and search text', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, { session: adminSession });

  await page.goto('/admin/auctions');
  await expect(page.getByRole('heading', { name: 'Auction Management' })).toBeVisible();
  await expect(page.locator('.admin-auctions__row')).toHaveCount(2);

  await page.getByRole('button', { name: 'Draft' }).click();
  await expect(page.locator('.admin-auctions__row')).toHaveCount(1);
  await expect(page.getByText('Retro Game Console')).toBeVisible();

  await page.getByLabel('Search auctions by title or seller').fill('Vintage');
  await expect(page.getByText('No auctions match your filters.')).toBeVisible();

  await page.getByRole('button', { name: 'All' }).click();
  await expect(page.locator('.admin-auctions__row')).toHaveCount(1);
  await expect(page.getByText('Vintage Camera Lot')).toBeVisible();
});

test('admin can edit and delete users and manage auction detail flows', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, { session: adminSession });

  await page.goto('/admin/users');
  await expect(page.getByRole('heading', { name: 'User Management' })).toBeVisible();
  await page.getByLabel('Search users by name or email').fill('Managed User');

  await page.getByRole('button', { name: 'Edit user' }).click();
  let dialog = page.getByRole('dialog', { name: 'Edit User' });
  await dialog.getByLabel('Full Name').fill('Managed Bidder');
  await dialog.getByRole('button', { name: 'Save' }).click();
  await page.getByLabel('Search users by name or email').fill('Managed Bidder');
  await expect(page.locator('.admin-users__name', { hasText: 'Managed Bidder' })).toBeVisible();

  await page.getByRole('button', { name: 'Delete user' }).click();
  dialog = page.getByRole('dialog', { name: 'Delete User' });
  await dialog.getByRole('button', { name: 'Delete User' }).click();
  await expect(page.locator('.admin-users__row').filter({ hasText: 'Managed Bidder' })).toHaveCount(0);

  await page.goto('/admin/auctions');
  await expect(page.getByRole('heading', { name: 'Auction Management' })).toBeVisible();
  await page.getByLabel('Search auctions by title or seller').fill('Vintage Camera');
  await page.getByRole('link', { name: 'View auction details' }).click();

  await expect(page).toHaveURL(`/admin/auctions/${ids.cameraAuction}`);
  await expect(page.getByRole('heading', { name: 'Vintage Camera Lot' })).toBeVisible();

  const saveRequest = page.waitForRequest((request) => request.method() === 'PUT' && request.url().endsWith(`/api/admin/auctions/${ids.cameraAuction}`));
  await page.getByLabel('Title').fill('Vintage Camera Lot Updated');
  await page.getByRole('button', { name: 'Save Changes' }).click();
  await saveRequest;

  const endRequest = page.waitForRequest((request) => request.method() === 'POST' && request.url().endsWith(`/api/admin/auctions/${ids.cameraAuction}/end`));
  await page.getByRole('button', { name: 'End Auction' }).click();
  await endRequest;
});

test('admin auction detail handles missing records', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, { session: adminSession });

  await page.goto('/admin/auctions/99999999-9999-4999-8999-999999999999');

  await expect(page.getByRole('heading', { name: 'Auction Not Found' })).toBeVisible();
  await expect(page.getByText('Could not load auction detail.')).toBeVisible();
});

test('admin can refund transactions and resolve flagged cases', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, { session: adminSession });

  await page.goto('/admin/transactions');
  await expect(page.getByRole('heading', { name: 'Transaction Management' })).toBeVisible();
  await page.getByRole('button', { name: ids.managedTransaction }).click();
  await page.getByRole('button', { name: 'Refund transaction' }).click();

  let dialog = page.getByRole('dialog', { name: 'Process Refund' });
  await dialog.getByLabel('Refund reason').fill('Duplicate authorization hold');
  await dialog.getByRole('button', { name: 'Confirm Refund' }).click();
  await expect(page.locator('.admin-transactions__status-pill', { hasText: 'Refunded' })).toBeVisible();

  await page.goto('/admin/flagged-cases');
  await expect(page.getByRole('heading', { name: 'Flagged Cases' })).toBeVisible();
  await page.getByRole('checkbox', { name: 'Include resolved cases' }).check();
  await page.getByRole('button', { name: /Vintage Camera Lot/i }).click();
  await page.getByLabel('Resolution Note').fill('Reviewed evidence and removed the listing from review queue.');
  await page.getByRole('button', { name: 'Resolve Case' }).click();
  await expect(page.getByText('Resolved by Ada Admin.')).toBeVisible();
});

test('admin can delete auctions from the list and delete transaction records', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, { session: adminSession });

  await page.goto('/admin/auctions');
  const consoleRow = page.locator('.admin-auctions__row').filter({ hasText: 'Retro Game Console' });
  await consoleRow.getByRole('button', { name: 'Delete auction' }).click();

  let dialog = page.getByRole('dialog', { name: 'Delete Auction' });
  await dialog.getByRole('button', { name: 'Delete Auction' }).click();
  await expect(page.locator('.admin-auctions__row').filter({ hasText: 'Retro Game Console' })).toHaveCount(0);

  await page.goto('/admin/transactions');
  await expect(page.getByRole('heading', { name: 'Transaction Management' })).toBeVisible();
  await expect(page.getByLabel('Description')).toHaveValue('Hold for camera auction');
  await page.getByRole('button', { name: ids.managedTransaction }).click();
  await page.getByLabel('Description').fill('Updated from Playwright');
  await page.getByRole('button', { name: 'Save transaction edit' }).click();
  await expect(page.getByLabel('Description')).toHaveValue('Updated from Playwright');

  await page.getByRole('button', { name: 'Delete transaction' }).click();
  dialog = page.getByRole('dialog', { name: 'Delete Transaction' });
  await dialog.getByRole('button', { name: 'Delete Transaction' }).click();

  await expect(page.getByText('No transactions available.')).toBeVisible();
});

test('admin can start a draft auction from the detail page', async ({ page }) => {
  const state = createMockState();
  state.adminAuctionDetails[ids.consoleAuction] = {
    auctionId: ids.consoleAuction,
    title: 'Retro Game Console',
    sellerId: ids.seller,
    sellerName: 'Sam Seller',
    category: 'Gaming',
    description: 'Boxed console with two controllers.',
    startingPriceAmount: 90,
    currency: 'USD',
    currentBidAmount: 90,
    bidCount: 0,
    startTimeUtc: null,
    endTimeUtc: '2027-06-10T16:30:00Z',
    endedAtUtc: null,
    status: 'Draft',
    highestBidderName: null,
    primaryImageId: null,
    imageCount: 0,
    bids: []
  };
  await setupMockApp(page, state, { session: adminSession });

  await page.goto(`/admin/auctions/${ids.consoleAuction}`);
  await expect(page.getByRole('heading', { name: 'Retro Game Console' })).toBeVisible();

  const startRequest = page.waitForRequest((request) => request.method() === 'POST' && request.url().endsWith(`/api/admin/auctions/${ids.consoleAuction}/start`));
  await page.getByRole('button', { name: 'Start Auction' }).click();
  await startRequest;

  await expect(page.locator('.admin-stat-card').filter({ hasText: 'Status' }).getByText('Active')).toBeVisible();
});
