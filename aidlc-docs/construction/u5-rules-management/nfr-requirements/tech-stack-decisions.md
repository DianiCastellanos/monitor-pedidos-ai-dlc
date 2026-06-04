# Tech Stack Decisions — U5 Rules Management

**Unidad:** U5 — Rules Management
**Stage:** Construction → NFR Requirements
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Resumen de decisiones

U5 introduce **1 paquete NuGet nuevo de test**. No hay paquetes nuevos de producción — toda la lógica de U5 se implementa con el stack ya establecido (EF Core 8, Blazor Server, Serilog).

---

## §2 Paquete NuGet nuevo — Tests

### Microsoft.EntityFrameworkCore.Sqlite

| Campo | Valor |
|-------|-------|
| **Paquete** | `Microsoft.EntityFrameworkCore.Sqlite` v8.x |
| **Proyecto** | `MonitorPedidos.UnitTests` |
| **Propósito** | Provider SQLite in-memory para integration tests de `RuleManagementService`. A diferencia del provider `InMemory`, SQLite soporta transacciones reales, lo que permite verificar la atomicidad Rule + RuleHistoryEntry (NFR-U5-02) |
| **Alternativa descartada** | `Microsoft.EntityFrameworkCore.InMemory` — no soporta transacciones implícitas de EF Core; los tests de atomicidad darían falsos positivos |

**Uso en tests:**

```csharp
// Setup de AppDbContext con SQLite in-memory por test
public class RuleManagementServiceIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext     _db;

    public RuleManagementServiceIntegrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();   // aplica el schema sin migraciones
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
```

---

## §3 Stack heredado de U1–U4 (sin cambios)

| Tecnología | Versión | Uso en U5 |
|-----------|---------|-----------|
| ASP.NET Core 8 | 8.x | Middleware de autorización para páginas de reglas |
| Blazor Server | 8.x | RulesPage, RuleEditPage, RuleHistoryPage |
| EF Core 8 | 8.x | Rule, RuleHistoryEntry — tablas `rules` y `rule_history` |
| SQL Server MonitorPedidosDb (172.16.0.41) | 2019+ | BD principal de desarrollo; migrations `AddRulesTables` |
| Serilog | 3.x | Log de operaciones CRUD (nivel Information) |
| Moq | 4.x | Mock de `IRuleRepository` e `IRuleHistoryRepository` en unit tests de validación |
| xUnit | 2.x | Framework de test para unit e integration tests |
| `System.Text.Json` | SDK nativo | Serialización/deserialización de `RuleCondition` y `RuleSnapshot` — sin paquete adicional |

---

## §4 Justificación: sin paquetes de producción nuevos

| Componente | Tecnología elegida | Razón |
|-----------|-------------------|-------|
| Serialización `ConditionJson` | `System.Text.Json` (SDK .NET 8) | Ya incluido; no requiere Newtonsoft.Json |
| Validación de formularios | DataAnnotations + validación server-side en servicio | Suficiente para los pocos campos del formulario; sin librería de validación adicional |
| Diff visual antes/después | Blazor markup nativo (dos columnas JSON pretty-printed) | Sin librería de diff — el snapshot completo antes/después es suficiente para la UI |
| Autorización | `[Authorize(Roles="Técnico")]` + `AuthenticationStateProvider` | Ya configurado en U1 (cookie auth + claims de rol) |

---

## §5 Evolución del proyecto MonitorPedidos.UnitTests

| Unidad | Paquetes agregados | Tests acumulados |
|--------|-------------------|-----------------|
| U3 | Moq, xUnit, FluentAssertions, coverlet | Tests para CauseClassifier, AlertTemplateRenderer, MonitoringSchedulerService |
| U4 | RichardSzalay.MockHttp | + SalesforceClientTests, MultivendeClientTests, ApiRetryPolicyTests |
| U5 | **Microsoft.EntityFrameworkCore.Sqlite** | + RuleManagementServiceValidationTests (5 unit tests) + RuleManagementServiceIntegrationTests (4 integration tests) + DbOrderCheckerTests extensión (3 nuevos casos) |

---

## §6 Trazabilidad

| Decisión | Paquete | Componentes | NFR |
|----------|---------|------------|-----|
| SQLite in-memory para integration tests | `Microsoft.EntityFrameworkCore.Sqlite` | RuleManagementServiceIntegrationTests | NFR-U5-02 |
| Moq para unit tests de validación | `Moq` (existente) | RuleManagementServiceValidationTests, DbOrderCheckerTests | NFR-U5-01, NFR-U5-03 |
| System.Text.Json para ConditionJson | SDK nativo | Rule.GetCondition(), RuleSnapshot.ToJson() | NFR-U5-04 |
