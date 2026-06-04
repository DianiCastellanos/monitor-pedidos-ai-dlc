# Build & Test Summary — MonitorPedidos

**Fecha**: 2026-05-24
**Proyecto**: MonitorPedidos — Manufacturas Eliot
**Solución**: `MonitorPedidos.sln`
**Fase**: Construction — Build & Test (documentación completa)

> **Estado**: Act5 (Code Generation) pendiente de activación. No existe código generado al 2026-05-24. Este documento resume el plan completo de build y test que se activará cuando Act5 sea aprobado por el owner. La documentación de diseño para las 7 unidades está completa.

---

## §1 Resumen Ejecutivo

| Aspecto | Detalle |
|---|---|
| Unidades de diseño | 7 unidades completas (U1–U7) |
| Tests planificados | 38 tests (unitarios + integración) |
| Migrations planificadas | 5 migrations en orden secuencial |
| Código generado | 0 — Act5 no activado |
| Stack tecnológico | ASP.NET Core 8 + Blazor Server + EF Core 8 + SignalR + Polly + xUnit + Moq |
| Despliegue objetivo | localhost / red interna Manufacturas Eliot |

Las 7 unidades de construcción tienen diseño completo aprobado (Functional Design, NFR Requirements, NFR Design, Infrastructure Design). El paso siguiente es la activación de Act5 (Code Generation), que está ON HOLD al 2026-05-24 pendiente de aprobación explícita del owner.

---

## §2 Documentos del Directorio Build & Test

| Archivo | Propósito | Contenido clave |
|---|---|---|
| `build-instructions.md` | Instrucciones completas de build y setup | Prerrequisitos, User Secrets (secretos de Salesforce + Multivende), migrations en orden, comandos `dotnet build` y `dotnet run`, HTTPS, URLs de verificación |
| `unit-test-instructions.md` | Instrucciones de tests unitarios y de integración de capa | 38 tests por clase + escenario, filtros `--filter`, coverage, convenciones de naming, patrones de mock (Moq, MockHttpMessageHandler, triple mock IHubContext) |
| `integration-test-instructions.md` | Instrucciones de tests de integración end-to-end | WebApplicationFactory, 4 flujos de integración (auth, incidente, historial, reglas), BD separada `MonitorPedidosTestDb`, limpieza entre tests |
| `performance-test-instructions.md` | Instrucciones de pruebas de performance | 7 targets NFR, herramientas NBomber/k6, seed de 1000 registros, verificación de índices, prueba manual SignalR multi-tab, criterios de aceptación |

---

## §3 Secuencia de Setup

El orden correcto para dejar el sistema funcionando desde cero:

```
1. Prerrequisitos
   └── .NET 8 SDK + SQL Server MonitorPedidosDb (172.16.0.41) + EF CLI instalados

2. Clonar repositorio
   └── git clone + dotnet restore MonitorPedidos.sln

3. Configurar User Secrets
   └── dotnet user-secrets set (Salesforce, Multivende, BD, admin seed)
       Ver build-instructions.md §3 para lista completa de secretos

4. Aplicar migrations (en orden)
   └── InitialCreate → AddIncidentSchema → AddRulesSchema
       → AddBrandSnapshots → AddSimulationSchema
   └── dotnet ef database update --project MonitorPedidos.Web

5. Confiar en certificado HTTPS (una vez por máquina)
   └── dotnet dev-certs https --trust

6. Build
   └── dotnet build MonitorPedidos.sln

7. Run
   └── dotnet run --project MonitorPedidos.Web
   └── Acceder a https://localhost:7xxx

8. Ejecutar tests
   └── dotnet test MonitorPedidos.UnitTests
   └── Resultado esperado: 38 passed, 0 failed
```

> NO saltear el paso 3 (User Secrets). La aplicación NO arranca sin las credenciales de Salesforce y Multivende configuradas.

---

## §4 Tests por Unidad

| Unidad | Test Class(es) | # Tests | Tipo | Dependencias de mock |
|---|---|---|---|---|
| U1 Foundation | `SecurityIntegrationTests` | 5 | Integration (WebApplicationFactory) | BD real (MonitorPedidosTestDb) |
| U2 Persistence | `IncidentDomainTests`, `IncidentServiceIntegrationTests` | 6 | Unit + Integration | BD real para service tests |
| U3 Detection | `MonitoringServiceTests` (3), `BrandMonitorCheckerTests` (4) | 7 | Unit | Moq: `IIncidentService`, `ISalesforceClient`, `IMultivendeClient` |
| U4 External Integrations | `SalesforceClientTests`, `MultivendeClientTests` | 4 | Unit | `MockHttpMessageHandler` (sin llamadas HTTP reales) |
| U5 Rules Management | `RuleConditionTests` | 6 | Unit | Moq: `IRuleRepository` |
| U6 Dashboard & Real-Time | `NotificationServiceTests` (3), `BrandMonitorCheckerTests` (4) | 7 | Unit | Triple mock `IHubContext<AlertsHub>` (ADR-U6-01) |
| U7 Simulation | `OrdersSimulatorServiceTests` | 3 | Unit | `InternalsVisibleTo` para `RunOneTickAsync` |
| **TOTAL** | **9 clases de test** | **38** | — | — |

**Nota sobre U3/U6**: Los 4 tests de `BrandMonitorCheckerTests` son compartidos. La misma clase de test valida tanto la detección (U3) como las notificaciones SignalR (U6). No se duplican.

---

## §5 Migrations en Orden

| # | Nombre de Migration | Unidad | Tablas creadas | Datos seed |
|---|---|---|---|---|
| 1 | `InitialCreate` | U1 Foundation | `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens`, `AspNetRoleClaims` | — |
| 2 | `AddIncidentSchema` | U2 Persistence | `incidents` | — |
| 3 | `AddRulesSchema` | U5 Rules Management | `rules`, `rule_conditions` | — |
| 4 | `AddBrandSnapshots` | U6 Dashboard & Real-Time | `brand_snapshots` | 4 reglas iniciales: Patprimo, SevenSeven, Atmos, Ostu |
| 5 | `AddSimulationSchema` | U7 Simulation | `simulated_orders`, `simulated_job_statuses` | — |

**Orden obligatorio**: las migrations deben aplicarse secuencialmente. `AddBrandSnapshots` (U6) depende de `rules` creada por `AddRulesSchema` (U5), y `AddRulesSchema` no depende de `AddIncidentSchema` pero ambas deben preceder a U6.

**Verificar estado**:
```bash
dotnet ef migrations list --project MonitorPedidos.Web
# Resultado esperado: todas las 5 migrations marcadas con [applied]
```

---

## §6 Decisiones de Testing Relevantes

Las siguientes decisiones de arquitectura de tests fueron tomadas durante el diseño y deben respetarse durante Code Generation (Act5):

### U1: WebApplicationFactory para Tests de Seguridad

Los tests de seguridad de U1 usan `WebApplicationFactory<Program>` porque verifican comportamiento real del pipeline ASP.NET Core (cookies de Identity, headers de seguridad, autorización por roles). No pueden ser tests unitarios pures — requieren el middleware completo.

**Implicación**: `MonitorPedidos.UnitTests` debe referenciar `Microsoft.AspNetCore.Mvc.Testing`. La clase `SecurityIntegrationTests` requiere que `Program.cs` sea `public partial` para acceso desde el assembly de test.

### U3/U6: BrandMonitorCheckerTests Compartidos (TC-U3-02 = TC-U6-02)

Los 4 tests de `BrandMonitorCheckerTests` aparecen tanto en el diseño de U3 como en el de U6 porque la clase `BrandMonitorChecker` pertenece a la intersección: detecta problemas (U3) Y dispara notificaciones SignalR (U6). Una sola clase de test cubre ambas responsabilidades.

**Implicación**: No crear dos clases de test separadas. Los tests de `BrandMonitorChecker` mockean tanto `IIncidentService` (para verificar creación de incidentes) como `INotificationService` o directamente `IHubContext<AlertsHub>` (para verificar que la alerta se envía).

### U4: MockHttpMessageHandler sin Llamadas HTTP Reales

Ningún test unitario de U4 llama a las APIs de Salesforce o Multivende. Los `HttpClient` se construyen con un `MockHttpMessageHandler` que intercepta las llamadas a nivel de `HttpMessageHandler`.

**Implicación**: Los clientes `SalesforceClient` y `MultivendeClient` deben recibir su `HttpClient` por inyección de dependencia (via `IHttpClientFactory`) para que sea reemplazable en tests.

### U6: Triple Mock IHubContext (ADR-U6-01)

`IHubContext<AlertsHub>` expone propiedades anidadas (`Clients.All.SendAsync`) que requieren tres niveles de mock. Mockear solo el `IHubContext` sin mockear `IHubClients` y `IClientProxy` resulta en `NullReferenceException` en runtime de test.

**Implicación**: Crear un helper `HubContextMockFactory` en el assembly de tests para evitar duplicación del setup de triple mock en `NotificationServiceTests` y `BrandMonitorCheckerTests`.

### U7: InternalsVisibleTo para RunOneTickAsync

El método `RunOneTickAsync` del `OrdersSimulatorService` se declara `internal` (no `public`) porque no debe ser parte del API público del servicio. Para que `OrdersSimulatorServiceTests` pueda invocarlo directamente, se usa el atributo `[assembly: InternalsVisibleTo("MonitorPedidos.UnitTests")]`.

**Implicación**: Agregar el atributo en `MonitorPedidos.Web` (en `AssemblyInfo.cs` o en el `.csproj`). Sin esto, los 3 tests de U7 no compilarán.

---

## §7 Activación de Build & Test

> **Estado al 2026-05-24**: ON HOLD

La ejecución real de los tests del §4 requiere que Act5 (Code Generation) haya generado el código de la aplicación. Los documentos de instrucciones en este directorio están listos, pero no son ejecutables hasta que el código exista.

**Para activar Build & Test**:
1. Owner aprueba explícitamente la activación de Act5 (Code Generation)
2. Act5 genera el código de las 7 unidades en secuencia (U1 → U7)
3. Build & Test se ejecuta después de que todas las unidades completan Code Generation
4. El owner valida los resultados (`38 passed, 0 failed`) antes de considerar Construction completa

**Criterio de "Build & Test completo"**:
- `dotnet build MonitorPedidos.sln` → 0 errores, 0 warnings
- `dotnet test MonitorPedidos.UnitTests` → 38 passed, 0 failed, 0 skipped
- Aplicación arranca correctamente en `https://localhost:7xxx`
- Todas las 5 migrations aplicadas exitosamente
- Performance targets RNF-01 a RNF-07 verificados manualmente

---

## §8 Code Generation (Act5) — Fuera de Scope

> Act5 (Code Generation) está fuera del scope al 2026-05-24.

Los artefactos de diseño completados y disponibles en `aidlc-docs/construction/` para cada unidad son:

| Unidad | Functional Design | NFR Requirements | NFR Design | Infrastructure Design |
|---|---|---|---|---|
| U1 Foundation | Completo | Completo | Completo | Completo |
| U2 Persistence | Completo | Completo | Completo | Completo |
| U3 Detection | Completo | Completo | Completo | Completo |
| U4 External Integrations | Completo | Completo | Completo | Completo |
| U5 Rules Management | Completo | Completo | Completo | Completo |
| U6 Dashboard & Real-Time | Completo | Completo | Completo | Completo |
| U7 Simulation | Completo | Completo | Completo | Completo |

Cuando el owner apruebe la activación de Act5, el modelo ejecutará Code Generation unit por unit (U1 primero, U7 último), creando archivos de código en el workspace root según las instrucciones definidas en cada plan de construction.
