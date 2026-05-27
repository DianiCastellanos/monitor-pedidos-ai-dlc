# Frontend Components — U4 External Integrations

**Unidad:** U4 — External Integrations
**Stage:** Construction → Functional Design
**Fecha:** 2026-05-23
**Versión:** 1.0

---

## §1 Páginas Blazor en U4

U4 **no introduce páginas nuevas**. Su única contribución de UI es una **actualización a `IncidentDetailPage`** (U2) para mostrar la sección de `auto_reintentos` cuando el incidente es de tipo API.

| Cambio | Tipo | Página afectada |
|--------|------|----------------|
| Sección `auto_reintentos` cronológica | Adición a página existente | `IncidentDetailPage` (U2) |

---

## §2 Actualización: IncidentDetailPage — Sección auto_reintentos

### Regla de visibilidad

La sección de reintentos es visible **únicamente** cuando:
1. El incidente tiene `Cause == Api` (no Token — los 401 no tienen reintentos)
2. `incident.GetRetryAttempts().Any()` es `true`
3. El usuario tiene rol **Técnico** (BR-INCIDENT-U4-03)

### Estructura visual

```
╔══════════════════════════════════════════════════════════╗
║  Incidente #abc123 — CRÍTICO — Salesforce API            ║
║  Módulo: SalesforceApi   Abierto: 15/05/2026 15:32       ║
╠══════════════════════════════════════════════════════════╣
║  Qué pasó:      API externa no responde...               ║
║  Causa probable: 2 reintentos automáticos realizados     ║
║  Acción:        Verificar estado de la API...            ║
╠══════════════════════════════════════════════════════════╣
║  [solo rol Técnico — si Cause == Api y hay reintentos]   ║
║                                                          ║
║  ┌─ Auto-reintentos (2/2) ──────────────────────────┐   ║
║  │  #  │ Timestamp       │ HTTP  │ Latencia          │   ║
║  │ ─── │ ─────────────── │ ───── │ ────────────────  │   ║
║  │  1  │ 15:32:06        │  503  │ 1.240 ms          │   ║
║  │  2  │ 15:32:12        │  503  │ 1.580 ms          │   ║
║  └─────────────────────────────────────────────────-┘   ║
╠══════════════════════════════════════════════════════════╣
║  [Cerrar manualmente]                                    ║
╚══════════════════════════════════════════════════════════╝
```

### Interacciones de la página (actualizadas)

```
IncidentDetailPage (U2 + U4)
    |
    OnInitializedAsync
        → IIncidentService.GetByIdAsync(id)
        → AuthenticationStateProvider → ClaimTypes.Role
        → incident.GetRetryAttempts()  ← NUEVO en U4
    |
    Render:
        → sección principal (igual que U2)
        → @if (userRole == "Técnico" && retryAttempts.Any() && incident.Cause == Api)
              tabla cronológica de retryAttempts  ← NUEVO en U4
        → botón "Cerrar manualmente" (igual que U2)
```

### Código Razor de la sección (referencia)

```razor
@* En IncidentDetailPage.razor — agregar tras la sección de detalles *@

@if (_userRole == "Técnico" && _retryAttempts.Any())
{
    <div class="retry-section">
        <h4>Auto-reintentos (@_retryAttempts.Count/2)</h4>
        <table>
            <thead>
                <tr>
                    <th>#</th>
                    <th>Timestamp</th>
                    <th>HTTP</th>
                    <th>Latencia</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var attempt in _retryAttempts)
                {
                    <tr>
                        <td>@attempt.AttemptNumber</td>
                        <td>@attempt.AttemptedAt.ToLocalTime().ToString("HH:mm:ss")</td>
                        <td>@attempt.HttpStatusCode</td>
                        <td>@attempt.LatencyMs ms</td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
}
```

---

## §3 Lo que U4 NO muestra en UI

| Elemento | Razón |
|----------|-------|
| Credenciales (API keys) | Nunca expuestas en UI — BR-CRED-03 |
| Detalle de headers HTTP | Solo disponible en logs de Serilog — no en dashboard |
| Reintentos de Token (401) | Los 401 no generan reintentos — sección no aplica |
| Alertas Token en tiempo real | Igual que cualquier alerta — llega vía SignalR de U6 |

---

## §4 Trazabilidad

| Componente | Story | RF | Seguridad |
|-----------|-------|----|-----------| 
| Sección `auto_reintentos` en `IncidentDetailPage` | US-16 | RF-06, RF-27 | SECURITY-06 (solo Técnico), SECURITY-08 (autorización server-side) |
| Restricción rol Técnico en sección reintentos | US-16 | RF-27 | SECURITY-06, SECURITY-08 |
| `AlertMessage.AccionSugerida` con SOP-001 | US-10 | RF-11 | — |
