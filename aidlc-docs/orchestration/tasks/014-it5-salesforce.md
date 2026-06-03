---
id: "014"
title: "IT5 — Salesforce Order Monitor (OCAPI, Multi-site, OAuth2)"
milestone: ms05-salesforce
priority: 2
estimate: "1d"
status: done
blockedBy: []
blocks: ["016"]
parent: null
---

# 014 — IT5 — Salesforce Order Monitor (OCAPI, Multi-site, OAuth2)

## Summary
Monitor real de pedidos en Salesforce Commerce Cloud: OCAPI order_search, 4 marcas Colombia, OAuth2 S2S.

## Acceptance Criteria
- [x] Salesforce conecta con OAuth2 real (account.demandware.com)
- [x] 4 sites consultados: PatPrimo, SevenSeven, Ostu, Atmos
- [x] M3 muestra detalle por site en Dashboard

## Test Plan
- Logs muestran `[SalesforceClient] PatPrimo — total=X hits=X`

## Context
- `aidlc-docs/construction/`  

## Definition of Ready
- ClientId y ClientPassword en .env
- Acceso a Salesforce OCAPI confirmado
