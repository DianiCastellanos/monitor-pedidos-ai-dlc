# MonitorPedidos — Guía de Setup y Ejecución

## Prerequisitos

| Herramienta | Versión mínima | Verificar |
|---|---|---|
| .NET SDK | 8.0 | `dotnet --version` |
| SQL Server | LocalDB / Express / Developer | `sqlcmd -?` |
| EF Core Tools | 8.0 | `dotnet ef --version` |

### Instalar EF Core Tools (si no está instalado)
```powershell
dotnet tool install --global dotnet-ef
```

---

## 1. Clonar el repositorio

```powershell
git clone https://github.com/DianiCastellanos/monitor-pedidos-ai.git
cd monitor-pedidos-ai
```

---

## 2. Configurar cadenas de conexión

Hay dos bases de datos:

| Conexión | Servidor | Base de Datos | Provider | Uso |
|---|---|---|---|---|
| `DefaultConnection` | 172.16.0.41 | MonitorPedidosDb | EF Core (SQL Server) | Incidents, Rules, Snapshots, Simulación |
| `ProductionDb` | 192.168.20.91 | vtainternet_qa | Dapper (solo SELECT) | `oc_encabezado` (M2) |

Las credenciales se cargan desde un archivo `.env` (gitignored) al arrancar la aplicación:

```env
ConnectionStrings__DefaultConnection=Server=172.16.0.41;Database=MonitorPedidosDb;User Id=Vtainternet;Password=Vta123;TrustServerCertificate=True;Encrypt=True
ConnectionStrings__ProductionDb=Server=192.168.20.91;Database=vtainternet_qa;User Id=salesviewer;Password=Ab321;TrustServerCertificate=True;Encrypt=True
```

> ⚠️ Nunca subir cadenas de conexión reales al repositorio. El archivo `.env` está en `.gitignore`. Los `appsettings.json` contienen placeholders vacíos.

---

## 3. Aplicar migraciones

```powershell
dotnet ef database update `
  --project src/MonitorPedidos.Infrastructure `
  --startup-project src/MonitorPedidos.Web
```

Esto aplica todas las migraciones pendientes y crea el esquema completo (tablas `incidents`, `rules`, `brand_snapshots`, `simulated_orders`, etc.).

---

## 4. Compilar la solución

```powershell
dotnet build MonitorPedidos.sln
```

---

## 5. Levantar la aplicación

```powershell
dotnet run --project src/MonitorPedidos.Web
```

La app queda disponible en: **http://localhost:5000/dashboard**

---

## Base de Conocimiento — Reglas y Restricciones Importantes

### Base de Datos

| Aspecto | Detalle |
|---------|---------|
| Motor | SQL Server (no PostgreSQL) |
| Tabla monitoreada | `oc_encabezado` (~239k filas) — tabla existente del ERP |
| TZ de `FechaGeneracion` | **Hora local Colombia (UTC-5)** — no está en UTC |
| Lectura | `WITH (NOLOCK)` — la tabla recibe escrituras concurrentes y el dashboard es solo lectura |
| Timeout de query | 30 segundos |

El checker M2 (`DbOrderChecker`) consulta `oc_encabezado` con esta lógica:
- Ventana de tiempo: `[now - N min, now]` en hora local Colombia
- Agrupa por `ChannelName` → espera `SALESFORCE` y `MULTIVENDE`
- Estado global: **OK** (ambos canales > 0), **WARN** (solo un canal > 0), **CRITICAL** (ambos en 0)
- Por cada canal genera: `CHANNEL:COUNT:OK|SIN_PEDIDOS`
- Severidad del incidente se actualiza en cada re‑chequeo via `UpdateAlert`

### Timezone — Crítico

`FechaGeneracion` está en **hora local Colombia (UTC-5)**. El query construye `DateTime` local (no UTC) para el WHERE. Si se usara `UtcDateTime`, el filtro no encontraría registros (off-by-5-hours).

### Incidentes — Una Alerta por Módulo

- Por módulo (M2, M3, M4, M11) solo puede haber **1 incidente abierto a la vez** (índice único filtrado `IX_incidents_Module_Open`)
- Cuando el checker detecta un problema y ya hay un incidente abierto, **actualiza el alert** (`Alert.QuePaso` + `Severity`) con los datos más recientes — no crea uno nuevo
- `OpenedAt` no cambia al actualizar — queda la fecha de creación original
- El cierre automático ocurre tras **2 resultados OK consecutivos** del mismo módulo

### Checker M2 — DbOrderChecker

1. Lee `oc_encabezado` entre `[now - window, now]`
2. `ChannelName` de la BD → `OrderSnapshot.OrderId`
3. Agrupa por canal → cuenta pedidos no cancelados
4. Estado global: OK (ambos > 0), WARN (uno > 0), CRITICAL (ambos 0)
5. Genera string: `SALESFORCE:18:OK|MULTIVENDE:0:SIN_PEDIDOS`
6. El string se almacena en `Alert.QuePaso` del incidente
7. El dashboard parsea ese string para renderizar la card M2
8. `LastCheckStore` guarda el último `CheckResult` por módulo — el dashboard lo lee para mostrar canales aunque no haya incidente activo

### Etiquetas de Módulo y Causa

Las columnas "Módulo" y "Causa" en todas las páginas (histórico, detalle, resumen semanal) muestran etiquetas legibles en lugar de nombres de enum:

| Enum | Etiqueta |
|------|----------|
| `DbOrderChecker` | M2 — BD Pedidos |
| `ApiChecker` | M3 — APIs Externas |
| `DbHealthChecker` | M4 — BD Salud |
| `JobsMonitor` | M11 — Jobs Sync |
| `SalesforceApi` | M13 — Salesforce API |
| `MultivendeApi` | M14 — Multivende API |
| `Bd` | Base de Datos |
| `Api` | API |
| `Token` | Token |
| `NoDeterminada` | No Determinada |

### Render Mode Interactivo

Todas las páginas con botones y formularios (`@onclick`, `@bind`) deben tener `@rendermode InteractiveServer`. Sin esto, los eventos no funcionan (página estática). Páginas afectadas: HistoricPage, IncidentDetailPage, WeeklySummaryPage, RulesPage, RuleEditPage, RuleHistoryPage.

### Dashboard — Timers Independientes

El dashboard tiene dos temporizadores independientes con sus propios contadores y marcas de tiempo:

| Timer | Refresca | Cadencia (dev) | Ubicación en UI |
|-------|----------|----------------|-----------------|
| Tarjetas (M2, M3, M4, M11) | Incidentes + cards de estado | 30s | Header del dashboard |
| Brand Monitor | Tabla de marcas (BrandSnapshot) | 30s | Arriba del Brand Monitor |

Cada timer muestra: `Actualizado hace: Xs · Próxima actualización en: Ys`.

La cookie de autenticación es **session‑only** (`IsPersistent = false`). Al cerrar el navegador se pierde y el usuario debe volver a seleccionar su identidad (Técnico / Operador).

### APIs Externas — Sin Conexión en Desarrollo

`sandbox.salesforce.com` y `api.multivende.com` requieren conectividad VPN/internet. En un entorno sin DNS externo, los checkers M3 fallan con `SocketException 11004`. Esto es esperado — no afecta el funcionamiento del dashboard.

---

## Ejecutar los tests

### Unit tests
```powershell
dotnet test tests/MonitorPedidos.UnitTests
```

### Integration tests
```powershell
dotnet test tests/MonitorPedidos.IntegrationTests
```

### Todos los tests
```powershell
dotnet test MonitorPedidos.sln
```

---

## Configuración por entorno

### Development (`appsettings.Development.json`)

| Clave | Valor por defecto | Descripción |
|---|---|---|
| `Simulation:Enabled` | `true` | Genera pedidos simulados sin APIs reales |
| `Monitoring:CheckerIntervalMinutes` | `1` | Frecuencia del checker (cada 1 min) |
| `Monitoring:ApiCheckerIntervalMinutes` | `1` | Frecuencia del checker de APIs |
| `Monitoring:BrandMonitorWindowSeconds` | `15` | Ventana de Brand Monitor |
| `Incidents:RetentionDays` | `7` | Días de retención de incidentes |

### Production (`appsettings.json`)

| Clave | Valor por defecto | Descripción |
|---|---|---|
| `Simulation:Enabled` | `false` | Desactivado en producción |
| `Monitoring:CheckerIntervalMinutes` | `5` | Frecuencia del checker |
| `Monitoring:BrandMonitorWindowSeconds` | `600` | 10 minutos de ventana |
| `Incidents:RetentionDays` | `90` | 90 días de retención |
| `Salesforce:BaseUrl` | _(vacío)_ | URL base de Salesforce |
| `Multivende:BaseUrl` | _(vacío)_ | URL base de Multivende |

---

## Archivos Clave

| Archivo | Rol |
|---------|-----|
| `src/MonitorPedidos.Web/BackgroundServices/MonitoringSchedulerService.cs` | Scheduler que ejecuta checkers (M2, M3, M4, M11) |
| `src/MonitorPedidos.Web/Features/Monitoring/DbOrderChecker.cs` | Checker M2 — consulta `oc_encabezado` |
| `src/MonitorPedidos.Infrastructure/Persistence/ProductionOrderRepository.cs` | Repositorio que ejecuta el SELECT contra `oc_encabezado` |
| `src/MonitorPedidos.Domain/Incidents/Incident.cs` | Entidad Incidente con `UpdateAlert(AlertMessage, Severity)` |
| `src/MonitorPedidos.Infrastructure/Incidents/IncidentService.cs` | Servicio que abre/cierra incidentes |
| `src/MonitorPedidos.Web/Components/Pages/Dashboard.razor` | Página del dashboard — cards M2-M4-M11 + alertas |
| `src/MonitorPedidos.Web/Services/MonitoringService.cs` | Orquesta checker + creación de incidentes |
| `src/MonitorPedidos.Web/Services/LastCheckStore.cs` | Singleton que guarda el último resultado por módulo |
| `src/MonitorPedidos.Web/Features/Monitoring/AlertTemplateRenderer.cs` | Plantillas de alerta en español |
| `src/MonitorPedidos.Web/Areas/Identity/Pages/Select.cshtml.cs` | Login page — cookie session‑only (`IsPersistent = false`) |
| `src/MonitorPedidos.Web/Components/Pages/Incidents/HistoricPage.razor` | Histórico de incidentes con filtros por módulo/fecha/severidad |
| `src/MonitorPedidos.Web/Components/Pages/Incidents/WeeklySummaryPage.razor` | Resumen semanal agrupado por módulo, causa y severidad |
| `src/MonitorPedidos.Web/Components/Pages/Incidents/IncidentDetailPage.razor` | Detalle de incidente con cierre manual |

---

## Solución de problemas frecuentes

**Error de conexión a SQL Server**
Verificar que SQL Server esté corriendo y la cadena de conexión sea correcta.
```powershell
# Verificar servicio
Get-Service MSSQLSERVER
```

**`dotnet ef` no reconocido**
```powershell
dotnet tool install --global dotnet-ef
# Reiniciar la terminal
```

**Puerto 5000 ocupado**
Cambiar el puerto en `src/MonitorPedidos.Web/Properties/launchSettings.json` en el perfil `http`.

**La app se cae sola — ERR_CONNECTION_REFUSED**
- Posible DNS no disponible para `sandbox.salesforce.com` / `api.multivende.com` (los checkers M3 hacen retry y timeout)
- Los background services tienen manejo de excepciones, pero si el scheduler se cae, reiniciar con `dotnet run`

**El dashboard muestra datos de ayer en la tabla de alertas**
- El incidente se creó ayer y `OpenedAt` no cambia aunque se actualice el `Alert.QuePaso`
- Si hay múltiples incidentes abiertos para el mismo módulo (por ejecuciones previas sin `UpdateAlert`), la consulta puede retornar uno desactualizado
- Solución: cerrar incidentes duplicados manualmente desde el botón "Resolver" en el dashboard
