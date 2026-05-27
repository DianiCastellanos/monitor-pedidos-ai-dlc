# Application Design Plan — MonitorPedidos AI

**Stage:** Inception → Application Design
**Parte:** 1 (Planning) — este documento
**Profundidad:** Standard
**Fuentes:** [`prd.md`](../../../prd.md) v2.3, [`requirements.md`](../requirements/requirements.md) v1.1, [`stories.md`](../user-stories/stories.md), [`personas.md`](../user-stories/personas.md), [`execution-plan.md`](./execution-plan.md)
**Rol asumido:** Software Architect

---

## Cómo usar este documento

1. Lee cada pregunta de la **§3 Cuestionario arquitectónico**.
2. Cada pregunta tiene opciones marcadas A, B, C, etc. Una opción está marcada como **(Recomendada)** con justificación.
3. Responde escribiendo la letra correspondiente después de `[Answer]:`.
4. Si ninguna opción aplica, escoge la última (Otro) y describe tu respuesta.
5. Cuando termines, escribe **"listo"** o **"completado"** para que proceda al análisis de ambigüedades y a la generación de los 4 artefactos de Application Design.

---

## §1 Foco del stage

Application Design define **componentes de alto nivel** (responsabilidades, interfaces, métodos públicos) y **patrones de orquestación** — **no** define lógica de negocio detallada (eso queda para Functional Design en Construction).

### Lo que generaremos al cerrar este stage

| Artefacto | Propósito |
|-----------|-----------|
| `components.md` | Definición de cada componente (M1–M11 + servicios de soporte) con responsabilidades e interfaces. |
| `component-methods.md` | Firmas (signatures) de métodos públicos por componente — sin reglas de negocio profundas. |
| `services.md` | Service layer: orquestación entre componentes, qué servicio coordina qué flujo. |
| `component-dependency.md` | Matriz de dependencias + diagrama de flujo de datos + patrones de comunicación. |

### Lo que NO generaremos aquí

- ❌ Pseudocódigo de cada método.
- ❌ Reglas de negocio detalladas (eso es Functional Design en Construction).
- ❌ Decisión final de librerías concretas (eso es NFR Requirements / NFR Design).
- ❌ Diseño de BD a nivel de columnas/índices (eso es Functional Design por unidad).

---

## §2 Plan de ejecución (checklist)

> Esta sección la marcaré [x] durante la generación (Parte 2) — solo está para visibilidad.

- [x] **2.1** Leer respuestas validadas de §3.
- [x] **2.2** Analizar respuestas para ambigüedades y crear follow-up si aplica. *(0 ambigüedades, 0 contradicciones)*
- [x] **2.3** Esperar aprobación explícita del plan + respuestas finales. *(Application Design no tiene gate de plan; se generan artefactos directos y se aprueban al final)*
- [x] **2.4** Generar `aidlc-docs/inception/application-design/components.md`.
- [x] **2.5** Generar `aidlc-docs/inception/application-design/component-methods.md`.
- [x] **2.6** Generar `aidlc-docs/inception/application-design/services.md`.
- [x] **2.7** Generar `aidlc-docs/inception/application-design/component-dependency.md` (incluye diagrama Mermaid).
- [x] **2.8** Validar trazabilidad cruzada: cada componente → ≥1 RF, ≥1 story. *(Cobertura verificada en components.md §3 y component-dependency.md §8)*
- [x] **2.9** Actualizar `aidlc-state.md` (Application Design ✅).
- [x] **2.10** Registrar en `audit.md`.
- [x] **2.11** Presentar mensaje de cierre con criterios de revisión. *(Aprobado por el owner: "Aprobado y continuar por favor")*

---

## §3 Cuestionario arquitectónico (6 preguntas)

### Pregunta 1 — Estilo arquitectónico del MVP

Dado el contexto (MVP de 4 semanas, sin exposición a internet, 11 módulos lógicos, single deployment en localhost o red interna), ¿qué estilo arquitectónico adoptamos?

A) **(Recomendada)** **Monolito modular** — un único proceso ASP.NET Core que aloja todos los módulos M1–M11 como componentes internos bien separados (proyectos/folders dentro de la misma solución). Despliegue único, debugging fácil, sin overhead de red entre módulos, cumple "single deployment" del PRD §9. Ideal para 4 semanas. Refactorizable a microservicios en futuro si el sistema crece.
B) **Microservicios** (un servicio por módulo o por dominio). Más escalable pero overhead de orquestación, observabilidad y deployment desproporcionado para localhost + 4 semanas + 5 usuarios concurrentes.
C) **Serverless / funciones** (cada chequeo como función). Atractivo para event-driven, pero requiere infraestructura cloud que está fuera de scope (C-02 sin cloud).
D) Otro (describir después de `[Answer]:`)

[Answer]: A

---

### Pregunta 2 — Patrón de capas / organización del código

Dentro del monolito modular, ¿qué patrón de capas usamos?

A) **(Recomendada)** **Feature Folders / Vertical Slice Architecture** — cada módulo M1–M11 vive en su propia carpeta con su modelo, lógica y endpoints (cuando aplica), con dependencias mínimas hacia un núcleo compartido (`Domain`/`Infrastructure`). Simple, rápido de implementar, fácil de testear por feature, alineado con la granularidad de stories. Recomendado para equipos pequeños y MVPs.
B) **Clean Architecture (Onion / Hexagonal)** — capas Domain / Application / Infrastructure / Presentation estrictas. Excelente arquitectura pero overhead de boilerplate (interfaces para todo, DTOs por capa) excesivo para 4 semanas. Más adecuado post-MVP si el sistema crece.
C) **N-Tier clásico** — Controllers → Services → Repositories → DbContext, capas horizontales. Familiar pero rompe la cohesión por feature: una story toca varias capas en archivos lejanos.
D) Otro (describir después de `[Answer]:`)

[Answer]: A

---

### Pregunta 3 — Framework de UI para el dashboard (M8)

Dado que el dashboard requiere vista real-time con auto-refresh, modo NOC y notificaciones push del navegador, ¿qué framework UI usamos?

A) **(Recomendada)** **Blazor Server** — UI en C# server-side con conexión SignalR persistente al servidor. Resuelve **gratis** los requisitos de real-time (cambios en servidor → UI se actualiza automáticamente vía el circuito SignalR), elimina la necesidad de un cliente JS separado, mantiene todo el stack en .NET. Ideal para usuarios concurrentes pequeños (5 — RF-30), localhost y red interna.
B) **Razor Pages + JavaScript ligero** — UI tradicional server-rendered con JS para auto-refresh (polling o fetch + JS para notificaciones). Más conocido, menos curva, pero hay que escribir el JS de polling/notification a mano y mantenerlo.
C) **ASP.NET Core MVC + JS** — controllers + views, JS para interactividad. Funciona pero la separación MVC añade más archivos por feature; menos productivo que Razor Pages para CRUD simples.
D) **Blazor WebAssembly** — UI client-side compilada a WASM, consume API REST. Más complejidad de hospedaje y más bytes en el bundle. No agrega valor para usuarios internos.
E) Otro (describir después de `[Answer]:`)

[Answer]: A

---

### Pregunta 4 — Mecanismo de scheduling (M1 Scheduler)

M1 orquesta chequeos cada 5/10 min. ¿Cómo lo implementamos?

A) **(Recomendada)** **`IHostedService` / `BackgroundService` nativo de .NET con `PeriodicTimer`** — clase derivada de `BackgroundService` que registra Timers por módulo (M2 cada 5min, M3 cada 10min, M4 cada 5min, M11 cada 5min). Cero dependencias adicionales, integrado al lifecycle de ASP.NET Core, simple. Soporta cancelación y graceful shutdown.
B) **Quartz.NET** — librería madura con cron expressions, scheduler clusterizado, persistencia opcional. Excede los requisitos del MVP (no necesitamos cron expressions complejas ni cluster) y agrega dependencia.
C) **Hangfire** — scheduler + cola de jobs persistente con dashboard propio. Pensado para jobs ad-hoc retryable; nuestro caso es polling determinístico, no cola de jobs.
D) Otro (describir después de `[Answer]:`)

[Answer]: A

---

### Pregunta 5 — Mecanismo de notificación al navegador (RF-24 / RF-25)

¿Cómo entregamos las notificaciones push al dashboard cuando aparece una alerta?

A) **(Recomendada)** **SignalR Hub (parte del stack ASP.NET Core)** — el servidor empuja alertas a clientes conectados vía WebSocket (o long polling como fallback). Si Blazor Server es la elección de la Pregunta 3, **el circuito SignalR ya está ahí gratis** y solo añadimos un hub específico de alertas. El fallback a long polling y eventualmente a polling regular cubre RF-25 (Notification API bloqueada).
B) **Server-Sent Events (SSE)** — endpoint que stream eventos unidireccional. Simple, pero requiere implementación manual de reconnect y no aprovecha el ecosistema de SignalR ya integrado.
C) **Polling HTTP** desde el cliente cada N segundos. Sencillo pero ineficiente y aumenta latencia de detección. Útil como último fallback, no como mecanismo principal.
D) **Web Push API estándar** (con service worker). Pensada para notificaciones offline o con tab cerrado; complejo de configurar (VAPID keys, push service externo) y excede lo necesario para un dashboard local activo.
E) Otro (describir después de `[Answer]:`)

[Answer]: A

---

### Pregunta 6 — Granularidad del service layer

¿Cómo organizamos los services que orquestan los componentes?

A) **(Recomendada)** **Un application service por capability funcional** — `MonitoringService` (coordina M1+M2+M3+M4+M11 → M7 → M9 → M10), `RuleManagementService` (M6 CRUD + historial), `IncidentService` (cierre auto/manual + historial), `AuthService` (Identity + roles), `NotificationService` (push al hub SignalR). Refleja directamente las sub-secciones de stories (Monitorear / Detectar / Investigar / Configurar) y minimiza el number de servicios sin perder cohesión.
B) **Un service por módulo M1–M11** — `M1SchedulerService`, `M2DbCheckService`, etc. Refleja 1:1 los módulos pero genera demasiados servicios; la orquestación queda dispersa.
C) **CQRS-style: Commands + Queries** sin services intermedios — handlers directos (MediatR). Patrón potente pero overhead de configuración y curva de aprendizaje desproporcionada para MVP de 4 semanas.
D) Otro (describir después de `[Answer]:`)

[Answer]: A

---

## §4 Después de responder

Cuando termines de contestar las 6 preguntas, escribe **"listo"** o **"completado"** y yo:

1. Analizaré tus respuestas en busca de ambigüedades o contradicciones (Step 8).
2. Si encuentro alguna, crearé un archivo de preguntas de seguimiento.
3. Si todas son claras, te pediré aprobación explícita.
4. Tras tu aprobación, generaré los 4 artefactos:
   - `application-design/components.md`
   - `application-design/component-methods.md`
   - `application-design/services.md`
   - `application-design/component-dependency.md` (incluye Mermaid del flujo de datos)
