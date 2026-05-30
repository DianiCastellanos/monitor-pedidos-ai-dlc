# M2 Configurable por Reglas — Design Doc

**Fecha:** 2026-05-29
**Contexto:** Evolucionar M2 (DbOrderChecker) para que su comportamiento se configure desde la UI de Reglas de Monitoreo en lugar de estar hardcodeado.

---

## 1. Estado Actual

| Componente | Estado |
|---|---|
| Entidad `Rule` + `RuleCondition` | ✅ Construido — `WindowMinutes`, `MinOrders` |
| Repositorio `IRuleRepository.GetActiveByModuleAsync()` | ✅ Construido |
| `RulesPage` (lista, crear, editar, historial) | ✅ Construido |
| `DbOrderChecker` lee reglas para `WindowMinutes` | ✅ Construido |
| `DbOrderChecker` lógica OK/WARN/CRITICAL | ❌ Hardcodeada — threshold fijo, channels fijos |
| `DbOrderChecker` lee `MinOrders` desde regla | ❌ No implementado |
| Regla semilla por defecto para M2 | ❌ No existe → pantalla /rules vacía |

---

## 2. Cambios

### 2.1 `RuleCondition` — Nuevo campo `Channels`

Agregar `string[]? Channels` al record `RuleCondition` para que el checker pueda leer desde la regla qué canales evaluar, en vez de tenerlos hardcodeados.

```json
{
  "WindowMinutes": 10,
  "MinOrders": 1,
  "Channels": ["SALESFORCE", "MULTIVENDE"]
}
```

**Sin regla** o `Channels = null` → fallback a `["SALESFORCE", "MULTIVENDE"]` (comportamiento actual).

### 2.2 `Rule.AppliesTo` → `Rule.ModuleId`

Renombrar la propiedad `AppliesTo` a `ModuleId` para consistencia con el sistema (`HistoricPage`, `WeeklySummaryPage`, etc.). El contenido sigue siendo el mismo (`DbOrderChecker`, `ApiCheckers`, etc. — el nombre del enum `ModuleId`).

Esto implica:
- Renombrar propiedad en entidad `Rule.cs`
- Agregar `[Column("AppliesTo")]` temporalmente para no perder datos existentes, O crear migración que renombre la columna en SQL Server

### 2.3 Migración Seed — Regla por defecto para M2

Agregar en nueva migración:

| Campo | Valor |
|---|---|
| `Name` | "Ventana de pedidos — Salesforce/Multivende" |
| `Description` | "Alerta si no hay pedidos en la ventana esperada para Salesforce o Multivende" |
| `ModuleId` | `DbOrderChecker` |
| `ConditionJson` | `{"WindowMinutes": 10, "MinOrders": 1, "Channels": ["SALESFORCE", "MULTIVENDE"]}` |
| `Severity` | `Critical` |
| `IsActive` | `true` |

### 2.4 DbOrderChecker — Leer MinOrders + Channels desde regla

Hoy el checker:
1. Carga reglas activas para `DbOrderChecker`
2. Extrae `WindowMinutes` (fallback 10 min)
3. Cuenta pedidos por canal (`SALESFORCE`, `MULTIVENDE`) — hardcodeado
4. Aplica lógica fija: ambos > 0 → OK, uno = 0 → WARN, ambos = 0 → CRITICAL

Cambio: después de obtener `WindowMinutes`, obtener también `MinOrders` y `Channels` desde `RuleCondition`:

```
minOrders = rule?.GetCondition().MinOrders ?? 1
channels  = rule?.GetCondition().Channels ?? ["SALESFORCE", "MULTIVENDE"]
```

Lógica de estado usando `MinOrders` como threshold:

```
if (both channels >= MinOrders)           → OK
else if (both channels < MinOrders)       → CRITICAL
else                                      → WARNING
```

**Sin regla configurada:** `MinOrders = 1`, `Channels = ["SALESFORCE", "MULTIVENDE"]` → comportamiento idéntico al actual.

### 2.5 UI — Ajustes menores

La pantalla `/rules` ya filtra por `AppliesTo` (pronto `ModuleId`). Verificar que:
- El `EnumDropDown` de módulo en el formulario funciona con `ModuleId`
- El campo `Channels` se muestra como texto libre (JSON array) en la UI — por ahora editable como string JSON o un input simple; en futura iteración se puede mejorar con checkboxes

---

## 3. Archivos modificados

| Archivo | Cambio |
|---|---|
| `src/MonitorPedidos.Domain/Rules/RuleCondition.cs` | Agregar `string[]? Channels` |
| `src/MonitorPedidos.Domain/Rules/Rule.cs` | Renombrar `AppliesTo` → `ModuleId` |
| `src/MonitorPedidos.Infrastructure/Persistence/AppDbContext.cs` | Seed data para regla M2, ajustar mapping |
| `src/MonitorPedidos.Web/Features/Monitoring/DbOrderChecker.cs` | Leer `MinOrders` y `Channels` desde regla |
| `src/MonitorPedidos.Web/Components/Pages/Rules/RulesPage.razor` | Ajustar binding `AppliesTo` → `ModuleId` (si aplica) |
| `src/MonitorPedidos.Web/Components/Pages/Rules/RuleEditPage.razor` | ídem |
| `src/MonitorPedidos.Infrastructure/Rules/RuleRepository.cs` | Actualizar query `AppliesTo` → `ModuleId` |
| `aidlc-docs/construction/sqlserver-migration-plan.md` | Documentar migración seed |

---

## 4. Compatibilidad

- Sin regla configurada → `MinOrders = 1`, `Channels = ["SALESFORCE", "MULTIVENDE"]` → mismo comportamiento que hoy
- Regla existente sin `Channels` → `Channels = null` → fallback a hardcodeado
- Regla existente sin `MinOrders` → `MinOrders = null` → fallback a 1
- Migración con rename de columna no pierde datos

---

## 5. Implementación — Resultado final

### 5.1 Archivos modificados (entregado)

| Archivo | Cambio |
|---|---|
| `src/MonitorPedidos.Domain/Rules/RuleCondition.cs` | +`string[]? Channels`; `ForDbOrders` acepta `channels`; `IsValidForModule` valida `Channels.Length > 0` |
| `src/MonitorPedidos.Domain/Rules/Rule.cs` | `AppliesTo` → `ModuleId` |
| `src/MonitorPedidos.Domain/Rules/RuleSnapshot.cs` | `rule.AppliesTo` → `rule.ModuleId` |
| `src/MonitorPedidos.Infrastructure/Persistence/Configurations/RuleConfiguration.cs` | `ModuleId` con `.HasColumnName("AppliesTo")`; index actualizado |
| `src/MonitorPedidos.Infrastructure/Rules/RuleRepository.cs` | `.Where(r => r.ModuleId == module)` |
| `src/MonitorPedidos.Infrastructure/Persistence/AppDbContext.cs` | Seed data: regla M2 + using `MonitorPedidos.Domain.Shared` |
| `src/MonitorPedidos.Web/Features/Monitoring/DbOrderChecker.cs` | Lectura de `MinOrders` + `Channels` desde regla; conteo vía `GroupBy(OrderId).ToDictionary()`; lógica `okCount == channels.Length` → OK, `okCount == 0` → CRITICAL, else → WARN |
| `src/MonitorPedidos.Web/Components/Pages/Rules/RulesPage.razor` | `@rule.AppliesTo` → `@rule.ModuleId` |
| `src/MonitorPedidos.Web/Components/Pages/Rules/RuleEditPage.razor` | `rule.AppliesTo` → `rule.ModuleId`; pasa `["SALESFORCE", "MULTIVENDE"]` a `ForDbOrders` |
| Migración `20260530034007_MakeRulesConfigurable` | Seed data insertada en `rules` |

### 5.2 Bug corregido post-implementación

**Síntoma:** Dashboard mostraba Salesforce=1 y resultado CRITICAL en vez de WARNING con SF=4, MV=0.

**Root cause:** El refactor inicial cambió el conteo de `GroupBy(OrderId).ToDictionary(k=>k, v=>v.Count())` (original, correcto) a `.Distinct().ToHashSet()` + `StartsWith()`. Como `OrderId` = `ChannelName` (e.g. "SALESFORCE"), `Distinct()` colapsaba N registros en 1 entrada, dando siempre count=1.

**Fix:** Revertir a `GroupBy(OrderId).ToDictionary()` + `GetValueOrDefault(ch, 0)`. 

### 5.3 Comportamiento final

- Sin regla configurada → `MinOrders=1`, `Channels=["SALESFORCE","MULTIVENDE"]` (idéntico al original)
- Con regla activa → usa `MinOrders` y `Channels` desde la BD
- Tabla `/rules` muestra la regla semilla; Técnico puede editarla desde UI
- Build: 0 errores, 0 warnings
- Migración `MakeRulesConfigurable` aplicada contra `172.16.0.41`

---

## 6. No incluido (futuro)

- M3, M4, M11, Brand Monitor configurables por reglas
- Templates de alerta configurables por regla
- Checkboxes en UI para seleccionar canales
