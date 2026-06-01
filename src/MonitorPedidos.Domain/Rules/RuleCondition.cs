using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Domain.Rules;

public sealed record RuleCondition(
    int?     WindowHours,
    int?     MinOrders,
    int?     LatencyWarnMs,
    int?     LatencyCriticalMs,
    int?     PendingDropThreshold,
    int?     PollIntervalSeconds         = null,
    int?     SnapshotMinIntervalSeconds  = null,
    int?     ComparisonWindowSeconds     = null,
    int?     WindowMinutes = null,
    string[]? Channels      = null
)
{
    public static RuleCondition ForDbOrders(int windowMinutes, int minOrders, string[]? channels = null)
        => new(null, minOrders, null, null, null, null, null, null, windowMinutes, channels);

    public static RuleCondition ForDbHealth(int latencyWarnMs, int latencyCriticalMs)
        => new(null, null, latencyWarnMs, latencyCriticalMs, null);

    public static RuleCondition ForJobs()
        => new(null, null, null, null, null);

    public static RuleCondition ForBrandMonitor(
        int pendingDropThreshold,
        int? pollIntervalSeconds = null,
        int? snapshotMinIntervalSeconds = null,
        int? comparisonWindowSeconds = null)
        => new(null, null, null, null, pendingDropThreshold,
               pollIntervalSeconds, snapshotMinIntervalSeconds, comparisonWindowSeconds);

    public bool IsValidForModule(ModuleId module) => module switch
    {
        ModuleId.DbOrderChecker  => MinOrders.HasValue && Channels is { Length: > 0 },
        ModuleId.DbHealthChecker => LatencyWarnMs.HasValue && LatencyCriticalMs.HasValue,
        ModuleId.JobsMonitor     => true,
        ModuleId.BrandMonitor    => PendingDropThreshold.HasValue,
        _                        => false
    };
}
