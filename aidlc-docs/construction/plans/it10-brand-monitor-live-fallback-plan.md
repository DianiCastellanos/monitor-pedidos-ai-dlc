# Plan IT10 — Brand Monitor Live Fallback + M2 UX

**Fecha:** 2026-05-31
**Estado:** Completado — validado 2026-05-31
**Unidades afectadas:** U6 (Dashboard/NOC UI), U4 (IBrandMonitorService)

---

## Objetivo

Cuando la BD no está disponible y el scheduler no ha podido persistir snapshots, mostrar datos en tiempo real de Salesforce (sin historial) en lugar del skeleton vacío. El usuario sigue viendo conteos reales de pedidos pendientes.

---

## Decisiones de diseño

| # | Decisión | Opción elegida | Razón |
|---|----------|----------------|-------|
| D1 | Fallback automático (timer) | `LastCheckStore[BrandMonitor].Details` | Sin costo — ya calculado por el checker |
| D2 | Fallback manual (botón) | `GetLiveCountsAsync()` → Salesforce directo | Acción explícita del usuario — justifica llamada directa |
| D3 | Indicador visual | Banner "⚠ Sin historial" por fila | Claro para negocio: "el dato es real pero sin comparación" |
| D4 | Skeleton sin datos | 4 marcas con "—" | Elimina "Esperando datos..." — más informativo |

---

## Checklist de implementación

- [x] `IBrandMonitorService`: + `GetLiveCountsAsync(CancellationToken)`
- [x] `BrandMonitorService`: implementar `GetLiveCountsAsync` via `IServiceScopeFactory` + `ISalesforceClient`
- [x] `BrandMonitorTable.razor`: params `LiveCounts`, `IsLiveFallback`; badge "⚠ Sin historial"; skeleton 4 marcas
- [x] `Dashboard.razor`: `GetBrandCountsFromLastCheckStore()`; fallback automático → LiveCounts; botón manual → `GetLiveCountsAsync`; banner "⚠ Datos en tiempo real"
- [x] `NocPage.razor`: fallback a `GetLiveCountsAsync`; pasa `LiveCounts` e `IsLiveFallback` a `BrandMonitorTable`
- [x] M2 UX: mensaje de detalle desde `LastCheckStore` en lugar de "Esperando datos..."

---

## Checklist de validación

- [x] Sin BD + checker corrió: Dashboard y NOC muestran conteos desde `LastCheckStore` con banner "Sin historial"
- [x] Sin BD + checker no corrió: skeleton con 4 marcas y "—" (no el mensaje anterior)
- [x] Con BD disponible: comportamiento normal sin banners
- [x] Botón manual "↻ Actualizar" con BD caída: llama Salesforce, muestra banner "⚠ Datos en tiempo real"
- [x] M2 muestra "Sin acceso a BD de pedidos" en rojo cuando BD no disponible (en lugar de "Esperando datos...")
