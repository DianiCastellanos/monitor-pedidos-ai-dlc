# NFR Requirements Plan — U6 Dashboard & Real-Time

## Metadata

| Field   | Value                                                                                                           |
|---------|-----------------------------------------------------------------------------------------------------------------|
| Stage   | Construction → NFR Requirements                                                                                 |
| Unit    | U6 — Dashboard & Real-Time                                                                                      |
| Date    | 2026-05-24                                                                                                      |
| Sources | u6 functional-design/ (4 artifacts approved), requirements.md §RF-22..RF-26 RF-30 RF-31 |

---

## Context — NFRs Already Defined

| NFR | Estado | Decisión |
|-----|--------|----------|
| SignalR hub [Authorize] | Definido | AlertsHub decorado con [Authorize] |
| signalr.min.js local | Definido | ADR-U6-04: sin CDN |
| LogsPage [Authorize(Roles="Técnico")] | Definido | ADR-U5-02 |
| BR-CONC-01..03 | Definidos | Ver business-rules.md |

---

## Questions

### Q1 — SignalR: auto-reconnect

**A) (Recomendada)** `withAutomaticReconnect()` en el JS client (`notifications.js`). En desconexión: reintenta automáticamente con backoff. Estado de conexión visible en UI (indicador pequeño). Los eventos perdidos durante desconexión no se re-envían (eventual consistency aceptable para alertas).

**B)** Sin auto-reconnect — si el servidor reinicia, el cliente JS queda sin alertas hasta F5.

**[Answer]: A — withAutomaticReconnect() en notifications.js *(2026-05-24)***

---

### Q2 — Sonido de alerta en el browser

**A) (Recomendada)** `new Audio('data:audio/wav;base64,...').play()` en notifications.js al recibir AlertReceived. Fallback: `document.title` flashea si `play()` falla (autoplay policy). Sin archivos de audio externos.

**B)** Sin sonido — solo visual.

**[Answer]: A — Audio inline con fallback flashTitle *(2026-05-24)***

---

### Q3 — Cobertura de tests para U6

**A) (Recomendada)** Unit tests: NotificationService (3 tests: BroadcastAlert invoca AlertBroadcaster + IHubContext; excepción en hub se propaga; BroadcastClose funciona). BrandMonitorCheckerTests (4 tests: bajó suficiente→Green; subió→Red; API falla→Red(current=-1); sin regla→threshold=0→Green). Total: 7 tests con triple mock IHubContext (ADR-U6-01).

**B)** Solo prueba manual de alertas en browser.

**[Answer]: A — 7 unit tests con Moq triple mock IHubContext *(2026-05-24)***

---

## Execution Checklist

- [x] 1 Analizar artefactos Functional Design de U6.
- [x] 2 Identificar NFRs ya definidos vs decisiones pendientes.
- [x] 3 Crear este plan en aidlc-docs/construction/plans/.
- [x] 4 Recopilar respuestas del owner. *(3/3 = A, A, A — 2026-05-24)*
- [x] 5 Analizar respuestas — 0 ambigüedades detectadas.
- [x] 6 Generar nfr-requirements.md.
- [x] 7 Generar tech-stack-decisions.md.
- [x] 8 Actualizar aidlc-state.md.
- [x] 9 Registrar en audit.md.
- [x] 10 Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-24)*
