# Services — MonitorPedidos AI

**Fecha:** 2026-05-21
**Versión:** 1.0
**Stage:** Inception → Application Design (Part 2 — Generation)
**Decisión Q6 del plan:** un application service por capability funcional (no por módulo).
**Alcance:** definir los **5 application services**, sus responsabilidades, su orquestación interna y su mapeo a stories.

---

## Mapa de servicios

```text
┌──────────────────────────────────────────────────────────────────────────┐
│                        APPLICATION SERVICES LAYER                         │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│  MonitoringService   ──orquesta──>  Checkers (M2/M3/M4/M11)               │
│       │                              │                                    │
│       ▼                              ▼                                    │
│  CauseClassifier (M7) ──>  AlertTemplateRenderer (M10)  ──>  IncidentSvc  │
│                                                                  │        │
│                                                                  ▼        │
│                                                            NotificationSvc│
│                                                                  │        │
│                                                                  ▼        │
│                                                            AlertsHub      │
│                                                                            │
│  RuleManagementService  ──CRUD─>  IRuleRepository + IRuleHistoryRepo       │
│  IncidentService        ──CRUD─>  IIncidentRepository                      │
│  IdentityService        ──cookie─>  HttpContext (SignInAsync / SignOutAsync) │
│  NotificationService    ──push──>  IHubContext<AlertsHub>                  │
└──────────────────────────────────────────────────────────────────────────┘
```

---

# 1. `MonitoringService` — orquestador del flujo de chequeo

## 1.1 Responsabilidad

Coordinar el flujo completo de detección desde que el Scheduler (M1) decide ejecutar un chequeo hasta que el incidente queda registrado y notificado:

1. Recibir el `ModuleId` a chequear desde `MonitoringSchedulerService`.
2. Resolver el `ICheckExecutor` correspondiente (M2 / M3 / M4 / M11).
3. Ejecutar `CheckAsync` y obtener `CheckResult`.
4. Actualizar el snapshot interno del sistema (para vista real-time del dashboard).
5. Si el resultado es `Warn` o `Critical`:
   - Recopilar los últimos `CheckResult` de los demás módulos (para que M7 tenga contexto).
   - Invocar `CauseClassifier.Classify(...)` → `CauseCategory`.
   - Marcar candidato a regla nueva si la causa es `NoDeterminada`.
   - Invocar `AlertTemplateRenderer.Render(context)` → `AlertMessage` con los 6 campos.
   - Invocar `IncidentService.OpenIncidentAsync(context, alert)` → persistencia + retorno de `Incident`.
   - Invocar `NotificationService.BroadcastAlertAsync(incident.Id, alert)` → push al dashboard.
6. Si el resultado es `Ok` y había incidente activo del módulo:
   - Evaluar regla de cierre automático: si el chequeo previo del mismo módulo también fue `Ok`, invocar `IncidentService.CloseAutomaticallyAsync(...)` y `NotificationService.BroadcastIncidentClosedAsync(...)`.
7. Invocar `NotificationService.BroadcastSystemStatusAsync(snapshot)` para refrescar el estado global en el dashboard.

## 1.2 Dependencias inyectadas

| Dependencia | Razón |
|-------------|-------|
| `IEnumerable<ICheckExecutor>` | Lista de checkers; se resuelve por `ModuleId` |
| `ICauseClassifier` | M7 |
| `IAlertExplainer` | M10 |
| `IIncidentService` | M9 (vía service layer) |
| `INotificationService` | Push al dashboard |
| `ILogger<MonitoringService>` | Logging (RNF-08) |

## 1.3 Stories cubiertas

US-06, US-07, US-08, US-09, US-10, US-14, US-16, US-18.

## 1.4 No-responsabilidades (qué NO hace)

- ❌ **NO** renueva tokens (Decisión #2 / RF-07).
- ❌ **NO** reintenta jobs (Decisión #2 / RF-05).
- ❌ **NO** decide los detalles de la lógica de clasificación — eso es responsabilidad de `CauseClassifier`.
- ❌ **NO** genera el texto de la alerta — eso es responsabilidad de `AlertTemplateRenderer`.

Esto mantiene `MonitoringService` como **orquestador puro**: lee inputs, llama colaboradores, persiste, notifica.

---

# 2. `RuleManagementService` — gestión de reglas estáticas (M6)

## 2.1 Responsabilidad

Encapsular el CRUD de reglas + historial garantizando que **toda mutación** pase por la validación de autor y razón obligatorios (P5).

## 2.2 Operaciones orquestadas

| Operación | Flujo interno |
|-----------|----------------|
| `CreateRuleAsync(draft, authorUserId, reason)` | 1. Validar `draft` (campos requeridos, longitudes). 2. Validar `reason` no vacío. 3. Insertar `Rule` vía `IRuleRepository.CreateAsync`. 4. Crear `RuleHistoryEntry` con `ChangeType=Creation`, `Reason`, autor, sin `DiffBefore`, `DiffAfter = JSON serializado del estado nuevo`. 5. Persistir vía `IRuleHistoryRepository.AppendAsync`. |
| `UpdateRuleAsync(ruleId, draft, authorUserId, reason)` | 1. Cargar la regla actual. 2. Validar `draft` + `reason`. 3. Calcular diff antes/después. 4. Actualizar vía `IRuleRepository.UpdateAsync`. 5. Crear `RuleHistoryEntry` con `ChangeType=Edit`, diff antes/después, autor, razón. |
| `SetActiveAsync(ruleId, isActive, authorUserId, reason)` | 1. Cargar regla. 2. Actualizar `IsActive` vía repo. 3. Crear `RuleHistoryEntry` con `ChangeType=Activation`/`Deactivation`, autor, razón opcional. |
| `ListAsync()` | Pasthrough a `IRuleRepository.GetAllAsync`. |
| `GetHistoryAsync(ruleId)` | Pasthrough a `IRuleHistoryRepository.GetByRuleIdAsync`. |

## 2.3 Dependencias inyectadas

`IRuleRepository`, `IRuleHistoryRepository`, `ILogger<RuleManagementService>`.

## 2.4 Reglas de negocio aplicadas (alto nivel)

- **Razón obligatoria** en `CreateRuleAsync` y `UpdateRuleAsync` (US-19, US-20). Si está vacía, lanza `ValidationException` antes de tocar el repositorio.
- **Historial inmutable** — el servicio nunca expone Update/Delete sobre `RuleHistoryEntry`.
- **Auditoría** — cada operación de mutación se loggea con request_id y autor.

## 2.5 Autorización (impuesta arriba)

Los endpoints/páginas Blazor que llaman a este servicio están protegidos con `[Authorize(Roles="Técnico")]` (RF-17). El servicio **asume** que la llamada ya pasó por el authorization middleware; no re-valida roles.

## 2.6 Stories cubiertas

US-19, US-20, US-21, US-22, US-18 (al permitir crear regla desde un candidato).

---

# 3. `IncidentService` — ciclo de vida de incidentes (M9)

## 3.1 Responsabilidad

Encapsular la apertura, cierre (auto/manual), búsqueda y archivado de incidentes. Es el único punto que muta incidentes.

## 3.2 Operaciones orquestadas

| Operación | Flujo interno |
|-----------|----------------|
| `OpenIncidentAsync(context, alert)` | 1. Construir `Incident` con `OpenedAt=now`, `Cause`, `Severity`, `Alert`, `IsCandidatoReglaNueva` = (Cause == NoDeterminada). 2. Persistir vía `IIncidentRepository.CreateAsync`. |
| `CloseAutomaticallyAsync(incidentId)` | 1. Validar que el incidente existe y está activo. 2. Llamar `IIncidentRepository.CloseAsync(incidentId, Automatic, null, null)`. |
| `CloseManuallyAsync(incidentId, userId, comment)` | 1. Validar que `comment` no está vacío (RF-20). 2. Validar que el incidente existe y está activo. 3. Llamar `IIncidentRepository.CloseAsync(incidentId, Manual, userId, comment)`. |
| `GetActiveAsync()` | Pasthrough a repo. |
| `GetByIdAsync(id)` | Pasthrough a repo. |
| `SearchHistoryAsync(query)` | Pasthrough a `IIncidentRepository.QueryAsync` con paginación. |
| `GetWeeklySummaryAsync(weekStart)` | Pasthrough a `IIncidentRepository.GetWeeklySummaryAsync`. |

## 3.3 Política de retención (RF-21)

Un job interno (orquestado por `MonitoringSchedulerService` a una cadencia diaria, p. ej. 03:00 hora local) invoca `IIncidentRepository.PurgeOlderThanAsync(DateTime.UtcNow.AddDays(-90))`. La cadencia y la decisión de archivar vs purgar se afina en NFR Design.

## 3.4 Dependencias inyectadas

`IIncidentRepository`, `ILogger<IncidentService>`.

## 3.5 Stories cubiertas

US-11, US-12, US-13, US-14, US-18 (apertura con candidato).

---

# 4. `IdentityService` — selección de identidad y sesión (cross-cutting)

## 4.1 Responsabilidad

Gestiona la selección de identidad sin contraseñas. Emite y cierra cookies de sesión ASP.NET Core con claims de rol. No usa `SignInManager` ni `UserManager` de Identity — solo `HttpContext.SignInAsync`/`SignOutAsync`.

## 4.2 Operaciones

| Operación | Flujo interno |
|-----------|----------------|
| `SelectIdentityAsync(identity, httpContext)` | 1. Resolver `UserIdentity` desde `PredefinedIdentities` (Operador o Tecnico). 2. Construir `ClaimsPrincipal` con claims `Name` y `Role`. 3. Llamar `HttpContext.SignInAsync(CookieScheme, principal, {IsPersistent:false})`. 4. Loggear `identidad_seleccionada | rol=X`. |
| `SignOutAsync(httpContext)` | `HttpContext.SignOutAsync(CookieScheme)`. Loggear `sesion_cerrada | rol=X`. |
| `GetCurrentIdentity(principal)` | Leer `ClaimTypes.Name` y `ClaimTypes.Role` del `ClaimsPrincipal`. |

## 4.3 Configuración de cookie

- Scheme: `CookieAuthenticationDefaults.AuthenticationScheme` (no Identity completo).
- Cookies: `HttpOnly=true`, `SameSite=Strict` siempre; `Secure=true` cuando HTTPS (RNF-14).
- `IsPersistent = false` — sin persistencia entre reinicios del navegador.
- Identidades pre-definidas: constantes en `PredefinedIdentities` — sin cuentas en BD.
- Sin lockout, sin password hashing, sin seeder de usuarios.

## 4.4 Dependencias inyectadas

`IHttpContextAccessor`, `ILogger<IdentityService>`.

## 4.5 Stories cubiertas

US-15, US-23, US-24.

---

# 5. `NotificationService` — push en tiempo real al dashboard

## 5.1 Responsabilidad

Único punto que escribe al hub SignalR `AlertsHub`. Encapsula los métodos de broadcast para que los demás services nunca toquen `IHubContext` directamente — facilita testing y mantiene el acoplamiento bajo.

## 5.2 Operaciones

| Operación | Mensaje SignalR emitido |
|-----------|--------------------------|
| `BroadcastAlertAsync(incidentId, alert)` | `Clients.All.SendAsync("AlertReceived", incidentId, alert)` |
| `BroadcastIncidentClosedAsync(incidentId, closeType)` | `Clients.All.SendAsync("IncidentClosed", incidentId, closeType)` |
| `BroadcastSystemStatusAsync(snapshot)` | `Clients.All.SendAsync("SystemStatusUpdated", snapshot)` |

## 5.3 Dependencias inyectadas

`IHubContext<AlertsHub>`, `ILogger<NotificationService>`.

## 5.4 Stories cubiertas

US-03, US-06, US-14 (broadcast de cierre).

## 5.5 Notas

- Por ahora se hace broadcast a **todos** los clientes conectados — el MVP no requiere segmentación por rol (cualquier usuario autenticado ve el mismo flujo de alertas).
- Si en el futuro se necesita segmentar (p. ej. solo notificar al Técnico para `causa_no_determinada`), se evolucionará a `Clients.Group("Técnico")` con grupos administrados al conectar/desconectar.

---

# 6. Patrones de orquestación

## 6.1 Flujo "Detección → Alerta → Notificación" (UC1 / UC3 / UC4)

```text
MonitoringSchedulerService (M1)
    │ tick (5min o 10min)
    ▼
MonitoringService.RunCheckAsync(moduleId)
    │
    ├─> ICheckExecutor.CheckAsync()    (M2/M3/M4/M11)
    │
    ├─> ICauseClassifier.Classify()     (M7)   [solo si WARN/CRITICAL]
    │
    ├─> IAlertExplainer.Render()        (M10)
    │
    ├─> IIncidentService.OpenIncidentAsync()
    │        │
    │        └─> IIncidentRepository.CreateAsync()
    │
    └─> INotificationService.BroadcastAlertAsync()
             │
             └─> IHubContext<AlertsHub>.Clients.All.SendAsync("AlertReceived", ...)
                       │
                       └─> Blazor clients (RealtimePage) → re-render
```

## 6.2 Flujo "Cierre automático" (US-14)

```text
MonitoringService.RunCheckAsync(moduleId)
    │
    ├─> CheckResult.Status == Ok
    │
    ├─> Si chequeo anterior del mismo módulo también fue OK
    │       y hay incidente activo:
    │
    │        IIncidentService.CloseAutomaticallyAsync(incidentId)
    │              │
    │              └─> IIncidentRepository.CloseAsync(..., Automatic, null, null)
    │
    └─> INotificationService.BroadcastIncidentClosedAsync(incidentId, Automatic)
```

## 6.3 Flujo "Cierre manual" (US-13)

```text
Blazor IncidentDetailPage (botón "Cerrar manualmente")
    │ ClaimsPrincipal.Identity.Name + comentario
    ▼
IIncidentService.CloseManuallyAsync(incidentId, userId, comment)
    │ validar comentario no vacío
    │
    ├─> IIncidentRepository.CloseAsync(..., Manual, userId, comment)
    │
    └─> INotificationService.BroadcastIncidentClosedAsync(incidentId, Manual)
              │
              └─> AlertsHub.Clients.All.SendAsync("IncidentClosed", ...)
                        │
                        └─> Otros usuarios conectados ven el cierre en tiempo real
```

## 6.4 Flujo "Crear/editar regla" (US-19, US-20, US-22)

```text
Blazor RuleEditPage (formulario con razón)
    │ requiere rol Técnico
    ▼
IRuleManagementService.CreateRuleAsync(draft, currentUserId, reason)
    │ validar razón obligatoria
    │
    ├─> IRuleRepository.CreateAsync(rule)
    │
    └─> IRuleHistoryRepository.AppendAsync(historyEntry)
           │
           └─> Auditable, inmutable, consultable desde RuleHistoryPage
```

---

# 7. Registro de servicios (DI)

Pre-vista del `Program.cs` (referencial — no es código final):

```csharp
// Domain checkers (M2, M3, M4, M11)
builder.Services.AddScoped<ICheckExecutor, DbOrderChecker>();
builder.Services.AddScoped<ICheckExecutor, ApiChecker>();     // se registran 2 instancias si Sf y Mv son separados
builder.Services.AddScoped<ICheckExecutor, DbHealthChecker>();
builder.Services.AddScoped<ICheckExecutor, JobsChecker>();

// Domain services
builder.Services.AddSingleton<ICauseClassifier, CauseClassifier>();
builder.Services.AddSingleton<IAlertExplainer, AlertTemplateRenderer>();

// Repositories (implementaciones en MonitorPedidos.Infrastructure)
builder.Services.AddScoped<IRuleRepository, RuleRepository>();
builder.Services.AddScoped<IRuleHistoryRepository, RuleHistoryRepository>();
builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();

// Application services (capability layer)
builder.Services.AddScoped<IMonitoringService, MonitoringService>();
builder.Services.AddScoped<IRuleManagementService, RuleManagementService>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();

// Background workers
builder.Services.AddHostedService<MonitoringSchedulerService>();
builder.Services.AddHostedService<OrdersSimulatorService>();

// Identity, SignalR, Blazor Server, middlewares, etc. — se definen en NFR Design.
```

---

# 8. Validación INVEST → services

Cada application service cubre un grupo de stories cohesivo (ver §1.3, §2.6, §3.5, §4.5, §5.4). Ningún service cruza más de una capability:

| Service | Capability | Stories | INVEST |
|---------|------------|---------|--------|
| `MonitoringService` | Detección + clasificación + alerta | 8 stories (Operador / Técnico investigar) | ✅ |
| `RuleManagementService` | Configurar reglas (Técnico) | 5 stories | ✅ |
| `IncidentService` | Investigar + cerrar incidentes | 5 stories | ✅ |
| `AuthService` | Auth + roles (cross-cutting) | 3 stories | ✅ |
| `NotificationService` | Real-time push (cross-cutting) | 3 stories | ✅ |

---

# 9. Resumen

- **5 application services** alineados con las **4 sub-secciones de stories** (Monitorear / Detectar / Investigar / Configurar) + cross-cutting.
- **Orquestación clara**: `MonitoringService` es el único que coordina el flujo detección → alerta → notificación.
- **Separación estricta**: cada service tiene una capability; ningún service cruza dominio.
- **Push real-time encapsulado** en `NotificationService` — el resto del sistema no toca SignalR.
- Próximo artefacto: [`component-dependency.md`](./component-dependency.md) con la matriz de dependencias y el diagrama Mermaid de flujo de datos.
