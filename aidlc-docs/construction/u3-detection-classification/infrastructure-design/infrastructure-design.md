# Infrastructure Design — U3 Detection & Classification

**Unidad:** U3 — Detection & Classification
**Stage:** Construction → Infrastructure Design
**Fecha:** 2026-05-23
**Versión:** 1.1 *(corregido: P1=B — U7 es dueño exclusivo de entidades y migrations simuladas)*

---

## §1 Resumen de decisiones

| Decisión | Opción elegida |
|----------|---------------|
| Entidades `SimulatedOrder` / `SimulatedJobStatus` | **U7 define todo** — entidades, DbSets, migrations. U3 solo usa `IOrderSource` / `IJobStatusSource`. U3 no toca `AppDbContext` |
| Proyecto de unit tests | Nuevo `MonitorPedidos.UnitTests` separado de IntegrationTests |
| Intervalo en Development | `CheckerIntervalMinutes: 1` en `appsettings.Development.json` |

---

## §2 Componentes de U1/U2 reutilizados sin cambios

| Componente U1/U2 | Uso en U3 |
|-----------------|-----------|
| `AppDbContext` | **Sin cambios en U3** — U7 agrega los DbSets de tablas simuladas |
| `MonitorPedidosDb (MonitorPedidosDb)` | Tablas simuladas creadas y gestionadas por U7 |
| Kestrel | Sin cambios — U3 es 100% backend, sin nuevas rutas HTTP |
| Serilog (File Sink) | `MonitoringService` y checkers usan el mismo sink de U1 |
| `GlobalExceptionHandler` (U1) | Captura excepciones de `MonitoringService` si escalan |
| `FallbackPolicy` (U1) | N/A — U3 no expone páginas |

---

## §3 Separación de responsabilidades U3 / U7

Esta es la decisión arquitectónica clave de Infrastructure Design para U3:

```
U3 — Detection & Classification
    DEFINE:  IOrderSource (interfaz)
             IJobStatusSource (interfaz)
             DbOrderChecker   → usa IOrderSource (no sabe de simulated_orders)
             JobsChecker      → usa IJobStatusSource (no sabe de simulated_job_statuses)

U7 — Simulation & Red-Teaming
    DEFINE:  SimulatedOrder          (entidad EF Core)
             SimulatedJobStatus      (entidad EF Core)
             AppDbContext.SimulatedOrders    (DbSet — agregado en U7)
             AppDbContext.SimulatedJobStatuses (DbSet — agregado en U7)
             Migration AddSimulatedTables    (crea las tablas en MonitorPedidosDb)
             SimulatedOrderRepository : IOrderSource
             SimulatedJobStatusRepository : IJobStatusSource

Program.cs — registro DI (coordinación U3 + U7)
    U3 registra: checkers, MonitoringService, MonitoringSchedulerService
    U7 registra: SimulatedOrderRepository como IOrderSource
                 SimulatedJobStatusRepository como IJobStatusSource
```

**Beneficio:** si en el futuro se reemplaza U7 por una fuente de datos real, U3 no cambia nada — solo se cambia el registro en DI.

---

## §4 Proyecto MonitorPedidos.UnitTests

### 4.1 Estructura

```
tests/
+-- MonitorPedidos.IntegrationTests/      (existente — U1/U2/U3 integration)
+-- MonitorPedidos.UnitTests/             (nuevo en U3)
    +-- Monitoring/
    |   +-- CauseClassifierTests.cs       (T-U3-01)
    |   +-- AlertTemplateRendererTests.cs (T-U3-02)
    |   +-- DbOrderCheckerTests.cs        (T-U3-04)
    +-- MonitorPedidos.UnitTests.csproj
```

### 4.2 Archivo .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk"        Version="17.*" />
    <PackageReference Include="xunit"                         Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio"     Version="2.*" />
    <PackageReference Include="Moq"                           Version="4.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\MonitorPedidos.Web\MonitorPedidos.Web.csproj" />
  </ItemGroup>
</Project>
```

### 4.3 Agregar a la solución

```bash
dotnet sln MonitorPedidos.sln add tests/MonitorPedidos.UnitTests/MonitorPedidos.UnitTests.csproj
```

### 4.4 Comparación de tiempos de ejecución

| Proyecto | Requiere MonitorPedidosDb | Tiempo estimado | Cuándo correr |
|---------|-----------------|----------------|---------------|
| `MonitorPedidos.UnitTests` | No | < 2 segundos | En cada cambio (fast feedback) |
| `MonitorPedidos.IntegrationTests` | Sí | 10-30 segundos | Antes de commit / en CI |

---

## §5 Configuración — nuevas claves de U3

### 5.1 appsettings.json (base — producción)

```json
{
  "Monitoring": {
    "CheckerIntervalMinutes":       5,
    "CheckerTimeoutMs":         30000,
    "OrderDetectionWindowMinutes": 30,
    "DbWarnLatencyMs":           1000,
    "DbCritTimeoutMs":           5000
  }
}
```

### 5.2 appsettings.Development.json

```json
{
  "Monitoring": {
    "CheckerIntervalMinutes":       1,
    "OrderDetectionWindowMinutes":  5
  }
}
```

En desarrollo: ciclo cada **1 minuto** y ventana de **5 minutos** para ver resultados rápido sin esperar.

### 5.3 appsettings.Testing.json

```json
{
  "Monitoring": {
    "CheckerIntervalMinutes":       1,
    "OrderDetectionWindowMinutes":  1,
    "CheckerTimeoutMs":          5000
  }
}
```

---

## §6 Registro DI en Program.cs

```csharp
// ── U3: Detection & Classification ──────────────────────────────────────
// Checkers — IEnumerable<ICheckExecutor> resuelve los 3 automáticamente
builder.Services.AddScoped<ICheckExecutor,     DbOrderChecker>();
builder.Services.AddScoped<ICheckExecutor,     DbHealthChecker>();
builder.Services.AddScoped<ICheckExecutor,     JobsChecker>();
builder.Services.AddScoped<IMonitoringService, MonitoringService>();
builder.Services.AddHostedService<MonitoringSchedulerService>();
// ─────────────────────────────────────────────────────────────────────────

// ── U7: Simulation & Red-Teaming (provee implementaciones para U3) ───────
builder.Services.AddScoped<IOrderSource,    SimulatedOrderRepository>();
builder.Services.AddScoped<IJobStatusSource, SimulatedJobStatusRepository>();
// ─────────────────────────────────────────────────────────────────────────
```

> U3 no registra `IOrderSource` ni `IJobStatusSource` — esa es responsabilidad de U7. Si U7 no está implementada todavía, se puede usar un stub temporal hasta que U7 esté lista.

---

## §7 Checklist de puesta en marcha de U3

```text
[ ] Crear proyecto tests/MonitorPedidos.UnitTests y agregar a la solución
[ ] Agregar sección "Monitoring" en appsettings.json, .Development.json y .Testing.json
[ ] Registrar servicios U3 en Program.cs (checkers + MonitoringService + HostedService)
[ ] dotnet build → confirmar compilación sin errores
[ ] dotnet test MonitorPedidos.UnitTests → T-U3-01, T-U3-02, T-U3-04 pasan (sin MonitorPedidosDb)
[ ] dotnet run → confirmar que MonitoringSchedulerService loggea arranque
[ ] Esperar 1 minuto (Dev) → confirmar tick en logs: "Check OK: DbHealth"
[ ] NOTA: IOrderSource e IJobStatusSource son registradas por U7.
         Hasta que U7 esté lista, registrar stubs temporales en Program.cs.
[ ] NOTA: T-U3-03 y T-U3-05 requieren la migration de U7 (simulated_orders) ejecutada primero
```

---

## §8 Trazabilidad

| Componente | ADR | NFR | Story | Dependencia |
|-----------|-----|-----|-------|------------|
| Separación U3/U7 (interfaces vs implementaciones) | — | — | US-07, US-09 | U7 implementa IOrderSource, IJobStatusSource |
| `MonitorPedidos.UnitTests` project | ADR-U3-04 | NFR-U3-02, NFR-U3-03 | US-06..09 | — |
| `Monitoring` config section | ADR-U3-01, ADR-U3-03 | NFR-U3-01 | US-07..09 | — |
| DI registration (IEnumerable ICheckExecutor) | ADR-U3-02 | — | US-07..09 | U4 extiende sin tocar scheduler |
