namespace MonitorPedidos.Domain.Monitoring;

public interface IOrderSource
{
    Task<IReadOnlyList<OrderSnapshot>> GetOrdersInWindowAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default);
}
