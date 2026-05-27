# Infrastructure Design — U5 Rules Management

**Unidad:** U5 — Rules Management
**Stage:** Construction → Infrastructure Design
**Fecha:** 2026-05-24
**Versión:** 1.0

---

## §1 Decisiones aplicadas

| ID | Pregunta | Decisión |
|----|----------|---------|
| P1 | Migración EF Core para tablas `rules` y `rule_history` + seed data | A — Nueva migración CLI: `AddRulesTables` con seed data en `Up()` |
| P2 | Ciclo de vida de `RuleManagementService`, `IRuleRepository`, `IRuleHistoryRepository` | A — `Scoped` — coherente con `IIncidentService`/`IIncidentRepository` de U2 |
| P3 | `DbOrderChecker` (Singleton) consume `IRuleRepository` (Scoped) | A — `IServiceScopeFactory` — mismo patrón que `MonitoringService` usa para `IIncidentRepository` |

---

## §2 Migración EF Core — tablas `rules` y `rule_history`

### Comando de migración

```bash
dotnet ef migrations add AddRulesTables --project src/MonitorPedidos.Web
dotnet ef database update --project src/MonitorPedidos.Web
```

### Secuencia de migraciones en el proyecto

```
Migrations/
    ├── 20260520_InitialCreate.cs                   ← U2: incidents, users, jobs
    ├── 20260523_AddRetryMetadataToIncidents.cs     ← U4: columna retry_metadata
    └── 20260524_AddRulesTables.cs                  ← U5: rules, rule_history + seed data
```

### Configuración de entidades en `OnModelCreating`

```csharp
// AppDbContext.OnModelCreating — extensión U5

modelBuilder.Entity<Rule>(b =>
{
    b.ToTable("rules");
    b.HasKey(r => r.Id);
    b.Property(r => r.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
    b.Property(r => r.ModuleId).HasColumnName("module_id").IsRequired();
    b.Property(r => r.ConditionJson)
     .HasColumnName("condition_json")
     .HasColumnType("nvarchar(max)")
     .IsRequired();
    b.Property(r => r.IsActive).HasColumnName("is_active").IsRequired();
    b.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
    b.Property(r => r.CreatedBy).HasColumnName("created_by").HasMaxLength(100).IsRequired();
    b.Property(r => r.UpdatedAt).HasColumnName("updated_at").IsRequired(false);
    b.Property(r => r.UpdatedBy).HasColumnName("updated_by").HasMaxLength(100).IsRequired(false);
    b.HasMany(r => r.History)
     .WithOne()
     .HasForeignKey(h => h.RuleId)
     .OnDelete(DeleteBehavior.Cascade);
});

modelBuilder.Entity<RuleHistoryEntry>(b =>
{
    b.ToTable("rule_history");
    b.HasKey(h => h.Id);
    b.Property(h => h.RuleId).HasColumnName("rule_id").IsRequired();
    b.Property(h => h.Action).HasColumnName("action").HasMaxLength(50).IsRequired();
    b.Property(h => h.ChangedBy).HasColumnName("changed_by").HasMaxLength(100).IsRequired();
    b.Property(h => h.Reason).HasColumnName("reason").HasMaxLength(500).IsRequired();
    b.Property(h => h.ChangedAt).HasColumnName("changed_at").IsRequired();
    b.Property(h => h.SnapshotBefore)
     .HasColumnName("snapshot_before")
     .HasColumnType("nvarchar(max)")
     .IsRequired(false);
    b.Property(h => h.SnapshotAfter)
     .HasColumnName("snapshot_after")
     .HasColumnType("nvarchar(max)")
     .IsRequired();
});
```

### Seed data en `Up()` de la migración

```csharp
// Migration Up() — 2 reglas iniciales para los módulos cubiertos por U3
migrationBuilder.InsertData(
    table: "rules",
    columns: new[] { "id", "name", "module_id", "condition_json", "is_active", "created_at", "created_by" },
    values: new object[]
    {
        1,
        "Umbral pedidos DB",
        1, // ModuleId.DbOrders
        "{\"windowHours\":2,\"minOrders\":1,\"latencyWarnMs\":null,\"latencyCriticalMs\":null}",
        true,
        new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc),
        "sistema"
    });

migrationBuilder.InsertData(
    table: "rules",
    columns: new[] { "id", "name", "module_id", "condition_json", "is_active", "created_at", "created_by" },
    values: new object[]
    {
        2,
        "Latencia DB",
        3, // ModuleId.DbHealth
        "{\"windowHours\":null,\"minOrders\":null,\"latencyWarnMs\":500,\"latencyCriticalMs\":2000}",
        true,
        new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc),
        "sistema"
    });
```

**Nota:** `Down()` elimina las filas de seed antes de `DropTable` para evitar errores de FK.

---

## §3 Registro en DI — Program.cs (extensión U5)

```csharp
// === U5: Rule repositories (Scoped — igual que IIncidentRepository de U2) ===
builder.Services.AddScoped<IRuleRepository, RuleRepository>();
builder.Services.AddScoped<IRuleHistoryRepository, RuleHistoryRepository>();

// === U5: Rule management service (Scoped — orquesta repositorios y SaveChangesAsync) ===
builder.Services.AddScoped<IRuleManagementService, RuleManagementService>();
```

**Rationale:** `Scoped` es el ciclo correcto para Blazor Server — cada circuito tiene su propio scope y `DbContext`. Consistente con `IIncidentService`/`IIncidentRepository` de U2 (SECURITY-09, NFR-U1-05).

---

## §4 Patrón `IServiceScopeFactory` en `DbOrderChecker`

`DbOrderChecker` está registrado como `Singleton` (ADR-U3-02 — `IEnumerable<ICheckExecutor>` en DI). Necesita acceder a `IRuleRepository` que es `Scoped`. Solución: `IServiceScopeFactory`, mismo patrón que `MonitoringService` usa para `IIncidentRepository`.

```csharp
public sealed class DbOrderChecker : ICheckExecutor
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDbConnectionFactory _dbFactory;
    private readonly ILogger<DbOrderChecker> _logger;

    public DbOrderChecker(
        IServiceScopeFactory scopeFactory,
        IDbConnectionFactory dbFactory,
        ILogger<DbOrderChecker> logger)
    {
        _scopeFactory = scopeFactory;
        _dbFactory    = dbFactory;
        _logger       = logger;
    }

    public async Task<CheckResult> ExecuteAsync(CancellationToken ct)
    {
        // Crear scope para resolver IRuleRepository (Scoped)
        await using var scope = _scopeFactory.CreateAsyncScope();
        var ruleRepo = scope.ServiceProvider.GetRequiredService<IRuleRepository>();

        var rule = await ruleRepo.GetActiveByModuleAsync(ModuleId.DbOrders, ct);
        var condition = rule?.GetCondition(); // null si no hay regla activa → defaults seguros

        var windowHours = condition?.WindowHours ?? 2;
        var minOrders   = condition?.MinOrders   ?? 1;

        // ... resto de la lógica de chequeo usando windowHours y minOrders
    }
}
```

**Flujo de vida del scope:** El `AsyncServiceScope` se crea al inicio de `ExecuteAsync` y se destruye al salir (await using). El `DbContext` scoped vive exactamente durante una ejecución del checker — sin retención entre ticks del timer.

---

## §5 Nuevo paquete — MonitorPedidos.UnitTests (U5)

```xml
<!-- MonitorPedidos.UnitTests.csproj — paquete adicional U5 -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.*" />
```

**Razón:** El proveedor `InMemory` de EF Core no soporta transacciones reales. SQLite en modo `:memory:` sí las soporta y permite verificar la atomicidad `Rule + RuleHistoryEntry` en una sola `SaveChangesAsync` (NFR-U5-02, ADR-U5-01).

Los paquetes existentes de U3/U4 (xUnit, Moq, FluentAssertions, coverlet, RichardSzalay.MockHttp) se mantienen sin cambios.

---

## §6 Modificaciones a `AppDbContext` (resumen)

| Cambio | Detalle |
|--------|---------|
| `DbSet<Rule> Rules` | Agrega tabla `rules` |
| `DbSet<RuleHistoryEntry> RuleHistory` | Agrega tabla `rule_history` |
| `OnModelCreating` | Configura columnas snake_case, tipos de columna, FK con cascade delete |
| Seed data | 2 filas en `rules` vía migración `AddRulesTables` |

---

## §7 Artefactos de infraestructura por unidad (acumulado)

| Unidad | Migraciones EF | Configuración | Proyectos | BackgroundServices |
|--------|---------------|--------------|----------|-------------------|
| U1 | InitialCreate (users) | HTTPS dev-certs, cookies, Data Protection | MonitorPedidos.Web | — |
| U2 | InitialCreate (incidents, jobs) | RetentionDays: 90 | — | IncidentMaintenanceService |
| U3 | — | CheckerIntervalMinutes: 5 (prod) / 1 (dev) | MonitorPedidos.UnitTests | MonitoringSchedulerService (Timer1) |
| U4 | AddRetryMetadataToIncidents | ApiCheckerIntervalMinutes: 10 (prod) / 1 (dev) | — (UnitTests ya existe) | MonitoringSchedulerService (Timer2) |
| U5 | AddRulesTables (+ seed 2 reglas) | — | EF.Sqlite en UnitTests | — |
| U6 | (pendiente) | — | — | — |
| U7 | simulated_orders, simulated_job_statuses | — | — | — |

---

## §8 Trazabilidad de decisiones de infraestructura

| Decisión | Componente afectado | NFR / ADR |
|----------|-------------------|-----------|
| Migración `AddRulesTables` | `AppDbContext`, tablas `rules`, `rule_history` | ADR-U5-03, BR-SEED-01, BR-SEED-02 |
| Seed data 2 reglas en `Up()` | `RuleRepository.GetActiveByModuleAsync` | BR-SEED-01, BR-SEED-02 |
| `AddScoped<IRuleRepository>` | DI container, Blazor Server circuits | NFR-U1-05, SECURITY-09 |
| `AddScoped<IRuleHistoryRepository>` | DI container | NFR-U1-05 |
| `AddScoped<IRuleManagementService>` | DI container | NFR-U1-05 |
| `IServiceScopeFactory` en `DbOrderChecker` | DbOrderChecker (Singleton), IRuleRepository (Scoped) | ADR-U3-02, NFR-U5-03 |
| `Microsoft.EntityFrameworkCore.Sqlite` en UnitTests | RuleManagementServiceIntegrationTests | NFR-U5-02, ADR-U5-01 |
