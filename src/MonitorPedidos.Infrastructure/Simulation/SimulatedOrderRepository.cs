using Microsoft.EntityFrameworkCore;
using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Simulation;
using MonitorPedidos.Infrastructure.Persistence;

namespace MonitorPedidos.Infrastructure.Simulation;

// Implementa IOrderSource (leído por DbOrderChecker) e ISimulatedOrderRepository (escrito por OrdersSimulatorService)
public sealed class SimulatedOrderRepository(AppDbContext context) : IOrderSource, ISimulatedOrderRepository
{
    public async Task<IReadOnlyList<OrderSnapshot>> GetOrdersInWindowAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var fromUtc = from.UtcDateTime;
        var toUtc   = to.UtcDateTime;

        var rows = await context.SimulatedOrders
            .Where(o => o.CreatedAt >= fromUtc && o.CreatedAt <= toUtc)
            .Select(o => new OrderSnapshot(o.Source, new DateTimeOffset(o.CreatedAt, TimeSpan.Zero), o.Status == "Cancelled"))
            .ToListAsync(ct);

        return rows;
    }

    public async Task InsertRangeAsync(IEnumerable<SimulatedOrder> orders, CancellationToken ct = default)
    {
        context.SimulatedOrders.AddRange(orders);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default)
    {
        await context.SimulatedOrders
            .Where(o => o.CreatedAt < cutoff)
            .ExecuteDeleteAsync(ct);
    }

    public async Task<Dictionary<string, int>> CountBySiteAsync(
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        var rows = await context.SimulatedOrders
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to)
            .GroupBy(o => o.Site)
            .Select(g => new { Site = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return rows.ToDictionary(x => x.Site, x => x.Count);
    }
}
