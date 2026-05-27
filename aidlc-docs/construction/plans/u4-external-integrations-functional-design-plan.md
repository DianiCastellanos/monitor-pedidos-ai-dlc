# Functional Design Plan — U4 External Integrations

## Metadata

| Field | Value |
|---|---|
| Stage | Construction → Functional Design |
| Unit | U4 — External Integrations |
| Date | 2026-05-23 |
| Sources | unit-of-work.md §U4, requirements.md §RF-07..RF-10, components.md §ISalesforceClient, §IMultivendeClient |

## Context — Decisions Already Taken

- ISalesforceClient y IMultivendeClient como interfaces en Domain — sin dependencia directa de HTTP
- Tokens vía User Secrets / variables de entorno — nunca hardcodeados
- Sin exposición pública a internet

---

## Questions

### Q1 — ISalesforceClient interface

**A) (Recomendada)** `Task<IReadOnlyList<PendingOrder>> GetPendingOrdersByBrandAsync(string site, CancellationToken ct)` — retorna pedidos pendientes por site (Patprimo, SevenSeven, Atmos, Ostu). DTO: PendingOrder(OrderId, Site, Status, CreatedAt). Usado tanto por SalesforceApiChecker como por BrandMonitorChecker.

**B)** GetAllPendingOrdersAsync() sin filtro por site — retorna todos y filtra en memoria.

[Answer]: A — GetPendingOrdersByBrandAsync(site) *(2026-05-23)*

---

### Q2 — IMultivendeClient interface

**A) (Recomendada)** `Task<IReadOnlyList<JobStatus>> GetJobStatusesAsync(CancellationToken ct)` — retorna estados de jobs de fulfillment Multivende. DTO: JobStatus(JobId, Status, LastUpdated). MultivendeApiChecker usa esto para detectar jobs stuck.

**B)** GetActiveJobsAsync() + GetFailedJobsAsync() — dos llamadas donde una es suficiente.

[Answer]: A — GetJobStatusesAsync() único método *(2026-05-23)*

---

### Q3 — Almacenamiento de tokens API

**A) (Recomendada)** User Secrets en desarrollo (`dotnet user-secrets set "Salesforce:Token" "..."`) y variables de entorno en producción. Se mapean a strongly-typed options: `SalesforceOptions` y `MultivendeOptions` con IOptions<T>. Sin valores en appsettings.json.

**B)** appsettings.Development.json con tokens reales — persiste en disco y puede committearse accidentalmente.

[Answer]: A — User Secrets / env vars con IOptions<T> *(2026-05-23)*

---

### Q4 — DTOs: mapping entre API response y domain

**A) (Recomendada)** DTOs ligeros en capa Infrastructure — `SalesforceOrderDto`, `MultivendeJobDto`. Implementación de ISalesforceClient en Infrastructure mapea DTO → Domain (PendingOrder, JobStatus). Domain no depende de los DTOs de API.

**B)** Usar los tipos de respuesta de API directamente en Domain — crea acoplamiento.

[Answer]: A — DTOs en Infrastructure, mapping a Domain types *(2026-05-23)*

---

### Q5 — Manejo de errores en llamadas externas

**A) (Recomendada)** En excepción (timeout, 401, 5xx): log Error con ILogger, retornar lista vacía para que el checker interprete como crítico. No relanzar excepción — aislamiento por checker. BrandMonitorChecker interpreta vacío como current=-1 → Red.

**B)** Relanzar excepción al caller — rompe el aislamiento entre checkers.

[Answer]: A — catch + log + retornar lista vacía *(2026-05-23)*

---

## Execution Checklist

- [x] 1 Leer artefactos Inception (unit-of-work.md §U4, requirements.md RF-07..RF-10, components.md).
- [x] 2 Identificar scope de U4 y preparar preguntas contextuales.
- [x] 3 Crear este plan en aidlc-docs/construction/plans/.
- [x] 4 Recopilar respuestas del owner. *(5/5 = A, A, A, A, A — 2026-05-23)*
- [x] 5 Analizar respuestas — 0 ambigüedades detectadas.
- [x] 6 Generar domain-entities.md.
- [x] 7 Generar business-logic-model.md.
- [x] 8 Generar business-rules.md.
- [x] 9 Generar frontend-components.md.
- [x] 10 Actualizar aidlc-state.md.
- [x] 11 Registrar en audit.md.
- [x] 12 Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-23)*
