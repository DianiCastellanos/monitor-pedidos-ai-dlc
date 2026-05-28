using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace MonitorPedidos.Web.Hubs;

[Authorize]
public sealed class AlertsHub : Hub { }
