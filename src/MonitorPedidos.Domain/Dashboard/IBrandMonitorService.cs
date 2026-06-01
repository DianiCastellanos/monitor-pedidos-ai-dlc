namespace MonitorPedidos.Domain.Dashboard;

public interface IBrandMonitorService
{
    Task<IReadOnlyList<BrandSnapshot>> GetCurrentSnapshotsAsync(CancellationToken ct = default);
    Task SimulateAndRefreshAsync(CancellationToken ct = default);

    /// Conteos en tiempo real desde Salesforce, sin depender de BD.
    /// Retorna null si Salesforce no está disponible.
    Task<IReadOnlyDictionary<string, int>?> GetLiveCountsAsync(CancellationToken ct = default);
}
