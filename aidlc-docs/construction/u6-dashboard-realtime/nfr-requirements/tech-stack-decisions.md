# Tech Stack Decisions — U6 Dashboard & Real-Time

**Unidad:** U6 — Dashboard & Real-Time
**Stage:** Construction → NFR Requirements
**Fecha:** 2026-05-24
**Versión:** 1.0

---

## §1 Paquetes NuGet — producción

**Decisión P4=A: Sin paquetes nuevos en producción.**

| Tecnología | Fuente | Versión | Razón |
|-----------|--------|---------|-------|
| `Microsoft.AspNetCore.SignalR` | Incluido en ASP.NET Core 8 | 8.x (framework) | `AlertsHub : Hub` e `IHubContext<T>` — sin NuGet separado |
| `IJSRuntime` | Incluido en `Microsoft.AspNetCore.Components` | 8.x (framework) | Download blob + interop con notifications.js |

**Sin cambios en `MonitorPedidos.Web.csproj` para producción.**

---

## §2 Paquetes NuGet — tests

**Sin paquetes nuevos en MonitorPedidos.UnitTests.**

Los paquetes existentes cubren todos los tests de U6:

| Paquete | Ya instalado desde | Uso en U6 |
|---------|-------------------|-----------|
| `xunit` | U2/U3 | Framework de tests |
| `Moq` | U3 | Mockear `ISalesforceClient`, `IHubContext<AlertsHub>`, `IBrandSnapshotRepository`, `IRuleRepository` |
| `FluentAssertions` | U3 | Aserciones legibles |
| `coverlet.collector` | U3 | Cobertura de código |
| `RichardSzalay.MockHttp` | U4 | Ya disponible — no usado en U6 (no hay nuevos HttpClients) |
| `Microsoft.EntityFrameworkCore.Sqlite` | U5 | Ya disponible — no usado en U6 (no hay integration tests con DB) |

**Sin cambios en `MonitorPedidos.UnitTests.csproj`.**

---

## §3 Recurso JavaScript externo (CDN)

| Recurso | Fuente | Versión |
|---------|--------|---------|
| `signalr.min.js` | cdnjs.cloudflare.com | `@microsoft/signalr` 8.0.0 |

**Referencia en `_Host.cshtml`:**

```html
<!-- Antes del cierre de </body> -->
<script src="https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js"
        crossorigin="anonymous" referrerpolicy="no-referrer"></script>
<script src="~/js/notifications.js"></script>
```

**Alternativa local (sin internet):** Si el entorno demo no tiene acceso a CDN, copiar `signalr.min.js` a `wwwroot/js/` y cambiar la referencia a `~/js/signalr.min.js`. El sistema funciona igual — sin cambios en el código de la aplicación.

---

## §4 Archivo JS local nuevo

| Archivo | Ruta | Responsabilidad |
|---------|------|----------------|
| `notifications.js` | `wwwroot/js/notifications.js` | Conexión al hub, Notification API, fallback (sonido + parpadeo), download blob |
| `alert.mp3` | `wwwroot/sounds/alert.mp3` | Sonido de alerta (fallback cuando Notification API bloqueada) |

`alert.mp3` es un recurso estático — cualquier archivo de audio corto (< 5s) es válido para el MVP.

---

## §5 Resumen de cambios por capa

| Capa | Cambio |
|------|--------|
| Producción NuGet | **Ninguno** — todo incluido en el framework |
| Tests NuGet | **Ninguno** — paquetes existentes cubren U6 |
| JavaScript | `notifications.js` nuevo en `wwwroot/js/` + referencia CDN `signalr.min.js` |
| Estático | `alert.mp3` en `wwwroot/sounds/` |
| Configuración | `Logging:MaxExportLines: 500` en `appsettings.json` |
