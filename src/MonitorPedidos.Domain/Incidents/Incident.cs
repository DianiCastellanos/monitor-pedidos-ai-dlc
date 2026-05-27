using MonitorPedidos.Domain.Alerts;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Domain.Incidents;

public sealed class Incident
{
    public Guid              Id                    { get; private set; }
    public ModuleId          Module                { get; private set; }
    public CauseCategory     Cause                 { get; private set; }
    public Severity          Severity              { get; private set; }
    public AlertMessage      Alert                 { get; private set; } = default!;
    public DateTimeOffset    OpenedAt              { get; private set; }
    public DateTimeOffset?   ClosedAt              { get; private set; }
    public IncidentCloseType? CloseType            { get; private set; }
    public string?           ClosedByRole          { get; private set; }
    public string?           ComentarioResolucion  { get; private set; }
    public bool              IsCandidatoReglaNueva { get; private set; }

    public bool IsOpen   => ClosedAt is null;
    public bool IsClosed => ClosedAt is not null;

    private Incident() { }

    public static Incident Open(
        ModuleId module,
        CauseCategory cause,
        Severity severity,
        AlertMessage alert)
    {
        return new Incident
        {
            Id                    = Guid.NewGuid(),
            Module                = module,
            Cause                 = cause,
            Severity              = severity,
            Alert                 = alert,
            OpenedAt              = DateTimeOffset.UtcNow,
            IsCandidatoReglaNueva = cause == CauseCategory.NoDeterminada
        };
    }

    public void CloseAutomatically()
    {
        if (IsClosed) throw new DomainException("El incidente ya está cerrado.");
        ClosedAt  = DateTimeOffset.UtcNow;
        CloseType = IncidentCloseType.Automatic;
    }

    public void CloseManually(string closedByRole, string comentario)
    {
        if (IsClosed) throw new DomainException("El incidente ya está cerrado.");
        if (string.IsNullOrWhiteSpace(comentario))
            throw new DomainException("El comentario de resolución es obligatorio para cierre manual.");

        ClosedAt             = DateTimeOffset.UtcNow;
        CloseType            = IncidentCloseType.Manual;
        ClosedByRole         = closedByRole;
        ComentarioResolucion = comentario.Trim();
    }

    public bool IsExpired(int retentionDays) =>
        OpenedAt < DateTimeOffset.UtcNow.AddDays(-retentionDays);
}
