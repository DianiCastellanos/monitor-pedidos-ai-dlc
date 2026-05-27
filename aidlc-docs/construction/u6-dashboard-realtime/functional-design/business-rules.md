# Business Rules — U6 Dashboard & Real-Time

**Unidad:** U6 — Dashboard & Real-Time
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-24
**Versión:** 1.0

---

## BR-DASH — Reglas del Dashboard en tiempo real

| ID | Regla | RF | Story |
|----|-------|----|----|
| BR-DASH-01 | `RealtimePage` muestra el estado actual (OK / WARN / CRITICAL) de los 4 dominios: DbOrders, DbHealth, Jobs, ApiIntegrations. El estado se deriva del último `CheckResult` recibido vía `AlertBroadcaster`. | RF-22 | US-01 |
| BR-DASH-02 | `RealtimePage` muestra el timestamp de la última verificación por dominio. Si no hay datos aún (sistema recién iniciado), muestra "Sin datos". | RF-22 | US-01 |
| BR-DASH-03 | `RealtimePage` se actualiza automáticamente cuando `AlertBroadcaster` dispara un evento — sin polling ni recarga manual del usuario. | RF-22, RF-25 | US-01, US-03 |
| BR-DASH-04 | `HistoricPage` muestra incidentes históricos con filtros por fecha, dominio y estado. Reutiliza `IIncidentService` de U2. Paginación de 20 por página. | RF-23 | US-01 |
| BR-DASH-05 | `WeeklySummaryPage` muestra resumen de la semana: incidentes por dominio, MTTR (tiempo medio de resolución). Fuente: `IIncidentService` de U2. | RF-24 | US-01 |

---

## BR-NOC — Reglas del Modo NOC

| ID | Regla | RF | Story |
|----|-------|----|----|
| BR-NOC-01 | `NocLayout` oculta completamente el `NavMenu`. El área de estado ocupa el 100% del ancho visible. | RF-22 | US-02 |
| BR-NOC-02 | El modo NOC es activable desde el `NavMenu` por cualquier usuario autenticado. La URL es `/noc`. | RF-22 | US-02 |
| BR-NOC-03 | El contenido mostrado en modo NOC es el mismo `RealtimePage`. No hay una página separada. `NocLayout` solo cambia el layout. | RF-22 | US-02 |

---

## BR-NOTIF — Reglas de notificación

| ID | Regla | RF | Story |
|----|-------|----|----|
| BR-NOTIF-01 | `NotificationService.BroadcastAlertAsync` invoca SIEMPRE los dos canales: `AlertBroadcaster` (in-process) e `IHubContext<AlertsHub>` (JS). Los errores en uno no deben bloquear el otro. | RF-25 | US-03 |
| BR-NOTIF-02 | Si la Notification API del navegador está bloqueada (`denied`), el sistema degrada silenciosamente: reproduce sonido + parpadea el título del tab. No muestra error al usuario. | RF-25 | US-03 |
| BR-NOTIF-03 | `AlertsHub` emite tres eventos: `AlertReceived(AlertMessage)`, `IncidentClosed(incidentId)`, `SystemStatusUpdated(StatusSummary)`. Los clientes JS suscritos al hub reciben todos los eventos. | RF-25 | US-03 |
| BR-NOTIF-04 | El parpadeo del título (`flashTitle`) se detiene automáticamente cuando el usuario hace foco en la pestaña del navegador. | RF-25 | US-03 |

---

## BR-DISC — Reglas de Discrepancias (UC6)

| ID | Regla | RF | Story |
|----|-------|----|----|
| BR-DISC-01 | `DiscrepanciesPage` lista incidentes cuya `Cause = Discrepancy`. El usuario puede ver el detalle pero no puede marcar como resuelto desde esta vista. | RF-24 | US-04 |
| BR-DISC-02 | La detección de discrepancias NO dispara `INotificationService`. La lista se actualiza solo al recargar la página. | RF-24 | US-04 |

---

## BR-LOG — Reglas de logs técnicos

| ID | Regla | RF | Story |
|----|-------|----|----|
| BR-LOG-01 | `LogsPage` es accesible exclusivamente al rol `Técnico`. Un `Operador` recibe redirect a `/access-denied` (server-side 403). | RF-30 | US-15, US-17 |
| BR-LOG-02 | `ITechnicalLogReader` lee las últimas 500 líneas del archivo Serilog activo (ruta configurable en appsettings). | RF-30 | US-17 |
| BR-LOG-03 | La exportación CSV incluye columnas: Timestamp, Level, Message, Exception. Sin librerías externas — `StringBuilder` manual. | RF-30 | US-17 |
| BR-LOG-04 | La exportación JSON serializa con `System.Text.Json` en formato array de objetos. La descarga se hace vía blob URL en el navegador (`IJSRuntime`). | RF-30 | US-17 |

---

## BR-BRAND — Reglas del Brand Monitor (extensión U6)

| ID | Regla | RF | Story |
|----|-------|----|----|
| BR-BRAND-01 | `BrandMonitorChecker` consulta `ISalesforceClient` para cada site (Patprimo, SevenSeven, Atmos, Ostu) y obtiene el conteo de pedidos pendientes. Se ejecuta en Timer2 de U4 (cada 10 minutos). | RF-22 ext. | ext. U6 |
| BR-BRAND-02 | La tabla `brand_snapshots` tiene exactamente 4 filas (una por site). Cada ejecución del checker SOBREESCRIBE la fila existente. No existe historial creciente. | RF-22 ext. | ext. U6 |
| BR-BRAND-03 | El semáforo se determina comparando `PendingCountCurrent` vs `PendingCountPrevious` contra el umbral configurado: (previous - current) >= threshold → 🟢 (bajó suficiente); (previous - current) > 0 AND < threshold → 🟡 (bajó poco); current >= previous → 🔴 (igual o subió). | RF-22 ext. | ext. U6 |
| BR-BRAND-04 | El umbral se obtiene de `IRuleRepository.GetActiveByModuleAsync(BrandMonitor)`. Si no hay regla activa, el threshold por defecto es 0 (cualquier bajada → 🟢). | RF-14 ext. | ext. U6 |
| BR-BRAND-05 | Si `ISalesforceClient` retorna error para un site, ese site muestra `Status = Red` con `PendingCountCurrent = -1` como indicador de "dato no disponible". No se interrumpe el chequeo de los otros sites. | RF-22 ext. | ext. U6 |
| BR-BRAND-06 | En el primer chequeo (no existe snapshot previo), `PendingCountPrevious` se inicializa igual a `PendingCountCurrent`. Estado inicial: 🟡. | RF-22 ext. | ext. U6 |

---

## BR-CONC — Reglas de concurrencia

| ID | Regla | RF | Story |
|----|-------|----|----|
| BR-CONC-01 | `AlertBroadcaster` es singleton. El event multicast de C# es thread-safe para suscripción y desuscripción (`+=` / `-=` son operaciones atómicas en .NET). | RNF-15 | US-29 |
| BR-CONC-02 | Cada circuito Blazor Server tiene su propio scope de DI. Los servicios Scoped (IIncidentService, IBrandMonitorService, IRuleManagementService) no se comparten entre usuarios. | RNF-15 | US-29 |
| BR-CONC-03 | `RealtimePage` desuscribe `AlertBroadcaster` en `Dispose()` para evitar memory leaks al cerrar el circuito. | RNF-15 | US-29 |

---

## Resumen de reglas por categoría

| Categoría | Cantidad |
|-----------|---------|
| BR-DASH   | 5       |
| BR-NOC    | 3       |
| BR-NOTIF  | 4       |
| BR-DISC   | 2       |
| BR-LOG    | 4       |
| BR-BRAND  | 6       |
| BR-CONC   | 3       |
| **Total** | **27**  |
