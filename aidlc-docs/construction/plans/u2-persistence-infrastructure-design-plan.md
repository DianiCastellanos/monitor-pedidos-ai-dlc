# Infrastructure Design Plan — U2 Persistence & Domain

**Stage:** Construction → Infrastructure Design
**Unidad:** U2 — Persistence & Domain
**Parte:** 1 (Planning) — este documento
**Fecha:** 2026-05-23
**Fuentes:**
- U2 `nfr-design/` (2 artefactos aprobados)
- U1 `infrastructure-design/` (contexto MonitorPedidosDb)

---

## Cómo usar este documento

1. Lee cada pregunta de la **§3 Cuestionario de Infrastructure Design**.
2. Cada pregunta tiene opciones A, B. Una está marcada como **(Recomendada)**.
3. Responde escribiendo la letra correspondiente después de `[Answer]:`.
4. Cuando termines, escribe **"listo"** para que proceda a generar los artefactos de Infrastructure Design.

---

## §1 Foco de U2 — Infrastructure Design

Con el diseño funcional y NFR aprobados, este stage define la **infraestructura de despliegue** de U2: migrations EF Core, health check endpoint y estrategia de inicialización del schema en el entorno de desarrollo/demo.

### Artefactos que generaremos al cerrar este stage

| Artefacto | Contenido |
|-----------|-----------|
| `infrastructure-design.md` | Descripción de migrations, health check, registro DI de U2 |
| `deployment-architecture.md` | Diagrama Mermaid del stack de despliegue (localhost, sin exposición pública) |

---

## §2 Contexto — Decisiones ya tomadas

Los siguientes aspectos ya están determinados desde U1 y desde los artefactos de diseño de U2:

| Aspecto | Decisión |
|---------|----------|
| Motor de BD | SQL Server MonitorPedidosDb (172.16.0.41) — misma decisión que U1 |
| Migrations | Manual con CLI (`dotnet ef database update`) |
| Sin exposición pública | Restricción explícita del owner |

---

## §3 Cuestionario de Infrastructure Design (3 preguntas)

---

### Pregunta 1 — Nombre de la migration inicial de U2

¿Cómo nombramos la migration EF Core que crea el schema de U2?

**A) (Recomendada) `AddIncidentSchema`** — crea la tabla `incidents` con todas las columnas de `AlertMessage` en línea (OwnsOne) y los índices compuestos. Se aplica después de `InitialCreate` de U1. El nombre es preciso: describe exactamente lo que hace la migration.

**B) `AddIncidentsAndAlerts`** — nombre alternativo menos preciso. `AlertMessage` no es una tabla separada (es OwnsOne inline), por lo que el sufijo "AndAlerts" puede generar confusión al leer el historial de migrations.

[Answer]: A — `AddIncidentSchema` *(2026-05-23)*

---

### Pregunta 2 — Health check endpoint

¿Cómo configuramos el endpoint de health check de la BD?

**A) (Recomendada) `/health` retorna JSON con `status` "Healthy"/"Unhealthy" + detalle por check** — registrado con `MapHealthChecks("/health")` en `Program.cs`. Respuesta JSON estándar de ASP.NET Core Health Checks. Sin autenticación (solo accesible desde `localhost`). Extensible para futuros checks (servicios externos, memoria).

**B) `/health` solo texto plano** — menos informativo. No sigue el estándar JSON de ASP.NET Core Health Checks; dificulta la integración con herramientas de monitoreo futuras.

[Answer]: A — `/health` con JSON y detalle por check *(2026-05-23)*

---

### Pregunta 3 — Inicialización del schema en demo

¿Cómo inicializamos el schema de BD antes de la primera ejecución?

**A) (Recomendada) `dotnet ef database update` documentado en README** — el operador/desarrollador corre el comando manualmente antes de la primera ejecución. Misma estrategia que U1 (`InitialCreate`). Explícito, auditable y sin riesgo de auto-migrate en producción.

**B) Auto-migrate al arrancar (`Database.MigrateAsync()` en startup)** — conveniente para demos pero riesgo de errores silenciosos en conflictos de schema. No recomendado: si la migration falla, la aplicación arranca en estado inconsistente.

[Answer]: A — manual CLI `dotnet ef database update` *(2026-05-23)*

---

## §4 Después de responder

Cuando completes las 3 preguntas, escribe **"listo"** y procederé a:

1. Analizar respuestas para ambigüedades.
2. Si todo es claro, generaré los 2 artefactos en:
   ```
   aidlc-docs/construction/u2-persistence-domain/infrastructure-design/
   ├── infrastructure-design.md
   └── deployment-architecture.md
   ```

---

## §5 Plan de ejecución (checklist)

- [x] **5.1** Analizar artefactos Functional Design y NFR Design de U2.
- [x] **5.2** Identificar decisiones ya tomadas vs ambigüedades pendientes.
- [x] **5.3** Crear este plan con 3 preguntas enfocadas.
- [x] **5.4** Recopilar respuestas del owner. *(3/3 = A, A, A — 2026-05-23)*
- [x] **5.5** Analizar respuestas — 0 ambigüedades detectadas.
- [x] **5.6** Generar `infrastructure-design.md`.
- [x] **5.7** Generar `deployment-architecture.md`.
- [x] **5.8** Actualizar `aidlc-state.md`.
- [x] **5.9** Registrar en `audit.md`.
- [x] **5.10** Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-23)*
