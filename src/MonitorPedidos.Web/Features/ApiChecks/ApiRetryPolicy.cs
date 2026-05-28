using Polly;
using Polly.Extensions.Http;

namespace MonitorPedidos.Web.Features.ApiChecks;

public static class ApiRetryPolicy
{
    /// <summary>
    /// Máximo 2 reintentos con backoff exponencial (2s, 4s).
    /// Solo ante 5xx y errores de red. Nunca ante 401.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> Create(ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, timespan, attempt, context) =>
                {
                    context.GetRetryAttempts().Add(new Domain.Monitoring.RetryAttempt(
                        AttemptNumber:  attempt,
                        HttpStatusCode: (int)(outcome.Result?.StatusCode ?? 0),
                        LatencyMs:      0,
                        AttemptedAt:    DateTimeOffset.UtcNow));

                    logger.LogWarning(
                        "API retry {Attempt}/2 — HTTP {Status} — waiting {Wait:F1}s",
                        attempt,
                        (int)(outcome.Result?.StatusCode ?? 0),
                        timespan.TotalSeconds);
                });
    }
}
