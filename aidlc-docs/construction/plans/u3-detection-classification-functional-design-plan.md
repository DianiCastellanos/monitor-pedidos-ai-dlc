# Functional Design Plan — U3 Detection & Classification

**Stage:** Construction → Functional Design
**Unidad:** U3 — Detection & Classification
**Parte:** 1 (Planning) — este documento
**Fecha:** 2026-05-23
**Fuentes:**
- [`unit-of-work.md`](../../inception/application-design/unit-of-work.md) §U3
- [`requirements.md`](../../inception/requirements/requirements.md) §RF-03..RF-06, RF-31
- [`components.md`](../../inception/application-design/components.md)

---

## Cómo usar este documento

1. Lee cada pregunta de la **§3 Cuestionario de diseño funcional**.
2. Cada pregunta tiene opciones A, B. Una está marcada como **(Recomendada)** con justificación.
3. Responde escribiendo la letra correspondiente después de `[Answer]:`.
4. Si ninguna aplica, elige la última opción y describe tu respuesta.
5. Cuando termines, escribe **"listo"** para que proceda a generar los 4 artefactos de Functional Design.

---

## §1 Foco de U3

U3 implementa el **motor de detección y clasificación de incidentes**. Orquesta la ejecución periódica de checkers (Salesforce, Multivende, DB, Brand), clasifica causas y decide si crear o cerrar incidentes. Depende de U2 (persistencia de incidentes) y es base para U4 (integración con APIs externas) y U5 (reglas).

### Artefactos que generaremos al cerrar este stage

| Artefacto | Contenido |
|-----------|-----------|
| `domain-entities.md` | ICheckExecutor, CheckResult, CauseClassifier, MonitoringService, BrandMonitorChecker |
| `business-logic-model.md` | Flujo de detección: timer → checkers → clasificación → incidente/cierre |
| `business-rules.md` | BR-SCHED-01..05, BR-DET-01..04: deduplicación, auto-resolución, aislamiento |
| `frontend-components.md` | Dashboard en tiempo real: estado por módulo, badges, semáforos |

---

## §2 Contexto de diseño — lo que ya sabemos

Antes de las preguntas, este es el estado confirmado desde Inception:

| Decisión ya tomada | Fuente |
|-------------------|--------|
| `ICheckExecutor` pattern con `IEnumerable<ICheckExecutor>` DI | ADR-U3-02 |
| `MonitoringSchedulerService` como `BackgroundService` con dos timers (5 min / 10 min) | components.md |
| `BrandMonitorChecker` NO genera incidentes — solo actualiza `brand_snapshots` | requirements.md RF-31 |

---

## §3 Cuestionario de diseño funcional (5 preguntas)

---

### Pregunta 1 — ICheckExecutor interface

¿Cómo definimos el contrato de los checkers de monitoreo?

**A) (Recomendada) `Task<CheckResult> CheckAsync(CancellationToken ct)`** — cada implementación (`SalesforceApiChecker`, `MultivendeApiChecker`, `DbHealthChecker`, `BrandMonitorChecker`) encapsula su lógica. `MonitoringSchedulerService` llama a todos vía `IEnumerable<ICheckExecutor>`. Compatibilidad completa con I/O async sin bloquear el thread pool.

**B) Interface con método `Execute()` síncrono** — no compatible con operaciones I/O async (llamadas HTTP a Salesforce/Multivende). Bloquearía threads del thread pool durante las esperas de red.

[Answer]: A — `ICheckExecutor` con `CheckAsync` async *(2026-05-23)*

---

### Pregunta 2 — CauseClassifier: estrategia de clasificación

¿Cómo implementamos la clasificación de causas probables a partir de un `CheckResult`?

**A) (Recomendada) Keyword matching en `CheckResult.Detail`** — diccionario estático de keywords → `CauseProbable`. Sin ML. Extensible vía diccionario en `appsettings.json` si se necesita. Ejemplo: `"timeout"` → `"Timeout de API"`, `"connection refused"` → `"Servicio externo caído"`. Cubre los ~10 casos de causa del MVP.

**B) Rules-based engine separado** — sobre-ingeniería para MVP con ~10 causas posibles. Un engine de reglas con DSL o pipeline de evaluación es mantenimiento innecesario cuando el keyword matching cubre todos los casos de uso actuales.

[Answer]: A — keyword matching estático *(2026-05-23)*

---

### Pregunta 3 — Error isolation entre checkers

Si un checker lanza una excepción, ¿cómo afecta a los demás?

**A) (Recomendada) `try/catch` en `MonitoringSchedulerService` por cada checker** — si `SalesforceApiChecker` lanza excepción, los demás checkers siguen ejecutándose. El error se loggea con Serilog como `Warning`/`Error` con el `ModuleId` y el mensaje de excepción. Garantiza operación continua del ciclo de monitoreo.

**B) Exception en un checker detiene todos** — inaceptable para operación continua. Un problema en la API de Salesforce no debe impedir que se detecten problemas en la BD o en Multivende.

[Answer]: A — `try/catch` por checker, ejecución independiente *(2026-05-23)*

---

### Pregunta 4 — BrandMonitorChecker: comportamiento ante fallo de API

¿Cómo maneja `BrandMonitorChecker` el fallo de `ISalesforceClient` para un site específico?

**A) (Recomendada) Error aislado por site: `PendingCountCurrent = -1`, `Status = Red` para ese site** — los otros 3 sites continúan sin verse afectados. `CheckResult` retorna `Ok` (el checker completó su ciclo aunque un site falló). La UI muestra `Red` con indicador de dato no disponible para el site en error.

**B) En error: abortar todo el ciclo BrandMonitor** — afecta los 4 sites por falla de uno. Viola el principio de aislamiento de errores establecido en ADR-U3-02.

[Answer]: A — error aislado por site, `current = -1` → Red *(2026-05-23)*

---

### Pregunta 5 — MonitoringService: lógica de creación de incidente

¿Cuándo crea `MonitoringService` un nuevo `Incident` a partir de un `CheckResult`?

**A) (Recomendada) Crea `Incident` solo si `CheckResult.Status == Critical` Y no hay incidente `Open` para ese módulo** — si ya existe `Open` → no duplica (deduplicación por módulo). Si `Status == Ok` y hay incidente `Open` → lo cierra automáticamente (AutoResolve). Implementa BR-DET-01..04.

**B) Crea incidente en cualquier `CheckResult` no-Ok** — puede generar decenas de incidentes duplicados por el mismo evento durante un ciclo de 5 minutos. Viola BR-DET-02 (deduplicación).

[Answer]: A — deduplicación por módulo + auto-resolución *(2026-05-23)*

---

## §4 Después de responder

Cuando completes las 5 preguntas, escribe **"listo"** y procederé a:

1. Analizar respuestas para ambigüedades.
2. Si hay ambigüedades, crearé preguntas de seguimiento.
3. Si todo es claro, generaré los 4 artefactos en:
   ```
   aidlc-docs/construction/u3-detection-classification/functional-design/
   ├── domain-entities.md
   ├── business-logic-model.md
   ├── business-rules.md
   └── frontend-components.md
   ```

---

## §5 Plan de ejecución (checklist)

- [x] **5.1** Leer artefactos de Inception (unit-of-work.md §U3, requirements.md RF-03..RF-06, RF-31).
- [x] **5.2** Identificar scope de U3 y preparar preguntas contextuales.
- [x] **5.3** Crear este plan en `aidlc-docs/construction/plans/`.
- [x] **5.4** Recopilar respuestas del owner. *(5/5 = A, A, A, A, A — 2026-05-23)*
- [x] **5.5** Analizar respuestas — 0 ambigüedades detectadas.
- [x] **5.6** Generar `domain-entities.md`. *(v1.0 inicial; v1.1 agrega `BrandMonitorChecker` §9 — 2026-05-24)*
- [x] **5.7** Generar `business-logic-model.md`.
- [x] **5.8** Generar `business-rules.md`.
- [x] **5.9** Generar `frontend-components.md`.
- [x] **5.10** Actualizar `aidlc-state.md`.
- [x] **5.11** Registrar en `audit.md`.
- [x] **5.12** Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-23)*
