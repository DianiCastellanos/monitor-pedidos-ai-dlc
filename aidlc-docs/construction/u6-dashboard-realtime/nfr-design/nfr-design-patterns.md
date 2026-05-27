# NFR Design Patterns — U6 Dashboard & Real-Time

**Unidad:** U6 — Dashboard & Real-Time
**Stage:** Construction → NFR Design
**Fecha:** 2026-05-24
**Versión:** 1.0

---

## ADR-U6-01 — Mock triple de `IHubContext<AlertsHub>` en tests

**Decisión:** Triple mock encadenado con Moq para testear `NotificationService` sin infraestructura SignalR real.

**Problema:** `IHubContext<T>.Clients.All.SendAsync(...)` es una cadena de tres interfaces (`IHubContext<T>` → `IHubClients` → `IClientProxy`). Moq requiere configurar cada nivel.

**Solución:**

```csharp
// Patrón de setup en NotificationServiceTests
var mockProxy   = new Mock<IClientProxy>();
var mockClients = new Mock<IHubClients>();
var mockHub     = new Mock<IHubContext<AlertsHub>>();

mockClients.Setup(c => c.All).Returns(mockProxy.Object);
mockHub.Setup(h => h.Clients).Returns(mockClients.Object);

// Verificación
mockProxy.Verify(
    p => p.SendCoreAsync("AlertReceived", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
    Times.Once);
```

**Rationale:** Patrón estándar de ASP.NET Core para testing de hubs. Sin paquetes adicionales. `SendCoreAsync` es el método interno que `SendAsync` invoca — es el punto correcto de verificación con Moq.

**Componentes afectados:** `NotificationServiceTests`

---

## ADR-U6-02 — Thread safety de `AlertBroadcaster` por captura local de delegate

**Decisión:** Captura local del delegate antes de la invocación del evento multicast.

**Problema:** `AlertBroadcaster.OnAlert` es un `event Func<AlertMessage, Task>?`. Si una página Blazor se desuscribe (`-=`) en otro hilo mientras el scheduler invoca el evento, puede ocurrir una `NullReferenceException` o invocar un handler ya retirado.

**Solución:**

```csharp
public sealed class AlertBroadcaster
{
    public event Func<AlertMessage, Task>? OnAlert;
    public event Func<int, Task>?          OnIncidentClosed;

    public async Task BroadcastAsync(AlertMessage alert)
    {
        var handler = OnAlert;          // captura atómica — snapshot del delegate actual
        if (handler is not null)
            await handler.Invoke(alert);
    }

    public async Task BroadcastCloseAsync(int incidentId)
    {
        var handler = OnIncidentClosed;
        if (handler is not null)
            await handler.Invoke(incidentId);
    }
}
```

**Rationale:** La asignación de `event` en .NET es atómica (`Interlocked.CompareExchange` internamente). La captura local garantiza que la referencia no se anule entre la comprobación `is not null` y la invocación. Para 5 usuarios concurrentes en localhost, este patrón es suficiente sin locks adicionales (BR-CONC-01).

**Componentes afectados:** `AlertBroadcaster`, `RealtimePage` (suscriptor)

---

## ADR-U6-03 — `static readonly Regex` compilada en `TechnicalLogReader`

**Decisión:** Usar una expresión regular compilada como campo estático para parsear líneas Serilog.

**Problema:** Serilog en formato texto plano emite líneas como `[2026-05-24 10:00:00.123 +00:00 INF] Mensaje`. Las stack traces son multilinea y no siguen el patrón. Parsear 500 líneas en cada carga de `LogsPage` requiere eficiencia.

**Solución:**

```csharp
public sealed class TechnicalLogReader : ITechnicalLogReader
{
    // Compilada en el primer uso — sin overhead por llamada
    private static readonly Regex _pattern = new(
        @"^\[(\d{4}-\d{2}-\d{2} [\d:.]+ \+\d{2}:\d{2}) (INF|WRN|ERR|CRT|DBG)\] (.+)$",
        RegexOptions.Compiled);

    public async Task<IReadOnlyList<LogLine>> GetRecentAsync(int maxLines, CancellationToken ct)
    {
        var path  = _config["Logging:FilePath"] ?? "logs/log-.txt";
        var lines = File.ReadLines(path).TakeLast(maxLines);

        var result  = new List<LogLine>();
        LogLine? current = null;

        foreach (var raw in lines)
        {
            var m = _pattern.Match(raw);
            if (m.Success)
            {
                current = new LogLine(
                    DateTime.Parse(m.Groups[1].Value),
                    m.Groups[2].Value,
                    m.Groups[3].Value,
                    Exception: null);
                result.Add(current);
            }
            else if (current is not null)
            {
                // Línea de stack trace — se concatena al campo Exception de la línea anterior
                current = current with { Exception = (current.Exception ?? "") + "\n" + raw };
                result[^1] = current;
            }
        }
        return result;
    }
}
```

**Rationale:** `RegexOptions.Compiled` genera IL en el primer uso — amortizado en sesiones con múltiples cargas de `LogsPage`. El patrón con `record ... with { }` aprovecha la inmutabilidad de `LogLine` para actualizar `Exception` sin crear objetos intermedios innecesarios.

**Componentes afectados:** `TechnicalLogReader`, `LogsPage`

---

## ADR-U6-04 — `signalr.min.js` como copia local en `wwwroot/js/lib/`

**Decisión:** Distribuir `signalr.min.js` como archivo estático local — sin dependencia de CDN en tiempo de ejecución.

**Problema:** El entorno de demo es localhost/red interna. Una referencia a CDN externo falla si no hay internet en el momento de la demo. Además, el middleware CSP de U1 tiene `script-src 'self'` — agregar un dominio CDN requeriría modificarlo.

**Solución:**

```html
<!-- _Host.cshtml o App.razor — referencias locales -->
<script src="~/js/lib/signalr.min.js"></script>
<script src="~/js/notifications.js"></script>
```

**Proceso de setup (una sola vez por desarrollador):**

```powershell
# Descargar desde npm o CDN y copiar al proyecto
# Opción npm:
npx jspm install @microsoft/signalr@8.0.0
# Copiar: node_modules/@microsoft/signalr/dist/browser/signalr.min.js
#      → src/MonitorPedidos.Web/wwwroot/js/lib/signalr.min.js

# Opción manual: descargar directamente desde
# https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js
```

**Rationale:** Cero dependencias de internet en runtime. CSP `script-src 'self'` sin modificar (SECURITY-01 compliance). El archivo es estático y versionado en el repositorio — no hay degradación accidental por cambio de versión en CDN.

**Componentes afectados:** `wwwroot/js/lib/signalr.min.js` (nuevo archivo estático), `_Host.cshtml`

---

## §5 Resumen de ADRs U6

| ADR | Patrón | Componentes afectados |
|-----|--------|----------------------|
| ADR-U6-01 | Triple mock Moq para `IHubContext<T>` | `NotificationServiceTests` |
| ADR-U6-02 | Captura local de delegate en event multicast | `AlertBroadcaster`, `RealtimePage` |
| ADR-U6-03 | `static readonly Regex` compilada + stack trace concatenación | `TechnicalLogReader` |
| ADR-U6-04 | `signalr.min.js` local en `wwwroot/js/lib/` | `_Host.cshtml`, `wwwroot/js/lib/` |
