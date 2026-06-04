import { test } from '@playwright/test';
import { loginAs } from '../helpers/auth';
test('estado actual completo', async ({ page }) => {
  await loginAs(page, 'Operador');
  await page.goto('/dashboard');
  await page.waitForTimeout(3000);
  await page.screenshot({ path: 'test-results/estado-ahora.png', fullPage: true });
});
