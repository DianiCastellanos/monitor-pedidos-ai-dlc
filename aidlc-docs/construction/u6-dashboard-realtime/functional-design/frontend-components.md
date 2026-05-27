# Frontend Components — U6 Dashboard & Real-Time

**Unidad:** U6 — Dashboard & Real-Time
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-24
**Versión:** 1.0

---

## §1 Páginas Blazor nuevas en U6

### 1.1 `RealtimePage.razor` — `/dashboard`

**Propósito:** Vista principal del sistema. Muestra el estado en tiempo real de los 4 dominios y el panel de brand monitor.

**Autorización:** `[Authorize]` — cualquier rol autenticado.

**Layout:** `MainLayout` (predeterminado) o `NocLayout` si la URL es `/noc`.

**Estructura de la página:**

```
RealtimePage
├── Fila de 4 tarjetas de dominio
│   ├── DomainStatusCard: DbOrders       (OK/WARN/CRITICAL + timestamp)
│   ├── DomainStatusCard: DbHealth       (OK/WARN/CRITICAL + timestamp)
│   ├── DomainStatusCard: Jobs           (OK/WARN/CRITICAL + timestamp)
│   └── DomainStatusCard: ApiIntegrations (OK/WARN/CRITICAL + timestamp)
│
└── Sección Brand Monitor (tabla compacta)
    ├── Encabezado: "Pedidos Pendientes por Marca (actualizado hace X min)"
    └── BrandMonitorTable (embed de BrandMonitorPage tabla)
```

**Suscripción a AlertBroadcaster:**

```csharp
@implements IAsyncDisposable

protected override async Task OnInitializedAsync()
{
    _snapshots  = await BrandMonitorService.GetCurrentSnapshotsAsync(default);
    _incidents  = await IncidentService.GetOpenIncidentsAsync(default);

    Broadcaster.OnAlert          += HandleAlertAsync;
    Broadcaster.OnIncidentClosed += HandleCloseAsync;
}

private async Task HandleAlertAsync(AlertMessage alert)
{
    // Actualizar estado del dominio correspondiente
    await InvokeAsync(StateHasChanged);
}

public async ValueTask DisposeAsync()
{
    Broadcaster.OnAlert          -= HandleAlertAsync;
    Broadcaster.OnIncidentClosed -= HandleCloseAsync;
}
```

---

### 1.2 `BrandMonitorPage.razor` — `/brand-monitor`

**Propósito:** Vista dedicada al monitoreo de pedidos pendientes por marca/site con semáforo visual.

**Autorización:** `[Authorize]` — cualquier rol autenticado.

**Estructura:**

```
BrandMonitorPage
├── Encabezado: "Monitor de Pedidos Pendientes por Marca"
├── Subtítulo: "Actualización automática cada 10 minutos"
└── Tabla:
    | Marca      | Actual | Hace 10 min | Cambio | Estado  |
    |------------|--------|-------------|--------|---------|
    | Patprimo   | 120    | 130         | -10    | 🟢 Bajó |
    | SevenSeven | 80     | 78          | +2     | 🔴 Subió|
    | Atmos      | 45     | 46          | -1     | 🟡 Poco |
    | Ostu       | 32     | 32          | 0      | 🔴 Igual|

    Columna "Cambio": PendingCountCurrent - PendingCountPrevious
    Columna "Estado": badge coloreado según SnapshotStatus
        🟢 Green  = "Normal"
        🟡 Yellow = "Lento"
        🔴 Red    = "Riesgo" / "Dato no disponible" (si current = -1)

    [Pie de tabla]: "Último chequeo: {CheckedAt:dd/MM HH:mm} — Umbral configurado: {threshold} pedidos"
```

**Datos:** `IBrandMonitorService.GetCurrentSnapshotsAsync()`. Carga en `OnInitializedAsync`. Sin suscripción a `AlertBroadcaster` (los snapshots se actualizan en el servidor cada 10 min; el usuario puede usar F5 para refrescar).

---

### 1.3 `DiscrepanciesPage.razor` — `/discrepancies`

**Propósito:** Lista de incidentes con causa `Discrepancy` (UC6). Vista silenciosa — sin push notifications.

**Autorización:** `[Authorize]` — cualquier rol autenticado.

**Estructura:**

```
DiscrepanciesPage
├── Encabezado: "Pedidos con Discrepancia"
├── Descripción: "Pedidos detectados en plataformas externas que no constan en la base de datos"
└── Tabla de incidentes:
    | Fecha       | Dominio  | Mensaje         | Estado |
    |-------------|----------|-----------------|--------|
    | 24/05 10:00 | DbOrders | "120 pedidos..." | Abierto|
    
    Sin botón "Reintentar" ni "Cerrar" — solo lectura
    Sin suscripción a AlertBroadcaster
```

---

### 1.4 `LogsPage.razor` — `/logs`

**Propósito:** Visor de logs técnicos para el `Técnico`. Exportación CSV y JSON.

**Autorización:** `[Authorize(Roles="Técnico")]` — redirect a `/access-denied` para `Operador`.

**Estructura:**

```
LogsPage
├── Encabezado: "Logs Técnicos del Sistema"
├── Controles:
│   ├── Selector de nivel: [Todos] [ERR] [WRN] [INF]
│   ├── Botón "Exportar CSV"
│   └── Botón "Exportar JSON"
└── Tabla de logs (últimas 500 líneas):
    | Timestamp           | Nivel | Mensaje           | Excepción |
    |---------------------|-------|-------------------|-----------|
    | 2026-05-24 10:00:00 | ERR   | "Connection timeout| ...stack  |
```

**Export:** `ITechnicalLogReader.ExportCsv` / `ExportJson` → `IJSRuntime.InvokeVoidAsync("downloadBlob", data, filename, mimeType)`.

---

### 1.5 `HistoricPage.razor` — `/history`

**Propósito:** Histórico de incidentes con filtros y paginación.

**Autorización:** `[Authorize]`.

**Estructura:**

```
HistoricPage
├── Filtros: Fecha desde/hasta | Dominio (dropdown) | Estado (Abierto/Cerrado)
├── Botón "Buscar"
└── Tabla paginada (20/página):
    | Fecha | Dominio | Causa | Mensaje | Estado | Duración |
    [< Anterior] [Página X de N] [Siguiente >]
```

---

### 1.6 `WeeklySummaryPage.razor` — `/summary`

**Propósito:** Resumen semanal de incidentes por dominio.

**Autorización:** `[Authorize]`.

**Estructura:**

```
WeeklySummaryPage
├── Encabezado: "Resumen Semanal — Semana del {inicio} al {fin}"
├── Tabla resumen:
    | Dominio     | Incidentes | MTTR (min) | Más frecuente |
    |-------------|------------|------------|---------------|
    | DbOrders    | 3          | 45         | Sin pedidos   |
    | DbHealth    | 1          | 12         | Latencia alta |
├── [< Semana anterior] [Semana actual]
```

---

## §2 Layouts

### 2.1 `NocLayout.razor`

**Propósito:** Layout alternativo sin sidebar. Activa el modo NOC.

```csharp
// NocLayout.razor
@inherits LayoutComponentBase

<div class="noc-container">
    <div class="noc-header">
        <span>MonitorPedidos — Modo NOC</span>
        <a href="/dashboard" class="btn-exit-noc">Salir de NOC</a>
    </div>
    <div class="noc-content">
        @Body
    </div>
</div>
```

**Ruta NOC:** `/noc` renderiza `RealtimePage` con `NocLayout`.

---

## §3 Servicios y hubs nuevos en U6

### 3.1 `AlertsHub.cs` — `/hubs/alerts`

```csharp
[Authorize]
public sealed class AlertsHub : Hub
{
    // Métodos que el servidor invoca en los clientes:
    // "AlertReceived"       → AlertMessage
    // "IncidentClosed"      → int incidentId
    // "SystemStatusUpdated" → StatusSummary
}
```

**Registro en Program.cs:**
```csharp
app.MapHub<AlertsHub>("/hubs/alerts");
```

---

### 3.2 `NotificationService.cs` — implementación real (reemplaza stub U3)

```csharp
public sealed class NotificationService : INotificationService
{
    private readonly IHubContext<AlertsHub> _hub;
    private readonly AlertBroadcaster      _broadcaster;

    public async Task BroadcastAlertAsync(AlertMessage alert, CancellationToken ct)
    {
        await _broadcaster.BroadcastAsync(alert);                          // Blazor in-process
        await _hub.Clients.All.SendAsync("AlertReceived", alert, ct);     // JS clients
    }

    public async Task BroadcastCloseAsync(int incidentId, CancellationToken ct)
    {
        await _broadcaster.BroadcastCloseAsync(incidentId);
        await _hub.Clients.All.SendAsync("IncidentClosed", incidentId, ct);
    }
}
```

---

### 3.3 `BrandMonitorChecker.cs`

```csharp
public sealed class BrandMonitorChecker : ICheckExecutor
{
    private static readonly string[] Sites = ["Patprimo", "SevenSeven", "Atmos", "Ostu"];

    private readonly IServiceScopeFactory     _scopeFactory;
    private readonly ISalesforceClient        _salesforce;
    private readonly ILogger<BrandMonitorChecker> _logger;

    // ISalesforceClient inyectado directo (es Transient via AddHttpClient — ok en Singleton)
    // IRuleRepository e IBrandSnapshotRepository: acceso via IServiceScopeFactory (Scoped)
}
```

---

## §4 JavaScript — `wwwroot/js/notifications.js`

```javascript
// Registrar conexión SignalR al AlertsHub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/alerts")
    .withAutomaticReconnect()
    .build();

connection.on("AlertReceived", (alert) => {
    showNotification(alert.title, alert.body);
});

connection.on("IncidentClosed", (id) => {
    flashTitle(`Incidente #${id} cerrado`);
});

async function showNotification(title, body) {
    if (Notification.permission === "granted") {
        new Notification(title, { body, icon: "/favicon.png" });
    } else {
        playAlertSound();
        flashTitle(title);
    }
}

function playAlertSound() {
    new Audio('/sounds/alert.mp3').play().catch(() => {});
}

let _flashInterval = null;
function flashTitle(message) {
    const original = document.title;
    _flashInterval = setInterval(() => {
        document.title = document.title === original ? `🔴 ${message}` : original;
    }, 1000);
    window.addEventListener("focus", () => {
        clearInterval(_flashInterval);
        document.title = original;
    }, { once: true });
}

window.downloadBlob = (data, filename, mimeType) => {
    const blob = new Blob([data], { type: mimeType });
    const url  = URL.createObjectURL(blob);
    const a    = document.createElement("a");
    a.href = url; a.download = filename; a.click();
    URL.revokeObjectURL(url);
};

connection.start();
```

---

## §5 Modificaciones a componentes existentes

| Componente | Unidad origen | Modificación en U6 |
|-----------|--------------|-------------------|
| `NavMenu.razor` | U1 | Agrega entradas: "Dashboard" (`/dashboard`), "Brand Monitor" (`/brand-monitor`), "Discrepancias" (`/discrepancies`), "Historial" (`/history`), "Resumen Semanal" (`/summary`), "Modo NOC" (`/noc`). Dentro de `<AuthorizeView Roles="Técnico">`: "Logs" (`/logs`) |
| `INotificationService` stub (U3) | U3 | Reemplazado por `NotificationService` real — DI registration cambia de stub a implementación |
| `MonitoringSchedulerService` | U3/U4 | Timer2 invoca también `BrandMonitorChecker` (registro como Singleton ICheckExecutor) |
| `AppDbContext` | U2 | Agrega `DbSet<BrandSnapshot> BrandSnapshots` |

---

## §6 Trazabilidad de componentes

| Componente | Story | RF | UC |
|-----------|-------|----|----|
| `RealtimePage` | US-01, US-02 | RF-22 | UC2 |
| `BrandMonitorPage` | ext. U6 | RF-22 ext. | — |
| `DiscrepanciesPage` | US-04 | RF-24 | UC6 |
| `LogsPage` | US-15, US-17 | RF-30 | — |
| `HistoricPage` | US-01 | RF-23 | UC2 |
| `WeeklySummaryPage` | US-01 | RF-24 | UC2 |
| `NocLayout` | US-02 | RF-22 | UC2 |
| `AlertsHub` | US-03 | RF-25 | UC3 |
| `NotificationService` (real) | US-03 | RF-25 | UC3 |
| `AlertBroadcaster` | US-01, US-03 | RF-22, RF-25 | UC2, UC3 |
| `BrandMonitorChecker` | ext. U6 | RF-22 ext. | — |
| `IBrandSnapshotRepository` | ext. U6 | RF-22 ext. | — |
| `IBrandMonitorService` | ext. U6 | RF-22 ext. | — |
| `ITechnicalLogReader` | US-17 | RF-30 | — |
| `notifications.js` | US-03 | RF-25 | UC3 |
