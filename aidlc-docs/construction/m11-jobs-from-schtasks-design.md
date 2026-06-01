# M11 — Jobs Monitor desde Windows Task Scheduler

**Fecha:** 2026-05-30
**Contexto:** Reemplazar la fuente simulada de `JobsChecker` por consultas reales al Task Scheduler del servidor `SR-SDEV02CO.patprimo.local` vía `schtasks.exe`.

---

## 1. Estado Actual

| Componente | Estado |
|---|---|
| `JobsChecker` (checker) | ✅ Existe — usa `IJobStatusSource`, retorna OK/CRITICAL |
| `IJobStatusSource` (abstracción) | ✅ Existe — `GetCurrentStatusAsync()` |
| `SimulatedJobStatusRepository` (impl DB) | ✅ Existe — lee `simulated_job_statuses` |
| `schtasks.exe` remoto | ❌ No implementado |
| Template alerta `(Job, Critical)` | ✅ Existe |
| `JobStatusSnapshot` record | ✅ Existe |
| `ModuleId.JobsMonitor` (value 11) | ✅ Existe |
| `CauseCategory.Job` | ✅ Existe |

---

## 2. Diseño

### 2.1 Nueva implementación `SchtasksJobStatusSource`

Crear `src/MonitorPedidos.Infrastructure/JobMonitoring/SchtasksJobStatusSource.cs` implementando `IJobStatusSource`.

**Llamada al sistema:**
```
schtasks /QUERY /S SR-SDEV02CO.patprimo.local /TN "OC_PATPRIMO" /FO CSV /NH
```

- `/FO CSV` — salida estructurada parseable
- `/NH` — omite cabecera

**Output esperado (habilitado):**
```
"\OC_PATPRIMO","5/30/2026 8:00:00 AM","Ready"
```

**Output esperado (deshabilitado):**
```
"\OC_PATPRIMO","5/30/2026 8:00:00 AM","Disabled"
```

**Output si no existe o error de conexión:** `schtasks.exe` retorna exit code distinto de 0 y mensaje por stderr.

**Mapeo a `JobStatusSnapshot`:**

| Condición | `IsRunning` | `LastExecutionSucceeded` |
|---|---|---|
| Status = "Ready" | `true` | `true` |
| Status = "Disabled" | `false` | `false` |
| Error de conexión / no encontrado | `false` | `false` |

### 2.2 Parámetros configurables

Agregar sección en `appsettings.json`:

```json
"JobsMonitor": {
  "Server": "SR-SDEV02CO.patprimo.local",
  "TaskName": "OC_PATPRIMO",
  "CommandTimeoutMs": 10000
}
```

### 2.3 JobsChecker — Sin cambios

El `JobsChecker` ya consume `IJobStatusSource` y retorna:
- `Ok` si todas las tasks tienen `IsRunning && LastExecutionSucceeded`
- `Critical` si alguna falla

Con una sola task (`OC_PATPRIMO`):
- Enabled → lista vacía de fallos → `Ok`
- Disabled → 1 fallo → `Critical`

### 2.4 Registro DI

En `Program.cs`:
```csharp
// Reemplazar fuente simulada por real
builder.Services.AddScoped<IJobStatusSource, SchtasksJobStatusSource>();
// Mantener ISimulatedJobStatusRepository → SimulatedJobStatusRepository para UI de simulación
```

### 2.5 Manejo de errores

| Escenario | Comportamiento |
|---|---|
| `schtasks.exe` no encontrado | Log Warning, retorna `IsRunning=false` |
| Timeout (>10s) | Cancelar proceso, retorna `IsRunning=false` |
| Servidor unreachable | ExitCode != 0, parsear stderr para log, retorna `IsRunning=false` |
| Salida inesperada | Log Warning, retorna `IsRunning=false` |

En todos los casos de error, el checker produce `Critical` → se abre incidente → el operador sabe que no se pudo verificar el job. Esto es intencional: mejor alertar por falso positivo que silenciar un problema real.

---

## 3. Archivos a modificar/crear

| Archivo | Acción |
|---|---|
| `src/MonitorPedidos.Infrastructure/JobMonitoring/SchtasksJobStatusSource.cs` | **CREAR** — implementación vía `schtasks.exe` |
| `src/MonitorPedidos.Web/Program.cs` | **MODIFICAR** — registrar `SchtasksJobStatusSource` como `IJobStatusSource` |
| `src/MonitorPedidos.Web/appsettings.json` | **MODIFICAR** — agregar sección `JobsMonitor` |
| `aidlc-docs/construction/m11-jobs-from-schtasks-design.md` | Este documento |

---

## 4. Compatibilidad

- `SimulatedJobStatusRepository` sigue existiendo y funcionando para la UI de simulación (`ISimulatedJobStatusRepository`)
- `JobChecker` no requiere cambios — solo cambia quién implementa `IJobStatusSource`
- Template de alerta `(Job, Critical)` ya existe y se usa sin modificación
- Si `SchtasksJobStatusSource` falla, el checker reporta Critical como cualquier otro fallo

---

## 5. No incluido (futuro)

- Múltiples jobs configurables (hoy: `OC_PATPRIMO` hardcodeado en config)
- Cache de última respuesta exitosa para evitar falsos positivos por fallos transitorios
- Health check de conectividad al servidor separado del job check
