# Performance Test Instructions — MonitorPedidos

**Fecha**: 2026-05-24
**Proyecto**: MonitorPedidos — Manufacturas Eliot
**Solución**: `MonitorPedidos.sln`

> **Estado**: Act5 (Code Generation) pendiente de activación — este documento describe las pruebas de rendimiento planificadas para cuando el código esté generado. Las pruebas de performance formal son post-MVP; en MVP se verifican manualmente los targets críticos.

---

## §1 Scope y Targets de Performance

Los requerimientos no funcionales (NFR) de MonitorPedidos establecen los siguientes targets de rendimiento:

| Ref | Componente | Target | Condición de medición |
|---|---|---|---|
| RNF-01 | Página Historial de Incidentes | < 2 segundos | Con 1000+ incidentes en BD, paginado, usuario autenticado |
| RNF-02 | BrandMonitorChecker completo | < 30 segundos | 4 sites verificados en paralelo, dentro del ciclo de 10 minutos |
| RNF-03 | Dashboard — carga inicial | < 3 segundos | Incluye snapshot más reciente por marca + SignalR handshake |
| RNF-04 | API de historial paginado | < 1 segundo | Solo el endpoint de datos, sin renderizado Blazor |
| RNF-05 | Reglas — carga de lista | < 1 segundo | Con hasta 50 reglas activas |

**Contexto de uso**:
- Usuarios concurrentes esperados: 3-10 (aplicación de uso interno)
- Pico de uso: horario laboral Manufacturas Eliot (7am-6pm)
- Datos acumulados: ~5000 incidentes después de 1 año de uso

---

## §2 Herramientas

MonitorPedidos opera exclusivamente en red interna sin acceso a internet. Las herramientas de performance seleccionadas no requieren infraestructura cloud.

### Opción A: NBomber (recomendada para .NET)

**Instalación**:
```bash
dotnet add package NBomber
dotnet add package NBomber.Http
```

**Ventajas para MonitorPedidos**:
- Se integra directamente en el proyecto .NET sin herramientas externas
- Los escenarios se escriben en C# — misma convención que el resto del proyecto
- Genera reportes HTML sin necesidad de servicios externos

**Estructura de proyecto de performance** (a crear cuando Act5 esté activo):
```
MonitorPedidos.PerformanceTests/
├── Scenarios/
│   ├── HistorialLoadScenario.cs
│   ├── DashboardLoadScenario.cs
│   └── BrandCheckerTimingScenario.cs
└── MonitorPedidos.PerformanceTests.csproj
```

### Opción B: k6

**Instalación local** (no requiere internet durante ejecución):
```bash
# Windows — descargar binario desde distribución interna
k6 version
```

**Ventajas**: más expresivo para escenarios HTTP complejos, reportes detallados.

> Para MVP: usar **verificación manual con Stopwatch / browser DevTools** es suficiente para confirmar que los targets se cumplen. NBomber o k6 se activan en fase post-MVP si se detectan regresiones.

---

## §3 Test de Carga: Historial Paginado

**Target**: Página `/HistorialIncidentes` con filtros → respuesta < 2s con 1000 incidentes (RNF-01).

### Preparación de datos seed

```sql
-- Script para insertar 1000 incidentes de prueba en MonitorPedidosTestDb
-- Distribuidos entre las 4 marcas, fechas variadas, estados mixtos

DECLARE @i INT = 0;
DECLARE @brands TABLE (Name NVARCHAR(50));
INSERT INTO @brands VALUES ('Patprimo'), ('SevenSeven'), ('Atmos'), ('Ostu');

WHILE @i < 1000
BEGIN
    INSERT INTO incidents (Id, Brand, Status, CreatedAt, ResolvedAt, Description)
    SELECT
        NEWID(),
        (SELECT TOP 1 Name FROM @brands ORDER BY NEWID()),
        CASE WHEN @i % 3 = 0 THEN 'Open' WHEN @i % 3 = 1 THEN 'Resolved' ELSE 'Detected' END,
        DATEADD(MINUTE, -@i * 15, GETDATE()),
        CASE WHEN @i % 3 = 1 THEN DATEADD(MINUTE, -@i * 10, GETDATE()) ELSE NULL END,
        'Incidente de prueba ' + CAST(@i AS NVARCHAR(10));
    SET @i = @i + 1;
END;
```

### Escenario NBomber

```csharp
var scenario = Scenario.Create("historial_paginado", async context =>
{
    var response = await httpClient.GetAsync(
        "/HistorialIncidentes?page=1&pageSize=20&brand=Patprimo");

    return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
})
.WithWarmUpDuration(TimeSpan.FromSeconds(5))
.WithLoadSimulations(
    Simulation.KeepConstant(copies: 5, during: TimeSpan.FromMinutes(2))
);

NBomberRunner
    .RegisterScenarios(scenario)
    .Run();
```

**Criterio de aprobación**:
- P95 (percentil 95) < 2000ms
- P99 < 3000ms
- Error rate < 1%

---

## §4 Verificación de Índices

La performance de las queries depende de los índices definidos en las migrations. Verificar que los índices clave están en su lugar antes de los tests de carga.

**Índices esperados** (definidos en las migrations):

| Tabla | Columna(s) indexada(s) | Migration | Propósito |
|---|---|---|---|
| `incidents` | `(Brand, Status)` | `AddIncidentSchema` | Filtro por marca + estado en historial |
| `incidents` | `CreatedAt DESC` | `AddIncidentSchema` | Ordenamiento por fecha |
| `brand_snapshots` | `(Brand, CheckedAt DESC)` | `AddBrandSnapshots` | Snapshot más reciente por marca |
| `rules` | `(Module, IsActive)` | `AddRulesSchema` | Reglas activas por módulo |
| `simulated_orders` | `CreatedAt DESC` | `AddSimulationSchema` | Visualización de pedidos simulados |

**Verificar query plan en SQL Server LocalDB** (SQL Server Management Studio o Azure Data Studio):

```sql
-- Verificar que la query de historial usa índice (no table scan)
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

SELECT TOP 20 *
FROM incidents
WHERE Brand = 'Patprimo' AND Status = 'Open'
ORDER BY CreatedAt DESC;

-- Resultado esperado: Index Seek (no Index Scan, no Table Scan)
-- Logical reads esperados: < 10 para 1000 registros con índice correcto
```

**Ejecutar EXPLAIN equivalente desde EF Core** (logging de queries):
```csharp
// En appsettings.Testing.json, habilitar logging de EF Core
"Logging": {
  "LogLevel": {
    "Microsoft.EntityFrameworkCore.Database.Command": "Information"
  }
}
```

**Señal de alerta**: si el query plan muestra `Table Scan` o `Clustered Index Scan` en tablas grandes, revisar que el índice compuesto está correctamente definido en la migration.

---

## §5 Prueba Manual de SignalR

La verificación del comportamiento real de SignalR (alertas en tiempo real) se realiza manualmente, dado que su naturaleza push no se presta bien para tests automatizados en el scope de MVP.

**Procedimiento de verificación**:

1. **Preparar sesiones de navegador**:
   - Abrir 3 tabs en el mismo navegador (o 3 navegadores distintos)
   - Iniciar sesión en cada tab con credenciales diferentes (Admin, Supervisor, Operador)
   - Navegar al Dashboard en cada tab
   - Verificar en DevTools > Network > WS que la conexión SignalR está activa en `/alertsHub`

2. **Triggear una alerta**:
   - Acceder con el usuario Admin a `/Simulacion`
   - Activar modo de fallo (IsFailureMode = true)
   - Esperar el próximo ciclo del scheduler (o triggear manualmente si hay endpoint de debug)

3. **Verificar propagación**:
   - La alerta debe aparecer en las 3 tabs simultáneamente (dentro de 2-3 segundos)
   - Verificar que el semáforo de la marca afectada cambia a rojo en todas las sesiones
   - Verificar que el contador de incidentes se actualiza sin recargar la página

4. **Verificar reconexión**:
   - Desconectar la red por 10 segundos (deshabilitar WiFi temporalmente)
   - Reconectar
   - Verificar que SignalR se reconecta automáticamente (máximo 30 segundos)
   - Verificar que el estado del dashboard es correcto después de reconexión

**Resultado esperado**: Alertas propagadas a todas las sesiones en < 3 segundos desde que `BrandMonitorChecker` detecta el problema.

---

## §6 Performance Post-MVP

Para MVP (alcance actual al 2026-05-24), la estrategia de performance es:

**MVP — Verificación manual**:
- Usar browser DevTools (Network tab) para medir tiempos de carga
- Verificar manualmente que los targets RNF-01 a RNF-05 se cumplen
- No se requieren herramientas especializadas

**Post-MVP — Automatización** (activar cuando haya > 2000 incidentes en producción):
1. Crear proyecto `MonitorPedidos.PerformanceTests` con NBomber
2. Integrar ejecución de performance tests en pipeline CI/CD interno
3. Configurar alertas si P95 supera threshold (regresión de performance)
4. Revisar query plans trimestralmente conforme crece el volumen de datos

**Trigger para activar performance tests formales**:
- Volumen de incidentes > 5000 registros
- Tiempo de carga del historial percibido como lento por usuarios
- Cambio mayor en queries de EF Core (nueva feature, refactoring de repositorios)

---

## §7 Criterios de Aceptación

| # | Target | Componente afectado | Método de verificación | Resultado esperado | Estado |
|---|---|---|---|---|---|
| RNF-01 | Historial < 2s con 1000+ incidentes | `HistorialIncidentes` Razor page | NBomber P95 / DevTools | P95 < 2000ms | Pendiente (post-código) |
| RNF-02 | BrandMonitorChecker < 30s para 4 sites | `BrandMonitorChecker.CheckAllBrandsAsync` | Stopwatch en logs | Duración total < 30s | Pendiente (post-código) |
| RNF-03 | Dashboard carga inicial < 3s | Dashboard Blazor + SignalR handshake | DevTools Network (first load) | < 3000ms | Pendiente (post-código) |
| RNF-04 | API historial paginado < 1s | Endpoint de datos (sin Blazor render) | DevTools / NBomber | P95 < 1000ms | Pendiente (post-código) |
| RNF-05 | Lista de reglas < 1s | `ReglasMonitoreo` Razor page | DevTools | < 1000ms | Pendiente (post-código) |
| RNF-06 | SignalR alerta < 3s propagación | `AlertsHub` + `NotificationService` | Verificación manual (multi-tab) | Todas las tabs actualizadas < 3s | Pendiente (post-código) |
| RNF-07 | Reconexión SignalR < 30s | Cliente SignalR en Dashboard | Verificación manual | Reconexión automática < 30s | Pendiente (post-código) |

**Convención de estado**:
- `Pendiente (post-código)`: no ejecutable hasta que Act5 genere el código
- `Aprobado`: target verificado y cumplido
- `Fallido`: target no cumplido — requiere optimización antes de deploy
