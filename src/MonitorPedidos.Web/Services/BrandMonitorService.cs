using MonitorPedidos.Domain.Dashboard;
using MonitorPedidos.Domain.Simulation;
using MonitorPedidos.Web.Features.Monitoring;

namespace MonitorPedidos.Web.Services;

public sealed class BrandMonitorService(
    IServiceScopeFactory         scopeFactory,
    ILogger<BrandMonitorService> logger) : IBrandMonitorService
{
    private static readonly string[] Sources = ["Salesforce", "Multivende"];
    private static readonly string[] Sites   = ["Patprimo", "SevenSeven", "Atmos", "Ostu"];

    public async Task<IReadOnlyList<BrandSnapshot>> GetCurrentSnapshotsAsync(CancellationToken ct = default)
    {
        // Scope fresco: AppDbContext limpio, independiente del estado del circuito Blazor.
        await using var scope = scopeFactory.CreateAsyncScope();
        var freshRepo         = scope.ServiceProvider.GetRequiredService<IBrandSnapshotRepository>();
        return await freshRepo.GetLatestPerSiteAsync(ct);
    }

    public async Task SimulateAndRefreshAsync(CancellationToken ct = default)
    {
        logger.LogInformation("[BrandMonitor] SimulateAndRefreshAsync — iniciando");

        await using var scope   = scopeFactory.CreateAsyncScope();
        var simRepo             = scope.ServiceProvider.GetRequiredService<ISimulatedOrderRepository>();
        var checker             = scope.ServiceProvider.GetRequiredService<BrandMonitorChecker>();

        // Insertar órdenes con conteo variado por marca para obtener datos no-cero en la ventana.
        // El conteo "anterior" sigue viniendo del snapshot histórico almacenado, no de estas órdenes.
        var orders = new List<SimulatedOrder>();
        foreach (var site in Sites)
        {
            var count = Random.Shared.Next(2, 16); // 2-15 órdenes por marca
            foreach (var source in Sources)
                for (var i = 0; i < count; i++)
                    orders.Add(SimulatedOrder.CreateNormal(source, site));
        }
        await simRepo.InsertRangeAsync(orders, ct);

        logger.LogInformation("[BrandMonitor] Órdenes simuladas insertadas — ejecutando checker");

        await checker.ExecuteAsync(ct);

        logger.LogInformation("[BrandMonitor] Checker completado");
    }
}
