using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Domain.Monitoring;

public sealed record CheckResult(
    ModuleId Module,
    CheckStatus Status,
    string? Detail,
    DateTimeOffset CheckedAt
);
