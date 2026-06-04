# User Stories — MonitorPedidos AI

**Fecha:** 2026-05-21
**Versión:** 1.1 (2026-05-24 — agrega US-30 BrandMonitor; actualiza trazabilidad RF-31; actualiza cobertura a 30/31 RFs)
**Stage:** Inception → User Stories (Part 2 — Generation)
**Personas:** ver [`personas.md`](./personas.md)
**Fuentes:** [`prd.md`](../../../prd.md) v2.3 + [`requirements.md`](../requirements/requirements.md) v1.3.

---

## Cómo se leen estas stories

- **Formato narrativo:** Connextra — *"Como [persona], quiero [acción], para [valor]"* (decisión Q3 del plan).
- **Criterios de aceptación:** Given/When/Then (Gherkin), 2–4 criterios por story (Q5).
- **Granularidad:** stories pequeñas de 1–3 días cada una (Q2).
- **Trazabilidad:** tabla compacta al final de cada story con columnas `RF` (de [`requirements.md`](../requirements/requirements.md)), `UC` (del PRD §5/§7) y `Red-Teaming` (escenarios PRD §11) — decisión Q6.
- **Organización:** breakdown híbrido persona-primero / feature-secundario con sub-secciones `Monitorear` / `Detectar` / `Investigar` / `Configurar` (Q4). Las stories de calidad y operación (RNF + SECURITY + red-teaming) viven en §3 (Q7).
- **INVEST:** todas las stories cumplen Independent, Negotiable, Valuable, Estimable, Small, Testable.
- **Idioma:** español; las alertas y mensajes UI deben emitirse en español (P1).

> **Nota sobre herencia:** El rol `Técnico` hereda todos los permisos del `Operador` (RF-27). Las stories que aparecen bajo Operador son ejecutables también por Técnico salvo que se indique lo contrario. Las stories que aparecen bajo Técnico son **exclusivas** de ese rol.

---

# 1. Operador / Analista operativo

## 1.1 Monitorear

### US-01 — Ver vista real-time del estado del sistema

**Como** Operador,
**quiero** ver en una sola vista real-time el estado actual de BD, APIs, jobs y health-check con su última verificación y resultado,
**para** conocer el pulso del sistema sin tener que ejecutar consultas manuales ni abrir varias herramientas.

**Criterios de aceptación:**

- *Given* que estoy autenticado como Operador y abro el dashboard, *when* la vista real-time se carga, *then* veo el estado actual (OK / WARN / CRITICAL) de los 4 dominios monitoreados (BD, APIs Salesforce, APIs Multivende, jobs) con timestamp de la última verificación.
- *Given* que la vista real-time está abierta, *when* transcurre la cadencia configurada (5 min BD/jobs/health, 10 min APIs), *then* la vista se actualiza automáticamente sin que tenga que recargar la página.
- *Given* que un dominio está en estado CRITICAL, *when* miro la vista real-time, *then* ese dominio aparece destacado visualmente (color + posición prominente).

**Trazabilidad:** RF-23 / UC2 / RT-Persist

---

### US-02 — Activar modo NOC (pantalla completa)

**Como** Operador,
**quiero** activar modo NOC (pantalla completa) en el dashboard,
**para** mantener el monitoreo visible durante toda mi jornada sin distracciones de otras aplicaciones.

**Criterios de aceptación:**

- *Given* que el dashboard está abierto, *when* activo el modo NOC, *then* la UI ocupa toda la pantalla, oculta menús no esenciales y maximiza el área de estado real-time.
- *Given* el sistema corre en localhost o red interna del equipo (sin exposición a internet, PRD §9), *when* uso modo NOC, *then* la funcionalidad opera sin requerir conexión externa ni servicios cloud.

**Trazabilidad:** RF-24 / UC2 / —

---

### US-03 — Recibir notificación del navegador con fallback

**Como** Operador,
**quiero** recibir notificación del navegador (push + sonido + parpadeo) cuando aparezca una alerta WARN o CRITICAL,
**para** enterarme aunque no esté mirando el dashboard activamente.

**Criterios de aceptación:**

- *Given* que el dashboard está abierto en una pestaña y aparece un incidente CRITICAL, *when* el navegador permite Notification API, *then* recibo notificación push + sonido + parpadeo del título de la pestaña.
- *Given* el navegador tiene Notification API bloqueada (R7), *when* aparece la alerta, *then* el sistema degrada automáticamente a fallback (sonido + parpadeo) sin bloquear el dashboard.
- *Given* un incidente WARN, *when* aparece, *then* hay sonido + parpadeo pero **sin** efecto de parpadeo continuo reservado a CRITICAL.

**Trazabilidad:** RF-24, RF-25 / UC1, UC3 / RT1, RT3

---

### US-04 — Ver panel de discrepancias de estado (UC6)

**Como** Operador,
**quiero** un panel dedicado a pedidos con estado incorrecto en origen,
**para** detectar problemas de calidad de datos sin que generen alertas WARN/CRITICAL ruidosas.

**Criterios de aceptación:**

- *Given* hay pedidos con estado incorrecto detectados por el chequeo, *when* abro el panel de discrepancias, *then* veo la lista con identificador del pedido, estado encontrado y timestamp de la detección.
- *Given* aparecen discrepancias, *when* se registran, *then* **no** se genera notificación de navegador (UC6 es solo visual, sin push).
- *Given* un pedido cancelado correctamente, *when* el chequeo lo evalúa, *then* **no** aparece en el panel de discrepancias.

**Trazabilidad:** RF-23 / UC6 / RT5, RT7

---

### US-05 — Persistencia de alertas activas al recargar el dashboard

**Como** Operador,
**quiero** que al cerrar y reabrir el dashboard las alertas activas sigan visibles,
**para** no perder contexto tras un reinicio del navegador o de mi sesión.

**Criterios de aceptación:**

- *Given* hay incidentes WARN/CRITICAL activos, *when* cierro el navegador y vuelvo a abrir el dashboard, *then* los mismos incidentes siguen visibles con su estado original.
- *Given* un incidente fue cerrado (auto o manual) antes del reinicio, *when* reabro el dashboard, *then* el incidente aparece en el histórico pero no en la vista real-time.

**Trazabilidad:** RF-22 / UC2 / RT-Persist

---

### US-30 — Ver tablero de estado por marca con semáforo

**Como** Operador,
**quiero** ver el tablero de estado por marca con indicador semáforo (🟢🟡🔴) para cada site (Patprimo, SevenSeven, Atmos, Ostu),
**para** identificar rápidamente qué marca tiene pedidos pendientes acumulados sin necesidad de revisar el histórico de incidentes.

**Criterios de aceptación:**

- *Given* que estoy autenticado como Operador y navego a Brand Monitor, *when* la página carga, *then* veo una tabla con los 4 sites (Patprimo, SevenSeven, Atmos, Ostu), recuento actual de pendientes, recuento de hace 10 min, cambio neto y semáforo por site.
- *Given* que el recuento de pendientes bajó >= umbral configurable (`PendingDropThreshold`), *when* veo el site, *then* el semáforo muestra 🟢 — pedidos se están procesando al ritmo esperado.
- *Given* que el recuento bajó pero menos que el umbral, *when* veo el site, *then* el semáforo muestra 🟡 — bajó pero lentamente.
- *Given* que el recuento es igual o subió, *when* veo el site, *then* el semáforo muestra 🔴 — acumulación que requiere revisión.
- *Given* que el chequeo de Brand Monitor falla para un site (API no responde), *when* veo la fila, *then* el semáforo muestra 🔴 y el dato indica error **sin** afectar la visualización de los otros tres sites.

> **Nota:** El umbral `PendingDropThreshold` se configura desde la UI de reglas (US-19 / US-20) bajo módulo `BrandMonitor`. La actualización es automática cada 10 min — sin acción manual del Operador.

**Trazabilidad:** RF-31, RF-03 / — / —

---

## 1.2 Detectar

### US-06 — Recibir alerta con los 6 campos en español

**Como** Operador,
**quiero** que cada alerta incluya **qué_pasó / cuándo / dónde / severidad / causa_probable / acción_sugerida** en lenguaje natural en español,
**para** entender el incidente y decidir acción en <30 segundos sin consultar al técnico.

**Criterios de aceptación:**

- *Given* el sistema detecta un incidente, *when* genera la alerta, *then* la alerta contiene los **6 campos** especificados, todos no vacíos, en español, sin JSON crudo ni códigos HTTP en `qué_pasó`.
- *Given* una alerta, *when* la veo, *then* la severidad es exactamente uno de `INFO` / `WARN` / `CRITICAL` (RF-12).
- *Given* el texto de los campos, *when* el sistema los genera, *then* provienen de plantillas configurables (RF-13) — no de concatenación ad-hoc — garantizando consistencia entre alertas.

**Trazabilidad:** RF-11, RF-12, RF-13 / UC1, UC3, UC4 / RT1, RT2, RT3

---

### US-07 — Detectar ausencia de pedidos automáticamente

**Como** Operador,
**quiero** que el sistema detecte automáticamente cuando no hay pedidos esperados en BD durante una ventana,
**para** enterarme proactivamente sin tener que ejecutar consultas SQL manuales.

**Criterios de aceptación:**

- *Given* la cadencia de chequeo BD configurada en 5 minutos, *when* transcurre el intervalo, *then* el sistema ejecuta el chequeo y registra el resultado.
- *Given* no hay pedidos esperados en la ventana definida por las reglas de M6, *when* el chequeo concluye, *then* M7 ejecuta clasificación en cascada (`bd` → `job` → `api` → `token` → `data_quality` → `no_determinada`) y genera incidente CRITICAL.
- *Given* hay pedidos cancelados correctamente en la ventana, *when* el chequeo evalúa, *then* **no** se genera alerta (RT7 — los cancelados se ignoran).

**Trazabilidad:** RF-01, RF-02, RF-08, RF-09 / UC1 / RT3, RT7

---

### US-08 — Detectar falla de health-check de BD

**Como** Operador,
**quiero** que el sistema verifique cada 5 minutos que la BD responde a `SELECT 1` y alerte CRITICAL si falla,
**para** detectar caídas de BD en <10 minutos sin depender de mi inspección.

**Criterios de aceptación:**

- *Given* la cadencia de health-check configurada en 5 min, *when* transcurre el intervalo, *then* M4 ejecuta `SELECT 1` y registra latencia + disponibilidad.
- *Given* el motor de BD no responde o timeout, *when* M4 lo detecta, *then* se emite incidente CRITICAL en <10 min desde la falla real (RNF-01).
- *Given* la latencia es alta pero la BD responde, *when* M4 lo detecta, *then* se emite incidente WARN — sin reintento automático y antes del estado CRITICAL (R10).

**Trazabilidad:** RF-04, RNF-01 / UC7 / RT3

---

### US-09 — Detectar falla de job de integración

**Como** Operador,
**quiero** que el sistema verifique cada 5 min el estado de los jobs y alerte si están detenidos,
**para** enterarme antes de que se acumulen pedidos pendientes sin descargar.

**Criterios de aceptación:**

- *Given* la cadencia de verificación de jobs configurada en 5 min, *when* transcurre el intervalo, *then* M11 consulta el estado del job y lo registra **sin** intentar reiniciarlo (Decisión #2).
- *Given* el job está detenido o falló su última ejecución, *when* M11 lo detecta, *then* se emite incidente CRITICAL con `causa_probable = job` en <10 min.

**Trazabilidad:** RF-05, RF-08 / UC1 / RT1

---

### US-10 — Detectar token expirado (401) sin renovación automática

**Como** Operador,
**quiero** que si una API externa devuelve 401, reciba una alerta CRITICAL con la sugerencia "Renovar token según SOP-001", sin que el sistema intente renovación automática,
**para** ejecutar la acción manual correcta sin riesgos de duplicación de credenciales.

**Criterios de aceptación:**

- *Given* la cadencia de chequeo de APIs en 10 min, *when* la API responde HTTP 401, *then* M3 **no** ejecuta reintentos (porque solo aplican a 5xx/timeout, RF-06) y M7 clasifica `causa_probable = token`.
- *Given* el incidente clasificado como `token`, *when* M10 genera la alerta, *then* el campo `acción_sugerida` contiene literalmente la referencia a `SOP-001` (renovación manual).
- *Given* la alerta es emitida, *when* el sistema actúa, *then* **no** se intenta renovación de token automáticamente y se registra el incidente como pendiente de acción humana.

**Trazabilidad:** RF-03, RF-07, RF-08, RF-11 / UC4 / RT2

---

## 1.3 Investigar

### US-11 — Consultar histórico de incidentes con filtros

**Como** Operador,
**quiero** consultar el historial de incidentes filtrable por rango de fechas, severidad y módulo,
**para** investigar patrones y revisar incidentes pasados durante calibración o auditoría.

**Criterios de aceptación:**

- *Given* el dashboard tiene una vista histórica, *when* aplico filtros (rango de fechas + severidad + módulo), *then* veo los incidentes que cumplen los criterios con paginación si exceden el tamaño de pantalla.
- *Given* la retención configurada en 90 días (RF-21), *when* consulto un rango dentro de esa ventana, *then* obtengo resultados; *when* consulto fuera (>90 días) *then* la vista indica claramente que esos datos ya no están disponibles.
- *Given* todo incidente queda registrado desde día 0 (RF-18), *when* consulto el histórico, *then* **ningún** incidente activo o pasado puede haber omitido el registro.

**Trazabilidad:** RF-18, RF-21, RF-23 / UC5 / —

---

### US-12 — Ver resumen semanal de calibración

**Como** Operador,
**quiero** un resumen semanal con conteo de incidentes agrupados por causa y severidad,
**para** preparar la reunión de calibración del lunes con datos a la mano.

**Criterios de aceptación:**

- *Given* abro la vista de resumen semanal el lunes 9 am, *when* selecciono la semana anterior, *then* veo un conteo agregado de incidentes por las 6 categorías de causa (`token`, `api`, `job`, `bd`, `data_quality`, `no_determinada`) y por las 3 severidades.
- *Given* hay incidentes `causa_no_determinada` en la semana, *when* veo el resumen, *then* aparecen destacados como candidatos a regla nueva (UC1 + Journey 2 + R9 mitigation).

**Trazabilidad:** RF-23 / UC5 / —

---

### US-13 — Cerrar incidente manualmente con comentario obligatorio

**Como** Operador,
**quiero** cerrar manualmente un incidente WARN o CRITICAL con un comentario obligatorio de resolución,
**para** registrar lo aprendido del caso aunque el cierre automático aún no haya disparado.

**Criterios de aceptación:**

- *Given* un incidente WARN o CRITICAL activo, *when* hago clic en "Cerrar manualmente", *then* el sistema me pide un `comentario_resolucion` obligatorio.
- *Given* envío el formulario con comentario vacío, *when* intento confirmar, *then* el cierre **no** se ejecuta y la UI me indica que el comentario es obligatorio.
- *Given* el cierre manual confirmado con comentario, *when* el sistema lo registra, *then* el incidente queda cerrado con autor (yo), timestamp y el comentario en el historial.

**Trazabilidad:** RF-19, RF-20 / — / —

---

### US-14 — Cierre automático tras 2 chequeos OK consecutivos

**Como** Operador,
**quiero** que un incidente WARN o CRITICAL se cierre automáticamente cuando dos chequeos consecutivos del módulo afectado regresan OK,
**para** no acumular incidentes ya resueltos en la vista real-time sin que tenga que hacerlo manualmente.

**Criterios de aceptación:**

- *Given* un incidente WARN/CRITICAL activo, *when* el módulo afectado regresa **dos** chequeos consecutivos en estado OK, *then* el sistema cierra automáticamente el incidente y registra el evento como `cierre_automatico`.
- *Given* solo **un** chequeo OK seguido de un chequeo no-OK, *when* esto ocurre, *then* el incidente **no** se cierra (mitigación de flapping).
- *Given* un cierre automático ocurre, *when* lo busco en el histórico, *then* aparece con autor `sistema`, timestamp del segundo OK y sin `comentario_resolucion` (queda diferenciado del cierre manual de US-13).

**Trazabilidad:** RF-19 / — / —

---

# 2. Técnico / Responsable técnico

> **Recordatorio:** el Técnico hereda **todas** las stories del Operador (US-01..US-14) por RF-27. Las stories de esta sección son **exclusivas del rol Técnico**.

## 2.1 Monitorear

### US-15 — Acceder al dashboard con rol Técnico (escalamientos)

**Como** Técnico,
**quiero** acceder al dashboard con todos los permisos del Operador más los míos,
**para** no tener que cambiar de cuenta entre rutina de monitoreo y mantenimiento de reglas.

**Criterios de aceptación:**

- *Given* mi cuenta tiene rol `Técnico`, *when* hago login, *then* veo todas las vistas disponibles para Operador **más** la sección de gestión de reglas y la opción de exportar logs técnicos.
- *Given* mi cuenta tiene rol `Técnico`, *when* intento acceder a edición de reglas, *then* la UI me lo permite (RF-17 + RNF-11 — authorization middleware).
- *Given* una cuenta con rol `Operador`, *when* intenta acceder a edición de reglas, *then* la respuesta del servidor es 403 Forbidden — la restricción se valida server-side, no solo en UI (SECURITY-08).

**Trazabilidad:** RF-26, RF-27, RF-29 / UC3 / —

---

## 2.3 Investigar

### US-16 — Ver detalle de auto_reintentos asociados a una alerta API

**Como** Técnico,
**quiero** ver el detalle de los auto_reintentos asociados a una alerta CRITICAL de API (cuántos, qué código HTTP en cada intento, latencia),
**para** entender si el reintento limitado funcionó como esperado o si hay un patrón más profundo.

**Criterios de aceptación:**

- *Given* un incidente API con uno o más auto_reintentos registrados (RF-06), *when* abro el detalle del incidente, *then* veo cronológicamente cada `auto_reintento` con: timestamp, código HTTP, latencia.
- *Given* el incidente fue clasificado por 5xx/timeout, *when* veo el detalle, *then* **no** aparecen reintentos de tokens ni de jobs (Decisión #2 — automatización limitada).
- *Given* el rol del usuario es `Operador`, *when* abre el detalle, *then* ve los auto_reintentos pero **no** ve la pestaña de "Logs técnicos" (que es exclusiva del Técnico, US-17).

**Trazabilidad:** RF-06, RF-27 / UC1, UC4 / —

---

### US-17 — Consultar y exportar logs técnicos

**Como** Técnico,
**quiero** consultar y exportar logs técnicos desde el dashboard (módulos, latencias, códigos HTTP),
**para** diagnosticar incidentes que requieren más profundidad que la alerta resumida sin tener que entrar al servidor.

**Criterios de aceptación:**

- *Given* mi rol `Técnico`, *when* abro la sección "Logs técnicos", *then* veo los eventos registrados (filtrados por fecha y módulo) con timestamp + request_id + log_level + mensaje (RNF-08).
- *Given* aplico un filtro, *when* solicito exportar, *then* el dashboard descarga el resultado en formato CSV o JSON.
- *Given* los logs visibles, *when* los reviso, *then* **no** contienen contraseñas, tokens ni PII (RNF-08 + SECURITY-03).

**Trazabilidad:** RF-23, RF-27, RNF-08 / UC3 / —

---

### US-18 — Recibir aviso de incidentes "causa_no_determinada"

**Como** Técnico,
**quiero** recibir un aviso cuando aparezca un incidente clasificado como `causa_no_determinada`, marcado como candidato a regla nueva,
**para** poder crear una regla en M6 que cubra el caso en el futuro (mitigación R9).

**Criterios de aceptación:**

- *Given* M7 evalúa la cascada de clasificación, *when* ninguna categoría aplica, *then* el incidente queda marcado `causa_no_determinada` y como `candidato_regla_nueva`.
- *Given* aparece un `causa_no_determinada`, *when* abro la vista "Candidatos a regla", *then* veo el incidente con los datos brutos relevantes (módulo, snapshot del estado del sistema en ese momento).
- *Given* yo (Técnico) cierro el incidente y creo una regla nueva (US-19), *when* esto ocurre, *then* el candidato queda enlazado a la regla creada en su historial.

**Trazabilidad:** RF-08, RF-10 / Journey 2 / —

---

## 2.4 Configurar

### US-19 — Crear regla estática nueva con autor + razón obligatorios

**Como** Técnico,
**quiero** crear una regla estática nueva desde una UI dedicada en el dashboard, con autor y razón obligatorios,
**para** incorporar conocimiento del Failbook al sistema sin depender de SSMS ni de scripts.

**Criterios de aceptación:**

- *Given* mi rol `Técnico`, *when* hago clic en "Crear regla", *then* veo un formulario con campos para nombre, descripción, condición (qué evalúa), módulo afectado, severidad asociada, y `razon_cambio` obligatorio.
- *Given* el formulario completado, *when* lo envío, *then* la regla queda creada y aparece en el historial de cambios como evento `creacion` con autor (yo), timestamp, razón.
- *Given* envío el formulario sin `razon_cambio`, *when* intento confirmar, *then* la UI rechaza el envío y me indica que la razón es obligatoria (P5).

**Trazabilidad:** RF-14, RF-15, RF-17, RF-31 / UC5, Journey 2 / —

> **Nota v1.1:** incluye creación de regla para módulo `BrandMonitor` con campo `PendingDropThreshold` (RF-31).

---

### US-20 — Editar regla existente con registro de diff

**Como** Técnico,
**quiero** editar una regla existente y registrar el motivo del cambio,
**para** calibrar el sistema sin perder trazabilidad histórica.

**Criterios de aceptación:**

- *Given* una regla existente, *when* hago clic en "Editar", *then* veo los valores actuales editables y un campo `razon_cambio` obligatorio.
- *Given* envío la edición con razón, *when* el sistema la registra, *then* en el historial de la regla queda un evento `edicion` con autor, timestamp, razón y diff antes/después.
- *Given* la regla está siendo evaluada por el scheduler, *when* la edito, *then* la próxima evaluación (siguiente cadencia) usa la versión nueva — no la cacheada.

**Trazabilidad:** RF-14, RF-15, RF-31 / UC5 / —

---

### US-21 — Activar / desactivar regla sin eliminarla

**Como** Técnico,
**quiero** activar o desactivar una regla sin eliminarla,
**para** probar cambios temporales antes de descartar definitivamente una regla con valor histórico.

**Criterios de aceptación:**

- *Given* una regla activa, *when* hago clic en "Desactivar", *then* la regla queda marcada como inactiva y el scheduler deja de evaluarla en la próxima cadencia — pero su historial se conserva.
- *Given* una regla inactiva, *when* la activo de nuevo, *then* vuelve a ser evaluada por el scheduler en la siguiente cadencia.
- *Given* cada cambio de estado (activar/desactivar), *when* ocurre, *then* queda en el historial con autor, timestamp y `razon_cambio` opcional para esta acción específica.

**Trazabilidad:** RF-14, RF-15 / UC5 / —

---

### US-22 — Consultar historial completo de cambios de una regla

**Como** Técnico,
**quiero** ver el historial completo de cambios de una regla (autor, fecha, razón, diff antes/después),
**para** auditar la evolución de la calibración y entender por qué un threshold quedó como está.

**Criterios de aceptación:**

- *Given* una regla con historial, *when* abro su detalle, *then* veo los eventos en orden cronológico (más reciente primero) con: tipo de evento (`creacion`, `edicion`, `activacion`, `desactivacion`), autor, timestamp, razón, diff antes/después.
- *Given* el historial visible, *when* solicito exportar, *then* obtengo el listado en CSV.
- *Given* el sistema cumple SECURITY-13 (data integrity), *when* miro un evento del historial, *then* es **inmutable** — no existe acción de UI para editar ni borrar eventos pasados del historial.

**Trazabilidad:** RF-15, RF-16 / UC5 / —

---

# 3. Stories de calidad y operación (cross-cutting)

> Estas stories cubren los **15 RNFs** y las **13 reglas SECURITY aplicables** (mapeo en [`requirements.md`](../requirements/requirements.md) §4.3). Aparecen como autor IT/Seguridad o Sponsor según corresponde (ver `personas.md`).

### US-23 — Selección de identidad + sesiones seguras

**Como** IT/Seguridad,
**quiero** que toda ruta del dashboard requiera selección de identidad previa y que las sesiones usen cookies `HttpOnly` + `SameSite=Strict` + `Secure` (cuando HTTPS),
**para** separar operaciones por rol y prevenir robo de sesión en la red interna.

**Criterios de aceptación:**

- *Given* el dashboard sin selección de identidad activa, *when* un usuario accede a cualquier ruta protegida, *then* es redirigido a la pantalla de selección de identidad (deny-by-default, SECURITY-08, RF-26).
- *Given* la pantalla de selección, *when* el usuario pulsa "Analista Operativo", *then* el servidor emite cookie de sesión con claims `Name=Analista Operativo, Role=Operador`; atributos `HttpOnly=true`, `SameSite=Strict`, `Secure=true` cuando HTTPS (RF-29, RNF-14).
- *Given* la pantalla de selección, *when* el usuario pulsa "Responsable Técnico", *then* el servidor emite cookie con claims `Name=Responsable Técnico, Role=Técnico`; mismos atributos de seguridad.
- *Given* una sesión activa, *when* el usuario pulsa "Salir", *then* `SignOutAsync()` invalida la cookie y redirige a la pantalla de selección.
- *Given* una cookie de sesión robada sin `SameSite=Strict`, *when* se intenta usar desde otro origen, *then* el navegador no la envía (SECURITY-08).

**Trazabilidad:** RF-26, RF-28, RF-29, RNF-11, RNF-14 / — / —

---

### US-24 — Separación de acceso por rol (Técnico vs Operador)

**Como** IT/Seguridad,
**quiero** que las funciones de gestión de reglas y logs técnicos estén restringidas al rol `Técnico` en el servidor,
**para** garantizar que el rol `Operador` no pueda alterar reglas ni acceder a información técnica sensible.

**Criterios de aceptación:**

- *Given* una sesión activa con rol `Operador`, *when* se intenta acceder a `/reglas` o `/logs` directamente (URL manual), *then* el servidor retorna HTTP 403 (no 404, no redirect al dashboard) y el evento queda registrado en logs (SECURITY-08).
- *Given* una sesión activa con rol `Técnico`, *when* accede a `/reglas` o `/logs`, *then* puede ver y operar todas las funciones de su rol sin restricción.
- *Given* el layout del dashboard con sesión `Operador`, *when* se renderiza la navegación, *then* los ítems "Reglas" y "Logs Técnicos" no aparecen en el menú (defensa en profundidad — el server igualmente retorna 403).
- *Given* ningún rol fuera de `Operador` y `Técnico`, *when* se intenta emitir una cookie con rol diferente, *then* la solicitud es rechazada (sin rol válido no hay sesión).

**Trazabilidad:** RF-27, RNF-11, SECURITY-08 / — / —

---

### US-25 — Logging estructurado sin PII ni secretos

**Como** IT/Seguridad,
**quiero** que el logging del dashboard incluya `timestamp + request_id + log_level + mensaje` y nunca contenga contraseñas, tokens ni PII,
**para** auditar comportamiento sin filtrar datos sensibles.

**Criterios de aceptación:**

- *Given* la app configurada con Serilog (o equivalente) + sink local rotado, *when* cualquier endpoint procesa una request, *then* se loggea con los 4 campos obligatorios + correlación.
- *Given* un usuario envía contraseña o token, *when* el log captura la request, *then* el contenido sensible queda redactado o excluido del log (filtros explícitos por nombre de campo).
- *Given* la retención de logs, *when* el sistema rota archivos, *then* mantiene los últimos 90 días disponibles (alineado con RF-21 y SECURITY-14).

**Trazabilidad:** RNF-08 / — / —

---

### US-26 — Cifrado at-rest + transporte cifrado a la BD

**Como** IT/Seguridad,
**quiero** cifrado at-rest en SQL Server MonitorPedidosDb (172.16.0.41) y canal cifrado al motor de BD,
**para** proteger datos sensibles (credenciales hasheadas, incidentes, historial de reglas) en disco y en tránsito.

**Criterios de aceptación:**

- *Given* SQL Server MonitorPedidosDb (172.16.0.41) / Express, *when* la BD se aprovisiona, *then* el archivo de datos está cifrado (TDE o cifrado de archivo del usuario) según las opciones disponibles del motor.
- *Given* la app abre conexión a la BD, *when* la connection string se evalúa, *then* incluye TLS / canal seguro habilitado (`Encrypt=True`).
- *Given* el dashboard se sirve por red interna del equipo, *when* se accede desde otro equipo, *then* la transmisión usa HTTPS con certificado de desarrollo de ASP.NET Core (RNF-07 escenario b).

**Trazabilidad:** RNF-06, RNF-07 / — / —

---

### US-27 — Hardening: errores genéricos en producción + sin defaults

**Como** IT/Seguridad,
**quiero** que los errores de producción devuelvan mensajes genéricos sin stack traces ni detalles del framework, y que no existan credenciales por defecto en la configuración,
**para** no exponer información que pueda asistir a un atacante.

**Criterios de aceptación:**

- *Given* la app en modo Production, *when* un endpoint lanza una excepción no manejada, *then* el global exception handler la loggea (RNF-08) y responde con un mensaje genérico al usuario, sin stack trace.
- *Given* la app desplegada, *when* se revisa la configuración, *then* **no** existen credenciales por defecto, ni endpoints de muestra (`/swagger` solo en Development), ni directory listing habilitado.
- *Given* la BD y los servicios externos, *when* el sistema arranca, *then* las credenciales provienen de User Secrets / variables de entorno locales — nunca de archivos versionados.

**Trazabilidad:** RNF-12, RNF-15 / — / —

---

### US-28 — Validación de entrada + headers HTTP de seguridad

**Como** IT/Seguridad,
**quiero** que todos los endpoints validen tipo, longitud y formato de los inputs, y que el dashboard emita headers de seguridad estándar,
**para** defensa en profundidad contra XSS, clickjacking, MIME-sniffing y SQL injection.

**Criterios de aceptación:**

- *Given* cualquier endpoint que reciba input del usuario, *when* el modelo se vincula, *then* se aplican validaciones de tipo + longitud máxima + formato (DataAnnotations / FluentValidation) y se rechazan inputs inválidos antes de tocar la BD.
- *Given* toda consulta a la BD, *when* se construye, *then* usa parámetros (Dapper / EF Core) — nunca concatenación SQL — para prevenir injection (SECURITY-05).
- *Given* el dashboard responde con HTML, *when* se inspeccionan los headers, *then* incluye: `Content-Security-Policy: default-src 'self'`, `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`. `Strict-Transport-Security` se incluye **solo** cuando se sirve por HTTPS interno (RNF-07 escenario b).

**Trazabilidad:** RNF-09, RNF-10 / — / —

---

### US-29 — Soporte de 5 usuarios concurrentes internos

**Como** Sponsor,
**quiero** que el dashboard soporte 5 usuarios concurrentes internos sin degradación funcional perceptible,
**para** que sponsor + analista + técnico + 2 invitados internos puedan participar en demos semanales sin lag.

**Criterios de aceptación:**

- *Given* 5 usuarios autenticados navegan simultáneamente, *when* todos cargan la vista real-time, *then* el tiempo de respuesta p95 sigue siendo <2 s en localhost (RNF-03).
- *Given* el escenario (b) de RNF-07 (acceso por red interna con HTTPS), *when* se replica el escenario anterior con los 5 usuarios desde equipos distintos, *then* la funcionalidad se mantiene sin errores.

**Trazabilidad:** RF-30, RNF-03, RNF-04 / — / —

---

# 4. Matriz de cobertura (validación INVEST + trazabilidad)

## 4.1 Cobertura RF → Story

| RF | Cubierto en |
|----|-------------|
| RF-01 Scheduler | US-07, US-08, US-09, US-10 (implícito en cada detección) |
| RF-02 Chequeo BD 5min | US-07 |
| RF-03 APIs 10min | US-10 |
| RF-04 Health BD 5min | US-08 |
| RF-05 Jobs 5min | US-09 |
| RF-06 Reintentos 5xx/timeout | US-16 |
| RF-07 401 sin renovación | US-10 |
| RF-08 Clasificación 6 categorías | US-07, US-09, US-10, US-18 |
| RF-09 Cascada bd→job→api→token→data_quality→nd | US-07 |
| RF-10 `no_determinada` → candidato | US-18 |
| RF-11 6 campos en alerta | US-06 |
| RF-12 3 severidades | US-06 |
| RF-13 Plantillas | US-06 |
| RF-14 CRUD reglas vía UI | US-19, US-20, US-21 |
| RF-15 Historial con autor+razón | US-19, US-20, US-21, US-22 |
| RF-16 Historial consultable y exportable | US-22 |
| RF-17 Edición de reglas solo `Técnico` | US-15, US-19 |
| RF-18 Registro desde día 0 | US-11 |
| RF-19 Cierre automático 2 OK | US-14 |
| RF-20 Cierre manual con comentario | US-13 |
| RF-21 Retención 90 días | US-11 |
| RF-22 Persistencia al recargar | US-05 |
| RF-23 4 vistas (real-time, histórico, semanal, discrepancias) | US-01, US-04, US-11, US-12, US-17 |
| RF-24 Push browser + modo NOC | US-02, US-03 |
| RF-25 Fallback Notification API | US-03 |
| RF-26 Selección de identidad en todas las rutas | US-15, US-23 |
| RF-27 2 roles (Operador / Técnico) | US-15, US-16, US-17, US-24 |
| RF-28 2 identidades pre-definidas (sin cuentas en BD) | US-23 |
| RF-29 Cookie de sesión ASP.NET Core | US-23 |
| RF-30 5 usuarios concurrentes | US-29 |
| RF-31 Vista Brand Monitor con semáforo por site | US-30 (vista Operador); US-19, US-20 (configuración umbral por Técnico) |

✅ **31/31 RFs cubiertos.**

## 4.2 Cobertura UC → Story

| UC | Cubierto en |
|----|-------------|
| UC1 Ausencia de pedidos | US-06, US-07, US-09, US-16 |
| UC2 Revisión proactiva | US-01, US-02, US-05 |
| UC3 Escalamiento | US-03, US-06, US-15, US-17 |
| UC4 Token expirado | US-06, US-10, US-16 |
| UC5 Revisión semanal / calibración | US-11, US-12, US-19, US-20, US-21, US-22 |
| UC6 Discrepancias de estado | US-04 |
| UC7 Health check BD | US-08 |
| Journey 1 (token sin automatización) | US-10 |
| Journey 2 (causa no determinada) | US-18, US-19 |

✅ **7/7 UCs + 2/2 Journeys cubiertos.**

## 4.3 Cobertura Red-Teaming → Story

| RT | Cubierto en |
|----|-------------|
| RT1 Apagar job Salesforce | US-03, US-06, US-09 |
| RT2 Revocar token Salesforce | US-06, US-10 |
| RT3 Apagar SQL Server | US-03, US-06, US-07, US-08 |
| RT5 Pedido con estado incorrecto | US-04 |
| RT7 Pedido cancelado correctamente ignorado | US-04, US-07 |
| RT-Persist Cerrar y reabrir dashboard | US-01, US-05 |

✅ **6/6 escenarios red-teaming cubiertos.**

## 4.4 Validación INVEST

| Criterio | Estado | Notas |
|----------|--------|-------|
| **Independent** | ✅ | Cada story implementable sin depender estrictamente de otra (con dependencias técnicas razonables — p. ej., US-19 depende de la BD y autenticación funcionando). |
| **Negotiable** | ✅ | Criterios Given/When/Then son revisables sin reescribir la story. |
| **Valuable** | ✅ | Cada story entrega valor identificable a su persona o stakeholder. |
| **Estimable** | ✅ | Granularidad 1–3 días/story (decisión Q2). |
| **Small** | ✅ | Stories pequeñas, ninguna abarca más de una capability dentro de su sub-sección. |
| **Testable** | ✅ | Todos los criterios usan Given/When/Then, directamente ejecutables como tests de aceptación. |

---

# 5. Resumen ejecutivo

- **30 user stories** en total: **15 Operador** (US-01..US-14, US-30) + **8 Técnico exclusivas** (US-15..US-22) + **7 cross-cutting** (US-23..US-29).
- **Cobertura completa** de los 31 RFs (incluye RF-31 BrandMonitor), 7 UCs, 2 Journeys y 6 escenarios de red-teaming.
- **Formato consistente:** Connextra + Given/When/Then + tabla de trazabilidad.
- **v1.1 (2026-05-24):** agrega US-30 (vista BrandMonitor Operador); US-19/US-20 actualizadas con RF-31 (configuración de umbral BrandMonitor por Técnico).
