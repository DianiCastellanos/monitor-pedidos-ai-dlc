# NFR Requirements Plan — U3 Detection & Classification

**Stage:** Construction → NFR Requirements
**Unidad:** U3 — Detection & Classification
**Parte:** 1 (Planning) — este documento
**Fecha:** 2026-05-23
**Fuentes:**
- U3 `functional-design/` (4 artefactos aprobados)
- [`requirements.md`](../../inception/requirements/requirements.md) RNF-01..RNF-05

---

## Cómo usar este documento

1. Lee cada pregunta de la **§3 Cuestionario de NFR**.
2. Cada pregunta tiene opciones A, B. Una está marcada como **(Recomendada)**.
3. Responde escribiendo la letra correspondiente después de `[Answer]:`.
4. Cuando termines, escribe **"listo"** para que proceda a generar los artefactos de NFR Requirements.

---

## §1 Foco de U3 — NFR Requirements

U3 ejecuta código periódico en background (`BackgroundService`). Los NFRs relevantes son de **fiabilidad del scheduler** (sin acumulación de ticks), **gestión de lifetimes DI** (Singleton scheduler vs Scoped repositorios) y **cobertura de tests** (lógica de deduplicación y BrandMonitorChecker).

### Artefactos que generaremos al cerrar este stage

| Artefacto | Contenido |
|-----------|-----------|
| `nfr-requirements.md` | NFRs específicos de U3: scheduler, DI lifetime, test coverage |
| `tech-stack-decisions.md` | Decisiones de stack para background processing y DI |

---

## §2 Contexto — NFRs ya definidos

Los siguientes NFRs ya están determinados desde Inception o desde los artefactos de Functional Design de U3:

| NFR | Estado | Decisión |
|-----|--------|----------|
| Timer1 5 min | Definido | `SalesforceApiChecker` + `DbHealthChecker` |
| Timer2 10 min | Definido | `MultivendeApiChecker` + `BrandMonitorChecker` |
| Error isolation | Definido | `try/catch` por checker |
| BR-SCHED-01..05 | Definidos | Ver `business-rules.md` de U3 |

---

## §3 Cuestionario de NFR Requirements (3 preguntas)

---

### Pregunta 1 — Implementación del scheduler: BackgroundService vs IHostedService

¿Cómo implementamos el scheduler periódico de U3?

**A) (Recomendada) `BackgroundService` con `PeriodicTimer`** — `PeriodicTimer` de .NET 6+ es más limpio que `System.Threading.Timer`. No acumula ticks si la iteración anterior no terminó (non-overlapping). Dos instancias de `PeriodicTimer` (Timer1: 5 min, Timer2: 10 min) en el mismo `BackgroundService`. Una sola clase registrada, un solo ciclo de vida.

**B) Dos `BackgroundService` separados (uno por timer)** — más separación de responsabilidades pero doble registro en DI, doble ciclo de vida gestionado. El overhead es innecesario cuando ambos timers comparten los mismos `ICheckExecutor` y `IServiceScopeFactory`.

[Answer]: A — un `BackgroundService` con dos `PeriodicTimer` *(2026-05-23)*

---

### Pregunta 2 — IServiceScopeFactory para servicios Scoped en Singleton

`MonitoringSchedulerService` se registra como Singleton, pero `IIncidentService` y `IIncidentRepository` son Scoped (EF Core `DbContext`). ¿Cómo resolvemos la captive dependency?

**A) (Recomendada) `IServiceScopeFactory`: crear scope por tick** — `MonitoringSchedulerService` (Singleton) recibe `IServiceScopeFactory` en constructor. En cada tick de timer, crea un `IServiceScope` para resolver `IIncidentService` y `IIncidentRepository`. El scope se destruye al finalizar el tick. Patrón establecido en ADR-U3-02 / ADR-U5-01. Evita la captive dependency.

**B) Registrar `IIncidentService` como Singleton** — viola el contrato de lifetime de EF Core. `DbContext` no es thread-safe ni está diseñado para lifetime Singleton; genera errores de concurrencia y fugas de conexión.

[Answer]: A — `IServiceScopeFactory`, scope por tick *(2026-05-23)*

---

### Pregunta 3 — Cobertura de tests para U3

¿Qué nivel de cobertura automatizada definimos para U3?

**A) (Recomendada) Unit tests con Moq para lógica de orquestación y BrandMonitorChecker** — `MonitoringService`: incidente creado si `Critical` + sin `Open`; no duplica si ya `Open`; cierra si `Ok` + `Open` existe. `BrandMonitorChecker`: bajó suficiente → Green; subió → Red; API falla → Red (`current=-1`); sin regla → threshold=0 → Green. Total: ~7 unit tests con xUnit + Moq.

**B) Solo integration tests** — más costosos y lentos para lógica de clasificación y deduplicación. Los unit tests con Moq son más rápidos de escribir y ejecutar; los integration tests se complementan pero no reemplazan.

[Answer]: A — unit tests con Moq *(2026-05-23)*

---

## §4 Después de responder

Cuando completes las 3 preguntas, escribe **"listo"** y procederé a:

1. Analizar respuestas para ambigüedades.
2. Si todo es claro, generaré los 2 artefactos en:
   ```
   aidlc-docs/construction/u3-detection-classification/nfr-requirements/
   ├── nfr-requirements.md
   └── tech-stack-decisions.md
   ```

---

## §5 Plan de ejecución (checklist)

- [x] **5.1** Analizar artefactos Functional Design de U3.
- [x] **5.2** Identificar NFRs ya definidos vs decisiones pendientes.
- [x] **5.3** Crear este plan en `aidlc-docs/construction/plans/`.
- [x] **5.4** Recopilar respuestas del owner. *(3/3 = A, A, A — 2026-05-23)*
- [x] **5.5** Analizar respuestas — 0 ambigüedades detectadas.
- [x] **5.6** Generar `nfr-requirements.md`.
- [x] **5.7** Generar `tech-stack-decisions.md`.
- [x] **5.8** Actualizar `aidlc-state.md`.
- [x] **5.9** Registrar en `audit.md`.
- [x] **5.10** Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-23)*
