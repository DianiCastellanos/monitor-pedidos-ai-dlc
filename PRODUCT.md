# MonitorPedidos AI — Product Overview

**Empresa:** Manufacturas Eliot  
**Owner:** Diana Castellanos  
**Sponsor:** Alex Cárdenas (Jefe de Análisis de Sistemas)  
**Marco:** Hardcore AI Cohorte 2 — Estación 2  
**Versión:** 2.0 (basada en PRD v2.5 + AI-DLC completo, 2026-05-25)

---

## One-Liner

MonitorPedidos AI detecta en menos de 5 minutos fallas en la descarga de pedidos, reduce el monitoreo manual de 25 a menos de 10 horas/mes, y entrega alertas con diagnóstico accionable en español — sin nube, sin complejidad.

---

## Problem

Los equipos de Manufacturas Eliot validan el flujo Salesforce → Multivende → SQL Server con consultas SQL manuales y Postman. No existen alertas automáticas, no hay historial de incidentes, y el conocimiento de diagnóstico depende de una sola persona.

| Dolor | Magnitud |
|-------|----------|
| Validación manual diaria | ~25 h/mes |
| Tiempo de detección de fallas | 1–2 horas |
| Incidentes no detectados | ≈2/semana |
| Conocimiento operativo no documentado | Riesgo de onboarding |

**Impacto:** retrasos en bodega, correos cruzados y escalamientos que el equipo técnico debe resolver sin contexto ni historial.

---

## Solution

Un agente basado en reglas configurables que observa continuamente el estado del sistema, detecta anomalías y genera alertas con diagnóstico accionable, ejecutándose exclusivamente en red interna sin dependencias de nube.

```
Scheduler → Checkers → Clasificador de causa → Incidentes → Dashboard NOC
                     ↗
          BrandMonitorChecker → Tablero de marcas (solo visual, sin incidentes)
```

### Principios no negociables

| Principio | Descripción |
|-----------|-------------|
| Explicabilidad | Toda alerta incluye 6 campos en español: qué pasó, cuándo, dónde, severidad, causa probable y acción sugerida |
| Solo lectura | Nunca modifica pedidos ni estados en APIs externas |
| Soberanía de datos | Los datos sensibles no salen del entorno local |
| Trazabilidad | Todo incidente se registra desde el día 0 |
| Humano en el loop | Reintentos solo para consultas API (máx 2, únicamente errores 5xx/timeout) |
| Calibración humana | Reglas editables con historial inmutable de cambios (autor + razón obligatoria) |

---

## Core Capabilities

| Capacidad | Descripción |
|-----------|-------------|
| Monitoreo de componentes | BD interna, APIs externas, jobs de sincronización, health check de BD |
| Dashboard NOC | Vista de estado en tiempo real con semáforo 🔴🟡🟢 por módulo |
| Alertas en tiempo real | SignalR — push browser, sonido y parpadeo de título como fallback |
| Clasificador de causa raíz | 6 categorías: `bd`, `job`, `api`, `token`, `data_quality`, `no_determinada` |
| Cierre automático | Incidente se cierra automáticamente tras 2 resultados OK consecutivos del mismo módulo |
| Brand Monitor | Tendencia de pedidos pendientes por marca/site cada 10 min — solo visual |
| Reglas editables | Motor de reglas configurables con historial inmutable de cambios (solo Técnico) |
| Historial de incidentes | Búsqueda paginada filtrable por fecha, severidad y módulo (retención 90 días) |
| Resumen semanal | Agrupación de incidentes por causa y severidad para calibración semanal |
| Modo NOC | Vista pantalla completa sin sidebar para monitoreo continuo en sala de operaciones |
| Simulación (dev) | Red-teaming con 6 escenarios controlados sin afectar datos reales |

---

## Monitored Modules

| ID | Módulo | Cadencia | Función |
|----|--------|----------|---------|
| M2 | BD Pedidos | 5 min | Chequeo de pedidos en BD interna — ventana configurable |
| M3 | APIs Externas | 10 min | Salesforce + Multivende (con reintentos en 5xx/timeout; sin renovación automática de token) |
| M4 | Health BD | 5 min | `SELECT 1` — latencia y disponibilidad de la base de datos |
| M11 | Jobs Sync | 5 min | Verificación de ejecución de jobs (solo consulta, sin reintento) |
| BM | Brand Monitor | 10 min | Tendencia de pedidos por site — no genera incidentes, solo actualiza vista visual |

---

## Alert Message Structure

Toda alerta generada por el sistema contiene exactamente **6 campos** en español, inmutables desde el momento de creación:

| Campo | Descripción | Ejemplo |
|-------|-------------|---------|
| `QuePaso` | Descripción del evento detectado | "API Salesforce respondió HTTP 401" |
| `Cuando` | Timestamp del evento (UTC) | "2026-05-25T22:10:34Z" |
| `Donde` | Módulo afectado en texto legible | "M3 — APIs Externas" |
| `SeveridadTexto` | Severidad en español | "Crítico" |
| `CausaProbable` | Causa clasificada en texto | "Token de autenticación expirado" |
| `AccionSugerida` | Pasos recomendados | "Renovar token según SOP-001" |

---

## Incident Lifecycle

```
CHECKER EJECUTA
     │
     ▼
CheckResult(Status=Critical)
     │
     ▼
IncidentService.OpenIncidentAsync()  ──────── si ya hay un incidente abierto para ese módulo
     │                                          → retorna el existente (idempotente)
     ▼
Incident.Open() ──► estado: ABIERTO
     │
     │  Checker sigue ejecutando en cadencia
     ▼
CheckResult(Status=Ok) × 2 consecutivos del mismo módulo
     │
     ▼
IncidentService.TryCloseOnConsecutiveOkAsync()
     │
     ├── [Automático]  ──► Incident.CloseAutomatically()  →  CloseType = Automatic
     │
     └── [Manual]      ──► Técnico/Operador cierra con comentario obligatorio
                             Incident.CloseManually(role, comentario) → CloseType = Manual

RETENCIÓN: los incidentes se purgan automáticamente a los 90 días
CANDIDATO: si Cause = NoDeterminada → IsCandidatoReglaNueva = true → visible en UI
```

---

## Brand Monitoring

El **BrandMonitorChecker** consulta pedidos pendientes por site a Salesforce, compara contra la snapshots anterior (hace 10 min) y clasifica según el umbral `PendingDropThreshold` configurado en M6.

| Site | Estado 🟢 "Normal" | Estado 🟡 "Lento" | Estado 🔴 "Riesgo" |
|------|--------------------------|---------------------|----------------------|
| Patprimo | Δ ≥ umbral | 0 < Δ < umbral | Δ ≤ 0 |
| SevenSeven | Δ ≥ umbral | 0 < Δ < umbral | Δ ≤ 0 |
| Atmos | Δ ≥ umbral | 0 < Δ < umbral | Δ ≤ 0 |
| Ostu | Δ ≥ umbral | 0 < Δ < umbral | Δ ≤ 0 |

El umbral `PendingDropThreshold` se configura desde Reglas (M6). El checker actualiza `brand_snapshots` (4 filas, una por site) — no genera incidentes ni push de notificaciones. Si un site falla al consultar, `current = -1` → estado Rojo por defecto.

---

## Rules Management

Las reglas son la capa configurable que define cuándo y cómo se generan incidentes. Solo el **Técnico** puede gestionarlas.

| Parámetro | Tipo | Ejemplo |
|-----------|------|---------|
| Nombre | texto | "Ventana de pedidos Salesforce" |
| Descripción | texto | "Alerta si no hay pedidos en la ventana esperada" |
| Módulo aplicable | `ModuleId` | `DbOrders`, `DbHealth`, `Jobs`, `BrandMonitor` |
| Severidad | `Severity` | `Info`, `Warn`, `Critical` |
| Condición (por módulo): | | |
| → `WindowHours` + `MinOrders` | DbOrders | ventana de 2h, mínimo 1 pedido |
| → `LatencyWarnMs` + `LatencyCriticalMs` | DbHealth | 500ms warn, 2000ms critical |
| → `PendingDropThreshold` | BrandMonitor | umbral de reducción por site |

Cada cambio de regla (creación, edición, activación, desactivación) genera un `RuleHistoryEntry` con autor, timestamp y razón obligatoria — historial inmutable.

---

## Pages & Routes

| Ruta | Página | Rol | Descripción |
|------|--------|-----|-------------|
| `/` | Selección de identidad | Todos | Pantalla inicial: 2 botones (Analista / Técnico) |
| `/dashboard` | Dashboard NOC | Todos | Cards de estado M2/M3/M4/M11 + alertas activas |
| `/noc` | Modo NOC | Todos | Dashboard sin sidebar, pantalla completa |
| `/brand-monitor` | Brand Monitor | Todos | Tabla semáforo por site |
| `/incidents` | Historial | Todos | Búsqueda paginada de incidentes con filtros |
| `/incidents/weekly` | Resumen semanal | Todos | Agrupación por causa/severidad |
| `/discrepancies` | Discrepancias | Todos | Panel UC6 — sin notificaciones, solo visual |
| `/rules` | Gestión de reglas | Solo Técnico | Lista de reglas con toggle activar/desactivar |
| `/rules/new` | Nueva regla | Solo Técnico | Formulario de creación |
| `/rules/{id}/edit` | Editar regla | Solo Técnico | Formulario de edición |
| `/rules/{id}/history` | Historial de regla | Solo Técnico | Timeline inmutable con diffs |
| `/logs` | Logs técnicos | Solo Técnico | Últimas 500 líneas Serilog, exportar CSV/JSON |
| `/simulation` | Simulación (dev) | Solo Técnico | Red-teaming con 6 escenarios |

---

## Users

| Perfil | Nombre en sistema | Acceso |
|--------|-------------------|---------| 
| Analista Operativo | `Operador` | Dashboard, Brand Monitor, Historial, Discrepancias, Resumen Semanal |
| Responsable Técnico | `Técnico` | Todo lo anterior + Reglas + Logs + Simulación |

**Identidad:** selección simple sin contraseñas — al abrir el dashboard el usuario elige su perfil entre dos botones. Cookie de sesión `HttpOnly + SameSite=Strict`. Sin ASP.NET Core Identity completo, sin password hashing, sin cuentas en BD. Los controles del rol `Técnico` son protegidos server-side en Blazor (`[Authorize(Roles="Técnico")]`), no solo ocultos en UI.

---

## Technology Stack

| Capa | Tecnología | Versión |
|------|-----------|---------|
| Frontend | Blazor Server | ASP.NET Core 8 |
| Real-time | SignalR (`AlertsHub`) + `AlertBroadcaster` | integrado en ASP.NET Core 8 |
| Backend | ASP.NET Core 8 — Background Services (`PeriodicTimer`) | .NET 8 LTS |
| Resiliencia | Polly v8 — 3 reintentos exponenciales + timeout 10s | v8.x |
| ORM | Entity Framework Core 8 — Code First, migrations | 8.x |
| Base de datos | SQL Server MonitorPedidosDb (172.16.0.41) / Express | MonitorPedidosDb v15+ |
| Logging | Serilog — campos estructurados sin PII | — |
| Secretos | User Secrets (dev) / Variables de entorno (prod) | — |

---

## Domain Model Summary

### Entidades principales

```
Incident (aggregate root)
├── AlertMessage (value object — 6 campos, OwnsOne en BD)
├── Severity: Info | Warn | Critical
├── CauseCategory: Bd | Job | Api | Token | DataQuality | NoDeterminada
├── ModuleId: DbOrderChecker(M2) | ApiChecker(M3) | DbHealthChecker(M4) | JobsMonitor(M11)
├── CloseType: Automatic | Manual
└── IsCandidatoReglaNueva (true si Cause=NoDeterminada)

Rule
├── RuleCondition (OwnsOne — campos nullable según módulo)
├── RuleHistoryEntry (colección — inmutable)
└── IsActive

BrandSnapshot (4 filas, una por site)
├── Site: Patprimo | SevenSeven | Atmos | Ostu
├── PendingCountCurrent / PendingCountPrevious
├── Threshold
└── SnapshotStatus: Green | Yellow | Red
```

### Tablas de base de datos

| Tabla | Propietario | Descripción |
|-------|-------------|-------------|
| `incidents` | U2 | Incidentes + AlertMessage (OwnsOne) |
| `rules` | U5 | Motor de reglas |
| `rule_conditions` | U5 | Condiciones por módulo |
| `rule_history` | U5 | Historial inmutable de cambios |
| `brand_snapshots` | U6 | 4 filas estáticas (una por site) |
| `simulated_orders` | U7 | Solo en entorno dev |
| `simulated_job_statuses` | U7 | Solo en entorno dev |

---

## Notification System

```
Incident abierto → NotificationService
     │
     ├── AlertBroadcaster (Singleton + IAsyncDisposable)
     │       → event OnAlert → RealtimePage.StateHasChanged()   [actualiza UI Blazor]
     │
     └── IHubContext<AlertsHub>
             → SignalR → navegador cliente
                  → notifications.js
                       ├── Si Notification.permission == "granted"
                       │       → Browser Push Notification
                       └── Fallback (denegado / primer uso)
                               → playAlertSound()   → Audio('/sounds/alert.mp3')
                               → flashTitle()        → alterna "🔴 ALERTA" / "MonitorPedidos"
```

---

## MVP Constraints

- **Sin exposición a internet** — solo `localhost` o red interna (LAN). Sin ngrok, túneles ni dominios públicos.
- **Sin credenciales hardcodeadas** — tokens y URLs de API en User Secrets / variables de entorno.
- **Sin automatización de tokens** — si un token expira (HTTP 401), el sistema alerta con sugerencia "Renovar según SOP-001"; el operador actúa manualmente.
- **Sin correos automáticos** — v1 solo dashboard + push browser.
- **Sin ML complejo** — reglas estáticas determinísticas, explicables y editables.
- **Sin eliminación de reglas** — solo activar/desactivar con razón obligatoria.

---

## Success Metrics

| KPI | Baseline | Objetivo MVP |
|-----|----------|-------------|
| O1 Tiempo de detección | 1–2 h | **< 10 min** |
| O2 Horas/mes de validación | ~25 h | **< 10 h** |
| O3 Incidentes no detectados | ≈2/semana | **< 1/mes** |
| O4 MTTR | — | **< 20 min** |

**North Star:** horas operativas recuperadas por mes (baseline 25h → objetivo ≥15h).  
**Hito de cierre:** 6 escenarios de red-teaming superados + métrica de reducción demostrable ante el sponsor.

---

## Red-Teaming Scenarios

| Escenario | Módulo | Resultado esperado |
|-----------|--------|-------------------|
| RT1 — Job Salesforce detenido | M11 | CRITICAL en < 10 min |
| RT2 — Token Salesforce revocado | M3 | CRITICAL con sugerencia SOP-001; sin reintento (401 no aplica) |
| RT3 — SQL Server apagado | M4 | CRITICAL inmediato |
| RT5 — Pedido con estado incorrecto | UC6 (Discrepancias) | Aparece en panel silencioso, sin push |
| RT7 — Pedido cancelado | M7 | Ignorado correctamente |
| RT-Persist — Cerrar y reabrir dashboard | Dashboard | Alertas activas siguen visibles |

---

## Architecture Summary

```
┌─────────────────────────────────────────────────────────────────┐
│                      Blazor Server (M8)                         │
│  /dashboard (NOC)  /brand-monitor  /incidents  /rules  /logs    │
└──────────────────────────┬──────────────────────────────────────┘
                           │  SignalR (AlertsHub) + AlertBroadcaster
┌──────────────────────────▼──────────────────────────────────────┐
│              ASP.NET Core 8 — Background Services               │
│  M1 Scheduler (PeriodicTimer) │ M7 CauseClassifier              │
│  M2 DbOrderChecker (5 min)    │ M10 AlertRenderer (plantillas)  │
│  M3 ApiChecker + Polly (10min)│ BrandMonitorChecker (10 min)    │
│  M4 DbHealthChecker (5 min)   │ M6 RulesManagement              │
│  M11 JobsMonitor (5 min)      │ M9 IncidentService              │
└──────────────────────────┬──────────────────────────────────────┘
                           │  EF Core 8 — Code First
┌──────────────────────────▼──────────────────────────────────────┐
│                    SQL Server MonitorPedidosDb (172.16.0.41)                           │
│  incidents  │  rules  │  rule_conditions  │  rule_history       │
│  brand_snapshots  │  simulated_orders (dev)                     │
└─────────────────────────────────────────────────────────────────┘
```

---

## Product Philosophy

MonitorPedidos no busca ser complejo. Busca ser:

- **Claro** — cualquier operador identifica el estado en menos de 2 segundos
- **Accionable** — cada alerta dice exactamente qué hacer
- **Explicable** — no hay black boxes; las reglas son visibles y editables
- **Propio** — activo interno de Manufacturas Eliot, sin dependencia de SaaS ni nube
- **Evolutivo** — la arquitectura permite escalar a on-prem corporativo sin cambios de código

---

*MonitorPedidos AI — PRODUCT v2.0 · Manufacturas Eliot · 2026-05-25*
