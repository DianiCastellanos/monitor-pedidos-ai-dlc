using MonitorPedidos.Domain.Incidents;
using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Shared;
using MonitorPedidos.Web.Features.Monitoring;

namespace MonitorPedidos.Web.Services;

public interface IMonitoringService
{
    Task RunCheckAsync(ICheckExecutor checker, CancellationToken ct = default);
}

public sealed class MonitoringService : IMonitoringService
{
    private readonly IIncidentService _incidents;
    private readonly ILogger<MonitoringService> _logger;

    public MonitoringService(IIncidentService incidents, ILogger<MonitoringService> logger)
    {
        _incidents = incidents;
        _logger    = logger;
    }

    public async Task RunCheckAsync(ICheckExecutor checker, CancellationToken ct = default)
    {
        CheckResult result;
        try
        {
            result = await checker.ExecuteAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Checker {Type} threw during ExecuteAsync", checker.GetType().Name);
            return;
        }

        if (result.Status == CheckStatus.Ok)
        {
            await _incidents.TryCloseOnConsecutiveOkAsync(checker.Module, ct);
            _logger.LogDebug("Check OK: {Module} — {Details}", checker.Module, result.Details);
            return;
        }

        var cause   = CauseClassifier.Classify(checker);
        var context = new CheckContext(checker.Module, result.Status, cause,
                                       result.Details, result.CheckedAt);
        var alert    = AlertTemplateRenderer.Render(context);
        var severity = result.Status == CheckStatus.Critical ? Severity.Critical : Severity.Warn;

        // OpenIncidentAsync is idempotent and handles notification internally
        var incident = await _incidents.OpenIncidentAsync(
            checker.Module, cause, severity, alert, ct);

        _logger.LogInformation("Incident {Id} opened: {Module} [{Status}] {Details}",
            incident.Id, checker.Module, result.Status, result.Details);
    }
}
