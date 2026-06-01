import { Page } from '@playwright/test';

/**
 * Inicia sesión en /Identity/Select y espera la redirección al dashboard.
 * identity='Operador' → "Analista Operativo"
 * identity='Tecnico'  → "Responsable Técnico"
 */
export async function loginAs(page: Page, identity: 'Operador' | 'Tecnico'): Promise<void> {
  await page.goto('/Identity/Select');
  const buttonText = identity === 'Operador' ? 'Analista Operativo' : 'Responsable Técnico';
  await page.getByRole('button', { name: buttonText }).click();
  await page.waitForURL((url) => !url.pathname.includes('/Identity'), { timeout: 10_000 });
}
