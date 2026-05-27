# NFR Design Patterns — U5 Rules Management

**Unidad:** U5 — Rules Management
**Stage:** Construction → NFR Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Decisiones aplicadas

| ID | Pregunta | Decisión |
|----|----------|---------|
| P1 | Setup integration tests con SQLite in-memory | A — `IDisposable` por clase de test, una conexión por instancia |
| P2 | Autorización páginas Blazor | A — `[Authorize(Roles="Técnico")]` en directiva de página + `AuthorizeView` en NavMenu |
| P3 | Serialización System.Text.Json | A — Opciones estáticas localizadas en `Rule` y `RuleSnapshot` |
| P4 | Inmutabilidad RuleHistoryEntry | A — Contrato de interfaz `IRuleHistoryRepository` (sin Update/Delete) |

---

## ADR-U5-01: IDisposable por clase para integration tests con SQLite in-memory

**Contexto:** Los 4 integration tests de `RuleManagementServiceIntegrationTests` necesitan cada uno una BD limpia para garantizar aislamiento total. SQLite con `DataSource=:memory:` crea una BD nueva por conexión — cada conexión es una BD independiente.

**Decisión:** Implementar `IDisposable` en la clase de test. El constructor abre la `SqliteConnection` y llama `Database.EnsureCreated()`. `Dispose()` cierra la conexión y destruye la BD in-memory.

**Implementación:**

```csharp
public sealed class RuleManagementServiceIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext     _db;
    private readonly IRuleManagementService _sut;

    public RuleManagementServiceIntegrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();   // aplica el schema (tablas rules + rule_history)

        var ruleRepo    = new RuleRepository(_db);
        var historyRepo = new RuleHistoryRepository(_db);
        _sut = new RuleManagementService(ruleRepo, historyRepo);
    }

    [Fact]
    public async Task CreateRuleAsync_PersistsRuleAndHistoryEntry_InSingleTransaction()
    {
        var rule = await _sut.CreateRuleAsync(
            "Test Rule", "Desc",
            ModuleId.DbOrders,
            RuleCondition.ForDbOrders(windowHours: 2, minOrders: 1),
            Severity.Critical,
            authorUserId: "tecnico",
            reason: "Prueba inicial");

        _db.ChangeTracker.Clear();   // descarta cache de EF — fuerza lectura desde BD

        var persistedRule    = await _db.Rules.FindAsync(rule.Id);
        var persistedHistory = await _db.RuleHistory
            .Where(h => h.RuleId == rule.Id).ToListAsync();

        persistedRule.Should().NotBeNull();
        persistedHistory.Should().HaveCount(1);
        persistedHistory[0].ChangeType.Should().Be(RuleChangeType.Creation);
        persistedHistory[0].SnapshotAfter.Should().NotBeNullOrEmpty();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
```

**Consecuencias:**
- Cada instancia del test runner crea su propia BD — xUnit crea una nueva instancia de la clase por cada `[Fact]`
- `_db.ChangeTracker.Clear()` después de la operación garantiza que la aserción lee desde la BD real, no desde el cache de EF Core
- `Database.EnsureCreated()` aplica el schema sin ejecutar migraciones — requiere que el modelo EF Core esté completo

**Alternativa descartada:** `IClassFixture` — BD compartida entre tests. Requiere limpiar tablas entre tests; el orden de ejecución puede afectar el resultado.

---

## ADR-U5-02: Autorización doble capa en Blazor — atributo + AuthorizeView

**Contexto:** Las páginas de reglas deben ser accesibles solo al rol `Técnico`. En Blazor Server, la autorización tiene dos capas: la directiva del componente (server-side real) y el menú de navegación (UX).

**Decisión:** Usar `[Authorize(Roles="Técnico")]` en la directiva de cada página razor como barrera real, y `<AuthorizeView Roles="Técnico">` en `NavMenu.razor` como UX.

**Implementación:**

```razor
@* RulesPage.razor *@
@page "/rules"
@attribute [Authorize(Roles = "Técnico")]

@* RuleEditPage.razor *@
@page "/rules/new"
@page "/rules/{Id:guid}/edit"
@attribute [Authorize(Roles = "Técnico")]

@* RuleHistoryPage.razor *@
@page "/rules/{Id:guid}/history"
@attribute [Authorize(Roles = "Técnico")]
```

```csharp
// Program.cs — ya configurado en U1, confirmar que AccessDeniedPath apunta a /access-denied
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.AccessDeniedPath = "/access-denied";   // BR-RULE-03 / P3
        // ...resto de config U1
    });
```

```razor
@* NavMenu.razor — entrada condicional *@
<AuthorizeView Roles="Técnico">
    <Authorized>
        <NavLink href="/rules" Match="NavLinkMatch.Prefix">
            Reglas
        </NavLink>
    </Authorized>
</AuthorizeView>
```

**Consecuencias:**
- Si un Operador navega directamente a `/rules`, ASP.NET Core intercepta antes de renderizar el componente Blazor y redirige a `/access-denied`
- `AuthorizeView` en el menú es solo UX — elimina la entrada del menú visual pero no es la barrera de seguridad
- `CascadingAuthenticationState` (configurado en U1 en `App.razor`) provee el estado de autenticación a todos los componentes — no hay setup adicional en U5

**Invariante:** La barrera de seguridad real es `[Authorize]` (server-side). `AuthorizeView` es secundaria. Nunca usar solo `AuthorizeView` como única restricción.

---

## ADR-U5-03: JsonSerializerOptions estáticas localizadas para RuleCondition/RuleSnapshot

**Contexto:** `Rule.GetCondition()` deserializa `ConditionJson` y `RuleSnapshot.ToJson()` serializa el estado de la regla. Instanciar `JsonSerializerOptions` en cada llamada es costoso (EF interno de reflection caching de System.Text.Json).

**Decisión:** Definir una instancia estática `readonly` de `JsonSerializerOptions` dentro de cada clase que la usa.

**Implementación:**

```csharp
// En Rule.cs
public sealed class Rule
{
    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public RuleCondition GetCondition()
        => JsonSerializer.Deserialize<RuleCondition>(ConditionJson, _jsonOpts)
           ?? throw new InvalidOperationException(
               $"Rule {Id}: ConditionJson no puede deserializarse a RuleCondition.");
    // NFR-U5-04: sin try/catch — fail fast
}

// En RuleSnapshot.cs
public sealed record RuleSnapshot(...)
{
    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public string ToJson() => JsonSerializer.Serialize(this, _jsonOpts);

    public static RuleSnapshot From(Rule rule) =>
        new(rule.Id, rule.Name, ...);
}
```

**Consecuencias:**
- La instancia estática es creada una vez y reutilizada — el cache de reflection de System.Text.Json se construye solo una vez por tipo
- `PropertyNamingPolicy.CamelCase` es consistente con la convención de JSON en ASP.NET Core sin afectar la serialización HTTP global
- El mensaje de excepción en `GetCondition()` incluye el `Id` de la regla para facilitar diagnóstico (NFR-U5-04 fail fast)

**Alternativa descartada:** Configuración global en `Program.cs` — afectaría todos los endpoints HTTP y componentes Blazor, lo que puede romper serialización de `AlertMessage` u otros tipos del dominio.

---

## ADR-U5-04: Inmutabilidad de RuleHistoryEntry por contrato de interfaz

**Contexto:** `RuleHistoryEntry` debe ser inmutable una vez creada (SECURITY-13). La única forma de garantizarlo sin restricciones de BD es a través del contrato de `IRuleHistoryRepository`.

**Decisión:** La interfaz `IRuleHistoryRepository` expone únicamente `AppendAsync` y métodos de consulta. No hay `Update`, `Delete`, ni `SaveChangesAsync` que permita modificaciones. El `RuleHistoryRepository` concreto no implementa ninguna operación de mutación sobre entradas existentes.

**Implementación:**

```csharp
// IRuleHistoryRepository — contrato completo
public interface IRuleHistoryRepository
{
    Task AppendAsync(RuleHistoryEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<RuleHistoryEntry>> GetByRuleIdAsync(Guid ruleId, CancellationToken ct = default);
    Task<IReadOnlyList<RuleHistoryEntry>> GetAllAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
    // Sin Update, sin Delete, sin SetActive, sin ninguna mutación
}

// RuleHistoryRepository — implementación concreta
public sealed class RuleHistoryRepository : IRuleHistoryRepository
{
    private readonly AppDbContext _db;

    public RuleHistoryRepository(AppDbContext db) => _db = db;

    public async Task AppendAsync(RuleHistoryEntry entry, CancellationToken ct = default)
        => await _db.RuleHistory.AddAsync(entry, ct);
        // SaveChangesAsync lo llama el servicio, no el repositorio

    public async Task<IReadOnlyList<RuleHistoryEntry>> GetByRuleIdAsync(Guid ruleId, CancellationToken ct = default)
        => await _db.RuleHistory
            .Where(h => h.RuleId == ruleId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<RuleHistoryEntry>> GetAllAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
        => await _db.RuleHistory
            .Where(h => h.ChangedAt >= from && h.ChangedAt <= to)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync(ct);
}
```

**Nota sobre `SaveChangesAsync`:** El repositorio de historial NO llama `SaveChangesAsync` — lo hace `RuleManagementService` después de agregar tanto la `Rule` como la `RuleHistoryEntry`. Esto garantiza la transacción implícita (ADR-U5-01 de persistencia).

**Consecuencias:**
- Cualquier desarrollador que intente actualizar o eliminar historial desde código de aplicación no encontrará el método — la interfaz es la única API pública
- Acceso directo al `DbContext` o a la BD puede saltarse esta restricción — se documenta como riesgo a mitigar post-MVP con triggers SQL (BR-RULE-02)
- El historial se retorna siempre en orden `ChangedAt DESC` — sin ordenamiento adicional en la UI

**Reevaluación post-MVP:** Al pasar a on-prem con múltiples usuarios, considerar agregar trigger `INSTEAD OF UPDATE, DELETE` en la tabla `rule_history` para defensa en profundidad a nivel BD.
