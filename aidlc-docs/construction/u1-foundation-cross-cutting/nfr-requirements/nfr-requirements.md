# NFR Requirements — U1 Foundation & Cross-Cutting

**Unidad:** U1 — Foundation & Cross-Cutting
**Stage:** Construction → NFR Requirements
**Fecha:** 2026-05-23
**Versión:** 1.0
**Fuente plan:** [`u1-foundation-cross-cutting-nfr-requirements-plan.md`](../../plans/u1-foundation-cross-cutting-nfr-requirements-plan.md)

---

## §1 NFRs heredados de Inception (ya definidos — sin cambios)

Estos NFRs están completamente especificados en `requirements.md` v1.2 y se implementan tal cual en U1:

| ID | Requerimiento | Implementación en U1 | Fuente |
|----|---------------|---------------------|--------|
| RNF-06 | Cifrado at-rest LocalDB + canal cifrado a BD | TDE o cifrado de archivo LocalDB; connection string con `Encrypt=True` | SECURITY-01 |
| RNF-07 | HTTPS cuando red interna | `app.UseHttpsRedirection()` condicional; dev-certs en entornos multi-equipo | SECURITY-01 (parcial) |
| RNF-08 | Logging sin PII | Serilog con filtros por nombre de campo (`password`, `token`, `secret`) | SECURITY-03, SECURITY-14 |
| RNF-10 | Input validation + consultas parametrizadas | DataAnnotations en PageModels; EF Core (parámetros automáticos) | SECURITY-05 |
| RNF-11 | Authorization en todas las rutas | `app.UseAuthorization()` + `[Authorize]` global; `/Identity/Select` y `/Error` exentos | SECURITY-08 |
| RNF-12 | Hardening de producción | `ASPNETCORE_ENVIRONMENT=Production` en demo; errores genéricos; sin Swagger | SECURITY-09, SECURITY-15 |
| RNF-13 | Dependency pinning | `packages.lock.json` commiteado; `dotnet restore --use-lock-file` en CI | SECURITY-10 |
| RNF-14 | Cookie de sesión segura | `HttpOnly=true`, `SameSite=Strict`, `Secure` cuando HTTPS, `IsPersistent=false` | SECURITY-12 (parcial) |
| RNF-15 | Exception handling fail-closed | `GlobalExceptionHandler` middleware; try/catch en todas las llamadas externas | SECURITY-15 |

---

## §2 NFRs de implementación — Decisiones tomadas en este stage

### NFR-U1-01 — Content Security Policy adaptada para Blazor Server

**Decisión:** Q1 = A

Blazor Server requiere WebSockets (circuito SignalR) y scripts inline para su inicialización. La CSP mínima `default-src 'self'` rompería el circuito. La política adoptada es:

```
Content-Security-Policy:
  default-src 'self';
  script-src 'self' 'unsafe-inline';
  style-src 'self' 'unsafe-inline';
  connect-src 'self' wss:;
  frame-ancestors 'none'
```

| Directiva | Razón |
|-----------|-------|
| `default-src 'self'` | Base restrictiva — bloquea todo por defecto excepto mismo origen |
| `script-src 'unsafe-inline'` | Blazor Server inyecta scripts inline al inicializar el circuito |
| `style-src 'unsafe-inline'` | Blazor puede emitir estilos inline en componentes |
| `connect-src wss:` | SignalR usa WebSocket (`wss://`) para el circuito en tiempo real |
| `frame-ancestors 'none'` | Refuerza `X-Frame-Options: DENY` (protección clickjacking) |

**Verifica:** Abrir DevTools → Console → confirmar que no hay errores CSP al cargar el dashboard y recibir notificaciones SignalR.

**Trazabilidad:** RNF-09, SECURITY-04, BR-HEADER-01

---

### NFR-U1-02 — Idle timeout de cookie: 8 horas con sliding expiration

**Decisión:** Q2 = A

```csharp
// Program.cs
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath         = "/Identity/Select";
        options.AccessDeniedPath  = "/Identity/AccessDenied";
        options.Cookie.HttpOnly   = true;
        options.Cookie.SameSite   = SameSiteMode.Strict;
        options.ExpireTimeSpan    = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        // options.Cookie.SecurePolicy se configura condicionalmente (ver NFR-U1-04)
    });
```

**Comportamiento:**
- La cookie expira 8 horas después del **último request activo** (sliding).
- Si el usuario está inactivo 8 horas seguidas → re-selección de identidad requerida.
- Al cerrar el navegador → cookie desaparece independientemente del timeout (IsPersistent=false).
- Cada request que llega renueva el contador de 8 horas.

**Trazabilidad:** RNF-14, RF-29, BR-COOKIE-04

---

### NFR-U1-03 — Serilog: formato texto legible + carpeta `logs/`

**Decisión:** Q3 = B

```json
// appsettings.json — sección Serilog
{
  "Serilog": {
    "Using": ["Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/monitor-pedidos-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 90,
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} | rid={RequestId}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

**Nota sobre formato:** texto legible directo en editor. Incluye `RequestId` para correlación. El campo `rid=` facilita buscar todos los logs de un request específico con `grep` o el buscador del editor. Los eventos de seguridad (`identidad_seleccionada`, `acceso_denegado`) serán visibles sin herramientas adicionales.

**Trazabilidad:** RNF-08, BR-LOG-02, BR-LOG-03, SECURITY-03, SECURITY-14

---

### NFR-U1-04 — SecurityHeaders Middleware personalizado

**Decisión:** Q4 = A

Sin dependencias externas. El middleware evalúa `context.Request.IsHttps` en runtime para emitir HSTS solo cuando aplica.

```csharp
// Middleware/SecurityHeadersMiddleware.cs
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["Content-Security-Policy"]   =
            "default-src 'self'; script-src 'self' 'unsafe-inline'; " +
            "style-src 'self' 'unsafe-inline'; connect-src 'self' wss:; " +
            "frame-ancestors 'none'";
        headers["X-Content-Type-Options"]    = "nosniff";
        headers["X-Frame-Options"]           = "DENY";
        headers["Referrer-Policy"]           = "strict-origin-when-cross-origin";

        if (context.Request.IsHttps)
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        await _next(context);
    }
}

// Extension method para Program.cs
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
```

**Trazabilidad:** RNF-09, BR-HEADER-01..06, SECURITY-04

---

### NFR-U1-05 — Tests de integración con WebApplicationFactory (~5 tests)

**Decisión:** Q5 = A

Proyecto de tests: `tests/MonitorPedidos.IntegrationTests/` con xUnit + `Microsoft.AspNetCore.Mvc.Testing`.

| Test | Descripción | Comportamiento esperado |
|------|-------------|------------------------|
| `T-U1-01` | Request sin cookie a `/dashboard` | Redirect 302 a `/Identity/Select` |
| `T-U1-02` | POST a `/Identity/Select` con `identity=Operador` | Cookie emitida con `ClaimTypes.Role = "Operador"`; redirect a `/dashboard` |
| `T-U1-03` | Cookie Operador + GET `/reglas` | HTTP 403 Forbidden |
| `T-U1-04` | GET a cualquier ruta HTML | Headers `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Content-Security-Policy` presentes |
| `T-U1-05` | Exception forzada en endpoint de prueba | Respuesta no contiene stack trace ni tipo de excepción; HTTP 500 con mensaje genérico |

**Nota:** Los tests corren contra la app real en memoria (`WebApplicationFactory`) — sin mocks de infraestructura para los comportamientos de seguridad (siguiendo la filosofía del proyecto).

**Trazabilidad:** BR-AUTHZ-01..03, BR-HEADER-01..04, BR-EX-02, US-23, US-24, US-27, US-28

---

## §3 Compliance SECURITY extension — U1

| Regla | Estado en U1 | NFR / BR que lo cubre |
|-------|-------------|----------------------|
| SECURITY-01 Encryption at-rest/transit | Aplica | RNF-06, RNF-07 |
| SECURITY-02 Network intermediaries | N/A MVP | (localhost/red interna) |
| SECURITY-03 Application logging | Aplica | RNF-08, NFR-U1-03 |
| SECURITY-04 HTTP security headers | Aplica | RNF-09, NFR-U1-01, NFR-U1-04 |
| SECURITY-05 Input validation | Aplica | RNF-10 |
| SECURITY-06 Least-privilege | Aplica (parcial) | RF-27 — roles en cookie; sin IAM cloud |
| SECURITY-07 Network config | N/A MVP | (sin exposición pública) |
| SECURITY-08 Access control | Aplica | RNF-11, BR-AUTHZ-01..05 |
| SECURITY-09 Hardening | Aplica | RNF-12, BR-HARD-01..03 |
| SECURITY-10 Supply chain | Aplica | RNF-13, BR-DEP-01..03 |
| SECURITY-11 Secure design | Aplica | Selección simple sin credenciales en BD; fail-closed |
| SECURITY-12 Auth & credential mgmt | Aplica (parcial) | RNF-14, NFR-U1-02 — cookie sin passwords |
| SECURITY-13 Software integrity | Aplica (parcial) | lock file; sin deserialización insegura |
| SECURITY-14 Alerting & monitoring | Aplica (parcial) | RNF-08, NFR-U1-03 — logs locales 90 días |
| SECURITY-15 Exception handling | Aplica | RNF-15, BR-EX-01..04, NFR-U1-04 (middleware) |

**Resultado:** 13/15 reglas cubiertas. 2 N/A documentados (SECURITY-02, SECURITY-07 — localhost MVP). Sin hallazgos bloqueantes.

---

## §4 Resumen de decisiones NFR-U1

| ID | Decisión | Impacto en Code Gen |
|----|----------|---------------------|
| NFR-U1-01 | CSP adaptada para Blazor | `SecurityHeadersMiddleware` — política CSP completa con `wss:` y `unsafe-inline` |
| NFR-U1-02 | Cookie idle 8h sliding | `options.ExpireTimeSpan = 8h; SlidingExpiration = true` en `Program.cs` |
| NFR-U1-03 | Serilog texto legible, `logs/` relativa | `appsettings.json` — `outputTemplate` con formato texto; `rollingInterval: Day` |
| NFR-U1-04 | Middleware personalizado | Clase `SecurityHeadersMiddleware` + extension method |
| NFR-U1-05 | 5 integration tests con WebApplicationFactory | Proyecto `tests/MonitorPedidos.IntegrationTests/` + 5 test methods |
