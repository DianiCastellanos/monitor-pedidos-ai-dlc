# NFR Design Plan — U7 Simulation & Red-Teaming

## Metadata

| Field   | Value                                                                                                         |
|---------|---------------------------------------------------------------------------------------------------------------|
| Stage   | Construction → NFR Design                                                                                     |
| Unit    | U7 — Simulation & Red-Teaming                                                                                 |
| Date    | 2026-05-24                                                                                                    |
| Sources | u7 nfr-requirements/ (2 artifacts approved), u7 functional-design/domain-entities.md |

---

## Context — Patterns Already Determined

| Patrón | Decisión |
|--------|----------|
| BackgroundService + PeriodicTimer | NFR-U7-01 |
| IServiceScopeFactory para repos Scoped | ADR-U5-01 (reutilizado en U7) |
| try/catch no rethrow (excepto OperationCanceledException) | NFR-U7-02 |

---

## Questions

### Q1 — Error handling en RunOneTickAsync

**A) (Recomendada)** `try { await RunOneTickAsync(ct); } catch (OperationCanceledException) { throw; } catch (Exception ex) { _logger.LogError(ex, "Simulation tick failed"); }`. El simulador no puede detener el servicio por un error transitorio.

**B)** Rethrow en todas las excepciones — detendría el BackgroundService.

**[Answer]: A — catch general + log, rethrow solo OperationCanceledException *(2026-05-24)***

---

### Q2 — Registro DI del simulador

**A) (Recomendada)** `services.AddHostedService<OrdersSimulatorService>()`. Registrado condicional: si no está en appsettings o Enabled=false, el servicio se registra igualmente pero sale inmediatamente en ExecuteAsync. Sin condicional en Program.cs (más limpio).

**B)** Registro condicional en Program.cs basado en IConfiguration — requiere acceso a IConfiguration antes de Build().

**[Answer]: A — siempre registrado, guard interno en ExecuteAsync *(2026-05-24)***

---

## Execution Checklist

- [x] 1 Analizar artefactos NFR Requirements de U7.
- [x] 2 Identificar patrones ya determinados vs decisiones pendientes.
- [x] 3 Crear este plan con 2 preguntas.
- [x] 4 Recopilar respuestas del owner. *(2/2 = A, A — 2026-05-24)*
- [x] 5 Analizar respuestas — 0 ambigüedades detectadas.
- [x] 6 Generar nfr-design-patterns.md.
- [x] 7 Generar logical-components.md.
- [x] 8 Actualizar aidlc-state.md.
- [x] 9 Registrar en audit.md.
- [x] 10 Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-24)*
