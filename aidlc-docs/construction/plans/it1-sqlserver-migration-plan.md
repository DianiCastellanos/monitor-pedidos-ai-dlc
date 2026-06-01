# Plan IT1 — Migración PostgreSQL → SQL Server + Dual DB

**Fecha:** 2026-05-30  
**Estado:** ✅ Completado

---

## Objetivo

Migrar el proveedor de BD de PostgreSQL a SQL Server (infraestructura de la empresa) e introducir una segunda conexión de solo lectura a la BD de producción para que M2 lea pedidos reales de `oc_encabezado`.

## Decisiones clave

| # | Decisión | Opción elegida | Razón |
|---|----------|----------------|-------|
| D1 | Proveedor EF Core | SQL Server (`Microsoft.EntityFrameworkCore.SqlServer`) | Infraestructura real de la empresa |
| D2 | Lectura de pedidos reales | Dapper contra `vtainternet_qa` | Solo SELECT — no queremos EF Core con migraciones en la BD de producción |
| D3 | Registro de `IOrderSource` | Condicional en `Program.cs` según `ProductionDb` string | Dev usa simulador; prod usa `ProductionOrderRepository` automáticamente |
| D4 | Credenciales | `.env` gitignored + placeholders en `appsettings.json` | Evita credenciales versionadas |

## Unidades afectadas

U3 (Detection — fuente de datos M2), U7 (Simulation — fallback `IOrderSource`), U1 (Infrastructure base — proveedor EF Core)

## Artefactos

- [functional-design/infrastructure-changes.md](../iteraciones/it1-sqlserver-migration/functional-design/infrastructure-changes.md)
- [infrastructure-design/deployment-architecture.md](../iteraciones/it1-sqlserver-migration/infrastructure-design/deployment-architecture.md)
