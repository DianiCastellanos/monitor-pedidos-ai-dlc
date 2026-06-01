using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Web.Features.Monitoring;

public sealed class JobsChecker : ICheckExecutor
{
    public ModuleId Module => ModuleId.JobsMonitor;

    private readonly IJobStatusSource _jobSource;

    public JobsChecker(IJobStatusSource jobSource) => _jobSource = jobSource;

    public async Task<CheckResult> ExecuteAsync(CancellationToken ct = default)
    {
        try
        {
            var jobs   = await _jobSource.GetCurrentStatusAsync(ct);
            var failed = jobs.Where(j => !j.IsRunning || !j.LastExecutionSucceeded).ToList();

            if (failed.Count == 0)
                return CheckResult.Ok($"{jobs.Count} job(s) activos y saludables.");

            // Incluir FailureReason cuando está disponible — distingue timeout de job deshabilitado
            var details = string.Join(" | ", failed.Select(j =>
                j.FailureReason is not null ? $"{j.JobName}: {j.FailureReason}" : j.JobName));
            return CheckResult.Critical(details);
        }
        catch (Exception ex)
        {
            return CheckResult.Critical($"Sin acceso a jobs: {ex.Message[..Math.Min(60, ex.Message.Length)]}");
        }
    }
}
