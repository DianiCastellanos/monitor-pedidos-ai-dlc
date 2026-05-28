namespace MonitorPedidos.Domain.Monitoring;

public sealed record RetryAttempt(
    int            AttemptNumber,
    int            HttpStatusCode,
    int            LatencyMs,
    DateTimeOffset AttemptedAt);
