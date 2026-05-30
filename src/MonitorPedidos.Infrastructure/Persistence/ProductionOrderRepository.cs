using Dapper;
using Microsoft.Data.SqlClient;
using MonitorPedidos.Domain.Monitoring;

namespace MonitorPedidos.Infrastructure.Persistence;

public sealed class ProductionOrderRepository : IOrderSource
{
    private readonly string _connectionString;

    public ProductionOrderRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<OrderSnapshot>> GetOrdersInWindowAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        var rows = await conn.QueryAsync<OrderRow>(
            "SELECT ChannelName, FechaGeneracion FROM oc_encabezado WITH (NOLOCK) WHERE FechaGeneracion >= @From AND FechaGeneracion <= @To",
            new { From = from.LocalDateTime, To = to.LocalDateTime },
            commandTimeout: 30);

        var localOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
        return rows
            .Select(r => new OrderSnapshot(r.ChannelName, new DateTimeOffset(r.FechaGeneracion, localOffset), false))
            .ToList();
    }

    private sealed record OrderRow(string ChannelName, DateTime FechaGeneracion);
}
