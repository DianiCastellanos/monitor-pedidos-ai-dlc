# IT3 — Brand Monitor Redesign · Domain Entities

**Fecha:** 2026-05-30  
**Iteración:** IT3 — Brand Monitor (append-only, histórico real, NoData)  
**Stories relacionadas:** US-30 (tablero de estado por marca), RF-31  
**Estado:** ✅ Completado — Paso 7 (retención) diferido

---

## 1. Entidad `BrandSnapshot`

### Propiedades

| Propiedad | Tipo | Nullable | Descripción |
|-----------|------|----------|-------------|
| `Id` | `Guid` | No | PK generado |
| `Site` | `string` | No | Marca: Patprimo, SevenSeven, Atmos, Ostu |
| `PendingCountCurrent` | `int` | No | Pedidos pendientes al momento del chequeo |
| `PendingCountPrevious` | `int?` | **Sí** | Pedidos del snapshot anterior. `null` = primer chequeo (sin histórico) |
| `Status` | `SnapshotStatus` | No | Valor calculado al insertar. UI usa `hasHistory`, no `Status` |
| `CheckedAt` | `DateTime` | No | UTC. Índice compuesto con `Site` |

### Factory method

```csharp
public static BrandSnapshot Create(
    string         site,
    int            currentPending,
    int?           previousPending,   // null en primer chequeo
    SnapshotStatus status)
```

**Invariante:** solo se crea con `Create()` — sin setters públicos. No existe método `Upsert()`.

---

## 2. Enum `SnapshotStatus`

```csharp
public enum SnapshotStatus
{
    Green,    // pendientes bajaron >= umbral
    Yellow,   // bajaron pero menos que umbral
    Red,      // subieron o igual
    NoData    // no existe snapshot anterior (primer chequeo)
}
```

**Nota:** la UI **no** deriva el badge de `Status` almacenado — deriva de `PendingCountPrevious.HasValue` para evitar inconsistencias con estados guardados en transición.

---

## 3. Interfaz `IBrandSnapshotRepository`

```csharp
public interface IBrandSnapshotRepository
{
    // append-only — nunca UpsertAsync
    Task InsertAsync(BrandSnapshot snapshot, CancellationToken ct = default);

    // carga-y-agrupa en memoria (workaround EF Core GroupBy SQL)
    Task<IReadOnlyList<BrandSnapshot>> GetLatestPerSiteAsync(CancellationToken ct = default);

    // histórico real para comparación "hace N segundos"
    Task<BrandSnapshot?> GetSnapshotBeforeAsync(string site, DateTime before, CancellationToken ct = default);

    // retención (Paso 7 — diferido)
    Task DeleteOlderThanAsync(DateTime cutoff, CancellationToken ct = default);
}
```

---

## 4. Índice de base de datos

| Campo | Tipo de índice |
|-------|---------------|
| `(site, checked_at)` | Compuesto, **no único** |

El índice único original (`site`) fue eliminado con la migración `20260528162611_BrandSnapshotsAppendOnly`. Múltiples filas por site son el comportamiento esperado (append-only).

---

## 5. Migraciones aplicadas

| Migración | Cambio |
|-----------|--------|
| `20260528162611_BrandSnapshotsAppendOnly` | Drop índice único `IX_brand_snapshots_Site`; crea índice compuesto `IX_brand_snapshots_Site_CheckedAt` |
| `20260528171754_BrandSnapshotPreviousNullable` | `pending_count_previous` → nullable |

---

## 6. Archivos modificados

| Archivo | Cambio |
|---------|--------|
| `src/MonitorPedidos.Domain/Dashboard/BrandSnapshot.cs` | `int?` en `PendingCountPrevious`; `SnapshotStatus.NoData`; `Create()` acepta `int?`; sin `Upsert()` |
| `src/MonitorPedidos.Domain/Dashboard/IBrandSnapshotRepository.cs` | `InsertAsync`, `GetLatestPerSiteAsync`, `GetSnapshotBeforeAsync`, `DeleteOlderThanAsync`; sin `UpsertAsync` |
| `src/MonitorPedidos.Infrastructure/Dashboard/BrandSnapshotRepository.cs` | Implementación con carga-y-agrupa en memoria |
| `src/MonitorPedidos.Infrastructure/Persistence/Configurations/BrandSnapshotConfiguration.cs` | Índice compuesto, `IsRequired(false)` en previous |
