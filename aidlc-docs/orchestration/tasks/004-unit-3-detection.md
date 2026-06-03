---
id: "004"
title: "U3 — Detection & Classification (Checkers, Scheduler)"
milestone: ms02-mvp-core
priority: 1
estimate: "1d"
status: done
blockedBy: ["003"]
blocks: ["007"]
parent: null
---

# 004 — U3 — Detection & Classification (Checkers, Scheduler)

## Summary
Checkers de monitoreo (M2/M4/M11) y scheduler de tareas periodicas. Clasificador de causa raiz.

## Scope
- DbOrderChecker, DbHealthChecker, JobsChecker
- MonitoringSchedulerService (PeriodicTimer)
- CauseClassifier
- LastCheckStore (ConcurrentDictionary)

## Deliverables
- `MonitorPedidos.Web/Features/Monitoring/`\n- `MonitorPedidos.Web/BackgroundServices/`

## Acceptance Criteria
- [x] Checkers reportan estado correcto
- [x] Scheduler corre sin crash
- [x] CauseClassifier asigna causa correcta

## Test Plan
- `dotnet test` → tests de checkers pasan

## Context
- `aidlc-docs/construction/u3-detection/`

## Definition of Ready
- U2 completada
- Reglas de negocio definidas en functional-design
