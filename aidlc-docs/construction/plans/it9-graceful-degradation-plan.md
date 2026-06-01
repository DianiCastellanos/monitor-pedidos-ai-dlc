# Plan IT9 — Graceful Degradation (Sin conectividad parcial)

**Fecha:** 2026-05-31
**Estado:** ✅ Completado — validado 2026-05-31
**Unidades afectadas:** U3 (Detection), U6 (Dashboard/NOC UI)

---

## Objetivo

Garantizar que la aplicación **nunca muestre "Error al cargar la página"** cuando hay pérdida parcial de conectividad (VPN caída, BD inaccesible, servidor de jobs inalcanzable).

Cada módulo debe degradar de forma independiente: si M2 falla, M3/M4/M11 siguen operando. La UI siempre carga y muestra el estado real por módulo.

---

## Causa raíz del crash actual

`Dashboard.razor.RefreshBrandAsync()` tiene `try/finally` **sin `catch`**:

```csharp
try
{
    await BrandMonitorService.SimulateAndRefreshAsync(); // lanza SqlException si BD no responde
    _brandSnapshots = await BrandMonitorService.GetCurrentSnapshotsAsync();
}
finally  // ← NO catch → excepción llega al circuito Blazor → teardown → "Error al cargar la página"
{
    _brandLoading = false;
}
```

En Blazor Server, una excepción no manejada en un método de componente **termina el circuito** del usuario.

---

## Diagnóstico por módulo

| Módulo | Checker | Sin VPN/BD | Comportamiento actual | Comportamiento esperado |
|--------|---------|------------|----------------------|------------------------|
| M4 BD Salud | `DbHealthChecker` | BD inaccesible | ✅ Devuelve `Critical("BD no responde")` | Sin cambio |
| M2 BD Pedidos | `DbOrderChecker` | BD inaccesible | ❌ Lanza excepción — no guarda `Critical` en `LastCheckStore` | Devolver `Critical("Sin acceso a BD")` |
| M11 Jobs | `JobsChecker` | Servidor de jobs inalcanzable | ❌ Lanza excepción — no guarda `Critical` en `LastCheckStore` | Devolver `Critical("Sin acceso a jobs")` |
| Brand Monitor | `BrandMonitorChecker` | BD inaccesible | ❌ Lanza SqlException en `GetSnapshotBeforeAsync`/`InsertAsync` | Continuar con Salesforce; snapshot sin histórico |
| Dashboard UI | `RefreshBrandAsync` | BD inaccesible | ❌ **Circuit teardown → crash** | Atrapar excepción, mantener último estado |

---

## Decisiones de diseño

| # | Decisión | Opción elegida | Razón |
|---|----------|----------------|-------|
| D1 | Dashboard crash | Agregar `catch` en `RefreshBrandAsync` y `RefreshIncidentsAsync` | Eliminar el circuit teardown — fix mínimo y seguro |
| D2 | Checkers sin catch | Envolver en `try/catch` → `CheckResult.Critical` con mensaje técnico | Permite que `LastCheckStore` refleje el estado real |
| D3 | BrandMonitorChecker sin BD | Continuar sin histórico — `previousCount = null`, status = `NoData` | Salesforce sigue funcionando; historial se retoma cuando BD vuelve |
| D4 | BrandMonitorChecker insert fallido | Log warning, continuar (snapshot no persiste) | No contaminar circuito por error de persistencia |
| D5 | UI timer | Cambiar de 10s → 15s | Balance performance/estabilidad pedido por el usuario |

---

## Flujo degradado esperado (sin VPN)

```
Sin VPN activa
    │
    ├── M4 DbHealthChecker      → Critical "BD no responde: ..."       ✅ (ya funciona)
    ├── M2 DbOrderChecker       → Critical "Sin acceso a BD de pedidos" (nuevo)
    ├── M11 JobsChecker         → Critical "Sin acceso a jobs: timeout" (nuevo)
    ├── M3 SalesforceApiChecker → Ok / Critical según Salesforce        (sin cambio)
    │
    ├── BrandMonitorChecker
    │       ├── Salesforce OK   → obtiene conteos reales
    │       ├── BD unavailable  → previousCount = null, status = NoData
    │       └── Insert falla    → log warning, continúa sin persistir
    │
    └── Dashboard / NOC
            ├── RefreshBrandAsync → catch excepción, mantiene _brandSnapshots anterior
            ├── RefreshIncidentsAsync → catch excepción, mantiene _openIncidents anterior
            └── Página carga correctamente — cards en rojo por módulo afectado
```

---

## Archivos a modificar

| Archivo | Cambio |
|---------|--------|
| `Features/Monitoring/DbOrderChecker.cs` | Envolver `ExecuteAsync` en `try/catch` → `Critical` en fallo |
| `Features/Monitoring/JobsChecker.cs` | Envolver `ExecuteAsync` en `try/catch` → `Critical` en fallo |
| `Features/Monitoring/BrandMonitorChecker.cs` | Envolver llamadas a BD en `try/catch` separados (Salesforce independiente) |
| `Services/MonitoringService.cs` | FIX A: LastCheckStore siempre actualizado; OpenIncidentAsync protegido |
| `Features/Monitoring/DbHealthChecker.cs` | FIX B: mensaje limpio "No hay conexión a la base de datos" |
| `Components/Pages/Dashboard.razor` | FIX C: fallback a LastCheckStore; FIX D: eliminar SimulateAndRefreshAsync |
| `Components/Pages/Noc/NocPage.razor` | FIX C: fallback a LastCheckStore; timer 15s |

### Archivos sin cambios

| Archivo | Razón |
|---------|-------|
| `DbHealthChecker.cs` | Ya tiene `try/catch` correcto |
| `SalesforceApiChecker.cs` | Outcome de Salesforce ya protegido |
| `BrandMonitorService.cs` | La protección va en el checker y en la UI |
| `MonitoringSchedulerService.cs` | Ya tiene `try/catch` externo por checker |

---

## Detalle de cambios por archivo

### 1. `DbOrderChecker.cs`

```csharp
public async Task<CheckResult> ExecuteAsync(CancellationToken ct = default)
{
    try
    {
        // ... lógica actual ...
    }
    catch (Exception ex)
    {
        _logger.LogWarning("[DbOrderChecker] Sin acceso a BD: {Msg}", ex.Message);
        return CheckResult.Critical("Sin acceso a BD de pedidos");
    }
}
```

### 2. `JobsChecker.cs`

```csharp
public async Task<CheckResult> ExecuteAsync(CancellationToken ct = default)
{
    try
    {
        // ... lógica actual ...
    }
    catch (Exception ex)
    {
        return CheckResult.Critical($"Sin acceso a jobs: {ex.Message[..Math.Min(60, ex.Message.Length)]}");
    }
}
```

### 3. `BrandMonitorChecker.cs`

```csharp
// Después de obtener outcome de Salesforce (sin cambio):
foreach (var site in BrandSnapshot.Sites)
{
    var currentCount = currentCounts[site];
    BrandSnapshot? previousSnapshot = null;

    try
    {
        previousSnapshot = await snapshotRepo.GetSnapshotBeforeAsync(site, now - window, ct);
    }
    catch (Exception ex)
    {
        logger.LogWarning("[BrandMonitorChecker] No se pudo leer histórico para {Site}: {Msg}", site, ex.Message);
    }

    // ... DetermineStatus igual ...

    try
    {
        await snapshotRepo.InsertAsync(snapshot, ct);
    }
    catch (Exception ex)
    {
        logger.LogWarning("[BrandMonitorChecker] No se pudo persistir snapshot para {Site}: {Msg}", site, ex.Message);
    }
}
```

### 4. `Dashboard.razor`

```csharp
private async Task RefreshBrandAsync()
{
    _brandLastUpdate = DateTimeOffset.UtcNow;
    _brandLoading = true;
    StateHasChanged();
    try
    {
        await BrandMonitorService.SimulateAndRefreshAsync();
        _brandSnapshots = await BrandMonitorService.GetCurrentSnapshotsAsync();
    }
    catch (Exception ex)
    {
        // Degradación: mantener último _brandSnapshots conocido
        _logger.LogWarning("RefreshBrandAsync falló: {Msg}", ex.Message);
    }
    finally
    {
        _brandLoading = false;
        StateHasChanged();
    }
}
```

### 5. `NocPage.razor` — timer 15s

```csharp
_refreshTimer = new Timer(..., TimeSpan.FromSeconds(15));
```

---

## Fixes implementados (2 rondas)

### Ronda 1 — IT9 inicial
| Fix | Archivo | Descripción |
|-----|---------|-------------|
| Checkers con catch | `DbOrderChecker`, `JobsChecker`, `BrandMonitorChecker` | Devuelven `Critical` en fallo, no lanzan |
| Dashboard sin crash | `Dashboard.razor` | `catch` en `RefreshBrandAsync` y `RefreshIncidentsAsync` |
| Timer NOC | `NocPage.razor` | 10s → 15s |

### Ronda 2 — Correcciones post-validación NOC
| Fix | Archivo | Problema resuelto |
|-----|---------|------------------|
| **A** `MonitoringService` | `Services/MonitoringService.cs` | Checker que lanza → `LastCheckStore` ya tenía stale OK; ahora siempre guarda `Critical`. `OpenIncidentAsync` protegido con catch |
| **B** `DbHealthChecker` | `Features/Monitoring/DbHealthChecker.cs` | Reemplaza raw SQL error por "No hay conexión a la base de datos" |
| **C** Dashboard fallback | `Components/Pages/Dashboard.razor` | `RefreshIncidentsAsync` falla → `_domains` desde `LastCheckStore` + banner "BD no disponible" |
| **C** NOC fallback | `Components/Pages/Noc/NocPage.razor` | Incidentes no cargables → muestra módulos críticos desde `LastCheckStore` |
| **D** UI no-bloqueo | `Components/Pages/Dashboard.razor` | Elimina `SimulateAndRefreshAsync()` del render — scheduler maneja refresh |

## Checklist de validación

- [x] Sin VPN: Dashboard carga sin error "Error al cargar la página"
- [x] Sin VPN: M2 muestra CRITICAL "Sin acceso a BD de pedidos"
- [x] Sin VPN: M11 muestra CRITICAL "Sin acceso a jobs"
- [x] Sin VPN: M4 muestra CRITICAL "No hay conexión a la base de datos"
- [x] Sin VPN: Brand Monitor muestra datos Salesforce sin histórico (`NoData`)
- [x] Sin VPN: NOC muestra módulos críticos desde LastCheckStore cuando BD no responde
- [x] Sin VPN: Dashboard muestra banner "BD no disponible" y cards en rojo
- [x] Salida de modo NOC: instantánea (sin bloqueo de Salesforce/BD)
- [x] Con VPN: comportamiento normal restaurado en el siguiente ciclo

---

## Restricciones preservadas

- `ProductionDb` READ-ONLY — no se agregan escrituras
- Credenciales solo en `.env` — sin cambio
- Append-only en `brand_snapshots` — el catch en `InsertAsync` no altera la lógica; simplemente no persiste
