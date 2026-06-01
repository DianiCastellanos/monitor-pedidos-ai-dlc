# Business Rules — U6 Dashboard & Real-Time

**Unidad:** U6 — Dashboard & Real-Time
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-24
**Versión:** 1.3 (2026-05-31 — BR-BRAND-11/12/13 añadidos: reglas críticas explícitas — no-fallback-automático, timestamp-checker, M3-peor-estado, refresh-no-destructivo, modo-degradado-banners)

---

## BR-DASH — Reglas del Dashboard en tiempo real

| ID | Regla | RF | Story |
|----|-------|----|----|
| BR-DASH-01 | `Dashboard.razor` muestra el estado actual de los 5 módulos: M2 (DbOrders), M4 (DbHealth), M11 (Jobs), M3 (ApiIntegrations), Brand Monitor. El estado se deriva de `LastCheckStore` en ciclo de timer. | RF-22 | US-01 |
| BR-DASH-02 | El Dashboard usa **dos timers independientes**: `_domainTimer` (30s) para cards M2/M3/M4/M11, y `_brandTimer` (60s) para Brand Monitor (IT4). | RF-22 | US-01 |
| BR-DASH-03 | Si la BD de incidentes no responde, el Dashboard construye las cards desde `LastCheckStore` (IT9) y muestra banner "BD no disponible". La página nunca muestra "Error al cargar". | RF-22 | US-01 |
| BR-DASH-04 | El estado "secondary" (gris) en M3 se muestra **solo durante los primeros ~30s de arranque** — cuando ningún checker de API ha corrido aún. Una vez con datos, nunca regresa a secondary (escalate-only, IT11). | RF-22 | US-01 |
| BR-DASH-05 | M3 muestra detalle individual por integración (Salesforce / Multivende) con el estado de cada una. El detalle técnico de error solo aparece en estado Critical (IT11). | RF-22 | US-01 |
| BR-DASH-06 | `HistoricPage` muestra incidentes históricos con filtros por fecha, dominio y estado. Reutiliza `IIncidentService` de U2. Paginación de 20 por página. | RF-23 | US-01 |
| BR-DASH-07 | `WeeklySummaryPage` muestra resumen de la semana: incidentes por dominio, MTTR. Fuente: `IIncidentService` de U2. | RF-24 | US-01 |

---

## BR-NOC — Reglas del Modo NOC

| ID | Regla | RF | Story |
|----|-------|----|----|
| BR-NOC-01 | `NocLayout` oculta completamente el `NavMenu`. El área de estado ocupa el 100% del ancho visible. | RF-22 | US-02 |
| BR-NOC-02 | El modo NOC es activable desde la barra de navegación principal. La URL es `/noc`. | RF-22 | US-02 |
| BR-NOC-03 | `NocPage.razor` es una página dedicada para la pantalla de TV, con timer de refresco automático cada **15 segundos** (IT8). Fondo oscuro, tipografía grande. | RF-22 | US-02 |
| BR-NOC-04 | `NocPage` carga datos desde la BD (incidentes + snapshots). Si la BD no responde, carga desde `LastCheckStore` (IT9). La pantalla nunca queda en blanco. | RF-22 | US-02 |
| BR-NOC-05 | `NocPage.RefreshAsync` llama `StateHasChanged()` al final (IT11 fix). Sin esto, las actualizaciones del timer no se renderizaban. | RF-22 | US-02 |

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

## BR-BRAND — Reglas del Brand Monitor (actualizado IT3/IT7/IT8/IT9/IT10/IT11)

| ID | Regla | RF | Story |
|----|-------|----|----|
| BR-BRAND-01 | `BrandMonitorChecker` consulta `ISalesforceClient.SearchPendingOrdersAsync` para obtener pedidos pendientes de descarga (IT7: datos reales de Salesforce OCAPI). Se ejecuta en Timer3 dinámico (cada 3 min prod / 1 min dev). | RF-22 ext. | ext. U6 |
| BR-BRAND-02 | La tabla `brand_snapshots` es **append-only** — `InsertAsync` siempre. El checker decide si guardar según `ShouldPersistSnapshot` (si cambió el conteo o pasó el intervalo mínimo). El historial crece con el tiempo (IT3). | RF-22 ext. | ext. U6 |
| BR-BRAND-03 | El semáforo se determina comparando el conteo actual con el snapshot de hace `ComparisonWindowSeconds` (configurable, default 600s): bajó → Green, igual → Yellow, subió → Red. Si no hay snapshot anterior → NoData (IT3). | RF-22 ext. | ext. U6 |
| BR-BRAND-04 | Los sites son: `["PatPrimo", "SevenSeven", "Ostu", "Atmos"]` con casing exacto (IT7: corregido de "Patprimo"). La comparación con `SiteId` de Salesforce es `OrdinalIgnoreCase`. | RF-22 ext. | ext. U6 |
| BR-BRAND-05 | Si `ISalesforceClient` retorna error, `BrandMonitorChecker` retorna `CheckResult.Warn` sin persistir snapshots. El `LastCheckStore` refleja el Warn. Los snapshots previos en BD permanecen. | RF-22 ext. | ext. U6 |
| BR-BRAND-06 | El checker guarda el resultado en `CheckResult.Details` en formato pipe-separado: `"PatPrimo:14\|SevenSeven:3\|Ostu:6\|Atmos:1"`. Este formato permite a la UI leer conteos sin llamar Salesforce (IT11). | RF-22 ext. | ext. U6 |
| BR-BRAND-07 | **[REGLA PROHIBITIVA]** La UI **NUNCA** llama Salesforce directamente en ciclos automáticos. El único camino automático es: `BrandMonitorChecker (background) → LastCheckStore → UI`. Solo el botón manual "↻ Actualizar" es la excepción permitida — puede invocar `GetLiveCountsAsync` únicamente cuando la BD no está disponible y el usuario lo solicita explícitamente. Cualquier implementación que llame Salesforce desde un timer o ciclo automático de UI viola esta regla (IT9/IT10). | RF-22 ext. | ext. U6 |
| BR-BRAND-08 | El refresh de Brand Monitor en Dashboard es **no-destructivo**: la tabla permanece visible durante todo el ciclo de actualización con spinner inline "Actualizando...". El reemplazo de datos es atómico: solo ocurre en el bloque `finally` cuando llegan datos confirmados. Si el refresh falla, los datos anteriores permanecen intactos en pantalla. `_brandStale` solo se activa si había datos previos y la BD falla (IT11). | RF-22 ext. | ext. U6 |
| BR-BRAND-09 | La fecha de corte para pedidos Salesforce es el inicio del año en curso (1 enero 00:00:00 UTC), no las últimas 24h. Esto alinea con la query de negocio usada en Postman por el equipo de integración. | RF-22 ext. | ext. U6 |
| BR-BRAND-10 | `BrandMonitorTable` siempre muestra el skeleton con las 4 marcas (PatPrimo / SevenSeven / Ostu / Atmos). Si no hay datos, conteos = "—". Nunca "Esperando datos del scheduler..." (IT11). | RF-22 ext. | ext. U6 |
| BR-BRAND-11 | El campo "Actualizado" en el header del Brand Monitor refleja el momento en que el **checker background** consultó Salesforce por última vez — tomado del `CheckedAt` de los snapshots o del momento en que `_brandLastUpdate` fue asignado por el checker. **NO refleja** cuándo la UI hizo refresh ni cuándo el usuario pulsó el botón manual. Es el timestamp del checker, no de la pantalla. | RF-22 ext. | ext. U6 |
| BR-BRAND-12 | El estado global del card M3 (APIs Externas) se deriva del **peor estado** entre Salesforce y Multivende. Si Salesforce = Ok pero Multivende = Critical → M3 = Critical. El card M3 **nunca puede mostrar verde** si alguna de sus integraciones está en rojo. Esta regla aplica en Dashboard y en NOC. Implementada en `ApplyApisWorstStatus` (IT11). | RF-22 ext. | ext. U6 |
| BR-BRAND-13 | Cuando la BD no está disponible, el sistema muestra el último dato conocido con un banner de advertencia. Los mensajes exactos según la fuente del dato son: `"⚠ Datos desactualizados"` (BD caída, hay snapshots anteriores), `"⚠ Datos en tiempo real · BD no disponible"` (dato viene de LastCheckStore), `"⚠ Tiempo real · BD no disponible — datos consultados ahora en Salesforce"` (fue refresh manual con botón), `"Sin datos aún"` (primer arranque, checker no ha corrido). El usuario siempre sabe qué tipo de dato está viendo (IT9/IT10). | RF-22 ext. | ext. U6 |

---

## BR-CONC — Reglas de concurrencia

| ID | Regla | RF | Story |
|----|-------|----|----|
| BR-CONC-01 | `AlertBroadcaster` es singleton. El event multicast de C# es thread-safe para suscripción y desuscripción (`+=` / `-=` son operaciones atómicas en .NET). | RNF-15 | US-29 |
| BR-CONC-02 | Cada circuito Blazor Server tiene su propio scope de DI. Los servicios Scoped (IIncidentService, IBrandMonitorService, IRuleManagementService) no se comparten entre usuarios. | RNF-15 | US-29 |
| BR-CONC-03 | `RealtimePage` desuscribe `AlertBroadcaster` en `Dispose()` para evitar memory leaks al cerrar el circuito. | RNF-15 | US-29 |

---

## Resumen de reglas por categoría

| Categoría | Cantidad | Cambios post-MVP |
|-----------|---------|-----------------|
| BR-DASH   | 7       | IT4/IT9/IT11 — timers, fallback, M3 detalle |
| BR-NOC    | 5       | IT8/IT9/IT11 — timer 15s, fallback, StateHasChanged fix |
| BR-NOTIF  | 4       | Sin cambios |
| BR-DISC   | 2       | Sin cambios |
| BR-LOG    | 4       | Sin cambios |
| BR-BRAND  | 13      | IT3/IT7/IT8/IT9/IT10/IT11 — Salesforce real, append-only, graceful degradation, reglas críticas explícitas |
| BR-CONC   | 3       | Sin cambios |
| **Total** | **38**  | +11 reglas vs MVP original |
