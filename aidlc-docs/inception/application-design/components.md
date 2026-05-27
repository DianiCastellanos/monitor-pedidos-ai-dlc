# Components — MonitorPedidos AI

**Fecha:** 2026-05-21
**Versión:** 1.0
**Stage:** Inception → Application Design (Part 2 — Generation)
**Fuentes:** [`prd.md`](../../../prd.md) v2.3 §9, [`requirements.md`](../requirements/requirements.md) v1.1, [`stories.md`](../user-stories/stories.md), [`application-design-plan.md`](../plans/application-design-plan.md) (6 decisiones = A).

---

## Decisiones arquitectónicas aplicadas

| # | Decisión | Aplicación |
|---|----------|-------------|
| Q1 | Monolito modular | Una sola solución `MonitorPedidos.sln` con varios proyectos. Un único proceso ASP.NET Core. |
| Q2 | Vertical Slice / Feature Folders | Cada módulo M1–M11 vive en una carpeta `Features/<Modulo>/` con su modelo, lógica y entrypoints. Núcleo compartido en `Domain/` e `Infrastructure/`. |
| Q3 | Blazor Server | El dashboard se implementa con páginas `.razor` server-rendered con circuito SignalR. |
| Q4 | `BackgroundService` + `PeriodicTimer` | M1 Scheduler se implementa como `BackgroundService` nativo. |
| Q5 | SignalR Hub | M8 incluye un `Hub` específico (`AlertsHub`) que el `NotificationService` usa para empujar alertas a los clientes Blazor conectados. |
| Q6 | Application services por capability | 5 services orquestadores: `MonitoringService`, `RuleManagementService`, `IncidentService`, `AuthService`, `NotificationService`. |

---

## Estructura de la solución

```text
MonitorPedidos.sln
├── src/
│   ├── MonitorPedidos.Web/                    # Proyecto ASP.NET Core + Blazor Server
│   │   ├── Pages/                             # Páginas Blazor del dashboard
│   │   ├── Components/                        # Componentes Blazor reutilizables
│   │   ├── Hubs/                              # AlertsHub (SignalR)
│   │   ├── Areas/Identity/                    # ASP.NET Core Identity (login + roles)
│   │   ├── Features/
│   │   │   ├── Scheduler/                     # M1
│   │   │   ├── Checks/
│   │   │   │   ├── DbOrderCheck/              # M2
│   │   │   │   ├── ApiCheck/                  # M3
│   │   │   │   ├── DbHealthCheck/             # M4
│   │   │   │   └── JobsCheck/                 # M11
│   │   │   ├── Rules/                         # M6
│   │   │   ├── Classification/                # M7
│   │   │   ├── Alerts/                        # M10
│   │   │   ├── Incidents/                     # M9
│   │   │   └── Dashboard/                     # M8 (orquesta lo demás en UI)
│   │   ├── Services/                          # Application services (capability layer)
│   │   ├── Simulation/                        # Job de generación de datos simulados
│   │   └── Program.cs
│   ├── MonitorPedidos.Domain/                 # Núcleo compartido (modelos + enums)
│   └── MonitorPedidos.Infrastructure/         # EF Core context / repositorios / logging
└── tests/
    ├── MonitorPedidos.UnitTests/
    └── MonitorPedidos.IntegrationTests/
```

> **Nota:** la decisión de **librería de persistencia** (Dapper vs EF Core) y la **librería de logging** (Serilog) se confirman en NFR Requirements (Construction phase). Application Design asume `MonitorPedidos.Infrastructure` como capa de abstracción sin atarse a una implementación específica.

---

# 1. Componentes Funcionales (Features / Slices)

## 1.1 M1 — Scheduler (`Features/Scheduler/`)

| Atributo | Valor |
|----------|-------|
| **Tipo** | Background worker |
| **Clase principal** | `MonitoringSchedulerService : BackgroundService` |
| **Responsabilidad** | Orquesta la ejecución periódica de los chequeos M2, M3, M4 y M11 según sus cadencias (5/10 min). Cancela limpiamente al apagado de la app. Para M3 (APIs), permite reintentos limitados (máx 2, solo 5xx/timeout). |
| **Interfaz pública** | Sin interfaz HTTP. Se registra en DI como `IHostedService`. |
| **Persistencia que toca** | Ninguna directamente — invoca `MonitoringService.RunCheckAsync(...)` que delega en repositorios. |
| **Trazabilidad** | RF-01, RF-02, RF-03, RF-04, RF-05, RF-06 / UC1, UC4, UC7 / US-07, US-08, US-09 |

## 1.2 M2 — Chequeo de pedidos en BD interna (`Features/Checks/DbOrderCheck/`)

| Atributo | Valor |
|----------|-------|
| **Tipo** | Domain checker |
| **Clase principal** | `DbOrderChecker : ICheckExecutor` |
| **Responsabilidad** | Consulta la tabla `simulated_orders` (o BD real post-MVP) para verificar la existencia de pedidos en la ventana esperada por las reglas activas de M6. |
| **Interfaz pública** | `ICheckExecutor.CheckAsync(CancellationToken) → CheckResult` |
| **Cadencia** | 5 minutos (configurable, default del PRD). |
| **Trazabilidad** | RF-02, RF-21, RNF-01 / UC1 / US-07, RT3, RT7 |

## 1.3 M3 — Chequeo de APIs externas (`Features/Checks/ApiCheck/`)

| Atributo | Valor |
|----------|-------|
| **Tipo** | Domain checker con políticas de reintento |
| **Clase principal** | `ApiChecker : ICheckExecutor` (compuesta de `SalesforceClient` + `MultivendeClient`) |
| **Responsabilidad** | Consulta las APIs externas (Salesforce, Multivende). **Solo lectura** (P2). Aplica reintentos limitados (máx 2) **solo** ante 5xx/timeout. Ante 401, **no reintenta** y deja que M7 clasifique como `token`. |
| **Interfaz pública** | `ICheckExecutor.CheckAsync(CancellationToken) → CheckResult` |
| **Cadencia** | 10 minutos. |
| **Persistencia** | Registra cada `auto_reintento` en el log estructurado y como evento asociado al incidente. |
| **Trazabilidad** | RF-03, RF-06, RF-07 / UC1, UC4 / US-10, US-16, RT2 |

## 1.4 M4 — Health check de BD (`Features/Checks/DbHealthCheck/`)

| Atributo | Valor |
|----------|-------|
| **Tipo** | Domain checker |
| **Clase principal** | `DbHealthChecker : ICheckExecutor` |
| **Responsabilidad** | Ejecuta `SELECT 1` contra el motor de BD; mide latencia y disponibilidad. Emite WARN antes de CRITICAL si la latencia es alta pero no hay timeout (mitigación R10). |
| **Interfaz pública** | `ICheckExecutor.CheckAsync(CancellationToken) → CheckResult` |
| **Cadencia** | 5 minutos. |
| **Trazabilidad** | RF-04, RNF-01 / UC7 / US-08, RT3 |

## 1.5 M11 — Verificador de ejecución de jobs (`Features/Checks/JobsCheck/`)

| Atributo | Valor |
|----------|-------|
| **Tipo** | Domain checker (read-only) |
| **Clase principal** | `JobsChecker : ICheckExecutor` |
| **Responsabilidad** | Consulta el estado de los jobs de integración (tabla / endpoint del orquestador externo) y registra el último resultado. **No** reintenta jobs (Decisión #2). |
| **Interfaz pública** | `ICheckExecutor.CheckAsync(CancellationToken) → CheckResult` |
| **Cadencia** | 5 minutos. |
| **Trazabilidad** | RF-05 / UC1 / US-09, RT1 |

## 1.6 M6 — Motor de reglas y expectativas (`Features/Rules/`)

| Atributo | Valor |
|----------|-------|
| **Tipo** | Feature slice (modelo + repositorio + páginas Blazor) |
| **Clases principales** | `Rule` (entidad), `RuleHistoryEntry` (entidad), `IRuleRepository`, `IRuleHistoryRepository`, `RulesPage.razor`, `RuleEditPage.razor`, `RuleHistoryPage.razor` |
| **Responsabilidad** | Almacena las reglas estáticas que definen "qué se espera" (ventanas, thresholds, severidades). Expone CRUD desde el dashboard con validación de autor y razón obligatoria. Mantiene historial inmutable con diff antes/después. |
| **Autorización** | Edición restringida a rol `Técnico` (RF-17). Operador solo puede leer. |
| **Trazabilidad** | RF-14, RF-15, RF-16, RF-17 / UC5, Journey 2 / US-19, US-20, US-21, US-22 |

## 1.7 M7 — Clasificador de causa raíz (`Features/Classification/`)

| Atributo | Valor |
|----------|-------|
| **Tipo** | Domain service (stateless) |
| **Clase principal** | `CauseClassifier : ICauseClassifier` |
| **Responsabilidad** | Aplica la cascada de clasificación `bd → job → api → token → data_quality → no_determinada` sobre el conjunto de `CheckResult` más reciente. Marca incidentes `no_determinada` como candidatos a regla nueva. |
| **Interfaz pública** | `ICauseClassifier.Classify(IReadOnlyList<CheckResult>) → CauseCategory` |
| **Persistencia que toca** | Ninguna. |
| **Trazabilidad** | RF-08, RF-09, RF-10 / UC1, UC4, Journey 2 / US-07, US-10, US-18 |

## 1.8 M10 — Explicador en lenguaje natural (`Features/Alerts/`)

| Atributo | Valor |
|----------|-------|
| **Tipo** | Domain service (stateless) |
| **Clase principal** | `AlertTemplateRenderer : IAlertExplainer` |
| **Responsabilidad** | Toma el contexto de un incidente (causa + módulo + datos brutos) y renderiza una alerta con los **6 campos** en español (`qué_pasó`, `cuándo`, `dónde`, `severidad`, `causa_probable`, `acción_sugerida`) a partir de plantillas configurables — sin concatenación ad-hoc. |
| **Interfaz pública** | `IAlertExplainer.Render(IncidentContext) → AlertMessage` |
| **Trazabilidad** | RF-11, RF-12, RF-13 / UC1, UC3, UC4 / US-06 |

## 1.9 M9 — Repositorio de incidentes (`Features/Incidents/`)

| Atributo | Valor |
|----------|-------|
| **Tipo** | Feature slice (entidad + repositorio + páginas) |
| **Clases principales** | `Incident` (entidad), `IIncidentRepository`, `IncidentHistoryPage.razor`, `WeeklySummaryPage.razor` |
| **Responsabilidad** | Persiste todos los incidentes desde el día 0 (P4, RF-18). Soporta consulta filtrada (vista histórica), resumen semanal (UC5), cierre automático tras 2 OK consecutivos, cierre manual con `comentario_resolucion`. Aplica retención de **90 días** (RF-21). |
| **Trazabilidad** | RF-18, RF-19, RF-20, RF-21, RF-22 / UC2, UC3, UC5 / US-05, US-11, US-12, US-13, US-14 |

## 1.10 M8 — Dashboard (`Features/Dashboard/` + `Hubs/AlertsHub.cs`)

| Atributo | Valor |
|----------|-------|
| **Tipo** | Presentation layer (Blazor Server) + SignalR Hub |
| **Componentes Blazor principales** | `RealtimePage.razor`, `HistoricPage.razor`, `WeeklySummaryPage.razor`, `DiscrepanciesPage.razor`, `RulesPage.razor` (de M6), `LogsPage.razor` (solo Técnico), `IncidentDetailPage.razor`, `NocLayout.razor` (modo NOC pantalla completa). |
| **Hub SignalR** | `AlertsHub : Hub` — clientes Blazor suscritos reciben `AlertReceived(AlertMessage)` y `IncidentClosed(incidentId)`. |
| **Responsabilidad** | Renderizar las 4 vistas requeridas (real-time, histórico, semanal, discrepancias). Mostrar modo NOC. Reaccionar a push del servidor vía SignalR. Implementar fallback (sonido + parpadeo del título) cuando la Notification API del navegador esté bloqueada (RF-25). |
| **Autorización** | Toda página excepto `/Identity/Select` requiere selección de identidad previa (RF-26). Las páginas de edición de reglas y de logs técnicos requieren rol `Técnico` (RF-27). |
| **Trazabilidad** | RF-22, RF-23, RF-24, RF-25, RF-26, RF-27, RF-29, RF-30 / UC2, UC3, UC5, UC6 / US-01..US-05, US-15, US-17 |

---

# 2. Componentes Cross-Cutting

## 2.1 Domain (`MonitorPedidos.Domain/`)

| Atributo | Valor |
|----------|-------|
| **Responsabilidad** | Modelos compartidos (`Incident`, `Rule`, `RuleHistoryEntry`, `AlertMessage`, `CheckResult`, `IncidentContext`) y enums (`Severity` = `INFO`/`WARN`/`CRITICAL`, `CauseCategory` = 6 categorías, `ModuleId`). |
| **Sin dependencias salientes** | Es la base de la pirámide; no referencia infraestructura ni UI. |

## 2.2 Infrastructure (`MonitorPedidos.Infrastructure/`)

| Atributo | Valor |
|----------|-------|
| **Responsabilidad** | Implementaciones concretas de repositorios (`IIncidentRepository`, `IRuleRepository`, etc.), `DbContext` (o equivalente), configuración de Serilog (logging estructurado). Sin Identity store — no hay tablas de usuarios. |
| **Cumple SECURITY** | SECURITY-01 (cifrado at-rest activado), SECURITY-03 (logging), SECURITY-05 (parametrized queries / EF Core o Dapper parámetros), SECURITY-12 (parcial — solo cookie management; sin password hashing). |

## 2.3 Application Services Layer (`MonitorPedidos.Web/Services/`)

Ver [`services.md`](./services.md) para detalle de los 5 application services. Resumen:

| Service | Coordina |
|---------|----------|
| `MonitoringService` | Scheduler → Checkers → Classifier → Explainer → IncidentRepository → NotificationService |
| `RuleManagementService` | RuleRepository + RuleHistoryRepository + validación de autor/razón |
| `IncidentService` | Cierre auto (2 OK), cierre manual con comentario, archivado por retención 90 días |
| `IdentityService` | Emite y cierra cookies de sesión (`SignInAsync`/`SignOutAsync`) para la selección simple de identidad |
| `NotificationService` | Pública: `BroadcastAlert(AlertMessage)`, `BroadcastClose(incidentId)`. Internamente delega en `IHubContext<AlertsHub>` |

## 2.4 Simulation (`MonitorPedidos.Web/Simulation/`)

| Atributo | Valor |
|----------|-------|
| **Tipo** | Background worker secundario |
| **Clase principal** | `OrdersSimulatorService : BackgroundService` |
| **Responsabilidad** | Inserta pedidos en la tabla `simulated_orders` cada 5–10 min con probabilidad configurable de fallo, para alimentar M2 y permitir los 6 escenarios de red-teaming. Estrictamente solo en MVP. |
| **Trazabilidad** | C-06 (restricción de requirements.md) / RT1, RT2, RT3, RT5, RT7 |

## 2.5 Identity Selection & Authorization (`Areas/Identity/` + middleware)

| Atributo | Valor |
|----------|-------|
| **Responsabilidad** | Selección simple de identidad sin contraseñas. Dos identidades pre-definidas en código: *Analista Operativo* (rol `Operador`) y *Responsable Técnico* (rol `Técnico`). Cookie de sesión con `HttpOnly` + `SameSite=Strict` + `Secure` cuando HTTPS interno. Sin ASP.NET Core Identity completo, sin password hashing, sin lockout, sin cuentas en base de datos. |
| **Trazabilidad** | RF-26, RF-27, RF-28, RF-29, RNF-11, RNF-14 / US-15, US-23, US-24 |

## 2.6 Security Headers Middleware

| Atributo | Valor |
|----------|-------|
| **Responsabilidad** | Middleware que emite `Content-Security-Policy: default-src 'self'`, `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, y `Strict-Transport-Security` solo cuando se sirve por HTTPS interno. |
| **Trazabilidad** | RNF-09 / US-28 |

## 2.7 Global Exception Handler

| Atributo | Valor |
|----------|-------|
| **Responsabilidad** | Captura excepciones no manejadas, las loggea con request_id (sin PII, RNF-08), responde al usuario con mensaje genérico (sin stack trace en producción). Cumple fail-closed (SECURITY-15). |
| **Trazabilidad** | RNF-15 / US-27 |

---

# 3. Matriz de cobertura componente → story

| Componente | Stories cubiertas |
|------------|--------------------|
| M1 Scheduler | US-07, US-08, US-09, US-10 (gatillan los checkers) |
| M2 DbOrderChecker | US-07 |
| M3 ApiChecker | US-09 (API down), US-10 (token), US-16 (reintentos) |
| M4 DbHealthChecker | US-08 |
| M11 JobsChecker | US-09 |
| M6 Rules | US-19, US-20, US-21, US-22 |
| M7 CauseClassifier | US-07, US-10, US-18 |
| M10 AlertTemplateRenderer | US-06 |
| M9 Incidents | US-05, US-11, US-12, US-13, US-14 |
| M8 Dashboard (Blazor + AlertsHub) | US-01..US-05, US-15, US-17 |
| `MonitoringService` | US-07..US-10, US-16, US-18 |
| `RuleManagementService` | US-19..US-22 |
| `IncidentService` | US-13, US-14 |
| `IdentityService` + Cookie Auth | US-15, US-23, US-24 |
| `NotificationService` + `AlertsHub` | US-03, US-06 |
| `OrdersSimulatorService` | RT1, RT2, RT3, RT5, RT7 (alimenta los escenarios) |
| Security Headers Middleware | US-28 |
| Global Exception Handler | US-27 |
| Infrastructure (cifrado + logging) | US-25, US-26 |

✅ **Cobertura completa de las 29 stories**.

---

# 4. Resumen

- **11 componentes funcionales** (M1–M11) en feature folders.
- **7 componentes cross-cutting** (Domain, Infrastructure, Application Services, Simulation, Auth, Security Headers, Exception Handler).
- **1 hub SignalR** (`AlertsHub`) para push real-time al dashboard Blazor.
- Toda la solución se compila y despliega como **un único proceso ASP.NET Core**, ejecutándose en localhost o red interna del equipo (sin exposición pública).
- Próximo artefacto: [`component-methods.md`](./component-methods.md) con las firmas públicas.
