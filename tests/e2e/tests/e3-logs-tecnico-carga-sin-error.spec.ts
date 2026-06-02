import { test, expect } from '@playwright/test';
import { loginAs } from '../helpers/auth';

// E3 — LogsPage carga correctamente para rol Técnico
// Fix: TechnicalLogReader usa FileStream con FileShare.ReadWrite (Serilog mantiene el archivo abierto)

test('E3 — /logs carga sin error para rol Técnico', async ({ page }) => {
  await loginAs(page, 'Tecnico');
  await page.goto('/logs');

  // Debe mostrar el título — sin error boundary
  await expect(page.getByText('Logs Técnicos')).toBeVisible({ timeout: 10_000 });
  await expect(page).not.toHaveURL(/error/i);

  // Muestra el panel de controles (funciona aunque no haya logs)
  await expect(page.getByRole('button', { name: '↻ Actualizar' })).toBeVisible();
});
