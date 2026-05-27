using MonitorPedidos.Domain.Monitoring;

namespace MonitorPedidos.Web.Features.Monitoring;

// Stub temporal — reemplazado por U7 con SimulatedOrderRepository + tabla simulated_orders.
public sealed class SimulatedOrderRepository : IOrderSource
{
    public Task<IReadOnlyList<OrderSnapshot>> GetOrdersInWindowAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        IReadOnlyList<OrderSnapshot> orders =
        [
            new OrderSnapshot("SIM-001", DateTimeOffset.UtcNow.AddMinutes(-10), false),
            new OrderSnapshot("SIM-002", DateTimeOffset.UtcNow.AddMinutes(-5),  false),
        ];
        return Task.FromResult(orders);
    }
}
