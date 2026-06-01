using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Rules;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Web.Features.Monitoring;

public sealed class DbOrderChecker : ICheckExecutor
{
    public ModuleId Module => ModuleId.DbOrderChecker;

    private readonly IOrderSource   _orderSource;
    private readonly IRuleRepository _ruleRepo;
    private readonly ILogger<DbOrderChecker> _logger;

    public DbOrderChecker(IOrderSource orderSource, IRuleRepository ruleRepo, ILogger<DbOrderChecker> logger)
    {
        _orderSource = orderSource;
        _ruleRepo    = ruleRepo;
        _logger      = logger;
    }

    public async Task<CheckResult> ExecuteAsync(CancellationToken ct = default)
    {
        try
        {
        return await ExecuteInternalAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[DbOrderChecker] Sin acceso a BD: {Msg}", ex.Message);
            return CheckResult.Critical("Sin acceso a BD de pedidos");
        }
    }

    private async Task<CheckResult> ExecuteInternalAsync(CancellationToken ct)
    {
        var rules = await _ruleRepo.GetActiveByModuleAsync(ModuleId.DbOrderChecker, ct);

        int      windowMinutes;
        int      minOrders;
        string[] channels;

        if (rules.Any())
        {
            var condition  = rules[0].GetCondition();
            windowMinutes  = condition.WindowMinutes ?? (condition.WindowHours ?? 1) * 60;
            minOrders      = condition.MinOrders      ?? 1;
            channels       = condition.Channels       ?? ["SALESFORCE", "MULTIVENDE"];
        }
        else
        {
            windowMinutes = 10;
            minOrders     = 1;
            channels      = ["SALESFORCE", "MULTIVENDE"];
        }

        var to     = DateTimeOffset.UtcNow;
        var from   = to.AddMinutes(-windowMinutes);

        var orders = await _orderSource.GetOrdersInWindowAsync(from, to, ct);

        var channelCounts = orders
            .Where(o => !o.IsCancelled)
            .GroupBy(o => o.OrderId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var entries = new List<string>(channels.Length);
        var okCount = 0;

        foreach (var ch in channels)
        {
            var count  = channelCounts.GetValueOrDefault(ch, 0);
            var status = count >= minOrders ? "OK" : "SIN_PEDIDOS";

            entries.Add($"{ch}:{count}:{status}");
            if (count >= minOrders) okCount++;
        }

        var details = string.Join("|", entries);

        CheckResult result;

        if (okCount == channels.Length)
            result = CheckResult.Ok(details);
        else if (okCount == 0)
            result = CheckResult.Critical(details);
        else
            result = CheckResult.Warn(details);

        _logger.LogInformation(
            "[DbOrderChecker] Canales: {Details} (ventana={W}min, minOrders={M}, estado={S})",
            details, windowMinutes, minOrders, result.Status);

        return result;
    }
}

