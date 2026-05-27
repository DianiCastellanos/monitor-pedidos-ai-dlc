# Infrastructure Design — U4 External Integrations

**Unidad:** U4 — External Integrations
**Stage:** Construction → Infrastructure Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Decisiones aplicadas

| ID | Pregunta | Decisión |
|----|----------|---------|
| P1 | Migración EF Core para retry_metadata | A — Nueva migración CLI: `AddRetryMetadataToIncidents` |
| P2 | ApiCheckerIntervalMinutes en Development | A — `Monitoring:ApiCheckerIntervalMinutes: 1` en appsettings.Development.json |
| P3 | Registro de checkers de API en DI | A — `AddSingleton<ICheckExecutor, SalesforceApiChecker>()` — igual que U3 |

---

## §2 Migración EF Core — columna retry_metadata

### Comando de migración

```bash
dotnet ef migrations add AddRetryMetadataToIncidents --project src/MonitorPedidos.Web
dotnet ef database update --project src/MonitorPedidos.Web
```

### Configuración de la entidad (OnModelCreating)

```csharp
// AppDbContext.OnModelCreating — extensión U4
modelBuilder.Entity<Incident>(b =>
{
    // Columna existente de U2 — no modificar
    // ...

    // Nueva columna U4
    b.Property(i => i.RetryMetadataJson)
     .HasColumnName("retry_metadata")
     .HasColumnType("nvarchar(max)")
     .IsRequired(false);
});
```

### Secuencia de migraciones en el proyecto

```
Migrations/
    ├── 20260520_InitialCreate.cs          ← U2: tabla incidents, users, jobs
    └── 20260523_AddRetryMetadataToIncidents.cs  ← U4: columna retry_metadata
```

**Nota:** Las migraciones de tablas simuladas (`simulated_orders`, `simulated_job_statuses`) serán generadas por U7, no por U4.

---

## §3 Configuración appsettings

### appsettings.json (valores de producción / demo)

```json
{
  "Monitoring": {
    "CheckerIntervalMinutes": 5,
    "ApiCheckerIntervalMinutes": 10
  },
  "Salesforce": {
    "BaseUrl": ""
  },
  "Multivende": {
    "BaseUrl": ""
  }
}
```

**Las API Keys nunca en appsettings.json** — solo `BaseUrl` (no es secreto). Las keys van en User Secrets o variables de entorno.

### appsettings.Development.json (extensión U4)

```json
{
  "Monitoring": {
    "CheckerIntervalMinutes": 1,
    "ApiCheckerIntervalMinutes": 1
  },
  "Salesforce": {
    "BaseUrl": "https://sandbox.salesforce.com/services/data/v58.0/"
  },
  "Multivende": {
    "BaseUrl": "https://api.multivende.com/apps/"
  }
}
```

---

## §4 Registro en DI — Program.cs (extensión U4)

```csharp
// === U4: API Checkers ===
builder.Services.AddSingleton<ICheckExecutor, SalesforceApiChecker>();
builder.Services.AddSingleton<ICheckExecutor, MultivendeApiChecker>();

// === U4: Typed HTTP Clients ===
var apiRetryPolicy = ApiRetryPolicy.Create(
    builder.Services.BuildServiceProvider()
           .GetRequiredService<ILogger<ApiRetryPolicy>>());

builder.Services
    .AddHttpClient<ISalesforceClient, SalesforceClient>(c =>
    {
        c.BaseAddress = new Uri(builder.Configuration["Salesforce:BaseUrl"]!);
        c.Timeout     = TimeSpan.FromSeconds(10);
    })
    .AddPolicyHandler(apiRetryPolicy);

builder.Services
    .AddHttpClient<IMultivendeClient, MultivendeClient>(c =>
    {
        c.BaseAddress = new Uri(builder.Configuration["Multivende:BaseUrl"]!);
        c.Timeout     = TimeSpan.FromSeconds(10);
    })
    .AddPolicyHandler(apiRetryPolicy);
```

---

## §5 User Secrets — configuración inicial (una vez por desarrollador)

```bash
dotnet user-secrets init --project src/MonitorPedidos.Web

dotnet user-secrets set "Salesforce:ApiKey" "sf_sandbox_key_xxx" --project src/MonitorPedidos.Web
dotnet user-secrets set "Multivende:ApiKey" "mv_sandbox_key_xxx" --project src/MonitorPedidos.Web
```

### Variables de entorno para demo interna (sin User Secrets)

```powershell
$env:Salesforce__ApiKey  = "sf_key_produccion"
$env:Salesforce__BaseUrl = "https://salesforce.empresa.com/api/"
$env:Multivende__ApiKey  = "mv_key_produccion"
$env:Multivende__BaseUrl = "https://api.multivende.com/apps/"
```

---

## §6 Nuevo proyecto — MonitorPedidos.UnitTests (paquetes U4)

El proyecto `MonitorPedidos.UnitTests` fue creado en U3. U4 agrega los siguientes paquetes:

```xml
<!-- MonitorPedidos.UnitTests.csproj — paquetes adicionales U4 -->
<PackageReference Include="RichardSzalay.MockHttp" Version="6.*" />
```

Los paquetes existentes de U3 (xUnit, Moq, FluentAssertions, coverlet) se mantienen sin cambios.

---

## §7 Trazabilidad de decisiones de infraestructura

| Decisión | Componente afectado | NFR / ADR |
|----------|-------------------|-----------|
| Migración `AddRetryMetadataToIncidents` | `AppDbContext`, tabla `incidents` | ADR-U4-01 (RetryAttempt en Incident) |
| `ApiCheckerIntervalMinutes: 1` en Dev | `MonitoringSchedulerService` | BR-SCHED-05 |
| `AddSingleton<ICheckExecutor, SalesforceApiChecker>()` | DI container | ADR-U3-02, ADR-U4-03 |
| `AddSingleton<ICheckExecutor, MultivendeApiChecker>()` | DI container | ADR-U3-02, ADR-U4-03 |
| `AddHttpClient<ISalesforceClient>().AddPolicyHandler(policy)` | DI container | ADR-U4-02 |
| `AddHttpClient<IMultivendeClient>().AddPolicyHandler(policy)` | DI container | ADR-U4-02 |
| `HttpClient.Timeout = 10s` | SalesforceClient, MultivendeClient | NFR-U4-01 |
| User Secrets para API keys | SalesforceClient, MultivendeClient | BR-CRED-01, BR-CRED-02 |
| `RichardSzalay.MockHttp` en UnitTests | SalesforceClientTests, ApiRetryPolicyTests | NFR-U4-02, NFR-U4-03 |
