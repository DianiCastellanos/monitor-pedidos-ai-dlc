# Plan IT11 — Dashboard Stability & M3 Per-API Detail

**Fecha:** 2026-05-31
**Estado:** Completado — validado 2026-05-31
**Unidades afectadas:** U6 (Dashboard/NOC UI)

---

## Objetivo

- Brand Monitor: refresh no-destructivo — tabla visible durante actualización
- M3: mostrar estado individual por integración (Salesforce / Multivende) con detalle técnico solo en Critical
- `_stateUnknown` (spinner) solo durante los primeros ~30s de arranque
- Timer countdown siempre visible
- `secondary` (gris) solo cuando ningún checker ha corrido jamás

---

## Decisiones de diseño

| # | Decisión | Opción elegida | Razón |
|---|----------|----------------|-------|
| D1 | Refresh Brand Monitor | No destruir datos durante refresh | UX degradada cuando tabla desaparece 1-3s cada 60s |
| D2 | `_brandStale` | Solo si ya había datos | Si no había datos, el skeleton es el estado correcto |
| D3 | `stateUnknown` | Basado en `_lastCheckStore.Results.Any()` | Transitorio real, no dependiente de la UI |
| D4 | ApiDetails | `List<ApiItem>` en DomainCard | Extensión limpia del model — sin acoplamiento en template |
| D5 | `secondary` M3 | Solo en arranque (ambos null) | Una vez con datos, nunca regresar a gris |

---

## Checklist de implementación

- [x] `Dashboard.razor`: `RefreshBrandAsync(bool isManual)` — spinner inline sin destruir tabla
- [x] `Dashboard.razor`: `_brandStale` solo si había datos previos
- [x] `Dashboard.razor`: `_stateUnknown` desde `!_lastCheckStore.Results.Any()`
- [x] `DomainCard` model: + `List<ApiItem>? ApiDetails`
- [x] `ApiItem` record: `(string Label, CheckStatus? Status, string? Detail)`
- [x] `BuildApiItem` helper: convierte `CheckResult?` → `ApiItem`
- [x] `ApiStatusClass` helper: CSS por estado
- [x] `ApplyApisWorstStatus`: secondary solo cuando ambos null; escalate-only
- [x] M3 card template: sub-tabla Salesforce/Multivende con detalle técnico en Critical
- [x] Timer countdown: visible en todos los estados
- [x] Header Brand Monitor: "Actualizado · Próx. verificación · Checker Salesforce: cada X min"
- [x] `BrandMonitorTable.razor`: skeleton persistente 4 marcas (nunca "Esperando datos...")
- [x] `NocPage.razor`: `StateHasChanged()` al final de `RefreshAsync` (fix: pantalla se actualizaba al final)

---

## Checklist de validación

- [x] Durante refresh de Brand Monitor: tabla permanece visible con datos anteriores + spinner "Actualizando..."
- [x] M3 muestra "Salesforce → Disponible" y "Multivende → Disponible" cuando ambos OK
- [x] M3 muestra detalle técnico solo cuando estado es Critical
- [x] M3 no vuelve a secondary después del primer dato (escalate-only)
- [x] Spinner "Cargando..." solo aparece durante los primeros ~30s, no en refreshes
- [x] Timer countdown visible en pantalla de arranque
- [x] NOC se actualiza correctamente cada 15s (StateHasChanged fix)
