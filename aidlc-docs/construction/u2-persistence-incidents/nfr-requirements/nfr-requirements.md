# NFR Requirements — U2 Persistence & Incidents

**Unidad:** U2 — Persistence & Incidents
**Stage:** Construction → NFR Requirements
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 NFRs heredados de Inception (aplican sin cambios)

| ID | Requerimiento | Aplicación en U2 |
|----|---------------|-----------------|
| RNF-01 | Respuesta UI < 2 segundos | `HistoricPage` y `WeeklySummaryPage` deben retornar en < 2s con paginación de 20 registros |
| RNF-06 | Cifrado at-rest | Heredado de U1 — AppDbContext sobre MonitorPedidosDb con cifrado configurado |
| RNF-08 | Logging sin PII | Los logs de apertura/cierre de incidentes no incluyen datos personales — solo ModuleId, Cause, Severity, rol |
| RNF-10 | Queries parametrizadas | EF Core genera parámetros automáticamente — sin riesgo de SQL injection |
| RNF-11 | Autorización en todas las rutas | FallbackPolicy de U1 protege HistoricPage, WeeklySummaryPage e IncidentDetailPage automáticamente |
| RNF-15 | Exception handling fail-closed | GlobalExceptionHandler de U1 captura excepciones no manejadas de U2 |

---

## §2 NFRs de implementación — Decisiones de U2

### NFR-U2-01 — Concurrencia: índice único filtrado para incidente abierto por módulo

**Decisión:** P1 = A

Para garantizar BR-INC-01 (máximo 1 incidente abierto por ModuleId) bajo escrituras concurrentes, se aplica un **índice único filtrado** en la base de datos:

```sql
CREATE UNIQUE INDEX IX_incidents_Module_Open
ON incidents (Module)
WHERE ClosedAt IS NULL;
```

**Configuración EF Core:**
```csharp
entity.HasIndex(i => i.Module)
      .HasFilter("\"ClosedAt\" IS NULL")
      .IsUnique()
      .HasDatabaseName("IX_incidents_Module_Open");
```

**Comportamiento ante colisión:** el repositorio captura `DbUpdateException` con código de violación de unique constraint y retorna el incidente abierto existente (idempotencia server-side).

| Atributo | Valor |
|----------|-------|
| **Tipo** | Constraint de BD (unique index filtrado) |
| **Alcance** | Tabla `incidents` |
| **Cumple** | BR-INC-01, RNF-10, SECURITY-05 |

---

### NFR-U2-02 — AsNoTracking en queries de solo lectura

**Decisión:** P2 = A

Todas las operaciones de lectura en `IIncidentRepository` usan `.AsNoTracking()`. Las operaciones de escritura usan tracking normal.

```csharp
// Lectura — sin tracking
public async Task<PagedResult<Incident>> SearchAsync(IncidentSearchFilter filter, ...)
    => await _context.Incidents.AsNoTracking()
        .Where(/* filtros */)
        .OrderByDescending(i => i.OpenedAt)
        ...

// Escritura — con tracking (default)
public async Task AddAsync(Incident incident, ...)
    => await _context.Incidents.AddAsync(incident, ct);
```

**Beneficio:** reduce uso de memoria del Change Tracker en Blazor Server (cada usuario tiene su propio circuito con su propio DbContext scope). Crítico para el requisito de 5 usuarios concurrentes (RNF-29).

| Atributo | Valor |
|----------|-------|
| **Aplica a** | `SearchAsync`, `GetWeeklySummaryAsync`, `GetRecentClosedByModuleAsync` |
| **No aplica a** | `AddAsync`, `GetByIdAsync` (usado para modificar) |
| **Cumple** | RNF-01 (performance), US-29 (5 usuarios concurrentes) |

---

### NFR-U2-03 — Sin caché para resumen semanal (MVP)

**Decisión:** P3 = A

`GetWeeklySummaryAsync` ejecuta la consulta LINQ directamente en cada request, sin capa de caché.

**Justificación:** con máximo 5 usuarios concurrentes y un volumen de incidentes de MVP (decenas, no millones), el GROUP BY sobre una semana es trivial para MonitorPedidosDb. Añadir `IMemoryCache` introduce complejidad de invalidación sin beneficio medible.

**Condición de revisión:** si en operación real el volumen supera 10.000 incidentes por semana, reconsiderar en una iteración post-MVP.

| Atributo | Valor |
|----------|-------|
| **Estrategia** | Consulta directa sin caché |
| **Revisión** | Post-MVP si volumen > 10k incidentes/semana |
| **Cumple** | RNF-01 (para MVP), simplicidad de mantenimiento |

---

### NFR-U2-04 — Tests de integración con MonitorPedidosDb real

**Decisión:** P4 = A

Los tests de `IncidentRepository` e `IncidentService` usan `WebApplicationFactory<Program>` con la BD MonitorPedidosDb real, no el proveedor InMemory.

**Razón crítica:** el índice único filtrado de NFR-U2-01 **no existe en el proveedor InMemory** de EF Core. Los tests con InMemory no detectarían violaciones de concurrencia ni problemas de traducción LINQ → SQL.

```csharp
// tests/MonitorPedidos.IntegrationTests/Infrastructure/TestWebAppFactory.cs
// Reutiliza TestWebAppFactory de U1, con migración aplicada al arrancar los tests
```

**Tests planificados para U2:**

| ID | Clase | Método | NFR / BR verificado |
|----|-------|--------|---------------------|
| T-U2-01 | `IncidentRepositoryTests` | `OpenIncident_SecondOpenSameModule_ReturnsExisting` | BR-INC-01, NFR-U2-01 |
| T-U2-02 | `IncidentServiceTests` | `CloseManually_EmptyComment_ThrowsDomainException` | BR-CLOSE-01, INV-02 |
| T-U2-03 | `IncidentServiceTests` | `TryCloseOnConsecutiveOk_TwoOk_ClosesIncident` | BR-INC-04, RF-19 |
| T-U2-04 | `IncidentRepositoryTests` | `SearchAsync_FilterBySeverity_ReturnsPaged` | RF-22, BR-SEARCH-01 |
| T-U2-05 | `IncidentServiceTests` | `PurgeExpired_OlderThan90Days_Deleted` | BR-PURGE-01, RF-21 |

---

## §3 SECURITY compliance — U2

| Regla | Estado | Implementación |
|-------|--------|---------------|
| SECURITY-01 (cifrado at-rest) | ✅ N/A en U2 — heredado de U1 | AppDbContext configurado en U1 |
| SECURITY-05 (queries parametrizadas) | ✅ Cumple | EF Core genera parámetros automáticamente; sin raw SQL en U2 |
| SECURITY-06 (validación de input) | ✅ Cumple | `IncidentSearchFilter.WithValidatedTake()` limita Take; `CloseManually` valida comentario server-side |
| SECURITY-08 (autorización rutas) | ✅ Cumple | FallbackPolicy de U1 + `[Authorize]` explícito en las 3 páginas |
| SECURITY-14 (no loggear datos sensibles) | ✅ Cumple | Logs de U2 solo incluyen IDs, módulos, severidad y roles — sin datos de negocio sensibles |
| SECURITY-15 (fail-closed) | ✅ Cumple | `DomainException` de cierre manual propagada al GlobalExceptionHandler; no expone detalles internos |
