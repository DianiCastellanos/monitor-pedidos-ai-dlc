# Story Generation Plan — MonitorPedidos AI

**Stage:** Inception → User Stories
**Parte:** 1 (Planning) — este documento
**Profundidad:** Standard (alineada con `requirements.md` v1.1)
**Fuentes:** [`prd.md`](../../../prd.md) v2.3, [`requirements.md`](../requirements/requirements.md) v1.1, [`user-stories-assessment.md`](./user-stories-assessment.md)
**Rol asumido:** Product Owner

---

## Cómo usar este documento

1. Lee cada pregunta de la **§3 Cuestionario estratégico**.
2. Cada pregunta tiene opciones A, B, C, D, etc. Una de ellas está marcada como **(Recomendada)** con la justificación.
3. Responde escribiendo la letra correspondiente después de `[Answer]:`.
4. Si ninguna opción aplica, escoge la última (Otro) y describe tu respuesta.
5. Cuando termines, escribe **"listo"** o **"completado"** para que proceda al análisis de ambigüedades y a la generación de `stories.md` + `personas.md`.

---

## §1 Metodología — Cómo convertiremos requirements en stories

1. **Punto de partida**: PRD v2.3 (2 personas, 7 UCs, 2 user journeys, 6 principios) + requirements.md v1.1 (30 RF, 15 RNF, 6 criterios red-teaming).
2. **Personas**: refinaremos las 2 personas existentes del PRD §3 con atributos accionables (objetivos, contexto operativo, frustraciones, herramientas, métricas de éxito). Si la respuesta a la Pregunta 1 lo indica, podemos añadir personas secundarias (Sponsor, IT/Seguridad) para stakeholder context, sin generar stories propias.
3. **Stories**: formato estándar **"Como [persona], quiero [acción], para [valor]"**, organizadas según el enfoque que decidas en la Pregunta 4.
4. **INVEST**: cada story debe ser Independent, Negotiable, Valuable, Estimable, Small, Testable.
5. **Criterios de aceptación**: formato **Given/When/Then** (Gherkin) para garantizar testeabilidad y compatibilidad con red-teaming.
6. **Trazabilidad**: cada story enlaza a (a) RFs de `requirements.md`, (b) UCs del PRD, (c) escenarios de red-teaming aplicables.

---

## §2 Plan de ejecución (checklist)

> Esta sección la marcaré [x] durante la generación (Parte 2). Tú no necesitas tocarla — solo está para que veas qué se ejecutará tras tu aprobación.

- [x] **2.1** Leer respuestas validadas de §3.
- [x] **2.2** Analizar respuestas para ambigüedades y crear preguntas de seguimiento si aplica.
- [x] **2.3** Esperar aprobación explícita del plan + respuestas finales.
- [x] **2.4** Generar `aidlc-docs/inception/user-stories/personas.md` con N personas refinadas (N = decisión Pregunta 1).
- [x] **2.5** Generar `aidlc-docs/inception/user-stories/stories.md` con stories organizadas según enfoque elegido (Pregunta 4), granularidad elegida (Pregunta 2), y formato de criterios de aceptación elegido (Pregunta 5).
- [x] **2.6** Validar INVEST en todas las stories (auto-check).
- [x] **2.7** Asegurar trazabilidad cruzada: cada story → RFs + UCs + red-teaming.
- [x] **2.8** Actualizar `aidlc-state.md` (User Stories ✅).
- [x] **2.9** Registrar en `audit.md`.
- [x] **2.10** Presentar mensaje de cierre con criterios de revisión. *(Aprobado por el owner: "Aprovar y avanzar")*

---

## §3 Cuestionario estratégico (7 preguntas)

### Pregunta 1 — Alcance de personas

El PRD §3 define **2 personas con stories propias** (Analista operativo, Responsable técnico) más 2 stakeholders (Sponsor, IT/Seguridad). ¿Cuántas personas formalizamos en `personas.md`?

A) **(Recomendada)** **2 personas con stories** (`Operador`/Analista, `Técnico`/Responsable) + **2 stakeholders documentados sin stories** (Sponsor, IT/Seguridad). Refleja la realidad operativa, mantiene foco de stories en quienes usan el dashboard a diario, y deja los stakeholders como referencia para entender prioridades (sponsor) y restricciones (IT/Seguridad).
B) Solo 2 personas (Operador, Técnico), sin documentar Sponsor ni IT/Seguridad. Más compacto, pero pierdes contexto importante para Workflow Planning y demos.
C) 4 personas plenas (Operador, Técnico, Sponsor, IT/Seguridad), todas con stories. Más completo pero infla `stories.md` con stories de stakeholders que probablemente no usen el dashboard día a día.
D) Otro (describir después de `[Answer]:`)

[Answer]: A

---

### Pregunta 2 — Granularidad de las stories

¿Qué tamaño/nivel de detalle queremos para las stories?

A) **(Recomendada)** **Stories pequeñas (1–3 días de trabajo cada una)**, organizadas en epics implícitas por módulo/UC. Ejemplo: en lugar de "Como Técnico quiero editar reglas" → 3 stories: "crear regla", "editar regla", "ver historial de reglas". Mejor para la ventana de 4 semanas: estimable por sprint, demostrable semanalmente, cada story es testable de forma aislada.
B) Stories medianas (3–5 días), una por UC del PRD. Más cerca de los UCs existentes pero menos granular para sprint planning. Ejemplo: una sola story para "edición de reglas" cubriendo CRUD + historial.
C) Epics grandes (1+ semana) con sub-stories. Más estructura jerárquica pero más overhead para un MVP de 4 semanas.
D) Otro (describir después de `[Answer]:`)

[Answer]: A

---

### Pregunta 3 — Formato de la story

¿Qué formato narrativo quieres para las stories?

A) **(Recomendada)** **Formato estándar Connextra**: *"Como [persona], quiero [acción], para [valor]"* + criterios de aceptación. Es el más reconocido por sponsor y stakeholders, lenguaje natural en español, compatible con cualquier herramienta posterior (Jira, Azure DevOps, etc.).
B) Formato job story: *"Cuando [situación], quiero [motivación], para que [resultado esperado]"*. Bueno para outcome-driven design pero menos familiar en el equipo.
C) Formato libre / use case extendido. Más expresivo pero menos consistente.
D) Otro (describir después de `[Answer]:`)

[Answer]: A

---

### Pregunta 4 — Estrategia de organización (breakdown)

¿Cómo agrupamos las stories en `stories.md`?

A) **(Recomendada)** **Híbrido Persona-primero / Feature-secundario**: dos grandes secciones (Operador, Técnico) y dentro de cada una, sub-secciones por capacidad (Monitorear / Detectar / Investigar / Configurar). Refleja cómo cada persona vive el producto y simplifica la planificación de demos semanales (cada demo cubre una persona).
B) Feature-Based puro: secciones por módulo M1–M11. Más cerca de la arquitectura técnica pero rompe la narrativa de usuario.
C) User Journey-Based puro: secciones por UC1–UC7. Útil pero los UCs ya están en el PRD; duplicaría estructura.
D) Persona-Based puro (solo Operador / Técnico): simple pero dentro de cada persona quedaría una lista plana y larga de stories, difícil de demos.
E) Epic-Based (jerárquico): epics → stories → sub-tareas. Demasiado overhead para 4 semanas.
F) Otro (describir después de `[Answer]:`)

[Answer]: A

---

### Pregunta 5 — Criterios de aceptación

¿Qué formato y profundidad de criterios de aceptación para cada story?

A) **(Recomendada)** **Given/When/Then (Gherkin) + 2–4 criterios por story**. Lenguaje natural pero estructurado, testable directamente, alinea con red-teaming. Ejemplo: *Given que el job de Salesforce está apagado, When pasen ≥10 min sin pedidos nuevos, Then el dashboard muestra un incidente CRITICAL con causa probable "job_no_ejecutado"*.
B) Bullet list informal (sin Given/When/Then). Más rápido de escribir pero menos testable.
C) Tabla de input/output esperado. Muy concreto pero rígido para stories de UX.
D) Otro (describir después de `[Answer]:`)

[Answer]: A

---

### Pregunta 6 — Trazabilidad explícita

¿Qué nivel de trazabilidad cruzada queremos en cada story?

A) **(Recomendada)** **Tabla compacta al final de cada story** con tres columnas: `RF` (lista de IDs de requirements.md), `UC` (caso de uso del PRD), `Red-Teaming` (escenario RT aplicable). Permite navegar requirements → stories → tests en ambas direcciones sin inflar el cuerpo de la story.
B) Solo IDs sueltos en el cuerpo de la story. Más ligero pero más difícil de auditar.
C) Sin trazabilidad explícita (el enlace queda implícito por el contenido). Riesgo: en Construction nadie sabrá qué story cubre qué RF.
D) Otro (describir después de `[Answer]:`)

[Answer]: A

---

### Pregunta 7 — Cobertura de aspectos no funcionales

Tenemos 15 RNFs (RNF-01..RNF-15), 13 reglas SECURITY aplicables y 6 escenarios de red-teaming. ¿Cómo los cubrimos en `stories.md`?

A) **(Recomendada)** **Sección dedicada al final de stories.md: "Stories de calidad y operación"**, con 3–5 stories cross-cutting (p. ej., "Como Técnico quiero que las contraseñas se almacenen hasheadas", "Como Operador quiero que el dashboard no exponga stack traces"). Mantiene los aspectos no funcionales visibles y testables sin contaminar las stories funcionales de persona.
B) RNFs incrustados como criterios de aceptación dentro de las stories funcionales. Más cohesivo pero hace stories grandes y mezcla concerns.
C) RNFs solo en `requirements.md`, no en stories. Riesgo SECURITY: las reglas son bloqueantes y sin story se diluyen en Construction.
D) Otro (describir después de `[Answer]:`)

[Answer]: A

---

## §4 Después de responder

Cuando termines de contestar las 7 preguntas, escribe **"listo"** o **"completado"** y yo:

1. Analizaré tus respuestas en busca de ambigüedades o contradicciones (Step 9 del proceso).
2. Si encuentro alguna, crearé un archivo de preguntas de seguimiento (`story-clarification-questions.md`).
3. Si todas las respuestas son claras, presentaré el plan finalizado para tu aprobación explícita.
4. Tras tu aprobación, generaré `personas.md` y `stories.md` según el enfoque elegido (Parte 2).
