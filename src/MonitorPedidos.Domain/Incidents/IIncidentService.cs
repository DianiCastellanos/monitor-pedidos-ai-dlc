using MonitorPedidos.Domain.Alerts;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Domain.Incidents;

public interface IIncidentService
{
    Task<Incident> OpenIncidentAsync(
        ModuleId module, CauseCategory cause, Severity severity,
        AlertMessage alert, CancellationToken ct = default);

    Task<bool> TryCloseOnConsecutiveOkAsync(
        ModuleId module, CancellationToken ct = default);

    Task CloseManuallyAsync(
        Guid incidentId, string closedByRole, string comentario,
        CancellationToken ct = default);

    Task<Incident?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<PagedResult<Incident>> SearchHistoryAsync(
        IncidentSearchFilter filter, CancellationToken ct = default);

    Task<IReadOnlyList<WeeklySummaryEntry>> GetWeeklySummaryAsync(
        DateOnly weekStart, CancellationToken ct = default);

    Task<int> PurgeExpiredIncidentsAsync(CancellationToken ct = default);
}
