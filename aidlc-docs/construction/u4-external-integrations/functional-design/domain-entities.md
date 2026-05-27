# Domain Entities — U4 External Integrations

**Unidad:** U4 — External Integrations
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-23
**Versión:** 1.0
**Fuentes:** `unit-of-work.md` (U4), `application-design.md`, `components.md`, `component-methods.md`

---

## §1 Bounded Context — ApiIntegration

U4 extiende el bounded context **Monitoring** de U3 con la capacidad de verificar APIs externas (Salesforce, Multivende) aplicando política de reintentos limitados. No introduce un bounded context nuevo — agrega componentes al dominio de monitoreo existente.

```
MonitorPedidos.Domain/Shared/
+-- ModuleId                           (enum — extendido con SalesforceApi, MultivendeApi)

MonitorPedidos.Domain/Monitoring/
+-- RetryAttempt                       (value object — un intento individual — §3)
+-- ApiPingResult                      (value object — resultado de llamada HTTP — §4)
+-- ISalesforceClient                  (contrato de cliente Salesforce — §5)
+-- IMultivendeClient                  (contrato de cliente Multivende — §6)

MonitorPedidos.Domain/Incidents/
+-- Incident                           (aggregate root — MODIFICADO: agrega RetryMetadata — §2)

MonitorPedidos.Web/Features/ApiChecks/
+-- SalesforceApiChecker               (ICheckExecutor — M3a — §7)
+-- MultivendeApiChecker               (ICheckExecutor — M3b — §8)
+-- SalesforceClient                   (ISalesforceClient — cliente tipado HTTP — §9)
+-- MultivendeClient                   (IMultivendeClient — cliente tipado HTTP — §10)
+-- ApiRetryPolicy                     (Polly policy factory — §11)

MonitorPedidos.Web/Features/Monitoring/
+-- AlertTemplateRenderer              (MODIFICADO: agrega plantillas Api y Token — §12)
+-- CauseClassifier                    (MODIFICADO: agrega SalesforceApiChecker, MultivendeApiChecker — §13)
+-- MonitoringSchedulerService         (MODIFICADO: agrega segundo PeriodicTimer 10 min — §14)
```

---

## §2 Aggregate Root: Incident — Modificación U4

El aggregate `Incident` (definido en U2) se extiende con soporte para registrar los intentos de reintento automático.

### Nueva propiedad

```csharp
// MonitorPedidos.Domain/Incidents/Incident.cs — agregar a la clase existente

// Almacena la lista de intentos serializada como JSON
// Nulo si el incidente no es de tipo API o si no hubo reintentos
public string? RetryMetadataJson { get; private set; }
```

### Nuevos métodos de dominio

```csharp
/// <summary>
/// Agrega un intento de reintento al incidente.
/// Solo válido para incidentes con Cause == Api (no para Token — 401 no reintenta).
/// </summary>
public void AddRetryAttempt(RetryAttempt attempt)
{
    // INV-U4-01: solo se agregan reintentos si la causa es Api
    if (Cause != CauseCategory.Api)
        throw new DomainException($"RetryAttempts solo aplican a incidentes Api. Cause actual: {Cause}");

    var attempts = GetRetryAttempts().ToList();
    attempts.Add(attempt);
    RetryMetadataJson = JsonSerializer.Serialize(attempts);
}

/// <summary>
/// Retorna los intentos de reintento deserializados.
/// Retorna lista vacía si no hay metadata.
/// </summary>
public IReadOnlyList<RetryAttempt> GetRetryAttempts()
{
    if (string.IsNullOrEmpty(RetryMetadataJson))
        return Array.Empty<RetryAttempt>();
    return JsonSerializer.Deserialize<List<RetryAttempt>>(RetryMetadataJson) ?? [];
}
```

### Invariante nueva

| ID | Invariante |
|----|-----------|
| INV-U4-01 | `AddRetryAttempt` solo puede llamarse en incidentes con `Cause == CauseCategory.Api`. Los 401 (Token) no generan reintentos. |
| INV-U4-02 | `RetryMetadataJson` no puede contener más de 2 intentos (máximo definido en la política Polly). |

### Mapeo EF Core (nuevo en migration U4)

```csharp
// IncidentConfiguration.cs — agregar en Configure()
entity.Property(i => i.RetryMetadataJson)
      .HasColumnName("retry_metadata")
      .HasColumnType("nvarchar(max)")
      .IsRequired(false);
```

---

## §3 Value Object: RetryAttempt

Representa un único intento de reintento automático realizado por la política Polly.

```csharp
// MonitorPedidos.Domain/Monitoring/RetryAttempt.cs
namespace MonitorPedidos.Domain.Monitoring;

public sealed record RetryAttempt(
    int             AttemptNumber,      // 1 o 2 (máximo 2 reintentos)
    int             HttpStatusCode,     // ej. 500, 503, 0 (timeout)
    int             LatencyMs,          // duración del intento en ms
    DateTimeOffset  AttemptedAt);       // timestamp UTC del intento
```

**Cuándo se crea:** dentro del callback `onRetry` de la política Polly, antes de cada reintento.

---

## §4 Value Object: ApiPingResult

Resultado que retorna `ISalesforceClient.PingOrdersAsync` / `IMultivendeClient.PingOrdersAsync`.

```csharp
// MonitorPedidos.Domain/Monitoring/ApiPingResult.cs
namespace MonitorPedidos.Domain.Monitoring;

public sealed record ApiPingResult(
    int             HttpStatusCode,     // código HTTP de la respuesta
    int             LatencyMs,          // tiempo de respuesta en ms
    bool            IsSuccess,          // 200..299
    bool            IsUnauthorized,     // 401 — token expirado/inválido
    string          Details)            // mensaje para logs (no para UI)
{
    public static ApiPingResult Success(int latencyMs)
        => new(200, latencyMs, true, false, "OK");

    public static ApiPingResult Unauthorized()
        => new(401, 0, false, true, "HTTP 401 — token inválido o expirado");

    public static ApiPingResult ServerError(int httpStatus, int latencyMs)
        => new(httpStatus, latencyMs, false, false, $"HTTP {httpStatus}");

    public static ApiPingResult Timeout()
        => new(0, 0, false, false, "Timeout — API no respondió en el tiempo límite");
}
```

---

## §5 Interfaz: ISalesforceClient

```csharp
// MonitorPedidos.Domain/Monitoring/ISalesforceClient.cs
namespace MonitorPedidos.Domain.Monitoring;

public interface ISalesforceClient
{
    /// <summary>
    /// Verifica conectividad con Salesforce en modo solo lectura.
    /// Nunca ejecuta POST / PUT / PATCH / DELETE.
    /// </summary>
    Task<ApiPingResult> PingOrdersAsync(CancellationToken ct = default);
}
```

---

## §6 Interfaz: IMultivendeClient

```csharp
// MonitorPedidos.Domain/Monitoring/IMultivendeClient.cs
namespace MonitorPedidos.Domain.Monitoring;

public interface IMultivendeClient
{
    /// <summary>
    /// Verifica conectividad con Multivende en modo solo lectura.
    /// Nunca ejecuta POST / PUT / PATCH / DELETE.
    /// </summary>
    Task<ApiPingResult> PingOrdersAsync(CancellationToken ct = default);
}
```

---

## §7 Checker: SalesforceApiChecker (M3a)

```csharp
// MonitorPedidos.Web/Features/ApiChecks/SalesforceApiChecker.cs
public sealed class SalesforceApiChecker : ICheckExecutor
{
    public ModuleId Module => ModuleId.SalesforceApi;

    private readonly ISalesforceClient        _client;
    private readonly IIncidentService         _incidentService;
    private readonly ILogger<SalesforceApiChecker> _logger;

    public async Task<CheckResult> ExecuteAsync(CancellationToken ct)
    {
        // La política Polly está configurada en el HttpClient (no aquí)
        // Este checker solo llama al cliente y mapea el resultado
        var ping = await _client.PingOrdersAsync(ct);

        return ping switch
        {
            { IsSuccess: true }       => CheckResult.Ok($"Salesforce OK — {ping.LatencyMs} ms"),
            { IsUnauthorized: true }  => CheckResult.Critical("Salesforce HTTP 401 — token inválido"),
            _                         => CheckResult.Critical($"Salesforce {ping.Details}")
        };
    }
}
```

---

## §8 Checker: MultivendeApiChecker (M3b)

```csharp
// MonitorPedidos.Web/Features/ApiChecks/MultivendeApiChecker.cs
public sealed class MultivendeApiChecker : ICheckExecutor
{
    public ModuleId Module => ModuleId.MultivendeApi;

    private readonly IMultivendeClient        _client;
    private readonly ILogger<MultivendeApiChecker> _logger;

    public async Task<CheckResult> ExecuteAsync(CancellationToken ct)
    {
        var ping = await _client.PingOrdersAsync(ct);

        return ping switch
        {
            { IsSuccess: true }       => CheckResult.Ok($"Multivende OK — {ping.LatencyMs} ms"),
            { IsUnauthorized: true }  => CheckResult.Critical("Multivende HTTP 401 — token inválido"),
            _                         => CheckResult.Critical($"Multivende {ping.Details}")
        };
    }
}
```

---

## §9 Cliente: SalesforceClient

```csharp
// MonitorPedidos.Web/Features/ApiChecks/SalesforceClient.cs
public sealed class SalesforceClient : ISalesforceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<SalesforceClient> _logger;

    public SalesforceClient(HttpClient http, ILogger<SalesforceClient> logger)
    {
        _http   = http;
        _logger = logger;
    }

    public async Task<ApiPingResult> PingOrdersAsync(CancellationToken ct)
    {
        // Solo GET — nunca POST/PUT/PATCH/DELETE (BR-API-03)
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await _http.GetAsync("orders?$top=1", ct);
            sw.Stop();

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return ApiPingResult.Unauthorized();

            return response.IsSuccessStatusCode
                ? ApiPingResult.Success((int)sw.ElapsedMilliseconds)
                : ApiPingResult.ServerError((int)response.StatusCode, (int)sw.ElapsedMilliseconds);
        }
        catch (TaskCanceledException)
        {
            return ApiPingResult.Timeout();
        }
    }
}
```

> **La política Polly de reintentos** se configura en `Program.cs` al registrar el `HttpClient`, no dentro de `SalesforceClient`. Esto mantiene la lógica de retry separada de la lógica de negocio.

---

## §10 Cliente: MultivendeClient

Misma estructura que `SalesforceClient` con la URL base de Multivende y su esquema de autenticación.

```csharp
public sealed class MultivendeClient : IMultivendeClient
{
    private readonly HttpClient _http;

    public MultivendeClient(HttpClient http) => _http = http;

    public async Task<ApiPingResult> PingOrdersAsync(CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await _http.GetAsync("v1/orders?limit=1", ct);
            sw.Stop();

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return ApiPingResult.Unauthorized();

            return response.IsSuccessStatusCode
                ? ApiPingResult.Success((int)sw.ElapsedMilliseconds)
                : ApiPingResult.ServerError((int)response.StatusCode, (int)sw.ElapsedMilliseconds);
        }
        catch (TaskCanceledException) { return ApiPingResult.Timeout(); }
    }
}
```

---

## §11 Polly: ApiRetryPolicy

Factory que crea la política de reintentos para los HttpClients de API.

```csharp
// MonitorPedidos.Web/Features/ApiChecks/ApiRetryPolicy.cs
public static class ApiRetryPolicy
{
    /// <summary>
    /// Máximo 2 reintentos. Solo ante 5xx o timeout. Nunca ante 401.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> Create(
        IServiceProvider sp, string moduleName)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()                // 5xx + HttpRequestException (timeout)
            .OrResult(r => (int)r.StatusCode >= 500)
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)), // 2s, 4s
                onRetry: (outcome, timespan, attempt, context) =>
                {
                    // Capturar el intento para RetryMetadata del incidente
                    var attempt_record = new RetryAttempt(
                        AttemptNumber:  attempt,
                        HttpStatusCode: (int?)outcome.Result?.StatusCode ?? 0,
                        LatencyMs:      0, // se mide en el cliente
                        AttemptedAt:    DateTimeOffset.UtcNow);

                    // Guardar en contexto Polly para que el checker lo recupere
                    if (!context.ContainsKey("retryAttempts"))
                        context["retryAttempts"] = new List<RetryAttempt>();
                    ((List<RetryAttempt>)context["retryAttempts"]).Add(attempt_record);

                    var logger = sp.GetRequiredService<ILogger<ApiRetryPolicy>>();
                    logger.LogWarning(
                        "API {Module} retry {Attempt}/2 — HTTP {Status} — waiting {Delay}s",
                        moduleName, attempt, (int?)outcome.Result?.StatusCode ?? 0,
                        timespan.TotalSeconds);
                });
    }
}
```

---

## §12 AlertTemplateRenderer — Plantillas nuevas (Api y Token)

Extensión del diccionario `_templates` de U3 con las 2 nuevas combinaciones:

```csharp
// Agregar en AlertTemplateRenderer._templates (U3):

[(CauseCategory.Api, Severity.Critical)] = ctx => new AlertMessage(
    QuePaso:        $"API externa no responde en módulo {ctx.Module}.",
    Cuando:         ctx.DetectedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
    Donde:          $"Módulo {ctx.Module} — API Externa",
    SeveridadTexto: "CRÍTICO",
    CausaProbable:  "La API externa no responde o retorna error de servidor (5xx). Se realizaron 2 reintentos automáticos.",
    AccionSugerida: "Verificar estado de la API. Revisar logs de reintentos en el detalle del incidente."),

[(CauseCategory.Token, Severity.Critical)] = ctx => new AlertMessage(
    QuePaso:        $"Token de autenticación inválido o expirado en módulo {ctx.Module}.",
    Cuando:         ctx.DetectedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
    Donde:          $"Módulo {ctx.Module} — API Externa",
    SeveridadTexto: "CRÍTICO",
    CausaProbable:  "La API retornó HTTP 401. El token de acceso ha expirado o fue revocado.",
    AccionSugerida: "Renovar token de acceso manualmente según procedimiento SOP-001."),
```

> **US-10 criterio 2:** el campo `AccionSugerida` para causa `Token` incluye literalmente `SOP-001`.

---

## §13 CauseClassifier — Extensión para M3a y M3b

```csharp
// Agregar en CauseClassifier._map (U3):
[typeof(SalesforceApiChecker)]  = CauseCategory.Api,
[typeof(MultivendeApiChecker)]  = CauseCategory.Api,
```

**Nota:** cuando el resultado es 401, el checker ya retorna un `CheckResult.Critical` con el detalle "token inválido". El `MonitoringService` detecta este caso por el contenido del `Details` y asigna `CauseCategory.Token` en lugar de `Api`:

```csharp
// En MonitoringService.RunCheckAsync — lógica adicional para U4
var cause = result.Details.Contains("401")
    ? CauseCategory.Token
    : CauseClassifier.Classify(checker);
```

---

## §14 ModuleId — Extensión para U4

```csharp
// MonitorPedidos.Domain/Shared/ModuleId.cs — agregar valores
public enum ModuleId
{
    // U3
    DbOrders,
    DbHealth,
    Jobs,
    // U4 — nuevos
    SalesforceApi,
    MultivendeApi
}
```

---

## §15 Trazabilidad de entidades

| Componente | Story | RF | Decisión |
|-----------|-------|----|---------| 
| `Incident.RetryMetadataJson` + `AddRetryAttempt` | US-16 | RF-06 | P2=A (JSON column) |
| `RetryAttempt` value object | US-16 | RF-06 | P2=A |
| `ApiPingResult` value object | US-10, US-16 | RF-03, RF-06, RF-07 | P1=B |
| `ISalesforceClient` / `SalesforceClient` | US-10 | RF-03, RF-07 | P1=B |
| `IMultivendeClient` / `MultivendeClient` | US-10 | RF-03, RF-07 | P1=B |
| `SalesforceApiChecker` / `MultivendeApiChecker` | US-10 | RF-03 | P1=B |
| `ApiRetryPolicy` (Polly) | US-10, US-16 | RF-06 | P3=A (2° timer) |
| `AlertTemplateRenderer` — Api + Token templates | US-10 | RF-11, RF-13 | P5=A |
| `CauseClassifier` — extensión 401 → Token | US-10 | RF-07, RF-08 | — |
| `ModuleId.SalesforceApi` / `.MultivendeApi` | US-10 | RF-03 | P1=B |
