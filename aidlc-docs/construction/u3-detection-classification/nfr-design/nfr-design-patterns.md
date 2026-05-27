# NFR Design Patterns — U3 Detection & Classification

**Unidad:** U3 — Detection & Classification
**Stage:** Construction → NFR Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## ADR-U3-01 — Timeout por Checker con Linked CancellationToken

**Flujo:** NFR-U3-01 (confiabilidad del ciclo) → Pregunta (¿cómo cancelar un checker colgado sin afectar los demás ni el shutdown?) → **Patrón Linked CancellationToken + CancelAfter**

| Campo | Detalle |
|-------|---------|
| **Estado** | Aceptado |
| **NFR** | NFR-U3-01 |
| **BR** | BR-DET-03, BR-SCHED-03 |

### Contexto

`MonitoringSchedulerService` ejecuta los 3 checkers en paralelo con `Task.WhenAll`. Si un checker se cuelga (ej. `DbHealthChecker` esperando una BD que nunca responde), el `WhenAll` completo queda bloqueado indefinidamente. Necesitamos un mecanismo que:

1. Cancele solo el checker colgado, no los demás
2. No interfiera con el `stoppingToken` del shutdown de la app
3. Sea configurable sin hardcodear el timeout en el código

### Decisión

Usar `CancellationTokenSource.CreateLinkedTokenSource` + `CancelAfter` dentro de cada `RunCheckAsync`:

```csharp
private async Task RunCheckAsync(ICheckExecutor checker, CancellationToken stoppingToken)
{
    var timeoutMs = _config.GetValue("Monitoring:CheckerTimeoutMs", 30_000);

    using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
    cts.CancelAfter(timeoutMs);

    try
    {
        var result = await checker.ExecuteAsync(cts.Token);
        await ProcessResultAsync(checker, result, stoppingToken);
    }
    catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
    {
        // Timeout del checker — el shutdown no está activo
        _logger.LogError(
            "Checker {Module} timed out after {Ms} ms. Skipping this cycle.",
            checker.Module, timeoutMs);
    }
    catch (OperationCanceledException)
    {
        // Shutdown de la app — propagar silenciosamente
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Checker {Module} failed unexpectedly.", checker.Module);
    }
}
```

**Distinción clave de los dos `catch`:** el primero captura timeout del checker (no es shutdown), el segundo captura el shutdown real. Sin esta distinción, un timeout en shutdown silenciaría el error de cancelación de la app.

### Alternativas consideradas

| Alternativa | Razón de descarte |
|-------------|------------------|
| `Task.WhenAny(checker.ExecuteAsync(ct), Task.Delay(timeout, ct))` | Deja el checker corriendo como tarea huérfana en background. La operación async no se cancela, solo se ignora. Con checkers que hacen queries a BD, esto genera conexiones abiertas acumuladas. |
| Timeout global del `WhenAll` completo | Cancela los 3 checkers aunque solo uno esté colgado. Pierde datos de los checkers que sí terminaron a tiempo. |

### Consecuencias

**Positivas:**
- El timeout cancela realmente la operación async del checker (no solo la ignora)
- Los demás checkers del mismo ciclo no se ven afectados
- El shutdown de la app siempre se propaga correctamente
- El valor de timeout es configurable por ambiente

**Negativas:**
- Cada `RunCheckAsync` instancia un `CancellationTokenSource` — objeto pequeño pero que requiere `Dispose` (resuelto con `using`)
- El `when (!stoppingToken.IsCancellationRequested)` hace el código más verboso

---

## ADR-U3-02 — Extensibilidad de Checkers con IEnumerable\<ICheckExecutor\>

**Flujo:** NFR-U3-02 (mantenibilidad / extensibilidad) → Pregunta (¿cómo agregar checkers en U4 sin modificar el scheduler?) → **Patrón Open/Closed via IEnumerable en DI**

| Campo | Detalle |
|-------|---------|
| **Estado** | Aceptado |
| **NFR** | NFR-U3-02 (extensibilidad), BR-SCHED-02 |
| **BR** | BR-SCHED-01, BR-SCHED-02 |

### Contexto

En Sprint 2, el scheduler ejecuta 3 checkers (M2, M4, M11). En U4 se agregarán `ApiChecker` y `TokenChecker`. Si el scheduler tiene una lista hardcodeada, cada checker nuevo requiere modificar `MonitoringSchedulerService` — violando el principio abierto/cerrado y generando riesgo de regresiones en el código existente.

### Decisión

Registrar todas las implementaciones de `ICheckExecutor` en DI y resolverlas via `IEnumerable<ICheckExecutor>`:

```csharp
// Program.cs — registro (U3)
builder.Services.AddScoped<ICheckExecutor, DbOrderChecker>();
builder.Services.AddScoped<ICheckExecutor, DbHealthChecker>();
builder.Services.AddScoped<ICheckExecutor, JobsChecker>();

// Program.cs — registro adicional en U4 (sin tocar el scheduler)
builder.Services.AddScoped<ICheckExecutor, ApiChecker>();
builder.Services.AddScoped<ICheckExecutor, TokenChecker>();

// MonitoringSchedulerService — resolución
public MonitoringSchedulerService(
    IServiceScopeFactory scopeFactory,
    IConfiguration config,
    ILogger<MonitoringSchedulerService> logger)
{ ... }

// En el loop del timer:
await using var scope = _scopeFactory.CreateAsyncScope();
var checkers = scope.ServiceProvider.GetServices<ICheckExecutor>();
await Task.WhenAll(checkers.Select(c => RunCheckAsync(c, stoppingToken)));
```

**Nota sobre scope:** igual que `IncidentMaintenanceService` en U2, el `BackgroundService` es singleton pero los checkers son scoped. Se usa `IServiceScopeFactory` para crear un scope por tick de timer.

### Alternativas consideradas

| Alternativa | Razón de descarte |
|-------------|------------------|
| Lista hardcodeada en el scheduler | Viola Open/Closed Principle. U4 debe modificar el scheduler para agregar sus checkers, aumentando el riesgo de romper la lógica existente. |
| Archivo de configuración con nombres de clases | Complejidad de reflection innecesaria para MVP. El DI nativo de .NET es la solución idiomática. |

### Consecuencias

**Positivas:**
- U4 agrega checkers sin tocar `MonitoringSchedulerService`
- El orden de ejecución es predecible (orden de registro en DI)
- Testeable: en tests se puede registrar solo los checkers relevantes

**Negativas:**
- `GetServices<ICheckExecutor>()` puede retornar colección vacía si no hay registros — el scheduler loggea un warning y no hace nada (comportamiento seguro)

---

## ADR-U3-03 — Ciclo de Vida del Scheduler con PeriodicTimer

**Flujo:** NFR-U3-01 (confiabilidad) → Pregunta (¿cómo manejar el intervalo sin drift acumulado?) → **Patrón PeriodicTimer + WaitForNextTickAsync**

| Campo | Detalle |
|-------|---------|
| **Estado** | Aceptado |
| **NFR** | NFR-U3-01, BR-SCHED-01 |
| **BR** | BR-DET-01, BR-SCHED-01 |

### Contexto

El scheduler debe ejecutar los checkers cada 5 minutos. El enfoque clásico (`Task.Delay(5min)`) introduce drift: si el ciclo de checkers tarda 2 segundos, el siguiente ciclo empieza a los 5m2s, luego 5m4s, etc. En una operación de 8 horas, el drift acumulado puede ser de varios minutos — el scheduler "se retrasa" gradualmente.

### Decisión

Usar `PeriodicTimer` (disponible desde .NET 6):

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    _logger.LogInformation("MonitoringSchedulerService started. Interval={Min} min.",
        _config.GetValue("Monitoring:CheckerIntervalMinutes", 5));

    using var timer = new PeriodicTimer(
        TimeSpan.FromMinutes(_config.GetValue("Monitoring:CheckerIntervalMinutes", 5)));

    while (await timer.WaitForNextTickAsync(stoppingToken))
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var checkers = scope.ServiceProvider.GetServices<ICheckExecutor>().ToList();

        if (checkers.Count == 0)
        {
            _logger.LogWarning("No ICheckExecutor implementations registered. Skipping cycle.");
            continue;
        }

        await Task.WhenAll(checkers.Select(c => RunCheckAsync(c, stoppingToken)));
    }

    _logger.LogInformation("MonitoringSchedulerService stopped.");
}
```

`WaitForNextTickAsync` retorna `false` cuando `stoppingToken` se cancela — el `while` termina limpiamente sin necesidad de `try-catch` adicional para el shutdown.

### Alternativas consideradas

| Alternativa | Razón de descarte |
|-------------|------------------|
| `while + Task.Delay(5min, stoppingToken)` | Acumula drift. Si los checkers tardan 2s, en 8h el drift puede superar varios minutos. No es crítico para MVP pero es una deuda técnica innecesaria. |
| Quartz.NET (scheduler externo) | Overkill para 3 checkers con un único intervalo. Agrega dependencia externa sin beneficio real en MVP. |

### Consecuencias

**Positivas:**
- Sin drift: el timer mide el tiempo desde el inicio del tick anterior, no desde el fin del ciclo
- `WaitForNextTickAsync(stoppingToken)` cancela limpiamente al hacer shutdown — sin `try-catch` adicional
- API moderna y legible — intención clara en el código

**Negativas:**
- `PeriodicTimer` no está disponible en .NET 5 o anterior — no es un problema en este proyecto (.NET 8)
- El primer tick ocurre inmediatamente al arrancar (no espera el primer intervalo) — comportamiento esperado: los checkers deben correr al arrancar la app

---

## ADR-U3-04 — Tests de MonitoringService con Moq para INotificationService

**Flujo:** NFR-U3-03 (tests) → Pregunta (¿cómo verificar que el servicio de notificación fue invocado correctamente en el flujo completo?) → **Patrón Moq con Verify**

| Campo | Detalle |
|-------|---------|
| **Estado** | Aceptado |
| **NFR** | NFR-U3-03 |
| **Tests** | T-U3-05 |

### Contexto

`MonitoringService.RunCheckAsync` cuando detecta WARN/CRITICAL llama a `INotificationService.BroadcastAlertAsync`. En Sprint 2, `INotificationService` es un stub que solo loggea. En tests, necesitamos verificar que fue invocado con los parámetros correctos sin depender de una implementación real (que llega en U6).

### Decisión

Usar `Moq` con `Mock<INotificationService>` y `Verify`:

```csharp
// T-U3-05 — MonitoringServiceTests.cs
[Fact]
public async Task RunCheckAsync_WarnResult_BroadcastsAlert()
{
    // Arrange
    var mockNotification = new Mock<INotificationService>();
    var mockOrderSource  = new Mock<IOrderSource>();
    mockOrderSource
        .Setup(s => s.GetOrdersInWindowAsync(It.IsAny<DateTimeOffset>(),
                                              It.IsAny<DateTimeOffset>(),
                                              It.IsAny<CancellationToken>()))
        .ReturnsAsync([]); // sin pedidos → Critical

    var checker = new DbOrderChecker(mockOrderSource.Object, _config);
    var service = new MonitoringService(_incidentService, mockNotification.Object, _logger);

    // Act
    await service.RunCheckAsync(checker, CancellationToken.None);

    // Assert
    mockNotification.Verify(
        x => x.BroadcastAlertAsync(It.IsAny<Guid>(), It.IsAny<AlertMessage>(),
                                    It.IsAny<CancellationToken>()),
        Times.Once);
}
```

### Alternativas consideradas

| Alternativa | Razón de descarte |
|-------------|------------------|
| `FakeNotificationService` manual | Clase adicional que solo captura llamadas en una lista. Moq hace exactamente eso con menos código y más expresividad en los `Verify`. |
| Implementación real de `INotificationService` | No existe en Sprint 2 (llega en U6). Usar la implementación real crearía una dependencia de U6 en los tests de U3. |

### Consecuencias

**Positivas:**
- Verificación precisa: `Verify` confirma que fue llamado una vez, con los tipos correctos
- Sin dependencias de U6 en los tests de U3
- `Moq` ya es dependencia del proyecto `MonitorPedidos.UnitTests` (agregado en Activity 2)

**Negativas:**
- Los tests con Moq son frágiles si la firma de `BroadcastAlertAsync` cambia — mitigación: usar `It.IsAny<>` para los parámetros que no son críticos de verificar

---

## §5 Resumen de patrones por NFR

| NFR / BR | Patrón | ADR |
|----------|--------|-----|
| NFR-U3-01, BR-DET-03 | Linked CancellationToken + CancelAfter (timeout aislado por checker) | ADR-U3-01 |
| BR-SCHED-01, BR-SCHED-02 | IEnumerable\<ICheckExecutor\> via DI (extensibilidad open/closed) | ADR-U3-02 |
| BR-DET-01, NFR-U3-01 | PeriodicTimer + WaitForNextTickAsync (sin drift, shutdown limpio) | ADR-U3-03 |
| NFR-U3-03, T-U3-05 | Moq + Verify para INotificationService (sin dependencias de U6) | ADR-U3-04 |
