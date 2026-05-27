# C4 Model — MonitorPedidos AI

**Fecha:** 2026-05-22
**Versión:** 1.1 — diagramas reescritos en sintaxis `flowchart` estándar (compatible con Mermaid 8+).
**Stage:** Inception → Application Design (artefacto complementario)
**Alcance:** Niveles 1 (System Context) y 2 (Container) del C4 Model.
**Fuentes:** [`prd.md`](../../../prd.md) v2.3, [`requirements.md`](../requirements/requirements.md) v1.1, [`components.md`](./components.md), [`component-dependency.md`](./component-dependency.md).

---

## Sobre la sintaxis Mermaid usada

Estos diagramas usan **`flowchart TB` estándar** con `classDef` para reproducir visualmente las convenciones de C4 (colores y formas estándar de Simon Brown). Esto garantiza renderizado en **cualquier versión de Mermaid 8+**, incluyendo:

- VS Code con extensión *Markdown Preview Mermaid Support*.
- GitHub / GitLab render nativo.
- Cualquier viewer Markdown moderno (Obsidian, Notion-style, etc.).
- [mermaid.live](https://mermaid.live).

**Convenciones de color C4 aplicadas:**

| Color | Tipo | Hex |
|-------|------|-----|
| 🔵 Azul oscuro | Person (usuario / stakeholder) | `#08427B` |
| 🔷 Azul medio | System (en alcance) | `#1168BD` |
| 🟦 Azul claro | Container | `#438DD5` |
| ⚪ Gris | External System | `#999999` |

> **Alternativa nativa C4:** Mermaid 10+ soporta `C4Context`/`C4Container` directamente. Si tu renderer ya está actualizado y prefieres la sintaxis nativa, la versión 1.0 de este documento (en git history o por solicitud) la incluye.

---

## ¿Qué es C4 Model?

| Nivel | Nombre | Audiencia | Foco |
|-------|--------|-----------|------|
| **1** | **System Context** | Cualquier stakeholder | El sistema como caja negra en su entorno: quién lo usa, con qué se integra. |
| **2** | **Container** | Equipo técnico y arquitectos | Procesos / servicios / BD que componen el sistema, y cómo se comunican. |
| 3 | Component | Desarrolladores | Componentes internos de cada container. *(Ver [`components.md`](./components.md).)* |
| 4 | Code | Desarrolladores | Firmas / código. *(Ver [`component-methods.md`](./component-methods.md).)* |

Aquí cubrimos los **niveles 1 y 2**.

---

## Nivel 1 — System Context Diagram

**Propósito:** MonitorPedidos AI como caja negra en su entorno.

```mermaid
flowchart TB
    classDef person fill:#08427B,stroke:#052E56,color:#ffffff,stroke-width:2px
    classDef system fill:#1168BD,stroke:#0B4884,color:#ffffff,stroke-width:2px
    classDef ext fill:#999999,stroke:#6B6B6B,color:#ffffff,stroke-width:2px

    op("<b>Operador</b><br/><i>[Persona]</i><br/>Analista operativo.<br/>Monitorea diariamente,<br/>cierra incidentes."):::person
    tec("<b>Técnico</b><br/><i>[Persona]</i><br/>Responsable técnico.<br/>Edita reglas, investiga,<br/>exporta logs."):::person
    spo("<b>Sponsor</b><br/><i>[Persona]</i><br/>Alex Cárdenas.<br/>Refuerza adopción.<br/>Demos semanales."):::person
    its("<b>IT / Seguridad</b><br/><i>[Persona]</i><br/>Define restricciones<br/>SECURITY (13 reglas<br/>bloqueantes)."):::person

    mon["<b>MonitorPedidos AI</b><br/><i>[Sistema]</i><br/>Detector proactivo de<br/>fallas en descarga de<br/>pedidos. Alertas con 6<br/>campos en español.<br/>Despliegue interno ·<br/>sin internet."]:::system

    sf["<b>Salesforce API</b><br/><i>[Sistema externo]</i><br/>Origen de pedidos.<br/>SOLO LECTURA (P2)."]:::ext
    mv["<b>Multivende API</b><br/><i>[Sistema externo]</i><br/>Origen de pedidos.<br/>SOLO LECTURA (P2)."]:::ext
    jobs["<b>Orquestador de Jobs</b><br/><i>[Sistema externo]</i><br/>Ejecuta jobs de descarga.<br/>Solo se consulta estado<br/>(no se controla)."]:::ext
    bdp[("<b>BD Interna de Pedidos</b><br/><i>[BD externa]</i><br/>SQL Server. En MVP:<br/>tabla simulated_orders.")]:::ext

    op -->|"Monitorea / cierra incidentes<br/>HTTPS · red interna"| mon
    tec -->|"Edita reglas / exporta logs<br/>HTTPS · red interna"| mon
    spo -->|"Demos semanales<br/>HTTPS · red interna"| mon
    its -.->|"Define SECURITY<br/>compliance (no acceso operativo)"| mon

    mon -->|"Polling 10 min · retry 2x<br/>HTTPS REST"| sf
    mon -->|"Polling 10 min · retry 2x<br/>HTTPS REST"| mv
    mon -->|"Verifica estado 5 min<br/>DB / API · read-only"| jobs
    mon -->|"Chequeo 5 min + health-check<br/>TDS · cifrado"| bdp
```

### Lectura

- **4 personas** se relacionan con el sistema.
  - **Operador** y **Técnico** son los únicos con interacción operativa diaria.
  - **Sponsor** accede en demos semanales internas.
  - **IT/Seguridad** **no accede al sistema**; solo impone las 13 restricciones SECURITY bloqueantes (relación punteada = compliance, no operativa).
- **4 sistemas externos**: Salesforce y Multivende (read-only por P2), Orquestador de Jobs (solo consulta), BD Interna de Pedidos (en MVP simulada).
- **Restricción crítica del despliegue:** sin exposición a internet. Todo acceso por **localhost** o **red interna del equipo**.

---

## Nivel 2 — Container Diagram

**Propósito:** abrir la caja negra del Nivel 1 y mostrar los containers (procesos, servicios, BD).

```mermaid
flowchart TB
    classDef person fill:#08427B,stroke:#052E56,color:#ffffff,stroke-width:2px
    classDef container fill:#438DD5,stroke:#2E6295,color:#ffffff,stroke-width:2px
    classDef ext fill:#999999,stroke:#6B6B6B,color:#ffffff,stroke-width:2px

    op("<b>Operador</b><br/><i>[Persona]</i>"):::person
    tec("<b>Técnico</b><br/><i>[Persona]</i>"):::person

    subgraph boundary["<b>MonitorPedidos AI</b> · despliegue interno (localhost o red interna del equipo)"]
        direction TB

        browser["<b>Navegador del usuario</b><br/><i>[Container: Chrome / Edge]</i><br/>Cliente Blazor Server.<br/>Circuito SignalR persistente.<br/>Modo NOC pantalla completa.<br/>Fallback sonido + parpadeo."]:::container

        web["<b>MonitorPedidos.Web</b><br/><i>[Container: .NET 8 · ASP.NET Core · Blazor Server]</i><br/>Monolito modular. Hospeda:<br/>· Páginas Blazor (Realtime, Histórico, Semanal, Discrepancias, Reglas, Logs)<br/>· AlertsHub SignalR<br/>· 5 Application Services (Monitoring, RuleMgmt, Incident, Auth, Notification)<br/>· MonitoringScheduler (BackgroundService + PeriodicTimer 5/10 min)<br/>· OrdersSimulator (BackgroundService)<br/>· ASP.NET Core Identity<br/>· Security Headers Middleware + Global Exception Handler"]:::container

        db[("<b>BD MonitorPedidos</b><br/><i>[Container BD: SQL Server LocalDB / Express]</i><br/>incidents · rules · rule_history<br/>simulated_orders · AspNetUsers/Roles<br/>Cifrado at-rest · Retención 90 días")]:::container

        logs[("<b>Logs estructurados</b><br/><i>[Container: Serilog · archivo rotado local]</i><br/>timestamp + request_id + level + mensaje<br/>SIN PII · SIN tokens · Retención 90 días")]:::container
    end

    sf["<b>Salesforce API</b><br/><i>[Externo · read-only]</i>"]:::ext
    mv["<b>Multivende API</b><br/><i>[Externo · read-only]</i>"]:::ext
    jobs["<b>Orquestador de Jobs</b><br/><i>[Externo · read-only]</i>"]:::ext

    op -->|"Usa<br/>teclado / mouse"| browser
    tec -->|"Usa<br/>teclado / mouse"| browser

    browser -->|"Render UI · interacciones<br/>cookies HttpOnly+SameSite<br/>(Secure si HTTPS interno)<br/>HTTPS · circuito SignalR"| web
    web -->|"Push: AlertReceived /<br/>IncidentClosed /<br/>SystemStatusUpdated<br/>SignalR (WebSocket)"| browser

    web -->|"Queries parametrizadas<br/>· transacciones · cifrado<br/>TDS"| db
    web -->|"Emite logs estructurados<br/>Serilog sink"| logs

    web -->|"Polling 10 min · retry 2x<br/>(5xx/timeout) · 401 sin renov.<br/>HTTPS REST"| sf
    web -->|"Polling 10 min · retry 2x<br/>HTTPS REST"| mv
    web -->|"Verifica estado 5 min<br/>read-only"| jobs
```

### Lectura

**4 containers dentro del boundary del sistema:**

| Container | Tecnología | Función |
|-----------|------------|---------|
| **Navegador del usuario** | Chrome / Edge | Cliente Blazor Server con circuito SignalR persistente. |
| **MonitorPedidos.Web** | .NET 8 · ASP.NET Core · Blazor Server | Único proceso de app. UI + hubs + services + scheduler + simulator + selección de identidad (cookie auth) + middlewares. |
| **BD MonitorPedidos** | SQL Server LocalDB / Express | Persistencia: incidentes, reglas + historial, datos simulados. Sin tablas de usuarios (identidad sin credenciales). Cifrado at-rest. |
| **Logs estructurados** | Serilog · archivo local rotado | Audit trail estructurado sin PII (RNF-08 + SECURITY-03). |

**Comunicaciones clave:**

- **Navegador ↔ Web:** circuito SignalR sobre WebSocket (Blazor Server). Cookies con `HttpOnly` + `SameSite=Strict` + `Secure` (cuando HTTPS interno). Doble flecha = real-time push servidor → cliente.
- **Web → BD:** TDS cifrado, consultas **parametrizadas** (SECURITY-05).
- **Web → APIs externas:** HTTPS REST con **política Polly** (máx 2 reintentos, solo 5xx/timeout). Ante 401, no renueva automáticamente.
- **Web → Logs:** Serilog sink con filtros explícitos de PII / tokens (no hay contraseñas en el modelo simplificado de identidad).

**Despliegue:** los 4 containers conviven en **una sola máquina** (localhost del owner o equipo de la red interna). Sin separación de hosts, sin exposición pública.

---

## Trazabilidad con artefactos existentes

| Elemento C4 | Documento Inception | Notas |
|-------------|---------------------|-------|
| Personas (L1) | [`personas.md`](../user-stories/personas.md) | 4 perfiles formalizados |
| Sistemas externos (L1) | [`requirements.md`](../requirements/requirements.md) C-04 (read-only) | Restricción P2 |
| Container Web (L2) | [`components.md`](./components.md) | 11 módulos M1–M11 + 5 services + hub |
| Container BD (L2) | [`components.md`](./components.md) §2.2 + [`component-methods.md`](./component-methods.md) | Entidades + repositorios |
| Comunicaciones (L2) | [`component-dependency.md`](./component-dependency.md) §3 | Patrones: in-process + SignalR + HTTP |
| **Nivel 3 (Component)** | [`components.md`](./components.md) + [`component-dependency.md`](./component-dependency.md) | Ya cubierto |
| **Nivel 4 (Code)** | [`component-methods.md`](./component-methods.md) | Firmas; implementación = Construction |

---

## Cómo visualizar

- **VS Code**: extensión *Markdown Preview Mermaid Support* (o similar) + abrir este archivo en preview.
- **mermaid.live**: pega cada bloque de código entre triples backticks.
- **GitHub / GitLab**: render nativo dentro de archivos `.md`.

Estos diagramas funcionan con **cualquier Mermaid 8+** — no requieren actualización del renderer.
