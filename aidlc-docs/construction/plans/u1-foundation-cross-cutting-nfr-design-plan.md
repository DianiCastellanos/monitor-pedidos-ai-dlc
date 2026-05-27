# NFR Design Plan — U1 Foundation & Cross-Cutting

**Stage:** Construction → NFR Design
**Unidad:** U1 — Foundation & Cross-Cutting
**Fecha:** 2026-05-23
**Fuentes:**
- `aidlc-docs/construction/u1-foundation-cross-cutting/nfr-requirements/nfr-requirements.md`
- `aidlc-docs/construction/u1-foundation-cross-cutting/nfr-requirements/tech-stack-decisions.md`
- `aidlc-docs/construction/u1-foundation-cross-cutting/functional-design/business-logic-model.md`

---

## Patrones ya determinados (sin preguntas necesarias)

| Patrón | Decisión ya tomada |
|--------|-------------------|
| Middleware chain (seguridad) | `SecurityHeadersMiddleware` personalizado — NFR-U1-04 |
| Cookie authentication | `AddAuthentication(CookieScheme)` + `SignInAsync` — NFR-U1-02 |
| Identity selection | 2 botones → `PredefinedIdentities` → claims → cookie — Functional Design |
| Logging pipeline | Serilog con sink de archivo, texto legible, 90 días — NFR-U1-03 |
| Authorization | `[Authorize]` global + `[Authorize(Roles="Técnico")]` — RNF-11 |
| CSP | Política Blazor-compatible definida — NFR-U1-01 |

**Quedan 2 decisiones de diseño de patrón** donde el input del owner determina el código generado.

---

## Cuestionario NFR Design (2 preguntas)

---

### Pregunta 1 — Implementación del GlobalExceptionHandler

ASP.NET Core 8 ofrece dos formas de capturar excepciones no manejadas globalmente:

**A) (Recomendada) `IExceptionHandler` interface (nuevo en .NET 8)** — clase que implementa `IExceptionHandler`, registrada con `builder.Services.AddExceptionHandler<GlobalExceptionHandler>()` + `app.UseExceptionHandler()`. Más testeable (unit testeable con mock de `HttpContext`), más explícita, alineada con el estilo de .NET 8.

**B) Lambda inline `UseExceptionHandler(app => app.Run(...))`** — configura el handler directamente en `Program.cs` como un lambda. Menos código pero más difícil de testear de forma aislada.

[Answer]: **A** — `IExceptionHandler` interface *(2026-05-23)*

---

### Pregunta 2 — Filtro PII en Serilog

BR-LOG-01 prohíbe loggear contraseñas, tokens y PII. Para cumplirlo en Serilog hay dos estrategias:

**A) (Recomendada) `IDestructuringPolicy` personalizado** — clase que inspecciona los valores destructurados por Serilog y reemplaza campos sensibles (`password`, `token`, `secret`, `credential`) por `[REDACTED]` antes de que lleguen al sink. Es la forma idiomática de Serilog, funciona para todos los sinks, y puede testearse unitariamente.

**B) Filtro de enriquecimiento (`ILogEventFilter`)** — filtra eventos completos de log si contienen campos sensibles. Más agresivo (puede descartar el evento entero) pero pierde información de contexto si el log mezcla datos seguros y sensibles.

[Answer]: **A** — `IDestructuringPolicy` personalizado *(2026-05-23)*

---

## Plan de ejecución (checklist)

- [x] **1** Analizar artefactos NFR Requirements de U1.
- [x] **2** Identificar patrones ya determinados vs decisiones pendientes.
- [x] **3** Crear este plan con 2 preguntas.
- [x] **4** Recopilar respuestas del owner. *(2/2 = A, A — 2026-05-23)*
- [x] **5** Analizar respuestas — 0 ambigüedades detectadas.
- [x] **6** Generar `nfr-design-patterns.md`.
- [x] **7** Generar `logical-components.md`.
- [x] **8** Actualizar `aidlc-state.md`.
- [x] **9** Registrar en `audit.md`.
- [x] **10** Presentar mensaje de cierre para aprobación explícita.
