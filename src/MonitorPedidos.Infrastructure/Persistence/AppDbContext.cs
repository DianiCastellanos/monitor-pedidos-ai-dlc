using Microsoft.EntityFrameworkCore;
using MonitorPedidos.Domain.Incidents;
using MonitorPedidos.Infrastructure.Persistence.Configurations;

namespace MonitorPedidos.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Incident> Incidents => Set<Incident>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new IncidentConfiguration());
    }
}
