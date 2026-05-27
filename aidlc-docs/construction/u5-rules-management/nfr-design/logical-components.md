# Logical Components — U5 Rules Management

**Unidad:** U5 — Rules Management
**Stage:** Construction → NFR Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Componentes de producción introducidos en U5

| ID | Componente | Tipo | Proyecto | Descripción |
|----|-----------|------|---------|-------------|
| LC-U5-01 | `Rule` | Aggregate Root | MonitorPedidos.Web | Entidad central con `Create`, `Update`, `Activate`, `Deactivate`; serializa/deserializa `ConditionJson` |
| LC-U5-02 | `RuleCondition` | Value object (record) | MonitorPedidos.Web | Parámetros configurables por módulo: `WindowHours`, `MinOrders`, `LatencyWarnMs`, `LatencyCriticalMs` |
| LC-U5-03 | `RuleSnapshot` | Value object (record) | MonitorPedidos.Web | Foto completa del estado de `Rule` serializada a JSON para `SnapshotBefore`/`SnapshotAfter` |
| LC-U5-04 | `RuleHistoryEntry` | Entity (inmutable) | MonitorPedidos.Web | Registro inmutable de cada cambio; factories `ForCreation`, `ForEdit`, `ForActivation`, `ForDeactivation` |
| LC-U5-05 | `IRuleRepository` | Interfaz | MonitorPedidos.Web | CRUD de reglas + `GetActiveByModuleAsync` para consumo de checkers |
| LC-U5-06 | `RuleRepository` | Clase (IRuleRepository) | MonitorPedidos.Web | Implementación EF Core; `SaveChangesAsync` NO llamado aquí — lo llama el servicio |
| LC-U5-07 | `IRuleHistoryRepository` | Interfaz | MonitorPedidos.Web | Solo `AppendAsync` + consultas — sin Update ni Delete (SECURITY-13) |
| LC-U5-08 | `RuleHistoryRepository` | Clase (IRuleHistoryRepository) | MonitorPedidos.Web | Implementación EF Core; retorna historial ordenado `ChangedAt DESC` |
| LC-U5-09 | `IRuleManagementService` | Interfaz | MonitorPedidos.Web | Contrato del servicio de aplicación: Create, Update, Activate, Deactivate, GetHistory |
| LC-U5-10 | `RuleManagementService` | Clase (IRuleManagementService) | MonitorPedidos.Web | Orquestador: valida → muta aggregate → crea historial → `SaveChangesAsync` único |
| LC-U5-11 | `RulesPage` | Blazor Page | MonitorPedidos.Web | Lista de reglas + toggle activo/inactivo con modal de razón; solo Técnico |
| LC-U5-12 | `RuleEditPage` | Blazor Page | MonitorPedidos.Web | Formulario crear/editar con sección de condición dinámica por módulo; solo Técnico |
| LC-U5-13 | `RuleHistoryPage` | Blazor Page | MonitorPedidos.Web | Historial inmutable con diff before/after side-by-side; solo Técnico |

---

## §2 Componentes de producción modificados en U5

| Componente | Unidad origen | Modificación |
|-----------|--------------|-------------|
| `DbOrderChecker` | U3 | Lee reglas activas vía `IRuleRepository.GetActiveByModuleAsync(DbOrders)` en lugar de valores hardcodeados; `RuleCondition.WindowHours`/`MinOrders` determinan la ventana de evaluación |
| `NavMenu.razor` | U1 | Agrega entrada "Reglas" dentro de `<AuthorizeView Roles="Técnico">` |
| `AppDbContext` | U2 | Agrega `DbSet<Rule> Rules` y `DbSet<RuleHistoryEntry> RuleHistory` |

---

## §3 Componentes de test introducidos en U5

| ID | Componente | Tipo | Proyecto | Escenarios |
|----|-----------|------|---------|-----------|
| TC-U5-01 | `RuleManagementServiceValidationTests` | xUnit + Moq | MonitorPedidos.UnitTests | 5 tests: reason vacía (Create/Update/Activate/Deactivate) + condition inválida para módulo |
| TC-U5-02 | `RuleManagementServiceIntegrationTests` | xUnit + SQLite in-memory | MonitorPedidos.UnitTests | 4 tests: Create atómico, Update con snapshot diff, Deactivate, Activate idempotente |
| TC-U5-03 | `DbOrderCheckerTests` (extensión U3) | xUnit + Moq | MonitorPedidos.UnitTests | 3 tests nuevos: regla activa + 0 órdenes → Critical, regla activa + 1 orden → Ok, sin reglas → Ok |

**Total tests U5:** 12 tests (5 unit + 4 integration + 3 unit extensión U3)

---

## §4 Diagrama de dependencias U5

```
RulesPage / RuleEditPage / RuleHistoryPage
    [Authorize(Roles="Técnico")]    ← ADR-U5-02
    |
    IRuleManagementService
    |
    RuleManagementService
        ├── IRuleRepository
        │       RuleRepository → AppDbContext → tabla rules
        │
        ├── IRuleHistoryRepository    ← ADR-U5-04: sin Update/Delete
        │       RuleHistoryRepository → AppDbContext → tabla rule_history
        │
        └── SaveChangesAsync único   ← transacción implícita EF Core (ADR del servicio)

Rule.GetCondition()
    → JsonSerializer.Deserialize<RuleCondition>(ConditionJson, _jsonOpts)  ← ADR-U5-03
    → fail fast si malformado                                              ← NFR-U5-04

RuleSnapshot.ToJson()
    → JsonSerializer.Serialize(this, _jsonOpts)                           ← ADR-U5-03

DbOrderChecker (U3 — modificado en U5)
    → IRuleRepository.GetActiveByModuleAsync(DbOrders)
    → rule.GetCondition() → WindowHours, MinOrders
    → evalúa pedidos en ventana

NavMenu.razor
    → <AuthorizeView Roles="Técnico"> → entrada "Reglas" visible / oculta

RuleManagementServiceIntegrationTests
    → IDisposable + SqliteConnection("DataSource=:memory:")  ← ADR-U5-01
    → AppDbContext.Database.EnsureCreated()
    → verifica atomicidad Rule + RuleHistoryEntry
```

---

## §5 Reglas de diseño aplicadas en U5

| ADR | Patrón | Componentes afectados |
|-----|--------|----------------------|
| ADR-U5-01 | IDisposable por clase + SQLite in-memory | RuleManagementServiceIntegrationTests |
| ADR-U5-02 | `[Authorize(Roles="Técnico")]` + `AuthorizeView` en NavMenu | RulesPage, RuleEditPage, RuleHistoryPage, NavMenu |
| ADR-U5-03 | JsonSerializerOptions estáticas localizadas | Rule.GetCondition(), RuleSnapshot.ToJson() |
| ADR-U5-04 | Inmutabilidad por contrato de interfaz | IRuleHistoryRepository, RuleHistoryRepository |
| ADR-U1 (cookie auth) | CascadingAuthenticationState + AccessDeniedPath | Toda la app — sin setup adicional en U5 |
| ADR-U3-02 (IEnumerable DI) | IRuleRepository inyectado en DbOrderChecker | DbOrderChecker (modificado) |

---

## §6 Trazabilidad de componentes

| Componente | Story | RF | NFR | ADR |
|-----------|-------|----|----|-----|
| Rule + RuleCondition + RuleSnapshot | US-19, US-20, US-21 | RF-14, RF-15, RF-16 | NFR-U5-04 | ADR-U5-03 |
| RuleHistoryEntry | US-22 | RF-16 | — | ADR-U5-04 |
| IRuleManagementService / RuleManagementService | US-19..US-22 | RF-14..RF-17 | NFR-U5-01, NFR-U5-02 | ADR-U5-01 |
| RulesPage / RuleEditPage / RuleHistoryPage | US-19..US-22 | RF-14..RF-17 | — | ADR-U5-02 |
| DbOrderChecker (modificado) | US-07 | RF-02, RF-14 | NFR-U5-03 | ADR-U3-02 |
| RuleManagementServiceValidationTests (5) | US-19..US-21 | RF-17 | NFR-U5-01 | — |
| RuleManagementServiceIntegrationTests (4) | US-19..US-21 | RF-14..RF-16 | NFR-U5-02 | ADR-U5-01 |
| DbOrderCheckerTests extensión (3) | US-07 | RF-02, RF-14 | NFR-U5-03 | — |
