namespace MonitorPedidos.Domain.Rules;

public interface IRuleHistoryRepository
{
    Task                                  AppendAsync(RuleHistoryEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<RuleHistoryEntry>> GetByRuleIdAsync(Guid ruleId, CancellationToken ct = default);
    Task<IReadOnlyList<RuleHistoryEntry>> GetAllAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
}
