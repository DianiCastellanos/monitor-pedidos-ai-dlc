using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Rules;
using MonitorPedidos.Domain.Shared;
using MonitorPedidos.Web.Features.Monitoring;
using Moq;
using Xunit;

namespace MonitorPedidos.UnitTests.Monitoring;

public class DbOrderCheckerTests
{
    private static Mock<IRuleRepository> RuleRepoWithActiveRule(int windowHours = 2, int minOrders = 1)
    {
        var rule = Rule.Create("Regla test", "Desc",
            ModuleId.DbOrderChecker,
            RuleCondition.ForDbOrders(windowHours, minOrders),
            Severity.Critical);

        var mock = new Mock<IRuleRepository>();
        mock.Setup(r => r.GetActiveByModuleAsync(ModuleId.DbOrderChecker, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rule> { rule });
        return mock;
    }

    private static Mock<IRuleRepository> RuleRepoNoRules()
    {
        var mock = new Mock<IRuleRepository>();
        mock.Setup(r => r.GetActiveByModuleAsync(ModuleId.DbOrderChecker, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Rule>());
        return mock;
    }

    [Fact]
    public async Task ExecuteAsync_NoOrders_ReturnsCritical()
    {
        var source = new Mock<IOrderSource>();
        source.Setup(s => s.GetOrdersInWindowAsync(It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<OrderSnapshot>());

        var checker = new DbOrderChecker(source.Object, RuleRepoWithActiveRule().Object);
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

        var checker = new DbOrderChecker(source.Object, RuleRepoWithActiveRule().Object);
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

        var checker = new DbOrderChecker(source.Object, RuleRepoWithActiveRule().Object);
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

        var checker = new DbOrderChecker(source.Object, RuleRepoWithActiveRule().Object);
        var result  = await checker.ExecuteAsync();

        Assert.Equal(CheckStatus.Ok, result.Status);
        Assert.Contains("1 pedido", result.Details);
    }

    [Fact]
    public void Module_IsDbOrderChecker()
    {
        var checker = new DbOrderChecker(Mock.Of<IOrderSource>(), Mock.Of<IRuleRepository>());
        Assert.Equal(ModuleId.DbOrderChecker, checker.Module);
    }

    // ── Tests U5 nuevos ───────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_NoActiveRules_ReturnsOk()
    {
        // BR-RULE-08: sin reglas activas → Ok (sin falso positivo)
        var source = new Mock<IOrderSource>();
        source.Setup(s => s.GetOrdersInWindowAsync(It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<OrderSnapshot>());

        var checker = new DbOrderChecker(source.Object, RuleRepoNoRules().Object);
        var result  = await checker.ExecuteAsync();

        Assert.Equal(CheckStatus.Ok, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_ActiveRuleWithThreshold_ZeroOrders_ReturnsCritical()
    {
        var source = new Mock<IOrderSource>();
        source.Setup(s => s.GetOrdersInWindowAsync(It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<OrderSnapshot>());

        // minOrders = 3
        var checker = new DbOrderChecker(source.Object, RuleRepoWithActiveRule(windowHours: 2, minOrders: 3).Object);
        var result  = await checker.ExecuteAsync();

        Assert.Equal(CheckStatus.Critical, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_BelowMinOrders_ReturnsCritical()
    {
        var source = new Mock<IOrderSource>();
        source.Setup(s => s.GetOrdersInWindowAsync(It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new OrderSnapshot("ORD-1", DateTimeOffset.UtcNow, false) });

        // minOrders = 5 → 1 orden no alcanza
        var checker = new DbOrderChecker(source.Object, RuleRepoWithActiveRule(windowHours: 2, minOrders: 5).Object);
        var result  = await checker.ExecuteAsync();

        Assert.Equal(CheckStatus.Critical, result.Status);
    }
}
