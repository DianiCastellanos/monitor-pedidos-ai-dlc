import { test, expect } from '@playwright/test';
import { loginAs } from '../helpers/auth';

test.describe('T4 — Brand Monitor siempre visible (no-destructive refresh)', () => {
  test('tabla permanece visible antes y después del auto-refresh', async ({ page }) => {
    await loginAs(page, 'Operador');
    await page.goto('/noc');

    const table = page.locator('[data-testid="brand-monitor-table"]');
    await expect(table).toBeVisible();

    // Los 4 sites siempre presentes (datos reales o skeleton)
    const sites = ['PatPrimo', 'SevenSeven', 'Atmos', 'Ostu'];
    for (const site of sites) {
      await expect(table.getByText(site)).toBeVisible();
    }

    // Esperar ciclo de auto-refresh NOC (15s) + margen de seguridad
    await page.waitForTimeout(18_000);

    // Tabla sigue visible — nunca desapareció durante el refresh
    await expect(table).toBeVisible();

    // Sites siguen presentes después del refresh
    for (const site of sites) {
      await expect(table.getByText(site)).toBeVisible();
    }
  });
});
