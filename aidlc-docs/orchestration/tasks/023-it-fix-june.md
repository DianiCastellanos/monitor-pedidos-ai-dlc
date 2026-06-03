---
id: "023"
title: "IT-FIX-JUNE — 6 fixes post-VALIDATE"
milestone: ms07-validate-rt
priority: 3
estimate: "0.5d"
status: done
blockedBy: []
blocks: []
parent: null
---

# 023 — IT-FIX-JUNE — 6 fixes post-VALIDATE

## Summary
Seis correcciones detectadas durante VALIDATE: HTTP 401 → SOP-001, startup inmediato, FileShare.ReadWrite, FailureReason M11.

## Acceptance Criteria
- [x] SalesforceApiChecker: HTTP 401 activa BR-TOKEN-01 → SOP-001
- [x] Checkers corren al arrancar sin esperar 5 min
- [x] LogsPage no falla con IOException
- [x] M11 distingue timeout de red vs job deshabilitado

## Test Plan
- `npx playwright test tests/e3*.spec.ts` → 1/1 passing
- Logs al arrancar muestran checkers en primer segundo

## Context
- `aidlc-docs/orchestration/`  

## Definition of Ready
- IT-VALIDATE completada
- Errores E1-E5 identificados en errores-playwright-dashboard.md
