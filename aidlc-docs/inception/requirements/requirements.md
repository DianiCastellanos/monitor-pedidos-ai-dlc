# Requirements — MonitorPedidos AI

**Proyecto:** MonitorPedidos AI
**Empresa:** Manufacturas Eliot
**Sponsor:** Alex Cárdenas (Jefe de Análisis de Sistemas)
**Owner:** Diana Castellanos
**Fecha:** 2026-05-20
**Versión:** 1.2
**Documento fuente:** [`prd.md`](../../../prd.md) (PRD v2.1, 2026-05-18)
**Cuestionario fuente:** [`requirement-verification-questions.md`](./requirement-verification-questions.md) (13/13 respondido)
**Profundidad:** Standard (Greenfield + complejidad moderada-alta + restricciones SECURITY activas)

**Changelog del documento:**
| Versión | Fecha | Cambios |
|---------|-------|---------|
| 1.0 | 2026-05-20 | Versión inicial generada tras responder las 13 preguntas de verificación. |
| 1.1 | 2026-05-20 | **Cambio de contexto de despliegue**: el sistema NO se expone a internet. Demo y operación se ejecutan exclusivamente en localhost o red interna del equipo. Eliminadas todas las referencias a ngrok, URL pública y demo externa. Arquitectura, módulos y lógica se mantienen sin cambios. |
| 1.2 | 2026-05-23 | **Cambio de modelo de autenticación**: de ASP.NET Core Identity con contraseñas a **selección simple de identidad sin contraseña** (dos botones: Analista Operativo / Responsable Técnico). RF-26, RF-28, RF-29, RNF-14 y SECURITY-12 actualizados. Refs a PBKDF2, lockout y credenciales hasheadas eliminadas. |
| 1.3 | 2026-05-24 | **Integración BrandMonitor**: agrega RF-31 (vista de monitoreo por marca con semáforo). Actualiza §2.1 Must Have. BrandMonitor documentado como extensión formal de U3 (ICheckExecutor en Timer2) y U5 (PendingDropThreshold en RuleCondition). |

---

## 1. Intent Analysis Summary

| Atributo | Valor |
|----------|-------|
| User Request | "Usando AI-DLC, construiremos MonitorPedidos AI: detector proactivo de fallas en la descarga de pedidos (Salesforce, Multivende → SQL Server). 25h/mes manuales → <10h/mes con alertas explicables y dashboard de uso interno (localhost / red interna del equipo, sin exposición pública)." *(actualizado v1.1: se eliminó la mención original a demo vía ngrok por solicitud explícita del owner — el sistema NO se expone a internet)* |
| Request Type | **New Project** (Greenfield) |
| Scope Estimate | **System-wide** — 11 módulos lógicos (M1–M11), 1 dashboard ASP.NET Core de uso interno (localhost o red interna del equipo), 1 base de datos SQL Server MonitorPedidosDb (172.16.0.41), 2 integraciones externas simuladas (Salesforce, Multivende) |
| Complexity Estimate | **Moderate-High** — múltiples integraciones, restricciones de seguridad activas, datos sensibles, restricciones temporales (4 semanas), múltiples stakeholders con expectativas distintas |
| Depth seleccionada | **Standard** — el PRD v2.1 cubre la mayoría de áreas; este documento formaliza y completa los gaps identificados en el cuestionario de verificación |

---

## 2. Alcance del MVP

### 2.1 Dentro de alcance (Must Have v1)

- Chequeos periódicos automáticos (BD/jobs cada 5 min, APIs cada 10 min).
- Clasificación de causa raíz en seis categorías: `token`, `api`, `job`, `bd`, `data_quality`, `no_determinada`.
- Reintentos controlados de consultas a APIs (máx 2, **solo** errores 5xx/timeout).
- Generación de alertas explicables en español (6 campos).
- Dashboard ASP.NET Core con vista real-time, histórico, semanal, panel de discrepancias, modo NOC y **vista de monitoreo por marca** con semáforo por site (Patprimo, SevenSeven, Atmos, Ostu), actualizada cada 10 minutos con umbral configurable (RF-31).
- Notificaciones del navegador (push, sonido, parpadeo).
- Registro de incidentes desde día 0 con retención de **90 días**.
- Edición de reglas estáticas vía UI dedicada con historial de cambios (autor + razón).
- Selección simple de identidad sin contraseña con 2 roles: `Operador` (Analista Operativo) y `Técnico` (Responsable Técnico) — pantalla de dos botones, cookie de sesión.
- Datos simulados generados internamente (tabla `simulated_orders` + job .NET).
- 6 escenarios de red-teaming validados.

### 2.2 Should Have (si el tiempo lo permite)

- Vista de patrones nuevos en dashboard.
- Filtros avanzados en histórico.

### 2.3 Could Have (v2 o posterior)

- Anomaly detection / reglas dinámicas.
- Correos recordatorios.
- Vista mobile.
- Renovación automática de tokens (post-MVP, requiere análisis adicional de R5).
- Reintento automático de jobs (descartado en v1).

### 2.4 Won't Have (explícitamente fuera de alcance)

- Correos automáticos en v1.
- Modelos ML complejos.
- Integración con Jira/SAP.
- Despliegue en nube, infraestructura corporativa o exposición pública a internet (ngrok, túneles, dominios públicos) durante el MVP. El sistema corre exclusivamente en localhost o red interna del equipo.

---

## 3. Requerimientos Funcionales

> **Convención de IDs:** `RF-NN`. Cada requerimiento referencia el módulo PRD (M*), caso de uso (UC*) y principio (P*) cuando aplica.

### 3.1 Detección y chequeos (M1–M4, M11)

| ID | Requerimiento | Referencia PRD | Origen |
|----|---------------|----------------|--------|
| RF-01 | El sistema DEBE ejecutar un scheduler que orqueste todos los chequeos periódicos sin requerir intervención manual. | M1 / UC1 | PRD §9 |
| RF-02 | El sistema DEBE chequear la base de datos cada **5 minutos** para verificar la existencia de pedidos recientes según las reglas configuradas. | M2 / UC1 / UC7 | PRD §9, Anexo A #11 |
| RF-03 | El sistema DEBE chequear las APIs externas (Salesforce, Multivende) cada **10 minutos**. | M3 / UC1 / UC4 | PRD §9, Anexo A #11 |
| RF-04 | El sistema DEBE ejecutar `SELECT 1` (health check de BD) cada **5 minutos**, registrando latencia y disponibilidad. | M4 / UC7 | PRD §9 |
| RF-05 | El sistema DEBE consultar y registrar el estado de los jobs de integración cada **5 minutos**, sin reintentarlos automáticamente. | M11 / UC1 | PRD §9, Decisión #2 |
| RF-06 | Ante errores HTTP **5xx** o **timeout** en las consultas a APIs, el sistema DEBE reintentar la consulta hasta **2 veces** y registrar cada reintento bajo el evento `auto_reintento`. | M1, M3 / UC4 / P6 | PRD §6, §9, Anexo A #2 |
| RF-07 | Ante errores HTTP **401** en APIs externas, el sistema DEBE clasificar el incidente como `token` y sugerir la ejecución del SOP-001 ("Renovar token manualmente"), **sin** intentar renovación automática. | M3, M7 / UC4 / Journey 1 | PRD §7, §12 R5 |

### 3.2 Clasificación de causa raíz (M7)

| ID | Requerimiento | Referencia |
|----|---------------|------------|
| RF-08 | El sistema DEBE clasificar la causa raíz de cada incidente en una de seis categorías: `token`, `api`, `job`, `bd`, `data_quality`, `no_determinada`. | M7 / UC1 |
| RF-09 | El sistema DEBE evaluar las causas en cascada en el siguiente orden: `bd` → `job` → `api` → `token` → `data_quality` → `no_determinada`. | M7 |
| RF-10 | Cada incidente clasificado como `no_determinada` DEBE quedar marcado como candidato a regla nueva, para revisión del responsable técnico. | M7 / R9 / Journey 2 |

### 3.3 Alertas explicables (M10, P1)

| ID | Requerimiento | Referencia |
|----|---------------|------------|
| RF-11 | Cada alerta generada DEBE incluir los siguientes 6 campos en español (sin JSON crudo): **(1)** `qué_pasó`, **(2)** `cuándo`, **(3)** `dónde` (módulo/integración afectada), **(4)** `severidad`, **(5)** `causa_probable`, **(6)** `acción_sugerida`. | P1, M10 / Pregunta 2 cuestionario |
| RF-12 | Cada alerta DEBE clasificarse en un nivel de severidad de exactamente 3 valores: **`INFO`**, **`WARN`** o **`CRITICAL`**. | Decisión #10 / Pregunta 3 |
| RF-13 | El sistema DEBE generar el texto de alerta a partir de plantillas configurables (no concatenación ad-hoc) para garantizar consistencia de formato. | M10 |

### 3.4 Reglas y expectativas (M6, P5)

| ID | Requerimiento | Referencia |
|----|---------------|------------|
| RF-14 | El sistema DEBE proveer una **UI dedicada dentro del dashboard** (CRUD) para crear, editar, activar/desactivar y eliminar reglas estáticas. | M6 / P5 / Pregunta 4 |
| RF-15 | Cada modificación de una regla DEBE registrar en historial: **autor** (usuario autenticado), **timestamp**, **razón** (campo obligatorio) y diff `antes/después`. | M6 / P5 |
| RF-16 | El historial de cambios de reglas DEBE ser consultable desde el dashboard, ordenable cronológicamente y exportable. | M6 |
| RF-17 | El acceso al CRUD de reglas DEBE estar restringido al rol `Técnico` (function-level authorization, ver RF-26). | M6 / P5 |

### 3.5 Gestión de incidentes (M9)

| ID | Requerimiento | Referencia |
|----|---------------|------------|
| RF-18 | El sistema DEBE registrar **todo incidente** detectado desde el día 0 de operación, sin excepciones. | P4 / Decisión #4 |
| RF-19 | Un incidente `WARN` o `CRITICAL` DEBE cerrarse **automáticamente** cuando **dos chequeos consecutivos** del mismo módulo retornan estado OK. | Pregunta 6 cuestionario |
| RF-20 | Adicionalmente, el dashboard DEBE permitir el **cierre manual** de un incidente, requiriendo un campo obligatorio `comentario_resolucion`. | Pregunta 6 |
| RF-21 | El sistema DEBE retener todos los datos históricos de incidentes y eventos por **90 días** a partir de su creación; pasado ese plazo, los registros pueden archivarse o purgarse. | Decisión #5 / Pregunta 7 |
| RF-22 | El sistema DEBE garantizar que los incidentes activos persisten correctamente al cerrar y reabrir el dashboard (RT-Persist). | §11 RT-Persist |

### 3.6 Dashboard (M8)

| ID | Requerimiento | Referencia |
|----|---------------|------------|
| RF-23 | El dashboard DEBE proveer las siguientes vistas: **real-time** (estado actual), **histórico** (incidentes por rango), **resumen semanal** (calibración), **panel de discrepancias** (UC6). | M8 / §8 Must Have |
| RF-24 | El dashboard DEBE soportar **modo NOC** (vista de pantalla completa para monitoreo continuo) y notificaciones de navegador con **push, sonido y parpadeo** (sonido y parpadeo activos para `WARN` y `CRITICAL`; parpadeo solo para `CRITICAL`). | M8 / R7 |
| RF-25 | Si la API de Notification del navegador está bloqueada, el sistema DEBE degradar a fallback (sonido + parpadeo) sin afectar la disponibilidad del dashboard. | R7 |
| RF-31 | El dashboard DEBE proveer una vista de **monitoreo por marca** (`/brand-monitor`) que muestre el recuento de pedidos pendientes de cada site (Patprimo, SevenSeven, Atmos, Ostu), actualizado cada 10 minutos por `BrandMonitorChecker` (ICheckExecutor — Timer2), con indicador semáforo 🟢🟡🔴 determinado por el umbral `PendingDropThreshold` configurable desde la UI de reglas (RF-14). | M8 / RF-03 / RF-14 / U6 |

### 3.7 Identidad y autorización — selección simple (P3, SECURITY-08)

> **Nota de contexto (v1.2):** el sistema usa **selección simple de identidad sin contraseña**. Al abrir el dashboard se muestra una pantalla con dos opciones: *Analista Operativo* (rol `Operador`) y *Responsable Técnico* (rol `Técnico`). La identidad seleccionada se persiste en cookie de sesión. Sin credenciales, sin password hashing, sin lockout. Adecuado para despliegue en localhost/red interna del equipo con acceso físico controlado.

| ID | Requerimiento | Referencia |
|----|---------------|------------|
| RF-26 | Toda ruta del dashboard DEBE requerir selección de identidad previa; deny-by-default hasta que el usuario seleccione su rol en la pantalla de inicio. | SECURITY-08 / Pregunta 5 |
| RF-27 | El sistema DEBE soportar exactamente **2 roles**: **`Operador`** (ver dashboard, cerrar incidentes manualmente, ver historial) y **`Técnico`** (todos los permisos de Operador + editar reglas + ver/exportar logs técnicos). | Pregunta 8 |
| RF-28 | El sistema DEBE presentar **2 identidades pre-definidas** seleccionables: *Analista Operativo* (rol `Operador`) y *Responsable Técnico* (rol `Técnico`). Sin contraseñas ni cuentas en base de datos. | 2026-05-23 — decisión owner |
| RF-29 | La identidad seleccionada DEBE persistirse en **cookie de sesión ASP.NET Core** (sin ASP.NET Core Identity completo). Atributos de cookie: `HttpOnly` y `SameSite=Strict` siempre; `Secure` cuando se sirva por HTTPS interno (ver RNF-07). | SECURITY-12 (parcial) |
| RF-30 | El sistema DEBE soportar **5 usuarios concurrentes internos** (analista + técnico + sponsor + invitados internos en demos) sin degradación funcional perceptible. | Pregunta 11 |

---

## 4. Requerimientos No Funcionales

> Restricciones SECURITY-01..SECURITY-15 son **bloqueantes** por decisión del usuario (Pregunta 1 = A).

### 4.1 Rendimiento y disponibilidad

| ID | Requerimiento | Métrica/Objetivo | Trazabilidad |
|----|---------------|------------------|--------------|
| RNF-01 | Tiempo desde la ocurrencia del fallo hasta la generación de la alerta. | **< 10 minutos** (objetivo O1; baseline 1–2 h). | PRD §10 KPI O1 |
| RNF-02 | Tiempo medio de resolución (MTTR) percibido por el operador. | **< 20 minutos** (objetivo O4). | PRD §10 KPI O4 |
| RNF-03 | Tiempo de respuesta de la UI (carga de vista real-time y de incidente). | **< 2 segundos** p95 sobre localhost. | Inferido de RF-30 |
| RNF-04 | Soporte de usuarios concurrentes. | **5 concurrentes** sin degradación. | RF-30 |
| RNF-05 | Frecuencia de polling configurable por módulo. | BD/jobs **5 min**, APIs **10 min**, ajustable vía configuración para mitigar rate-limiting (R6). | Anexo A #11, R6 |

### 4.2 Seguridad (SECURITY extension — bloqueante)

| ID | Requerimiento | SECURITY rule | Aplicabilidad MVP |
|----|---------------|---------------|--------------------|
| RNF-06 | Cifrado at-rest de SQL Server MonitorPedidosDb (172.16.0.41) (TDE habilitado o cifrado de archivo del usuario) y conexión cifrada (TLS) o canal local seguro al motor de BD. | SECURITY-01 | **Aplica** — la BD almacena tokens (referenciados), incidentes, historial de reglas y datos simulados. Sin credenciales de usuario (modelo de selección simple sin passwords). |
| RNF-07 | Modalidad de transporte del dashboard: **(a)** HTTP en `localhost` (loopback) es aceptable cuando la app corre en la máquina del usuario; **(b)** cuando el dashboard se sirva sobre la red interna del equipo (otros dispositivos accediendo al host), DEBE usar HTTPS con certificado de desarrollo de ASP.NET Core (`dotnet dev-certs https`) o certificado interno. **No** se expone a internet. | SECURITY-01 (parcial) | Aplica solo cuando hay acceso por red interna. |
| RNF-08 | Logging estructurado centralizado con: timestamp, request_id, log_level, mensaje. **Prohibido** loggear contraseñas, tokens o PII. | SECURITY-03 / SECURITY-14 | Aplica. Implementación con Serilog + sink local (archivo rotado) en MVP; sink remoto post-MVP. |
| RNF-09 | El dashboard ASP.NET Core DEBE emitir los headers `Content-Security-Policy: default-src 'self'`, `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`. **`Strict-Transport-Security`** se emite **solo** cuando se sirve por HTTPS interno (escenario (b) de RNF-07), con `max-age=31536000; includeSubDomains`. | SECURITY-04 | Aplica. |
| RNF-10 | Validación de entrada en todos los endpoints (model binding con DataAnnotations + FluentValidation), límites de tamaño de payload, consultas parametrizadas (Dapper/EF Core con parámetros, nunca concatenación SQL). | SECURITY-05 | Aplica a todos los endpoints del dashboard y la API interna. |
| RNF-11 | Authorization middleware aplicado a **todos** los controllers; admin/privileged routes con `[Authorize(Roles="Técnico")]`; **CORS deshabilitado** (mismo origen — la app sirve la UI y la API). | SECURITY-08 | Aplica. RF-17, RF-26, RF-27. |
| RNF-12 | Hardening: sin credenciales por defecto, errores de producción devuelven mensajes genéricos (sin stack trace), sin endpoints de muestra, sin directory listing. | SECURITY-09 / SECURITY-15 | Aplica. |
| RNF-13 | Dependency pinning (NuGet lock file `packages.lock.json`), escaneo de vulnerabilidades (`dotnet list package --vulnerable` o equivalente en CI/script), sin paquetes sin uso. | SECURITY-10 | Aplica al runtime .NET 8. |
| RNF-14 | Selección simple de identidad: sin password hashing, sin lockout. Cookie de sesión emitida con `HttpOnly=true`, `SameSite=Strict`; `Secure=true` cuando se sirve por HTTPS interno (RNF-07). Sin credenciales hardcodeadas ni cuentas en base de datos. Implementación: `AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)` + `SignInAsync` con claims de rol. | SECURITY-12 (parcial — solo cookie management, sin credential management) | Aplica parcialmente. |
| RNF-15 | Manejo de excepciones fail-closed: try/catch en todas las llamadas externas (HTTP, BD), global exception handler que loggea (RNF-08) y responde con error genérico, recursos liberados (`using`/`try-finally`). | SECURITY-15 | Aplica. |

### 4.3 SECURITY rules — Mapeo de aplicabilidad completo

| Regla | Estado MVP | Justificación |
|-------|-----------|---------------|
| SECURITY-01 Encryption at rest/in transit | **Aplica** | Cubierto por RNF-06, RNF-07. |
| SECURITY-02 Access logging on network intermediaries | **N/A para MVP** | Despliegue exclusivamente local / red interna del equipo; no hay LB, API gateway ni CDN propios. Re-evaluar en transición on-prem post-MVP. |
| SECURITY-03 Application-level logging | **Aplica** | Cubierto por RNF-08. |
| SECURITY-04 HTTP security headers | **Aplica** | Cubierto por RNF-09. |
| SECURITY-05 Input validation | **Aplica** | Cubierto por RNF-10. |
| SECURITY-06 Least-privilege IAM | **Aplica parcialmente** | Aplica a roles de aplicación (RF-27); IAM cloud N/A (MVP es localhost). El service account local que ejecuta la app correrá con permisos mínimos sobre la carpeta del proyecto y `localdb`. |
| SECURITY-07 Restrictive network configuration | **N/A para MVP** | El sistema corre en localhost o red interna del equipo, **sin exposición pública**. No hay firewall propio configurable a nivel de aplicación. Re-evaluar post-MVP cuando se despliegue on-prem dentro de la red corporativa. |
| SECURITY-08 Application-level access control | **Aplica** | Cubierto por RF-26, RF-27, RNF-11. |
| SECURITY-09 Hardening & misconfiguration prevention | **Aplica** | Cubierto por RNF-12. |
| SECURITY-10 Software supply chain | **Aplica** | Cubierto por RNF-13. |
| SECURITY-11 Secure design principles | **Aplica** | Reglas, autenticación y autorización en módulos dedicados; rate limiting básico vía ASP.NET Core middleware sobre endpoints públicos. |
| SECURITY-12 Authentication & credential management | **Aplica parcialmente** | Modelo de selección simple: sin gestión de credenciales (no hay passwords, no hay hashing). Aplica la parte de gestión de cookies seguras (RNF-14, RF-29). No aplica password storage/hashing. |
| SECURITY-13 Software & data integrity | **Aplica parcialmente** | Cambios críticos (reglas) auditados (RF-15). Sin deserialización insegura. CI/CD básico para MVP académico. |
| SECURITY-14 Alerting & monitoring | **Aplica parcialmente** | Logs locales rotados con retención de 90 días alineada a RF-21; alertas de seguridad (login failures) registradas. Monitoring dashboard avanzado N/A en MVP. |
| SECURITY-15 Exception handling & fail-safe defaults | **Aplica** | Cubierto por RNF-15. |

### 4.4 Trazabilidad y auditoría (P4, P5)

| ID | Requerimiento |
|----|---------------|
| Trazabilidad-1 | 100% de las alertas DEBEN contener los 6 campos definidos en RF-11 (métrica de calidad no negociable). |
| Trazabilidad-2 | 0 violaciones al principio P2 (solo lectura sobre Salesforce/Multivende) durante el MVP. Métrica de calidad no negociable. |
| Trazabilidad-3 | Cada cambio de regla DEBE quedar en historial con autor + razón + diff (RF-15). |
| Trazabilidad-4 | Cada `auto_reintento` DEBE quedar registrado y ser consultable desde el dashboard (RF-06). |

### 4.5 Usabilidad y operación

| ID | Requerimiento |
|----|---------------|
| Usab-1 | Idioma de la UI: **español**. |
| Usab-2 | Las alertas deben ser comprensibles por personal operativo no técnico (lenguaje natural, sin códigos crudos en el campo `qué_pasó`). |
| Usab-3 | El dashboard DEBE ser usable en pantalla completa (modo NOC) sin pérdida de información crítica. |

---

## 5. Casos de Uso (referencia)

Los casos de uso UC1–UC7 del PRD §5 se asumen completos y se trazan en los requerimientos funcionales arriba. Resumen:

| UC | Trazabilidad principal |
|----|------------------------|
| UC1 Ausencia de pedidos | RF-02, RF-08, RF-09, RF-11, RF-12 |
| UC2 Revisión proactiva | RF-23 |
| UC3 Escalamiento | RF-11, RF-23, RNF-02 |
| UC4 Token expirado | RF-07, RF-08, RF-11 |
| UC5 Revisión semanal | RF-23, RF-14..RF-16 |
| UC6 Discrepancias de estado | RF-23 (panel de discrepancias) |
| UC7 Health check BD | RF-04 |

---

## 6. Criterios de Aceptación (Red-Teaming, §11 PRD)

Cada escenario es un criterio de aceptación bloqueante para el cierre del MVP:

| # | Escenario | Resultado esperado | Trazabilidad |
|---|-----------|--------------------|--------------|
| RT1 | Apagar job de Salesforce | M11 detecta y genera CRITICAL en <10 min | RF-05, RF-08, RF-11, RNF-01 |
| RT2 | Revocar token de Salesforce sandbox | M3 detecta 401, NO reintenta, emite CRITICAL con sugerencia SOP-001 | RF-07, RF-08, RF-11 |
| RT3 | Apagar SQL Server | M4 detecta y genera CRITICAL inmediato | RF-04, RF-08, RF-11 |
| RT5 | Pedido con estado incorrecto | Aparece en panel UC6 (solo dashboard, sin alerta WARN/CRITICAL) | RF-23 |
| RT7 | Pedido cancelado | Se ignora (no genera alerta) | RF-08, RF-09 |
| RT-Persist | Cerrar y reabrir dashboard | Alertas activas siguen visibles | RF-22 |

---

## 7. Restricciones

| ID | Restricción | Origen |
|----|-------------|--------|
| C-01 | El MVP DEBE entregarse en **4 semanas** (4 sprints). | PRD §13 |
| C-02 | El MVP corre **exclusivamente en localhost o red interna del equipo**, sin exposición pública a internet. No se utiliza ngrok, túneles ni servicios de exposición externa. La demo se ejecuta dentro del entorno local del owner o de la red de la empresa. Sin nube ni infraestructura corporativa adicional durante el MVP. | Revisión de contexto 2026-05-20 / Decisión #8 |
| C-03 | Stack tecnológico: **.NET 8 (ASP.NET Core) + SQL Server MonitorPedidosDb (172.16.0.41)**. (El stack se mantiene como decisión por defecto del PRD; podrá reconfirmarse en NFR Requirements de Construction). | PRD §13 Sprint 1 |
| C-04 | Solo lectura sobre Salesforce y Multivende. El sistema NUNCA escribe sobre pedidos ni modifica estados en APIs origen. | P2 |
| C-05 | Datos sensibles NO salen del entorno local. Dashboard cloud (futuro) requeriría autorización explícita y operaría sin payloads. | P3 |
| C-06 | Datos del MVP son **simulados**: tabla `simulated_orders` poblada por script SQL inicial + job .NET que inserta pedidos cada 5–10 min con probabilidad configurable de fallo (debe permitir ejecutar los 6 escenarios de red-teaming). | Pregunta 12 |
| C-07 | Operación en paralelo con el monitoreo manual durante **2 semanas post-demo** antes del switch definitivo. | Pregunta 10 / R2 |
| C-08 | El **Failbook** inicial (5–7 casos) se mantiene como **documento externo** en `aidlc-docs/` (Markdown), no como feature del software. Se usa como insumo para crear las reglas iniciales en M6. | Pregunta 9 |
| C-09 | Las reglas SECURITY-01..15 del extension `security/baseline` son **bloqueantes**. Cualquier hallazgo no resuelto bloquea el avance entre stages. | Pregunta 1 |

---

## 8. Supuestos

| ID | Supuesto |
|----|----------|
| A-01 | Los accesos a BD/APIs de Salesforce y Multivende existen y estarán disponibles desde día 0 (R1 cerrado en PRD §12). |
| A-02 | El analista operativo y el responsable técnico están disponibles para validación semanal y para la operación en paralelo de 2 semanas. |
| A-03 | El sponsor (Alex Cárdenas) refuerza la adopción durante la transición (mitigación R2). |
| A-04 | La frecuencia de polling (5/10 min) no excede los rate-limits de Salesforce/Multivende; si lo hace, se ajusta backoff (R6) sin replantear el diseño. |
| A-05 | El navegador objetivo (Chrome/Edge modernos) soporta Notification API; si está bloqueada, se aplica fallback (R7). |
| A-06 | MonitorPedidosDb es suficiente para 90 días de historial con cadencia 5–10 min (~10–50 MB estimado). |

---

## 9. Dependencias

| ID | Dependencia | Tipo |
|----|-------------|------|
| Dep-01 | .NET 8 SDK instalado en el equipo de desarrollo y de demo. | Técnica |
| Dep-02 | SQL Server MonitorPedidosDb (172.16.0.41) instalado (típicamente vía SQL Server Express / Visual Studio). | Técnica |
| Dep-03 | Certificado de desarrollo HTTPS de ASP.NET Core (`dotnet dev-certs https --trust`) en cada equipo desde el que se acceda al dashboard por red interna. Solo aplica al escenario (b) de RNF-07. | Técnica |
| Dep-04 | Failbook inicial (5–7 casos) elaborado por responsable técnico antes del Sprint 2 (para alimentar las reglas iniciales). | Producto |
| Dep-05 | TBDs activos del PRD (TBD-D4 onboarding, TBD-S3-org datos organizacionales, TBD-S3-verbatims) — **diferidos a Sprint 1**, no bloquean Inception. | Producto (Pregunta 13) |

---

## 10. Riesgos abiertos relevantes para Construcción

Heredados del PRD §12 y confirmados por las respuestas del cuestionario:

| Riesgo | Estado | Mitigación referenciada en requirements |
|--------|--------|----------------------------------------|
| R2 No adopción | Abierto | C-07 (operación en paralelo 2 sem) + RF-23/RF-24 (modo NOC) |
| R3 Falsos positivos | Abierto | RF-12 (3 niveles severidad) + UC5 (calibración semanal) |
| R4 Cronograma | Abierto | Should-Haves sacrificables (§2.2) |
| R5 Token rotado | Cerrado a nivel de scope | RF-07 (manual) — sin renovación automática en MVP |
| R7 Notification API bloqueada | Abierto | RF-25 (fallback sonido+parpadeo) |
| R9 Baja cobertura del clasificador | Abierto | RF-10 (causa_no_determinada genera regla nueva) |

---

## 11. Glosario

| Término | Definición |
|---------|------------|
| Operador | Rol con permisos para ver dashboard, cerrar incidentes manualmente y consultar historial. Corresponde al "analista operativo". |
| Técnico | Rol con todos los permisos de Operador más edición de reglas y exportación de logs técnicos. Corresponde al "responsable técnico". |
| Incidente | Evento detectado por el sistema que requiere registro y eventual alerta. |
| Alerta | Notificación generada por el sistema con los 6 campos definidos en RF-11. |
| Modo NOC | Vista de pantalla completa optimizada para monitoreo continuo. |
| SOP-001 | Procedimiento operativo estándar para renovación manual de token de Salesforce/Multivende (referenciado, no parte del software). |
| Failbook | Documento externo con casos históricos de fallas; insumo para creación de reglas. |
| auto_reintento | Evento registrado cada vez que el sistema reintenta una consulta API por error 5xx/timeout. |

---

## 12. Resumen ejecutivo de requerimientos

- **30 requerimientos funcionales** (RF-01 a RF-30) cubren los 11 módulos del PRD, los 7 casos de uso y los 6 principios no negociables.
- **15 requerimientos no funcionales** (RNF-01 a RNF-15) alineados con las 13 reglas SECURITY aplicables (2 marcadas como N/A para MVP localhost: SECURITY-02 y SECURITY-07).
- **9 restricciones** (C-01..C-09), **6 supuestos** (A-01..A-06), **5 dependencias** (Dep-01..Dep-05).
- **6 criterios de aceptación** mediante escenarios de red-teaming.
- **Métrica North Star:** horas operativas recuperadas por mes — baseline 25h → objetivo ≥15h recuperadas (=<10h restantes).

**Estado al cierre de Requirements Analysis (v1.1):** SECURITY extension **habilitada** y aplicable; sin ambigüedades abiertas. Contexto de despliegue confirmado: **localhost o red interna del equipo, sin exposición a internet**. Listo para Workflow Planning (con evaluación condicional de User Stories y Application Design según los 7 UCs ya documentados y las 2 personas de PRD §3).
