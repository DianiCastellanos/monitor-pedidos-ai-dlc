# Personas — MonitorPedidos AI

**Fecha:** 2026-05-21
**Versión:** 1.0
**Stage:** Inception → User Stories (Part 2 — Generation)
**Decisión Q1 del plan:** 2 personas con stories propias + 2 stakeholders documentados sin stories.
**Fuentes:** [`prd.md`](../../../prd.md) v2.3 §3 + [`requirements.md`](../requirements/requirements.md) v1.1 §3.7, RF-26..RF-30.

---

## Cómo se leen estas personas

- Las **2 primeras personas** son los usuarios directos del dashboard. Cada user story de [`stories.md`](./stories.md) se asigna a una de ellas (o a las dos cuando es compartida).
- Los **2 stakeholders al final** no usan el dashboard de forma rutinaria pero imponen restricciones de diseño (sponsor → prioridades de demo y métricas; IT/Seguridad → reglas SECURITY). Aparecen como autor de algunas stories de **calidad y operación** (cross-cutting) en `stories.md` cuando la regla SECURITY que motiva la story proviene claramente de esa expectativa.

---

## P1 — Operador (Analista operativo)

| Atributo | Valor |
|----------|-------|
| **Rol en el sistema** | `Operador` (RF-27) — permisos: ver dashboard, cerrar incidentes manualmente con comentario, ver historial y resumen semanal. |
| **Rol organizacional** | Analista operativo del proceso de descarga de pedidos. |
| **Frecuencia de uso** | Diaria, alta — mantiene el dashboard abierto la mayor parte de la jornada (modo NOC). |

### Objetivos

- **O1.** Saber en <10 segundos si hay algún incidente activo al iniciar la jornada.
- **O2.** Recibir alertas accionables (con causa probable + acción sugerida) sin necesidad de escalar al técnico para casos conocidos.
- **O3.** Reducir su tiempo de validación manual de ~25 h/mes a <10 h/mes (KPI O2 del PRD).
- **O4.** Cerrar incidentes resueltos con un comentario que sirva al equipo en el futuro.

### Contexto operativo

- Trabaja en oficina, con un equipo dedicado a operación de la integración Salesforce/Multivende → BD interna.
- El dashboard corre en su equipo local o en una máquina dentro de la red interna (PRD §9, Decisión #8).
- Usa el navegador (Chrome/Edge) en pantalla completa (modo NOC, RF-24) — sin exposición a internet.
- Es el primer punto de contacto cuando algo falla; escala al responsable técnico solo cuando la acción sugerida requiere intervención técnica.

### Dolores actuales (pre-MVP)

- Pasa **25 h/mes** corriendo SQL manuales + Postman para validar que los pedidos están bajando.
- Detección de fallos toma **1–2 horas** porque depende de su inspección.
- Cuando algo falla, depende del responsable técnico para diagnóstico (conocimiento tácito, dolor D4 del PRD §2).
- No tiene historial centralizado de incidentes (D5).

### Métricas de éxito personales

- Reducción de tiempo dedicado a inspección manual ↓ ≥60%.
- Recibir alertas en <10 min desde que ocurre el fallo (RNF-01).
- Entender la causa probable de cada alerta sin consultar al técnico para casos del Failbook.

### Herramientas que usa actualmente

- SQL Server Management Studio (consultas manuales).
- Postman (verificar APIs Salesforce/Multivende).
- Correo + chat interno (escalamientos).

### Conocimiento técnico

- **Medio.** Sabe leer SQL básico, ejecutar queries de validación, usar Postman para consultas REST simples. **No** edita reglas ni interpreta stack traces.

### Tono apropiado en el dashboard

- **Lenguaje natural en español**, sin jerga técnica innecesaria. Las alertas deben ser comprensibles sin diccionario de códigos HTTP ni de errores SQL (RNF-08 + P1 del PRD).

---

## P2 — Técnico (Responsable técnico)

| Atributo | Valor |
|----------|-------|
| **Rol en el sistema** | `Técnico` (RF-27) — todos los permisos del Operador **más**: CRUD de reglas estáticas, ver historial de cambios de reglas, ver/exportar logs técnicos. |
| **Rol organizacional** | Responsable técnico del proceso de descarga de pedidos y custodio del conocimiento tácito sobre fallas. |
| **Frecuencia de uso** | Media — entra al dashboard cuando hay escalamientos (UC3), durante la calibración semanal (UC5), o cuando aparece una causa `no_determinada` (UC1 + Journey 2). |

### Objetivos

- **O1.** Eliminar la dependencia tácita: convertir su conocimiento informal en reglas explícitas (P5 del PRD).
- **O2.** Atender solo escalamientos cualificados (con contexto de la causa probable + reintentos + logs técnicos).
- **O3.** Mantener calibradas las reglas estáticas a partir del aprendizaje semanal (UC5).
- **O4.** Documentar cada `causa_no_determinada` como regla nueva para futuras detecciones (R9 mitigation).

### Contexto operativo

- Acceso al dashboard desde su equipo dentro de la red interna del equipo.
- Combina el dashboard con SSMS y logs locales para diagnósticos profundos.
- Es responsable de mantener el Failbook (documento externo, [`requirements.md`](../requirements/requirements.md) C-08) sincronizado con las reglas activas.

### Dolores actuales (pre-MVP)

- **Concentración de conocimiento** — único en la organización que sabe diagnosticar fallos de tokens, jobs, integraciones.
- Recibe **escalamientos sin contexto** (correos con "no entran pedidos, ¿qué hago?").
- **Falta de trazabilidad** de cambios pasados: no recuerda por qué decidió un threshold hace 3 meses.
- Carga de interrupciones constantes que rompen su trabajo planificado.

### Métricas de éxito personales

- **Escalamientos con contexto** — cada alerta CRITICAL escalada incluye causa probable, módulo, intentos automáticos, sugerencia.
- **Cobertura del clasificador** ↑: <10% de incidentes terminan como `causa_no_determinada` después de Sprint 4 (R9).
- Historial de reglas auditable con autor + razón (P5).

### Herramientas que usa actualmente

- SQL Server Management Studio (queries profundas, performance).
- Postman + Salesforce CLI.
- Logs locales de sus jobs.
- Conversaciones ad-hoc en chat interno (registro disperso, D5).

### Conocimiento técnico

- **Alto.** Lee y escribe SQL avanzado, depura llamadas HTTP, entiende códigos HTTP, conoce el dominio Salesforce/Multivende y los flujos de tokens.

### Tono apropiado en el dashboard

- **Mixto.** Las alertas mantienen los 6 campos en español (P1) — comparte vocabulario con el Operador — pero el Técnico **también** quiere ver los detalles técnicos (códigos HTTP, número de reintentos, latencias) en una vista expandible o pestaña dedicada (UC3 escalamiento, US-16/US-17).

---

## Stakeholders documentados (sin stories funcionales propias)

Estos perfiles **no usan el dashboard a diario** pero su expectativa moldea el diseño. Aparecen como autor de algunas stories cross-cutting en `stories.md` §3 cuando la regla SECURITY o el RNF está motivado por su responsabilidad.

### S1 — Sponsor (Alex Cárdenas, Jefe de Análisis de Sistemas)

| Atributo | Valor |
|----------|-------|
| **Rol** | Dueño del proceso de descarga de pedidos; refuerza adopción. |
| **Expectativa del MVP** | Reducción medible (≥10 h/mes recuperadas → North Star) + activo propio (no SaaS recurrente). |
| **Restricción que impone al diseño** | Demos semanales internas durante el cronograma de 4 sprints (PRD §13). Visibilidad clara del progreso (R2 mitigation). |
| **Aparece como autor en** | US-29 (concurrencia 5 usuarios — sponsor + invitados internos en demos). |

### S2 — IT/Seguridad

| Atributo | Valor |
|----------|-------|
| **Rol** | Custodio de seguridad y estabilidad del entorno corporativo. |
| **Expectativa del MVP** | Solo lectura sobre Salesforce/Multivende, on-prem (sin SaaS, sin exposición a internet), credenciales gestionadas. |
| **Restricción que impone al diseño** | Las 13 reglas SECURITY aplicables como restricciones bloqueantes (Pregunta 1 = A del cuestionario de requirements). |
| **Aparece como autor en** | US-23 (auth + sesiones seguras), US-24 (password hashing + lockout), US-25 (logging sin PII), US-26 (cifrado at-rest + transporte), US-27 (hardening de errores), US-28 (validación de entrada + headers HTTP). |

---

## Cobertura de personas en stories.md

| Persona | # stories propias | # stories heredadas/compartidas |
|---------|--------------------|----------------------------------|
| P1 Operador | 14 (US-01..US-14) | Hereda monitoreo y detección del Técnico cuando aplica |
| P2 Técnico | 8 (US-15..US-22) | Hereda todo lo del Operador (RF-27) |
| Cross-cutting (autor: IT/Seguridad o Sponsor) | 7 (US-23..US-29) | — |
| **Total** | **29 stories** | — |
