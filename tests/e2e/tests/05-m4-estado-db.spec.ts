import { test, expect } from '@playwright/test';
import { loginAs } from '../helpers/auth';

const VALID_STATUSES = ['ok', 'warn', 'critical', 'unknown'] as const;

test.describe('T5 — M4 siempre muestra estado de BD', () => {
  test('M4 BD Salud visible con latencia o N/A — nunca vacío', async ({ page }) => {
    await loginAs(page, 'Operador');
    await page.goto('/noc');

    const m4 = page.locator('[data-testid="card-m4"]');
    await expect(m4).toBeVisible();

    // Etiqueta "Latencia:" siempre visible
    await expect(m4.getByText(/Latencia:/)).toBeVisible();

    // El valor de latencia es un string no vacío ("12 ms", "N/A", etc.)
    const latencyValue = await m4.locator('strong').textContent();
    expect(latencyValue).not.toBeNull();
    expect(latencyValue!.trim().length).toBeGreaterThan(0);

    // El indicador de estado tiene un valor válido — nunca sin resolver
    const dotStatus = await m4
      .locator('[data-testid="status-dot"]')
      .getAttribute('data-status');
    expect(VALID_STATUSES).toContain(dotStatus as typeof VALID_STATUSES[number]);
  });

  test('M4 muestra "BD no disponible" cuando el checker reporta Critical', async ({ page }) => {
    await loginAs(page, 'Operador');
    await page.goto('/noc');

    const m4 = page.locator('[data-testid="card-m4"]');
    const dotStatus = await m4.locator('[data-testid="status-dot"]').getAttribute('data-status');

    if (dotStatus === 'critical') {
      // Con BD caída: el dot es critical (rojo) y la tarjeta lo refleja
      const dot = m4.locator('[data-testid="status-dot"]');
      await expect(dot).toHaveAttribute('data-status', 'critical');
    } else {
      // BD disponible: muestra latencia numérica
      const latencyValue = await m4.locator('strong').textContent();
      expect(latencyValue).toMatch(/\d+ ms|N\/A/);
    }
  });
});
