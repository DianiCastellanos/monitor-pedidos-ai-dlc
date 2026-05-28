using MonitorPedidos.Domain.Alerts;
using MonitorPedidos.Domain.Monitoring;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Web.Features.Monitoring;

public static class AlertTemplateRenderer
{
    private static readonly IReadOnlyDictionary<(CauseCategory, Severity), Func<CheckContext, AlertMessage>>
        _templates = new Dictionary<(CauseCategory, Severity), Func<CheckContext, AlertMessage>>
        {
            [(CauseCategory.Bd, Severity.Critical)] = ctx => new AlertMessage(
                QuePaso:        $"No se detectaron pedidos activos en el módulo {ctx.Module}.",
                Cuando:         ctx.DetectedAt,
                Donde:          $"Módulo {ctx.Module} — Base de Datos",
                SeveridadTexto: "CRÍTICO",
                CausaProbable:  "Posible falla en la base de datos o ausencia real de transacciones.",
                AccionSugerida: "Verificar conectividad con la BD. Revisar logs del motor SQL."),

            [(CauseCategory.Bd, Severity.Warn)] = ctx => new AlertMessage(
                QuePaso:        $"Latencia alta en la base de datos del módulo {ctx.Module}.",
                Cuando:         ctx.DetectedAt,
                Donde:          $"Módulo {ctx.Module} — Base de Datos",
                SeveridadTexto: "ADVERTENCIA",
                CausaProbable:  "Degradación de rendimiento de la BD. Posible sobrecarga.",
                AccionSugerida: "Monitorear latencia. Revisar queries activas en la base de datos."),

            [(CauseCategory.Job, Severity.Critical)] = ctx => new AlertMessage(
                QuePaso:        $"Job de integración detenido en el módulo {ctx.Module}.",
                Cuando:         ctx.DetectedAt,
                Donde:          $"Módulo {ctx.Module} — Servicio de Jobs",
                SeveridadTexto: "CRÍTICO",
                CausaProbable:  "Job de descarga de pedidos detenido o con error en última ejecución.",
                AccionSugerida: "Revisar consola de administración de jobs. Reiniciar si es necesario."),

            [(CauseCategory.NoDeterminada, Severity.Critical)] = ctx => new AlertMessage(
                QuePaso:        $"Anomalía detectada en módulo {ctx.Module} — causa no identificada.",
                Cuando:         ctx.DetectedAt,
                Donde:          $"Módulo {ctx.Module}",
                SeveridadTexto: "CRÍTICO",
                CausaProbable:  "No fue posible determinar la causa automáticamente. Marcado como candidato a regla nueva.",
                AccionSugerida: "Revisar incidente en el historial y crear regla personalizada si el patrón se repite."),

            [(CauseCategory.Api, Severity.Critical)] = ctx => new AlertMessage(
                QuePaso:        $"API externa no responde en módulo {ctx.Module}.",
                Cuando:         ctx.DetectedAt,
                Donde:          $"Módulo {ctx.Module} — API Externa",
                SeveridadTexto: "CRÍTICO",
                CausaProbable:  "La API externa no responde o retorna error de servidor (5xx). Se realizaron 2 reintentos automáticos.",
                AccionSugerida: "Verificar estado de la API. Revisar logs de reintentos en el detalle del incidente."),

            [(CauseCategory.Token, Severity.Critical)] = ctx => new AlertMessage(
                QuePaso:        $"Token de autenticación inválido o expirado en módulo {ctx.Module}.",
                Cuando:         ctx.DetectedAt,
                Donde:          $"Módulo {ctx.Module} — API Externa",
                SeveridadTexto: "CRÍTICO",
                CausaProbable:  "La API retornó HTTP 401. El token de acceso ha expirado o fue revocado.",
                AccionSugerida: "Renovar token de acceso manualmente según procedimiento SOP-001."),
        };

    private static AlertMessage Fallback(CheckContext ctx) => new(
        QuePaso:        $"Anomalía en módulo {ctx.Module}: {ctx.CheckDetails}",
        Cuando:         ctx.DetectedAt,
        Donde:          $"Módulo {ctx.Module}",
        SeveridadTexto: ctx.Status == CheckStatus.Critical ? "CRÍTICO" : "ADVERTENCIA",
        CausaProbable:  "Sin plantilla específica para esta combinación.",
        AccionSugerida: "Revisar logs del sistema para más detalles.");

    public static AlertMessage Render(CheckContext ctx)
    {
        var severity = ctx.Status == CheckStatus.Critical ? Severity.Critical : Severity.Warn;
        return _templates.TryGetValue((ctx.Cause, severity), out var template)
            ? template(ctx)
            : Fallback(ctx);
    }
}
