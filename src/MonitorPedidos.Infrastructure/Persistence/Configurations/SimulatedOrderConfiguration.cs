using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitorPedidos.Domain.Simulation;

namespace MonitorPedidos.Infrastructure.Persistence.Configurations;

public sealed class SimulatedOrderConfiguration : IEntityTypeConfiguration<SimulatedOrder>
{
    public void Configure(EntityTypeBuilder<SimulatedOrder> entity)
    {
        entity.ToTable("simulated_orders");
        entity.HasKey(o => o.Id);

        entity.Property(o => o.Source)
            .HasColumnName("source").HasMaxLength(50).IsRequired();

        entity.Property(o => o.Status)
            .HasColumnName("status").HasMaxLength(50).IsRequired();

        entity.Property(o => o.CreatedAt)
            .HasColumnName("created_at").IsRequired();

        entity.Property(o => o.IsFailure)
            .HasColumnName("is_failure").IsRequired();

        entity.Property(o => o.Site)
            .HasColumnName("site").HasMaxLength(50).IsRequired();

        entity.HasIndex(o => o.CreatedAt)
            .HasDatabaseName("IX_simulated_orders_created_at");
    }
}
