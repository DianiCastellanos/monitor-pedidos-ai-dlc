using MonitorPedidos.Domain.Dashboard;
using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Shared;
using MonitorPedidos.Domain.Simulation;

namespace MonitorPedidos.Web.Features.Monitoring;

/// <summary>
/// Monitorea el FLUJO de pedidos por marca.
/// Detecta si el backlog crece (descarga detenida) o baja (descarga normal).
/// </summary>
public sealed class BrandMonitorChecker(
    ISimulatedOrderRepository    simRepo,
    IBrandSnapshotRepository     snapshotRepo,
    ILogger<BrandMonitorChecker> logger) : ICheckExecutor
{
    public ModuleId Module => ModuleId.BrandMonitor;

    public async Task<CheckResult> ExecuteAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // Ventana actual: pedidos de los últimos 10 minutos
        var actual   = await simRepo.CountBySiteAsync(now.AddMinutes(-10), now, ct);

        // Ventana anterior: pedidos de hace 10-20 minutos
        var anterior = await simRepo.CountBySiteAsync(now.AddMinutes(-20), now.AddMinutes(-10), ct);

        foreach (var site in BrandSnapshot.Sites)
        {
            var countActual   = actual.TryGetValue(site, out var a) ? a : 0;
            var countAnterior = anterior.TryGetValue(site, out var p) ? p : 0;

            // Lógica de flujo:
            // actual < anterior → backlog baja  → OK   (descarga funcionando)
            // actual == anterior → sin cambio   → WARN (posible problema)
            // actual > anterior  → backlog crece → CRITICAL (descarga detenida)
            var status = countActual < countAnterior
                ? SnapshotStatus.Green
                : countActual == countAnterior
                    ? SnapshotStatus.Yellow
                    : SnapshotStatus.Red;

            var snapshot = BrandSnapshot.Create(site, countActual, countAnterior, status);
            await snapshotRepo.UpsertAsync(snapshot, ct);

            logger.LogDebug(
                "BrandMonitor site={Site} actual={A} anterior={P} status={S}",
                site, countActual, countAnterior, status);
        }

        return CheckResult.Ok($"Brand monitor actualizado — {BrandSnapshot.Sites.Length} sites.");
    }
}
