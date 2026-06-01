# IT6 — UI Navigation & Modo NOC · Functional Design

**Fecha:** 2026-05-30  
**Iteración:** IT6 — Navegación y vista NOC para TV  
**Estado:** ✅ Completado

---

## 1. Navegación — Enlace a Modo NOC en MainLayout

### Antes

`MainLayout.razor` no tenía enlace a `/noc`. La página solo era accesible escribiendo la URL directamente o desde `NavMenu.razor` (sidebar del template, no usado como layout activo).

### Después

Nuevo enlace "Modo NOC" en la barra de navegación horizontal, visible para todos los roles autenticados, ubicado después de "Resumen Semanal".

```html
<a href="/noc" class="nav-link px-2 border-start ms-1 ps-3">Modo NOC</a>
```

Ligeramente separado del resto (`border-start`) para distinguirlo como un modo de visualización diferente.

---

## 2. NocLayout — Vista limpia para TV

### Objetivo

Pantalla de monitoreo para TV: sin distracciones, fondo oscuro para mejor contraste en sala, tipografía grande, auto-refresco.

### Layout

```
┌──────────────────────────────────────────────────┐
│ 🖥 Modo NOC — MonitorPedidos   [Salir de NOC]   │  ← header compacto
├──────────────────────────────────────────────────┤
│                                                  │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐│
│  │ BD Ped. │ │ BD Salud│ │ Jobs    │ │ APIs    ││
│  │   ✅ OK │ │   ✅ OK │ │  ✅ OK  │ │  ✅ OK  ││
│  └─────────┘ └─────────┘ └─────────┘ └─────────┘│
│                                                  │
│  Incidentes activos (tabla) o "Sin incidentes"   │
│                                                  │
└──────────────────────────────────────────────────┘
```

### Estilos

| Elemento | Estilo |
|----------|--------|
| Fondo general | `bg-dark text-light` |
| Header | Fondo semi-transparente, texto blanco |
| Cards | Fondo `#1a1a2e` con borde izquierdo de color según estado |
| Badge severity | Mismos colores que el dashboard (`danger`, `warning`, `success`) |
| Tipografía | `font-size: 1.1rem` base, cards con `1.5rem` para estado |
| Enlace salida | `btn-outline-light` |

---

## 3. NocPage — Refresco periódico de respaldo

### Antes

La página se refrescaba solo cuando llegaba un evento SignalR (`Broadcaster.OnAlert`). Si no había actividad de alertas, las cards se quedaban con datos estáticos.

### Después

Se agrega un timer de respaldo de 30 segundos que refresca los datos periódicamente, independientemente de eventos SignalR.

```csharp
private Timer? _refreshTimer;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (!firstRender) return;
    _refreshTimer = new Timer(async _ => await InvokeAsync(RefreshAsync), null,
                             TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
}

public async ValueTask DisposeAsync()
{
    await (_refreshTimer?.DisposeAsync() ?? ValueTask.CompletedTask);
    // ... cleanup de event handlers
}
```

El primer refresh ocurre a los 5 segundos (tiempo para que la página cargue completamente), luego cada 30 segundos.

---

## 4. Archivos modificados

| Archivo | Cambio |
|---------|--------|
| `MainLayout.razor` | Enlace "Modo NOC" en navbar |
| `NocLayout.razor` | Layout oscuro, tipografía TV |
| `NocPage.razor` | Timer periódico de respaldo 30s |
