using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Web.Features.Monitoring;

public sealed class DbOrderChecker : ICheckExecutor
{
    public ModuleId Module => ModuleId.DbOrderChecker;

    private readonly IOrderSource _orderSource;
    private readonly IConfiguration _config;

    public DbOrderChecker(IOrderSource orderSource, IConfiguration config)
    {
        _orderSource = orderSource;
        _config      = config;
    }

    private TimeSpan DetectionWindow =>
        TimeSpan.FromMinutes(_config.GetValue("Monitoring:OrderDetectionWindowMinutes", 30));

    public async Task<CheckResult> ExecuteAsync(CancellationToken ct = default)
    {
        var to     = DateTimeOffset.UtcNow;
        var from   = to - DetectionWindow;
        var orders = await _orderSource.GetOrdersInWindowAsync(from, to, ct);

        var active = orders.Where(o => !o.IsCancelled).ToList();

        return active.Count == 0
            ? CheckResult.Critical($"Sin pedidos activos en últimos {DetectionWindow.TotalMinutes} min.")
            : CheckResult.Ok($"{active.Count} pedido(s) activos en ventana.");
    }
}
