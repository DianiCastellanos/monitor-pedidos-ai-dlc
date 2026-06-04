# Business Rules — U1 Foundation & Cross-Cutting

**Unidad:** U1 — Foundation & Cross-Cutting
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-23
**Versión:** 1.1 (reescritura — seleccion simple de identidad sin passwords ni lockout)

---

## §1 Reglas de Seleccion de Identidad

| ID | Regla | Fuente | Verificacion |
|----|-------|--------|-------------|
| BR-ID-01 | El sistema DEBE ofrecer exactamente **2 opciones** de identidad: *Analista Operativo* (rol `Operador`) y *Responsable Tecnico* (rol `Tecnico`). Sin campo de texto, sin password, sin email. | RF-28 | UI: verificar que no existen otros campos en la pantalla |
| BR-ID-02 | Al seleccionar una identidad, el servidor DEBE emitir una cookie de sesion con los claims `ClaimTypes.Name` (nombre visible) y `ClaimTypes.Role` (nombre de rol). No se persiste nada en base de datos. | RF-28, RF-29 | Test: seleccionar identidad -> cookie emitida con claims correctos |
| BR-ID-03 | La pantalla de seleccion de identidad es la **unica ruta publica** del sistema. Todas las demas rutas requieren cookie de sesion valida (deny-by-default). | RF-26, SECURITY-08 | Test E2E: request sin cookie a /dashboard -> redirect a /Identity/Select |
| BR-ID-04 | No existe mecanismo de registro, recuperacion de acceso, ni cambio de identidad fuera de la pantalla de seleccion. El unico flujo es: seleccionar -> cookie emitida -> sesion activa. | RF-28 | Code review: sin endpoints de registro ni recuperacion |

---

## §2 Reglas de Sesion y Cookies

| ID | Regla | Fuente | Verificacion |
|----|-------|--------|-------------|
| BR-COOKIE-01 | La cookie de sesion DEBE emitirse siempre con `HttpOnly = true`. Sin excepciones. | RF-29, RNF-14, SECURITY-12 | DevTools del navegador: cookie sin flag HttpOnly -> falla |
| BR-COOKIE-02 | La cookie DEBE emitirse con `SameSite = Strict`. | RF-29, RNF-14 | DevTools: verificar atributo SameSite |
| BR-COOKIE-03 | El flag `Secure` DEBE emitirse SOLO cuando el sistema sirve sobre HTTPS (RNF-07 escenario b). En localhost HTTP (escenario a) el flag `Secure` puede omitirse. | RF-29, RNF-07 | Test en HTTP local: cookie sin Secure es aceptable. Test en HTTPS interno: cookie CON Secure requerida. |
| BR-COOKIE-04 | La cookie NO es persistente entre reinicios del navegador (`IsPersistent = false`). El usuario debe seleccionar identidad nuevamente al cerrar y reabrir el navegador. | RF-29 | Test: seleccionar identidad -> cerrar navegador -> reabrir -> pantalla de seleccion aparece |

---

## §3 Reglas de Autorizacion

| ID | Regla | Fuente | Verificacion |
|----|-------|--------|-------------|
| BR-AUTHZ-01 | **Deny-by-default**: toda ruta del sistema requiere autenticacion, excepto `/Identity/Select` y `/Identity/Logout`. Un request sin cookie valida a cualquier otra ruta DEBE redirigir a `/Identity/Select`. | RF-26, RNF-11, SECURITY-08 | Test E2E: request sin cookie -> redirect a seleccion de identidad |
| BR-AUTHZ-02 | Las rutas de gestion de reglas (`/reglas`, `/reglas/editar`, `/reglas/historial`) DEBEN retornar **HTTP 403 server-side** si el usuario tiene rol `Operador`. Ocultar el enlace en la UI no es suficiente. | RF-17, RF-27, RNF-11, SECURITY-08 | Test: seleccionar Analista Operativo + GET /reglas -> 403 (no 404, no redirect) |
| BR-AUTHZ-03 | Las rutas de logs tecnicos (`/logs`) DEBEN retornar HTTP 403 para rol `Operador`. | RF-27, RNF-11 | Test: seleccionar Analista Operativo + GET /logs -> 403 |
| BR-AUTHZ-04 | CORS DEBE estar **deshabilitado**. La app sirve la UI y la API desde el mismo origen. No se admiten origenes cruzados. | RNF-11, SECURITY-08 | Test: request con header `Origin` diferente -> rechazado o ignorado |
| BR-AUTHZ-05 | El rol `Tecnico` tiene acceso a todo lo que tiene `Operador` mas las rutas protegidas por `[Authorize(Roles="Tecnico")]`. No existe jerarquia de roles — se usan claims de rol por ruta. | RF-27 | Test: seleccionar Responsable Tecnico -> acceso a /reglas y /logs exitoso |

---

## §4 Reglas de HTTP Security Headers

| ID | Regla | Fuente | Verificacion |
|----|-------|--------|-------------|
| BR-HEADER-01 | Toda respuesta HTML DEBE incluir `Content-Security-Policy: default-src 'self'`. | RNF-09, SECURITY-04 | curl: verificar header presente en respuesta |
| BR-HEADER-02 | Toda respuesta HTML DEBE incluir `X-Content-Type-Options: nosniff`. | RNF-09, SECURITY-04 | curl: verificar header |
| BR-HEADER-03 | Toda respuesta HTML DEBE incluir `X-Frame-Options: DENY`. | RNF-09, SECURITY-04 | curl: verificar header |
| BR-HEADER-04 | Toda respuesta HTML DEBE incluir `Referrer-Policy: strict-origin-when-cross-origin`. | RNF-09, SECURITY-04 | curl: verificar header |
| BR-HEADER-05 | `Strict-Transport-Security: max-age=31536000; includeSubDomains` DEBE emitirse **SOLO** cuando `HttpContext.Request.IsHttps == true`. En HTTP no se emite. | RNF-09, RNF-07 | Test HTTP: header HSTS ausente. Test HTTPS: header HSTS presente. |
| BR-HEADER-06 | Los headers de seguridad se aplican a **todas** las respuestas HTML del sistema, incluyendo la pantalla de seleccion de identidad, paginas de error y demas Razor Pages. | RNF-09 | Verificar headers en /Identity/Select, /Error, /dashboard |

---

## §5 Reglas de Logging

| ID | Regla | Fuente | Verificacion |
|----|-------|--------|-------------|
| BR-LOG-01 | **Prohibido** loggear: tokens, secrets, cualquier dato que permita identificar a una persona de forma combinada, numeros de identificacion. No aplica password (no existen). | RNF-08, SECURITY-03, SECURITY-14 | Code review + auditoria de logs generados en demo |
| BR-LOG-02 | Toda entrada de log DEBE incluir: `timestamp` (UTC, ISO 8601), `request_id`, `log_level`, `message`. | RNF-08, SECURITY-03 | Inspeccion del archivo de log |
| BR-LOG-03 | Los archivos de log se rotan **diariamente** y se retienen durante **90 dias** (alineado con retencion de incidentes, RF-21). | RNF-08 | Inspeccion de config Serilog (`rollingInterval: Day`, `retainedFileCountLimit: 90`) |
| BR-LOG-04 | El log de errores DEBE incluir el stack trace completo en el archivo de log (para debugging). NUNCA en la respuesta HTTP al cliente. | RNF-15, RNF-12 | Test: excepcion forzada -> stack trace en log, mensaje generico en UI |
| BR-LOG-05 | Eventos de identidad/sesion DEBEN loggearse con nivel `Information` o superior: `identidad_seleccionada`, `sesion_cerrada`, `acceso_denegado`, `excepcion_no_manejada`. | SECURITY-03, SECURITY-14 | Inspeccion de log tras pruebas de seleccion de identidad |

---

## §6 Reglas de Manejo de Excepciones

| ID | Regla | Fuente | Verificacion |
|----|-------|--------|-------------|
| BR-EX-01 | Toda llamada a recursos externos (BD, HTTP) DEBE estar envuelta en try/catch. Un fallo no manejado no puede escapar al pipeline sin ser capturado por `GlobalExceptionHandler`. | RNF-15, SECURITY-15 | Code review — sin llamadas a BD/HTTP sin try/catch |
| BR-EX-02 | `GlobalExceptionHandler` DEBE retornar un mensaje generico al cliente: **nunca** el tipo de excepcion, el stack trace, ni rutas internas del servidor. | RNF-12, RNF-15, SECURITY-09 | Test: forzar excepcion -> respuesta al cliente sin detalles tecnicos |
| BR-EX-03 | Los recursos (conexiones a BD, streams, clientes HTTP) DEBEN liberarse con `using` o `try-finally`. Sin resource leaks. | RNF-15 | Code review |
| BR-EX-04 | En caso de fallo de la BD en startup (durante migracion), la aplicacion DEBE fallar con log detallado y mensaje claro. No puede arrancar en estado inconsistente. | RNF-15 | Test: apagar MonitorPedidosDb -> app falla con log claro |

---

## §7 Reglas de Dependencias (Supply Chain)

| ID | Regla | Fuente | Verificacion |
|----|-------|--------|-------------|
| BR-DEP-01 | El archivo `packages.lock.json` DEBE existir en el repositorio y estar actualizado. Generado con `dotnet restore --use-lock-file`. | RNF-13, SECURITY-10 | Pre-commit: verificar existencia del lock file |
| BR-DEP-02 | No puede haber paquetes NuGet con vulnerabilidades conocidas de severidad **High** o **Critical**. Verificar con `dotnet list package --vulnerable`. | RNF-13, SECURITY-10 | Script de verificacion ejecutado antes de cada sprint |
| BR-DEP-03 | No puede haber paquetes NuGet sin uso en el proyecto. | RNF-13 | `dotnet list package` + code review |

---

## §8 Reglas de Hardening

| ID | Regla | Fuente | Verificacion |
|----|-------|--------|-------------|
| BR-HARD-01 | Sin endpoints de muestra, scaffolded pages de prueba, o rutas de diagnostico activas en produccion (ej. `/swagger`, `/health` expuesto publicamente, `/api/debug`). | RNF-12, SECURITY-09 | Inspeccion del routing en `Program.cs` |
| BR-HARD-02 | Directory listing DEBE estar deshabilitado. El servidor no puede listar archivos de carpetas estaticas. | RNF-12, SECURITY-09 | Test: GET /wwwroot -> no lista archivos |
| BR-HARD-03 | La variable de entorno `ASPNETCORE_ENVIRONMENT` DEBE ser `Production` en el entorno de demo. `Development` solo en maquina de desarrollo. En `Production`, las paginas de error de desarrollo de ASP.NET Core estan deshabilitadas. | RNF-12, SECURITY-09 | Inspeccion de la configuracion de entorno |
