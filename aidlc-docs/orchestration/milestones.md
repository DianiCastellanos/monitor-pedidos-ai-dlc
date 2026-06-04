# Milestones — MonitorPedidos AI

## MS1: Inception Complete
- **Fecha**: 2026-05-22
- **Estado**: ✅ COMPLETADO
- **Descripción**: Fase de planificación completa — workspace detection, requirements analysis, user stories, workflow planning, application design, units generation. 6 stages ejecutados, 0 pendientes.
- **Entregables**: `aidlc-docs/inception/` (18 artefactos), PRD v2.3, 29 stories, 30 RFs, 6 RTs, 7 unidades descompuestas.

---

## MS2: MVP Core (U1–U7)
- **Fecha**: 2026-05-28
- **Estado**: ✅ COMPLETADO
- **Descripción**: Implementación completa del MVP — desde foundation hasta simulación. 7 unidades construidas secuencialmente con diseño + NFR + infraestructura por unidad.
- **Entregables**: `src/MonitorPedidos.Web/`, 50 tests (45 unit + 5 integration), build 0 errores, 5 migraciones, app arranca.
- **Dependencias**: MS1

---

## MS3: SQL Server & Configurabilidad (IT1–IT2)
- **Fecha**: 2026-05-30
- **Estado**: ✅ COMPLETADO
- **Descripción**: Migración de PostgreSQL a SQL Server + dual DB (AppDb + ProductionDb). M2 configurable por reglas con canales y mínimo de pedidos.
- **Entregables**: SQL Server MonitorPedidosDb (172.16.0.41), `ProductionOrderRepository`, `RuleCondition.Channels`, seed `MakeRulesConfigurable`.

---

## MS4: Brand Monitor & Dashboard (IT3–IT4)
- **Fecha**: 2026-05-29
- **Estado**: ✅ COMPLETADO
- **Descripción**: Rediseño completo de Brand Monitor (append-only, smart snapshots, comparación histórica). Dashboard con timers separados y labels humanas.
- **Entregables**: `BrandMonitorService` refactorizado, `brand_snapshots` append-only, Dashboard con 2 timers independientes.

---

## MS5: Salesforce Integration (IT5–IT6–IT7)
- **Fecha**: 2026-05-31
- **Estado**: ✅ COMPLETADO
- **Descripción**: Integración real con Salesforce OCAPI (4 marcas Colombia). UI Navigation + NOC mode. Brand Monitor conectado a datos reales de Salesforce.
- **Entregables**: `SalesforceApiChecker`, OAuth2 Account Manager, 4 sites multi-marca, `NocPage.razor`, Brand Monitor con datos reales.

---

## MS6: Stability & Degradation (IT8–IT9–IT10–IT11)
- **Fecha**: 2026-05-31
- **Estado**: ✅ COMPLETADO
- **Descripción**: Mejoras de estabilidad — graceful degradation, live fallback para Brand Monitor, refresh no-destructivo, M3 con detalle por API.
- **Entregables**: Fallbacks en todos los checkers, live counts vía Salesforce directo, tabla Brand Monitor persistente durante refresh.

---

## MS7: VALIDATE & Red-Teaming
- **Fecha**: 2026-06-01
- **Estado**: ✅ COMPLETADO
- **Descripción**: Pruebas E2E con Playwright (14 spec files, 24 tests). 6 escenarios de red-teaming del PRD Segmento 11 validados. 6 fixes aplicados.
- **Entregables**: `tests/e2e/` con 14 spec files, `test-plan-playwright-resultados.md`, 6 RT docs con evidencia.

---

## MS8: Operations Ready
- **Fecha**: 2026-06-02
- **Estado**: 🟡 PARCIAL (deploy pendiente)
- **Descripción**: Documentación de orquestación, ficha de arnés, task packages. Deploy on-prem pendiente de decisión de negocio.
- **Entregables**: `aidlc-docs/orchestration/`, CI/CD pipeline pendiente.
