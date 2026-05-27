# NFR Design Patterns — U1 Foundation & Cross-Cutting

**Unidad:** U1 — Foundation & Cross-Cutting
**Stage:** Construction → NFR Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Patron 1 — Middleware Chain (seguridad HTTP)

**Problema:** Cada response HTTP debe incluir headers de seguridad y redireccion HTTPS, independientemente de qué endpoint se invoque.

**Patron aplicado:** Pipeline Middleware de ASP.NET Core — los middlewares se encadenan en orden y envuelven cada request/response.

**Implementacion:**

```
Request entrante
    |
    v
[1] HttpsRedirection    --> redirige HTTP->HTTPS cuando aplica (RNF-07)
    |
    v
[2] SecurityHeaders     --> inyecta CSP, X-Content-Type-Options, X-Frame-Options,
                           Referrer-Policy, HSTS (condicional)
    |
    v
[3] StaticFiles
    |
    v
[4] Routing
    |
    v
[5] Authentication      --> lee cookie, carga ClaimsPrincipal
    |
    v
[6] Authorization       --> evalua [Authorize] attributes
    |
    v
[7] ExceptionHandler    --> captura excepciones no manejadas
    |
    v
[8] Endpoints           --> Razor Pages + Blazor
```

**Regla critica de orden:** SecurityHeaders DEBE registrarse antes de Authentication y Authorization para que los headers se emitan incluso en respuestas de redirect (302) a `/Identity/Select`.

**Cumple:** NFR-U1-04, RNF-09, BR-HEADER-01..06, SECURITY-04

---

## §2 Patron 2 — Cookie Authentication sin Identity Library

**Problema:** Necesitamos sesiones con roles sin la complejidad de ASP.NET Core Identity (sin tablas de usuarios, sin password hashing).

**Patron aplicado:** Cookie Authentication Scheme directo — `AddAuthentication(CookieScheme)` emite/valida cookies con ClaimsPrincipal construido en codigo.

**Flujo de emision:**

```
POST /Identity/Select?identity=Operador
    |
    v
SelectModel.OnPostAsync()
    |
    v
Resolver PredefinedIdentities.AnalistaOperativo
    |
    v
Construir claims:
    ClaimTypes.Name = "Analista Operativo"
    ClaimTypes.Role = "Operador"
    |
    v
HttpContext.SignInAsync(
    CookieAuthenticationDefaults.AuthenticationScheme,
    new ClaimsPrincipal(new ClaimsIdentity(claims, scheme)),
    new AuthenticationProperties { IsPersistent = false }
)
    |
    v
Cookie emitida: HttpOnly=true, SameSite=Strict,
               Secure=true (si HTTPS), ExpireTimeSpan=8h sliding
```

**Flujo de validacion (por cada request):**

```
Authentication Middleware
    |
    v
Lee cookie de sesion
    |
    v
Valida firma (data protection key ring de ASP.NET Core)
    |
    +-> [Valida] --> ClaimsPrincipal cargado en HttpContext.User
    |
    +-> [Invalida/ausente] --> HttpContext.User = anonimo
                               --> Authorization Middleware redirige a /Identity/Select
```

**Cumple:** RNF-14, NFR-U1-02, RF-26, RF-28, RF-29, BR-COOKIE-01..04, SECURITY-12

---

## §3 Patron 3 — Global Exception Handler (IExceptionHandler .NET 8)

**Problema:** Cualquier excepcion no capturada en el pipeline debe: (a) loggearse con detalle completo, (b) retornar al cliente un mensaje generico sin detalles tecnicos.

**Patron aplicado:** `IExceptionHandler` interface — nuevo en .NET 8, permite clase dedicada, testeable en aislamiento.

**Estructura:**

```
Excepcion no manejada en cualquier middleware/endpoint
    |
    v
ExceptionHandlerMiddleware (app.UseExceptionHandler())
    |
    v
GlobalExceptionHandler.TryHandleAsync(HttpContext, Exception, CancellationToken)
    |
    v
ILogger.LogError(exception, "excepcion_no_manejada | type={Type} | rid={RequestId}", ...)
(stack trace completo en log -- NUNCA en la respuesta)
    |
    v
Detectar tipo de respuesta:
    |
    +-> [Accept: application/json o ruta /api/*]
    |       --> HTTP 500
    |       --> body: {"error": "Error interno del servidor"}
    |
    +-> [Blazor / HTML]
            --> context.Response.Redirect("/Error")
            --> ErrorPage muestra mensaje generico
```

**Configuracion en Program.cs:**

```csharp
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
// ...
app.UseExceptionHandler();
```

**Cumple:** RNF-15, BR-EX-01..04, SECURITY-15, NFR-U1-05 (T-U1-05)

---

## §4 Patron 4 — PII Filtering en Serilog (IDestructuringPolicy)

**Problema:** BR-LOG-01 prohibe loggear contraseñas, tokens, secrets o PII. El riesgo es que un desarrollador accidentalmente use `{@request}` y Serilog serialice toda la request incluyendo headers de autorización.

**Patron aplicado:** `IDestructuringPolicy` — intercepta la destructuracion de objetos antes de que lleguen al sink y redacta campos sensibles.

**Estructura:**

```csharp
public sealed class SensitiveDataDestructuringPolicy : IDestructuringPolicy
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "password", "token", "secret", "credential",
        "authorization", "apikey", "api_key", "accesstoken"
    };

    public bool TryDestructure(object value, ILogEventPropertyValueFactory factory,
        out LogEventPropertyValue result)
    {
        // Solo aplica a diccionarios y objetos anonimos
        // Reemplaza valores cuya KEY sea sensible por "[REDACTED]"
    }
}
```

**Flujo:**

```
ILogger.LogInformation("Login | {@Request}", httpRequest)
    |
    v
Serilog destructura {@Request}
    |
    v
SensitiveDataDestructuringPolicy.TryDestructure()
    |
    v
Campo "Authorization" detectado --> valor reemplazado por "[REDACTED]"
Campo "Cookie" detectado        --> valor reemplazado por "[REDACTED]"
    |
    v
LogEvent limpio llega al File sink
```

**Registro en Program.cs:**

```csharp
Log.Logger = new LoggerConfiguration()
    .Destructure.With<SensitiveDataDestructuringPolicy>()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
```

**Cumple:** RNF-08, BR-LOG-01, SECURITY-03, SECURITY-14

---

## §5 Patron 5 — Deny-by-Default Authorization

**Problema:** Toda ruta excepto `/Identity/Select` y `/Error` debe requerir autenticacion. Olvidar un `[Authorize]` no debe abrir una brecha.

**Patron aplicado:** Authorization Policy global como fallback — todo endpoint requiere autenticacion por defecto; las excepciones se marcan explicitamente con `[AllowAnonymous]`.

**Configuracion:**

```csharp
builder.Services.AddAuthorization(options =>
{
    // Politica global: cualquier endpoint no marcado requiere usuario autenticado
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});
```

**Excepciones marcadas con `[AllowAnonymous]`:**
- `Select.cshtml.cs` — pantalla de seleccion de identidad
- `Error.cshtml` — pagina de error (puede ocurrir antes de autenticacion)
- Archivos estaticos (wwwroot) — servidos antes del middleware de auth

**Resultado:** un endpoint Blazor nuevo que se agregue en U2..U6 queda automaticamente protegido sin requerir `[Authorize]` explicito.

**Cumple:** RNF-11, RF-26, BR-AUTHZ-01, SECURITY-08

---

## §6 Patron 6 — Sliding Expiration con Renovacion Automatica

**Problema:** El idle timeout de 8 horas debe renovarse con cada request activo (sliding), pero no debe generar overhead de escritura en cada request.

**Patron aplicado:** ASP.NET Core Cookie Authentication con `SlidingExpiration = true` — la implementacion interna solo re-emite la cookie cuando ha pasado mas del 50% del tiempo de expiracion (para evitar re-emision en cada request).

**Comportamiento:**
- Request en la hora 1 de 8: no re-emite cookie (< 4 horas transcurridas = 50%).
- Request en la hora 5 de 8: re-emite cookie con nuevo expiry de 8 horas desde ahora.
- Sin requests por 8 horas: cookie expira; siguiente request redirige a `/Identity/Select`.

**Cumple:** RNF-14, NFR-U1-02, BR-COOKIE-04

---

## §7 Resumen de patrones por NFR

| NFR / BR | Patron aplicado | Artefacto |
|----------|----------------|-----------|
| RNF-09, BR-HEADER-01..06 | Middleware Chain + SecurityHeadersMiddleware | §1 |
| RNF-14, BR-COOKIE-01..04 | Cookie Auth sin Identity | §2 |
| RNF-15, BR-EX-01..04 | GlobalExceptionHandler (IExceptionHandler) | §3 |
| RNF-08, BR-LOG-01 | IDestructuringPolicy PII filter | §4 |
| RNF-11, BR-AUTHZ-01 | FallbackPolicy deny-by-default | §5 |
| RNF-14, NFR-U1-02 | SlidingExpiration 50% threshold | §6 |
