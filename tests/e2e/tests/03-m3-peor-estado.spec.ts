import { test, expect } from '@playwright/test';
import { loginAs } from '../helpers/auth';

// Orden de severidad: critical > warn > ok > unknown
const SEVERITY: Record<string, number> = { critical: 3, warn: 2, ok: 1, unknown: 0 };

function worstOf(a: string | null, b: string | null): string {
  const sa = SEVERITY[a ?? 'unknown'] ?? 0;
  const sb = SEVERITY[b ?? 'unknown'] ?? 0;
  return sa >= sb ? (a ?? 'unknown') : (b ?? 'unknown');
}

test.describe('T3 — M3 estado global = peor estado de sus APIs', () => {
  test('data-status del card M3 coincide con el peor estado individual', async ({ page }) => {
    await loginAs(page, 'Operador');
    await page.goto('/noc');

    // Estado individual de cada API
    const sfStatus = await page.locator('[data-testid="api-status-sf"]').getAttribute('data-status');
    const mvStatus = await page.locator('[data-testid="api-status-mv"]').getAttribute('data-status');

    // Estado del card M3 (dot indicator)
    const cardDotStatus = await page
      .locator('[data-testid="card-m3"] [data-testid="status-dot"]')
      .getAttribute('data-status');

    const expected = worstOf(sfStatus, mvStatus);
    expect(cardDotStatus).toBe(expected);
  });
});
