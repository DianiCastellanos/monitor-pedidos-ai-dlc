# Business Rules — U3 Detection & Classification

**Unidad:** U3 — Detection & Classification
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 BR-DET — Reglas de detección

| ID | Regla | Implementación | Story | RF |
|----|-------|---------------|-------|----|
| BR-DET-01 | Todos los checkers (M2, M4, M11) se ejecutan cada **5 minutos** vía un único `PeriodicTimer` | `MonitoringSchedulerService` — `PeriodicTimer(TimeSpan.FromMinutes(5))` | US-07, US-08, US-09 | RF-01, RF-04, RF-05 |
| BR-DET-02 | Solo `CheckStatus.Warn` o `CheckStatus.Critical` generan un incidente; `Ok` lo cierra si había uno abierto | `MonitoringService.RunCheckAsync` — `if result.RequiresIncident` | US-07, US-08, US-09 | RF-08 |
| BR-DET-03 | Si un checker lanza excepción no manejada, el orquestador la captura, la loggea y continúa con los demás checkers (**aislamiento**) | try-catch en `RunCheckAsync` por checker individual | US-07..09 | RF-09 |
| BR-DET-04 | La ventana de detección de ausencia de pedidos es configurable vía `appsettings.json` (`Monitoring:OrderDetectionWindowMinutes`, default: **30**) | `DbOrderChecker` — `_config.GetValue(...)` | US-07 | RF-01 |
| BR-DET-05 | Los pedidos **cancelados** no se contabilizan al evaluar ausencia — solo cuentan pedidos activos | `DbOrderChecker` — `orders.Where(o => !o.IsCancelled)` | US-07 | RF-02 |

---

## §2 BR-SCHED — Reglas del scheduler

| ID | Regla | Implementación | Story | RF |
|----|-------|---------------|-------|----|
| BR-SCHED-01 | `MonitoringSchedulerService` arranca automáticamente al iniciar la aplicación y se detiene limpiamente con graceful shutdown | `BackgroundService` registrado con `AddHostedService<>()` | US-07..09 | RF-01 |
| BR-SCHED-02 | Los 3 checkers se ejecutan **en paralelo** (`Task.WhenAll`) — ninguno bloquea a los otros | `MonitoringSchedulerService` — `Task.WhenAll(...)` | US-07..09 | RF-01 |
| BR-SCHED-03 | El scheduler no propaga la excepción de un checker individual — cada `RunCheckAsync` es independiente | try-catch por checker dentro del `WhenAll` | US-07..09 | RF-09 |

---

## §3 BR-CLASS — Reglas de clasificación

| ID | Regla | Implementación | Story | RF |
|----|-------|---------------|-------|----|
| BR-CLASS-01 | La `CauseCategory` se determina por el **tipo** del checker: `DbOrderChecker`/`DbHealthChecker` → `Bd`, `JobsChecker` → `Job` | `CauseClassifier._map` (diccionario estático) | US-07, US-08, US-09 | RF-08 |
| BR-CLASS-02 | Si el tipo del checker no tiene entrada en el mapa → `CauseCategory.NoDeterminada` | `CauseClassifier.Classify` — fallback en `TryGetValue` | US-18 | RF-10 |
| BR-CLASS-03 | Un incidente con `Cause == NoDeterminada` queda marcado `IsCandidatoReglaNueva = true` | `MonitoringService.RunCheckAsync` — set antes de llamar `OpenIncidentAsync` | US-18 | RF-10 |
| BR-CLASS-04 | La clasificación es **sin estado** — no consulta BD ni servicios externos; solo depende del tipo del checker y su `CheckResult` | `CauseClassifier` es una clase estática pura | US-07..09 | RF-08 |

---

## §4 BR-ALERT — Reglas de renderizado de alertas

| ID | Regla | Implementación | Story | RF |
|----|-------|---------------|-------|----|
| BR-ALERT-01 | Todo WARN/CRITICAL genera un `AlertMessage` con los **6 campos** completos — ninguno nulo ni vacío | `AlertTemplateRenderer.Render` + `Fallback` garantiza campos no nulos | US-06 | RF-11 |
| BR-ALERT-02 | Los campos se generan **desde plantillas C#** — no mediante concatenación ad-hoc en el checker | `AlertTemplateRenderer._templates` — diccionario de funciones | US-06 | RF-13 |
| BR-ALERT-03 | Si no existe plantilla para la combinación (causa, severidad), se usa la plantilla **genérica de fallback** — nunca se lanza excepción | `AlertTemplateRenderer.Fallback` | US-06 | RF-13 |
| BR-ALERT-04 | El campo `SeveridadTexto` usa los literales en español: **"CRÍTICO"** para Critical, **"ADVERTENCIA"** para Warn | Plantillas y `Fallback` en `AlertTemplateRenderer` | US-06 | RF-12 |
| BR-ALERT-05 | El campo `Cuando` muestra la hora **local** (no UTC) en formato `dd/MM/yyyy HH:mm` | `ctx.DetectedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")` | US-06 | RF-11 |

---

## §5 BR-SOURCE — Reglas de fuentes de datos

| ID | Regla | Implementación | Story | RF |
|----|-------|---------------|-------|----|
| BR-SOURCE-01 | `DbOrderChecker` accede a los pedidos **únicamente** a través de `IOrderSource` — nunca directamente a `AppDbContext` | `DbOrderChecker` depende de `IOrderSource` por DI | US-07 | RF-01 |
| BR-SOURCE-02 | `JobsChecker` accede al estado de jobs **únicamente** a través de `IJobStatusSource` — nunca directamente | `JobsChecker` depende de `IJobStatusSource` por DI | US-09 | RF-05 |
| BR-SOURCE-03 | En Sprint 2, `SimulatedOrderRepository` e `SimulatedJobStatusRepository` son las implementaciones concretas registradas en DI. Post-MVP se intercambian sin modificar los checkers | Registro DI en `Program.cs` | US-07, US-09 | — |

---

## §6 BR-HEALTH — Reglas del health check de BD

| ID | Regla | Implementación | Story | RF |
|----|-------|---------------|-------|----|
| BR-HEALTH-01 | `DbHealthChecker` emite **WARN** cuando la latencia supera `DbWarnLatencyMs` (default: **1000 ms**) | `DbHealthChecker` — `sw.ElapsedMilliseconds > WarnLatencyMs` | US-08 | RF-04 |
| BR-HEALTH-02 | `DbHealthChecker` emite **CRITICAL** cuando la BD no responde o excede `DbCritTimeoutMs` (default: **5000 ms**) | `CancellationTokenSource.CancelAfter(CritTimeoutMs)` + catch | US-08 | RF-04 |
| BR-HEALTH-03 | El health check **no reintenta** — un fallo inmediato es CRITICAL (no espera retry) | sin lógica de retry en `DbHealthChecker` | US-08 | RF-09 |

---

## §7 Invariantes del bounded context Monitoring

| ID | Invariante | Verificación |
|----|-----------|-------------|
| INV-MON-01 | Todo `ICheckExecutor` debe declarar un `Module` válido (no null) | Validado en `MonitoringSchedulerService` al registrar los checkers |
| INV-MON-02 | `AlertMessage` generado por `AlertTemplateRenderer` nunca tiene campos nulos | Garantizado por `Fallback` — cubre toda combinación posible |
| INV-MON-03 | `CauseClassifier` es determinista — el mismo tipo de checker siempre produce la misma causa | Diccionario inmutable (`IReadOnlyDictionary`) — sin estado mutable |

---

## §8 Trazabilidad completa

| Categoría | Reglas | Stories | RFs |
|-----------|--------|---------|-----|
| BR-DET (5) | BR-DET-01..05 | US-07, US-08, US-09 | RF-01, RF-02, RF-04, RF-05, RF-08, RF-09 |
| BR-SCHED (3) | BR-SCHED-01..03 | US-07..09 | RF-01, RF-09 |
| BR-CLASS (4) | BR-CLASS-01..04 | US-07, US-08, US-09, US-18 | RF-08, RF-10 |
| BR-ALERT (5) | BR-ALERT-01..05 | US-06 | RF-11, RF-12, RF-13 |
| BR-SOURCE (3) | BR-SOURCE-01..03 | US-07, US-09 | RF-01, RF-05 |
| BR-HEALTH (3) | BR-HEALTH-01..03 | US-08 | RF-04, RF-09 |
| **Total** | **23 reglas** | US-06..09, US-18 | RF-01..05, RF-08..13 |
