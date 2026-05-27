# Infrastructure Design Plan — U3 Detection & Classification

**Stage:** Construction → Infrastructure Design
**Unidad:** U3 — Detection & Classification
**Parte:** 1 (Planning) — este documento
**Fecha:** 2026-05-23
**Fuentes:**
- U3 `nfr-design/` (2 artefactos aprobados)
- U2 `infrastructure-design/` (contexto `AppDbContext`)

---

## Cómo usar este documento

1. Lee cada pregunta de la **§3 Cuestionario de Infrastructure Design**.
2. Cada pregunta tiene opciones A, B. Una está marcada como **(Recomendada)**.
3. Responde escribiendo la letra correspondiente después de `[Answer]:`.
4. Cuando termines, escribe **"listo"** para que proceda a generar los artefactos de Infrastructure Design.

---

## §1 Foco de U3 — Infrastructure Design

Con el diseño funcional y NFR aprobados, este stage define la **infraestructura de despliegue** de U3: configuración de intervalos de timer, registro explícito de checkers en DI, y la decisión sobre qué unidad es responsable de la migration `AddBrandSnapshots`.

### Artefactos que generaremos al cerrar este stage

| Artefacto | Contenido |
|-----------|-----------|
| `infrastructure-design.md` | Registro DI del scheduler, checkers, configuración `appsettings.json` |
| `deployment-architecture.md` | Diagrama Mermaid del stack de despliegue con `BackgroundService` en el host |

---

## §2 Contexto — Decisiones ya tomadas

Los siguientes aspectos ya están determinados desde los artefactos de diseño de U3:

| Aspecto | Decisión |
|---------|----------|
| Hosting | `BackgroundService` registrado en `Program.cs` |
| Registro scheduler | `AddHostedService<MonitoringSchedulerService>()` |
| Registro checkers | `AddSingleton<ICheckExecutor, ...>()` × 4 |
| Sin exposición pública | Restricción explícita del owner |

---

## §3 Cuestionario de Infrastructure Design (3 preguntas)

---

### Pregunta 1 — Intervalos de timer en configuración

¿Los intervalos de Timer1 (5 min) y Timer2 (10 min) son hardcoded o configurables?

**A) (Recomendada) Configurables vía `appsettings.json` sección `"Monitoring"`** — estructura: `{ "Timer1IntervalMinutes": 5, "Timer2IntervalMinutes": 10 }`. Valores por defecto en código si la sección no está presente. Permite ajustar intervalos sin recompilar ni redesplegar. Bindeados con `IOptions<MonitoringOptions>` en `Program.cs`.

**B) Hardcoded en el `BackgroundService`** — menos flexible para ajuste operativo. Cambiar un intervalo requiere recompilación y redespliegue. No recomendado para parámetros operativos que el administrador puede querer afinar.

[Answer]: A — intervalos en `appsettings.json` sección `Monitoring` *(2026-05-23)*

---

### Pregunta 2 — Orden de registro de checkers en DI

¿Cómo garantizamos un orden de ejecución predecible para `IEnumerable<ICheckExecutor>`?

**A) (Recomendada) Orden explícito en `Program.cs`** — registro en orden: `SalesforceApiChecker`, `MultivendeApiChecker`, `DbHealthChecker`, `BrandMonitorChecker`. El scheduler filtra por los checkers que corresponden a cada timer (Timer1: Salesforce + DB; Timer2: Multivende + Brand). El orden de `IEnumerable` sigue el orden de registro en el contenedor DI de .NET.

**B) Sin orden definido** — el orden de `IEnumerable<ICheckExecutor>` no está garantizado por especificación. En la práctica .NET respeta el orden de registro, pero depender de un comportamiento no especificado es un riesgo de mantenimiento.

[Answer]: A — orden explícito en `Program.cs` *(2026-05-23)*

---

### Pregunta 3 — Migration para brand_snapshots (tabla de U6)

`BrandMonitorChecker` (U3) escribe en `brand_snapshots`, pero esta tabla pertenece al modelo de U6. ¿Quién es responsable de la migration?

**A) (Recomendada) La migration `AddBrandSnapshots` pertenece a U6 infrastructure-design** — U3 no añade migrations propias. `BrandMonitorChecker` usa `IBrandSnapshotRepository` definido en U6 vía inyección de dependencia. No hay acoplamiento de schema entre U3 y U6 en la capa de Infrastructure.

**B) Migration en U3** — genera acoplamiento: U3 quedaría responsable de una entidad (`BrandSnapshot`) cuyo diseño completo es de U6. Si U6 cambia el schema, U3 debe actualizarse aunque no cambie su lógica.

[Answer]: A — migration `AddBrandSnapshots` pertenece a U6 *(2026-05-23)*

---

## §4 Después de responder

Cuando completes las 3 preguntas, escribe **"listo"** y procederé a:

1. Analizar respuestas para ambigüedades.
2. Si todo es claro, generaré los 2 artefactos en:
   ```
   aidlc-docs/construction/u3-detection-classification/infrastructure-design/
   ├── infrastructure-design.md
   └── deployment-architecture.md
   ```

---

## §5 Plan de ejecución (checklist)

- [x] **5.1** Analizar artefactos Functional Design y NFR Design de U3.
- [x] **5.2** Identificar decisiones ya tomadas vs ambigüedades pendientes.
- [x] **5.3** Crear este plan con 3 preguntas enfocadas.
- [x] **5.4** Recopilar respuestas del owner. *(3/3 = A, A, A — 2026-05-23)*
- [x] **5.5** Analizar respuestas — 0 ambigüedades detectadas.
- [x] **5.6** Generar `infrastructure-design.md`.
- [x] **5.7** Generar `deployment-architecture.md`.
- [x] **5.8** Actualizar `aidlc-state.md`.
- [x] **5.9** Registrar en `audit.md`.
- [x] **5.10** Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-23)*
