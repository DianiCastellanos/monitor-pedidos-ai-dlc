# Business Logic Model — U7 Simulation & Red-Teaming

**Unidad:** U7 — Simulation & Red-Teaming
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-24
**Versión:** 1.0

---

## Flujo 1 — `OrdersSimulatorService.ExecuteAsync` (ciclo normal)

```
OrdersSimulatorService (BackgroundService) arranca con la aplicación
    |
    IOptions<SimulationOptions> → Enabled=true, InsertIntervalMinutes=5, FailureProbability=0.0
    |
    loop (PeriodicTimer de InsertIntervalMinutes):
        |
        if SimulationOptions.Enabled == false → skip tick
        |
        if SimulationOptions.NoOrdersMode == true → skip tick (RT1: sin pedidos)
        |
        foreach source in ["Salesforce", "Multivende"]:
            foreach site in ["Patprimo", "SevenSeven", "Atmos", "Ostu"]:
                |
                if Random.Shared.NextDouble() < FailureProbability:
                    order = SimulatedOrder.CreateFailure(source, site, "Error")   ← RT5
                else:
                    order = SimulatedOrder.CreateNormal(source, site)
                |
                ISimulatedOrderRepository.InsertAsync(order, ct)
                    → INSERT INTO simulated_orders ...
        |
        (próximo tick en InsertIntervalMinutes)
```

**Resultado en baseline:** Cada 5 minutos se insertan 8 pedidos normales (2 sources × 4 sites). `DbOrderChecker` los encontrará en su ventana de tiempo → `CheckResult.Ok`.

---

## Flujo 2 — `DbOrderChecker` leyendo `simulated_orders` (MVP)

```
MonitoringSchedulerService Timer1 (5 min)
    |
    DbOrderChecker.ExecuteAsync(ct)
        |
        IServiceScopeFactory.CreateAsyncScope()
            |
            ISimulatedOrderRepository.CountRecentAsync("Salesforce", windowHours=2, ct)
                → SELECT COUNT(*) FROM simulated_orders
                  WHERE source='Salesforce' AND created_at > NOW()-2h
                → count = 8 (baseline normal)
            |
            if count < MinOrders (regla U5: MinOrders=1):
                return CheckResult.Critical("Sin pedidos en ventana de 2h")
            else:
                return CheckResult.Ok()
```

**En RT1 (NoOrdersMode=true):** simulador no inserta nada → `COUNT=0` → `CheckResult.Critical` → incidente abierto → alerta en dashboard.

---

## Flujo 3 — `JobsChecker` leyendo `simulated_job_statuses` (MVP)

```
MonitoringSchedulerService Timer1 (5 min)
    |
    JobsChecker.ExecuteAsync(ct)
        |
        ISimulatedJobStatusRepository.GetAllAsync(ct)
            → [SalesforceDownload: Completed, MultivendeDownload: Completed]
        |
        foreach job in statuses:
            if job.Status == "Failed":
                return CheckResult.Critical($"Job {job.JobName} falló: {job.ErrorMessage}")
            if job.LastRunAt < DateTime.UtcNow - MaxJobAge:
                return CheckResult.Warning($"Job {job.JobName} no ejecuta desde {job.LastRunAt}")
        |
        return CheckResult.Ok()
```

**En RT1 manual (job SF apagado):** ejecutar script SQL: `UPDATE simulated_job_statuses SET status='Failed', error_message='Job detenido' WHERE job_name='SalesforceDownload'` → próximo tick del checker → `CheckResult.Critical` con causa `job`.

---

## Flujo 4 — Escenario RT2 (token revocado — sin simulador, via API real)

```
[Técnico revoca el token de Salesforce en User Secrets / variables de entorno]
    |
    MonitoringSchedulerService Timer2 (10 min)
        SalesforceApiChecker.ExecuteAsync(ct)
            SalesforceClient.PingOrdersAsync(ct)
                GET /orders?$top=1 → HTTP 401 Unauthorized
                |
                [Polly intercepta — ADR-U4-01: 401 NO reintenta]
                    → ApiPingResult.Unauthorized
                |
            CheckResult.Critical("Salesforce HTTP 401 — token inválido o revocado")
            |
        MonitoringService.RunCheckAsync
            CauseClassifier → cause = Token (detecta patrón 401)
            AlertTemplateRenderer → template "Token": "Verificar SOP-001: renovar credenciales"
            IIncidentService.OpenIncidentAsync(...)
                → incidents table: accion_sugerida = "Renovar token — ver SOP-001"
            INotificationService.BroadcastAlertAsync → dashboard CRITICAL
```

**Verificación RT2:** `IncidentDetailPage` muestra `causa_probable = Token` + `accion_sugerida` con referencia SOP-001. Sin reintentos en `retry_metadata`.

---

## Flujo 5 — Escenario RT3 (SQL Server apagado)

```
[Técnico detiene el servicio SQL Server MonitorPedidosDb (172.16.0.41) en Windows]
    |
    MonitoringSchedulerService Timer1 tick
        DbHealthChecker.ExecuteAsync(ct)
            IDbConnectionFactory.OpenConnectionAsync(ct)
                → SqlException / timeout inmediato
            |
            CheckResult.Critical("Latencia DB: excede umbral crítico" o "DB inaccesible")
            |
        MonitoringService.RunCheckAsync → incidente CRITICAL inmediato
```

---

## Flujo 6 — Escenario RT5 (estado incorrecto → Discrepancies)

```
[Técnico configura FailureProbability=1.0 en appsettings.Development.json]
    |
    OrdersSimulatorService inserta pedidos con Status="Error", IsFailure=true
    |
    DbOrderChecker.ExecuteAsync(ct)
        COUNT(*) WHERE source='Salesforce' AND created_at > NOW()-2h → N > 0
        → CheckResult.Ok() — hay pedidos, aunque estén en error
    |
    [En MVP: la detección de estado incorrecto es responsabilidad de un checker separado
     o se filtra manualmente en DiscrepanciesPage — sin alerta WARN/CRITICAL (BR-DISC-01)]
    |
    DiscrepanciesPage → IIncidentService.GetDiscrepancyIncidentsAsync()
        → lista pedidos con IsFailure=true — silencioso, sin notificación push
```

---

## Flujo 7 — Escenario RT-Persist (cerrar y reabrir dashboard)

```
[Estado actual: incidente CRITICAL abierto en RealtimePage]
    |
    Técnico cierra el navegador (o la pestaña)
    |
    [Espera 30 segundos]
    |
    Técnico reabre el navegador → navega a /dashboard
        RealtimePage.OnInitializedAsync()
            IIncidentService.GetOpenIncidentsAsync()
                → incidente sigue en tabla incidents (Status=Open)
            AlertBroadcaster suscripción re-establecida
        |
        RealtimePage muestra el estado CRITICAL con el incidente anterior
    |
    ✅ RT-Persist: la persistencia en SQL Server garantiza que el estado no se pierde
```

---

## Flujo 8 — Limpieza periódica de `simulated_orders`

```
OrdersSimulatorService (cada 24h, separado del loop de inserción):
    ISimulatedOrderRepository.DeleteOlderThanAsync(DateTime.UtcNow - 48h, ct)
        → DELETE FROM simulated_orders WHERE created_at < NOW()-48h
```

**Propósito:** Evitar crecimiento ilimitado de la tabla en sesiones de demo prolongadas.
