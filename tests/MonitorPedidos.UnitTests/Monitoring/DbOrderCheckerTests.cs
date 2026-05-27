using Microsoft.Extensions.Configuration;
using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Shared;
using MonitorPedidos.Web.Features.Monitoring;
using Moq;
using Xunit;

namespace MonitorPedidos.UnitTests.Monitoring;

public class DbOrderCheckerTests
{
    private static IConfiguration BuildConfig(int windowMinutes = 30) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Monitoring:OrderDetectionWindowMinutes"] = windowMinutes.ToString()
            })
            .Build();

    [Fact]
    public async Task ExecuteAsync_NoOrders_ReturnsCritical()
    {
        var source = new Mock<IOrderSource>();
        source.Setup(s => s.GetOrdersInWindowAsync(It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<OrderSnapshot>());

        var checker = new DbOrderChecker(source.Object, BuildConfig());
        var result  = await checker.ExecuteAsync();

        Assert.Equal(CheckStatus.Critical, result.Status);
        Assert.True(result.RequiresIncident);
    }

    [Fact]
    public async Task ExecuteAsync_WithActiveOrders_ReturnsOk()
    {
        var source = new Mock<IOrderSource>();
        source.Setup(s => s.GetOrdersInWindowAsync(It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new OrderSnapshot("ORD-1", DateTimeOffset.UtcNow, false) });

        var checker = new DbOrderChecker(source.Object, BuildConfig());
        var result  = await checker.ExecuteAsync();

        Assert.Equal(CheckStatus.Ok, result.Status);
        Assert.False(result.RequiresIncident);
    }

    [Fact]
    public async Task ExecuteAsync_OnlyCancelledOrders_ReturnsCritical()
    {
        var source = new Mock<IOrderSource>();
        source.Setup(s => s.GetOrdersInWindowAsync(It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new OrderSnapshot("ORD-1", DateTimeOffset.UtcNow, true),
                new OrderSnapshot("ORD-2", DateTimeOffset.UtcNow, true),
            });

        var checker = new DbOrderChecker(source.Object, BuildConfig());
        var result  = await checker.ExecuteAsync();

        Assert.Equal(CheckStatus.Critical, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_MixedOrders_ActiveCountInDetails()
    {
        var source = new Mock<IOrderSource>();
        source.Setup(s => s.GetOrdersInWindowAsync(It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new OrderSnapshot("ORD-1", DateTimeOffset.UtcNow, false),
                new OrderSnapshot("ORD-2", DateTimeOffset.UtcNow, true),
            });

        var checker = new DbOrderChecker(source.Object, BuildConfig());
        var result  = await checker.ExecuteAsync();

        Assert.Equal(CheckStatus.Ok, result.Status);
        Assert.Contains("1 pedido", result.Details);
    }

    [Fact]
    public void Module_IsDbOrderChecker()
    {
        var checker = new DbOrderChecker(Mock.Of<IOrderSource>(), Mock.Of<IConfiguration>());
        Assert.Equal(ModuleId.DbOrderChecker, checker.Module);
    }
}
