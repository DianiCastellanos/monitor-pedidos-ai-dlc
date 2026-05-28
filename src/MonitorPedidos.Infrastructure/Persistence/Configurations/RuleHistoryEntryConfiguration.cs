using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitorPedidos.Domain.Rules;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Infrastructure.Persistence.Configurations;

public sealed class RuleHistoryEntryConfiguration : IEntityTypeConfiguration<RuleHistoryEntry>
{
    public void Configure(EntityTypeBuilder<RuleHistoryEntry> entity)
    {
        entity.ToTable("rule_history");

        entity.HasKey(h => h.Id);

        entity.Property(h => h.RuleId)
            .HasColumnName("rule_id")
            .IsRequired();

        entity.Property(h => h.ChangeType)
            .HasConversion<string>()
            .HasColumnName("change_type")
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(h => h.AuthorUserId)
            .HasColumnName("author_user_id")
            .HasMaxLength(100)
            .IsRequired();

        entity.Property(h => h.Reason)
            .HasMaxLength(500)
            .IsRequired();

        entity.Property(h => h.SnapshotBefore)
            .HasColumnName("snapshot_before")
            .HasColumnType("text")
            .IsRequired(false);

        entity.Property(h => h.SnapshotAfter)
            .HasColumnName("snapshot_after")
            .HasColumnType("text")
            .IsRequired(false);

        entity.Property(h => h.ChangedAt)
            .HasColumnName("changed_at")
            .IsRequired();

        entity.HasOne<Rule>()
            .WithMany()
            .HasForeignKey(h => h.RuleId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(h => h.RuleId)
            .HasDatabaseName("IX_rule_history_RuleId");

        entity.HasIndex(h => h.ChangedAt)
            .HasDatabaseName("IX_rule_history_ChangedAt");
    }
}
