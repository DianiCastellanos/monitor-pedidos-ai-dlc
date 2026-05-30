using System.Collections.Concurrent;
using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Web.Services;

public sealed class LastCheckStore
{
    public ConcurrentDictionary<ModuleId, CheckResult> Results { get; } = new();
}
