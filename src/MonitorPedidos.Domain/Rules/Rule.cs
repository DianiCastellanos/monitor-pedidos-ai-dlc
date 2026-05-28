using System.Text.Json;
using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Domain.Rules;

public sealed class Rule
{
    private static readonly JsonSerializerOptions _opts = new(JsonSerializerDefaults.Web);

    public Guid            Id            { get; private set; }
    public string          Name          { get; private set; } = string.Empty;
    public string          Description   { get; private set; } = string.Empty;
    public ModuleId        AppliesTo     { get; private set; }
    public string          ConditionJson { get; private set; } = string.Empty;
    public Severity        Severity      { get; private set; }
    public bool            IsActive      { get; private set; }
    public DateTimeOffset  CreatedAt     { get; private set; }
    public DateTimeOffset? UpdatedAt     { get; private set; }

    private Rule() { }

    public static Rule Create(
        string        name,
        string        description,
        ModuleId      appliesTo,
        RuleCondition condition,
        Severity      severity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (!condition.IsValidForModule(appliesTo))
            throw new ArgumentException(
                $"Condición inválida para módulo {appliesTo}.", nameof(condition));

        return new Rule
        {
            Id            = Guid.NewGuid(),
            Name          = name,
            Description   = description,
            AppliesTo     = appliesTo,
            ConditionJson = JsonSerializer.Serialize(condition, _opts),
            Severity      = severity,
            IsActive      = true,
            CreatedAt     = DateTimeOffset.UtcNow
        };
    }

    public void Update(string name, string description, RuleCondition condition, Severity severity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (!condition.IsValidForModule(AppliesTo))
            throw new ArgumentException(
                $"Condición inválida para módulo {AppliesTo}.", nameof(condition));

        Name          = name;
        Description   = description;
        ConditionJson = JsonSerializer.Serialize(condition, _opts);
        Severity      = severity;
        UpdatedAt     = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        if (IsActive) return;
        IsActive  = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive  = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public RuleCondition GetCondition()
        => JsonSerializer.Deserialize<RuleCondition>(ConditionJson, _opts)!;
}
