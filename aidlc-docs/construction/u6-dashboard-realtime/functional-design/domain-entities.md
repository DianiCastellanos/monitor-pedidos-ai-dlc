# Domain Entities — U6 Dashboard & Real-Time

**Unidad:** U6 — Dashboard & Real-Time
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-24
**Versión:** 1.2 (2026-05-31 — §3 actualizado: BrandSnapshot es append-only con histórico real IT3; `SnapshotStatus.NoData` IT3; §5 IBrandSnapshotRepository actualizado IT3; §6 IBrandMonitorService + GetLiveCountsAsync IT10)

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

## §3 Entidad actualizada: `BrandSnapshot` — versión IT3/IT7

```csharp
// Entidad liviana — no aggregate root
// Representa un snapshot puntual de pedidos pendientes de un site
// Tabla: append-only — nunca UPDATE, nunca DELETE (excepto retención futura)
public sealed class BrandSnapshot
{
    public int     Id                   { get; private set; }
    public string  Site                 { get; private set; }  // "PatPrimo" | "SevenSeven" | "Atmos" | "Ostu"
    public int     PendingCountCurrent  { get; private set; }  // pedidos pendientes en este momento
    public int?    PendingCountPrevious { get; private set; }  // snapshot anterior para comparar (nullable — NoData si null)
    public DateTime CheckedAt           { get; private set; }
    public SnapshotStatus Status        { get; private set; }  // Green | Yellow | Red | NoData

    // Sites activos Colombia — orden canónico
    public static readonly string[] Sites = ["PatPrimo", "SevenSeven", "Ostu", "Atmos"];

    private BrandSnapshot() { }

    // Factory — IT3: usa Create (insert), no Upsert (overwrite)
    public static BrandSnapshot Create(
        string site,
        int currentPending,
        int? previousPending,
        SnapshotStatus status)
    {
        return new BrandSnapshot
        {
            Site                 = site,
            PendingCountCurrent  = currentPending,
            PendingCountPrevious = previousPending,
            CheckedAt            = DateTime.UtcNow,
            Status               = status
        };
    }
}

// IT3: NoData agregado — primer snapshot sin histórico para comparar
public enum SnapshotStatus { Green, Yellow, Red, NoData }
```

**Cambios vs diseño original:**
- `PendingCountPrevious` ahora es `int?` (nullable) — `null` cuando no hay histórico (`NoData`)
- `SnapshotStatus.NoData` — primer snapshot de un site, sin comparación posible
- Factory `Create` en lugar de `Upsert` — la tabla es append-only con historial real
- `Sites` es un array estático con los 4 sites válidos de Colombia (IT7: "PatPrimo" con P mayúscula)
- El estado lo determina `BrandMonitorChecker.DetermineStatus(current, previous)` — no la entidad

**Reglas de determinación de estado (en BrandMonitorChecker):**
```csharp
private static SnapshotStatus DetermineStatus(int current, int previous)
{
    if (current < previous) return SnapshotStatus.Green;   // bajaron
    if (current == previous) return SnapshotStatus.Yellow; // igual
    return SnapshotStatus.Red;                             // subieron
}
```

**Invariantes:**
- `Site` es uno de los 4 valores de `BrandSnapshot.Sites`
- `brand_snapshots` es append-only — `InsertAsync` nunca `UpsertAsync`
- `PendingCountCurrent` siempre >= 0
- `PendingCountPrevious` es null solo en el primer snapshot de un site (hasta que hay un snapshot anterior en la ventana de comparación)

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

## §5 Interfaces de repositorio — actualizadas IT3

### `IBrandSnapshotRepository`

```csharp
public interface IBrandSnapshotRepository
{
    // INSERT siempre — append-only (IT3: reemplaza UpsertAsync)
    Task InsertAsync(BrandSnapshot snapshot, CancellationToken ct);

    // Último snapshot de cada site (agrupación en memoria — workaround EF Core GroupBy)
    Task<IReadOnlyList<BrandSnapshot>> GetLatestPerSiteAsync(CancellationToken ct);

    // Snapshot más reciente de un site en o antes de un timestamp (para comparación de tendencia)
    Task<BrandSnapshot?> GetLatestAsync(string site, CancellationToken ct);

    // Snapshot más cercano a un momento dado hacia atrás (para comparación con ventana configurable)
    Task<BrandSnapshot?> GetSnapshotBeforeAsync(string site, DateTime before, CancellationToken ct);
}
```

**Invariante:** Sin `Delete`, sin `Update`. `brand_snapshots` crece con el tiempo. Retención futura (Paso 7 diferido): `DeleteOlderThanAsync(48h)`.

---

## §6 Interfaces de servicio — actualizadas IT3/IT10

### `IBrandMonitorService`

```csharp
public interface IBrandMonitorService
{
    // Lectura desde AppDb — snapshot más reciente por site
    Task<IReadOnlyList<BrandSnapshot>> GetCurrentSnapshotsAsync(CancellationToken ct);

    // Forzar refresh desde Salesforce y persistir (usado por botón manual cuando BD disponible)
    Task SimulateAndRefreshAsync(CancellationToken ct);

    // IT10 — Live fallback: llama Salesforce directamente sin BD, retorna conteos por site
    // Retorna null si Salesforce no responde o falla
    Task<IReadOnlyDictionary<string, int>?> GetLiveCountsAsync(CancellationToken ct);
}
```

**Regla de uso de `GetLiveCountsAsync`:**
- Se usa como fallback cuando `GetCurrentSnapshotsAsync` falla (BD no disponible)
- En ciclos automáticos de UI: se consulta `LastCheckStore[BrandMonitor].Details` primero (sin costo)
- `GetLiveCountsAsync` solo se invoca si `LastCheckStore` está vacío Y el usuario hizo clic en "↻ Actualizar"
- Nunca se invoca en ciclos automáticos de timer — solo en acción manual

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
