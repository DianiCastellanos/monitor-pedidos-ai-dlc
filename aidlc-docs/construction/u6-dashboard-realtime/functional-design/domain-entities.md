# Domain Entities — U6 Dashboard & Real-Time

**Unidad:** U6 — Dashboard & Real-Time
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-24
**Versión:** 1.1 (2026-05-24 — §4 actualizado: RuleCondition/ModuleId son definición primaria en U5 v1.1; referencias aquí actualizadas)

---

## §1 Decisiones de diseño aplicadas

| ID | Pregunta | Decisión |
|----|----------|---------|
| P1 | Mecanismo real-time en `RealtimePage` | A — `AlertBroadcaster` singleton (C# event) + `InvokeAsync(StateHasChanged)` — Blazor Server idiomatic, sin doble conexión SignalR |
| P2 | Brand Monitor: fuente de datos | A — `BrandMonitorChecker` (ICheckExecutor) + tabla `brand_snapshots` (4 filas fijas, overwrite, sin histórico) — reutiliza Timer2 de U4 |
| P3 | Semáforo brand monitor: umbrales | A — `RuleCondition` extendida con `PendingDropThreshold`; módulo `BrandMonitor` configurable desde `RulesPage` (U5). Seed: umbral=5 |
| P4 | NocLayout: modo de activación | A — Toggle en `NavMenu` + layout `NocLayout.razor` que oculta sidebar. Sin JS fullscreen API |
| P5 | LogsPage: exportación | A — CSV con `StringBuilder` + JSON con `System.Text.Json` — 0 dependencias adicionales; download via `IJSRuntime` blob |

---

## §2 Bounded context: Dashboard

El bounded context `Dashboard` es la capa de presentación del sistema. Consume servicios de U2 (IIncidentService), U3 (INotificationService stub → reemplazado aquí), U4 (ISalesforceClient, IMultivendeClient), U5 (IRuleRepository). No define un aggregate propio — su responsabilidad es presentar, notificar y coordinar.

---

## §3 Nueva entidad: `BrandSnapshot`

```csharp
// Entidad liviana — no aggregate root
// Representa el estado de pedidos pendientes de un site en el último chequeo
public sealed class BrandSnapshot
{
    public int     Id                   { get; private set; }
    public string  Site                 { get; private set; }  // "Patprimo" | "SevenSeven" | "Atmos" | "Ostu"
    public int     PendingCountCurrent  { get; private set; }  // pedidos pendientes ahora
    public int     PendingCountPrevious { get; private set; }  // pedidos pendientes hace 10 min
    public DateTime CheckedAt           { get; private set; }
    public SnapshotStatus Status        { get; private set; }  // Green | Yellow | Red

    private BrandSnapshot() { }

    // Crea o actualiza snapshot para un site
    public static BrandSnapshot Upsert(
        string site,
        int currentPending,
        int previousPending,
        int dropThreshold)
    {
        var status = DetermineStatus(currentPending, previousPending, dropThreshold);
        return new BrandSnapshot
        {
            Site                 = site,
            PendingCountCurrent  = currentPending,
            PendingCountPrevious = previousPending,
            CheckedAt            = DateTime.UtcNow,
            Status               = status
        };
    }

    private static SnapshotStatus DetermineStatus(int current, int previous, int threshold)
    {
        var drop = previous - current;
        if (drop >= threshold) return SnapshotStatus.Green;   // bajó suficiente → 🟢
        if (drop > 0)          return SnapshotStatus.Yellow;  // bajó poco → 🟡
        return SnapshotStatus.Red;                            // igual o subió → 🔴
    }
}

public enum SnapshotStatus { Green, Yellow, Red }
```

**Invariantes:**
- `Site` es uno de los 4 valores válidos — validado en `BrandMonitorChecker`
- La tabla `brand_snapshots` tiene exactamente 4 filas (UPSERT por site) — sin historial creciente
- `PendingCountCurrent` y `PendingCountPrevious` son siempre >= 0

---

## §4 Referencia a entidades U5 actualizadas: `RuleCondition` y `ModuleId`

`BrandMonitorChecker` consume el motor de reglas de U5. Las modificaciones a `RuleCondition` y `ModuleId` están definidas como **fuente primaria en U5 v1.1** ([domain-entities.md U5 §2 y §3](../../u5-rules-management/functional-design/domain-entities.md)).

**Resumen de los cambios incorporados en U5:**

| Artefacto | Cambio | Dónde |
|-----------|--------|-------|
| `ModuleId` (enum U1) | Agrega `BrandMonitor = 4` | U5 domain-entities §2 |
| `RuleCondition` | Agrega `int? PendingDropThreshold` + factory `ForBrandMonitor(int)` + caso en `IsValidForModule` | U5 domain-entities §3 |
| Seed regla | `ModuleId.BrandMonitor`, `PendingDropThreshold = 5`, `IsActive = true` | Migración `AddBrandSnapshots` (U6 infrastructure-design) |

U6 usa estos contratos sin redefinirlos. `BrandMonitorChecker` llama `IRuleRepository.GetActiveByModuleAsync(ModuleId.BrandMonitor)` para obtener el umbral en runtime (BR-RULE-09).

---

## §5 Nuevas interfaces de repositorio

### `IBrandSnapshotRepository`

```csharp
public interface IBrandSnapshotRepository
{
    // UPSERT por site — sobrescribe la fila existente; crea si no existe
    Task UpsertAsync(BrandSnapshot snapshot, CancellationToken ct);

    // Retorna las 4 filas (uno por site)
    Task<IReadOnlyList<BrandSnapshot>> GetAllAsync(CancellationToken ct);
}
```

**Invariante:** Sin `Delete`, sin historial. La tabla `brand_snapshots` siempre tiene <= 4 filas.

---

## §6 Nuevas interfaces de servicio

### `IBrandMonitorService`

```csharp
public interface IBrandMonitorService
{
    Task<IReadOnlyList<BrandSnapshot>> GetCurrentSnapshotsAsync(CancellationToken ct);
}
```

Servicio de aplicación delgado — solo lectura desde repositorio para la página Blazor.

### `INotificationService` (implementación concreta reemplaza stub U3)

```csharp
// Interfaz definida en U3 — U6 provee la implementación real
public interface INotificationService
{
    Task BroadcastAlertAsync(AlertMessage alert, CancellationToken ct);
    Task BroadcastCloseAsync(int incidentId, CancellationToken ct);
}
```

### `AlertBroadcaster` (singleton — no es interfaz, es clase concreta)

```csharp
// Singleton in-process para notificar a páginas Blazor sin doble conexión SignalR
public sealed class AlertBroadcaster
{
    public event Func<AlertMessage, Task>? OnAlert;
    public event Func<int, Task>? OnIncidentClosed;

    public async Task BroadcastAsync(AlertMessage alert)
    {
        if (OnAlert is not null)
            await OnAlert.Invoke(alert);
    }

    public async Task BroadcastCloseAsync(int incidentId)
    {
        if (OnIncidentClosed is not null)
            await OnIncidentClosed.Invoke(incidentId);
    }
}
```

---

## §7 Nuevas interfaces de lectura de logs

```csharp
public interface ITechnicalLogReader
{
    Task<IReadOnlyList<LogLine>> GetRecentAsync(int maxLines, CancellationToken ct);
    string ExportCsv(IReadOnlyList<LogLine> lines);
    string ExportJson(IReadOnlyList<LogLine> lines);
}

public record LogLine(
    DateTime Timestamp,
    string   Level,    // "INF" | "WRN" | "ERR" | "CRT"
    string   Message,
    string?  Exception);
```

---

## §8 Trazabilidad de entidades y contratos

| Componente | Story | RF | UC |
|-----------|-------|----|----|
| `BrandSnapshot` + `IBrandSnapshotRepository` | — | RF-31 | — |
| `IBrandMonitorService` | — | RF-31 | — |
| `RuleCondition.PendingDropThreshold` (def. en U5 v1.1) | — | RF-31, RF-14 | BR-COND-04 |
| `ModuleId.BrandMonitor = 4` (def. en U5 v1.1) | — | RF-31, RF-14 | BR-RULE-09 |
| `AlertBroadcaster` | US-01, US-03 | RF-22, RF-25 | UC2, UC3 |
| `INotificationService` (impl real) | US-03 | RF-25 | UC3 |
| `ITechnicalLogReader` | US-17 | RF-30 | — |
| `AlertsHub : Hub` | US-03 | RF-25 | UC3 |
