using System.Text.Json;
using MonitorPedidos.Domain.Dashboard;
using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Rules;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Web.Features.Monitoring;

public sealed class BrandMonitorChecker(
    ISalesforceClient            salesforceClient,
    IBrandSnapshotRepository     snapshotRepo,
    IRuleRepository              ruleRepo,
    ILogger<BrandMonitorChecker> logger) : ICheckExecutor
{
    public ModuleId Module => ModuleId.BrandMonitor;

    private static readonly JsonSerializerOptions _jsonOpts = new(JsonSerializerDefaults.Web);

    public async Task<CheckResult> ExecuteAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // Load BrandMonitor rule condition
        BrandMonitorSettings condition;
        bool ruleFound;
        try { (condition, ruleFound) = await LoadRuleConditionAsync(ct); }
        catch { (condition, ruleFound) = (new BrandMonitorSettings(), false); }

        var snapshotMinInterval = TimeSpan.FromSeconds(condition.SnapshotMinIntervalSeconds ?? 60);
        var comparisonWindow    = TimeSpan.FromSeconds(condition.ComparisonWindowSeconds ?? 600);

        logger.LogInformation(
            "[BrandMonitorChecker] Execute iniciado — snapshotMinInterval={S}s comparisonWindow={C}s ruleFound={R}",
            snapshotMinInterval.TotalSeconds, comparisonWindow.TotalSeconds, ruleFound);

        // Consultar Salesforce
        var outcome = await salesforceClient.SearchPendingOrdersAsync(ct);

        if (!outcome.IsSuccess)
        {
            var reason = outcome.IsUnauthorized ? "HTTP 401 — token inválido"
                       : outcome.IsTimeout      ? "Timeout"
                       : outcome.ErrorDetails;
            logger.LogWarning("[BrandMonitorChecker] Salesforce no disponible — {Reason}", reason);
            return CheckResult.Warn($"Brand Monitor — Salesforce: {reason}");
        }

        // Contar pedidos por site
        var currentCounts = BrandSnapshot.Sites.ToDictionary(
            site => site,
            site => outcome.Items.Count(i => string.Equals(i.SiteId, site, StringComparison.OrdinalIgnoreCase)));

        logger.LogInformation(
            "[BrandMonitorChecker] Conteos Salesforce: {Counts}",
            string.Join(", ", currentCounts.Select(kv => $"{kv.Key}={kv.Value}")));

        foreach (var site in BrandSnapshot.Sites)
        {
            var currentCount = currentCounts[site];

            // Decidir si guardar snapshot
            BrandSnapshot? latestSnapshot = null;
            try { latestSnapshot = await snapshotRepo.GetLatestAsync(site, ct); }
            catch (Exception ex) { logger.LogWarning("[BrandMonitorChecker] Sin acceso a latest para {Site}: {Msg}", site, ex.Message); }
            var shouldPersist = ShouldPersistSnapshot(latestSnapshot, currentCount, snapshotMinInterval, now);

            // Obtener snapshot para comparación de tendencia (hace comparisonWindow)
            BrandSnapshot? comparisonSnapshot = null;
            try { comparisonSnapshot = await snapshotRepo.GetSnapshotBeforeAsync(site, now - comparisonWindow, ct); }
            catch (Exception ex) { logger.LogWarning("[BrandMonitorChecker] Sin acceso a histórico para {Site}: {Msg}", site, ex.Message); }

            SnapshotStatus status;
            int?           previousCount;

            if (comparisonSnapshot is null)
            {
                status        = SnapshotStatus.NoData;
                previousCount = null;
            }
            else
            {
                previousCount = comparisonSnapshot.PendingCountCurrent;
                status        = DetermineStatus(currentCount, previousCount.Value);
            }

            if (shouldPersist)
            {
                logger.LogInformation(
                    "[BrandMonitorChecker] site={Site} actual={A} anterior={P} status={S} — guardando snapshot",
                    site, currentCount, previousCount?.ToString() ?? "N/D", status);

                var snapshot = BrandSnapshot.Create(site, currentCount, previousCount, status);
                try { await snapshotRepo.InsertAsync(snapshot, ct); }
                catch (Exception ex) { logger.LogWarning("[BrandMonitorChecker] No se pudo persistir {Site}: {Msg}", site, ex.Message); }
            }
            else
            {
                logger.LogInformation(
                    "[BrandMonitorChecker] site={Site} actual={A} status={S} — sin cambios, snapshot omitido",
                    site, currentCount, status);
            }
        }

        logger.LogInformation(
            "[BrandMonitorChecker] Execute finalizado — {N} sites procesados",
            BrandSnapshot.Sites.Length);

        // Details en formato "Site:Count" pipe-separado para que el Dashboard pueda leerlo desde LastCheckStore
        var details = string.Join("|", currentCounts.Select(kv => $"{kv.Key}:{kv.Value}"));
        return CheckResult.Ok(details);
    }

    private bool ShouldPersistSnapshot(
        BrandSnapshot? latest, int currentCount, TimeSpan minInterval, DateTime now)
    {
        // No existe snapshot previo -> siempre guardar
        if (latest is null)
            return true;

        // El valor de pendientes cambió -> guardar
        if (latest.PendingCountCurrent != currentCount)
            return true;

        // Pasó el intervalo mínimo desde el último snapshot -> guardar
        if (now - latest.CheckedAt >= minInterval)
            return true;

        return false;
    }

    private static SnapshotStatus DetermineStatus(int current, int previous)
    {
        if (current < previous) return SnapshotStatus.Green;
        if (current == previous) return SnapshotStatus.Yellow;
        return SnapshotStatus.Red;
    }

    private async Task<(BrandMonitorSettings Settings, bool RuleFound)> LoadRuleConditionAsync(CancellationToken ct)
    {
        var rules = await ruleRepo.GetActiveByModuleAsync(ModuleId.BrandMonitor, ct);

        if (rules.Count == 0)
            return (new BrandMonitorSettings(), false);

        var condition = JsonSerializer.Deserialize<BrandMonitorSettings>(
            rules[0].ConditionJson, _jsonOpts);

        return (condition ?? new BrandMonitorSettings(), true);
    }

    private sealed record BrandMonitorSettings
    {
        public int? PendingDropThreshold        { get; init; }
        public int? PollIntervalSeconds         { get; init; }
        public int? SnapshotMinIntervalSeconds  { get; init; }
        public int? ComparisonWindowSeconds     { get; init; }
    }
}
