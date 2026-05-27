# Business Logic Model — U3 Detection & Classification

**Unidad:** U3 — Detection & Classification
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Decisiones aplicadas

| ID | Pregunta | Decisión |
|----|----------|---------|
| P1 | Contrato ICheckExecutor | A — `Task<CheckResult> ExecuteAsync(ct)` retorna value object |
| P2 | Mecanismo de clasificación | A — causa determinada por tipo de checker (sin reglas externas) |
| P3 | Almacenamiento de plantillas | A — diccionario estático C# en `AlertTemplateRenderer` |
| P4 | Abstracción fuente de pedidos | A — `IOrderSource` + `SimulatedOrderRepository` para Sprint 2 |
| P5 | Timers del scheduler | A — un `PeriodicTimer` cada 5 min + `Task.WhenAll` de los 3 checkers |

---

## §2 Flujo 1 — Ciclo principal de detección (orquestado por MonitoringService)

Este es el flujo más importante de U3: lo que ocurre cada 5 minutos.

```
MonitoringSchedulerService
    |
    +-- PeriodicTimer (5 min) dispara
    |
    v
Task.WhenAll([
    RunCheckAsync(DbOrderChecker,  ModuleId.DbOrders),
    RunCheckAsync(DbHealthChecker, ModuleId.DbHealth),
    RunCheckAsync(JobsChecker,     ModuleId.Jobs)
])
    |
    +-- por cada checker, en paralelo -->
    |
    v
MonitoringService.RunCheckAsync(checker, ct)
    |
    +-- CheckResult = await checker.ExecuteAsync(ct)
    |
    +-- [CheckResult.Status == OK] -->
    |       TryAutoCloseAsync(checker.Module, ct)   [delega a IIncidentService]
    |       return
    |
    +-- [CheckResult.Status == WARN o CRITICAL] -->
            cause   = CauseClassifier.Classify(checker)
            context = new CheckContext(checker.Module, result.Status, cause,
                                       result.Details, result.CheckedAt)
            alert   = AlertTemplateRenderer.Render(context)
            severity = result.Status == Critical ? Severity.Critical : Severity.Warn
            incident = await IIncidentService.OpenIncidentAsync(
                           checker.Module, cause, severity, alert, ct)
            await INotificationService.BroadcastAlertAsync(incident.Id, alert, ct)
            _logger.LogInformation("Incident {Id} opened for {Module}", incident.Id, checker.Module)
```

**Aislamiento de fallos:** si un checker lanza excepción no manejada, `RunCheckAsync` la captura con try-catch, loggea el error y retorna sin interrumpir los demás checkers (BR-DET-03).

---

## §3 Flujo 2 — Clasificación por tipo de checker (M7)

```
CauseClassifier.Classify(checker)
    |
    +-- checker.GetType() --> lookup en _map (diccionario estático)
    |
    +-- [DbOrderChecker]   --> CauseCategory.Bd
    +-- [DbHealthChecker]  --> CauseCategory.Bd
    +-- [JobsChecker]      --> CauseCategory.Job
    +-- [ApiChecker  U4]   --> CauseCategory.Api      (se agregará en U4)
    +-- [TokenChecker U4]  --> CauseCategory.Token    (se agregará en U4)
    +-- [sin match]        --> CauseCategory.NoDeterminada
```

Cuando la causa es `NoDeterminada`, `MonitoringService` marca el incidente como `IsCandidatoReglaNueva = true` antes de persistirlo (BR-CLASS-03).

---

## §4 Flujo 3 — Renderizado de alerta (M10)

```
AlertTemplateRenderer.Render(context)
    |
    +-- severity = context.Status == Critical ? Severity.Critical : Severity.Warn
    |
    +-- lookup en _templates[(context.Cause, severity)]
    |
    +-- [plantilla encontrada] --> invocar función con context --> AlertMessage (6 campos)
    |
    +-- [plantilla NO encontrada] --> Fallback(context) --> AlertMessage genérico
    |
    v
AlertMessage {
    QuePaso, Cuando, Donde, SeveridadTexto, CausaProbable, AccionSugerida
}
    (todos los campos no nulos, en español)
```

---

## §5 Flujo 4 — Cierre automático por 2 OK consecutivos

Este flujo ya está implementado en `IIncidentService` (U2 — `TryCloseOnConsecutiveOkAsync`). U3 lo invoca desde `MonitoringService` cuando un checker retorna OK.

```
MonitoringService — resultado OK para ModuleId X
    |
    v
IIncidentService.TryCloseOnConsecutiveOkAsync(moduleId, ct)
    |
    +-- [no hay incidente abierto para X] --> return false (noop)
    |
    +-- [hay incidente abierto] -->
            ¿últimos 2 resultados del módulo X = OK?
                +-- [No] --> return false
                +-- [Sí] --> incident.CloseAutomatically()
                             SaveChanges
                             INotificationService.BroadcastCloseAsync(id, ct)
                             return true
```

---

## §6 Flujo 5 — Detección de ausencia de pedidos (M2 + IOrderSource)

```
DbOrderChecker.ExecuteAsync(ct)
    |
    +-- to   = DateTimeOffset.UtcNow
    +-- from = to - DetectionWindow (default: 30 min — RF-01)
    |
    v
IOrderSource.GetOrdersInWindowAsync(from, to, ct)
    |
    +-- [Sprint 2] SimulatedOrderRepository --> SELECT de simulated_orders WHERE CreatedAt BETWEEN from AND to
    |
    +-- activeOrders = orders.Where(o => !o.IsCancelled)
    |
    +-- [activeOrders.Count == 0] --> CheckResult.Critical("Sin pedidos activos en últimos N min.")
    +-- [activeOrders.Count > 0]  --> CheckResult.Ok("{n} pedido(s) activos en ventana.")
```

**Pedidos cancelados se ignoran** (RT-7 del PRD): un cancelado no cuenta como pedido esperado.

---

## §7 Flujo 6 — Health check de BD (M4)

```
DbHealthChecker.ExecuteAsync(ct)
    |
    +-- Stopwatch.StartNew()
    +-- CancellationTokenSource con timeout = DbCritTimeoutMs (default: 5000 ms)
    |
    v
AppDbContext.Database.ExecuteSqlRawAsync("SELECT 1", linkedCts)
    |
    +-- [excepción o timeout] --> CheckResult.Critical("BD no responde: {mensaje}")
    |
    +-- [OK] --> sw.Stop()
                +-- [ms > DbWarnLatencyMs (1000)] --> CheckResult.Warn("Latencia alta: {ms} ms.")
                +-- [ms ≤ DbWarnLatencyMs]         --> CheckResult.Ok("BD responde en {ms} ms.")
```

---

## §8 Flujo 7 — Verificación de jobs (M11 + IJobStatusSource)

```
JobsChecker.ExecuteAsync(ct)
    |
    v
IJobStatusSource.GetCurrentStatusAsync(ct)
    |
    +-- [Sprint 2] SimulatedJobStatusRepository --> SELECT de simulated_job_statuses
    |
    +-- failed = jobs.Where(j => !j.IsRunning || !j.LastExecutionSucceeded)
    |
    +-- [failed.Count == 0] --> CheckResult.Ok("{n} job(s) activos y saludables.")
    +-- [failed.Count > 0]  --> CheckResult.Critical("{n} job(s) fallido(s): {nombres}")
```

---

## §9 Flujo 8 — Scheduler: arranque y ciclo de vida (M1)

```
MonitoringSchedulerService.ExecuteAsync(stoppingToken)
    |
    +-- PeriodicTimer timer = new(TimeSpan.FromMinutes(5))
    |
    +-- loop: while await timer.WaitForNextTickAsync(stoppingToken)
    |           Task.WhenAll([
    |               RunCheckAsync(DbOrderChecker,  stoppingToken),
    |               RunCheckAsync(DbHealthChecker, stoppingToken),
    |               RunCheckAsync(JobsChecker,     stoppingToken)
    |           ])
    |
    +-- [stoppingToken cancelado] --> exit loop → graceful shutdown
```

Cada `RunCheckAsync` tiene su propio try-catch — un checker que falla no cancela el `WhenAll` (BR-SCHED-03).

---

## §10 Dependencias entre bounded contexts

```
U3 (Monitoring)
    |-- consume --> IIncidentService          (U2 — abrir / cerrar incidentes)
    |-- consume --> IOrderSource              (U3 Sprint2: SimulatedOrderRepository)
    |-- consume --> IJobStatusSource          (U3 Sprint2: SimulatedJobStatusRepository)
    |-- produce --> AlertMessage              (U2 — persistido en Incident.Alert)
    |-- notifica -> INotificationService      (U6 — broadcast; stub en Sprint 2)
    |-- extiende -> CauseClassifier._map      (U4 agregará Api y Token en _map)
```

---

## §11 Trazabilidad de flujos

| Flujo | Story | RF | BR |
|-------|-------|----|----|
| Ciclo principal (§2) | US-07, US-08, US-09 | RF-01, RF-04, RF-05 | BR-DET-01, BR-DET-03 |
| Clasificación por tipo (§3) | US-07, US-18 | RF-08, RF-10 | BR-CLASS-01, BR-CLASS-03 |
| Renderizado alerta (§4) | US-06 | RF-11, RF-12, RF-13 | BR-ALERT-01, BR-ALERT-02 |
| Cierre automático (§5) | US-15 | RF-19 | BR-AUTOCLOSE-01 (en U2) |
| Detección pedidos (§6) | US-07 | RF-01, RF-02 | BR-DET-02, BR-SOURCE-01 |
| Health check BD (§7) | US-08 | RF-04 | BR-DET-02 |
| Verificación jobs (§8) | US-09 | RF-05 | BR-DET-02, BR-SOURCE-02 |
| Scheduler ciclo de vida (§9) | US-07..09 | RF-01 | BR-SCHED-01, BR-SCHED-02 |
