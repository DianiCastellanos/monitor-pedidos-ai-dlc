---
id: "012"
title: "IT3 — Brand Monitor Redesign + retencion 48h snapshots"
milestone: ms04-brand-dashboard
priority: 2
estimate: "1d"
status: done
blockedBy: []
blocks: []
parent: null
---

# 012 — IT3 — Brand Monitor Redesign + retencion 48h snapshots

## Summary
Rediseno Brand Monitor: append-only snapshots, comparacion historica real, retencion automatica 48h.

## Acceptance Criteria
- [x] Snapshots append-only funcional
- [x] Comparacion historica real con GetSnapshotBeforeAsync
- [x] Retencion 48h: IncidentMaintenanceService purga snapshots viejos

## Test Plan
- Brand Monitor muestra tendencia (flecha ↓↑=) tras 2+ ciclos

## Context
- `aidlc-docs/construction/`  

## Definition of Ready
- IT1 completada
- BrandMonitorChecker diseñado
