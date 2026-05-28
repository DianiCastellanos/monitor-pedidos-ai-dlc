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
        IBrandSnapshotRepository  snapshotRepo)
        => new BrandMonitorChecker(simRepo, snapshotRepo,
            Mock.Of<ILogger<BrandMonitorChecker>>());

    private static Mock<ISimulatedOrderRepository> SimRepo(int actual, int anterior)
    {
        var mock          = new Mock<ISimulatedOrderRepository>();
        var dictActual    = BrandSnapshot.Sites.ToDictionary(s => s, _ => actual);
        var dictAnterior  = BrandSnapshot.Sites.ToDictionary(s => s, _ => anterior);

        mock.SetupSequence(r => r.CountBySiteAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dictActual)
            .ReturnsAsync(dictAnterior);

        return mock;
    }

    private static Mock<IBrandSnapshotRepository> SnapshotRepo()
    {
        var mock = new Mock<IBrandSnapshotRepository>();
        mock.Setup(r => r.UpsertAsync(It.IsAny<BrandSnapshot>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return mock;
    }

    [Fact]
    public async Task ExecuteAsync_BacklogBaja_StatusGreen()
    {
        // actual(5) < anterior(10) → backlog bajando → OK
        var captured = new List<BrandSnapshot>();
        var snap = SnapshotRepo();
        snap.Setup(r => r.UpsertAsync(It.IsAny<BrandSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<BrandSnapshot, CancellationToken>((s, _) => captured.Add(s))
            .Returns(Task.CompletedTask);

        var result = await BuildChecker(SimRepo(actual: 5, anterior: 10).Object, snap.Object).ExecuteAsync();

        Assert.Equal(CheckStatus.Ok, result.Status);
        Assert.All(captured, s => Assert.Equal(SnapshotStatus.Green, s.Status));
    }

    [Fact]
    public async Task ExecuteAsync_BacklogIgual_StatusYellow()
    {
        // actual(10) == anterior(10) → sin cambio → WARNING
        var captured = new List<BrandSnapshot>();
        var snap = SnapshotRepo();
        snap.Setup(r => r.UpsertAsync(It.IsAny<BrandSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<BrandSnapshot, CancellationToken>((s, _) => captured.Add(s))
            .Returns(Task.CompletedTask);

        var result = await BuildChecker(SimRepo(actual: 10, anterior: 10).Object, snap.Object).ExecuteAsync();

        Assert.Equal(CheckStatus.Ok, result.Status);
        Assert.All(captured, s => Assert.Equal(SnapshotStatus.Yellow, s.Status));
    }

    [Fact]
    public async Task ExecuteAsync_BacklogCrece_StatusRed()
    {
        // actual(20) > anterior(10) → backlog creciendo → CRITICAL
        var captured = new List<BrandSnapshot>();
        var snap = SnapshotRepo();
        snap.Setup(r => r.UpsertAsync(It.IsAny<BrandSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<BrandSnapshot, CancellationToken>((s, _) => captured.Add(s))
            .Returns(Task.CompletedTask);

        var result = await BuildChecker(SimRepo(actual: 20, anterior: 10).Object, snap.Object).ExecuteAsync();

        Assert.Equal(CheckStatus.Ok, result.Status);
        Assert.All(captured, s => Assert.Equal(SnapshotStatus.Red, s.Status));
    }

    [Fact]
    public async Task ExecuteAsync_SinDatos_MuestraCerosYStatusYellow()
    {
        // Sin datos en ninguna ventana → actual=0, anterior=0 → Yellow (igual)
        var captured = new List<BrandSnapshot>();
        var snap = SnapshotRepo();
        snap.Setup(r => r.UpsertAsync(It.IsAny<BrandSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<BrandSnapshot, CancellationToken>((s, _) => captured.Add(s))
            .Returns(Task.CompletedTask);

        var result = await BuildChecker(SimRepo(actual: 0, anterior: 0).Object, snap.Object).ExecuteAsync();

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
