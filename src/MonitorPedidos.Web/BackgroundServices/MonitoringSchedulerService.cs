using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Web.Services;

namespace MonitorPedidos.Web.BackgroundServices;

public sealed class MonitoringSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<MonitoringSchedulerService> _logger;

    public MonitoringSchedulerService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<MonitoringSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _config       = config;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalMinutes = _config.GetValue("Monitoring:CheckerIntervalMinutes", 5);
        _logger.LogInformation("MonitoringSchedulerService started — interval: {Min} min", intervalMinutes);

        // ADR-U3-03: PeriodicTimer eliminates drift accumulation
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunTickAsync(stoppingToken);
        }
    }

    private async Task RunTickAsync(CancellationToken stoppingToken)
    {
        using var scope  = _scopeFactory.CreateScope();
        // ADR-U3-02: IEnumerable<ICheckExecutor> — all registered checkers resolved automatically
        var checkers     = scope.ServiceProvider.GetRequiredService<IEnumerable<ICheckExecutor>>();
        var svc          = scope.ServiceProvider.GetRequiredService<IMonitoringService>();
        var timeoutMs    = _config.GetValue("Monitoring:CheckerTimeoutMs", 30_000);

        foreach (var checker in checkers)
        {
            // ADR-U3-01: per-checker linked token — timeout does not cancel other checkers or shutdown
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            cts.CancelAfter(timeoutMs);

            try
            {
                await svc.RunCheckAsync(checker, cts.Token);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning("Checker {Type} timed out after {Ms} ms",
                    checker.GetType().Name, timeoutMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected failure in checker {Type}", checker.GetType().Name);
            }
        }
    }
}
