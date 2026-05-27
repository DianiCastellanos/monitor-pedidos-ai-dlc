# Deployment Architecture — U4 External Integrations

**Unidad:** U4 — External Integrations
**Stage:** Construction → Infrastructure Design
**Fecha:** 2026-05-23
**Versión:** 1.0
**Acumulado desde:** U1

---

## §1 Diagrama de despliegue (U1 + U2 + U3 + U4)

```mermaid
graph TD
    subgraph DEV["Máquina del desarrollador / Red interna"]

        subgraph APP["MonitorPedidos.Web — ASP.NET Core 8 + Blazor Server"]

            subgraph UI["UI Layer"]
                LoginPage["LoginPage (U1)"]
                DashboardPage["DashboardPage (U6)"]
                IncidentList["IncidentListPage (U2)"]
                IncidentDetail["IncidentDetailPage (U2+U4)\nauto_reintentos solo Tecnico"]
                RulesPage["RulesPage (U5)"]
            end

            subgraph AUTH["Auth — U1"]
                CookieAuth["CookieAuthentication\n8h sliding window"]
                Roles["Roles: Operador / Tecnico"]
            end

            subgraph CROSS["Cross-Cutting — U1"]
                GEH["GlobalExceptionHandler\nIExceptionHandler"]
                SERI["Serilog\nTexto plano + PII filter"]
                DP["DataProtection\nkeys/ en filesystem"]
            end

            subgraph PERSIST["Persistence — U2"]
                IS["IIncidentService"]
                IR["IIncidentRepository\nEF Core 8"]
                IMS["IncidentMaintenanceService\nBackgroundService 24h"]
            end

            subgraph DETECT["Detection — U3"]
                MS["MonitoringService"]
                DOC["DbOrderChecker"]
                DHC["DbHealthChecker"]
                JC["JobsChecker"]
                CC["CauseClassifier"]
                ATR["AlertTemplateRenderer\n+ Api template\n+ Token template SOP-001"]
            end

            subgraph APIINT["API Integration — U4"]
                SAC["SalesforceApiChecker\nICheckExecutor"]
                MAC["MultivendeApiChecker\nICheckExecutor"]
                SC["SalesforceClient\nTyped HttpClient 10s timeout"]
                MC["MultivendeClient\nTyped HttpClient 10s timeout"]
                ARP["ApiRetryPolicy\nPolly 2 reintentos\nbackoff 2s y 4s\n401 sin reintento"]
                PCX["PollyContextExtensions\nGetRetryAttempts"]
            end

            subgraph BG["Background Services"]
                MSS["MonitoringSchedulerService\nTimer1: 5min — DB/Jobs\nTimer2: 10min — APIs"]
            end

        end

        subgraph DB["SQL Server LocalDB"]
            INC["incidents\n+ retry_metadata nullable\ncolumna JSON"]
            USR["users"]
            JOB["jobs"]
            SIM["simulated_orders\nsimulated_job_statuses\ncreadas por U7"]
        end

    end

    subgraph EXT["APIs Externas — acceso desde red interna"]
        SFAPI["Salesforce Orders API\nGET /orders?top=1"]
        MVAPI["Multivende Orders API\nGET /orders ping"]
    end

    MSS -->|"Timer1 5min"| DOC
    MSS -->|"Timer1 5min"| DHC
    MSS -->|"Timer1 5min"| JC
    MSS -->|"Timer2 10min"| SAC
    MSS -->|"Timer2 10min"| MAC

    DOC --> MS
    DHC --> MS
    JC --> MS
    SAC --> SC
    MAC --> MC
    SC --> ARP
    MC --> ARP
    ARP -->|"GET ping + reintentos"| SFAPI
    ARP -->|"GET ping + reintentos"| MVAPI
    SAC --> MS
    MAC --> MS

    MS --> CC
    MS --> ATR
    MS --> IS
    MS --> PCX

    IS --> IR
    IR --> INC
    IMS --> INC
    IncidentDetail --> IS
    IncidentList --> IS
```

---

## §2 Flujo de datos — ciclo de API checker con reintentos

```
MonitoringSchedulerService
    Timer2 (10 min) tick
        |
        Task.WhenAll([SalesforceApiChecker, MultivendeApiChecker])
            |
            SalesforceApiChecker.ExecuteAsync(ct)
                |
                SalesforceClient.PingOrdersAsync(ct)
                    |
                    [Polly intercepta]
                    Intento 1: GET /orders?$top=1 → 503
                    onRetry → RetryAttempt(1, 503, latencia) → context["retryAttempts"]
                    espera 2s
                    Intento 2: GET /orders?$top=1 → 503
                    onRetry → RetryAttempt(2, 503, latencia) → context["retryAttempts"]
                    espera 4s
                    Intento 3: GET /orders?$top=1 → falla
                    → ApiPingResult.ServerError
                |
                CheckResult.Critical("Salesforce HTTP 5xx")
                |
            MonitoringService.RunCheckAsync
                cause = Api (via CauseClassifier)
                alert = AlertTemplateRenderer → "2 reintentos automaticos realizados"
                incident = IIncidentService.OpenIncidentAsync(...)
                retryAttempts = context.GetRetryAttempts()   [2 items]
                incident.AddRetryAttempt(attempt1)
                incident.AddRetryAttempt(attempt2)
                → incidents table: retry_metadata = "[{...},{...}]"
                INotificationService.BroadcastAlertAsync → SignalR (U6)
```

---

## §3 Artefactos de infraestructura por unidad (acumulado)

| Unidad | Migraciones EF | Configuración | Proyectos | BackgroundServices |
|--------|---------------|--------------|----------|-------------------|
| U1 | InitialCreate (users) | HTTPS dev-certs, cookies, Data Protection | MonitorPedidos.Web | — |
| U2 | InitialCreate (incidents, jobs) | RetentionDays: 90 | — | IncidentMaintenanceService |
| U3 | — | CheckerIntervalMinutes: 5 (prod) / 1 (dev) | MonitorPedidos.UnitTests | MonitoringSchedulerService (Timer1) |
| U4 | AddRetryMetadataToIncidents | ApiCheckerIntervalMinutes: 10 (prod) / 1 (dev) | — (UnitTests ya existe) | MonitoringSchedulerService (Timer2 agregado) |
| U5 | (pendiente) | — | — | — |
| U6 | (pendiente) | — | — | — |
| U7 | simulated_orders, simulated_job_statuses | — | — | — |

---

## §4 Trazabilidad

| Componente de infraestructura | Story | NFR | ADR |
|------------------------------|-------|-----|-----|
| Migración AddRetryMetadataToIncidents | US-10, US-16 | — | ADR-U4-01 |
| ApiCheckerIntervalMinutes en appsettings | US-10 | NFR-U4-01 | BR-SCHED-05 |
| AddSingleton ICheckExecutor (Salesforce, Multivende) | US-10 | — | ADR-U3-02, ADR-U4-03 |
| AddHttpClient + AddPolicyHandler | US-10 | NFR-U4-01 | ADR-U4-02 |
| User Secrets (Salesforce:ApiKey, Multivende:ApiKey) | — | SECURITY-03 | BR-CRED-01 |
| RichardSzalay.MockHttp en UnitTests | US-10, US-16 | NFR-U4-02, NFR-U4-03 | ADR-U4-04 |
