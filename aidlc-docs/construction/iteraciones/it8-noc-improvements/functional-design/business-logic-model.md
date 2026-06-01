# IT8 — NOC Page Improvements · Business Logic Model

**Fecha:** 2026-05-31
**Iteración:** IT8 — Mejoras a la vista NOC y redefinición de M3
**Estado:** Completado
**Unidades afectadas:** U6 (Dashboard/NOC UI), U4 (External Integrations)

---

## 1. Objetivo

Mejorar la legibilidad y precisión del modo NOC para pantallas de TV:

- `BrandMonitorTable`: mostrar tendencias reales con flechas (↑ ↓ =)
- M3 APIs Externas: cambiar semántica de "Pendientes" a "Disponibilidad" — el checker ya no evalúa volumen de pedidos, solo disponibilidad del servicio
- `ApiStatusBadge`: agregar soporte para estado `Warn` con color amarillo
- Timing: reducir ciclo dev a 30s para validaciones ágiles

---

## 2. Cambio de semántica en M3 — SalesforceApiChecker

### Antes (IT5)

`SalesforceApiChecker` retornaba `Warn` si había pedidos pendientes por site, `Critical` si total >= umbral. El resultado representaba **volumen de pedidos**.

### Después (IT8)

`SalesforceApiChecker` solo evalúa **disponibilidad del API**:

- `CheckResult.Ok` → Salesforce responde correctamente (200 OK con body válido)
- `CheckResult.Critical` → Salesforce no responde (401, timeout, error de red)
- `CheckResult.Warn` → eliminado de `SalesforceApiChecker` (el Warn queda para otros casos)

**Razón:** La semántica de "pedidos pendientes por descargar" pertenece al Brand Monitor (M11 ampliado), no a M3 (disponibilidad de APIs). Mezclar ambas en un checker creaba ambigüedad.

---

## 3. Flujo Brand Monitor en NocPage

```
NocPage.RefreshAsync()
    │
    ├── GetLatestPerSiteAsync() → IReadOnlyList<BrandSnapshot>
    │       │
    │       ├── OK → _brandSnapshots = snapshots
    │       │        ParseBrandCountsFromLastCheckStore() (no usado — BD disponible)
    │       │
    │       └── Excepción → ParseBrandCountsFromLastCheckStore()
    │               │
    │               ├── LastCheckStore[BrandMonitor].Details != null
    │               │       → _liveCounts = ParsePipeFormat(details)
    │               │         _isLiveFallback = true
    │               │
    │               └── Details nulo → _brandSnapshots = null, _liveCounts = null
    │
    └── StateHasChanged()
```

### ParsePipeFormat

`LastCheckStore[BrandMonitor].Details` tiene formato: `"PatPrimo:14|SevenSeven:3|Ostu:6|Atmos:1"`

```csharp
private IReadOnlyDictionary<string, int>? ParseBrandCountsFromLastCheckStore()
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

## 4. BrandMonitorTable — componente actualizado

### Parámetros

```csharp
[Parameter] public IReadOnlyList<BrandSnapshot>?         Snapshots     { get; set; }
[Parameter] public IReadOnlyDictionary<string, int>?     LiveCounts    { get; set; }
[Parameter] public bool                                   IsLiveFallback { get; set; }
```

### Lógica de renderizado

| Condición | Qué se muestra |
|-----------|---------------|
| `Snapshots != null && HasItems` | Tabla completa con tendencia (↑ ↓ =) desde `SnapshotStatus` |
| `LiveCounts != null && IsLiveFallback` | Tabla con conteos, badge amarillo "⚠ Sin historial" por fila |
| `Snapshots == null && LiveCounts == null` | Skeleton con las 4 marcas (PatPrimo / SevenSeven / Ostu / Atmos) y "—" en conteos |

### Flechas de tendencia desde SnapshotStatus

```
SnapshotStatus.Green  → ↓ (bajaron)  → verde
SnapshotStatus.Yellow → =  (igual)    → amarillo
SnapshotStatus.Red    → ↑ (subieron) → rojo
SnapshotStatus.NoData → —             → gris
```

---

## 5. ApiStatusBadge — soporte para Warn

Antes solo soportaba `Ok` (verde) y `Critical` (rojo).

Después:

| Estado | CSS | Label |
|--------|-----|-------|
| `Ok` | `badge bg-success` | Disponible |
| `Warn` | `badge bg-warning text-dark` | Con advertencias |
| `Critical` | `badge bg-danger` | Sin respuesta |
| `null` / desconocido | `badge bg-secondary` | Sin datos |

---

## 6. GetApisStatus() — propagación correcta de Warn

```csharp
private CheckStatus? GetApisStatus()
{
    var sf  = _lastCheckStore.Results.GetValueOrDefault(ModuleId.SalesforceApi);
    var mv  = _lastCheckStore.Results.GetValueOrDefault(ModuleId.MultivendeApi);

    if (sf is null && mv is null) return null;   // secondary

    var statuses = new[] { sf?.Status, mv?.Status }
        .Where(s => s.HasValue)
        .Select(s => s!.Value)
        .ToList();

    if (statuses.Contains(CheckStatus.Critical)) return CheckStatus.Critical;
    if (statuses.Contains(CheckStatus.Warn))     return CheckStatus.Warn;
    return CheckStatus.Ok;
}
```

---

## 7. GetApiShortDetail() — detalle técnico solo en Critical

```csharp
private string? GetApiShortDetail(ModuleId module)
{
    var result = _lastCheckStore.Results.GetValueOrDefault(module);
    if (result?.Status == CheckStatus.Critical)
        return result.Details;
    return null;  // Ok y Warn no muestran detalle técnico en NOC
}
```

---

## 8. Timing actualizado

| Componente | Antes | Después | Motivo |
|-----------|-------|---------|--------|
| `CheckerIntervalMinutes` (dev) | 1 min | 0.5 min (30s) | Ciclos más ágiles en desarrollo |
| NOC refresh timer | 30s | 15s | Respuesta visual más rápida en TV |

---

## 9. Archivos modificados

| Archivo | Cambio |
|---------|--------|
| `Features/ApiChecks/SalesforceApiChecker.cs` | Solo disponibilidad (Ok/Critical), sin Warn por volumen |
| `Components/Shared/BrandMonitorTable.razor` | `SnapshotStatus` real + flechas tendencia + skeleton 4 marcas |
| `Components/Shared/ApiStatusBadge.razor` | Estado `Warn` (amarillo) + labels descriptivos |
| `Components/Pages/Noc/NocPage.razor` | `GetApisStatus()` con Warn; `GetApiShortDetail()` solo Critical; `ParseBrandCountsFromLastCheckStore()`; timer 15s |
| `appsettings.Development.json` | `CheckerIntervalMinutes: 0.5` |
