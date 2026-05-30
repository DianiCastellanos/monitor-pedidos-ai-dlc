using Microsoft.Extensions.Logging;
using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Rules;
using MonitorPedidos.Domain.Shared;
using MonitorPedidos.Web.Features.Monitoring;
using Moq;
using Xunit;

namespace MonitorPedidos.UnitTests.Monitoring;

public class DbOrderCheckerTests
{
    private static Mock<IRuleRepository> RuleRepoWithActiveRule(int windowMinutes = 10, int minOrders = 1)
    {
        var rule = Rule.Create("Regla test", "Desc",
            ModuleId.DbOrderChecker,
            RuleCondition.ForDbOrders(windowMinutes, minOrders),
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

    private static ILogger<DbOrderChecker> Logger() =>
        Mock.Of<ILogger<DbOrderChecker>>();

    [Fact]
    public async Task ExecuteAsync_NoOrders_ReturnsCritical()
    {
        var source = new Mock<IOrderSource>();
        source.Setup(s => s.GetOrdersInWindowAsync(It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<OrderSnapshot>());

        var checker = new DbOrderChecker(source.Object, RuleRepoWithActiveRule().Object, Logger());
        var result  = await checker.ExecuteAsync();

        Assert.Equal(CheckStatus.Critical, result.Status);
        Assert.True(result.RequiresIncident);
    }

    [Fact]
    public async Task ExecuteAsync_AllChannelsHaveOrders_ReturnsOk()
    {
        var source = new Mock<IOrderSource>();
        source.Setup(s => s.GetOrdersInWindowAsync(It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new OrderSnapshot("SALESFORCE", DateTimeOffset.UtcNow, false),
                new OrderSnapshot("MULTIVENDE", DateTimeOffset.UtcNow, false),
            });

        var checker = new DbOrderChecker(source.Object, RuleRepoWithActiveRule().Object, Logger());
        var result  = await checker.ExecuteAsync();

        Assert.Equal(CheckStatus.Ok, result.Status);
        Assert.False(result.RequiresIncident);
        Assert.Contains("SALESFORCE", result.Details);
        Assert.Contains("MULTIVENDE", result.Details);
    }

    [Fact]
    public async Task ExecuteAsync_OnlyCancelledOrders_ReturnsCritical()
    {
        var source = new Mock<IOrderSource>();
        source.Setup(s => s.GetOrdersInWindowAsync(It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new OrderSnapshot("SALESFORCE", DateTimeOffset.UtcNow, true),
                new OrderSnapshot("MULTIVENDE", DateTimeOffset.UtcNow, true),
            });

        var checker = new DbOrderChecker(source.Object, RuleRepoWithActiveRule().Object, Logger());
        var result  = await checker.ExecuteAsync();

        Assert.Equal(CheckStatus.Critical, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_SalesforceZero_ReturnsCritical()
    {
        var source = new Mock<IOrderSource>();
        source.Setup(s => s.GetOrdersInWindowAsync(It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new OrderSnapshot("MULTIVENDE", DateTimeOffset.UtcNow, false),
            });

        var checker = new DbOrderChecker(source.Object, RuleRepoWithActiveRule().Object, Logger());
        var result  = await checker.ExecuteAsync();

        Assert.Equal(CheckStatus.Critical, result.Status);
        Assert.Contains("SALESFORCE:0:CRITICAL", result.Details);
        Assert.Contains("MULTIVENDE:1:OK", result.Details);
    }

    [Fact]
    public async Task ExecuteAsync_BelowMinOrders_ReturnsCritical()
    {
        var source = new Mock<IOrderSource>();
        source.Setup(s => s.GetOrdersInWindowAsync(It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new OrderSnapshot("SALESFORCE", DateTimeOffset.UtcNow, false),
                new OrderSnapshot("MULTIVENDE", DateTimeOffset.UtcNow, false),
            });

        // minOrders = 5 → 1 por canal no alcanza
        var checker = new DbOrderChecker(source.Object, RuleRepoWithActiveRule(windowMinutes: 10, minOrders: 5).Object, Logger());
        var result  = await checker.ExecuteAsync();

        Assert.Equal(CheckStatus.Critical, result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_NoActiveRules_UsesDefaultsAndReturnsCritical_WhenNoOrders()
    {
        var source = new Mock<IOrderSource>();
        source.Setup(s => s.GetOrdersInWindowAsync(It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<OrderSnapshot>());

        var checker = new DbOrderChecker(source.Object, RuleRepoNoRules().Object, Logger());
        var result  = await checker.ExecuteAsync();

        Assert.Equal(CheckStatus.Critical, result.Status);
    }

    [Fact]
    public void Module_IsDbOrderChecker()
    {
        var checker = new DbOrderChecker(Mock.Of<IOrderSource>(), Mock.Of<IRuleRepository>(), Logger());
        Assert.Equal(ModuleId.DbOrderChecker, checker.Module);
    }
}
