# IT1 — Migración PostgreSQL → SQL Server · Functional Design

**Fecha:** 2026-05-30  
**Iteración:** IT1 — PostgreSQL → SQL Server + Dual DB  
**Stories relacionadas:** US-26 (cifrado at-rest + transporte), US-07 (detección de órdenes — fuente real)  
**Estado:** ✅ Completado

---

## 1. Objetivo

Migrar el proveedor de base de datos de PostgreSQL (prototipo) a SQL Server (infraestructura real de la empresa) e introducir una segunda conexión de solo lectura a la BD de producción para que M2 lea pedidos reales.

---

## 2. Arquitectura de dos bases de datos

| Conexión | Servidor | Base de datos | Provider | Uso |
|----------|----------|---------------|----------|-----|
| `DefaultConnection` | 172.16.0.41 | MonitorPedidosDb | EF Core (SQL Server) | `incidents`, `rules`, `rule_history`, `brand_snapshots`, `simulated_orders`, `simulated_job_statuses` |
| `ProductionDb` | 192.168.20.91 | vtainternet_qa | Dapper (solo SELECT) | `oc_encabezado` — fuente real de pedidos para M2 |

**Invariante de seguridad:** `ProductionDb` es **solo lectura** — nunca DELETE, UPDATE, ALTER, INSERT.

---

## 3. Cambios de proveedor

### Paquetes NuGet — `MonitorPedidos.Infrastructure.csproj`

| Acción | Paquete |
|--------|---------|
| Eliminado | `Npgsql.EntityFrameworkCore.PostgreSQL` |
| Agregado | `Microsoft.EntityFrameworkCore.SqlServer` 8.* |
| Agregado | `Dapper` 2.1.79 |

### Configuración EF Core

`AppDbContext.OnConfiguring` / `Program.cs`:
- `UseNpgsql(...)` → `UseSqlServer(...)`
- `TrustServerCertificate=True` + `Encrypt=True` en la connection string

---

## 4. Entidad IOrderSource — lectura de pedidos reales

### Interfaz

```csharp
public interface IOrderSource
{
    Task<IReadOnlyList<OrderSnapshot>> GetOrdersInWindowAsync(
        DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default);
}
```

### Implementaciones

| Implementación | Cuándo se usa |
|----------------|---------------|
| `SimulatedOrderRepository` | `ProductionDb` no configurada (dev/test) |
| `ProductionOrderRepository` | `ProductionDb` configurada (producción) |

`Program.cs` decide en arranque:
```csharp
var prodConn = builder.Configuration.GetConnectionString("ProductionDb");
if (!string.IsNullOrEmpty(prodConn))
    builder.Services.AddScoped<IOrderSource>(_ => new ProductionOrderRepository(prodConn));
else
    builder.Services.AddScoped<IOrderSource, SimulatedOrderRepository>();
```

### ProductionOrderRepository — query principal

```sql
SELECT ChannelName, FechaGeneracion
FROM oc_encabezado
WHERE FechaGeneracion >= @From AND FechaGeneracion <= @To
```

Canales esperados: `["SALESFORCE", "MULTIVENDE"]` (case-insensitive).

---

## 5. Migraciones EF Core — tablas creadas

Con el nuevo proveedor SQL Server se generó la migración `InitialCreate`:

| Tabla | Descripción |
|-------|-------------|
| `incidents` | Ciclo de vida completo de incidentes (U2) |
| `rules` + `rule_history` | Gestión de reglas con historial (U5) |
| `brand_snapshots` | Snapshots append-only Brand Monitor (IT3) |
| `simulated_orders` | Órdenes simuladas para dev/test (U7) |
| `simulated_job_statuses` | Estado de jobs simulados (U7) |

Comando aplicado:
```bash
dotnet ef migrations add InitialCreate --project src/MonitorPedidos.Infrastructure --startup-project src/MonitorPedidos.Web
dotnet ef database update --project src/MonitorPedidos.Infrastructure --startup-project src/MonitorPedidos.Web
```

---

## 6. Archivos modificados

| Archivo | Cambio |
|---------|--------|
| `src/MonitorPedidos.Infrastructure/MonitorPedidos.Infrastructure.csproj` | Swap Npgsql → SqlServer + Dapper |
| `src/MonitorPedidos.Infrastructure/Persistence/AppDbContext.cs` | `UseSqlServer(...)` |
| `src/MonitorPedidos.Infrastructure/Persistence/ProductionOrderRepository.cs` | Nuevo — Dapper solo lectura |
| `src/MonitorPedidos.Web/Program.cs` | Registro condicional `IOrderSource` |
| `src/MonitorPedidos.Web/appsettings.json` | Placeholders `DefaultConnection` + `ProductionDb` |
| `.env` (gitignored) | Credenciales reales |
