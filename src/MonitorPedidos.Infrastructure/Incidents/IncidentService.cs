using Microsoft.Extensions.Configuration;
using MonitorPedidos.Domain.Alerts;
using MonitorPedidos.Domain.Incidents;
using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Notifications;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Infrastructure.Incidents;

public sealed class IncidentService(
    IIncidentRepository repo,
    INotificationService notifications,
    IConfiguration config) : IIncidentService
{
    public async Task<Incident> OpenIncidentAsync(
        ModuleId module, CauseCategory cause, Severity severity,
        AlertMessage alert, CancellationToken ct = default)
    {
        var existing = await repo.GetOpenByModuleAsync(module, ct);
        if (existing is not null)
        {
            existing.UpdateAlert(alert, severity);
            await repo.CloseAllByModuleExceptAsync(module, existing.Id, ct);
            await repo.SaveChangesAsync(ct);
            await notifications.NotifyIncidentAsync(existing.Id, ct);
            return existing;
        }

        var incident = Incident.Open(module, cause, severity, alert);
        await repo.AddAsync(incident, ct);
        await repo.SaveChangesAsync(ct);
        await notifications.NotifyIncidentAsync(incident.Id, ct);
        return incident;
    }

    public async Task<bool> TryCloseOnConsecutiveOkAsync(
        ModuleId module, CancellationToken ct = default)
    {
        var incident = await repo.GetOpenByModuleAsync(module, ct);
        if (incident is null) return false;

        incident.CloseAutomatically();
        await repo.SaveChangesAsync(ct);
        await notifications.NotifyStatusChangeAsync(module, CheckStatus.Ok, ct);
        return true;
    }

    public async Task CloseManuallyAsync(
        Guid incidentId, string closedByRole, string comentario,
        CancellationToken ct = default)
    {
        var incident = await repo.GetByIdAsync(incidentId, ct)
            ?? throw new KeyNotFoundException($"Incidente {incidentId} no encontrado.");

        incident.CloseManually(closedByRole, comentario);
        await repo.SaveChangesAsync(ct);
        await notifications.NotifyIncidentAsync(incidentId, ct);
    }

    public async Task<Incident?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await repo.GetByIdAsync(id, ct);

    public async Task<PagedResult<Incident>> SearchHistoryAsync(
        IncidentSearchFilter filter, CancellationToken ct = default)
        => await repo.SearchAsync(filter.WithValidatedTake(), ct);

    public async Task<IReadOnlyList<WeeklySummaryEntry>> GetWeeklySummaryAsync(
        DateOnly weekStart, CancellationToken ct = default)
        => await repo.GetWeeklySummaryAsync(weekStart, ct);

    public async Task<int> PurgeExpiredIncidentsAsync(CancellationToken ct = default)
    {
        var retentionDays = config.GetValue<int>("Incidents:RetentionDays", 90);
        return await repo.PurgeExpiredAsync(retentionDays, ct);
    }
}
