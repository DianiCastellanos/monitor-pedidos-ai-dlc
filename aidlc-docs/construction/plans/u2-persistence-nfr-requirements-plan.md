# NFR Requirements Plan — U2 Persistence & Domain

**Stage:** Construction → NFR Requirements
**Unidad:** U2 — Persistence & Domain
**Parte:** 1 (Planning) — este documento
**Fecha:** 2026-05-23
**Fuentes:**
- U2 `functional-design/` (4 artefactos aprobados)
- [`requirements.md`](../../inception/requirements/requirements.md) RNF-01..RNF-05

---

## Cómo usar este documento

1. Lee cada pregunta de la **§3 Cuestionario de NFR**.
2. Cada pregunta tiene opciones A, B. Una está marcada como **(Recomendada)**.
3. Responde escribiendo la letra correspondiente después de `[Answer]:`.
4. Cuando termines, escribe **"listo"** para que proceda a generar los artefactos de NFR Requirements.

---

## §1 Foco de U2 — NFR Requirements

U2 establece la capa de datos y el modelo de dominio. Los NFRs relevantes para esta unidad son principalmente de **rendimiento** (queries paginadas < 2s), **consistencia** (transacciones EF Core) y **cobertura de tests** (lógica de dominio y repositorio).

### Artefactos que generaremos al cerrar este stage

| Artefacto | Contenido |
|-----------|-----------|
| `nfr-requirements.md` | NFRs específicos de U2: índices, health check, test coverage |
| `tech-stack-decisions.md` | Decisiones de stack para la capa de persistencia |

---

## §2 Contexto — NFRs ya definidos

Los siguientes NFRs ya están determinados desde Inception o desde los artefactos de Functional Design de U2:

| NFR | Estado | Decisión |
|-----|--------|----------|
| RNF-01 Rendimiento | Definido | Página de historial < 2s; queries paginadas |
| RNF-02 Disponibilidad | Definido | MonitorPedidosDb en mismo host; sin réplica en MVP |
| RNF-03 Consistencia | Definido | EF Core transactions en `CreateAsync` |
| BR-INC-01..07 | Definidos | Ver `business-rules.md` de U2 |

---

## §3 Cuestionario de NFR Requirements (3 preguntas)

---

### Pregunta 1 — Índices en tabla incidents

¿Qué estrategia de índices usamos en la tabla `incidents` para cumplir RNF-01?

**A) (Recomendada) Índice compuesto `(status, occurred_at DESC)` + `(module, occurred_at DESC)`** — el índice `(status, occurred_at DESC)` cubre el query más común: incidentes activos ordenados por fecha. El índice adicional `(module, occurred_at DESC)` cubre filtros por módulo en el historial. Ambos se definen en `IncidentConfiguration.cs` vía Fluent API.

**B) Solo índice en `Id` (primary key por defecto)** — sin índices adicionales. Aceptable si el volumen nunca supera ~1.000 registros, pero el crecimiento continuo del historial degradará los queries de historial paginado sin índices de apoyo.

[Answer]: A — índice `(status, occurred_at DESC)` + `(module, occurred_at DESC)` *(2026-05-23)*

---

### Pregunta 2 — Health check para la BD

¿Implementamos un health check para la conectividad con SQL Server MonitorPedidosDb (172.16.0.41)?

**A) (Recomendada) `IHealthCheck` personalizado con `context.Database.CanConnectAsync()`** — endpoint `/health` con respuesta JSON. Registrado como health check con tag `"database"` en `Program.cs`. Permite monitoreo proactivo de la BD desde la misma aplicación.

**B) Sin health check para MVP** — la BD es MonitorPedidosDb en el mismo host; una falla es obvia. Menos código, pero sin endpoint estándar de monitoreo que futuras herramientas puedan consumir.

[Answer]: A — `IHealthCheck` con `/health` endpoint *(2026-05-23)*

---

### Pregunta 3 — Cobertura de tests para U2

¿Qué nivel de cobertura automatizada definimos para U2?

**A) (Recomendada) Unit tests para lógica de dominio + integration tests para `IIncidentService`** — unit tests: transiciones de estado (`Open→Acknowledged`, `Acknowledged→Resolved`, transición inválida → excepción), `DetermineStatus` de `BrandSnapshot`. Integration tests para `IIncidentService` con BD real (MonitorPedidosDb en test). Total ~6 tests con xUnit y Moq.

**B) Solo prueba manual** — sin tests automáticos para U2. No recomendado: la lógica de transiciones de estado es crítica y el riesgo de regresión es alto.

[Answer]: A — unit tests de transiciones + integration tests *(2026-05-23)*

---

## §4 Después de responder

Cuando completes las 3 preguntas, escribe **"listo"** y procederé a:

1. Analizar respuestas para ambigüedades.
2. Si todo es claro, generaré los 2 artefactos en:
   ```
   aidlc-docs/construction/u2-persistence-domain/nfr-requirements/
   ├── nfr-requirements.md
   └── tech-stack-decisions.md
   ```

---

## §5 Plan de ejecución (checklist)

- [x] **5.1** Analizar artefactos Functional Design de U2.
- [x] **5.2** Identificar NFRs ya definidos vs decisiones pendientes.
- [x] **5.3** Crear este plan en `aidlc-docs/construction/plans/`.
- [x] **5.4** Recopilar respuestas del owner. *(3/3 = A, A, A — 2026-05-23)*
- [x] **5.5** Analizar respuestas — 0 ambigüedades detectadas.
- [x] **5.6** Generar `nfr-requirements.md`.
- [x] **5.7** Generar `tech-stack-decisions.md`.
- [x] **5.8** Actualizar `aidlc-state.md`.
- [x] **5.9** Registrar en `audit.md`.
- [x] **5.10** Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-23)*
