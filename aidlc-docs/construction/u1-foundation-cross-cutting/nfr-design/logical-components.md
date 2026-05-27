# Logical Components — U1 Foundation & Cross-Cutting

**Unidad:** U1 — Foundation & Cross-Cutting
**Stage:** Construction → NFR Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Vista general de componentes logicos de U1

```
MonitorPedidos.Web/
+-- [MIDDLEWARE PIPELINE]
|   +-- SecurityHeadersMiddleware        (patron §1)
|   +-- CookieAuthenticationMiddleware   (ASP.NET Core built-in)
|   +-- AuthorizationMiddleware          (ASP.NET Core built-in + FallbackPolicy)
|   +-- GlobalExceptionHandler           (patron §3, IExceptionHandler)
|
+-- [IDENTITY SELECTION]
|   +-- IdentitySelectionPage            (Razor Page, ruta: /Identity/Select)
|   +-- LogoutEndpoint                   (Razor Page POST-only, /Identity/Logout)
|   +-- PredefinedIdentities             (constantes de dominio)
|
+-- [CROSS-CUTTING]
|   +-- SensitiveDataDestructuringPolicy (Serilog IDestructuringPolicy, patron §4)
|   +-- MainLayout                       (Blazor layout con nav por rol)
|   +-- ErrorPage                        (Razor Page, /Error, [AllowAnonymous])
|   +-- AccessDeniedPage                 (Razor Page, /Identity/AccessDenied)
|
+-- [INFRAESTRUCTURA]
    +-- AppDbContext                      (EF Core, schema base)
    +-- SerilogConfiguration             (extension method)
    +-- Program.cs                       (composicion del host)

MonitorPedidos.Domain/
+-- Identity/ (PredefinedIdentities, UserIdentity, ApplicationRole)
+-- Shared/   (enums: Severity, CauseCategory, ModuleId, IncidentCloseType, RuleChangeType)
+-- Monitoring/ (CheckResult, CheckStatus, ICheckExecutor)
+-- Alerts/   (AlertMessage)
+-- Notifications/ (INotificationService)

tests/MonitorPedidos.IntegrationTests/
+-- Identity/   (IdentitySelectionTests)
+-- Security/   (SecurityHeadersTests, ExceptionHandlerTests)
```

---

## §2 Componente: SecurityHeadersMiddleware

| Atributo | Detalle |
|----------|---------|
| **Tipo** | ASP.NET Core Middleware |
| **Namespace** | `MonitorPedidos.Web.Middleware` |
| **Clase** | `SecurityHeadersMiddleware : IMiddleware` |
| **Registro** | `app.UseSecurityHeaders()` — antes de Authentication |
| **Responsabilidad** | Emite los 4 headers obligatorios + HSTS condicional en cada response HTML |

**Comportamiento por entorno:**

| Header | Siempre | Solo HTTPS |
|--------|---------|-----------|
| `Content-Security-Policy` | Si | — |
| `X-Content-Type-Options` | Si | — |
| `X-Frame-Options` | Si | — |
| `Referrer-Policy` | Si | — |
| `Strict-Transport-Security` | — | Si (`request.IsHttps`) |

**Dependencias:** ninguna (no inyecta servicios).

---

## §3 Componente: IdentitySelectionPage

| Atributo | Detalle |
|----------|---------|
| **Tipo** | Razor Page |
| **Ruta** | `/Identity/Select` |
| **Autorizacion** | `[AllowAnonymous]` |
| **Responsabilidad** | Presenta 2 botones; en POST emite cookie de sesion con claims del rol seleccionado |

**Interacciones:**

```
IdentitySelectionPage
    |-- lee --> PredefinedIdentities (Domain)
    |-- llama --> HttpContext.SignInAsync() (Cookie Middleware)
    |-- loggea --> ILogger<SelectModel>
    |-- redirige --> /dashboard (tras exito)
```

**Validacion de input:** solo acepta `identity=Operador` o `identity=Tecnico`. Cualquier otro valor vuelve a mostrar la pantalla (sin error visible — evita enumeracion).

---

## §4 Componente: GlobalExceptionHandler

| Atributo | Detalle |
|----------|---------|
| **Tipo** | `IExceptionHandler` (.NET 8) |
| **Namespace** | `MonitorPedidos.Web.Infrastructure` |
| **Clase** | `GlobalExceptionHandler : IExceptionHandler` |
| **Registro** | `builder.Services.AddExceptionHandler<GlobalExceptionHandler>()` |
| **Responsabilidad** | Captura excepciones no manejadas; loggea con detalle; responde con mensaje generico |

**Logica de decision de respuesta:**

```
TryHandleAsync(context, exception, ct)
    |
    v
Determinar formato de respuesta:
    context.Request.Headers["Accept"].Contains("application/json")
    || context.Request.Path.StartsWithSegments("/api")
        |
        +-> [Si] HTTP 500 + ProblemDetails { title = "Error interno" }
        |
        +-> [No] Redirect a /Error
    |
    v
ILogger.LogError(exception,
    "excepcion_no_manejada | type={Type} | rid={RequestId}",
    exception.GetType().Name,
    context.TraceIdentifier)
```

**Invariante critico:** nunca escribe `exception.Message`, `exception.StackTrace` ni rutas internas en la respuesta HTTP.

**Dependencias:** `ILogger<GlobalExceptionHandler>`.

---

## §5 Componente: SensitiveDataDestructuringPolicy

| Atributo | Detalle |
|----------|---------|
| **Tipo** | `IDestructuringPolicy` (Serilog) |
| **Namespace** | `MonitorPedidos.Infrastructure.Logging` |
| **Clase** | `SensitiveDataDestructuringPolicy` |
| **Registro** | `.Destructure.With<SensitiveDataDestructuringPolicy>()` en configuracion de Serilog |
| **Responsabilidad** | Reemplaza valores de campos sensibles por `[REDACTED]` antes de que lleguen al sink |

**Palabras clave monitoreadas (case-insensitive):**
`password`, `token`, `secret`, `credential`, `authorization`, `apikey`, `api_key`, `accesstoken`, `cookie`

**Alcance:** aplica a destructuracion de objetos (`{@objeto}`). Strings literales (`{valor}`) no son afectados — la disciplina de no loggear strings sensibles directamente es responsabilidad del codigo.

---

## §6 Componente: AppDbContext (schema base U1)

| Atributo | Detalle |
|----------|---------|
| **Tipo** | `DbContext` (EF Core 8) |
| **Namespace** | `MonitorPedidos.Infrastructure.Persistence` |
| **Clase** | `AppDbContext : DbContext` |
| **Responsabilidad** | Contexto base de EF Core. En U1 solo configura la conexion y las migraciones iniciales. Las entidades de negocio (`DbSet<Incident>`, etc.) se agregan en U2. |

**Migration inicial de U1:** crea el schema base (sin tablas de usuarios — no hay Identity). La primera migration se llama `InitialCreate` y puede estar vacia o contener solo la tabla de configuracion si se decide tenerla.

**Nota:** No existe `IdentityDbContext` porque no se usa ASP.NET Core Identity.

---

## §7 Componente: FallbackPolicy (configuracion de Authorization)

No es una clase propia — es configuracion en `Program.cs` que afecta a todos los endpoints del sistema.

```csharp
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

**Efecto:** cualquier Razor Page o componente Blazor que NO tenga `[AllowAnonymous]` requiere automaticamente un `ClaimsPrincipal` autenticado. Los endpoints de U2..U6 heredan esta proteccion sin necesitar `[Authorize]` explicito (aunque se recomienda ser explicito en rutas criticas).

---

## §8 Suite de integration tests (WebApplicationFactory)

| Test | Clase | Metodo | NFR / BR verificado |
|------|-------|--------|---------------------|
| T-U1-01 | `IdentitySelectionTests` | `UnauthenticatedRequest_RedirectsToSelect` | BR-AUTHZ-01, RNF-11 |
| T-U1-02 | `IdentitySelectionTests` | `SelectOperador_EmitsCookieWithCorrectClaims` | RF-28, RF-29, NFR-U1-02 |
| T-U1-03 | `IdentitySelectionTests` | `OperadorCookie_AccessReglas_Returns403` | BR-AUTHZ-02, RF-27 |
| T-U1-04 | `SecurityHeadersTests` | `AnyHtmlResponse_ContainsSecurityHeaders` | RNF-09, NFR-U1-01, BR-HEADER-01..04 |
| T-U1-05 | `ExceptionHandlerTests` | `UnhandledException_ReturnsGenericMessage_NoStackTrace` | RNF-15, BR-EX-02 |

**Infraestructura compartida:**

```csharp
// tests/MonitorPedidos.IntegrationTests/Infrastructure/TestWebAppFactory.cs
public sealed class TestWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // Reemplazar DbContext con InMemory para tests de U1
            // (los tests de seguridad no requieren BD real)
        });
    }
}
```

---

## §9 Trazabilidad completa de componentes logicos

| Componente | Patron | NFR | Story | SECURITY |
|------------|--------|-----|-------|----------|
| SecurityHeadersMiddleware | §1 Middleware Chain | RNF-09, NFR-U1-04 | US-28 | SECURITY-04 |
| IdentitySelectionPage | §2 Cookie Auth | RNF-14, NFR-U1-02 | US-23 | SECURITY-08, SECURITY-12 |
| LogoutEndpoint | §2 Cookie Auth | RF-26 | US-23 | SECURITY-08 |
| GlobalExceptionHandler | §3 IExceptionHandler | RNF-15 | US-27 | SECURITY-15 |
| SensitiveDataDestructuringPolicy | §4 IDestructuringPolicy | RNF-08 | US-25 | SECURITY-03, SECURITY-14 |
| FallbackPolicy | §5 Deny-by-default | RNF-11 | US-23, US-24 | SECURITY-08 |
| AppDbContext | — | RNF-06 | US-26 | SECURITY-01 |
| Integration Tests | NFR-U1-05 | NFR-U1-05 | US-23, US-24, US-27, US-28 | SECURITY-08, SECURITY-15 |
