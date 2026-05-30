using Microsoft.EntityFrameworkCore;
using MonitorPedidos.Domain.Rules;
using MonitorPedidos.Domain.Shared;
using MonitorPedidos.Infrastructure.Persistence;

namespace MonitorPedidos.Infrastructure.Rules;

public sealed class RuleRepository(AppDbContext context) : IRuleRepository
{
    public async Task<IReadOnlyList<Rule>> GetAllAsync(CancellationToken ct = default)
        => await context.Rules.AsNoTracking().OrderBy(r => r.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Rule>> GetActiveAsync(CancellationToken ct = default)
        => await context.Rules.AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Rule>> GetActiveByModuleAsync(ModuleId module, CancellationToken ct = default)
        => await context.Rules.AsNoTracking()
            .Where(r => r.IsActive && r.ModuleId == module)
            .ToListAsync(ct);

    public async Task<Rule?> GetByIdAsync(Guid ruleId, CancellationToken ct = default)
        => await context.Rules.FindAsync(new object?[] { ruleId }, ct);

    public async Task AddAsync(Rule rule, CancellationToken ct = default)
        => await context.Rules.AddAsync(rule, ct);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
}
