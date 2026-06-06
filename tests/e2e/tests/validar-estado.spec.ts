import { test } from '@playwright/test';
import { loginAs } from '../helpers/auth';
test.setTimeout(20_000);
test('validar estado actual del dashboard', async ({ page }) => {
  await loginAs(page, 'Operador');
  await page.goto('/dashboard');
  await page.waitForTimeout(5000);
  await page.screenshot({ path: 'test-results/validacion-final.png', fullPage: false });
});
