import { test, expect } from '@playwright/test';
import { loginAs } from '../helpers/auth';

test.setTimeout(120_000);

test('RT2 — M3 Salesforce muestra Critical con token inválido', async ({ page }) => {
  await loginAs(page, 'Operador');
  await page.goto('/noc');
  const sfBadge = page.locator('[data-testid="api-status-sf"]');
  await expect(async () => {
    await page.reload();
    expect(await sfBadge.getAttribute('data-status')).toBe('critical');
  }).toPass({ timeout: 75_000, intervals: [8_000] });
});

test('RT2 — M3 card global refleja Critical', async ({ page }) => {
  await loginAs(page, 'Operador');
  await page.goto('/noc');
  const m3Dot = page.locator('[data-testid="card-m3"] [data-testid="status-dot"]');
  await expect(async () => {
    await page.reload();
    expect(await m3Dot.getAttribute('data-status')).toBe('critical');
  }).toPass({ timeout: 75_000, intervals: [8_000] });
});

test('RT2 — Dashboard M3 refleja falla de Salesforce', async ({ page }) => {
  await loginAs(page, 'Operador');
  await page.goto('/dashboard');
  await expect(page.locator('[data-testid="domain-card-M3"]').getByText('Salesforce')).toBeVisible();
});

test('RT2 — Detalle de incidente Salesforce muestra SOP-001 (BR-TOKEN-01)', async ({ page }) => {
  await loginAs(page, 'Operador');

  // Polling: esperar a que el checker corra y actualice AccionSugerida con SOP-001
  await expect(async () => {
    // Ruta correcta: /incidents (no /incidents/history)
    await page.goto('/incidents');
    await page.waitForLoadState('networkidle');

    // Buscar fila de Salesforce
    const sfRow = page.getByRole('row').filter({ hasText: 'M13 — Salesforce API' });
    await expect(sfRow.first()).toBeVisible({ timeout: 5_000 });

    // Abrir detalle
    await sfRow.first().getByRole('button', { name: 'Ver' }).click();

    // AccionSugerida debe tener SOP-001 (BR-TOKEN-01: 401 → Token cause → template SOP-001)
    await expect(
      page.getByText('SOP-001').or(page.getByText('Renovar token'))
    ).toBeVisible({ timeout: 5_000 });
  }).toPass({ timeout: 90_000, intervals: [10_000] });
});
