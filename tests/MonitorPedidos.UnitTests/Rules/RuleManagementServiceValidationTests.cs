using MonitorPedidos.Domain.Rules;
using MonitorPedidos.Domain.Shared;
using MonitorPedidos.Web.Services;
using Moq;
using Xunit;

namespace MonitorPedidos.UnitTests.Rules;

public class RuleManagementServiceValidationTests
{
    private static (RuleManagementService svc, Mock<IRuleRepository> ruleRepo, Mock<IRuleHistoryRepository> histRepo)
        Build()
    {
        var ruleRepo = new Mock<IRuleRepository>();
        var histRepo = new Mock<IRuleHistoryRepository>();
        var svc = new RuleManagementService(ruleRepo.Object, histRepo.Object);
        return (svc, ruleRepo, histRepo);
    }

    [Fact]
    public async Task CreateRule_EmptyReason_ThrowsArgumentException()
    {
        var (svc, _, _) = Build();
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            svc.CreateRuleAsync("N", "D", ModuleId.DbOrderChecker,
                RuleCondition.ForDbOrders(120, 1), Severity.Critical,
                "user1", " "));
    }

    [Fact]
    public async Task UpdateRule_EmptyReason_ThrowsArgumentException()
    {
        var (svc, ruleRepo, _) = Build();
        var rule = Rule.Create("N", "D", ModuleId.DbOrderChecker,
            RuleCondition.ForDbOrders(120, 1), Severity.Critical);
        ruleRepo.Setup(r => r.GetByIdAsync(rule.Id, default)).ReturnsAsync(rule);

        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            svc.UpdateRuleAsync(rule.Id, "N2", "D2",
                RuleCondition.ForDbOrders(180, 2), Severity.Critical,
                "user1", ""));
    }

    [Fact]
    public async Task ActivateRule_EmptyReason_ThrowsArgumentException()
    {
        var (svc, _, _) = Build();
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            svc.ActivateRuleAsync(Guid.NewGuid(), "user1", "  "));
    }

    [Fact]
    public async Task DeactivateRule_EmptyReason_ThrowsArgumentException()
    {
        var (svc, _, _) = Build();
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            svc.DeactivateRuleAsync(Guid.NewGuid(), "user1", ""));
    }

    [Fact]
    public async Task CreateRule_InvalidConditionForModule_ThrowsArgumentException()
    {
        var (svc, _, _) = Build();
        // DbOrders condition applied to DbHealthChecker — invalid
        var badCondition = RuleCondition.ForDbOrders(120, 1);
        await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            svc.CreateRuleAsync("N", "D", ModuleId.DbHealthChecker,
                badCondition, Severity.Warn,
                "user1", "razón válida"));
    }
}
