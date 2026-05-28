using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Web.Features.ApiChecks;

public sealed class MultivendeApiChecker : ICheckExecutor
{
    public ModuleId Module => ModuleId.MultivendeApi;

    private readonly IMultivendeClient _client;
    private readonly ILogger<MultivendeApiChecker> _logger;

    public MultivendeApiChecker(IMultivendeClient client, ILogger<MultivendeApiChecker> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<CheckResult> ExecuteAsync(CancellationToken ct = default)
    {
        var ping = await _client.PingOrdersAsync(ct);

        var result = ping switch
        {
            { IsSuccess: true }      => CheckResult.Ok($"Multivende OK — {ping.LatencyMs} ms"),
            { IsUnauthorized: true } => CheckResult.Critical("Multivende HTTP 401 — token inválido"),
            _                        => CheckResult.Critical($"Multivende {ping.Details}")
        };

        _logger.LogDebug("MultivendeApiChecker: {Status} — {Details}", result.Status, result.Details);
        return result;
    }
}
