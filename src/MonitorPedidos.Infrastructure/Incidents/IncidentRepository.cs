using Microsoft.EntityFrameworkCore;
using MonitorPedidos.Domain.Incidents;
using MonitorPedidos.Domain.Shared;
using MonitorPedidos.Infrastructure.Persistence;

namespace MonitorPedidos.Infrastructure.Incidents;

public sealed class IncidentRepository(AppDbContext context) : IIncidentRepository
{
    public async Task AddAsync(Incident incident, CancellationToken ct = default)
        => await context.Incidents.AddAsync(incident, ct);

    public async Task<Incident?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Incidents.FindAsync(new object?[] { id }, ct);

    public async Task<Incident?> GetOpenByModuleAsync(ModuleId module, CancellationToken ct = default)
        => await context.Incidents
            .OrderByDescending(i => i.OpenedAt)
            .FirstOrDefaultAsync(i => i.Module == module && i.ClosedAt == null, ct);

    public async Task<IReadOnlyList<Incident>> GetRecentClosedByModuleAsync(
        ModuleId module, int count, CancellationToken ct = default)
        => await context.Incidents
            .AsNoTracking()
            .Where(i => i.Module == module && i.ClosedAt != null)
            .OrderByDescending(i => i.ClosedAt)
            .Take(count)
            .ToListAsync(ct);

    public async Task<PagedResult<Incident>> SearchAsync(
        IncidentSearchFilter filter, CancellationToken ct = default)
    {
        var query = context.Incidents.AsNoTracking().AsQueryable();

        if (filter.From.HasValue)
            query = query.Where(i => i.OpenedAt >= filter.From.Value);
        if (filter.To.HasValue)
            query = query.Where(i => i.OpenedAt <= filter.To.Value);
        if (filter.Severity.HasValue)
            query = query.Where(i => i.Severity == filter.Severity.Value);
        if (filter.Module.HasValue)
            query = query.Where(i => i.Module == filter.Module.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(i => i.OpenedAt)
            .Skip(filter.Skip)
            .Take(filter.Take)
            .ToListAsync(ct);

        return new PagedResult<Incident>(items, total, filter.Skip, filter.Take);
    }

    public async Task<IReadOnlyList<WeeklySummaryEntry>> GetWeeklySummaryAsync(
        DateOnly weekStart, CancellationToken ct = default)
    {
        var from = weekStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var to   = weekStart.AddDays(7).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var rawGroups = await context.Incidents
            .AsNoTracking()
            .Where(i => i.OpenedAt >= from && i.OpenedAt < to)
            .GroupBy(i => new { i.Module, i.Cause, i.Severity })
            .Select(g => new
            {
                g.Key.Module,
                g.Key.Cause,
                g.Key.Severity,
                Count                = g.Count(),
                CandidatosReglaNueva = g.Count(i => i.IsCandidatoReglaNueva)
            })
            .ToListAsync(ct);

        return rawGroups
            .Select(x => new WeeklySummaryEntry(x.Module, x.Cause, x.Severity, x.Count, x.CandidatosReglaNueva))
            .OrderByDescending(e => (int)e.Severity)
            .ThenByDescending(e => e.Count)
            .ToList();
    }

    public async Task CloseAllByModuleExceptAsync(
        ModuleId module, Guid exceptId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        await context.Incidents
            .Where(i => i.Module == module && i.ClosedAt == null && i.Id != exceptId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.ClosedAt, now)
                .SetProperty(i => i.CloseType, IncidentCloseType.Automatic), ct);
    }

    public async Task<int> PurgeExpiredAsync(int retentionDays, CancellationToken ct = default)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);
        return await context.Incidents
            .Where(i => i.OpenedAt < cutoff)
            .ExecuteDeleteAsync(ct);
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await context.SaveChangesAsync(ct);
}
