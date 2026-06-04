# NFR Design Patterns — U2 Persistence & Incidents

**Unidad:** U2 — Persistence & Incidents
**Stage:** Construction → NFR Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## ADR-U2-01 — Idempotencia con Try-Catch + Re-Query

**Flujo:** Pregunta (¿cómo garantizar 1 incidente abierto por módulo bajo concurrencia?) → NFR-U2-01 (índice único filtrado) → **Patrón Try-Catch + Re-Query**

| Campo | Detalle |
|-------|---------|
| **Estado** | Aceptado |
| **NFR** | NFR-U2-01 |
| **BR** | BR-INC-01 |

### Contexto

El scheduler puede invocar `OpenIncidentAsync` para el mismo módulo desde múltiples threads simultáneos (ej. si el timer se solapa). El índice único filtrado `IX_incidents_Module_Open` garantiza en BD que nunca haya 2 incidentes abiertos del mismo módulo. Necesitamos decidir cómo el repositorio maneja la excepción de violación y presenta una API limpia al servicio.

### Decisión

Implementar **Try-Catch + Re-Query** en `IncidentRepository.AddAsync`:

```csharp
public async Task<Incident> AddOrGetExistingAsync(
    Incident incident, CancellationToken ct = default)
{
    try
    {
        await _context.Incidents.AddAsync(incident, ct);
        await _context.SaveChangesAsync(ct);
        return incident;
    }
    catch (DbUpdateException ex)
        when (ex.InnerException?.Message.Contains("IX_incidents_Module_Open") == true)
    {
        // Colisión por concurrencia — retornar el incidente abierto existente
        _context.ChangeTracker.Clear();
        return await _context.Incidents
            .AsNoTracking()
            .FirstAsync(i => i.Module == incident.Module && i.ClosedAt == null, ct);
    }
}
```

### Alternativas consideradas

| Alternativa | Razón de descarte |
|-------------|------------------|
| Check-then-insert (GetOpen primero, luego Add si null) | Ventana de race condition entre el SELECT y el INSERT. Dos threads pueden pasar el check simultáneamente y ambos intentar insertar. |
| Serializable transaction isolation | Overhead excesivo para MVP. Bloquea la tabla completa durante la transacción. |

### Consecuencias

**Positivas:**
- Maneja concurrencia real — funciona incluso si dos threads pasan simultáneamente
- La BD es la fuente de verdad — el constraint es la defensa final
- API limpia: el servicio siempre recibe un `Incident` válido, nunca una excepción

**Negativas:**
- El catch filtra por nombre del índice (string) — si el índice se renombra, el filtro falla silenciosamente. Mitigación: test T-U2-01 valida este comportamiento.

---

## ADR-U2-02 — Separación Lectura/Escritura con AsNoTracking

**Flujo:** Pregunta (¿cómo optimizar performance de lectura en Blazor Server?) → NFR-U2-02 → **Patrón Read/Write Separation**

| Campo | Detalle |
|-------|---------|
| **Estado** | Aceptado |
| **NFR** | NFR-U2-02 |
| **BR** | RNF-01, US-29 |

### Contexto

Blazor Server crea un scope de DI por circuito (por usuario conectado). Con 5 usuarios concurrentes, cada uno tiene su propio `DbContext`. Si las queries de lectura usan tracking, el Change Tracker acumula objetos en memoria innecesariamente ya que estas queries nunca modifican datos.

### Decisión

Aplicar `.AsNoTracking()` en todas las queries de solo lectura. Establecer una convención explícita en el repositorio:

```
Lectura  → AsNoTracking()   [Search, GetWeeklySummary, GetRecentClosed]
Escritura → Tracking normal  [Add, GetById para modificar]
```

**Regla:** cualquier método de `IIncidentRepository` que retorne `IReadOnlyList<T>` o `PagedResult<T>` usa `AsNoTracking()` obligatoriamente.

### Alternativas consideradas

| Alternativa | Razón de descarte |
|-------------|------------------|
| Tracking por defecto en todo | Desperdicia memoria del Change Tracker en objetos que nunca se modifican. Con listas de 20-100 incidentes por query, el impacto es medible. |
| DbContext separado para lectura (CQRS completo) | Overkill para MVP. Complejidad innecesaria con 5 usuarios concurrentes. |

### Consecuencias

**Positivas:**
- Menor presión de memoria por circuito Blazor
- Queries de lectura ~15-20% más rápidas (sin overhead del Change Tracker)
- Hace explícita la intención: si ves `AsNoTracking()`, sabes que el objeto no se va a modificar

**Negativas:**
- El desarrollador debe recordar la convención. Mitigación: regla documentada aquí y reforzada en code review.

---

## ADR-U2-03 — Purga Masiva con ExecuteDeleteAsync

**Flujo:** Pregunta (¿cómo eliminar registros expirados eficientemente?) → BR-PURGE-01 → **Patrón Bulk Delete con ExecuteDeleteAsync**

| Campo | Detalle |
|-------|---------|
| **Estado** | Aceptado |
| **NFR** | NFR-U2-03 (sin caché), BR-PURGE-01 |
| **BR** | BR-PURGE-01, BR-PURGE-02 |

### Contexto

La purga de retención elimina incidentes con `OpenedAt < UtcNow - 90 días`. En producción real (post-MVP) esto podría ser cientos de registros por ejecución. El enfoque tradicional (`RemoveRange` + `SaveChanges`) carga todas las entidades en memoria antes de eliminarlas — innecesario y costoso.

### Decisión

Usar `ExecuteDeleteAsync` (disponible desde EF Core 7):

```csharp
public async Task<int> PurgeExpiredAsync(int retentionDays, CancellationToken ct)
{
    var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays);
    return await _context.Incidents
        .Where(i => i.OpenedAt < cutoff)
        .ExecuteDeleteAsync(ct);
}
```

Esto genera directamente:
```sql
DELETE FROM incidents WHERE OpenedAt < @cutoff
```

Sin cargar ninguna entidad en memoria.

### Alternativas consideradas

| Alternativa | Razón de descarte |
|-------------|------------------|
| `RemoveRange` + `SaveChanges` | Carga todas las entidades expiradas en memoria. Con 90 días de datos acumulados sin purga previa, puede ser cientos de objetos innecesarios. |
| SQL raw (`ExecuteSqlRaw`) | Funciona pero pierde el type-safety de LINQ. `ExecuteDeleteAsync` es la opción idiomática de EF Core 7+. |

### Consecuencias

**Positivas:**
- Un solo round-trip a la BD independientemente del volumen
- Sin presión de memoria — no carga entidades
- Sintaxis LINQ mantenible (no SQL crudo)

**Negativas:**
- `ExecuteDeleteAsync` bypasea el Change Tracker — los eventos de dominio (si se añaden en el futuro) no se dispararán. Aceptable: la purga es una operación de mantenimiento, no de negocio.

---

## ADR-U2-04 — Tests de Integración con MonitorPedidosDb Real

**Flujo:** Pregunta (¿cómo testear constraints de BD y comportamiento EF Core?) → NFR-U2-04 → **Patrón Integration Tests con WebApplicationFactory + MonitorPedidosDb**

| Campo | Detalle |
|-------|---------|
| **Estado** | Aceptado |
| **NFR** | NFR-U2-04 |
| **Tests** | T-U2-01 a T-U2-05 |

### Contexto

El índice único filtrado `IX_incidents_Module_Open` no existe en el proveedor InMemory de EF Core. Usar InMemory haría que T-U2-01 (test de idempotencia concurrente) nunca fallara correctamente, dando falsa seguridad.

### Decisión

Reutilizar `TestWebAppFactory` de U1, extendida para U2:

```csharp
public sealed class TestWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Usar MonitorPedidosDb real — misma cadena de conexión con BD de test separada
        builder.ConfigureServices(services =>
        {
            // Reemplazar connection string con BD de test
            var descriptor = services.Single(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(
                    "Server=(localdb)\\mssqllocaldb;Database=MonitorPedidosTest;..."));
        });
    }
}
```

### Alternativas consideradas

| Alternativa | Razón de descarte |
|-------------|------------------|
| EF Core InMemory | No ejecuta constraints de BD. El índice único filtrado de NFR-U2-01 no existe en InMemory. Tests dan falsos positivos. |
| SQLite en memoria | Más cercano a SQL real pero no soporta índices filtrados con la misma sintaxis que SQL Server. |

### Consecuencias

**Positivas:**
- Los tests detectan problemas reales de SQL (constraints, traducción LINQ, índices)
- La BD de test se crea/migra automáticamente antes de cada test suite

**Negativas:**
- Requiere MonitorPedidosDb instalado en la máquina de desarrollo y CI
- Tests más lentos que InMemory (~500ms vs ~50ms por test)

---

## §5 Resumen de patrones por NFR

| NFR / BR | Patrón | ADR |
|----------|--------|-----|
| NFR-U2-01, BR-INC-01 | Try-Catch + Re-Query (idempotencia concurrente) | ADR-U2-01 |
| NFR-U2-02, RNF-01 | AsNoTracking en lecturas / Tracking en escrituras | ADR-U2-02 |
| BR-PURGE-01, BR-PURGE-02 | ExecuteDeleteAsync (bulk delete sin cargar memoria) | ADR-U2-03 |
| NFR-U2-04 | WebApplicationFactory + MonitorPedidosDb real | ADR-U2-04 |
