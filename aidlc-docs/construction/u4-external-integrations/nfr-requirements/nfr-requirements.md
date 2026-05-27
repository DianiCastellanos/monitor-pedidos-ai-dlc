# NFR Requirements — U4 External Integrations

**Unidad:** U4 — External Integrations
**Stage:** Construction → NFR Requirements
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Decisiones aplicadas

| ID | Pregunta | Decisión |
|----|----------|---------|
| P1 | Timeout llamadas HTTP externas | A — 10 segundos por llamada (`HttpClient.Timeout`) |
| P2 | Tests para clientes HTTP | A — Unit tests con `MockHttpMessageHandler` |
| P3 | Tests para ApiRetryPolicy (Polly) | A — 3 unit tests que simulan secuencias de respuestas |
| P4 | Logging de llamadas exitosas a APIs | A — `LogDebug` (consistente con NFR-U3-04) |

---

## §2 NFR-U4 — Atributos de calidad

### NFR-U4-01 — Timeout de clientes HTTP externos (Desempeño)

| Atributo | Valor |
|----------|-------|
| **Categoría** | Desempeño |
| **Requisito** | Cada llamada HTTP a Salesforce o Multivende tiene un timeout máximo de **10 segundos** |
| **Implementación** | `SalesforceClient` y `MultivendeClient`: `services.AddHttpClient<ISalesforceClient, SalesforceClient>().ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(10))` |
| **Contexto** | El timeout aplica a cada intento individual (original + reintentos). Con 2 reintentos y backoff 2s/4s, el peor caso total por checker es: 10s + 2s + 10s + 4s + 10s = 36 segundos, dentro del ciclo de 10 minutos |
| **Story** | US-10 |
| **RF** | RF-03, RF-06 |

---

### NFR-U4-02 — Tests de clientes HTTP externos (Mantenibilidad)

| Atributo | Valor |
|----------|-------|
| **Categoría** | Mantenibilidad |
| **Requisito** | `SalesforceClient` y `MultivendeClient` deben tener **unit tests con `MockHttpMessageHandler`** — sin llamadas HTTP reales |
| **Alcance** | 3 escenarios por cliente: respuesta 200 (éxito), respuesta 503 (server error), respuesta 401 (unauthorized) |
| **Proyecto** | `MonitorPedidos.UnitTests` |
| **Story** | US-10, US-16 |
| **RF** | RF-03, RF-06, RF-07 |

**Tests requeridos:**

```
SalesforceClientTests
    ├── PingOrdersAsync_Returns200_ReturnsSuccess
    ├── PingOrdersAsync_Returns503_ReturnsServerError
    └── PingOrdersAsync_Returns401_ReturnsUnauthorized

MultivendeClientTests
    ├── PingOrdersAsync_Returns200_ReturnsSuccess
    ├── PingOrdersAsync_Returns503_ReturnsServerError
    └── PingOrdersAsync_Returns401_ReturnsUnauthorized
```

---

### NFR-U4-03 — Tests de ApiRetryPolicy — Polly (Confiabilidad)

| Atributo | Valor |
|----------|-------|
| **Categoría** | Confiabilidad |
| **Requisito** | La lógica de reintentos Polly y el callback `onRetry` deben tener **3 unit tests** que validan el comportamiento exacto |
| **Proyecto** | `MonitorPedidos.UnitTests` |
| **Story** | US-10, US-16 |
| **RF** | RF-06, RF-07 |

**Tests requeridos:**

```
ApiRetryPolicyTests
    ├── RetryPolicy_503_503_200_ExecutesTwoRetriesAndCaptures2RetryAttempts
    │       MockHttpMessageHandler devuelve: 503 → 503 → 200
    │       Assert: 3 intentos totales, 2 RetryAttempts en contexto Polly
    │       Assert: último resultado = ApiPingResult.Success
    │
    ├── RetryPolicy_401_NoRetry_ZeroRetryAttempts
    │       MockHttpMessageHandler devuelve: 401
    │       Assert: 1 intento total (sin reintentos)
    │       Assert: resultado = ApiPingResult.Unauthorized
    │
    └── RetryPolicy_Timeout_200_ExecutesOneRetryAndCaptures1RetryAttempt
            MockHttpMessageHandler devuelve: timeout → 200
            Assert: 2 intentos totales, 1 RetryAttempt en contexto Polly
            Assert: último resultado = ApiPingResult.Success
```

---

### NFR-U4-04 — Logging (Mantenibilidad / Seguridad)

| Atributo | Valor |
|----------|-------|
| **Categoría** | Mantenibilidad, Seguridad |
| **Requisito** | Nivel de log consistente con NFR-U3-04: `LogDebug` para respuestas 200, `LogWarning` para cada reintento, `LogError` para incidente final |
| **Implementación** | `SalesforceClient`/`MultivendeClient`: `_logger.LogDebug("API {Module} OK — HTTP 200 latency {Latency}ms", ...)` |
| **Restricción de seguridad** | Los logs **nunca incluyen** API keys, tokens, valores de headers de Authorization ni ningún otro dato de credencial (BR-CRED-03) |
| **Story** | US-10 |
| **RF** | RF-06 |

---

## §3 NFR heredados de U1 aplicables a U4

Los siguientes NFRs de U1 aplican sin cambios a los componentes de U4:

| NFR U1 | Aplica en U4 | Contexto |
|--------|-------------|---------|
| Security Baseline — SECURITY-08 | `IncidentDetailPage` — autorización server-side para sección `auto_reintentos` | Rol Técnico verificado en servidor, no solo en UI |
| Security Baseline — SECURITY-03 | `SalesforceClient`, `MultivendeClient` — sin PII en logs | Nunca loggear API keys ni tokens |
| Security Baseline — SECURITY-14 | Serilog PII filter heredado de U1 | Destructuring policy excluye campos de credenciales |
| NFR-U1 (autenticación cookie) | `IncidentDetailPage` — cookie de sesión valida antes de mostrar sección reintentos | AuthenticationStateProvider valida sesión activa |

---

## §4 Trazabilidad NFR

| NFR | Categoría | Componentes afectados | Stories | RFs |
|-----|-----------|----------------------|---------|-----|
| NFR-U4-01 | Desempeño | SalesforceClient, MultivendeClient | US-10 | RF-03, RF-06 |
| NFR-U4-02 | Mantenibilidad | SalesforceClient, MultivendeClient (tests) | US-10, US-16 | RF-03, RF-06, RF-07 |
| NFR-U4-03 | Confiabilidad | ApiRetryPolicy (tests) | US-10, US-16 | RF-06, RF-07 |
| NFR-U4-04 | Mantenibilidad / Seguridad | SalesforceClient, MultivendeClient | US-10 | RF-06 |
| SECURITY-03/14 (heredado) | Seguridad | SalesforceClient, MultivendeClient | — | SECURITY |
| SECURITY-08 (heredado) | Seguridad | IncidentDetailPage | US-16 | RF-27 |
