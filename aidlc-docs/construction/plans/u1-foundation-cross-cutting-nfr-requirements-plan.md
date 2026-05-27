# NFR Requirements Plan — U1 Foundation & Cross-Cutting

**Stage:** Construction → NFR Requirements
**Unidad:** U1 — Foundation & Cross-Cutting
**Fecha:** 2026-05-23
**Fuentes:**
- `aidlc-docs/construction/u1-foundation-cross-cutting/functional-design/` (4 artefactos aprobados)
- `aidlc-docs/inception/requirements/requirements.md` v1.2 (RNF-06..RNF-15)
- `aidlc-docs/inception/requirements/nfr-quality-matrix.md` v1.1

---

## Contexto: NFRs ya definidos para U1

Estos NFRs están completamente especificados desde Inception y no requieren preguntas adicionales:

| NFR | Estado | Decisión confirmada |
|-----|--------|---------------------|
| RNF-07 HTTPS | Definido | HTTP en localhost OK; HTTPS con dev-certs cuando hay red interna |
| RNF-08 Logging sin PII | Definido | Serilog, sin contraseñas/tokens/PII, filtros explícitos |
| RNF-09 Security Headers | Definido | 4 headers obligatorios; HSTS solo en HTTPS |
| RNF-10 Input validation | Definido | DataAnnotations + FluentValidation; consultas parametrizadas |
| RNF-11 Authorization | Definido | deny-by-default; 403 server-side para rutas de Técnico |
| RNF-12 Hardening | Definido | Sin stack trace en prod; sin endpoints de muestra |
| RNF-13 Dependency pinning | Definido | packages.lock.json; scan vulnerabilidades |
| RNF-14 Cookie auth | Definido | HttpOnly + SameSite=Strict; IsPersistent=false |
| RNF-15 Exception handling | Definido | GlobalExceptionHandler; try/catch en llamadas externas |
| BR-COOKIE-01..04 | Definido | Ver business-rules.md §2 |

**Quedan 5 decisiones técnicas de implementación** donde la respuesta del owner determinará el código generado.

---

## Cuestionario NFR Requirements (5 preguntas)

---

### Pregunta 1 — Content Security Policy y Blazor Server

`default-src 'self'` es el mínimo de seguridad, pero **Blazor Server requiere**:
- WebSocket a `wss://localhost` (para el circuito SignalR)
- `frame-ancestors 'none'` (complementa `X-Frame-Options: DENY`)
- Potencialmente `script-src 'self' 'unsafe-inline'` porque Blazor inyecta scripts inline al inicializar

**A) (Recomendada) CSP adaptada para Blazor Server** — política: `default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; connect-src 'self' wss:; frame-ancestors 'none'`. Permite que Blazor funcione correctamente y sigue siendo más restrictiva que sin CSP.

**B) CSP mínima `default-src 'self'` como está en requirements.md** — puede romper el circuito SignalR de Blazor en producción; requiere prueba exhaustiva. Se mantiene como está en el documento y se ajusta en construcción si hay problemas.

**C) Sin CSP en MVP, solo los otros 3 headers** — elimina el riesgo de romper Blazor pero sacrifica protección XSS. No recomendado (SECURITY-04 requiere CSP).

[Answer]:

---

### Pregunta 2 — Timeout de la cookie de sesión (sliding expiry)

La cookie no es persistente (`IsPersistent=false`), pero ASP.NET Core Cookie Auth tiene un **`ExpireTimeSpan`** (idle timeout) configurable. Si el usuario no hace requests durante ese tiempo, la cookie expira aunque el navegador siga abierto.

**A) (Recomendada) 8 horas de idle timeout** — cubre una jornada laboral completa sin forzar re-selección. Alineado con uso operativo (el dashboard NOC corre durante el turno). Valor: `options.ExpireTimeSpan = TimeSpan.FromHours(8); options.SlidingExpiration = true`.

**B) 4 horas** — más conservador desde seguridad; puede requerir re-selección en jornadas largas. Adecuado si el uso es por sesiones cortas de consulta.

**C) Sin idle timeout explícito (usar el default de ASP.NET Core: 14 días)** — el default está pensado para cookies persistentes; no aplica bien al modelo de selección simple de identidad. No recomendado.

[Answer]:

---

### Pregunta 3 — Ubicación y formato de los archivos de log (Serilog)

Los logs deben rotarse diariamente y retenerse 90 días (RNF-08, BR-LOG-03). La decisión afecta la configuración de Serilog en `appsettings.json` y el path del sink.

**A) (Recomendada) Carpeta `logs/` relativa al directorio de la app, formato JSON estructurado** — ruta: `logs/monitor-pedidos-.log` (Serilog añade la fecha). Formato: `Serilog.Formatting.Compact` (JSON compacto, 1 línea por evento). Parseable por herramientas. Retención: 90 archivos (`retainedFileCountLimit: 90`).

**B) Carpeta `logs/` relativa al directorio de la app, formato texto legible** — formato: `[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}`. Más fácil de leer directamente en un editor de texto. Menos parseable por herramientas, pero más amigable para revisión manual en MVP.

**C) Ruta absoluta configurable vía `appsettings.json`** — permite al operador elegir la ruta sin recompilar. Más flexible pero añade un parámetro de configuración a gestionar. Se combinaria con cualquiera de los formatos anteriores.

[Answer]:

---

### Pregunta 4 — SecurityHeaders Middleware: ¿personalizado o paquete NuGet?

El middleware de security headers puede implementarse de dos formas:

**A) (Recomendada) Middleware personalizado — clase `SecurityHeadersMiddleware`** — ~20 líneas de código C#, sin dependencias externas, control total sobre qué headers se emiten y cuándo (especialmente el comportamiento condicional de HSTS según `request.IsHttps`). Más mantenible a largo plazo.

**B) Paquete NuGet `NetEscapades.AspNetCore.SecurityHeaders`** — configuración declarativa en `Program.cs`, ampliamente usado en la comunidad ASP.NET Core. Reduce código a escribir pero agrega una dependencia externa. Requiere verificar vulnerabilidades (RNF-13).

[Answer]:

---

### Pregunta 5 — Cobertura de tests automatizados para U1

¿Qué nivel de testing automatizado se requiere para U1 antes de avanzar a U2?

**A) (Recomendada) Tests de integración para comportamientos de seguridad críticos** — con `WebApplicationFactory<Program>`: (1) request sin cookie → redirect a `/Identity/Select`, (2) seleccionar identidad → cookie con claims correctos, (3) Operador accede a `/reglas` → 403, (4) headers de seguridad presentes en respuesta, (5) error forzado → mensaje genérico sin stack trace. Total: ~5 integration tests. Sin unit tests de lógica trivial.

**B) Solo prueba manual** — probar los comportamientos en el navegador y documentar evidencia. Más rápido de completar, pero sin regresión automática.

**C) Coverage completo: integration tests + unit tests de enums/value objects** — máxima cobertura; puede ser excesivo para value objects triviales como enums. Añade tiempo al sprint.

[Answer]:

---

## Plan de ejecución (checklist)

- [x] **1** Analizar artefactos de Functional Design de U1.
- [x] **2** Identificar NFRs ya definidos vs decisiones pendientes.
- [x] **3** Crear este plan con 5 preguntas en `aidlc-docs/construction/plans/`.
- [x] **4** Recopilar respuestas del owner. *(5/5 = A, A, B, A, A — 2026-05-23)*
- [x] **5** Analizar respuestas — 0 ambigüedades detectadas.
- [x] **6** Generar `nfr-requirements.md`.
- [x] **7** Generar `tech-stack-decisions.md`.
- [x] **8** Actualizar `aidlc-state.md`.
- [x] **9** Registrar en `audit.md`.
- [x] **10** Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-23)*
