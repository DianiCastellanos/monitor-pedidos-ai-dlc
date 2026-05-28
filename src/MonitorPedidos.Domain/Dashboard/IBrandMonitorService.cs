namespace MonitorPedidos.Domain.Dashboard;

public interface IBrandMonitorService
{
    Task<IReadOnlyList<BrandSnapshot>> GetCurrentSnapshotsAsync(CancellationToken ct = default);
    Task SimulateAndRefreshAsync(CancellationToken ct = default);
}
