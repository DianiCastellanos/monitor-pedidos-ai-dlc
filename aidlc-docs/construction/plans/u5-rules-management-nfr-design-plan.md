# NFR Design Plan — U5 Rules Management

## Metadata

| Field | Value |
|---|---|
| Stage | Construction → NFR Design |
| Unit | U5 — Rules Management |
| Date | 2026-05-23 |
| Sources | u5 nfr-requirements/ (2 artifacts approved), u5 functional-design/domain-entities.md |

## Context — Patterns Already Determined

| Patrón | Decisión |
|---|---|
| IRuleRepository Scoped | EF Core directo, sin Unit of Work adicional |
| [Authorize(Roles="Técnico")] | ADR-U5-02 — doble capa: page + NavMenu |
| RuleCondition nullable fields | Campo nullable por módulo, IsValidForModule valida |

---

## Questions

### Q1 — Implementación de GetActiveByModuleAsync

**A) (Recomendada)** `context.Rules.Include(r => r.Condition).Where(r => r.Module == moduleId && r.IsActive).FirstOrDefaultAsync(ct)`. Si no hay regla activa → retorna null → BrandMonitorChecker usa threshold=0 como fallback (BR-RULE-09).

**B)** Cacheo en memoria del resultado — el threshold puede cambiar desde RulesPage en cualquier momento.

[Answer]: A — query directa a BD, sin caché *(2026-05-23)*

---

### Q2 — Registro DI de IRuleRepository

**A) (Recomendada)** `services.AddScoped<IRuleRepository, RuleRepository>()`. Scoped compatible con AppDbContext (también Scoped). BrandMonitorChecker (Singleton) accede via IServiceScopeFactory (ADR-U5-01).

**B)** Singleton — captura DbContext en Singleton → excepción en runtime.

[Answer]: A — Scoped, IServiceScopeFactory para acceso desde Singleton *(2026-05-23)*

---

## Execution Checklist

- [x] 1 Analizar artefactos NFR Requirements de U5.
- [x] 2 Identificar patrones ya determinados vs decisiones pendientes.
- [x] 3 Crear este plan con 2 preguntas.
- [x] 4 Recopilar respuestas del owner. *(2/2 = A, A — 2026-05-23)*
- [x] 5 Analizar respuestas — 0 ambigüedades detectadas.
- [x] 6 Generar nfr-design-patterns.md.
- [x] 7 Generar logical-components.md.
- [x] 8 Actualizar aidlc-state.md.
- [x] 9 Registrar en audit.md.
- [x] 10 Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-23)*
