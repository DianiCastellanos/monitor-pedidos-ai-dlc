# NFR Design Patterns — U7 Simulation & Red-Teaming

**Unidad:** U7 — Simulation & Red-Teaming
**Stage:** Construction → NFR Design
**Fecha:** 2026-05-24
**Versión:** 1.0

---

## ADR-U7-01 — `IServiceScopeFactory` en `OrdersSimulatorService`

**Decisión:** `OrdersSimulatorService` (Singleton vía `AddHostedService`) accede a `ISimulatedOrderRepository` (Scoped) mediante `IServiceScopeFactory`, creando un scope por tick.

**Problema:** `AddHostedService` registra el servicio como Singleton. `ISimulatedOrderRepository` depende de `AppDbContext` que es Scoped. Inyectar un Scoped en un Singleton directamente causa `InvalidOperationException` en startup.

**Solución:**

```csharp
public sealed class OrdersSimulatorService : BackgroundService
{
    private readonly IServiceScopeFactory     _scopeFactory;
    private readonly IOptions<SimulationOptions> _options;
    private readonly ILogger<OrdersSimulatorService> _logger;

    private static readonly string[] Sources = ["Salesforce", "Multivende"];
    private static readonly string[] Sites   = ["Patprimo", "SevenSeven", "Atmos", "Ostu"];

    public OrdersSimulatorService(
        IServiceScopeFactory scopeFactory,
        IOptions<SimulationOptions> options,
        ILogger<OrdersSimulatorService> logger)
    {
        _scopeFactory = scopeFactory;
        _options      = options;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogInformation("Simulador desactivado (Simulation:Enabled=false)");
            return;
        }

        var timer = new PeriodicTimer(
            TimeSpan.FromMinutes(_options.Value.InsertIntervalMinutes));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var repo = scope.ServiceProvider
                    .GetRequiredService<ISimulatedOrderRepository>();
                await InsertTickAsync(repo, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en tick del simulador — se reintenta en el próximo ciclo");
            }
        }
    }
    // ...
}
```

**Rationale:** Consistente con ADR-U5-01 (`DbOrderChecker`), ADR-U6 (`BrandMonitorChecker`). Patrón único en el proyecto para Singleton + Scoped dependency.

**Componentes afectados:** `OrdersSimulatorService`

---

## ADR-U7-02 — Verificación Moq con `Times.Never` e `It.Is<>` para tests del simulador

**Decisión:** Los tests de `OrdersSimulatorService` usan `Mock<ISimulatedOrderRepository>` con predicados `It.Is<>` para inspeccionar los objetos insertados sin implementar un fake manual.

**Problema:** El simulador construye objetos `SimulatedOrder` internamente — no los recibe por parámetro. Para verificar que los objetos tienen las propiedades correctas (`IsFailure`, `Status`) se necesita inspeccionar el argumento pasado a `InsertRangeAsync`.

**Solución:**

```csharp
// Test 1: NoOrdersMode=true → InsertRangeAsync nunca invocado
var mockRepo = new Mock<ISimulatedOrderRepository>();
var opts = Options.Create(new SimulationOptions { Enabled=true, NoOrdersMode=true });
var svc = new OrdersSimulatorService(_scopeFactory, opts, _logger);

await svc.RunOneTick(mockRepo.Object, CancellationToken.None); // método interno de test

mockRepo.Verify(
    r => r.InsertRangeAsync(It.IsAny<IEnumerable<SimulatedOrder>>(), It.IsAny<CancellationToken>()),
    Times.Never());

// Test 2: FailureProbability=1.0 → todos IsFailure=true
var opts2 = Options.Create(new SimulationOptions { Enabled=true, FailureProbability=1.0 });
// ...
mockRepo.Verify(
    r => r.InsertRangeAsync(
        It.Is<IEnumerable<SimulatedOrder>>(orders => orders.All(o => o.IsFailure)),
        It.IsAny<CancellationToken>()),
    Times.Once());

// Test 3: FailureProbability=0.0 → todos IsFailure=false
mockRepo.Verify(
    r => r.InsertRangeAsync(
        It.Is<IEnumerable<SimulatedOrder>>(orders => orders.All(o => !o.IsFailure)),
        It.IsAny<CancellationToken>()),
    Times.Once());
```

**Nota de testabilidad:** Para hacer testeable la lógica de un tick sin invocar el timer, `OrdersSimulatorService` expone un método `internal async Task RunOneTickAsync(ISimulatedOrderRepository repo, CancellationToken ct)`. El proyecto de tests accede a él mediante `InternalsVisibleTo`.

**Componentes afectados:** `OrdersSimulatorServiceTests`, `OrdersSimulatorService`

---

## ADR-U7-03 — `try/catch` sin rethrow en el loop del simulador

**Decisión:** Excepciones en el cuerpo del tick se capturan con `try/catch`, se loguean y el loop continúa. No se relanza la excepción.

**Problema:** Un `BackgroundService` en .NET 8 cuyo `ExecuteAsync` lanza una excepción no controlada hace que el host loguee un error crítico. En algunos escenarios (especialmente RT3 con SQL Server apagado), `InsertRangeAsync` lanzará `SqlException`. Si se propaga, el simulador muere y no se reinicia sin reiniciar la app.

**Solución:**

```csharp
while (await timer.WaitForNextTickAsync(stoppingToken))
{
    try
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ISimulatedOrderRepository>();
        await InsertTickAsync(repo, stoppingToken);
    }
    catch (OperationCanceledException)
    {
        throw;  // no suprimir CancellationToken — permite shutdown limpio
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error en tick del simulador — reintento en próximo ciclo");
        // continúa el loop
    }
}
```

**Rationale:** `OperationCanceledException` se re-lanza para permitir el shutdown limpio de la aplicación. Cualquier otra excepción (SQL, timeout) se loguea y el loop continúa — el simulador es auxiliar, no crítico.

**Componentes afectados:** `OrdersSimulatorService`

---

## ADR-U7-04 — `IOptions<SimulationOptions>` con `Configure<T>` en Program.cs

**Decisión:** `SimulationOptions` se registra con el sistema de Options de .NET para soportar appsettings.json y variables de entorno con doble guión bajo.

**Solución en Program.cs:**

```csharp
// === U7: Simulation options ===
builder.Services.Configure<SimulationOptions>(
    builder.Configuration.GetSection(SimulationOptions.Section));  // "Simulation"

// === U7: Simulator (BackgroundService) ===
builder.Services.AddHostedService<OrdersSimulatorService>();
```

**Soporte de variables de entorno:**

```powershell
# Equivalentes a appsettings.json via variables de entorno
$env:Simulation__Enabled            = "true"
$env:Simulation__NoOrdersMode       = "true"    # RT1
$env:Simulation__FailureProbability = "1.0"     # RT5
```

**Rationale:** El sistema de Options de .NET transforma automáticamente `Simulation__NoOrdersMode` (env var) en `SimulationOptions.NoOrdersMode` (propiedad). Sin código adicional de parsing. Consistente con cómo se leen `Monitoring:CheckerIntervalMinutes` y `Logging:MaxExportLines` en el resto del proyecto.

**Componentes afectados:** `Program.cs`, `OrdersSimulatorService`, `SimulationOptions`

---

## §5 Resumen de ADRs U7

| ADR | Patrón | Componentes afectados |
|-----|--------|----------------------|
| ADR-U7-01 | `IServiceScopeFactory` — scope por tick | `OrdersSimulatorService` |
| ADR-U7-02 | Moq `Times.Never` + `It.Is<>` predicados + `InternalsVisibleTo` | `OrdersSimulatorServiceTests` |
| ADR-U7-03 | `try/catch` sin rethrow (excepto `OperationCanceledException`) | `OrdersSimulatorService` |
| ADR-U7-04 | `Configure<SimulationOptions>` + `IOptions<T>` | `Program.cs`, `OrdersSimulatorService` |
