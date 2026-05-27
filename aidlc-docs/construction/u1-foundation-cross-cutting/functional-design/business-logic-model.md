# Business Logic Model — U1 Foundation & Cross-Cutting

**Unidad:** U1 — Foundation & Cross-Cutting
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-23
**Versión:** 1.1 (reescritura — seleccion simple de identidad sin ASP.NET Core Identity)

---

## §1 Flujos de Negocio

### Flujo 1 — Seleccion de Identidad

```
Usuario abre el dashboard sin sesion activa
        |
        v
Redireccionado a /Identity/Select (deny-by-default, RF-26)
        |
        v
Pantalla muestra dos opciones:
    [Analista Operativo]    [Responsable Tecnico]
        |
        +---> [Pulsado: Analista Operativo]
        |           |
        |           v
        |       Claims = { Name="Analista Operativo", Role="Operador" }
        |           |
        |           v
        |       HttpContext.SignInAsync(CookieScheme, claims)
        |           |
        |           v
        |       Cookie emitida: HttpOnly + SameSite=Strict [+ Secure si HTTPS]
        |           |
        |           v
        |       Log: "identidad_seleccionada | rol=Operador"
        |           |
        |           v
        |       Redirect a /dashboard
        |
        +---> [Pulsado: Responsable Tecnico]
                    |
                    v
                Claims = { Name="Responsable Tecnico", Role="Tecnico" }
                    |
                    v
                HttpContext.SignInAsync(CookieScheme, claims)
                    |
                    v
                Cookie emitida: HttpOnly + SameSite=Strict [+ Secure si HTTPS]
                    |
                    v
                Log: "identidad_seleccionada | rol=Tecnico"
                    |
                    v
                Redirect a /dashboard
```

**Nota:** No hay validacion de credenciales. La identidad es una declaracion del usuario. El control de acceso real ocurre en el servidor via `[Authorize(Roles="Tecnico")]` (Flujo 3).

---

### Flujo 2 — Cierre de Sesion (Logout)

```
Usuario hace clic en "Salir"
        |
        v
POST a /Identity/Logout (con anti-forgery token)
        |
        v
HttpContext.SignOutAsync(CookieScheme)
        |
        v
Cookie de sesion invalidada (eliminada del navegador)
        |
        v
Log: "sesion_cerrada | rol={Role}"
        |
        v
Redirect a /Identity/Select
```

---

### Flujo 3 — Autorizacion (Acceso a ruta protegida)

```
Request HTTP llega a cualquier ruta
        |
        v
Authorization Middleware evalua cookie de sesion
        |
        +---> [Sin cookie valida / sesion expirada]
        |           |
        |           v
        |       Redirect a /Identity/Select?ReturnUrl={ruta}
        |
        +---> [Cookie valida — rol insuficiente para la ruta]
        |     (ej. Operador intenta /reglas o /logs)
        |           |
        |           v
        |       HTTP 403 Forbidden
        |       AccessDeniedPage (mensaje generico, sin detalle tecnico)
        |           |
        |           v
        |       Log: "acceso_denegado | rol={Role} | ruta={Path}"
        |
        +---> [Cookie valida — rol suficiente]
                    |
                    v
                Continua procesamiento normal del request
```

---

### Flujo 4 — Pipeline de Seguridad HTTP (por cada request)

El pipeline se ejecuta en este orden segun registro en `Program.cs`:

```
Request entrante
        |
        v
[1] HTTPS Redirection Middleware     -> redirige HTTP -> HTTPS (escenario b de RNF-07)
        |
        v
[2] SecurityHeadersMiddleware        -> inyecta headers de seguridad en toda respuesta HTML
    Content-Security-Policy: default-src 'self'
    X-Content-Type-Options: nosniff
    X-Frame-Options: DENY
    Referrer-Policy: strict-origin-when-cross-origin
    [+ Strict-Transport-Security solo si request.IsHttps]
        |
        v
[3] Authentication Middleware        -> valida cookie, carga ClaimsPrincipal
        |
        v
[4] Authorization Middleware         -> evalua [Authorize] attributes y roles
        |
        v
[5] GlobalExceptionHandler           -> captura excepciones no manejadas
        |
        v
[6] Routing + Endpoint               -> Blazor pages / Razor Pages Identity
        |
        v
Response saliente
        |
        v
[5 unwinding] GlobalExceptionHandler -> si hubo excepcion: log + error generico
        |
        v
[2 unwinding] SecurityHeadersMiddleware -> headers ya inyectados
```

---

### Flujo 5 — Manejo Global de Excepciones

```
Excepcion no manejada en cualquier punto del pipeline
        |
        v
GlobalExceptionHandler intercepta
        |
        v
Log: "excepcion_no_manejada | type={Type} | message={Message} | request_id={Id} | stack={StackTrace}"
(Stack trace SOLO en el log — NUNCA en la respuesta al cliente)
        |
        v
        +---> [Request es API/JSON] -> HTTP 500 + body: { "error": "Error interno del servidor" }
        |
        +---> [Request es Blazor/HTML] -> Redirect a /Error
                    |
                    v
                Pagina de error: "Ocurrio un error inesperado. Por favor intente mas tarde."
                Sin detalles tecnicos, sin stack trace, sin tipo de excepcion
```

---

### Flujo 6 — Logging Estructurado (Pipeline Serilog)

```
Cualquier componente llama ILogger<T>.Log*(...)
        |
        v
Serilog recibe el log event
        |
        v
Filtros PII:
    El mensaje o las propiedades contienen "password", "token", "secret", "credential"?
        +---> Si  -> enriquecer con [REDACTED] o descartar campo
        +---> No  -> continua
        |
        v
Enriquecer con:
    - Timestamp (UTC, ISO 8601)
    - request_id (desde HttpContext.TraceIdentifier)
    - log_level
    - source_context (clase que loggeo)
        |
        v
Sink: archivo rotado diariamente
    Ruta: logs/monitor-pedidos-{Date}.log
    Retencion: 90 dias (alineado con RF-21)
    Formato: JSON estructurado
        |
        v
[Futuro post-MVP] Sink adicional: remoto (Seq, ELK, etc.)
```

---

## §2 Estado del sistema al completar U1

| Componente | Estado tras U1 |
|------------|----------------|
| `MonitorPedidos.sln` | Compila sin warnings |
| Pantalla de seleccion | Funcional — 2 botones emiten cookie con rol |
| Cookie de sesion | HttpOnly + SameSite=Strict + Secure si HTTPS |
| Authorization | deny-by-default + 403 para rutas de Tecnico con rol Operador |
| Headers de seguridad | Presentes en toda respuesta HTML |
| Logging | Serilog escribiendo a archivo rotado, sin PII |
| Exception handling | GlobalExceptionHandler activo, errores genericos al cliente |
| Base de datos de usuarios | No existe — no hay tabla de cuentas |
| Resto del sistema | Sin funcionalidad de monitoreo — eso es U2..U6 |
