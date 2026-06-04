import { test } from '@playwright/test';
import { loginAs } from '../helpers/auth';

test('estado actual M2 y Brand Monitor', async ({ page }) => {
  await loginAs(page, 'Operador');
  await page.goto('/dashboard');
  await page.waitForTimeout(4000);
  await page.screenshot({ path: 'test-results/estado-actual.png', fullPage: false });
});
