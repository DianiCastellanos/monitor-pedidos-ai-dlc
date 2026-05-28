using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitorPedidos.Domain.Dashboard;

namespace MonitorPedidos.Infrastructure.Persistence.Configurations;

public sealed class BrandSnapshotConfiguration : IEntityTypeConfiguration<BrandSnapshot>
{
    public void Configure(EntityTypeBuilder<BrandSnapshot> entity)
    {
        entity.ToTable("brand_snapshots");

        entity.HasKey(s => s.Id);

        entity.Property(s => s.Site)
            .HasColumnName("site")
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(s => s.PendingCountCurrent)
            .HasColumnName("pending_count_current")
            .IsRequired();

        entity.Property(s => s.PendingCountPrevious)
            .HasColumnName("pending_count_previous")
            .IsRequired(false);

        entity.Property(s => s.CheckedAt)
            .HasColumnName("checked_at")
            .IsRequired();

        entity.Property(s => s.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        entity.HasIndex(s => new { s.Site, s.CheckedAt })
            .HasDatabaseName("IX_brand_snapshots_Site_CheckedAt");
    }
}
