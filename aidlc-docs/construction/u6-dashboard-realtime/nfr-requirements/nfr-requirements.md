# NFR Requirements — U6 Dashboard & Real-Time

**Unidad:** U6 — Dashboard & Real-Time
**Stage:** Construction → NFR Requirements
**Fecha:** 2026-05-24
**Versión:** 1.0

---

## §1 Decisiones aplicadas

| ID | Pregunta | Decisión |
|----|----------|---------|
| P1 | Tests para NotificationService y BrandMonitorChecker | A — 7 unit tests con Moq: 3 para NotificationService + 4 para BrandMonitorChecker |
| P2 | Reconexión automática del AlertsHub | A — `.withAutomaticReconnect()` en notifications.js — política por defecto (0s, 2s, 10s, 30s) |
| P3 | Límite de líneas en LogsPage | A — Configurable en `appsettings.json` (`Logging:MaxExportLines: 500`); lectura con `File.ReadLines().TakeLast(N)` |
| P4 | Paquetes NuGet nuevos en producción | A — Sin paquetes nuevos; SignalR JS cargado desde CDN en `_Host.cshtml` |

---

## §2 NFRs propios de U6

### NFR-U6-01 — Tests unitarios: NotificationService (3 tests)

**Descripción:** Verificar que `NotificationService` invoca ambos canales correctamente y que el fallo de uno no bloquea al otro.

**Tests (xUnit + Moq):**

| # | Escenario | Verificación |
|---|-----------|-------------|
| 1 | `BroadcastAlertAsync` exitoso | Invoca `AlertBroadcaster.BroadcastAsync` Y `IHubContext.Clients.All.SendAsync("AlertReceived")` |
| 2 | `IHubContext.SendAsync` lanza excepción | La excepción se propaga (no se silencia) — el caller en `MonitoringService` la captura |
| 3 | `BroadcastCloseAsync` exitoso | Invoca `AlertBroadcaster.BroadcastCloseAsync` Y `IHubContext.Clients.All.SendAsync("IncidentClosed")` |

---

### NFR-U6-02 — Tests unitarios: BrandMonitorChecker (4 tests)

**Descripción:** Verificar la lógica del semáforo determinista y el manejo de errores de API.

**Tests (xUnit + Moq):**

| # | Escenario | Input | Expected |
|---|-----------|-------|---------|
| 1 | Pedidos bajaron suficiente | previous=130, current=120, threshold=5 → drop=10 ≥ 5 | `SnapshotStatus.Green` |
| 2 | Pedidos subieron (sin mejora) | previous=78, current=80, threshold=5 → drop=-2 | `SnapshotStatus.Red` |
| 3 | `ISalesforceClient` lanza excepción para un site | site="Atmos" → excepción | `SnapshotStatus.Red`, `PendingCountCurrent = -1`; otros sites no afectados |
| 4 | Sin regla activa en `IRuleRepository` | `GetActiveByModuleAsync` retorna null → threshold=0 | cualquier drop > 0 → `SnapshotStatus.Green` |

**Total tests U6:** 7 (3 NotificationService + 4 BrandMonitorChecker)

---

### NFR-U6-03 — Reconexión automática del AlertsHub

**Descripción:** El cliente JS se reconecta automáticamente al `AlertsHub` sin intervención del usuario.

**Implementación:**

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/alerts")
    .withAutomaticReconnect()   // política: 0s, 2s, 10s, 30s
    .build();
```

**Comportamiento:** Si la conexión se pierde (reinicio del servidor, corte de red), el cliente JS reintenta automáticamente con backoff. El dashboard Blazor sigue mostrando el último estado conocido durante la reconexión.

---

### NFR-U6-04 — Límite configurable de líneas exportadas

**Descripción:** `ITechnicalLogReader` no carga el archivo de log completo en memoria.

**Configuración:**

```json
// appsettings.json
{
  "Logging": {
    "MaxExportLines": 500
  }
}
```

**Implementación:** `File.ReadLines(path).TakeLast(maxLines)` — eficiencia O(N) donde N = `MaxExportLines`. Sin riesgo de timeout para archivos grandes.

**Desarrollo:** `appsettings.Development.json` puede reducir a 100 líneas para pruebas rápidas.

---

### NFR-U6-05 — SignalR JS desde CDN (sin paquete NuGet en producción)

**Descripción:** El cliente JavaScript de SignalR se carga desde CDN, no desde un paquete NuGet.

**Referencia en `_Host.cshtml` (o `App.razor`):**

```html
<script src="https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js"
        integrity="sha512-..." crossorigin="anonymous"></script>
<script src="~/js/notifications.js"></script>
```

**Rationale:** La aplicación corre en localhost/red interna — el CDN es accesible. Sin paquete `Microsoft.AspNetCore.SignalR.Client` en producción (solo es necesario para integration tests del hub, que se decide no implementar en MVP).

---

## §3 NFRs heredados de U1 aplicables a U6

| NFR | Descripción | Aplicación en U6 |
|-----|------------|-----------------|
| NFR-U1-01 | CSP headers via middleware | Aplica a todas las páginas nuevas de U6. La política existente cubre `/hubs/*` vía `connect-src 'self'` |
| NFR-U1-02 | Cookie auth 8h sliding | `AlertsHub` decorado con `[Authorize]` — el middleware de cookies aplica automáticamente |
| NFR-U1-03 | Serilog logging | `BrandMonitorChecker` y `NotificationService` inyectan `ILogger<T>`. LogDebug para chequeos exitosos (mismo patrón U4) |
| NFR-U1-04 | GlobalExceptionHandler | Captura excepciones no controladas en páginas Blazor y en el hub |
| NFR-U1-05 | Scoped por circuito Blazor | `IBrandMonitorService`, `IIncidentService`, `IRuleRepository` registrados como Scoped — un DbContext por usuario |

---

## §4 Trazabilidad NFR → Story

| NFR | Story | RF | ADR pendiente |
|-----|-------|----|----|
| NFR-U6-01 (7 tests) | US-01, US-03, ext. U6 | RF-22, RF-25 | ADR-U6-01 (Moq IHubContext) |
| NFR-U6-02 (reconexión) | US-03 | RF-25 | ADR-U6-02 (withAutomaticReconnect) |
| NFR-U6-03 (límite logs) | US-17 | RF-30 | ADR-U6-03 (TakeLast + config) |
| NFR-U6-04 (CDN) | US-03 | RF-25 | ADR-U6-04 (CDN vs bundle) |
| NFR-U6-05 (Scoped DI) | US-29 | RNF-15 | — (heredado ADR-U1) |
