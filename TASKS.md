# MonitorPedidos AI — Tareas de Desarrollo

**Proyecto:** MonitorPedidos AI · Manufacturas Eliot  
**Owner:** Diana Castellanos  
**Total de tareas:** 30  
**Estado:** Pendientes de implementación (Activity 5 — Code Generation)

> Las tareas están organizadas por Unidad de Trabajo (U1–U7).  
> Deben implementarse **en orden**: cada unidad depende de la anterior.  
> Los artefactos de referencia están en `aidlc-docs/construction/`.

---

## Epic U1 — Estructura Base y Fundamentos
> Prerequisito de todo el proyecto. Sin esto, nada más funciona.

- [ ] **U1-01 — Crear la solución completa del proyecto**  
  Solución `.sln` con proyectos `Domain`, `Infrastructure`, `Web` (Blazor Server ASP.NET Core 8).  
  Instalar dependencias (EF Core 8, Serilog, Polly v8, SignalR). Configurar SQL Server LocalDB y User Secrets para credenciales.  
  _Referencia: `u1/.../tech-stack-decisions.md` + `u1/.../infrastructure-design.md`_

- [ ] **U1-02 — Implementar selección de identidad sin contraseña**  
  Pantalla inicial con 2 botones: *Analista Operativo* y *Responsable Técnico*.  
  Emitir cookie de sesión `HttpOnly + SameSite=Strict` con claims de rol. Sin ASP.NET Core Identity.  
  _Referencia: `u1/.../domain-entities.md §2`_

- [ ] **U1-03 — Implementar el modelo de dominio base**  
  Value object `AlertMessage` con los 6 campos de alerta en español (`QuePaso`, `Cuando`, `Donde`, `SeveridadTexto`, `CausaProbable`, `AccionSugerida`).  
  Enums compartidos: `Severity`, `CauseCategory`, `ModuleId`, `IncidentCloseType`, `CheckStatus`.  
  _Referencia: `u1/.../domain-entities.md §3 y §4`_

- [ ] **U1-04 — Definir los contratos de integración entre capas (interfaces)**  
  `ICheckExecutor` — contrato que implementan todos los checkers (M2, M3, M4, M11, BrandMonitor).  
  `INotificationService` — contrato que implementa SignalR en U6.  
  _Referencia: `u1/.../domain-entities.md §5`_

---

## Epic U2 — Persistencia e Incidentes
> Crea la base de datos y toda la lógica de ciclo de vida de un incidente. **Depende de U1.**

- [ ] **U2-01 — Implementar la entidad Incidente con sus reglas de negocio**  
  `Incident.Open()` — abre incidente nuevo (idempotente: si ya hay uno abierto para ese módulo, retorna el existente).  
  Marca automática `IsCandidatoReglaNueva = true` si la causa no pudo determinarse.  
  _Referencia: `u2/.../domain-entities.md §2`_

- [ ] **U2-02 — Crear la tabla de incidentes en base de datos (migración)**  
  Tabla `incidents` con todos los campos + columnas `Alert_*` para el value object AlertMessage.  
  Índices de rendimiento para búsquedas por módulo, fecha y estado.  
  _Referencia: `u2/.../infrastructure-design.md`_

- [ ] **U2-03 — Implementar el ciclo de vida completo de un incidente**  
  Cierre automático tras 2 resultados OK consecutivos del mismo módulo.  
  Cierre manual con comentario obligatorio (lanza error si el comentario está vacío). Registra quién cerró.  
  _Referencia: `u2/.../business-logic-model.md §Flujo 2 y §Flujo 3`_

- [ ] **U2-04 — Implementar consultas de historial y resumen semanal**  
  Búsqueda paginada filtrable por fecha, severidad y módulo (máx 100 resultados por página).  
  Resumen semanal agrupado por causa y severidad. Destaca candidatos a nueva regla.  
  _Referencia: `u2/.../business-logic-model.md §Flujo 4 y §Flujo 5`_

- [ ] **U2-05 — Implementar purga automática (90 días) y health check**  
  Elimina incidentes con más de 90 días, se ejecuta diariamente dentro del scheduler.  
  Endpoint `/health` que responde con estado de BD y checkers activos.  
  _Referencia: `u2/.../business-logic-model.md §Flujo 6` + `u2/.../nfr-requirements.md`_

---

## Epic U3 — Detección y Clasificación de Incidentes
> Implementa los checkers que observan el sistema y el clasificador de causa. **Depende de U1 y U2.**

- [ ] **U3-01 — Implementar checkers de base de datos (M2 y M4)**  
  M2 — `DbOrderChecker`: verifica que haya pedidos en la BD en la ventana de tiempo configurada (cadencia: 5 min).  
  M4 — `DbHealthChecker`: ejecuta `SELECT 1` y mide latencia. WARN si lenta, CRITICAL si timeout (cadencia: 5 min).  
  _Referencia: `u3/.../functional-design/`_

- [ ] **U3-02 — Implementar checkers de APIs externas y jobs (M3 y M11)**  
  M3 — `ApiChecker`: consulta Salesforce y Multivende. Reintenta en 5xx/timeout. Si 401 → causa `Token` + "Renovar según SOP-001". Sin renovación automática (cadencia: 10 min).  
  M11 — `JobsMonitor`: verifica que los jobs ejecutaron. Solo consulta, sin reintento (cadencia: 5 min).  
  _Referencia: `u3/.../functional-design/`_

- [ ] **U3-03 — Implementar el clasificador de causa raíz**  
  Cascada de 6 categorías: `bd → job → api → token → data_quality → no_determinada`.  
  Si no determina causa → `IsCandidatoReglaNueva = true` (aparece destacado en historial y resumen semanal).  
  _Referencia: `u3/.../business-rules.md`_

- [ ] **U3-04 — Implementar el servicio de monitoreo en segundo plano**  
  `BackgroundService` con `PeriodicTimer`. Ejecuta checkers en orden configurado.  
  Aislamiento: si un checker falla, los demás siguen ejecutándose. Sin incidentes duplicados por módulo.  
  _Referencia: `u3/.../nfr-requirements.md`_

---

## Epic U4 — Integraciones con APIs Externas
> Clientes HTTP para Salesforce y Multivende con resiliencia. **Depende de U1 y U3.**

- [ ] **U4-01 — Implementar clientes HTTP Salesforce y Multivende**  
  `ISalesforceClient.GetPendingOrdersByBrandAsync(site)` — pedidos pendientes por site.  
  `IMultivendeClient.GetJobStatusesAsync()` — estado de jobs de sincronización.  
  Tokens y URLs desde User Secrets. Sin credenciales en código.  
  _Referencia: `u4/.../domain-entities.md`_

- [ ] **U4-02 — Agregar resiliencia a las llamadas API (Polly)**  
  3 reintentos con espera exponencial (1s → 2s → 4s) para errores 5xx y timeout.  
  Timeout máximo de 10 segundos por llamada. Solo aplica a errores transitorios — nunca a 401.  
  _Referencia: `u4/.../nfr-requirements.md`_

- [ ] **U4-03 — Implementar logging seguro y mapping de respuestas**  
  `DelegatingHandler` que registra método, URL y código de respuesta sin exponer tokens ni payloads.  
  Transformar respuestas JSON de APIs externas a objetos internos del sistema (DTOs).  
  _Referencia: `u4/.../nfr-design-patterns.md`_

---

## Epic U5 — Gestión de Reglas de Negocio
> Permite al Técnico configurar cuándo se generan incidentes. **Depende de U1, U2 y U4.**

- [ ] **U5-01 — Implementar entidades de reglas + migración con datos iniciales**  
  `Rule` + `RuleCondition` (campos por módulo: ventana horaria, pedidos mínimos, latencia, umbral Brand Monitor).  
  Migración con seed de reglas por defecto para M2, M3, M4, M11 y BrandMonitor.  
  _Referencia: `u5/.../domain-entities.md` + `u5/.../infrastructure-design.md`_

- [ ] **U5-02 — Implementar activación/desactivación de reglas con trazabilidad**  
  Toggle activar/desactivar. Razón del cambio obligatoria — no se puede omitir.  
  Cada cambio genera un registro inmutable en `rule_history` con autor, timestamp y razón.  
  _Referencia: `u5/.../business-rules.md`_

- [ ] **U5-03 — Implementar página de lista y gestión de reglas (solo Técnico)**  
  Tabla con reglas activas e inactivas. Botones: Editar, Historial, Activar/Desactivar.  
  Operador es redirigido a `/access-denied` — restricción server-side, no solo CSS.  
  _Referencia: `u5/.../frontend-components.md §2`_

- [ ] **U5-04 — Implementar formulario de reglas e historial inmutable**  
  Formulario crear/editar con campos que cambian según el módulo seleccionado. Validación server-side.  
  Historial con diff antes/después de cada cambio. Sin botones de edición — solo lectura.  
  _Referencia: `u5/.../frontend-components.md §3 y §4`_

---

## Epic U6 — Dashboard en Tiempo Real
> La interfaz visual completa: dashboard NOC, alertas en vivo, Brand Monitor y notificaciones. **Depende de U1–U5.**

- [ ] **U6-01 — Implementar sistema de notificaciones en tiempo real**  
  `AlertsHub` (SignalR) — envía alertas a todos los clientes conectados. Reconexión automática.  
  `AlertBroadcaster` (Singleton) — actualiza páginas Blazor en memoria sin latencia de red.  
  Notificación browser → si se deniega: reproducir `alert.mp3` + parpadear título de pestaña ("🔴 ALERTA").  
  _Referencia: `u6/.../business-logic-model.md §Flujo 1 y §Flujo 8`_

- [ ] **U6-02 — Implementar dashboard principal NOC con tarjetas de estado**  
  4 tarjetas en tiempo real: M2, M3, M4, M11 — con estado Normal/Degradado/Error y timestamp.  
  Tarjeta CRITICAL: borde superior rojo + punto animado. Actualización sin recargar página.  
  _Referencia: `u6/.../frontend-components.md`_

- [ ] **U6-03 — Implementar tabla de alertas activas con cierre manual**  
  Columnas: Módulo, Severidad, Qué ocurrió, Detectado, Causa, Siguiente paso.  
  Estado vacío: "Sin alertas activas ✓". Botón de cierre con modal que pide comentario obligatorio.  
  _Referencia: `u6/.../frontend-components.md`_

- [ ] **U6-04 — Implementar Brand Monitor (checker + BD + vista semáforo)**  
  `BrandMonitorChecker`: consulta Salesforce por site (Patprimo, SevenSeven, Atmos, Ostu) cada 10 min.  
  Migración `brand_snapshots` (4 filas fijas con seed). Vista: tabla con semáforo 🟢 Normal / 🟡 Lento / 🔴 Riesgo.  
  _Referencia: `u6/.../business-logic-model.md §Flujo 3 y §Flujo 4`_

- [ ] **U6-05 — Implementar modo NOC, vistas secundarias y logs técnicos**  
  Modo NOC: pantalla completa sin sidebar, tipografía grande para lectura a distancia.  
  Vistas: historial paginado, resumen semanal, panel de discrepancias silencioso (UC6).  
  Logs técnicos (solo Técnico): últimas 500 líneas Serilog + exportar CSV/JSON.  
  _Referencia: `u6/.../business-logic-model.md §Flujo 5, 6 y 7`_

---

## Epic U7 — Simulación y Red-Teaming
> Prueba el sistema con fallos controlados. Solo visible en entorno Development. **Depende de U1–U6.**

- [ ] **U7-01 — Implementar servicio de simulación con guard de entorno + migración**  
  `BackgroundService` que solo ejecuta si `ASPNETCORE_ENVIRONMENT = Development`. Si se activa en prod → solo registra advertencia.  
  Migración `simulated_orders` + `simulated_job_statuses` (solo en desarrollo).  
  _Referencia: `u7/.../functional-design/` + `u7/.../infrastructure-design.md`_

- [ ] **U7-02 — Implementar escenarios de fallo + panel de simulación en UI**  
  6 escenarios activables: job detenido, token inválido, BD timeout, pedido mal estado, pedido cancelado, job Multivende detenido.  
  Panel en UI claramente marcado "ENTORNO DE DESARROLLO". Solo visible para Técnico. Botones Activar/Resetear.  
  _Referencia: `u7/.../functional-design/` + `u7/.../frontend-components.md`_

- [ ] **U7-03 — Configurar parámetros de simulación en appsettings.Development.json**  
  `SimulationOptions`: `Enabled`, probabilidad de fallo por escenario, intervalos de activación.  
  Sin credenciales — los tokens de API siguen en User Secrets.  
  _Referencia: `u7/.../infrastructure-design.md`_

- [ ] **U7-04 — Ejecutar y documentar los 6 escenarios de red-teaming**  
  Activar cada escenario y verificar que el sistema detecta el fallo en < 10 minutos.  
  Completar la plantilla `red-team-report-template.md` con resultados.  
  Criterio de cierre del proyecto: los 6 escenarios superados + métrica de reducción demostrable.  
  _Referencia: `u7/.../infrastructure-design/red-team-report-template.md`_

---

## Resumen

| Unidad | Épica | Tareas | Depende de |
|--------|-------|--------|-----------|
| U1 | Foundation & Cross-Cutting | 4 | — |
| U2 | Persistence & Incidents | 5 | U1 |
| U3 | Detection & Classification | 4 | U1, U2 |
| U4 | External Integrations | 3 | U1, U3 |
| U5 | Rules Management | 4 | U1, U2, U4 |
| U6 | Dashboard & Real-Time | 5 | U1–U5 |
| U7 | Simulation & Red-Teaming | 4 | U1–U6 |
| **Total** | | **30** | |

---

## Orden de implementación

```
U1 → U2 → U3 → U4 → U5 → U6 → U7
               ↑
         U4 y U5 pueden ir en paralelo después de U3
```

---

*MonitorPedidos AI — TASKS v2.0 (compacto) · Manufacturas Eliot · 2026-05-27*
