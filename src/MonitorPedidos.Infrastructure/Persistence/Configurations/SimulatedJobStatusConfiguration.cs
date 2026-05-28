using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MonitorPedidos.Domain.Simulation;

namespace MonitorPedidos.Infrastructure.Persistence.Configurations;

public sealed class SimulatedJobStatusConfiguration : IEntityTypeConfiguration<SimulatedJobStatus>
{
    public void Configure(EntityTypeBuilder<SimulatedJobStatus> entity)
    {
        entity.ToTable("simulated_job_statuses");
        entity.HasKey(j => j.Id);

        entity.Property(j => j.JobName)
            .HasColumnName("job_name").HasMaxLength(100).IsRequired();

        entity.Property(j => j.LastRunAt)
            .HasColumnName("last_run_at").IsRequired();

        entity.Property(j => j.Status)
            .HasColumnName("status").HasMaxLength(50).IsRequired();

        entity.Property(j => j.ErrorMessage)
            .HasColumnName("error_message").HasMaxLength(500).IsRequired(false);

        entity.HasIndex(j => j.JobName)
            .IsUnique()
            .HasDatabaseName("UX_simulated_job_statuses_job_name");
    }
}
