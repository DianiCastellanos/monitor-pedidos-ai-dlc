# Frontend Components — U2 Persistence & Incidents

**Unidad:** U2 — Persistence & Incidents
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Resumen de componentes

| Componente | Tipo | Ruta | Rol requerido |
|-----------|------|------|--------------|
| `HistoricPage` | Blazor Page | `/incidents` | Autenticado (Operador + Técnico) |
| `WeeklySummaryPage` | Blazor Page | `/incidents/weekly` | Autenticado (Operador + Técnico) |
| `IncidentDetailPage` | Blazor Page | `/incidents/{id}` | Autenticado (Operador + Técnico) |

---

## §2 Componente: HistoricPage

| Atributo | Detalle |
|----------|---------|
| **Tipo** | Blazor Server Page |
| **Ruta** | `/incidents` |
| **Archivo** | `Features/Incidents/HistoricPage.razor` |
| **Autorización** | `[Authorize]` (hereda FallbackPolicy — Operador y Técnico) |
| **Responsabilidad** | Lista todos los incidentes (abiertos y cerrados) con filtros opcionales y paginación Anterior/Siguiente |

**Parámetros de filtro (query string):**

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `from` | `DateTimeOffset?` | Fecha inicio del filtro |
| `to` | `DateTimeOffset?` | Fecha fin del filtro |
| `severity` | `Severity?` | Filtro por severidad |
| `module` | `ModuleId?` | Filtro por módulo |
| `skip` | `int` | Offset de paginación (default 0) |
| `take` | `int` | Tamaño de página (default 20, máx 100) |

**Flujo de interacción:**

```
Usuario llega a /incidents
    |
    v
OnInitializedAsync() → SearchHistoryAsync(filtro vacío, skip=0, take=20)
    |
    v
Renderiza tabla:
  Columnas: Módulo | Causa | Severidad | Abierto | Cerrado | Estado | Acciones
    |
    v
Usuario aplica filtros → OnFilterChangedAsync() → reset skip=0 → SearchHistoryAsync
    |
    v
Usuario hace clic en [Siguiente] → skip += take → SearchHistoryAsync
Usuario hace clic en [Anterior] → skip -= take → SearchHistoryAsync
    |
    v
Clic en fila o botón [Ver detalle] → NavigateTo(/incidents/{id})
```

**Estructura visual:**

```
[ Filtros: Desde | Hasta | Severidad | Módulo ] [Buscar] [Limpiar]

| Módulo  | Causa       | Severidad | Abierto          | Cerrado | Estado  | Acciones |
|---------|-------------|-----------|------------------|---------|---------|----------|
| BD      | Job         | CRITICAL  | 2026-05-23 10:30 | —       | Abierto | [Ver]    |
| API     | Token       | CRITICAL  | 2026-05-22 08:00 | 08:45   | Cerrado | [Ver]    |
| ...

                        [← Anterior]  Página 1 de 3  [Siguiente →]
```

**Interacciones:**

```
HistoricPage
    |-- llama --> IIncidentService.SearchHistoryAsync(filter)
    |-- navega --> /incidents/{id}  (al hacer clic en Ver)
    |-- lee --> ClaimsPrincipal (para mostrar rol)
```

---

## §3 Componente: WeeklySummaryPage

| Atributo | Detalle |
|----------|---------|
| **Tipo** | Blazor Server Page |
| **Ruta** | `/incidents/weekly` |
| **Archivo** | `Features/Incidents/WeeklySummaryPage.razor` |
| **Autorización** | `[Authorize]` (Operador y Técnico) |
| **Responsabilidad** | Muestra resumen de incidentes de la semana actual agrupados por causa y severidad |

**Parámetros:**

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `week` | `DateOnly?` | Inicio de semana a consultar. Si vacío, usa la semana actual |

**Flujo de interacción:**

```
Usuario llega a /incidents/weekly
    |
    v
OnInitializedAsync() → weekStart = lunes de la semana actual
    → GetWeeklySummaryAsync(weekStart)
    |
    v
Renderiza tabla agrupada por Causa y Severidad
    |
    v
Usuario cambia semana → OnWeekChangedAsync(newWeek) → GetWeeklySummaryAsync(newWeek)
```

**Estructura visual:**

```
Semana del 19 al 25 de mayo 2026   [← Semana anterior]  [Semana siguiente →]

| Causa            | Severidad  | Incidentes | Candidatos a regla nueva |
|------------------|------------|------------|--------------------------|
| Job              | CRITICAL   | 3          | 0                        |
| Token            | CRITICAL   | 2          | 0                        |
| No determinada   | WARN       | 1          | 1                        |
| BD               | WARN       | 1          | 0                        |
|                  | **Total**  | **7**      | **1**                    |
```

**Interacciones:**

```
WeeklySummaryPage
    |-- llama --> IIncidentService.GetWeeklySummaryAsync(weekStart)
```

---

## §4 Componente: IncidentDetailPage

| Atributo | Detalle |
|----------|---------|
| **Tipo** | Blazor Server Page |
| **Ruta** | `/incidents/{id:guid}` |
| **Archivo** | `Features/Incidents/IncidentDetailPage.razor` |
| **Autorización** | `[Authorize]` (Operador y Técnico) |
| **Responsabilidad** | Muestra el detalle completo de un incidente y permite el cierre manual con comentario obligatorio |

**Parámetros de ruta:**

| Parámetro | Tipo | Descripción |
|-----------|------|-------------|
| `id` | `Guid` | Id del incidente |

**Flujo de interacción:**

```
Usuario llega a /incidents/{id}
    |
    v
OnInitializedAsync() → GetByIdAsync(id)
    |
    +-> [No encontrado] → redirige a /Error
    |
    +-> [Encontrado] → renderiza detalle completo
        |
        v
Si incidente IsOpen:
    Muestra formulario de cierre manual:
        [Textarea: Comentario de resolución *]
        [Botón: Cerrar incidente]
            |
            v
    OnPostCloseAsync(comentario)
        → IIncidentService.CloseManuallyAsync(id, closedByRole, comentario)
            |
            +-> [comentario vacío] → muestra error de validación inline
            |
            +-> [OK] → redirige a /incidents
```

**Estructura visual:**

```
← Volver al histórico

INCIDENTE #a3f2...  [ABIERTO / CERRADO]

Módulo:    BD Interna
Causa:     Job detenido
Severidad: ⚠ CRITICAL
Abierto:   2026-05-23 10:30 UTC

─── Alerta ──────────────────────────────────────────
Qué pasó:       No se detectaron pedidos en ventana esperada
Cuándo:         2026-05-23 10:30 UTC
Dónde:          BD Interna (MonitorPedidosDb)
Severidad:      Crítico
Causa probable: Job de integración detenido
Acción sugerida: Verificar estado del job en el orquestador. Ref. SOP-001
─────────────────────────────────────────────────────

[Si abierto:]
── Cerrar manualmente ───────────────────────────────
Comentario de resolución *
┌─────────────────────────────────────────────────┐
│                                                 │
└─────────────────────────────────────────────────┘
[Cerrar incidente]
─────────────────────────────────────────────────────

[Si cerrado:]
Cerrado:        2026-05-23 10:45 UTC
Tipo de cierre: Manual
Cerrado por:    Técnico
Comentario:     Se reinició el job manualmente. Causa: timeout en BD.
```

**Interacciones:**

```
IncidentDetailPage
    |-- llama --> IIncidentService (GetByIdAsync, CloseManuallyAsync)
    |-- lee --> ClaimsPrincipal (para closedByRole)
    |-- navega --> /incidents (tras cierre exitoso)
    |-- extiende en U4 --> muestra lista de auto_reintento para incidentes API
```

---

## §5 Trazabilidad

| Componente | Story | RF | Servicio invocado |
|-----------|-------|----|------------------|
| `HistoricPage` | US-11 | RF-22 | `IIncidentService.SearchHistoryAsync` |
| `WeeklySummaryPage` | US-12 | RF-22 | `IIncidentService.GetWeeklySummaryAsync` |
| `IncidentDetailPage` — vista | US-05 | RF-18 | `IIncidentService.GetByIdAsync` |
| `IncidentDetailPage` — cierre manual | US-13 | RF-20 | `IIncidentService.CloseManuallyAsync` |
