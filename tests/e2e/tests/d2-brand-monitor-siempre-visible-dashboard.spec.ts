import { test, expect } from '@playwright/test';
import { loginAs } from '../helpers/auth';

const SITES = ['PatPrimo', 'SevenSeven', 'Atmos', 'Ostu'] as const;

test.describe('D2 — Brand Monitor siempre visible en Dashboard', () => {
  test('tabla Brand Monitor aparece tras carga inicial y contiene los 4 sites', async ({ page }) => {
    await loginAs(page, 'Operador');
    await page.goto('/dashboard');

    // Espera a que el estado de loading inicial se resuelva
    // (brand-loading desaparece cuando termina la primera carga)
    await expect(page.locator('[data-testid="brand-loading"]')).not.toBeVisible({ timeout: 15_000 });

    // La tabla Brand Monitor debe ser visible en cualquiera de sus estados
    // (datos reales, skeleton, live fallback — siempre hay una tabla)
    const table = page.locator('[data-testid="brand-monitor-table"]').first();
    await expect(table).toBeVisible();

    // Los 4 sites siempre presentes — ya sea con datos o con "—" (skeleton)
    for (const site of SITES) {
      await expect(table.getByText(site, { exact: true })).toBeVisible();
    }
  });
});
