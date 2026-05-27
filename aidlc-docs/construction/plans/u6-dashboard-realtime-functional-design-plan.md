# Functional Design Plan — U6 Dashboard & Real-Time

## Metadata

| Field   | Value                                                                           |
|---------|---------------------------------------------------------------------------------|
| Stage   | Construction → Functional Design                                                |
| Unit    | U6 — Dashboard & Real-Time                                                      |
| Date    | 2026-05-24                                                                      |
| Sources | unit-of-work.md §U6, requirements.md §RF-22..RF-26 RF-30 RF-31, u3 domain-entities.md §AlertBroadcaster, u5 domain-entities.md v1.1 |

---

## Context — Decisions Already Taken

Documented in `aidlc-docs/construction/u6-dashboard-realtime/functional-design/domain-entities.md` §1 (decisions P1–P5):

| Decision | Summary |
|----------|---------|
| P1 | AlertBroadcaster singleton para real-time en RealtimePage |
| P2 | BrandMonitorChecker + tabla `brand_snapshots` (4 filas fijas, overwrite, sin histórico) |
| P3 | PendingDropThreshold desde RuleCondition vía IRuleRepository |
| P4 | Toggle NocLayout en NavMenu sin JS fullscreen API |
| P5 | CSV/JSON export via IJSRuntime blob, 0 dependencias adicionales |

---

## Questions

### Q1 (P1) — Mecanismo real-time en RealtimePage

**A) (Recomendada)** `AlertBroadcaster` singleton (C# event multicast) + `InvokeAsync(StateHasChanged)`. RealtimePage se suscribe a `OnAlert` en `OnInitializedAsync` y se desuscribe en `DisposeAsync` (IAsyncDisposable). Captura local de delegate para evitar double-invocation (ADR-U6-02). Sin doble conexión SignalR.

**B)** Polling HTTP desde la página cada 5 segundos — no real-time; carga innecesaria.

**[Answer]: A — AlertBroadcaster singleton con IAsyncDisposable *(2026-05-24)***

---

### Q2 (P2) — Brand Monitor: fuente de datos

**A) (Recomendada)** `BrandMonitorChecker` (ICheckExecutor) + tabla `brand_snapshots` (4 filas fijas, UPSERT por site, sin histórico). Timer2 (10 min) de MonitoringSchedulerService ejecuta BrandMonitorChecker. `BrandMonitorPage` lee vía `IBrandMonitorService.GetCurrentSnapshotsAsync()`. NO genera incidentes.

**B)** Polling directo desde BrandMonitorPage a ISalesforceClient — viola separación de concerns.

**[Answer]: A — BrandMonitorChecker + brand_snapshots (4 filas fijas) *(2026-05-24)***

---

### Q3 (P3) — Semáforo brand monitor: umbrales

**A) (Recomendada)** `RuleCondition.PendingDropThreshold` configurable desde `RulesPage` (U5). `BrandMonitorChecker` llama `IRuleRepository.GetActiveByModuleAsync(ModuleId.BrandMonitor)`. Fallback threshold=0 si no hay regla activa (BR-RULE-09). Seed inicial: PendingDropThreshold=5.

**B)** Umbral hardcoded en appsettings — no configurable desde UI.

**[Answer]: A — PendingDropThreshold desde IRuleRepository, configurable desde RulesPage *(2026-05-24)***

---

### Q4 (P4) — NocLayout: modo de activación

**A) (Recomendada)** Enlace "Modo NOC" en NavMenu → `/noc` usa `NocLayout.razor` que oculta sidebar. Botón "Salir de NOC" → `/dashboard`. Sin JS fullscreen API. Sin riesgo de bloqueo de browser.

**B)** JS fullscreen API con `document.documentElement.requestFullscreen()` — requiere gesture del usuario y puede ser bloqueado por browser.

**[Answer]: A — NocLayout.razor con toggle NavMenu *(2026-05-24)***

---

### Q5 (P5) — LogsPage: exportación

**A) (Recomendada)** CSV con `StringBuilder` + JSON con `System.Text.Json`. Download via `IJSRuntime` llamando `window.downloadBlob(data, filename, mimeType)`. Sin dependencias adicionales (0 nuevos paquetes).

**B)** Endpoint HTTP GET /api/logs/export — requiere controlador API y manejo de stream.

**[Answer]: A — StringBuilder + System.Text.Json + IJSRuntime blob *(2026-05-24)***

---

## Execution Checklist

- [x] 1 Leer artefactos Inception y u3/u5 domain entities para contexto de dependencias.
- [x] 2 Identificar scope de U6 y las 5 preguntas clave de diseño.
- [x] 3 Crear este plan en aidlc-docs/construction/plans/.
- [x] 4 Recopilar respuestas del owner. *(5/5 = A, A, A, A, A — 2026-05-24)*
- [x] 5 Analizar respuestas — 0 ambigüedades detectadas.
- [x] 6 Generar domain-entities.md (v1.0; v1.1 actualiza §4 referencia a U5 v1.1 — 2026-05-24).
- [x] 7 Generar business-logic-model.md.
- [x] 8 Generar business-rules.md.
- [x] 9 Generar frontend-components.md.
- [x] 10 Actualizar aidlc-state.md.
- [x] 11 Registrar en audit.md.
- [x] 12 Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-24)*
