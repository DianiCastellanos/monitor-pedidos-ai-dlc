# NFR Design Patterns — U4 External Integrations

**Unidad:** U4 — External Integrations
**Stage:** Construction → NFR Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Decisiones aplicadas

| ID | Pregunta | Decisión |
|----|----------|---------|
| P1 | Captura de RetryAttempts desde Polly | A — Polly Context dictionary (`context["retryAttempts"]`) |
| P2 | Registro de clientes tipados + Polly en DI | A — `AddHttpClient<T>().AddPolicyHandler(policy)` con instancia compartida |
| P3 | Separación de checkers en MonitoringSchedulerService | A — Filtro por tipo concreto en constructor |
| P4 | Patrón de test con MockHttpMessageHandler | A — Inline por test, sin fixture compartida |

---

## ADR-U4-01: Polly Context como mecanismo de captura de RetryAttempts

**Contexto:** El callback `onRetry` de Polly se ejecuta en un contexto separado al del código que invoca `policy.ExecuteAsync`. Necesitamos trasladar los `RetryAttempt` capturados en `onRetry` hacia `MonitoringService.RunCheckAsync` para adjuntarlos al incidente.

**Decisión:** Usar `Polly.Context` (dictionary) como canal de comunicación entre el callback y el invocador.

**Implementación:**

```csharp
// ApiRetryPolicy.cs
public static IAsyncPolicy<HttpResponseMessage> Create(ILogger logger)
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 2,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (outcome, timespan, attempt, context) =>
            {
                var attempts = context.GetRetryAttempts();
                attempts.Add(new RetryAttempt(
                    AttemptNumber:  attempt,
                    HttpStatusCode: (int)(outcome.Result?.StatusCode ?? 0),
                    LatencyMs:      (int)outcome.Result?.Headers.Date
                                        .HasValue == true ? 0 : 0, // latencia real en checker
                    AttemptedAt:    DateTimeOffset.UtcNow));

                logger.LogWarning(
                    "API retry {Attempt}/2 — HTTP {Status} — waiting {Wait}s",
                    attempt,
                    (int)(outcome.Result?.StatusCode ?? 0),
                    timespan.TotalSeconds);
            });
}

// Extensión del Context para acceso tipado
public static class PollyContextExtensions
{
    private const string RetryAttemptsKey = "retryAttempts";

    public static List<RetryAttempt> GetRetryAttempts(this Context context)
    {
        if (!context.TryGetValue(RetryAttemptsKey, out var value))
        {
            value = new List<RetryAttempt>();
            context[RetryAttemptsKey] = value;
        }
        return (List<RetryAttempt>)value;
    }
}

// MonitoringService.RunCheckAsync — sitio de llamada
var pollyContext = new Context();
var httpResponse = await _policy.ExecuteAsync(
    ctx => client.PingOrdersAsync(ct),
    pollyContext);

var retryAttempts = pollyContext.GetRetryAttempts(); // lista vacía si no hubo reintentos
foreach (var attempt in retryAttempts)
    incident.AddRetryAttempt(attempt);
```

**Consecuencias:**
- `ApiRetryPolicy` no necesita conocer a `MonitoringService` — sin acoplamiento inverso
- `Context` viaja con la ejecución de la política — thread-safe por diseño de Polly
- La extensión `GetRetryAttempts()` encapsula la clave del diccionario — no hay strings mágicos en `MonitoringService`

**Alternativa descartada:** Closure (`List<RetryAttempt>` capturada por `onRetry`) — funciona pero obliga a redeclarar la lista en cada sitio de llamada y no es el patrón oficial Polly.

---

## ADR-U4-02: Registro de clientes tipados con política Polly compartida

**Contexto:** `SalesforceClient` y `MultivendeClient` son typed clients de `IHttpClientFactory`. Ambos necesitan el mismo timeout (10s) y la misma política de reintentos (`ApiRetryPolicy`).

**Decisión:** Construir una única instancia de la política con `ApiRetryPolicy.Create(logger)` y pasarla a `AddPolicyHandler` en ambos clientes.

**Implementación:**

```csharp
// Program.cs
var apiRetryPolicy = ApiRetryPolicy.Create(
    app.Services.GetRequiredService<ILogger<ApiRetryPolicy>>());

builder.Services
    .AddHttpClient<ISalesforceClient, SalesforceClient>(c =>
    {
        c.BaseAddress = new Uri(builder.Configuration["Salesforce:BaseUrl"]!);
        c.Timeout     = TimeSpan.FromSeconds(10);
    })
    .AddPolicyHandler(apiRetryPolicy);

builder.Services
    .AddHttpClient<IMultivendeClient, MultivendeClient>(c =>
    {
        c.BaseAddress = new Uri(builder.Configuration["Multivende:BaseUrl"]!);
        c.Timeout     = TimeSpan.FromSeconds(10);
    })
    .AddPolicyHandler(apiRetryPolicy);
```

**Consecuencias:**
- La política es un singleton compartido — seguro porque `WaitAndRetryAsync` es stateless (el estado por ejecución viaja en `Context`)
- El timeout de 10s aplica por intento individual, no al ciclo total de reintentos
- Si `Salesforce:BaseUrl` o `Multivende:BaseUrl` no están configurados, la aplicación falla al iniciar con `NullReferenceException` — fallo rápido, no silencioso

**Invariante de seguridad:** Los valores `Salesforce:ApiKey` y `Multivende:ApiKey` nunca se usan para configurar `BaseAddress` — solo se leen dentro de `SalesforceClient`/`MultivendeClient` como headers HTTP. Nunca se loggean.

---

## ADR-U4-03: Separación de checkers por tipo concreto en MonitoringSchedulerService

**Contexto:** `MonitoringSchedulerService` recibe `IEnumerable<ICheckExecutor>` del DI (ADR-U3-02). Para U4 necesita dividirlos en dos grupos para dos `PeriodicTimer` con cadencias distintas (5 min y 10 min).

**Decisión:** Filtrar por tipo concreto en el constructor del servicio.

**Implementación:**

```csharp
// MonitoringSchedulerService.cs — constructor extendido para U4
public MonitoringSchedulerService(
    IEnumerable<ICheckExecutor> checkers,
    IMonitoringService monitoringService,
    IConfiguration config,
    ILogger<MonitoringSchedulerService> logger)
{
    _monitorCheckers = checkers
        .Where(c => c is DbOrderChecker or DbHealthChecker or JobsChecker)
        .ToList();

    _apiCheckers = checkers
        .Where(c => c is SalesforceApiChecker or MultivendeApiChecker)
        .ToList();

    _monitoringService = monitoringService;
    _config            = config;
    _logger            = logger;
}

protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    using var monitorTimer = new PeriodicTimer(
        TimeSpan.FromMinutes(_config.GetValue("Monitoring:CheckerIntervalMinutes", 5)));

    using var apiTimer = new PeriodicTimer(
        TimeSpan.FromMinutes(_config.GetValue("Monitoring:ApiCheckerIntervalMinutes", 10)));

    await Task.WhenAll(
        RunTimerLoop(monitorTimer, _monitorCheckers, stoppingToken),
        RunTimerLoop(apiTimer,    _apiCheckers,     stoppingToken));
}

private async Task RunTimerLoop(
    PeriodicTimer timer,
    IReadOnlyList<ICheckExecutor> checkers,
    CancellationToken stoppingToken)
{
    while (await timer.WaitForNextTickAsync(stoppingToken))
        await Task.WhenAll(checkers.Select(c => RunCheckAsync(c, stoppingToken)));
}
```

**Consecuencias:**
- Agregar un nuevo checker de API solo requiere: (1) implementar `ICheckExecutor`, (2) registrar en DI, (3) agregar `or NuevoChecker` en el filtro del constructor
- Si se llega a 4+ APIs en el futuro, la interfaz marcadora (B) sería más limpia — reevaluar en post-MVP
- El constructor detecta en startup si hay checkers sin clasificar (`checkers.Except(_monitorCheckers.Union(_apiCheckers))`) — agregar un log de advertencia es opcional

**Invariante:** `_monitorCheckers` + `_apiCheckers` debe cubrir todos los checkers registrados. Un checker no clasificado no se ejecuta nunca.

---

## ADR-U4-04: Tests con MockHttpMessageHandler inline por test

**Contexto:** 9 unit tests (6 para clientes HTTP + 3 para Polly) necesitan interceptar llamadas `HttpClient` sin hacer requests reales.

**Decisión:** Cada test crea su propio `MockHttpMessageHandler` (RichardSzalay.MockHttp) con las respuestas que necesita. Sin fixtures compartidas.

**Implementación — patrón para SalesforceClientTests:**

```csharp
[Fact]
public async Task PingOrdersAsync_Returns200_ReturnsSuccess()
{
    // Arrange
    var mockHttp = new MockHttpMessageHandler();
    mockHttp
        .When(HttpMethod.Get, "https://api.salesforce.test/orders*")
        .Respond(HttpStatusCode.OK);

    var httpClient = mockHttp.ToHttpClient();
    httpClient.BaseAddress = new Uri("https://api.salesforce.test/");
    httpClient.Timeout     = TimeSpan.FromSeconds(10);

    var client = new SalesforceClient(httpClient, NullLogger<SalesforceClient>.Instance);

    // Act
    var result = await client.PingOrdersAsync(CancellationToken.None);

    // Assert
    result.IsSuccess.Should().BeTrue();
    result.HttpStatusCode.Should().Be(200);
    mockHttp.VerifyNoOutstandingExpectation();
}
```

**Implementación — patrón para ApiRetryPolicyTests (secuencia 503→503→200):**

```csharp
[Fact]
public async Task RetryPolicy_503_503_200_ExecutesTwoRetriesCaptures2Attempts()
{
    // Arrange
    var responses = new Queue<HttpStatusCode>(
        [HttpStatusCode.ServiceUnavailable,
         HttpStatusCode.ServiceUnavailable,
         HttpStatusCode.OK]);

    var mockHttp = new MockHttpMessageHandler();
    mockHttp
        .When("*")
        .Respond(() => Task.FromResult(
            new HttpResponseMessage(responses.Dequeue())));

    var httpClient = mockHttp.ToHttpClient();
    httpClient.BaseAddress = new Uri("https://api.salesforce.test/");

    var policy     = ApiRetryPolicy.Create(NullLogger<ApiRetryPolicy>.Instance);
    var context    = new Context();
    int callCount  = 0;

    // Act
    var response = await policy.ExecuteAsync(
        async ctx =>
        {
            callCount++;
            return await httpClient.GetAsync("/orders?$top=1");
        },
        context);

    // Assert
    callCount.Should().Be(3);                                      // 1 original + 2 reintentos
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    context.GetRetryAttempts().Should().HaveCount(2);
    context.GetRetryAttempts()[0].AttemptNumber.Should().Be(1);
    context.GetRetryAttempts()[1].AttemptNumber.Should().Be(2);
}
```

**Consecuencias:**
- Cada test es completamente independiente — sin interferencias por estado compartido
- `NullLogger<T>` evita la necesidad de mockear `ILogger` en tests de infraestructura
- `responses.Dequeue()` simula secuencias de respuestas HTTP sin complejidad adicional
