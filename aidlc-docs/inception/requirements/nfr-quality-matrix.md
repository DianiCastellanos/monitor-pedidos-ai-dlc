# Matriz de Atributos de Calidad (NFR) — MonitorPedidos AI

**Versión:** 1.1
**Fecha:** 2026-05-23
**Fuente primaria:** [`requirements.md`](./requirements.md) v1.2
**Fuente arquitectura:** [`components.md`](../application-design/components.md), [`unit-of-work.md`](../application-design/unit-of-work.md)
**Alineación:** ISO/IEC 25010:2011 (SQuaRE — Software Product Quality)

---

## §1 Registro de Atributos de Calidad

Seis atributos identificados. Los dos marcados como **CRÍTICO** son bloqueantes para el MVP: sin cumplirlos, el sistema no cumple su razón de existir ni puede operar en el contexto de seguridad del cliente.

| QA | Atributo | Criticidad | Categoría ISO 25010 | Justificación para MonitorPedidos AI | NFRs Cubiertos |
|----|----------|-----------|---------------------|--------------------------------------|----------------|
| QA1 | **Rendimiento** | **CRÍTICO** | Performance Efficiency | El KPI North Star es detección < 10 min (O1); baseline actual es 1–2 h. Sin este atributo el sistema no cumple su propósito de negocio. | RNF-01, RNF-02, RNF-03, RNF-04, RNF-05 |
| QA2 | **Seguridad** | **CRÍTICO** | Security | La extensión SECURITY está habilitada como bloqueante (C-09). El sistema almacena referencias a tokens y datos de pedidos (incidentes, historial de reglas) en localhost/red interna; cualquier brecha es inaceptable para Manufacturas Eliot. Modelo de identidad simplificado (sin credenciales en BD). | RNF-06..RNF-15 (10 NFRs) |
| QA3 | **Confiabilidad** | Alto | Reliability | Sistema de monitoreo continuo: si falla al detectar un fallo real, pierde su valor completamente. Cubre tolerancia a fallos de BD/API y recuperabilidad tras reinicio. | RNF-15 (compartido con QA2), RF-22 |
| QA4 | **Trazabilidad** | Alto | Accountability / Auditability | Principios P4 (registro total desde día 0) y P5 (historial de reglas con autor+razón) son explícitamente no negociables en el PRD. Sin trazabilidad el cliente no puede auditar el comportamiento del sistema. | Trazabilidad-1..4 |
| QA5 | **Usabilidad** | Medio | Usability | El Operador es personal no técnico que debe comprender alertas y operar en modo NOC; la UI debe estar completamente en español. | Usab-1..3 |
| QA6 | **Mantenibilidad** | Medio | Maintainability | La arquitectura Vertical Slice permite evolucionar módulos sin romper otros; las reglas deben ser editables sin redeploy; el polling es configurable. Crítico para el escenario post-MVP. | Arquitectura (implícito), RNF-05, RF-14..16 |

---

## §2 Matriz Completa de NFRs

> **Leyenda de Prioridad:** `[CRÍTICO]` = bloqueante para MVP | `[Alto]` = debe resolverse antes del go-live | `[Medio]` = debe resolverse; no bloquea demo
>
> **Leyenda de Verificación:** `RT-n` = escenario red-teaming | `CR` = code review | `UT` = unit test | `LT` = load test | `IC` = inspección de configuración | `E2E` = prueba end-to-end | `DM` = demo supervisada

### 2.1 QA1 — Rendimiento

| ID NFR | Descripción | Sub-atributo | Métrica Objetivo | Prioridad | Verificación | Unidades |
|--------|-------------|-------------|------------------|-----------|-------------|---------|
| RNF-01 | Tiempo desde fallo hasta generación de alerta | Comportamiento temporal | **< 10 min** (KPI O1; baseline 1–2 h) | `[CRÍTICO]` | RT1, RT2, RT3 | U3, U4 |
| RNF-02 | MTTR percibido por operador | Comportamiento temporal | **< 20 min** (KPI O4) | `[CRÍTICO]` | DM (medición cronometrada) | U3, U6 |
| RNF-03 | Tiempo de respuesta de la UI (p95) | Comportamiento temporal | **< 2 s** en localhost | `[Alto]` | LT con 5 usuarios concurrentes | U6 |
| RNF-04 | Usuarios concurrentes sin degradación | Capacidad | **5 usuarios** simultáneos | `[Alto]` | LT (NBomber o similar) | U6 |
| RNF-05 | Frecuencia de polling configurable por módulo | Uso de recursos | BD/jobs **5 min**, APIs **10 min**; ajustable sin recompilación | `[Medio]` | IC (`appsettings.json`), prueba de reconfiguración | U3, U4 |

### 2.2 QA2 — Seguridad

| ID NFR | Descripción | Sub-atributo | Métrica Objetivo | Prioridad | Verificación | Unidades | Regla SECURITY |
|--------|-------------|-------------|------------------|-----------|-------------|---------|----------------|
| RNF-06 | Cifrado at-rest (LocalDB) + canal cifrado a BD | Confidencialidad | TDE habilitado o cifrado de archivo; conexión TLS o canal local seguro | `[CRÍTICO]` | IC (config BD + string de conexión) | U1, U2 | SECURITY-01 |
| RNF-07 | HTTPS cuando se sirve por red interna | Confidencialidad | `dotnet dev-certs https` instalado y confiado en cada equipo accedente | `[Alto]` | Test manual desde segundo equipo | U1 | SECURITY-01 (parcial) |
| RNF-08 | Logging estructurado sin PII | No-repudio + Privacidad | **0 entradas** con contraseñas, tokens o PII; campos obligatorios: `timestamp`, `request_id`, `log_level`, `mensaje` | `[CRÍTICO]` | CR + auditoría de logs generados | U1, U3, U4 | SECURITY-03, SECURITY-14 |
| RNF-09 | HTTP security headers emitidos | Integridad de transporte | 4 headers presentes: `Content-Security-Policy: default-src 'self'`, `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`; `HSTS` solo en HTTPS | `[CRÍTICO]` | E2E headers check (curl / browser DevTools) | U1 | SECURITY-04 |
| RNF-10 | Validación de entrada + consultas parametrizadas | Integridad / Prevención inyección | **0** concatenaciones SQL; **100 %** de endpoints con DataAnnotations o FluentValidation; límites de payload configurados | `[CRÍTICO]` | CR + SAST (Roslyn analyzers) | U1, U2, U5 | SECURITY-05 |
| RNF-11 | Authorization middleware en todas las rutas | Control de acceso | **0** rutas sin `[Authorize]` excepto `/Identity/Select`; rutas Técnico con `[Authorize(Roles="Técnico")]`; CORS deshabilitado | `[CRÍTICO]` | E2E (acceso sin sesión devuelve redirect a selección de identidad); CR | U1 | SECURITY-08 |
| RNF-12 | Hardening de producción | Configuración segura | Sin stack traces en respuestas de error; sin endpoints de muestra; sin directory listing; errores genéricos al cliente | `[CRÍTICO]` | E2E errores forzados; IC entorno producción | U1 | SECURITY-09, SECURITY-15 |
| RNF-13 | Dependency pinning y escaneo de vulnerabilidades | Cadena de suministro | `packages.lock.json` presente; **0 vulnerabilidades** High/Critical en `dotnet list package --vulnerable` | `[Alto]` | IC (lock file); script `dotnet list package --vulnerable` | U1 | SECURITY-10 |
| RNF-14 | Identidad simplificada: gestión de cookies seguras | Gestión de sesión (sin credenciales) | Selección simple sin password ni lockout; cookies `HttpOnly + SameSite=Strict`; `Secure` solo en HTTPS; **0** credenciales hardcodeadas; **0** cuentas en BD | `[CRÍTICO]` | IC cookie attributes (DevTools); CR (sin ASP.NET Core Identity ni passwords en código) | U1 | SECURITY-12 (parcial) |
| RNF-15 | Exception handling fail-closed | Tolerancia a fallos + Confidencialidad | Try/catch en **100 %** de llamadas externas (HTTP + BD); global exception handler activo; **0** recursos sin liberar (`using`/`try-finally`) | `[CRÍTICO]` | UT con fallos inyectados; CR | U1, U3, U4 | SECURITY-15 |

### 2.3 QA3 — Confiabilidad

| ID NFR | Descripción | Sub-atributo | Métrica Objetivo | Prioridad | Verificación | Unidades |
|--------|-------------|-------------|------------------|-----------|-------------|---------|
| RNF-15* | Exception handling + liberación de recursos | Tolerancia a fallos | (ídem arriba — QA3 + QA2 compartidos) | `[CRÍTICO]` | UT con fallos inyectados | U1, U3, U4 |
| RF-22 (NFR operacional) | Alertas activas persisten al cerrar y reabrir dashboard | Recuperabilidad | **100 %** de alertas activas visibles tras reinicio de navegador/app | `[CRÍTICO]` | RT-Persist | U2, U6 |
| RF-06 (conducta de reintento) | Reintentos controlados ante 5xx/timeout | Tolerancia a fallos | Máx **2 reintentos**; **0 reintentos** en error 401; cada reintento registrado como `auto_reintento` | `[Alto]` | UT fallo simulado 5xx/timeout; UT error 401 | U4 |

> *RNF-15 es la misma regla; se duplica en QA3 para reflejar su dimensión de confiabilidad además de seguridad.

### 2.4 QA4 — Trazabilidad

| ID NFR | Descripción | Sub-atributo | Métrica Objetivo | Prioridad | Verificación | Unidades |
|--------|-------------|-------------|------------------|-----------|-------------|---------|
| Trazabilidad-1 | 100 % de alertas contienen los 6 campos de RF-11 | Completitud de registro | **6/6 campos** presentes en **toda** alerta generada: `qué_pasó`, `cuándo`, `dónde`, `severidad`, `causa_probable`, `acción_sugerida` | `[CRÍTICO]` | RT1, RT2, RT3, RT5, RT7; UT unitario por campo | U3 |
| Trazabilidad-2 | 0 operaciones de escritura sobre APIs origen | Integridad de datos externos | **0 llamadas** HTTP write/mutating a Salesforce o Multivende (P2 no negociable) | `[CRÍTICO]` | CR (solo GET/read calls); RT completos + spy de HTTP client | U4 |
| Trazabilidad-3 | Historial completo de cambios de reglas | Auditabilidad | **100 %** de cambios registran: autor (usuario autenticado), timestamp, razón (campo obligatorio), diff antes/después | `[Alto]` | UT CRUD reglas; E2E flujo edición | U5 |
| Trazabilidad-4 | Eventos `auto_reintento` consultables | Auditabilidad | **100 %** de reintentos loggeados y visibles desde el dashboard histórico | `[Alto]` | UT con fallo API simulado; E2E consulta histórico | U4, U6 |

### 2.5 QA5 — Usabilidad

| ID NFR | Descripción | Sub-atributo | Métrica Objetivo | Prioridad | Verificación | Unidades |
|--------|-------------|-------------|------------------|-----------|-------------|---------|
| Usab-1 | UI completa en español | Operabilidad | **100 %** de labels, mensajes, alertas y errores en español | `[Medio]` | Inspección visual + revisión de templates M10 | U6, U3 |
| Usab-2 | Alertas comprensibles para personal no técnico | Apropiabilidad | Campo `qué_pasó` en lenguaje natural; **0** JSON crudos ni códigos técnicos expuestos al operador | `[Alto]` | DM con persona Operador; revisión plantillas M10 | U3, U6 |
| Usab-3 | Dashboard funcional en modo pantalla completa | Operabilidad | Modo NOC muestra: severidad actual, alertas activas, timestamp último chequeo — sin scroll oculto de info crítica | `[Alto]` | DM modo NOC; test en resolución 1920×1080 | U6 |

### 2.6 QA6 — Mantenibilidad

| ID NFR | Descripción | Sub-atributo | Métrica Objetivo | Prioridad | Verificación | Unidades |
|--------|-------------|-------------|------------------|-----------|-------------|---------|
| Arq-01 (implícito) | Vertical Slice / Feature Folders sin acoplamiento cruzado | Modularidad | Cada módulo M1–M11 en su propia carpeta; **0** dependencias directas entre módulos (solo a través de servicios/interfaces) — regla estructural de `component-dependency.md` | `[Medio]` | CR (análisis de `using` statements); `dotnet-depends` o equivalente | U1..U7 |
| Arq-02 (implícito) | Polling configurable sin recompilación | Modificabilidad | Intervalos en `appsettings.json`; cambio de frecuencia aplicable con restart del proceso, sin cambio de código | `[Medio]` | IC + test de reconfiguración | U3, U4 |
| RF-14/15/16 (mantenibilidad de reglas) | Reglas editables desde UI con historial | Modificabilidad | CRUD operativo + historial auditable (cubre también QA4) | `[Alto]` | E2E CRUD completo; test historial | U5 |

---

## §3 Resumen por Atributo de Calidad

| QA | Atributo | NFRs Totales | NFRs Críticos | NFRs Altos | NFRs Medios | Unidades Principales |
|----|----------|:---:|:---:|:---:|:---:|---------------------|
| QA1 | Rendimiento | 5 | 2 | 2 | 1 | U3, U4, U6 |
| QA2 | Seguridad | 10 | 8 | 2 | — | U1 (fundación) |
| QA3 | Confiabilidad | 3 | 2 | 1 | — | U1, U2, U3, U4, U6 |
| QA4 | Trazabilidad | 4 | 2 | 2 | — | U3, U4, U5, U6 |
| QA5 | Usabilidad | 3 | — | 2 | 1 | U3, U6 |
| QA6 | Mantenibilidad | 3 | — | 1 | 2 | U1..U7 (transversal) |
| **Total** | | **28*** | **14** | **10** | **4** | |

> *RNF-15 y Arq-01 aparecen en 2 atributos (contados una vez en el total).

---

## §4 Cobertura NFRs → Unidades de Trabajo

| Unidad | QA1 | QA2 | QA3 | QA4 | QA5 | QA6 | NFRs directos |
|--------|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| U1 Foundation & Cross-Cutting | — | RNF-06..15 | RNF-15 | — | — | Arq-01 | 11 |
| U2 Persistence & Incidents | — | RNF-06 | RF-22 | — | — | — | 2 |
| U3 Detection & Classification | RNF-01, RNF-02 | RNF-08, RNF-15 | RNF-15 | Traz-1 | Usab-1, Usab-2 | Arq-01 | 7 |
| U4 External Integrations | RNF-01, RNF-05 | RNF-08, RNF-15 | RF-06 (reintento) | Traz-2, Traz-4 | — | Arq-02 | 7 |
| U5 Rules Management | — | RNF-10 | — | Traz-3 | — | RF-14..16 | 3 |
| U6 Dashboard & Real-Time | RNF-03, RNF-04 | — | RF-22 | Traz-4 | Usab-1..3 | — | 6 |
| U7 Simulation & Red-Teaming | — | — | RF-06 | Traz-1, Traz-2 | — | — | 3 |

---

## §5 Criterios de Aceptación por Atributo (gate de MVP)

| QA | Condición de aprobación (gate) | Escenario de validación |
|----|-------------------------------|------------------------|
| QA1 Rendimiento | RNF-01 demostrado en ≥ 3 escenarios RT; RNF-02 medido en demo cronometrada | RT1, RT2, RT3 + DM |
| QA2 Seguridad | Los 13 checks SECURITY completados (0 hallazgos bloqueantes abiertos) | CR + E2E headers + IC |
| QA3 Confiabilidad | RT-Persist exitoso; UT de reintento 5xx y bloqueo 401 pasados | RT-Persist + UT |
| QA4 Trazabilidad | 6/6 campos en alertas RT; 0 escrituras a APIs origen en todos los RT; historial de regla con autor+razón verificado | RT completo + E2E |
| QA5 Usabilidad | UI 100 % en español validada por DM; modo NOC demo en pantalla completa aprobado por sponsor | DM con sponsor |
| QA6 Mantenibilidad | 0 dependencias directas entre módulos (CR); polling reconfigurable sin recompilación (IC) | CR + IC |

---

## §6 Riesgos de Calidad Abiertos

| Riesgo | QA afectado | Probabilidad | Impacto | Mitigación |
|--------|------------|:---:|:---:|------------|
| Falsos positivos excesivos (R3) | QA1 (precisión detección), QA5 (fatiga del operador) | Media | Alto | RF-12 (3 niveles severidad) + UC5 (calibración semanal) |
| Notification API bloqueada (R7) | QA5 (usabilidad modo NOC) | Media | Medio | RF-25 (fallback sonido + parpadeo) |
| Rate-limiting de APIs externas (R6) | QA1 (detección tardía) + QA3 | Baja | Alto | RNF-05 (polling configurable + backoff) |
| Baja cobertura clasificador (R9) | QA4 (trazabilidad / completitud) | Media | Medio | RF-10 (causa_no_determinada → candidato a regla nueva) |
| Vulnerabilidad en dependencia NuGet | QA2 (seguridad) | Baja | Crítico | RNF-13 (dependency pinning + `dotnet list package --vulnerable`) |
