# Infrastructure Design Plan — U5 Rules Management

## Metadata

| Field | Value |
|---|---|
| Stage | Construction → Infrastructure Design |
| Unit | U5 — Rules Management |
| Date | 2026-05-23 |
| Sources | u5 nfr-design/ (2 artifacts approved), u2 infrastructure-design/ (migration context) |

## Context — Already Decided

| Aspecto | Decisión |
|---|---|
| Migrations | Manual CLI — dotnet ef database update |
| Seed de reglas | En migration AddBrandSnapshots (U6) — seed de 4 reglas |
| [Authorize] | Doble capa: página + NavMenu (ADR-U5-02) |

---

## Questions

### Q1 — Nombre de la migration de U5

**A) (Recomendada)** AddRulesSchema — crea tablas rules y rule_conditions con foreign key. Se aplica después de AddIncidentSchema (U2).

**B)** AddRulesAndConditions — nombre menos conciso.

[Answer]: A — AddRulesSchema *(2026-05-23)*

---

### Q2 — Seed de reglas: en migration vs al startup

**A) (Recomendada)** Seed en la migration AddBrandSnapshots (U6) que agrega las 4 reglas iniciales con `migrationBuilder.InsertData()`. Las reglas tienen valores por defecto razonables. Idempotente: si ya existen no duplica.

**B)** DatabaseInitializer al startup — puede crear duplicados si se verifica incorrectamente.

[Answer]: A — InsertData en migration AddBrandSnapshots *(2026-05-23)*

---

### Q3 — Endpoint de gestión de reglas

**A) (Recomendada)** RulesPage en Blazor (/reglas) — solo accesible con [Authorize(Roles="Técnico")]. Sin API REST separada. Servidor-side Blazor maneja toda la lógica.

**B)** API REST /api/rules — innecesario para sistema monolítico server-side.

[Answer]: A — RulesPage Blazor, sin API REST *(2026-05-23)*

---

## Execution Checklist

- [x] 1 Analizar artefactos Functional Design y NFR Design de U5.
- [x] 2 Identificar decisiones ya tomadas vs ambigüedades pendientes.
- [x] 3 Crear este plan con 3 preguntas enfocadas.
- [x] 4 Recopilar respuestas del owner. *(3/3 = A, A, A — 2026-05-23)*
- [x] 5 Analizar respuestas — 0 ambigüedades detectadas.
- [x] 6 Generar infrastructure-design.md.
- [x] 7 Generar deployment-architecture.md.
- [x] 8 Actualizar aidlc-state.md.
- [x] 9 Registrar en audit.md.
- [x] 10 Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-23)*
