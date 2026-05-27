# Unit of Work Plan — MonitorPedidos AI

**Stage:** Inception → Units Generation
**Parte:** 1 (Planning) — este documento
**Profundidad:** Standard
**Fuentes:** [`prd.md`](../../../prd.md) v2.3 §13 (sprints), [`requirements.md`](../requirements/requirements.md) v1.1, [`stories.md`](../user-stories/stories.md), [`personas.md`](../user-stories/personas.md), [`components.md`](../application-design/components.md), [`services.md`](../application-design/services.md), [`component-dependency.md`](../application-design/component-dependency.md).
**Rol asumido:** Tech Lead / Engineering Manager

---

## Cómo usar este documento

1. Lee cada pregunta de la **§3 Cuestionario de descomposición**.
2. Cada pregunta tiene opciones marcadas A, B, C, etc. Una opción está marcada como **(Recomendada)** con justificación.
3. Responde escribiendo la letra correspondiente después de `[Answer]:`.
4. Si ninguna opción aplica, escoge la última (Otro) y describe tu respuesta.
5. Cuando termines, escribe **"listo"** o **"completado"** para que proceda al análisis de ambigüedades y a la generación de los 3 artefactos.

---

## §1 Foco del stage

**Units Generation** descompone el sistema en **unidades de trabajo** (UoW) que sirven como input para Construction. Una unidad es una agrupación lógica de stories + componentes + módulos que se pueden construir y testear con cohesión interna alta y acoplamiento entre unidades bajo.

### Contexto importante para este stage

- La arquitectura es **monolito modular** (decisión Q1 de Application Design) → todas las unidades viven en el mismo proceso ASP.NET Core. No hay despliegues separados.
- **PRD §13 ya define 4 sprints** con asignación específica de módulos. Las unidades pueden o no alinear con esos sprints (lo decides en la Pregunta 3).
- Construction está **OUT OF SCOPE** para esta iteración. Las unidades son insumo para activar Construction en el futuro.

### Lo que generaremos al cerrar este stage

| Artefacto | Propósito |
|-----------|-----------|
| `unit-of-work.md` | Definición de cada unidad (propósito, scope, stories, components, entry/exit criteria, sprint sugerido). |
| `unit-of-work-dependency.md` | Matriz de dependencias entre unidades + diagrama Mermaid del orden recomendado. |
| `unit-of-work-story-map.md` | Mapeo bidireccional Story ↔ Unidad. Verificación de cobertura 29/29 stories. |

---

## §2 Plan de ejecución (checklist)

> Esta sección la marcaré [x] durante la generación (Parte 2) — solo está para visibilidad.

- [x] **2.1** Leer respuestas validadas de §3.
- [x] **2.2** Analizar respuestas para ambigüedades y crear follow-up si aplica. *(0 ambigüedades)*
- [x] **2.3** Esperar aprobación explícita del plan + respuestas finales. *(Aprobado por el owner: "Approve & procede Part 2 — Generation")*
- [x] **2.4** Generar `aidlc-docs/inception/application-design/unit-of-work.md` con 7 unidades.
- [x] **2.5** Generar `aidlc-docs/inception/application-design/unit-of-work-dependency.md` con matriz y Mermaid.
- [x] **2.6** Generar `aidlc-docs/inception/application-design/unit-of-work-story-map.md` con mapeo bidireccional.
- [x] **2.7** Validar cobertura: **29/29 stories asignadas, 0 huérfanas, 0 duplicadas** (verificado en unit-of-work-story-map.md §5).
- [x] **2.8** Validar dependencias: **DAG sin ciclos** (verificado en unit-of-work-dependency.md §6).
- [x] **2.9** Actualizar `aidlc-state.md` (Units Generation ✅, Inception phase cerrado).
- [x] **2.10** Registrar en `audit.md`.
- [x] **2.11** Presentar mensaje de cierre con criterios de revisión. *(Aprobado por el owner: "Approve & Close Inception")*

---

## §3 Cuestionario de descomposición (5 preguntas)

### Pregunta 1 — Estrategia de descomposición

¿Cómo agrupamos los componentes en unidades de trabajo?

A) **(Recomendada)** **Por capability funcional** — ~6–7 unidades agrupadas alrededor de una capability cohesiva: *Foundation & Cross-Cutting*, *Persistence & Incidents*, *Detection & Classification*, *External Integrations*, *Rules Management*, *Dashboard & Real-Time*, *Simulation & Red-Teaming*. Refleja cómo se piensa el sistema y permite asignar a desarrolladores distintos sin solapamientos. Compatible con sprints del PRD §13 sin forzar 1:1.
B) **Por sprint del PRD §13** — 4 unidades alineadas 1:1 con Sprint 1, 2, 3, 4. Simple y predecible, pero rigidiza el cronograma y mezcla capabilities que no son cohesivas dentro de un mismo sprint.
C) **Por feature folder M1–M11** — una unidad por cada módulo (11 unidades). Demasiado fragmentado: la mayoría de stories cruzan varios módulos.
D) **Unidad única (monolito como tal)** — 1 sola unidad cubriendo todo. Mínima estructura pero pierde todo el valor de planificación incremental.
E) Otro (describir después de `[Answer]:`)

[Answer]: A

---

### Pregunta 2 — Granularidad y detalle por unidad en `unit-of-work.md`

¿Qué nivel de detalle queremos en la descripción de cada unidad?

A) **(Recomendada)** **Detalle estándar (~1–1.5 páginas por unidad)** con: **(a)** propósito, **(b)** scope (stories incluidas, RFs cubiertos, componentes que toca), **(c)** entry criteria (qué tiene que existir antes de empezar), **(d)** exit criteria (cómo sabemos que la unidad está terminada), **(e)** sprint(s) sugerido(s) del PRD §13, **(f)** notas técnicas relevantes para Construction. Suficiente para que cualquier equipo arranque la unidad sin volver a leer todo el contexto previo.
B) **Detalle profundo (~3+ páginas por unidad)** con sub-tareas, estimaciones en horas, asignaciones tentativas. Overkill para Inception — eso pertenece a Code Planning en Construction.
C) **Resumen compacto (tabla de 1 fila por unidad)** con solo propósito + stories. Pierde entry/exit criteria, complica activar Construction en el futuro.
D) Otro (describir después de `[Answer]:`)

[Answer]: A

---

### Pregunta 3 — Alineación con sprints del PRD §13

¿Mapeamos explícitamente las unidades a los sprints del PRD §13?

A) **(Recomendada)** **Sí, mapeo explícito como sugerencia** — cada unidad incluye un campo "Sprint sugerido" (puede ser 1 sprint o un rango, p. ej. "Sprint 2–3"). Es **sugerencia**, no compromiso: cuando Construction se active el equipo puede ajustar. Ofrece roadmap concreto y respeta el cronograma del sponsor sin atar el diseño.
B) **No** — las unidades son agnósticas a calendario. Más flexible pero pierde un input valioso del PRD y obliga a re-planificar desde cero cuando Construction arranque.
C) Otro (describir después de `[Answer]:`)

[Answer]: A

---

### Pregunta 4 — Estrategia de sequencing en `unit-of-work-dependency.md`

¿Cómo expresamos las dependencias entre unidades?

A) **(Recomendada)** **Matriz de dependencias + diagrama Mermaid + clasificación por capa** — matriz N×N marcando dependencias directas + diagrama Mermaid de flujo (qué unidad debe terminar antes de cuál) + clasificación en capas (Foundation → Persistence/Domain → Capabilities → Presentation). Hace explícitas las dependencias críticas (p. ej. *Foundation* debe terminar antes que cualquier otra) y permite paralelizar lo que se puede.
B) **Solo lista narrativa de dependencias** (sin matriz ni Mermaid). Más rápido de leer pero menos navegable.
C) **Solo diagrama Mermaid** (sin matriz). Bonito pero pierde la vista tabular útil para auditoría.
D) Otro (describir después de `[Answer]:`)

[Answer]: A

---

### Pregunta 5 — Inclusión de stories cross-cutting (US-23..US-29)

Las stories de calidad y operación (US-23..US-29: auth, password hashing, logging, cifrado, hardening, headers, concurrencia) son transversales. ¿Cómo las asignamos a unidades?

A) **(Recomendada)** **Mayoría en `Foundation & Cross-Cutting`** (auth, headers, exception handler, logging, cifrado, password hashing) + **una (US-29 concurrencia)** distribuida implícitamente entre todas las unidades como criterio NFR. Mantiene cohesión en la unidad de fundación y evita duplicar SECURITY en cada unidad.
B) **Distribuir cada US cross-cutting a la unidad donde tiene más afinidad** (p. ej. US-23 auth en `Auth`, US-25 logging en `Foundation`, US-29 concurrencia en `Dashboard`). Más granular pero rompe la naturaleza cross-cutting de SECURITY.
C) **Crear una unidad dedicada "Quality & Security"** con todas las stories cross-cutting. Da máxima visibilidad pero se construye en paralelo a casi todas las demás — el límite entre "lo cross-cutting" y "lo funcional" puede volverse confuso.
D) Otro (describir después de `[Answer]:`)

[Answer]: A

---

## §4 Después de responder

Cuando termines de contestar las 5 preguntas, escribe **"listo"** o **"completado"** y yo:

1. Analizaré tus respuestas en busca de ambigüedades o contradicciones (Step 7).
2. Si encuentro alguna, crearé preguntas de seguimiento.
3. Si todas son claras, te pediré aprobación explícita del plan (Step 9 del rule file).
4. Tras tu aprobación, generaré los 3 artefactos en la carpeta `application-design/`:
   - `unit-of-work.md`
   - `unit-of-work-dependency.md` (con Mermaid)
   - `unit-of-work-story-map.md`
5. Tras tu aprobación final de los artefactos, **se cerrará la fase de Inception** completa.
