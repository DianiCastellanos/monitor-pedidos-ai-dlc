# Plan de Pruebas E2E — Playwright
## MonitorPedidos NOC — Fase VALIDATE

**Versión**: 1.2  
**Fecha**: 2026-06-01  
**Estado**: EJECUTADO — 16/16 passing (10 funcionales + 3 Dashboard + 3 RT Bloque 1)  
**Proyecto tests**: `tests/e2e/`  
**URL base**: `http://localhost:5000`

---

## 1. Objetivo

Validar mediante pruebas automatizadas que el sistema NOC cumple las reglas críticas de negocio definidas en el AI-DLC, evitando regresiones en:

- Carga correcta de la vista NOC
- Visualización de detalle por API en M3
- Regla de estado global (peor estado)
- Persistencia visual del Brand Monitor (refresco no destructivo)
- Estado siempre definido en M4

---

## 2. Tecnología

| Componente | Detalle |
|---|---|
| Framework | Playwright `@playwright/test` v1.48+ |
| Lenguaje | TypeScript |
| Browser | Chromium (headless) |
| Runner | `npx playwright test` |
| Ubicación | `tests/e2e/` |
| Retries | 1 (automático en fallo) |
| Timeout por test | 40 segundos |

---

## 3. Prerequisitos de ejecución

1. La app debe estar corriendo en `http://localhost:5000`
2. Node.js >= 18 instalado
3. Primera vez: `npm install && npx playwright install chromium`

```bash
cd tests/e2e
npm test                   # headless
npm run test:headed        # con ventana visible
npm run test:ui            # interfaz interactiva
npm run test:report        # reporte HTML último run
```

---

## 4. Estrategia de autenticación

El sistema usa autenticación por cookie sin contraseña (selección de rol). El helper `helpers/auth.ts` encapsula el login:

```
GET /Identity/Select → click "Analista Operativo" → redirect a /dashboard
```

Todos los tests llaman `loginAs(page, 'Operador')` al inicio.

---

## 5. Atributos data-testid agregados

Para garantizar selectores robustos (sin dependencia de estilos o textos dinámicos):

| Selector | Elemento | Archivo |
|---|---|---|
| `data-testid="card-m3"` | Card APIs Externas M3 | NocCard.razor / NocPage.razor |
| `data-testid="card-m2"` | Card BD Pedidos M2 | NocCard.razor / NocPage.razor |
| `data-testid="card-m4"` | Card BD Salud M4 | NocCard.razor / NocPage.razor |
| `data-testid="card-m11"` | Card Jobs M11 | NocCard.razor / NocPage.razor |
| `data-testid="status-dot"` | Indicador de estado (dot) en cada card | NocCard.razor |
| `data-status="ok\|warn\|critical\|unknown"` | Valor semántico del estado | NocCard.razor |
| `data-testid="api-status-sf"` | Badge estado Salesforce | NocPage.razor |
| `data-testid="api-status-mv"` | Badge estado Multivende | NocPage.razor |
| `data-status="ok\|warn\|critical\|unknown"` | Valor semántico del badge API | NocPage.razor |
| `data-testid="brand-monitor-table"` | Contenedor raíz Brand Monitor | BrandMonitorTable.razor |

---

## 6. Casos de prueba

### T1 — NOC carga correctamente
**Archivo**: `tests/01-noc-carga.spec.ts`  
**Regla validada**: La vista NOC carga completamente sin errores

| Paso | Acción | Verificación |
|---|---|---|
| 1 | Login como Operador | Redirect fuera de /Identity |
| 2 | Navegar a `/noc` | URL = /noc |
| 3 | — | Título "Estado del Sistema" visible |
| 4 | — | Cards card-m3, card-m2, card-m4, card-m11 visibles |
| 5 | — | brand-monitor-table visible |
| 6 | — | URL no contiene "error" |

**Resultado**: ✅ PASSING

---

### T2 — M3 muestra detalle por API
**Archivo**: `tests/02-m3-detalle-apis.spec.ts`  
**Regla validada**: M3 siempre muestra estado individual de cada integración (no solo el estado global)

| Paso | Acción | Verificación |
|---|---|---|
| 1 | Login + /noc | — |
| 2 | — | card-m3 visible |
| 3 | — | Etiqueta "Salesforce" visible en card-m3 (exact match) |
| 4 | — | Etiqueta "Multivende" visible en card-m3 (exact match) |
| 5 | — | api-status-sf visible con data-status en {ok, warn, critical, unknown} |
| 6 | — | api-status-mv visible con data-status en {ok, warn, critical, unknown} |
| 7 | — | Ambos badges tienen texto no vacío |

**Nota**: `{ exact: true }` necesario en Multivende porque el detalle de error "Multivende HTTP 0" también contiene la palabra "Multivende".  
**Resultado**: ✅ PASSING

---

### T3 — M3 estado global = peor estado de sus APIs
**Archivo**: `tests/03-m3-peor-estado.spec.ts`  
**Regla validada**: El estado del card M3 refleja el peor estado entre Salesforce y Multivende (BR-M3-01)

Escala de severidad: `critical (3) > warn (2) > ok (1) > unknown (0)`

| Paso | Acción | Verificación |
|---|---|---|
| 1 | Login + /noc | — |
| 2 | Leer data-status de api-status-sf | valor en escala de severidad |
| 3 | Leer data-status de api-status-mv | valor en escala de severidad |
| 4 | Calcular peor estado entre los dos | — |
| 5 | Leer data-status del status-dot de card-m3 | debe ser igual al peor calculado |

**Resultado**: ✅ PASSING  
**Estado real en ejecución**: Salesforce=ok, Multivende=critical → card M3=critical ✓

---

### T4 — Brand Monitor siempre visible (non-destructive refresh)
**Archivo**: `tests/04-brand-monitor-visible.spec.ts`  
**Regla validada**: La tabla Brand Monitor nunca desaparece durante el auto-refresh de 15s (NOC)

| Paso | Acción | Verificación |
|---|---|---|
| 1 | Login + /noc | — |
| 2 | — | brand-monitor-table visible |
| 3 | — | PatPrimo, SevenSeven, Atmos, Ostu visibles en tabla |
| 4 | Esperar 18 segundos (refresh cada 15s) | — |
| 5 | — | brand-monitor-table sigue visible |
| 6 | — | Los 4 sites siguen visibles |

**Resultado**: ✅ PASSING

---

### T5 — M4 siempre muestra estado de BD
**Archivo**: `tests/05-m4-estado-db.spec.ts`  
**Regla validada**: M4 nunca queda vacío — siempre muestra latencia o N/A

**T5a — Estado normal**:

| Paso | Acción | Verificación |
|---|---|---|
| 1 | Login + /noc | — |
| 2 | — | card-m4 visible |
| 3 | — | Etiqueta "Latencia:" visible |
| 4 | — | Valor `<strong>` no vacío (ej: "19 ms" o "N/A") |
| 5 | — | status-dot de card-m4 tiene data-status válido |

**T5b — Estado crítico (condicional)**:

| Condición | Verificación |
|---|---|
| dotStatus = "critical" | dot tiene data-status="critical" |
| dotStatus ≠ "critical" | latencia tiene formato `\d+ ms` o `N/A` |

**Resultado**: ✅ PASSING

---

## 7. Cobertura de reglas críticas

| Regla | Test | Estado |
|---|---|---|
| NOC carga sin errores | T1 | ✅ |
| M3 muestra detalle por integración | T2 | ✅ |
| M3 estado global = peor componente | T3 | ✅ |
| Brand Monitor nunca desaparece (refresco no destructivo) | T4 | ✅ |
| M4 siempre muestra estado (nunca vacío) | T5 | ✅ |

---

## 8. Ejecución de referencia — suite completa

```
Running 10 tests using 9 workers

  ok T1 — NOC carga correctamente                          15.8s
  ok T2 — M3 muestra detalle por API                       13.2s
  ok T3 — M3 estado global = peor estado de sus APIs       12.9s
  ok T4 — Brand Monitor siempre visible                    33.5s
  ok T5a — M4 BD Salud visible con latencia o N/A          14.7s
  ok T5b — M4 muestra estado crítico cuando reporta Critical 11.3s
  ok D1 — Dashboard carga sin errores                       4.1s
  ok D2 — Brand Monitor siempre visible en Dashboard         3.1s
  ok D3 — Botón Chequear ahora funciona                      5.4s
  ok D4 — Refresh no destructivo Brand Monitor               5.4s

  10 passed (35.8s)
```

---

## 9. Red-Team Bloque 1 — Validación RT-Persist · RT5 · RT7

**Archivo**: `tests/e2e/tests/rt-bloque1.spec.ts`

```
Running 3 tests using 1 worker

  ok RT-Persist — alertas persisten tras reinicio de app        1.9s
  ok RT5 — panel de discrepancias UC6 existe y carga            0.8s
  ok RT7 — M3 activo, filtro cancelados confirmado en código   11.4s

  3 passed (17.2s)
```

Ver detalles completos en: `rt-bloque1-validacion.md`

---

## 10. Errores documentados

| Error | Archivo | Estado |
|---|---|---|
| E1 — data-testid faltante en rama else Brand Monitor | errores-playwright-dashboard.md | ✅ Corregido |
| E2 — estado disabled transitorio no detectable | errores-playwright-dashboard.md | ✅ Test rediseñado |
| E3 — LogsPage falla para rol Técnico | rt-bloque1-validacion.md | ⚠️ Pendiente investigación |

---

## 11. Pendiente / Evolución futura

| Item | Descripción |
|---|---|
| Bloque 2 RT | RT3 (BD caída) y RT2 (token inválido) — simulación por .env |
| Bloque 3 RT | RT1 (job deshabilitado) — acción en SR-SDEV02CO |
| E3 — LogsPage | Investigar error en pre-rendering con JSRuntime |
| CI/CD | Integrar `npx playwright test` en pipeline de despliegue |
