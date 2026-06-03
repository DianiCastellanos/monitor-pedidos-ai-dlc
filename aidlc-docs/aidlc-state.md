# AI-DLC State Tracking

## Project Information
- **Project Name**: MonitorPedidos AI
- **Company**: Manufacturas Eliot
- **Sponsor**: Alex Cárdenas (Jefe de Análisis de Sistemas)
- **Owner**: Diana Castellanos
- **Project Type**: Greenfield
- **Start Date**: 2026-05-20T00:00:00Z
- **Current Phase**: 🏁 **INCEPTION — CLOSED (2026-05-22)**
- **Current Stage**: ✅ All Inception stages completed and approved by owner
- **Next Stage**: Construction (OUT OF SCOPE per owner instruction — requires explicit new iteration to activate)

## Workspace State
- **Existing Code**: No
- **Reverse Engineering Needed**: No
- **Workspace Root**: C:/Users/dcastellanos/Estacion4/PRD_AI_DLC/MonitorPedidos/
- **Reference Document**: prd.md (PRD v2.3, fecha 2026-05-20 — actualizado durante mantenimiento; cambios principales: contexto de despliegue interno + aclaración modo NOC)
- **Deployment Context (v1.1, 2026-05-20)**: **localhost o red interna del equipo**. SIN exposición pública a internet. SIN ngrok, túneles ni dominios públicos durante el MVP. Demo se ejecuta en el entorno local del owner o de la red de la empresa.

## Code Location Rules
- **Application Code**: Workspace root (NEVER in aidlc-docs/)
- **Documentation**: aidlc-docs/ only
- **Structure patterns**: See code-generation.md Critical Rules

## Extension Configuration
| Extension | Enabled | Decided At | Notes |
|-----------|---------|------------|-------|
| security/baseline | **Yes** | Requirements Analysis (2026-05-20) | Aplicable y bloqueante. Reglas N/A en MVP: SECURITY-02 (sin LB/API GW/CDN — despliegue local/red interna), SECURITY-07 (sin exposición pública; sistema corre en localhost o red interna del equipo). Resto aplica. Reevaluar SECURITY-02 y SECURITY-07 al pasar a on-prem post-MVP. |

## Execution Plan Summary
- **Total Stages Inception ejecutados**: 6 (Workspace Detection, Requirements Analysis, User Stories, Workflow Planning, Application Design, Units Generation)
- **Stages Inception saltados**: 1 (Reverse Engineering — N/A para greenfield)
- **Phases saltados**: Construction completa + Operations (Construction OUT OF SCOPE por decisión del owner; Operations es placeholder del framework)
- **Profundidad usada en todos los stages condicionales**: Standard
- **Detalle completo**: ver [`inception/plans/execution-plan.md`](inception/plans/execution-plan.md)

## Current Status
- **Lifecycle Phase**: 🟡 **OPERATIONS** — activa desde 2026-06-01
- **Current Stage**: VALIDATE completado ✅ + Orchestración documentada ✅
- **Next Stage**: Publicar tareas en Linear / Deploy on-prem
- **Status**: ✅ 24/24 tests Playwright passing. 6/6 RT del PRD Segmento 11 validados. 5 reglas en DB (BrandMonitor, DbOrderChecker, SalesforceApi, DbHealthChecker, JobsMonitor). 11 iteraciones post-MVP completadas (IT1-IT11). 6 fixes de código aplicados post-VALIDATE. Orquestación documentada en aidlc-docs/orchestration/ (25 tasks, 8 milestones). Todos los Must Have del PRD implementados y verificados.

## Stage Progress

### INCEPTION Phase
- [x] Workspace Detection — 2026-05-20 (Greenfield confirmed)
- [x] Reverse Engineering — SKIPPED (Greenfield)
- [x] Requirements Analysis — 2026-05-20 (Standard depth; 13/13 preguntas respondidas; SECURITY enabled; requirements.md v1.1 con revisión de contexto de despliegue — sin exposición pública; aprobado implícitamente al avanzar a User Stories)
- [x] User Stories — 2026-05-21 (Standard depth; 7/7 decisiones del plan = A; 29 stories generadas — 14 Operador + 8 Técnico + 7 cross-cutting; cobertura 30/30 RF, 7/7 UC, 6/6 RT; aprobado por owner)
- [x] Workflow Planning — 2026-05-21 (execution-plan.md aprobado por owner; Application Design + Units Generation EXECUTE; Construction OUT OF SCOPE)
- [x] Application Design — 2026-05-21 (Standard depth; 6/6 decisiones del plan = A; 4 artefactos generados — components.md, component-methods.md, services.md, component-dependency.md; cobertura completa 29/29 stories; aprobado por owner)
- [x] Units Generation — 2026-05-22 (Standard depth; 5/5 decisiones del plan = A; 3 artefactos generados; 7 unidades descompuestas por capability; cobertura 29/29 stories, 30/30 RFs, 7/7 UCs, 6/6 RTs; DAG acíclico; **aprobado por owner — Inception CLOSED**)

### CONSTRUCTION Phase
- **Activada**: 2026-05-23 por instrucción explícita del owner ("arranquemos con la U1")
- **Unidad actual**: U1 — Foundation & Cross-Cutting
- [x] **Functional Design** — ✅ APROBADO (2026-05-23). 4 artefactos v1.1 (selección simple sin Identity). requirements.md v1.2. stories US-23/US-24 actualizadas. 10 documentos de Inception actualizados para coherencia.
- [x] **NFR Requirements** — ✅ APROBADO (2026-05-23). 5 decisiones: CSP Blazor, cookie 8h sliding, Serilog texto, middleware personalizado, 5 integration tests.
- [x] **NFR Design** — ✅ APROBADO (2026-05-23). 2 artefactos: nfr-design-patterns.md (6 patrones), logical-components.md (8 componentes). Decisiones: GlobalExceptionHandler=IExceptionHandler, PII filter=IDestructuringPolicy.
- [x] **Infrastructure Design** — ✅ APROBADO (2026-05-23). 2 artefactos: infrastructure-design.md (v1.0), deployment-architecture.md (v1.1 — diagrama Mermaid). Decisiones: Data Protection=file system (keys/), HTTPS=dev-certs, migrations=manual CLI.
- [ ] Code Generation
- [x] **U2 — Persistence & Incidents** — ✅ CERRADA 2026-05-23. Act1 ✅ Act2 ✅ Act3 ✅ Act4 ✅. 9 artefactos: domain-entities.md, business-logic-model.md, business-rules.md, frontend-components.md, nfr-requirements.md, tech-stack-decisions.md, nfr-design-patterns.md, logical-components.md, infrastructure-design.md, deployment-architecture.md (Mermaid). Act5 Code Generation OUT OF SCOPE.
- [x] **U3 — Detection & Classification** — ✅ CERRADA 2026-05-23. Act1 ✅ Act2 ✅ Act3 ✅ Act4 ✅. 10 artefactos. Act5 Code Generation OUT OF SCOPE.
- [x] **U4 — External Integrations** — ✅ CERRADA. Act1 ✅ Act2 ✅ Act3 ✅ Act4 ✅. 10 artefactos. Act5 Code Generation OUT OF SCOPE.
- [x] **U5 — Rules Management** — ✅ CERRADA 2026-05-24. Act1 ✅ Act2 ✅ Act3 ✅ Act4 ✅. 11 artefactos. Act5 Code Generation OUT OF SCOPE.
- [x] **U6 — Dashboard & Real-Time** — ✅ CERRADA 2026-05-24. Act1 ✅ Act2 ✅ Act3 ✅ Act4 ✅. 10 artefactos. Act5 Code Generation OUT OF SCOPE.
- [x] **U7 — Simulation & Red-Teaming** — ✅ CERRADA 2026-05-24. Act1 ✅ Act2 ✅ Act3 ✅ Act4 ✅. 11 artefactos: domain-entities.md, business-logic-model.md, business-rules.md, frontend-components.md, nfr-requirements.md, tech-stack-decisions.md, nfr-design-patterns.md, logical-components.md, infrastructure-design.md, deployment-architecture.md (Mermaid final U1–U7), red-team-report-template.md. Act5 Code Generation OUT OF SCOPE.
- [x] **Build & Test** — ✅ EJECUTADO 2026-05-28. 50/50 tests passing (45 unit + 5 integration). Build 0 errores. 5 migraciones aplicadas. App arranca. MVP completo.

### Plans — Estado de archivos de plan
- [x] **Plans U1** — 4 archivos (Act1–Act4) ✅ existían desde 2026-05-23
- [x] **Plans U2** — 4 archivos (Act1–Act4) ✅ generados 2026-05-24 (retrospectivos)
- [x] **Plans U3** — 4 archivos (Act1–Act4) ✅ generados 2026-05-24 (retrospectivos)
- [x] **Plans U4** — 4 archivos (Act1–Act4) ✅ generados 2026-05-24 (retrospectivos)
- [x] **Plans U5** — 4 archivos (Act1–Act4) ✅ generados 2026-05-24 (retrospectivos)
- [x] **Plans U6** — 4 archivos (Act1–Act4) ✅ generados 2026-05-24 (retrospectivos)
- [x] **Plans U7** — 4 archivos (Act1–Act4) ✅ generados 2026-05-24 (retrospectivos)
- **Total plans**: 28/28 archivos ✅ COMPLETO

### Iteraciones post-MVP

> Documentación organizada en `construction/iteraciones/` + planes en `construction/plans/`.

- [x] **IT1 — PostgreSQL → SQL Server + Dual DB** — 2026-05-30
  - Swap proveedor EF Core: Npgsql → SqlServer
  - Segunda conexión `ProductionDb` (Dapper, solo SELECT) → `oc_encabezado`
  - `IOrderSource` condicional: `ProductionOrderRepository` en prod, `SimulatedOrderRepository` en dev
  - Artefactos: `construction/iteraciones/it1-sqlserver-migration/` · Plan: `plans/it1-sqlserver-migration-plan.md`
- [x] **IT2 — M2 Configurable por Reglas** — 2026-05-29
  - `RuleCondition`: nuevo campo `Channels`; `Rule.AppliesTo` → `ModuleId`
  - `DbOrderChecker`: lee `MinOrders` + `Channels` desde regla activa; conteo vía `GroupBy(OrderId).ToDictionary()`
  - Seed data migración `MakeRulesConfigurable` (WindowMinutes=10, MinOrders=1, Channels=["SALESFORCE","MULTIVENDE"])
  - Artefactos: `construction/iteraciones/it2-m2-configurable/` · Plan: `plans/it2-m2-configurable-plan.md`
- [x] **IT3 — Brand Monitor Redesign** — 2026-05-28
  - Append-only (`InsertAsync`); comparación vs snapshot histórico real (`GetSnapshotBeforeAsync`)
  - `PendingCountPrevious` nullable; `SnapshotStatus.NoData`; badge desde `hasHistory`
  - `IServiceScopeFactory.CreateAsyncScope()` en `BrandMonitorService`; ventana configurable
  - Paso 7 (retención 48h) diferido por el owner
  - Artefactos: `construction/iteraciones/it3-brand-monitor/` · Plan: `plans/it3-brand-monitor-plan.md`
- [x] **IT4 — Dashboard Improvements** — 2026-05-28/29
  - Timers separados: `_domainTimer` + `_brandTimer` (30s c/u)
  - Labels humanas Módulo/Causa en HistoricPage, IncidentDetailPage, WeeklySummaryPage
  - `@rendermode InteractiveServer` en páginas de reglas e histórico
  - Artefactos: `construction/iteraciones/it4-dashboard-improvements/` · Plan: `plans/it4-dashboard-improvements-plan.md`

- [x] **IT6 — UI Navigation & NOC Mode** — 2026-05-31
  - Implementación de vista NOC (pantalla completa TV-friendly)
  - `NocPage.razor` con 4 cards ordenadas por prioridad (M3 → M11 → M2 → M4)
  - `NocCard.razor`, `BrandMonitorTable.razor` — texto grande, alto contraste
  - `Components/Pages/Noc/` como feature completa

- [x] **IT5 — Salesforce Order Monitor** — 2026-05-30
  - `ISalesforceClient`: `PingOrdersAsync` → `SearchPendingOrdersAsync` (POST OCAPI order_search)
  - `SalesforceSearchOutcome` nuevo tipo de resultado con datos de pedidos por site
  - `SalesforceApiChecker`: evalúa pedidos pendientes por site Colombia; details `"SITE:COUNT:STATUS"`
  - `AlertTemplateRenderer`: `+` template `(Api, Warn)` con `ctx.CheckDetails`
  - Dashboard "APIs Externas": sub-rows por site via `ApplyLastCheckResult` → `LastCheckStore[SalesforceApi]`
  - Config multi-site 4 marcas Colombia (PatPrimo, SevenSeven, Ostu, Atmos) — OAuth2 Account Manager
  - Token endpoint: `https://account.demandware.com/dwsso/oauth2/access_token` (client_credentials S2S)
  - `SalesforceApiChecker` refactorizado IT8: solo disponibilidad (Ok/Critical), sin Warn por volumen
  - Plan: `plans/it5-salesforce-order-monitor-plan.md`

- [x] **IT7 — Salesforce → Brand Monitor Integration** — 2026-05-30/31
  - `BrandMonitorChecker` reemplaza `ISimulatedOrderRepository` por `ISalesforceClient`
  - Conteos reales por site con `OrdinalIgnoreCase`; fallo Salesforce → `Warn` sin snapshots
  - `BrandMonitorService.SimulateAndRefreshAsync` simplificado (solo llama checker)
  - `BrandSnapshot.Sites`: `"Patprimo"` → `"PatPrimo"`; registros viejos eliminados de BD
  - Validado: `PatPrimo=14, SevenSeven=6, Atmos=1, Ostu=15` (total=36)
  - Plan: `plans/it7-salesforce-brand-monitor-plan.md`

- [x] **IT8 — NOC Page Improvements** — 2026-05-31
  - `BrandMonitorTable`: usa `SnapshotStatus` real + flechas tendencia (↑↓=) + "pendientes por descargar"
  - M3 APIs Externas: elimina "Pendientes", muestra `Disponible/Con advertencias/Sin respuesta`
  - `ApiStatusBadge`: agrega estado `Warn` (amarillo) + labels descriptivos
  - `GetApisStatus()`: propaga `Warn` correctamente
  - `GetApiShortDetail()`: detalle técnico solo en Critical
  - Timing dev: `CheckerIntervalMinutes=0.5` (30s); NOC UI timer 15s
  - Artefactos: `construction/iteraciones/it8-noc-improvements/` · Plan: `plans/it8-noc-improvements-plan.md`

- [x] **IT10 — Brand Monitor Live Fallback + M2 UX** — 2026-05-31
  - `IBrandMonitorService.GetLiveCountsAsync()`: nuevo método — llama `ISalesforceClient` sin BD
  - `BrandMonitorService`: implementa `GetLiveCountsAsync()` via `IServiceScopeFactory`
  - `BrandMonitorTable.razor`: nuevos params `LiveCounts`, `IsLiveFallback` — tarjetas amarillas con "⚠ Sin historial"
  - `NocPage.razor`: cuando `GetLatestPerSiteAsync` falla → `GetLiveCountsAsync()` fallback; M2 muestra detalle del checker en rojo en lugar de "Esperando datos..."
  - `Dashboard.razor`: cuando `GetCurrentSnapshotsAsync` falla → `GetLiveCountsAsync()` fallback; tabla live con banner "⚠ Datos en tiempo real"
  - Artefactos: `construction/iteraciones/it10-brand-monitor-live-fallback/` · Plan: `plans/it10-brand-monitor-live-fallback-plan.md`

- [x] **IT11 — Dashboard Stability & M3 Per-API Detail** — 2026-05-31
  - `Dashboard.razor`: Brand Monitor refresh no-destructivo — tabla permanece visible durante refresh; spinner inline "Actualizando..."; `_brandStale` banner cuando ambos fallan con datos previos
  - `ApplyApisWorstStatus`: "secondary" solo cuando ningún checker ha corrido (arranque ~30s); partial data usa peor estado conocido; escalate-only preservado
  - `DomainCard.ApiDetails`: nueva propiedad `List<ApiItem>` para detalle por integración en M3
  - `BuildApiItem`: helper que convierte `CheckResult?` → `ApiItem` con label descriptivo por estado
  - `ApiStatusClass`: helper CSS para colorear filas de API
  - M3 card: muestra `Salesforce → ✅/⚠/❌ [estado]` y `Multivende → ...` siempre que haya datos
  - Template: dot `secondary` (gris) + `ApiDetails` > `Channels` en prioridad de render
  - Artefactos: `construction/iteraciones/it11-dashboard-stability/` · Plan: `plans/it11-dashboard-stability-plan.md`

- [x] **IT9 — Graceful Degradation** — 2026-05-31
  - `MonitoringService.RunCheckAsync`: checker que lanza → guarda `Critical` en `LastCheckStore`; `OpenIncidentAsync` + `TryCloseOnConsecutiveOkAsync` protegidos con catch
  - `DbOrderChecker`: `try/catch` externo → `Critical("Sin acceso a BD de pedidos")`
  - `JobsChecker`: `try/catch` → `Critical("Sin acceso a jobs: ...")`
  - `BrandMonitorChecker`: catch separados para `GetLatestAsync`, `GetSnapshotBeforeAsync`, `InsertAsync`
  - `DbHealthChecker`: mensaje limpio `"No hay conexión a la base de datos"` (sin raw SQL)
  - `SalesforceApiChecker`: elimina parámetro `IConfiguration` no usado
  - `Dashboard.razor`: `RefreshIncidentsAsync` falla → `BuildDomainsFromLastCheckStore()` + banner "BD no disponible"; elimina `SimulateAndRefreshAsync()` del render
  - `NocPage.razor`: fallback desde `LastCheckStore` cuando incidentes no cargables
  - Artefactos: `construction/iteraciones/it9-graceful-degradation/` · Plan: `plans/it9-graceful-degradation-plan.md`

### VALIDATE Phase — 2026-06-01

#### IT-VALIDATE — Pruebas E2E Playwright

- [x] **Configuración Playwright** — `tests/e2e/` con TypeScript, Chromium, helper auth
- [x] **data-testid instrumentación** — NocCard, NocPage, BrandMonitorTable, Dashboard (17 atributos)
- [x] **Suite NOC** — T1–T5 (6 tests): carga, M3 detalle, M3 peor estado, Brand Monitor refresh, M4 estado
- [x] **Suite Dashboard** — D1–D4 (4 tests): carga, Brand Monitor visible, Chequear ahora, refresh no destructivo
- [x] **Red-Team Bloque 1** — RT-Persist, RT5, RT7 (3 tests) — sin cambios de entorno
- [x] **Red-Team Bloque 2** — RT3 BD caída, RT2 token inválido (7 tests) — vía .env temporal
- [x] **Red-Team Bloque 3** — RT1 job deshabilitado (3 tests) — acción en SR-SDEV02CO
- [x] **Error E3 LogsPage** — (1 test) — fix TechnicalLogReader FileShare.ReadWrite
- [x] **Resultado final**: **24/24 tests passing** — NOC + Dashboard + 6 RT del PRD Segmento 11
- [x] **Documentación**: `test-plan-playwright-resultados.md` (plan unificado + resultados)

#### IT-FIX-JUNE — Fixes de código detectados durante VALIDATE

- [x] **SalesforceApiChecker.cs** — mensaje HTTP 401 incluye "401" → activa BR-TOKEN-01 → template SOP-001
- [x] **MonitoringSchedulerService.cs** — checkers corren inmediatamente al arrancar (sin esperar 5 min)
- [x] **TechnicalLogReader.cs** — FileStream con FileShare.ReadWrite → LogsPage ya no falla con IOException
- [x] **JobStatusSnapshot.cs** — nuevo campo `FailureReason` (nullable) para diagnóstico
- [x] **SchtasksJobStatusSource.cs** — distingue timeout de red vs job deshabilitado en FailureReason
- [x] **NocPage.razor** — muestra `FailureReason` en card M11 en lugar de "Disabled" hardcoded

### ORCHESTRATION — Documentación de Estación 6/7
- [x] **Orchestration docs creados** — `aidlc-docs/orchestration/` (2026-06-02)
- [x] `task-package.yaml` — 25 tareas, 8 milestones (MS1-MS8)
- [x] `milestones.md` — MS1 a MS8 con descripciones
- [x] `tasks/` — 25 archivos de tarea individuales (001-025)
- [x] `harness-ficha.md` — OpenCode como harness
- [x] `orchestration-map.md` — diagrama Mermaid de dependencias
- [x] `build-and-test-summary.md` — incluye Playwright (14 specs, 24 tests)
- [x] `aidlc-state.md` actualizado — refleja 24 tests, 5 reglas, 11 ITs
- [ ] Publicar tasks en Linear via Linear MCP
- [ ] Llenar `aidlc-docs/operations/` con runbook

### OPERATIONS Phase
- [ ] Deploy on-prem (pendiente decisión de negocio)
- [ ] Configurar CI/CD para `npx playwright test` en pipeline
