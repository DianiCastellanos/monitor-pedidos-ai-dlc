# NFR Design Plan — U4 External Integrations

## Metadata

| Field | Value |
|---|---|
| Stage | Construction → NFR Design |
| Unit | U4 — External Integrations |
| Date | 2026-05-23 |
| Sources | u4 nfr-requirements/ (2 artifacts approved), u4 functional-design/domain-entities.md |

## Context — Patterns Already Determined

| Patrón | Decisión |
|---|---|
| IHttpClientFactory con named clients | AddHttpClient<ISalesforceClient, SalesforceClient>() |
| Polly retry policy | 3 reintentos exponencial |
| IOptions<SalesforceOptions> / IOptions<MultivendeOptions> | NFR-U4-01 |

---

## Questions

### Q1 — Registro de clientes HTTP

**A) (Recomendada)** `AddHttpClient<ISalesforceClient, SalesforceClient>()` + `AddHttpClient<IMultivendeClient, MultivendeClient>()`. BaseAddress y headers de autenticación configurados en el handler. IHttpClientFactory gestiona el lifetime de HttpClient. Transient (compatible con Singleton checkers).

**B)** `new HttpClient()` en cada llamada — anti-patrón; agota puertos TCP.

[Answer]: A — AddHttpClient con typed clients, Transient *(2026-05-23)*

---

### Q2 — Logging de llamadas HTTP

**A) (Recomendada)** DelegatingHandler personalizado que loggea URL, StatusCode y duración. Sin loggear el body (puede contener datos de pedidos). Sin loggear tokens (headers de autorización excluidos por IDestructuringPolicy de U1).

**B)** HttpClientFactory logging built-in — loggea más de lo necesario incluyendo headers completos.

[Answer]: A — DelegatingHandler personalizado sin PII *(2026-05-23)*

---

## Execution Checklist

- [x] 1 Analizar artefactos NFR Requirements de U4.
- [x] 2 Identificar patrones ya determinados vs decisiones pendientes.
- [x] 3 Crear este plan con 2 preguntas.
- [x] 4 Recopilar respuestas del owner. *(2/2 = A, A — 2026-05-23)*
- [x] 5 Analizar respuestas — 0 ambigüedades detectadas.
- [x] 6 Generar nfr-design-patterns.md.
- [x] 7 Generar logical-components.md.
- [x] 8 Actualizar aidlc-state.md.
- [x] 9 Registrar en audit.md.
- [x] 10 Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-23)*
