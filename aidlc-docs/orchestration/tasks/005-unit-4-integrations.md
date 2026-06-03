---
id: "005"
title: "U4 — External Integrations (Salesforce, Multivende Clients)"
milestone: ms02-mvp-core
priority: 1
estimate: "1d"
status: done
blockedBy: ["003"]
blocks: ["007"]
parent: null
---

# 005 — U4 — External Integrations (Salesforce, Multivende Clients)

## Summary
Clientes HTTP para Salesforce OCAPI y Multivende API con reintentos Polly.

## Scope
- ISalesforceClient con OAuth2 dinamico
- IMultivendeClient con retry
- SalesforceTokenCache (Singleton)
- DelegatingHandler para auth

## Deliverables
- `MonitorPedidos.Web/Features/ApiChecks/`

## Acceptance Criteria
- [x] Salesforce responde con token valido
- [x] Multivende conecta
- [x] Reintentos funcionan en 5xx

## Test Plan
- Tests de integracion con mocks HTTP

## Context
- `aidlc-docs/construction/u4-integrations/`

## Definition of Ready
- Credenciales Salesforce en .env
- U3 completada
