# Infrastructure Design Plan — U1 Foundation & Cross-Cutting

**Stage:** Construction → Infrastructure Design
**Unidad:** U1 — Foundation & Cross-Cutting
**Fecha:** 2026-05-23
**Fuentes:**
- `aidlc-docs/construction/u1-foundation-cross-cutting/nfr-requirements/tech-stack-decisions.md`
- `aidlc-docs/construction/u1-foundation-cross-cutting/nfr-design/logical-components.md`
- `aidlc-docs/aidlc-state.md` (contexto de despliegue: localhost / red interna, sin nube)

---

## Contexto de despliegue (ya decidido — sin preguntas)

| Aspecto | Decisión | Fuente |
|---------|----------|--------|
| Entorno | Localhost / red interna del equipo | aidlc-state.md, PRD v2.4 |
| Exposición pública | **Ninguna** — sin ngrok, sin internet | Restricción explícita del owner |
| Motor de BD | SQL Server LocalDB (desarrollo/demo) | ADR-001, Inception |
| Connection string | `Server=(localdb)\mssqllocaldb;Database=MonitorPedidosDb;...` | tech-stack-decisions.md §5 |
| Logging | Serilog → `logs/` relativo al ejecutable, rotación diaria, 90 días | NFR-U1-03 |
| Hosting | Kestrel (`dotnet run`) — sin IIS, sin Docker para MVP | ADR-001 |
| Cloud / infra externa | **Ninguna** en U1 (sin AWS, Azure, GCP) | MVP scope |

---

## Decisiones pendientes (requieren input del owner)

Hay **3 puntos de infraestructura** donde el comportamiento del sistema en entorno real depende de una elección concreta:

---

### Pregunta 1 — Persistencia del Data Protection Key Ring

**Contexto:** ASP.NET Core usa el Key Ring de Data Protection para **firmar y descifrar las cookies de sesión**. Si las claves viven solo en memoria (comportamiento por defecto), se pierden al reiniciar la aplicación y **todas las sesiones activas quedan inválidas** — los usuarios deben volver a seleccionar su identidad.

Para una demo o uso en red interna esto puede ser aceptable o molesto, dependiendo del uso.

**Opciones:**

**A) (Recomendada) Persistencia en sistema de archivos** — `PersistKeysToFileSystem(new DirectoryInfo(@"keys\"))`. Las claves sobreviven reinicios. Las sesiones se mantienen válidas aunque el servidor se reinicie.

**B) In-memory (comportamiento por defecto)** — sin configuración adicional. Las claves se regeneran en cada inicio. Cada reinicio invalida todas las cookies activas. Aceptable si el equipo tolera re-selección de identidad tras reinicios.

[Answer]: **A** — Persistencia en file system (`keys/`) *(2026-05-23)*

---

### Pregunta 2 — HTTPS para desarrollo y demo

**Contexto:** RNF-07 requiere HTTPS cuando se corre en red interna. Las cookies de sesión tienen `Secure=true` cuando la conexión es HTTPS, lo que refuerza la seguridad. La pregunta es cómo gestionar el certificado.

**Opciones:**

**A) (Recomendada) dotnet dev-certs** — `dotnet dev-certs https --trust`. Certificado autofirmado de .NET, válido para `https://localhost`. Ya incluido en el SDK, sin costo, sin configuración extra. Cada máquina que ejecute el sistema corre este comando una vez.

**B) HTTP plano en desarrollo, HTTPS opcional en demo** — el servidor corre en `http://localhost`. Sin HTTPS en desarrollo. Las cookies no usarán `Secure=true` en desarrollo (pero el sistema detecta `request.IsHttps` y aplica HSTS solo cuando aplica). Más simple, sin gestión de certificados.

[Answer]: **A** — dotnet dev-certs (`https://localhost`) *(2026-05-23)*

---

### Pregunta 3 — Estrategia de aplicación de migrations EF Core

**Contexto:** La migration `InitialCreate` de U1 crea el schema base de la BD. Necesitamos definir cómo se aplica: al desplegar manualmente o automáticamente al arrancar la app.

**Opciones:**

**A) (Recomendada) Manual con CLI** — el desarrollador/operador corre `dotnet ef database update` antes de la primera ejecución (y después de cada nueva migration). Control explícito, sin sorpresas en startup. Estándar en proyectos .NET.

**B) Auto-migrate al arrancar** — `context.Database.Migrate()` en `Program.cs` durante startup. La app crea/actualiza la BD automáticamente en cada inicio. Más conveniente para demos rápidas, pero puede causar errores silenciosos si hay conflictos de migration.

[Answer]: **A** — Manual CLI (`dotnet ef database update`) *(2026-05-23)*

---

## Plan de ejecución (checklist)

- [x] **1** Analizar artefactos Functional Design y NFR Design de U1.
- [x] **2** Identificar decisiones ya tomadas vs ambigüedades pendientes.
- [x] **3** Crear este plan con 3 preguntas enfocadas.
- [x] **4** Recopilar respuestas del owner. *(3/3 = A, A, A — 2026-05-23)*
- [x] **5** Analizar respuestas — 0 ambigüedades detectadas.
- [x] **6** Generar `infrastructure-design.md`.
- [x] **7** Generar `deployment-architecture.md`.
- [x] **8** Actualizar `aidlc-state.md`.
- [x] **9** Registrar en `audit.md`.
- [x] **10** Presentar mensaje de cierre para aprobación explícita.
