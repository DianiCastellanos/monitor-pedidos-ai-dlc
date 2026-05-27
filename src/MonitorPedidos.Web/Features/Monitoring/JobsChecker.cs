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
        var jobs   = await _jobSource.GetCurrentStatusAsync(ct);
        var failed = jobs.Where(j => !j.IsRunning || !j.LastExecutionSucceeded).ToList();

        return failed.Count == 0
            ? CheckResult.Ok($"{jobs.Count} job(s) activos y saludables.")
            : CheckResult.Critical(
                $"{failed.Count} job(s) fallido(s): {string.Join(", ", failed.Select(j => j.JobName))}");
    }
}
