# NFR Requirements — U5 Rules Management

**Unidad:** U5 — Rules Management
**Stage:** Construction → NFR Requirements
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Decisiones aplicadas

| ID | Pregunta | Decisión |
|----|----------|---------|
| P1 | Tests para RuleManagementService | C — Unit tests para validación + integration tests para persistencia atómica |
| P2 | Tests integración U3 → U5 (DbOrderChecker + IRuleRepository) | A — Unit test con IRuleRepository mockeado |
| P3 | ConditionJson malformado en BD | A — Fail fast: InvalidOperationException → incidente NoDeterminada |
| P4 | Concurrencia entre Técnicos | A — Last-write-wins (EF Core default; historial inmutable cubre la auditoría) |

---

## §2 NFR-U5 — Atributos de calidad

### NFR-U5-01 — Unit tests de validación de RuleManagementService (Mantenibilidad)

| Atributo | Valor |
|----------|-------|
| **Categoría** | Mantenibilidad |
| **Requisito** | Validaciones del servicio deben tener unit tests con repositorios mockeados (Moq) |
| **Proyecto** | `MonitorPedidos.UnitTests` |
| **Stories** | US-19, US-20, US-21 |
| **RF** | RF-17 |

**Tests requeridos:**

```
RuleManagementServiceValidationTests
    ├── CreateRuleAsync_EmptyReason_ThrowsArgumentException
    ├── CreateRuleAsync_InvalidConditionForModule_ThrowsArgumentException
    ├── UpdateRuleAsync_EmptyReason_ThrowsArgumentException
    ├── ActivateRuleAsync_EmptyReason_ThrowsArgumentException
    └── DeactivateRuleAsync_EmptyReason_ThrowsArgumentException
```

---

### NFR-U5-02 — Integration tests de persistencia atómica (Confiabilidad)

| Atributo | Valor |
|----------|-------|
| **Categoría** | Confiabilidad |
| **Requisito** | La atomicidad Rule + RuleHistoryEntry debe verificarse con una BD real (SQLite in-memory) — el proveedor InMemory de EF Core no soporta transacciones reales |
| **Proyecto** | `MonitorPedidos.UnitTests` (nuevo paquete `Microsoft.EntityFrameworkCore.Sqlite`) |
| **Stories** | US-19, US-20, US-21 |
| **RF** | RF-14, RF-15, RF-16 |

**Tests requeridos:**

```
RuleManagementServiceIntegrationTests  [usa AppDbContext con SQLite in-memory]
    ├── CreateRuleAsync_PersistsRuleAndHistoryEntry_InSingleTransaction
    │       Assert: rules table tiene 1 fila, rule_history tiene 1 fila ChangeType=Creation
    │
    ├── UpdateRuleAsync_PersistsUpdatedRuleAndHistoryEntry_WithSnapshotDiff
    │       Assert: rule.Name actualizado, rule_history tiene entrada Edit con SnapshotBefore ≠ SnapshotAfter
    │
    ├── DeactivateRuleAsync_PersistsIsActiveFalse_AndHistoryEntry
    │       Assert: rule.IsActive = false, rule_history tiene entrada Deactivation
    │
    └── ActivateRuleAsync_IsAlreadyActive_IsIdempotent_NoHistoryEntry
            Assert: rule.IsActive sigue true, rule_history no agrega nueva entrada
```

---

### NFR-U5-03 — Unit test DbOrderChecker con reglas vivas (Mantenibilidad)

| Atributo | Valor |
|----------|-------|
| **Categoría** | Mantenibilidad |
| **Requisito** | `DbOrderChecker` debe tener un unit test que verifique la lectura de `RuleCondition` deserializada desde `IRuleRepository` mockeado |
| **Proyecto** | `MonitorPedidos.UnitTests` — agrega tests a la clase `DbOrderCheckerTests` existente (U3) |
| **Stories** | US-07 |
| **RF** | RF-02, RF-14 |

**Tests requeridos:**

```
DbOrderCheckerTests  [extensión de tests U3 — nuevos casos con IRuleRepository]
    ├── ExecuteAsync_WithActiveRule_WindowHours2_And0Orders_ReturnsCritical
    │       Mock IRuleRepository → devuelve regla con WindowHours=2, MinOrders=1
    │       Mock IOrderSource → devuelve 0 pedidos en ventana
    │       Assert: CheckResult.Status == Critical
    │
    ├── ExecuteAsync_WithActiveRule_WindowHours2_And1Order_ReturnsOk
    │       Mock IRuleRepository → regla con WindowHours=2, MinOrders=1
    │       Mock IOrderSource → devuelve 1 pedido
    │       Assert: CheckResult.Status == Ok
    │
    └── ExecuteAsync_WithNoActiveRules_ReturnsOk
            Mock IRuleRepository → lista vacía
            Assert: CheckResult.Status == Ok (sin falso positivo — BR-RULE-08)
```

---

### NFR-U5-04 — Fail fast en ConditionJson malformado (Confiabilidad / Seguridad)

| Atributo | Valor |
|----------|-------|
| **Categoría** | Confiabilidad |
| **Requisito** | Si `Rule.GetCondition()` no puede deserializar `ConditionJson`, lanza `InvalidOperationException`. El `GlobalExceptionHandler` de U1 captura la excepción y genera un incidente `NoDeterminada` visible al Técnico |
| **Implementación** | `Rule.GetCondition()` — sin try/catch; deja propagar la excepción de `JsonSerializer.Deserialize` |
| **Stories** | US-07 |
| **RF** | RF-02 |

---

### NFR-U5-05 — Concurrencia last-write-wins (Escalabilidad / MVP)

| Atributo | Valor |
|----------|-------|
| **Categoría** | Escalabilidad |
| **Requisito** | No se implementa optimistic concurrency (`RowVersion`) en el MVP. Con 1 perfil Técnico activo, la colisión real no puede ocurrir. El historial inmutable registra toda escritura, proveyendo trazabilidad completa |
| **Decisión diferida** | Si se agrega soporte multi-usuario post-MVP, agregar columna `RowVersion` en migración y capturar `DbUpdateConcurrencyException` en el servicio |
| **Stories** | US-19..US-21 |
| **RF** | RF-14..RF-16 |

---

## §3 NFR heredados de U1 aplicables a U5

| NFR U1 | Aplica en U5 | Contexto |
|--------|-------------|---------|
| SECURITY-06 | `RulesPage`, `RuleEditPage`, `RuleHistoryPage` | Solo rol Técnico accede a gestión de reglas |
| SECURITY-08 | Todas las páginas de reglas | Autorización server-side via `[Authorize(Roles="Técnico")]` — no solo ocultar en UI |
| SECURITY-13 | `IRuleHistoryRepository` | Sin Update ni Delete sobre historial — inmutabilidad garantizada por contrato de interfaz |
| NFR-U1 (cookie auth) | Formularios de reglas | Cookie de sesión activa requerida antes de mostrar páginas de edición |

---

## §4 Trazabilidad NFR

| NFR | Categoría | Componentes afectados | Stories | RFs |
|-----|-----------|----------------------|---------|-----|
| NFR-U5-01 | Mantenibilidad | RuleManagementService (tests validación) | US-19..US-21 | RF-17 |
| NFR-U5-02 | Confiabilidad | RuleManagementService (tests integración) | US-19..US-21 | RF-14..RF-16 |
| NFR-U5-03 | Mantenibilidad | DbOrderChecker (tests con IRuleRepository) | US-07 | RF-02, RF-14 |
| NFR-U5-04 | Confiabilidad | Rule.GetCondition(), GlobalExceptionHandler | US-07 | RF-02 |
| NFR-U5-05 | Escalabilidad | RuleManagementService, AppDbContext | US-19..US-21 | RF-14..RF-16 |
| SECURITY-06/08/13 (heredado) | Seguridad | RulesPage, RuleEditPage, RuleHistoryPage, IRuleHistoryRepository | US-19..US-22, US-24 | RF-17, RF-27 |
