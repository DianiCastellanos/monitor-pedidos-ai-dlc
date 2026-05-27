# NFR Requirements Plan — U7 Simulation & Red-Teaming

## Metadata

| Field   | Value                                                                                              |
|---------|----------------------------------------------------------------------------------------------------|
| Stage   | Construction → NFR Requirements                                                                    |
| Unit    | U7 — Simulation & Red-Teaming                                                                      |
| Date    | 2026-05-24                                                                                         |
| Sources | u7 functional-design/ (4 artifacts approved), requirements.md §RF-32..RF-35 |

---

## Context — NFRs Already Defined

| NFR | Estado | Decisión |
|-----|--------|----------|
| Enabled=false en producción | Definido | appsettings.json default |
| Tablas separadas | Definido | simulated_orders + simulated_job_statuses |
| InternalsVisibleTo | Definido | MonitorPedidos.UnitTests |

---

## Questions

### Q1 — Impacto en producción: garantía de aislamiento

**A) (Recomendada)** `if (!_options.Enabled) return;` en el primer statement de `ExecuteAsync`. Verificación en startup: si Environment.IsProduction() y Enabled=true → log Warning. Tablas de simulación nunca escritas por componentes de producción.

**B)** Solo flag — sin verificación adicional en startup.

**[Answer]: A — guard en ExecuteAsync + log Warning si Enabled en producción *(2026-05-24)***

---

### Q2 — Tests del simulador

**A) (Recomendada)** Unit tests via `RunOneTickAsync` (internal + InternalsVisibleTo): (1) Enabled=false → no escribe pedidos; (2) Enabled=true + IsFailureMode=false → escribe N pedidos; (3) IsFailureMode=true + probabilidad → algunos pedidos con status Error. Total: ~3 unit tests con Moq de ISimulatedOrderRepository.

**B)** Solo integration tests arrancando el BackgroundService — difícil de hacer determinista.

**[Answer]: A — unit tests via RunOneTickAsync con Moq *(2026-05-24)***

---

### Q3 — Cleanup post-demo

**A) (Recomendada)** Scripts de limpieza en red-team-report-template.md: PowerShell para TRUNCATE TABLE simulated_orders + simulated_job_statuses. Instrucciones en sección "Limpieza post-demo" del template.

**B)** Limpieza manual desde SQL Server Management Studio — no documentado.

**[Answer]: A — scripts PowerShell en red-team-report-template.md *(2026-05-24)***

---

## Execution Checklist

- [x] 1 Analizar artefactos Functional Design de U7.
- [x] 2 Identificar NFRs ya definidos vs decisiones pendientes.
- [x] 3 Crear este plan en aidlc-docs/construction/plans/.
- [x] 4 Recopilar respuestas del owner. *(3/3 = A, A, A — 2026-05-24)*
- [x] 5 Analizar respuestas — 0 ambigüedades detectadas.
- [x] 6 Generar nfr-requirements.md.
- [x] 7 Generar tech-stack-decisions.md.
- [x] 8 Actualizar aidlc-state.md.
- [x] 9 Registrar en audit.md.
- [x] 10 Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-24)*
