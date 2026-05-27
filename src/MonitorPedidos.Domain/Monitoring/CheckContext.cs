using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Domain.Monitoring;

public sealed record CheckContext(
    ModuleId Module,
    CheckStatus Status,
    CauseCategory Cause,
    string CheckDetails,
    DateTimeOffset DetectedAt);
