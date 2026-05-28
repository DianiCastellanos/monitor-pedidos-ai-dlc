using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Web.Features.ApiChecks;
using MonitorPedidos.Web.Features.Monitoring;
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
        var monitorMin = _config.GetValue("Monitoring:CheckerIntervalMinutes",    5);
        var apiMin     = _config.GetValue("Monitoring:ApiCheckerIntervalMinutes", 10);

        _logger.LogInformation(
            "MonitoringSchedulerService started — monitor: {M} min, api: {A} min",
            monitorMin, apiMin);

        // ADR-U3-03: PeriodicTimer elimina deriva acumulada
        using var monitorTimer = new PeriodicTimer(TimeSpan.FromMinutes(monitorMin));
        using var apiTimer     = new PeriodicTimer(TimeSpan.FromMinutes(apiMin));

        // ADR-U4-03: dos loops independientes con cadencias distintas
        await Task.WhenAll(
            RunTimerLoopAsync(monitorTimer, IsMonitorChecker, stoppingToken),
            RunTimerLoopAsync(apiTimer,     IsApiChecker,     stoppingToken));
    }

    private static bool IsMonitorChecker(ICheckExecutor c) =>
        c is DbOrderChecker or DbHealthChecker or JobsChecker;

    private static bool IsApiChecker(ICheckExecutor c) =>
        c is SalesforceApiChecker or MultivendeApiChecker or BrandMonitorChecker;

    private async Task RunTimerLoopAsync(
        PeriodicTimer timer,
        Func<ICheckExecutor, bool> filter,
        CancellationToken stoppingToken)
    {
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            using var scope  = _scopeFactory.CreateScope();
            // ADR-U3-02: IEnumerable<ICheckExecutor> resuelve todos los registrados
            var checkers     = scope.ServiceProvider
                                    .GetRequiredService<IEnumerable<ICheckExecutor>>()
                                    .Where(filter);
            var svc          = scope.ServiceProvider.GetRequiredService<IMonitoringService>();
            var timeoutMs    = _config.GetValue("Monitoring:CheckerTimeoutMs", 30_000);

            foreach (var checker in checkers)
            {
                // ADR-U3-01: token por checker — timeout aislado del shutdown
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
                    _logger.LogError(ex, "Unexpected failure in checker {Type}",
                        checker.GetType().Name);
                }
            }
        }
    }
}
