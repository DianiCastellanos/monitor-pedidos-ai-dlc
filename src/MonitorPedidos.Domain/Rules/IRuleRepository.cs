using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Domain.Rules;

public interface IRuleRepository
{
    Task<IReadOnlyList<Rule>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Rule>> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Rule>> GetActiveByModuleAsync(ModuleId module, CancellationToken ct = default);
    Task<Rule?>               GetByIdAsync(Guid ruleId, CancellationToken ct = default);
    Task                      AddAsync(Rule rule, CancellationToken ct = default);
    Task                      SaveChangesAsync(CancellationToken ct = default);
}
