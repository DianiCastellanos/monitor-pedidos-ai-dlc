# IT2 — M2 Configurable por Reglas · Functional Design

**Fecha:** 2026-05-30  
**Iteración:** IT2 — M2 Configurable por Reglas  
**Stories relacionadas:** US-19 (crear regla), US-20 (editar regla), RF-14 (CRUD reglas via UI)  
**Estado:** ✅ Completado

---

## 1. Objetivo

Evolucionar `DbOrderChecker` (M2) para que su comportamiento — ventana de tiempo, mínimo de pedidos y canales esperados — se configure desde la UI de Reglas de Monitoreo en lugar de estar hardcodeado.

---

## 2. Cambios en el modelo de dominio

### 2.1 `RuleCondition` — nuevo campo `Channels`

```csharp
public record RuleCondition(
    int?      WindowMinutes,
    int?      MinOrders,
    string[]? Channels      // ["SALESFORCE", "MULTIVENDE"]
)
```

**Serialización JSON:**
```json
{
  "WindowMinutes": 10,
  "MinOrders": 1,
  "Channels": ["SALESFORCE", "MULTIVENDE"]
}
```

**Fallbacks cuando null:**
- `Channels = null` → `["SALESFORCE", "MULTIVENDE"]`
- `MinOrders = null` → `1`
- `WindowMinutes = null` → `10` minutos

### 2.2 `Rule.AppliesTo` → `Rule.ModuleId`

Renombramiento para consistencia con el enum `ModuleId` usado en todo el sistema (HistoricPage, WeeklySummaryPage, clasificador).

**Compatibilidad DB:** columna sigue siendo `AppliesTo` en SQL Server vía `[Column("AppliesTo")]` — sin pérdida de datos.

---

## 3. Lógica de evaluación M2 (DbOrderChecker)

```
1. Cargar regla activa para ModuleId = DbOrderChecker
2. Extraer: windowMinutes, minOrders, channels
   └─ Fallback a defaults si null
3. Contar pedidos por canal en ventana:
   orders.GroupBy(o => o.OrderId, StringComparer.OrdinalIgnoreCase)
         .ToDictionary(g => g.Key, g => g.Count())
4. Por cada channel en channels:
   └─ count = dict.GetValueOrDefault(channel, 0)
   └─ isOk  = count >= minOrders
5. Determinar estado:
   └─ okCount == channels.Length  → CheckResult.Ok
   └─ okCount == 0                → CheckResult.Critical
   └─ else                        → CheckResult.Warning
6. Formatear detalles: "SALESFORCE:40:OK|MULTIVENDE:0:CRITICAL"
```

**Bug corregido post-implementación:** el conteo inicial usaba `Distinct().ToHashSet()` que colapsaba N pedidos del mismo canal en 1 entrada. Corregido a `GroupBy(OrderId).ToDictionary()`.

---

## 4. Seed data — regla por defecto

Se inserta con la migración `MakeRulesConfigurable`:

| Campo | Valor |
|-------|-------|
| `Name` | "Ventana de pedidos — Salesforce/Multivende" |
| `Description` | "Alerta si no hay pedidos en la ventana esperada para Salesforce o Multivende" |
| `ModuleId` | `DbOrderChecker` |
| `ConditionJson` | `{"WindowMinutes":10,"MinOrders":1,"Channels":["SALESFORCE","MULTIVENDE"]}` |
| `Severity` | `Critical` |
| `IsActive` | `true` |

---

## 5. Resultado en UI por estado

| Estado | Dashboard |
|--------|-----------|
| Ambos canales >= MinOrders | `✅ M2 BD Pedidos` — SALESFORCE (40) OK, MULTIVENDE (25) OK |
| Un canal < MinOrders | `⚠️ M2 BD Pedidos` — SALESFORCE (0) CRITICAL, MULTIVENDE (20) OK |
| Ambos < MinOrders | `🔴 M2 BD Pedidos` — SALESFORCE (0) CRITICAL, MULTIVENDE (0) CRITICAL |

---

## 6. Archivos modificados

| Archivo | Cambio |
|---------|--------|
| `src/MonitorPedidos.Domain/Rules/RuleCondition.cs` | `+string[]? Channels`; `ForDbOrders` con channels; `IsValidForModule` valida `Channels.Length > 0` |
| `src/MonitorPedidos.Domain/Rules/Rule.cs` | `AppliesTo` → `ModuleId` |
| `src/MonitorPedidos.Domain/Rules/RuleSnapshot.cs` | `rule.AppliesTo` → `rule.ModuleId` |
| `src/MonitorPedidos.Infrastructure/Persistence/Configurations/RuleConfiguration.cs` | `ModuleId` con `.HasColumnName("AppliesTo")`; índice actualizado |
| `src/MonitorPedidos.Infrastructure/Rules/RuleRepository.cs` | `.Where(r => r.ModuleId == module)` |
| `src/MonitorPedidos.Infrastructure/Persistence/AppDbContext.cs` | Seed data regla M2 |
| `src/MonitorPedidos.Web/Features/Monitoring/DbOrderChecker.cs` | Lee `MinOrders` + `Channels`; conteo `GroupBy(OrderId)` |
| `src/MonitorPedidos.Web/Components/Pages/Rules/RulesPage.razor` | `@rule.AppliesTo` → `@rule.ModuleId` |
| `src/MonitorPedidos.Web/Components/Pages/Rules/RuleEditPage.razor` | `AppliesTo` → `ModuleId`; label "Ventana (horas)" → "Ventana (minutos)" |
| `src/MonitorPedidos.Web/Components/Pages/Dashboard.razor` | Sub-rows por canal con count y OK/CRITICAL |
| `src/MonitorPedidos.Web/Components/Pages/Noc/NocPage.razor` | ídem Dashboard |
| Migración `20260530034007_MakeRulesConfigurable` | Seed data insertada |

---

## 7. Tests

### Nuevos (`DbOrderCheckerTests.cs`)

| Test | Escenario | Resultado esperado |
|------|-----------|-------------------|
| `NoOrders` | Sin órdenes | CRITICAL |
| `AllChannelsHaveOrders` | Ambos canales con 1+ pedido | OK |
| `OnlyCancelledOrders` | Todas canceladas | CRITICAL |
| `SalesforceZero` | Solo Multivende tiene pedidos | CRITICAL |
| `BelowMinOrders` | minOrders=5, solo 1 por canal | CRITICAL |
| `NoActiveRules` | Sin reglas, defaults, sin pedidos | CRITICAL |
| `Module_IsDbOrderChecker` | Verifica ModuleId | DbOrderChecker |

### Actualizados por renombramiento

- `CauseClassifierTests`: agrega `ILogger<DbOrderChecker>` mock
- `AlertTemplateRendererTests`: `QuePaso` usa `ctx.CheckDetails` en BD-CRITICAL
- `RuleManagementServiceIntegrationTests`: `ForDbOrders(2,1)` → `ForDbOrders(120,1)`
- `RuleManagementServiceValidationTests`: ídem

---

## 8. No incluido (futuro)

- M3, M4, M11, Brand Monitor configurables por reglas
- Templates de alerta configurables por regla
- Checkboxes en UI para seleccionar canales (hoy es string JSON editable)
