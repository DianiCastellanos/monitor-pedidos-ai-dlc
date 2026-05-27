# Domain Entities — U5 Rules Management

**Unidad:** U5 — Rules Management
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-23
**Versión:** 1.1 (2026-05-24 — integra BrandMonitor: PendingDropThreshold en RuleCondition, ModuleId.BrandMonitor, seed regla BrandMonitor)

---

## §1 Decisiones aplicadas

| ID | Pregunta | Decisión |
|----|----------|---------|
| P1 | Formato ConditionExpression | A — JSON estructurado en columna `nvarchar(max)` (`ConditionJson`) |
| P2 | Diff antes/después en historial | A — JSON serialización del estado completo de `Rule` en `SnapshotBefore` / `SnapshotAfter` |
| P3 | Acceso de Operador a páginas de reglas | C — Oculto del menú + redirect a `/access-denied` si accede por URL directa |
| P4 | Reglas semilla (seed data) | A — Seed en migración EF Core de U5 (2 reglas iniciales) |
| P5 | Atomicidad Rule + RuleHistoryEntry | A — Un solo `SaveChangesAsync` (transacción implícita EF Core) |

---

## §2 Bounded Context: RulesManagement

```
┌─────────────────────────────────────────────────────┐
│  Bounded Context: RulesManagement                   │
│                                                     │
│  Aggregate Root: Rule                               │
│    └── ConditionJson → RuleCondition (value object) │
│                                                     │
│  Entity: RuleHistoryEntry                           │
│    └── SnapshotBefore / SnapshotAfter (JSON)        │
│                                                     │
│  Value Objects: RuleCondition, RuleSnapshot         │
│  Enums: RuleChangeType (U1), ModuleId (U1/ext U5),  │
│         Severity (U1)                               │
│                                                     │
│  Contratos: IRuleRepository, IRuleHistoryRepository │
│  Service: IRuleManagementService                    │
└─────────────────────────────────────────────────────┘
```

> **Nota ModuleId (v1.1):** `ModuleId` está definido en U1 (Domain/Shared). U5 lo extiende con el valor `BrandMonitor = 4`, requerido por `BrandMonitorChecker` (U6/U3) y `RuleCondition.IsValidForModule`. El enum completo activo:
>
> ```csharp
> public enum ModuleId { DbOrders = 1, DbHealth = 2, Jobs = 3, BrandMonitor = 4 }
> ```

---

## §3 Value object — RuleCondition

`RuleCondition` representa los parámetros configurables de una regla. Se serializa a JSON y se almacena en `Rule.ConditionJson`.

```csharp
/// <summary>
/// Condición evaluable de una regla. No todos los campos aplican a todos los módulos.
/// DbOrders usa WindowHours + MinOrders.
/// DbHealth usa LatencyWarnMs + LatencyCriticalMs.
/// Jobs: sin parámetros numéricos (solo verifica que el job esté activo).
/// BrandMonitor usa PendingDropThreshold (RF-31 — umbral de caída de pedidos pendientes por site).
/// </summary>
public sealed record RuleCondition(
    int?  WindowHours,           // DbOrders: pedidos esperados en las últimas N horas
    int?  MinOrders,             // DbOrders: cantidad mínima de pedidos en la ventana
    int?  LatencyWarnMs,         // DbHealth: umbral en ms para emitir WARN
    int?  LatencyCriticalMs,     // DbHealth: umbral en ms para emitir CRITICAL
    int?  PendingDropThreshold   // BrandMonitor: caída mínima de pendientes para semáforo Verde
)
{
    // Factories por módulo
    public static RuleCondition ForDbOrders(int windowHours, int minOrders)
        => new(windowHours, minOrders, null, null, null);

    public static RuleCondition ForDbHealth(int latencyWarnMs, int latencyCriticalMs)
        => new(null, null, latencyWarnMs, latencyCriticalMs, null);

    public static RuleCondition ForJobs()
        => new(null, null, null, null, null);

    public static RuleCondition ForBrandMonitor(int pendingDropThreshold)
        => new(null, null, null, null, pendingDropThreshold);

    public bool IsValidForModule(ModuleId module) => module switch
    {
        ModuleId.DbOrders     => WindowHours.HasValue && MinOrders.HasValue,
        ModuleId.DbHealth     => LatencyWarnMs.HasValue && LatencyCriticalMs.HasValue,
        ModuleId.Jobs         => true,
        ModuleId.BrandMonitor => PendingDropThreshold.HasValue,
        _                     => false
    };
}
```

---

## §4 Value object — RuleSnapshot

`RuleSnapshot` es la serialización completa del estado de una `Rule` en un punto en el tiempo. Se usa para `SnapshotBefore` y `SnapshotAfter` en `RuleHistoryEntry`.

```csharp
/// <summary>
/// Foto inmutable del estado de una Rule en el momento de una edición.
/// Se serializa a JSON y se almacena en RuleHistoryEntry.
/// </summary>
public sealed record RuleSnapshot(
    Guid          Id,
    string        Name,
    string        Description,
    string        AppliesTo,     // ModuleId.ToString()
    RuleCondition Condition,
    string        Severity,      // Severity.ToString()
    bool          IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
)
{
    public static RuleSnapshot From(Rule rule) =>
        new(rule.Id, rule.Name, rule.Description,
            rule.AppliesTo.ToString(),
            rule.GetCondition(),
            rule.Severity.ToString(),
            rule.IsActive,
            rule.CreatedAt,
            rule.UpdatedAt);

    public string ToJson() => JsonSerializer.Serialize(this);
}
```

---

## §5 Aggregate Root — Rule

```csharp
public sealed class Rule
{
    public Guid           Id           { get; private set; }
    public string         Name         { get; private set; } = string.Empty;
    public string         Description  { get; private set; } = string.Empty;
    public ModuleId       AppliesTo    { get; private set; }
    public string         ConditionJson { get; private set; } = string.Empty;
    public Severity       Severity     { get; private set; }
    public bool           IsActive     { get; private set; }
    public DateTimeOffset CreatedAt    { get; private set; }
    public DateTimeOffset? UpdatedAt   { get; private set; }

    private Rule() { }  // EF Core

    // ── Factory ──────────────────────────────────────────────────────────────

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
            Id           = Guid.NewGuid(),
            Name         = name,
            Description  = description,
            AppliesTo    = appliesTo,
            ConditionJson = JsonSerializer.Serialize(condition),
            Severity     = severity,
            IsActive     = true,
            CreatedAt    = DateTimeOffset.UtcNow
        };
    }

    // ── Domain methods ───────────────────────────────────────────────────────

    public void Update(string name, string description, RuleCondition condition, Severity severity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        if (!condition.IsValidForModule(AppliesTo))
            throw new ArgumentException(
                $"Condición inválida para módulo {AppliesTo}.", nameof(condition));

        Name          = name;
        Description   = description;
        ConditionJson = JsonSerializer.Serialize(condition);
        Severity      = severity;
        UpdatedAt     = DateTimeOffset.UtcNow;
    }

    public void Activate()
    {
        if (IsActive) return;   // idempotente — BR-RULE-06
        IsActive  = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive) return;  // idempotente — BR-RULE-06
        IsActive  = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    public RuleCondition GetCondition()
        => JsonSerializer.Deserialize<RuleCondition>(ConditionJson)!;
}
```

---

## §6 Entity — RuleHistoryEntry

`RuleHistoryEntry` es inmutable desde el momento de su creación. El repositorio **nunca expone** Update ni Delete sobre esta entidad (BR-RULE-02 / SECURITY-13).

```csharp
public sealed class RuleHistoryEntry
{
    public Guid           Id             { get; private set; }
    public Guid           RuleId         { get; private set; }
    public RuleChangeType ChangeType     { get; private set; }
    public string         AuthorUserId   { get; private set; } = string.Empty;
    public string         Reason         { get; private set; } = string.Empty;
    public string?        SnapshotBefore { get; private set; }  // null en Creation
    public string?        SnapshotAfter  { get; private set; }  // null en Deactivation pura si no aplica
    public DateTimeOffset ChangedAt      { get; private set; }

    private RuleHistoryEntry() { }  // EF Core

    // ── Factories ────────────────────────────────────────────────────────────

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
```

---

## §7 Enum — RuleChangeType (definido en U1, referenciado aquí)

```csharp
// Definido en MonitorPedidos.Domain (U1) — no redefinir en U5
public enum RuleChangeType
{
    Creation,
    Edit,
    Activation,
    Deactivation
}
```

---

## §8 Interfaces de repositorio

```csharp
public interface IRuleRepository
{
    Task<IReadOnlyList<Rule>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Rule>> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Rule>> GetActiveByModuleAsync(ModuleId module, CancellationToken ct = default);
    Task<Rule?>               GetByIdAsync(Guid ruleId, CancellationToken ct = default);
    Task                      AddAsync(Rule rule, CancellationToken ct = default);
    Task                      SaveChangesAsync(CancellationToken ct = default);
}

public interface IRuleHistoryRepository
{
    Task                             AppendAsync(RuleHistoryEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<RuleHistoryEntry>> GetByRuleIdAsync(Guid ruleId, CancellationToken ct = default);
    Task<IReadOnlyList<RuleHistoryEntry>> GetAllAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    // Sin Update ni Delete — historial inmutable (SECURITY-13, BR-RULE-02)
}
```

---

## §9 Interfaz del servicio de aplicación

```csharp
public interface IRuleManagementService
{
    Task<IReadOnlyList<Rule>> GetAllRulesAsync(CancellationToken ct = default);
    Task<Rule?>               GetRuleByIdAsync(Guid ruleId, CancellationToken ct = default);
    Task<Rule>                CreateRuleAsync(string name, string description, ModuleId appliesTo, RuleCondition condition, Severity severity, string authorUserId, string reason, CancellationToken ct = default);
    Task<Rule>                UpdateRuleAsync(Guid ruleId, string name, string description, RuleCondition condition, Severity severity, string authorUserId, string reason, CancellationToken ct = default);
    Task                      ActivateRuleAsync(Guid ruleId, string authorUserId, string reason, CancellationToken ct = default);
    Task                      DeactivateRuleAsync(Guid ruleId, string authorUserId, string reason, CancellationToken ct = default);
    Task<IReadOnlyList<RuleHistoryEntry>> GetHistoryAsync(Guid ruleId, CancellationToken ct = default);
}
```

---

## §10 Seed data — migración EF Core (U5)

Dos reglas iniciales que replican los valores hardcodeados usados por U3 en Sprint 2:

```csharp
// En la migración AddRulesTables de U5 — método Up()
migrationBuilder.InsertData(
    table: "rules",
    columns: ["id", "name", "description", "applies_to", "condition_json",
              "severity", "is_active", "created_at", "updated_at"],
    values: new object[]
    {
        // Regla 1: DbOrders — ventana de 2 horas
        Guid.Parse("00000000-0000-0000-0000-000000000001"),
        "Ventana de pedidos — Salesforce/Multivende",
        "Se esperan al menos 1 pedido en las últimas 2 horas desde DbOrders.",
        "DbOrders",
        """{"WindowHours":2,"MinOrders":1,"LatencyWarnMs":null,"LatencyCriticalMs":null}""",
        "Critical",
        true,
        new DateTimeOffset(2026, 5, 23, 0, 0, 0, TimeSpan.Zero),
        (DateTimeOffset?)null
    });

migrationBuilder.InsertData(
    table: "rules",
    columns: ["id", "name", "description", "applies_to", "condition_json",
              "severity", "is_active", "created_at", "updated_at"],
    values: new object[]
    {
        // Regla 2: DbHealth — latencia
        Guid.Parse("00000000-0000-0000-0000-000000000002"),
        "Latencia de base de datos",
        "WARN si latencia > 500ms. CRITICAL si latencia > 2000ms o timeout.",
        "DbHealth",
        """{"WindowHours":null,"MinOrders":null,"LatencyWarnMs":500,"LatencyCriticalMs":2000}""",
        "Warn",
        true,
        new DateTimeOffset(2026, 5, 23, 0, 0, 0, TimeSpan.Zero),
        (DateTimeOffset?)null
    });
```

---

## §11 Trazabilidad

| Componente | Story | RF | Seguridad |
|-----------|-------|----|-----------| 
| `Rule` aggregate + `Rule.Create` / `Update` / `Activate` / `Deactivate` | US-19, US-20, US-21 | RF-14, RF-15, RF-16, RF-17 | SECURITY-13 |
| `RuleCondition` value object (JSON en `ConditionJson`) | US-19, US-20 | RF-14, RF-15 | — |
| `RuleSnapshot` value object (diff antes/después) | US-20, US-22 | RF-15 | SECURITY-13 |
| `RuleHistoryEntry` (inmutable, sin Update/Delete) | US-22 | RF-16 | SECURITY-13 |
| `IRuleRepository.GetActiveByModuleAsync` | US-07 | RF-02, RF-14 | — |
| Seed data (2 reglas iniciales en migración) | US-19 | RF-14 | — |
| Validación `reason` obligatoria | US-19, US-20, US-21 | RF-17 | — |
