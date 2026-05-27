# NFR Requirements — U3 Detection & Classification

**Unidad:** U3 — Detection & Classification
**Stage:** Construction → NFR Requirements
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 NFRs heredados de Inception y U1 (aplican sin cambios)

| ID | Requerimiento | Aplicación en U3 |
|----|---------------|-----------------|
| RNF-01 | Respuesta UI < 2 segundos | N/A — U3 no tiene páginas Blazor |
| RNF-06 | Cifrado at-rest | Heredado de U1 — `SimulatedOrderRepository` y `SimulatedJobStatusRepository` usan `AppDbContext` ya configurado |
| RNF-08 | Logging sin PII | Los logs de U3 incluyen solo `ModuleId`, `CauseCategory`, `IncidentId` — sin datos de pedidos, usuarios ni credenciales |
| RNF-10 | Queries parametrizadas | EF Core genera parámetros automáticamente en `SimulatedOrderRepository` y `DbHealthChecker` |
| RNF-11 | Autorización en todas las rutas | N/A — U3 no expone rutas HTTP |
| RNF-15 | Exception handling fail-closed | `MonitoringSchedulerService` captura excepciones por checker con try-catch; el `BackgroundService` base relanza si `ExecuteAsync` falla completamente, lo cual reinicia el servicio (comportamiento seguro) |

---

## §2 NFRs de implementación — Decisiones de U3

### NFR-U3-01 — Timeout independiente por checker (30 s)

**Decisión:** P1 = A

Cada `RunCheckAsync` envuelve su checker en un `CancellationTokenSource` vinculado con timeout configurable. Si el checker supera ese tiempo, se cancela y el ciclo de los demás continúa sin interrupción.

```csharp
var timeout = _config.GetValue("Monitoring:CheckerTimeoutMs", 30_000);
using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
cts.CancelAfter(timeout);
try
{
    result = await checker.ExecuteAsync(cts.Token);
}
catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
{
    _logger.LogError("Checker {Module} timed out after {Ms} ms.", checker.Module, timeout);
    return; // no abre incidente — timeout no es un check result válido
}
```

| Atributo | Valor |
|----------|-------|
| **Tipo** | Timeout de operación (confiabilidad) |
| **Default** | 30 000 ms (30 s) |
| **Config key** | `Monitoring:CheckerTimeoutMs` |
| **Cumple** | BR-DET-03 (aislamiento), BR-SCHED-03, RNF-15 |

---

### NFR-U3-02 — Tests unitarios para componentes puros (M7, M10)

**Decisión:** P2 = A

`CauseClassifier` y `AlertTemplateRenderer` son clases estáticas puras (sin BD, sin DI). Se testean con **unit tests xUnit directos**, sin `WebApplicationFactory`.

```csharp
// Ejemplo: CauseClassifierTests.cs
[Fact]
public void Classify_DbOrderChecker_ReturnsBd()
{
    var checker = new DbOrderChecker(/* mocked */);
    var cause   = CauseClassifier.Classify(checker);
    Assert.Equal(CauseCategory.Bd, cause);
}

// Ejemplo: AlertTemplateRendererTests.cs
[Fact]
public void Render_BdCritical_AllSixFieldsNonEmpty()
{
    var ctx    = new CheckContext(ModuleId.DbOrders, CheckStatus.Critical,
                                  CauseCategory.Bd, "detail", DateTimeOffset.UtcNow);
    var alert  = AlertTemplateRenderer.Render(ctx);
    Assert.NotEmpty(alert.QuePaso);
    Assert.NotEmpty(alert.AccionSugerida);
    // ... 6 campos
}
```

| Atributo | Valor |
|----------|-------|
| **Tipo** | Unit tests (sin infraestructura) |
| **Tests planificados** | T-U3-01 (CauseClassifier — 6 causas), T-U3-02 (AlertTemplateRenderer — plantillas + fallback) |
| **Velocidad** | < 50 ms por test (sin BD) |
| **Cumple** | Cobertura de BR-CLASS y BR-ALERT |

---

### NFR-U3-03 — Tests de integración con LocalDB para checkers con BD

**Decisión:** P3 = A

Los checkers que dependen de BD o de repositorios con tabla real se testean de forma diferenciada:

| Checker | Tipo de test | Razón |
|---------|-------------|-------|
| `DbHealthChecker` (M4) | Integration test con LocalDB real | Necesita `SELECT 1` real — un mock no valida la conectividad |
| `DbOrderChecker` (M2) | Unit test con mock de `IOrderSource` | `IOrderSource` es la abstracción; el comportamiento del checker no depende de SQL |
| `JobsChecker` (M11) | Unit test con mock de `IJobStatusSource` | Misma razón que M2 |
| `MonitoringService` | Integration test con LocalDB + mocks de `INotificationService` | Valida el flujo completo: checker → incident → notification stub |

```csharp
// T-U3-03 — DbHealthChecker con BD real
[Fact]
public async Task DbHealthChecker_LocalDbRunning_ReturnsOk()
{
    // Usa la misma TestWebAppFactory de U1/U2
    var checker = _factory.Services.GetRequiredService<DbHealthChecker>();
    var result  = await checker.ExecuteAsync();
    Assert.Equal(CheckStatus.Ok, result.Status);
}
```

| Atributo | Valor |
|----------|-------|
| **Tests planificados** | T-U3-03 (DbHealthChecker), T-U3-04 (DbOrderChecker mock), T-U3-05 (MonitoringService flujo completo) |
| **Infraestructura** | Reutiliza `TestWebAppFactory` de U1/U2 para tests con BD |
| **Cumple** | NFR-U2-04 (patrón LocalDB real — extendido a U3) |

---

### NFR-U3-04 — Logging diferenciado por relevancia operacional

**Decisión:** P4 = A

Con el scheduler corriendo cada 5 minutos, en 8 horas de operación se generan ~96 ticks. El nivel de log distingue entre ruido operacional y eventos de negocio:

| Evento | Nivel | Razón |
|--------|-------|-------|
| Tick OK — sin incidente | `Debug` | Ruido rutinario — no interesa en producción |
| Incidente abierto | `Information` | Evento de negocio — operador necesita saberlo |
| Incidente cerrado automáticamente | `Information` | Evento de negocio — señal de recuperación |
| Checker timeout | `Error` | Problema de infraestructura |
| Checker excepción no manejada | `Error` | Problema de código/infraestructura |
| Causa no determinada | `Warning` | Candidato a regla nueva — Técnico debe revisar |
| Scheduler arranca / se detiene | `Information` | Lifecycle del servicio |

```csharp
// En MonitoringService.RunCheckAsync
if (result.Status == CheckStatus.Ok)
    _logger.LogDebug("Check OK: {Module} at {Time}", checker.Module, result.CheckedAt);
else
    _logger.LogInformation("Incident opened: {Module} Cause={Cause} Severity={Severity}",
                            checker.Module, cause, severity);
```

| Atributo | Valor |
|----------|-------|
| **Tipo** | Convención de niveles de log |
| **Cumple** | RNF-08 (sin PII), mantenibilidad operacional |

---

## §3 Configuración appsettings.json — nuevas claves de U3

```json
{
  "Monitoring": {
    "CheckerIntervalMinutes": 5,
    "CheckerTimeoutMs": 30000,
    "OrderDetectionWindowMinutes": 30,
    "DbWarnLatencyMs": 1000,
    "DbCritTimeoutMs": 5000
  }
}
```

Override para Development (`appsettings.Development.json`):

```json
{
  "Monitoring": {
    "CheckerIntervalMinutes": 1,
    "OrderDetectionWindowMinutes": 5
  }
}
```

En desarrollo el intervalo es 1 minuto para poder observar el ciclo sin esperar 5 minutos.

---

## §4 SECURITY compliance — U3

| Regla | Estado | Implementación |
|-------|--------|---------------|
| SECURITY-05 (queries parametrizadas) | ✅ Cumple | EF Core en `SimulatedOrderRepository`; `ExecuteSqlRawAsync("SELECT 1")` sin parámetros de usuario |
| SECURITY-14 (no loggear datos sensibles) | ✅ Cumple | Logs de U3 solo incluyen `ModuleId`, `CauseCategory`, `IncidentId` — sin contenido de pedidos, credenciales ni datos de usuario |
| SECURITY-15 (fail-closed) | ✅ Cumple | Timeout por checker + try-catch por checker + `BackgroundService` reinicia si falla completamente |

---

## §5 Tests planificados para U3

| ID | Clase | Método | NFR / BR verificado |
|----|-------|--------|---------------------|
| T-U3-01 | `CauseClassifierTests` | `Classify_EachCheckerType_ReturnsExpectedCause` | BR-CLASS-01, BR-CLASS-02 |
| T-U3-02 | `AlertTemplateRendererTests` | `Render_AllCombinations_SixFieldsNonEmpty` | BR-ALERT-01, BR-ALERT-03 |
| T-U3-03 | `DbHealthCheckerTests` | `ExecuteAsync_LocalDbRunning_ReturnsOk` | BR-HEALTH-01, BR-HEALTH-02 |
| T-U3-04 | `DbOrderCheckerTests` | `ExecuteAsync_NoCancelledOrders_ReturnsCritical` | BR-DET-04, BR-DET-05 |
| T-U3-05 | `MonitoringServiceTests` | `RunCheckAsync_WarnResult_OpensIncident` | BR-DET-02, ciclo completo |

---

## §6 Trazabilidad de NFRs

| NFR | BR | Story | SECURITY |
|-----|-----|-------|----------|
| NFR-U3-01 (timeout 30s) | BR-DET-03, BR-SCHED-03 | US-07..09 | SECURITY-15 |
| NFR-U3-02 (unit tests M7/M10) | BR-CLASS-01..04, BR-ALERT-01..05 | US-06, US-18 | — |
| NFR-U3-03 (integration tests checkers) | BR-DET-01, BR-HEALTH-01..03 | US-07, US-08, US-09 | SECURITY-05 |
| NFR-U3-04 (logging diferenciado) | BR-DET-02, BR-CLASS-03 | US-06..09, US-18 | SECURITY-14 |
