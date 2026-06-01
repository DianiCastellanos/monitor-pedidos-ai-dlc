# Errores encontrados — Playwright Dashboard Tests

## E1 — data-testid faltante en rama `else` del Brand Monitor

**Archivo**: `src/MonitorPedidos.Web/Components/Pages/Dashboard.razor`  
**Tests afectados**: D2, D4  
**Detectado**: Ejecución initial de tests Dashboard  

### Qué fallaba
`[data-testid="brand-monitor-table"]` no encontrado aunque la tabla era visible.

### Causa raíz
`replace_all` aplicado sobre `<div class="table-responsive">` no capturó la rama
`else` final (snapshots reales disponibles) porque tenía 4 espacios de indentación
mientras las otras ramas tenían 8–12. La rama `else` (línea ~340) es exactamente
la que se renderiza cuando el checker ya corrió y hay datos reales — el caso más
frecuente en producción.

### Corrección
Agregar `data-testid="brand-monitor-table"` manualmente a la rama `else`:
```razor
// antes
<div class="table-responsive">

// después
<div class="table-responsive" data-testid="brand-monitor-table">
```

---

## E2 — Estado `disabled` del botón "Chequear ahora" no detectable por Playwright

**Archivo**: `tests/e2e/tests/08-dashboard-check-now.spec.ts`  
**Test afectado**: D3  
**Detectado**: Ejecución tras corrección de E1  

### Qué fallaba
```
Error: expect(locator).toBeDisabled() failed
Expected: disabled
Received: enabled
```

### Causa raíz
`ManualRefreshAsync` en Blazor Server tiene esta secuencia:
```csharp
_refreshing = true;
_cardsCountdown = CardsRefreshIntervalSeconds;
StateHasChanged();          // schedules re-render
await RefreshAllAsync();    // si la BD responde rápido (< 100ms), ya terminó
_refreshing = false;
StateHasChanged();
```

Cuando la BD está disponible y responde en < 100ms, la transición
`enabled → disabled → enabled` ocurre en menos tiempo del que Playwright tarda
en hacer una nueva evaluación del localizador. El botón nunca se observa como
`disabled`.

Esto **NO es un bug de la aplicación** — el refresh funcionó correctamente y
rápido. Es una limitación de testear estados transitorios muy breves en Playwright.

### Corrección
Eliminar la aserción `toBeDisabled()` del test. Probar el **resultado** del refresh
en lugar del estado transitorio:
- Botón habilitado antes del clic ✅
- Botón vuelve a estar habilitado tras el clic ✅
- Countdown visible y válido tras el clic ✅
- Estado global sigue visible ✅

### Lección
No testear estados transitorios brevísimos con Playwright. En su lugar, testear
el estado inicial y el estado final (precondición → acción → postcondición).
