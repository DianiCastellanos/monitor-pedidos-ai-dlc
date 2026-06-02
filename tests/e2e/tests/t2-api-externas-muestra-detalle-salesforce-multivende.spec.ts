import { test, expect } from '@playwright/test';
import { loginAs } from '../helpers/auth';

const VALID_STATUSES = ['ok', 'warn', 'critical', 'unknown'] as const;

test.describe('T2 — M3 muestra detalle por API', () => {
  test('Salesforce y Multivende visibles con estado definido', async ({ page }) => {
    await loginAs(page, 'Operador');
    await page.goto('/noc');

    const m3 = page.locator('[data-testid="card-m3"]');
    await expect(m3).toBeVisible();

    // Etiquetas de integración visibles dentro de M3 (exact: evita colisión con el detalle "Multivende HTTP 0")
    await expect(m3.getByText('Salesforce', { exact: true })).toBeVisible();
    await expect(m3.getByText('Multivende', { exact: true })).toBeVisible();

    // Badges con data-testid robustos
    const sfBadge = page.locator('[data-testid="api-status-sf"]');
    const mvBadge = page.locator('[data-testid="api-status-mv"]');
    await expect(sfBadge).toBeVisible();
    await expect(mvBadge).toBeVisible();

    // Cada badge tiene data-status definido y válido
    const sfStatus = await sfBadge.getAttribute('data-status');
    const mvStatus = await mvBadge.getAttribute('data-status');
    expect(VALID_STATUSES).toContain(sfStatus as typeof VALID_STATUSES[number]);
    expect(VALID_STATUSES).toContain(mvStatus as typeof VALID_STATUSES[number]);

    // Cada badge tiene texto visible (no vacío)
    await expect(sfBadge).not.toBeEmpty();
    await expect(mvBadge).not.toBeEmpty();
  });
});
