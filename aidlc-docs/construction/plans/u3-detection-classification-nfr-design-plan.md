# NFR Design Plan — U3 Detection & Classification

**Stage:** Construction → NFR Design
**Unidad:** U3 — Detection & Classification
**Parte:** 1 (Planning) — este documento
**Fecha:** 2026-05-23
**Fuentes:**
- U3 `nfr-requirements/` (2 artefactos aprobados)
- U3 `functional-design/domain-entities.md`

---

## Cómo usar este documento

1. Lee cada pregunta de la **§3 Cuestionario de NFR Design**.
2. Cada pregunta tiene opciones A, B. Una está marcada como **(Recomendada)**.
3. Responde escribiendo la letra correspondiente después de `[Answer]:`.
4. Cuando termines, escribe **"listo"** para que proceda a generar los artefactos de NFR Design.

---

## §1 Foco de U3 — NFR Design

Con los NFR Requirements aprobados, este stage traduce los requisitos no funcionales de U3 en **patrones de diseño concretos**: cómo se registra `ICheckExecutor` en DI, cómo se estructura el logging estructurado dentro de cada checker, y cómo se gestiona el lifetime del scope por tick.

### Artefactos que generaremos al cerrar este stage

| Artefacto | Contenido |
|-----------|-----------|
| `nfr-design-patterns.md` | Patrones aplicados: IEnumerable DI, IServiceScopeFactory, ILogger<T> estructurado |
| `logical-components.md` | Diagrama de componentes lógicos de U3: scheduler, checkers, MonitoringService, DI |

---

## §2 Contexto — Patrones ya determinados

Los siguientes patrones ya están determinados desde NFR Requirements de U3:

| Patrón | Decisión |
|--------|----------|
| `PeriodicTimer` en `BackgroundService` | NFR-U3-01 |
| `IServiceScopeFactory` para repos Scoped | ADR-U3-02, ADR-U5-01 |
| `try/catch` por checker | NFR-U3-02 |

---

## §3 Cuestionario de NFR Design (2 preguntas)

---

### Pregunta 1 — Registro DI de ICheckExecutor

¿Cómo registramos las implementaciones de `ICheckExecutor` en el contenedor DI?

**A) (Recomendada) Cada `ICheckExecutor` registrado con `services.AddSingleton<ICheckExecutor, SalesforceApiChecker>()`** — `MonitoringSchedulerService` recibe `IEnumerable<ICheckExecutor>` en constructor; DI resuelve todos los registrados. Sin factory ni keyed services. El orden de registro en `Program.cs` determina el orden de ejecución. Patrón ADR-U3-02.

**B) Keyed services (.NET 8) con `services.AddKeyedSingleton<ICheckExecutor, ...>(key)`** — no necesario; `IEnumerable<ICheckExecutor>` DI ya cumple el patrón de forma más simple. Los keyed services añaden complejidad de resolución sin beneficio para este caso de uso.

[Answer]: A — `IEnumerable<ICheckExecutor>` DI pattern (ADR-U3-02) *(2026-05-23)*

---

### Pregunta 2 — Logging estructurado en checkers

¿Cómo estructuramos el logging dentro de cada checker?

**A) (Recomendada) `ILogger<T>` inyectado en cada checker con campos estructurados** — log level: `Information` para Ok, `Warning` para Warn, `Error` para Critical/Exception. Campos estructurados: `{ModuleId}`, `{CheckStatus}`, `{Duration}` (en ms). Sin PII en mensajes de log (RNF-08). Cada checker loggea su propio contexto; Serilog centraliza la salida.

**B) Logging centralizado solo en `MonitoringSchedulerService`** — los checkers no loggean directamente. Pierde el contexto detallado de cada checker (duración, status específico). El scheduler no tiene acceso al contexto interno de cada checker para loggearlo apropiadamente.

[Answer]: A — `ILogger<T>` en cada checker con campos estructurados *(2026-05-23)*

---

## §4 Después de responder

Cuando completes las 2 preguntas, escribe **"listo"** y procederé a:

1. Analizar respuestas para ambigüedades.
2. Si todo es claro, generaré los 2 artefactos en:
   ```
   aidlc-docs/construction/u3-detection-classification/nfr-design/
   ├── nfr-design-patterns.md
   └── logical-components.md
   ```

---

## §5 Plan de ejecución (checklist)

- [x] **5.1** Analizar artefactos NFR Requirements de U3.
- [x] **5.2** Identificar patrones ya determinados vs decisiones pendientes.
- [x] **5.3** Crear este plan con 2 preguntas.
- [x] **5.4** Recopilar respuestas del owner. *(2/2 = A, A — 2026-05-23)*
- [x] **5.5** Analizar respuestas — 0 ambigüedades detectadas.
- [x] **5.6** Generar `nfr-design-patterns.md`.
- [x] **5.7** Generar `logical-components.md`.
- [x] **5.8** Actualizar `aidlc-state.md`.
- [x] **5.9** Registrar en `audit.md`.
- [x] **5.10** Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-23)*
