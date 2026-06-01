# IT10 — Brand Monitor Live Fallback + M2 UX · Business Logic Model

**Fecha:** 2026-05-31
**Iteración:** IT10 — Live fallback para Brand Monitor cuando BD no disponible; mejora UX M2
**Estado:** Completado
**Unidades afectadas:** U6 (Dashboard/NOC UI), U4 (External Integrations — IBrandMonitorService)

---

## 1. Objetivo

Cuando la BD no está disponible y el `BrandMonitorChecker` no ha persistido snapshots, mostrar datos en tiempo real de Salesforce (sin histórico, sin tendencias) en lugar del skeleton vacío. El usuario sigue viendo conteos reales de pedidos pendientes.

---

## 2. Nuevo método — IBrandMonitorService.GetLiveCountsAsync

```csharp
public interface IBrandMonitorService
{
    Task<IReadOnlyList<BrandSnapshot>>           GetCurrentSnapshotsAsync(CancellationToken ct);
    Task                                          SimulateAndRefreshAsync(CancellationToken ct);
    Task<IReadOnlyDictionary<string, int>?>       GetLiveCountsAsync(CancellationToken ct);
}
```

### Implementación

```csharp
public async Task<IReadOnlyDictionary<string, int>?> GetLiveCountsAsync(CancellationToken ct = default)
{
    try
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var sfClient          = scope.ServiceProvider.GetRequiredService<ISalesforceClient>();
        var outcome           = await sfClient.SearchPendingOrdersAsync(ct);
        if (!outcome.IsSuccess) return null;

        return BrandSnapshot.Sites.ToDictionary(
            site => site,
            site => outcome.Items.Count(i =>
                string.Equals(i.SiteId, site, StringComparison.OrdinalIgnoreCase)));
    }
    catch (Exception ex)
    {
        logger.LogWarning("[BrandMonitor] GetLiveCountsAsync falló: {Msg}", ex.Message);
        return null;
    }
}
```

**REGLA CRÍTICA:** Este método llama Salesforce directamente. **Está PROHIBIDO usarlo en ciclos automáticos de la UI** (timers, refreshes periódicos). Solo puede invocarse cuando el usuario pulsa explícitamente el botón "↻ Actualizar" y la BD no está disponible. El único camino automático para obtener datos de Brand Monitor es: `BrandMonitorChecker (background) → LastCheckStore → UI`.

---

## 3. Árbol de decisión — Dashboard.RefreshBrandAsync

```
RefreshBrandAsync(isManual)
        │
        ├── GetCurrentSnapshotsAsync() OK
        │       → _brandSnapshots = snapshots
        │         _liveCounts = null
        │         _isLiveFallback = false
        │
        └── GetCurrentSnapshotsAsync() falla (BD no disponible)
                │
                ├── isManual == true
                │       → GetLiveCountsAsync() → Salesforce directo
                │         _liveCounts = counts / null
                │         _isLiveFallback = true
                │         _brandStale = false (dato fresco)
                │
                └── isManual == false
                        → GetBrandCountsFromLastCheckStore()
                          (lee LastCheckStore[BrandMonitor].Details)
                          _liveCounts = counts / null
                          _isLiveFallback = (counts != null)
```

### GetBrandCountsFromLastCheckStore

Lee el formato pipe-separado guardado por `BrandMonitorChecker`:

```csharp
// LastCheckStore[BrandMonitor].Details = "PatPrimo:14|SevenSeven:3|Ostu:6|Atmos:1"
private IReadOnlyDictionary<string, int>? GetBrandCountsFromLastCheckStore()
{
    var result = _lastCheckStore.Results.GetValueOrDefault(ModuleId.BrandMonitor);
    if (result?.Details is null) return null;

    return result.Details
        .Split('|', StringSplitOptions.RemoveEmptyEntries)
        .Select(part => part.Split(':', 2))
        .Where(p => p.Length == 2 && int.TryParse(p[1], out _))
        .ToDictionary(p => p[0], p => int.Parse(p[1]));
}
```

---

## 4. Árbol de decisión — NocPage.RefreshAsync (Brand Monitor)

```
RefreshAsync()
        │
        ├── GetCurrentSnapshotsAsync() OK (GetLatestPerSiteAsync)
        │       → _brandSnapshots = snapshots
        │         _liveCounts = null, _isLiveFallback = false
        │
        └── GetCurrentSnapshotsAsync() falla
                │
                ├── ParseBrandCountsFromLastCheckStore() tiene datos
                │       → _liveCounts = counts
                │         _isLiveFallback = true
                │
                └── ParseBrandCountsFromLastCheckStore() vacío
                        → _liveCounts = null, _brandSnapshots = null
                          → Skeleton con 4 marcas y "—"
```

---

## 5. BrandMonitorTable — modo Live Fallback

Cuando `IsLiveFallback = true` y `LiveCounts != null`:

- Cada fila muestra el conteo de Salesforce
- Badge: amarillo con "⚠ Sin historial" (no hay comparación con periodo anterior)
- Header de tabla incluye texto: "Datos en tiempo real — sin historial de comparación"

Cuando `Snapshots = null` y `LiveCounts = null`:

- Tabla skeleton con las 4 marcas (PatPrimo / SevenSeven / Ostu / Atmos)
- Conteos: "—" en cada fila
- No se muestra "Esperando datos del scheduler..." — el skeleton es suficiente

---

## 6. Activación del banner por estado

El usuario siempre sabe qué tipo de dato está viendo. Los mensajes exactos son:

| Condición | Banner Dashboard | Banner NOC |
|-----------|-----------------|------------|
| BD OK, snapshots normales | ninguno | ninguno |
| BD caída, hay snapshots anteriores en pantalla | `⚠ Datos desactualizados` | `⚠ Datos desactualizados` |
| BD no disponible, dato viene de LastCheckStore (checker corrió) | `⚠ Datos en tiempo real · BD no disponible` | `⚠ Sin historial` por fila |
| Botón manual "↻ Actualizar" con BD caída (Salesforce directo) | `⚠ Tiempo real · BD no disponible — datos consultados ahora en Salesforce` | N/A (NOC es vista pasiva, sin botón manual) |
| Primer arranque, checker no ha corrido, sin datos | `Sin datos aún` (skeleton con "—") | skeleton con "—" |

**Regla:** Ninguna de estas condiciones provoca una llamada automática a Salesforce desde la UI. El dato siempre viene de BD o de LastCheckStore en el ciclo automático. Solo el botón manual rompe esta regla, y solo cuando la BD no está disponible.

---

## 7. M2 UX — Mensaje claro en modo degradado

**Antes:** M2 mostraba "Esperando datos..." cuando el checker no había corrido o la BD no respondía.

**Después:** M2 muestra el mensaje de detalle del checker desde `LastCheckStore`:

```csharp
// En Dashboard.BuildDomainsFromLastCheckStore
var m2Result = _lastCheckStore.Results.GetValueOrDefault(ModuleId.DbOrders);
var m2Detail = m2Result?.Details ?? "Sin datos aún";
// Mostrar en la card con badge Critical/Warn/Ok según el estado
```

Si el checker aún no ha corrido (arranque ~30s): "Sin datos aún" en badge gris.
Si BD no disponible: "Sin acceso a BD de pedidos" en badge rojo.

---

## 8. Archivos modificados

| Archivo | Cambio |
|---------|--------|
| `Services/IBrandMonitorService.cs` | + `GetLiveCountsAsync()` |
| `Services/BrandMonitorService.cs` | Implementa `GetLiveCountsAsync()` via `IServiceScopeFactory` |
| `Components/Shared/BrandMonitorTable.razor` | Params `LiveCounts`, `IsLiveFallback`; skeleton 4 marcas; badge "⚠ Sin historial" |
| `Components/Pages/Noc/NocPage.razor` | Fallback a `GetLiveCountsAsync` → liveCounts; pasa `LiveCounts` e `IsLiveFallback` a `BrandMonitorTable` |
| `Components/Pages/Dashboard.razor` | `GetBrandCountsFromLastCheckStore()`; botón manual → `GetLiveCountsAsync`; banner "⚠ Datos en tiempo real" |
