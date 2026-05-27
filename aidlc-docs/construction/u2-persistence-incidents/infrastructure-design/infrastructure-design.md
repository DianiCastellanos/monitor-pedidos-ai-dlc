# Infrastructure Design — U2 Persistence & Incidents

**Unidad:** U2 — Persistence & Incidents
**Stage:** Construction → Infrastructure Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Resumen de decisiones

| Decisión | Opción elegida |
|----------|---------------|
| Migration `AddIncidentSchema` | Manual con `dotnet ef database update` (consistente con U1) |
| Días de retención | Configurable en `appsettings.json` → `"Incidents": { "RetentionDays": 90 }` |
| Invocación de purga | `IncidentMaintenanceService : BackgroundService` — corre cada 24h dentro del proceso |

---

## §2 Componentes de U1 reutilizados sin cambios

U2 corre dentro del mismo proceso que U1. No agrega servicios externos, ni cambia el deployment.

| Componente U1 | Uso en U2 |
|--------------|-----------|
| `AppDbContext` | Agrega `DbSet<Incident>` y aplica `IncidentConfiguration` |
| `LocalDB (MonitorPedidosDb)` | Tabla `incidents` creada por migration de U2 |
| Kestrel (`:7001` / `:5001`) | Sin cambios — U2 expone páginas Blazor bajo las rutas `/incidents/*` |
| Serilog (File Sink) | `IncidentService` e `IncidentMaintenanceService` usan `ILogger<T>` del mismo sink |
| `FallbackPolicy` (U1 Authorization) | Protege automáticamente `/incidents`, `/incidents/weekly`, `/incidents/{id:guid}` |
| `GlobalExceptionHandler` (U1) | Captura excepciones no manejadas de U2 sin cambios |

---

## §3 Base de datos — Migration AddIncidentSchema

### 3.1 Qué crea la migration

| Objeto | Detalle |
|--------|---------|
| Tabla `incidents` | 17 columnas (ver `domain-entities.md §5`) |
| `IX_incidents_OpenedAt` | Índice simple — acelera filtros por fecha |
| `IX_incidents_Module_ClosedAt` | Índice compuesto — acelera GetOpenByModule y GetRecentClosed |
| `IX_incidents_Module_Open` | **Índice único filtrado** (`WHERE ClosedAt IS NULL`) — garantiza BR-INC-01 |

### 3.2 Comandos

```bash
# Crear migration
dotnet ef migrations add AddIncidentSchema --project src/MonitorPedidos.Web

# Aplicar a la BD de desarrollo
dotnet ef database update --project src/MonitorPedidos.Web
```

**Cuándo ejecutar:** antes del primer arranque después de hacer pull de los cambios de U2. El índice filtrado es crítico — sin él, `IX_incidents_Module_Open` no existe y ADR-U2-01 no funciona.

### 3.3 BD de test (TestWebAppFactory)

La `TestWebAppFactory` de U2 usa `MonitorPedidosTest` con `MigrateAsync()` al arrancar la suite. Esto garantiza que el índice único filtrado exista en los tests (ver ADR-U2-04).

```csharp
// En TestWebAppFactory — OnStarted o fixture de test
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await db.Database.MigrateAsync();
```

---

## §4 BackgroundService — IncidentMaintenanceService

### 4.1 Responsabilidad

Invoca `IIncidentRepository.PurgeExpiredAsync(retentionDays, ct)` una vez cada 24 horas para eliminar incidentes expirados (BR-PURGE-01).

### 4.2 Clase

```csharp
// MonitorPedidos.Web/BackgroundServices/IncidentMaintenanceService.cs
namespace MonitorPedidos.Web.BackgroundServices;

public sealed class IncidentMaintenanceService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<IncidentMaintenanceService> _logger;

    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    public IncidentMaintenanceService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<IncidentMaintenanceService> logger)
    {
        _scopeFactory = scopeFactory;
        _config      = config;
        _logger      = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunPurgeAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task RunPurgeAsync(CancellationToken ct)
    {
        var retentionDays = _config.GetValue<int>("Incidents:RetentionDays", 90);

        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IIncidentRepository>();
            var deleted = await repo.PurgeExpiredAsync(retentionDays, ct);
            _logger.LogInformation(
                "Incident purge completed. RetentionDays={Days} Deleted={Count}",
                retentionDays, deleted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Incident purge failed. RetentionDays={Days}", retentionDays);
        }
    }
}
```

**Por qué `IServiceScopeFactory`:** `BackgroundService` es singleton, pero `IIncidentRepository` (y `AppDbContext`) son scoped. Crear un scope por ejecución garantiza que el DbContext se cree y destruya correctamente cada 24h.

### 4.3 Registro en Program.cs

```csharp
// Program.cs — junto con los demás registros de U2
builder.Services.AddHostedService<IncidentMaintenanceService>();
```

---

## §5 Configuración — appsettings.json

### 5.1 Sección nueva en appsettings.json

```json
{
  "Incidents": {
    "RetentionDays": 90
  }
}
```

### 5.2 Por ambiente

| Ambiente | Valor recomendado | Razón |
|---------|------------------|-------|
| `appsettings.json` (base) | `90` | Valor de producción (BR-PURGE-01) |
| `appsettings.Development.json` | `7` | Permite purgar datos de prueba rápidamente en desarrollo |
| `appsettings.Testing.json` | `0` o `1` | Los tests de T-U2-05 crean incidentes con `OpenedAt` muy antiguo y necesitan que se purguen |

### 5.3 Lectura en el BackgroundService

`_config.GetValue<int>("Incidents:RetentionDays", 90)` — si la clave no existe, usa el default `90` como fallback.

---

## §6 Registro DI completo de U2 en Program.cs

```csharp
// ── U2: Persistence & Incidents ──────────────────────────────────────────
builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddHostedService<IncidentMaintenanceService>();
// ─────────────────────────────────────────────────────────────────────────
```

**Nota:** `INotificationService` (dependencia de `IncidentService`) es un stub en U2. Se registra en U1 como parte del cross-cutting y se reemplaza en U6 con la implementación real.

---

## §7 Deployment — sin cambios vs U1

U2 no modifica el proceso de despliegue. El diagrama Mermaid de `deployment-architecture.md` de U1 sigue vigente.

Adición implícita al diagrama: `IncidentMaintenanceService` corre como hilo de fondo dentro del mismo proceso Kestrel. No requiere puerto, proceso externo, ni configuración de red adicional.

---

## §8 Checklist de puesta en marcha de U2

```text
[ ] dotnet ef migrations add AddIncidentSchema --project src/MonitorPedidos.Web
[ ] dotnet ef database update --project src/MonitorPedidos.Web
[ ] Verificar que appsettings.json tiene sección "Incidents": { "RetentionDays": 90 }
[ ] Verificar que appsettings.Development.json tiene "RetentionDays": 7
[ ] dotnet run → confirmar que IncidentMaintenanceService arranca (log en Serilog)
[ ] Navegar a /incidents → página carga con tabla vacía (sin incidentes aún)
```

---

## §9 Trazabilidad

| Componente | ADR | NFR | Story | BR |
|-----------|-----|-----|-------|----|
| Migration `AddIncidentSchema` | ADR-U2-04 | NFR-U2-01 | US-05 | BR-INC-01 |
| `IncidentMaintenanceService` | ADR-U2-03 | — | US-14 | BR-PURGE-01, BR-PURGE-02 |
| `Incidents:RetentionDays` config | — | — | US-14 | BR-PURGE-01 |
| TestWebAppFactory + MigrateAsync | ADR-U2-04 | NFR-U2-04 | US-13, US-14 | BR-INC-01, BR-CLOSE-01 |
| Registro DI U2 | — | — | US-05, US-11..14 | — |
