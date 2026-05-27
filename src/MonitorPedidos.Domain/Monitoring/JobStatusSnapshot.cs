namespace MonitorPedidos.Domain.Monitoring;

public sealed record JobStatusSnapshot(
    string JobId,
    string JobName,
    bool IsRunning,
    DateTimeOffset? LastExecutedAt,
    bool LastExecutionSucceeded);
