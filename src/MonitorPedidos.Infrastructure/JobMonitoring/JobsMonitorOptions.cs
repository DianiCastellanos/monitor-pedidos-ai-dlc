namespace MonitorPedidos.Infrastructure.JobMonitoring;

public sealed class JobsMonitorOptions
{
    public const string Section = "JobsMonitor";

    public string Server            { get; set; } = "172.16.0.41";
    public string TaskName          { get; set; } = "OC_PATPRIMO";
    public int    CommandTimeoutMs  { get; set; } = 10_000;
}
