using MonitorPedidos.Domain.Monitoring;
using Polly;

namespace MonitorPedidos.Web.Features.ApiChecks;

public static class PollyContextExtensions
{
    private const string Key = "retryAttempts";

    public static List<RetryAttempt> GetRetryAttempts(this Context context)
    {
        if (!context.TryGetValue(Key, out var value))
        {
            value = new List<RetryAttempt>();
            context[Key] = value;
        }
        return (List<RetryAttempt>)value;
    }
}
