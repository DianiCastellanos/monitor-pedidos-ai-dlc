# Infrastructure Design — U7 Simulation & Red-Teaming

**Unidad:** U7 — Simulation & Red-Teaming
**Stage:** Construction → Infrastructure Design
**Fecha:** 2026-05-24
**Versión:** 1.0

---

## §1 Decisiones aplicadas

| ID | Pregunta | Decisión |
|----|----------|---------|
| P1 | Migración EF Core | A — Una migración `AddSimulatedTables`: ambas tablas + seed `simulated_job_statuses` |
| P2 | Lifecycle de repositorios | A — Scoped — usan `DbContext`; consistente con todos los repos del proyecto |
| P3 | `InternalsVisibleTo` para tests | A — `[assembly: InternalsVisibleTo("MonitorPedidos.UnitTests")]` en `AssemblyInfo.cs` |
| P4 | Sección `Simulation` en appsettings | A — `Enabled=false` en base (prod); `Enabled=true` con valores de dev en Development |

---

## §2 Migración EF Core — `AddSimulatedTables`

### Comando de migración

```bash
dotnet ef migrations add AddSimulatedTables --project src/MonitorPedidos.Web
dotnet ef database update --project src/MonitorPedidos.Web
```

### Secuencia completa de migraciones del proyecto

```
Migrations/
    ├── 20260520_InitialCreate.cs                    ← U2: incidents, users, jobs
    ├── 20260523_AddRetryMetadataToIncidents.cs      ← U4: retry_metadata
    ├── 20260524_AddRulesTables.cs                   ← U5: rules, rule_history + seed 2 reglas
    ├── 20260524_AddBrandSnapshots.cs                ← U6: brand_snapshots + seed BrandMonitor
    └── 20260524_AddSimulatedTables.cs               ← U7: simulated_orders + simulated_job_statuses + seed jobs
```

### Configuración en `OnModelCreating`

```csharp
// AppDbContext.OnModelCreating — extensión U7

modelBuilder.Entity<SimulatedOrder>(b =>
{
    b.ToTable("simulated_orders");
    b.HasKey(o => o.Id);
    b.Property(o => o.Source)
     .HasColumnName("source").HasMaxLength(50).IsRequired();
    b.Property(o => o.Status)
     .HasColumnName("status").HasMaxLength(50).IsRequired();
    b.Property(o => o.CreatedAt)
     .HasColumnName("created_at").IsRequired();
    b.Property(o => o.IsFailure)
     .HasColumnName("is_failure").IsRequired();
    b.Property(o => o.Site)
     .HasColumnName("site").HasMaxLength(50).IsRequired();

    // Índice en created_at para la consulta del DbOrderChecker (WHERE created_at > NOW()-Nh)
    b.HasIndex(o => o.CreatedAt).HasDatabaseName("IX_simulated_orders_created_at");
});

modelBuilder.Entity<SimulatedJobStatus>(b =>
{
    b.ToTable("simulated_job_statuses");
    b.HasKey(j => j.Id);
    b.Property(j => j.JobName)
     .HasColumnName("job_name").HasMaxLength(100).IsRequired();
    b.Property(j => j.LastRunAt)
     .HasColumnName("last_run_at").IsRequired();
    b.Property(j => j.Status)
     .HasColumnName("status").HasMaxLength(50).IsRequired();
    b.Property(j => j.ErrorMessage)
     .HasColumnName("error_message").HasMaxLength(500).IsRequired(false);

    b.HasIndex(j => j.JobName).IsUnique()
     .HasDatabaseName("UX_simulated_job_statuses_job_name");
});
```

### Seed data en `Up()` de la migración

```csharp
// Seed: 2 jobs en estado Completed (baseline normal)
migrationBuilder.InsertData(
    table: "simulated_job_statuses",
    columns: new[] { "id", "job_name", "last_run_at", "status", "error_message" },
    values: new object[]
    {
        1, "SalesforceDownload",
        new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc),
        "Completed", null
    });

migrationBuilder.InsertData(
    table: "simulated_job_statuses",
    columns: new[] { "id", "job_name", "last_run_at", "status", "error_message" },
    values: new object[]
    {
        2, "MultivendeDownload",
        new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc),
        "Completed", null
    });
// simulated_orders inicia vacía — el simulador la llena en el primer tick
```

---

## §3 Registro en DI — Program.cs (extensión U7)

```csharp
// === U7: Simulation options ===
builder.Services.Configure<SimulationOptions>(
    builder.Configuration.GetSection(SimulationOptions.Section));

// === U7: Simulation repositories (Scoped — usan DbContext) ===
builder.Services.AddScoped<ISimulatedOrderRepository, SimulatedOrderRepository>();
builder.Services.AddScoped<ISimulatedJobStatusRepository, SimulatedJobStatusRepository>();

// === U7: Simulator BackgroundService (Singleton vía AddHostedService) ===
builder.Services.AddHostedService<OrdersSimulatorService>();
```

---

## §4 `InternalsVisibleTo` — acceso a `RunOneTickAsync` desde tests

```csharp
// src/MonitorPedidos.Web/Properties/AssemblyInfo.cs (crear si no existe)
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MonitorPedidos.UnitTests")]
```

Permite que `OrdersSimulatorServiceTests` invoque `internal async Task RunOneTickAsync(...)` directamente sin reflexión.

---

## §5 Configuración appsettings

### `appsettings.json` (base — producción / post-MVP)

```json
{
  "Simulation": {
    "Enabled": false
  }
}
```

`Enabled=false` en producción: el simulador sale inmediatamente en `ExecuteAsync` sin loop (ADR-U7-03). Los repositorios Scoped siguen registrados — sin impacto.

### `appsettings.Development.json` (extensión U7)

```json
{
  "Simulation": {
    "Enabled": true,
    "InsertIntervalMinutes": 5,
    "FailureProbability": 0.0,
    "NoOrdersMode": false
  }
}
```

### Variables de entorno para escenarios red-teaming (sin editar archivos)

```powershell
$env:Simulation__NoOrdersMode       = "true"   # RT1: sin pedidos
$env:Simulation__FailureProbability = "1.0"    # RT5: todos los pedidos con error
$env:Simulation__Enabled            = "false"  # apagar simulador
```

---

## §6 Modificaciones a `AppDbContext` (resumen U7)

| Cambio | Detalle |
|--------|---------|
| `DbSet<SimulatedOrder> SimulatedOrders` | Agrega tabla `simulated_orders` |
| `DbSet<SimulatedJobStatus> SimulatedJobStatuses` | Agrega tabla `simulated_job_statuses` |
| `OnModelCreating` | Configura columnas snake_case, índice en `created_at`, índice único en `job_name` |

---

## §7 Artefactos de infraestructura por unidad — TABLA FINAL COMPLETA

| Unidad | Migraciones EF | Config nueva | BackgroundServices |
|--------|---------------|-------------|-------------------|
| U1 | InitialCreate (users) | HTTPS, cookies, DataProtection | — |
| U2 | InitialCreate (incidents, jobs) | RetentionDays: 90 | IncidentMaintenanceService |
| U3 | — | CheckerIntervalMinutes: 5/1 | MonitoringSchedulerService (Timer1) |
| U4 | AddRetryMetadataToIncidents | ApiCheckerIntervalMinutes: 10/1 | MonitoringSchedulerService (Timer2) |
| U5 | AddRulesTables + seed 2 reglas | — | — |
| U6 | AddBrandSnapshots + seed BrandMonitor | Logging:MaxExportLines + FilePath | BrandMonitorChecker en Timer2 |
| U7 | AddSimulatedTables + seed 2 jobs | Simulation:Enabled false/true | OrdersSimulatorService (propio) |

---

## §8 Trazabilidad de decisiones de infraestructura

| Decisión | Componente afectado | NFR / ADR |
|----------|-------------------|-----------|
| Migración `AddSimulatedTables` + seed | `AppDbContext`, `simulated_orders`, `simulated_job_statuses` | BR-SIM-01, BR-SIM-04 |
| Índice `IX_simulated_orders_created_at` | `DbOrderChecker` query | BR-CLEAN-01 |
| `AddScoped<ISimulatedOrderRepository>` | DI container | NFR-U1-05 |
| `AddScoped<ISimulatedJobStatusRepository>` | DI container | NFR-U1-05 |
| `AddHostedService<OrdersSimulatorService>` | DI container, startup | ADR-U7-01, ADR-U7-04 |
| `Configure<SimulationOptions>` | DI container | ADR-U7-04 |
| `InternalsVisibleTo("MonitorPedidos.UnitTests")` | `AssemblyInfo.cs` | ADR-U7-02 |
| `Simulation:Enabled=false` en base | `OrdersSimulatorService` | BR-SIM-01, NFR-U7-04 |
