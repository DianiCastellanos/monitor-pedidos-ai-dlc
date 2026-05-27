# Business Rules — U5 Rules Management

**Unidad:** U5 — Rules Management
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-23
**Versión:** 1.1 (2026-05-24 — agrega BR-COND-04 BrandMonitor, BR-SEED-03 seed BrandMonitor, BR-RULE-09 consumo de regla por BrandMonitorChecker)

---

## §1 BR-RULE — Reglas de gestión de reglas

| ID | Regla | Implementación | Story | RF |
|----|-------|---------------|-------|----|
| BR-RULE-01 | El campo `Reason` es **obligatorio** en toda operación que modifique una regla (crear, editar, activar, desactivar). `ArgumentException` si vacío o whitespace | `IRuleManagementService` — `ArgumentException.ThrowIfNullOrWhiteSpace(reason)` antes de cualquier escritura | US-19, US-20, US-21 | RF-17 |
| BR-RULE-02 | El historial de reglas es **inmutable**: `IRuleHistoryRepository` no expone métodos `Update` ni `Delete`. Ninguna capa puede modificar una `RuleHistoryEntry` una vez creada | `IRuleHistoryRepository` — interfaz sin métodos de mutación; EF Core no configura operaciones de actualización/borrado sobre `RuleHistoryEntry` | US-22 | RF-16, SECURITY-13 |
| BR-RULE-03 | Solo el rol **Técnico** puede crear, editar, activar y desactivar reglas. El rol Operador no puede ver las páginas de gestión de reglas | `RulesPage`, `RuleEditPage`, `RuleHistoryPage` — `[Authorize(Roles = "Técnico")]`; menú de navegación oculta las entradas para Operador | US-19..US-22 | RF-17, RF-27 |
| BR-RULE-04 | `ConditionJson` debe serializar una `RuleCondition` válida para el módulo al que aplica la regla. `condition.IsValidForModule(appliesTo)` debe retornar `true` | `Rule.Create` y `Rule.Update` — validan `IsValidForModule` antes de serializar | US-19, US-20 | RF-14, RF-15 |
| BR-RULE-05 | Al editar una regla, el historial captura el **estado completo** antes y después como JSON (`RuleSnapshot`). La UI puede mostrar ambos snapshots side-by-side sin lógica adicional | `RuleManagementService.UpdateRuleAsync` — toma snapshot antes de `rule.Update(...)` y otro después; ambos van a `RuleHistoryEntry.ForEdit(...)` | US-20 | RF-15 |
| BR-RULE-06 | `Activate()` y `Deactivate()` son **idempotentes**: si la regla ya está en el estado solicitado, no hacen nada y no generan entrada de historial | `Rule.Activate` — `if (IsActive) return;` / `Rule.Deactivate` — `if (!IsActive) return;` | US-21 | RF-16 |
| BR-RULE-07 | El historial es consultable por `RuleId` o por rango de fechas. Siempre se retorna en orden cronológico descendente | `IRuleHistoryRepository.GetByRuleIdAsync` y `GetAllAsync` — ordenados por `ChangedAt DESC` | US-22 | RF-16 |
| BR-RULE-08 | `DbOrderChecker` lee reglas activas del módulo `DbOrders` vía `IRuleRepository.GetActiveByModuleAsync`. Si no hay reglas activas para el módulo, emite `CheckResult.Ok` con mensaje informativo (sin falso positivo) | `DbOrderChecker.ExecuteAsync` — `if (!reglas.Any()) return CheckResult.Ok(...)` | US-07 | RF-02, RF-14 |
| BR-RULE-09 | `BrandMonitorChecker` lee la regla activa del módulo `BrandMonitor` vía `IRuleRepository.GetActiveByModuleAsync(BrandMonitor)` para obtener `PendingDropThreshold`. Si no hay regla activa, usa `threshold = 0` (sin Green posible → semáforo siempre Amarillo o Rojo hasta configurar) | `BrandMonitorChecker.ExecuteAsync` — `var threshold = rule?.GetCondition().PendingDropThreshold ?? 0` | — | RF-31, RF-14 |

---

## §2 BR-HIST — Reglas de generación de historial

| ID | Regla | Implementación | Story | RF |
|----|-------|---------------|-------|----|
| BR-HIST-01 | La creación de una regla genera automáticamente una `RuleHistoryEntry` de tipo `Creation` con `SnapshotBefore = null` y `SnapshotAfter = estado_inicial` | `RuleHistoryEntry.ForCreation(rule.Id, authorUserId, reason, rule)` en `CreateRuleAsync` | US-19 | RF-14 |
| BR-HIST-02 | La edición genera `RuleHistoryEntry` de tipo `Edit` con `SnapshotBefore` = estado antes y `SnapshotAfter` = estado después | `RuleHistoryEntry.ForEdit(...)` en `UpdateRuleAsync` — snapshot capturado antes de llamar `rule.Update(...)` | US-20 | RF-15 |
| BR-HIST-03 | La activación genera `RuleHistoryEntry` de tipo `Activation` con `SnapshotAfter` = estado activado | `RuleHistoryEntry.ForActivation(...)` en `ActivateRuleAsync` | US-21 | RF-16 |
| BR-HIST-04 | La desactivación genera `RuleHistoryEntry` de tipo `Deactivation` con `SnapshotBefore` = estado antes de desactivar | `RuleHistoryEntry.ForDeactivation(...)` en `DeactivateRuleAsync` | US-21 | RF-16 |
| BR-HIST-05 | La creación de `Rule` y su `RuleHistoryEntry` se persisten en una **sola llamada `SaveChangesAsync`** — transacción implícita EF Core | `RuleManagementService` — `AddAsync(rule)` + `AppendAsync(history)` + un único `SaveChangesAsync()` | US-19..US-21 | RF-14..RF-16 |

---

## §3 BR-SEED — Reglas de datos semilla

| ID | Regla | Implementación | Story | RF |
|----|-------|---------------|-------|----|
| BR-SEED-01 | La migración EF Core de U5 inserta **2 reglas iniciales** que replican los valores hardcodeados usados por U3 en Sprint 2: `DbOrders (windowHours=2, minOrders=1, severity=Critical)` y `DbHealth (latencyWarnMs=500, latencyCriticalMs=2000, severity=Warn)` | Migración `AddRulesTables` — `migrationBuilder.InsertData(...)` | US-07, US-08 | RF-14 |
| BR-SEED-02 | Las reglas semilla tienen `IsActive = true` desde el primer `database update`, garantizando que U3 encuentre reglas activas desde el inicio sin configuración adicional | Migración — campo `is_active = 1` en el InsertData | US-07, US-08 | RF-14 |
| BR-SEED-03 | La migración EF Core de U6 (`AddBrandSnapshots`) inserta una **tercera regla semilla** para `ModuleId.BrandMonitor` con `PendingDropThreshold = 5` e `IsActive = true`. Garantiza que `BrandMonitorChecker` encuentre umbral activo desde el primer tick sin configuración manual. | Migración `AddBrandSnapshots` — `migrationBuilder.InsertData(...)` | — | RF-31, RF-14 |

---

## §4 BR-COND — Reglas de validación de condiciones

| ID | Regla | Implementación | Story | RF |
|----|-------|---------------|-------|----|
| BR-COND-01 | Una `RuleCondition` para `ModuleId.DbOrders` **debe** tener `WindowHours > 0` y `MinOrders >= 1` | `RuleCondition.IsValidForModule(DbOrders)` → `WindowHours.HasValue && MinOrders.HasValue` | US-19, US-20 | RF-14 |
| BR-COND-02 | Una `RuleCondition` para `ModuleId.DbHealth` **debe** tener `LatencyWarnMs < LatencyCriticalMs` | `RuleCondition.IsValidForModule(DbHealth)` → ambos presentes (validación de orden en `Rule.Create`) | US-19, US-20 | RF-14 |
| BR-COND-03 | Una `RuleCondition` para `ModuleId.Jobs` no requiere parámetros numéricos (verifica solo que el job esté activo) | `RuleCondition.IsValidForModule(Jobs)` → siempre `true` | US-19 | RF-14 |
| BR-COND-04 | Una `RuleCondition` para `ModuleId.BrandMonitor` **debe** tener `PendingDropThreshold >= 1`. Determina cuántos pedidos pendientes deben reducirse en 10 min para que el semáforo sea Verde (🟢) | `RuleCondition.IsValidForModule(BrandMonitor)` → `PendingDropThreshold.HasValue` | — | RF-31, RF-14 |

---

## §5 Trazabilidad completa

| Categoría | Reglas | Stories | RFs |
|-----------|--------|---------|-----|
| BR-RULE (9) | BR-RULE-01..09 | US-07, US-19..US-22, US-24 | RF-02, RF-14..RF-17, RF-27, RF-31 |
| BR-HIST (5) | BR-HIST-01..05 | US-19..US-21 | RF-14..RF-16 |
| BR-SEED (3) | BR-SEED-01..03 | US-07, US-08 | RF-14, RF-31 |
| BR-COND (4) | BR-COND-01..04 | US-19, US-20 | RF-14, RF-31 |
| **Total** | **21 reglas** | US-07, US-08, US-19..US-22, US-24 | RF-02, RF-14..RF-17, RF-27, RF-31 |
