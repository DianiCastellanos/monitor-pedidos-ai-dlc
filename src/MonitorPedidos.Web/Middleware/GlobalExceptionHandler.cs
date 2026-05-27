using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using System.Text.Json;

namespace MonitorPedidos.Web.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        logger.LogError(
            exception,
            "excepcion_no_manejada | type={Type} | rid={RequestId}",
            exception.GetType().Name,
            context.TraceIdentifier);

        var acceptHeader = context.Request.Headers.Accept.ToString();
        var isApiRequest = acceptHeader.Contains("application/json")
                        || context.Request.Path.StartsWithSegments("/api");

        if (isApiRequest)
        {
            context.Response.StatusCode  = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new { error = "Error interno del servidor" }), ct);
        }
        else
        {
            context.Response.Redirect("/Error");
        }

        return true;
    }
}
