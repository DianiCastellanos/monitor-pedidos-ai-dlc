using Serilog.Core;
using Serilog.Events;

namespace MonitorPedidos.Infrastructure.Logging;

public sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "token", "secret", "credential",
        "authorization", "apikey", "api_key", "accesstoken"
    };

    public bool TryDestructure(object value, ILogEventPropertyValueFactory factory,
        out LogEventPropertyValue result)
    {
        if (value is IDictionary<string, object> dict)
        {
            var sanitized = dict
                .Select(kv => new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                    new ScalarValue(kv.Key),
                    SensitiveKeys.Contains(kv.Key)
                        ? new ScalarValue("[REDACTED]")
                        : factory.CreatePropertyValue(kv.Value, true)))
                .ToList();

            result = new DictionaryValue(sanitized);
            return true;
        }

        result = null!;
        return false;
    }
}
