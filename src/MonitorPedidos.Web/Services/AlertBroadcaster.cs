using MonitorPedidos.Domain.Alerts;

namespace MonitorPedidos.Web.Services;

public sealed class AlertBroadcaster
{
    public event Func<AlertMessage, Task>? OnAlert;
    public event Func<Guid, Task>?         OnIncidentClosed;

    public async Task BroadcastAsync(AlertMessage alert)
    {
        var handler = OnAlert; // captura atómica — ADR-U6-02
        if (handler is not null)
            await handler.Invoke(alert);
    }

    public async Task BroadcastCloseAsync(Guid incidentId)
    {
        var handler = OnIncidentClosed;
        if (handler is not null)
            await handler.Invoke(incidentId);
    }
}
