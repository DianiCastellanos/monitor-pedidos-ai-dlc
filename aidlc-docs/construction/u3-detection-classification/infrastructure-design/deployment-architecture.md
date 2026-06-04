# Deployment Architecture — U3 Detection & Classification

**Unidad:** U3 — Detection & Classification
**Stage:** Construction → Infrastructure Design
**Fecha:** 2026-05-23
**Versión:** 1.0

> **Nota:** U3 no modifica el proceso de despliegue ni los puertos. Este diagrama extiende el de U2 agregando los componentes de detección y clasificación al mismo proceso Kestrel.

---

## §1 Diagrama de arquitectura de despliegue — U1 + U2 + U3

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

            subgraph APP["Capa de Presentación — U2"]
                RZ["Razor Pages U1\nIdentitySelection · Error"]
                subgraph INC["Blazor — Incidents U2"]
                    HP["HistoricPage /incidents"]
                    WP["WeeklySummaryPage /incidents/weekly"]
                    IDP["IncidentDetailPage /incidents/{id}"]
                end
                IS["IncidentService\nscoped"]
            end

            subgraph DETECT["Capa de Detección — U3"]
                MS["MonitoringService\nscoped"]
                subgraph CHK["Checkers — scoped"]
                    M2["DbOrderChecker\nM2"]
                    M4["DbHealthChecker\nM4"]
                    M11["JobsChecker\nM11"]
                end
                CLS["CauseClassifier\nestático puro"]
                TPL["AlertTemplateRenderer\nestático puro"]
            end

            subgraph INFRA["Capa de Infraestructura"]
                CTX["AppDbContext\nEF Core 8"]
                IR["IncidentRepository\nscoped"]
                SERI["Serilog\nFile Sink — 90d"]
                DPK["Data Protection\nkeys/"]
            end

            subgraph BG["Servicios de Fondo"]
                SCH["MonitoringSchedulerService\nBackgroundService singleton\nPeriodicTimer — cada 5 min (Dev: 1 min)"]
                MAINT["IncidentMaintenanceService\nBackgroundService singleton\ncada 24h — purga expirados"]
            end
        end

        subgraph FS["Sistema de Archivos"]
            KEYS[("keys/\nclaves criptográficas")]
            LOGS[("logs/\nmonitor-pedidos-YYYYMMDD.log")]
        end

        subgraph DB["SQL Server MonitorPedidosDb (172.16.0.41) — MonitorPedidosDb"]
            T_INC[("tabla: incidents\nIX_incidents_Module_Open")]
            T_SIM[("tablas: simulated_orders\nsimulated_job_statuses\ncreadas por U7")]
        end
    end

    NAV -->|"HTTPS :7001 / HTTP :5001"| MW1
    MW1 --> MW2 --> MW3 --> MW4
    MW4 --> RZ
    MW4 --> INC
    INC <-->|"SignalR WebSocket"| NAV
    INC --> IS --> IR --> CTX
    CTX --> T_INC
    SCH -->|"PeriodicTimer tick\nTask.WhenAll"| MS
    MS --> CHK
    M2 -->|"IOrderSource\nU7 provee impl."| CTX
    M4 -->|"SELECT 1"| CTX
    M11 -->|"IJobStatusSource\nU7 provee impl."| CTX
    CTX --> T_SIM
    MS --> CLS
    MS --> TPL
    MS --> IS
    MAINT -->|"IServiceScopeFactory"| IR
    SERI --> LOGS
    DPK <--> KEYS
    MW2 -.->|"firma cookies"| DPK
```

---

## §2 Componentes nuevos que agrega U3

| Componente | Tipo | Ciclo de vida | Nota |
|-----------|------|--------------|------|
| `MonitoringSchedulerService` | BackgroundService | **Singleton** | `PeriodicTimer` — 5 min prod / 1 min dev |
| `MonitoringService` | Application Service | Scoped | Orquesta checker → classify → render → incident → notify |
| `DbOrderChecker` | ICheckExecutor | Scoped | Usa `IOrderSource` (implementado por U7) |
| `DbHealthChecker` | ICheckExecutor | Scoped | `SELECT 1` directo a `AppDbContext` |
| `JobsChecker` | ICheckExecutor | Scoped | Usa `IJobStatusSource` (implementado por U7) |
| `CauseClassifier` | Clase estática | Sin DI | Determinista — lookup por tipo de checker |
| `AlertTemplateRenderer` | Clase estática | Sin DI | Diccionario C# de plantillas en español |

---

## §3 Flujo de detección en el proceso (cada tick)

```
MonitoringSchedulerService (singleton — fondo)
    |
    PeriodicTimer dispara cada 5 min
    |
    IServiceScopeFactory.CreateAsyncScope()
    |
    Task.WhenAll([DbOrderChecker, DbHealthChecker, JobsChecker])
         cada uno con Linked CancellationToken + timeout 30s
    |
    MonitoringService.RunCheckAsync(checker, ct)
         [WARN/CRITICAL] → CauseClassifier → AlertTemplateRenderer
                         → IncidentService.OpenIncidentAsync
                         → INotificationService.BroadcastAlertAsync (stub → real en U6)
         [OK]            → IncidentService.TryCloseOnConsecutiveOkAsync
```

---

## §4 Sin cambios vs U2

| Aspecto | Estado |
|---------|--------|
| Proceso de arranque (`dotnet run`) | Sin cambios |
| Puertos Kestrel (`:7001` / `:5001`) | Sin cambios |
| `AppDbContext` | Sin cambios en U3 — U7 agrega DbSets |
| `keys/` y `logs/` | Sin cambios |
| Migration | Sin migration en U3 — U7 crea `AddSimulatedTables` |
| Acceso LAN | Sin cambios |
