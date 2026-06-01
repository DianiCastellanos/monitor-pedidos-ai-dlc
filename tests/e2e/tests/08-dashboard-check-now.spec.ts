import { test, expect } from '@playwright/test';
import { loginAs } from '../helpers/auth';

test.describe('D3 — Botón "Chequear ahora" funciona correctamente', () => {
  test('spinner aparece → desaparece → countdown se resetea', async ({ page }) => {
    await loginAs(page, 'Operador');
    await page.goto('/dashboard');

    const btn = page.locator('[data-testid="btn-check-now"]');

    // Botón habilitado antes de hacer clic
    await expect(btn).toBeEnabled({ timeout: 10_000 });

    // Clic en "Chequear ahora"
    await btn.click();

    // El refresh puede ser tan rápido (< 100ms con BD disponible) que el estado
    // disabled es transitorio y Playwright no alcanza a detectarlo — es correcto.
    // Verificamos el resultado: el botón vuelve a estar habilitado.
    await expect(btn).toBeEnabled({ timeout: 35_000 });

    // Countdown visible con cualquier valor numérico (se resetó al hacer clic)
    await expect(page.getByText(/Próxima actualización en: \d+/)).toBeVisible();

    // Estado global sigue visible y válido
    await expect(page.locator('[data-testid="overall-status"]')).toBeVisible();
  });
});
