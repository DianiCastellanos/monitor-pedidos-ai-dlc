# Business Logic Model — U2 Persistence & Incidents

**Unidad:** U2 — Persistence & Incidents
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Flujos de negocio

### Flujo 1 — Apertura de incidente

Invocado por `MonitoringService` (U3) cuando un checker reporta WARN o CRITICAL.

```
MonitoringService.RunCheckAsync(moduleId, checkResult)
    |
    v
checkResult.Status == WARN o CRITICAL ?
    |
    +-> [No — OK] --> TryCloseOnConsecutiveOkAsync(moduleId)  (ver Flujo 2)
    |
    +-> [Sí] -->
        IIncidentRepository.GetOpenByModuleAsync(moduleId)
            |
            +-> [Ya existe incidente abierto] --> retorna incidente existente (idempotente)
            |
            +-> [No existe] -->
                Incident.Open(module, cause, severity, alert)
                    |
                    v
                IIncidentRepository.AddAsync(incident)
                IIncidentRepository.SaveChangesAsync()
                    |
                    v
                INotificationService.BroadcastAlertAsync(alert)   [interfaz de U3]
                    |
                    v
                retorna Incident con Id generado
```

**Invariante:** Solo puede existir **1 incidente abierto por ModuleId** en cualquier momento.

---

### Flujo 2 — Cierre automático (2 OK consecutivos)

```
IIncidentService.TryCloseOnConsecutiveOkAsync(moduleId)
    |
    v
IIncidentRepository.GetOpenByModuleAsync(moduleId)
    |
    +-> [No hay incidente abierto] --> retorna false (nada que cerrar)
    |
    +-> [Hay incidente abierto] -->
        IIncidentRepository.GetRecentClosedByModuleAsync(moduleId, count: 2)
            |
            v
        ¿Los últimos 2 checks del módulo fueron OK?
            |
            +-> [No] --> retorna false
            |
            +-> [Sí — 2 OK consecutivos de cualquier checker del módulo] -->
                incident.CloseAutomatically()
                IIncidentRepository.SaveChangesAsync()
                    |
                    v
                INotificationService.BroadcastCloseAsync(incident.Id)  [U3]
                    |
                    v
                retorna true
```

**Decisión P3 = A:** cualquier checker del `ModuleId` (M2, M4, M11) cuenta para los 2 OK. No se requiere que sea el mismo tipo de checker que abrió el incidente.

---

### Flujo 3 — Cierre manual con comentario obligatorio

Invocado desde `IncidentDetailPage` por cualquier rol autenticado.

```
POST /incidents/{id}/close (Razor Page handler OnPostCloseAsync)
    |
    v
IncidentDetailPage recibe: incidentId, comentario
    |
    v
IIncidentService.CloseManuallyAsync(incidentId, closedByRole, comentario)
    |
    v
IIncidentRepository.GetByIdAsync(incidentId)
    |
    +-> [No encontrado] --> lanza NotFoundException → GlobalExceptionHandler → /Error
    |
    +-> [Encontrado] -->
        incident.CloseManually(closedByRole, comentario)
            |
            +-> [comentario vacío] --> lanza DomainException → página muestra error de validación
            |
            +-> [OK] -->
                IIncidentRepository.SaveChangesAsync()
                    |
                    v
                INotificationService.BroadcastCloseAsync(incident.Id)  [U3]
                    |
                    v
                Redirect → HistoricPage
```

---

### Flujo 4 — Consulta histórica paginada

```
HistoricPage.OnGetAsync(filter: IncidentSearchFilter)
    |
    v
filter.WithValidatedTake()   (limita Take a máx 100)
    |
    v
IIncidentService.SearchHistoryAsync(filter)
    |
    v
IIncidentRepository.SearchAsync(filter)
    |
    v
EF Core LINQ:
    WHERE OpenedAt >= filter.From (si presente)
    AND   OpenedAt <= filter.To   (si presente)
    AND   Severity == filter.Severity (si presente)
    AND   Module == filter.Module  (si presente)
    ORDER BY OpenedAt DESC
    COUNT total (para paginación)
    SKIP filter.Skip  TAKE filter.Take
    |
    v
retorna PagedResult<Incident>
    |
    v
HistoricPage renderiza tabla + controles Anterior/Siguiente
```

**Decisión P5 = A:** paginación con botones Anterior/Siguiente (no scroll infinito).

---

### Flujo 5 — Resumen semanal

```
WeeklySummaryPage.OnGetAsync(weekStart: DateOnly)
    |
    v
IIncidentService.GetWeeklySummaryAsync(weekStart)
    |
    v
IIncidentRepository.GetWeeklySummaryAsync(weekStart)
    |
    v
EF Core LINQ:
    WHERE OpenedAt >= weekStart.ToDateTime()
    AND   OpenedAt <  weekStart.AddDays(7).ToDateTime()
    GROUP BY Cause, Severity
    SELECT new WeeklySummaryEntry(
        Cause, Severity,
        Count = group.Count(),
        CandidatosReglaNueva = group.Count(i => i.IsCandidatoReglaNueva)
    )
    ORDER BY Severity DESC, Count DESC
    |
    v
retorna IReadOnlyList<WeeklySummaryEntry>
    |
    v
WeeklySummaryPage renderiza tabla agrupada
```

**Decisión P1 = A:** LINQ sobre EF Core. Sin raw SQL ni stored procedures.

---

### Flujo 6 — Purga de retención 90 días

```
MonitoringSchedulerService (U3) — timer diario al arrancar
    |
    v
IIncidentService.PurgeExpiredIncidentsAsync()
    |
    v
IIncidentRepository.PurgeExpiredAsync(retentionDays: 90)
    |
    v
EF Core:
    DELETE FROM incidents
    WHERE OpenedAt < UtcNow - 90 días
    |
    v
ILogger.LogInformation("purga_incidentes | eliminados={Count}", count)
    |
    v
retorna int (cantidad eliminada)
```

**Decisión P2 = A:** el timer de purga vive dentro del `MonitoringSchedulerService` existente. No se crea un `BackgroundService` adicional.

---

## §2 Reglas de orquestación entre flujos

| Regla | Descripción |
|-------|-------------|
| Flujo 1 y 2 son mutuamente excluyentes por invocación | Un `RunCheckAsync` con OK invoca Flujo 2; con WARN/CRITICAL invoca Flujo 1 |
| Flujo 3 es independiente | El cierre manual puede ocurrir en cualquier momento, sin esperar checks |
| Flujo 6 no bloquea otros flujos | La purga es fire-and-forget dentro del scheduler; no afecta la detección |
| Idempotencia del Flujo 1 | Llamadas repetidas con el mismo módulo en estado CRITICAL no generan duplicados |
