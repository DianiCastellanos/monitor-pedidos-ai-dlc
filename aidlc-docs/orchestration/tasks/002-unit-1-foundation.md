---
id: "002"
title: "U1 — Foundation & Cross-Cutting (Auth, Security, Logging)"
milestone: ms02-mvp-core
priority: 1
estimate: "2d"
status: done
blockedBy: []
blocks: ["003"]
parent: null
---

# 002 — U1 — Foundation & Cross-Cutting (Auth, Security, Logging)

## Summary
Foundation del sistema: autenticacion por cookie sin password, logging con Serilog, middleware de seguridad, Data Protection.

## Scope
- Cookie auth (seleccion de identidad sin password)
- Serilog text logging
- GlobalExceptionHandler
- Data Protection con file system
- HTTPS dev-certs

## Deliverables
- `src/MonitorPedidos.Web/` — estructura base
- Program.cs con todos los servicios registrados
- Areas/Identity/ con Select.cshtml

## Acceptance Criteria
- [x] Login funciona con Analista Operativo y Responsable Tecnico
- [x] Logs en formato texto
- [x] HTTPS con dev-cert

## Test Plan
- `dotnet run` → app arranca
- Navegar a / → redirige a /dashboard

## Context
- `aidlc-docs/construction/u1-foundation/`

## Definition of Ready
- Inception aprobada (001 completada)
- Stack .NET 8 + Blazor Server confirmado
