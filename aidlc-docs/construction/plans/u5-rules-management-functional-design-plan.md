# Functional Design Plan — U5 Rules Management

## Metadata

| Field | Value |
|---|---|
| Stage | Construction → Functional Design |
| Unit | U5 — Rules Management |
| Date | 2026-05-23 |
| Sources | unit-of-work.md §U5, requirements.md §RF-13..RF-17, components.md §RulesPage |

## Context — Decisions Already Taken

- Rule aggregate con RuleCondition (uno-a-uno)
- ModuleId enum: DbOrders=1, DbHealth=2, Jobs=3, BrandMonitor=4 (extendido en U5)
- Solo rol Técnico puede gestionar reglas (RF-14)
- Máximo 1 regla activa por módulo (BR-RULE-02)

---

## Questions

### Q1 — Rule aggregate design

**A) (Recomendada)** Rule(Id, ModuleId, IsActive, CreatedAt, UpdatedAt) + RuleCondition(Id, RuleId, ThresholdMinutes?, MaxRetries?, MaxFailures?, HealthCheckInterval?, PendingDropThreshold?). Relación 1:1. Condición especializada por módulo via IsValidForModule(moduleId). Soft delete con IsActive.

**B)** Tabla polimórfica con discriminator — más complejo sin beneficio para 4 módulos.

[Answer]: A — Rule + RuleCondition con campos nullable por módulo *(2026-05-23)*

---

### Q2 — RuleCondition: campo PendingDropThreshold para BrandMonitor

**A) (Recomendada)** `int? PendingDropThreshold` en RuleCondition. Factory `ForBrandMonitor(int threshold)`. `IsValidForModule(BrandMonitor) => PendingDropThreshold.HasValue`. Seed: PendingDropThreshold=5, IsActive=true en migration AddBrandSnapshots.

**B)** Tabla separada BrandMonitorRuleCondition — sobre-normalización para un campo.

[Answer]: A — PendingDropThreshold en RuleCondition con nullable *(2026-05-23)*

---

### Q3 — IRuleRepository interface

**A) (Recomendada)** `GetActiveByModuleAsync(ModuleId)`, `GetAllAsync()`, `AddAsync(Rule)`, `UpdateAsync(Rule)`. Sin delete (soft delete con IsActive=false). BrandMonitorChecker llama GetActiveByModuleAsync(BrandMonitor) para obtener threshold.

**B)** Interface CRUD completa con delete real — las reglas son configuración, no deben borrarse.

[Answer]: A — IRuleRepository sin delete real, con GetActiveByModuleAsync *(2026-05-23)*

---

### Q4 — RulesPage: UX de activación/desactivación

**A) (Recomendada)** Tabla con todas las reglas + toggle IsActive por fila + botón Editar condición. Solo Técnico accede. AlertMessage para confirmación antes de desactivar una regla activa.

**B)** Formulario CRUD separado por módulo — más pantallas, menos eficiente.

[Answer]: A — tabla con toggle + edición inline *(2026-05-23)*

---

### Q5 — Seed data de reglas iniciales

**A) (Recomendada)** Migration AddBrandSnapshots (U6) incluye seed: 4 reglas iniciales — DbOrders (ThresholdMinutes=30), DbHealth (MaxFailures=3), Jobs (MaxFailures=2), BrandMonitor (PendingDropThreshold=5). Todas IsActive=true. Configurable desde RulesPage.

**B)** Seed en DatabaseInitializer al startup — crea reglas duplicadas en cada reinicio si no se verifica.

[Answer]: A — seed en migration, idempotente *(2026-05-23)*

---

## Execution Checklist

- [x] 1 Leer artefactos Inception (unit-of-work.md §U5, requirements.md RF-13..RF-17, components.md).
- [x] 2 Identificar scope de U5 y preparar preguntas contextuales.
- [x] 3 Crear este plan en aidlc-docs/construction/plans/.
- [x] 4 Recopilar respuestas del owner. *(5/5 = A, A, A, A, A — 2026-05-23)*
- [x] 5 Analizar respuestas — 0 ambigüedades detectadas.
- [x] 6 Generar domain-entities.md (v1.0; v1.1 agrega PendingDropThreshold + BrandMonitor=4 — 2026-05-24).
- [x] 7 Generar business-logic-model.md (v1.0; v1.1 agrega flujo §7 BrandMonitorChecker — 2026-05-24).
- [x] 8 Generar business-rules.md (v1.0; v1.1 agrega BR-COND-04, BR-SEED-03, BR-RULE-09 — 2026-05-24).
- [x] 9 Generar frontend-components.md.
- [x] 10 Actualizar aidlc-state.md.
- [x] 11 Registrar en audit.md.
- [x] 12 Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-23)*
