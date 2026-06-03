# Build & Test Summary

## Build
- **Solution**: `MonitorPedidos.sln`
- **Platform**: .NET 8, Blazor Server
- **Status**: ✅ 0 errores
- **Command**: `dotnet build`

## Unit Tests
- **Framework**: xUnit
- **Total**: 45 tests
- **Status**: ✅ All passing
- **Command**: `dotnet test`

## Integration Tests
- **Framework**: xUnit con SQL Server real
- **Total**: 5 tests
- **Status**: ✅ All passing
- **Command**: `dotnet test`

## E2E Tests (Playwright)
- **Framework**: Playwright (TypeScript, Chromium)
- **Location**: `tests/e2e/`
- **Spec files**: 15
- **Total tests**: 27
- **Status**: ✅ 27/27 passing
- **Command**: `cd tests/e2e && npx playwright test`

### Desglose por suite

| Suite | Spec file | Tests | Estado |
|-------|-----------|-------|--------|
| NOC T1 — carga sistema | `t1-noc-muestra-estado-sistema.spec.ts` | 1 | ✅ |
| NOC T2 — M3 detalle APIs | `t2-api-externas-muestra-detalle-salesforce-multivende.spec.ts` | 1 | ✅ |
| NOC T3 — M3 peor estado | `t3-api-externas-peor-estado-global.spec.ts` | 1 | ✅ |
| NOC T4 — Brand Monitor NOC | `t4-brand-monitor-no-destructive-refresh.spec.ts` | 1 | ✅ |
| NOC T5 — M4 estado BD | `t5-bd-salud-muestra-latencia-o-critical.spec.ts` | 2 | ✅ |
| Dashboard D1 — carga módulos | `d1-dashboard-muestra-modulos-y-estado.spec.ts` | 1 | ✅ |
| Dashboard D2 — Brand Monitor | `d2-brand-monitor-siempre-visible-dashboard.spec.ts` | 1 | ✅ |
| Dashboard D3 — Chequear ahora | `d3-dashboard-check-now-resetea-countdown.spec.ts` | 1 | ✅ |
| Dashboard D4 — refresh no-destructivo | `d4-brand-monitor-refresh-no-destruye-tabla.spec.ts` | 1 | ✅ |
| IT6 — Navegación UI | `it6-navegacion.spec.ts` | 3 | ✅ |
| LogsPage E3 | `e3-logs-tecnico-carga-sin-error.spec.ts` | 1 | ✅ |
| RT-Persist + RT5 + RT7 | `rt-persist-rt5-rt7-persistencia-discrepancias.spec.ts` | 3 | ✅ |
| RT1 — Job deshabilitado | `rt1-job-deshabilitado-muestra-critical-e-incidente.spec.ts` | 3 | ✅ |
| RT2 — Token Salesforce inválido | `rt2-token-salesforce-invalido-muestra-critical-y-sop001.spec.ts` | 4 | ✅ |
| RT3 — BD caída | `rt3-bd-caida-muestra-critical-y-graceful-degradation.spec.ts` | 3 | ✅ |
| **TOTAL** | **15 specs** | **27** | **✅** |

## Red-Teaming (PRD Segmento 11)
- **Escenarios validados**: 6/6
- **Criterio de aceptación PRD**: ✅ CUMPLIDO

| RT | Escenario | Método | Resultado | Tests |
|----|-----------|--------|-----------|-------|
| RT-Persist | Alertas persisten tras reinicio | App restart + verificación UI | ✅ | 1 |
| RT1 | Job OC_PATPRIMO deshabilitado → M11 Critical | Task Scheduler SR-SDEV02CO | ✅ detectado en 22s | 3 |
| RT2 | Token Salesforce inválido → M3 Critical + SOP-001 | `.env` ClientId inválido | ✅ detectado en 22s | 4 |
| RT3 | SQL Server inaccesible → M4+M2 Critical | `.env` IP inválida | ✅ M4 en 39s, M2 en 23s | 3 |
| RT5 | Panel discrepancias UC6 existe y funciona | Navegación `/discrepancies` | ✅ | 1 |
| RT7 | Pedido cancelado ignorado | Código + checker activo | ✅ `must_not: cancelled` | 1 |

- **Evidencia**: `aidlc-docs/construction/u6-dashboard-realtime/functional-design/`
- **Reporte completo**: `red-team-report-template.md` (llenado con resultados reales)

## Base de datos
- **Proveedor**: SQL Server
- **Migraciones aplicadas**: 5
- **Reglas configuradas**: 5 (BrandMonitor, DbOrderChecker, SalesforceApi, DbHealthChecker, JobsMonitor)

## App Runtime
- **URL**: `http://localhost:5000`
- **Status**: ✅ Running
- **Checkers**: corren inmediatamente al arrancar (startup fix IT-FIX-JUNE)
