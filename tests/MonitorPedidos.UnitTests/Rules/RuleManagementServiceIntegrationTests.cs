using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MonitorPedidos.Domain.Rules;
using MonitorPedidos.Domain.Shared;
using MonitorPedidos.Infrastructure.Persistence;
using MonitorPedidos.Infrastructure.Rules;
using MonitorPedidos.Web.Services;
using Xunit;

namespace MonitorPedidos.UnitTests.Rules;

public sealed class RuleManagementServiceIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext     _context;
    private readonly RuleManagementService _svc;

    public RuleManagementServiceIntegrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(opts);
        _context.Database.EnsureCreated();

        var ruleRepo = new RuleRepository(_context);
        var histRepo = new RuleHistoryRepository(_context);
        _svc = new RuleManagementService(ruleRepo, histRepo);
    }

    [Fact]
    public async Task CreateRule_PersistsRuleAndHistory_Atomically()
    {
        var rule = await _svc.CreateRuleAsync(
            "Test rule", "Descripción",
            ModuleId.DbOrderChecker,
            RuleCondition.ForDbOrders(120, 1),
            Severity.Critical,
            "user-1", "creación inicial");

        var dbRule    = await _context.Rules.FindAsync(rule.Id);
        var dbHistory = await _context.RuleHistory
            .Where(h => h.RuleId == rule.Id).ToListAsync();

        Assert.NotNull(dbRule);
        Assert.Single(dbHistory);
        Assert.Equal(RuleChangeType.Creation, dbHistory[0].ChangeType);
        Assert.Null(dbHistory[0].SnapshotBefore);
        Assert.NotNull(dbHistory[0].SnapshotAfter);
    }

    [Fact]
    public async Task UpdateRule_CreatesEditHistoryWithBeforeAndAfterSnapshots()
    {
        var rule = await _svc.CreateRuleAsync(
            "Original", "Desc original",
            ModuleId.DbOrderChecker,
            RuleCondition.ForDbOrders(120, 1),
            Severity.Critical,
            "user-1", "razón creación");

        await _svc.UpdateRuleAsync(rule.Id,
            "Actualizada", "Desc nueva",
            RuleCondition.ForDbOrders(240, 2),
            Severity.Warn,
            "user-1", "ajuste operativo");

        var dbHistory = await _context.RuleHistory
            .Where(h => h.RuleId == rule.Id)
            .ToListAsync();
        dbHistory = dbHistory.OrderBy(h => h.ChangedAt).ToList();

        Assert.Equal(2, dbHistory.Count);
        var editEntry = dbHistory[1];
        Assert.Equal(RuleChangeType.Edit, editEntry.ChangeType);
        Assert.NotNull(editEntry.SnapshotBefore);
        Assert.NotNull(editEntry.SnapshotAfter);
        Assert.Contains("Original", editEntry.SnapshotBefore);
        Assert.Contains("Actualizada", editEntry.SnapshotAfter);
    }

    [Fact]
    public async Task DeactivateRule_ChangesIsActive_AndCreatesHistoryEntry()
    {
        var rule = await _svc.CreateRuleAsync(
            "Regla activa", "Desc",
            ModuleId.DbHealthChecker,
            RuleCondition.ForDbHealth(500, 2000),
            Severity.Warn,
            "user-1", "creación");

        await _svc.DeactivateRuleAsync(rule.Id, "user-1", "mantenimiento programado");

        var dbRule = await _context.Rules.FindAsync(rule.Id);
        Assert.NotNull(dbRule);
        Assert.False(dbRule.IsActive);

        var deactivationEntry = await _context.RuleHistory
            .FirstOrDefaultAsync(h => h.RuleId == rule.Id && h.ChangeType == RuleChangeType.Deactivation);
        Assert.NotNull(deactivationEntry);
        Assert.NotNull(deactivationEntry.SnapshotBefore);
        Assert.Null(deactivationEntry.SnapshotAfter);
    }

    [Fact]
    public async Task ActivateRule_WhenAlreadyActive_IsIdempotent()
    {
        var rule = await _svc.CreateRuleAsync(
            "Ya activa", "Desc",
            ModuleId.JobsMonitor,
            RuleCondition.ForJobs(),
            Severity.Critical,
            "user-1", "creación");

        // rule ya está activa — activar de nuevo no debe generar historial adicional
        await _svc.ActivateRuleAsync(rule.Id, "user-1", "reactivación redundante");

        var historyCount = await _context.RuleHistory
            .CountAsync(h => h.RuleId == rule.Id);

        Assert.Equal(1, historyCount); // solo la entrada de Creation
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
