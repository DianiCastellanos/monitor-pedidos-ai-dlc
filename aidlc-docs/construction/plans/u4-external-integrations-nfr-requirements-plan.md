# NFR Requirements Plan — U4 External Integrations

## Metadata

| Field | Value |
|---|---|
| Stage | Construction → NFR Requirements |
| Unit | U4 — External Integrations |
| Date | 2026-05-23 |
| Sources | u4 functional-design/ (4 artifacts approved), requirements.md RNF-01..RNF-05 |

## Context — NFRs Already Defined

| NFR | Estado | Decisión |
|---|---|---|
| Tokens sin hardcoding | Definido | User Secrets / env vars |
| Sin exposición a internet | Definido | Restricción del owner |
| BR-API-01..05 | Definidos | Ver business-rules.md |

---

## Questions

### Q1 — Política de retry para HTTP calls

**A) (Recomendada)** Polly con 3 reintentos, backoff exponencial (1s, 2s, 4s), solo en transient errors (5xx, timeout). Circuit breaker opcional post-MVP. Registrado con `AddPolicyHandler` en AddHttpClient.

**B)** Sin retry — si falla, falla. Aceptable si la API es muy confiable.

[Answer]: A — Polly 3 reintentos exponencial *(2026-05-23)*

---

### Q2 — Timeout por llamada HTTP

**A) (Recomendada)** 10 segundos por llamada (configurable en appsettings "ExternalApis:TimeoutSeconds": 10). Si supera timeout → HttpRequestException → checker interpreta como error → Red. Razonable para APIs de terceros.

**B)** 30 segundos — demasiado largo; bloquea el timer tick del scheduler.

[Answer]: A — 10 segundos configurable en appsettings *(2026-05-23)*

---

### Q3 — Cobertura de tests para U4

**A) (Recomendada)** Unit tests con HttpClient mock (MockHttpMessageHandler): SalesforceClient retorna pedidos correctamente (HTTP 200), SalesforceClient 401 → lista vacía, MultivendeClient timeout → lista vacía. Total: ~4 unit tests. Sin integration tests reales a APIs externas.

**B)** Solo prueba manual con token real — no repetible en CI.

[Answer]: A — unit tests con MockHttpMessageHandler *(2026-05-23)*

---

## Execution Checklist

- [x] 1 Analizar artefactos Functional Design de U4.
- [x] 2 Identificar NFRs ya definidos vs decisiones pendientes.
- [x] 3 Crear este plan en aidlc-docs/construction/plans/.
- [x] 4 Recopilar respuestas del owner. *(3/3 = A, A, A — 2026-05-23)*
- [x] 5 Analizar respuestas — 0 ambigüedades detectadas.
- [x] 6 Generar nfr-requirements.md.
- [x] 7 Generar tech-stack-decisions.md.
- [x] 8 Actualizar aidlc-state.md.
- [x] 9 Registrar en audit.md.
- [x] 10 Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-23)*
