# Business Logic Model — U5 Rules Management

**Unidad:** U5 — Rules Management
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-23
**Versión:** 1.1 (2026-05-24 — agrega Flujo §7: BrandMonitorChecker consumiendo PendingDropThreshold)

---

## §1 Decisiones aplicadas

| ID | Pregunta | Decisión |
|----|----------|---------|
| P1 | ConditionExpression | A — JSON en `ConditionJson` |
| P2 | Diff historial | A — Snapshot completo antes/después |
| P3 | Acceso Operador a reglas | C — Oculto en menú + redirect a /access-denied |
| P4 | Seed data | A — Inserción en migración EF Core |
| P5 | Atomicidad | A — `SaveChangesAsync` único (transacción implícita) |

---

## §2 Flujo 1 — Crear regla (`CreateRuleAsync`)

```
Técnico → RuleEditPage (modo Crear)
    |
    [Completa: Nombre, Descripción, Módulo, Condición, Severidad, Razón]
    |
    [Submit] → IRuleManagementService.CreateRuleAsync(...)
        |
        Validaciones server-side:
            ArgumentException.ThrowIfNullOrWhiteSpace(reason)    → BR-RULE-01
            ArgumentException.ThrowIfNullOrWhiteSpace(name)
            condition.IsValidForModule(appliesTo)                 → BR-RULE-04
        |
        rule = Rule.Create(name, description, appliesTo, condition, severity)
        history = RuleHistoryEntry.ForCreation(rule.Id, authorUserId, reason, rule)
        |
        await _ruleRepo.AddAsync(rule)
        await _historyRepo.AppendAsync(history)
        await _ruleRepo.SaveChangesAsync()           ← P5: transacción implícita
        |
        [Redirige a RulesPage con mensaje de éxito]
```

---

## §3 Flujo 2 — Editar regla (`UpdateRuleAsync`)

```
Técnico → RuleEditPage (modo Editar, ruleId en URL)
    |
    OnInitializedAsync → IRuleManagementService.GetRuleByIdAsync(ruleId)
    |
    [Formulario prellenado con valores actuales]
    |
    [Técnico modifica campos + escribe Razón obligatoria]
    |
    [Submit] → IRuleManagementService.UpdateRuleAsync(ruleId, ...)
        |
        Validaciones:
            rule = await _ruleRepo.GetByIdAsync(ruleId) ?? throw NotFoundException
            ArgumentException.ThrowIfNullOrWhiteSpace(reason)    → BR-RULE-01
            condition.IsValidForModule(rule.AppliesTo)           → BR-RULE-04
        |
        // Captura snapshot ANTES de la modificación
        snapshotBefore = RuleSnapshot.From(rule)
        |
        rule.Update(name, description, condition, severity)      // muta el aggregate
        |
        // Captura snapshot DESPUÉS de la modificación
        snapshotAfter = RuleSnapshot.From(rule)
        |
        history = RuleHistoryEntry.ForEdit(
                    rule.Id, authorUserId, reason,
                    ruleBefore: snapshotBefore,   // P2: JSON completo
                    ruleAfter:  snapshotAfter)
        |
        await _historyRepo.AppendAsync(history)
        await _ruleRepo.SaveChangesAsync()           ← P5: transacción implícita
        |
        [Redirige a RulesPage con mensaje de éxito]
```

**Nota sobre el snapshot:** `RuleHistoryEntry.ForEdit` recibe `Rule ruleBefore` y `Rule ruleAfter`. El servicio captura `ruleBefore` = snapshot antes de llamar `rule.Update(...)`, y `ruleAfter` = snapshot después. Ambos se serializan a JSON.

---

## §4 Flujo 3 — Activar / Desactivar regla (`SetActiveAsync`)

```
Técnico → RulesPage
    |
    [Toggle Activo/Inactivo] + [Modal pide Razón obligatoria]
    |
    [Confirmar] → IRuleManagementService.ActivateRuleAsync(ruleId, authorUserId, reason)
              ó → IRuleManagementService.DeactivateRuleAsync(ruleId, authorUserId, reason)
        |
        rule = await _ruleRepo.GetByIdAsync(ruleId) ?? throw NotFoundException
        ArgumentException.ThrowIfNullOrWhiteSpace(reason)        → BR-RULE-01
        |
        [Activar]  → rule.Activate()    // idempotente si ya activa   → BR-RULE-06
        [Desactivar] → rule.Deactivate() // idempotente si ya inactiva → BR-RULE-06
        |
        history = RuleHistoryEntry.ForActivation(...)
               ó RuleHistoryEntry.ForDeactivation(...)
        |
        await _historyRepo.AppendAsync(history)
        await _ruleRepo.SaveChangesAsync()           ← P5: transacción implícita
        |
        [RulesPage actualiza estado del toggle]
```

---

## §5 Flujo 4 — Consultar historial (`GetHistoryAsync`)

```
Técnico → RuleHistoryPage (ruleId en URL)
    |
    OnInitializedAsync
        → IRuleManagementService.GetRuleByIdAsync(ruleId)  → título de la regla
        → IRuleManagementService.GetHistoryAsync(ruleId)   → lista de RuleHistoryEntry
    |
    Render:
        Lista cronológica descendente de entradas:
        | Timestamp | Tipo cambio | Autor | Razón | Diff |
        ← SnapshotBefore / SnapshotAfter mostrados como JSON pretty-printed side-by-side
        ← Entradas de tipo Creation: solo SnapshotAfter (la regla naciente)
        ← Entradas de tipo Deactivation: solo SnapshotBefore (el estado que se desactivó)
```

---

## §6 Flujo 5 — DbOrderChecker consumiendo regla activa (U3 → U5)

Este flujo muestra cómo U5 alimenta a U3. Cuando U5 entrega el CRUD, `DbOrderChecker` deja de usar valores hardcodeados y lee de la BD:

```
MonitoringSchedulerService — Timer1 (5 min)
    |
    DbOrderChecker.ExecuteAsync(ct)
        |
        reglas = await _ruleRepo.GetActiveByModuleAsync(ModuleId.DbOrders, ct)
        |
        [Sin reglas activas] → CheckResult.Ok("Sin reglas activas para DbOrders")
        |
        [Con reglas] →
            foreach (rule in reglas)
                condition = rule.GetCondition()         // deserializa ConditionJson
                since = UtcNow - TimeSpan.FromHours(condition.WindowHours!.Value)
                count = await _orderSource.CountOrdersSinceAsync(since, ct)
                |
                [count >= condition.MinOrders] → CheckResult.Ok(...)
                [count < condition.MinOrders]  → CheckResult.Critical(
                    $"Solo {count} pedidos en últimas {condition.WindowHours}h (mínimo: {condition.MinOrders})")
```

---

## §7 Flujo 6 — Autorización Operador (P3)

```
Operador → Menú de navegación
    |
    [RulesPage NO aparece en el menú]  ← P3: NavMenu.razor filtra por rol
    |
    [Operador navega manualmente a /rules]
        |
        ASP.NET Core Authorization middleware
            → cookie del Operador no tiene claim rol="Técnico"
            → redirect a /access-denied
            → página muestra: "Esta sección requiere rol Técnico"
```

---

## §7 Flujo 7 — BrandMonitorChecker consumiendo umbral (U3/U6 → U5)

Este flujo muestra cómo U5 provee el umbral configurable que `BrandMonitorChecker` (U6) usa para determinar el semáforo por site (RF-31):

```
MonitoringSchedulerService — Timer2 (10 min)
    |
    BrandMonitorChecker.ExecuteAsync(ct)  [IServiceScopeFactory — scope por tick]
        |
        regla = await _ruleRepo.GetActiveByModuleAsync(ModuleId.BrandMonitor, ct)
        |
        [Sin regla activa] → threshold = 0
            → DetermineStatus: drop >= 0 nunca se cumple con threshold=0 si drop=0
            → semáforo siempre Red o Yellow hasta que el Técnico configure la regla
        |
        [Con regla] →
            threshold = regla.GetCondition().PendingDropThreshold!.Value   // siempre presente por BR-COND-04
            |
            foreach site in [Patprimo, SevenSeven, Atmos, Ostu]:
                current  = await _sfClient.GetPendingOrdersAsync(site, ct)
                previous = (await _snapshotRepo.GetBySiteAsync(site, ct))?.PendingCountCurrent ?? 0
                snapshot = BrandSnapshot.Upsert(site, current, previous, threshold)
                    ← DetermineStatus(current, previous, threshold):
                        drop = previous - current
                        if drop >= threshold → Green   🟢
                        if drop > 0          → Yellow  🟡
                        else                 → Red     🔴
                await _snapshotRepo.UpsertAsync(snapshot, ct)  // overwrite fila del site
```

**Diferencia clave vs DbOrderChecker (§6):** `BrandMonitorChecker` NO llama a `MonitoringService`. No genera `Incident`. El resultado es visual (semáforo en `BrandMonitorPage`) y no dispara alertas push — cumple RF-31 sin interferir con el pipeline de incidentes (RF-18..RF-22).

---

## §8 Trazabilidad de flujos

| Flujo | Story | RF | BR |
|-------|-------|----|----|
| Crear regla (§2) | US-19 | RF-14, RF-17 | BR-RULE-01, BR-RULE-04, BR-RULE-05 |
| Editar regla (§3) | US-20 | RF-15, RF-17 | BR-RULE-01, BR-RULE-04, BR-RULE-05 |
| Activar/Desactivar (§4) | US-21 | RF-16, RF-17 | BR-RULE-01, BR-RULE-06 |
| Consultar historial (§5) | US-22 | RF-16 | BR-RULE-02, BR-RULE-07 |
| DbOrderChecker consumiendo regla (§6) | US-07 | RF-02, RF-14 | BR-RULE-08 |
| Autorización Operador (§7 anterior) | US-24 | RF-17, RF-27 | BR-RULE-03 |
| BrandMonitorChecker consumiendo umbral (§7) | — | RF-31, RF-14 | BR-RULE-09, BR-COND-04, BR-SEED-03 |
