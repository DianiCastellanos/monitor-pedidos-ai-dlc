namespace MonitorPedidos.Domain.Dashboard;

public interface IBrandSnapshotRepository
{
    Task InsertAsync(BrandSnapshot snapshot, CancellationToken ct = default);
    Task<IReadOnlyList<BrandSnapshot>> GetLatestPerSiteAsync(CancellationToken ct = default);
    Task<BrandSnapshot?> GetLatestAsync(string site, CancellationToken ct = default);
    Task<BrandSnapshot?> GetSnapshotBeforeAsync(string site, DateTime before, CancellationToken ct = default);
    Task DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default);
}
