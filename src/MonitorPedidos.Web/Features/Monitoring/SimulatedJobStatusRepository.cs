using MonitorPedidos.Domain.Monitoring;

namespace MonitorPedidos.Web.Features.Monitoring;

// Stub temporal — reemplazado por U7 con SimulatedJobStatusRepository + tabla simulated_job_statuses.
public sealed class SimulatedJobStatusRepository : IJobStatusSource
{
    public Task<IReadOnlyList<JobStatusSnapshot>> GetCurrentStatusAsync(CancellationToken ct = default)
    {
        IReadOnlyList<JobStatusSnapshot> jobs =
        [
            new JobStatusSnapshot(
                JobId:                  "JOB-001",
                JobName:                "DescargaPedidos",
                IsRunning:              true,
                LastExecutedAt:         DateTimeOffset.UtcNow.AddMinutes(-3),
                LastExecutionSucceeded: true),
        ];
        return Task.FromResult(jobs);
    }
}
