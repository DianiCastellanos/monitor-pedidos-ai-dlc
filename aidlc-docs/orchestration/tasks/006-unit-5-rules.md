---
id: "006"
title: "U5 — Rules Management (CRUD, History, Conditions)"
milestone: ms02-mvp-core
priority: 2
estimate: "1d"
status: done
blockedBy: ["003"]
blocks: ["007"]
parent: null
---

# 006 — U5 — Rules Management (CRUD, History, Conditions)

## Summary
Motor de reglas configurable: CRUD de reglas, historial de cambios, condiciones por modulo.

## Scope
- Rule entity con RuleCondition
- IRuleRepository + EF Core
- RulesPage.razor, RuleEditPage.razor
- Historial de cambios con autor+razon

## Deliverables
- `MonitorPedidos.Domain/Rules/`\n- `Components/Pages/Rules/`

## Acceptance Criteria
- [x] Reglas editables desde UI
- [x] Historial registra autor y razon
- [x] Condiciones afectan comportamiento M2

## Test Plan
- `dotnet test` → tests de RuleService pasan

## Context
- `aidlc-docs/construction/u5-rules/`

## Definition of Ready
- U3 completada
- Business rules de M2 definidas
