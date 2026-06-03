---
id: "009"
title: "Build & Test MVP — 50 tests, build 0 errores"
milestone: ms02-mvp-core
priority: 1
estimate: "1d"
status: done
blockedBy: ["008"]
blocks: ["021"]
parent: null
---

# 009 — Build & Test MVP

## Summary
Compilacion y pruebas del MVP completo: 50 tests (45 unit + 5 integration), build 0 errores, 5 migraciones aplicadas, app arranca en localhost:5000.

## Scope
- Compilacion de solucion completa (dotnet build)
- Tests unitarios (45) y de integracion (5)
- Migraciones de base de datos aplicadas
- Verificacion de arranque de la app

## Deliverables
- dotnet build 0 errores
- 50/50 tests passing
- App corriendo en localhost:5000

## Acceptance Criteria
- [x] Build exitoso sin errores
- [x] 45 tests unitarios pasan
- [x] 5 tests de integracion pasan
- [x] App arranca sin errores en localhost:5000

## Test Plan
- `dotnet test MonitorPedidos.UnitTests`
- `dotnet test MonitorPedidos.IntegrationTests`
- `curl http://localhost:5000/Identity/Select` → HTTP 200

## Context
- `aidlc-docs/construction/build-and-test/build-and-test-summary.md`
- `aidlc-docs/construction/build-and-test/unit-test-instructions.md`

## Definition of Ready
- U1-U7 completadas y aprobadas por owner
- Todas las migraciones EF Core escritas
- Tests escritos para todas las unidades
