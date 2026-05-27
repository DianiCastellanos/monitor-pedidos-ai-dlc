using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Domain.Incidents;

public interface IIncidentRepository
{
    Task AddAsync(Incident incident, CancellationToken ct = default);

    Task<Incident?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Incident?> GetOpenByModuleAsync(ModuleId module, CancellationToken ct = default);

    Task<IReadOnlyList<Incident>> GetRecentClosedByModuleAsync(
        ModuleId module, int count, CancellationToken ct = default);

    Task<PagedResult<Incident>> SearchAsync(
        IncidentSearchFilter filter, CancellationToken ct = default);

    Task<IReadOnlyList<WeeklySummaryEntry>> GetWeeklySummaryAsync(
        DateOnly weekStart, CancellationToken ct = default);

    Task<int> PurgeExpiredAsync(int retentionDays, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
