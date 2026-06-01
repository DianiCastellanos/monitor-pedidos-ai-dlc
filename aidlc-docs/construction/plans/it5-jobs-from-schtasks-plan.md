# Plan IT5 — Jobs Monitor desde Windows Task Scheduler

**Fecha:** 2026-05-30  
**Estado:** ✅ Completado

---

## Objetivo

Reemplazar la fuente simulada de `JobsChecker` por consultas reales al Task Scheduler del servidor `SR-SDEV02CO.patprimo.local` vía `schtasks.exe`, verificando que la tarea `OC_PATPRIMO` esté en estado "Ready".

---

## Archivos creados/modificados

| Archivo | Acción |
|---------|--------|
| `src/MonitorPedidos.Infrastructure/JobMonitoring/SchtasksJobStatusSource.cs` | **CREAR** — implementación que ejecuta `schtasks /QUERY` y parsea CSV |
| `src/MonitorPedidos.Infrastructure/JobMonitoring/JobsMonitorOptions.cs` | **CREAR** — options class para `Server`, `TaskName`, `CommandTimeoutMs` |
| `src/MonitorPedidos.Web/Program.cs` | **MODIFICAR** — registro condicional de `SchtasksJobStatusSource` vs `SimulatedJobStatusRepository` |
| `src/MonitorPedidos.Web/appsettings.json` | **MODIFICAR** — agregar sección `JobsMonitor` |
| `aidlc-docs/construction/plans/it5-jobs-from-schtasks-plan.md` | Este documento |

---

## Detalle de implementación

### Llamada al sistema
```powershell
schtasks /QUERY /S SR-SDEV02CO.patprimo.local /TN "OC_PATPRIMO" /FO CSV /NH
```

### Output real confirmado
```
"\OC_PATPRIMO","2026/05/30 1:24:31 PM","Ready"
```

### Mapeo a JobStatusSnapshot
| Condición | `IsRunning` | `LastExecutionSucceeded` |
|-----------|------------|--------------------------|
| Status = "Ready" | `true` | `true` |
| Status = "Disabled" | `false` | `false` |
| Error de conexión / timeout / no encontrado | `false` | `false` |

### Manejo de errores
- Timeout > `CommandTimeoutMs` → Cancela proceso, log Warning, retorna snapshot fallido
- ExitCode != 0 → Log Warning con stderr, retorna snapshot fallido
- Salida vacía o mal formateada → Log Warning, retorna snapshot fallido
- Cualquier excepción → Log Error, retorna snapshot fallido

### Configuración
```json
"JobsMonitor": {
  "Server": "SR-SDEV02CO.patprimo.local",
  "TaskName": "OC_PATPRIMO",
  "CommandTimeoutMs": 10000
}
```

### Registro DI (Program.cs)
- Si `JobsMonitor:Server` está configurado → `SchtasksJobStatusSource`
- Si no → `SimulatedJobStatusRepository` (fallback para dev sin conexión)

---

## Pruebas realizadas

- ✅ Compilación correcta (0 errores, 0 warnings)
- ✅ App inicia sin errores con la nueva implementación
- ✅ Timeout (servidor no reachable) logea warning sin crash
- ✅ Se confirmó output real `"Ready"` desde el servidor vía RDP
- ⚠️ `schtasks /S` remoto desde fuera de la red del servidor no funciona — depende de conectividad de red.
