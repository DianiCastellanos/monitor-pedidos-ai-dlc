# Domain Entities — U7 Simulation & Red-Teaming

**Unidad:** U7 — Simulation & Red-Teaming
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-24
**Versión:** 1.0

---

## §1 Decisiones de diseño aplicadas

| ID | Pregunta | Decisión |
|----|----------|---------|
| P1 | Estructura de `simulated_orders` | A — 6 columnas: id, source, status, created_at, is_failure, site |
| P2 | Estructura de `simulated_job_statuses` | A — 5 columnas + seed 2 filas (SalesforceDownload, MultivendeDownload) con status="Completed" |
| P3 | Lógica de fallo en `OrdersSimulatorService` | A — `FailureProbability` (0..1) + `NoOrdersMode` (bool) en `SimulationOptions`, configurables en appsettings |
| P4 | Reporte red-teaming | A — Plantilla Markdown en aidlc-docs con tabla de 6 escenarios, completada manualmente durante demo |

---

## §2 Entidad: `SimulatedOrder`

```csharp
// Tabla simulated_orders — representa un pedido en el MVP
// DbOrderChecker (U3) lee de esta tabla en lugar de la BD real
public sealed class SimulatedOrder
{
    public int      Id        { get; private set; }
    public string   Source    { get; private set; }  // "Salesforce" | "Multivende"
    public string   Status    { get; private set; }  // "Pending" | "Processing" | "Cancelled" | "Error"
    public DateTime CreatedAt { get; private set; }
    public bool     IsFailure { get; private set; }  // true → pedido con estado incorrecto (RT5/RT7)
    public string   Site      { get; private set; }  // "Patprimo" | "SevenSeven" | "Atmos" | "Ostu"

    private SimulatedOrder() { }

    // Pedido normal — status normal según fuente
    public static SimulatedOrder CreateNormal(string source, string site) =>
        new()
        {
            Source    = source,
            Status    = "Pending",
            CreatedAt = DateTime.UtcNow,
            IsFailure = false,
            Site      = site
        };

    // Pedido con fallo — para reproducir RT5 (estado incorrecto) o RT7 (cancelado ignorado)
    public static SimulatedOrder CreateFailure(string source, string site, string failStatus) =>
        new()
        {
            Source    = source,
            Status    = failStatus,     // "Error" para RT5 | "Cancelled" para RT7
            CreatedAt = DateTime.UtcNow,
            IsFailure = true,
            Site      = site
        };
}
```

**Rol en el sistema:** `DbOrderChecker` consulta `simulated_orders WHERE created_at > NOW()-WindowHours` para contar pedidos recientes. En MVP esta tabla ES la fuente de datos — no existe tabla de pedidos reales.

---

## §3 Entidad: `SimulatedJobStatus`

```csharp
// Tabla simulated_job_statuses — representa el estado de los jobs de descarga
// JobsChecker (U3) lee de esta tabla en lugar de la BD real
public sealed class SimulatedJobStatus
{
    public int      Id           { get; private set; }
    public string   JobName      { get; private set; }   // "SalesforceDownload" | "MultivendeDownload"
    public DateTime LastRunAt    { get; private set; }
    public string   Status       { get; private set; }   // "Running" | "Completed" | "Failed" | "NotRun"
    public string?  ErrorMessage { get; private set; }   // null cuando Completed

    private SimulatedJobStatus() { }

    // Actualiza el estado del job (para scripts de red-teaming)
    public SimulatedJobStatus MarkFailed(string errorMessage) =>
        new()
        {
            Id           = Id,
            JobName      = JobName,
            LastRunAt    = DateTime.UtcNow,
            Status       = "Failed",
            ErrorMessage = errorMessage
        };

    public SimulatedJobStatus MarkCompleted() =>
        new()
        {
            Id           = Id,
            JobName      = JobName,
            LastRunAt    = DateTime.UtcNow,
            Status       = "Completed",
            ErrorMessage = null
        };
}
```

**Seed data:** 2 filas en la migración con `Status="Completed"` — baseline normal. Para RT1 se actualiza `Status="Failed"` vía script SQL o método de reset.

---

## §4 Clase de configuración: `SimulationOptions`

```csharp
// Configuración del simulador — no es una entidad EF
// Valores leídos de appsettings.json / variables de entorno
public sealed class SimulationOptions
{
    public const string Section = "Simulation";

    public int    InsertIntervalMinutes { get; init; } = 5;      // frecuencia de inserción
    public double FailureProbability    { get; init; } = 0.0;    // 0.0 = todos normales; 1.0 = todos Error
    public bool   NoOrdersMode          { get; init; } = false;  // true = no inserta nada (RT1)
    public bool   Enabled               { get; init; } = true;   // false = apaga el simulador
}
```

**Escenarios cubiertos por configuración:**

| Escenario | `NoOrdersMode` | `FailureProbability` | `Enabled` |
|-----------|---------------|---------------------|-----------|
| Baseline normal | false | 0.0 | true |
| RT1 (sin pedidos) | **true** | — | true |
| RT5 (estado incorrecto) | false | **1.0** | true |
| RT7 (cancelado ignorado) | false | 0.0 | true (manual) |
| Simulador apagado | — | — | **false** |

---

## §5 Interfaz de repositorio: `ISimulatedOrderRepository`

```csharp
public interface ISimulatedOrderRepository
{
    Task InsertAsync(SimulatedOrder order, CancellationToken ct);
    Task<int> CountRecentAsync(string source, int windowHours, CancellationToken ct);
    Task DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct);  // limpieza periódica
}
```

---

## §6 Interfaz de repositorio: `ISimulatedJobStatusRepository`

```csharp
public interface ISimulatedJobStatusRepository
{
    Task<SimulatedJobStatus?> GetByJobNameAsync(string jobName, CancellationToken ct);
    Task UpdateAsync(SimulatedJobStatus status, CancellationToken ct);
    Task<IReadOnlyList<SimulatedJobStatus>> GetAllAsync(CancellationToken ct);
}
```

---

## §7 Trazabilidad de entidades

| Componente | RT | RF | Story |
|-----------|-----|----|----|
| `SimulatedOrder` | RT1, RT5, RT7 | C-06 | soporta U2/U3 |
| `SimulatedJobStatus` | RT1 (job SF apagado) | C-06 | soporta U3 |
| `SimulationOptions` | RT1, RT2, RT3, RT5, RT7 | C-06 | soporta todos |
| `ISimulatedOrderRepository` | RT1, RT5, RT7 | C-06 | — |
| `ISimulatedJobStatusRepository` | RT1 | C-06 | — |
