# Component Methods — MonitorPedidos AI

**Fecha:** 2026-05-21
**Versión:** 1.0
**Stage:** Inception → Application Design (Part 2 — Generation)
**Alcance:** Firmas (signatures) de métodos públicos de cada componente. **Sin lógica de negocio profunda** (eso queda para Functional Design en Construction).
**Lenguaje:** C# 12 / .NET 8 (notación). Async/Await como default — todos los métodos I/O son `Task<T>`.

---

## Convenciones

- **Visibilidad:** todas las firmas listadas son `public`. Métodos privados/internos no se documentan aquí.
- **Cancellation:** todos los métodos asíncronos aceptan `CancellationToken cancellationToken = default` salvo que se indique. Por concisión, no se repite en cada firma — se asume al final de la lista de parámetros.
- **Errores:** las firmas no listan excepciones explícitas; se manejan vía global exception handler (US-27). Métodos que pueden retornar "no encontrado" usan `T?` o tipos `Result<T>` (decisión final en Construction).
- **Identificadores:** `Guid` para IDs de incidentes y reglas. `string` para IDs de usuarios de Identity.

---

# 1. Componentes funcionales (M1–M11)

## 1.1 M1 Scheduler — `MonitoringSchedulerService`

```csharp
// Derived from BackgroundService — only ExecuteAsync is the public lifecycle method.
public sealed class MonitoringSchedulerService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken);
}
```

Internamente registra `PeriodicTimer` por checker:
- M2 `DbOrderChecker` → cada 5 min
- M3 `ApiChecker` → cada 10 min
- M4 `DbHealthChecker` → cada 5 min
- M11 `JobsChecker` → cada 5 min

Cada tick invoca `MonitoringService.RunCheckAsync(moduleId, ...)`.

---

## 1.2 Interface compartida — `ICheckExecutor`

```csharp
public interface ICheckExecutor
{
    ModuleId Module { get; }
    Task<CheckResult> CheckAsync(CancellationToken cancellationToken = default);
}
```

`ModuleId` es enum: `DbOrders` (M2), `ApiSalesforce` / `ApiMultivende` (M3), `DbHealth` (M4), `Jobs` (M11).

`CheckResult` es un record:

```csharp
public sealed record CheckResult(
    ModuleId Module,
    CheckStatus Status,          // Ok | Warn | Critical
    string? CauseHint,           // Hint para M7 — opcional
    IReadOnlyDictionary<string, string> Metadata,  // Latencias, códigos HTTP, etc.
    DateTime Timestamp
);
```

---

## 1.3 M2 — `DbOrderChecker : ICheckExecutor`

Sin métodos públicos adicionales. Toda la lógica vive detrás de `CheckAsync`.

## 1.4 M3 — `ApiChecker : ICheckExecutor`

```csharp
public sealed class ApiChecker : ICheckExecutor
{
    public ModuleId Module { get; }   // ApiSalesforce o ApiMultivende
    public Task<CheckResult> CheckAsync(CancellationToken cancellationToken = default);
}

// Clientes inyectados (no son "componentes" formales del PRD pero soportan M3):
public interface ISalesforceClient
{
    Task<HttpStatusCode> PingOrdersAsync(CancellationToken cancellationToken = default);
}

public interface IMultivendeClient
{
    Task<HttpStatusCode> PingOrdersAsync(CancellationToken cancellationToken = default);
}
```

**Política de reintentos (RF-06):** implementada como wrapper alrededor de `*Client.PingOrdersAsync` con `Polly` o equivalente. Máximo 2 reintentos, solo ante `5xx` o timeout. Ante `401`, no reintenta. Cada intento se loggea como evento `auto_reintento`.

## 1.5 M4 — `DbHealthChecker : ICheckExecutor`

Sin métodos públicos adicionales. `CheckAsync` ejecuta `SELECT 1` y mide latencia.

## 1.6 M11 — `JobsChecker : ICheckExecutor`

Sin métodos públicos adicionales. **No** reintenta jobs (Decisión #2).

---

## 1.7 M6 — Motor de reglas (`Features/Rules/`)

### 1.7.1 Entidad `Rule`

```csharp
public sealed class Rule
{
    public Guid Id { get; }
    public string Name { get; }
    public string Description { get; }
    public ModuleId AppliesTo { get; }
    public string ConditionExpression { get; }   // Forma textual o JSON — definición final en Functional Design
    public Severity Severity { get; }
    public bool IsActive { get; }
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; }
}
```

### 1.7.2 `IRuleRepository`

```csharp
public interface IRuleRepository
{
    Task<IReadOnlyList<Rule>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Rule>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Rule?> GetByIdAsync(Guid ruleId, CancellationToken cancellationToken = default);
    Task<Rule> CreateAsync(Rule rule, CancellationToken cancellationToken = default);
    Task<Rule> UpdateAsync(Rule rule, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid ruleId, bool isActive, CancellationToken cancellationToken = default);
}
```

### 1.7.3 Entidad `RuleHistoryEntry`

```csharp
public sealed class RuleHistoryEntry
{
    public Guid Id { get; }
    public Guid RuleId { get; }
    public RuleChangeType ChangeType { get; }   // Creation | Edit | Activation | Deactivation
    public string AuthorUserId { get; }         // Foreign key a AspNetUsers
    public string Reason { get; }               // Obligatorio (P5)
    public string? DiffBefore { get; }          // null en Creation
    public string? DiffAfter { get; }           // null en Deactivation pura
    public DateTime Timestamp { get; }
}
```

### 1.7.4 `IRuleHistoryRepository`

```csharp
public interface IRuleHistoryRepository
{
    Task AppendAsync(RuleHistoryEntry entry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RuleHistoryEntry>> GetByRuleIdAsync(Guid ruleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RuleHistoryEntry>> GetAllAsync(DateRange range, CancellationToken cancellationToken = default);
}
```

> Nota: el repositorio de historial **no expone Update ni Delete** — el historial es inmutable (SECURITY-13, US-22 criterio 3).

---

## 1.8 M7 — `CauseClassifier : ICauseClassifier`

```csharp
public interface ICauseClassifier
{
    CauseCategory Classify(IReadOnlyList<CheckResult> recentResults);
}
```

`CauseCategory`: enum `Bd | Job | Api | Token | DataQuality | NoDeterminada`.

`Classify` aplica la cascada `bd → job → api → token → data_quality → no_determinada` (RF-09).

---

## 1.9 M10 — `AlertTemplateRenderer : IAlertExplainer`

```csharp
public interface IAlertExplainer
{
    AlertMessage Render(IncidentContext context);
}

public sealed record IncidentContext(
    ModuleId Module,
    CauseCategory Cause,
    Severity Severity,
    DateTime OccurredAt,
    IReadOnlyDictionary<string, string> Metadata   // datos útiles para rellenar plantillas
);

public sealed record AlertMessage(
    string QuePaso,           // qué_pasó
    DateTime Cuando,          // cuándo
    string Donde,             // dónde (módulo/integración)
    Severity Severidad,
    string CausaProbable,
    string AccionSugerida
);
```

> Las plantillas concretas se definirán en Functional Design (Construction). Aquí solo se garantiza el shape de 6 campos en español.

---

## 1.10 M9 — Incidentes (`Features/Incidents/`)

### 1.10.1 Entidad `Incident`

```csharp
public sealed class Incident
{
    public Guid Id { get; }
    public ModuleId Module { get; }
    public CauseCategory Cause { get; }
    public Severity Severity { get; }
    public AlertMessage Alert { get; }
    public DateTime OpenedAt { get; }
    public DateTime? ClosedAt { get; }
    public IncidentCloseType? CloseType { get; }      // null si activo; Automatic | Manual
    public string? ClosedByUserId { get; }            // null si Automatic
    public string? ComentarioResolucion { get; }      // obligatorio si CloseType == Manual
    public bool IsCandidatoReglaNueva { get; }        // true si Cause == NoDeterminada
}
```

### 1.10.2 `IIncidentRepository`

```csharp
public interface IIncidentRepository
{
    Task<Incident> CreateAsync(Incident incident, CancellationToken cancellationToken = default);
    Task<Incident?> GetByIdAsync(Guid incidentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Incident>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Incident>> QueryAsync(IncidentQuery query, CancellationToken cancellationToken = default);
    Task<WeeklySummary> GetWeeklySummaryAsync(DateOnly weekStart, CancellationToken cancellationToken = default);
    Task CloseAsync(Guid incidentId, IncidentCloseType closeType, string? userId, string? comment, CancellationToken cancellationToken = default);
    Task PurgeOlderThanAsync(DateTime cutoff, CancellationToken cancellationToken = default);
}

public sealed record IncidentQuery(
    DateTime? From,
    DateTime? To,
    Severity? Severity,
    ModuleId? Module,
    int Skip,
    int Take
);

public sealed record WeeklySummary(
    DateOnly WeekStart,
    IReadOnlyDictionary<CauseCategory, int> CountByCause,
    IReadOnlyDictionary<Severity, int> CountBySeverity,
    int CandidatosReglaNueva
);
```

---

## 1.11 M8 — Dashboard (Blazor + SignalR Hub)

### 1.11.1 `AlertsHub : Hub` (SignalR)

```csharp
public sealed class AlertsHub : Hub
{
    // Métodos servidor → cliente (no se definen aquí — son los Send/Invoke desde IHubContext).
    // Métodos cliente → servidor (opcional, para acks):
    public Task Acknowledge(Guid incidentId);
}
```

Eventos enviados a clientes (vía `IHubContext<AlertsHub>` en `NotificationService`):

```text
AlertReceived(AlertMessage alert, Guid incidentId)
IncidentClosed(Guid incidentId, IncidentCloseType closeType)
SystemStatusUpdated(IReadOnlyDictionary<ModuleId, CheckStatus> snapshot)
```

### 1.11.2 Páginas Blazor (componentes Razor)

Las páginas son archivos `.razor` server-rendered. No exponen métodos públicos "típicos" sino `OnInitializedAsync`, `OnAfterRenderAsync`, etc. Cada página declara su ruta y los servicios que inyecta:

| Página | Ruta | Autorización | Inyecta |
|--------|------|--------------|---------|
| `RealtimePage.razor` | `/` | `[Authorize]` | `IIncidentRepository`, `IMonitoringSnapshotProvider`, `AlertsHubClient` |
| `HistoricPage.razor` | `/historico` | `[Authorize]` | `IIncidentRepository` |
| `WeeklySummaryPage.razor` | `/semanal` | `[Authorize]` | `IIncidentRepository` |
| `DiscrepanciesPage.razor` | `/discrepancias` | `[Authorize]` | `IDiscrepancyRepository` (parte de M2) |
| `IncidentDetailPage.razor` | `/incidentes/{id:guid}` | `[Authorize]` | `IIncidentRepository`, `IIncidentService` |
| `RulesPage.razor` | `/reglas` | `[Authorize(Roles="Técnico")]` | `IRuleManagementService` |
| `RuleEditPage.razor` | `/reglas/{id:guid}` | `[Authorize(Roles="Técnico")]` | `IRuleManagementService` |
| `RuleHistoryPage.razor` | `/reglas/{id:guid}/historial` | `[Authorize(Roles="Técnico")]` | `IRuleHistoryRepository` |
| `LogsPage.razor` | `/logs` | `[Authorize(Roles="Técnico")]` | `ITechnicalLogReader` |
| `NocLayout.razor` | (layout) | `[Authorize]` | — |
| `IdentitySelectionPage.cshtml` | `/Identity/Select` | público | Pantalla con 2 botones — emite cookie de sesión con claims de rol |

---

# 2. Application Services (`MonitorPedidos.Web/Services/`)

Ver detalle de orquestación en [`services.md`](./services.md). Aquí solo las firmas públicas.

## 2.1 `IMonitoringService`

```csharp
public interface IMonitoringService
{
    Task RunCheckAsync(ModuleId module, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<ModuleId, CheckStatus>> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
```

## 2.2 `IRuleManagementService`

```csharp
public interface IRuleManagementService
{
    Task<Rule> CreateRuleAsync(RuleDraft draft, string authorUserId, string reason, CancellationToken cancellationToken = default);
    Task<Rule> UpdateRuleAsync(Guid ruleId, RuleDraft draft, string authorUserId, string reason, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid ruleId, bool isActive, string authorUserId, string? reason, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Rule>> ListAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RuleHistoryEntry>> GetHistoryAsync(Guid ruleId, CancellationToken cancellationToken = default);
}

public sealed record RuleDraft(
    string Name,
    string Description,
    ModuleId AppliesTo,
    string ConditionExpression,
    Severity Severity
);
```

## 2.3 `IIncidentService`

```csharp
public interface IIncidentService
{
    Task<Incident> OpenIncidentAsync(IncidentContext context, AlertMessage alert, CancellationToken cancellationToken = default);
    Task CloseAutomaticallyAsync(Guid incidentId, CancellationToken cancellationToken = default);
    Task CloseManuallyAsync(Guid incidentId, string userId, string comment, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Incident>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<Incident?> GetByIdAsync(Guid incidentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Incident>> SearchHistoryAsync(IncidentQuery query, CancellationToken cancellationToken = default);
    Task<WeeklySummary> GetWeeklySummaryAsync(DateOnly weekStart, CancellationToken cancellationToken = default);
}
```

## 2.4 `IIdentityService`

```csharp
public interface IIdentityService
{
    Task SelectIdentityAsync(string identityKey, HttpContext httpContext, CancellationToken cancellationToken = default);
    Task SignOutAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
    SelectedIdentity? GetCurrentIdentity(ClaimsPrincipal principal);
}

public sealed record SelectedIdentity(string DisplayName, string RoleName);
```

> Implementación usa `HttpContext.SignInAsync(CookieScheme, principal)` directamente. Sin ASP.NET Core Identity, sin password, sin lockout.

## 2.5 `INotificationService`

```csharp
public interface INotificationService
{
    Task BroadcastAlertAsync(Guid incidentId, AlertMessage alert, CancellationToken cancellationToken = default);
    Task BroadcastIncidentClosedAsync(Guid incidentId, IncidentCloseType closeType, CancellationToken cancellationToken = default);
    Task BroadcastSystemStatusAsync(IReadOnlyDictionary<ModuleId, CheckStatus> snapshot, CancellationToken cancellationToken = default);
}
```

> Implementación inyecta `IHubContext<AlertsHub>` y llama `Clients.All.SendAsync(...)`.

---

# 3. Componentes cross-cutting (firmas mínimas)

## 3.1 Simulation — `OrdersSimulatorService`

```csharp
public sealed class OrdersSimulatorService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken);
}
```

Configuración via `SimulationOptions`:

```csharp
public sealed class SimulationOptions
{
    public TimeSpan InsertInterval { get; init; } = TimeSpan.FromMinutes(5);
    public double FailureProbability { get; init; } = 0.0;   // 0..1, configurable para red-teaming
}
```

## 3.2 Security Headers Middleware

```csharp
public sealed class SecurityHeadersMiddleware
{
    public Task InvokeAsync(HttpContext context, RequestDelegate next);
}
```

Se registra vía `app.UseMiddleware<SecurityHeadersMiddleware>()`. Agrega los 5 headers descritos en `components.md` §2.6.

## 3.3 Global Exception Handler

```csharp
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken);
}
```

Se registra vía `app.UseExceptionHandler(...)`. Loggea con request_id, redacta detalles sensibles, responde JSON/HTML genérico según content negotiation.

---

# 4. Trazabilidad de métodos públicos → stories

| Método público (representativo) | Stories que satisface |
|--------------------------------|------------------------|
| `MonitoringSchedulerService.ExecuteAsync` | US-07, US-08, US-09, US-10 |
| `ICheckExecutor.CheckAsync` (M2/M3/M4/M11) | US-07, US-08, US-09, US-10, US-16 |
| `ICauseClassifier.Classify` | US-07, US-10, US-18 |
| `IAlertExplainer.Render` | US-06 |
| `IRuleManagementService.CreateRuleAsync` / `UpdateRuleAsync` / `SetActiveAsync` / `GetHistoryAsync` | US-19, US-20, US-21, US-22 |
| `IIncidentService.OpenIncidentAsync` / `CloseAutomaticallyAsync` / `CloseManuallyAsync` | US-13, US-14 (cierre); US-07..US-10 (apertura) |
| `IIncidentService.SearchHistoryAsync` / `GetWeeklySummaryAsync` | US-11, US-12 |
| `INotificationService.BroadcastAlertAsync` / `BroadcastIncidentClosedAsync` | US-03, US-06, US-14 |
| `IAuthService.SignInAsync` / `IsInRoleAsync` | US-15, US-23, US-24 |
| Páginas Blazor con `[Authorize]` / `[Authorize(Roles="Técnico")]` | US-01..US-05, US-11, US-12, US-15, US-17, US-19..US-22 |
| `SecurityHeadersMiddleware.InvokeAsync` | US-28 |
| `GlobalExceptionHandler.TryHandleAsync` | US-27 |
| `OrdersSimulatorService.ExecuteAsync` | RT1, RT2, RT3, RT5, RT7 (datos para escenarios red-teaming) |

✅ Todas las stories tienen al menos un método público que las soporta.

---

# 5. Notas para Construction (cuando se active)

- **Validación de inputs (RNF-10):** se aplicará a `RuleDraft`, `IncidentQuery`, etc., mediante DataAnnotations + FluentValidation (decisión final en NFR Design).
- **Repositorios:** las firmas listadas son agnósticas a la implementación. Functional Design definirá si usa `DbContext` (EF Core) o un mapeo con Dapper.
- **Plantillas de alertas:** la sustitución concreta de placeholders en `IAlertExplainer.Render` es lógica de negocio profunda — pertenece a Functional Design.
- **Política de reintentos (Polly):** la configuración exacta (delays, jitter) se afina en NFR Design.
