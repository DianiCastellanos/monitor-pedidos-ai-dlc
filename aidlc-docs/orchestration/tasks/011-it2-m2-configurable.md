---
id: "011"
title: "IT2 — M2 Configurable por Reglas (Channels, MinOrders)"
milestone: ms03-sqlserver-config
priority: 2
estimate: "1d"
status: done
blockedBy: []
blocks: []
parent: null
---

# 011 — IT2 — M2 Configurable por Reglas (Channels, MinOrders)

## Summary
DbOrderChecker configurable mediante reglas activas: canales (SALESFORCE/MULTIVENDE) y minimo de pedidos.

## Acceptance Criteria
- [x] M2 respeta canales configurados en la regla activa
- [x] Seed MakeRulesConfigurable aplicado
- [x] UI permite cambiar MinOrders y Channels

## Test Plan
- Cambiar regla en UI → M2 refleja nuevo umbral en proximo ciclo

## Context
- `aidlc-docs/construction/`  

## Definition of Ready
- IT1 completada
- Reglas base en base de datos
