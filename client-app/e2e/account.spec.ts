import { expect, test } from '@playwright/test';

import { bidderSession, createMockState, setupMockApp } from './helpers/mock-app';

test('profile updates the user full name through the API flow', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, { session: bidderSession });

  await page.goto('/profile');
  await expect(page.getByRole('heading', { name: bidderSession.fullName })).toBeVisible();

  await page.getByRole('button', { name: 'Edit full name' }).click();
  const dialog = page.getByRole('dialog', { name: /edit full name/i });
  await dialog.locator('input').fill('Taylor Collector');
  await dialog.getByRole('button', { name: 'Save Changes' }).click();

  await expect(page.getByRole('heading', { name: 'Taylor Collector' })).toBeVisible();
});

test('settings can change the password through the modal workflow', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, { session: bidderSession });

  await page.goto('/settings');
  await expect(page.getByRole('heading', { name: 'Settings' })).toBeVisible();
  await page.getByRole('button', { name: 'Change Password' }).click();

  const dialog = page.getByRole('dialog', { name: 'Change Password' });
  await dialog.getByRole('textbox', { name: 'Current Password' }).fill('Secret123!');
  await dialog.getByRole('textbox', { name: 'New Password', exact: true }).fill('EvenMoreSecret123!');
  await dialog.getByRole('textbox', { name: 'Confirm New Password' }).fill('EvenMoreSecret123!');
  await dialog.getByRole('button', { name: 'Save Changes' }).click();

  await expect(dialog).toBeHidden();
  await expect(page.getByText('Last updated: Just now')).toBeVisible();
});

test('change password modal can show and hide password fields', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, { session: bidderSession });

  await page.goto('/settings');
  await page.getByRole('button', { name: 'Change Password' }).click();

  const dialog = page.getByRole('dialog', { name: 'Change Password' });
  const currentPassword = dialog.getByRole('textbox', { name: 'Current Password' });
  const newPassword = dialog.getByRole('textbox', { name: 'New Password', exact: true });
  const confirmPassword = dialog.getByRole('textbox', { name: 'Confirm New Password' });

  await currentPassword.fill('Secret123!');
  await newPassword.fill('EvenMoreSecret123!');
  await confirmPassword.fill('EvenMoreSecret123!');

  await expect(currentPassword).toHaveAttribute('type', 'password');
  await expect(newPassword).toHaveAttribute('type', 'password');
  await expect(confirmPassword).toHaveAttribute('type', 'password');

  await dialog.getByRole('button', { name: 'Show current password' }).click();
  await dialog.getByRole('button', { name: 'Show new password' }).click();
  await dialog.getByRole('button', { name: 'Show confirm new password' }).click();

  await expect(currentPassword).toHaveAttribute('type', 'text');
  await expect(newPassword).toHaveAttribute('type', 'text');
  await expect(confirmPassword).toHaveAttribute('type', 'text');

  await dialog.getByRole('button', { name: 'Hide current password' }).click();
  await dialog.getByRole('button', { name: 'Hide new password' }).click();
  await dialog.getByRole('button', { name: 'Hide confirm new password' }).click();

  await expect(currentPassword).toHaveAttribute('type', 'password');
  await expect(newPassword).toHaveAttribute('type', 'password');
  await expect(confirmPassword).toHaveAttribute('type', 'password');
});

test('payment methods support add, edit, and remove flows', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, { session: bidderSession });

  await page.goto('/payment-methods');
  await expect(page.getByRole('heading', { name: 'Payment Methods' })).toBeVisible();

  await page.getByRole('button', { name: /Add New Card/i }).click();
  let dialog = page.getByRole('dialog', { name: 'Add Payment Method' });
  await dialog.getByLabel('Card Holder Name').fill('Taylor Collector');
  await dialog.getByLabel('Card Number').fill('4111111111111111');
  await dialog.getByLabel('Expiry (MM/YY)').fill('12/30');
  await dialog.getByLabel('CVV').fill('123');
  await dialog.getByRole('button', { name: 'Add Card' }).click();

  await expect(page.getByText(/1111/)).toBeVisible();
  await expect(page.getByText(/Taylor Collector.*Expires 12\/30/)).toBeVisible();

  const addedCard = page.locator('.payment-method').filter({ has: page.getByText(/1111/) });
  await addedCard.getByRole('button', { name: 'Edit payment method' }).click();
  dialog = page.getByRole('dialog', { name: 'Edit Payment Method' });
  await dialog.getByLabel('Card Holder Name').fill('Taylor C.');
  await dialog.getByRole('button', { name: 'Save Changes' }).click();
  await expect(page.getByText(/Taylor C\..*Expires 12\/30/)).toBeVisible();

  await addedCard.getByRole('button', { name: 'Remove payment method' }).click();
  dialog = page.getByRole('dialog', { name: 'Remove Payment Method' });
  await dialog.getByRole('button', { name: 'Remove Card' }).click();

  await expect(page.getByText(/1111/)).toBeHidden();
});

test('profile updates email, phone number, and address flows', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, { session: bidderSession });

  await page.goto('/profile');

  await page.getByRole('button', { name: 'Edit email' }).click();
  let dialog = page.getByRole('dialog', { name: 'Edit Email' });
  await dialog.getByLabel('Email').fill('updated.bidder@example.com');
  await dialog.getByRole('button', { name: 'Save Changes' }).click();
  await expect(page.getByText('updated.bidder@example.com')).toBeVisible();

  await page.getByRole('button', { name: 'Edit phone number' }).click();
  dialog = page.getByRole('dialog', { name: 'Edit Phone Number' });
  await dialog.getByLabel('Phone Number').fill('+1 (555) 999-8888');
  await dialog.getByRole('button', { name: 'Save Changes' }).click();
  await expect(page.getByText('+1 (555) 999-8888')).toBeVisible();

  await page.getByRole('button', { name: 'Edit address' }).click();
  dialog = page.getByRole('dialog', { name: 'Edit Address' });
  await dialog.getByLabel('Address').fill('742 Evergreen Terrace, Springfield');
  await dialog.getByRole('button', { name: 'Save Changes' }).click();
  await expect(page.getByText('742 Evergreen Terrace, Springfield')).toBeVisible();
});

test('profile contact dialogs show validation errors for invalid values', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, { session: bidderSession });

  await page.goto('/profile');

  await page.getByRole('button', { name: 'Edit email' }).click();
  let dialog = page.getByRole('dialog', { name: 'Edit Email' });
  await dialog.getByLabel('Email').fill('not-an-email');
  await dialog.getByRole('button', { name: 'Save Changes' }).click();
  await expect(dialog.getByText('Enter a valid email address (max 320 characters).')).toBeVisible();
  await dialog.getByRole('button', { name: 'Cancel' }).click();

  await page.getByRole('button', { name: 'Edit phone number' }).click();
  dialog = page.getByRole('dialog', { name: 'Edit Phone Number' });
  await dialog.getByLabel('Phone Number').fill('abc');
  await dialog.getByRole('button', { name: 'Save Changes' }).click();
  await expect(dialog.getByText('Enter a valid phone number (7-20 characters).')).toBeVisible();
  await dialog.getByRole('button', { name: 'Cancel' }).click();

  await page.getByRole('button', { name: 'Edit address' }).click();
  dialog = page.getByRole('dialog', { name: 'Edit Address' });
  await dialog.getByLabel('Address').fill('abc');
  await dialog.getByRole('button', { name: 'Save Changes' }).click();
  await expect(dialog.getByText('Address must be between 5 and 200 characters.')).toBeVisible();
});

test('profile rolls back optimistic edits when the backend rejects an update', async ({ page }) => {
  const state = createMockState();
  await setupMockApp(page, state, { session: bidderSession });
  await page.route('**/api/users/profile', async (route) => {
    if (route.request().method() === 'PUT') {
      await route.fulfill({
        status: 400,
        contentType: 'application/json',
        body: JSON.stringify({ details: 'Email is already in use.' })
      });
      return;
    }

    await route.fallback();
  });

  await page.goto('/profile');
  await page.getByRole('button', { name: 'Edit full name' }).click();

  const dialog = page.getByRole('dialog', { name: /edit full name/i });
  await dialog.getByLabel('Full Name').fill('Rejected Name');
  await dialog.getByRole('button', { name: 'Save Changes' }).click();

  await expect(page.getByText('Could not save profile changes. Please try again.')).toBeVisible();
  await expect(page.getByRole('heading', { name: bidderSession.fullName })).toBeVisible();
});
