namespace MonitorPedidos.Domain.Shared;

public enum ModuleId
{
    Scheduler       = 1,
    DbOrderChecker  = 2,
    ApiChecker      = 3,
    DbHealthChecker = 4,
    RulesManagement = 6,
    CauseClassifier = 7,
    Dashboard       = 8,
    IncidentManager = 9,
    AlertRenderer   = 10,
    JobsMonitor     = 11,
    BrandMonitor    = 12
}
