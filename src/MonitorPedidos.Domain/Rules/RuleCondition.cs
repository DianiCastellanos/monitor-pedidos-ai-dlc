using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Domain.Rules;

public sealed record RuleCondition(
    int?  WindowHours,
    int?  MinOrders,
    int?  LatencyWarnMs,
    int?  LatencyCriticalMs,
    int?  PendingDropThreshold
)
{
    public static RuleCondition ForDbOrders(int windowHours, int minOrders)
        => new(windowHours, minOrders, null, null, null);

    public static RuleCondition ForDbHealth(int latencyWarnMs, int latencyCriticalMs)
        => new(null, null, latencyWarnMs, latencyCriticalMs, null);

    public static RuleCondition ForJobs()
        => new(null, null, null, null, null);

    public static RuleCondition ForBrandMonitor(int pendingDropThreshold)
        => new(null, null, null, null, pendingDropThreshold);

    public bool IsValidForModule(ModuleId module) => module switch
    {
        ModuleId.DbOrderChecker  => WindowHours.HasValue && MinOrders.HasValue,
        ModuleId.DbHealthChecker => LatencyWarnMs.HasValue && LatencyCriticalMs.HasValue,
        ModuleId.JobsMonitor     => true,
        ModuleId.BrandMonitor    => PendingDropThreshold.HasValue,
        _                        => false
    };
}
