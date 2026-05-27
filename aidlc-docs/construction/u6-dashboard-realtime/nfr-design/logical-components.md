# Logical Components — U6 Dashboard & Real-Time

**Unidad:** U6 — Dashboard & Real-Time
**Stage:** Construction → NFR Design
**Fecha:** 2026-05-24
**Versión:** 1.1 (2026-05-24 — trazabilidad actualizada: BrandMonitor referencias a RF-31; etiquetas "ext. U6" eliminadas)

---

## §1 Componentes de producción introducidos en U6

| ID | Componente | Tipo | Proyecto | Descripción |
|----|-----------|------|---------|-------------|
| LC-U6-01 | `BrandSnapshot` | Entity | MonitorPedidos.Web | Estado de pedidos pendientes de un site; `static Upsert(site, current, previous, threshold)` determina `SnapshotStatus` |
| LC-U6-02 | `SnapshotStatus` | Enum | MonitorPedidos.Web | `Green` / `Yellow` / `Red` — semáforo determinista |
| LC-U6-03 | `IBrandSnapshotRepository` | Interfaz | MonitorPedidos.Web | Solo `UpsertAsync` + `GetAllAsync` — sin historial, sin Delete |
| LC-U6-04 | `BrandSnapshotRepository` | Clase (IBrandSnapshotRepository) | MonitorPedidos.Web | EF Core UPSERT por site; tabla `brand_snapshots` con 4 filas fijas |
| LC-U6-05 | `IBrandMonitorService` | Interfaz | MonitorPedidos.Web | `GetCurrentSnapshotsAsync()` — lectura para la página Blazor |
| LC-U6-06 | `BrandMonitorService` | Clase (IBrandMonitorService) | MonitorPedidos.Web | Scoped; delega en `IBrandSnapshotRepository` |
| LC-U6-07 | `AlertBroadcaster` | Clase singleton | MonitorPedidos.Web | Event multicast in-process; captura local de delegate (ADR-U6-02) |
| LC-U6-08 | `NotificationService` | Clase (INotificationService) | MonitorPedidos.Web | Reemplaza stub U3; invoca `AlertBroadcaster` + `IHubContext<AlertsHub>` |
| LC-U6-09 | `AlertsHub` | Hub SignalR | MonitorPedidos.Web | Emite `AlertReceived`, `IncidentClosed`, `SystemStatusUpdated`; decorado con `[Authorize]` |
| LC-U6-10 | `BrandMonitorChecker` | Clase (ICheckExecutor) | MonitorPedidos.Web | Singleton; `IServiceScopeFactory` para repos Scoped; evalúa 4 sites; error de API → Red con current=-1 |
| LC-U6-11 | `ITechnicalLogReader` | Interfaz | MonitorPedidos.Web | `GetRecentAsync(maxLines)`, `ExportCsv(lines)`, `ExportJson(lines)` |
| LC-U6-12 | `TechnicalLogReader` | Clase (ITechnicalLogReader) | MonitorPedidos.Web | `static readonly Regex` compilada (ADR-U6-03); `File.ReadLines().TakeLast(N)`; stack traces → `LogLine.Exception` |
| LC-U6-13 | `RealtimePage` | Blazor Page (`/dashboard`) | MonitorPedidos.Web | Suscrita a `AlertBroadcaster`; `IAsyncDisposable`; muestra 4 dominios + Brand Monitor table |
| LC-U6-14 | `BrandMonitorPage` | Blazor Page (`/brand-monitor`) | MonitorPedidos.Web | Tabla Marca\|Actual\|Hace 10 min\|Cambio\|Estado con badges de color |
| LC-U6-15 | `DiscrepanciesPage` | Blazor Page (`/discrepancies`) | MonitorPedidos.Web | Lista UC6 silenciosa; sin push; solo lectura |
| LC-U6-16 | `LogsPage` | Blazor Page (`/logs`) | MonitorPedidos.Web | `[Authorize(Roles="Técnico")]`; filtro por nivel; export CSV/JSON via `IJSRuntime` blob |
| LC-U6-17 | `HistoricPage` | Blazor Page (`/history`) | MonitorPedidos.Web | Filtros fecha/dominio/estado + paginación 20/página; reutiliza `IIncidentService` |
| LC-U6-18 | `WeeklySummaryPage` | Blazor Page (`/summary`) | MonitorPedidos.Web | Resumen semanal: incidentes por dominio + MTTR; navegación semana anterior/actual |
| LC-U6-19 | `NocLayout` | Blazor Layout | MonitorPedidos.Web | Oculta `NavMenu`; área de estado 100% ancho; botón "Salir de NOC" → `/dashboard` |

---

## §2 Componentes de producción modificados en U6

| Componente | Unidad origen | Modificación |
|-----------|--------------|-------------|
| `INotificationService` stub | U3 | Reemplazado en DI por `NotificationService` real — el stub se elimina del registro en `Program.cs` |
| `NavMenu.razor` | U1 | Agrega: "Dashboard" (`/dashboard`), "Brand Monitor" (`/brand-monitor`), "Discrepancias" (`/discrepancies`), "Historial" (`/history`), "Resumen Semanal" (`/summary`), "Modo NOC" (`/noc`). Dentro de `<AuthorizeView Roles="Técnico">`: "Logs" (`/logs`) |
| `AppDbContext` | U2 | Agrega `DbSet<BrandSnapshot> BrandSnapshots` |
| `MonitoringSchedulerService` | U3/U4 | Timer2 (10 min) invoca también `BrandMonitorChecker` vía `IEnumerable<ICheckExecutor>` — sin modificación al código del scheduler (ADR-U3-02: resolución por tipo) |
| `RuleCondition` | U5 | Agrega campo `int? PendingDropThreshold`; `IsValidForModule` agrega caso `BrandMonitor` |
| `ModuleId` | U5 | Agrega valor `BrandMonitor = 4` |

---

## §3 Componentes de test introducidos en U6

| ID | Componente | Tipo | Proyecto | Escenarios |
|----|-----------|------|---------|-----------|
| TC-U6-01 | `NotificationServiceTests` | xUnit + Moq | MonitorPedidos.UnitTests | 3 tests: BroadcastAlert invoca ambos canales (ADR-U6-01 triple mock); excepción en hub se propaga; BroadcastClose funciona |
| TC-U6-02 | `BrandMonitorCheckerTests` | xUnit + Moq | MonitorPedidos.UnitTests | 4 tests: bajó suficiente → Green; subió → Red; ISalesforceClient falla → Red(current=-1) sin afectar otros sites; sin regla activa → threshold=0 → Green |

**Total tests U6:** 7 (3 unit TC-U6-01 + 4 unit TC-U6-02)

---

## §4 Diagrama de dependencias U6

```
RealtimePage / BrandMonitorPage / DiscrepanciesPage / HistoricPage / WeeklySummaryPage
    [Authorize]
    |
    ├── AlertBroadcaster (Singleton)          ← RealtimePage se suscribe/desuscribe (IAsyncDisposable)
    │       OnAlert += HandleAlertAsync       ← ADR-U6-02: captura local de delegate
    │
    ├── IBrandMonitorService (Scoped)
    │       BrandMonitorService
    │           IBrandSnapshotRepository
    │               BrandSnapshotRepository → AppDbContext → tabla brand_snapshots (4 filas)
    │
    └── IIncidentService (Scoped, U2)         ← HistoricPage, WeeklySummaryPage, DiscrepanciesPage

LogsPage [Authorize Roles="Técnico"]
    ITechnicalLogReader (Scoped)
        TechnicalLogReader
            static readonly Regex (ADR-U6-03)
            File.ReadLines().TakeLast(N)      ← Logging:MaxExportLines desde appsettings
            IJSRuntime → downloadBlob         ← export CSV/JSON

NocLayout → wraps RealtimePage → misma suscripción a AlertBroadcaster

NotificationService (INotificationService — reemplaza stub U3)
    ├── AlertBroadcaster.BroadcastAsync()     ← in-process Blazor pages
    └── IHubContext<AlertsHub>.Clients.All    ← JS clients

AlertsHub [Authorize]
    → /hubs/alerts
    → emite: AlertReceived | IncidentClosed | SystemStatusUpdated

BrandMonitorChecker (Singleton + IServiceScopeFactory)
    ├── ISalesforceClient (Transient via AddHttpClient — seguro en Singleton)
    ├── IRuleRepository (Scoped via scope) → ModuleId.BrandMonitor → PendingDropThreshold
    └── IBrandSnapshotRepository (Scoped via scope) → UpsertAsync por site

MonitoringSchedulerService
    Timer2 (10 min) → IEnumerable<ICheckExecutor> donde c is BrandMonitorChecker
                                                           or SalesforceApiChecker
                                                           or MultivendeApiChecker

wwwroot/js/lib/signalr.min.js    ← local (ADR-U6-04: sin CDN)
wwwroot/js/notifications.js
    signalR.HubConnectionBuilder().withAutomaticReconnect()  ← ADR-U6-02 JS
    .on("AlertReceived") → showNotification | playAlertSound + flashTitle (fallback)
    window.downloadBlob  ← llamado desde LogsPage via IJSRuntime
```

---

## §5 Reglas de diseño aplicadas en U6

| ADR | Patrón | Componentes afectados |
|-----|--------|----------------------|
| ADR-U6-01 | Triple mock `IHubContext<T>` (Moq) | `NotificationServiceTests` |
| ADR-U6-02 | Captura local de delegate en event multicast | `AlertBroadcaster`, `RealtimePage` |
| ADR-U6-03 | `static readonly Regex` compilada + acumulación de stack traces | `TechnicalLogReader` |
| ADR-U6-04 | `signalr.min.js` local en `wwwroot/js/lib/` | `_Host.cshtml`, build |
| ADR-U3-02 (IEnumerable DI) | `BrandMonitorChecker` registrado como `ICheckExecutor` Singleton | `MonitoringSchedulerService` (sin cambios) |
| ADR-U5-01 (IServiceScopeFactory) | Scope por tick para `IRuleRepository` + `IBrandSnapshotRepository` | `BrandMonitorChecker` |
| ADR-U5-02 (`[Authorize]` doble capa) | `[Authorize(Roles="Técnico")]` en `LogsPage` + `AuthorizeView` en `NavMenu` | `LogsPage`, `NavMenu` |

---

## §6 Trazabilidad de componentes

| Componente | Story | RF | NFR | ADR |
|-----------|-------|----|----|-----|
| `BrandSnapshot` + `IBrandSnapshotRepository` | — | RF-31 | NFR-U6-02 | ADR-U6-05 (IServiceScopeFactory) |
| `BrandMonitorChecker` | — | RF-31, RF-03 | NFR-U6-02 | ADR-U3-02, ADR-U5-01 |
| `AlertBroadcaster` | US-01, US-03 | RF-22, RF-25 | BR-CONC-01 | ADR-U6-02 |
| `NotificationService` (real) | US-03 | RF-25 | NFR-U6-01 | ADR-U6-01 |
| `AlertsHub` | US-03 | RF-25 | NFR-U6-03 | ADR-U6-04 |
| `TechnicalLogReader` | US-17 | RF-30 | NFR-U6-04 | ADR-U6-03 |
| `RealtimePage` | US-01, US-02 | RF-22 | BR-CONC-03 | ADR-U6-02 |
| `LogsPage` | US-15, US-17 | RF-30 | — | ADR-U5-02 |
| `NocLayout` | US-02 | RF-22 | — | — |
| `NotificationServiceTests` (3) | US-03 | RF-25 | NFR-U6-01 | ADR-U6-01 |
| `BrandMonitorCheckerTests` (4) | ext. U6 | RF-22 ext. | NFR-U6-02 | — |
