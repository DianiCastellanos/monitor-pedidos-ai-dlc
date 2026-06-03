---
id: "008"
title: "U7 — Simulation & Red-Teaming (Simulator, RT Framework)"
milestone: ms02-mvp-core
priority: 2
estimate: "1d"
status: done
blockedBy: ["007"]
blocks: ["009"]
parent: null
---

# 008 — U7 — Simulation & Red-Teaming (Simulator, RT Framework)

## Summary
Simulador de pedidos para desarrollo/testing y framework de red-teaming con plantilla de escenarios.

## Scope
- OrdersSimulatorService (BackgroundService)
- SimulatedOrderRepository, SimulatedJobStatusRepository
- red-team-report-template.md con 6 escenarios

## Deliverables
- `MonitorPedidos.Infrastructure/Simulation/`\n- `aidlc-docs/construction/u7-simulation/red-team-report-template.md`

## Acceptance Criteria
- [x] Simulador genera pedidos en dev
- [x] Plantilla RT con 6 escenarios
- [x] Simulation:Enabled=false en prod

## Test Plan
- App con Simulation:Enabled=true → pedidos aparecen en M2

## Context
- `aidlc-docs/construction/u7-simulation/`

## Definition of Ready
- U6 completada
- Escenarios RT del PRD Segmento 11 definidos
