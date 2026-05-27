# NFR Requirements Plan — U5 Rules Management

## Metadata

| Field | Value |
|---|---|
| Stage | Construction → NFR Requirements |
| Unit | U5 — Rules Management |
| Date | 2026-05-23 |
| Sources | u5 functional-design/ (4 artifacts approved), requirements.md RNF-01..RNF-05, RNF-11 |

## Context — NFRs Already Defined

| NFR | Estado | Decisión |
|---|---|---|
| Autorización RulesPage | Definido | [Authorize(Roles="Técnico")] + AuthorizeView en NavMenu (ADR-U5-02) |
| Máximo 1 regla activa por módulo | Definido | Validado en servicio antes de UpsertAsync |
| BR-RULE-01..09 | Definidos | Ver business-rules.md |

---

## Questions

### Q1 — Validación de RuleCondition al guardar

**A) (Recomendada)** Validación en capa de servicio (RuleService) antes de persistir: IsValidForModule(moduleId) debe retornar true; si no, retornar error con mensaje descriptivo. Sin DataAnnotations en entidad de dominio.

**B)** Validación solo en UI (Blazor EditForm) — sin validación server-side.

[Answer]: A — validación en RuleService, server-side *(2026-05-23)*

---

### Q2 — Audit log de cambios a reglas

**A) (Recomendada)** Loggear con Serilog: {UserId} cambió {ModuleId} regla — {cambio}. Sin tabla de auditoría separada en MVP. UpdatedAt en Rule actualizado en cada cambio.

**B)** Tabla rule_audit_log — overhead extra para MVP con 2 usuarios técnicos.

[Answer]: A — Serilog + UpdatedAt, sin tabla de auditoría *(2026-05-23)*

---

### Q3 — Cobertura de tests para U5

**A) (Recomendada)** Unit tests: IsValidForModule para cada módulo (4 tests), activar regla cuando ya existe otra activa → error (1 test), ForBrandMonitor factory (1 test). Total: ~6 unit tests.

**B)** Solo prueba manual desde RulesPage — sin cobertura automatizada.

[Answer]: A — unit tests de validación de dominio *(2026-05-23)*

---

## Execution Checklist

- [x] 1 Analizar artefactos Functional Design de U5.
- [x] 2 Identificar NFRs ya definidos vs decisiones pendientes.
- [x] 3 Crear este plan en aidlc-docs/construction/plans/.
- [x] 4 Recopilar respuestas del owner. *(3/3 = A, A, A — 2026-05-23)*
- [x] 5 Analizar respuestas — 0 ambigüedades detectadas.
- [x] 6 Generar nfr-requirements.md.
- [x] 7 Generar tech-stack-decisions.md.
- [x] 8 Actualizar aidlc-state.md.
- [x] 9 Registrar en audit.md.
- [x] 10 Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-23)*
