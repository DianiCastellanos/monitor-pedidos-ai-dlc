---
id: "016"
title: "IT7 — Salesforce a Brand Monitor Integration"
milestone: ms05-salesforce
priority: 2
estimate: "1d"
status: done
blockedBy: ["014"]
blocks: []
parent: null
---

# 016 — IT7 — Salesforce a Brand Monitor Integration

## Summary
BrandMonitorChecker reemplaza datos simulados por datos reales de Salesforce. Conteos reales por site.

## Acceptance Criteria
- [x] BrandMonitorChecker usa ISalesforceClient real
- [x] Fallo Salesforce → Warn sin snapshots (no Critical)
- [x] 4 sites con conteos reales visibles

## Test Plan
- Brand Monitor muestra conteos no-cero de PatPrimo/SevenSeven/Ostu/Atmos

## Context
- `aidlc-docs/construction/`  

## Definition of Ready
- IT5 completada (Salesforce conectado)
