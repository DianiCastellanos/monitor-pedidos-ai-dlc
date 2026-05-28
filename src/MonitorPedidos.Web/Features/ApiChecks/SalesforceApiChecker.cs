using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Web.Features.ApiChecks;

public sealed class SalesforceApiChecker : ICheckExecutor
{
    public ModuleId Module => ModuleId.SalesforceApi;

    private readonly ISalesforceClient _client;
    private readonly ILogger<SalesforceApiChecker> _logger;

    public SalesforceApiChecker(ISalesforceClient client, ILogger<SalesforceApiChecker> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<CheckResult> ExecuteAsync(CancellationToken ct = default)
    {
        var ping = await _client.PingOrdersAsync(ct);

        var result = ping switch
        {
            { IsSuccess: true }      => CheckResult.Ok($"Salesforce OK — {ping.LatencyMs} ms"),
            { IsUnauthorized: true } => CheckResult.Critical("Salesforce HTTP 401 — token inválido"),
            _                        => CheckResult.Critical($"Salesforce {ping.Details}")
        };

        _logger.LogDebug("SalesforceApiChecker: {Status} — {Details}", result.Status, result.Details);
        return result;
    }
}
