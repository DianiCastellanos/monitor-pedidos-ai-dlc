# NFR Design Plan — U2 Persistence & Domain

**Stage:** Construction → NFR Design
**Unidad:** U2 — Persistence & Domain
**Parte:** 1 (Planning) — este documento
**Fecha:** 2026-05-23
**Fuentes:**
- U2 `nfr-requirements/` (2 artefactos aprobados)
- U2 `functional-design/domain-entities.md`

---

## Cómo usar este documento

1. Lee cada pregunta de la **§3 Cuestionario de NFR Design**.
2. Cada pregunta tiene opciones A, B. Una está marcada como **(Recomendada)**.
3. Responde escribiendo la letra correspondiente después de `[Answer]:`.
4. Cuando termines, escribe **"listo"** para que proceda a generar los artefactos de NFR Design.

---

## §1 Foco de U2 — NFR Design

Con los NFR Requirements aprobados, este stage traduce los requisitos no funcionales de U2 en **patrones de diseño concretos**: cómo se implementa el repositorio, cómo se estructura la configuración EF Core, y cómo se gestionan transacciones y índices.

### Artefactos que generaremos al cerrar este stage

| Artefacto | Contenido |
|-----------|-----------|
| `nfr-design-patterns.md` | Patrones aplicados: Repository, Owned Entity, Transaction scope, índices |
| `logical-components.md` | Diagrama de componentes lógicos de U2: entidades, repositorios, DbContext |

---

## §2 Contexto — Patrones ya determinados

Los siguientes patrones ya están determinados desde NFR Requirements de U2:

| Patrón | Decisión |
|--------|----------|
| EF Core `OwnsOne` para `AlertMessage` | NFR-U2-01 |
| Transacciones en `CreateAsync` | NFR-U2-02 |
| Índices `(status, occurred_at)`, `(module, occurred_at)` | NFR-U2-01 |

---

## §3 Cuestionario de NFR Design (2 preguntas)

---

### Pregunta 1 — Patrón de repositorio para Incident

¿Cómo implementamos el acceso a datos para el aggregate `Incident`?

**A) (Recomendada) `IIncidentRepository` implementado con EF Core directo** — `AppDbContext` inyectado directamente en la implementación del repositorio. Queries LINQ directas sin abstracción genérica adicional. Sin Unit of Work adicional (EF Core ya implementa el patrón UoW vía `DbContext`). Registrado como `Scoped` per request.

**B) Repositorio genérico con `IRepository<T>`** — abstracción extra innecesaria para MVP con una sola entidad compleja. Añade indirección sin beneficio real para el scope de U2.

[Answer]: A — `IIncidentRepository` directo sin Unit of Work adicional *(2026-05-23)*

---

### Pregunta 2 — Configuración de entidades EF Core

¿Cómo configuramos el mapeo de `Incident` y `AlertMessage` en EF Core?

**A) (Recomendada) Fluent API en `IEntityTypeConfiguration<T>` separada por entidad** — `IncidentConfiguration.cs` configura: nombre de tabla (`incidents`), `OwnsOne` de `AlertMessage` con nombres de columna explícitos, índices compuestos, columnas nullable para `ResolvedAt` y `AcknowledgedAt`. Más mantenible que DataAnnotations para configuración compleja. Registrado en `AppDbContext.OnModelCreating` con `modelBuilder.ApplyConfigurationsFromAssembly(...)`.

**B) DataAnnotations en las entidades** — mezcla concerns de UI/persistencia con el modelo de dominio. Viola la separación de responsabilidades; las entidades de dominio no deben conocer detalles de mapeo de BD.

[Answer]: A — Fluent API en `IEntityTypeConfiguration` separado *(2026-05-23)*

---

## §4 Después de responder

Cuando completes las 2 preguntas, escribe **"listo"** y procederé a:

1. Analizar respuestas para ambigüedades.
2. Si todo es claro, generaré los 2 artefactos en:
   ```
   aidlc-docs/construction/u2-persistence-domain/nfr-design/
   ├── nfr-design-patterns.md
   └── logical-components.md
   ```

---

## §5 Plan de ejecución (checklist)

- [x] **5.1** Analizar artefactos NFR Requirements de U2.
- [x] **5.2** Identificar patrones ya determinados vs decisiones pendientes.
- [x] **5.3** Crear este plan con 2 preguntas.
- [x] **5.4** Recopilar respuestas del owner. *(2/2 = A, A — 2026-05-23)*
- [x] **5.5** Analizar respuestas — 0 ambigüedades detectadas.
- [x] **5.6** Generar `nfr-design-patterns.md`.
- [x] **5.7** Generar `logical-components.md`.
- [x] **5.8** Actualizar `aidlc-state.md`.
- [x] **5.9** Registrar en `audit.md`.
- [x] **5.10** Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-23)*
