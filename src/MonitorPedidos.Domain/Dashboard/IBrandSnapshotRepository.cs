namespace MonitorPedidos.Domain.Dashboard;

public interface IBrandSnapshotRepository
{
    Task UpsertAsync(BrandSnapshot snapshot, CancellationToken ct = default);
    Task<IReadOnlyList<BrandSnapshot>> GetAllAsync(CancellationToken ct = default);
}
