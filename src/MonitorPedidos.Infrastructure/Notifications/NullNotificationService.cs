using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Notifications;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Infrastructure.Notifications;

public sealed class NullNotificationService : INotificationService
{
    public Task NotifyIncidentAsync(Guid incidentId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task NotifyStatusChangeAsync(ModuleId module, CheckStatus status, CancellationToken ct = default)
        => Task.CompletedTask;
}
