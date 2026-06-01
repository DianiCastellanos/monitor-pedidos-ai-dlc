namespace MonitorPedidos.Domain.Monitoring;

public sealed record SalesforceSearchOutcome
{
    public bool   IsSuccess      { get; init; }
    public bool   IsUnauthorized { get; init; }
    public bool   IsTimeout      { get; init; }
    public int    Total          { get; init; }
    public IReadOnlyList<SalesforceOrderItem> Items { get; init; } = [];
    public string ErrorDetails   { get; init; } = "";

    public static SalesforceSearchOutcome Success(int total, IReadOnlyList<SalesforceOrderItem> items)
        => new() { IsSuccess = true, Total = total, Items = items };

    public static SalesforceSearchOutcome Unauthorized()
        => new() { IsUnauthorized = true, ErrorDetails = "HTTP 401 — token inválido o expirado" };

    public static SalesforceSearchOutcome Timeout()
        => new() { IsTimeout = true, ErrorDetails = "Timeout — API no respondió en el tiempo límite" };

    public static SalesforceSearchOutcome Failure(string details)
        => new() { ErrorDetails = details };
}

public record SalesforceOrderItem(
    string         OrderNo,
    string         SiteId,
    DateTimeOffset CreationDate);
