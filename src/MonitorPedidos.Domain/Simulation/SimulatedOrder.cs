namespace MonitorPedidos.Domain.Simulation;

public sealed class SimulatedOrder
{
    public int      Id        { get; private set; }
    public string   Source    { get; private set; } = null!;  // "Salesforce" | "Multivende"
    public string   Status    { get; private set; } = null!;  // "Pending" | "Cancelled" | "Error"
    public DateTime CreatedAt { get; private set; }
    public bool     IsFailure { get; private set; }
    public string   Site      { get; private set; } = null!;  // "Patprimo" | "SevenSeven" | "Atmos" | "Ostu"

    private SimulatedOrder() { }

    public static SimulatedOrder CreateNormal(string source, string site) =>
        new() { Source = source, Status = "Pending", CreatedAt = DateTime.UtcNow, IsFailure = false, Site = site };

    // failStatus: "Error" para RT5 | "Cancelled" para RT7
    public static SimulatedOrder CreateFailure(string source, string site, string failStatus) =>
        new() { Source = source, Status = failStatus, CreatedAt = DateTime.UtcNow, IsFailure = true, Site = site };
}
