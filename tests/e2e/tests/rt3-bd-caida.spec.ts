import { test, expect } from '@playwright/test';
import { loginAs } from '../helpers/auth';

// RT3: BD caída (IP inválida 192.168.20.99, Connect Timeout=5s)
// Checkers M2/M4 corren cada 30s en dev — espera máxima 45s

test.setTimeout(90_000);

test('RT3 — M4 BD Salud muestra Critical cuando BD no responde', async ({ page }) => {
  await loginAs(page, 'Operador');
  await page.goto('/noc');

  const m4Dot = page.locator('[data-testid="card-m4"] [data-testid="status-dot"]');

  // Esperar hasta 60s a que el checker corra y detecte la falla
  await expect(async () => {
    await page.reload();
    const status = await m4Dot.getAttribute('data-status');
    expect(status).toBe('critical');
  }).toPass({ timeout: 60_000, intervals: [5_000] });
});

test('RT3 — M2 BD Pedidos muestra Critical cuando BD no responde', async ({ page }) => {
  await loginAs(page, 'Operador');
  await page.goto('/noc');

  const m2Dot = page.locator('[data-testid="card-m2"] [data-testid="status-dot"]');

  await expect(async () => {
    await page.reload();
    const status = await m2Dot.getAttribute('data-status');
    expect(status).toBe('critical');
  }).toPass({ timeout: 60_000, intervals: [5_000] });
});

test('RT3 — Dashboard no crashea con BD caída (graceful degradation)', async ({ page }) => {
  await loginAs(page, 'Operador');
  await page.goto('/dashboard');

  // La página debe cargar — no debe mostrar un error fatal
  await expect(page.getByText('Dashboard — MonitorPedidos AI')).toBeVisible();

  // Módulos siguen visibles (aunque con estado degradado)
  for (const code of ['M2', 'M3', 'M4', 'M11']) {
    await expect(page.locator(`[data-testid="domain-card-${code}"]`)).toBeVisible();
  }

  // Brand Monitor header siempre visible (no crash)
  await expect(page.locator('[data-testid="brand-monitor-header"]')).toBeVisible();
});
