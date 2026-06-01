namespace MonitorPedidos.Domain.Dashboard;

public enum SnapshotStatus { Green, Yellow, Red, NoData }

public sealed class BrandSnapshot
{
    public static readonly string[] Sites = ["PatPrimo", "SevenSeven", "Atmos", "Ostu"];

    public int            Id                   { get; private set; }
    public string         Site                 { get; private set; } = string.Empty;
    public int            PendingCountCurrent  { get; private set; }
    public int?           PendingCountPrevious { get; private set; }
    public DateTime       CheckedAt            { get; private set; }
    public SnapshotStatus Status               { get; private set; }

    private BrandSnapshot() { }

    public static BrandSnapshot Create(
        string site, int currentPending, int? previousPending, SnapshotStatus status)
    {
        return new BrandSnapshot
        {
            Site                 = site,
            PendingCountCurrent  = Math.Max(0, currentPending),
            PendingCountPrevious = previousPending.HasValue ? Math.Max(0, previousPending.Value) : null,
            CheckedAt            = DateTime.UtcNow,
            Status               = status
        };
    }
}
