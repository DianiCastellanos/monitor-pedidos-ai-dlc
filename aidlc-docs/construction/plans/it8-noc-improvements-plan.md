# Plan IT8 — NOC Page Improvements

**Fecha:** 2026-05-31
**Estado:** Completado — validado 2026-05-31
**Unidades afectadas:** U6 (Dashboard/NOC UI), U4 (External Integrations)

---

## Objetivo

- Mejorar visualización Brand Monitor en NOC: flechas de tendencia reales (↑ ↓ =)
- Redefinir M3: `SalesforceApiChecker` solo evalúa disponibilidad (Ok/Critical), no volumen de pedidos
- `ApiStatusBadge`: agregar estado `Warn` con color amarillo y labels descriptivos
- Reducir timing dev: `CheckerIntervalMinutes=0.5` (30s); NOC timer 15s

---

## Decisiones de diseño

| # | Decisión | Opción elegida | Razón |
|---|----------|----------------|-------|
| D1 | Semántica M3 | Solo disponibilidad en `SalesforceApiChecker` | Los conteos de pedidos pertenecen a Brand Monitor, no a M3 |
| D2 | Tendencias Brand Monitor | `SnapshotStatus` real + flechas | Más informativo que solo colores |
| D3 | Skeleton sin datos | 4 marcas con "—" | Siempre hay estructura — evita página en blanco |
| D4 | Timing NOC | 15s | Balance velocidad/estabilidad para TV |

---

## Checklist de implementación

- [x] `SalesforceApiChecker`: eliminar lógica de volumen de pedidos → solo Ok/Critical por disponibilidad
- [x] `BrandMonitorTable.razor`: flechas ↑ ↓ = desde `SnapshotStatus`; skeleton 4 marcas
- [x] `ApiStatusBadge.razor`: estado `Warn` amarillo + labels "Disponible / Con advertencias / Sin respuesta"
- [x] `NocPage.razor`: `GetApisStatus()` con propagación Warn; `GetApiShortDetail()` solo Critical; timer 15s
- [x] `NocPage.razor`: `ParseBrandCountsFromLastCheckStore()` para fallback sin BD
- [x] `appsettings.Development.json`: `CheckerIntervalMinutes: 0.5`

---

## Checklist de validación

- [x] NOC muestra flechas de tendencia según SnapshotStatus (↑ rojo, ↓ verde, = amarillo, — gris)
- [x] M3 en NOC muestra "Disponible" (verde) cuando Salesforce responde, "Sin respuesta" (rojo) cuando no
- [x] M3 en NOC no muestra conteos de pedidos — esa información está en Brand Monitor
- [x] Badge amarillo "Con advertencias" aparece correctamente
- [x] NOC se actualiza cada 15s correctamente
- [x] Sin BD: NOC muestra conteos desde `LastCheckStore[BrandMonitor].Details`
- [x] Sin datos en absoluto: skeleton con 4 marcas y "—"
