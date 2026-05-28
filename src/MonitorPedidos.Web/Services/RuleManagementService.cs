using MonitorPedidos.Domain.Rules;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Web.Services;

public sealed class RuleManagementService(
    IRuleRepository        ruleRepo,
    IRuleHistoryRepository historyRepo) : IRuleManagementService
{
    public Task<IReadOnlyList<Rule>> GetAllRulesAsync(CancellationToken ct = default)
        => ruleRepo.GetAllAsync(ct);

    public Task<Rule?> GetRuleByIdAsync(Guid ruleId, CancellationToken ct = default)
        => ruleRepo.GetByIdAsync(ruleId, ct);

    public async Task<Rule> CreateRuleAsync(
        string name, string description, ModuleId appliesTo,
        RuleCondition condition, Severity severity,
        string authorUserId, string reason,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var rule    = Rule.Create(name, description, appliesTo, condition, severity);
        var history = RuleHistoryEntry.ForCreation(rule.Id, authorUserId, reason, rule);

        await ruleRepo.AddAsync(rule, ct);
        await historyRepo.AppendAsync(history, ct);
        await ruleRepo.SaveChangesAsync(ct);

        return rule;
    }

    public async Task<Rule> UpdateRuleAsync(
        Guid ruleId, string name, string description,
        RuleCondition condition, Severity severity,
        string authorUserId, string reason,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var rule = await ruleRepo.GetByIdAsync(ruleId, ct)
            ?? throw new InvalidOperationException($"Regla {ruleId} no encontrada.");

        var snapshotBefore = RuleSnapshot.From(rule).ToJson();
        rule.Update(name, description, condition, severity);
        var snapshotAfter  = RuleSnapshot.From(rule).ToJson();

        var history = RuleHistoryEntry.ForEdit(ruleId, authorUserId, reason, snapshotBefore, snapshotAfter);
        await historyRepo.AppendAsync(history, ct);
        await ruleRepo.SaveChangesAsync(ct);

        return rule;
    }

    public async Task ActivateRuleAsync(
        Guid ruleId, string authorUserId, string reason,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var rule = await ruleRepo.GetByIdAsync(ruleId, ct)
            ?? throw new InvalidOperationException($"Regla {ruleId} no encontrada.");

        if (rule.IsActive) return; // idempotente — BR-RULE-06

        rule.Activate();
        var history = RuleHistoryEntry.ForActivation(ruleId, authorUserId, reason, rule);
        await historyRepo.AppendAsync(history, ct);
        await ruleRepo.SaveChangesAsync(ct);
    }

    public async Task DeactivateRuleAsync(
        Guid ruleId, string authorUserId, string reason,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var rule = await ruleRepo.GetByIdAsync(ruleId, ct)
            ?? throw new InvalidOperationException($"Regla {ruleId} no encontrada.");

        if (!rule.IsActive) return; // idempotente — BR-RULE-06

        rule.Deactivate();
        var history = RuleHistoryEntry.ForDeactivation(ruleId, authorUserId, reason, rule);
        await historyRepo.AppendAsync(history, ct);
        await ruleRepo.SaveChangesAsync(ct);
    }

    public Task<IReadOnlyList<RuleHistoryEntry>> GetHistoryAsync(Guid ruleId, CancellationToken ct = default)
        => historyRepo.GetByRuleIdAsync(ruleId, ct);
}
