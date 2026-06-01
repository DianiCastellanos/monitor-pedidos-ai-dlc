using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MonitorPedidos.Domain.Dashboard;
using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Rules;
using MonitorPedidos.Domain.Shared;
using MonitorPedidos.Web.Features.Monitoring;
using Moq;
using Xunit;

namespace MonitorPedidos.UnitTests.Dashboard;

public class BrandMonitorCheckerTests
{
    private static BrandMonitorChecker BuildChecker(
        ISalesforceClient        salesforceClient,
        IBrandSnapshotRepository snapshotRepo,
        IRuleRepository          ruleRepo)
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Monitoring:BrandMonitorWindowSeconds"] = "600"
            })
            .Build();
        return new BrandMonitorChecker(salesforceClient, snapshotRepo, ruleRepo,
            Mock.Of<ILogger<BrandMonitorChecker>>());
    }

    private static Mock<ISalesforceClient> SfClient(int total = 0, IReadOnlyList<SalesforceOrderItem>? items = null)
    {
        var mock = new Mock<ISalesforceClient>();
        mock.Setup(c => c.SearchPendingOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(SalesforceSearchOutcome.Success(total, items ?? []));
        return mock;
    }

    private static Mock<ISalesforceClient> SfClientFailing(string reason = "Error")
    {
        var mock = new Mock<ISalesforceClient>();
        mock.Setup(c => c.SearchPendingOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(SalesforceSearchOutcome.Failure(reason));
        return mock;
    }

    private static Mock<ISalesforceClient> SfClientTimeout()
    {
        var mock = new Mock<ISalesforceClient>();
        mock.Setup(c => c.SearchPendingOrdersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(SalesforceSearchOutcome.Timeout());
        return mock;
    }

    private static Mock<IBrandSnapshotRepository> SnapshotRepo(
        Action<BrandSnapshot>? onInsert = null,
        BrandSnapshot?         latest    = null,
        BrandSnapshot?         before    = null)
    {
        var mock = new Mock<IBrandSnapshotRepository>();

        mock.Setup(r => r.InsertAsync(It.IsAny<BrandSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<BrandSnapshot, CancellationToken>((s, _) => onInsert?.Invoke(s))
            .Returns(Task.CompletedTask);

        mock.Setup(r => r.GetLatestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string site, CancellationToken _) =>
                Task.FromResult(latest?.Site == site ? latest : null));

        mock.Setup(r => r.GetSnapshotBeforeAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns((string site, DateTime _, CancellationToken _) =>
                Task.FromResult(before?.Site == site ? before : null));

        return mock;
    }

    private static Mock<IRuleRepository> RuleRepoWithRule()
    {
        var conditionJson = "{\"pendingDropThreshold\":20,\"pollIntervalSeconds\":120," +
                            "\"snapshotMinIntervalSeconds\":60,\"comparisonWindowSeconds\":600}";
        var rule = CreateBrandMonitorRule(conditionJson);
        var mock = new Mock<IRuleRepository>();
        mock.Setup(r => r.GetActiveByModuleAsync(ModuleId.BrandMonitor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rule> { rule });
        return mock;
    }

    private static Mock<IRuleRepository> RuleRepoEmpty()
    {
        var mock = new Mock<IRuleRepository>();
        mock.Setup(r => r.GetActiveByModuleAsync(ModuleId.BrandMonitor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rule>());
        return mock;
    }

    private static Rule CreateBrandMonitorRule(string conditionJson)
    {
        // Use reflection-free approach: Rule.Create expects valid params
        var condition = new RuleCondition(
            null, null, null, null, 20, 120, 60, 600);
        return Rule.Create(
            "Brand Monitor — Umbral de pendientes",
            "Test rule",
            ModuleId.BrandMonitor,
            condition,
            Severity.Warn);
    }

    private static List<SalesforceOrderItem> ItemsForSite(string site, int count)
    {
        var items = new List<SalesforceOrderItem>();
        for (var i = 0; i < count; i++)
            items.Add(new SalesforceOrderItem($"ORD-{site}-{i}", site, DateTimeOffset.UtcNow));
        return items;
    }

    // ── Existing trend tests ───────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_BacklogBaja_StatusGreen()
    {
        var captured = new List<BrandSnapshot>();
        var items    = ItemsForSite("PatPrimo", 5);
        var before   = BrandSnapshot.Create("PatPrimo", 10, null, SnapshotStatus.NoData);

        var sfClient    = SfClient(total: 5, items: items);
        var snapRepo    = SnapshotRepo(onInsert: s => captured.Add(s), latest: null, before: before);
        var ruleRepo    = RuleRepoWithRule();

        var result = await BuildChecker(sfClient.Object, snapRepo.Object, ruleRepo.Object)
            .ExecuteAsync();

        Assert.Equal(CheckStatus.Ok, result.Status);
        var patPrimoSnap = Assert.Single(captured, s => s.Site == "PatPrimo");
        Assert.Equal(SnapshotStatus.Green, patPrimoSnap.Status);
    }

    [Fact]
    public async Task ExecuteAsync_BacklogIgual_StatusYellow()
    {
        var captured = new List<BrandSnapshot>();
        var items    = ItemsForSite("PatPrimo", 10);
        var before   = BrandSnapshot.Create("PatPrimo", 10, null, SnapshotStatus.NoData);

        var sfClient    = SfClient(total: 10, items: items);
        var snapRepo    = SnapshotRepo(onInsert: s => captured.Add(s), latest: null, before: before);
        var ruleRepo    = RuleRepoWithRule();

        var result = await BuildChecker(sfClient.Object, snapRepo.Object, ruleRepo.Object)
            .ExecuteAsync();

        Assert.Equal(CheckStatus.Ok, result.Status);
        var patPrimoSnap = Assert.Single(captured, s => s.Site == "PatPrimo");
        Assert.Equal(SnapshotStatus.Yellow, patPrimoSnap.Status);
    }

    [Fact]
    public async Task ExecuteAsync_BacklogCrece_StatusRed()
    {
        var captured = new List<BrandSnapshot>();
        var items    = ItemsForSite("PatPrimo", 20);
        var before   = BrandSnapshot.Create("PatPrimo", 10, null, SnapshotStatus.NoData);

        var sfClient    = SfClient(total: 20, items: items);
        var snapRepo    = SnapshotRepo(onInsert: s => captured.Add(s), latest: null, before: before);
        var ruleRepo    = RuleRepoWithRule();

        var result = await BuildChecker(sfClient.Object, snapRepo.Object, ruleRepo.Object)
            .ExecuteAsync();

        Assert.Equal(CheckStatus.Ok, result.Status);
        var patPrimoSnap = Assert.Single(captured, s => s.Site == "PatPrimo");
        Assert.Equal(SnapshotStatus.Red, patPrimoSnap.Status);
    }

    // ── SF failure tests ───────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_SFFailure_ReturnsWarn()
    {
        var sfClient = SfClientFailing("Connection refused");
        var snapRepo = new Mock<IBrandSnapshotRepository>(MockBehavior.Loose);
        var ruleRepo = RuleRepoEmpty();

        var result = await BuildChecker(sfClient.Object, snapRepo.Object, ruleRepo.Object)
            .ExecuteAsync();

        Assert.Equal(CheckStatus.Warn, result.Status);
        Assert.Contains("Salesforce", result.Details);
    }

    [Fact]
    public async Task ExecuteAsync_SFFailure_DoesNotInsertSnapshots()
    {
        var sfClient = SfClientTimeout();
        var snapRepo = new Mock<IBrandSnapshotRepository>(MockBehavior.Strict);
        snapRepo.Setup(r => r.GetLatestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Should not be called"));
        snapRepo.Setup(r => r.GetSnapshotBeforeAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Should not be called"));
        var ruleRepo = RuleRepoEmpty();

        var result = await BuildChecker(sfClient.Object, snapRepo.Object, ruleRepo.Object)
            .ExecuteAsync();

        Assert.Equal(CheckStatus.Warn, result.Status);
        snapRepo.Verify(r => r.InsertAsync(It.IsAny<BrandSnapshot>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── Smart snapshot persistence tests ────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_SinSnapshotPrev_SiempreGuarda()
    {
        var captured = new List<BrandSnapshot>();
        var items    = ItemsForSite("PatPrimo", 5);

        var sfClient = SfClient(total: 5, items: items);
        var snapRepo = SnapshotRepo(
            onInsert: s => captured.Add(s),
            latest: null,           // no hay snapshot previo
            before: null);
        var ruleRepo = RuleRepoWithRule();

        await BuildChecker(sfClient.Object, snapRepo.Object, ruleRepo.Object)
            .ExecuteAsync();

        Assert.Contains(captured, s => s.Site == "PatPrimo");
    }

    [Fact]
    public async Task ExecuteAsync_MismoValorYReciente_NoGuarda()
    {
        var now      = DateTime.UtcNow;
        var items    = new List<SalesforceOrderItem>();
        var latests  = new List<BrandSnapshot>();

        // Crear items y latest snapshots para todos los sites con valores iguales y recientes
        foreach (var site in BrandSnapshot.Sites)
        {
            items.AddRange(ItemsForSite(site, 5));
            var snap = BrandSnapshot.Create(site, 5, null, SnapshotStatus.Green);
            typeof(BrandSnapshot).GetProperty("CheckedAt")!.SetValue(snap, now.AddSeconds(-30));
            latests.Add(snap);
        }

        var sfClient  = SfClient(total: items.Count, items: items);
        var snapRepo  = new Mock<IBrandSnapshotRepository>(MockBehavior.Loose);

        snapRepo.Setup(r => r.GetLatestAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string site, CancellationToken _) =>
                Task.FromResult(latests.FirstOrDefault(s => s.Site == site)));

        snapRepo.Setup(r => r.GetSnapshotBeforeAsync(
                It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns((string site, DateTime _, CancellationToken _) =>
                Task.FromResult(latests.FirstOrDefault(s => s.Site == site)));

        var ruleRepo = RuleRepoWithRule();

        var result = await BuildChecker(sfClient.Object, snapRepo.Object, ruleRepo.Object)
            .ExecuteAsync();

        Assert.Equal(CheckStatus.Ok, result.Status);

        // Verificar que ningún snapshot fue insertado
        snapRepo.Verify(r => r.InsertAsync(It.IsAny<BrandSnapshot>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_MismoValorPeroAntiguo_Guarda()
    {
        var captured = new List<BrandSnapshot>();
        var now      = DateTime.UtcNow;
        var items    = ItemsForSite("PatPrimo", 5);
        var latest   = BrandSnapshot.Create("PatPrimo", 5, null, SnapshotStatus.Green);
        // CheckedAt hace 5 min — supera el intervalo mínimo de 60s
        typeof(BrandSnapshot).GetProperty("CheckedAt")!.SetValue(latest, now.AddMinutes(-5));

        var sfClient = SfClient(total: 5, items: items);
        var snapRepo = SnapshotRepo(
            onInsert: s => captured.Add(s),
            latest: latest,
            before: latest);
        var ruleRepo = RuleRepoWithRule();

        await BuildChecker(sfClient.Object, snapRepo.Object, ruleRepo.Object)
            .ExecuteAsync();

        var snap = Assert.Single(captured, s => s.Site == "PatPrimo");
        Assert.Equal(5, snap.PendingCountCurrent);
    }

    [Fact]
    public async Task ExecuteAsync_ValorCambio_SiempreGuarda()
    {
        var captured = new List<BrandSnapshot>();
        var now      = DateTime.UtcNow;
        var items    = ItemsForSite("PatPrimo", 8);
        var latest   = BrandSnapshot.Create("PatPrimo", 5, null, SnapshotStatus.Green);
        typeof(BrandSnapshot).GetProperty("CheckedAt")!.SetValue(latest, now.AddSeconds(-30));

        var sfClient = SfClient(total: 8, items: items);
        var snapRepo = SnapshotRepo(
            onInsert: s => captured.Add(s),
            latest: latest,
            before: latest);
        var ruleRepo = RuleRepoWithRule();

        await BuildChecker(sfClient.Object, snapRepo.Object, ruleRepo.Object)
            .ExecuteAsync();

        var snap = Assert.Single(captured, s => s.Site == "PatPrimo");
        Assert.Equal(8, snap.PendingCountCurrent);
    }

    [Fact]
    public async Task ExecuteAsync_SinReglas_UsaDefaultsYNuncaFalla()
    {
        var captured = new List<BrandSnapshot>();
        var items    = ItemsForSite("PatPrimo", 3);
        var before   = BrandSnapshot.Create("PatPrimo", 5, null, SnapshotStatus.NoData);

        var sfClient = SfClient(total: 3, items: items);
        var snapRepo = SnapshotRepo(
            onInsert: s => captured.Add(s),
            latest: null,
            before: before);
        var ruleRepo = RuleRepoEmpty();

        var result = await BuildChecker(sfClient.Object, snapRepo.Object, ruleRepo.Object)
            .ExecuteAsync();

        Assert.Equal(CheckStatus.Ok, result.Status);
        var snap = Assert.Single(captured, s => s.Site == "PatPrimo");
        Assert.Equal(SnapshotStatus.Green, snap.Status);
    }
}
