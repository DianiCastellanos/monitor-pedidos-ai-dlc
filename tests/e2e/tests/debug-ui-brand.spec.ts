import { test } from '@playwright/test';
import { loginAs } from '../helpers/auth';
test.setTimeout(30_000);
test('UI Brand Monitor mejorada', async ({ page }) => {
  await loginAs(page, 'Operador');
  await page.goto('/dashboard');
  await page.waitForTimeout(4000);
  await page.screenshot({ path: 'test-results/brand-ui-nueva.png', fullPage: false });
});
