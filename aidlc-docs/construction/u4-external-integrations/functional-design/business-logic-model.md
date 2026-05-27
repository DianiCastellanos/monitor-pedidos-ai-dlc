# Business Logic Model — U4 External Integrations

**Unidad:** U4 — External Integrations
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Decisiones aplicadas

| ID | Pregunta | Decisión |
|----|----------|---------|
| P1 | Diseño de ApiChecker | B — Dos checkers separados: `SalesforceApiChecker` + `MultivendeApiChecker` |
| P2 | Almacenamiento de auto_reintentos | A — JSON en columna `retry_metadata` de la tabla `incidents` |
| P3 | Cadencia de chequeo APIs | A — Segundo `PeriodicTimer` de 10 min en `MonitoringSchedulerService` |
| P4 | Credenciales externas | A — User Secrets / variables de entorno. Nunca en appsettings.json |
| P5 | Plantillas Api/Token en AlertTemplateRenderer | A — Extender el diccionario `_templates` de U3 directamente |

---

## §2 Flujo 1 — Ciclo de detección de APIs (segundo PeriodicTimer)

```
MonitoringSchedulerService — segundo PeriodicTimer (10 min)
    |
    while await apiTimer.WaitForNextTickAsync(stoppingToken)
    |
    Task.WhenAll([
        RunCheckAsync(SalesforceApiChecker, stoppingToken),
        RunCheckAsync(MultivendeApiChecker, stoppingToken)
    ])
    |
    cada checker con Linked CancellationToken + timeout (ADR-U3-01)
```

Los dos timers corren en `ExecuteAsync` del mismo `MonitoringSchedulerService`:

```
Timer 1 (5 min)  → DbOrderChecker, DbHealthChecker, JobsChecker  [U3]
Timer 2 (10 min) → SalesforceApiChecker, MultivendeApiChecker     [U4]
```

---

## §3 Flujo 2 — Verificación de API con reintentos Polly (caso 5xx/timeout)

```
SalesforceApiChecker.ExecuteAsync(ct)
    |
    ISalesforceClient.PingOrdersAsync(ct)
        |
        [Polly intercepta — política ApiRetryPolicy]
        |
        Intento 1: GET /orders?$top=1
            |
            +-- [HTTP 200] → ApiPingResult.Success → Polly retorna → CheckResult.Ok
            |
            +-- [HTTP 5xx / timeout] →
                    onRetry callback:
                        RetryAttempt(1, httpStatus, latencyMs, UtcNow) → context["retryAttempts"]
                        LogWarning("Salesforce retry 1/2...")
                    espera 2 s (backoff exponencial)
                    |
                    Intento 2: GET /orders?$top=1
                        |
                        +-- [HTTP 200] → ApiPingResult.Success → CheckResult.Ok
                        |
                        +-- [HTTP 5xx / timeout] →
                                onRetry callback:
                                    RetryAttempt(2, ...) → context["retryAttempts"]
                                    LogWarning("Salesforce retry 2/2...")
                                espera 4 s
                                |
                                Intento 3 (original + 2 reintentos):
                                    +-- [falla] → ApiPingResult.ServerError
                                                  CheckResult.Critical("Salesforce HTTP 5xx")
    |
    MonitoringService.RunCheckAsync:
        cause = CauseClassifier.Classify(checker) → CauseCategory.Api
        context = CheckContext(SalesforceApi, Critical, Api, details, now)
        alert   = AlertTemplateRenderer.Render(context) → "2 reintentos automáticos"
        incident = IIncidentService.OpenIncidentAsync(...)
        |
        // Adjuntar reintentos al incidente
        var retryAttempts = (List<RetryAttempt>)pollyContext["retryAttempts"]
        foreach (var attempt in retryAttempts)
            incident.AddRetryAttempt(attempt)
        await IIncidentRepository.SaveChangesAsync()
        |
        INotificationService.BroadcastAlertAsync(incident.Id, alert, ct)
```

---

## §4 Flujo 3 — Verificación de API con 401 (caso Token expirado)

```
SalesforceApiChecker.ExecuteAsync(ct)
    |
    ISalesforceClient.PingOrdersAsync(ct)
        |
        [Polly intercepta]
        Intento 1: GET /orders?$top=1
            |
            HTTP 401 → ApiPingResult.Unauthorized()
            |
            [Polly NO reintenta — 401 no es error transitorio]
            Retorna ApiPingResult inmediatamente
    |
    CheckResult.Critical("Salesforce HTTP 401 — token inválido")
    |
    MonitoringService.RunCheckAsync:
        // Detección especial del 401
        cause = result.Details.Contains("401")
                ? CauseCategory.Token
                : CauseClassifier.Classify(checker)
        // cause = Token
        context = CheckContext(SalesforceApi, Critical, Token, details, now)
        alert   = AlertTemplateRenderer.Render(context)
                  → AccionSugerida incluye "SOP-001"
        incident = IIncidentService.OpenIncidentAsync(...)
        // NO se adjuntan RetryAttempts — 401 no reintenta (INV-U4-01)
        INotificationService.BroadcastAlertAsync(incident.Id, alert, ct)
```

---

## §5 Flujo 4 — Registro de credenciales (User Secrets)

```
Desarrollo (una vez por desarrollador):
    dotnet user-secrets init --project src/MonitorPedidos.Web
    dotnet user-secrets set "Salesforce:ApiKey"  "sf_sandbox_key_xxx"  --project src/MonitorPedidos.Web
    dotnet user-secrets set "Salesforce:BaseUrl" "https://sandbox.salesforce.com/api/"
    dotnet user-secrets set "Multivende:ApiKey"  "mv_sandbox_key_xxx"  --project src/MonitorPedidos.Web
    dotnet user-secrets set "Multivende:BaseUrl" "https://api.multivende.com/"

Program.cs lee automáticamente User Secrets en Development:
    builder.Configuration.AddUserSecrets<Program>()  // activo cuando ASPNETCORE_ENVIRONMENT=Development

Demo en red interna (sin User Secrets):
    Variables de entorno del sistema operativo:
    $env:Salesforce__ApiKey  = "sf_key"
    $env:Multivende__ApiKey  = "mv_key"
```

---

## §6 Flujo 5 — Configuración del segundo PeriodicTimer

```csharp
// MonitoringSchedulerService.ExecuteAsync — extendido para U4

protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    using var monitorTimer = new PeriodicTimer(
        TimeSpan.FromMinutes(_config.GetValue("Monitoring:CheckerIntervalMinutes", 5)));

    using var apiTimer = new PeriodicTimer(
        TimeSpan.FromMinutes(_config.GetValue("Monitoring:ApiCheckerIntervalMinutes", 10)));

    var monitorLoop = RunTimerLoop(monitorTimer, _monitorCheckers, stoppingToken);
    var apiLoop     = RunTimerLoop(apiTimer,    _apiCheckers,     stoppingToken);

    await Task.WhenAll(monitorLoop, apiLoop);
}

private async Task RunTimerLoop(
    PeriodicTimer timer,
    IEnumerable<ICheckExecutor> checkers,
    CancellationToken stoppingToken)
{
    while (await timer.WaitForNextTickAsync(stoppingToken))
        await Task.WhenAll(checkers.Select(c => RunCheckAsync(c, stoppingToken)));
}
```

Los checkers se inyectan con tags o resueltos por tipo:

```csharp
// En el constructor — separación por tipo de checker
_monitorCheckers = checkers.Where(c => c is DbOrderChecker
                                     or DbHealthChecker
                                     or JobsChecker);
_apiCheckers = checkers.Where(c => c is SalesforceApiChecker
                                 or MultivendeApiChecker);
```

---

## §7 Flujo 6 — Vista de auto_reintentos en IncidentDetailPage (US-16)

```
IncidentDetailPage (U2 — actualizado en U4)
    |
    OnInitializedAsync → IIncidentService.GetByIdAsync(id)
    |
    incident.GetRetryAttempts()
    |
    +-- [vacío]               → no muestra sección de reintentos
    +-- [1 o 2 reintentos] →
            [visible solo si rol == "Técnico"]
            Tabla cronológica:
                | # | Timestamp     | HTTP Status | Latencia |
                | 1 | 15:32:04      | 503         | 1240 ms  |
                | 2 | 15:32:10      | 503         | 1580 ms  |
```

---

## §8 Trazabilidad de flujos

| Flujo | Story | RF | BR |
|-------|-------|----|----|
| Ciclo 10 min APIs (§2) | US-10 | RF-03 | BR-API-01, BR-SCHED-04 |
| Reintentos Polly 5xx/timeout (§3) | US-10, US-16 | RF-06 | BR-RETRY-01, BR-RETRY-02 |
| 401 sin reintento → Token (§4) | US-10 | RF-07 | BR-TOKEN-01, BR-TOKEN-02 |
| Credenciales User Secrets (§5) | — | — | BR-CRED-01, BR-CRED-02 |
| Segundo PeriodicTimer (§6) | US-10 | RF-03 | BR-SCHED-04 |
| auto_reintentos en IncidentDetailPage (§7) | US-16 | RF-06 | BR-RETRY-03 |
