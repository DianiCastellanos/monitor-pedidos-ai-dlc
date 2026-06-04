# Deployment Architecture — U2 Persistence & Incidents

**Unidad:** U2 — Persistence & Incidents
**Stage:** Construction → Infrastructure Design
**Fecha:** 2026-05-23
**Versión:** 1.0

> **Nota:** U2 no modifica el proceso de despliegue de U1. Este diagrama extiende el de U1 mostrando los componentes nuevos que U2 agrega al mismo proceso Kestrel.

---

## §1 Diagrama de arquitectura de despliegue — U1 + U2

```mermaid
graph TB
    subgraph MAQUINA["Máquina Local / Red Interna LAN"]

        NAV["Navegador Web"]

        subgraph PROC["Proceso: dotnet run — Kestrel :7001 / :5001"]

            subgraph MW["Pipeline de Middleware — U1"]
                MW1["SecurityHeadersMiddleware\nCSP · X-Frame-Options"]
                MW2["CookieAuthenticationMiddleware\ncookie 8h sliding"]
                MW3["AuthorizationMiddleware\nFallbackPolicy — deny by default"]
                MW4["GlobalExceptionHandler\nfail-closed"]
            end

            subgraph APP["Capa de Presentación"]
                RZ["Razor Pages U1\nIdentitySelection · Error · AccessDenied"]
                subgraph INC["Blazor — U2 Incidents"]
                    HP["HistoricPage\n/incidents"]
                    WP["WeeklySummaryPage\n/incidents/weekly"]
                    IDP["IncidentDetailPage\n/incidents/{id:guid}"]
                end
                IS["IncidentService\nIIncidentService — scoped"]
            end

            subgraph INFRA["Capa de Infraestructura"]
                CTX["AppDbContext\nEF Core 8 — SQL Server provider"]
                IR["IncidentRepository\nIIncidentRepository — scoped"]
                SERI["Serilog\nFile Sink — rotación diaria 90d"]
                DPK["Data Protection\nPersistKeysToFileSystem — keys/"]
            end

            BG["IncidentMaintenanceService\nBackgroundService singleton\ncada 24h — purga expirados"]
        end

        subgraph FS["Sistema de Archivos"]
            KEYS[("keys/\nclaves criptográficas")]
            LOGS[("logs/\nmonitor-pedidos-YYYYMMDD.log")]
        end

        subgraph DB["SQL Server MonitorPedidosDb (172.16.0.41)"]
            SCHEMA1[("MonitorPedidosDb\ntablas U1: asp_net_*")]
            SCHEMA2[("MonitorPedidosDb\ntabla U2: incidents\nIX_incidents_Module_Open")]
        end
    end

    NAV -->|"HTTPS :7001 / HTTP :5001"| MW1
    MW1 --> MW2
    MW2 --> MW3
    MW3 --> MW4
    MW4 --> RZ
    MW4 --> INC
    INC <-->|"SignalR WebSocket"| NAV
    HP --> IS
    WP --> IS
    IDP --> IS
    IS --> IR
    IR --> CTX
    CTX -->|"Trusted_Connection"| SCHEMA1
    CTX -->|"Trusted_Connection"| SCHEMA2
    SERI -->|"escribe"| LOGS
    DPK <-->|"lee / escribe"| KEYS
    MW2 -.->|"firma y verifica cookies"| DPK
    BG -.->|"IServiceScopeFactory\ncrea scope por ejecución"| IR
```

---

## §2 Componentes nuevos que agrega U2

| Componente | Tipo | Ciclo de vida | Nota |
|-----------|------|--------------|------|
| `HistoricPage` | Blazor Page | Transient (por circuito) | Ruta `/incidents` |
| `WeeklySummaryPage` | Blazor Page | Transient (por circuito) | Ruta `/incidents/weekly` |
| `IncidentDetailPage` | Blazor Page | Transient (por circuito) | Ruta `/incidents/{id:guid}` |
| `IncidentService` | Application Service | Scoped | Implementa `IIncidentService` |
| `IncidentRepository` | EF Core Repository | Scoped | Implementa `IIncidentRepository` |
| `IncidentMaintenanceService` | BackgroundService | **Singleton** | Corre en hilo de fondo — cada 24h |
| Tabla `incidents` | SQL Schema | — | Creada por migration `AddIncidentSchema` |

---

## §3 Detalle del BackgroundService

`IncidentMaintenanceService` es singleton pero necesita `IIncidentRepository` (scoped). El patrón correcto es `IServiceScopeFactory`:

```
IncidentMaintenanceService (singleton)
    |
    +-- cada 24h -->
        IServiceScopeFactory.CreateAsyncScope()
            |
            +-- resuelve IIncidentRepository (scoped dentro del scope)
            +-- llama PurgeExpiredAsync(retentionDays, ct)
            +-- scope se destruye → DbContext se libera
```

Sin `IServiceScopeFactory`, el DI de ASP.NET Core lanzaría `InvalidOperationException: Cannot consume scoped service from singleton`.

---

## §4 Migration AddIncidentSchema

```bash
# Crear la migration (solo la primera vez, al desarrollar U2)
dotnet ef migrations add AddIncidentSchema --project src/MonitorPedidos.Web

# Aplicar antes del primer arranque con U2
dotnet ef database update --project src/MonitorPedidos.Web
```

Lo que crea esta migration en MonitorPedidosDb:

| Objeto | Tipo |
|--------|------|
| `incidents` | Tabla — 17 columnas |
| `IX_incidents_OpenedAt` | Índice simple |
| `IX_incidents_Module_ClosedAt` | Índice compuesto |
| `IX_incidents_Module_Open` | **Índice único filtrado** `WHERE ClosedAt IS NULL` |

---

## §5 Sin cambios vs U1

Los siguientes aspectos del despliegue **no cambian** con U2:

| Aspecto | Estado |
|---------|--------|
| Proceso de arranque (`dotnet run`) | Sin cambios |
| Puerto Kestrel (`:7001` / `:5001`) | Sin cambios |
| Certificado HTTPS (`dev-certs`) | Sin cambios |
| `keys/` Data Protection | Sin cambios |
| `logs/` Serilog | Sin cambios — U2 escribe en el mismo sink |
| Acceso desde red interna LAN | Sin cambios — ver §6 de U1 `deployment-architecture.md` |
