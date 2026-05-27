# Functional Design Plan — U7 Simulation & Red-Teaming

## Metadata

| Field   | Value                                                                                                    |
|---------|----------------------------------------------------------------------------------------------------------|
| Stage   | Construction → Functional Design                                                                         |
| Unit    | U7 — Simulation & Red-Teaming                                                                            |
| Date    | 2026-05-24                                                                                               |
| Sources | unit-of-work.md §U7, requirements.md §RF-32..RF-35, red-team-report-template.md |

---

## Context — Decisions Already Taken

| Decision | Summary |
|----------|---------|
| OrdersSimulatorService | BackgroundService (no ICheckExecutor) |
| SimulationOptions | Enabled=false en producción, Enabled=true en appsettings.Development.json |
| InternalsVisibleTo | Para tests de RunOneTickAsync |
| Aislamiento | Sin impacto en datos de producción |

---

## Questions

### Q1 — OrdersSimulatorService: estructura base

**A) (Recomendada)** BackgroundService con PeriodicTimer (intervalo configurable en SimulationOptions). `RunOneTickAsync(CancellationToken)` interno para tests (internal + InternalsVisibleTo). IServiceScopeFactory para ISimulatedOrderRepository (Scoped). Si Enabled=false: exit inmediato del ExecuteAsync.

**B)** ICheckExecutor — semántica incorrecta; el simulador no produce CheckResult.

**[Answer]: A — BackgroundService con RunOneTickAsync internal *(2026-05-24)***

---

### Q2 — SimulationOptions: estructura

**A) (Recomendada)** `SimulationOptions { bool Enabled, int TickIntervalSeconds, int OrdersPerTick, bool IsFailureMode, double FailureProbability }`. En appsettings.Development.json: `"Simulation": { "Enabled": true, "TickIntervalSeconds": 30, "OrdersPerTick": 5, "IsFailureMode": false, "FailureProbability": 0.3 }`. En appsettings.json: `"Simulation": { "Enabled": false }`.

**B)** Flags hardcodeados en el servicio — no configurable sin recompilar.

**[Answer]: A — SimulationOptions con IOptions\<T\> + appsettings.Development.json *(2026-05-24)***

---

### Q3 — IsFailureMode: lógica de simulación de fallos

**A) (Recomendada)** Cuando `IsFailureMode=true`: algunos pedidos simulados tienen Status="Error" o el simulador no llama a ISalesforceClient (simula API down). `FailureProbability` controla % de pedidos fallidos. Permite ejecutar escenarios RT1..RT7.

**B)** Modo de fallo solo con flag booleano sin probabilidad — menos granular.

**[Answer]: A — IsFailureMode + FailureProbability configurable *(2026-05-24)***

---

### Q4 — Aislamiento de datos de simulación

**A) (Recomendada)** Tablas separadas: `simulated_orders` y `simulated_job_statuses`. IBrandSnapshotRepository de producción opera en `brand_snapshots`. Los checkers de producción (SalesforceApiChecker, etc.) leen de las APIs reales, no de datos simulados. El simulador escribe en tablas propias; los componentes de demo leen de esas tablas.

**B)** Mezclar con tablas de producción — riesgo de contaminación de datos.

**[Answer]: A — tablas simulated_orders + simulated_job_statuses separadas *(2026-05-24)***

---

### Q5 — Red-team: documentación de escenarios

**A) (Recomendada)** `red-team-report-template.md` con 6 escenarios (RT1..RT7 seleccionados): RT1 sin pedidos, RT2 token revocado, RT3 SQL down, RT5 estado incorrecto, RT7 cancelado ignorado, RT-Persist reapertura dashboard. Pasos numerados + resultado esperado + estado (pendiente) + evidencia. Instrucciones de limpieza post-demo.

**B)** Solo checklist ad-hoc sin formato — no repetible en demo.

**[Answer]: A — red-team-report-template.md estructurado *(2026-05-24)***

---

## Execution Checklist

- [x] 1 Leer artefactos Inception (unit-of-work.md §U7, requirements.md RF-32..RF-35).
- [x] 2 Identificar scope de U7 y preparar preguntas contextuales.
- [x] 3 Crear este plan en aidlc-docs/construction/plans/.
- [x] 4 Recopilar respuestas del owner. *(5/5 = A, A, A, A, A — 2026-05-24)*
- [x] 5 Analizar respuestas — 0 ambigüedades detectadas.
- [x] 6 Generar domain-entities.md.
- [x] 7 Generar business-logic-model.md.
- [x] 8 Generar business-rules.md.
- [x] 9 Generar frontend-components.md.
- [x] 10 Actualizar aidlc-state.md.
- [x] 11 Registrar en audit.md.
- [x] 12 Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-24)*
