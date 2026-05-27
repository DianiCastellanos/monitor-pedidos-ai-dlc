using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Domain.Monitoring;

public interface ICheckExecutor
{
    ModuleId Module { get; }
    Task<CheckResult> ExecuteAsync(CancellationToken ct = default);
}
