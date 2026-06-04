# Unit of Work — MonitorPedidos AI

**Fecha:** 2026-05-22
**Versión:** 1.0
**Stage:** Inception → Units Generation (Part 2 — Generation)
**Fuentes:** [`prd.md`](../../../prd.md) v2.3 §13, [`requirements.md`](../requirements/requirements.md) v1.1, [`stories.md`](../user-stories/stories.md), [`components.md`](./components.md), [`services.md`](./services.md), [`unit-of-work-plan.md`](../plans/unit-of-work-plan.md) (5 decisiones = A).

---

## Resumen

7 unidades de trabajo descompuestas por **capability funcional**, con mapeo sugerido a los 4 sprints del PRD §13:

| # | Unidad | Capability | Sprint sugerido |
|---|--------|------------|------------------|
| **U1** | Foundation & Cross-Cutting | Setup + auth + logging + cifrado + headers | Sprint 1 |
| **U2** | Persistence & Incidents | M9 + ciclo de vida de incidentes | Sprint 1 (base) + Sprint 3 (vistas) |
| **U3** | Detection & Classification | M1 + M2 + M4 + M11 + M7 + M10 + MonitoringService | Sprint 2 |
| **U4** | External Integrations | M3 (Salesforce + Multivende) | Sprint 2 |
| **U5** | Rules Management | M6 + historial + UI Técnico | Sprint 3 |
| **U6** | Dashboard & Real-Time | M8 + AlertsHub + NotificationService | Sprint 2–3 |
| **U7** | Simulation & Red-Teaming | OrdersSimulator + validación 6 escenarios | Sprint 1 (sim) + Sprint 4 (red-team) |

> **Decisión Q1 = A:** descomposición por capability. **Decisión Q3 = A:** mapeo a sprints como **sugerencia**, no compromiso — al activar Construction el equipo puede ajustar.

---

## Estrategia de organización del código (greenfield)

Single solution `MonitorPedidos.sln` con 3 proyectos (definidos en [`components.md`](./components.md) §"Estructura de la solución"):

- `src/MonitorPedidos.Domain/` — modelos compartidos y enums (sin dependencias salientes).
- `src/MonitorPedidos.Infrastructure/` — repositorios, DbContext, Serilog setup.
- `src/MonitorPedidos.Web/` — ASP.NET Core + Blazor Server + Identity + Features/ + Services/ + Simulation/.

Cada unidad de trabajo se materializa en un conjunto de **carpetas / archivos concretos** dentro de esa estructura (especificado en cada unidad abajo).

---

# U1 — Foundation & Cross-Cutting

## Propósito

Establecer la base del sistema: solución, dependencias, selección simple de identidad (sin contraseñas), persistencia base, logging estructurado, headers de seguridad y manejo global de excepciones. Es **prerequisito de todas las demás unidades**.

## Scope

| Aspecto | Contenido |
|---------|-----------|
| **Stories** | US-23 (selección de identidad + sesiones seguras), US-24 (separación de acceso por rol), US-25 (logging sin PII), US-26 (cifrado at-rest + transporte), US-27 (hardening de errores), US-28 (validación + headers HTTP) |
| **RFs cubiertos** | RF-26, RF-27, RF-28, RF-29, RNF-06, RNF-07, RNF-08, RNF-09, RNF-10, RNF-11, RNF-12, RNF-13, RNF-14, RNF-15 |
| **SECURITY rules** | SECURITY-01, SECURITY-03, SECURITY-04, SECURITY-05, SECURITY-08, SECURITY-09, SECURITY-10, SECURITY-11, SECURITY-12, SECURITY-13, SECURITY-15 |
| **Componentes** | `MonitorPedidos.Domain/` (POCOs base + enums `Severity`, `CauseCategory`, `ModuleId`, `IncidentCloseType`, `RuleChangeType` + value objects `AlertMessage`, `CheckResult` + `PredefinedIdentities`, `UserIdentity`, `ApplicationRole`); `MonitorPedidos.Infrastructure/` (DbContext base + Serilog config); `Areas/Identity/` (IdentitySelectionPage, LogoutEndpoint — sin scaffold de Identity completo); `Middleware/SecurityHeadersMiddleware`; `GlobalExceptionHandler` |
| **Service layer** | Cookie authentication (`AddAuthentication(CookieScheme)` + `SignInAsync` con claims); sin `AuthService` sobre Identity |
| **Carpetas tocadas** | `src/MonitorPedidos.Domain/`, `src/MonitorPedidos.Infrastructure/`, `src/MonitorPedidos.Web/Program.cs`, `src/MonitorPedidos.Web/Areas/Identity/Pages/`, `src/MonitorPedidos.Web/Middleware/` |

## Entry criteria

- ✅ Inception completa hasta Application Design aprobado.
- ✅ .NET 8 SDK + SQL Server MonitorPedidosDb (172.16.0.41) instalados.
- ✅ .NET 8 SDK instalado (no se requieren User Secrets para credenciales — identidad sin passwords).

## Exit criteria

- [ ] Solución `MonitorPedidos.sln` compila sin warnings (treat warnings as errors recomendado).
- [ ] EF Core migrations iniciales aplicadas (esquema base sin tablas de Identity — solo las tablas de negocio de U2+).
- [ ] Pantalla de selección de identidad funcional: 2 botones (*Analista Operativo* / *Responsable Técnico*) emiten cookie de sesión con claims correctos.
- [ ] Cookies de sesión emitidas con `HttpOnly`, `SameSite=Strict`, `Secure` (cuando HTTPS).
- [ ] Headers de seguridad presentes en respuestas HTML: `Content-Security-Policy: default-src 'self'`, `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`. `Strict-Transport-Security` solo si HTTPS interno.
- [ ] Logging estructurado emite a archivo rotado con `timestamp + request_id + log_level + mensaje`; **sin** tokens ni PII (filtros explícitos en Serilog — no existen contraseñas en este modelo).
- [ ] Errores no manejados retornan mensaje genérico (sin stack trace) en producción.
- [ ] Cifrado at-rest configurado en la BD (TDE o archivo cifrado del usuario, según opciones disponibles).

## Sprint sugerido

**Sprint 1 (semana 1).** El PRD §13 lo asigna a Sprint 1 — "Setup .NET 8 + SQL Server MonitorPedidosDb (172.16.0.41)".

## Notas técnicas para Construction

- La **decisión EF Core vs Dapper** queda diferida a NFR Requirements de Construction. U1 debe dejar la capa `Infrastructure` lista para una u otra implementación, con interfaces de repositorio ya definidas en `Domain` (siguiendo `component-methods.md`).
- La librería de logging probable es **Serilog** (decisión final en NFR Design). U1 abstrae el logger detrás de `ILogger<T>` para mantener la opción abierta.
- El `dotnet dev-certs https --trust` debe documentarse como parte de la onboarding del desarrollador (ver `requirements.md` Dep-03).

---

# U2 — Persistence & Incidents

## Propósito

Materializar M9 (repositorio de incidentes) y el ciclo de vida completo de un incidente: apertura, cierre automático (2 OK consecutivos), cierre manual con comentario obligatorio, retención 90 días.

## Scope

| Aspecto | Contenido |
|---------|-----------|
| **Stories** | US-05 (persistencia tras recargar), US-11 (histórico filtrable + paginado), US-12 (resumen semanal), US-13 (cierre manual + comentario obligatorio), US-14 (cierre automático tras 2 OK) |
| **RFs cubiertos** | RF-18, RF-19, RF-20, RF-21, RF-22 |
| **Componentes** | Entidad `Incident`, `IIncidentRepository` + `IncidentRepository`, `IIncidentService` + `IncidentService`, páginas Blazor `HistoricPage`, `WeeklySummaryPage`, `IncidentDetailPage` |
| **Service layer** | `IncidentService` (apertura, cierre auto/manual, búsqueda, resumen semanal, purga retención) |
| **Carpetas tocadas** | `src/MonitorPedidos.Domain/Incidents/`, `src/MonitorPedidos.Infrastructure/Persistence/IncidentRepository.cs`, `src/MonitorPedidos.Web/Features/Incidents/`, `src/MonitorPedidos.Web/Services/IncidentService.cs`, EF Core migration para tabla `incidents` |

## Entry criteria

- ✅ U1 completada (Domain + Infrastructure base + Identity).

## Exit criteria

- [ ] Tabla `incidents` migrada con columnas: `Id`, `Module`, `Cause`, `Severity`, campos de la `AlertMessage` (6 campos), `OpenedAt`, `ClosedAt?`, `CloseType?`, `ClosedByUserId?`, `ComentarioResolucion?`, `IsCandidatoReglaNueva`.
- [ ] `IncidentService.OpenIncidentAsync` persiste correctamente y retorna `Incident` con `Id` generado.
- [ ] `IncidentService.CloseAutomaticallyAsync` cierra el incidente con `CloseType=Automatic`, sin comentario.
- [ ] `IncidentService.CloseManuallyAsync` valida server-side que `comment` no está vacío (lanza `ValidationException` si vacío) y persiste con `CloseType=Manual`, `ClosedByUserId`.
- [ ] `IncidentService.SearchHistoryAsync` retorna resultados filtrados por `(From, To, Severity, Module)` con paginación `(Skip, Take)`.
- [ ] `IncidentService.GetWeeklySummaryAsync` agrupa por causa y severidad para la semana solicitada y cuenta candidatos a regla nueva.
- [ ] Job de purga elimina (o archiva) incidentes con `OpenedAt < UtcNow - 90 días`.
- [ ] Tras cerrar y reabrir el dashboard, los incidentes activos siguen visibles (RT-Persist verificado manualmente).

## Sprint sugerido

**Sprint 1 (base persistente)** + **Sprint 3 (vistas Histórico/Semanal completas)**. El PRD §13 asigna "Tabla `incidents` poblándose" a Sprint 1; las vistas completas se completan en Sprint 3.

## Notas técnicas para Construction

- El `WeeklySummary` puede materializarse vía LINQ (EF Core) o procedure SQL — decidir en Functional Design.
- La purga puede correr como job semanal en `MonitoringSchedulerService` o como SQL Agent job (post-MVP on-prem). En MVP se ejecuta en el scheduler nativo.
- La pestaña `IncidentDetailPage` es la base que U4 extenderá para mostrar el detalle de `auto_reintento`.

---

# U3 — Detection & Classification

## Propósito

Implementar el corazón del sistema: ciclo completo de detección automática Scheduler → Checkers → Classifier → Explainer → Incident → Notification. Incluye chequeo de BD interna, health-check BD, verificación de jobs, clasificación en cascada y rendering de alertas con los 6 campos.

## Scope

| Aspecto | Contenido |
|---------|-----------|
| **Stories** | US-06 (6 campos en español), US-07 (ausencia pedidos), US-08 (health BD), US-09 (jobs), US-18 (causa no determinada → candidato) |
| **RFs cubiertos** | RF-01, RF-02, RF-04, RF-05, RF-08, RF-09, RF-10, RF-11, RF-12, RF-13 |
| **Componentes** | M1 `MonitoringSchedulerService : BackgroundService`, M2 `DbOrderChecker`, M4 `DbHealthChecker`, M11 `JobsChecker`, M7 `CauseClassifier`, M10 `AlertTemplateRenderer`, interfaz `ICheckExecutor`, `MonitoringService`, **interfaz** `INotificationService` (la implementación concreta llega en U6) |
| **Service layer** | `MonitoringService` (orquestador principal), `INotificationService` (contrato; sin implementación aún) |
| **Carpetas tocadas** | `src/MonitorPedidos.Web/Features/Scheduler/`, `src/MonitorPedidos.Web/Features/Checks/DbOrderCheck/`, `Features/Checks/DbHealthCheck/`, `Features/Checks/JobsCheck/`, `Features/Classification/`, `Features/Alerts/`, `Services/MonitoringService.cs`, `Services/INotificationService.cs` |

## Entry criteria

- ✅ U1 completada (Foundation + DI).
- ✅ U2 completada (`IIncidentRepository` y `IIncidentService` disponibles).
- ⚠️ Para que M2 detecte algo en Sprint 2, la **tabla `simulated_orders` de U7** debe estar lista — coordinar con U7.

## Exit criteria

- [ ] `MonitoringSchedulerService` arranca al lanzar la app y se detiene limpiamente con `Ctrl+C` / graceful shutdown.
- [ ] Timers periódicos: M2 cada 5 min, M4 cada 5 min, M11 cada 5 min.
- [ ] M2 detecta ausencia de pedidos en ventana definida por reglas (en Sprint 2 se usan reglas seedeadas mínimas; el CRUD UI llega en U5).
- [ ] M4 ejecuta `SELECT 1` y emite WARN ante latencia alta, CRITICAL ante timeout/error.
- [ ] M11 consulta estado de jobs sin reintentarlos; emite CRITICAL si están detenidos.
- [ ] M7 clasifica en cascada `bd → job → api → token → data_quality → no_determinada`.
- [ ] M7 marca incidente como `IsCandidatoReglaNueva = true` cuando `Cause == NoDeterminada`.
- [ ] M10 renderiza `AlertMessage` con los 6 campos en español desde plantillas (no concatenación ad-hoc).
- [ ] `MonitoringService.RunCheckAsync` integra el flujo completo: ejecuta checker → si Warn/Critical → clasifica → renderiza → llama `IncidentService.OpenIncidentAsync` → llama `INotificationService.BroadcastAlertAsync`.
- [ ] `MonitoringService` detecta cierre automático: 2 OK consecutivos del mismo módulo → llama `IncidentService.CloseAutomaticallyAsync`.
- [ ] `INotificationService` definido como interfaz sin implementación funcional (stub que loggea — implementación real en U6).

## Sprint sugerido

**Sprint 2 (semana 2).** PRD §13 asigna "M2, M3 (parte de U4), M4, M11, M6 reglas, M7 clasificador" a Sprint 2.

## Notas técnicas para Construction

- Las **plantillas concretas** de `M10 AlertTemplateRenderer` (texto en español para cada combinación causa × módulo × severidad) se definen en Functional Design.
- La **estructura del `ConditionExpression` de las reglas** (string textual vs JSON) se decide en Functional Design — U3 trabaja con reglas seedeadas hardcoded para Sprint 2; el CRUD viene en U5.
- M2 puede usar inicialmente la tabla `simulated_orders` (U7) como fuente. Post-MVP se cambia a tabla real de pedidos.

---

# U4 — External Integrations

## Propósito

Integrar las APIs externas Salesforce y Multivende con política de reintentos limitados (máx 2, solo 5xx/timeout) y manejo correcto de 401 (sin renovación automática, sugerencia SOP-001).

## Scope

| Aspecto | Contenido |
|---------|-----------|
| **Stories** | US-10 (token 401 sin renovación), US-16 (detalle de auto_reintentos visible para Técnico) |
| **RFs cubiertos** | RF-03, RF-06, RF-07 |
| **Componentes** | M3 `ApiChecker` (puede ser una instancia por API o dos: `SalesforceApiChecker` + `MultivendeApiChecker`), clientes tipados `ISalesforceClient` + `IMultivendeClient`, política Polly de retry |
| **Service layer** | Se integra a `MonitoringService` (ya definido en U3) |
| **Carpetas tocadas** | `src/MonitorPedidos.Web/Features/Checks/ApiCheck/`, `Infrastructure/HttpClients/` |

## Entry criteria

- ✅ U1 completada (HttpClient factory + logging).
- ✅ U3 completada (`MonitoringService` + Classifier + Explainer disponibles).
- ✅ Credenciales sandbox de Salesforce y Multivende disponibles vía User Secrets / variables de entorno.

## Exit criteria

- [ ] `ISalesforceClient.PingOrdersAsync` y `IMultivendeClient.PingOrdersAsync` consumen las APIs en **solo lectura** (P2 — nunca POST/PUT/PATCH/DELETE).
- [ ] Política Polly aplica: **máximo 2 reintentos**, **solo para 5xx o timeout**, con backoff configurable. Ante **401, no reintenta**.
- [ ] Cada `auto_reintento` queda registrado: en log estructurado (con request_id) **y** como metadata del incidente generado.
- [ ] Ante 401, M7 clasifica `causa_probable = token` y M10 incluye la referencia literal a `SOP-001` en `acción_sugerida` (US-10 criterio 2).
- [ ] El `IncidentDetailPage` (de U2) muestra cronológicamente los `auto_reintento` para incidentes API, visible solo al rol Técnico (US-16 criterio 3).
- [ ] La cadencia de chequeo APIs es **10 minutos** (M3) — configurable.
- [ ] El sistema **nunca** intenta renovación automática de token (verificado por tests de integración / red-teaming RT2).

## Sprint sugerido

**Sprint 2 (semana 2).** PRD §13 asigna M3 a Sprint 2.

## Notas técnicas para Construction

- La librería de retry es Polly (estándar de facto en .NET).
- Los clientes tipados se registran con `IHttpClientFactory` para gestión correcta de sockets (`AddHttpClient<ISalesforceClient, SalesforceClient>()`).
- El **rate limiting** de las APIs externas (R6) se gestiona ajustando la cadencia de chequeo + backoff de Polly, no requiere cambios en U4 si los defaults son razonables.

---

# U5 — Rules Management

## Propósito

CRUD completo de reglas estáticas con historial inmutable, autor y razón obligatoria. Es el punto donde el responsable técnico convierte conocimiento tácito en reglas explícitas (P5).

## Scope

| Aspecto | Contenido |
|---------|-----------|
| **Stories** | US-19 (crear regla con autor + razón), US-20 (editar con diff), US-21 (activar/desactivar), US-22 (consultar historial inmutable) |
| **RFs cubiertos** | RF-14, RF-15, RF-16, RF-17 |
| **Componentes** | Entidades `Rule`, `RuleHistoryEntry`; `IRuleRepository` + `RuleRepository`; `IRuleHistoryRepository` + `RuleHistoryRepository`; `IRuleManagementService` + `RuleManagementService`; páginas Blazor `RulesPage`, `RuleEditPage`, `RuleHistoryPage` |
| **Service layer** | `RuleManagementService` |
| **Carpetas tocadas** | `src/MonitorPedidos.Domain/Rules/`, `src/MonitorPedidos.Infrastructure/Persistence/RuleRepository.cs`, `Persistence/RuleHistoryRepository.cs`, `src/MonitorPedidos.Web/Features/Rules/`, `src/MonitorPedidos.Web/Services/RuleManagementService.cs` |

## Entry criteria

- ✅ U1 completada (Identity + roles funcionando).
- ✅ U3 completada (M7 consume reglas activas — en Sprint 2 con reglas seedeadas; aquí las eleva a CRUD vivo).

## Exit criteria

- [ ] Tabla `rules` migrada con columnas: `Id`, `Name`, `Description`, `AppliesTo`, `ConditionExpression`, `Severity`, `IsActive`, `CreatedAt`, `UpdatedAt`.
- [ ] Tabla `rule_history` migrada con columnas: `Id`, `RuleId`, `ChangeType`, `AuthorUserId`, `Reason`, `DiffBefore`, `DiffAfter`, `Timestamp`.
- [ ] `RuleManagementService.CreateRuleAsync` valida razón obligatoria server-side; inserta `Rule` y `RuleHistoryEntry` (Creation) en transacción.
- [ ] `RuleManagementService.UpdateRuleAsync` calcula diff antes/después y crea `RuleHistoryEntry` (Edit).
- [ ] `RuleManagementService.SetActiveAsync` cambia `IsActive` y crea `RuleHistoryEntry` (Activation/Deactivation).
- [ ] Páginas `RulesPage` / `RuleEditPage` / `RuleHistoryPage` accesibles **solo al rol `Técnico`** (`[Authorize(Roles="Técnico")]`).
- [ ] Operador autenticado intenta acceder a `/reglas` → recibe **403 server-side** (no solo UI escondida).
- [ ] `RuleHistoryPage` muestra eventos en orden cronológico inverso con autor + razón + diff.
- [ ] Sin endpoint ni operación que permita Update/Delete sobre `RuleHistoryEntry` (verificado por code review + tests).
- [ ] El scheduler/`M7 CauseClassifier` consume las reglas activas vivas; la próxima evaluación post-edición usa la versión nueva (no caché stale).

## Sprint sugerido

**Sprint 3 (semana 3).** PRD §13 asigna "Tabla reglas editable" a Sprint 3.

## Notas técnicas para Construction

- La **estructura del `ConditionExpression`** (string textual evaluado por un parser propio vs JSON evaluado por un motor de reglas) es decisión de Functional Design.
- El diff antes/después puede serializarse como JSON del estado completo de la regla — simple y suficiente para auditoría.
- El seeder de reglas iniciales (a partir del Failbook 5–7 casos) corre como parte del bootstrap o como migration data.

---

# U6 — Dashboard & Real-Time

## Propósito

Implementar la capa de presentación completa: páginas Blazor para las 4 vistas del dashboard (real-time, histórico, semanal, discrepancias) + modo NOC + AlertsHub SignalR + NotificationService (implementación concreta) + fallback de Notification API.

## Scope

| Aspecto | Contenido |
|---------|-----------|
| **Stories** | US-01 (vista real-time), US-02 (modo NOC), US-03 (notificación con fallback), US-04 (panel discrepancias UC6), US-15 (acceso rol Técnico), US-17 (logs técnicos exportables), US-29 (concurrencia 5 usuarios) |
| **RFs cubiertos** | RF-22, RF-23, RF-24, RF-25, RF-27 (server-side authz), RF-30 |
| **Componentes** | Páginas Blazor `RealtimePage`, `DiscrepanciesPage`, `LogsPage`, `NocLayout`; `AlertsHub : Hub` (SignalR); `NotificationService` (implementación concreta de la interfaz definida en U3); cliente `AlertsHubClient` en Blazor; `ITechnicalLogReader` |
| **Service layer** | `NotificationService` |
| **Carpetas tocadas** | `src/MonitorPedidos.Web/Features/Dashboard/`, `src/MonitorPedidos.Web/Hubs/AlertsHub.cs`, `src/MonitorPedidos.Web/Services/NotificationService.cs`, `src/MonitorPedidos.Web/Pages/` (rutas Razor) |

## Entry criteria

- ✅ U1 completada (auth + roles).
- ✅ U2 completada (acceso a `IIncidentRepository` + `IIncidentService` para histórico y semanal).
- ✅ U3 completada (`MonitoringService` + interfaz `INotificationService` ya definida).
- ✅ U5 idealmente completada antes de finalizar U6 — las páginas de reglas viven aquí pero los servicios son de U5.

## Exit criteria

- [ ] `RealtimePage` muestra estado actual (OK/WARN/CRITICAL) de los 4 dominios con timestamp de última verificación, auto-actualizado vía SignalR.
- [ ] `HistoricPage` (extiende U2) y `WeeklySummaryPage` (extiende U2) renderizan filtros + paginación.
- [ ] `DiscrepanciesPage` lista pedidos con estado incorrecto **sin** disparar push notification (UC6 silencioso).
- [ ] `NocLayout` activable desde el menú; oculta navegación y maximiza área de estado.
- [ ] `AlertsHub` emite eventos `AlertReceived`, `IncidentClosed`, `SystemStatusUpdated` a todos los clientes conectados.
- [ ] `NotificationService` reemplaza al stub de U3 con la implementación SignalR real.
- [ ] Cliente Blazor recibe los eventos y dispara: notificación nativa (si Notification API permitida) + sonido + parpadeo del título.
- [ ] Fallback verificado: con Notification API bloqueada en el navegador, el dashboard sigue funcionando con sonido + parpadeo.
- [ ] `LogsPage` accesible solo al rol `Técnico` (403 server-side para Operador), con exportación CSV/JSON.
- [ ] Las páginas que escriben a `RuleManagementService` (en realidad implementadas en U5 pero alojadas en el árbol de `Features/Rules/`) están protegidas por `[Authorize(Roles="Técnico")]`.
- [ ] **5 usuarios concurrentes autenticados** navegan simultáneamente sin lag perceptible (test de carga manual o automatizado).

## Sprint sugerido

**Sprint 2 (mínimo) + Sprint 3 (completo).** PRD §13: "Dashboard mínimo + push browser" en Sprint 2; "Dashboard completo (histórico, semanal, UC6)" en Sprint 3.

## Notas técnicas para Construction

- Blazor Server **circuito SignalR** es la base del real-time — `AlertsHub` es un hub adicional específico para alertas (decisión Q5 = A).
- Para concurrencia 5 usuarios, Blazor Server requiere atención al **server-side state** (cada conexión consume RAM). 5 usuarios es trivial en localhost.
- El **fallback de Notification API** se implementa con `IJSRuntime` que detecta el estado de `Notification.permission` y degrada según corresponda.

---

# U7 — Simulation & Red-Teaming

## Propósito

Dos sub-objetivos:

1. **Simulator (Sprint 1):** generador automático de datos en `simulated_orders` con probabilidad configurable de fallo — alimenta a U2/U3 para que la detección funcione sin BD real.
2. **Red-teaming validation (Sprint 4):** ejecución y reporte de los 6 escenarios obligatorios del PRD §11.

## Scope

| Aspecto | Contenido |
|---------|-----------|
| **Stories** | Ninguna directa — esta unidad soporta a todas las demás validando los escenarios end-to-end |
| **Criterios de aceptación red-teaming** | RT1 (apagar job SF), RT2 (revocar token SF), RT3 (apagar SQL), RT5 (estado incorrecto), RT7 (cancelado ignorado), RT-Persist (cerrar/reabrir dashboard) |
| **RFs cubiertos indirectamente** | C-06 (restricción de datos simulados) |
| **Componentes** | `OrdersSimulatorService : BackgroundService`, tabla `simulated_orders`, configuración `SimulationOptions` (intervalos, probabilidad de fallo), scripts SQL de seed |
| **Service layer** | (interno al simulator) |
| **Carpetas tocadas** | `src/MonitorPedidos.Web/Simulation/`, EF Core migration para `simulated_orders`, scripts en `db/seed/` |

## Entry criteria

**Para simulator (Sprint 1):**

- ✅ U1 completada.
- ✅ U2 base completada (acceso a `DbContext` y migraciones).

**Para red-teaming (Sprint 4):**

- ✅ Todas las demás unidades (U1..U6) completadas y desplegadas en el entorno de demo.

## Exit criteria

**Sub-objetivo 1 — Simulator:**

- [ ] Tabla `simulated_orders` migrada con columnas suficientes para representar el dominio (id, source, status, created_at, etc.).
- [ ] Script SQL inicial puebla la tabla con un baseline.
- [ ] `OrdersSimulatorService` inserta nuevos pedidos cada **5–10 min** (configurable) con probabilidad configurable de fallo.
- [ ] La probabilidad de fallo es ajustable en runtime vía configuración (variable de entorno o appsettings) — sin recompilar.

**Sub-objetivo 2 — Red-teaming:**

- [ ] **RT1** ejecutado: con job de Salesforce apagado, sistema emite CRITICAL en <10 min con `causa_probable = job`.
- [ ] **RT2** ejecutado: con token de SF revocado, sistema **no reintenta**, emite CRITICAL con `acción_sugerida` referenciando SOP-001.
- [ ] **RT3** ejecutado: con SQL Server apagado, M4 emite CRITICAL inmediato (latencia inmediata).
- [ ] **RT5** ejecutado: pedido con estado incorrecto aparece en `DiscrepanciesPage` **sin** disparar alerta WARN/CRITICAL.
- [ ] **RT7** ejecutado: pedido cancelado correctamente **no** genera alerta.
- [ ] **RT-Persist** ejecutado: cerrar y reabrir el dashboard → alertas activas siguen visibles.
- [ ] Reporte de red-teaming generado (markdown o tabla) con resultado por escenario + evidencia (screenshots opcionales).
- [ ] Los 6 escenarios pasan **antes del cierre del curso** (criterio de aceptación PRD §11).

## Sprint sugerido

**Sprint 1 (simulator) + Sprint 4 (red-teaming).** PRD §13: red-teaming es la actividad principal del Sprint 4.

## Notas técnicas para Construction

- El simulator es **exclusivo del MVP**. Post-MVP se desconecta y M2 apunta a la BD real.
- Los 6 escenarios pueden ejecutarse manualmente o automatizarse con un test runner (`xunit` + helpers que toggle el simulator).
- La probabilidad de fallo configurable permite reproducir cada escenario con consistencia (`failureProbability=1.0` para RT3, `failureProbability=0.0` para baseline).

---

# Resumen final

- **7 unidades de trabajo** descompuestas por capability.
- **29 stories distribuidas** sin huérfanas (verificación detallada en `unit-of-work-story-map.md`).
- **Mapeo a sprints PRD §13:**
  - **Sprint 1:** U1 + U2 (base) + U7 (simulator).
  - **Sprint 2:** U3 + U4 + U6 (mínimo).
  - **Sprint 3:** U5 + U6 (completo) + U2 (vistas).
  - **Sprint 4:** U7 (red-teaming) + ajustes finales + demo.
- **Próximos artefactos en este stage:** [`unit-of-work-dependency.md`](./unit-of-work-dependency.md), [`unit-of-work-story-map.md`](./unit-of-work-story-map.md).
- **Tras cerrar Units Generation:** Inception **completa**. Construction queda como hito futuro (fuera del alcance actual).
