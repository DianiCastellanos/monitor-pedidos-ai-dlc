namespace MonitorPedidos.Domain.Simulation;

public interface ISimulatedOrderRepository
{
    Task InsertRangeAsync(IEnumerable<SimulatedOrder> orders, CancellationToken ct = default);
    Task DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default);
    Task<Dictionary<string, int>> CountBySiteAsync(DateTime from, DateTime to, CancellationToken ct = default);
}
