# Plan IT2 — M2 Configurable por Reglas

**Fecha:** 2026-05-30  
**Estado:** ✅ Completado

---

## Objetivo

Evolucionar `DbOrderChecker` para que ventana de tiempo, mínimo de pedidos y canales esperados se configuren desde la UI de Reglas en lugar de estar hardcodeados.

## Decisiones clave

| # | Decisión | Opción elegida | Razón |
|---|----------|----------------|-------|
| D1 | Campo `Channels` en `RuleCondition` | `string[]?` nullable | Fallback a `["SALESFORCE","MULTIVENDE"]` cuando null — sin breaking change |
| D2 | Rename `AppliesTo` → `ModuleId` | Con `[Column("AppliesTo")]` en EF Core | Consistencia con el resto del sistema sin migración destructiva |
| D3 | Conteo por canal | `GroupBy(OrderId).ToDictionary()` | `Distinct()` colapsaba N pedidos del mismo canal en 1 — bug corregido |
| D4 | Seed data | Migración `MakeRulesConfigurable` | Pantalla `/rules` siempre tiene al menos una regla activa para M2 |

## Unidades afectadas

U3 (Detection — `DbOrderChecker`), U5 (Rules — `RuleCondition`, `Rule`), U6 (Dashboard — sub-rows por canal)

## Artefactos

- [functional-design/business-logic-model.md](../iteraciones/it2-m2-configurable/functional-design/business-logic-model.md)
