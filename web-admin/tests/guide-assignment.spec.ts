import { test, expect } from '@playwright/test';

const TEST_EMAIL = 'kasun@mail.com';
const TEST_PASSWORD = 'K12345';

test.describe('Guide Assignment Integration', () => {

  test.beforeEach(async ({ page }) => {
    // Go directly to the login page
    await page.goto('/login');

    // Fill in admin credentials
    await page.getByLabel('Email address').fill(TEST_EMAIL);
    await page.getByLabel('Password').fill(TEST_PASSWORD);

    // Sign in
    await page.getByRole('button', {
      name: 'Sign In to Console',
      exact: true,
    }).click();

    // Wait until login completes
    await expect(page).not.toHaveURL(/\/login/, {
      timeout: 10000,
    });
  });


  test('successfully assigns and unassigns a guide from a booking', async ({ page }) => {

    // Navigate to the actual Guide Assignment page
    await page.goto('/operations/guide-assignments');

    // Verify that the correct page loaded
    await expect(page.locator('.page-title')).toContainText(
      'Guide assignment matrix',
      { timeout: 10000 }
    );

    // Wait for at least one guide row
    await expect(
      page.locator('.matrix-table tbody tr').first()
    ).toBeVisible({
      timeout: 10000,
    });

    // Get the first schedule cell of the first guide
    // First <td> contains the guide name, so nth(1) is the first schedule cell.
    const scheduleCell = page
      .locator('.matrix-table tbody tr')
      .first()
      .locator('td')
      .nth(1);

    await expect(scheduleCell).toBeVisible();

    // Open assignment modal
    await scheduleCell.click();

    const modalHeading = page.getByRole('heading', {
      name: 'Assign Guide',
    });

    await expect(modalHeading).toBeVisible({
      timeout: 5000,
    });

    // Find an available tour
    const assignButton = page.getByRole('button', {
      name: 'Assign',
      exact: true,
    }).first();

    const noToursMessage = page.getByText(
      'No unassigned tours found for this date.'
    );

    // Wait until tours have loaded
    await expect(
      assignButton.or(noToursMessage)
    ).toBeVisible({
      timeout: 10000,
    });

    // This test requires at least one unassigned tour
    await expect(assignButton).toBeVisible({
      timeout: 10000,
    });

    // Assign guide
    await assignButton.click();

    // Successful assignment closes the modal
    await expect(modalHeading).toBeHidden({
      timeout: 10000,
    });

    // Open the same schedule cell again
    await scheduleCell.click();

    await expect(modalHeading).toBeVisible({
      timeout: 5000,
    });

    // The assignment should now exist
    const unassignButton = page.getByRole('button', {
      name: 'Unassign',
      exact: true,
    });

    await expect(unassignButton).toBeVisible({
      timeout: 10000,
    });

    // Unassign guide
    await unassignButton.click();

    // Successful unassignment closes the modal
    await expect(modalHeading).toBeHidden({
      timeout: 10000,
    });
  });

});