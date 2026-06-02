import { test, expect } from '@playwright/test';
import { loginAs } from '../helpers/auth';

test('IT6 — Navbar muestra Discrepancias y Modo NOC como botón', async ({ page }) => {
  await loginAs(page, 'Operador');
  await page.goto('/dashboard');

  // Discrepancias ahora está en el navbar
  await expect(page.getByRole('link', { name: 'Discrepancias' })).toBeVisible();

  // Modo NOC aparece como botón diferenciado
  await expect(page.getByRole('link', { name: /Modo NOC/ })).toBeVisible();
});

test('IT6 — Link activo se resalta en la página actual', async ({ page }) => {
  await loginAs(page, 'Operador');
  await page.goto('/incidents');

  // El link "Historial" debe tener la clase activa
  const historialLink = page.getByRole('link', { name: 'Historial' });
  await expect(historialLink).toHaveClass(/nav-link-active/);

  // Dashboard no debe estar activo
  const dashLink = page.getByRole('link', { name: 'Dashboard' });
  await expect(dashLink).not.toHaveClass(/nav-link-active/);
});

test('IT6 — NocLayout muestra reloj en vivo y badge EN VIVO', async ({ page }) => {
  await loginAs(page, 'Operador');
  await page.goto('/noc');

  // Badge EN VIVO visible
  await expect(page.getByText('EN VIVO')).toBeVisible();

  // Reloj con formato fecha/hora
  await expect(page.getByText(/\d{2}\/\d{2}\/\d{4} \d{2}:\d{2}:\d{2}/)).toBeVisible();

  // Botón Salir de NOC
  await expect(page.getByRole('link', { name: 'Salir de NOC' })).toBeVisible();
});
