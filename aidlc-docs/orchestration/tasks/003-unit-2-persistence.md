---
id: "003"
title: "U2 — Persistence & Incidents (DB, Entities, Repositories)"
milestone: ms02-mvp-core
priority: 1
estimate: "1d"
status: done
blockedBy: ["002"]
blocks: ["004","005","006"]
parent: null
---

# 003 — U2 — Persistence & Incidents (DB, Entities, Repositories)

## Summary
Capa de persistencia: entidades de dominio, repositorios EF Core, servicio de incidentes, migraciones.

## Scope
- Incident entity con lifecycle
- IIncidentRepository + EF Core implementation
- AppDbContext con SQL Server
- Migraciones iniciales

## Deliverables
- `MonitorPedidos.Domain/Incidents/`\n- `MonitorPedidos.Infrastructure/Incidents/`\n- Primera migracion aplicada

## Acceptance Criteria
- [x] Incidentes se guardan en BD
- [x] CRUD completo funcional
- [x] Migracion aplicada sin errores

## Test Plan
- `dotnet test` → tests unitarios de IncidentService pasan

## Context
- `aidlc-docs/construction/u2-persistence/`

## Definition of Ready
- U1 completada (002 done)
- SQL Server LocalDB disponible
