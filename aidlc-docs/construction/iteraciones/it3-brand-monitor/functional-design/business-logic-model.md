# IT3 — Brand Monitor Redesign · Business Logic Model

**Fecha:** 2026-05-30  
**Iteración:** IT3 — Brand Monitor (append-only, histórico real, NoData)  
**Stories relacionadas:** US-30 (tablero de estado por marca), RF-31  
**Estado:** ✅ Completado — Paso 7 (retención) diferido

---

## 1. Flujo principal — SimulateAndRefreshAsync

```
[Botón "Actualizar"]
        │
        ▼
BrandMonitorService.SimulateAndRefreshAsync()
        │
        ├─ Scope fresco #1 (IServiceScopeFactory.CreateAsyncScope)
        │   ├─ ISimulatedOrderRepository.InsertRangeAsync(órdenes variadas)
        │   │   └─ 2-15 órdenes aleatorias × 2 fuentes × 4 marcas
        │   └─ BrandMonitorChecker.ExecuteAsync()
        │       ├─ Por cada site en [Patprimo, SevenSeven, Atmos, Ostu]:
        │       │   ├─ currentCount  = CountBySiteAsync(now - window, now)
        │       │   ├─ prevSnapshot  = GetSnapshotBeforeAsync(site, now - window)
        │       │   │   ├─ null  → status=NoData, previousCount=null
        │       │   │   └─ exist → previousCount=prevSnapshot.PendingCountCurrent
        │       │   │              status=DetermineStatus(current, previous)
        │       │   └─ InsertAsync(BrandSnapshot.Create(site, current, previous, status))
        │       └─ [append-only — nunca update]
        │
        └─ Scope fresco #2 (IServiceScopeFactory.CreateAsyncScope)
            └─ IBrandSnapshotRepository.GetLatestPerSiteAsync()
                └─ ToListAsync() → GroupBy(Site).Select(g => g.First())
                   (agrupación en memoria — workaround EF Core GroupBy SQL)

[UI re-renderiza con StateHasChanged() en finally]
```

---

## 2. Regla de determinación de estado

```csharp
private static SnapshotStatus DetermineStatus(int current, int previous)
{
    var drop = previous - current;
    if (drop > 0)  return SnapshotStatus.Green;   // bajaron
    if (drop < 0)  return SnapshotStatus.Red;     // subieron
    return SnapshotStatus.Yellow;                  // igual
}
```

---

## 3. Ventana de comparación configurable

| Entorno | Clave | Valor | Propósito |
|---------|-------|-------|-----------|
| Development | `Monitoring:BrandMonitorWindowSeconds` | `15` | Pruebas rápidas — esperar 15s entre clics |
| Production | `Monitoring:BrandMonitorWindowSeconds` | `600` | 10 minutos reales |

Lectura en `BrandMonitorChecker`:
```csharp
var windowSeconds = config.GetValue("Monitoring:BrandMonitorWindowSeconds", 600);
var window        = TimeSpan.FromSeconds(windowSeconds);
```

---

## 4. Patrón IServiceScopeFactory — por qué es obligatorio

Blazor Server crea un `AppDbContext` **Scoped** que vive todo el circuito. Si ese contexto lanza una excepción (unique constraint, timeout, etc.), queda en estado roto y **no se recupera** — todos los `SaveChangesAsync` siguientes fallan silenciosamente.

**Solución:** `BrandMonitorService` recibe solo `IServiceScopeFactory` y crea un scope fresco por operación. Esto garantiza un `AppDbContext` limpio independiente del estado del circuito Blazor.

```csharp
await using var scope = scopeFactory.CreateAsyncScope();
var repo = scope.ServiceProvider.GetRequiredService<IBrandSnapshotRepository>();
```

---

## 5. Lógica de badge en UI — derivada de `hasHistory`

La UI **no usa `s.Status`** almacenado para determinar el badge. En su lugar:

```csharp
var hasHistory = s.PendingCountPrevious.HasValue;
var drop       = hasHistory ? s.PendingCountPrevious!.Value - s.PendingCountCurrent : (int?)null;
var badgeColor = hasHistory ? DropBadgeClass(drop)  : "secondary";
var badgeLabel = hasHistory ? DropBadgeLabel(drop)   : "Sin datos";
```

**Por qué:** `s.Status = NoData` puede haberse guardado en una transición pero si `PendingCountPrevious` tiene valor, hay histórico real. El badge debe reflejar el estado actual, no el almacenado.

---

## 6. Comportamiento esperado por escenario

| Escenario | `PendingCountPrevious` | Badge | Columna "Hace 10 min" |
|-----------|------------------------|-------|----------------------|
| 1er clic, sin histórico | `NULL` | ⬜ Sin datos | `—` |
| 2do clic (>15s después) | valor real (ej. 18) | 🟢/🟡/🔴 según drop | `18` |
| `previous=0, current=0` | `0` | 🟡 Moderado | `0` |
| `previous=10, current=5` | `10` | 🟢 OK (bajó) | `10` |
| `previous=5, current=12` | `5` | 🔴 Crítico (subió) | `5` |

---

## 7. Workaround EF Core GroupBy

`GroupBy(...).Select(g => g.First())` **no se traduce confiablemente** a SQL en EF Core 8 + PostgreSQL/SQL Server. Puede retornar vacío sin lanzar excepción.

**Fix aplicado en `GetLatestPerSiteAsync`:**
```csharp
var all = await context.BrandSnapshots
    .AsNoTracking()
    .OrderByDescending(s => s.CheckedAt)
    .ToListAsync(ct);                    // carga completa en memoria

return all
    .GroupBy(s => s.Site)
    .Select(g => g.First())             // primero = más reciente por el OrderByDescending
    .OrderBy(s => s.Site)
    .ToList();
```

---

## 8. Paso 7 pendiente — Retención de histórico

```csharp
// Agregar en IncidentMaintenanceService.cs
await brandSnapshotRepo.DeleteOlderThanAsync(DateTime.UtcNow.AddHours(-48), ct);
```

Requiere inyectar `IBrandSnapshotRepository` en `IncidentMaintenanceService` y registrarlo en `Program.cs` dentro del scope de ese servicio.

---

## 9. Archivos modificados

| Archivo | Cambio |
|---------|--------|
| `src/MonitorPedidos.Web/Features/Monitoring/BrandMonitorChecker.cs` | Ventana configurable; `GetSnapshotBeforeAsync`; `InsertAsync`; `NoData` cuando sin histórico |
| `src/MonitorPedidos.Web/Services/BrandMonitorService.cs` | Solo `IServiceScopeFactory` + `ILogger`; `SimulateAndRefreshAsync` con scopes frescos + simulación variada |
| `src/MonitorPedidos.Web/Services/IBrandMonitorService.cs` | `+SimulateAndRefreshAsync()` |
| `src/MonitorPedidos.Web/Components/Pages/BrandMonitor/BrandMonitorPage.razor` | Badge desde `hasHistory`; `try/finally`; `StateHasChanged()` |
| `src/MonitorPedidos.Web/Components/Pages/Dashboard.razor` | ídem |
| `src/MonitorPedidos.Web/appsettings.json` | `BrandMonitorWindowSeconds: 600` |
| `src/MonitorPedidos.Web/appsettings.Development.json` | `BrandMonitorWindowSeconds: 15`; Console sink |
| `src/MonitorPedidos.Web/Program.cs` | `AddScoped<BrandMonitorChecker>()`; registro doble con `ICheckExecutor` |
