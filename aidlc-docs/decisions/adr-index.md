# Registro de Decisiones de Arquitectura (ADR Index) — MonitorPedidos AI

**Proyecto:** MonitorPedidos AI — Manufacturas Eliot
**Owner:** Diana Castellanos
**Última actualización:** 2026-05-22
**Convención de nomenclatura:** `ADR-{NNN}-{slug-descriptivo}.md`

---

## ¿Qué es un ADR?

Un **Architecture Decision Record** documenta una decisión de arquitectura significativa: el contexto que la originó, las opciones evaluadas, la decisión adoptada y sus consecuencias. El objetivo es que cualquier miembro del equipo, incluyendo quienes se incorporen en el futuro, entienda **por qué** el sistema está construido como está — no solo **cómo**.

---

## Registro Completo

| ID | Título | Estado | Fecha | Categoría | Impacto |
|----|--------|--------|-------|-----------|---------|
| [ADR-001](ADR-001-monolito-modular-vertical-slice.md) | Monolito Modular con Vertical Slice Architecture | **Aceptada** | 2026-05-21 | Arquitectura | Crítico |
| ADR-002 | Blazor Server como framework de UI | Pendiente | — | Stack UI | Crítico |
| ADR-003 | SignalR Hub para notificaciones en tiempo real | Pendiente | — | Comunicación | Alto |
| ADR-004 | BackgroundService + PeriodicTimer para scheduling | Pendiente | — | Infraestructura | Alto |
| ADR-005 | ASP.NET Core Identity con sesiones HTTP cookie (no JWT) | Pendiente | — | Seguridad | Alto |
| ADR-006 | SQL Server LocalDB como motor de datos del MVP | Pendiente | — | Datos | Alto |
| ADR-007 | 5 Application Services por capability (no CQRS/MediatR) | Pendiente | — | Diseño de servicios | Alto |
| ADR-008 | Política de reintentos Polly: máx 2, solo 5xx/timeout, sin renovación automática de token | Pendiente | — | Resiliencia | Medio |
| ADR-009 | Despliegue exclusivo en localhost/red interna (sin nube, sin ngrok) | Pendiente | — | Despliegue | Medio |
| ADR-010 | SECURITY extension como restricción bloqueante desde Inception | Pendiente | — | Seguridad | Medio |
| ADR-011 | Clasificación de causa raíz en cascada (6 categorías, orden fijo) | Pendiente | — | Dominio | Medio |
| ADR-012 | Cierre automático de incidentes por 2 chequeos consecutivos OK | Pendiente | — | Dominio | Medio |

---

## Estados posibles

| Estado | Significado |
|--------|-------------|
| **Pendiente** | ADR identificado, aún no redactado |
| **Propuesta** | Borrador en revisión, decisión no confirmada |
| **Aceptada** | Decisión aprobada por el owner y en vigor |
| **Deprecada** | Decisión reemplazada por una más reciente (el ADR original se conserva) |
| **Supersedida** | Reemplazada; enlaza al ADR sucesor |

---

## Orden de prioridad para documentar los ADRs pendientes

Los siguientes ADRs son candidatos a generarse durante la fase de **Construction** (cuando se active), en el orden sugerido por su impacto en el diseño de cada unidad de trabajo:

| Orden sugerido | ADR | Cuándo generar |
|:-:|-----|----------------|
| 1 | ADR-002 Blazor Server | Antes de comenzar U6 (Dashboard & Real-Time) |
| 2 | ADR-005 Identity / cookies | Antes de comenzar U1 (Foundation) |
| 3 | ADR-004 BackgroundService | Antes de comenzar U3 (Detection & Classification) |
| 4 | ADR-003 SignalR Hub | Junto con ADR-002 (U6) |
| 5 | ADR-006 SQL Server LocalDB | Antes de comenzar U2 (Persistence & Incidents) |
| 6 | ADR-007 Application Services | Antes de comenzar cualquier unidad |
| 7 | ADR-008 Política de reintentos | Antes de U4 (External Integrations) |
| 8 | ADR-009 Despliegue local | Al cierre de Inception o inicio de Construction |
| 9 | ADR-010 SECURITY extension | Al cierre de Inception (ya documentado en requirements.md) |
| 10 | ADR-011 Clasificación causa raíz | Antes de U3 (Detection & Classification) |
| 11 | ADR-012 Cierre automático incidentes | Antes de U2 (Persistence & Incidents) |

---

## Trazabilidad con artefactos de Inception

| ADR | Fuente primaria |
|-----|-----------------|
| ADR-001 | `application-design-plan.md` §3 Q1 + Q2 |
| ADR-002 | `application-design-plan.md` §3 Q3 |
| ADR-003 | `application-design-plan.md` §3 Q5 |
| ADR-004 | `application-design-plan.md` §3 Q4 |
| ADR-005 | `requirements.md` RF-26..RF-29, RNF-14 |
| ADR-006 | `requirements.md` C-03, `prd.md` §13 Sprint 1 |
| ADR-007 | `application-design-plan.md` §3 Q6, `services.md` |
| ADR-008 | `requirements.md` RF-06, RF-07 |
| ADR-009 | `requirements.md` C-02, `prd.md` Decisión #8 |
| ADR-010 | `requirements.md` §4.3, C-09 |
| ADR-011 | `requirements.md` RF-08, RF-09 |
| ADR-012 | `requirements.md` RF-19 |
