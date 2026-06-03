---
id: "017"
title: "IT8 — NOC Page Improvements (Status, Tendencias, Timing)"
milestone: ms06-stability-degradation
priority: 3
estimate: "0.5d"
status: done
blockedBy: []
blocks: []
parent: null
---

# 017 — IT8 — NOC Page Improvements (Status, Tendencias, Timing)

## Summary
Mejoras NOC: Brand Monitor con SnapshotStatus real, flechas tendencia, M3 con labels descriptivos.

## Acceptance Criteria
- [x] Flechas tendencia visibles (↓↑=)
- [x] M3 muestra Disponible/Con advertencias/Sin respuesta
- [x] ApiStatusBadge Warn en amarillo

## Test Plan
- NOC con Multivende caida → M3 muestra rojo, Multivende en rojo

## Context
- `aidlc-docs/construction/`  

## Definition of Ready
- IT7 completada
- BrandMonitorTable funcional
