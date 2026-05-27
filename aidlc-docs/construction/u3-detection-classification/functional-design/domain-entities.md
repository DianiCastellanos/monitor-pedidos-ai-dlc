# Domain Entities — U3 Detection & Classification

**Unidad:** U3 — Detection & Classification
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-23
**Versión:** 1.1 (2026-05-24 — agrega BrandMonitorChecker como ICheckExecutor en Timer2; no genera incidentes, actualiza brand_snapshots)

---

## §1 Bounded Context — Monitoring

U3 introduce el bounded context **Monitoring**: responsable del ciclo completo de detección automática, clasificación de causas y renderizado de alertas. Es el contexto que orquesta a todos los demás — consume `IIncidentService` (U2) y produce datos para `INotificationService` (U6).

```
MonitorPedidos.Domain/Monitoring/
+-- CheckResult                    (value object — resultado de un checker)
+-- CheckContext                   (value object — contexto para renderizar alerta)
+-- ICheckExecutor                 (contrato de checker)
+-- IOrderSource                   (contrato de fuente de pedidos)
+-- IJobStatusSource               (contrato de fuente de estado de jobs)
+-- OrderSnapshot                  (value object — snapshot de un pedido)
+-- JobStatusSnapshot              (value object — snapshot de un job)

MonitorPedidos.Domain/Shared/      (tipos compartidos — definidos aquí, usados desde U2)
+-- CauseCategory                  (enum — cascade de clasificación)
+-- CheckStatus                    (enum — OK / WARN / CRITICAL)

MonitorPedidos.Web/Features/Monitoring/
+-- MonitoringSchedulerService     (BackgroundService — M1)
+-- DbOrderChecker                 (ICheckExecutor — M2)
+-- DbHealthChecker                (ICheckExecutor — M4)
+-- JobsChecker                    (ICheckExecutor — M11)
+-- CauseClassifier                (servicio — M7)
+-- AlertTemplateRenderer          (servicio — M10)
+-- SimulatedOrderRepository       (IOrderSource — Sprint 2)
+-- SimulatedJobStatusRepository   (IJobStatusSource — Sprint 2)

MonitorPedidos.Web/Services/
+-- MonitoringService              (application service — orquestador principal)
```

---

## §2 Enums de dominio compartido

### CheckStatus

```csharp
// MonitorPedidos.Domain/Shared/CheckStatus.cs
namespace MonitorPedidos.Domain.Shared;

public enum CheckStatus
{
    Ok,
    Warn,
    Critical
}
```

### CauseCategory

```csharp
// MonitorPedidos.Domain/Shared/CauseCategory.cs
namespace MonitorPedidos.Domain.Shared;

public enum CauseCategory
{
    Bd,           // Problema en base de datos de pedidos
    Job,          // Job de integración detenido o fallido
    Api,          // API externa no responde (U4)
    Token,        // Token de autenticación expirado o inválido (U4)
    DataQuality,  // Datos corruptos o inconsistentes
    NoDeterminada // Causa no identificada — candidato a regla nueva
}
```

> **Nota:** `CauseCategory` y `Severity` (definida en U1/Domain/Shared) son los tipos de clasificación usados desde U2 en la entidad `Incident`. Se consolidan aquí como definición formal.

---

## §3 Value Object: CheckResult

Resultado que retorna todo `ICheckExecutor` tras ejecutarse.

```csharp
// MonitorPedidos.Domain/Monitoring/CheckResult.cs
namespace MonitorPedidos.Domain.Monitoring;

public sealed record CheckResult(
    CheckStatus Status,
    string Details,
    DateTimeOffset CheckedAt)
{
    public static CheckResult Ok(string details = "")
        => new(CheckStatus.Ok, details, DateTimeOffset.UtcNow);

    public static CheckResult Warn(string details)
        => new(CheckStatus.Warn, details, DateTimeOffset.UtcNow);

    public static CheckResult Critical(string details)
        => new(CheckStatus.Critical, details, DateTimeOffset.UtcNow);

    public bool RequiresIncident => Status is CheckStatus.Warn or CheckStatus.Critical;
}
```

---

## §4 Value Object: CheckContext

Contexto completo que se pasa a `AlertTemplateRenderer` para renderizar el `AlertMessage`.

```csharp
// MonitorPedidos.Domain/Monitoring/CheckContext.cs
namespace MonitorPedidos.Domain.Monitoring;

public sealed record CheckContext(
    ModuleId Module,
    CheckStatus Status,
    CauseCategory Cause,
    string CheckDetails,
    DateTimeOffset DetectedAt);
```

---

## §5 Value Objects: snapshots de fuentes de datos

### OrderSnapshot

```csharp
// MonitorPedidos.Domain/Monitoring/OrderSnapshot.cs
namespace MonitorPedidos.Domain.Monitoring;

public sealed record OrderSnapshot(
    string OrderId,
    DateTimeOffset CreatedAt,
    bool IsCancelled);
```

### JobStatusSnapshot

```csharp
// MonitorPedidos.Domain/Monitoring/JobStatusSnapshot.cs
namespace MonitorPedidos.Domain.Monitoring;

public sealed record JobStatusSnapshot(
    string JobId,
    string JobName,
    bool IsRunning,
    DateTimeOffset? LastExecutedAt,
    bool LastExecutionSucceeded);
```

---

## §6 Interfaz: ICheckExecutor

Contrato que implementan todos los checkers (M2, M4, M11 y futuros).

```csharp
// MonitorPedidos.Domain/Monitoring/ICheckExecutor.cs
namespace MonitorPedidos.Domain.Monitoring;

public interface ICheckExecutor
{
    ModuleId Module { get; }
    Task<CheckResult> ExecuteAsync(CancellationToken ct = default);
}
```

**Invariante:** cada implementación declara su `Module` (el `ModuleId` al que pertenece) para que el orquestador sepa a qué incidente abrir si el resultado es WARN/CRITICAL.

---

## §7 Interfaz: IOrderSource

Abstracción sobre la fuente de pedidos. En Sprint 2: `SimulatedOrderRepository`. Post-MVP: repositorio de la tabla real de pedidos.

```csharp
// MonitorPedidos.Domain/Monitoring/IOrderSource.cs
namespace MonitorPedidos.Domain.Monitoring;

public interface IOrderSource
{
    Task<IReadOnlyList<OrderSnapshot>> GetOrdersInWindowAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default);
}
```

---

## §8 Interfaz: IJobStatusSource

Abstracción sobre la fuente de estado de jobs. En Sprint 2: `SimulatedJobStatusRepository`. En U4: implementación con estado real.

```csharp
// MonitorPedidos.Domain/Monitoring/IJobStatusSource.cs
namespace MonitorPedidos.Domain.Monitoring;

public interface IJobStatusSource
{
    Task<IReadOnlyList<JobStatusSnapshot>> GetCurrentStatusAsync(CancellationToken ct = default);
}
```

---

## §9 Checkers — implementaciones de ICheckExecutor

### DbOrderChecker (M2)

```csharp
// MonitorPedidos.Web/Features/Monitoring/DbOrderChecker.cs
public sealed class DbOrderChecker : ICheckExecutor
{
    public ModuleId Module => ModuleId.DbOrders;

    private readonly IOrderSource _orderSource;
    private readonly IConfiguration _config;

    // Ventana de detección: RF-01 sugiere 30 min
    private TimeSpan DetectionWindow =>
        TimeSpan.FromMinutes(_config.GetValue("Monitoring:OrderDetectionWindowMinutes", 30));

    public async Task<CheckResult> ExecuteAsync(CancellationToken ct)
    {
        var to   = DateTimeOffset.UtcNow;
        var from = to - DetectionWindow;
        var orders = await _orderSource.GetOrdersInWindowAsync(from, to, ct);

        var activeOrders = orders.Where(o => !o.IsCancelled).ToList();

        return activeOrders.Count == 0
            ? CheckResult.Critical($"Sin pedidos activos en últimos {DetectionWindow.TotalMinutes} min.")
            : CheckResult.Ok($"{activeOrders.Count} pedido(s) activos en ventana.");
    }
}
```

### DbHealthChecker (M4)

```csharp
// MonitorPedidos.Web/Features/Monitoring/DbHealthChecker.cs
public sealed class DbHealthChecker : ICheckExecutor
{
    public ModuleId Module => ModuleId.DbHealth;

    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    private int WarnLatencyMs  => _config.GetValue("Monitoring:DbWarnLatencyMs",  1000);
    private int CritTimeoutMs  => _config.GetValue("Monitoring:DbCritTimeoutMs",  5000);

    public async Task<CheckResult> ExecuteAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(CritTimeoutMs);
            await _db.Database.ExecuteSqlRawAsync("SELECT 1", cts.Token);
            sw.Stop();

            return sw.ElapsedMilliseconds > WarnLatencyMs
                ? CheckResult.Warn($"BD responde con latencia alta: {sw.ElapsedMilliseconds} ms.")
                : CheckResult.Ok($"BD responde en {sw.ElapsedMilliseconds} ms.");
        }
        catch (Exception ex)
        {
            return CheckResult.Critical($"BD no responde: {ex.Message}");
        }
    }
}
```

### JobsChecker (M11)

```csharp
// MonitorPedidos.Web/Features/Monitoring/JobsChecker.cs
public sealed class JobsChecker : ICheckExecutor
{
    public ModuleId Module => ModuleId.Jobs;

    private readonly IJobStatusSource _jobSource;

    public async Task<CheckResult> ExecuteAsync(CancellationToken ct)
    {
        var jobs = await _jobSource.GetCurrentStatusAsync(ct);
        var failed = jobs.Where(j => !j.IsRunning || !j.LastExecutionSucceeded).ToList();

        return failed.Count == 0
            ? CheckResult.Ok($"{jobs.Count} job(s) activos y saludables.")
            : CheckResult.Critical(
                $"{failed.Count} job(s) fallido(s): {string.Join(", ", failed.Select(j => j.JobName))}");
    }
}
```

### BrandMonitorChecker (M-BrandMonitor — RF-31)

```csharp
// MonitorPedidos.Web/Features/Monitoring/BrandMonitorChecker.cs
// Implementación completa definida en U6. Resumen del contrato aquí para trazabilidad U3.
public sealed class BrandMonitorChecker : ICheckExecutor
{
    public ModuleId Module => ModuleId.BrandMonitor;

    // Registrado como Singleton + IServiceScopeFactory (ADR-U5-01)
    // Ejecuta en Timer2 (10 min) junto con SalesforceApiChecker y MultivendeApiChecker
    // Para cada site en [Patprimo, SevenSeven, Atmos, Ostu]:
    //   1. Obtiene pedidos pendientes vía ISalesforceClient.GetPendingOrdersAsync(site)
    //   2. Lee PendingDropThreshold desde IRuleRepository.GetActiveByModuleAsync(BrandMonitor)
    //   3. Llama IBrandSnapshotRepository.UpsertAsync(BrandSnapshot.Upsert(...))
    //
    // COMPORTAMIENTO DIFERENCIADO: BrandMonitorChecker NO genera incidentes a través de
    // MonitoringService. Actualiza brand_snapshots directamente. El semáforo en
    // BrandMonitorPage refleja el estado visual sin disparar alertas push.
    // Error en ISalesforceClient → BrandSnapshot con current=-1, Status=Red (falla silenciosa).

    public async Task<CheckResult> ExecuteAsync(CancellationToken ct)
    {
        // Implementación completa en U6 (BrandMonitorChecker.cs)
        // Retorna CheckResult.Ok siempre — no propaga errores al pipeline de incidentes
        await Task.CompletedTask;
        return CheckResult.Ok("Brand monitor actualizado.");
    }
}
```

> **Invariante de integración con U3:** `BrandMonitorChecker` implementa `ICheckExecutor` y es resuelto por `MonitoringSchedulerService` vía `IEnumerable<ICheckExecutor>` (ADR-U3-02). Sin embargo, **no pasa por `CauseClassifier` ni crea `Incident`**. Su resultado siempre es `CheckResult.Ok` — la lógica de semáforo vive en `BrandSnapshot.DetermineStatus()` (U6).

---

## §10 CauseClassifier (M7)

Determina la `CauseCategory` a partir del checker que generó el resultado (P2 = A).

```csharp
// MonitorPedidos.Web/Features/Monitoring/CauseClassifier.cs
public static class CauseClassifier
{
    private static readonly IReadOnlyDictionary<Type, CauseCategory> _map =
        new Dictionary<Type, CauseCategory>
        {
            [typeof(DbOrderChecker)]  = CauseCategory.Bd,
            [typeof(DbHealthChecker)] = CauseCategory.Bd,
            [typeof(JobsChecker)]     = CauseCategory.Job,
            // U4 agregará: ApiChecker → CauseCategory.Api, TokenChecker → CauseCategory.Token
        };

    public static CauseCategory Classify(ICheckExecutor checker)
        => _map.TryGetValue(checker.GetType(), out var cause)
            ? cause
            : CauseCategory.NoDeterminada;
}
```

---

## §11 AlertTemplateRenderer (M10)

Renderiza `AlertMessage` (6 campos en español) desde un diccionario estático C# (P3 = A).

```csharp
// MonitorPedidos.Web/Features/Monitoring/AlertTemplateRenderer.cs
public static class AlertTemplateRenderer
{
    private static readonly IReadOnlyDictionary<(CauseCategory, Severity), Func<CheckContext, AlertMessage>>
        _templates = new Dictionary<(CauseCategory, Severity), Func<CheckContext, AlertMessage>>
    {
        [(CauseCategory.Bd, Severity.Critical)] = ctx => new AlertMessage(
            QuePaso:         $"No se detectaron pedidos activos en el módulo {ctx.Module}.",
            Cuando:          ctx.DetectedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
            Donde:           $"Módulo {ctx.Module} — Base de Datos",
            SeveridadTexto:  "CRÍTICO",
            CausaProbable:   "Posible falla en la base de datos o ausencia real de transacciones.",
            AccionSugerida:  "Verificar conectividad con la BD. Revisar logs del motor SQL."),

        [(CauseCategory.Bd, Severity.Warn)] = ctx => new AlertMessage(
            QuePaso:         $"Latencia alta en la base de datos del módulo {ctx.Module}.",
            Cuando:          ctx.DetectedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
            Donde:           $"Módulo {ctx.Module} — Base de Datos",
            SeveridadTexto:  "ADVERTENCIA",
            CausaProbable:   "Degradación de rendimiento de la BD. Posible sobrecarga.",
            AccionSugerida:  "Monitorear latencia. Revisar queries activas en SQL Server."),

        [(CauseCategory.Job, Severity.Critical)] = ctx => new AlertMessage(
            QuePaso:         $"Job de integración detenido en el módulo {ctx.Module}.",
            Cuando:          ctx.DetectedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
            Donde:           $"Módulo {ctx.Module} — Servicio de Jobs",
            SeveridadTexto:  "CRÍTICO",
            CausaProbable:   "Job de descarga de pedidos detenido o con error en última ejecución.",
            AccionSugerida:  "Revisar consola de administración de jobs. Reiniciar si es necesario."),

        [(CauseCategory.NoDeterminada, Severity.Critical)] = ctx => new AlertMessage(
            QuePaso:         $"Anomalía detectada en módulo {ctx.Module} — causa no identificada.",
            Cuando:          ctx.DetectedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
            Donde:           $"Módulo {ctx.Module}",
            SeveridadTexto:  "CRÍTICO",
            CausaProbable:   "No fue posible determinar la causa automáticamente. Marcado como candidato a regla nueva.",
            AccionSugerida:  "Revisar incidente en el historial y crear regla personalizada si el patrón se repite."),
    };

    private static AlertMessage Fallback(CheckContext ctx) => new(
        QuePaso:        $"Anomalía en módulo {ctx.Module}: {ctx.CheckDetails}",
        Cuando:         ctx.DetectedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
        Donde:          $"Módulo {ctx.Module}",
        SeveridadTexto: ctx.Status == CheckStatus.Critical ? "CRÍTICO" : "ADVERTENCIA",
        CausaProbable:  "Sin plantilla específica para esta combinación.",
        AccionSugerida: "Revisar logs del sistema para más detalles.");

    public static AlertMessage Render(CheckContext ctx)
    {
        var severity = ctx.Status == CheckStatus.Critical ? Severity.Critical : Severity.Warn;
        return _templates.TryGetValue((ctx.Cause, severity), out var template)
            ? template(ctx)
            : Fallback(ctx);
    }
}
```

---

## §12 Trazabilidad de entidades

| Componente | Story | RF | Decisión |
|-----------|-------|-----|---------|
| `ICheckExecutor` + `CheckResult` | US-07, US-08, US-09 | RF-01, RF-04, RF-05 | P1=A |
| `CauseClassifier` | US-07, US-18 | RF-08, RF-10 | P2=A |
| `AlertTemplateRenderer` | US-06 | RF-11, RF-12, RF-13 | P3=A |
| `IOrderSource` + `SimulatedOrderRepository` | US-07 | RF-01, RF-02 | P4=A |
| `IJobStatusSource` + `SimulatedJobStatusRepository` | US-09 | RF-05 | P4=A (análogo) |
| `CheckStatus`, `CauseCategory` | US-06..09, US-18 | RF-08, RF-10, RF-12 | — |
| `BrandMonitorChecker` (ICheckExecutor, Timer2) | — | RF-31, RF-03 | ADR-U3-02 (IEnumerable DI), ADR-U5-01 (IServiceScopeFactory) |
