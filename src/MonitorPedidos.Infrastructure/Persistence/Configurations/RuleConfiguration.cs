using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitorPedidos.Domain.Rules;

namespace MonitorPedidos.Infrastructure.Persistence.Configurations;

public sealed class RuleConfiguration : IEntityTypeConfiguration<Rule>
{
    public void Configure(EntityTypeBuilder<Rule> entity)
    {
        entity.ToTable("rules");

        entity.HasKey(r => r.Id);

        entity.Property(r => r.Name)
            .HasMaxLength(200)
            .IsRequired();

        entity.Property(r => r.Description)
            .HasMaxLength(500)
            .IsRequired();

        entity.Property(r => r.ModuleId)
            .HasColumnName("AppliesTo")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(r => r.ConditionJson)
            .HasColumnName("condition_json")
            .HasColumnType("text")
            .IsRequired();

        entity.Property(r => r.Severity)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        entity.Property(r => r.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        entity.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        entity.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired(false);

        entity.HasIndex(r => new { r.ModuleId, r.IsActive })
            .HasDatabaseName("IX_rules_Module_Active");
    }
}
