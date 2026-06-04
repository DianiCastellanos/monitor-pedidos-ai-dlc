# Logical Components — U2 Persistence & Incidents

**Unidad:** U2 — Persistence & Incidents
**Stage:** Construction → NFR Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Vista general de componentes lógicos de U2

```
MonitorPedidos.Domain/Incidents/
+-- Incident                        (aggregate root — §2)
+-- IIncidentRepository             (contrato de persistencia — §3)
+-- IIncidentService                (contrato de servicio — §4)
+-- IncidentSearchFilter            (value object — query)
+-- WeeklySummaryEntry              (value object — resumen)

MonitorPedidos.Domain/Shared/
+-- PagedResult<T>                  (value object — paginación)

MonitorPedidos.Infrastructure/Persistence/
+-- IncidentRepository              (implementación EF Core — §5)
+-- Configurations/
    +-- IncidentConfiguration       (IEntityTypeConfiguration<Incident>)

MonitorPedidos.Web/
+-- Features/Incidents/
|   +-- HistoricPage                (Blazor Page — §6)
|   +-- WeeklySummaryPage           (Blazor Page — §7)
|   +-- IncidentDetailPage          (Blazor Page — §8)
+-- Services/
    +-- IncidentService             (application service — §9)

tests/MonitorPedidos.IntegrationTests/
+-- Incidents/
    +-- IncidentRepositoryTests     (T-U2-01, T-U2-04)
    +-- IncidentServiceTests        (T-U2-02, T-U2-03, T-U2-05)
```

---

## §2 Componente: Incident (Aggregate Root)

| Atributo | Detalle |
|----------|---------|
| **Tipo** | Domain Entity / Aggregate Root |
| **Namespace** | `MonitorPedidos.Domain.Incidents` |
| **Responsabilidad** | Encapsula el estado y las transiciones del ciclo de vida de un incidente |
| **Comportamiento** | `Open()` (factory) · `CloseAutomatically()` · `CloseManually(role, comment)` · `IsExpired(days)` |
| **Invariantes** | INV-01..INV-05 (ver business-rules.md) |
| **Dependencias** | Ninguna — solo tipos del dominio (enums, AlertMessage) |

---

## §3 Componente: IIncidentRepository

| Atributo | Detalle |
|----------|---------|
| **Tipo** | Interface (contrato de persistencia) |
| **Namespace** | `MonitorPedidos.Domain.Incidents` |
| **Responsabilidad** | Define el contrato de acceso a datos para `Incident` |
| **Patrón** | Repository Pattern — aísla el dominio de EF Core |

**Métodos:**

| Método | Patrón aplicado | ADR |
|--------|----------------|-----|
| `AddOrGetExistingAsync` | Try-Catch + Re-Query (ADR-U2-01) | ADR-U2-01 |
| `GetByIdAsync` | Tracking normal (para modificación posterior) | — |
| `GetOpenByModuleAsync` | AsNoTracking | ADR-U2-02 |
| `GetRecentClosedByModuleAsync` | AsNoTracking | ADR-U2-02 |
| `SearchAsync` | AsNoTracking + paginación | ADR-U2-02 |
| `GetWeeklySummaryAsync` | AsNoTracking + LINQ GroupBy | ADR-U2-02 |
| `PurgeExpiredAsync` | ExecuteDeleteAsync | ADR-U2-03 |

---

## §4 Componente: IIncidentService

| Atributo | Detalle |
|----------|---------|
| **Tipo** | Interface (contrato de servicio de aplicación) |
| **Namespace** | `MonitorPedidos.Domain.Incidents` |
| **Responsabilidad** | Orquesta la lógica de negocio del ciclo de vida de incidentes usando `IIncidentRepository` |

**Interacciones:**

```
IIncidentService
    |-- usa --> IIncidentRepository (persistencia)
    |-- usa --> INotificationService (U3 — broadcast de alertas y cierres)
    |-- loggea --> ILogger<IncidentService>
```

---

## §5 Componente: IncidentRepository

| Atributo | Detalle |
|----------|---------|
| **Tipo** | EF Core Repository |
| **Namespace** | `MonitorPedidos.Infrastructure.Persistence` |
| **Clase** | `IncidentRepository : IIncidentRepository` |
| **Dependencias** | `AppDbContext` (inyectado por DI) |
| **Registro DI** | `services.AddScoped<IIncidentRepository, IncidentRepository>()` |

**Comportamiento crítico — idempotencia (ADR-U2-01):**

```
AddOrGetExistingAsync(incident)
    |
    v
_context.Incidents.AddAsync(incident)
_context.SaveChangesAsync()
    |
    +-> [Éxito] --> retorna incident nuevo
    |
    +-> [DbUpdateException con IX_incidents_Module_Open] -->
        _context.ChangeTracker.Clear()
        FirstAsync(i => i.Module == module && i.ClosedAt == null)
        retorna incident existente
```

---

## §6 Componente: HistoricPage

| Atributo | Detalle |
|----------|---------|
| **Tipo** | Blazor Server Page |
| **Ruta** | `/incidents` |
| **Namespace** | `MonitorPedidos.Web.Features.Incidents` |
| **Autorización** | `[Authorize]` |
| **Dependencias** | `IIncidentService` |

**Interacciones:**

```
HistoricPage
    |-- OnInitializedAsync → IIncidentService.SearchHistoryAsync(filtro vacío)
    |-- OnFilterChanged   → IIncidentService.SearchHistoryAsync(filtro aplicado)
    |-- OnNextPage        → skip += take → SearchHistoryAsync
    |-- OnPrevPage        → skip -= take → SearchHistoryAsync
    |-- NavigateTo        → /incidents/{id}
```

---

## §7 Componente: WeeklySummaryPage

| Atributo | Detalle |
|----------|---------|
| **Tipo** | Blazor Server Page |
| **Ruta** | `/incidents/weekly` |
| **Namespace** | `MonitorPedidos.Web.Features.Incidents` |
| **Autorización** | `[Authorize]` |
| **Dependencias** | `IIncidentService` |

**Interacciones:**

```
WeeklySummaryPage
    |-- OnInitializedAsync → weekStart = lunes actual
                           → IIncidentService.GetWeeklySummaryAsync(weekStart)
    |-- OnWeekChanged      → IIncidentService.GetWeeklySummaryAsync(newWeek)
```

---

## §8 Componente: IncidentDetailPage

| Atributo | Detalle |
|----------|---------|
| **Tipo** | Blazor Server Page |
| **Ruta** | `/incidents/{id:guid}` |
| **Namespace** | `MonitorPedidos.Web.Features.Incidents` |
| **Autorización** | `[Authorize]` |
| **Dependencias** | `IIncidentService`, `AuthenticationStateProvider` |

**Interacciones:**

```
IncidentDetailPage
    |-- OnInitializedAsync → IIncidentService.GetByIdAsync(id)
    |                         [No encontrado → redirige /Error]
    |-- OnPostClose        → IIncidentService.CloseManuallyAsync(id, role, comentario)
    |                         [comentario vacío → muestra error inline]
    |                         [OK → NavigateTo /incidents]
    |-- lee rol            → AuthenticationStateProvider → ClaimTypes.Role
    |-- extiende en U4     → muestra auto_reintento para incidentes de tipo API
```

---

## §9 Componente: IncidentService

| Atributo | Detalle |
|----------|---------|
| **Tipo** | Application Service |
| **Namespace** | `MonitorPedidos.Web.Services` |
| **Clase** | `IncidentService : IIncidentService` |
| **Registro DI** | `services.AddScoped<IIncidentService, IncidentService>()` |
| **Dependencias** | `IIncidentRepository`, `INotificationService` (stub en U2, real en U6), `ILogger<IncidentService>` |

**Lógica del cierre automático (ADR-U2-01 + BR-INC-04):**

```
TryCloseOnConsecutiveOkAsync(moduleId)
    |
    v
GetOpenByModuleAsync(moduleId)
    |
    +-> [null] --> return false
    |
    +-> [incidente abierto] -->
        GetRecentClosedByModuleAsync(moduleId, count: 2)
            |
            ¿Últimos 2 checks del módulo = OK?
                |
                +-> [No] --> return false
                |
                +-> [Sí] -->
                    incident.CloseAutomatically()
                    SaveChangesAsync()
                    INotificationService.BroadcastCloseAsync(id)
                    return true
```

---

## §10 Suite de integration tests

| Test | Clase | Método | NFR / BR verificado |
|------|-------|--------|---------------------|
| T-U2-01 | `IncidentRepositoryTests` | `OpenIncident_SecondOpenSameModule_ReturnsExisting` | BR-INC-01, ADR-U2-01 |
| T-U2-02 | `IncidentServiceTests` | `CloseManually_EmptyComment_ThrowsDomainException` | BR-CLOSE-01, INV-02 |
| T-U2-03 | `IncidentServiceTests` | `TryCloseOnConsecutiveOk_TwoOk_ClosesIncident` | BR-INC-04, RF-19 |
| T-U2-04 | `IncidentRepositoryTests` | `SearchAsync_FilterBySeverity_ReturnsPaged` | RF-22, BR-SEARCH-01 |
| T-U2-05 | `IncidentServiceTests` | `PurgeExpired_OlderThan90Days_Deleted` | BR-PURGE-01, RF-21 |

---

## §11 Trazabilidad completa

| Componente | ADR | NFR | Story | SECURITY |
|-----------|-----|-----|-------|----------|
| IncidentRepository.AddOrGetExistingAsync | ADR-U2-01 | NFR-U2-01 | US-05 | SECURITY-05 |
| AsNoTracking en lecturas | ADR-U2-02 | NFR-U2-02 | US-11, US-12 | — |
| IncidentRepository.PurgeExpiredAsync | ADR-U2-03 | BR-PURGE-01 | RF-21 | — |
| TestWebAppFactory con MonitorPedidosDb | ADR-U2-04 | NFR-U2-04 | US-13, US-14 | SECURITY-05 |
| HistoricPage | — | RNF-01 | US-11 | SECURITY-08 |
| WeeklySummaryPage | — | RNF-01 | US-12 | SECURITY-08 |
| IncidentDetailPage | — | RF-20 | US-13 | SECURITY-06, SECURITY-08 |
| IncidentService | — | RF-18..RF-22 | US-05, US-13, US-14 | SECURITY-15 |
