# Integration Test Instructions — MonitorPedidos

**Fecha**: 2026-05-24
**Proyecto**: MonitorPedidos — Manufacturas Eliot
**Solución**: `MonitorPedidos.sln`
**Framework**: xUnit + WebApplicationFactory (ASP.NET Core 8)

> **Estado**: Act5 (Code Generation) pendiente de activación — este documento describe las pruebas de integración planificadas para cuando el código esté generado. Los flujos y configuraciones aquí descritos corresponden al diseño aprobado.

---

## §1 Scope

Las pruebas de integración de MonitorPedidos verifican la **interacción entre capas reales del sistema**, sin mocks de base de datos. El objetivo es detectar errores que los tests unitarios no cubren: queries EF Core incorrectas, problemas de mapping, comportamiento real del pipeline ASP.NET Core, y coherencia del schema de BD.

**Lo que cubren los integration tests**:
- Pipeline HTTP completo (middleware, autenticación, autorización, routing)
- Queries EF Core contra SQL Server LocalDB real
- Transiciones de estado persisted en base de datos
- Behavior del Identity framework con cookies reales
- Flujo de datos desde servicio de dominio hasta repositorio hasta BD

**Lo que NO cubren los integration tests**:
- Llamadas a APIs externas (Salesforce, Multivende) — esas usan mocks en tests unitarios (ver `unit-test-instructions.md` §2, U4)
- SignalR en tiempo real — verificado manualmente (ver `performance-test-instructions.md` §5)
- Escenarios de carga — cubiertos en `performance-test-instructions.md`

---

## §2 WebApplicationFactory

Los integration tests de U1 y los flujos de capas usan `WebApplicationFactory<Program>` de ASP.NET Core Testing para levantar la aplicación completa en memoria con una base de datos de test aislada.

**Configuración de la factory**:

```csharp
public class MonitorPedidosWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Reemplazar la conexión de producción con la BD de test
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    "Server=(localdb)\\MSSQLLocalDB;Database=MonitorPedidosTestDb;Trusted_Connection=True;"));

            // Deshabilitar el scheduler de background para no interferir con tests
            services.AddSingleton<IHostedService, NoopMonitoringService>();
        });

        builder.Configure(app =>
        {
            // Aplicar migrations automáticamente antes de los tests
            using var scope = app.ApplicationServices.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        });
    }
}
```

**Uso en clases de test**:
```csharp
public class SecurityIntegrationTests : IClassFixture<MonitorPedidosWebApplicationFactory>
{
    private readonly HttpClient _client;

    public SecurityIntegrationTests(MonitorPedidosWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
    }
}
```

**Consideraciones importantes**:
- `AllowAutoRedirect = false` permite verificar el código HTTP 302 exacto en redirects de login
- `HandleCookies = true` es necesario para que la cookie de autenticación persista entre requests
- El factory debe implementar `IDisposable` para limpiar la BD después de la suite

---

## §3 Pruebas de Integración por Flujo

### Flujo 1: Autenticación

**Objetivo**: Verificar que el pipeline de autenticación completo funciona end-to-end.

**Pasos del flujo**:
```
POST /Identity/Select
  → Identity Framework valida credenciales contra AspNetUsers en BD
  → Emite cookie .AspNetCore.Identity.Application
  → Redirect 302 a /Dashboard
  → GET /Dashboard con cookie válida
  → HTTP 200, HTML del dashboard
```

**Tests**:
```
Precondición: seed de usuario administrador en MonitorPedidosTestDb
```
1. POST a `/Identity/Select` con credenciales válidas → verificar HTTP 302 + `Location: /Dashboard` + cookie presente
2. GET a `/Dashboard` con cookie → verificar HTTP 200
3. POST a `/Identity/Select` con credenciales inválidas → verificar no hay cookie, redirección a login con error
4. GET a `/ReglasMonitoreo` sin cookie → verificar redirect a `/Identity/Login`
5. GET a `/ReglasMonitoreo` con cookie de Operador → verificar HTTP 403

**Datos de test**:
```
Admin: email=test-admin@eliot.local, password=TestAdmin123!, role=Administrador
Operador: email=test-operador@eliot.local, password=TestOp123!, role=Operador
```

---

### Flujo 2: Creación y Persistencia de Incidente

**Objetivo**: Verificar que `BrandMonitorChecker` detecta una condición de fallo y el incidente queda correctamente persistido en `brand_snapshots` e `incidents`.

**Pasos del flujo**:
```
BrandMonitorChecker.CheckAllBrandsAsync()
  → ISalesforceClient.GetPendingOrdersAsync(brand) [mock en test de integración]
  → IIncidentService.CreateAsync(command)
  → IncidentRepository.AddAsync(incident)
  → AppDbContext.SaveChangesAsync()
  → Verificar: registro en tabla incidents con estado=Open
```

**Tests**:
1. Ejecutar `CheckAllBrandsAsync` con mock de Salesforce que retorna umbral superado para `Patprimo`
2. Consultar `AppDbContext.Incidents` directamente → debe existir exactamente 1 registro con `Brand = "Patprimo"`, `Status = Open`
3. Ejecutar `CheckAllBrandsAsync` nuevamente → no debe crearse duplicado (incidente ya abierto)
4. Resolver el incidente → `IIncidentService.ResolveAsync(incidentId)` → verificar `Status = Resolved`, `ResolvedAt != null`

**Nota**: En este flujo de integración, `ISalesforceClient` sí se mockea (su comportamiento pertenece a unit tests de U4). Lo que se prueba sin mock es la capa de repositorio y base de datos.

---

### Flujo 3: Historial de Incidentes con Filtros

**Objetivo**: Verificar que `IIncidentService.GetHistoryAsync` aplica filtros correctamente contra la base de datos real.

**Pasos del flujo**:
```
Seed de 10 incidentes en BD (mezcla de marcas, fechas, estados)
  → IIncidentService.GetHistoryAsync(filters)
  → IncidentRepository.GetByFiltersAsync(query)
  → EF Core genera SQL con WHERE + ORDER BY + paginación
  → Resultado verificado contra datos seed conocidos
```

**Tests**:
1. Filtro por marca `Patprimo` → solo incidentes de esa marca
2. Filtro por rango de fechas → solo incidentes en el período
3. Filtro por estado `Open` → solo incidentes abiertos
4. Filtros combinados (marca + estado + fecha) → intersección correcta
5. Paginación: página 1 de 5 con 2 por página → 2 registros, `TotalCount = 10`
6. Sin filtros → todos los registros, ordenados por `CreatedAt DESC`

**Seed de datos para este flujo**:
```csharp
private async Task SeedIncidentsAsync(AppDbContext db)
{
    var brands = new[] { "Patprimo", "SevenSeven", "Atmos", "Ostu" };
    // 10 incidentes distribuidos en las 4 marcas, fechas variadas, estados variados
    // Creados con fechas fijas para que los tests sean determinísticos
}
```

---

### Flujo 4: Reglas de Monitoreo

**Objetivo**: Verificar que `IRuleRepository.GetActiveByModuleAsync` retorna las reglas correctas desde la base de datos real (incluyendo las seeded por `AddBrandSnapshots`).

**Pasos del flujo**:
```
Migration AddBrandSnapshots aplicada (seed de 4 reglas)
  → IRuleRepository.GetActiveByModuleAsync("BrandMonitor")
  → EF Core query: SELECT * FROM rules WHERE Module = 'BrandMonitor' AND IsActive = 1
  → Resultado: 4 reglas (una por marca)
```

**Tests**:
1. `GetActiveByModuleAsync("BrandMonitor")` → exactamente 4 reglas activas
2. Verificar que cada regla tiene `RuleConditions` cargadas (eager loading correcto)
3. Verificar que reglas inactivas NO aparecen en el resultado
4. Crear una nueva regla → persiste correctamente → aparece en próxima consulta
5. Desactivar una regla → no aparece en `GetActiveByModuleAsync` → sí aparece en `GetAllByModuleAsync`

---

## §4 Base de Datos de Test

**Nombre**: `MonitorPedidosTestDb`
**Server**: `(localdb)\MSSQLLocalDB`
**Connection String de test**:
```
Server=(localdb)\MSSQLLocalDB;Database=MonitorPedidosTestDb;Trusted_Connection=True;MultipleActiveResultSets=true
```

**Separación de la BD de producción**:
- BD de desarrollo: `MonitorPedidosDb`
- BD de test: `MonitorPedidosTestDb`
- Son completamente independientes; aplicar migrations en ambas por separado

**Crear la BD de test** (primera vez):
```bash
# La WebApplicationFactory aplica las migrations automáticamente
# O hacerlo manualmente:
dotnet ef database update --project MonitorPedidos.Web --connection "Server=(localdb)\\MSSQLLocalDB;Database=MonitorPedidosTestDb;Trusted_Connection=True;"
```

---

## §5 Precondiciones

Antes de ejecutar los integration tests, verificar:

1. **Migrations aplicadas** en `MonitorPedidosTestDb`:
   - Todas las 5 migrations (InitialCreate → AddSimulationSchema)
   - Verificar con: `dotnet ef migrations list --project MonitorPedidos.Web`

2. **Seed de reglas iniciales** presente en `rules`:
   - 4 reglas: Patprimo, SevenSeven, Atmos, Ostu
   - La migration `AddBrandSnapshots` las inserta automáticamente

3. **LocalDB corriendo**:
   ```bash
   sqllocaldb start MSSQLLocalDB
   ```

4. **Usuario admin de test** disponible:
   - El factory hace seed del usuario de test durante `Configure`
   - O crearlo con el endpoint de seed si existe

---

## §6 Limpieza entre Tests

Para garantizar aislamiento entre tests, limpiar datos transaccionales después de cada suite (pero NO el schema ni la seed data).

**Estrategia recomendada: Transacciones que no se confirman**:
```csharp
public class IncidentIntegrationTests : IClassFixture<MonitorPedidosWebApplicationFactory>, IAsyncLifetime
{
    private IDbContextTransaction? _transaction;

    public async Task InitializeAsync()
    {
        // Iniciar transacción antes de cada test
        _transaction = await _db.Database.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        // Hacer rollback después de cada test — los datos no quedan en BD
        if (_transaction != null)
            await _transaction.RollbackAsync();
    }
}
```

**Estrategia alternativa: Truncate selectivo**:
```sql
-- Ejecutar después de cada test suite completa
-- (NO truncar: AspNetUsers, AspNetRoles, rules, rule_conditions — son datos fijos)
TRUNCATE TABLE incidents;
TRUNCATE TABLE brand_snapshots;
TRUNCATE TABLE simulated_orders;
TRUNCATE TABLE simulated_job_statuses;
```

**Tablas que se limpian** (datos transaccionales):
- `incidents`
- `brand_snapshots`
- `simulated_orders`
- `simulated_job_statuses`

**Tablas que NO se limpian** (datos de configuración):
- `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles` (usuarios de test)
- `rules`, `rule_conditions` (seed de `AddBrandSnapshots`)

---

## §7 Restricción: Sin Tests contra APIs Externas

> **REGLA FIJA**: Ningún integration test llama a las APIs reales de Salesforce o Multivende.

Las razones son:
1. Las APIs solo existen en red interna de Eliot — no disponibles en entornos de CI/CD
2. Los datos retornados no son predecibles (afectarían la determinancia de los tests)
3. Las llamadas generarían carga innecesaria en sistemas de producción

**Cómo manejar los clientes externos en integration tests**:
```csharp
// En la WebApplicationFactory, reemplazar clientes reales por fakes/mocks
services.AddScoped<ISalesforceClient, FakeSalesforceClient>();
services.AddScoped<IMultivendeClient, FakeMultivendeClient>();
```

Los `FakeSalesforceClient` y `FakeMultivendeClient` son implementaciones de test que retornan datos configurables sin hacer llamadas HTTP. Su comportamiento se configura por test según el escenario a verificar.

**Pruebas de los clientes HTTP reales** están cubiertas en `unit-test-instructions.md` §2, U4, usando `MockHttpMessageHandler`.
