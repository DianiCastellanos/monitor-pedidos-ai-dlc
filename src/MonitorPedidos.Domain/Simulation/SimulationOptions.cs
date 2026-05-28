namespace MonitorPedidos.Domain.Simulation;

public sealed class SimulationOptions
{
    public const string Section = "Simulation";

    public bool   Enabled               { get; init; } = true;
    public int    InsertIntervalMinutes  { get; init; } = 5;
    public double FailureProbability    { get; init; } = 0.0;
    public bool   NoOrdersMode          { get; init; } = false;
}
