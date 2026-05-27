# User Stories Assessment — MonitorPedidos AI

**Fecha:** 2026-05-20
**Stage:** Inception → User Stories (Part 1 — Planning)
**Documento de salida:** este archivo + `story-generation-plan.md`
**Fuente:** [`prd.md`](../../../prd.md) v2.3 + [`requirements.md`](../requirements/requirements.md) v1.1

---

## 1. Request Analysis

| Atributo | Valor |
|----------|-------|
| **Original Request** | Construir MonitorPedidos AI — detector proactivo de fallas en la descarga de pedidos (Salesforce/Multivende → SQL Server). MVP de 4 semanas, dashboard interno, sin exposición a internet. |
| **User Impact** | **Directo y alto.** El sistema es la nueva herramienta diaria del analista operativo (reduce 25h/mes → <10h/mes) y la herramienta de escalamiento del responsable técnico. |
| **Complexity Level** | **Moderate-High** — 11 módulos, 7 casos de uso, 2 roles diferenciados, 6 escenarios de red-teaming, ventana de 4 semanas. |
| **Stakeholders** | Analista operativo, Responsable técnico, Sponsor (Alex Cárdenas), IT/Seguridad. |

---

## 2. Assessment Criteria Met

### Criterios de Alta Prioridad (ejecutar siempre)

- [x] **New User Features** — toda la funcionalidad del MVP es nueva para los dos roles.
- [x] **Multi-Persona Systems** — 2 personas distintas con flujos diferenciados (`Operador` y `Técnico`).
- [x] **Complex Business Logic** — 6 categorías de causa raíz, cascada de clasificación, lógica de reintentos condicional (solo 5xx/timeout), reglas estáticas editables con historial, cierre automático vs manual.
- [x] **Cross-Team Projects** — sponsor + IT/Seguridad + analista + técnico necesitan alineación compartida.

### Criterios de Prioridad Media (asegurados por complejidad)

- [x] **Backend User Impact** — chequeos automáticos invisibles que disparan alertas visibles.
- [x] **Security Enhancements** — autenticación + roles + 13 reglas SECURITY aplicables.
- [x] **Multiple Touchpoints** — dashboard (4 vistas distintas: real-time, histórico, semanal, panel de discrepancias) + notificaciones de navegador + edición de reglas + cierre de incidentes.

### Skip Criteria — NO aplica ningún descarte

- ❌ No es pure refactoring.
- ❌ No es isolated bug fix.
- ❌ No es infrastructure-only.
- ❌ No es developer tooling.
- ❌ No es documentation.

---

## 3. Decision

**Execute User Stories:** ✅ **Sí**

**Reasoning:**

1. El proyecto cumple **4 de 6** indicadores de "ALWAYS Execute" — el caso es claro.
2. El PRD ya identifica las 2 personas y los 7 UCs, pero **no formaliza criterios de aceptación testeables por story** — eso es exactamente lo que User Stories aporta.
3. Las 4 semanas de cronograma exigen estimación clara y trazabilidad — INVEST (Independent, Negotiable, Valuable, Estimable, Small, Testable) reduce el riesgo R4 (Cronograma).
4. Los **6 escenarios de red-teaming** son criterios de aceptación a nivel sistema; las user stories permiten descomponerlos en criterios accionables por sprint.
5. Sponsor refuerza adopción (mitigación R2): las stories son el lenguaje natural para las **demos semanales** previstas en R2 mitigation.

---

## 4. Expected Outcomes

| Beneficio | Cómo lo aporta User Stories |
|-----------|------------------------------|
| **Claridad por persona** | Stories diferenciadas para `Operador` vs `Técnico` (cubren intersección y diferencias). |
| **Trazabilidad RF → Story → Test** | Cada story enlaza a sus RF y a uno o más criterios de red-teaming. |
| **Estimación por sprint** | Stories pequeñas y testables permiten distribuir trabajo en los 4 sprints del §13. |
| **Alineación con sponsor** | Stories en lenguaje natural sirven como agenda de demos semanales. |
| **Calibración de scope (R4)** | Should-Have y Could-Have explícitas por story facilitan recortes informados. |

---

## 5. Próximo paso

→ Generar `aidlc-docs/inception/plans/story-generation-plan.md` con plan de ejecución y preguntas estratégicas para definir el enfoque de stories.
