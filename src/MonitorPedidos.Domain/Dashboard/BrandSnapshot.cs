namespace MonitorPedidos.Domain.Dashboard;

public enum SnapshotStatus { Green, Yellow, Red }

public sealed class BrandSnapshot
{
    public static readonly string[] Sites = ["Patprimo", "SevenSeven", "Atmos", "Ostu"];

    public int            Id                   { get; private set; }
    public string         Site                 { get; private set; } = string.Empty;
    public int            PendingCountCurrent  { get; private set; }
    public int            PendingCountPrevious { get; private set; }
    public DateTime       CheckedAt            { get; private set; }
    public SnapshotStatus Status               { get; private set; }

    private BrandSnapshot() { }

    public static BrandSnapshot Upsert(
        string site, int currentPending, int previousPending, int dropThreshold)
    {
        return new BrandSnapshot
        {
            Site                 = site,
            PendingCountCurrent  = Math.Max(0, currentPending),
            PendingCountPrevious = Math.Max(0, previousPending),
            CheckedAt            = DateTime.UtcNow,
            Status               = DetermineStatus(currentPending, previousPending, dropThreshold)
        };
    }

    public static BrandSnapshot Create(
        string site, int currentPending, int previousPending, SnapshotStatus status)
    {
        return new BrandSnapshot
        {
            Site                 = site,
            PendingCountCurrent  = Math.Max(0, currentPending),
            PendingCountPrevious = Math.Max(0, previousPending),
            CheckedAt            = DateTime.UtcNow,
            Status               = status
        };
    }

    private static SnapshotStatus DetermineStatus(int current, int previous, int threshold)
    {
        if (current < 0) return SnapshotStatus.Red;          // error de API
        var drop = previous - current;
        if (drop >= threshold) return SnapshotStatus.Green;
        if (drop > 0)          return SnapshotStatus.Yellow;
        return SnapshotStatus.Red;
    }
}
