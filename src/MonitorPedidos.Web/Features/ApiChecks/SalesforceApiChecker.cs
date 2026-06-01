using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Web.Features.ApiChecks;

public sealed class SalesforceApiChecker(
    ISalesforceClient              client,
    ILogger<SalesforceApiChecker>  logger) : ICheckExecutor
{
    private static readonly string[] ColombianSites = ["PatPrimo", "SevenSeven", "Ostu", "Atmos"];

    public ModuleId Module => ModuleId.SalesforceApi;

    public async Task<CheckResult> ExecuteAsync(CancellationToken ct = default)
    {
        var outcome = await client.SearchPendingOrdersAsync(ct);

        if (!outcome.IsSuccess)
        {
            var errorMsg = outcome.IsUnauthorized
                ? $"HTTP 401 — token inválido o expirado"   // "401" activa BR-TOKEN-01 en MonitoringService
                : outcome.IsTimeout
                    ? "Sin respuesta — timeout al conectar con Salesforce"
                    : $"Error de conexión — {outcome.ErrorDetails}";

            logger.LogWarning("[SalesforceApiChecker] {Error}", errorMsg);
            return CheckResult.Critical(errorMsg);
        }

        // Contar pedidos pendientes por site Colombia
        var bySite = ColombianSites.ToDictionary(
            s => s,
            s => outcome.Items.Count(i => string.Equals(i.SiteId, s, StringComparison.OrdinalIgnoreCase)));

        var total   = bySite.Values.Sum();
        var details = string.Join("|", bySite.Select(kv =>
            $"{kv.Key}:{kv.Value}:{(kv.Value == 0 ? "OK" : "WARN")}"));

        logger.LogInformation("[SalesforceApiChecker] total={Total} details={Details}", total, details);

        return CheckResult.Ok(details);
    }
}
