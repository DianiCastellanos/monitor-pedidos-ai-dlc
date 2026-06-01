# Plan IT7 — Salesforce → Brand Monitor Integration

**Fecha:** 2026-05-30  
**Estado:** ✅ Completado — validado 2026-05-30  
**Unidades afectadas:** U2 (Persistence — brand_snapshots), U3 (Detection — BrandMonitorChecker), U4 (External — ISalesforceClient)

---

## Objetivo

Reemplazar la fuente de datos del Brand Monitor: en lugar de contar órdenes en `ISimulatedOrderRepository` (datos simulados/internos), usar los pedidos pendientes de descarga obtenidos desde Salesforce OCAPI (`ISalesforceClient.SearchPendingOrdersAsync`).

El Brand Monitor reflejará exactamente cuántos pedidos están pendientes de ser descargados desde Salesforce al sistema de integración, por marca, en tiempo real.

---

## Contexto y relación con iteraciones anteriores

| Iteración | Relación |
|-----------|----------|
| IT3 — Brand Monitor Redesign | Define el modelo `brand_snapshots`, la lógica append-only, comparación histórica y `DetermineStatus`. **IT7 preserva todo esto intacto.** |
| IT5 — Salesforce Order Monitor | Implementa `ISalesforceClient.SearchPendingOrdersAsync` con multi-site paralelo. **IT7 reutiliza este client.** |

---

## Decisiones de diseño

| # | Decisión | Opción elegida | Razón |
|---|----------|----------------|-------|
| D1 | Fuente de datos | `ISalesforceClient.SearchPendingOrdersAsync` | Datos reales de SFCC — elimina simulación |
| D2 | Responsable de `brand_snapshots` | `BrandMonitorChecker` (sin cambio) | Mantiene IT3: single owner de escrituras |
| D3 | Doble query Salesforce | Aceptado (M3 + BrandMonitor hacen cada uno su llamada) | Simplicidad > optimización prematura; token cacheado, requests rápidos (~300ms) |
| D4 | Fallo de Salesforce | Retorna `Critical` sin guardar snapshots | Preserva historial limpio; no contamina con ceros ficticios |
| D5 | Casing de sites | `OrdinalIgnoreCase` en comparación + `"PatPrimo"` en `BrandSnapshot.Sites` | SFCC devuelve `"PatPrimo"` — alinear con realidad |
| D6 | Ventana histórica | Se mantiene `BrandMonitorWindowSeconds` para `GetSnapshotBeforeAsync` | Comparación "hace N segundos" sigue siendo válida |
| D7 | `SimulateAndRefreshAsync` | Simplificado: solo llama `checker.ExecuteAsync` | Sin órdenes simuladas — Salesforce es la fuente |

---

## Flujo de datos nuevo

```
MonitoringSchedulerService (cada 1 min)
    │
    ├── BrandMonitorChecker.ExecuteAsync()
    │       │
    │       ├── ISalesforceClient.SearchPendingOrdersAsync()
    │       │       └── 4 requests paralelos → PatPrimo / SevenSeven / Ostu / Atmos
    │       │           (token cacheado desde SalesforceTokenCache)
    │       │
    │       ├── Por cada site en BrandSnapshot.Sites:
    │       │       currentCount  = Items.Count(SiteId == site)
    │       │       previousCount = GetSnapshotBeforeAsync(site, now - window)
    │       │       status        = DetermineStatus(current, previous)
    │       │       InsertAsync(BrandSnapshot)
    │       │
    │       └── CheckResult.Ok("Brand monitor actualizado — N pedidos pendientes Salesforce")
    │
    └── SalesforceApiChecker.ExecuteAsync()  [independiente — incidentes M3]
            └── ISalesforceClient.SearchPendingOrdersAsync()  [segunda llamada, independiente]
```

---

## Archivos modificados

| Archivo | Tipo de cambio | Detalle |
|---------|---------------|---------|
| `MonitorPedidos.Web/Features/Monitoring/BrandMonitorChecker.cs` | **Modificado** | Reemplaza `ISimulatedOrderRepository` → `ISalesforceClient`; agrega guard en caso de fallo de SFCC |
| `MonitorPedidos.Domain/Dashboard/BrandSnapshot.cs` | **Modificado** | `"Patprimo"` → `"PatPrimo"` en `Sites[]` |
| `MonitorPedidos.Web/Services/BrandMonitorService.cs` | **Modificado** | `SimulateAndRefreshAsync` elimina inserción de órdenes simuladas |

### Archivos sin cambios (preservados de IT3)

| Archivo | Razón |
|---------|-------|
| `BrandSnapshotRepository.cs` | Sin cambios — append-only, `GetSnapshotBeforeAsync`, `GetLatestPerSiteAsync` igual |
| `IBrandSnapshotRepository.cs` | Sin cambios |
| `BrandSnapshot.cs` (lógica) | Solo cambia `Sites[]`, la entidad y `Create()` sin cambios |
| `Program.cs` | `BrandMonitorChecker` sigue registrado como scoped + ICheckExecutor |
| Dashboard pages | Sin cambios — consumen `IBrandMonitorService.GetCurrentSnapshotsAsync()` igual |

---

## Invariantes de IT3 preservadas

- ✅ Append-only — solo `InsertAsync`, nunca UPDATE en snapshots
- ✅ `PendingCountPrevious = null` en primer check (sin histórico)
- ✅ `DetermineStatus`: `Green` (bajó) / `Yellow` (igual) / `Red` (subió)
- ✅ Comparación con `GetSnapshotBeforeAsync(site, now - BrandMonitorWindowSeconds)`
- ✅ `BrandMonitorWindowSeconds` configurable (dev=15s, prod=600s)
- ✅ `IServiceScopeFactory` en `BrandMonitorService` — AppDbContext fresco para lecturas UI

---

## Comportamiento esperado en Dashboard

```
┌─────────────────────────────────────┐
│ Brand Monitor                       │
│ PatPrimo   │ 10 pend │ ↓ vs 12 🟢  │  ← Green: bajó (job descargó 2)
│ SevenSeven │  5 pend │ = vs  5 🟡  │  ← Yellow: igual
│ Ostu       │ 11 pend │ ↑ vs  9 🔴  │  ← Red: subió (más pedidos entraron)
│ Atmos      │  0 pend │ ↓ vs  1 🟢  │  ← Green: bajó
└─────────────────────────────────────┘
```

`PendingCountCurrent` = pedidos SFCC pendientes de descarga en este momento  
`PendingCountPrevious` = valor guardado hace `BrandMonitorWindowSeconds`

---

## Checklist de validación

- [x] Build sin errores ni warnings
- [x] App levanta correctamente
- [x] Primer ciclo: `Conteos Salesforce: PatPrimo=14, SevenSeven=6, Atmos=1, Ostu=15`
- [x] Snapshots guardados en `brand_snapshots` con valores reales (4 inserts)
- [x] Comparación histórica funciona (`anterior=N` desde snapshot previo)
- [x] `SalesforceApiChecker` (M3) corre independiente en mismo ciclo — total=36 coincide

---

## Restricciones de seguridad

- Credenciales Salesforce solo en `.env` (gitignored) — sin cambio
- `ProductionDb` (192.168.20.91) READ-ONLY — `BrandMonitorChecker` ya no accede a ProductionDb
- `brand_snapshots` en `DefaultConnection` (App DB) — R/W via EF Core ✅
