using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MonitorPedidos.Domain.Dashboard;
using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Shared;
using MonitorPedidos.Domain.Simulation;
using MonitorPedidos.Web.Features.Monitoring;
using Moq;
using Xunit;

namespace MonitorPedidos.UnitTests.Dashboard;

public class BrandMonitorCheckerTests
{
    private static BrandMonitorChecker BuildChecker(
        ISimulatedOrderRepository simRepo,
        IBrandSnapshotRepository  snapshotRepo,
        int                       windowSeconds = 600)
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Monitoring:BrandMonitorWindowSeconds"] = windowSeconds.ToString()
            })
            .Build();
        return new BrandMonitorChecker(simRepo, snapshotRepo, cfg,
            Mock.Of<ILogger<BrandMonitorChecker>>());
    }

    private static Mock<ISimulatedOrderRepository> SimRepo(int actual, int? anterior = null)
    {
        var mock = new Mock<ISimulatedOrderRepository>();
        var dictActual = BrandSnapshot.Sites.ToDictionary(s => s, _ => actual);
        mock.Setup(r => r.CountBySiteAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dictActual);
        return mock;
    }

    private static Mock<IBrandSnapshotRepository> SnapshotRepo(
        Action<BrandSnapshot>? onInsert = null,
        int?                  previousPending = null)
    {
        var mock = new Mock<IBrandSnapshotRepository>();

        mock.Setup(r => r.InsertAsync(It.IsAny<BrandSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<BrandSnapshot, CancellationToken>((s, _) => onInsert?.Invoke(s))
            .Returns(Task.CompletedTask);

        if (previousPending.HasValue)
        {
            var previous = BrandSnapshot.Create("Patprimo", previousPending.Value, null, SnapshotStatus.NoData);
            mock.Setup(r => r.GetSnapshotBeforeAsync(
                    It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(previous);
        }

        return mock;
    }

    [Fact]
    public async Task ExecuteAsync_BacklogBaja_StatusGreen()
    {
        var captured = new List<BrandSnapshot>();
        var snap = SnapshotRepo(onInsert: s => captured.Add(s), previousPending: 10);

        var result = await BuildChecker(SimRepo(actual: 5).Object, snap.Object)
            .ExecuteAsync();

        Assert.Equal(CheckStatus.Ok, result.Status);
        Assert.All(captured, s => Assert.Equal(SnapshotStatus.Green, s.Status));
    }

    [Fact]
    public async Task ExecuteAsync_BacklogIgual_StatusYellow()
    {
        var captured = new List<BrandSnapshot>();
        var snap = SnapshotRepo(onInsert: s => captured.Add(s), previousPending: 10);

        var result = await BuildChecker(SimRepo(actual: 10).Object, snap.Object)
            .ExecuteAsync();

        Assert.Equal(CheckStatus.Ok, result.Status);
        Assert.All(captured, s => Assert.Equal(SnapshotStatus.Yellow, s.Status));
    }

    [Fact]
    public async Task ExecuteAsync_BacklogCrece_StatusRed()
    {
        var captured = new List<BrandSnapshot>();
        var snap = SnapshotRepo(onInsert: s => captured.Add(s), previousPending: 10);

        var result = await BuildChecker(SimRepo(actual: 20).Object, snap.Object)
            .ExecuteAsync();

        Assert.Equal(CheckStatus.Ok, result.Status);
        Assert.All(captured, s => Assert.Equal(SnapshotStatus.Red, s.Status));
    }

    [Fact]
    public async Task ExecuteAsync_SinDatos_MuestraCerosYStatusYellow()
    {
        var captured = new List<BrandSnapshot>();
        var snap = SnapshotRepo(onInsert: s => captured.Add(s), previousPending: 0);

        var result = await BuildChecker(SimRepo(actual: 0).Object, snap.Object)
            .ExecuteAsync();

        Assert.Equal(CheckStatus.Ok, result.Status);
        Assert.False(result.RequiresIncident);
        Assert.All(captured, s =>
        {
            Assert.Equal(0, s.PendingCountCurrent);
            Assert.Equal(0, s.PendingCountPrevious);
            Assert.Equal(SnapshotStatus.Yellow, s.Status);
        });
    }
}
