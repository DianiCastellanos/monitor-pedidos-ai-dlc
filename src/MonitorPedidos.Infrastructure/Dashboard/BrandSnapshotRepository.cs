using Microsoft.EntityFrameworkCore;
using MonitorPedidos.Domain.Dashboard;
using MonitorPedidos.Infrastructure.Persistence;

namespace MonitorPedidos.Infrastructure.Dashboard;

public sealed class BrandSnapshotRepository(AppDbContext context) : IBrandSnapshotRepository
{
    public async Task UpsertAsync(BrandSnapshot snapshot, CancellationToken ct = default)
    {
        var existing = await context.BrandSnapshots
            .FirstOrDefaultAsync(s => s.Site == snapshot.Site, ct);

        if (existing is null)
        {
            await context.BrandSnapshots.AddAsync(snapshot, ct);
        }
        else
        {
            // Overwrite all fields via ExecuteUpdate (EF Core 7+)
            await context.BrandSnapshots
                .Where(s => s.Site == snapshot.Site)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(s => s.PendingCountCurrent,  snapshot.PendingCountCurrent)
                    .SetProperty(s => s.PendingCountPrevious, snapshot.PendingCountPrevious)
                    .SetProperty(s => s.CheckedAt,            snapshot.CheckedAt)
                    .SetProperty(s => s.Status,               snapshot.Status),
                ct);
            return; // ExecuteUpdate ya hizo SaveChanges implícito
        }

        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<BrandSnapshot>> GetAllAsync(CancellationToken ct = default)
        => await context.BrandSnapshots.AsNoTracking().OrderBy(s => s.Site).ToListAsync(ct);
}
