# ADR-001 — Monolito Modular con Vertical Slice Architecture

## Metadatos

| Campo | Valor |
|-------|-------|
| **ID** | ADR-001 |
| **Estado** | Aceptada |
| **Fecha de decisión** | 2026-05-21 |
| **Decider** | Diana Castellanos (Owner) |
| **Propuesto por** | Arquitecto (AI-DLC Application Design) |
| **Categoría** | Arquitectura de sistema |
| **Impacto** | Crítico — afecta organización del código, estrategia de pruebas, evolución futura y velocidad de desarrollo |
| **Documentos relacionados** | [`application-design-plan.md`](../inception/plans/application-design-plan.md) §3 Q1–Q2, [`components.md`](../inception/application-design/components.md), [`component-dependency.md`](../inception/application-design/component-dependency.md), [`unit-of-work.md`](../inception/application-design/unit-of-work.md) |
| **Revisión sugerida** | Al pasar de MVP (localhost) a despliegue on-prem con > 10 usuarios concurrentes |

---

## Contexto

MonitorPedidos AI es un sistema de monitoreo proactivo de descarga de pedidos para Manufacturas Eliot. El equipo operativo dedicaba entre 1 y 2 horas diarias (≈ 25 h/mes) a validación manual; el objetivo es reducirlo a < 10 h/mes con detección automática en < 10 minutos.

Las restricciones concretas al momento de la decisión:

| Restricción | Detalle |
|-------------|---------|
| **Tiempo de entrega** | 4 semanas (4 sprints de 1 semana) |
| **Usuarios concurrentes** | 5 máximo (Operador, Técnico, Sponsor, invitados internos) |
| **Despliegue** | Localhost o red interna del equipo; **sin exposición a internet** durante el MVP |
| **Equipo de desarrollo** | Pequeño; sin overhead de DevOps distribuido |
| **Módulos lógicos** | 11 módulos (M1–M11, M5 integrado en M6) ya definidos en el PRD v2.3 |
| **Stack decidido** | .NET 8 + ASP.NET Core + SQL Server MonitorPedidosDb (172.16.0.41) + Blazor Server + SignalR |
| **Naturaleza del sistema** | Proceso único; los módulos se comunican internamente, no entre procesos |

El sistema requería una decisión sobre **dos aspectos inseparables**:
1. **Estilo arquitectónico** — cómo se despliega y distribuye el sistema.
2. **Patrón de organización de código** — cómo se estructuran los módulos dentro de ese estilo.

---

## Problema

¿Qué estilo arquitectónico y patrón de organización de código adoptar para un sistema de 11 módulos, 4 semanas de desarrollo, 5 usuarios concurrentes y despliegue exclusivo en localhost/red interna?

---

## Factores de Decisión

1. **Velocidad de desarrollo** — 4 semanas es un plazo corto; el overhead de infraestructura debe ser mínimo.
2. **Debugging y observabilidad** — un solo proceso es más fácil de depurar en entorno local.
3. **Cohesión por feature** — las stories y los módulos del PRD están organizados por capacidad funcional, no por capa técnica.
4. **Acoplamiento entre módulos** — debe ser bajo para poder construir y testear cada unidad de trabajo de forma independiente.
5. **Evolución futura** — la arquitectura debe ser refactorizable a microservicios si el sistema crece, sin reescritura total.
6. **Consistencia con el stack** — ASP.NET Core es nativo para monolitos modulares; no se gana nada dividiendo en múltiples procesos .NET para 5 usuarios.
7. **Testabilidad** — cada módulo debe ser testeable con cohesión interna alta.

---

## Opciones Consideradas

### Opción A — Monolito modular + Vertical Slice Architecture *(ADOPTADA)*

Un único proceso ASP.NET Core. Cada módulo M1–M11 vive en su propia carpeta (`Features/`) con su modelo, lógica de dominio y endpoint/componente de UI. Las dependencias entre módulos se canalizan exclusivamente a través de Application Services o contratos de dominio compartido (`Domain/`). No hay dependencias directas entre carpetas de Features.

### Opción B — Microservicios (1 servicio por módulo o por dominio)

Múltiples procesos .NET independientes comunicándose por HTTP/gRPC o mensajería.

**Rechazo**: overhead de orquestación (service discovery, distributed tracing, health checks entre servicios), complejidad de deployment, latencia de red interna innecesaria para 5 usuarios, y tiempo de implementación incompatible con 4 semanas. El beneficio (escalado independiente por servicio) no aplica en este contexto.

### Opción C — Serverless / Funciones (Azure Functions o similar)

Cada chequeo como función independiente, desencadenada por timer o evento.

**Rechazo**: requiere infraestructura cloud (fuera de alcance por C-02 y C-05), no hay nube disponible en MVP. El modelo de ejecución event-driven es atractivo conceptualmente, pero el despliegue en localhost es inviable para serverless real.

### Opción D — Clean Architecture / Onion Architecture

Capas estrictas: Domain → Application → Infrastructure → Presentation, con interfaces para todo, DTOs por capa, inversión de dependencias completa.

**Rechazo**: excelente arquitectura para sistemas de larga duración con equipos grandes, pero genera boilerplate desproporcionado para 4 semanas. Cada story requeriría tocar 4–5 archivos en capas distintas. La inversión en interfaces y mapeos no añade valor en el plazo actual. Puede adoptarse parcialmente en una refactorización post-MVP si el equipo crece.

### Opción E — N-Tier clásico (Controllers → Services → Repositories → DbContext)

Capas horizontales tradicionales.

**Rechazo**: rompe la cohesión por feature. Una story de "crear regla" tocaría `RulesController` (Presentation), `RulesService` (Business), `RuleRepository` (Data), `Rule` (Domain) — archivos en carpetas lejanas. Hace el desarrollo más lento y el testing más acoplado a la capa de infraestructura.

---

## Decisión Adoptada

**Opción A: Monolito modular con Vertical Slice Architecture / Feature Folders.**

### Estructura de solución resultante

```
MonitorPedidos.sln
├── MonitorPedidos.Domain/          # Entidades, interfaces de dominio, enumeraciones
│   ├── Entities/                   # Order, Incident, Alert, Rule, RuleHistory
│   ├── Enums/                      # Severity, IncidentStatus, RootCause
│   └── Interfaces/                 # ICheckExecutor, ICauseClassifier, IAlertExplainer
│
├── MonitorPedidos.Infrastructure/  # Implementaciones de infraestructura
│   ├── Persistence/                # DbContext, migraciones EF Core / Dapper
│   ├── Scheduling/                 # BackgroundService + PeriodicTimer (M1)
│   ├── ExternalChecks/             # HTTP clients para APIs externas (M3)
│   └── Logging/                    # Configuración Serilog
│
└── MonitorPedidos.Web/             # ASP.NET Core + Blazor Server
    ├── Features/                   # ← VERTICAL SLICE: un folder por módulo
    │   ├── M2_DbChecker/
    │   ├── M3_ApiChecker/
    │   ├── M4_HealthCheck/
    │   ├── M6_RulesManagement/
    │   ├── M7_CauseClassifier/
    │   ├── M8_Dashboard/
    │   ├── M9_IncidentManagement/
    │   ├── M10_AlertExplainer/
    │   └── M11_JobMonitor/
    ├── Hubs/                       # AlertsHub (SignalR)
    ├── Middleware/                 # SecurityHeadersMiddleware, GlobalExceptionHandler
    ├── Services/                   # Application Services (MonitoringService, etc.)
    └── Pages/                      # Blazor pages (Dashboard, Login, Rules)
```

### Reglas de implementación (contrato arquitectónico)

Estas reglas son verificables en code review y deben cumplirse en Construction:

| # | Regla | Justificación |
|---|-------|---------------|
| R1 | Las carpetas `Features/` **no** se importan entre sí directamente | Bajo acoplamiento; si M7 necesita datos de M2, lo hace a través de un servicio o interfaz de dominio |
| R2 | Las Blazor Pages **no** inyectan repositorios directamente — solo Application Services | Evita que la UI acople a la persistencia; testabilidad de servicios |
| R3 | Solo `NotificationService` puede inyectar `IHubContext<AlertsHub>` | Centraliza el canal de push; evita que múltiples componentes empujen al hub directamente |
| R4 | Los Application Services **no** se llaman entre sí en cadena — si necesitan coordinación, un servicio orquesta a los otros | Previene dependencias circulares entre servicios |
| R5 | `MonitorPedidos.Domain` **no** tiene dependencias hacia Infrastructure ni Web | Mantiene el dominio puro; facilita testing unitario sin base de datos |
| R6 | Todo acceso a datos externo (HTTP, BD) va envuelto en try/catch con fallo-closed | Cumplimiento RNF-15 + SECURITY-15 |

---

## Justificación de la Elección

**Por qué Vertical Slice dentro del monolito modular es la mejor opción para este contexto:**

1. **Alineación con el modelo mental del equipo**: las stories del PRD están organizadas por capacidad (Monitorear, Detectar, Investigar, Configurar). Vertical Slice refleja esa misma granularidad en el código — un desarrollador trabaja en una carpeta, no en cuatro capas.

2. **Velocidad en 4 semanas**: añadir una nueva feature implica crear/editar archivos en una sola carpeta. El N-Tier requiere tocar capas en múltiples ubicaciones; Clean Architecture requiere mapeos adicionales. Vertical Slice minimiza el "context switching" de archivos.

3. **Testeabilidad por unidad de trabajo**: cada una de las 7 unidades de trabajo (U1–U7) corresponde a un subconjunto de carpetas `Features/`. Los tests de una unidad no dependen de las otras.

4. **Camino a microservicios**: si post-MVP el sistema necesita escalar, cada carpeta `Features/` es un candidato natural a extraerse como servicio. El bajo acoplamiento entre Features facilita la extracción incremental sin reescritura.

5. **Compatible con el despliegue actual**: un único proceso, un único deployment, sin overhead de red entre módulos. Perfecto para localhost y red interna.

---

## Consecuencias

### Positivas

- Desarrollo más rápido por feature (story → carpeta → componentes cohesivos).
- Debugging simple: un solo proceso, un solo log, una sola instancia.
- Deployment trivial: `dotnet publish` + ejecutar el binario. Sin orquestación.
- Bajo overhead de infraestructura: no hay service mesh, no hay broker de mensajes, no hay distributed tracing.
- Cada módulo es testeable de forma aislada con mocks de interfaces de dominio.
- Fácil de demostrar al sponsor: una URL local, sin setup adicional.

### Negativas / Trade-offs

- **Escalado horizontal limitado**: un monolito escala verticalmente (más CPU/RAM en la misma máquina). Para 5 usuarios esto no es un problema, pero si el sistema crece a 50+ usuarios concurrentes el monolito puede ser un cuello de botella.
- **Riesgo de acoplamiento accidental**: sin enforcement automático (analizadores de arquitectura), los desarrolladores pueden crear dependencias directas entre Features violando las reglas R1–R6. Mitigación: code review estricto en Construction.
- **Despliegue todo-o-nada**: no se puede desplegar M6 (reglas) sin desplegar M8 (dashboard). En microservicios sería posible. Para MVP esto es aceptable.
- **Base de datos compartida**: todos los módulos acceden al mismo SQL Server MonitorPedidosDb (172.16.0.41). Si dos módulos necesitan schemas incompatibles en el futuro, el esquema compartido puede convertirse en deuda técnica. Mitigación: namespacing de tablas por módulo desde el inicio.

### Riesgos

| Riesgo | Probabilidad | Impacto | Mitigación |
|--------|:---:|:---:|------------|
| Acoplamiento accidental entre Features | Media | Alto | Code review + reglas R1–R6 en checklist de PR |
| Dificultad para extraer microservicio si el sistema crece | Baja | Medio | Mantener interfaces limpias en Domain; no exponer DbContext fuera de Infrastructure |
| Performance bajo carga inesperada (> 5 usuarios) | Baja | Medio | RNF-04 + load tests en Build & Test; mitigación: pool de conexiones configurable |

---

## Cumplimiento con Restricciones y Extensiones

| Restricción | Cumplimiento |
|-------------|-------------|
| C-01 (4 semanas) | Monolito modular es la opción más rápida de implementar; sin overhead de infraestructura distribuida. |
| C-02 (localhost / red interna) | Single deployment; `dotnet run` o `dotnet publish`. Sin requisitos de orquestación cloud. |
| C-03 (.NET 8 + ASP.NET Core) | Monolito modular es el patrón nativo del stack. |
| SECURITY-08 (control de acceso) | Authorization middleware se aplica una sola vez en el pipeline ASP.NET Core; cubre todas las rutas del monolito. |
| SECURITY-15 (exception handling) | Global exception handler registrado una sola vez en `Program.cs`; cubre todos los Features. |
| SECURITY-05 (input validation) | Validación centralizable con model binding de ASP.NET Core + FluentValidation en cada Feature. |

---

## Revisión de la Decisión

Esta decisión debe revisarse en los siguientes escenarios:

| Escenario disparador | Acción sugerida |
|---------------------|-----------------|
| Usuarios concurrentes > 20 | Evaluar extracción de módulos de alto tráfico (M8 Dashboard, M3 API Checker) como servicios independientes |
| Equipo de desarrollo > 3 personas en módulos distintos | Evaluar separación en proyectos .NET distintos (uno por Feature área) para reducir conflictos de merge |
| Despliegue on-prem con requisitos de alta disponibilidad | Evaluar separación de BackgroundService (M1 Scheduler) como worker process independiente |
| Requisito de escalar solo el módulo de notificaciones | Extraer `AlertsHub` + `NotificationService` a proceso dedicado con SignalR backplane (Redis) |

---

## Historial de la Decisión

| Versión | Fecha | Evento |
|---------|-------|--------|
| 1.0 | 2026-05-21 | Decisión adoptada durante Application Design (Q1 + Q2 = A en ambas). Aprobada por owner: "Aprobado y continuar por favor". |
