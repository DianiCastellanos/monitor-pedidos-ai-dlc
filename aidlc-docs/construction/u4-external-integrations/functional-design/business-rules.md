# Business Rules — U4 External Integrations

**Unidad:** U4 — External Integrations
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-23
**Versión:** 1.1 (2026-05-31 — BR-API-01 actualizado: 5 min IT5; BR-API-05 nuevo: solo disponibilidad IT8; BR-SCHED-05 actualizado: 5 min)

---

## §1 BR-API — Reglas de chequeo de APIs

| ID | Regla | Implementación | Story | RF |
|----|-------|---------------|-------|----|
| BR-API-01 | Los ApiCheckers (`SalesforceApiChecker`, `MultivendeApiChecker`) se ejecutan cada **5 minutos** en producción (1 min en dev) via `PeriodicTimer` independiente. Configurable en `Monitoring:ApiCheckerIntervalMinutes` | `MonitoringSchedulerService` — `apiTimer = new PeriodicTimer(ApiCheckerIntervalMinutes)` | US-10 | RF-03 |
| BR-API-02 | Cada API tiene su propio checker, su propio `ModuleId` y su propio cliente tipado | `SalesforceApiChecker` → `ModuleId.SalesforceApi`; `MultivendeApiChecker` → `ModuleId.MultivendeApi` | US-10 | RF-03 |
| BR-API-03 | Los clientes de API operan en **solo lectura**. `SalesforceClient` usa POST a `order_search` (requerido por OCAPI — es una consulta, no una mutación). `MultivendeClient` usa GET. Nunca PUT, PATCH ni DELETE | `SalesforceClient.SearchPendingOrdersAsync` — HttpPost; `MultivendeClient` — HttpGet | US-10 | RF-07 |
| BR-API-05 | `SalesforceApiChecker` evalúa **solo disponibilidad** del API (Ok/Critical). No evalúa volumen de pedidos — esa responsabilidad pertenece a `BrandMonitorChecker` (IT8) | `SalesforceApiChecker.ExecuteAsync` — Ok si responde HTTP 200 con body válido, Critical si no responde | US-10 | RF-03 |
| BR-API-04 | El timeout por checker de API usa el mismo mecanismo que U3 (`CancellationTokenSource.CreateLinkedTokenSource` + `CancelAfter`) | Heredado de ADR-U3-01 — `MonitoringSchedulerService.RunCheckAsync` | US-10 | RF-03 |

---

## §2 BR-RETRY — Reglas de reintentos automáticos

| ID | Regla | Implementación | Story | RF |
|----|-------|---------------|-------|----|
| BR-RETRY-01 | La política de reintentos aplica **solo para 5xx y timeout**. Nunca para 4xx (incluyendo 401) | `ApiRetryPolicy` — Polly `HandleTransientHttpError()` — no incluye 401 en los manejadores | US-10 | RF-06 |
| BR-RETRY-02 | El número máximo de reintentos es **2** (3 intentos totales: 1 original + 2 reintentos). Con backoff exponencial: espera 2s tras el primero, 4s tras el segundo | `ApiRetryPolicy` — `WaitAndRetryAsync(2, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)))` | US-10 | RF-06 |
| BR-RETRY-03 | Cada reintento se registra como `RetryAttempt` en el `Incident` resultante — visible en `IncidentDetailPage` solo para el rol **Técnico** | `MonitoringService.RunCheckAsync` — `incident.AddRetryAttempt(attempt)` tras recibir los intentos del contexto Polly | US-16 | RF-06 |
| BR-RETRY-04 | El log de cada reintento incluye: módulo, número de intento, código HTTP y tiempo de espera. Sin PII ni credenciales | `ApiRetryPolicy.onRetry` — `LogWarning("API {Module} retry {Attempt}/2 — HTTP {Status}")` | US-10 | RF-06 |

---

## §3 BR-TOKEN — Reglas de manejo de token 401

| ID | Regla | Implementación | Story | RF |
|----|-------|---------------|-------|----|
| BR-TOKEN-01 | Ante HTTP 401, el sistema **no reintenta** y no intenta renovar el token automáticamente | `ApiRetryPolicy` — 401 no es error transitorio (no está en `HandleTransientHttpError`) + política no lo incluye | US-10 | RF-07 |
| BR-TOKEN-02 | Ante 401, la causa del incidente se clasifica como `CauseCategory.Token` (no `Api`) | `MonitoringService.RunCheckAsync` — `result.Details.Contains("401") ? Token : Classify(checker)` | US-10 | RF-07, RF-08 |
| BR-TOKEN-03 | El campo `AccionSugerida` del `AlertMessage` para causa `Token` incluye literalmente la referencia **"SOP-001"** | `AlertTemplateRenderer._templates[(Token, Critical)]` — texto fijo: "según procedimiento SOP-001" | US-10 | RF-11 |
| BR-TOKEN-04 | Un incidente de tipo `Token` **no registra `RetryAttempts`** — el campo `RetryMetadataJson` permanece nulo | `MonitoringService.RunCheckAsync` — solo llama `AddRetryAttempt` cuando la causa es `Api`, no `Token` | US-10 | INV-U4-01 |

---

## §4 BR-CRED — Reglas de gestión de credenciales

| ID | Regla | Implementación | Story | RF |
|----|-------|---------------|-------|----|
| BR-CRED-01 | Las credenciales de APIs externas (API keys, base URLs) **nunca se almacenan en appsettings.json ni en el repositorio** | `dotnet user-secrets` en desarrollo; variables de entorno en demo interna | — | SECURITY |
| BR-CRED-02 | Las credenciales se leen vía `IConfiguration` — la aplicación no sabe si vienen de User Secrets o variables de entorno | `SalesforceClient` y `MultivendeClient` — `IConfiguration["Salesforce:ApiKey"]` | — | SECURITY |
| BR-CRED-03 | Los logs **nunca incluyen** API keys, tokens ni headers de autorización | `SalesforceClient` / `MultivendeClient` — solo loggea módulo, código HTTP y latencia | — | SECURITY-03, SECURITY-14 |

---

## §5 BR-INCIDENT — Reglas de incidentes de API (extensión de U2)

| ID | Regla | Implementación | Story | RF |
|----|-------|---------------|-------|----|
| BR-INCIDENT-U4-01 | Un incidente de causa `Api` puede tener entre 0 y 2 `RetryAttempts` en `RetryMetadataJson` | `Incident.AddRetryAttempt` — INV-U4-02 limita a máximo 2 | US-16 | RF-06 |
| BR-INCIDENT-U4-02 | Un incidente de causa `Token` siempre tiene `RetryMetadataJson == null` | `MonitoringService.RunCheckAsync` — no llama `AddRetryAttempt` para Token | US-10 | INV-U4-01 |
| BR-INCIDENT-U4-03 | Los `RetryAttempts` se muestran en `IncidentDetailPage` **únicamente** si el usuario tiene rol **Técnico** | `IncidentDetailPage` — `@if (userRole == "Técnico" && incident.GetRetryAttempts().Any())` | US-16 | SECURITY-06, RF-27 |

---

## §6 BR-SCHED — Extensión de reglas del scheduler (U3)

| ID | Regla | Implementación | Story | RF |
|----|-------|---------------|-------|----|
| BR-SCHED-04 | Los `ApiCheckers` corren en un `PeriodicTimer` **separado** al de los `MonitorCheckers` de U3 | `MonitoringSchedulerService` — `apiTimer = new PeriodicTimer(10 min)` paralelo a `monitorTimer` | US-10 | RF-03 |
| BR-SCHED-05 | La cadencia de los ApiCheckers es configurable: `Monitoring:ApiCheckerIntervalMinutes` (default: **5** prod, **1** dev) | `_config.GetValue("Monitoring:ApiCheckerIntervalMinutes", 5)` | US-10 | RF-03 |

---

## §7 Trazabilidad completa

| Categoría | Reglas | Stories | RFs |
|-----------|--------|---------|-----|
| BR-API (4) | BR-API-01..04 | US-10 | RF-03, RF-07 |
| BR-RETRY (4) | BR-RETRY-01..04 | US-10, US-16 | RF-06 |
| BR-TOKEN (4) | BR-TOKEN-01..04 | US-10 | RF-07, RF-08, RF-11 |
| BR-CRED (3) | BR-CRED-01..03 | — | SECURITY-03, SECURITY-14 |
| BR-INCIDENT-U4 (3) | BR-INCIDENT-U4-01..03 | US-10, US-16 | RF-06, RF-27 |
| BR-SCHED (2) | BR-SCHED-04..05 | US-10 | RF-03 |
| **Total** | **20 reglas** | US-10, US-16 | RF-03, RF-06, RF-07, RF-08, RF-11, RF-27 |
