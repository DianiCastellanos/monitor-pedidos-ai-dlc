using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Rules;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Web.Features.Monitoring;

public sealed class DbOrderChecker : ICheckExecutor
{
    public ModuleId Module => ModuleId.DbOrderChecker;

    private readonly IOrderSource   _orderSource;
    private readonly IRuleRepository _ruleRepo;

    public DbOrderChecker(IOrderSource orderSource, IRuleRepository ruleRepo)
    {
        _orderSource = orderSource;
        _ruleRepo    = ruleRepo;
    }

    public async Task<CheckResult> ExecuteAsync(CancellationToken ct = default)
    {
        var rules = await _ruleRepo.GetActiveByModuleAsync(ModuleId.DbOrderChecker, ct);

        if (!rules.Any())
            return CheckResult.Ok("Sin reglas activas para DbOrderChecker — chequeo omitido.");

        var condition   = rules[0].GetCondition();
        var windowHours = condition.WindowHours   ?? 2;
        var minOrders   = condition.MinOrders      ?? 1;

        var to     = DateTimeOffset.UtcNow;
        var from   = to.AddHours(-windowHours);
        var orders = await _orderSource.GetOrdersInWindowAsync(from, to, ct);

        var active = orders.Where(o => !o.IsCancelled).ToList();

        if (active.Count == 0)
            return CheckResult.Critical($"Sin pedidos activos en las últimas {windowHours}h (mínimo esperado: {minOrders}).");

        if (active.Count < minOrders)
            return CheckResult.Critical($"Solo {active.Count} pedido(s) en {windowHours}h, se esperaban al menos {minOrders}.");

        return CheckResult.Ok($"{active.Count} pedido(s) activos en las últimas {windowHours}h.");
    }
}
