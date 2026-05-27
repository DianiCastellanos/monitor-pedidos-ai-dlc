using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitorPedidos.Domain.Alerts;
using MonitorPedidos.Domain.Incidents;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Infrastructure.Persistence.Configurations;

public sealed class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> entity)
    {
        entity.ToTable("incidents");

        entity.HasKey(i => i.Id);

        entity.Property(i => i.Module)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(i => i.Cause)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        entity.Property(i => i.Severity)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        entity.Property(i => i.OpenedAt).IsRequired();
        entity.Property(i => i.ClosedAt);

        entity.Property(i => i.CloseType)
            .HasConversion<string>()
            .HasMaxLength(20);

        entity.Property(i => i.ClosedByRole).HasMaxLength(50);
        entity.Property(i => i.ComentarioResolucion).HasMaxLength(1000);
        entity.Property(i => i.IsCandidatoReglaNueva).IsRequired();

        entity.OwnsOne(i => i.Alert, alert =>
        {
            alert.Property(a => a.QuePaso)
                .HasColumnName("Alert_QuePaso")
                .HasMaxLength(500)
                .IsRequired();
            alert.Property(a => a.Cuando)
                .HasColumnName("Alert_Cuando")
                .IsRequired();
            alert.Property(a => a.Donde)
                .HasColumnName("Alert_Donde")
                .HasMaxLength(100)
                .IsRequired();
            alert.Property(a => a.SeveridadTexto)
                .HasColumnName("Alert_SeveridadTexto")
                .HasMaxLength(50)
                .IsRequired();
            alert.Property(a => a.CausaProbable)
                .HasColumnName("Alert_CausaProbable")
                .HasMaxLength(300)
                .IsRequired();
            alert.Property(a => a.AccionSugerida)
                .HasColumnName("Alert_AccionSugerida")
                .HasMaxLength(500)
                .IsRequired();
        });

        entity.HasIndex(i => i.OpenedAt)
            .HasDatabaseName("IX_incidents_OpenedAt");

        entity.HasIndex(i => new { i.Module, i.ClosedAt })
            .HasDatabaseName("IX_incidents_Module_ClosedAt");

        // Índice único filtrado: garantiza máximo 1 incidente abierto por módulo (BR-INC-01)
        entity.HasIndex(i => i.Module)
            .HasFilter("\"ClosedAt\" IS NULL")
            .IsUnique()
            .HasDatabaseName("IX_incidents_Module_Open");
    }
}
