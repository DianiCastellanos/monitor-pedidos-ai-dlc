# Plan IT4 — Dashboard Improvements

**Fecha:** 2026-05-30  
**Estado:** ✅ Completado

---

## Objetivo

Tres mejoras de UI independientes: separar los timers de refresco, mostrar labels legibles para módulo/causa, y agregar `@rendermode InteractiveServer` a páginas que lo necesitaban.

## Decisiones clave

| # | Decisión | Opción elegida | Razón |
|---|----------|----------------|-------|
| D1 | Timers | Dos `System.Threading.Timer` independientes | Brand Monitor tiene lógica de refresco más costosa; intervalos deben ser ajustables por separado |
| D2 | Labels | Métodos estáticos de mapeo string→string en cada página | Solución simple sin capa de traducción externa; el dominio usa enums en inglés, la UI muestra español |
| D3 | `@rendermode InteractiveServer` | Agregado a páginas de reglas e histórico | Sin esta directiva, Blazor sirve la página en modo estático — los event handlers no ejecutan |

## Unidades afectadas

U6 (Dashboard — `Dashboard.razor`, `HistoricPage`, `WeeklySummaryPage`, `IncidentDetailPage`, páginas de reglas), U1 (NFR Design — rendermode como convención)

## Artefactos

- [functional-design/frontend-components.md](../iteraciones/it4-dashboard-improvements/functional-design/frontend-components.md)
