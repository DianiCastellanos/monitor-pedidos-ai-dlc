# Frontend Components — U3 Detection & Classification

**Unidad:** U3 — Detection & Classification
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Páginas Blazor en U3

**U3 no introduce páginas Blazor nuevas.** Todos sus componentes son servicios de backend (BackgroundService, checkers, classifier, renderer). El usuario no interactúa directamente con U3 — U3 detecta y produce datos que otras unidades muestran.

| Unidad | Qué muestra lo que U3 produce |
|--------|-------------------------------|
| U6 Dashboard & Real-Time | Alertas en tiempo real vía SignalR (`INotificationService.BroadcastAlertAsync`) |
| U2 Persistence & Incidents | Historial de incidentes, detalle de incidente (`IncidentDetailPage`) |
| U5 Rules Management | Vista "Candidatos a regla nueva" — incidentes con `IsCandidatoReglaNueva = true` |

---

## §2 Contratos de datos que U3 expone a la UI

Aunque U3 no tiene páginas propias, define los tipos que U6 y U2 consumen para mostrar información al operador.

### AlertMessage (producido por M10 — consumido por U6)

```
AlertMessage
├── QuePaso          → descripción legible del incidente en español
├── Cuando           → fecha/hora local en dd/MM/yyyy HH:mm
├── Donde            → módulo + capa (ej. "Módulo DbOrders — Base de Datos")
├── SeveridadTexto   → "CRÍTICO" | "ADVERTENCIA"
├── CausaProbable    → explicación de la causa probable
└── AccionSugerida   → pasos recomendados para el operador
```

Este objeto se persiste dentro del `Incident` (U2) y se envía via SignalR al dashboard (U6).

### CheckResult (producido por checkers — consumido internamente por MonitoringService)

```
CheckResult
├── Status     → Ok | Warn | Critical
├── Details    → string técnico (va a logs, NO a la UI del operador)
└── CheckedAt  → timestamp UTC de la ejecución
```

`Details` es información técnica para logs. El operador ve `AlertMessage`, no `CheckResult.Details`.

---

## §3 US-18 — Vista "Candidatos a regla" (diferida a U5)

US-18 requiere que el Técnico vea los incidentes marcados como `IsCandidatoReglaNueva = true`. Esta vista **no se implementa en U3** — se implementa en U5 (Rules Management) donde el Técnico también crea las reglas correspondientes.

Lo que U3 sí hace para US-18:
- Marca `IsCandidatoReglaNueva = true` en el incidente cuando `CauseCategory == NoDeterminada` (BR-CLASS-03)
- Loggea el evento con nivel Warning: `"Incidente {Id} en módulo {Module} — causa no determinada, candidato a regla nueva"`

Lo que U5 implementará para completar US-18:
- Filtro en `HistoricPage` o página dedicada para ver candidatos
- Acción para crear regla desde el incidente candidato

---

## §4 Logging visible indirectamente en UI (logs Serilog)

Aunque U3 no tiene páginas, sus logs son visibles para el Técnico que revisa los archivos `logs/monitor-pedidos-*.log`. Los eventos importantes que U3 registra:

| Evento | Nivel | Campos loggeados |
|--------|-------|-----------------|
| Checker ejecutado OK | `Information` | Module, Status, Details |
| Incidente abierto | `Information` | IncidentId, Module, Cause, Severity |
| Incidente cerrado automáticamente | `Information` | IncidentId, Module |
| Checker falló con excepción | `Error` | Module, ExceptionMessage |
| Causa no determinada | `Warning` | IncidentId, Module, CheckDetails |
| Purge scheduler tick | `Debug` | Timestamp |

Todos los campos respetan SECURITY-14 — sin PII, sin datos de negocio sensibles, solo IDs técnicos y módulos.

---

## §5 Trazabilidad

| Componente | Story | Unidad que lo muestra |
|-----------|-------|----------------------|
| `AlertMessage` (6 campos) | US-06 | U6 Dashboard |
| Incidente WARN/CRITICAL | US-07, US-08, US-09 | U2 HistoricPage |
| `IsCandidatoReglaNueva` | US-18 | U5 Rules Management |
| Logs de detección | US-07..09 | Técnico vía archivo log |
