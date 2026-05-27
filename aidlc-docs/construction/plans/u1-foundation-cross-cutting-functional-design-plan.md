# Functional Design Plan — U1 Foundation & Cross-Cutting

**Stage:** Construction → Functional Design
**Unidad:** U1 — Foundation & Cross-Cutting
**Parte:** 1 (Planning) — este documento
**Fecha:** 2026-05-23
**Fuentes:**
- [`unit-of-work.md`](../../inception/application-design/unit-of-work.md) §U1
- [`unit-of-work-story-map.md`](../../inception/application-design/unit-of-work-story-map.md)
- [`components.md`](../../inception/application-design/components.md)
- [`component-methods.md`](../../inception/application-design/component-methods.md)
- [`requirements.md`](../../inception/requirements/requirements.md) v1.1 §RF-26..RF-29, RNF-06..RNF-15

---

## Cómo usar este documento

1. Lee cada pregunta de la **§3 Cuestionario de diseño funcional**.
2. Cada pregunta tiene opciones A, B, C. Una está marcada como **(Recomendada)** con justificación.
3. Responde escribiendo la letra correspondiente después de `[Answer]:`.
4. Si ninguna aplica, elige la última opción (Otro) y describe tu respuesta.
5. Cuando termines, escribe **"listo"** para que proceda a generar los 4 artefactos de Functional Design.

---

## §1 Foco de U1

U1 es la **unidad fundacional**: establece el esqueleto del sistema antes de que cualquier otra unidad pueda construirse. Su alcance de dominio es principalmente **identidad, seguridad y cross-cutting** — no tiene entidades de negocio complejas como Incident o Rule (esas son U2 y U5). Lo que sí define son:

- Las **entidades de Identity** (`ApplicationUser`, `ApplicationRole`)
- Los **enums de dominio compartidos** usados por todas las unidades
- Los **value objects cross-cutting** (`AlertMessage`, `CheckResult`)
- Las **entidades de auditoría de seguridad** (si se decide tenerlas)

### Artefactos que generaremos al cerrar este stage

| Artefacto | Contenido |
|-----------|-----------|
| `domain-entities.md` | Bounded context, entidades, value objects, aggregates de U1 |
| `business-logic-model.md` | Flujo de autenticación, lockout, cookie lifecycle |
| `business-rules.md` | Reglas de validación: password policy, lockout, headers, logging sin PII |
| `frontend-components.md` | Páginas de Login/Logout (Blazor + Identity scaffold) |

---

## §2 Contexto de diseño — lo que ya sabemos

Antes de las preguntas, este es el estado confirmado desde Inception:

| Decisión ya tomada | Fuente |
|-------------------|--------|
| 2 roles exactamente: `Operador` y `Técnico` | RF-27 |
| 2 cuentas pre-creadas, contraseñas vía User Secrets | RF-28, RNF-14 |
| Cookies: `HttpOnly + SameSite=Strict`, `Secure` cuando HTTPS | RF-29, RNF-14 |
| Lockout: 5 intentos / 15 min | RNF-14 |
| Password hashing: PBKDF2 HMAC-SHA256 ≥ 10.000 iteraciones (Identity default) | RNF-14 |
| Logging: sin contraseñas, tokens ni PII | RNF-08 |
| 4 headers HTTP de seguridad obligatorios | RNF-09 |
| Exception handling fail-closed (errores genéricos al cliente) | RNF-15 |
| Stack: ASP.NET Core Identity + SQL Server LocalDB | C-03 |

---

## §3 Cuestionario de diseño funcional (6 preguntas)

---

### Pregunta 1 — Extensión de ApplicationUser

`ASP.NET Core Identity` provee `IdentityUser` con: `Id`, `UserName`, `Email`, `PasswordHash`, `LockoutEnd`, `AccessFailedCount`, etc. ¿Extendemos `ApplicationUser` con campos adicionales?

**A) (Recomendada) Solo `DisplayName` (string, requerido)** — nombre visible en el dashboard ("Hola, Diana"). Sin más campos. `UserName` se usa para login; `DisplayName` para UX. Mínimo y suficiente para el MVP: los 5 usuarios internos no requieren perfil rico.

**B) `DisplayName` + `Area` (string, opcional)** — identifica a qué área pertenece el usuario (Operativo, Técnico, Dirección). Útil para el historial de reglas (¿quién de qué área cambió qué?), pero la información ya está implícita en el rol. Añade un campo de mantenimiento.

**C) Solo los campos de `IdentityUser` por defecto** — sin extensión. `UserName` hace las veces de nombre visible. Mínimo absoluto, pero la UI queda con usernames técnicos como display name.

[Answer]:

---

### Pregunta 2 — Tabla de auditoría de eventos de seguridad

Los eventos `login_exitoso`, `login_fallido`, `lockout`, `logout` son requeridos por SECURITY-03 y SECURITY-14. ¿Cómo los registramos?

**A) (Recomendada) Solo en Serilog (logs estructurados en archivo)** — ASP.NET Core Identity ya emite estos eventos como logs. Serilog los captura en el archivo rotado con `timestamp + request_id + evento`. No requiere tabla adicional ni entidad. Cumple SECURITY-03 y SECURITY-14. Para el MVP con 5 usuarios es suficiente.

**B) Tabla `security_audit_log` en BD + Serilog** — persistencia de eventos de seguridad en BD con columnas `(Id, UserId, EventType, Timestamp, IpAddress, Success)`. Mayor trazabilidad y consultable desde el dashboard Técnico. Añade una entidad de dominio (`SecurityAuditEntry`) y migración. Más robusto para auditorías post-MVP.

**C) Tabla `security_audit_log` solo en BD (sin Serilog para eventos de auth)** — centraliza en BD, elimina duplicación. Riesgo: si la BD está caída, no se logra el evento. No recomendado.

[Answer]:

---

### Pregunta 3 — AlertMessage como Value Object

`AlertMessage` contiene los 6 campos requeridos por RF-11 (`qué_pasó`, `cuándo`, `dónde`, `severidad`, `causa_probable`, `acción_sugerida`). Se usa en U2 (persistida en tabla `incidents`) y U3 (generada por M10). ¿Cómo la modelamos?

**A) (Recomendada) Record inmutable en `Domain/` + columnas individuales en BD** — `AlertMessage` es un C# `record` con los 6 campos. En la tabla `incidents`, cada campo se persiste como columna separada (`what_happened`, `when_occurred`, etc.). Permite queries SQL directas por campo (p. ej. filtrar por `severity` o `cause_probable`). EF Core lo mapea como `OwnsOne` (Owned Entity).

**B) Record inmutable en `Domain/` + serializado como JSON en una columna `alert_message_json`** — más simple de migrar (una columna), pero no filtrable por SQL sin JSON functions. Adecuado si nunca se necesitarán queries directas a los campos del alert.

**C) Clase mutable con setters (no record)** — permite construcción paso a paso en M10. Menos seguro: alguien podría mutar la alerta después de generada. No recomendado para un value object.

[Answer]:

---

### Pregunta 4 — CheckResult como Value Object

`CheckResult` es el DTO que retorna cada `ICheckExecutor.CheckAsync()` con el resultado de un chequeo (estado del módulo, timestamp, metadatos opcionales). ¿Cómo lo modelamos?

**A) (Recomendada) Record de tránsito (no persistido)** — `CheckResult` es un C# `record` con: `ModuleId Module`, `CheckStatus Status` (enum: Ok, Warn, Critical), `string? Detail`, `DateTimeOffset CheckedAt`. Solo vive en memoria entre el Checker y `MonitoringService`; no se persiste directamente (el incidente y el log recogen la información relevante). Ligero y sin overhead de BD.

**B) Record persistido en tabla `check_events`** — cada ejecución de chequeo queda en BD. Permite ver el historial completo de todos los chequeos (no solo los que generaron incidente). Mayor granularidad pero crece rápido: con chequeos cada 5 min por 4 módulos = ~1.150 registros/día = 104.000 en 90 días. Puede justificarse en post-MVP para análisis de tendencias.

**C) Diccionario/Dictionary<ModuleId, CheckResult> en memoria como estado vivo** — mantiene el estado actual de cada módulo en RAM para el dashboard real-time. Útil como caché de estado pero no reemplaza la persistencia si el proceso reinicia.

[Answer]:

---

### Pregunta 5 — ModuleId Enum

Los módulos M1–M11 (M5 integrado en M6) se identifican a lo largo del sistema. ¿Cómo definimos el enum `ModuleId`?

**A) (Recomendada) Enum con nombres descriptivos** — valores: `Scheduler`, `DbOrderChecker`, `ApiChecker`, `DbHealthChecker`, `RulesManagement`, `CauseClassifier`, `Dashboard`, `IncidentManager`, `AlertRenderer`, `JobsMonitor`. Legibles en logs y en la UI sin traducción adicional. Número de valor = M1..M11 como `int` subyacente para trazabilidad.

**B) Enum con códigos cortos (M1..M11)** — valores: `M1`, `M2`, `M3`, `M4`, `M6`, `M7`, `M8`, `M9`, `M10`, `M11`. Fiel a la nomenclatura del PRD. Requiere un diccionario de display names para la UI.

**C) String libre (no enum)** — `string ModuleId` en lugar de enum. Flexible pero sin validación en tiempo de compilación; errores de typo posibles.

[Answer]:

---

### Pregunta 6 — Seed data de las 2 cuentas pre-creadas

RF-28 exige 2 cuentas creadas desde el arranque con contraseñas únicas (no por defecto) vía User Secrets. ¿Qué datos mínimos necesita cada cuenta?

**A) (Recomendada) `UserName` + `DisplayName` + `Role` + contraseña vía User Secrets** — solo lo necesario para operar. Ejemplo: `Username=operador1`, `DisplayName=Analista Operativo`, `Role=Operador`. Sin email (el sistema es interno; Identity no requiere email confirmado). Siembra vía `IHostedService` en startup o `DatabaseInitializer`.

**B) `UserName` + `Email` (interno ficticio) + `DisplayName` + `Role` + contraseña** — añade un email interno (`operador@monitorporedidos.local`) para compatibilidad futura si se activan notificaciones por correo (Could Have en PRD §2.3). Mínimo overhead.

**C) Solo `UserName` + `Role` + contraseña (sin DisplayName)** — mínimo absoluto. La UI muestra el `UserName` directamente como nombre visible.

[Answer]:

---

## §4 Después de responder

Cuando completes las 6 preguntas, escribe **"listo"** y procederé a:

1. Analizar respuestas para ambigüedades.
2. Si hay ambigüedades, crearé preguntas de seguimiento.
3. Si todo es claro, generaré los 4 artefactos en:
   ```
   aidlc-docs/construction/u1-foundation-cross-cutting/functional-design/
   ├── domain-entities.md
   ├── business-logic-model.md
   ├── business-rules.md
   └── frontend-components.md
   ```

---

## §5 Plan de ejecución (checklist)

- [x] **5.1** Leer artefactos de entrada de Inception (unit-of-work.md, components.md, requirements.md).
- [x] **5.2** Identificar scope de dominio de U1 y preparar preguntas contextuales.
- [x] **5.3** Crear este plan con preguntas en `aidlc-docs/construction/plans/`.
- [x] **5.4** Recopilar respuestas del owner. *(6/6 = A — 2026-05-23)*
- [x] **5.5** Analizar respuestas — 0 ambigüedades detectadas.
- [x] **5.6** Generar `domain-entities.md`. *(v1.0 inicial; v1.1 reescritura por cambio de autenticación — 2026-05-23)*
- [x] **5.7** Generar `business-logic-model.md`. *(v1.0 inicial; v1.1 reescritura por cambio de autenticación — 2026-05-23)*
- [x] **5.8** Generar `business-rules.md`. *(v1.0 inicial; v1.1 reescritura por cambio de autenticación — 2026-05-23)*
- [x] **5.9** Generar `frontend-components.md`. *(v1.0 inicial; v1.1 reescritura por cambio de autenticación — 2026-05-23)*
- [x] **5.9b** Actualizar `requirements.md` v1.2 (RF-26..RF-29, RNF-06, RNF-14, SECURITY-12, §2.1 — 2026-05-23).
- [x] **5.9c** Actualizar `stories.md` (US-23 reescrita, US-24 repropuesta — 2026-05-23).
- [x] **5.10** Actualizar `aidlc-state.md` (Functional Design U1 ✅ — pendiente aprobación del owner).
- [x] **5.11** Registrar en `audit.md`.
- [x] **5.12** Presentar mensaje de cierre para aprobación explícita. *(Aprobado por owner — 2026-05-23)*
