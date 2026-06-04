import { test, expect } from '@playwright/test';
import { loginAs } from '../helpers/auth';

test.setTimeout(60_000);

test('evaluacion completa botón Actualizar Brand Monitor', async ({ page }) => {
  await loginAs(page, 'Operador');
  await page.goto('/dashboard');

  // Esperar carga
  await expect(page.locator('[data-testid="brand-loading"]')).not.toBeVisible({ timeout: 15_000 });

  // Capturar timestamp ANTES
  const headerBefore = await page.locator('p.text-muted.small').first().textContent();
  console.log('TIMESTAMP ANTES:', headerBefore?.trim());

  await page.screenshot({ path: 'test-results/actualizar-antes.png' });

  // Click en Actualizar
  const btn = page.locator('[data-testid="btn-brand-refresh"]');
  await expect(btn).toBeEnabled();
  await btn.click();
  console.log('CLICK hecho');

  // Esperar que complete (con BD disponible es rápido)
  await expect(btn).toBeEnabled({ timeout: 35_000 });

  await page.waitForTimeout(1000);

  // Capturar timestamp DESPUÉS
  const headerAfter = await page.locator('p.text-muted.small').first().textContent();
  console.log('TIMESTAMP DESPUÉS:', headerAfter?.trim());

  await page.screenshot({ path: 'test-results/actualizar-despues.png' });

  // Verificar que la tabla sigue visible
  await expect(page.locator('[data-testid="brand-monitor-table"]').first()).toBeVisible();
});
