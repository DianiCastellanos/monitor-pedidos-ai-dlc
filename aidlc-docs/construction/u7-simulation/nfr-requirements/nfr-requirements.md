# NFR Requirements — U7 Simulation & Red-Teaming

**Unidad:** U7 — Simulation & Red-Teaming
**Stage:** Construction → NFR Requirements
**Fecha:** 2026-05-24
**Versión:** 1.0

---

## §1 Decisiones aplicadas

| ID | Pregunta | Decisión |
|----|----------|---------|
| P1 | Tests para `OrdersSimulatorService` | A — 3 unit tests Moq: NoOrdersMode, FailureProbability=1.0, FailureProbability=0.0 |
| P2 | Estrategia de inserción por tick | A — `AddRangeAsync` + `SaveChangesAsync` — transacción única por tick |
| P3 | Registro en DI | A — `AddHostedService<OrdersSimulatorService>()` — patrón estándar BackgroundService |
| P4 | Scripts SQL: ejecución exclusivamente manual | A — Sin endpoint en producción; ejecución manual con SSMS / sqlcmd |

---

## §2 NFRs propios de U7

### NFR-U7-01 — Tests unitarios: `OrdersSimulatorService` (3 tests)

**Descripción:** Verificar la lógica de decisión del simulador sin invocar el timer ni la BD.

**Tests (xUnit + Moq):**

| # | Escenario | Configuración | Verificación |
|---|-----------|--------------|-------------|
| 1 | `NoOrdersMode=true` | `SimulationOptions { NoOrdersMode=true }` | `ISimulatedOrderRepository.InsertAsync` **nunca** se invoca en el tick |
| 2 | Todos los pedidos son fallos | `SimulationOptions { FailureProbability=1.0 }` | Todos los `SimulatedOrder` insertados tienen `IsFailure=true` y `Status="Error"` |
| 3 | Todos los pedidos son normales | `SimulationOptions { FailureProbability=0.0 }` | Todos los `SimulatedOrder` insertados tienen `IsFailure=false` y `Status="Pending"` |

**Nota sobre `Random.Shared`:** Para que `FailureProbability=1.0` y `=0.0` sean deterministas sin inyectar un `Random`, los tests verifican el outcome de los objetos construidos — no mockean `Random`. Con `FailureProbability=1.0`, `NextDouble()` siempre es < 1.0 (garantizado por la distribución [0,1)); con `=0.0`, siempre es >= 0.0 (nunca < 0.0). Los casos extremos son deterministas sin mock.

**Total tests U7:** 3

---

### NFR-U7-02 — Inserción por lote en una transacción

**Descripción:** El simulador inserta los 8 pedidos del tick en una sola llamada a `SaveChangesAsync`.

**Implementación:**

```csharp
// OrdersSimulatorService — lógica de inserción por tick
var orders = new List<SimulatedOrder>();

foreach (var source in _sources)
    foreach (var site in _sites)
    {
        bool fail = Random.Shared.NextDouble() < _options.FailureProbability;
        orders.Add(fail
            ? SimulatedOrder.CreateFailure(source, site, "Error")
            : SimulatedOrder.CreateNormal(source, site));
    }

await _repo.InsertRangeAsync(orders, ct);   // AddRangeAsync + SaveChangesAsync
```

**Rationale:** 8 filas por tick es trivial en volumen, pero la transacción única garantiza que `DbOrderChecker` nunca lea un estado parcial (por ejemplo: solo 4 de 8 pedidos insertados).

---

### NFR-U7-03 — Sin endpoint de simulación en producción

**Descripción:** Los scripts SQL de red-teaming se ejecutan exclusivamente de forma manual. Ningún endpoint HTTP ni página Blazor ejecuta SQL arbitrario.

**Verificación:** Revisar `Program.cs` que no exista ningún `MapPost("/api/simulation/...")` ni `SqlCommand` ejecutando los scripts. Los scripts `.sql` son archivos estáticos en `db/seed/` — sin conexión al runtime de la aplicación.

**Rationale:** SECURITY-01 (superficie de ataque mínima). Un endpoint de reset podría ser explotado para modificar la BD en un entorno compartido.

---

### NFR-U7-04 — `OrdersSimulatorService` desactivable sin recompilar

**Descripción:** `Simulation:Enabled=false` detiene la inserción de pedidos sin reinicio de la aplicación (requiere cambio de config y restart del proceso, pero sin recompilación).

**Implementación:**

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    if (!_options.Enabled)
    {
        _logger.LogInformation("Simulador desactivado (Simulation:Enabled=false)");
        return;  // sale inmediatamente, sin loop
    }
    // ... loop de PeriodicTimer
}
```

---

## §3 NFRs heredados de U1 aplicables a U7

| NFR | Aplicación en U7 |
|-----|-----------------|
| NFR-U1-03 (Serilog) | `OrdersSimulatorService` loguea cada tick con `LogDebug` (baseline) y `LogInformation` para cambios de modo (NoOrdersMode activado/desactivado) |
| NFR-U1-04 (GlobalExceptionHandler) | Excepciones en el loop del simulador son capturadas con `try/catch` interno — no deben matar el proceso |

---

## §4 Trazabilidad NFR → RT

| NFR | RT cubierto | Componente |
|-----|------------|-----------|
| NFR-U7-01 (tests simulador) | RT1, RT5 | `OrdersSimulatorService`, `OrdersSimulatorServiceTests` |
| NFR-U7-02 (lote único) | todos | `ISimulatedOrderRepository.InsertRangeAsync` |
| NFR-U7-03 (sin endpoint) | todos | Verificación estática de `Program.cs` |
| NFR-U7-04 (desactivable) | post-demo | `SimulationOptions.Enabled` |
