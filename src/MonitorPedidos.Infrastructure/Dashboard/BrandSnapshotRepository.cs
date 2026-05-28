using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MonitorPedidos.Domain.Dashboard;
using MonitorPedidos.Infrastructure.Persistence;

namespace MonitorPedidos.Infrastructure.Dashboard;

public sealed class BrandSnapshotRepository(
    AppDbContext                          context,
    ILogger<BrandSnapshotRepository>     logger) : IBrandSnapshotRepository
{
    public async Task InsertAsync(BrandSnapshot snapshot, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[BrandSnapshotRepository] InsertAsync — site={Site} currentCount={C} checkedAt={T:HH:mm:ss.fff}",
            snapshot.Site, snapshot.PendingCountCurrent, snapshot.CheckedAt);

        await context.BrandSnapshots.AddAsync(snapshot, ct);

        logger.LogInformation("[BrandSnapshotRepository] Llamando SaveChangesAsync para site={Site}", snapshot.Site);

        var rows = await context.SaveChangesAsync(ct);

        logger.LogInformation(
            "[BrandSnapshotRepository] SaveChangesAsync completado — filas afectadas={Rows} site={Site}",
            rows, snapshot.Site);
    }

    public async Task<IReadOnlyList<BrandSnapshot>> GetLatestPerSiteAsync(CancellationToken ct = default)
    {
        // OrderByDescending + GroupBy en memoria: evita problemas de traducción SQL
        // de GroupBy con First() que EF Core/PostgreSQL no garantiza traducir correctamente.
        var all = await context.BrandSnapshots
            .AsNoTracking()
            .OrderByDescending(s => s.CheckedAt)
            .ToListAsync(ct);

        return all
            .GroupBy(s => s.Site)
            .Select(g => g.First())   // primero = más reciente por el OrderByDescending anterior
            .OrderBy(s => s.Site)
            .ToList();
    }

    public async Task<BrandSnapshot?> GetSnapshotBeforeAsync(
        string site, DateTime before, CancellationToken ct = default)
    {
        return await context.BrandSnapshots
            .AsNoTracking()
            .Where(s => s.Site == site && s.CheckedAt <= before)
            .OrderByDescending(s => s.CheckedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default)
    {
        await context.BrandSnapshots
            .Where(s => s.CheckedAt < cutoff)
            .ExecuteDeleteAsync(ct);
    }
}
