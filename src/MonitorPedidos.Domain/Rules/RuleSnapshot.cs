using System.Text.Json;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Domain.Rules;

public sealed record RuleSnapshot(
    Guid            Id,
    string          Name,
    string          Description,
    string          AppliesTo,
    RuleCondition   Condition,
    string          Severity,
    bool            IsActive,
    DateTimeOffset  CreatedAt,
    DateTimeOffset? UpdatedAt
)
{
    private static readonly JsonSerializerOptions _opts = new(JsonSerializerDefaults.Web);

    public static RuleSnapshot From(Rule rule) =>
        new(rule.Id, rule.Name, rule.Description,
            rule.AppliesTo.ToString(),
            rule.GetCondition(),
            rule.Severity.ToString(),
            rule.IsActive,
            rule.CreatedAt,
            rule.UpdatedAt);

    public string ToJson() => JsonSerializer.Serialize(this, _opts);
}
