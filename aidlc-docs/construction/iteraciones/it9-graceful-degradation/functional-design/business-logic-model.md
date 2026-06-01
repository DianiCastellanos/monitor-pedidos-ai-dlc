# IT9 — Graceful Degradation · Business Logic Model

**Fecha:** 2026-05-31
**Iteración:** IT9 — Degradación graceful ante pérdida parcial de conectividad
**Estado:** Completado — validado 2026-05-31
**Unidades afectadas:** U3 (Detection & Classification), U6 (Dashboard/NOC UI)

---

## 1. Objetivo

Garantizar que la aplicación nunca muestre "Error al cargar la página" cuando hay pérdida parcial de conectividad (VPN caída, BD inaccesible, servidor de jobs inalcanzable).

Cada módulo degrada de forma independiente: si M2 falla, M3/M4/M11 siguen operando. La UI siempre carga y muestra el estado real por módulo.

---

## 2. Principio de diseño

```
Checker con excepción → CheckResult.Critical(mensaje limpio)
                      → LastCheckStore[módulo] = Critical
                      → UI lee LastCheckStore → muestra estado correcto
```

La UI nunca propaga excepciones del backend al circuito Blazor. Si algo falla, mantiene el último estado conocido y muestra un banner informativo.

---

## 3. Comportamiento esperado por módulo (sin VPN)

| Módulo | Checker | Antes de IT9 | Después de IT9 |
|--------|---------|-------------|----------------|
| M4 BD Salud | `DbHealthChecker` | Critical correcto | Sin cambio (ya funcionaba) |
| M2 BD Pedidos | `DbOrderChecker` | Lanzaba excepción, `LastCheckStore` desactualizado | Critical "Sin acceso a BD de pedidos" |
| M11 Jobs | `JobsChecker` | Lanzaba excepción, `LastCheckStore` desactualizado | Critical "Sin acceso a jobs: ..." |
| Brand Monitor | `BrandMonitorChecker` | SqlException en `GetSnapshotBeforeAsync`/`InsertAsync` bloqueaba checker | Continúa con Salesforce; sin histórico OK |
| Dashboard UI | `RefreshBrandAsync` | Circuit teardown (crash) | Catch, mantiene estado anterior, banner |
| NOC UI | `RefreshAsync` | Incidentes fallidos mostraban pantalla vacía | Fallback desde `LastCheckStore` |

---

## 4. Flujo degradado completo (sin VPN)

```
Sin VPN activa
    │
    ├── M4 DbHealthChecker      → Critical "No hay conexión a la base de datos"   (ya OK)
    ├── M2 DbOrderChecker       → Critical "Sin acceso a BD de pedidos"           (nuevo)
    ├── M11 JobsChecker         → Critical "Sin acceso a jobs: timeout"           (nuevo)
    ├── M3 SalesforceApiChecker → Ok / Critical según Salesforce                  (sin cambio)
    │
    ├── BrandMonitorChecker
    │       ├── Salesforce OK   → obtiene conteos reales → Details pipe-separado
    │       ├── BD unavailable  → previousCount = null, status = NoData (no crash)
    │       └── Insert falla    → log warning, continúa sin persistir
    │
    └── Dashboard / NOC
            ├── RefreshBrandAsync    → catch, mantiene _brandSnapshots anterior
            ├── RefreshIncidentsAsync → catch, BuildDomainsFromLastCheckStore() + banner
            └── Página carga correctamente — cards en rojo por módulo afectado
```

---

## 5. Fix A — MonitoringService: LastCheckStore siempre actualizado

**Problema:** si un checker lanzaba excepción (no capturada dentro del checker), `LastCheckStore` conservaba el último estado válido (ej. `Ok`) en lugar del estado real (fallo).

**Fix:**

```csharp
// MonitoringService.RunCheckAsync
try
{
    var result = await checker.ExecuteAsync(ct);
    _lastCheckStore.Update(checker.Module, result);   // siempre actualizar

    if (result.RequiresIncident)
    {
        try { await OpenIncidentAsync(checker, result, ct); }
        catch (Exception ex) { _logger.LogWarning("OpenIncidentAsync falló: {Msg}", ex.Message); }
    }
    else
    {
        try { await TryCloseOnConsecutiveOkAsync(checker.Module, ct); }
        catch (Exception ex) { _logger.LogWarning("TryCloseAsync falló: {Msg}", ex.Message); }
    }
}
catch (Exception ex)
{
    // Checker que lanzó → guardar Critical en LastCheckStore
    _logger.LogError(ex, "Checker {Type} lanzó excepción", checker.GetType().Name);
    _lastCheckStore.Update(checker.Module, CheckResult.Critical($"Error interno: {ex.Message[..60]}"));
}
```

---

## 6. Fix B — DbHealthChecker: mensaje limpio

**Antes:** "BD no responde: The connection string is not valid..."
**Después:** "No hay conexión a la base de datos"

El mensaje técnico (inner exception) va al log, no al usuario ni al `LastCheckStore`.

---

## 7. Fix C — Dashboard: fallback desde LastCheckStore

### RefreshIncidentsAsync

```csharp
private async Task RefreshIncidentsAsync()
{
    try
    {
        _openIncidents = await IncidentService.GetOpenIncidentsAsync();
        _domains       = BuildDomainsFromIncidents(_openIncidents);
        _dbUnavailable = false;
    }
    catch (Exception ex)
    {
        _logger.LogWarning("RefreshIncidentsAsync falló: {Msg}", ex.Message);
        _domains       = BuildDomainsFromLastCheckStore();
        _dbUnavailable = true;
    }
}
```

Cuando `_dbUnavailable = true`, el Dashboard muestra un banner: "⚠ BD no disponible — mostrando estado del checker".

### BuildDomainsFromLastCheckStore

Lee los 5 módulos desde `_lastCheckStore.Results` y construye las cards de la UI sin necesidad de acceder a la BD de incidentes.

---

## 8. Fix D — Dashboard: eliminar SimulateAndRefreshAsync del render

**Antes:** `RefreshBrandAsync` llamaba `SimulateAndRefreshAsync` en cada ciclo del timer UI (cada 30s), provocando llamadas Salesforce frecuentes y no controladas desde la UI.

**Después:** `RefreshBrandAsync` solo lee datos (BD o `LastCheckStore`). El scheduler controla cuándo llama Salesforce.

---

## 9. Fix E — NocPage: fallback desde LastCheckStore

```csharp
private async Task RefreshAsync()
{
    try
    {
        _openIncidents = await IncidentService.GetOpenIncidentsAsync();
        _domains       = BuildDomainsFromIncidents(_openIncidents);
    }
    catch
    {
        _domains = BuildDomainsFromLastCheckStore();
    }

    try { _brandSnapshots = await BrandMonitorService.GetCurrentSnapshotsAsync(); }
    catch { /* liveCounts ya manejado en BrandMonitorTable */ }

    StateHasChanged();
}
```

---

## 10. Archivos modificados

| Archivo | Cambio |
|---------|--------|
| `Services/MonitoringService.cs` | LastCheckStore siempre actualizado; OpenIncidentAsync protegido con catch |
| `Features/Monitoring/DbOrderChecker.cs` | try/catch externo → Critical en fallo |
| `Features/Monitoring/JobsChecker.cs` | try/catch externo → Critical en fallo |
| `Features/Monitoring/DbHealthChecker.cs` | Mensaje limpio "No hay conexión a la base de datos" |
| `Features/Monitoring/BrandMonitorChecker.cs` | catch separados para GetLatestAsync, GetSnapshotBeforeAsync, InsertAsync |
| `Features/ApiChecks/SalesforceApiChecker.cs` | Elimina parámetro IConfiguration no usado |
| `Components/Pages/Dashboard.razor` | RefreshIncidentsAsync falla → BuildDomainsFromLastCheckStore + banner; elimina SimulateAndRefreshAsync |
| `Components/Pages/Noc/NocPage.razor` | Fallback desde LastCheckStore; StateHasChanged al final de RefreshAsync |

---

## 11. Invariantes preservados

- `ProductionDb` es READ-ONLY — no se agregan escrituras
- Credenciales solo en `.env` — sin cambio
- `brand_snapshots` es append-only — el catch en `InsertAsync` simplemente no persiste; no altera la lógica de negocio
- Los módulos siguen siendo independientes — el fallo de uno no afecta a los demás
