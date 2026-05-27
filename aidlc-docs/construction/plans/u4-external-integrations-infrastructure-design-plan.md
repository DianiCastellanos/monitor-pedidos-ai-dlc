# Infrastructure Design Plan — U4 External Integrations

## Metadata

| Field | Value |
|---|---|
| Stage | Construction → Infrastructure Design |
| Unit | U4 — External Integrations |
| Date | 2026-05-23 |
| Sources | u4 nfr-design/ (2 artifacts approved), u1 infrastructure-design/ (User Secrets context) |

## Context — Already Decided

| Aspecto | Decisión |
|---|---|
| Tokens | User Secrets / env vars — nunca en appsettings |
| Sin internet público | Restricción explícita del owner |
| URLs de API | Configurables en appsettings.json (no secretas) |

---

## Questions

### Q1 — Configuración de URLs de API externas

**A) (Recomendada)** appsettings.json sección "ExternalApis": `{ "Salesforce": { "BaseUrl": "https://...", "TimeoutSeconds": 10 }, "Multivende": { "BaseUrl": "https://...", "TimeoutSeconds": 10 } }`. Las URLs no son secretas — solo los tokens lo son.

**B)** URLs también en User Secrets — innecesario; las URLs no son sensibles.

[Answer]: A — URLs en appsettings.json, tokens en User Secrets *(2026-05-23)*

---

### Q2 — Sin migrations propias en U4

**A) (Recomendada)** U4 no añade tablas a la BD — los clientes HTTP no persisten datos directamente. Los resultados los procesa U3 (BrandMonitorChecker persiste via IBrandSnapshotRepository de U6).

**B)** Tabla cache_api_responses — overhead innecesario para MVP.

[Answer]: A — sin migrations en U4 *(2026-05-23)*

---

### Q3 — Sin endpoints propios en U4

**A) (Recomendada)** U4 solo expone interfaces de Domain. No tiene controladores ni páginas propias. Los clientes son consumidos por U3 (checkers) y U6 (BrandMonitorPage via BrandMonitorService).

**B)** Endpoint /api/salesforce-proxy — innecesario; el sistema es server-side Blazor.

[Answer]: A — sin endpoints propios en U4 *(2026-05-23)*

---

## Execution Checklist

- [x] 1 Analizar artefactos Functional Design y NFR Design de U4.
- [x] 2 Identificar decisiones ya tomadas vs ambigüedades pendientes.
- [x] 3 Crear este plan con 3 preguntas enfocadas.
- [x] 4 Recopilar respuestas del owner. *(3/3 = A, A, A — 2026-05-23)*
- [x] 5 Analizar respuestas — 0 ambigüedades detectadas.
- [x] 6 Generar infrastructure-design.md.
- [x] 7 Generar deployment-architecture.md.
- [x] 8 Actualizar aidlc-state.md.
- [x] 9 Registrar en audit.md.
- [x] 10 Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-23)*
