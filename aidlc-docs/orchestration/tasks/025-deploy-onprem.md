---
id: "025"
title: "Deploy on-prem (pendiente decision de negocio)"
milestone: ms08-operations-ready
priority: 4
estimate: "?d"
status: pending
blockedBy: []
blocks: []
parent: null
---

# 025 — Deploy on-prem (pendiente decision de negocio)

## Summary
Despliegue en servidor on-prem de la empresa. Pendiente decision de negocio y definicion de infraestructura.

## Acceptance Criteria
- [ ] App deployada en servidor on-prem
- [ ] Playwright integrado en pipeline CI/CD
- [ ] Runbook de operaciones documentado

## Test Plan
- `curl http://<servidor>:5000/Identity/Select` → HTTP 200

## Context
- `aidlc-docs/orchestration/`  

## Definition of Ready
- Decision de negocio confirmada
- Servidor objetivo identificado
- Docker instalado O IIS configurado
