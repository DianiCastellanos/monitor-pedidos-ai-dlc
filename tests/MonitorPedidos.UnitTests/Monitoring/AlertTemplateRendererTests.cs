using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Shared;
using MonitorPedidos.Web.Features.Monitoring;
using Xunit;

namespace MonitorPedidos.UnitTests.Monitoring;

public class AlertTemplateRendererTests
{
    private static CheckContext Ctx(ModuleId module, CheckStatus status, CauseCategory cause, string details = "test")
        => new(module, status, cause, details, DateTimeOffset.UtcNow);

    [Fact]
    public void Render_BdCritical_ReturnsCriticalTemplate()
    {
        var alert = AlertTemplateRenderer.Render(Ctx(ModuleId.DbOrderChecker, CheckStatus.Critical, CauseCategory.Bd));

        Assert.Contains("DbOrderChecker", alert.QuePaso);
        Assert.Equal("CRÍTICO", alert.SeveridadTexto);
        Assert.Contains("Base de Datos", alert.Donde);
    }

    [Fact]
    public void Render_BdWarn_ReturnsWarnTemplate()
    {
        var alert = AlertTemplateRenderer.Render(Ctx(ModuleId.DbHealthChecker, CheckStatus.Warn, CauseCategory.Bd));

        Assert.Equal("ADVERTENCIA", alert.SeveridadTexto);
        Assert.Contains("latencia", alert.QuePaso, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_JobCritical_ReturnsJobTemplate()
    {
        var alert = AlertTemplateRenderer.Render(Ctx(ModuleId.JobsMonitor, CheckStatus.Critical, CauseCategory.Job));

        Assert.Equal("CRÍTICO", alert.SeveridadTexto);
        Assert.Contains("job", alert.QuePaso, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Jobs", alert.Donde);
    }

    [Fact]
    public void Render_NoDeterminada_ReturnsCandidatoTemplate()
    {
        var alert = AlertTemplateRenderer.Render(Ctx(ModuleId.DbOrderChecker, CheckStatus.Critical, CauseCategory.NoDeterminada));

        Assert.Equal("CRÍTICO", alert.SeveridadTexto);
        Assert.Contains("candidato", alert.CausaProbable, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Render_UnknownCombination_ReturnsFallback()
    {
        // (Job, Warn) has no dedicated template → fallback
        var alert = AlertTemplateRenderer.Render(Ctx(ModuleId.JobsMonitor, CheckStatus.Warn, CauseCategory.Job));

        Assert.NotNull(alert.QuePaso);
        Assert.Equal("ADVERTENCIA", alert.SeveridadTexto);
        Assert.Contains("Sin plantilla", alert.CausaProbable);
    }

    [Fact]
    public void Render_AllTemplates_HaveNonNullFields()
    {
        var combinations = new[]
        {
            Ctx(ModuleId.DbOrderChecker,  CheckStatus.Critical, CauseCategory.Bd),
            Ctx(ModuleId.DbHealthChecker, CheckStatus.Warn,     CauseCategory.Bd),
            Ctx(ModuleId.JobsMonitor,     CheckStatus.Critical, CauseCategory.Job),
        };

        foreach (var ctx in combinations)
        {
            var alert = AlertTemplateRenderer.Render(ctx);
            Assert.False(string.IsNullOrWhiteSpace(alert.QuePaso),       $"QuePaso null for {ctx.Cause}/{ctx.Status}");
            Assert.False(string.IsNullOrWhiteSpace(alert.Donde),         $"Donde null for {ctx.Cause}/{ctx.Status}");
            Assert.False(string.IsNullOrWhiteSpace(alert.SeveridadTexto),$"SeveridadTexto null for {ctx.Cause}/{ctx.Status}");
            Assert.False(string.IsNullOrWhiteSpace(alert.CausaProbable), $"CausaProbable null for {ctx.Cause}/{ctx.Status}");
            Assert.False(string.IsNullOrWhiteSpace(alert.AccionSugerida),$"AccionSugerida null for {ctx.Cause}/{ctx.Status}");
        }
    }
}
