import { test, expect } from '@playwright/test';
import { loginAs } from '../helpers/auth';

const SITES = ['PatPrimo', 'SevenSeven', 'Atmos', 'Ostu'] as const;

test.describe('D4 — Botón "↻ Actualizar" Brand Monitor no destruye la tabla', () => {
  test('tabla permanece visible durante y después del refresh manual', async ({ page }) => {
    await loginAs(page, 'Operador');
    await page.goto('/dashboard');

    // Esperar a que Brand Monitor termine la carga inicial
    await expect(page.locator('[data-testid="brand-loading"]')).not.toBeVisible({ timeout: 15_000 });

    const table = page.locator('[data-testid="brand-monitor-table"]').first();
    const refreshBtn = page.locator('[data-testid="btn-brand-refresh"]');

    // Estado previo: tabla visible con los 4 sites
    await expect(table).toBeVisible();
    for (const site of SITES) {
      await expect(table.getByText(site, { exact: true })).toBeVisible();
    }

    // Botón habilitado antes del refresh
    await expect(refreshBtn).toBeEnabled();

    // Clic en "↻ Actualizar"
    await refreshBtn.click();

    // Durante el refresh (botón deshabilitado con spinner):
    // la tabla NUNCA debe desaparecer — refresco no destructivo
    await expect(refreshBtn).toBeDisabled();
    await expect(table).toBeVisible();

    // Esperar fin del refresh
    await expect(refreshBtn).toBeEnabled({ timeout: 20_000 });

    // Post-refresh: tabla sigue visible, los 4 sites presentes
    await expect(table).toBeVisible();
    for (const site of SITES) {
      await expect(table.getByText(site, { exact: true })).toBeVisible();
    }
  });
});
