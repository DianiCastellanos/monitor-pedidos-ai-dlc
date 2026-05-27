namespace MonitorPedidos.Domain.Monitoring;

public interface IJobStatusSource
{
    Task<IReadOnlyList<JobStatusSnapshot>> GetCurrentStatusAsync(CancellationToken ct = default);
}
