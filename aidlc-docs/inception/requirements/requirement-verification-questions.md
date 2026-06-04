# Requirements Verification Questions — MonitorPedidos AI

> ⚠️ **Nota de revisión de contexto (2026-05-20T09:55):** después de responder este cuestionario, el owner solicitó eliminar **toda exposición pública del sistema**. Las preguntas y respuestas se conservan como artefacto histórico. El contexto efectivo es: **localhost o red interna del equipo, sin ngrok, sin URL pública, sin acceso desde internet**. Esto afecta principalmente la *justificación* de las preguntas 5, 11 y 12 (las decisiones se mantienen, pero el motivo "demo pública via ngrok" deja de aplicar). Ver `requirements.md` v1.1, sección C-02 y nota de contexto en §3.7. Trazabilidad completa en `audit.md`.

**Instrucciones de uso:**
1. Cada pregunta tiene opciones marcadas A, B, C, D, etc.
2. Donde corresponda, una opción está marcada como **(Recomendada)** con la justificación estratégica.
3. Responda escribiendo la letra correspondiente después de `[Answer]:`.
4. Si ninguna opción aplica, escoja la última (Otro) y describa su respuesta.
5. Cuando termine de contestar todas las preguntas, indíqueme **"listo"** o **"completado"**.

---

## Pregunta 1 — Aplicabilidad de extensión SECURITY (Obligatoria del framework AI-DLC)

¿Se debe aplicar el conjunto de reglas SECURITY (SECURITY-01 a SECURITY-15) como restricciones bloqueantes en el proyecto MonitorPedidos AI?

**Trade-off:** Aplicar SECURITY agrega rigor (encriptación at-rest/in-transit, logging centralizado, validación de entrada, manejo de credenciales con secret manager, deny-by-default, hardening, etc.) pero requiere más esfuerzo de diseño y configuración en cada etapa.

A) **Sí — aplicar todas las reglas SECURITY como restricciones bloqueantes (Recomendada).** El sistema maneja tokens de APIs externas (Salesforce/Multivende), credenciales de BD, y se expondrá vía ngrok para la demo (URL pública HTTPS). El principio P3 (soberanía de datos) y P2 (solo lectura) ya están alineados con SECURITY.
B) No — omitir SECURITY (apropiado solo para PoC desechable; no recomendado dado que el MVP evolucionará a producción on-prem post-MVP).
C) Otro (describir después de `[Answer]:`)

[Answer]:A

---

## Pregunta 2 — Los 6 campos de la alerta explicable (P1)

El principio P1 (Explicabilidad) exige que cada alerta tenga 6 campos en español. ¿Qué 6 campos deben componer la alerta?

A) **(Recomendada)** `qué_pasó`, `cuándo`, `dónde` (módulo/integración), `severidad`, `causa_probable`, `acción_sugerida`. Cubre las preguntas básicas de un operador en ≤30 s y enlaza directo con el SOP correspondiente.
B) `qué_pasó`, `cuándo`, `cuántos_pedidos_afectados`, `severidad`, `causa_probable`, `acción_sugerida`. Más orientado al impacto cuantitativo.
C) `qué_pasó`, `cuándo`, `dónde`, `severidad`, `causa_probable`, `evidencia_técnica` (códigos de error, snippets). Más técnico, menos accionable.
D) Otro (describir después de `[Answer]:`)

[Answer]: A

---

## Pregunta 3 — Los 3 niveles de severidad

La Decisión #10 fija 3 niveles. ¿Cuáles deben usarse?

A) **(Recomendada)** `INFO` / `WARN` / `CRITICAL`. Estándar en la industria (alineado con syslog/logging), legible, fácil de mapear a comportamiento del dashboard (parpadeo solo en CRITICAL, sonido en WARN+CRITICAL).
B) `BAJA` / `MEDIA` / `ALTA`. Más cercano al usuario de negocio pero menos preciso técnicamente.
C) `OK` / `ATENCIÓN` / `URGENTE`. Más imperativo, podría confundir entre estado normal y notificación.
D) Otro (describir después de `[Answer]:`)

[Answer]:A

---

## Pregunta 4 — Mecanismo de edición de reglas estáticas (M6)

Las reglas son "editables" con "historial de cambios con autor+razón" (P5). ¿Cómo se editarán las reglas en el MVP?

A) **(Recomendada para MVP)** UI dedicada en el dashboard ASP.NET Core (CRUD de reglas + historial). Mejor experiencia para el técnico responsable, valida en cliente, registro automático de autor.
B) Tabla `rules` en SQL Server, edición directa por SSMS / scripts. Más rápido de implementar (menos UI) pero rompe el principio de "historial con autor" sin auditoría manual.
C) Archivo `rules.json` versionado en git. Trazabilidad perfecta pero requiere git instalado y conocimiento al técnico, no práctico para demo.
D) Otro (describir después de `[Answer]:`)

[Answer]: A

---

## Pregunta 5 — Autenticación del dashboard

Aunque el MVP corre en localhost, la demo del curso lo expondrá vía ngrok (URL HTTPS pública temporal). ¿Qué mecanismo de autenticación se requiere?

A) **(Recomendada)** Login básico con usuario/contraseña (ASP.NET Core Identity), 2 cuentas pre-creadas (analista, técnico), sesión con cookies seguras (Secure/HttpOnly/SameSite). Mínimo viable que cumple SECURITY-12 sin sobre-ingeniería.
B) Sin autenticación, confiando en que la URL ngrok es efímera. Inseguro: ngrok publica los túneles, y SECURITY-12 lo prohíbe.
C) Autenticación Windows / AD pass-through (post-MVP on-prem). Bloqueante para la demo del curso.
D) Token compartido en query string. No cumple buenas prácticas.
E) Otro (describir después de `[Answer]:`)

[Answer]: A

---

## Pregunta 6 — Cierre de incidentes

Cuando un incidente CRITICAL/WARN se resuelve (la causa raíz ya no aparece), ¿cómo se cierra?

A) **(Recomendada)** Cierre **automático** cuando 2 chequeos consecutivos del mismo módulo regresan OK + opción de **cierre manual** desde el dashboard (con campo "comentario de resolución"). Combina eficiencia con trazabilidad humana.
B) Solo cierre automático (1 chequeo OK). Riesgo: cierres prematuros (flapping).
C) Solo cierre manual. Más fricción para el analista; contradice el principio de reducir trabajo manual.
D) Otro (describir después de `[Answer]:`)

[Answer]: A

---

## Pregunta 7 — Retención histórica del MVP

La Decisión #5 dice "30-90 días". ¿Qué retención fija el MVP?

A) **(Recomendada)** **90 días** desde el día 0. Permite construir baseline robusto para futuras reglas dinámicas (v2 anomaly detection), y 90 días en SQL Server MonitorPedidosDb (172.16.0.41) es manejable (≈10–50 MB con la cadencia 5–10 min).
B) 30 días. Más liviano pero limita análisis de tendencia mensual/trimestral.
C) Sin límite en MVP, definir en post-MVP. Permite máxima información pero podría crecer descontroladamente.
D) Otro (describir después de `[Answer]:`)

[Answer]:A

---

## Pregunta 8 — Roles y permisos del dashboard

Hay 2 personas distintas (analista operativo, responsable técnico). ¿Necesitan permisos diferentes en el MVP?

A) **(Recomendada)** Sí, 2 roles: **`Operador`** (ver dashboard, cerrar incidentes manualmente, ver historial) y **`Técnico`** (todo lo del Operador + editar reglas + ver/exportar logs técnicos). Refleja la realidad organizacional y cumple SECURITY-08 (function-level authorization).
B) Un solo rol (cualquier usuario puede todo). Más simple pero rompe least-privilege.
C) 3 roles (`Operador`, `Técnico`, `Admin`). Sobre-diseño para 2 personas en MVP.
D) Otro (describir después de `[Answer]:`)

[Answer]:A

---

## Pregunta 9 — Failbook

El Anexo A / Sprint 0 menciona "Failbook inicial (5-7 casos)". ¿Es parte del software MonitorPedidos AI o documento externo?

A) **(Recomendada)** **Documento externo** (Markdown en `aidlc-docs/` o wiki interno) consumido por el responsable técnico para crear las reglas iniciales en M6. Mantiene el MVP enfocado en software (menos scope) y permite evolución independiente.
B) Feature del software (sección en dashboard con casos documentados). Útil pero agrega scope a MVP de 4 semanas.
C) Híbrido: documento externo en Sprint 0, importado al dashboard en Sprint 3 si hay tiempo. Trade-off razonable si los tiempos permiten.
D) Otro (describir después de `[Answer]:`)

[Answer]:A

---

## Pregunta 10 — Estrategia de transición desde el monitoreo manual

¿Cómo gestionamos la transición desde el proceso manual de 25h/mes actual hacia MonitorPedidos AI?

A) **(Recomendada)** **Operación en paralelo durante 2 semanas** post-demo: el analista valida el dashboard contra su rutina manual existente. Mitiga riesgo R2 (no adopción), construye confianza y permite calibrar reglas con datos reales.
B) Switch directo tras la demo. Más rápido pero R2 (no adopción) y R3 (falsos positivos sin calibrar) elevados.
C) Operación en paralelo indefinida hasta que el sponsor lo decida. Falta de cierre claro.
D) Otro (describir después de `[Answer]:`)

[Answer]:A

---

## Pregunta 11 — Concurrencia esperada de usuarios

¿Cuántos usuarios concurrentes deben soportar el dashboard en MVP?

A) **(Recomendada)** **Hasta 5 concurrentes**. Cubre los 2 usuarios principales + sponsor + 2 invitados (demos), holgura suficiente sin sobre-dimensionar localhost.
B) Hasta 2 (solo analista + técnico). Demasiado ajustado para demos.
C) Hasta 20+. Sobre-dimensionado para 4 semanas y localhost.
D) Otro (describir después de `[Answer]:`)

[Answer]: A

---

## Pregunta 12 — Generación de datos simulados para MVP

La Estrategia de despliegue dice "localhost + datos simulados". ¿Cómo se generarán esos datos?

A) **(Recomendada)** **Tabla `simulated_orders`** en SQL Server MonitorPedidosDb (172.16.0.41) poblada por un script SQL inicial + un job .NET que inserta pedidos cada 5–10 min con probabilidad configurable de fallo. Realista, controlable, reseteable, permite ejecutar los 6 escenarios de red-teaming.
B) **Mocks HTTP** de Salesforce/Multivende con WireMock o similar. Más fiel a producción pero agrega complejidad de setup.
C) Datos estáticos (JSON/CSV cargado una vez). Más simple pero no permite simular escenarios dinámicos (falla intermitente, latencia variable).
D) Otro (describir después de `[Answer]:`)

[Answer]:A

---

## Pregunta 13 — TBDs activos del PRD (Anexo B)

Hay 3 TBDs activos pendientes para Semana 1. ¿Quieres que los abordemos en este análisis de requerimientos o los dejamos como acción separada de Sprint 1?

- TBD-D4: Tiempo de onboarding
- TBD-S3-org: Datos organizacionales
- TBD-S3-verbatims: Verbatims

A) **(Recomendada)** Dejarlos como acción separada de Sprint 1. No bloquean el diseño funcional ni la arquitectura — son inputs de discovery que el sponsor recolectará en paralelo a la construcción.
B) Resolverlos ahora con preguntas adicionales en este archivo. Demora el avance del flujo Inception.
C) Otro (describir después de `[Answer]:`)

[Answer]: A

---

**Cuando termines de responder todas las preguntas, escribe "listo" para que proceda al análisis y la generación de `requirements.md`.**
