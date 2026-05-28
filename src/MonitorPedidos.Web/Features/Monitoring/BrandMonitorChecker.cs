using Microsoft.Extensions.Configuration;
using MonitorPedidos.Domain.Dashboard;
using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Shared;
using MonitorPedidos.Domain.Simulation;

namespace MonitorPedidos.Web.Features.Monitoring;

public sealed class BrandMonitorChecker(
    ISimulatedOrderRepository    simRepo,
    IBrandSnapshotRepository     snapshotRepo,
    IConfiguration               config,
    ILogger<BrandMonitorChecker> logger) : ICheckExecutor
{
    public ModuleId Module => ModuleId.BrandMonitor;

    public async Task<CheckResult> ExecuteAsync(CancellationToken ct = default)
    {
        var windowSeconds = config.GetValue("Monitoring:BrandMonitorWindowSeconds", 600);
        var window        = TimeSpan.FromSeconds(windowSeconds);
        var now           = DateTime.UtcNow;

        logger.LogInformation(
            "[BrandMonitorChecker] ExecuteAsync iniciado — ventana={W}s now={Now:HH:mm:ss.fff}",
            windowSeconds, now);

        var currentCounts = await simRepo.CountBySiteAsync(now - window, now, ct);

        logger.LogInformation(
            "[BrandMonitorChecker] Órdenes contadas en ventana: {Counts}",
            string.Join(", ", currentCounts.Select(kv => $"{kv.Key}={kv.Value}")));

        foreach (var site in BrandSnapshot.Sites)
        {
            var currentCount = currentCounts.TryGetValue(site, out var c) ? c : 0;

            var previousSnapshot = await snapshotRepo.GetSnapshotBeforeAsync(
                site, now - window, ct);

            SnapshotStatus status;
            int?           previousCount;

            if (previousSnapshot is null)
            {
                status        = SnapshotStatus.NoData;
                previousCount = null;
            }
            else
            {
                previousCount = previousSnapshot.PendingCountCurrent;
                status        = DetermineStatus(currentCount, previousCount.Value);
            }

            logger.LogInformation(
                "[BrandMonitorChecker] site={Site} actual={A} anterior={P} status={S} — llamando InsertAsync",
                site, currentCount, previousCount?.ToString() ?? "N/D", status);

            var snapshot = BrandSnapshot.Create(site, currentCount, previousCount, status);
            await snapshotRepo.InsertAsync(snapshot, ct);

            logger.LogInformation("[BrandMonitorChecker] site={Site} — InsertAsync completado", site);
        }

        logger.LogInformation("[BrandMonitorChecker] ExecuteAsync finalizado — {N} sites procesados", BrandSnapshot.Sites.Length);

        return CheckResult.Ok($"Brand monitor actualizado — {BrandSnapshot.Sites.Length} sites (ventana {windowSeconds}s).");
    }

    private static SnapshotStatus DetermineStatus(int current, int previous)
    {
        if (current < previous) return SnapshotStatus.Green;
        if (current == previous) return SnapshotStatus.Yellow;
        return SnapshotStatus.Red;
    }
}
