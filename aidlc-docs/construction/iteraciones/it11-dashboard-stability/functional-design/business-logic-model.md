# IT11 — Dashboard Stability & M3 Per-API Detail · Business Logic Model

**Fecha:** 2026-05-31
**Iteración:** IT11 — Estabilidad del Dashboard y detalle por integración en M3
**Estado:** Completado
**Unidades afectadas:** U6 (Dashboard/NOC UI)

---

## 1. Objetivo

- Brand Monitor: refresh no-destructivo — la tabla permanece visible durante el ciclo de actualización
- M3 (APIs Externas): mostrar estado individual por integración (Salesforce / Multivende) siempre que haya datos
- Estado "secondary" (gris): solo durante los primeros ~30s de arranque, no en refreshes posteriores
- Timer UI: countdown siempre visible en todos los estados

---

## 2. Brand Monitor — Refresh no-destructivo

### Antes

El refresh del Brand Monitor vaciaba `_brandSnapshots` al inicio, causando que la tabla desapareciera y mostrara el skeleton durante 1-3 segundos en cada ciclo de 60s.

### Después

```csharp
private async Task RefreshBrandAsync(bool isManual = false)
{
    _brandRefreshing = true;   // spinner inline "Actualizando..." — tabla permanece visible
    StateHasChanged();

    try
    {
        var snapshots = await BrandMonitorService.GetCurrentSnapshotsAsync();
        _brandSnapshots  = snapshots;
        _liveCounts      = null;
        _isLiveFallback  = false;
        _brandStale      = false;
    }
    catch (Exception ex)
    {
        // Si ya había datos previos (snapshots o live), marcar como stale
        if (_brandSnapshots?.Count > 0 || _liveCounts?.Count > 0)
            _brandStale = true;   // banner "Actualizando..." sin borrar datos

        // Fallback desde LastCheckStore
        var liveCounts = GetBrandCountsFromLastCheckStore();
        if (liveCounts is not null)
        {
            _liveCounts      = liveCounts;
            _isLiveFallback  = true;
        }
        else if (isManual)
        {
            // Botón manual → llamar Salesforce directamente
            _liveCounts     = await BrandMonitorService.GetLiveCountsAsync();
            _isLiveFallback = _liveCounts is not null;
        }
    }
    finally
    {
        _brandRefreshing = false;
        StateHasChanged();
    }
}
```

### Regla _brandStale

`_brandStale = true` solo se activa cuando:
- La BD falla durante un refresh
- Y ya existían datos previos (snapshots de BD o live counts)

`_brandStale` NO se activa si el único estado anterior era el skeleton (sin datos) — en ese caso simplemente se mantiene el skeleton.

---

## 3. stateUnknown — spinner solo en primera carga

### Antes

`_stateUnknown` (spinner "cargando...") se mostraba en cada refresh de la card de estado principal.

### Después

```csharp
// _stateUnknown = true solo mientras no ha corrido ningún checker
private bool _stateUnknown => !_lastCheckStore.Results.Any();
```

Una vez que al menos un checker ha corrido (~30s desde arranque), `_stateUnknown` pasa a `false` definitivamente. Los refreshes posteriores no vuelven a mostrarlo.

---

## 4. M3 — Detalle por integración (ApiDetails)

### Antes

La card M3 "APIs Externas" mostraba un único badge con el estado agregado (peor de Salesforce/Multivende). Sin detalle por integración.

### Después

```csharp
// En DomainCard model
public sealed record DomainCard
{
    // ...props existentes...
    public List<ApiItem>? ApiDetails { get; init; }  // nuevo
}

public sealed record ApiItem(string Label, CheckStatus? Status, string? Detail);
```

### BuildApiItem — helper

```csharp
private ApiItem BuildApiItem(string label, ModuleId module)
{
    var result = _lastCheckStore.Results.GetValueOrDefault(module);
    return new ApiItem(
        Label:  label,
        Status: result?.Status,
        Detail: result?.Status == CheckStatus.Critical ? result.Details : null);
}
```

### Render en M3 card

```
┌──────────────────────────────┐
│ M3  APIs Externas            │
│   ● (peor estado)            │
│                              │
│ Salesforce  →  ✅ Disponible │
│ Multivende  →  ✅ Disponible │
│                              │
│ 14:32:15                     │
└──────────────────────────────┘
```

Si Salesforce está en Critical:
```
│ Salesforce  →  ❌ Sin respuesta │
│                HTTP 401 — token │
│ Multivende  →  ✅ Disponible   │
```

El detalle técnico (ej. "HTTP 401 — token inválido") solo aparece en estado Critical.

---

## 4b. Regla crítica — M3 siempre refleja el peor estado

**El estado del card M3 es siempre el peor estado entre Salesforce y Multivende.**

- Salesforce = Ok, Multivende = Critical → M3 = **Critical**
- Salesforce = Critical, Multivende = Ok → M3 = **Critical**
- Ambas Ok → M3 = **Ok**
- M3 **nunca puede mostrar verde** si alguna integración está en rojo

Esta regla aplica en Dashboard y en NOC sin excepción. La lógica está en `ApplyApisWorstStatus`.

---

## 5. ApplyApisWorstStatus — secondary solo en arranque

### Antes

La card M3 mostraba estado "secondary" (gris) en cualquier situación donde los checkers de API no tuvieran datos, incluyendo durante refreshes periódicos.

### Después

```csharp
private void ApplyApisWorstStatus(DomainCard card)
{
    var sf  = _lastCheckStore.Results.GetValueOrDefault(ModuleId.SalesforceApi);
    var mv  = _lastCheckStore.Results.GetValueOrDefault(ModuleId.MultivendeApi);

    // secondary SOLO si ningún checker de API ha corrido jamás (arranque)
    if (sf is null && mv is null)
    {
        card = card with { Status = null };  // secondary
        return;
    }

    // Escalate-only: una vez que tenemos datos, nunca bajar a secondary
    var statuses = new[] { sf?.Status, mv?.Status }
        .Where(s => s.HasValue)
        .Select(s => s!.Value);

    var worst = statuses.Contains(CheckStatus.Critical) ? CheckStatus.Critical
              : statuses.Contains(CheckStatus.Warn)     ? CheckStatus.Warn
              :                                           CheckStatus.Ok;

    card = card with { Status = worst };
}
```

**Escalate-only:** si el checker de Salesforce corrió y está Ok, pero el de Multivende aún no ha corrido (null), se usa el dato conocido (Ok de Salesforce) como estado parcial — no se regresa a secondary.

---

## 5b. Regla crítica — el timestamp "Actualizado" es del checker, no de la UI

El campo "Actualizado" en el header del Brand Monitor refleja cuándo el **checker background** (`BrandMonitorChecker`) consultó Salesforce por última vez.

- En modo normal: viene del `CheckedAt` del snapshot más reciente guardado en BD
- En modo fallback: viene del `CheckedAt` almacenado en `LastCheckStore[BrandMonitor]`

**El timestamp NO refleja:**
- Cuándo la UI hizo su último refresh de 60s
- Cuándo el usuario pulsó el botón "↻ Actualizar"
- Cuándo se renderizó la página

Si el checker corrió hace 2 minutos y la UI hizo refresh hace 5 segundos, el campo muestra "hace 2 min" — no "hace 5s". Esto es intencional: el dato relevante para el negocio es cuándo fue la última consulta real a Salesforce.

---

## 6. Timer UI — countdown siempre visible

### Antes

El countdown de "Próx. verificación" solo aparecía cuando había datos. En estado de arranque o sin datos, no se mostraba.

### Después

El countdown es visible en todos los estados, incluyendo arranque. El header del Brand Monitor siempre muestra:

```
Actualizado · [timestamp] · Próx. verificación · Checker Salesforce: cada 3 min
```

---

## 7. ApiStatusClass — helper CSS

```csharp
private static string ApiStatusClass(CheckStatus? status) => status switch
{
    CheckStatus.Ok       => "table-success",
    CheckStatus.Warn     => "table-warning",
    CheckStatus.Critical => "table-danger",
    _                    => "table-secondary"
};
```

Coloreado por fila en la sub-tabla de APIs dentro de M3.

---

## 8. Tabla Brand Monitor — skeleton persistente

Antes de IT11, si `_brandSnapshots` se vaciaba en el refresh, se mostraba momentáneamente la frase "Esperando datos del scheduler...".

Después: la tabla siempre renderiza con las 4 marcas (PatPrimo / SevenSeven / Ostu / Atmos). Si no hay datos, muestra "—" en los conteos. El skeleton nunca desaparece — siempre hay estructura visible.

---

## 9. Archivos modificados

| Archivo | Cambio |
|---------|--------|
| `Components/Pages/Dashboard.razor` | Refresh no-destructivo; `_brandStale` condicional; `stateUnknown` solo en arranque; timer countdown siempre visible; header Brand Monitor; `GetBrandCountsFromLastCheckStore()` |
| `Components/Pages/Noc/NocPage.razor` | `StateHasChanged()` al final de `RefreshAsync` — ya no se perdían actualizaciones |
| `Components/Shared/BrandMonitorTable.razor` | Skeleton persistente con 4 marcas y "—" |
| `Components/Pages/Dashboard.razor` | `DomainCard.ApiDetails`; `BuildApiItem`; `ApiStatusClass`; render sub-tabla M3 |
