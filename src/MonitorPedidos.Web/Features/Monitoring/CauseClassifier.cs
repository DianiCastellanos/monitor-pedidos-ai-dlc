using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Web.Features.Monitoring;

public static class CauseClassifier
{
    private static readonly IReadOnlyDictionary<Type, CauseCategory> _map =
        new Dictionary<Type, CauseCategory>
        {
            [typeof(DbOrderChecker)]  = CauseCategory.Bd,
            [typeof(DbHealthChecker)] = CauseCategory.Bd,
            [typeof(JobsChecker)]     = CauseCategory.Job,
            // U4 agrega: ApiChecker → CauseCategory.Api, TokenChecker → CauseCategory.Token
        };

    public static CauseCategory Classify(ICheckExecutor checker)
        => _map.TryGetValue(checker.GetType(), out var cause)
            ? cause
            : CauseCategory.NoDeterminada;
}
