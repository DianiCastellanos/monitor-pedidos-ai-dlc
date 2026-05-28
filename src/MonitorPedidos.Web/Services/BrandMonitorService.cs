using MonitorPedidos.Domain.Dashboard;

namespace MonitorPedidos.Web.Services;

public sealed class BrandMonitorService(IBrandSnapshotRepository repo) : IBrandMonitorService
{
    public Task<IReadOnlyList<BrandSnapshot>> GetCurrentSnapshotsAsync(CancellationToken ct = default)
        => repo.GetAllAsync(ct);
}
