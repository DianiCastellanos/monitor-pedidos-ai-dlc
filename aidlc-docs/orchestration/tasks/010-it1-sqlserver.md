---
id: "010"
title: "IT1 — PostgreSQL a SQL Server + Dual DB"
milestone: ms03-sqlserver-config
priority: 2
estimate: "1d"
status: done
blockedBy: []
blocks: []
parent: null
---

# 010 — IT1 — PostgreSQL a SQL Server + Dual DB

## Summary
Migracion de PostgreSQL a SQL Server LocalDB. Dual DB: AppDb (EF Core R/W) + ProductionDb (Dapper READ-ONLY).

## Acceptance Criteria
- [x] App funciona con SQL Server LocalDB
- [x] ProductionDb conecta a oc_encabezado (solo SELECT)
- [x] Dual DB operativa sin conflictos

## Test Plan
- `dotnet test` → integracion pasa con SQL Server
- ProductionDb: `SELECT TOP 1 * FROM oc_encabezado`

## Context
- `aidlc-docs/construction/`  

## Definition of Ready
- MVP (009) completado
- Acceso a SQL Server de produccion confirmado (READ-ONLY)
