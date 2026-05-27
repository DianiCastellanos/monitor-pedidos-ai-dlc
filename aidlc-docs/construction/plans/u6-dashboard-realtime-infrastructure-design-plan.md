# Infrastructure Design Plan — U6 Dashboard & Real-Time

## Metadata

| Field   | Value                                                                                                                            |
|---------|----------------------------------------------------------------------------------------------------------------------------------|
| Stage   | Construction → Infrastructure Design                                                                                             |
| Unit    | U6 — Dashboard & Real-Time                                                                                                       |
| Date    | 2026-05-24                                                                                                                       |
| Sources | u6 nfr-design/ (2 artifacts approved), u6 functional-design/domain-entities.md, u5 infrastructure-design/ |

---

## Context — Already Decided

| Aspecto | Decisión |
|---------|----------|
| signalr.min.js | Local en wwwroot/js/lib/ — ADR-U6-04 |
| Hub endpoint | /hubs/alerts |
| Sin exposición pública | Restricción del owner |
| Migration brand_snapshots | AddBrandSnapshots incluida aquí |

---

## Questions

### Q1 — Migration para brand_snapshots

**A) (Recomendada)** Migration `AddBrandSnapshots`: crea tabla `brand_snapshots` (Id, Site, PendingCountCurrent, PendingCountPrevious, CheckedAt, Status). Incluye `InsertData` para seed de 4 reglas iniciales (DbOrders, DbHealth, Jobs, BrandMonitor con PendingDropThreshold=5). Aplicada después de AddRulesSchema (U5).

**B)** Tabla sin seed — requiere inserción manual de reglas antes de operar.

**[Answer]: A — AddBrandSnapshots con tabla + seed de 4 reglas *(2026-05-24)***

---

### Q2 — Registro de SignalR en Program.cs

**A) (Recomendada)** `builder.Services.AddSignalR()` + `app.MapHub<AlertsHub>("/hubs/alerts")`. CSP actualizada para incluir `connect-src wss:` (ya definida en U1). Sin opciones adicionales de SignalR para MVP (sin sticky sessions necesarias en proceso único).

**B)** SignalR con Azure SignalR Service — innecesario para monolito local.

**[Answer]: A — SignalR local con MapHub("/hubs/alerts") *(2026-05-24)***

---

### Q3 — Configuración de MaxExportLines en LogsPage

**A) (Recomendada)** `appsettings.json` sección `"Logging": { "MaxExportLines": 1000 }`. `TechnicalLogReader` lee este valor vía IConfiguration. Límite razonable para export sin memory issues.

**B)** Hardcoded en TechnicalLogReader — no ajustable sin recompilar.

**[Answer]: A — MaxExportLines en appsettings.json *(2026-05-24)***

---

## Execution Checklist

- [x] 1 Analizar artefactos Functional Design y NFR Design de U6.
- [x] 2 Identificar decisiones ya tomadas vs ambigüedades pendientes.
- [x] 3 Crear este plan con 3 preguntas enfocadas.
- [x] 4 Recopilar respuestas del owner. *(3/3 = A, A, A — 2026-05-24)*
- [x] 5 Analizar respuestas — 0 ambigüedades detectadas.
- [x] 6 Generar infrastructure-design.md.
- [x] 7 Generar deployment-architecture.md (cumulative U1..U6).
- [x] 8 Actualizar aidlc-state.md.
- [x] 9 Registrar en audit.md.
- [x] 10 Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-24)*
