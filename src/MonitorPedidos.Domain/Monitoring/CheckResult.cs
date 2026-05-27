namespace MonitorPedidos.Domain.Monitoring;

public sealed record CheckResult(
    CheckStatus Status,
    string Details,
    DateTimeOffset CheckedAt)
{
    public static CheckResult Ok(string details = "")
        => new(CheckStatus.Ok, details, DateTimeOffset.UtcNow);

    public static CheckResult Warn(string details)
        => new(CheckStatus.Warn, details, DateTimeOffset.UtcNow);

    public static CheckResult Critical(string details)
        => new(CheckStatus.Critical, details, DateTimeOffset.UtcNow);

    public bool RequiresIncident => Status is CheckStatus.Warn or CheckStatus.Critical;
}
