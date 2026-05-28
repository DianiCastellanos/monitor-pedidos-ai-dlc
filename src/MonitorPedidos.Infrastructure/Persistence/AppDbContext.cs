using Microsoft.EntityFrameworkCore;
using MonitorPedidos.Domain.Dashboard;
using MonitorPedidos.Domain.Incidents;
using MonitorPedidos.Domain.Rules;
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
    }
}
