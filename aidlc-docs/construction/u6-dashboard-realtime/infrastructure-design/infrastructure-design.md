# Infrastructure Design — U6 Dashboard & Real-Time

**Unidad:** U6 — Dashboard & Real-Time
**Stage:** Construction → Infrastructure Design
**Fecha:** 2026-05-24
**Versión:** 1.0

---

## §1 Decisiones aplicadas

| ID | Pregunta | Decisión |
|----|----------|---------|
| P1 | Migración EF Core para `brand_snapshots` + seed `BrandMonitor` | A — Una sola migración `AddBrandSnapshots`: tabla + seed regla `PendingDropThreshold=5` |
| P2 | Ciclo de vida en DI de `AlertBroadcaster`, `NotificationService`, `BrandMonitorService`, `TechnicalLogReader` | A — Singleton para `AlertBroadcaster` y `NotificationService`; Scoped para el resto |
| P3 | Registro de `BrandMonitorChecker` + filtro en `MonitoringSchedulerService` | A — `AddSingleton<ICheckExecutor, BrandMonitorChecker>()` + extensión de filtro en Timer2 |
| P4 | `AddSignalR` y mapeo de `AlertsHub` | A — `AddSignalR()` + `EnableDetailedErrors` en Development + `MapHub<AlertsHub>("/hubs/alerts")` |

---

## §2 Migración EF Core — `AddBrandSnapshots`

### Comando de migración

```bash
dotnet ef migrations add AddBrandSnapshots --project src/MonitorPedidos.Web
dotnet ef database update --project src/MonitorPedidos.Web
```

### Secuencia de migraciones en el proyecto

```
Migrations/
    ├── 20260520_InitialCreate.cs                    ← U2: incidents, users, jobs
    ├── 20260523_AddRetryMetadataToIncidents.cs      ← U4: columna retry_metadata
    ├── 20260524_AddRulesTables.cs                   ← U5: rules, rule_history + seed 2 reglas
    └── 20260524_AddBrandSnapshots.cs                ← U6: brand_snapshots + seed regla BrandMonitor
```

### Configuración de entidad en `OnModelCreating`

```csharp
// AppDbContext.OnModelCreating — extensión U6
modelBuilder.Entity<BrandSnapshot>(b =>
{
    b.ToTable("brand_snapshots");
    b.HasKey(s => s.Id);
    b.Property(s => s.Site)
     .HasColumnName("site")
     .HasMaxLength(50)
     .IsRequired();
    b.Property(s => s.PendingCountCurrent)
     .HasColumnName("pending_count_current")
     .IsRequired();
    b.Property(s => s.PendingCountPrevious)
     .HasColumnName("pending_count_previous")
     .IsRequired();
    b.Property(s => s.CheckedAt)
     .HasColumnName("checked_at")
     .IsRequired();
    b.Property(s => s.Status)
     .HasColumnName("status")
     .HasConversion<string>()  // almacena "Green" | "Yellow" | "Red"
     .IsRequired();

    // Índice único por site — garantiza máximo 4 filas
    b.HasIndex(s => s.Site).IsUnique();
});
```

### Seed data en `Up()` de la migración

```csharp
// Regla seed para módulo BrandMonitor (PendingDropThreshold = 5)
migrationBuilder.InsertData(
    table: "rules",
    columns: new[] { "id", "name", "module_id", "condition_json", "is_active", "created_at", "created_by" },
    values: new object[]
    {
        3,
        "Umbral pedidos pendientes por marca",
        4, // ModuleId.BrandMonitor
        "{\"windowHours\":null,\"minOrders\":null,\"latencyWarnMs\":null,\"latencyCriticalMs\":null,\"pendingDropThreshold\":5}",
        true,
        new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc),
        "sistema"
    });
```

**Retrocompatibilidad de `RuleCondition`:** Las 2 reglas existentes (U5) tienen JSON sin el campo `pendingDropThreshold`. `System.Text.Json` con `JsonSerializerOptions` por defecto ignora propiedades ausentes — el campo deserializa como `null`, lo cual es correcto: `IsValidForModule(DbOrders)` y `IsValidForModule(DbHealth)` no evalúan `PendingDropThreshold`.

---

## §3 Registro en DI — Program.cs (extensión U6)

```csharp
// === U6: SignalR ===
builder.Services.AddSignalR(opts =>
{
    if (builder.Environment.IsDevelopment())
        opts.EnableDetailedErrors = true;
});

// === U6: AlertBroadcaster (Singleton — event multicast compartido) ===
builder.Services.AddSingleton<AlertBroadcaster>();

// === U6: NotificationService (Singleton — reemplaza stub U3; consume solo Singleton deps) ===
builder.Services.AddSingleton<INotificationService, NotificationService>();

// === U6: Brand Monitor repos y servicio (Scoped — usan DbContext) ===
builder.Services.AddScoped<IBrandSnapshotRepository, BrandSnapshotRepository>();
builder.Services.AddScoped<IBrandMonitorService, BrandMonitorService>();

// === U6: Technical Log Reader (Scoped — sin estado compartido) ===
builder.Services.AddScoped<ITechnicalLogReader, TechnicalLogReader>();

// === U6: BrandMonitorChecker (Singleton — igual que SalesforceApiChecker / MultivendeApiChecker) ===
builder.Services.AddSingleton<ICheckExecutor, BrandMonitorChecker>();

// === U6: Mapeo del hub ===
// (en la sección de middleware, después de app.UseAuthorization())
app.MapHub<AlertsHub>("/hubs/alerts");
```

**Nota:** El stub `INotificationService` registrado en U3 se elimina o reemplaza — en `Program.cs` la línea `AddSingleton<INotificationService, NotificationServiceStub>()` se sustituye por la línea de U6 anterior.

---

## §4 Extensión del filtro en `MonitoringSchedulerService`

Un solo cambio de una línea en el constructor del scheduler (U3/U4):

```csharp
// MonitoringSchedulerService — constructor (modificación U6)
// Antes (U4):
_apiCheckers = checkers
    .Where(c => c is SalesforceApiChecker or MultivendeApiChecker)
    .ToList();

// Después (U6) — agrega BrandMonitorChecker al Timer2:
_apiCheckers = checkers
    .Where(c => c is SalesforceApiChecker or MultivendeApiChecker or BrandMonitorChecker)
    .ToList();
```

**Impacto:** `BrandMonitorChecker.ExecuteAsync` se invoca en cada tick de Timer2 (10 min) junto a los checkers de API. Sin cambios en la lógica del timer.

---

## §5 Configuración — `appsettings.json` (extensión U6)

```json
{
  "Monitoring": {
    "CheckerIntervalMinutes": 5,
    "ApiCheckerIntervalMinutes": 10
  },
  "Logging": {
    "MaxExportLines": 500,
    "FilePath": "logs/log-.txt"
  }
}
```

```json
// appsettings.Development.json (extensión U6)
{
  "Monitoring": {
    "CheckerIntervalMinutes": 1,
    "ApiCheckerIntervalMinutes": 1
  },
  "Logging": {
    "MaxExportLines": 100,
    "FilePath": "logs/log-.txt"
  }
}
```

---

## §6 Archivo estático — `signalr.min.js` (setup una vez)

```powershell
# Descargar signalr.min.js y colocar en wwwroot/js/lib/
# (ejecutar desde la raíz del proyecto — una vez al configurar)
Invoke-WebRequest `
    -Uri "https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js" `
    -OutFile "src/MonitorPedidos.Web/wwwroot/js/lib/signalr.min.js"
```

**Referencia en `_Host.cshtml`:**

```html
<script src="~/js/lib/signalr.min.js"></script>
<script src="~/js/notifications.js"></script>
```

Sin dependencia de internet en runtime. CSP `script-src 'self'` sin modificar (ADR-U6-04).

---

## §7 Modificaciones a `AppDbContext` (resumen U6)

| Cambio | Detalle |
|--------|---------|
| `DbSet<BrandSnapshot> BrandSnapshots` | Agrega tabla `brand_snapshots` |
| `OnModelCreating` | Configura columnas snake_case, índice único por `site`, `Status` como string |

---

## §8 Artefactos de infraestructura por unidad (acumulado)

| Unidad | Migraciones EF | Configuración nueva | BackgroundServices |
|--------|---------------|---------------------|-------------------|
| U1 | InitialCreate (users) | HTTPS dev-certs, cookies, Data Protection | — |
| U2 | InitialCreate (incidents, jobs) | RetentionDays: 90 | IncidentMaintenanceService |
| U3 | — | CheckerIntervalMinutes: 5/1 | MonitoringSchedulerService (Timer1) |
| U4 | AddRetryMetadataToIncidents | ApiCheckerIntervalMinutes: 10/1 | MonitoringSchedulerService (Timer2) |
| U5 | AddRulesTables + seed 2 reglas | — | — |
| U6 | AddBrandSnapshots + seed regla BrandMonitor | Logging:MaxExportLines + FilePath | BrandMonitorChecker en Timer2 |
| U7 | simulated_orders, simulated_job_statuses | — | — |

---

## §9 Trazabilidad de decisiones de infraestructura

| Decisión | Componente afectado | NFR / ADR |
|----------|-------------------|-----------|
| Migración `AddBrandSnapshots` + seed | `AppDbContext`, tabla `brand_snapshots`, regla BrandMonitor | ADR-U6-02, BR-BRAND-04 |
| `AddSingleton<AlertBroadcaster>` | DI container, todos los circuitos Blazor | ADR-U6-02, BR-CONC-01 |
| `AddSingleton<INotificationService, NotificationService>` | DI container, `MonitoringService` | NFR-U6-01 |
| `AddScoped<IBrandSnapshotRepository>` | DI container | NFR-U1-05 |
| `AddScoped<IBrandMonitorService>` | DI container | NFR-U1-05 |
| `AddScoped<ITechnicalLogReader>` | DI container | NFR-U6-03 |
| `AddSingleton<ICheckExecutor, BrandMonitorChecker>` | DI container, MonitoringSchedulerService Timer2 | ADR-U3-02 |
| `AddSignalR()` + `MapHub<AlertsHub>` | DI container, routing | NFR-U6-02 |
| `Logging:MaxExportLines` en appsettings | `TechnicalLogReader` | NFR-U6-03 |
| `signalr.min.js` en `wwwroot/js/lib/` | `_Host.cshtml`, CSP | ADR-U6-04 |
