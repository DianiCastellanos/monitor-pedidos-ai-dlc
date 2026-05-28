using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Domain.Rules;

public interface IRuleManagementService
{
    Task<IReadOnlyList<Rule>>             GetAllRulesAsync(CancellationToken ct = default);
    Task<Rule?>                           GetRuleByIdAsync(Guid ruleId, CancellationToken ct = default);
    Task<Rule>                            CreateRuleAsync(string name, string description, ModuleId appliesTo, RuleCondition condition, Severity severity, string authorUserId, string reason, CancellationToken ct = default);
    Task<Rule>                            UpdateRuleAsync(Guid ruleId, string name, string description, RuleCondition condition, Severity severity, string authorUserId, string reason, CancellationToken ct = default);
    Task                                  ActivateRuleAsync(Guid ruleId, string authorUserId, string reason, CancellationToken ct = default);
    Task                                  DeactivateRuleAsync(Guid ruleId, string authorUserId, string reason, CancellationToken ct = default);
    Task<IReadOnlyList<RuleHistoryEntry>> GetHistoryAsync(Guid ruleId, CancellationToken ct = default);
}
