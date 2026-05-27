# Tech Stack Decisions — U2 Persistence & Incidents

**Unidad:** U2 — Persistence & Incidents
**Stage:** Construction → NFR Requirements
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Stack de U2 — todo heredado de U1

U2 no introduce nuevas tecnologías. Reutiliza el stack completo de U1:

| Capa | Tecnología | Versión | Fuente |
|------|-----------|---------|--------|
| ORM | Entity Framework Core 8 | 8.x | U1, ADR-001 |
| BD | SQL Server LocalDB | 2019+ | U1, ADR-001 |
| Logging | Serilog (File Sink) | 3.x | U1, NFR-U1-03 |
| Web | ASP.NET Core 8 + Blazor Server | — | U1, ADR-001 |
| Tests | xUnit + WebApplicationFactory | — | U1, NFR-U1-05 |

**Nuevos paquetes NuGet en U2:** ninguno.

---

## §2 Estructura de archivos que genera U2

```
src/
+-- MonitorPedidos.Domain/
|   +-- Incidents/
|   |   +-- Incident.cs                    (aggregate root)
|   |   +-- IIncidentRepository.cs         (contrato de persistencia)
|   |   +-- IIncidentService.cs            (contrato de servicio)
|   |   +-- IncidentSearchFilter.cs        (value object — query params)
|   |   +-- WeeklySummaryEntry.cs          (value object — resumen)
|   +-- Shared/
|       +-- PagedResult.cs                  (genérico — si no existe de U1)
|
+-- MonitorPedidos.Infrastructure/
|   +-- Persistence/
|       +-- IncidentRepository.cs           (implementación EF Core)
|       +-- Configurations/
|           +-- IncidentConfiguration.cs    (IEntityTypeConfiguration<Incident>)
|
+-- MonitorPedidos.Web/
    +-- Features/
    |   +-- Incidents/
    |       +-- HistoricPage.razor
    |       +-- HistoricPage.razor.cs
    |       +-- WeeklySummaryPage.razor
    |       +-- WeeklySummaryPage.razor.cs
    |       +-- IncidentDetailPage.razor
    |       +-- IncidentDetailPage.razor.cs
    +-- Services/
        +-- IncidentService.cs              (implementación de IIncidentService)

tests/
+-- MonitorPedidos.IntegrationTests/
    +-- Incidents/
        +-- IncidentRepositoryTests.cs      (T-U2-01, T-U2-04)
        +-- IncidentServiceTests.cs         (T-U2-02, T-U2-03, T-U2-05)
```

---

## §3 Configuración EF Core específica de U2

### 3.1 IncidentConfiguration (IEntityTypeConfiguration)

```csharp
public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> entity)
    {
        entity.HasKey(i => i.Id);
        entity.Property(i => i.Id).ValueGeneratedNever(); // GUID generado en dominio

        entity.Property(i => i.Module)
              .HasConversion<string>().HasMaxLength(50).IsRequired();
        entity.Property(i => i.Cause)
              .HasConversion<string>().HasMaxLength(50).IsRequired();
        entity.Property(i => i.Severity)
              .HasConversion<string>().HasMaxLength(20).IsRequired();
        entity.Property(i => i.CloseType)
              .HasConversion<string>().HasMaxLength(20);
        entity.Property(i => i.ClosedByRole).HasMaxLength(50);
        entity.Property(i => i.ComentarioResolucion).HasMaxLength(1000);

        // AlertMessage — owned value object
        entity.OwnsOne(i => i.Alert, alert =>
        {
            alert.Property(a => a.QuePaso).HasColumnName("Alert_QuePaso").HasMaxLength(500);
            alert.Property(a => a.Cuando).HasColumnName("Alert_Cuando");
            alert.Property(a => a.Donde).HasColumnName("Alert_Donde").HasMaxLength(100);
            alert.Property(a => a.SeveridadTexto).HasColumnName("Alert_SeveridadTexto").HasMaxLength(50);
            alert.Property(a => a.CausaProbable).HasColumnName("Alert_CausaProbable").HasMaxLength(300);
            alert.Property(a => a.AccionSugerida).HasColumnName("Alert_AccionSugerida").HasMaxLength(500);
        });

        // Índices
        entity.HasIndex(i => i.OpenedAt)
              .HasDatabaseName("IX_incidents_OpenedAt");

        entity.HasIndex(i => new { i.Module, i.ClosedAt })
              .HasDatabaseName("IX_incidents_Module_ClosedAt");

        // NFR-U2-01 — índice único filtrado: máximo 1 incidente abierto por módulo
        entity.HasIndex(i => i.Module)
              .HasFilter("\"ClosedAt\" IS NULL")
              .IsUnique()
              .HasDatabaseName("IX_incidents_Module_Open");
    }
}
```

### 3.2 Registro en AppDbContext

```csharp
// AppDbContext.cs — agregar en OnModelCreating
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfiguration(new IncidentConfiguration());
}

// DbSet en AppDbContext
public DbSet<Incident> Incidents => Set<Incident>();
```

---

## §4 Migration de U2

```bash
# Crear migration para tabla incidents
dotnet ef migrations add AddIncidentSchema --project src/MonitorPedidos.Web

# Aplicar
dotnet ef database update --project src/MonitorPedidos.Web
```

**La migration crea:**
- Tabla `incidents` con 17 columnas (ver domain-entities.md §5)
- Índice `IX_incidents_OpenedAt`
- Índice `IX_incidents_Module_ClosedAt`
- Índice único filtrado `IX_incidents_Module_Open` (NFR-U2-01)

---

## §5 Patrón AsNoTracking — separación lectura / escritura

```csharp
// IncidentRepository.cs — patrón aplicado

// LECTURA — AsNoTracking (NFR-U2-02)
public async Task<PagedResult<Incident>> SearchAsync(
    IncidentSearchFilter filter, CancellationToken ct)
{
    var query = _context.Incidents.AsNoTracking();
    // aplicar filtros...
}

public async Task<IReadOnlyList<WeeklySummaryEntry>> GetWeeklySummaryAsync(
    DateOnly weekStart, CancellationToken ct)
{
    var from = weekStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
    return await _context.Incidents.AsNoTracking()
        .Where(i => i.OpenedAt >= from && i.OpenedAt < from.AddDays(7))
        .GroupBy(i => new { i.Cause, i.Severity })
        .Select(g => new WeeklySummaryEntry(
            g.Key.Cause, g.Key.Severity,
            g.Count(),
            g.Count(i => i.IsCandidatoReglaNueva)))
        .ToListAsync(ct);
}

// ESCRITURA — tracking normal
public async Task AddAsync(Incident incident, CancellationToken ct)
    => await _context.Incidents.AddAsync(incident, ct);
```

---

## §6 Trazabilidad de decisiones

| Decisión | NFR | Business Rule | SECURITY |
|----------|-----|---------------|----------|
| Unique index filtrado | NFR-U2-01 | BR-INC-01 | SECURITY-05 |
| AsNoTracking en lecturas | NFR-U2-02 | — | — |
| Sin caché WeeklySummary | NFR-U2-03 | BR-WEEKLY-03 | — |
| Tests con LocalDB real | NFR-U2-04 | BR-INC-01, BR-CLOSE-01 | SECURITY-05 |
