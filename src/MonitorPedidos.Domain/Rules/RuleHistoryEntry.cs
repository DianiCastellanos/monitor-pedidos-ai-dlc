using MonitorPedidos.Domain.Shared;

namespace MonitorPedidos.Domain.Rules;

public sealed class RuleHistoryEntry
{
    public Guid           Id             { get; private set; }
    public Guid           RuleId         { get; private set; }
    public RuleChangeType ChangeType     { get; private set; }
    public string         AuthorUserId   { get; private set; } = string.Empty;
    public string         Reason         { get; private set; } = string.Empty;
    public string?        SnapshotBefore { get; private set; }
    public string?        SnapshotAfter  { get; private set; }
    public DateTimeOffset ChangedAt      { get; private set; }

    private RuleHistoryEntry() { }

    public static RuleHistoryEntry ForCreation(
        Guid ruleId, string authorUserId, string reason, Rule rule) =>
        new()
        {
            Id             = Guid.NewGuid(),
            RuleId         = ruleId,
            ChangeType     = RuleChangeType.Creation,
            AuthorUserId   = authorUserId,
            Reason         = reason,
            SnapshotBefore = null,
            SnapshotAfter  = RuleSnapshot.From(rule).ToJson(),
            ChangedAt      = DateTimeOffset.UtcNow
        };

    public static RuleHistoryEntry ForEdit(
        Guid ruleId, string authorUserId, string reason,
        Rule ruleBefore, Rule ruleAfter) =>
        new()
        {
            Id             = Guid.NewGuid(),
            RuleId         = ruleId,
            ChangeType     = RuleChangeType.Edit,
            AuthorUserId   = authorUserId,
            Reason         = reason,
            SnapshotBefore = RuleSnapshot.From(ruleBefore).ToJson(),
            SnapshotAfter  = RuleSnapshot.From(ruleAfter).ToJson(),
            ChangedAt      = DateTimeOffset.UtcNow
        };

    public static RuleHistoryEntry ForActivation(
        Guid ruleId, string authorUserId, string reason, Rule rule) =>
        new()
        {
            Id             = Guid.NewGuid(),
            RuleId         = ruleId,
            ChangeType     = RuleChangeType.Activation,
            AuthorUserId   = authorUserId,
            Reason         = reason,
            SnapshotBefore = null,
            SnapshotAfter  = RuleSnapshot.From(rule).ToJson(),
            ChangedAt      = DateTimeOffset.UtcNow
        };

    public static RuleHistoryEntry ForEdit(
        Guid ruleId, string authorUserId, string reason,
        string snapshotBeforeJson, string snapshotAfterJson) =>
        new()
        {
            Id             = Guid.NewGuid(),
            RuleId         = ruleId,
            ChangeType     = RuleChangeType.Edit,
            AuthorUserId   = authorUserId,
            Reason         = reason,
            SnapshotBefore = snapshotBeforeJson,
            SnapshotAfter  = snapshotAfterJson,
            ChangedAt      = DateTimeOffset.UtcNow
        };

    public static RuleHistoryEntry ForDeactivation(
        Guid ruleId, string authorUserId, string reason, Rule rule) =>
        new()
        {
            Id             = Guid.NewGuid(),
            RuleId         = ruleId,
            ChangeType     = RuleChangeType.Deactivation,
            AuthorUserId   = authorUserId,
            Reason         = reason,
            SnapshotBefore = RuleSnapshot.From(rule).ToJson(),
            SnapshotAfter  = null,
            ChangedAt      = DateTimeOffset.UtcNow
        };
}
