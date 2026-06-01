# Business Rules — U3 Detection & Classification

**Unidad:** U3 — Detection & Classification
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-23
**Versión:** 1.1 (2026-05-31 — BR-DET-01 actualizado: 3 timers IT4/IT7; BR-DET-03 actualizado: graceful degradation IT9; BR-SCHED-04 nuevo; BR-SOURCE-01/02 actualizados: fuentes reales IT1/IT5)

---

## §1 BR-DET — Reglas de detección

| ID | Regla | Implementación | Story | RF |
|----|-------|---------------|-------|----|
| BR-DET-01 | Los checkers se ejecutan en **3 timers independientes**: Timer1 (M2/M4/M11, 5 min prod/30s dev), Timer2 (APIs M3, 5 min prod/1 min dev), Timer3 dinámico (Brand Monitor, 3 min prod/1 min dev). Intervalos configurables en `appsettings.json`. | `MonitoringSchedulerService` — `Task.WhenAll(Timer1Loop, Timer2Loop, Timer3Loop)` | US-07, US-08, US-09, US-10 | RF-01, RF-03, RF-04, RF-05 |
| BR-DET-02 | Solo `CheckStatus.Warn` o `CheckStatus.Critical` generan un incidente; `Ok` lo cierra si había uno abierto | `MonitoringService.RunCheckAsync` — `if result.RequiresIncident` | US-07, US-08, US-09 | RF-08 |
| BR-DET-03 | Cada checker (`DbOrderChecker`, `JobsChecker`, `BrandMonitorChecker`) tiene try-catch interno que convierte excepciones en `CheckResult.Critical(mensaje limpio)`. `MonitoringService.RunCheckAsync` siempre actualiza `LastCheckStore`, incluso en caso de excepción del checker (**graceful degradation IT9**). | try-catch en checker + try-catch en `RunCheckAsync` | US-07..09 | RF-09 |
| BR-DET-04 | La ventana de detección de ausencia de pedidos es configurable vía `appsettings.json` (`Monitoring:OrderDetectionWindowMinutes`, default: **30**) | `DbOrderChecker` — `_config.GetValue(...)` | US-07 | RF-01 |
| BR-DET-05 | Los pedidos **cancelados** no se contabilizan al evaluar ausencia — solo cuentan pedidos activos | `DbOrderChecker` — `orders.Where(o => !o.IsCancelled)` | US-07 | RF-02 |

---

## §2 BR-SCHED — Reglas del scheduler

| ID | Regla | Implementación | Story | RF |
|----|-------|---------------|-------|----|
| BR-SCHED-01 | `MonitoringSchedulerService` arranca automáticamente al iniciar la aplicación y se detiene limpiamente con graceful shutdown | `BackgroundService` registrado con `AddHostedService<>()` | US-07..09 | RF-01 |
| BR-SCHED-02 | Los checkers de cada timer se ejecutan **en secuencia** dentro de su loop (no en paralelo) para evitar condiciones de concurrencia en el `DbContext` | `RunTimerLoopAsync` — foreach sin Task.WhenAll | US-07..09 | RF-01 |
| BR-SCHED-03 | El scheduler no propaga la excepción de un checker individual — cada loop tiene try-catch por checker | try-catch por checker dentro del loop | US-07..09 | RF-09 |
| BR-SCHED-04 | El timer de Brand Monitor es dinámico (re-lee `BrandMonitorPollIntervalMinutes` cada iteración) — permite actualizar la frecuencia sin reiniciar la app | `RunBrandTimerLoopAsync` — `Task.Delay` con lectura de config en cada iteración | — | RF-03 |

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
| BR-SOURCE-01 | `DbOrderChecker` accede a los pedidos **únicamente** a través de `IOrderSource` — nunca directamente a `AppDbContext`. En producción: `ProductionOrderRepository` (SQL Server, BD `oc_encabezado`, solo lectura, Dapper). En desarrollo: `SimulatedOrderRepository` | `DbOrderChecker` depende de `IOrderSource` por DI; `IOrderSource` condicional en `Program.cs` según `ASPNETCORE_ENVIRONMENT` | US-07 | RF-01 |
| BR-SOURCE-02 | `JobsChecker` accede al estado de jobs vía `schtasks.exe` consultando Windows Task Scheduler en `SR-SDEV02CO.patprimo.local`. Configurado en `JobsMonitor:Server` y `JobsMonitor:TaskName` | `JobsChecker` — schtasks con timeout `JobsMonitor:CommandTimeoutMs` (default 10s) | US-09 | RF-05 |
| BR-SOURCE-03 | La implementación de `IOrderSource` es condicional: `ProductionOrderRepository` en producción, `SimulatedOrderRepository` en desarrollo. Sin modificar los checkers al cambiar de entorno | Registro DI en `Program.cs` según `ASPNETCORE_ENVIRONMENT` | US-07, US-09 | — |

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
