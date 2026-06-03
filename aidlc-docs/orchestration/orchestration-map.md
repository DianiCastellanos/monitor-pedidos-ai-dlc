# Orchestration Map — MonitorPedidos AI

## Architecture Overview

```mermaid
graph TD
    subgraph "Estación 5-6: UI + Brand Monitor"
        U6["U6: Dashboard & NOC"]
        IT3["IT3: Brand Monitor Redesign"]
        IT4["IT4: Dashboard Improvements"]
        IT6["IT6: UI Navigation & NOC Mode"]
        IT7["IT7: Salesforce→Brand Monitor"]
        IT8["IT8: NOC Page Improvements"]
        IT10["IT10: Brand Monitor Live Fallback"]
        IT11["IT11: Dashboard Stability"]
    end

    subgraph "Estación 4: Backend"
        U1["U1: Foundation"]
        U2["U2: Persistence & Incidents"]
        U3["U3: Detection & Classification"]
        U4["U4: External Integrations"]
        U5["U5: Rules Management"]
        U7["U7: Simulation & Red-Teaming"]
    end

    subgraph "Estación 3: Integraciones"
        IT1["IT1: PostgreSQL→SQL Server"]
        IT2["IT2: M2 Configurable"]
        IT5["IT5: Salesforce Order Monitor"]
        IT9["IT9: Graceful Degradation"]
    end

    subgraph "Estación 7: Testing+Ops"
        IT_VALIDATE["IT-VALIDATE: Playwright E2E"]
        RT["Red-Teaming (6 escenarios)"]
        IT_FIX["IT-FIX-JUNE: 6 fixes"]
        DEPLOY["Deploy on-prem (PENDING)"]
    end

    subgraph "Documentación"
        STATE["aidlc-state.md"]
        ORCH["orchestration/"]
        OPS["operations/"]
    end

    U1 --> U2 --> U3 --> U4 --> U5 --> U6 --> U7
    U6 --> IT3 --> IT4 --> IT6
    U4 --> IT1 --> IT2 --> IT5
    U3 --> IT9
    U7 --> IT_VALIDATE --> RT --> IT_FIX
    IT_VALIDATE --> STATE
    RT --> ORCH
    IT_FIX --> DEPLOY
```

## Milestone Progress

| Milestone | Tasks | Status |
|-----------|-------|--------|
| MS1: Inception | 001-002 | ✅ COMPLETED |
| MS2: MVP Core | 003-009 | ✅ COMPLETED |
| MS3: SQL Server + Config | 010-011 | ✅ COMPLETED |
| MS4: Brand + Dashboard | 012-013 | ✅ COMPLETED |
| MS5: Salesforce | 014-016 | ✅ COMPLETED |
| MS6: Stability | 017-020 | ✅ COMPLETED |
| MS7: Validate + Red-Team | 021-023 | ✅ COMPLETED |
| MS8: Operations Ready | 024-025 | 🟡 IN PROGRESS |

## File Map
```
aidlc-docs/orchestration/
├── task-package.yaml      # 25 tareas, 8 milestones
├── milestones.md          # MS1-MS8 detalles
├── harness-ficha.md       # OpenCode capabilities
├── orchestration-map.md   # This file
├── linear-publish.yaml    # Linear publishing config
└── tasks/
    ├── 001-inception.md
    ├── 002-unit-1-foundation.md
    ├── 003-unit-2-persistence.md
    ├── 004-unit-3-detection.md
    ├── 005-unit-4-integrations.md
    ├── 006-unit-5-rules.md
    ├── 007-unit-6-dashboard.md
    ├── 008-unit-7-simulation.md
    ├── 009-build-and-test-mvp.md
    ├── 010-it1-sqlserver.md
    ├── 011-it2-m2-configurable.md
    ├── 012-it3-brand-monitor.md
    ├── 013-it4-dashboard.md
    ├── 014-it5-salesforce.md
    ├── 015-it6-ui-navigation.md
    ├── 016-it7-brand-sf.md
    ├── 017-it8-noc.md
    ├── 018-it9-graceful.md
    ├── 019-it10-live-fallback.md
    ├── 020-it11-stability.md
    ├── 021-validate-playwright.md
    ├── 022-red-teaming.md
    ├── 023-it-fix-june.md
    ├── 024-orchestration-docs.md
    └── 025-deploy-onprem.md
```
