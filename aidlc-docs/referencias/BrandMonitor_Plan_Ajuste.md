# Brand Monitor — Plan de Ajuste

**Referencia**: `BrandMonitor_Diseño_Comportamient_Referencia.md`  
**Fecha última actualización**: 2026-05-28  
**Estado**: ✅ Funcional — Paso 7 (retención) diferido

---

## Checklist de inicio de sesión

Al comenzar una nueva sesión de trabajo en este módulo, verificar:

1. **PostgreSQL corriendo** en `localhost:5432` — base `MonitorPedidosDb`
2. **Migraciones aplicadas** — ejecutar si hay migraciones nuevas:
   ```bash
   dotnet ef database update --project src/MonitorPedidos.Infrastructure --startup-project src/MonitorPedidos.Web
   ```
   Si falla porque `__EFMigrationsHistory` no está sincronizada, usar `aidlc-docs/referencias/fix_migrations.sql` como plantilla.
3. **Levantar la app**:
   ```powershell
   $env:ASPNETCORE_ENVIRONMENT = "Development"
   Start-Process -FilePath "dotnet" -ArgumentList "run","--no-build","--urls","http://localhost:5000" -WorkingDirectory "src/MonitorPedidos.Web"
   ```
4. **Probar Brand Monitor**: clic en "Actualizar" → esperar 15 s → clic de nuevo → debe mostrar datos no-cero y cambios históricos reales.

---

## Estado de ejecución de pasos

| Paso | Descripción | Estado |
|------|-------------|--------|
| 1 | Dominio: `BrandSnapshot` nullable + `NoData` | ✅ Completado |
| 2 | Dominio + Infra: interfaz y repositorio append-only | ✅ Completado |
| 3 | Migración: drop unique index + índice compuesto | ✅ `20260528162611_BrandSnapshotsAppendOnly` |
| 4 | Migración: `pending_count_previous` nullable | ✅ `20260528171754_BrandSnapshotPreviousNullable` |
| 5 | Checker: comparar vs snapshot histórico real | ✅ Completado |
| 6 | Servicio: scope aislado + simulación variada | ✅ Completado |
| 7 | Retención de histórico (`DeleteOlderThanAsync`) | 🔜 Diferido |
| 8 | UI: estado `NoData` / badge basado en `hasHistory` | ✅ Completado |
| 9 | DB: sincronización `__EFMigrationsHistory` | ✅ Completado — `fix_migrations.sql` ejecutado |
| 10 | UI: refresco correcto post-Actualizar | ✅ Completado |
| 11 | Simulación: datos variados no-cero por marca | ✅ Completado |

---

## Arquitectura del flujo (estado actual)

```
[Botón "Actualizar"]
        │
        ▼
BrandMonitorService.SimulateAndRefreshAsync()
        │
        ├─ Crea scope fresco (IServiceScopeFactory)
        │   ├─ ISimulatedOrderRepository → InsertRangeAsync(órdenes variadas por marca)
        │   │   └─ 2-15 órdenes × 2 fuentes × 4 marcas = datos no-cero
        │   └─ BrandMonitorChecker.ExecuteAsync()
        │       ├─ CountBySiteAsync(now - windowSeconds, now) → conteo actual
        │       ├─ GetSnapshotBeforeAsync(site, now - windowSeconds) → histórico real
        │       │   └─ null → Status=NoData, previous=null
        │       │   └─ existe → DetermineStatus(actual, anterior)
        │       └─ InsertAsync(nuevo snapshot) ← append-only
        │
        └─ Crea segundo scope fresco
            └─ IBrandSnapshotRepository.GetLatestPerSiteAsync()
                └─ Carga todos ordenados desc → agrupa en memoria → 1 por marca
                   (evita GroupBy SQL que EF Core no traduce confiablemente)

[UI re-renderiza con StateHasChanged() en finally]
```

---

## Configuración de desarrollo vs producción

| Clave | Dev (`appsettings.Development.json`) | Prod (`appsettings.json`) |
|-------|--------------------------------------|---------------------------|
| `Monitoring:BrandMonitorWindowSeconds` | `15` — pruebas rápidas (esperar 15s entre clics) | `600` — 10 minutos reales |
| `Monitoring:CheckerIntervalMinutes` | `1` | `5` |
| `Simulation:Enabled` | `true` | `false` |
| `Simulation:InsertIntervalMinutes` | `5` | — |
| `Serilog:WriteTo` | Console + File | File |

---

## Lecciones aprendidas — problemas resueltos

### 1. `__EFMigrationsHistory` desincronizada

**Síntoma**: `dotnet ef database update` falla con `42P07: relation already exists`.  
**Causa**: Las tablas existen en la BD pero el historial EF está vacío.  
**Fix**: Ejecutar `aidlc-docs/referencias/fix_migrations.sql` en el cliente de BD.  
**Prevención**: Siempre correr `dotnet ef database update` después de `dotnet ef migrations add`.

### 2. `SaveChangesAsync` falla silenciosamente en el circuito Blazor

**Síntoma**: El botón "Actualizar" no inserta filas. Los logs muestran `ERR: Checker threw`.  
**Causa**: El `AppDbContext` del circuito Blazor Server queda en estado roto después de un error previo (unique constraint). Al ser `Scoped`, el mismo contexto se reutiliza toda la sesión.  
**Fix**: `SimulateAndRefreshAsync` y `GetCurrentSnapshotsAsync` usan `IServiceScopeFactory.CreateAsyncScope()` → contexto fresco por ejecución.

### 3. `GetLatestPerSiteAsync` retornaba vacío

**Síntoma**: UI muestra "Sin datos aún" aunque hay filas en la BD.  
**Causa**: `GroupBy(...).Select(g => g.OrderByDescending(...).First())` no se traduce a SQL confiablemente en EF Core 8 + PostgreSQL. Puede retornar vacío sin lanzar excepción.  
**Fix**: Cargar todas las filas ordenadas (`OrderByDescending`), luego agrupar en memoria con `.GroupBy().Select(g => g.First())`.

### 4. Todos los conteos en cero

**Síntoma**: Brand Monitor muestra `current=0`, `previous=0`, estado `🟡 Moderado` para todo.  
**Causa**: La ventana de 15s raramente tiene órdenes porque el simulador inserta cada 5 min.  
**Fix**: `SimulateAndRefreshAsync` inserta órdenes variadas (2-15 por marca) antes de correr el checker. La comparación histórica sigue siendo correcta porque "anterior" proviene del snapshot almacenado, no del reconteo.

### 5. UI no se actualizaba después de clic

**Síntoma**: Tabla mantiene datos viejos o `_loading` queda en `true`.  
**Causa**: Sin `try/finally` en `RefreshAsync`. Sin `StateHasChanged()` explícito al terminar.  
**Fix**: Ambas páginas usan `try/finally { _loading = false; StateHasChanged(); }`.

### 6. `pending_count_previous = 0` mostraba "Sin datos"

**Síntoma**: Hay datos históricos reales pero la UI muestra "Sin datos".  
**Causa**: Badge derivado de `s.Status` almacenado. Si `Status = NoData` (guardado en transición) pero `previous = 0` (dato real), el badge decía "Sin datos".  
**Fix**: Badge derivado de `hasHistory = s.PendingCountPrevious.HasValue`. `s.Status` ignorado en la UI — solo `null` produce "Sin datos".

---

## Archivos clave modificados

```
Domain/
  Dashboard/
    BrandSnapshot.cs              int? PendingCountPrevious; SnapshotStatus.NoData
    IBrandSnapshotRepository.cs   InsertAsync / GetLatestPerSiteAsync / GetSnapshotBeforeAsync / DeleteOlderThanAsync

Infrastructure/
  Dashboard/
    BrandSnapshotRepository.cs    append-only; GetLatestPerSiteAsync carga-y-agrupa en memoria
  Migrations/
    20260528162611_BrandSnapshotsAppendOnly.cs
    20260528171754_BrandSnapshotPreviousNullable.cs

Web/
  Features/Monitoring/
    BrandMonitorChecker.cs        ventana configurable; histórico real; NoData cuando no hay snapshot
  Services/
    BrandMonitorService.cs        scope fresco para escritura Y lectura; simulación variada
  Components/Pages/BrandMonitor/
    BrandMonitorPage.razor        badge desde hasHistory; try/finally; StateHasChanged
  Components/Pages/
    Dashboard.razor               ídem
  appsettings.json                BrandMonitorWindowSeconds: 600
  appsettings.Development.json    BrandMonitorWindowSeconds: 15; Console sink; Simulation enabled

aidlc-docs/referencias/
  fix_migrations.sql              Script para sincronizar __EFMigrationsHistory manualmente
  BrandMonitor_Plan_Ajuste.md     Este archivo
```

---

## Paso 7 pendiente — Retención de histórico

Agregar en `IncidentMaintenanceService.cs`:
```csharp
await brandSnapshotRepo.DeleteOlderThanAsync(DateTime.UtcNow.AddHours(-48), ct);
```
Requiere inyectar `IBrandSnapshotRepository` en ese servicio y registrarlo en `Program.cs` si no está ya disponible en ese scope.

---

## Comportamiento esperado por escenario

| Escenario | `pending_count_previous` | Badge | Columna "Hace 10 min" |
|-----------|--------------------------|-------|----------------------|
| 1er clic, sin histórico | `NULL` | ⬜ Sin datos | `—` |
| 2do clic (>15s después) | valor real (ej. 18) | 🟢/🟡/🔴 según drop | `18` |
| `previous=0, current=0` | `0` | 🟡 Moderado | `0` |
| `previous=10, current=5` | `10` | 🟢 OK (bajó) | `10` |
| `previous=5, current=12` | `5` | 🔴 Crítico (subió) | `5` |
