using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Shared;
using MonitorPedidos.Web.Features.ApiChecks;

namespace MonitorPedidos.Web.Features.Monitoring;

public static class CauseClassifier
{
    private static readonly IReadOnlyDictionary<Type, CauseCategory> _map =
        new Dictionary<Type, CauseCategory>
        {
            [typeof(DbOrderChecker)]       = CauseCategory.Bd,
            [typeof(DbHealthChecker)]      = CauseCategory.Bd,
            [typeof(JobsChecker)]          = CauseCategory.Job,
            [typeof(SalesforceApiChecker)] = CauseCategory.Api,
            [typeof(MultivendeApiChecker)] = CauseCategory.Api,
        };

    public static CauseCategory Classify(ICheckExecutor checker)
        => _map.TryGetValue(checker.GetType(), out var cause)
            ? cause
            : CauseCategory.NoDeterminada;
}
