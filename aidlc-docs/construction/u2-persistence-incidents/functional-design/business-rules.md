# Business Rules — U2 Persistence & Incidents

**Unidad:** U2 — Persistence & Incidents
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Reglas de ciclo de vida del incidente (BR-INC)

| ID | Regla | Fuente |
|----|-------|--------|
| BR-INC-01 | Solo puede existir **1 incidente abierto por ModuleId** en cualquier momento. Un segundo WARN/CRITICAL del mismo módulo retorna el incidente abierto existente sin crear uno nuevo. | INV-01, RF-18 |
| BR-INC-02 | Un incidente **cerrado no puede reabrirse**. Para el mismo módulo, un nuevo WARN/CRITICAL crea un incidente nuevo con nuevo Id. | INV-03 |
| BR-INC-03 | El campo `IsCandidatoReglaNueva` se establece en `true` automáticamente cuando `Cause == CauseCategory.NoDeterminada`. No puede cambiarse manualmente. | INV-04, RF-18 |
| BR-INC-04 | El cierre automático se activa cuando se reciben **2 checks OK consecutivos del mismo ModuleId**, independientemente del tipo de checker (M2, M4 o M11). | RF-19, P3=A |
| BR-INC-05 | El cierre automático establece `CloseType = Automatic` y **no requiere ni acepta** comentario de resolución. | RF-19 |

---

## §2 Reglas de cierre manual (BR-CLOSE)

| ID | Regla | Fuente |
|----|-------|--------|
| BR-CLOSE-01 | El `ComentarioResolucion` es **obligatorio** en el cierre manual. El sistema rechaza el cierre si el campo está vacío o solo contiene espacios en blanco. El rechazo ocurre **server-side** (no solo validación UI). | RF-20, INV-02 |
| BR-CLOSE-02 | El cierre manual registra el **rol** del usuario que cerró (`ClosedByRole`: `"Operador"` o `"Técnico"`). No registra nombre personal (no hay usuarios nominales). | RF-20 |
| BR-CLOSE-03 | Cualquier rol autenticado (Operador o Técnico) puede cerrar manualmente un incidente. No hay restricción por rol para esta acción. | RF-20 |
| BR-CLOSE-04 | El cierre manual de un incidente ya cerrado retorna error de dominio (`DomainException`). No es una operación silenciosa. | INV-03 |

---

## §3 Reglas de consulta histórica (BR-SEARCH)

| ID | Regla | Fuente |
|----|-------|--------|
| BR-SEARCH-01 | Los filtros de búsqueda son **opcionales y combinables**: `From`, `To`, `Severity`, `Module`. Si ninguno se especifica, retorna todos los incidentes paginados. | RF-22, US-11 |
| BR-SEARCH-02 | El parámetro `Take` tiene un **máximo de 100 registros** por página. Si se recibe un valor mayor, se recorta a 100 silenciosamente. | US-11 |
| BR-SEARCH-03 | Los resultados se ordenan por `OpenedAt` **descendente** (más recientes primero). | US-11 |
| BR-SEARCH-04 | La consulta incluye tanto incidentes abiertos como cerrados. No existe filtro obligatorio de estado. | RF-22 |

---

## §4 Reglas del resumen semanal (BR-WEEKLY)

| ID | Regla | Fuente |
|----|-------|--------|
| BR-WEEKLY-01 | La semana se define por su **fecha de inicio** (`DateOnly weekStart`). El período cubierto es `[weekStart, weekStart + 7 días)`. | US-12 |
| BR-WEEKLY-02 | El resumen agrupa por `(Cause, Severity)` y cuenta incidentes. También cuenta cuántos son `IsCandidatoReglaNueva = true` por grupo. | US-12, RF-18 |
| BR-WEEKLY-03 | Se implementa con **LINQ sobre EF Core** (sin raw SQL). | P1=A |

---

## §5 Reglas de retención (BR-PURGE)

| ID | Regla | Fuente |
|----|-------|--------|
| BR-PURGE-01 | Los incidentes con `OpenedAt < UtcNow - 90 días` se **eliminan físicamente** (hard delete). No se archivan. | RF-21 |
| BR-PURGE-02 | La purga se ejecuta **una vez al día** al arrancar el scheduler. Si la app no corrió un día, la purga del día siguiente elimina todos los expirados acumulados. | P2=A |
| BR-PURGE-03 | La purga registra en el log la cantidad de incidentes eliminados (`purga_incidentes | eliminados={Count}`). | RNF-08 |
| BR-PURGE-04 | La purga **no bloquea** ni afecta la detección de incidentes activos. Se ejecuta de forma independiente. | P2=A |

---

## §6 Reglas de persistencia (BR-PERSIST)

| ID | Regla | Fuente |
|----|-------|--------|
| BR-PERSIST-01 | Todos los campos de `AlertMessage` se almacenan como columnas en la tabla `incidents` (no como JSON). | US-05, US-06 |
| BR-PERSIST-02 | El `Id` del incidente es un **GUID generado en el dominio** (no por la base de datos). | domain-entities.md §2.3 |
| BR-PERSIST-03 | Los campos de cierre (`ClosedAt`, `CloseType`, `ClosedByRole`, `ComentarioResolucion`) son **nullable** y solo se poblan al cerrar. Un incidente recién abierto los tiene en NULL. | RF-18 |
| BR-PERSIST-04 | Tras reiniciar la aplicación, los incidentes activos siguen siendo visibles en el dashboard (persistencia garantizada por BD). | RT-Persist, US-05 |
