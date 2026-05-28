using Microsoft.EntityFrameworkCore;
using MonitorPedidos.Domain.Rules;
using MonitorPedidos.Infrastructure.Persistence;

namespace MonitorPedidos.Infrastructure.Rules;

public sealed class RuleHistoryRepository(AppDbContext context) : IRuleHistoryRepository
{
    public async Task AppendAsync(RuleHistoryEntry entry, CancellationToken ct = default)
        => await context.RuleHistory.AddAsync(entry, ct);

    public async Task<IReadOnlyList<RuleHistoryEntry>> GetByRuleIdAsync(Guid ruleId, CancellationToken ct = default)
        => await context.RuleHistory.AsNoTracking()
            .Where(h => h.RuleId == ruleId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<RuleHistoryEntry>> GetAllAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
        => await context.RuleHistory.AsNoTracking()
            .Where(h => h.ChangedAt >= from && h.ChangedAt <= to)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(ct);
}
