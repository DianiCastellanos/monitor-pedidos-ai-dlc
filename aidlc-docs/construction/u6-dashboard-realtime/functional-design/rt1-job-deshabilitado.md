# Validación Red-Team — Bloque 3
## RT1 — Job OC_PATPRIMO deshabilitado

**Fecha**: 2026-06-01  
**Estado**: ✅ 3/3 PASSING  
**Archivo de test**: `tests/e2e/tests/rt1-job-deshabilitado-muestra-critical-e-incidente.spec.ts`

---

## Escenario PRD

> RT1: Apagar job de Salesforce → M11 detecta → CRITICAL en < 10 min

**Método ejecutado**: Deshabilitar manualmente `OC_PATPRIMO` en Windows Task Scheduler
del servidor `SR-SDEV02CO.patprimo.local`.

---

## Cómo funciona el checker (M11)

`SchtasksJobStatusSource` ejecuta:
```
schtasks /QUERY /S SR-SDEV02CO /TN OC_PATPRIMO /FO CSV /NH
```

Parsea la tercera columna del CSV (estado). Solo `"Ready"` → `IsRunning=true`.
Cualquier otro valor (`"Disabled"`, `"Stopped"`, etc.) → `IsRunning=false`.

`JobsChecker` detecta `IsRunning=false` → devuelve `CheckResult.Critical`.

En `Development`, `CheckerIntervalMinutes=0.5` (30s) → fallo detectado en ≤ 30s.

---

## Pasos ejecutados

1. Job `OC_PATPRIMO` deshabilitado manualmente en `SR-SDEV02CO` (por la owner)
2. Test ejecutado inmediatamente — primera detección en **22s**
3. Job re-habilitado tras completar los tests

---

## Resultado

```
Running 3 tests using 1 worker

  ok RT1 — M11 Jobs muestra Critical cuando OC_PATPRIMO está Disabled   22.6s
  ok RT1 — M11 card muestra OC_PATPRIMO como Disabled                   21.8s
  ok RT1 — Incidente M11 creado con causa Job                            2.0s

  3 passed (48.2s)
```

### Validación por test

| Test | Verifica | Resultado |
|---|---|---|
| T1 | `data-status="critical"` en card-m11 dentro de 45s | ✅ Detectado en 22s |
| T2 | Card M11 muestra texto "Disabled" en el job | ✅ Visible |
| T3 | Incidente M11 creado, visible en historial | ✅ Fila "M11" en /incidents |

---

## Evidencia técnica

- Tiempo de detección: **22 segundos** (bien por debajo del límite PRD de < 10 min)
- Causa del incidente: `CauseCategory.Job`
- Alerta en Dashboard: "Alertas Activas" visible
- Template utilizado: `(Job, Critical)` → "Revisar consola de administración de jobs. Reiniciar si es necesario."

---

## Sin errores de implementación

El Bloque 3 no requirió ninguna corrección de código. El checker funcionó
exactamente según el diseño desde el primer intento.
