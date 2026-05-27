namespace MonitorPedidos.Domain.Monitoring;

public sealed record OrderSnapshot(
    string OrderId,
    DateTimeOffset CreatedAt,
    bool IsCancelled);
