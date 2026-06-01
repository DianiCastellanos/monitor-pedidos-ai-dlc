import { test, expect } from '@playwright/test';
import { loginAs } from '../helpers/auth';

test.describe('T1 — NOC carga correctamente', () => {
  test('página NOC carga sin errores y muestra todos los módulos', async ({ page }) => {
    await loginAs(page, 'Operador');
    await page.goto('/noc');

    // Encabezado principal visible
    await expect(page.getByText('Estado del Sistema')).toBeVisible();

    // Timestamp de actualización presente
    await expect(page.locator('text=/Actualizado: \\d{2}:\\d{2}:\\d{2}/')).toBeVisible();

    // Los 4 cards de módulo visibles
    await expect(page.locator('[data-testid="card-m3"]')).toBeVisible();
    await expect(page.locator('[data-testid="card-m2"]')).toBeVisible();
    await expect(page.locator('[data-testid="card-m4"]')).toBeVisible();
    await expect(page.locator('[data-testid="card-m11"]')).toBeVisible();

    // Brand Monitor visible
    await expect(page.locator('[data-testid="brand-monitor-table"]')).toBeVisible();

    // No se redirigió a error
    await expect(page).not.toHaveURL(/error/i);
  });
});
