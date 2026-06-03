---
id: "022"
title: "Red-Teaming — 6 escenarios RT del PRD Segmento 11"
milestone: ms07-validate-rt
priority: 2
estimate: "1d"
status: done
blockedBy: ["021"]
blocks: []
parent: null
---

# 022 — Red-Teaming — 6 escenarios RT del PRD Segmento 11

## Summary
Validacion de 6 escenarios del PRD Segmento 11. Todos passing con Playwright automatizado.

## Acceptance Criteria
- [x] RT-Persist: alertas persisten tras reinicio
- [x] RT1: M11 Critical en 22s al deshabilitar job
- [x] RT2: token invalido → Critical + SOP-001
- [x] RT3: BD caida → M4+M2 Critical en <40s
- [x] RT5: panel discrepancias UC6 funciona
- [x] RT7: pedidos cancelados excluidos por OCAPI

## Test Plan
- `npx playwright test tests/rt*.spec.ts` → 13/13 RT tests passing

## Context
- `aidlc-docs/orchestration/`  

## Definition of Ready
- IT-VALIDATE completada (021)
- Acceso a SR-SDEV02CO para RT1
- Credenciales Salesforce para RT2
