# Tech Stack Decisions — U1 Foundation & Cross-Cutting

**Unidad:** U1 — Foundation & Cross-Cutting
**Stage:** Construction → NFR Requirements
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Stack confirmado para U1

Todas las decisiones de stack para U1 vienen de Inception (ADR-001) o fueron confirmadas en NFR Requirements. Ninguna es nueva.

| Capa | Tecnología | Versión | Decisión |
|------|-----------|---------|----------|
| Runtime | .NET 8 | LTS | ADR-001, Inception |
| Web Framework | ASP.NET Core 8 | — | ADR-001 |
| UI | Blazor Server | — | ADR-001 |
| Real-time | SignalR (integrado en ASP.NET Core) | — | ADR-001 |
| Auth | `Microsoft.AspNetCore.Authentication.Cookies` | integrado .NET 8 | NFR-U1-02; sin Identity library |
| ORM | Entity Framework Core 8 | 8.x | ADR-001 |
| BD desarrollo | SQL Server MonitorPedidosDb (172.16.0.41) | — | ADR-001 |
| Logging | Serilog | 3.x | RNF-08, NFR-U1-03 |
| Serilog sink | `Serilog.Sinks.File` | 5.x | NFR-U1-03 |
| Security headers | Middleware personalizado | — | NFR-U1-04 |
| Tests | xUnit + `Microsoft.AspNetCore.Mvc.Testing` | — | NFR-U1-05 |

---

## §2 Paquetes NuGet de U1

### 2.1 Producción (`MonitorPedidos.Web.csproj`)

| Paquete | Versión mínima | Uso |
|---------|---------------|-----|
| `Microsoft.EntityFrameworkCore` | 8.x | ORM base |
| `Microsoft.EntityFrameworkCore.SqlServer` | 8.x | Provider SQL Server |
| `Microsoft.EntityFrameworkCore.Tools` | 8.x | Migrations CLI |
| `Serilog.AspNetCore` | 8.x | Integración Serilog con ASP.NET Core host |
| `Serilog.Sinks.File` | 5.x | Sink de archivo rotado |
| `Serilog.Enrichers.Environment` | 2.x | Enriquecimiento con `MachineName` |
| `Serilog.Enrichers.Thread` | 3.x | Enriquecimiento con `ThreadId` |

**No incluidos (deliberadamente):**
- `Microsoft.AspNetCore.Identity.*` — no se usa. Selección simple sin Identity library.
- `NetEscapades.AspNetCore.SecurityHeaders` — middleware personalizado elegido (NFR-U1-04).

### 2.2 Tests (`MonitorPedidos.IntegrationTests.csproj`)

| Paquete | Versión | Uso |
|---------|---------|-----|
| `xunit` | 2.x | Framework de tests |
| `xunit.runner.visualstudio` | 2.x | Runner para VS/dotnet test |
| `Microsoft.AspNetCore.Mvc.Testing` | 8.x | `WebApplicationFactory` |
| `Microsoft.NET.Test.Sdk` | — | SDK de test |

---

## §3 Estructura de archivos que genera U1

```
src/
+-- MonitorPedidos.Domain/
|   +-- Identity/
|   |   +-- PredefinedIdentities.cs
|   |   +-- UserIdentity.cs
|   |   +-- ApplicationRole.cs
|   +-- Alerts/
|   |   +-- AlertMessage.cs
|   +-- Monitoring/
|   |   +-- CheckResult.cs
|   |   +-- CheckStatus.cs
|   |   +-- ICheckExecutor.cs
|   +-- Notifications/
|   |   +-- INotificationService.cs
|   +-- Shared/
|       +-- Severity.cs
|       +-- CauseCategory.cs
|       +-- ModuleId.cs
|       +-- IncidentCloseType.cs
|       +-- RuleChangeType.cs
|
+-- MonitorPedidos.Infrastructure/
|   +-- Persistence/
|   |   +-- AppDbContext.cs
|   |   +-- Migrations/              (creadas por EF Core — solo schema base)
|   +-- Logging/
|       +-- SerilogConfiguration.cs  (extension method para Program.cs)
|
+-- MonitorPedidos.Web/
    +-- Program.cs                   (configuración completa del host)
    +-- Areas/
    |   +-- Identity/
    |       +-- Pages/
    |           +-- Select.cshtml
    |           +-- Select.cshtml.cs
    |           +-- Logout.cshtml.cs
    |           +-- AccessDenied.cshtml
    +-- Middleware/
    |   +-- SecurityHeadersMiddleware.cs
    +-- Shared/
    |   +-- MainLayout.razor
    +-- Pages/
    |   +-- Error.cshtml
    |   +-- Error.cshtml.cs
    +-- appsettings.json
    +-- appsettings.Development.json

tests/
+-- MonitorPedidos.IntegrationTests/
    +-- Identity/
    |   +-- IdentitySelectionTests.cs    (T-U1-01, T-U1-02, T-U1-03)
    +-- Security/
        +-- SecurityHeadersTests.cs      (T-U1-04)
        +-- ExceptionHandlerTests.cs     (T-U1-05)
```

---

## §4 Configuración de Program.cs — orden del pipeline

El orden es crítico para la seguridad (definido en `business-logic-model.md` Flujo 4):

```csharp
// Program.cs — orden de middleware (U1)
var app = builder.Build();

// 1. Redirección HTTPS (solo en escenario red interna)
if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

// 2. Security headers (antes de cualquier respuesta)
app.UseSecurityHeaders();

// 3. Archivos estáticos
app.UseStaticFiles();

// 4. Routing
app.UseRouting();

// 5. Autenticación (lee cookie, carga ClaimsPrincipal)
app.UseAuthentication();

// 6. Autorización (evalúa [Authorize] attributes)
app.UseAuthorization();

// 7. Manejo global de excepciones
app.UseExceptionHandler("/Error");

// 8. Endpoints (Blazor + Razor Pages)
app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
```

**Nota:** `GlobalExceptionHandler` se registra como `UseExceptionHandler("/Error")` — ASP.NET Core lo gestiona internamente.

---

## §5 Configuración `appsettings.json` completa para U1

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MonitorPedidosDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Serilog": {
    "Using": ["Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "logs/monitor-pedidos-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 90,
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj} | rid={RequestId}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"]
  }
}
```

---

## §6 Trazabilidad completa

| Decisión | NFR | Business Rule | Story | SECURITY |
|----------|-----|---------------|-------|----------|
| Cookie auth scheme | RNF-14, NFR-U1-02 | BR-COOKIE-01..04 | US-23 | SECURITY-12 |
| CSP Blazor | RNF-09, NFR-U1-01 | BR-HEADER-01 | US-28 | SECURITY-04 |
| SecurityHeadersMiddleware | RNF-09, NFR-U1-04 | BR-HEADER-01..06 | US-28 | SECURITY-04 |
| Serilog texto + logs/ | RNF-08, NFR-U1-03 | BR-LOG-02, BR-LOG-03 | US-25 | SECURITY-03, SECURITY-14 |
| Integration tests | NFR-U1-05 | BR-AUTHZ-01..03, BR-EX-02 | US-23, US-24, US-27 | SECURITY-08, SECURITY-15 |
| EF Core sin Identity | RNF-06 | BR-ID-02 | US-26 | SECURITY-01 |
| Middleware personalizado | NFR-U1-04 | BR-HEADER-01..06 | US-28 | SECURITY-04 |
