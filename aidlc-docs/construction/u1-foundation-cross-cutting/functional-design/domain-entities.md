# Domain Entities — U1 Foundation & Cross-Cutting

**Unidad:** U1 — Foundation & Cross-Cutting
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-23
**Versión:** 1.1 (reescritura — modelo de selección simple de identidad sin ASP.NET Core Identity)

---

## §1 Bounded Context: Identity & Cross-Cutting Foundation

U1 define las piezas que **todas las demás unidades comparten**:

- **Modelo de identidad** — selección simple de rol sin contraseñas ni cuentas en BD
- **Enums de dominio compartidos** — usados por U2..U7
- **Value objects cross-cutting** — `AlertMessage`, `CheckResult`
- **Interfaces de contrato** — `ICheckExecutor`, `INotificationService`

No existe base de datos de usuarios. La identidad se emite como cookie de sesión ASP.NET Core al momento de la selección.

---

## §2 Modelo de Identidad (sin entidades en BD)

### 2.1 Identidades pre-definidas (constantes, no entidades)

El sistema tiene exactamente **2 identidades fijas** definidas como constantes de código — no como filas en base de datos:

```csharp
// Domain/Identity/PredefinedIdentities.cs
public static class PredefinedIdentities
{
    public static readonly UserIdentity AnalistaOperativo = new(
        DisplayName: "Analista Operativo",
        RoleName: ApplicationRole.Names.Operador
    );

    public static readonly UserIdentity ResponsableTecnico = new(
        DisplayName: "Responsable Técnico",
        RoleName: ApplicationRole.Names.Tecnico
    );
}
```

### 2.2 UserIdentity (Value Object de tránsito)

```csharp
// Domain/Identity/UserIdentity.cs
public sealed record UserIdentity(string DisplayName, string RoleName);
```

Propósito: agrupa el nombre visible y el nombre de rol. Se usa solo en el flujo de selección para emitir los claims de la cookie. No se persiste.

### 2.3 ApplicationRole (constantes de rol)

```csharp
// Domain/Identity/ApplicationRole.cs
public static class ApplicationRole
{
    public static class Names
    {
        public const string Operador = "Operador";
        public const string Tecnico  = "Técnico";
    }
}
```

No hereda de ninguna clase de Identity. Solo define los nombres de rol usados en `[Authorize(Roles="...")]` y en `ClaimTypes.Role`.

---

## §3 Value Objects Cross-Cutting

### 3.1 AlertMessage

Transporta los 6 campos de alerta explicable requeridos por RF-11. Inmutable por diseño. Usado en U2 (persistido via EF Core OwnsOne en tabla `incidents`) y generado en U3 (M10).

```csharp
// Domain/Alerts/AlertMessage.cs
public sealed record AlertMessage(
    string WhatHappened,
    DateTimeOffset WhenOccurred,
    string WhereOccurred,
    Severity Severity,
    string ProbableCause,
    string SuggestedAction
);
```

**Persistencia (U2):** EF Core `OwnsOne` — cada campo se mapea a columna individual en `incidents` (`what_happened`, `when_occurred`, `where_occurred`, `severity`, `probable_cause`, `suggested_action`). Permite filtros SQL directos por campo.

### 3.2 CheckResult

DTO de tránsito que retorna cada `ICheckExecutor.CheckAsync()`. No se persiste directamente — el incidente y el log consumen sus datos. Vive solo en memoria entre Checker y MonitoringService.

```csharp
// Domain/Monitoring/CheckResult.cs
public sealed record CheckResult(
    ModuleId Module,
    CheckStatus Status,
    string? Detail,
    DateTimeOffset CheckedAt
);
```

---

## §4 Enums de Dominio (compartidos por U1..U7)

### 4.1 Severity

```csharp
// Domain/Shared/Severity.cs
public enum Severity
{
    Info     = 1,
    Warn     = 2,
    Critical = 3
}
```

### 4.2 CauseCategory

```csharp
// Domain/Shared/CauseCategory.cs
public enum CauseCategory
{
    Bd            = 1,
    Job           = 2,
    Api           = 3,
    Token         = 4,
    DataQuality   = 5,
    NoDeterminada = 6
}
```

### 4.3 ModuleId

```csharp
// Domain/Shared/ModuleId.cs
public enum ModuleId
{
    Scheduler        = 1,   // M1
    DbOrderChecker   = 2,   // M2
    ApiChecker       = 3,   // M3
    DbHealthChecker  = 4,   // M4
    RulesManagement  = 6,   // M6 (M5 integrado)
    CauseClassifier  = 7,   // M7
    Dashboard        = 8,   // M8
    IncidentManager  = 9,   // M9
    AlertRenderer    = 10,  // M10
    JobsMonitor      = 11   // M11
}
```

### 4.4 IncidentCloseType

```csharp
// Domain/Incidents/IncidentCloseType.cs
public enum IncidentCloseType
{
    Automatic = 1,
    Manual    = 2
}
```

### 4.5 RuleChangeType

```csharp
// Domain/Rules/RuleChangeType.cs
public enum RuleChangeType
{
    Creation     = 1,
    Edit         = 2,
    Activation   = 3,
    Deactivation = 4
}
```

### 4.6 CheckStatus

```csharp
// Domain/Monitoring/CheckStatus.cs
public enum CheckStatus
{
    Ok       = 1,
    Warn     = 2,
    Critical = 3
}
```

---

## §5 Interfaces de Contrato

### 5.1 ICheckExecutor

```csharp
// Domain/Monitoring/ICheckExecutor.cs
public interface ICheckExecutor
{
    ModuleId Module { get; }
    Task<CheckResult> CheckAsync(CancellationToken ct = default);
}
```

### 5.2 INotificationService

```csharp
// Domain/Notifications/INotificationService.cs
public interface INotificationService
{
    Task NotifyIncidentAsync(Guid incidentId, CancellationToken ct = default);
    Task NotifyStatusChangeAsync(ModuleId module, CheckStatus status, CancellationToken ct = default);
}
```

---

## §6 Estructura de carpetas de U1

```
Domain/
+-- Identity/
|   +-- PredefinedIdentities.cs   (2 identidades fijas — no filas en BD)
|   +-- UserIdentity.cs           (record de transito para cookie)
|   +-- ApplicationRole.cs        (constantes de nombre de rol)
+-- Alerts/
|   +-- AlertMessage.cs           (value object — 6 campos, OwnsOne en U2)
+-- Monitoring/
|   +-- CheckResult.cs            (value object de transito)
|   +-- CheckStatus.cs            (enum)
|   +-- ICheckExecutor.cs         (contrato checkers)
+-- Notifications/
|   +-- INotificationService.cs   (contrato SignalR — implementado en U6)
+-- Shared/
    +-- Severity.cs
    +-- CauseCategory.cs
    +-- ModuleId.cs
    +-- IncidentCloseType.cs
    +-- RuleChangeType.cs
```

---

## §7 Trazabilidad

| Artefacto | RF / RNF | Decision |
|-----------|----------|---------|
| PredefinedIdentities | RF-28 | 2 identidades fijas sin BD |
| UserIdentity | RF-28, RF-29 | Cookie claims carrier |
| ApplicationRole | RF-27 | 2 roles exactamente |
| AlertMessage | RF-11 | 6 campos + OwnsOne (Q3=A) |
| CheckResult | — | DTO de transito (Q4=A) |
| ModuleId | — | Nombres descriptivos (Q5=A) |
| Severity, CauseCategory | RF-07, RF-08 | Compartidos U2..U7 |
| ICheckExecutor | M2, M3, M4, M11 | Contrato checkers |
| INotificationService | RF-13 | Abstraccion SignalR (U6) |
