---
id: "007"
title: "U6 — Dashboard & Real-Time (Blazor UI, NOC, Brand Monitor)"
milestone: ms02-mvp-core
priority: 1
estimate: "2d"
status: done
blockedBy: ["004","005","006"]
blocks: ["008"]
parent: null
---

# 007 — U6 — Dashboard & Real-Time (Blazor UI, NOC, Brand Monitor)

## Summary
Dashboard principal con auto-refresh, modo NOC, Brand Monitor, historial de incidentes y vista semanal.

## Scope
- Dashboard.razor con timers (30s cards, 60s brand)
- NocPage.razor con layout oscuro
- IncidentHistoricPage, WeeklySummaryPage
- Brand Monitor tabla con snapshots

## Deliverables
- `Components/Pages/Dashboard.razor`\n- `Components/Pages/Noc/`

## Acceptance Criteria
- [x] Dashboard carga en <3s
- [x] Auto-refresh funcional
- [x] NOC modo pantalla completa
- [x] Brand Monitor muestra 4 sites

## Test Plan
- Playwright T1-T5 (NOC) y D1-D4 (Dashboard)

## Context
- `aidlc-docs/construction/u6-dashboard-realtime/`

## Definition of Ready
- U3, U4, U5 completadas
- Diseño de UI aprobado
