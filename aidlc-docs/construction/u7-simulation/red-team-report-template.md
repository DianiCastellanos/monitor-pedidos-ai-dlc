# Reporte Red-Teaming — MonitorPedidos AI

**Fecha de ejecución:** ___________________________
**Ejecutado por:** Diana Castellanos
**Entorno:** localhost / red interna (Manufacturas Eliot)
**Versión de app:** MVP v1.0
**Branch / Commit:** ___________________________

---

## Instrucciones de uso

1. Ejecutar cada escenario en orden.
2. Marcar Estado: **✅ Pasó** / **❌ Falló** / **⏳ Pendiente**.
3. Registrar evidencia (captura de pantalla, texto de alerta, número de incidente generado).
4. Al finalizar, ejecutar `cleanup-simulated.sql` para restaurar el entorno.

---

## Tabla de escenarios

| # | Escenario | Pasos | Resultado Esperado | Estado | Evidencia |
|---|-----------|-------|--------------------|--------|-----------|
| **RT1** | **Job SF apagado — sin pedidos** | 1. Editar `appsettings.Development.json`: `"NoOrdersMode": true`<br>2. Ejecutar `db/seed/rt1-job-fail.sql` (SalesforceDownload → Failed)<br>3. Reiniciar la aplicación<br>4. Esperar hasta 5 min (Timer1) | Incidente **CRITICAL** en RealtimePage. Causa = **JobStatus** para SalesforceDownload. Notificación en navegador. | ⏳ |  |
| **RT2** | **Token SF revocado** | 1. Abrir terminal: `dotnet user-secrets set "Salesforce:ApiKey" "invalid-token-test"`<br>2. Reiniciar la aplicación<br>3. Esperar hasta 10 min (Timer2 — SalesforceApiChecker) | Incidente **CRITICAL**. Causa = **API**. Sin reintentos (Polly no aplica a 401). SOP-001 visible en detalle del incidente. | ⏳ |  |
| **RT3** | **SQL Server apagado** | 1. Ejecutar en PowerShell: `Stop-Service -Name "MSSQL$LOCALDB"`<br>2. Esperar hasta 5 min (Timer1 — DbHealthChecker)<br>3. Restaurar: `Start-Service -Name "MSSQL$LOCALDB"` al terminar | Incidente **CRITICAL** inmediato. Causa = **DbHealth**. Simulador loguea error pero continúa (try/catch sin rethrow — ADR-U7-03). | ⏳ |  |
| **RT5** | **Estado incorrecto — sin alerta** | 1. Editar `appsettings.Development.json`: `"FailureProbability": 1.0`<br>2. Reiniciar la aplicación<br>3. Esperar 5 min (simulador genera pedidos con `is_failure=true`)<br>4. Navegar a `/discrepancies` | Pedidos con estado incorrecto visibles en **DiscrepanciesPage** como reporte. **Sin incidente CRITICAL** generado (DiscrepanciesPage no dispara alertas — BR-DISC-01). | ⏳ |  |
| **RT7** | **Pedido cancelado ignorado** | 1. Ejecutar `db/seed/rt7-insert-cancelled.sql`<br>2. Esperar hasta 5 min (Timer1 — DbOrderChecker)<br>3. Revisar RealtimePage e IncidentListPage | **Sin incidente generado**. DbOrderChecker ignora pedidos con `status = 'Cancelled'` (BR-RT-04). | ⏳ |  |
| **RT-Persist** | **Estado persiste al cerrar/reabrir** | 1. Generar un incidente CRITICAL (ejecutar RT1 o RT3)<br>2. Verificar que aparece en RealtimePage<br>3. Cerrar completamente el tab/navegador<br>4. Reabrir la aplicación y navegar a `/dashboard` o `/incidents` | El incidente **CRITICAL persiste**. Los datos están en SQL Server LocalDB. RealtimePage carga los últimos incidentes al navegar. Estado = consistente. | ⏳ |  |

---

## Resumen de resultados

| Escenario | Estado | Observaciones |
|-----------|--------|---------------|
| RT1 — Job SF apagado | ⏳ | |
| RT2 — Token revocado | ⏳ | |
| RT3 — SQL apagado | ⏳ | |
| RT5 — Estado incorrecto | ⏳ | |
| RT7 — Cancelado ignorado | ⏳ | |
| RT-Persist — Persistencia | ⏳ | |
| **Total Pasaron** | — / 6 | |
| **Total Fallaron** | — / 6 | |

---

## Restauración del entorno post-demo

```powershell
# 1. Restaurar job statuses
# Ejecutar: db/seed/rt1-job-reset.sql

# 2. Limpiar pedidos simulados
# Ejecutar: db/seed/cleanup-simulated.sql

# 3. Restaurar appsettings.Development.json a valores baseline
# "FailureProbability": 0.0
# "NoOrdersMode": false

# 4. Restaurar User Secrets (si se cambió RT2)
dotnet user-secrets set "Salesforce:ApiKey" "<token-original>"

# 5. Verificar SQL Server (si se detuvo en RT3)
Get-Service -Name "MSSQL*"
```

---

## Notas

- Los escenarios RT2 y RT3 requieren acción fuera de la aplicación (User Secrets / Stop-Service).
- RT5 no genera alerta por diseño — la ausencia de incidente es el resultado correcto.
- RT-Persist valida durabilidad de datos, no comportamiento en tiempo real.
- Todos los escenarios se ejecutan en localhost o red interna. Sin exposición pública (NFR-U1-SECURITY).
