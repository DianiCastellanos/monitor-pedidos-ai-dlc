namespace MonitorPedidos.Domain.Shared;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Skip,
    int Take
)
{
    public bool HasMore => Skip + Items.Count < TotalCount;
}
