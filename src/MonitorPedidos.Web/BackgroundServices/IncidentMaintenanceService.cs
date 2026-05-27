using MonitorPedidos.Domain.Incidents;

namespace MonitorPedidos.Web.BackgroundServices;

public sealed class IncidentMaintenanceService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration       _config;
    private readonly ILogger<IncidentMaintenanceService> _logger;

    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    public IncidentMaintenanceService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<IncidentMaintenanceService> logger)
    {
        _scopeFactory = scopeFactory;
        _config       = config;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunPurgeAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task RunPurgeAsync(CancellationToken ct)
    {
        var retentionDays = _config.GetValue<int>("Incidents:RetentionDays", 90);
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repo    = scope.ServiceProvider.GetRequiredService<IIncidentRepository>();
            var deleted = await repo.PurgeExpiredAsync(retentionDays, ct);
            _logger.LogInformation(
                "Incident purge completed. RetentionDays={Days} Deleted={Count}",
                retentionDays, deleted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "Incident purge failed. RetentionDays={Days}", retentionDays);
        }
    }
}
