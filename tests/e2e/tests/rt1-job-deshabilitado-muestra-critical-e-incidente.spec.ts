import { test, expect } from '@playwright/test';
import { loginAs } from '../helpers/auth';

// RT1: Job OC_PATPRIMO deshabilitado en SR-SDEV02CO
// Checker M11 corre cada 30s en dev — espera máxima 45s

test.setTimeout(90_000);

test('RT1 — M11 Jobs muestra Critical cuando OC_PATPRIMO está Disabled', async ({ page }) => {
  await loginAs(page, 'Operador');
  await page.goto('/noc');

  const m11Dot = page.locator('[data-testid="card-m11"] [data-testid="status-dot"]');

  // Polling: esperar hasta 45s a que el checker detecte el job deshabilitado
  await expect(async () => {
    await page.reload();
    const status = await m11Dot.getAttribute('data-status');
    expect(status).toBe('critical');
  }).toPass({ timeout: 45_000, intervals: [5_000] });
});

test('RT1 — M11 card muestra OC_PATPRIMO como Disabled', async ({ page }) => {
  await loginAs(page, 'Operador');
  await page.goto('/noc');

  // Tras el checker detectar el fallo, el card M11 muestra "Disabled"
  const m11Card = page.locator('[data-testid="card-m11"]');
  await expect(async () => {
    await page.reload();
    const hasCritical = await m11Card.locator('[data-testid="status-dot"]')
      .getAttribute('data-status') === 'critical';
    expect(hasCritical).toBe(true);
    // El card muestra el nombre del job y "Disabled"
    await expect(m11Card.getByText('Disabled')).toBeVisible({ timeout: 3_000 });
  }).toPass({ timeout: 45_000, intervals: [5_000] });
});

test('RT1 — Incidente M11 creado con causa Job', async ({ page }) => {
  await loginAs(page, 'Operador');
  await page.goto('/dashboard');

  // El checker ya corrió — debe haber alerta activa
  await expect(async () => {
    await page.reload();
    await expect(page.getByText('Alertas Activas')).toBeVisible({ timeout: 5_000 });
  }).toPass({ timeout: 45_000, intervals: [5_000] });

  // Verificar en historial que el incidente M11 fue creado
  await page.goto('/incidents');
  await page.waitForLoadState('networkidle');
  await expect(
    page.getByRole('row').filter({ hasText: 'M11' }).first()
  ).toBeVisible({ timeout: 10_000 });
});
