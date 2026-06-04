# Functional Design Plan — U2 Persistence & Domain

**Stage:** Construction → Functional Design
**Unidad:** U2 — Persistence & Domain
**Parte:** 1 (Planning) — este documento
**Fecha:** 2026-05-23
**Fuentes:**
- [`unit-of-work.md`](../../inception/application-design/unit-of-work.md) §U2
- [`requirements.md`](../../inception/requirements/requirements.md) §RF-01..RF-12
- [`components.md`](../../inception/application-design/components.md)

---

## Cómo usar este documento

1. Lee cada pregunta de la **§3 Cuestionario de diseño funcional**.
2. Cada pregunta tiene opciones A, B. Una está marcada como **(Recomendada)** con justificación.
3. Responde escribiendo la letra correspondiente después de `[Answer]:`.
4. Si ninguna aplica, elige la última opción y describe tu respuesta.
5. Cuando termines, escribe **"listo"** para que proceda a generar los 4 artefactos de Functional Design.

---

## §1 Foco de U2

U2 define el **modelo de dominio central** del sistema y la capa de persistencia sobre SQL Server MonitorPedidosDb (172.16.0.41). Su aggregate root es `Incident`, que encapsula el ciclo de vida de un incidente detectado (Open → Acknowledged → Resolved). U2 también establece `AppDbContext` en la capa Infrastructure y las interfaces de servicio que las unidades superiores (U3, U4, U5) consumen.

### Artefactos que generaremos al cerrar este stage

| Artefacto | Contenido |
|-----------|-----------|
| `domain-entities.md` | Aggregate root Incident, OwnsOne AlertMessage, enums IncidentStatus, ModuleId |
| `business-logic-model.md` | Flujo de creación, acknowledgment y resolución de incidentes |
| `business-rules.md` | BR-INC-01..07: transiciones válidas, deduplicación, historial inmutable |
| `frontend-components.md` | Componentes Blazor que consumen IIncidentService (tabla activos, historial paginado) |

---

## §2 Contexto de diseño — lo que ya sabemos

Antes de las preguntas, este es el estado confirmado desde Inception:

| Decisión ya tomada | Fuente |
|-------------------|--------|
| EF Core 8 con SQL Server MonitorPedidosDb (172.16.0.41) | C-03 |
| `Incident` es el aggregate root principal | components.md |
| `AppDbContext` en capa Infrastructure | components.md |
| `AlertMessage` es Owned Entity de `Incident` (OwnsOne) | components.md |

---

## §3 Cuestionario de diseño funcional (5 preguntas)

---

### Pregunta 1 — Incident aggregate structure

¿Cómo estructuramos el aggregate `Incident` y su relación con `AlertMessage`?

**A) (Recomendada) `Incident` + `AlertMessage` como `OwnsOne`** — `AlertMessage` se persiste como columnas individuales en la tabla `incidents` (`what_happened`, `when_occurred`, `where_occurred`, `severity`, `probable_cause`, `suggested_action`). Permite queries SQL directas por campo. `Incident` tiene: `Id`, `Status` (enum), `Module` (ModuleId), `OccurredAt`, `ResolvedAt?`, `AcknowledgedAt?`, `AlertMessage` (OwnsOne), `CreatedAt`.

**B) `Incident` + `AlertMessage` serializado como JSON en una columna** — más simple pero no filtrable por SQL sin JSON functions. Adecuado si nunca se necesitan queries directas a los campos del alert.

[Answer]: A — columnas individuales con OwnsOne *(2026-05-23)*

---

### Pregunta 2 — IncidentStatus flow

¿Cómo modelamos el ciclo de vida de estados de un `Incident`?

**A) (Recomendada) Enum: `Open → Acknowledged → Resolved`** — con métodos de dominio: `Acknowledge()`, `Resolve()` que validan transiciones. Solo se puede resolver desde `Acknowledged`. Transición inválida lanza `InvalidOperationException`. Reglas BR-INC-02 y BR-INC-03.

**B) String libre** — menos control de transiciones. Permite estados no definidos; riesgo de corrupción de datos.

[Answer]: A — IncidentStatus enum con transiciones validadas *(2026-05-23)*

---

### Pregunta 3 — IIncidentService interface

¿Cuáles métodos expone `IIncidentService` a las capas superiores?

**A) (Recomendada) `CreateAsync(AlertMessage, ModuleId)`, `AcknowledgeAsync(int id)`, `ResolveAsync(int id)`, `GetActiveAsync()`, `GetHistoryAsync(DateTime from, DateTime to, int page, int pageSize)`, `GetByIdAsync(int id)`** — sin métodos de delete. Los incidentes son registros de auditoría inmutables (BR-INC-07).

**B) Interfaz más amplia con delete** — incidentes son registros de auditoría, no deben borrarse. Viola BR-INC-07.

[Answer]: A — sin delete, historial inmutable *(2026-05-23)*

---

### Pregunta 4 — Paginación en GetHistoryAsync

¿Cómo implementamos la paginación del historial de incidentes?

**A) (Recomendada) Server-side: 20 items/página, skip/take en EF Core** — parámetros: `page` (1-based) + `pageSize`. Retorna `IReadOnlyList<Incident>` + `totalCount`. Cumple RNF-01 (< 2s). La query nunca carga toda la tabla en memoria.

**B) Client-side: retorna todos los registros y pagina en memoria** — simple de implementar pero no escala. Con crecimiento del historial puede superar el límite de rendimiento RNF-01.

[Answer]: A — server-side 20/página *(2026-05-23)*

---

### Pregunta 5 — Detección de discrepancias

Las discrepancias entre jobs Multivende y órdenes activas, ¿requieren persistencia propia?

**A) (Recomendada) In-memory en `MultivendeApiChecker`** — compara estado de jobs Multivende vs órdenes activos. Sin tabla separada. Solo visual en la UI (badge de alerta). Sin overhead de BD para MVP.

**B) Tabla `discrepancies` en BD** — overhead innecesario para MVP. Las discrepancias son efímeras y no requieren historial propio; el incidente generado ya es el registro de auditoría.

[Answer]: A — detección in-memory, sin tabla adicional *(2026-05-23)*

---

## §4 Después de responder

Cuando completes las 5 preguntas, escribe **"listo"** y procederé a:

1. Analizar respuestas para ambigüedades.
2. Si hay ambigüedades, crearé preguntas de seguimiento.
3. Si todo es claro, generaré los 4 artefactos en:
   ```
   aidlc-docs/construction/u2-persistence-domain/functional-design/
   ├── domain-entities.md
   ├── business-logic-model.md
   ├── business-rules.md
   └── frontend-components.md
   ```

---

## §5 Plan de ejecución (checklist)

- [x] **5.1** Leer artefactos de Inception (unit-of-work.md §U2, components.md, requirements.md).
- [x] **5.2** Identificar scope de dominio de U2 y preparar preguntas contextuales.
- [x] **5.3** Crear este plan en `aidlc-docs/construction/plans/`.
- [x] **5.4** Recopilar respuestas del owner. *(5/5 = A, A, A, A, A — 2026-05-23)*
- [x] **5.5** Analizar respuestas — 0 ambigüedades detectadas.
- [x] **5.6** Generar `domain-entities.md`.
- [x] **5.7** Generar `business-logic-model.md`.
- [x] **5.8** Generar `business-rules.md`.
- [x] **5.9** Generar `frontend-components.md`.
- [x] **5.10** Actualizar `aidlc-state.md`.
- [x] **5.11** Registrar en `audit.md`.
- [x] **5.12** Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-23)*
