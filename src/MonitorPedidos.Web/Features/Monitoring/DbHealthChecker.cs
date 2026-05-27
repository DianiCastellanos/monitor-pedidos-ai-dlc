using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Shared;
using MonitorPedidos.Infrastructure.Persistence;

namespace MonitorPedidos.Web.Features.Monitoring;

public sealed class DbHealthChecker : ICheckExecutor
{
    public ModuleId Module => ModuleId.DbHealthChecker;

    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public DbHealthChecker(AppDbContext db, IConfiguration config)
    {
        _db     = db;
        _config = config;
    }

    private int WarnLatencyMs => _config.GetValue("Monitoring:DbWarnLatencyMs",  1000);
    private int CritTimeoutMs => _config.GetValue("Monitoring:DbCritTimeoutMs",  5000);

    public async Task<CheckResult> ExecuteAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // ADR-U3-01: linked token isolates DB timeout from app shutdown token
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(CritTimeoutMs);
            await _db.Database.ExecuteSqlRawAsync("SELECT 1", cts.Token);
            sw.Stop();

            return sw.ElapsedMilliseconds > WarnLatencyMs
                ? CheckResult.Warn($"BD responde con latencia alta: {sw.ElapsedMilliseconds} ms.")
                : CheckResult.Ok($"BD responde en {sw.ElapsedMilliseconds} ms.");
        }
        catch (Exception ex)
        {
            return CheckResult.Critical($"BD no responde: {ex.Message}");
        }
    }
}
