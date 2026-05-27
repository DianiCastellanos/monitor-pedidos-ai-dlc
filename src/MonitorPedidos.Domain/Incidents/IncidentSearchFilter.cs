using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Domain.Incidents;

public sealed record IncidentSearchFilter(
    DateTimeOffset? From     = null,
    DateTimeOffset? To       = null,
    Severity?       Severity = null,
    ModuleId?       Module   = null,
    int             Skip     = 0,
    int             Take     = 20
)
{
    public const int MaxTake = 100;

    public IncidentSearchFilter WithValidatedTake() =>
        this with { Take = Math.Min(Take, MaxTake) };
}
