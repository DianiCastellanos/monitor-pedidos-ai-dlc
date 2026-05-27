using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Web.Features.Monitoring;

// Stub — implementación completa en U6. No genera incidentes; actualiza brand_snapshots.
public sealed class BrandMonitorChecker : ICheckExecutor
{
    public ModuleId Module => ModuleId.BrandMonitor;

    public Task<CheckResult> ExecuteAsync(CancellationToken ct = default)
        => Task.FromResult(CheckResult.Ok("Brand monitor pendiente de implementación en U6."));
}
