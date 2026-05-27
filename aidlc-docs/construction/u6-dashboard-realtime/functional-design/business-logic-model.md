# Business Logic Model — U6 Dashboard & Real-Time

**Unidad:** U6 — Dashboard & Real-Time
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-24
**Versión:** 1.0

---

## Flujo 1 — `NotificationService.BroadcastAlertAsync` (reemplaza stub U3)

```
MonitoringService detecta incidente → llama INotificationService.BroadcastAlertAsync(alert, ct)
    |
    NotificationService (implementación real — reemplaza stub U3)
        |
        ├── AlertBroadcaster.BroadcastAsync(alert)
        │       → dispara event OnAlert en todos los suscriptores
        │           → RealtimePage.InvokeAsync(StateHasChanged)  ← actualiza UI Blazor
        │           → (otros componentes suscritos en el futuro)
        │
        └── IHubContext<AlertsHub>.Clients.All.SendAsync("AlertReceived", alert, ct)
                → cliente JS recibe el evento
                    → notifications.js: showNotification(title, body)
                        ├── Si Notification.permission == "granted" → browser notification
                        └── Fallback: playAlertSound() + flashTitle(message)
```

**Doble camino:** `AlertBroadcaster` actualiza páginas Blazor en proceso (sin latencia de red). `AlertsHub` notifica al cliente JS para la notificación nativa del navegador.

---

## Flujo 2 — `RealtimePage`: actualización en tiempo real

```
Usuario navega a /dashboard → RealtimePage se monta
    |
    OnInitializedAsync()
        |
        IBrandMonitorService.GetCurrentSnapshotsAsync()
            → IReadOnlyList<BrandSnapshot> — se muestran en la tabla brand monitor
        |
        IIncidentService.GetOpenIncidentsAsync()
            → estado actual de los 4 dominios (DbOrders, DbHealth, Jobs, APIs)
        |
        AlertBroadcaster.OnAlert += HandleAlertAsync    ← suscripción
        AlertBroadcaster.OnIncidentClosed += HandleCloseAsync
    |
    [En memoria mientras el circuito Blazor está activo]
        MonitoringService dispara alert
            → AlertBroadcaster.BroadcastAsync(alert)
                → HandleAlertAsync invocado
                    → await InvokeAsync(StateHasChanged)  ← re-render
    |
    Dispose()
        AlertBroadcaster.OnAlert -= HandleAlertAsync    ← desuscripción
        AlertBroadcaster.OnIncidentClosed -= HandleCloseAsync
```

**Thread safety:** `AlertBroadcaster` usa `event` de C# — Blazor Server garantiza que `InvokeAsync` es thread-safe para el circuito.

---

## Flujo 3 — `BrandMonitorChecker.ExecuteAsync` (cada 10 min)

```
MonitoringSchedulerService Timer2 (10 min) tick
    |
    BrandMonitorChecker.ExecuteAsync(ct)
        |
        IServiceScopeFactory.CreateAsyncScope()
            |
            foreach site in ["Patprimo", "SevenSeven", "Atmos", "Ostu"]:
                |
                ISalesforceClient.GetPendingOrdersAsync(site, ct)   ← U4 client
                    → pendingCount (int)
                |
                IBrandSnapshotRepository.GetBySiteAsync(site, ct)
                    → previousSnapshot (puede ser null en primer chequeo)
                    → previousCount = previousSnapshot?.PendingCountCurrent ?? pendingCount
                |
                IRuleRepository.GetActiveByModuleAsync(BrandMonitor, ct)   ← U5
                    → rule?.GetCondition()?.PendingDropThreshold ?? 0
                    → threshold (int)
                |
                BrandSnapshot.Upsert(site, pendingCount, previousCount, threshold)
                    → SnapshotStatus determinado: Green | Yellow | Red
                |
                IBrandSnapshotRepository.UpsertAsync(snapshot, ct)
                    → sobreescribe la fila de ese site en brand_snapshots
        |
        scope destruido (await using)
        |
        CheckResult retornado: Ok (siempre — el checker no genera incidentes propios;
                                    solo actualiza snapshots para la vista)
```

**Nota:** `BrandMonitorChecker` no genera incidentes en `MonitoringService`. Su único output es actualizar `brand_snapshots`. La vista Blazor muestra los datos directamente.

---

## Flujo 4 — `BrandMonitorPage`: visualización semáforo

```
Usuario navega a /brand-monitor → BrandMonitorPage se monta
    |
    OnInitializedAsync()
        IBrandMonitorService.GetCurrentSnapshotsAsync()
            → IReadOnlyList<BrandSnapshot> (4 elementos)
    |
    Render tabla:
        | Marca      | Actual | Hace 10 min | Cambio | Estado |
        |------------|--------|-------------|--------|--------|
        | Patprimo   | 120    | 130         | -10    | 🟢     |
        | SevenSeven | 80     | 78          | +2     | 🔴     |
        | Atmos      | 45     | 46          | -1     | 🟡     |
        | Ostu       | 32     | 32          | 0      | 🔴     |

    Columna "Cambio" = PendingCountCurrent - PendingCountPrevious (negativo = baja = bueno)
    Columna "Estado" = badge con color según SnapshotStatus:
        Green  → badge verde "Normal"
        Yellow → badge amarillo "Lento"
        Red    → badge rojo "Riesgo"
```

---

## Flujo 5 — `DiscrepanciesPage`: panel UC6 silencioso

```
Usuario navega a /discrepancies
    |
    IIncidentService.GetDiscrepancyIncidentsAsync(filtros)
        → incidencias con Cause = Discrepancy (UC6)
        → NO dispara INotificationService
    |
    Render tabla de incidencias de discrepancia
        → Operador las revisa manualmente
        → Sin botón "reintentar" (solo lectura)
```

---

## Flujo 6 — `LogsPage`: exportación técnica

```
Técnico navega a /logs → [Authorize(Roles="Técnico")]
    |
    ITechnicalLogReader.GetRecentAsync(maxLines: 500, ct)
        → lee últimas 500 líneas del archivo Serilog (log-.txt)
        → parsea a IReadOnlyList<LogLine>
    |
    Render tabla: Timestamp | Level | Message | Exception
    |
    Usuario pulsa "Exportar CSV":
        ITechnicalLogReader.ExportCsv(lines)
            → StringBuilder → string CSV
        IJSRuntime.InvokeVoidAsync("downloadBlob", csv, "logs.csv", "text/csv")
    |
    Usuario pulsa "Exportar JSON":
        ITechnicalLogReader.ExportJson(lines)
            → JsonSerializer.Serialize(lines, _jsonOpts)
        IJSRuntime.InvokeVoidAsync("downloadBlob", json, "logs.json", "application/json")
```

---

## Flujo 7 — `NocLayout`: activación modo NOC

```
Usuario (cualquier rol) pulsa "Modo NOC" en NavMenu
    |
    NavigationManager.NavigateTo("/noc")
    |
    NocLayout.razor se aplica como layout
        → NavMenu oculto (no está en NocLayout)
        → Contenido = RealtimePage sin sidebar
        → Título: "Monitor en tiempo real — Modo NOC"
    |
    Usuario pulsa "Salir de NOC":
        NavigationManager.NavigateTo("/dashboard")
        → vuelve a MainLayout con sidebar
```

---

## Flujo 8 — Fallback de Notification API

```
Primera vez que usuario recibe alerta con Notification API:
    |
    notifications.js: Notification.requestPermission()
        ├── "granted"  → showNotification(title, body)
        │                   → new Notification(title, { body, icon })
        └── "denied" / "default"  → fallback:
                playAlertSound()      → new Audio('/sounds/alert.mp3').play()
                flashTitle(message)   → setInterval alterna title "🔴 ALERTA" / "MonitorPedidos"
```
