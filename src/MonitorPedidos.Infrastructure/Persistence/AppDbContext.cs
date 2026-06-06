using Microsoft.EntityFrameworkCore;
using MonitorPedidos.Domain.Dashboard;
using MonitorPedidos.Domain.Incidents;
using MonitorPedidos.Domain.Rules;
using MonitorPedidos.Domain.Shared;
using MonitorPedidos.Domain.Simulation;
using MonitorPedidos.Infrastructure.Persistence.Configurations;

namespace MonitorPedidos.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Incident>            Incidents           => Set<Incident>();
    public DbSet<Rule>                Rules               => Set<Rule>();
    public DbSet<RuleHistoryEntry>    RuleHistory         => Set<RuleHistoryEntry>();
    public DbSet<BrandSnapshot>       BrandSnapshots      => Set<BrandSnapshot>();
    public DbSet<SimulatedOrder>      SimulatedOrders     => Set<SimulatedOrder>();
    public DbSet<SimulatedJobStatus>  SimulatedJobStatuses => Set<SimulatedJobStatus>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new IncidentConfiguration());
        modelBuilder.ApplyConfiguration(new RuleConfiguration());
        modelBuilder.ApplyConfiguration(new RuleHistoryEntryConfiguration());
        modelBuilder.ApplyConfiguration(new BrandSnapshotConfiguration());
        modelBuilder.ApplyConfiguration(new SimulatedOrderConfiguration());
        modelBuilder.ApplyConfiguration(new SimulatedJobStatusConfiguration());

        // Índice único filtrado — sintaxis diferente por proveedor (BR-INC-01)
        var isNpgsql = Database.ProviderName?.Contains("Npgsql") == true;
        var filterExpr = isNpgsql ? "\"ClosedAt\" IS NULL" : "[ClosedAt] IS NULL";
        modelBuilder.Entity<Incident>()
            .HasIndex(i => i.Module)
            .HasFilter(filterExpr)
            .IsUnique()
            .HasDatabaseName("IX_incidents_Module_Open");

        modelBuilder.Entity<Rule>().HasData(new
        {
            Id            = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
            Name          = "Ventana de pedidos — Salesforce/Multivende",
            Description   = "Alerta si no hay pedidos en la ventana esperada para Salesforce o Multivende",
            ModuleId      = ModuleId.DbOrderChecker,
            ConditionJson = """{"WindowMinutes":10,"MinOrders":1,"Channels":["SALESFORCE","MULTIVENDE"]}""",
            Severity      = Severity.Critical,
            IsActive      = true,
            CreatedAt     = new DateTimeOffset(2026, 5, 29, 0, 0, 0, TimeSpan.Zero),
            UpdatedAt     = (DateTimeOffset?)null
        });
    }
}
