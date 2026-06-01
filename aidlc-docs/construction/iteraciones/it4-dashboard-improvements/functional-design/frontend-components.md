# IT4 — Dashboard Improvements · Functional Design

**Fecha:** 2026-05-30  
**Iteración:** IT4 — Mejoras de UI (timers, labels, rendermode)  
**Stories relacionadas:** US-01 (vista real-time), US-11 (histórico), US-12 (semanal)  
**Estado:** ✅ Completado

---

## 1. Timers separados — Dashboard principal

### Antes

Un solo timer compartido actualizaba todas las cards del dashboard simultáneamente.

### Después

Dos timers independientes con intervalos propios:

| Timer | Intervalo | Qué actualiza |
|-------|-----------|---------------|
| `_domainTimer` | 30 segundos | Cards de dominio (M2, M4, M11, M3) |
| `_brandTimer` | 30 segundos | Card de Brand Monitor |

**Por qué separados:** Brand Monitor tiene su propia lógica de refresco (`SimulateAndRefreshAsync` + checker) que es más costosa que una lectura de incidentes. Separarlos permite ajustar los intervalos de forma independiente sin afectar el resto del dashboard.

### Implementación

```csharp
// Dashboard.razor @code
private Timer? _domainTimer;
private Timer? _brandTimer;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (!firstRender) return;
    _domainTimer = new Timer(_ => InvokeAsync(RefreshDomainsAsync), null,
                             TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    _brandTimer  = new Timer(_ => InvokeAsync(RefreshBrandAsync),   null,
                             TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
}

public async ValueTask DisposeAsync()
{
    await (_domainTimer?.DisposeAsync() ?? ValueTask.CompletedTask);
    await (_brandTimer?.DisposeAsync()  ?? ValueTask.CompletedTask);
}
```

---

## 2. Labels humanas — Módulo y Causa

### Problema

Las páginas `HistoricPage`, `IncidentDetailPage` y `WeeklySummaryPage` mostraban los valores enum crudos de C# (e.g., `DbOrderChecker`, `NoDeterminada`) en lugar de texto legible en español.

### Solución

Métodos estáticos de mapeo en cada página:

```csharp
private static string ModuleLabel(string module) => module switch
{
    "DbOrderChecker"   => "BD Pedidos",
    "DbHealthChecker"  => "Health BD",
    "JobsChecker"      => "Jobs",
    "ApiChecker"       => "APIs Externas",
    "BrandMonitor"     => "Brand Monitor",
    _                  => module
};

private static string CauseLabel(string cause) => cause switch
{
    "Bd"            => "Base de datos",
    "Job"           => "Job de integración",
    "Api"           => "API externa",
    "Token"         => "Token expirado",
    "DataQuality"   => "Calidad de datos",
    "NoDeterminada" => "No determinada",
    _               => cause
};
```

### Páginas modificadas

| Página | Archivo | Cambio |
|--------|---------|--------|
| Histórico | `HistoricPage.razor` | `@ModuleLabel(inc.Module)`, `@CauseLabel(inc.Cause)` |
| Detalle incidente | `IncidentDetailPage.razor` | ídem |
| Resumen semanal | `WeeklySummaryPage.razor` | ídem en agrupaciones |

---

## 3. `@rendermode InteractiveServer` — páginas adicionales

### Problema

Algunas páginas Blazor no tenían `@rendermode InteractiveServer` y se servían en modo estático, por lo que los event handlers (`@onclick`, timers, `StateHasChanged`) no funcionaban.

### Páginas donde se agregó

| Página | Ruta |
|--------|------|
| `RulesPage.razor` | `/reglas` |
| `RuleEditPage.razor` | `/reglas/editar/{id}` |
| `RuleHistoryPage.razor` | `/reglas/{id}/historial` |
| `HistoricPage.razor` | `/historico` |
| `WeeklySummaryPage.razor` | `/semanal` |

### Directiva agregada

```razor
@rendermode InteractiveServer
```

**Nota:** `Dashboard.razor` y `BrandMonitorPage.razor` ya tenían esta directiva desde la construcción inicial.

---

## 4. Archivos modificados

| Archivo | Cambio |
|---------|--------|
| `src/MonitorPedidos.Web/Components/Pages/Dashboard.razor` | Timers separados `_domainTimer` + `_brandTimer`; `IAsyncDisposable` |
| `src/MonitorPedidos.Web/Components/Pages/Incidents/HistoricPage.razor` | Labels humanas + `@rendermode InteractiveServer` |
| `src/MonitorPedidos.Web/Components/Pages/Incidents/IncidentDetailPage.razor` | Labels humanas |
| `src/MonitorPedidos.Web/Components/Pages/Incidents/WeeklySummaryPage.razor` | Labels humanas + `@rendermode InteractiveServer` |
| `src/MonitorPedidos.Web/Components/Pages/Rules/RulesPage.razor` | `@rendermode InteractiveServer` |
| `src/MonitorPedidos.Web/Components/Pages/Rules/RuleEditPage.razor` | `@rendermode InteractiveServer` |
| `src/MonitorPedidos.Web/Components/Pages/Rules/RuleHistoryPage.razor` | `@rendermode InteractiveServer` |
