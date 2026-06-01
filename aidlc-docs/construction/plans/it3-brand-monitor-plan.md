# Plan IT3 — Brand Monitor Redesign

**Fecha:** 2026-05-30  
**Estado:** ✅ Completado — Paso 7 (retención) diferido

---

## Objetivo

Rediseñar Brand Monitor de upsert+ventana a append-only+snapshot histórico real, para que "Hace 10 min" refleje un valor guardado y no un reconteo.

## Decisiones clave

| # | Decisión | Opción elegida | Razón |
|---|----------|----------------|-------|
| D1 | Modelo de persistencia | Append-only (`InsertAsync` únicamente) | Permite comparar snapshots reales de distintos momentos |
| D2 | Comparación histórica | `GetSnapshotBeforeAsync(site, now - window)` | El "anterior" proviene de un snapshot guardado, no de un reconteo |
| D3 | Primer chequeo sin histórico | `PendingCountPrevious = null`, `Status = NoData` | Dato honesto — no fabricar un "0" |
| D4 | Badge en UI | Derivado de `PendingCountPrevious.HasValue` | `s.Status` puede estar desactualizado; `null` es la única señal confiable de "sin datos" |
| D5 | Scopes DI en BrandMonitorService | `IServiceScopeFactory.CreateAsyncScope()` por operación | El `AppDbContext` del circuito Blazor se corrompe tras error y no se recupera |
| D6 | `GetLatestPerSiteAsync` | Carga en memoria + `GroupBy` LINQ | EF Core GroupBy SQL no garantiza traducción correcta con PostgreSQL/SQL Server |
| D7 | Ventana configurable | `Monitoring:BrandMonitorWindowSeconds` en appsettings | Dev=15s para pruebas rápidas; Prod=600s (10 min reales) |

## Unidades afectadas

U2 (Persistence — entidad `BrandSnapshot`, repositorio), U3 (Detection — `BrandMonitorChecker`), U6 (Dashboard — `BrandMonitorPage.razor`, `Dashboard.razor`), U1 (NFR Design — patrón `IServiceScopeFactory`)

## Artefactos

- [functional-design/domain-entities.md](../iteraciones/it3-brand-monitor/functional-design/domain-entities.md)
- [functional-design/business-logic-model.md](../iteraciones/it3-brand-monitor/functional-design/business-logic-model.md)

## Pendiente — Paso 7

Retención de histórico: `DeleteOlderThanAsync(DateTime.UtcNow.AddHours(-48))` en `IncidentMaintenanceService`. Aprobado, diferido por el owner.
