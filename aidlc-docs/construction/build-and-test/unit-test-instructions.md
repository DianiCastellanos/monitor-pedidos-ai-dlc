# Unit Test Instructions — MonitorPedidos

**Fecha**: 2026-05-24
**Proyecto**: MonitorPedidos — Manufacturas Eliot
**Test Project**: `MonitorPedidos.UnitTests`
**Framework**: xUnit + Moq

> **Estado**: Act5 (Code Generation) pendiente de activación — este documento describe los tests planificados para cuando el código esté generado. Los 38 tests listados aquí corresponden al diseño aprobado de las 7 unidades de construcción.

---

## §1 Ejecutar Todos los Tests

**Ejecutar la suite completa**:
```bash
dotnet test MonitorPedidos.UnitTests
```

**Ejecutar con output detallado**:
```bash
dotnet test MonitorPedidos.UnitTests --verbosity normal
```

**Ejecutar con diagnóstico completo** (útil para CI o fallo inexplicable):
```bash
dotnet test MonitorPedidos.UnitTests --verbosity diagnostic
```

**Ejecutar en modo Release**:
```bash
dotnet test MonitorPedidos.UnitTests --configuration Release
```

La ejecución debe reportar **38 tests pasados, 0 fallos, 0 omitidos** en condiciones normales.

---

## §2 Tests por Unidad

### U1 — Foundation (5 tests de integración)

**Clase**: `SecurityIntegrationTests`
**Tipo**: Integration (WebApplicationFactory)
**Total**: 5 tests

| Test | Escenario | Resultado esperado |
|---|---|---|
| `Login_ValidCredentials_SetsCookieAndRedirectsToDashboard` | POST /Identity/Select con credenciales válidas | Cookie `.AspNetCore.Identity.Application` presente, redirect 302 a `/Dashboard` |
| `SecurityHeaders_OnAllResponses_PresentAndCorrect` | GET en cualquier página autenticada | Headers `X-Content-Type-Options`, `X-Frame-Options`, `Content-Security-Policy` presentes |
| `AccountLockout_AfterFiveFailedAttempts_Returns403` | 5 intentos fallidos de login consecutivos | Respuesta 403 o redirect a página de bloqueo |
| `OperadorRole_AccessToAdminPages_Returns403` | Usuario con rol Operador accede a `/ReglasMonitoreo` | HTTP 403 Forbidden |
| `UnhandledException_GenericErrorPage_NoStackTraceExposed` | Request que provoca excepción no manejada | Página de error genérica, sin stack trace expuesto en el HTML |

**Configuración de WebApplicationFactory**:
```csharp
// En Program.cs o en el factory, usar base de datos InMemory o MonitorPedidosDb de test
// Ver §2 de integration-test-instructions.md para configuración detallada
```

---

### U2 — Persistence (6 tests)

**Clases**: `IncidentDomainTests` (unitarios), `IncidentServiceIntegrationTests` (integración con BD)
**Tipo**: Unit + Integration
**Total**: 6 tests

| Test | Escenario | Resultado esperado |
|---|---|---|
| `Incident_TransitionToOpen_FromDetected_Succeeds` | Transición de estado `Detected` → `Open` | Estado actualizado, timestamp registrado |
| `Incident_TransitionToResolved_FromOpen_Succeeds` | Transición de estado `Open` → `Resolved` | Estado `Resolved`, `ResolvedAt` no nulo |
| `Incident_TransitionToOpen_FromResolved_ThrowsDomainException` | Transición inválida `Resolved` → `Open` | `DomainException` lanzada |
| `DetermineStatus_PendingOrdersAboveThreshold_ReturnsRed` | >N pedidos pendientes | `IncidentStatus.Red` |
| `DetermineStatus_PendingOrdersNearThreshold_ReturnsYellow` | Pedidos en zona de alerta | `IncidentStatus.Yellow` |
| `IIncidentService_GetHistoryAsync_FiltersAndPaginatesCorrectly` | Consulta con filtros de fecha + marca | Registros filtrados correctamente, paginación respetada |

---

### U3 — Detection (7 tests)

**Clases**: `MonitoringServiceTests` (3 tests), `BrandMonitorCheckerTests` (4 tests)
**Tipo**: Unit
**Total**: 7 tests

**MonitoringServiceTests**:

| Test | Escenario | Resultado esperado |
|---|---|---|
| `StartAsync_WhenEnabled_ExecutesCheckerOnSchedule` | Servicio habilitado arranca | `BrandMonitorChecker.CheckAllBrandsAsync` es invocado al menos una vez |
| `StartAsync_WhenDisabled_DoesNotExecuteChecker` | Servicio deshabilitado | `CheckAllBrandsAsync` nunca es invocado |
| `ExecuteAsync_CheckerThrowsException_LogsErrorAndContinues` | Checker lanza excepción en un ciclo | Error logueado, servicio continúa en el próximo ciclo |

**BrandMonitorCheckerTests** (compartidos con U6 — ver nota en §6):

| Test | Escenario | Resultado esperado |
|---|---|---|
| `CheckAllBrandsAsync_AllBrandsHealthy_NoIncidentCreated` | Todos los sites responden OK | No se crea ningún incidente |
| `CheckAllBrandsAsync_OneBrandFails_CreatesIncident` | Un site falla (simulado con mock) | `IIncidentService.CreateAsync` llamado con datos correctos |
| `CheckAllBrandsAsync_ExistingOpenIncident_DoesNotDuplicate` | Incidente ya abierto para esa marca | No se crea incidente duplicado |
| `CheckAllBrandsAsync_BrandRecovers_ClosesIncident` | Site falla y luego se recupera | `IIncidentService.ResolveAsync` llamado |

---

### U4 — External Integrations (4 tests)

**Clases**: `SalesforceClientTests`, `MultivendeClientTests`
**Tipo**: Unit (HttpClient con mock)
**Total**: 4 tests

| Test | Clase | Escenario | Resultado esperado |
|---|---|---|---|
| `GetPendingOrders_Returns200_ParsesResponseCorrectly` | `SalesforceClientTests` | HTTP 200 con payload válido | DTO de pedidos correctamente deserializado |
| `GetPendingOrders_Returns401_ThrowsAuthException` | `SalesforceClientTests` | HTTP 401 (token expirado) | `ExternalAuthException` lanzada |
| `GetJobStatuses_RequestTimesOut_RetriesWithPolly` | `MultivendeClientTests` | Timeout en primera llamada, éxito en retry | Respuesta correcta después de 1 retry (política Polly) |
| `GetJobStatuses_Returns200_ParsesResponseCorrectly` | `MultivendeClientTests` | HTTP 200 con payload válido | DTO de job statuses correctamente deserializado |

**Configuración de mock de HttpClient**:
```csharp
// Usar MockHttpMessageHandler para interceptar llamadas HTTP
// NUNCA llamar a las APIs reales en tests unitarios
var mockHandler = new MockHttpMessageHandler();
mockHandler.When("https://salesforce-interno.eliot.local/*")
           .Respond(HttpStatusCode.OK, "application/json", salesforcePayload);
var httpClient = new HttpClient(mockHandler);
```

---

### U5 — Rules Management (6 tests)

**Clase**: `RuleConditionTests`
**Tipo**: Unit
**Total**: 6 tests

| Test | Escenario | Resultado esperado |
|---|---|---|
| `IsValidForModule_DashboardModule_ActiveRuleMatches` | Regla activa para módulo Dashboard | `true` |
| `IsValidForModule_HistoryModule_ActiveRuleMatches` | Regla activa para módulo Historial | `true` |
| `IsValidForModule_SimulationModule_ActiveRuleMatches` | Regla activa para módulo Simulación | `true` |
| `IsValidForModule_BrandMonitorModule_ActiveRuleMatches` | Regla activa para módulo BrandMonitor | `true` |
| `ActivateRule_DuplicateActiveRule_ThrowsBusinessRuleException` | Activar regla cuando ya existe una activa para misma marca/módulo | `BusinessRuleException` lanzada |
| `ForBrandMonitorFactory_CreatesRuleWithCorrectDefaults` | Factory method `Rule.ForBrandMonitor(brand)` | Regla con valores por defecto correctos (thresholds, estado) |

---

### U6 — Dashboard & Real-Time (7 tests)

**Clases**: `NotificationServiceTests` (3 tests), `BrandMonitorCheckerTests` (4 tests — mismo que TC-U3-02)
**Tipo**: Unit
**Total**: 7 tests

> **Nota**: Los 4 tests de `BrandMonitorCheckerTests` son idénticos a los de U3 (TC-U3-02 = TC-U6-02). Se ejecutan una sola vez desde la misma clase de test, pero validan tanto la lógica de detección (U3) como la notificación SignalR (U6).

**NotificationServiceTests**:

| Test | Escenario | Resultado esperado |
|---|---|---|
| `SendAlertAsync_IncidentCreated_BroadcastsToAllClients` | Nuevo incidente detectado | `IHubContext<AlertsHub>.Clients.All.SendAsync("ReceiveAlert", ...)` invocado |
| `SendAlertAsync_IncidentResolved_SendsResolutionNotification` | Incidente resuelto | Notificación de resolución enviada con datos correctos |
| `SendAlertAsync_HubThrowsException_LogsErrorAndDoesNotPropagate` | SignalR Hub lanza excepción | Error logueado, excepción NO propagada al caller |

**BrandMonitorCheckerTests** (ver U3 para detalle de los 4 tests):
- `CheckAllBrandsAsync_AllBrandsHealthy_NoIncidentCreated`
- `CheckAllBrandsAsync_OneBrandFails_CreatesIncident`
- `CheckAllBrandsAsync_ExistingOpenIncident_DoesNotDuplicate`
- `CheckAllBrandsAsync_BrandRecovers_ClosesIncident`

**Mock de IHubContext** (ADR-U6-01 — triple mock obligatorio):
```csharp
// Los tres niveles de mock son necesarios porque IHubContext<T> tiene propiedades anidadas
var mockClients = new Mock<IHubClients>();
var mockClientProxy = new Mock<IClientProxy>();
var mockHubContext = new Mock<IHubContext<AlertsHub>>();

mockHubContext.Setup(h => h.Clients).Returns(mockClients.Object);
mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
mockClientProxy.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), default))
               .Returns(Task.CompletedTask);
```

---

### U7 — Simulation (3 tests)

**Clase**: `OrdersSimulatorServiceTests`
**Tipo**: Unit
**Total**: 3 tests

> **Nota**: `RunOneTickAsync` es `internal`. Requiere `[assembly: InternalsVisibleTo("MonitorPedidos.UnitTests")]` en `MonitorPedidos.Web`.

| Test | Escenario | Resultado esperado |
|---|---|---|
| `RunOneTickAsync_SimulationDisabled_DoesNothing` | `SimulationOptions.Enabled = false` | No se genera ningún pedido simulado ni se modifica el estado |
| `RunOneTickAsync_SimulationEnabled_NormalMode_GeneratesOrders` | `Enabled = true`, `IsFailureMode = false` | Pedidos simulados generados con distribución normal de estados |
| `RunOneTickAsync_SimulationEnabled_FailureMode_GeneratesFailedOrders` | `Enabled = true`, `IsFailureMode = true` | Pedidos generados con alta proporción de estados de fallo |

---

## §3 Filtros de Ejecución por Unidad

Para ejecutar tests de una unidad específica usar el flag `--filter`:

```bash
# U1 — Foundation
dotnet test MonitorPedidos.UnitTests --filter "ClassName=SecurityIntegrationTests"

# U2 — Persistence
dotnet test MonitorPedidos.UnitTests --filter "ClassName~IncidentDomain|ClassName~IncidentService"

# U3 — Detection
dotnet test MonitorPedidos.UnitTests --filter "ClassName~MonitoringService|ClassName~BrandMonitorChecker"

# U4 — External Integrations
dotnet test MonitorPedidos.UnitTests --filter "ClassName~SalesforceClient|ClassName~MultivendeClient"

# U5 — Rules Management
dotnet test MonitorPedidos.UnitTests --filter "ClassName=RuleConditionTests"

# U6 — Dashboard & Real-Time
dotnet test MonitorPedidos.UnitTests --filter "ClassName~NotificationService|ClassName~BrandMonitorChecker"

# U7 — Simulation
dotnet test MonitorPedidos.UnitTests --filter "ClassName=OrdersSimulatorServiceTests"
```

**Filtrar por nombre de test específico**:
```bash
dotnet test MonitorPedidos.UnitTests --filter "Name=Login_ValidCredentials_SetsCookieAndRedirectsToDashboard"
```

**Filtrar por categoría** (si los tests tienen atributos `[Trait]`):
```bash
dotnet test MonitorPedidos.UnitTests --filter "Category=Integration"
dotnet test MonitorPedidos.UnitTests --filter "Category=Unit"
```

---

## §4 Coverage

**Recolectar cobertura de código**:
```bash
dotnet test MonitorPedidos.UnitTests --collect:"XPlat Code Coverage"
```

**Generar reporte HTML con ReportGenerator** (instalar globalmente una vez):
```bash
dotnet tool install --global dotnet-reportgenerator-globaltool

dotnet test MonitorPedidos.UnitTests --collect:"XPlat Code Coverage" --results-directory ./TestResults

reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./TestResults/CoverageReport" -reporttypes:Html
```

**Abrir reporte**:
```
TestResults\CoverageReport\index.html
```

**Objetivo de cobertura**: >= 80% en líneas de código de `MonitorPedidos.Web` (excluyendo migrations y scaffolded code).

---

## §5 Convenciones de Naming

Los tests en `MonitorPedidos.UnitTests` siguen la convención:

```
ClassName_Scenario_ExpectedResult
```

**Ejemplos correctos**:
- `Incident_TransitionToResolved_FromOpen_Succeeds`
- `CheckAllBrandsAsync_OneBrandFails_CreatesIncident`
- `IsValidForModule_DashboardModule_ActiveRuleMatches`
- `RunOneTickAsync_SimulationDisabled_DoesNothing`

**Estructura interna de cada test** (patrón AAA):
```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange
    // ... configurar mocks y estado inicial

    // Act
    // ... ejecutar el método bajo prueba

    // Assert
    // ... verificar resultado y llamadas a mocks
}
```

**Tests parametrizados** usan `[Theory]` + `[InlineData]`:
```csharp
[Theory]
[InlineData("Dashboard")]
[InlineData("Historial")]
[InlineData("Simulacion")]
[InlineData("BrandMonitor")]
public void IsValidForModule_ActiveRule_ReturnsTrue(string moduleName)
```

---

## §6 Estrategia de Mocks

**Moq para interfaces de dominio**:
```csharp
var mockIncidentService = new Mock<IIncidentService>();
mockIncidentService.Setup(s => s.CreateAsync(It.IsAny<CreateIncidentCommand>()))
                   .ReturnsAsync(new IncidentDto { Id = Guid.NewGuid() });
```

**MockHttpMessageHandler para HttpClient** (paquete `RichardSzalay.MockHttp` o similar):
```csharp
var mockHttp = new MockHttpMessageHandler();
mockHttp.When(HttpMethod.Get, "*/api/orders/pending")
        .Respond("application/json", JsonSerializer.Serialize(expectedResponse));
var client = mockHttp.ToHttpClient();
```

**Triple mock de IHubContext** (ADR-U6-01 — ver §2, U6 para código completo):
- Mock 1: `IHubContext<AlertsHub>`
- Mock 2: `IHubClients` (propiedad `Clients` del hub context)
- Mock 3: `IClientProxy` (propiedad `All` de hub clients)

**InternalsVisibleTo para U7**:

En `MonitorPedidos.Web/MonitorPedidos.Web.csproj` o en un archivo `AssemblyInfo.cs`:
```csharp
[assembly: InternalsVisibleTo("MonitorPedidos.UnitTests")]
```

Esto permite que `OrdersSimulatorServiceTests` acceda al método `internal RunOneTickAsync`.
