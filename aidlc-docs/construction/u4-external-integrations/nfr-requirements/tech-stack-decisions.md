# Tech Stack Decisions — U4 External Integrations

**Unidad:** U4 — External Integrations
**Stage:** Construction → NFR Requirements
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Resumen de decisiones

U4 introduce **2 paquetes NuGet nuevos de producción** y **1 paquete de test nuevo**. El resto del stack se hereda de U1–U3 sin cambios.

---

## §2 Paquetes NuGet nuevos — Producción

### Polly + Microsoft.Extensions.Http.Polly

| Campo | Valor |
|-------|-------|
| **Paquete 1** | `Polly` v8.x |
| **Paquete 2** | `Microsoft.Extensions.Http.Polly` v8.x |
| **Proyecto** | `MonitorPedidos.Web` |
| **Propósito** | Política de reintentos para clientes HTTP externos (Salesforce, Multivende). `Polly` es el core; `Microsoft.Extensions.Http.Polly` integra la política con `IHttpClientBuilder` para los clientes tipados |
| **Decisión** | Usar `HandleTransientHttpError()` + `WaitAndRetryAsync(2, ...)`. 401 excluido explícitamente (no es error transitorio) |
| **Alternativa descartada** | `Microsoft.Extensions.Http.Resilience` (Polly 8 abstracto) — más verboso para configurar el callback `onRetry` con captura de `RetryAttempt` |

**Registro en DI (`Program.cs`):**

```csharp
// Salesforce
builder.Services
    .AddHttpClient<ISalesforceClient, SalesforceClient>(c =>
    {
        c.BaseAddress = new Uri(builder.Configuration["Salesforce:BaseUrl"]!);
        c.Timeout = TimeSpan.FromSeconds(10);
    })
    .AddPolicyHandler(ApiRetryPolicy.Create(app.Services.GetRequiredService<ILogger<ApiRetryPolicy>>()));

// Multivende
builder.Services
    .AddHttpClient<IMultivendeClient, MultivendeClient>(c =>
    {
        c.BaseAddress = new Uri(builder.Configuration["Multivende:BaseUrl"]!);
        c.Timeout = TimeSpan.FromSeconds(10);
    })
    .AddPolicyHandler(ApiRetryPolicy.Create(app.Services.GetRequiredService<ILogger<ApiRetryPolicy>>()));
```

---

## §3 Paquetes NuGet nuevos — Tests

### RichardSzalay.MockHttp

| Campo | Valor |
|-------|-------|
| **Paquete** | `RichardSzalay.MockHttp` v6.x |
| **Proyecto** | `MonitorPedidos.UnitTests` |
| **Propósito** | `MockHttpMessageHandler` para interceptar llamadas `HttpClient` en tests sin hacer llamadas HTTP reales. Permite simular secuencias de respuestas (503→503→200, 401, timeout) |
| **Alternativa descartada** | Custom `DelegatingHandler` hecho a mano — más código boilerplate para el mismo resultado |

**Uso en tests:**

```csharp
var mockHttp = new MockHttpMessageHandler();
mockHttp.When("/orders*").Respond(HttpStatusCode.ServiceUnavailable);

var client = mockHttp.ToHttpClient();
client.BaseAddress = new Uri("https://api.salesforce.test/");
```

---

## §4 Stack heredado de U1–U3 (sin cambios)

| Tecnología | Versión | Notas |
|-----------|---------|-------|
| ASP.NET Core 8 | 8.x | Proyecto `MonitorPedidos.Web` |
| Blazor Server | 8.x | UI de `IncidentDetailPage` con sección auto_reintentos |
| EF Core 8 | 8.x | Columna `retry_metadata` (nvarchar(max), nullable) en `incidents` |
| SQL Server MonitorPedidosDb (172.16.0.41) | 2019+ | Base de datos de desarrollo |
| Serilog | 3.x | Nivel Debug para OK, Warning para reintentos, Error para incidentes |
| Moq | 4.x | Tests de checkers (`SalesforceApiChecker`, `MultivendeApiChecker`) |
| xUnit | 2.x | Framework de test para `MonitorPedidos.UnitTests` |

---

## §5 Credenciales — User Secrets (no es paquete, es configuración)

Las credenciales de Salesforce y Multivende se gestionan con **dotnet user-secrets** en desarrollo y **variables de entorno** en demo interna. No se agrega ningún paquete adicional — `Microsoft.Extensions.Configuration.UserSecrets` ya viene incluido en el SDK de ASP.NET Core 8.

| Secreto | Clave de configuración |
|---------|----------------------|
| Salesforce API Key | `Salesforce:ApiKey` |
| Salesforce Base URL | `Salesforce:BaseUrl` |
| Multivende API Key | `Multivende:ApiKey` |
| Multivende Base URL | `Multivende:BaseUrl` |

**Nunca en `appsettings.json` ni en el repositorio** (BR-CRED-01).

---

## §6 Trazabilidad

| Decisión | Paquete | Componentes | NFR |
|----------|---------|------------|-----|
| Polly retry policy | `Polly` + `Microsoft.Extensions.Http.Polly` | ApiRetryPolicy, SalesforceClient, MultivendeClient | NFR-U4-01, NFR-U4-03 |
| MockHttpMessageHandler en tests | `RichardSzalay.MockHttp` | SalesforceClientTests, MultivendeClientTests, ApiRetryPolicyTests | NFR-U4-02, NFR-U4-03 |
| Timeout 10s | Config en `AddHttpClient` | SalesforceClient, MultivendeClient | NFR-U4-01 |
| User Secrets | SDK nativo | SalesforceClient, MultivendeClient | SECURITY (BR-CRED-01/02) |
