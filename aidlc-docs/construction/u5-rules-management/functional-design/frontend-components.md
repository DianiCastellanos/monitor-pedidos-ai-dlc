# Frontend Components — U5 Rules Management

**Unidad:** U5 — Rules Management
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Páginas Blazor en U5

U5 introduce **3 páginas nuevas**, todas restringidas al rol `Técnico`.

| Página | Ruta | Rol | Descripción |
|--------|------|-----|-------------|
| `RulesPage` | `/rules` | Solo Técnico | Lista de todas las reglas (activas + inactivas) con toggle y acciones |
| `RuleEditPage` | `/rules/new` y `/rules/{id}/edit` | Solo Técnico | Formulario de creación y edición de regla |
| `RuleHistoryPage` | `/rules/{id}/history` | Solo Técnico | Historial inmutable de cambios de una regla |

El menú de navegación **oculta estas entradas para el rol Operador** (BR-RULE-03, P3).

---

## §2 RulesPage — Lista de reglas

### Regla de visibilidad

- La entrada "Reglas" en el menú de navegación solo aparece cuando `userRole == "Técnico"`.
- Si un Operador accede directamente a `/rules`, el middleware de autorización redirige a `/access-denied`.

### Estructura visual

```
╔══════════════════════════════════════════════════════════════╗
║  Gestión de Reglas                      [+ Nueva regla]      ║
╠══════════════════════════════════════════════════════════════╣
║  Módulo        │ Nombre              │ Severidad │ Estado    ║
║  ──────────────┼─────────────────────┼───────────┼────────── ║
║  DbOrders      │ Ventana de pedidos  │ CRÍTICO   │ ● Activa  ║
║                │                     │           │ [Editar]  ║
║                │                     │           │ [Historial]║
║                │                     │           │ [Desactivar]║
║  ─────────────────────────────────────────────────────────── ║
║  DbHealth      │ Latencia de BD      │ WARN      │ ● Activa  ║
║                │                     │           │ [Editar]  ║
║                │                     │           │ [Historial]║
║                │                     │           │ [Desactivar]║
╚══════════════════════════════════════════════════════════════╝
```

### Interacciones

```
RulesPage
    |
    OnInitializedAsync
        → IRuleManagementService.GetAllRulesAsync()
    |
    Render:
        → tabla con todas las reglas (activas primero, inactivas al final)
        → botón [+ Nueva regla] → navega a /rules/new
        → botón [Editar] → navega a /rules/{id}/edit
        → botón [Historial] → navega a /rules/{id}/history
        → botón [Activar / Desactivar]:
              → abre modal "Ingresa una razón obligatoria:"
              → [Confirmar] → IRuleManagementService.Activate/DeactivateRuleAsync(id, userId, reason)
              → tabla se refresca tras la operación
```

### Modal de razón obligatoria (Activar / Desactivar)

```
╔═══════════════════════════════════════╗
║  Desactivar regla: "Ventana pedidos"  ║
║                                       ║
║  Razón (obligatorio):                 ║
║  ┌─────────────────────────────────┐  ║
║  │ Por mantenimiento programado... │  ║
║  └─────────────────────────────────┘  ║
║                                       ║
║       [Cancelar]   [Confirmar]        ║
╚═══════════════════════════════════════╝
```

---

## §3 RuleEditPage — Crear / Editar regla

### Regla de visibilidad

- Solo accesible con rol `Técnico` (`[Authorize(Roles = "Técnico")]`).
- En modo Crear (`/rules/new`): formulario vacío.
- En modo Editar (`/rules/{id}/edit`): formulario prellenado con valores actuales de la regla.

### Estructura visual

```
╔══════════════════════════════════════════════════════════════╗
║  Nueva regla  (ó  Editar: "Ventana de pedidos")             ║
╠══════════════════════════════════════════════════════════════╣
║  Nombre *          [___________________________________]     ║
║  Descripción *     [___________________________________]     ║
║  Módulo *          [ DbOrders ▼ ]                           ║
║  Severidad *       [ CRÍTICO  ▼ ]                           ║
║                                                              ║
║  ── Condición ─────────────────────────────────────────     ║
║  [si módulo=DbOrders]                                       ║
║  Ventana (horas) * [___]   Pedidos mínimos * [___]          ║
║                                                              ║
║  [si módulo=DbHealth]                                       ║
║  WARN (ms) * [___]         CRITICAL (ms) * [___]            ║
║                                                              ║
║  ── Auditoría ─────────────────────────────────────────     ║
║  Razón del cambio * [________________________________]      ║
║                                                              ║
║       [Cancelar]                    [Guardar]               ║
╚══════════════════════════════════════════════════════════════╝
```

### Interacciones

```
RuleEditPage
    |
    OnInitializedAsync (modo Editar)
        → IRuleManagementService.GetRuleByIdAsync(id)
        → prellenar formulario con Name, Description, AppliesTo, Condition, Severity
    |
    [Módulo cambia] → sección de condición se adapta dinámicamente
        DbOrders → muestra WindowHours + MinOrders
        DbHealth → muestra LatencyWarnMs + LatencyCriticalMs
        Jobs     → sin campos de condición numérica
    |
    [Submit] → validación client-side (campos requeridos, Razón no vacía)
            → IRuleManagementService.CreateRuleAsync(...)  [modo Crear]
            → IRuleManagementService.UpdateRuleAsync(...)  [modo Editar]
            → [éxito] → redirige a RulesPage con mensaje de confirmación
            → [error validación server] → muestra mensaje de error inline
```

---

## §4 RuleHistoryPage — Historial de una regla

### Regla de visibilidad

- Solo accesible con rol `Técnico`.
- Historial inmutable: no hay botones de edición ni eliminación.

### Estructura visual

```
╔══════════════════════════════════════════════════════════════╗
║  Historial: "Ventana de pedidos — Salesforce/Multivende"    ║
╠══════════════════════════════════════════════════════════════╣
║  Timestamp            │ Tipo        │ Autor      │ Razón    ║
║  ─────────────────────┼─────────────┼────────────┼───────── ║
║  23/05/2026 10:30     │ Edición     │ Técnico    │ Ajuste.. ║
║  ▼ [ver diff]                                               ║
║  ┌─ ANTES ───────────┐  ┌─ DESPUÉS ───────────┐            ║
║  │ WindowHours: 2    │  │ WindowHours: 3       │            ║
║  │ MinOrders: 1      │  │ MinOrders: 1         │            ║
║  │ Severity: Critical│  │ Severity: Critical   │            ║
║  └───────────────────┘  └─────────────────────┘            ║
║  ─────────────────────────────────────────────────────────  ║
║  23/05/2026 09:00     │ Creación    │ Técnico    │ Inicial  ║
║  ▼ [ver diff]                                               ║
║  ┌─ ESTADO INICIAL ─────────────────────────────┐          ║
║  │ WindowHours: 2, MinOrders: 1, ...            │          ║
║  └──────────────────────────────────────────────┘          ║
╚══════════════════════════════════════════════════════════════╝
```

### Interacciones

```
RuleHistoryPage
    |
    OnInitializedAsync
        → IRuleManagementService.GetRuleByIdAsync(id)   → nombre de la regla (header)
        → IRuleManagementService.GetHistoryAsync(id)    → entradas ordenadas DESC
    |
    Render:
        → lista cronológica de RuleHistoryEntry
        → [ver diff] expande el panel con SnapshotBefore/After en JSON pretty-printed
        → Tipo Creation: solo muestra SnapshotAfter (estado inicial)
        → Tipo Edit: muestra Before + After side-by-side
        → Tipo Activation / Deactivation: muestra el snapshot correspondiente
        → Sin botones de acción (historial read-only)
    |
    [← Volver a Reglas] → navega a RulesPage
```

---

## §5 Actualización de NavMenu.razor

```razor
@* NavMenu.razor — entrada condicional por rol *@

@if (_userRole == "Técnico")
{
    <NavLink href="/rules" Match="NavLinkMatch.Prefix">
        Reglas
    </NavLink>
}
```

El bloque `@if` se evalúa en el circuito Blazor Server con el `AuthenticationStateProvider` del usuario activo — la restricción es server-side, no solo client-side.

---

## §6 Lo que U5 NO muestra en UI

| Elemento | Razón |
|----------|-------|
| Páginas de reglas para Operador | BR-RULE-03 — solo Técnico |
| Edición de historial | BR-RULE-02 — historial inmutable |
| Eliminación de reglas | No está en el scope del PRD — solo activar/desactivar |
| `ConditionJson` raw en formulario | El Técnico interactúa con campos tipados (WindowHours, MinOrders, etc.); el JSON es un detalle de implementación |

---

## §7 Trazabilidad

| Componente | Story | RF | Seguridad |
|-----------|-------|----|-----------| 
| `RulesPage` — lista + toggle | US-19, US-21 | RF-14, RF-16, RF-17 | SECURITY-06 (solo Técnico), SECURITY-08 (server-side) |
| Modal de razón obligatoria | US-19, US-20, US-21 | RF-17 | — |
| `RuleEditPage` — crear | US-19 | RF-14, RF-17 | SECURITY-06, SECURITY-08 |
| `RuleEditPage` — editar | US-20 | RF-15, RF-17 | SECURITY-06, SECURITY-08 |
| `RuleHistoryPage` — historial read-only | US-22 | RF-16 | SECURITY-06, SECURITY-13 |
| NavMenu oculto para Operador | US-24 | RF-27 | SECURITY-06 |
| Redirect /access-denied para Operador | US-24 | RF-27 | SECURITY-08 |
