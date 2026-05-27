using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Domain.Monitoring;

public interface ICheckExecutor
{
    ModuleId Module { get; }
    Task<CheckResult> CheckAsync(CancellationToken ct = default);
}
