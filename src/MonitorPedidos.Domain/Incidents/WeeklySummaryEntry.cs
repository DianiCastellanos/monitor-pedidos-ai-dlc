using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Domain.Incidents;

public sealed record WeeklySummaryEntry(
    ModuleId       Module,
    CauseCategory  Cause,
    Severity       Severity,
    int            Count,
    int            CandidatosReglaNueva
);
