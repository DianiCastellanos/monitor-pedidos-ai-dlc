# Logical Components — U3 Detection & Classification

**Unidad:** U3 — Detection & Classification
**Stage:** Construction → NFR Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Vista general de componentes lógicos de U3

```
MonitorPedidos.Domain/Shared/
+-- CheckStatus                         (enum — Ok/Warn/Critical)
+-- CauseCategory                       (enum — 6 causas en cascada)

MonitorPedidos.Domain/Monitoring/
+-- CheckResult                         (value object — resultado del checker)
+-- CheckContext                        (value object — contexto para renderer)
+-- OrderSnapshot                       (value object — snapshot de pedido)
+-- JobStatusSnapshot                   (value object — snapshot de job)
+-- ICheckExecutor                      (contrato de checker — §2)
+-- IOrderSource                        (contrato de fuente de pedidos — §3)
+-- IJobStatusSource                    (contrato de fuente de jobs — §4)

MonitorPedidos.Web/Features/Monitoring/
+-- MonitoringSchedulerService          (BackgroundService singleton — §5)
+-- DbOrderChecker                      (ICheckExecutor scoped — §6)
+-- DbHealthChecker                     (ICheckExecutor scoped — §7)
+-- JobsChecker                         (ICheckExecutor scoped — §8)
+-- CauseClassifier                     (clase estática pura — §9)
+-- AlertTemplateRenderer               (clase estática pura — §10)
+-- SimulatedOrderRepository            (IOrderSource scoped — §11)
+-- SimulatedJobStatusRepository        (IJobStatusSource scoped — §12)

MonitorPedidos.Web/Services/
+-- MonitoringService                   (application service scoped — §13)
+-- IMonitoringService                  (contrato)

tests/MonitorPedidos.UnitTests/Monitoring/
+-- CauseClassifierTests                (T-U3-01)
+-- AlertTemplateRendererTests          (T-U3-02)
+-- DbOrderCheckerTests                 (T-U3-04)

tests/MonitorPedidos.IntegrationTests/Monitoring/
+-- DbHealthCheckerTests                (T-U3-03)
+-- MonitoringServiceTests              (T-U3-05)
```

---

## §2 Componente: ICheckExecutor

| Atributo | Detalle |
|----------|---------|
| **Tipo** | Interface (contrato de checker) |
| **Namespace** | `MonitorPedidos.Domain.Monitoring` |
| **Responsabilidad** | Define el contrato que todo checker debe implementar para ser descubierto por el scheduler |
| **Método** | `Task<CheckResult> ExecuteAsync(CancellationToken ct)` |
| **Propiedad** | `ModuleId Module` — identifica el módulo del que es responsable |

---

## §3 Componente: IOrderSource

| Atributo | Detalle |
|----------|---------|
| **Tipo** | Interface (contrato de fuente de pedidos) |
| **Namespace** | `MonitorPedidos.Domain.Monitoring` |
| **Responsabilidad** | Abstrae la fuente de datos de pedidos. Sprint 2: `SimulatedOrderRepository`. Post-MVP: repositorio real |
| **Método** | `Task<IReadOnlyList<OrderSnapshot>> GetOrdersInWindowAsync(from, to, ct)` |

---

## §4 Componente: IJobStatusSource

| Atributo | Detalle |
|----------|---------|
| **Tipo** | Interface (contrato de fuente de jobs) |
| **Namespace** | `MonitorPedidos.Domain.Monitoring` |
| **Responsabilidad** | Abstrae la fuente de estado de jobs. Sprint 2: `SimulatedJobStatusRepository`. U4: estado real |
| **Método** | `Task<IReadOnlyList<JobStatusSnapshot>> GetCurrentStatusAsync(ct)` |

---

## §5 Componente: MonitoringSchedulerService

| Atributo | Detalle |
|----------|---------|
| **Tipo** | `BackgroundService` (singleton) |
| **Namespace** | `MonitorPedidos.Web.Features.Monitoring` |
| **Responsabilidad** | Dispara el ciclo de detección cada N minutos (configurable) |
| **Ciclo de vida** | Singleton — arranca con la app, se detiene con graceful shutdown |
| **Registro DI** | `services.AddHostedService<MonitoringSchedulerService>()` |
| **Dependencias** | `IServiceScopeFactory`, `IConfiguration`, `ILogger<>` |

**Flujo interno (ADR-U3-02 + ADR-U3-03):**

```
ExecuteAsync(stoppingToken)
    |
    PeriodicTimer(CheckerIntervalMinutes)
    |
    while WaitForNextTickAsync(stoppingToken)
        |
        scope = IServiceScopeFactory.CreateAsyncScope()
        checkers = scope.GetServices<ICheckExecutor>()
        Task.WhenAll(checkers.Select(c => RunCheckAsync(c, stoppingToken)))
```

---

## §6 Componente: DbOrderChecker (M2)

| Atributo | Detalle |
|----------|---------|
| **Tipo** | `ICheckExecutor` (scoped) |
| **Namespace** | `MonitorPedidos.Web.Features.Monitoring` |
| **Module** | `ModuleId.DbOrders` |
| **Responsabilidad** | Detecta ausencia de pedidos activos en la ventana de detección |
| **Dependencias** | `IOrderSource`, `IConfiguration` |
| **Resultado Critical** | 0 pedidos activos en últimos N minutos |
| **Resultado Ok** | ≥ 1 pedido activo en la ventana |

---

## §7 Componente: DbHealthChecker (M4)

| Atributo | Detalle |
|----------|---------|
| **Tipo** | `ICheckExecutor` (scoped) |
| **Namespace** | `MonitorPedidos.Web.Features.Monitoring` |
| **Module** | `ModuleId.DbHealth` |
| **Responsabilidad** | Verifica conectividad y latencia de la BD con `SELECT 1` |
| **Dependencias** | `AppDbContext`, `IConfiguration` |
| **Resultado Warn** | Latencia > `DbWarnLatencyMs` (default: 1000 ms) |
| **Resultado Critical** | BD no responde o timeout > `DbCritTimeoutMs` (default: 5000 ms) |

---

## §8 Componente: JobsChecker (M11)

| Atributo | Detalle |
|----------|---------|
| **Tipo** | `ICheckExecutor` (scoped) |
| **Namespace** | `MonitorPedidos.Web.Features.Monitoring` |
| **Module** | `ModuleId.Jobs` |
| **Responsabilidad** | Verifica que los jobs de integración estén activos y con última ejecución exitosa |
| **Dependencias** | `IJobStatusSource` |
| **Resultado Critical** | ≥ 1 job detenido o con última ejecución fallida |
| **Resultado Ok** | Todos los jobs activos y con última ejecución exitosa |

---

## §9 Componente: CauseClassifier (M7)

| Atributo | Detalle |
|----------|---------|
| **Tipo** | Clase estática pura |
| **Namespace** | `MonitorPedidos.Web.Features.Monitoring` |
| **Responsabilidad** | Determina la `CauseCategory` a partir del tipo del checker. Sin estado, sin BD |
| **Método** | `static CauseCategory Classify(ICheckExecutor checker)` |
| **Extensión en U4** | Agregar `[typeof(ApiChecker)] = CauseCategory.Api` y `[typeof(TokenChecker)] = CauseCategory.Token` en `_map` |

---

## §10 Componente: AlertTemplateRenderer (M10)

| Atributo | Detalle |
|----------|---------|
| **Tipo** | Clase estática pura |
| **Namespace** | `MonitorPedidos.Web.Features.Monitoring` |
| **Responsabilidad** | Renderiza `AlertMessage` (6 campos en español) desde diccionario estático C# |
| **Método** | `static AlertMessage Render(CheckContext ctx)` |
| **Fallback** | Si no hay plantilla para la combinación (causa, severidad) → `Fallback(ctx)` nunca lanza excepción |

---

## §11 Componente: SimulatedOrderRepository

| Atributo | Detalle |
|----------|---------|
| **Tipo** | `IOrderSource` (scoped) |
| **Namespace** | `MonitorPedidos.Web.Features.Monitoring` |
| **Responsabilidad** | Implementación de `IOrderSource` para Sprint 2 — lee de tabla `simulated_orders` (creada en U7) |
| **Registro DI** | `services.AddScoped<IOrderSource, SimulatedOrderRepository>()` |
| **Reemplazo post-MVP** | Intercambiar por `RealOrderRepository` en Program.cs sin cambiar `DbOrderChecker` |

---

## §12 Componente: SimulatedJobStatusRepository

| Atributo | Detalle |
|----------|---------|
| **Tipo** | `IJobStatusSource` (scoped) |
| **Namespace** | `MonitorPedidos.Web.Features.Monitoring` |
| **Responsabilidad** | Implementación de `IJobStatusSource` para Sprint 2 — lee de tabla `simulated_job_statuses` (creada en U7) |
| **Registro DI** | `services.AddScoped<IJobStatusSource, SimulatedJobStatusRepository>()` |

---

## §13 Componente: MonitoringService

| Atributo | Detalle |
|----------|---------|
| **Tipo** | Application Service (scoped) |
| **Namespace** | `MonitorPedidos.Web.Services` |
| **Clase** | `MonitoringService : IMonitoringService` |
| **Registro DI** | `services.AddScoped<IMonitoringService, MonitoringService>()` |
| **Dependencias** | `IIncidentService`, `INotificationService` (stub U2→real U6), `ILogger<MonitoringService>` |

**Interacciones:**

```
MonitoringService
    |-- RunCheckAsync(checker, ct) -->
    |       CheckResult = checker.ExecuteAsync(ct)
    |       [WARN/CRITICAL] -->
    |           cause   = CauseClassifier.Classify(checker)
    |           context = new CheckContext(...)
    |           alert   = AlertTemplateRenderer.Render(context)
    |           incident = IIncidentService.OpenIncidentAsync(...)
    |           INotificationService.BroadcastAlertAsync(incident.Id, alert, ct)
    |       [OK] -->
    |           IIncidentService.TryCloseOnConsecutiveOkAsync(checker.Module, ct)
```

---

## §14 Suite de tests

| Test | Proyecto | Clase | Método | NFR / BR verificado |
|------|---------|-------|--------|---------------------|
| T-U3-01 | UnitTests | `CauseClassifierTests` | `Classify_EachCheckerType_ReturnsExpectedCause` | BR-CLASS-01, BR-CLASS-02 |
| T-U3-02 | UnitTests | `AlertTemplateRendererTests` | `Render_AllCombinations_SixFieldsNonEmpty` | BR-ALERT-01, BR-ALERT-03 |
| T-U3-03 | IntegrationTests | `DbHealthCheckerTests` | `ExecuteAsync_LocalDbRunning_ReturnsOk` | BR-HEALTH-01, BR-HEALTH-02 |
| T-U3-04 | UnitTests | `DbOrderCheckerTests` | `ExecuteAsync_NoActiveOrders_ReturnsCritical` | BR-DET-04, BR-DET-05 |
| T-U3-05 | IntegrationTests | `MonitoringServiceTests` | `RunCheckAsync_WarnResult_OpensIncidentAndBroadcasts` | BR-DET-02, ADR-U3-04 |

---

## §15 Trazabilidad completa

| Componente | ADR | NFR | Story | SECURITY |
|-----------|-----|-----|-------|----------|
| Linked CancellationToken en RunCheckAsync | ADR-U3-01 | NFR-U3-01 | US-07..09 | SECURITY-15 |
| IEnumerable\<ICheckExecutor\> en DI | ADR-U3-02 | — | US-07..09 | — |
| PeriodicTimer + WaitForNextTickAsync | ADR-U3-03 | NFR-U3-01 | US-07..09 | SECURITY-15 |
| Moq para INotificationService en tests | ADR-U3-04 | NFR-U3-03 | US-06 | — |
| CauseClassifier (estático puro) | — | NFR-U3-02 | US-07..09, US-18 | — |
| AlertTemplateRenderer (estático puro) | — | NFR-U3-02 | US-06 | SECURITY-14 |
| SimulatedOrderRepository | — | — | US-07 | SECURITY-05 |
| SimulatedJobStatusRepository | — | — | US-09 | SECURITY-05 |
