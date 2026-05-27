# Frontend Components & Simulation Control — U7 Simulation & Red-Teaming

**Unidad:** U7 — Simulation & Red-Teaming
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-24
**Versión:** 1.0

---

## §1 Páginas Blazor nuevas en U7

**U7 no introduce páginas Blazor propias.** El simulador se controla exclusivamente mediante configuración (`appsettings.json` o variables de entorno). No hay UI de control del simulador en el dashboard — es una decisión deliberada para mantener la superficie de ataque mínima (SECURITY-01) y evitar que el Operador modifique parámetros de simulación accidentalmente.

---

## §2 Control del simulador — `appsettings.json`

El único punto de control en runtime para los escenarios de red-teaming:

```json
// appsettings.Development.json — valores por escenario
{
  "Simulation": {
    "Enabled": true,
    "InsertIntervalMinutes": 5,
    "FailureProbability": 0.0,
    "NoOrdersMode": false
  }
}
```

### Configuración por escenario

| Escenario | `Enabled` | `NoOrdersMode` | `FailureProbability` | Acción adicional |
|-----------|----------|---------------|---------------------|-----------------|
| Baseline (normal) | true | false | 0.0 | — |
| **RT1** (sin pedidos) | true | **true** | — | Actualizar `simulated_job_statuses` vía script SQL |
| **RT2** (token revocado) | true | false | 0.0 | Cambiar `Salesforce:ApiKey` a valor inválido en User Secrets |
| **RT3** (SQL apagado) | — | — | — | `Stop-Service -Name "MSSQL$LOCALDB"` en PowerShell |
| **RT5** (estado incorrecto) | true | false | **1.0** | — |
| **RT7** (cancelado ignorado) | true | false | 0.0 | Insertar manualmente: `INSERT INTO simulated_orders ... Status='Cancelled'` |
| **RT-Persist** | true | false | 0.0 | Cerrar y reabrir el navegador |

### Variables de entorno equivalentes (para demo sin editar archivos)

```powershell
$env:Simulation__Enabled        = "true"
$env:Simulation__NoOrdersMode   = "true"    # RT1
$env:Simulation__FailureProbability = "1.0"  # RT5
```

---

## §3 Scripts SQL de red-teaming

Estos scripts se ejecutan manualmente durante la validación. Se ubican en `db/seed/`:

### `rt1-job-fail.sql` — Simula job de Salesforce apagado

```sql
UPDATE simulated_job_statuses
SET status = 'Failed',
    last_run_at = GETUTCDATE(),
    error_message = 'Job SalesforceDownload detenido manualmente (RT1)'
WHERE job_name = 'SalesforceDownload';
```

### `rt1-job-reset.sql` — Restaura job a estado normal

```sql
UPDATE simulated_job_statuses
SET status = 'Completed',
    last_run_at = GETUTCDATE(),
    error_message = NULL
WHERE job_name = 'SalesforceDownload';
```

### `rt7-insert-cancelled.sql` — Inserta pedido cancelado

```sql
INSERT INTO simulated_orders (source, status, created_at, is_failure, site)
VALUES ('Salesforce', 'Cancelled', GETUTCDATE(), 1, 'Patprimo');
```

### `cleanup-simulated.sql` — Limpia datos de simulación post-demo

```sql
DELETE FROM simulated_orders WHERE created_at < DATEADD(HOUR, -48, GETUTCDATE());
-- simulated_job_statuses: restaurar a Completed si fue modificado
UPDATE simulated_job_statuses SET status = 'Completed', error_message = NULL;
```

---

## §4 Componentes modificados en U7

| Componente | Unidad origen | Modificación |
|-----------|--------------|-------------|
| `DbOrderChecker` | U3 | Ya lee de `simulated_orders` (la tabla existe desde U7 migration). Sin cambio de código — la tabla es la fuente en MVP. |
| `JobsChecker` | U3 | Lee de `simulated_job_statuses`. Sin cambio de código. |
| `AppDbContext` | U2 | Agrega `DbSet<SimulatedOrder>` + `DbSet<SimulatedJobStatus>` |

**Nota clave:** Los checkers de U3 no saben que los datos son "simulados". `DbOrderChecker` y `JobsChecker` ya están escritos para consultar estas tablas — U7 simplemente las crea y las puebla.

---

## §5 Plantilla de reporte red-teaming

La plantilla se genera como artefacto separado en `aidlc-docs/construction/u7-simulation/red-team-report-template.md`. La owner la completa durante la demo marcando cada escenario como ✅ Pasó / ❌ Falló / ⏳ Pendiente con evidencia opcional (screenshot, texto).

**Formato de la plantilla:**

```markdown
# Reporte Red-Teaming — MonitorPedidos AI

**Fecha de ejecución:** ___________
**Ejecutado por:** Diana Castellanos
**Entorno:** localhost / red interna

| # | Escenario | Pasos | Resultado Esperado | Estado | Evidencia |
|---|-----------|-------|--------------------|--------|-----------|
| RT1 | Job SF apagado | 1. NoOrdersMode=true en appsettings ... | CRITICAL en <5 min, causa=Job | ⏳ | |
| RT2 | Token SF revocado | 1. Cambiar ApiKey inválido ... | CRITICAL, sin reintento, SOP-001 | ⏳ | |
| RT3 | SQL Server apagado | 1. Stop-Service LOCALDB ... | CRITICAL inmediato, causa=DbHealth | ⏳ | |
| RT5 | Estado incorrecto | 1. FailureProbability=1.0 ... | Pedidos en DiscrepanciesPage, sin alerta | ⏳ | |
| RT7 | Cancelado ignorado | 1. Insertar ORDER Cancelled ... | Sin incidente generado | ⏳ | |
| RT-Persist | Cerrar/reabrir | 1. Generar CRITICAL, cerrar tab ... | Estado CRITICAL visible al reabrir | ⏳ | |
```

---

## §6 Trazabilidad

| Componente | RT | RF | Notas |
|-----------|-----|-----|-------|
| `SimulationOptions` + `appsettings` | RT1, RT5 | C-06 | Control sin recompilar |
| Scripts SQL (`db/seed/`) | RT1, RT7 | C-06 | Ejecución manual |
| `red-team-report-template.md` | RT1..RT-Persist | C-06 | Completado por owner durante demo |
| `DbOrderChecker` (sin cambio) | RT1, RT5, RT7 | RF-02, RF-14 | Lee `simulated_orders` |
| `JobsChecker` (sin cambio) | RT1 | RF-04 | Lee `simulated_job_statuses` |
| `SalesforceApiChecker` (sin cambio) | RT2 | RF-08 | Polly ADR-U4-01: sin reintento en 401 |
| `DbHealthChecker` (sin cambio) | RT3 | RF-03 | Falla al conectar a SQL |
