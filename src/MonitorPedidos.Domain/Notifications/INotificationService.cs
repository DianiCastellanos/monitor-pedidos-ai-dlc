using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Domain.Notifications;

public interface INotificationService
{
    Task NotifyIncidentAsync(Guid incidentId, CancellationToken ct = default);
    Task NotifyStatusChangeAsync(ModuleId module, CheckStatus status, CancellationToken ct = default);
}
