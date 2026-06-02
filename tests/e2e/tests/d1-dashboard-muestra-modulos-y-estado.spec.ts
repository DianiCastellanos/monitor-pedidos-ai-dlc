import { test, expect } from '@playwright/test';
import { loginAs } from '../helpers/auth';

const VALID_COLORS = ['success', 'danger', 'warning', 'secondary'] as const;

test.describe('D1 — Dashboard carga correctamente', () => {
  test('página Dashboard carga sin errores y muestra todos los módulos', async ({ page }) => {
    await loginAs(page, 'Operador');
    await page.goto('/dashboard');

    // Título principal visible
    await expect(page.getByText('Dashboard — MonitorPedidos AI')).toBeVisible();

    // Los 4 cards de módulo visibles
    for (const code of ['M2', 'M3', 'M4', 'M11']) {
      await expect(page.locator(`[data-testid="domain-card-${code}"]`)).toBeVisible();
    }

    // Badge de estado global con data-color válido
    const badge = page.locator('[data-testid="overall-status"]');
    await expect(badge).toBeVisible();
    const color = await badge.getAttribute('data-color');
    expect(VALID_COLORS).toContain(color as typeof VALID_COLORS[number]);

    // Encabezado Brand Monitor visible
    await expect(page.locator('[data-testid="brand-monitor-header"]')).toBeVisible();

    // No redirigió a error
    await expect(page).not.toHaveURL(/error/i);
  });
});
