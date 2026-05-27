namespace MonitorPedidos.Domain.Identity;

public static class PredefinedIdentities
{
    public static readonly UserIdentity AnalistaOperativo = new(
        DisplayName: "Analista Operativo",
        RoleName: ApplicationRole.Names.Operador
    );

    public static readonly UserIdentity ResponsableTecnico = new(
        DisplayName: "Responsable Técnico",
        RoleName: ApplicationRole.Names.Tecnico
    );
}
