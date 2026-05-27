# Unit of Work — Story Map

**Fecha:** 2026-05-22
**Versión:** 1.0
**Stage:** Inception → Units Generation (Part 2 — Generation)
**Propósito:** mapeo bidireccional **Story ↔ Unidad** con verificación de cobertura 29/29.
**Fuentes:** [`stories.md`](../user-stories/stories.md), [`unit-of-work.md`](./unit-of-work.md), [`unit-of-work-plan.md`](../plans/unit-of-work-plan.md) (decisión Q5 = A).

---

## 1. Mapeo Story → Unidad (vista por story)

> Cada story está asignada a **exactamente una** unidad. Se incluye el sprint sugerido para visibilidad.

### 1.1 Operador / Analista operativo (US-01..US-14)

| Story | Título corto | Unidad | Sprint sugerido |
|-------|---------------|--------|------------------|
| US-01 | Ver vista real-time del estado | **U6** Dashboard & Real-Time | Sprint 2 |
| US-02 | Activar modo NOC | **U6** Dashboard & Real-Time | Sprint 2 |
| US-03 | Recibir notificación con fallback | **U6** Dashboard & Real-Time | Sprint 2 |
| US-04 | Ver panel de discrepancias (UC6) | **U6** Dashboard & Real-Time | Sprint 3 |
| US-05 | Persistencia de alertas al recargar | **U2** Persistence & Incidents | Sprint 1 |
| US-06 | Recibir alerta con 6 campos en español | **U3** Detection & Classification | Sprint 2 |
| US-07 | Detectar ausencia de pedidos | **U3** Detection & Classification | Sprint 2 |
| US-08 | Detectar falla health-check BD | **U3** Detection & Classification | Sprint 2 |
| US-09 | Detectar falla de job | **U3** Detection & Classification | Sprint 2 |
| US-10 | Detectar token 401 sin renovación | **U4** External Integrations | Sprint 2 |
| US-11 | Consultar histórico de incidentes | **U2** Persistence & Incidents | Sprint 3 |
| US-12 | Ver resumen semanal | **U2** Persistence & Incidents | Sprint 3 |
| US-13 | Cierre manual con comentario | **U2** Persistence & Incidents | Sprint 1 |
| US-14 | Cierre automático tras 2 OK | **U2** Persistence & Incidents | Sprint 1 |

### 1.2 Técnico / Responsable técnico exclusivas (US-15..US-22)

| Story | Título corto | Unidad | Sprint sugerido |
|-------|---------------|--------|------------------|
| US-15 | Acceder al dashboard con rol Técnico | **U6** Dashboard & Real-Time | Sprint 2 |
| US-16 | Ver detalle de auto_reintentos | **U4** External Integrations | Sprint 2 |
| US-17 | Consultar / exportar logs técnicos | **U6** Dashboard & Real-Time | Sprint 3 |
| US-18 | Aviso de causa_no_determinada | **U3** Detection & Classification | Sprint 2 |
| US-19 | Crear regla con autor + razón | **U5** Rules Management | Sprint 3 |
| US-20 | Editar regla con diff | **U5** Rules Management | Sprint 3 |
| US-21 | Activar / desactivar regla | **U5** Rules Management | Sprint 3 |
| US-22 | Consultar historial inmutable | **U5** Rules Management | Sprint 3 |

### 1.3 Cross-cutting calidad y operación (US-23..US-29)

| Story | Título corto | Unidad | Sprint sugerido |
|-------|---------------|--------|------------------|
| US-23 | Selección de identidad + sesiones seguras | **U1** Foundation & Cross-Cutting | Sprint 1 |
| US-24 | Separación de acceso por rol (Técnico vs Operador) | **U1** Foundation & Cross-Cutting | Sprint 1 |
| US-25 | Logging estructurado sin PII | **U1** Foundation & Cross-Cutting | Sprint 1 |
| US-26 | Cifrado at-rest + transporte BD | **U1** Foundation & Cross-Cutting | Sprint 1 |
| US-27 | Hardening de errores | **U1** Foundation & Cross-Cutting | Sprint 1 |
| US-28 | Validación de entrada + headers HTTP | **U1** Foundation & Cross-Cutting | Sprint 1 |
| US-29 | Concurrencia 5 usuarios internos | **U6** Dashboard & Real-Time | Sprint 3 (validar) |

> **US-29** se asigna a U6 porque es donde se prueba operativamente (5 sesiones Blazor concurrentes), pero su cumplimiento es **criterio NFR transversal** evaluado al cerrar U6 (decisión Q5 = A).

---

## 2. Mapeo Unidad → Stories (vista por unidad)

> Suma final de stories por unidad. Cobertura objetivo: 29/29.

| Unidad | Stories asignadas | Conteo |
|--------|---------------------|--------|
| **U1** Foundation & Cross-Cutting | US-23, US-24, US-25, US-26, US-27, US-28 | **6** |
| **U2** Persistence & Incidents | US-05, US-11, US-12, US-13, US-14 | **5** |
| **U3** Detection & Classification | US-06, US-07, US-08, US-09, US-18 | **5** |
| **U4** External Integrations | US-10, US-16 | **2** |
| **U5** Rules Management | US-19, US-20, US-21, US-22 | **4** |
| **U6** Dashboard & Real-Time | US-01, US-02, US-03, US-04, US-15, US-17, US-29 | **7** |
| **U7** Simulation & Red-Teaming | (0 stories directas — soporta a todas) | **0** |
| **TOTAL** | | **29** |

✅ **6 + 5 + 5 + 2 + 4 + 7 + 0 = 29** — match exacto con el catálogo de [`stories.md`](../user-stories/stories.md).

---

## 3. Mapeo Sprint → Stories (vista por sprint)

> Útil para planning del Sprint Review.

### Sprint 1 (semana 1)

| Story | Unidad |
|-------|--------|
| US-05 | U2 |
| US-13 | U2 |
| US-14 | U2 |
| US-23 | U1 |
| US-24 | U1 |
| US-25 | U1 |
| US-26 | U1 |
| US-27 | U1 |
| US-28 | U1 |

**9 stories** + setup del simulator (U7-sim, sin stories directas).

### Sprint 2 (semana 2)

| Story | Unidad |
|-------|--------|
| US-01 | U6 |
| US-02 | U6 |
| US-03 | U6 |
| US-06 | U3 |
| US-07 | U3 |
| US-08 | U3 |
| US-09 | U3 |
| US-10 | U4 |
| US-15 | U6 |
| US-16 | U4 |
| US-18 | U3 |

**11 stories.**

### Sprint 3 (semana 3)

| Story | Unidad |
|-------|--------|
| US-04 | U6 |
| US-11 | U2 |
| US-12 | U2 |
| US-17 | U6 |
| US-19 | U5 |
| US-20 | U5 |
| US-21 | U5 |
| US-22 | U5 |
| US-29 | U6 (validación NFR) |

**9 stories.**

### Sprint 4 (semana 4)

| Story / Validación | Unidad |
|---------------------|--------|
| Red-teaming RT1..RT-Persist | U7-rt |
| Ajustes basados en hallazgos | (cualquiera) |
| Calibración de reglas con datos reales | U5 (ajustes) |
| Medición de KPIs | (dashboard) |
| Demo con sponsor | — |

**0 stories nuevas;** validación end-to-end de las 29 stories ya implementadas.

**Total a través de los 4 sprints:** 9 + 11 + 9 + 0 = **29 stories** ✅.

---

## 4. Cobertura cruzada — Story ↔ RF ↔ UC ↔ RT

> Trazabilidad heredada de [`stories.md`](../user-stories/stories.md) §4 y consolidada aquí para el cierre de Inception.

### 4.1 Verificación RF cubiertos

Los 30 RFs (RF-01..RF-30) se cubren a través de las 29 stories distribuidas en las 7 unidades (ver `stories.md` §4.1).

Las unidades que cubren cada bloque de RFs:

| Bloque RF | Unidades involucradas |
|-----------|------------------------|
| RF-01..RF-10 (detección + reintentos + clasificación) | U3, U4 |
| RF-11..RF-13 (alertas en español) | U3 |
| RF-14..RF-17 (reglas CRUD + historial) | U5 |
| RF-18..RF-22 (incidentes + retención + persistencia) | U2 |
| RF-23..RF-25 (vistas dashboard + push) | U6 |
| RF-26..RF-30 (auth + roles + concurrencia) | U1, U6 |

✅ **30/30 RFs cubiertos.**

### 4.2 Verificación UC cubiertos

| UC | Unidades involucradas |
|----|------------------------|
| UC1 Ausencia de pedidos | U3, U7 (simulator de fallos) |
| UC2 Revisión proactiva | U6 |
| UC3 Escalamiento | U6 (logs Técnico) + U3 (alerta) |
| UC4 Token expirado | U4 |
| UC5 Revisión semanal / calibración | U2 (resumen), U5 (ajuste reglas) |
| UC6 Discrepancias de estado | U6 (panel) + U3 (detección) |
| UC7 Health check BD | U3 |
| Journey 1 (token sin auto) | U4 |
| Journey 2 (causa no determinada) | U3 + U5 (crear regla nueva) |

✅ **7/7 UCs + 2/2 Journeys cubiertos.**

### 4.3 Verificación Red-Teaming cubiertos

| RT | Validado por |
|----|--------------|
| RT1 (job SF apagado) | U7-rt (Sprint 4) — usa flujos de U3, U6 |
| RT2 (token SF revocado) | U7-rt — usa flujos de U4, U6 |
| RT3 (SQL apagado) | U7-rt — usa flujos de U3, U6 |
| RT5 (estado incorrecto) | U7-rt — usa flujos de U3, U6 |
| RT7 (cancelado ignorado) | U7-rt — usa flujos de U3 |
| RT-Persist (cerrar/reabrir) | U7-rt — usa flujos de U2, U6 |

✅ **6/6 escenarios de red-teaming cubiertos.**

---

## 5. Validación final

| Criterio | Status |
|----------|--------|
| 29/29 stories asignadas a alguna unidad | ✅ |
| 0 stories huérfanas | ✅ |
| 0 stories duplicadas (asignadas a >1 unidad) | ✅ |
| 30/30 RFs cubiertos | ✅ |
| 7/7 UCs + 2/2 Journeys cubiertos | ✅ |
| 6/6 escenarios red-teaming cubiertos | ✅ |
| Grafo de dependencias acíclico (DAG) | ✅ (verificado en `unit-of-work-dependency.md` §6) |
| Mapeo a 4 sprints del PRD §13 | ✅ |

✅ **Validación completa — sin huérfanas, sin duplicadas, sin ciclos, cobertura total.**

---

## 6. Resumen

- **29 stories** distribuidas en **7 unidades**, sin solapamientos.
- **Unidad con más stories:** U6 Dashboard & Real-Time (7 stories — incluye toda la presentación).
- **Unidad con menos stories directas:** U7 Simulation & Red-Teaming (0 directas, soporta a todas indirectamente).
- **Cobertura completa** del PRD v2.3 + requirements.md v1.1 + stories.md a través de las 7 unidades.
- **Listo para activar Construction** en una iteración futura: cualquier unidad puede arrancar consultando solo su entrada de `unit-of-work.md` + las dependencias listadas en `unit-of-work-dependency.md`.
