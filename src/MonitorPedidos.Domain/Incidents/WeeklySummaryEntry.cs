using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Domain.Incidents;

public sealed record WeeklySummaryEntry(
    CauseCategory Cause,
    Severity      Severity,
    int           Count,
    int           CandidatosReglaNueva
);
