# Guía — Cómo tratar cambios de código post-MVP en el AI-DLC

**Fecha:** 2026-05-30  
**Contexto:** MonitorPedidos — cambios de código pendientes que no fueron contemplados en las unidades originales U1–U7.

---

## No necesitas retroceder a Inception. Usa el patrón de iteración sobre unidades existentes.

### La regla de decisión

```
¿El cambio agrega un NUEVO requisito que no existía antes?
│
├─ NO → Es un ajuste/refinamiento de una unidad ya existente
│        → Solo actualiza los docs de esa unidad en Construction
│        → No toques Inception
│
└─ SÍ → ¿El cambio agrega una nueva CAPACIDAD funcional?
         │
         ├─ Cabe en una unidad existente (extiende su scope)
         │   → Agrega la story en stories.md + actualiza docs de esa unidad
         │
         └─ No cabe en ninguna unidad (capacidad completamente nueva)
             → Crea una unidad nueva U8, U9, etc. con sus propios docs
```

---

## Aplicado al proyecto actual

| Cambio | ¿Es nuevo? | Acción |
|--------|-----------|--------|
| Brand Monitor append-only, scopes, `NoData` | Refinamiento de US-30/RF-31 | Actualizar docs U2 (entidades) + U3 (checker) |
| M2 configurable (`Channels`, `MinOrders`) | Extensión de US-19/US-20/RF-14 | Actualizar docs U3 + U5 |
| Dashboard timers separados | Refinamiento de US-01 | Actualizar docs U6 |
| Labels humanas en páginas históricas | Refinamiento de US-11/US-12 | Actualizar docs U6 |
| `@rendermode InteractiveServer` | NFR design ajuste | Actualizar docs U1 NFR Design |
| M11 — Jobs Monitor desde Task Scheduler (**IT5**) | Capacidad nueva — no cabe en U1–U7 | Plan en `construction/plans/it5-jobs-from-schtasks-plan.md` |
| UI Navigation & Modo NOC (**IT6**) | Refinamiento de U1 + U6 | Plan en `construction/plans/it6-ui-navigation-noc-plan.md` |
| Cambios futuros | Depende del tipo | Evaluar con la regla de arriba |

---

## El flujo concreto para cada cambio pendiente

1. **Identifica la unidad afectada** (U1–U7 según el scope de arriba)
2. **Actualiza solo los docs de esa unidad** que cambiaron:
   - `functional-design/` → si cambian entidades, reglas de negocio, o UI
   - `nfr-design/` → si cambian patrones técnicos
   - No es necesario tocar los demás docs de la unidad si no cambiaron
3. **Registra la iteración en `aidlc-state.md`** bajo `Iteraciones post-MVP`
4. **Haz el cambio en el código**

---

## Referencia rápida — Qué docs actualizar por unidad

| Unidad | Functional Design | NFR Design | Cuándo actualizar |
|--------|------------------|------------|-------------------|
| **U1** Foundation | domain-entities, business-rules | nfr-design-patterns, logical-components | Cambios en auth, logging, headers, patrones DI |
| **U2** Persistence | domain-entities, business-logic-model, business-rules | — | Cambios en entidades, ciclo de vida de incidentes o snapshots |
| **U3** Detection | business-logic-model, business-rules | — | Cambios en checkers, clasificador, ventanas de tiempo |
| **U4** Integrations | business-logic-model, business-rules | — | Cambios en clientes HTTP, política de reintentos |
| **U5** Rules Mgmt | domain-entities, business-logic-model, business-rules | — | Cambios en estructura de reglas o historial |
| **U6** Dashboard | business-rules, frontend-components | nfr-design-patterns | Cambios en UI, timers, labels, SignalR |
| **U7** Simulation | domain-entities, business-logic-model | — | Cambios en simulador, datos de seed |

---

## Cuándo sí volver a Inception

Solo es necesario retroceder a Inception cuando:

- Se agrega un **nuevo usuario/persona** con flujos propios que no existían.
- Se agrega un **nuevo módulo de negocio** que genera 3 o más historias de usuario nuevas.
- Cambia el **alcance del producto** (nuevo PRD o cambio de dirección de negocio).
- Se detecta un **RF faltante** que impacta múltiples unidades simultáneamente.

En todos los demás casos — ajustes, refinamientos, mejoras, correcciones de comportamiento — se trabaja directamente en Construction actualizando los docs de la unidad afectada.
