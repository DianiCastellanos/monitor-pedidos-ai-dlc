import { test, expect } from '@playwright/test';
import { loginAs } from '../helpers/auth';

test('debug — botón Actualizar Brand Monitor', async ({ page }) => {
  await loginAs(page, 'Operador');
  await page.goto('/dashboard');

  // Esperar carga inicial
  await expect(page.locator('[data-testid="brand-loading"]')).not.toBeVisible({ timeout: 15_000 });

  // Screenshot antes de hacer click
  await page.screenshot({ path: 'test-results/brand-antes.png', fullPage: false });

  const btn = page.locator('[data-testid="btn-brand-refresh"]');
  
  // Verificar que el botón existe y está habilitado
  await expect(btn).toBeVisible({ timeout: 5_000 });
  const isEnabled = await btn.isEnabled();
  console.log('Botón habilitado antes del click:', isEnabled);

  // Click
  await btn.click();
  
  // Screenshot inmediatamente después
  await page.screenshot({ path: 'test-results/brand-durante.png', fullPage: false });
  
  // Esperar que termine
  await expect(btn).toBeEnabled({ timeout: 35_000 });
  
  // Screenshot final
  await page.screenshot({ path: 'test-results/brand-despues.png', fullPage: false });

  // Tabla sigue visible
  await expect(page.locator('[data-testid="brand-monitor-table"]').first()).toBeVisible();
});
