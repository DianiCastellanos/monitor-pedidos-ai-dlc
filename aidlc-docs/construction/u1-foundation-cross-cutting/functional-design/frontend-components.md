# Frontend Components — U1 Foundation & Cross-Cutting

**Unidad:** U1 — Foundation & Cross-Cutting
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-23
**Versión:** 1.1 (reescritura — seleccion simple de identidad sin ASP.NET Core Identity)

---

## §1 Alcance de UI en U1

U1 no incluye paginas de negocio (dashboard, incidentes, reglas — esas son U2..U6). Su alcance de frontend se limita a:

1. **Pantalla de seleccion de identidad** — 2 botones, sin campos de texto ni password
2. **Pagina de Acceso Denegado** — mensaje generico para HTTP 403
3. **Pagina de Error generica** — sin detalles tecnicos (RNF-12)
4. **Layout base** (`MainLayout.razor`) — barra de navegacion con nombre de identidad y boton de salida

---

## §2 Componentes

### 2.1 IdentitySelectionPage

| Atributo | Valor |
|----------|-------|
| **Tipo** | Razor Page |
| **Ruta** | `/Identity/Select` |
| **Archivo** | `src/MonitorPedidos.Web/Areas/Identity/Pages/Select.cshtml` + `.cshtml.cs` |
| **Autorizacion** | Publica — punto de entrada al sistema |

**Contenido visual:**

```
+--------------------------------------------------+
|         Monitor de Pedidos — Manufacturas Eliot  |
|                                                  |
|         Seleccione su identidad para             |
|         continuar al dashboard                   |
|                                                  |
|   +---------------------+  +------------------+ |
|   | Analista Operativo  |  | Responsable      | |
|   |                     |  | Tecnico          | |
|   | [Ingresar como      |  | [Ingresar como   | |
|   |  Operador]          |  |  Tecnico]        | |
|   +---------------------+  +------------------+ |
+--------------------------------------------------+
```

**Comportamiento:**
- Al pulsar "Analista Operativo": POST a `/Identity/Select` con `identity=Operador`
- Al pulsar "Responsable Tecnico": POST a `/Identity/Select` con `identity=Tecnico`
- En ambos casos el servidor emite cookie de sesion con los claims correspondientes y redirige a `/dashboard`
- Sin campo de texto, sin password, sin email, sin "Recordarme"

**PageModel:**

```csharp
// Areas/Identity/Pages/Select.cshtml.cs
public class SelectModel : PageModel
{
    private readonly ILogger<SelectModel> _logger;

    public SelectModel(ILogger<SelectModel> logger)
    {
        _logger = logger;
    }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Dashboard/Index");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string identity, string? returnUrl)
    {
        var selected = identity switch
        {
            "Operador" => PredefinedIdentities.AnalistaOperativo,
            "Tecnico"  => PredefinedIdentities.ResponsableTecnico,
            _          => null
        };

        if (selected is null)
            return Page(); // input invalido — vuelve a mostrar pantalla

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, selected.DisplayName),
            new Claim(ClaimTypes.Role, selected.RoleName)
        };
        var identity_ = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity_),
            new AuthenticationProperties { IsPersistent = false }
        );

        _logger.LogInformation("identidad_seleccionada | rol={Role}", selected.RoleName);

        return LocalRedirect(returnUrl ?? "/dashboard");
    }
}
```

**Reglas de UI:**
- Toda la UI en espanol (Usab-1)
- Sin campo de texto — solo botones de accion
- Los botones deben deshabilitarse mientras el form esta siendo enviado (evitar doble submit)
- Anti-forgery token incluido automaticamente por ASP.NET Core Razor Pages

---

### 2.2 LogoutEndpoint

| Atributo | Valor |
|----------|-------|
| **Tipo** | Razor Page (POST-only) |
| **Ruta** | `/Identity/Logout` |
| **Archivo** | `src/MonitorPedidos.Web/Areas/Identity/Pages/Logout.cshtml.cs` |
| **Autorizacion** | `[Authorize]` — solo sesiones activas |

**Comportamiento:**
- POST al endpoint de logout -> `HttpContext.SignOutAsync(CookieScheme)` -> cookie invalidada -> redirect a `/Identity/Select`
- No se muestra pagina de confirmacion (logout inmediato al hacer clic en "Salir")
- El enlace de logout en el layout usa `method="post"` con anti-forgery token incluido

```csharp
// Areas/Identity/Pages/Logout.cshtml.cs
public class LogoutModel : PageModel
{
    private readonly ILogger<LogoutModel> _logger;

    public LogoutModel(ILogger<LogoutModel> logger)
    {
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "desconocido";
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("sesion_cerrada | rol={Role}", role);
        return RedirectToPage("/Identity/Select");
    }
}
```

---

### 2.3 AccessDeniedPage

| Atributo | Valor |
|----------|-------|
| **Tipo** | Razor Page |
| **Ruta** | `/Identity/AccessDenied` |
| **Archivo** | `src/MonitorPedidos.Web/Areas/Identity/Pages/AccessDenied.cshtml` |
| **Autorizacion** | `[Authorize]` — usuarios autenticados que intentan ruta fuera de su rol (403) |

**Contenido:**

```html
<h2>Acceso restringido</h2>
<p>No tienes permisos para acceder a esta seccion.</p>
<a asp-page="/Dashboard/Index">Volver al dashboard</a>
```

**Reglas:**
- Sin detalles de que ruta se intento acceder (no exponer paths internos)
- Sin stack trace ni tipo de error
- Link de vuelta al dashboard (que si tiene acceso)

---

### 2.4 ErrorPage

| Atributo | Valor |
|----------|-------|
| **Tipo** | Razor Page |
| **Ruta** | `/Error` |
| **Archivo** | `src/MonitorPedidos.Web/Pages/Error.cshtml` + `.cshtml.cs` |
| **Autorizacion** | Publica (los errores ocurren antes de autenticacion tambien) |

**Contenido:**

```html
<h2>Error inesperado</h2>
<p>Ocurrio un error inesperado. Por favor intente nuevamente o contacte al administrador.</p>
```

**Reglas:**
- **NUNCA** mostrar `exception.Message`, `exception.StackTrace`, ni `RequestId` al usuario
- `RequestId` SI se loggea en Serilog (correlacion interna) pero NO se muestra en UI
- Solo en `Development`: `@if (ShowRequestId) { <p>Request ID: @RequestId</p> }` — deshabilitado en `Production`

---

### 2.5 MainLayout (Layout base)

| Atributo | Valor |
|----------|-------|
| **Tipo** | Blazor Layout Component |
| **Archivo** | `src/MonitorPedidos.Web/Shared/MainLayout.razor` |
| **Aplica a** | Todas las paginas Blazor autenticadas |

**Estructura:**

```
+--------------------------------------------------------------+
|  BARRA SUPERIOR                                              |
|  [Logo/Nombre del sistema]   [Hola, {DisplayName}]  [Salir] |
+--------------------------------------------------------------+
|  NAVEGACION LATERAL (segun rol)                              |
|  - Dashboard (todos)                                         |
|  - Historico (todos)                                         |
|  - Resumen Semanal (todos)                                   |
|  - Discrepancias (todos)                                     |
|  - Reglas [solo Tecnico]                                     |
|  - Logs Tecnicos [solo Tecnico]                              |
+--------------------------------------------------------------+
|  AREA DE CONTENIDO                                           |
|  @Body                                                       |
+--------------------------------------------------------------+
```

**Props / State:**

```razor
@* MainLayout.razor *@
@inherits LayoutComponentBase
@inject AuthenticationStateProvider AuthStateProvider

@code {
    private string DisplayName { get; set; } = string.Empty;
    private bool IsTecnico { get; set; }

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        DisplayName = user.FindFirst(ClaimTypes.Name)?.Value ?? "Usuario";
        IsTecnico = user.IsInRole(ApplicationRole.Names.Tecnico);
    }
}
```

**Reglas de UI:**
- Los items de navegacion restringidos (`Reglas`, `Logs Tecnicos`) se **ocultan en la UI** para el Operador — pero el servidor igualmente retorna 403 si se accede directamente (BR-AUTHZ-02, BR-AUTHZ-03)
- El nombre del usuario se muestra desde el claim `ClaimTypes.Name` (el nombre descriptivo — no un username tecnico)
- El boton "Salir" hace POST al endpoint `/Identity/Logout` con anti-forgery token

---

## §3 Flujo de navegacion de Identity en U1

```
Usuario sin sesion activa
        |
        v
Cualquier ruta protegida
        |
        v (redirect automatico de Authorization Middleware)
/Identity/Select
        |
        +-> [Pulsa Analista Operativo]  -> /dashboard (U6) como Operador
        |
        +-> [Pulsa Responsable Tecnico] -> /dashboard (U6) como Tecnico

Usuario con sesion activa
        |
        +-> Accede a ruta de su rol     -> pagina normal
        |
        +-> Accede a ruta restringida   -> /Identity/AccessDenied (403)
        |
        +-> Hace clic en "Salir"        -> /Identity/Logout -> /Identity/Select
```

---

## §4 Trazabilidad de componentes

| Componente | RF | Story | SECURITY |
|------------|----|-------|----------|
| IdentitySelectionPage | RF-26, RF-28, RF-29 | US-23 | SECURITY-08, SECURITY-12 |
| LogoutEndpoint | RF-26 | US-23 | SECURITY-08 |
| AccessDeniedPage | RF-17, RF-26, RF-27 | US-23, US-24 | SECURITY-08, SECURITY-09 |
| ErrorPage | RNF-12, RNF-15 | US-27 | SECURITY-09, SECURITY-15 |
| MainLayout | RF-27 | US-23, US-15 | SECURITY-08 |
