# PRD — MonitorPedidos AI (v2.5)

**Empresa:** MANUFACTURAS ELIOT  
**Sponsor:** Alex Cárdenas (Jefe de Análisis de Sistemas)  
**Owner:** Diana Castellanos  
**Marco:** Hardcore AI Cohorte 2 — Estación 2  

---

## Changelog resumido

| Versión | Fecha | Cambios clave |
|---------|-------|----------------|
| 1.0–1.4 | 2026-05-01/18 | Versiones iniciales, consolidación, cierre de bloqueantes. |
| 2.0 | 2026-05-18 | Versión recortada para el curso. |
| **2.1** | **2026-05-18** | **Ajuste de automatización limitada**: reintentos solo a consultas API (5xx/timeout, máx 2), sin renovación de token ni reintento de jobs. Eliminada métrica O6. Corregidas inconsistencias en §7 Journey 1, §8 MoSCoW y §12 R5. |
| **2.2** | **2026-05-20** | **Cambio de contexto de despliegue MVP**: sistema NO se expone a internet. Eliminada toda referencia a ngrok / túneles públicos. Añadida "Nota de despliegue MVP" en Resumen ejecutivo. Reescrita §9 "Estrategia de despliegue del MVP" (unificada en una sola subsección, sin duplicados; HTTPS interno con dev cert). Actualizada Decisión #8 del Anexo A. Aclarada fusión M5→M6 y reformateada lista de módulos M1–M11. |
| **2.3** | **2026-05-20** | **Aclaración de "modo NOC"**: restaurado en M8 y especificado en Must Have (§8) como **vista local a pantalla completa para monitoreo continuo, sin exposición a internet** — consistente con la decisión de despliegue interno de v2.2. Sin cambios funcionales adicionales. |
| **2.4** | **2026-05-23** | **Simplificación del modelo de identidad**: se reemplaza ASP.NET Core Identity con contraseñas por **selección simple de identidad sin contraseña**. Al abrir el dashboard se muestra una pantalla con dos botones: *Analista Operativo* (rol `Operador`) y *Responsable Técnico* (rol `Técnico`). La identidad seleccionada se persiste en cookie de sesión. Sin credenciales, sin password hashing, sin lockout. Adecuado para despliegue interno (localhost/red interna) con acceso físico controlado. Actualizado §8 y Anexo A. |
| **2.5** | **2026-05-24** | **Integración BrandMonitor**: agrega **tablero de estado por marca** (RF-31) como funcionalidad Must Have. Vista `/brand-monitor` muestra semáforo 🟢🟡🔴 por site (Patprimo, SevenSeven, Atmos, Ostu) actualizado cada 10 min. Umbral `PendingDropThreshold` configurable desde la UI de reglas (M6). `BrandMonitorChecker` implementa `ICheckExecutor` y reutiliza cadencia de M3 (Timer2 — 10 min) y cliente Salesforce. No genera incidentes — es vista de estado visual complementaria. Actualizado §8 Must Have, §9 módulos M6 y M8. |

---
## Resumen ejecutivo

**MonitorPedidos AI** detecta en <5 minutos fallas en la descarga de pedidos (Salesforce, Multivende → SQL Server).  
**MVP (4 semanas):** dashboard local + datos simulados, ejecutado en entorno controlado (localhost). Sin nube, sin infraestructura corporativa ni exposición a internet.  

**Nota de despliegue MVP:**  
El sistema se ejecuta exclusivamente en entorno local (localhost) o red interna del equipo. No se contempla uso de túneles (ngrok), dominios públicos ni acceso desde internet, priorizando simplicidad técnica y control del entorno.  
La arquitectura permite su futura exposición controlada en fases posteriores si el negocio lo requiere.
**North Star:** horas operativas recuperadas por mes (baseline 25h → objetivo ≥15h).



# Segmento 1 — One-Liner, JTBD, Misión

**One-Liner:** MonitorPedidos AI detecta en <5 min fallas en la descarga de pedidos, reduce monitoreo manual de 25 a <10 h/mes y entrega alertas con diagnóstico en lenguaje natural.

**JTBD:** Cuando el flujo de descarga se desvía del patrón esperado, quiero una alerta en <5 min con causa probable y acción sugerida, para resolver sin perder 1-2 horas en diagnóstico manual.

**Misión:** Transformar el monitoreo de integraciones de reactivo a proactivo, liberando al equipo operativo de inspección rutinaria y dotando al equipo técnico de evidencia accionable.

---

# Segmento 2 — Contexto y Problema (resumido)

| # | Dolor | Magnitud | Fuente |
|---|-------|----------|--------|
| D1 | Validación manual | ~25 h/mes | Decisión #3 |
| D2 | Tiempo de detección | 1-2 horas | ISB §1 |
| D3 | Incidentes no detectados | ≈2/semana | Decisión #4 |
| D4 | Conocimiento no documentado | Onboarding TBD | ISB §2 |
| D5–D8 | Sin visibilidad, logs inconsistentes, dependencia tácita, sin historial | — | ISB §3, §6 |

**Impacto cualitativo:** retrasos en bodega, correos cruzados, escalamiento a otras áreas.

**¿Por qué ahora?** Ventana de 4 semanas del curso, sponsor con autoridad, costo manual creciente, AI-Ops validado en retail.

**Alternativas actuales:** SQL manual + Postman + logs dispersos → reactivo, sin historial, no escala.

**Build vs. buy (resumido):** Construir sobre .NET/SQL Server existente es más rápido, seguro y genera activo propio. Herramientas comerciales (Datadog, Splunk) son caras, requieren SaaS o no encajan.

---

# Segmento 3 — Stakeholders (resumido)

| Persona | Rol | Dolor principal | Expectativa del MVP |
|---------|-----|----------------|---------------------|
| Analista operativo | Validación manual diaria | 25h/mes, detección lenta | Dashboard + alertas |
| Responsable técnico | Diagnóstico tácito | Concentración de conocimiento | Escalamientos cualificados + reglas editables |
| Alex Cárdenas (sponsor) | Dueño del proceso | Costo recurrente, riesgo operativo | Reducción medible, activo propio |
| IT/Seguridad | Accesos | Seguridad, estabilidad | Solo lectura, on-prem, sin SaaS |

**Objeciones principales y respuestas:** falsos positivos (calibración semanal), carga al técnico (solo escalamientos), seguridad (solo lectura), adopción (sponsor refuerza).

---

# Segmento 4 — UVP y Diferenciadores

**Problema que resuelve:**  
- Analista operativo: inspección manual 25h/mes → alerta proactiva.  
- Responsable técnico: conocimiento tácito → reglas explícitas + registro.  
- Sponsor: costo recurrente + riesgo → activo medible en 4 semanas.

**Cómo lo hace:** chequeos cada 5/10 min, reglas estáticas configurables, alerta con 6 campos en lenguaje natural, solo lectura, registro histórico.

**Diferenciadores clave:**  
- Específico para el dominio (Salesforce/Multivende → SQL).  
- On-prem (datos sensibles no salen).  
- Explicable (no black box).  
- Activo propio (no renta).

**Posicionamiento:** único en el cuadrante on-prem + específico para descarga de pedidos.

---

# Segmento 5 — Casos de Uso (resumidos, UC1 a UC7)

| UC | Nombre | Trigger | Acción resumida | KPI |
|----|--------|---------|----------------|-----|
| UC1 | Ausencia de pedidos | No hay pedidos en ventana esperada | Cascada: API → job → BD → alerta CRITICAL | <10 min detección |
| UC2 | Revisión proactiva | Inicio de jornada | Dashboard muestra estado general | <10 h/mes validación |
| UC3 | Escalamiento | CRITICAL no resuelto | Notifica a técnico con contexto | MTTR <20 min |
| UC4 | Token expirado | HTTP 401 | Si error es 5xx/timeout reintenta hasta 2 veces; si 401, alerta con sugerencia renovación manual (SOP-001) | <10 min detección |
| UC5 | Revisión semanal | Lunes 9am | Resumen de incidentes, ajuste de reglas | Calibración ≥1/semana |
| UC6 | Discrepancias de estado | Estados incorrectos en origen | Panel en dashboard (solo visual, sin email) | Calidad de datos |
| UC7 | Health check BD | SELECT 1 cada 5 min | Alerta si timeout o latencia alta | <10 min detección |

---

# Segmento 6 — Principios No Negociables

| # | Principio | Resumen |
|---|-----------|---------|
| P1 | Explicabilidad | Alerta con 6 campos en español, nada de JSON crudo |
| P2 | Solo lectura | Nunca escribe sobre pedidos ni cambia estados en APIs |
| P3 | Soberanía de datos | Datos sensibles no salen del entorno; dashboard cloud solo si autorizado y sin payloads |
| P4 | Trazabilidad histórica | Todo incidente se registra desde día 0 |
| P5 | Calibración humana | Reglas editables, historial de cambios con autor+razón |
| P6 | Automatización limitada y supervisada | Reintentos controlados de consultas API (5xx/timeout, máx 2). No renueva tokens ni reintenta jobs. Cada reintento se registra (`auto_reintento`) y se notifica. Humano en el loop. |

---

# Segmento 7 — User Journeys (solo 2, los más ilustrativos)

## Journey 1 — Token expirado (sin automatización)
1. M3 detecta 401 y sugiere: "Renovar token según SOP-001".
2. Analista revisa el dashboard, ve la sugerencia, ejecuta SOP-001 manualmente.
3. Sistema registra el incidente como resuelto por humano.
**Tiempo humano: <5 min. Sin reintento automático.**

## Journey 2 — Causa no determinada (edge case)
1. CRITICAL por ausencia de pedidos, pero todos los componentes responden OK.  
2. M7 emite `causa_no_determinada`, alerta a analista y técnico.  
3. Técnico investiga, descubre cambio de zona horaria en Multivende.  
4. Crea regla nueva en M6 para futuros casos.  
**Lección:** cada `causa_no_determinada` se convierte en regla.

---

# Segmento 8 — MVP Scope (MoSCoW)

**Must Have (v1):**  
- Chequeos periódicos (5/10 min) de BD, APIs, jobs, health check.  
- Clasificador de causa raíz (token, api, job, bd, data_quality, no_determinada).  
- Dashboard con vista real-time, histórico, semanal, panel de discrepancias y **tablero de estado por marca** con semáforo 🟢🟡🔴 por site (Patprimo, SevenSeven, Atmos, Ostu), actualizado cada 10 min con umbral configurable.  
- Notificaciones del navegador (push, sonido, parpadeo, **modo NOC** — vista local a pantalla completa para monitoreo continuo, sin exposición a internet).  
- Registro de incidentes desde día 0.  
- Reglas estáticas editables, historial de cambios.  
- **Selección simple de identidad** (pantalla con 2 botones: *Analista Operativo* / *Responsable Técnico*) con cookie de sesión — sin contraseñas ni ASP.NET Core Identity completo.  
- **Reintentos controlados de consultas a APIs (máx 2, solo para errores 5xx/timeout).**  
- ❌ Sin renovación automática de token.  
- ❌ Sin reintento automático de jobs.

**Should Have (si da tiempo):** vista de patrones nuevos, filtros avanzados.

**Could Have (v2):** correos recordatorios, anomaly detection, vista mobile.

**Won't Have (fuera de alcance):**  
- Correos automáticos (v1 solo dashboard).  
- ML complejo (Prophet, etc.).  
- Integración con Jira/SAP.

---

# Segmento 9 — Especificación Funcional (resumida)

## Módulos principales (M1 a M11)

> **Nota:** El módulo M5 fue fusionado con M6 (reglas y expectativas) según la decisión #1 (v2.1). La numeración M1-M11 se mantiene por coherencia con el resto del documento.

- **M1 – Scheduler:** orquesta chequeos periódicos. Puede ejecutar reintentos limitados de consultas a APIs (máx 2, solo para errores 5xx/timeout).
- **M2 – Chequeo de pedidos en BD interna** (cadencia: 5 min).
- **M3 – Chequeo de APIs externas (Salesforce, Multivende)** (cadencia: 10 min, con reintentos en 5xx/timeout; no renueva token).
- **M4 – Health check de BD** (cadencia: 5 min, SELECT 1).
- **M6 – Motor de reglas, filtros y expectativas** (estáticas, editables, incluye funcionalidad del antiguo M5; extiende al módulo `BrandMonitor` con parámetro `PendingDropThreshold` — umbral configurable desde la UI de reglas).
- **M7 – Clasificador de causa raíz** (cascada, no ordena reintentos de token/jobs).
- **M8 – Dashboard** (ASP.NET Core, notificaciones push del navegador, auto-refresh, **modo NOC**: vista local a pantalla completa para monitoreo continuo; **tablero de estado por marca** `/brand-monitor`: semáforo 🟢🟡🔴 por site actualizado cada 10 min — sin alertas push, solo visual; sin exposición a internet — solo accesible desde localhost o red interna del equipo).
- **M9 – Repositorio de incidentes** (tabla `incidents`).
- **M10 – Explicador en lenguaje natural** (plantillas).
- **M11 – Verificador de ejecución de jobs** (cadencia: 5 min, solo consulta y registra – no reintenta).

## Estrategia de despliegue del MVP

El sistema MonitorPedidos AI se ejecuta únicamente en un entorno controlado:
- **Aplicación web:** ASP.NET Core ejecutado en `localhost` (entorno de desarrollo) o en una máquina dentro de la red interna (LAN) con HTTPS habilitado mediante certificado de desarrollo.
- **Base de datos:** SQL Server LocalDB / Express, con datos simulados para la demo.
- **Acceso:** restringido a los miembros del equipo (analista operativo y responsable técnico) que tengan acceso físico o de red al equipo donde corre la aplicación.
- **Exposición a internet:** No se utiliza ni se requiere ngrok, túneles, dominios públicos ni servicios cloud. La demostración se realiza compartiendo pantalla localmente o accediendo desde otro equipo de la red interna.

> **Post-MVP (futuro):** Si la empresa lo requiere, el sistema podrá desplegarse on‑prem dentro de la red corporativa bajo los controles habituales de IT y seguridad. La arquitectura actual lo permite sin cambios de código.


---

# Segmento 10 — Métricas de Éxito (resumido)

**North Star:** horas recuperadas por mes (objetivo ≥15h).

| KPI | Baseline | Objetivo |
|-----|----------|----------|
| O1 Tiempo de detección | 1-2h | **<10 min** |
| O2 Horas/mes validación | ~25h | **<10 h** |
| O3 Incidentes no detectados | ≈2/sem | **<1/mes** |
| O4 MTTR | — | **<20 min** |
| O5 Precisión causa | — | baseline |

**Métricas de calidad no negociables:**  
- Seguridad: 0 violaciones a solo lectura.  
- Trazabilidad: 100% alertas con 6 campos.  

---

# Segmento 11 — Plan de Evaluación (red-teaming)

**6 escenarios obligatorios para el MVP:**

| # | Escenario | Esperado |
|---|-----------|----------|
| RT1 | Apagar job de Salesforce | M11 detecta → CRITICAL <10 min |
| RT2 | Revocar token de Salesforce (sandbox) | M3 detecta 401 → reintentos (no aplican por ser 401) → alerta CRITICAL con sugerencia "Renovar token manualmente según SOP-001". No hay renovación automática. |
| RT3 | Apagar SQL Server | M4 detecta → CRITICAL inmediato |
| RT5 | Pedido con estado incorrecto | Aparece en panel UC6 (solo dashboard) |
| RT7 | Pedido cancelado | Se ignora correctamente |
| RT-Persist | Cerrar y reabrir dashboard | Alertas activas siguen visibles |

**Criterio de aceptación:** todos los escenarios superados antes del cierre del curso.

---

# Segmento 12 — Riesgos (resumido)

| # | Riesgo | Mitigación |
|---|--------|-------------|
| R1 (cerrado) | Accesos a BD/APIs | ✅ Disponibles desde día 0 |
| R2 | No adopción del dashboard | Sponsor refuerza, modo NOC, demos semanales |
| R3 | Falsos positivos | 3 niveles de severidad, calibración semanal |
| R4 | Cronograma | Should-Haves se sacrifican |
| R5 | Token rotado | El sistema detecta 401 y sugiere renovación manual (SOP-001). No hay renovación automática. |
| R6 | Rate-limiting | Cadencia configurable, backoff |
| R7 | Notification API bloqueada | Fallback: sonido + parpadeo |
| R8 | Conocimiento no documentado | Failbook + historial de reglas |
| R9 | Baja cobertura del clasificador | Cada `causa_no_determinada` crea regla nueva |
| R10 | Falsa alarma de BD lenta | WARN antes de CRITICAL, sin reintento automático |

---

# Segmento 13 — Plan de Entrega (hitos)

**Pre-Sprint 0 (Día 0):**  
- Failbook inicial (5-7 casos).  
- TBDs se cierran durante semana 1.

**Sprint 1 (semana 1):**  
- Setup .NET 8 + SQL Server LocalDB.  
- M1, M2, M9 básicos.  
- Tabla `incidents` poblándose.

**Sprint 2 (semana 2):**  
- M3 (APIs + reintentos en 5xx/timeout), M4 (health check), M11 (jobs verificador, sin reintento).  
- M6 reglas, M7 clasificador.  
- Dashboard mínimo + push browser.

**Sprint 3 (semana 3):**  
- M10 explicador NL.  
- Dashboard completo (histórico, semanal, UC6).  
- Tabla reglas editable.  
- Etiquetado opcional.

**Sprint 4 (semana 4):**  
- Red-teaming (6 escenarios).  
- Ajustes de reglas.  
- Medición de KPIs.  
- Demo con sponsor.

**Hito de cierre:** 6 escenarios superados + métrica demostrable de reducción de tiempo de detección + demo aprobada.

---

# Anexo A — Decisiones congeladas del Paso 0 (resumido)

| # | Decisión |
|---|----------|
| 1 | Reglas estáticas, anomaly detection → v2 |
| 2 | **Automatización limitada y segura en v1**: reintentos controlados de consultas API (máx 2, solo 5xx/timeout). No renueva tokens, no reintenta jobs. Humano en el loop. |
| 3 | Baseline 25h/mes |
| 4 | Registro de incidentes desde día 0 |
| 5 | Historial 30-90 días para baseline |
| 6 | Explicabilidad: 6 campos |
| 7 | Usuarios: analista operativo, responsable técnico |
| 8 | Despliegue MVP: entorno local (localhost) o red interna del equipo. Sin ngrok, sin túneles, sin exposición a internet. Post-MVP on-prem en red corporativa bajo controles de IT. |
| 9 | Canal: solo dashboard + push browser |
| 10 | Severidad: 3 niveles |
| 11 | Frecuencias: 5 min (BD, jobs), 10 min (APIs) |
| 12 | BLK-12 cerrado |
| 13 | **Identidad simplificada (v2.4, 2026-05-23):** modelo de selección simple sin contraseñas. Al abrir el dashboard, el usuario selecciona su identidad entre dos opciones predefinidas (*Analista Operativo* / *Responsable Técnico*). Cookie de sesión `HttpOnly + SameSite=Strict`. Sin ASP.NET Core Identity completo, sin password hashing, sin lockout, sin cuentas en base de datos. Adecuado para el contexto de despliegue interno (localhost/red interna del equipo). |

---

# Anexo B — Trazadores activos

| ID | Asunto | Estado |
|----|--------|--------|
| TBD-D4 | Tiempo onboarding | 🟡 Pendiente (semana 1) |
| TBD-S3-org | Datos organizacionales | 🟡 Pendiente (semana 1) |
| TBD-S3-verbatims | Verbatims | 🟡 Pendiente (semana 1) |

---

*Hardcore AI Cohorte 2 — PRD v2.5 (2026-05-24)*