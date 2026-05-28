using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MonitorPedidos.Domain.Simulation;
using MonitorPedidos.Web.BackgroundServices;
using Moq;
using Xunit;

namespace MonitorPedidos.UnitTests.Simulation;

public class OrdersSimulatorServiceTests
{
    private static OrdersSimulatorService BuildService(SimulationOptions opts) =>
        new OrdersSimulatorService(
            Mock.Of<IServiceScopeFactory>(),
            Options.Create(opts),
            Mock.Of<ILogger<OrdersSimulatorService>>());

    [Fact]
    public async Task RunOneTickAsync_NoOrdersMode_DoesNotInsert()
    {
        // BR-SIM-02: NoOrdersMode=true → ningún pedido insertado
        var repo = new Mock<ISimulatedOrderRepository>();
        var svc  = BuildService(new SimulationOptions { Enabled = true, NoOrdersMode = true });

        await svc.RunOneTickAsync(repo.Object, CancellationToken.None);

        repo.Verify(
            r => r.InsertRangeAsync(It.IsAny<IEnumerable<SimulatedOrder>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RunOneTickAsync_FailureProbability1_AllOrdersAreFailures()
    {
        // BR-SIM-03: FailureProbability=1.0 → todos IsFailure=true
        IEnumerable<SimulatedOrder>? captured = null;
        var repo = new Mock<ISimulatedOrderRepository>();
        repo.Setup(r => r.InsertRangeAsync(It.IsAny<IEnumerable<SimulatedOrder>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<SimulatedOrder>, CancellationToken>((orders, _) => captured = orders)
            .Returns(Task.CompletedTask);

        var svc = BuildService(new SimulationOptions { Enabled = true, FailureProbability = 1.0 });
        await svc.RunOneTickAsync(repo.Object, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.All(captured!, o => Assert.True(o.IsFailure));
    }

    [Fact]
    public async Task RunOneTickAsync_FailureProbability0_NoOrdersAreFailures()
    {
        // BR-SIM-03: FailureProbability=0.0 → todos IsFailure=false
        IEnumerable<SimulatedOrder>? captured = null;
        var repo = new Mock<ISimulatedOrderRepository>();
        repo.Setup(r => r.InsertRangeAsync(It.IsAny<IEnumerable<SimulatedOrder>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<SimulatedOrder>, CancellationToken>((orders, _) => captured = orders)
            .Returns(Task.CompletedTask);

        var svc = BuildService(new SimulationOptions { Enabled = true, FailureProbability = 0.0 });
        await svc.RunOneTickAsync(repo.Object, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.All(captured!, o => Assert.False(o.IsFailure));
    }

    [Fact]
    public async Task RunOneTickAsync_NormalMode_InsertsEightOrders()
    {
        // BR-SIM-04: 2 sources × 4 sites = 8 pedidos por tick
        IEnumerable<SimulatedOrder>? captured = null;
        var repo = new Mock<ISimulatedOrderRepository>();
        repo.Setup(r => r.InsertRangeAsync(It.IsAny<IEnumerable<SimulatedOrder>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<SimulatedOrder>, CancellationToken>((orders, _) => captured = orders)
            .Returns(Task.CompletedTask);

        var svc = BuildService(new SimulationOptions { Enabled = true, FailureProbability = 0.0, NoOrdersMode = false });
        await svc.RunOneTickAsync(repo.Object, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(8, captured!.Count());
    }
}
