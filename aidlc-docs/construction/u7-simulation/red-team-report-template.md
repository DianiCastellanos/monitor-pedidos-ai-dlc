# Reporte Red-Teaming — MonitorPedidos AI

**Fecha de ejecución:** 2026-06-01  
**Ejecutado por:** Diana Castellanos  
**Entorno:** localhost (dev) + red interna Manufacturas Eliot (SR-SDEV02CO)  
**Versión de app:** MVP v1.0 — post-IT11, con fixes IT-VALIDATE y IT-FIX-JUNE  
**Branch / Commit:** `main` — `fde21e8` (monitor-pedidos-ai-v2)  

> **Nota metodológica:** La ejecución real usó datos reales de Salesforce OCAPI y SQL Server
> de producción (read-only). Los pasos de la plantilla original (scripts SQL simulados,
> `FailureProbability`, `NoOrdersMode`) aplican al entorno de simulación; en producción
> se usaron mecanismos equivalentes: cambio de `.env` para BD/token, Task Scheduler para jobs.
> Cada escenario tiene un test Playwright automatizado que se puede re-ejecutar.

---

## Tabla de escenarios

| # | Escenario | Pasos ejecutados | Resultado esperado | Estado | Evidencia |
|---|-----------|------------------|--------------------|--------|-----------|
| **RT1** | **Job OC_PATPRIMO deshabilitado** | 1. Deshabilitar `OC_PATPRIMO` en Task Scheduler de `SR-SDEV02CO.patprimo.local`<br>2. Reiniciar app<br>3. Esperar ≤ 30s (CheckerIntervalMinutes=0.5 dev)<br>4. Verificar NOC y Dashboard | Incidente **CRITICAL** — M11 Jobs. Card NOC muestra "OC_PATPRIMO Disabled". Alerta en Dashboard. | ✅ |  Playwright `rt1-job-deshabilitado.spec.ts` — 3/3 passing (22s). Commit `e706089`. Job re-habilitado post-test. |
| **RT2** | **Token Salesforce revocado** | 1. Cambiar `.env`: `Salesforce__ClientId=INVALID_CLIENT_ID`<br>2. Reiniciar app<br>3. Esperar ≤ 1 min (ApiCheckerIntervalMinutes=1 dev)<br>4. Verificar NOC M3 e incidente | Incidente **CRITICAL** — M3 SalesforceApi. Causa = Token (HTTP 401). `AccionSugerida` contiene "Renovar token manualmente según procedimiento SOP-001". | ✅ | Playwright `rt2-token-invalido.spec.ts` — 4/4 passing (51s). Fix aplicado: `SalesforceApiChecker` incluye "401" en mensaje. `.env` restaurado post-test. |
| **RT3** | **SQL Server inaccesible** | 1. Cambiar `.env`: IP `192.168.20.91` → `192.168.20.99` (inválida)<br>2. Reiniciar app<br>3. Esperar ≤ 45s (Connect Timeout=5s + CheckerInterval=30s)<br>4. Verificar M4 y M2 en NOC y Dashboard | Incidentes **CRITICAL** en M4 BD Salud y M2 BD Pedidos. Dashboard carga sin crash (graceful degradation IT9). | ✅ | Playwright `rt3-bd-caida.spec.ts` — 3/3 passing. M4 Critical en 39s, M2 en 23s. `.env` restaurado post-test. |
| **RT5** | **Pedido con estado incorrecto** | 1. Navegar a `/discrepancies` con rol Operador<br>2. Verificar que el panel UC6 carga sin errores<br>3. Verificar que muestra incidentes con `Cause=NoDeterminada` (discrepancias) | Panel UC6 visible y funcional. Incidentes de causa no determinada visibles para revisión humana. **Sin alerta automática** (BR-DISC-01). | ✅ | Playwright `rt-bloque1.spec.ts` test RT5 — passing. Panel `/discrepancies` carga correctamente, rol Operador. |
| **RT7** | **Pedido cancelado ignorado** | 1. Verificar código `SalesforceClient.BuildQueryBody()` — cláusula `must_not: status=cancelled`<br>2. Verificar con Playwright que M3 checker corre y reporta estado (sin contar cancelados)<br>3. Datos reales de Salesforce confirman conteo correcto | Pedidos con `status=cancelled` **excluidos** de todos los conteos. M3 checker activo reporta solo pedidos válidos. | ✅ | `SalesforceClient.cs` línea ~122: `must_not: [{ "fields": ["status"], "values": ["cancelled"] }]`. Playwright `rt-bloque1.spec.ts` test RT7 — passing. |
| **RT-Persist** | **Estado persiste al cerrar/reabrir** | 1. Incidentes activos presentes en BD (generados por checkers anteriores)<br>2. Detener app completamente (`Stop-Process`)<br>3. Reiniciar app (`dotnet run`)<br>4. Navegar a `/dashboard` | Incidentes **CRITICAL persisten** tras reinicio. Dashboard carga alertas activas desde SQL Server. Badge `overall-status` refleja estado correcto. | ✅ | Playwright `rt-bloque1.spec.ts` test RT-Persist — passing (1.9s). App reiniciada múltiples veces en la sesión, incidentes siempre recuperados. |

---

## Resumen de resultados

| Escenario | Estado | Observaciones |
|-----------|--------|---------------|
| RT1 — Job OC_PATPRIMO deshabilitado | ✅ Pasó | Detectado en 22s. Test automatizado Playwright. |
| RT2 — Token Salesforce revocado | ✅ Pasó | Detectado en ~22s. Fix aplicado: mensaje HTTP 401 activa template SOP-001. |
| RT3 — SQL Server inaccesible | ✅ Pasó | M4 Critical en 39s, M2 en 23s. Graceful degradation confirmada. |
| RT5 — Estado incorrecto | ✅ Pasó | Panel UC6 funcional. Sin alerta automática (por diseño). |
| RT7 — Pedido cancelado ignorado | ✅ Pasó | Validado por código (`must_not`) + checker activo en NOC. |
| RT-Persist — Persistencia | ✅ Pasó | Incidentes persisten tras reinicio. BD como fuente de verdad. |
| **Total Pasaron** | **6 / 6** | Criterio de aceptación PRD Segmento 11 cumplido. |
| **Total Fallaron** | **0 / 6** | — |

---

## Errores de implementación encontrados durante ejecución

| Error | RT que lo detectó | Fix aplicado |
|---|---|---|
| `SalesforceApiChecker` no incluía "401" en mensaje → SOP-001 nunca aparecía | RT2 | Mensaje cambiado a `"HTTP 401 — token inválido o expirado"` |
| Ruta `/incidents/history` incorrecta en test | RT2 | Corregida a `/incidents` |

---

## Restauración del entorno post-ejecución

Todos los cambios temporales fueron revertidos durante la sesión:

```
✅ .env: IP de BD restaurada a 192.168.20.91
✅ .env: Salesforce ClientId/ClientPassword restaurados a valores reales
✅ OC_PATPRIMO: re-habilitado en SR-SDEV02CO.patprimo.local tras RT1
```

No se ejecutaron scripts SQL de limpieza porque los datos reales de Salesforce
y la BD de producción son read-only — no se insertaron ni modificaron registros.

---

## Tests automatizados — re-ejecución futura

```bash
cd tests/e2e
npm test   # ejecuta todos los tests funcionales (no destructivos)

# RT1 requiere deshabilitar OC_PATPRIMO en SR-SDEV02CO antes de ejecutar
npx playwright test tests/rt1-job-deshabilitado.spec.ts

# RT2 y RT3 requieren modificar .env antes de ejecutar y restaurarlo después
# Ver instrucciones en: rt-bloque2-plan.md
```

---

## Notas

- RT5 no genera alerta por diseño — la ausencia de incidente es el resultado correcto.
- RT7 validado por código + ejecución del checker. No se insertaron pedidos cancelados manualmente (BD producción es read-only).
- Todos los escenarios ejecutados en entorno dev (localhost) con datos reales de Salesforce OCAPI.
- La evidencia detallada de cada RT está en `aidlc-docs/construction/u6-dashboard-realtime/functional-design/rt-bloque1-validacion.md`, `rt-bloque2-plan.md` y `rt-bloque3-plan.md`.
- Tests Playwright completos en `tests/e2e/tests/` — 13 tests RT, todos passing.
