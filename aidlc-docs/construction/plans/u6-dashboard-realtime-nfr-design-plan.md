# NFR Design Plan — U6 Dashboard & Real-Time

## Metadata

| Field   | Value                                                                                                         |
|---------|---------------------------------------------------------------------------------------------------------------|
| Stage   | Construction → NFR Design                                                                                     |
| Unit    | U6 — Dashboard & Real-Time                                                                                    |
| Date    | 2026-05-24                                                                                                    |
| Sources | u6 nfr-requirements/ (2 artifacts approved), u6 functional-design/domain-entities.md |

---

## Context — Patterns Already Determined

| Patrón | Decisión |
|--------|----------|
| AlertBroadcaster Singleton | event multicast in-process, captura local delegate (ADR-U6-02) |
| IHubContext\<AlertsHub\> | Triple mock en tests (ADR-U6-01) |
| signalr.min.js local | wwwroot/js/lib/ (ADR-U6-04) |
| static readonly Regex | Compilada en TechnicalLogReader (ADR-U6-03) |

---

## Questions

### Q1 — IAsyncDisposable en RealtimePage

**A) (Recomendada)** `RealtimePage : IAsyncDisposable`. En `OnInitializedAsync`: captura delegate local `HandleAlertAsync`, suscribe `AlertBroadcaster.OnAlert += _handler`. En `DisposeAsync`: `AlertBroadcaster.OnAlert -= _handler`. Evita memory leak al navegar fuera de la página.

**B)** Dispose en `OnAfterRenderAsync` — no garantizado; puede causar memory leak.

**[Answer]: A — IAsyncDisposable con captura local de delegate (ADR-U6-02) *(2026-05-24)***

---

### Q2 — Regex en TechnicalLogReader

**A) (Recomendada)** `static readonly Regex LogLinePattern = new Regex(@"...", RegexOptions.Compiled)` — compilada una vez al arrancar. Pattern extrae: timestamp, level (INF|WRN|ERR|CRT), mensaje. Stack traces acumulados en LogLine.Exception. File.ReadLines().TakeLast(N) para eficiencia (ADR-U6-03).

**B)** Regex no compilada instanciada por llamada — overhead en operación frecuente de logs.

**[Answer]: A — static readonly Regex compilada + TakeLast(N) *(2026-05-24)***

---

## Execution Checklist

- [x] 1 Analizar artefactos NFR Requirements de U6.
- [x] 2 Identificar patrones ya determinados vs decisiones pendientes.
- [x] 3 Crear este plan con 2 preguntas.
- [x] 4 Recopilar respuestas del owner. *(2/2 = A, A — 2026-05-24)*
- [x] 5 Analizar respuestas — 0 ambigüedades detectadas.
- [x] 6 Generar nfr-design-patterns.md.
- [x] 7 Generar logical-components.md (v1.0; v1.1 trazabilidad BrandMonitor→RF-31 — 2026-05-24).
- [x] 8 Actualizar aidlc-state.md.
- [x] 9 Registrar en audit.md.
- [x] 10 Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-24)*
