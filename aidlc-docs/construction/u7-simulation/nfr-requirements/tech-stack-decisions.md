# Tech Stack Decisions — U7 Simulation & Red-Teaming

**Unidad:** U7 — Simulation & Red-Teaming
**Stage:** Construction → NFR Requirements
**Fecha:** 2026-05-24
**Versión:** 1.0

---

## §1 Paquetes NuGet — producción

**Sin paquetes nuevos en producción.**

| Tecnología | Fuente | Uso en U7 |
|-----------|--------|-----------|
| `Microsoft.Extensions.Hosting` | Incluido en ASP.NET Core 8 | `BackgroundService` base class para `OrdersSimulatorService` |
| `Microsoft.Extensions.Options` | Incluido en ASP.NET Core 8 | `IOptions<SimulationOptions>` para leer configuración |
| `Microsoft.EntityFrameworkCore` | Ya instalado (U2) | `AppDbContext` con `DbSet<SimulatedOrder>` + `DbSet<SimulatedJobStatus>` |

**Sin cambios en `MonitorPedidos.Web.csproj`.**

---

## §2 Paquetes NuGet — tests

**Sin paquetes nuevos en `MonitorPedidos.UnitTests`.**

| Paquete | Ya instalado desde | Uso en U7 |
|---------|-------------------|-----------|
| `xunit` | U2/U3 | `OrdersSimulatorServiceTests` |
| `Moq` | U3 | Mockear `ISimulatedOrderRepository` |
| `FluentAssertions` | U3 | Aserciones legibles |

**Sin cambios en `MonitorPedidos.UnitTests.csproj`.**

---

## §3 Archivos nuevos no-NuGet

| Archivo | Ruta | Tipo |
|---------|------|------|
| `rt1-job-fail.sql` | `db/seed/rt1-job-fail.sql` | Script SQL manual |
| `rt1-job-reset.sql` | `db/seed/rt1-job-reset.sql` | Script SQL manual |
| `rt7-insert-cancelled.sql` | `db/seed/rt7-insert-cancelled.sql` | Script SQL manual |
| `cleanup-simulated.sql` | `db/seed/cleanup-simulated.sql` | Script SQL manual |
| `red-team-report-template.md` | `aidlc-docs/construction/u7-simulation/red-team-report-template.md` | Plantilla Markdown |

Los scripts SQL no forman parte del build ni del deploy — son auxiliares de ejecución manual durante red-teaming.

---

## §4 Resumen de cambios por capa

| Capa | Cambio |
|------|--------|
| Producción NuGet | **Ninguno** |
| Tests NuGet | **Ninguno** |
| Configuración | `Simulation:Enabled`, `InsertIntervalMinutes`, `FailureProbability`, `NoOrdersMode` en `appsettings.json` |
| Archivos estáticos | 4 scripts SQL en `db/seed/` + plantilla Markdown |
| Migraciones | `AddSimulatedTables` (a generar en Act4) |
