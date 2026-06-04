# Tech Stack Decisions — U3 Detection & Classification

**Unidad:** U3 — Detection & Classification
**Stage:** Construction → NFR Requirements
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Stack de U3 — todo heredado de U1/U2

U3 no introduce nuevas tecnologías ni paquetes NuGet.

| Capa | Tecnología | Versión | Fuente |
|------|-----------|---------|--------|
| Scheduler | `BackgroundService` + `PeriodicTimer` | .NET 8 (built-in) | U1, ADR-001 |
| ORM | Entity Framework Core 8 | 8.x | U1, ADR-001 |
| BD | SQL Server MonitorPedidosDb (172.16.0.41) | 2019+ | U1, ADR-001 |
| Logging | Serilog (File Sink) | 3.x | U1, NFR-U1-03 |
| Web | ASP.NET Core 8 + Blazor Server | — | U1, ADR-001 |
| Tests | xUnit + WebApplicationFactory | — | U1, NFR-U1-05 |

**Nuevos paquetes NuGet en U3:** ninguno.

---

## §2 Estructura de archivos que genera U3

```
src/
+-- MonitorPedidos.Domain/
|   +-- Shared/
|   |   +-- CheckStatus.cs                      (enum — nuevo en U3)
|   |   +-- CauseCategory.cs                    (enum — nuevo en U3)
|   +-- Monitoring/
|       +-- CheckResult.cs                      (value object)
|       +-- CheckContext.cs                     (value object)
|       +-- OrderSnapshot.cs                    (value object)
|       +-- JobStatusSnapshot.cs                (value object)
|       +-- ICheckExecutor.cs                   (interfaz)
|       +-- IOrderSource.cs                     (interfaz)
|       +-- IJobStatusSource.cs                 (interfaz)
|
+-- MonitorPedidos.Web/
    +-- Features/
    |   +-- Monitoring/
    |       +-- MonitoringSchedulerService.cs   (BackgroundService — M1)
    |       +-- DbOrderChecker.cs               (ICheckExecutor — M2)
    |       +-- DbHealthChecker.cs              (ICheckExecutor — M4)
    |       +-- JobsChecker.cs                  (ICheckExecutor — M11)
    |       +-- CauseClassifier.cs              (static — M7)
    |       +-- AlertTemplateRenderer.cs        (static — M10)
    |       +-- SimulatedOrderRepository.cs     (IOrderSource — Sprint 2)
    |       +-- SimulatedJobStatusRepository.cs (IJobStatusSource — Sprint 2)
    +-- Services/
        +-- MonitoringService.cs                (application service — orquestador)
        +-- IMonitoringService.cs               (contrato)

tests/
+-- MonitorPedidos.IntegrationTests/
|   +-- Monitoring/
|       +-- DbHealthCheckerTests.cs             (T-U3-03)
|       +-- MonitoringServiceTests.cs           (T-U3-05)
+-- MonitorPedidos.UnitTests/                   (proyecto nuevo para tests puros)
    +-- Monitoring/
        +-- CauseClassifierTests.cs             (T-U3-01)
        +-- AlertTemplateRendererTests.cs       (T-U3-02)
        +-- DbOrderCheckerTests.cs              (T-U3-04)
```

> **Nota:** U3 introduce el proyecto `MonitorPedidos.UnitTests` para los tests puros (sin BD). `MonitorPedidos.IntegrationTests` ya existe desde U1/U2.

---

## §3 Configuración EF Core específica de U3

### 3.1 Tablas simuladas (Sprint 2 — U7 las crea, U3 las lee)

U3 lee de tablas creadas por U7 (`simulated_orders`, `simulated_job_statuses`). En U3 solo se definen las entidades de lectura; U7 crea la configuración completa.

```csharp
// AppDbContext — DbSets de lectura para U3 (agregados en OnModelCreating)
public DbSet<SimulatedOrder>     SimulatedOrders     => Set<SimulatedOrder>();
public DbSet<SimulatedJobStatus> SimulatedJobStatuses => Set<SimulatedJobStatus>();
```

Las entidades `SimulatedOrder` y `SimulatedJobStatus` son entidades de solo lectura en U3 — sin configuración de escritura ni migrations propias (U7 las crea).

### 3.2 Registro DI en Program.cs

```csharp
// ── U3: Detection & Classification ──────────────────────────────────────
builder.Services.AddScoped<IOrderSource,      SimulatedOrderRepository>();
builder.Services.AddScoped<IJobStatusSource,  SimulatedJobStatusRepository>();
builder.Services.AddScoped<ICheckExecutor,    DbOrderChecker>();
builder.Services.AddScoped<ICheckExecutor,    DbHealthChecker>();
builder.Services.AddScoped<ICheckExecutor,    JobsChecker>();
builder.Services.AddScoped<IMonitoringService, MonitoringService>();
builder.Services.AddHostedService<MonitoringSchedulerService>();
// ─────────────────────────────────────────────────────────────────────────
```

> **Múltiples implementaciones de `ICheckExecutor`:** `MonitoringSchedulerService` resuelve todas las implementaciones vía `IEnumerable<ICheckExecutor>` — patrón estándar de ASP.NET Core DI que permite agregar checkers nuevos (U4) solo con registrar su implementación.

---

## §4 Proyecto MonitorPedidos.UnitTests

```xml
<!-- tests/MonitorPedidos.UnitTests/MonitorPedidos.UnitTests.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
    <PackageReference Include="xunit" Version="2.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
    <PackageReference Include="Moq" Version="4.*" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\MonitorPedidos.Web\MonitorPedidos.Web.csproj" />
  </ItemGroup>
</Project>
```

**Paquete nuevo:** `Moq` (solo en el proyecto de unit tests). No se agrega a producción.

---

## §5 Trazabilidad de decisiones

| Decisión | NFR | Business Rule | SECURITY |
|----------|-----|---------------|----------|
| Timeout 30s por checker | NFR-U3-01 | BR-DET-03, BR-SCHED-03 | SECURITY-15 |
| Unit tests M7/M10 (sin BD) | NFR-U3-02 | BR-CLASS, BR-ALERT | — |
| Integration tests M4/MonitoringService | NFR-U3-03 | BR-HEALTH, BR-DET | SECURITY-05 |
| Logging Debug/Information diferenciado | NFR-U3-04 | BR-DET-02, BR-CLASS-03 | SECURITY-14 |
| IEnumerable<ICheckExecutor> en DI | — | BR-SCHED-02 | — |
