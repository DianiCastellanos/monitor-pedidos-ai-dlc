# Infrastructure Design Plan — U7 Simulation & Red-Teaming

## Metadata

| Field   | Value                                                                                                                              |
|---------|------------------------------------------------------------------------------------------------------------------------------------|
| Stage   | Construction → Infrastructure Design                                                                                               |
| Unit    | U7 — Simulation & Red-Teaming                                                                                                      |
| Date    | 2026-05-24                                                                                                                         |
| Sources | u7 nfr-design/ (2 artifacts approved), u7 functional-design/domain-entities.md |

---

## Context — Already Decided

| Aspecto | Decisión |
|---------|----------|
| SimulationOptions | appsettings.Development.json — sección "Simulation" |
| Sin exposición pública | Restricción del owner |
| Tablas simulated_orders + simulated_job_statuses | Migration AddSimulationSchema |

---

## Questions

### Q1 — Migration de U7

**A) (Recomendada)** Migration `AddSimulationSchema`: crea tablas `simulated_orders` (Id, Site, Status, CreatedAt, IsFailure) y `simulated_job_statuses` (Id, JobId, Status, UpdatedAt). Aplicada después de AddBrandSnapshots (U6).

**B)** Sin migration — tablas creadas manualmente — no reproducible.

**[Answer]: A — AddSimulationSchema con ambas tablas *(2026-05-24)***

---

### Q2 — appsettings.Development.json para Simulation

**A) (Recomendada)** Sección "Simulation" en appsettings.Development.json: `{ "Enabled": true, "TickIntervalSeconds": 30, "OrdersPerTick": 5, "IsFailureMode": false, "FailureProbability": 0.3 }`. appsettings.json solo tiene `"Simulation": { "Enabled": false }`.

**B)** Solo en appsettings.json con flag disabled — no permite override por entorno.

**[Answer]: A — appsettings.Development.json con config completa *(2026-05-24)***

---

### Q3 — InternalsVisibleTo

**A) (Recomendada)** En `MonitorPedidos.Web.csproj`: `<InternalsVisibleTo Include="MonitorPedidos.UnitTests" />`. Permite que los unit tests de U7 invoquen `RunOneTickAsync` directamente sin hacerlo público.

**B)** Hacer RunOneTickAsync public — rompe encapsulamiento del servicio.

**[Answer]: A — InternalsVisibleTo en .csproj *(2026-05-24)***

---

## Execution Checklist

- [x] 1 Analizar artefactos Functional Design y NFR Design de U7.
- [x] 2 Identificar decisiones ya tomadas vs ambigüedades pendientes.
- [x] 3 Crear este plan con 3 preguntas enfocadas.
- [x] 4 Recopilar respuestas del owner. *(3/3 = A, A, A — 2026-05-24)*
- [x] 5 Analizar respuestas — 0 ambigüedades detectadas.
- [x] 6 Generar infrastructure-design.md.
- [x] 7 Generar deployment-architecture.md (cumulative U1..U7 final).
- [x] 8 Actualizar aidlc-state.md.
- [x] 9 Registrar en audit.md.
- [x] 10 Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-24)*
