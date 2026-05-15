import { expect, test } from '@playwright/test';

import { bidderSession, createMockState, ids, sellerSession, setupMockApp } from './helpers/mock-app';

test('guest marketplace actions redirect to login', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state);

  await page.goto('/');
  const cameraCard = page.locator('article.auction-card').filter({ has: page.getByRole('heading', { name: 'Vintage Camera Lot' }) });
  await cameraCard.getByRole('button', { name: 'Place Bid' }).click();

  await expect(page).toHaveURL(/\/login$/);
});

test('bidder can bid, report, and toggle watchlist from the home page', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, { session: bidderSession, watchlistIds: [ids.consoleAuction] });

  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Active Auctions' })).toBeVisible();

  const cameraCard = page.locator('article.auction-card').filter({ has: page.getByRole('heading', { name: 'Vintage Camera Lot' }) });
  await expect(cameraCard).toBeVisible();

  await cameraCard.getByTitle('Add to watchlist').click();
  await expect(cameraCard.getByTitle('Remove from watchlist')).toBeVisible();

  await cameraCard.getByRole('button', { name: 'Place Bid' }).click();
  const bidDialog = page.getByRole('dialog', { name: 'Place Your Bid' });
  await expect(bidDialog).toBeVisible();
  await bidDialog.getByLabel('Bid Amount (USD)').fill('151');
  await bidDialog.getByRole('button', { name: /^Place Bid$/ }).click();
  await expect(bidDialog).toBeHidden();
  await expect(cameraCard.getByText(/151(?:\.00)?\s+USD/)).toBeVisible();

  await cameraCard.getByRole('button', { name: 'Report listing' }).click();
  const reportDialog = page.getByRole('dialog', { name: 'Report Auction' });
  await expect(reportDialog).toBeVisible();
  await reportDialog.getByLabel('Details').fill('The certificate image does not match the serial number.');
  await reportDialog.getByRole('button', { name: 'Submit Report' }).click();

  await expect(page.getByText('Report submitted for Vintage Camera Lot. Our team will review it.')).toBeVisible();
});

test('bid modal blocks amounts below the current minimum before calling the API', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, { session: bidderSession });

  let bidRequestCount = 0;
  page.on('request', (request) => {
    if (request.method() === 'POST' && request.url().includes(`/api/auctions/${ids.cameraAuction}/bids`)) {
      bidRequestCount += 1;
    }
  });

  await page.goto('/');
  const cameraCard = page.locator('article.auction-card').filter({ has: page.getByRole('heading', { name: 'Vintage Camera Lot' }) });
  await cameraCard.getByRole('button', { name: 'Place Bid' }).click();

  const bidDialog = page.getByRole('dialog', { name: 'Place Your Bid' });
  await bidDialog.getByLabel('Bid Amount (USD)').fill('150');
  await bidDialog.getByRole('button', { name: /^Place Bid$/ }).click();

  await expect(bidDialog.getByText('Bid amount must be at least 151.00 USD.')).toBeVisible();
  expect(bidRequestCount).toBe(0);
});

test('seller can create a new auction from the frontend form', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, { session: sellerSession });

  await page.goto('/create');
  await expect(page.getByRole('heading', { name: 'Create Auction' })).toBeVisible();

  await page.getByLabel('Title').fill('Playwright Verified Lot');
  await page.getByLabel('Starting Price').fill('250');
  await page.getByLabel('Description').fill('A seller-created lot submitted through the Playwright suite.');
  await page.locator('#imageUpload').setInputFiles({
    name: 'lot.png',
    mimeType: 'image/png',
    buffer: Buffer.from('89504e470d0a1a0a', 'hex')
  });
  await page.locator('#endDateUtc').click();
  await page.locator('.flatpickr-day:not(.prevMonthDay):not(.nextMonthDay):not(.flatpickr-disabled)').first().click();

  const createRequest = page.waitForRequest((request) => request.method() === 'POST' && request.url().endsWith('/api/auctions'));
  await page.getByRole('button', { name: 'Create Auction' }).click();
  await createRequest;

  await expect(page).toHaveURL('/');
  await expect(page.getByRole('heading', { name: 'Active Auctions' })).toBeVisible();
});

test('create auction shows validation and backend errors without navigating away', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, { session: sellerSession });
  await page.route('**/api/auctions', async (route) => {
    if (route.request().method() === 'POST') {
      await route.fulfill({
        status: 400,
        contentType: 'application/json',
        body: JSON.stringify({ details: 'End date must be in the future.' })
      });
      return;
    }

    await route.fallback();
  });

  await page.goto('/create');
  await page.getByRole('button', { name: 'Create Auction' }).click();
  await expect(page.getByText('Title is required.')).toBeVisible();

  await page.getByLabel('Title').fill('Playwright Rejected Lot');
  await page.getByLabel('Starting Price').fill('250');
  await page.getByLabel('Description').fill('This submission is rejected by the mocked backend.');
  await page.locator('#endDateUtc').click();
  await page.locator('.flatpickr-day:not(.prevMonthDay):not(.nextMonthDay):not(.flatpickr-disabled)').first().click();
  await page.getByRole('button', { name: 'Create Auction' }).click();

  await expect(page.getByText('End date must be in the future.')).toBeVisible();
  await expect(page).toHaveURL('/create');
});

test('authenticated bidder can navigate my bids, watchlist, and won auctions', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, { session: bidderSession, watchlistIds: [ids.cameraAuction] });

  await page.goto('/my-bids');
  await expect(page.getByRole('heading', { name: 'My Bids' })).toBeVisible();
  await expect(page.getByText('You currently hold the top bid.')).toBeVisible();

  await page.goto('/watchlist');
  await expect(page.getByRole('heading', { name: 'Watchlist' })).toBeVisible();
  await expect(page.getByText('Vintage Camera Lot')).toBeVisible();
  await page.getByRole('button', { name: 'Remove from watchlist' }).click();
  await expect(page.getByText('No watched auctions yet. Use the heart button on auction cards to add items.')).toBeVisible();

  await page.goto('/won-auctions');
  await expect(page.getByRole('heading', { name: 'Won Auctions' })).toBeVisible();
  await expect(page.getByText('Payment pending. Complete checkout to finalize this win.')).toBeVisible();
});

test('home search, filters, clear, and pagination work together', async ({ page }) => {
  const state = createMockState();
  state.activeAuctions.push(
    createAuction('77777777-1111-4111-8111-111111111111', 'Comic Archive Volume 1', 'Collectibles', 120, 1),
    createAuction('77777777-2222-4222-8222-222222222222', 'Art Deco Lamp', 'Art', 80, 2),
    createAuction('77777777-3333-4333-8333-333333333333', 'Signed Baseball', 'Sports', 210, 3),
    createAuction('77777777-4444-4444-8444-444444444444', 'Luxury Watch', 'Luxury', 520, 4),
    createAuction('77777777-5555-4555-8555-555555555555', 'Vinyl Collector Box', 'Music', 140, 5),
    createAuction('77777777-6666-4666-8666-666666666666', 'Espresso Home Set', 'Home', 175, 6)
  );

  await setupMockApp(page, state, { session: bidderSession });

  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Active Auctions' })).toBeVisible();
  await expect(page.getByText('Page 1 of 2')).toBeVisible();
  await expect(page.locator('article.auction-card')).toHaveCount(6);

  await page.getByRole('button', { name: '2' }).click();
  await expect(page.getByText('Page 2 of 2')).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Espresso Home Set' })).toBeVisible();

  const searchInput = page.getByLabel('Search auctions');
  await searchInput.fill('Retro');
  await searchInput.press('Enter');
  await expect(page).toHaveURL(/search=Retro/);
  await expect(page.locator('article.auction-card')).toHaveCount(1);
  await expect(page.getByRole('heading', { name: 'Retro Game Console' })).toBeVisible();

  await searchInput.fill('');
  await searchInput.press('Enter');
  await expect(page).not.toHaveURL(/search=/);

  await page.getByLabel('Collectibles').check();
  await expect(page.getByRole('heading', { name: 'Vintage Camera Lot' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Comic Archive Volume 1' })).toBeVisible();
  await expect(page.locator('article.auction-card')).toHaveCount(2);

  await page.getByRole('spinbutton', { name: 'Min' }).fill('130');
  await expect(page.locator('article.auction-card')).toHaveCount(1);
  await expect(page.getByRole('heading', { name: 'Vintage Camera Lot' })).toBeVisible();

  await page.getByRole('button', { name: 'Clear' }).click();
  await expect(page.getByText('Page 1 of 2')).toBeVisible();
  await expect(page.locator('article.auction-card')).toHaveCount(6);
});

test('watchlist supports placing a bid from the bid modal flow', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, { session: bidderSession, watchlistIds: [ids.cameraAuction] });

  await page.goto('/watchlist');
  await expect(page.getByRole('heading', { name: 'Watchlist' })).toBeVisible();

  const watchCard = page.locator('.watch-card').filter({ has: page.getByRole('heading', { name: 'Vintage Camera Lot' }) });
  await watchCard.getByRole('button', { name: 'Place bid' }).click();

  const dialog = page.getByRole('dialog', { name: 'Place Your Bid' });
  await dialog.getByLabel('Bid Amount (USD)').fill('152');
  await dialog.getByRole('button', { name: /^Place Bid$/ }).click();

  await expect(dialog).toBeHidden();
  await expect(watchCard.getByText(/USD\s+152/)).toBeVisible();
  await expect(watchCard.getByText(/USD\s+153/)).toBeVisible();
});

function createAuction(id: string, title: string, category: string, priceAmount: number, dayOffset: number) {
  return {
    id,
    sellerId: ids.seller,
    title,
    category,
    description: `${title} description`,
    primaryImageId: null,
    priceAmount,
    currency: 'USD',
    endTimeUtc: new Date(Date.UTC(2027, 5, dayOffset + 10, 12, 0, 0)).toISOString(),
    bidCount: dayOffset
  };
}
