---
id: "019"
title: "IT10 — Brand Monitor Live Fallback + M2 UX"
milestone: ms06-stability-degradation
priority: 3
estimate: "0.5d"
status: done
blockedBy: []
blocks: []
parent: null
---

# 019 — IT10 — Brand Monitor Live Fallback + M2 UX

## Summary
Sin BD, Brand Monitor muestra datos en tiempo real de Salesforce. M2 muestra detalle del checker.

## Acceptance Criteria
- [x] Live fallback funcional: banner amarillo con datos reales
- [x] M2 muestra detalle en rojo cuando Critical
- [x] NocPage tiene fallback a GetLiveCountsAsync

## Test Plan
- Desconectar BD → Brand Monitor muestra conteos Salesforce con banner

## Context
- `aidlc-docs/construction/`  

## Definition of Ready
- IT9 completada
- GetLiveCountsAsync disenado
