using Microsoft.AspNetCore.SignalR;
using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Notifications;
using MonitorPedidos.Domain.Shared;
using MonitorPedidos.Web.Hubs;

namespace MonitorPedidos.Web.Services;

public sealed class NotificationService : INotificationService
{
    private readonly AlertBroadcaster              _broadcaster;
    private readonly IHubContext<AlertsHub>        _hub;
    private readonly ILogger<NotificationService>  _logger;

    public NotificationService(
        AlertBroadcaster              broadcaster,
        IHubContext<AlertsHub>        hub,
        ILogger<NotificationService>  logger)
    {
        _broadcaster = broadcaster;
        _hub         = hub;
        _logger      = logger;
    }

    // BR-NOTIF-01: ambos canales siempre — error en uno no bloquea el otro
    public async Task NotifyIncidentAsync(Guid incidentId, CancellationToken ct = default)
    {
        try { await _broadcaster.BroadcastCloseAsync(incidentId); }
        catch (Exception ex) { _logger.LogWarning(ex, "AlertBroadcaster error on incident {Id}", incidentId); }

        try { await _hub.Clients.All.SendAsync("IncidentUpdated", incidentId, ct); }
        catch (Exception ex) { _logger.LogWarning(ex, "SignalR error on incident {Id}", incidentId); }
    }

    public async Task NotifyStatusChangeAsync(ModuleId module, CheckStatus status, CancellationToken ct = default)
    {
        var payload = new { Module = module.ToString(), Status = status.ToString() };

        try { await _hub.Clients.All.SendAsync("SystemStatusUpdated", payload, ct); }
        catch (Exception ex) { _logger.LogWarning(ex, "SignalR error on status change {Module}", module); }
    }
}
