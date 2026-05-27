# Logical Components — U4 External Integrations

**Unidad:** U4 — External Integrations
**Stage:** Construction → NFR Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Componentes de producción introducidos en U4

| ID | Componente | Tipo | Proyecto | Descripción |
|----|-----------|------|---------|-------------|
| LC-U4-01 | `SalesforceApiChecker` | Clase (ICheckExecutor) | MonitorPedidos.Web | Checker que invoca `ISalesforceClient.PingOrdersAsync` y retorna `CheckResult` |
| LC-U4-02 | `MultivendeApiChecker` | Clase (ICheckExecutor) | MonitorPedidos.Web | Checker que invoca `IMultivendeClient.PingOrdersAsync` y retorna `CheckResult` |
| LC-U4-03 | `ISalesforceClient` | Interfaz | MonitorPedidos.Web | Contrato del cliente tipado Salesforce |
| LC-U4-04 | `IMultivendeClient` | Interfaz | MonitorPedidos.Web | Contrato del cliente tipado Multivende |
| LC-U4-05 | `SalesforceClient` | Clase (ISalesforceClient) | MonitorPedidos.Web | Typed HttpClient — GET ping a Salesforce Orders API |
| LC-U4-06 | `MultivendeClient` | Clase (IMultivendeClient) | MonitorPedidos.Web | Typed HttpClient — GET ping a Multivende Orders API |
| LC-U4-07 | `ApiRetryPolicy` | Clase estática | MonitorPedidos.Web | Polly: 2 reintentos, backoff 2s/4s, solo 5xx/timeout, captura RetryAttempts en Context |
| LC-U4-08 | `PollyContextExtensions` | Clase estática (extensión) | MonitorPedidos.Web | `GetRetryAttempts(this Context)` — acceso tipado al Context de Polly |
| LC-U4-09 | `RetryAttempt` | Value object (record) | MonitorPedidos.Web | `(AttemptNumber, HttpStatusCode, LatencyMs, AttemptedAt)` |
| LC-U4-10 | `ApiPingResult` | Value object (record) | MonitorPedidos.Web | Resultado del ping HTTP: Success / Unauthorized / ServerError / Timeout |

---

## §2 Componentes de producción modificados en U4

| Componente | Unidad origen | Modificación |
|-----------|--------------|-------------|
| `Incident` (aggregate) | U2 | Agrega columna `RetryMetadataJson` (nvarchar nullable) + métodos `AddRetryAttempt` / `GetRetryAttempts` |
| `MonitoringSchedulerService` | U3 | Agrega segundo `PeriodicTimer` (10 min) + `_apiCheckers` separados de `_monitorCheckers` + método `RunTimerLoop` |
| `AlertTemplateRenderer` | U3 | Extiende `_templates` con entradas para `(Api, Critical)` y `(Token, Critical)` — incluye referencia SOP-001 |
| `CauseClassifier` | U3 | Agrega `[typeof(SalesforceApiChecker)] = Api` y `[typeof(MultivendeApiChecker)] = Api` |
| `ModuleId` (enum) | U2 | Agrega valores `SalesforceApi` y `MultivendeApi` |
| `MonitoringService.RunCheckAsync` | U3 | Agrega detección de 401 (`result.Details.Contains("401") ? Token : Classify`) + adjuntar RetryAttempts al incidente |
| `IncidentDetailPage` | U2 | Agrega sección `auto_reintentos` visible solo para rol Técnico cuando `Cause == Api` |

---

## §3 Componentes de test introducidos en U4

| ID | Componente | Tipo | Proyecto | Escenarios |
|----|-----------|------|---------|-----------|
| TC-U4-01 | `SalesforceClientTests` | xUnit | MonitorPedidos.UnitTests | 200 (éxito), 503 (error), 401 (unauthorized) |
| TC-U4-02 | `MultivendeClientTests` | xUnit | MonitorPedidos.UnitTests | 200 (éxito), 503 (error), 401 (unauthorized) |
| TC-U4-03 | `ApiRetryPolicyTests` | xUnit | MonitorPedidos.UnitTests | 503→503→200 (2 reintentos), 401 (sin reintento), timeout→200 (1 reintento) |

**Total tests U4:** 9 unit tests

---

## §4 Diagrama de dependencias U4

```
MonitoringSchedulerService (U3 modificado)
    ├── PeriodicTimer 5 min → _monitorCheckers [U3]
    │       DbOrderChecker, DbHealthChecker, JobsChecker
    │
    └── PeriodicTimer 10 min → _apiCheckers [U4 NEW]
            SalesforceApiChecker ──→ ISalesforceClient ──→ SalesforceClient
            │                                                    │
            │                                              HttpClient + Polly
            │                                              ApiRetryPolicy
            │                                              PollyContextExtensions
            │
            MultivendeApiChecker ──→ IMultivendeClient ──→ MultivendeClient
                                                              │
                                                        HttpClient + Polly
                                                        (misma ApiRetryPolicy)

MonitoringService.RunCheckAsync [U3 modificado]
    ├── CheckResult.Details → detecta "401" → CauseCategory.Token
    ├── CauseClassifier → SalesforceApiChecker/MultivendeApiChecker → CauseCategory.Api
    ├── AlertTemplateRenderer → (Api, Critical) / (Token, Critical) con SOP-001
    ├── IIncidentService.OpenIncidentAsync → Incident
    ├── pollyContext.GetRetryAttempts() → incident.AddRetryAttempt() [si Api]
    └── INotificationService.BroadcastAlertAsync

IncidentDetailPage [U2 modificado]
    └── @if (rol == Técnico && cause == Api && retryAttempts.Any())
              tabla cronológica de RetryAttempts
```

---

## §5 Reglas de diseño aplicadas en U4

| ADR | Patrón | Componentes afectados |
|-----|--------|----------------------|
| ADR-U4-01 | Polly Context dictionary para captura de RetryAttempts | ApiRetryPolicy, PollyContextExtensions, MonitoringService |
| ADR-U4-02 | `AddHttpClient<T>().AddPolicyHandler(policy)` — política compartida | Program.cs, SalesforceClient, MultivendeClient |
| ADR-U4-03 | Filtro por tipo concreto en constructor | MonitoringSchedulerService |
| ADR-U4-04 | MockHttpMessageHandler inline por test | SalesforceClientTests, MultivendeClientTests, ApiRetryPolicyTests |
| ADR-U3-01 | Linked CancellationToken + timeout (heredado) | SalesforceApiChecker, MultivendeApiChecker |
| ADR-U3-02 | IEnumerable<ICheckExecutor> en DI (heredado) | SalesforceApiChecker, MultivendeApiChecker registrados en DI |
| ADR-U3-03 | PeriodicTimer + WaitForNextTickAsync (heredado) | Segundo timer en MonitoringSchedulerService |

---

## §6 Trazabilidad de componentes

| Componente | Story | RF | NFR | ADR |
|-----------|-------|----|----|-----|
| SalesforceApiChecker | US-10 | RF-03, RF-07 | NFR-U4-01 | ADR-U3-01, ADR-U3-02 |
| MultivendeApiChecker | US-10 | RF-03, RF-07 | NFR-U4-01 | ADR-U3-01, ADR-U3-02 |
| SalesforceClient | US-10 | RF-03 | NFR-U4-01, NFR-U4-02 | ADR-U4-02 |
| MultivendeClient | US-10 | RF-03 | NFR-U4-01, NFR-U4-02 | ADR-U4-02 |
| ApiRetryPolicy | US-10, US-16 | RF-06 | NFR-U4-03 | ADR-U4-01 |
| PollyContextExtensions | US-10, US-16 | RF-06 | NFR-U4-03 | ADR-U4-01 |
| RetryAttempt | US-10, US-16 | RF-06 | — | ADR-U4-01 |
| ApiPingResult | US-10 | RF-06, RF-07 | — | — |
| SalesforceClientTests (3) | US-10 | RF-03, RF-07 | NFR-U4-02 | ADR-U4-04 |
| MultivendeClientTests (3) | US-10 | RF-03, RF-07 | NFR-U4-02 | ADR-U4-04 |
| ApiRetryPolicyTests (3) | US-10, US-16 | RF-06 | NFR-U4-03 | ADR-U4-04 |
