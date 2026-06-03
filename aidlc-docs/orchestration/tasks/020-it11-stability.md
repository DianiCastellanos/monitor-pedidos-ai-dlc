---
id: "020"
title: "IT11 — Dashboard Stability & M3 Per-API Detail"
milestone: ms06-stability-degradation
priority: 3
estimate: "0.5d"
status: done
blockedBy: []
blocks: ["021"]
parent: null
---

# 020 — IT11 — Dashboard Stability & M3 Per-API Detail

## Summary
Dashboard estable: refresh no-destructivo Brand Monitor, M3 con detalle por integracion (Salesforce/Multivende).

## Acceptance Criteria
- [x] Tabla Brand Monitor no desaparece durante refresh
- [x] M3 muestra detalle individual por API
- [x] Escalate-only preservado en ApplyApisWorstStatus

## Test Plan
- Playwright D4: refresh no destruye tabla (passing)

## Context
- `aidlc-docs/construction/`  

## Definition of Ready
- IT10 completada
- Brand Monitor disenado con refresh no-destructivo
