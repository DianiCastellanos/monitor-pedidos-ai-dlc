---
id: "018"
title: "IT9 — Graceful Degradation (Exception Boundaries, Fallbacks)"
milestone: ms06-stability-degradation
priority: 2
estimate: "1d"
status: done
blockedBy: []
blocks: []
parent: null
---

# 018 — IT9 — Graceful Degradation (Exception Boundaries, Fallbacks)

## Summary
Ninguna excepcion en checkers o servicios debe tumbar la app. Todos los modulos con fallback a LastCheckStore.

## Acceptance Criteria
- [x] BD caida: Dashboard/NOC cargan con banner, no crash
- [x] Salesforce falla → Warn, no Critical
- [x] JobsChecker timeout → Critical con mensaje claro

## Test Plan
- Playwright RT3: BD caida → M4+M2 Critical, Dashboard carga (3/3 passing)

## Context
- `aidlc-docs/construction/`  

## Definition of Ready
- MVP completado
- Escenarios de fallo identificados en IT9 design
