namespace MonitorPedidos.Domain.Alerts;

public sealed record AlertMessage(
    string QuePaso,
    DateTimeOffset Cuando,
    string Donde,
    string SeveridadTexto,
    string CausaProbable,
    string AccionSugerida
);
