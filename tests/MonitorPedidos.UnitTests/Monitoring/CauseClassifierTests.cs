using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Rules;
using MonitorPedidos.Domain.Shared;
using MonitorPedidos.Web.Features.Monitoring;
using Moq;
using Xunit;

namespace MonitorPedidos.UnitTests.Monitoring;

public class CauseClassifierTests
{
    [Fact]
    public void Classify_DbOrderChecker_ReturnsBd()
    {
        var checker = new DbOrderChecker(Mock.Of<IOrderSource>(), Mock.Of<IRuleRepository>());
        Assert.Equal(CauseCategory.Bd, CauseClassifier.Classify(checker));
    }

    [Fact]
    public void Classify_JobsChecker_ReturnsJob()
    {
        var checker = new JobsChecker(Mock.Of<IJobStatusSource>());
        Assert.Equal(CauseCategory.Job, CauseClassifier.Classify(checker));
    }

    [Fact]
    public void Classify_UnknownChecker_ReturnsNoDeterminada()
    {
        var checker = Mock.Of<ICheckExecutor>();
        Assert.Equal(CauseCategory.NoDeterminada, CauseClassifier.Classify(checker));
    }

    [Fact]
    public void Classify_DbHealthChecker_ReturnsBd()
    {
        // Verify static map contains DbHealthChecker without instantiating it
        var mapField = typeof(CauseClassifier)
            .GetField("_map", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var map = (IReadOnlyDictionary<Type, CauseCategory>)mapField.GetValue(null)!;
        Assert.Equal(CauseCategory.Bd, map[typeof(DbHealthChecker)]);
    }
}
