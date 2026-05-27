# Logical Components — U7 Simulation & Red-Teaming

**Unidad:** U7 — Simulation & Red-Teaming
**Stage:** Construction → NFR Design
**Fecha:** 2026-05-24
**Versión:** 1.0

---

## §1 Componentes de producción introducidos en U7

| ID | Componente | Tipo | Proyecto | Descripción |
|----|-----------|------|---------|-------------|
| LC-U7-01 | `SimulatedOrder` | Entity | MonitorPedidos.Web | Pedido simulado con `CreateNormal`/`CreateFailure`; tabla `simulated_orders` |
| LC-U7-02 | `SimulatedJobStatus` | Entity | MonitorPedidos.Web | Estado de job simulado con `MarkFailed`/`MarkCompleted`; tabla `simulated_job_statuses` |
| LC-U7-03 | `SimulationOptions` | Configuration record | MonitorPedidos.Web | `Enabled`, `InsertIntervalMinutes`, `FailureProbability`, `NoOrdersMode`; sección `"Simulation"` |
| LC-U7-04 | `ISimulatedOrderRepository` | Interfaz | MonitorPedidos.Web | `InsertRangeAsync`, `CountRecentAsync`, `DeleteOlderThanAsync` |
| LC-U7-05 | `SimulatedOrderRepository` | Clase (ISimulatedOrderRepository) | MonitorPedidos.Web | EF Core; `AddRangeAsync + SaveChangesAsync` por tick (ADR-U7-01) |
| LC-U7-06 | `ISimulatedJobStatusRepository` | Interfaz | MonitorPedidos.Web | `GetByJobNameAsync`, `UpdateAsync`, `GetAllAsync` |
| LC-U7-07 | `SimulatedJobStatusRepository` | Clase (ISimulatedJobStatusRepository) | MonitorPedidos.Web | EF Core; `GetAllAsync` para `JobsChecker` |
| LC-U7-08 | `OrdersSimulatorService` | BackgroundService | MonitorPedidos.Web | Singleton vía `AddHostedService`; `IServiceScopeFactory` (ADR-U7-01); `try/catch` sin rethrow (ADR-U7-03); `RunOneTickAsync` interno para tests (ADR-U7-02) |

---

## §2 Componentes de producción modificados en U7

| Componente | Unidad origen | Modificación |
|-----------|--------------|-------------|
| `AppDbContext` | U2 | Agrega `DbSet<SimulatedOrder> SimulatedOrders` + `DbSet<SimulatedJobStatus> SimulatedJobStatuses` |
| `DbOrderChecker` | U3/U5 | **Sin cambio de código** — ya consulta la tabla `simulated_orders`; la tabla simplemente ahora existe con datos |
| `JobsChecker` | U3 | **Sin cambio de código** — ya consulta `simulated_job_statuses`; la tabla ahora existe con seed |

---

## §3 Componentes de test introducidos en U7

| ID | Componente | Tipo | Proyecto | Escenarios |
|----|-----------|------|---------|-----------|
| TC-U7-01 | `OrdersSimulatorServiceTests` | xUnit + Moq | MonitorPedidos.UnitTests | 3 tests: NoOrdersMode → InsertRangeAsync Times.Never; FailureProbability=1.0 → todos IsFailure=true; FailureProbability=0.0 → todos IsFailure=false (ADR-U7-02) |

**Total tests U7:** 3

---

## §4 Artefacto adicional: `red-team-report-template.md`

| Artefacto | Ruta | Tipo |
|-----------|------|------|
| `red-team-report-template.md` | `aidlc-docs/construction/u7-simulation/` | Plantilla Markdown (completada por owner durante demo) |

La plantilla contiene la tabla de 6 escenarios con columnas: Escenario | Descripción | Pasos | Resultado Esperado | Estado | Evidencia. Se genera en Act4 como artefacto de cierre de U7.

---

## §5 Diagrama de dependencias U7

```
OrdersSimulatorService (BackgroundService — Singleton)
    IOptions<SimulationOptions>              ← ADR-U7-04: Configure<T> en Program.cs
    IServiceScopeFactory
        |
        scope por tick (await using)
            ISimulatedOrderRepository (Scoped)
                SimulatedOrderRepository
                    AppDbContext → tabla simulated_orders
                    AddRangeAsync + SaveChangesAsync  ← batch único por tick (NFR-U7-02)

    PeriodicTimer(InsertIntervalMinutes min)
    try/catch sin rethrow (excepto OperationCanceledException)  ← ADR-U7-03

SimulatedJobStatusRepository (Scoped)
    AppDbContext → tabla simulated_job_statuses
    seed: 2 filas [SalesforceDownload: Completed, MultivendeDownload: Completed]

DbOrderChecker (U3/U5 — sin cambio)
    → tabla simulated_orders  ← ahora tiene datos del simulador

JobsChecker (U3 — sin cambio)
    → tabla simulated_job_statuses  ← ahora tiene datos con seed

OrdersSimulatorServiceTests
    Mock<ISimulatedOrderRepository>
        Times.Never() para NoOrdersMode       ← ADR-U7-02
        It.Is<>(orders.All(o => o.IsFailure)) ← ADR-U7-02
    InternalsVisibleTo("MonitorPedidos.UnitTests") ← acceso a RunOneTickAsync

db/seed/
    rt1-job-fail.sql          ← UPDATE simulated_job_statuses → Failed
    rt1-job-reset.sql         ← UPDATE simulated_job_statuses → Completed
    rt7-insert-cancelled.sql  ← INSERT simulated_orders status=Cancelled
    cleanup-simulated.sql     ← DELETE simulated_orders WHERE created_at < NOW()-48h
```

---

## §6 Reglas de diseño aplicadas en U7

| ADR | Patrón | Componentes afectados |
|-----|--------|----------------------|
| ADR-U7-01 | `IServiceScopeFactory` — scope por tick | `OrdersSimulatorService` |
| ADR-U7-02 | `Times.Never` + `It.Is<>` + `InternalsVisibleTo` | `OrdersSimulatorServiceTests` |
| ADR-U7-03 | `try/catch` sin rethrow (solo re-lanza `OperationCanceledException`) | `OrdersSimulatorService` |
| ADR-U7-04 | `Configure<SimulationOptions>` + `IOptions<T>` | `Program.cs`, `OrdersSimulatorService` |
| ADR-U3-02 (IEnumerable DI) | `DbOrderChecker` y `JobsChecker` sin cambio — tablas ya existentes | — |

---

## §7 Trazabilidad de componentes

| Componente | RT | RF | NFR | ADR |
|-----------|-----|----|----|-----|
| `SimulatedOrder` | RT1, RT5, RT7 | C-06 | NFR-U7-02 | — |
| `SimulatedJobStatus` | RT1 | C-06 | — | — |
| `SimulationOptions` | todos | C-06 | NFR-U7-04 | ADR-U7-04 |
| `OrdersSimulatorService` | RT1, RT5 | C-06 | NFR-U7-01, NFR-U7-04 | ADR-U7-01, ADR-U7-03 |
| `ISimulatedOrderRepository` | RT1, RT5, RT7 | C-06 | NFR-U7-02 | ADR-U7-01 |
| `ISimulatedJobStatusRepository` | RT1 | C-06 | — | — |
| `OrdersSimulatorServiceTests` (3) | RT1, RT5 | C-06 | NFR-U7-01 | ADR-U7-02 |
