using Microsoft.Extensions.Options;
using MonitorPedidos.Domain.Simulation;

namespace MonitorPedidos.Web.BackgroundServices;

public sealed class OrdersSimulatorService : BackgroundService
{
    private static readonly string[] Sources = ["Salesforce", "Multivende"];
    private static readonly string[] Sites   = ["Patprimo", "SevenSeven", "Atmos", "Ostu"];

    private readonly IServiceScopeFactory           _scopeFactory;
    private readonly IOptions<SimulationOptions>    _options;
    private readonly ILogger<OrdersSimulatorService> _logger;

    public OrdersSimulatorService(
        IServiceScopeFactory            scopeFactory,
        IOptions<SimulationOptions>     options,
        ILogger<OrdersSimulatorService> logger)
    {
        _scopeFactory = scopeFactory;
        _options      = options;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogInformation("Simulador desactivado (Simulation:Enabled=false)");
            return;
        }

        _logger.LogInformation("OrdersSimulatorService arrancando — intervalo {Min} min", _options.Value.InsertIntervalMinutes);

        // Limpieza periódica cada 24 h (en un timer independiente)
        _ = RunCleanupLoopAsync(stoppingToken);

        var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.Value.InsertIntervalMinutes));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var repo = scope.ServiceProvider.GetRequiredService<ISimulatedOrderRepository>();
                await RunOneTickAsync(repo, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                throw; // permite shutdown limpio — ADR-U7-03
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en tick del simulador — reintento en próximo ciclo");
            }
        }
    }

    // internal: accesible desde tests via InternalsVisibleTo — ADR-U7-02
    internal async Task RunOneTickAsync(ISimulatedOrderRepository repo, CancellationToken ct)
    {
        var opts = _options.Value;

        if (opts.NoOrdersMode)
        {
            _logger.LogDebug("Simulador: NoOrdersMode=true — tick omitido");
            return;
        }

        var orders = new List<SimulatedOrder>(Sources.Length * Sites.Length);

        foreach (var source in Sources)
        {
            foreach (var site in Sites)
            {
                var order = Random.Shared.NextDouble() < opts.FailureProbability
                    ? SimulatedOrder.CreateFailure(source, site, "Error")
                    : SimulatedOrder.CreateNormal(source, site);

                orders.Add(order);
            }
        }

        await repo.InsertRangeAsync(orders, ct);
        _logger.LogDebug("Simulador: {Count} pedidos insertados", orders.Count);
    }

    private async Task RunCleanupLoopAsync(CancellationToken ct)
    {
        var cleanupTimer = new PeriodicTimer(TimeSpan.FromHours(24));

        while (await cleanupTimer.WaitForNextTickAsync(ct))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var repo    = scope.ServiceProvider.GetRequiredService<ISimulatedOrderRepository>();
                var cutoff  = DateTime.UtcNow.AddHours(-48);
                await repo.DeleteOlderThanAsync(cutoff, ct);
                _logger.LogInformation("Simulador: limpieza de simulated_orders (cutoff={Cutoff})", cutoff);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error en limpieza del simulador");
            }
        }
    }
}
