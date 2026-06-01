# Estado Actual del Sistema — Por Módulos

**Proyecto:** MonitorPedidos AI
**Empresa:** Manufacturas Eliot (Pat Primo)
**Fecha:** 2026-06-01
**Versión:** 1.2 (2026-06-01 — M11 diagnóstico mejorado: distingue timeout de red vs job deshabilitado; checkers arrancan inmediatamente al iniciar la app)

---

## Propósito del documento

Este documento describe el sistema MonitorPedidos organizado por sus módulos de negocio. Está orientado tanto a técnicos como a negocio. Para cada módulo se explica qué mide, cómo se comporta en condiciones normales y degradadas, y qué ve el usuario en cada escenario.

---

## Arquitectura general

```
APIs externas (Salesforce OCAPI / Multivende)
BD producción (oc_encabezado — solo lectura)
BD aplicación (AppDb — incidentes, snapshots, reglas)
Task Scheduler (SR-SDEV02CO.patprimo.local)
         │
         ▼
MonitoringSchedulerService (BackgroundService)
         │
         ├── Timer 1 (cada 5 min prod / 30s dev)
         │       ├── DbOrderChecker    → M2
         │       ├── DbHealthChecker   → M4
         │       └── JobsChecker       → M11
         │
         ├── Timer 2 (cada 5 min prod / 1 min dev)
         │       ├── SalesforceApiChecker  → M3
         │       └── MultivendeApiChecker  → M3
         │
         └── Timer 3 dinámico (cada 3 min prod / 1 min dev)
                 └── BrandMonitorChecker   → Brand Monitor
                         │
                         ├── Guarda en BD: brand_snapshots (append-only)
                         └── Guarda en LastCheckStore[BrandMonitor].Details
                                 formato: "PatPrimo:14|SevenSeven:3|Ostu:6|Atmos:1"
         │
         ▼
LastCheckStore (singleton en memoria)
         │
         ▼
Dashboard (timer 30s + 60s) / NOC (timer 15s)
```

**Principio clave:** La UI nunca llama APIs externas en el ciclo automático. Solo el scheduler consulta Salesforce/Multivende. La UI lee desde `LastCheckStore` (en memoria, siempre disponible) o BD.

---

## M2 — Ingreso de Pedidos (BD Pedidos)

### Propósito

Detectar cuando no llegan pedidos al sistema de integración desde la BD de producción (`oc_encabezado`). Una ausencia prolongada de pedidos en la ventana de tiempo configurada indica un problema en el flujo de descarga.

### Fuente de datos

BD de producción (SQL Server) — tabla `oc_encabezado` — conexión `ProductionDb` (solo lectura, Dapper).

### Comportamiento normal

- `DbOrderChecker` consulta pedidos en ventana configurable (default 30 min prod / 5 min dev)
- Filtra por canales `["SALESFORCE", "MULTIVENDE"]` y `MinOrders` según regla activa
- Conteo vía `GroupBy(OrderId)` — un pedido con múltiples líneas cuenta como uno
- `CheckResult.Ok` si hay pedidos activos; `CheckResult.Critical` si no hay pedidos en la ventana

### Comportamiento degradado

| Escenario | Estado mostrado | Detalle |
|-----------|-----------------|---------|
| BD de producción inaccesible (sin VPN) | Critical | "Sin acceso a BD de pedidos" |
| Sin pedidos en ventana | Critical | "Sin pedidos activos en últimos N min" |
| Pedidos cancelados solamente | Critical | Los cancelados no cuentan |
| Con pedidos activos | Ok | "N pedidos activos en ventana" |

### Estados posibles

| Estado | Significado |
|--------|-------------|
| Ok | Hay pedidos recientes en la ventana configurada |
| Critical | Sin pedidos activos o BD inaccesible |
| Sin datos (secondary) | Checker aún no ha corrido (~30s desde arranque) |

### Reglas de interpretación

- El conteo incluye solo pedidos activos (no cancelados)
- La ventana y el mínimo son configurables desde la página de Reglas
- Un Critical de M2 con BD accesible indica un problema real en el flujo de pedidos
- Un Critical de M2 con BD inaccesible es un problema de infraestructura (VPN/red)

### Dependencias

- `ProductionDb` (SQL Server, solo lectura)
- Regla activa de módulo M2 (`IRuleRepository.GetActiveByModuleAsync(DbOrders)`)

### UX

- Dashboard: card "BD Pedidos" con badge de color + conteo o mensaje de error
- NOC: card grande con estado principal + último timestamp
- M2 muestra el detalle del checker directamente (no "Esperando datos...")

### Tiempos

- Checker: cada 5 min (prod) / 30s (dev)
- UI Dashboard refresh: cada 30s (lee LastCheckStore)
- UI NOC refresh: cada 15s

---

## M3 — APIs Externas

### Propósito

Verificar la disponibilidad de las APIs de las plataformas de comercio electrónico (Salesforce Commerce Cloud y Multivende). M3 mide **disponibilidad**, no volumen de pedidos.

### Fuente de datos

- Salesforce OCAPI: POST a `order_search` — si responde con HTTP 200 y body válido → disponible
- Multivende API: GET a endpoint de salud — si responde → disponible

### Comportamiento normal

- `SalesforceApiChecker`: retorna `Ok` si Salesforce responde, `Critical` si no responde (401, timeout, error)
- `MultivendeApiChecker`: retorna `Ok` si Multivende responde, `Critical` si no responde
- Los dos checkers son independientes — el fallo de uno no afecta al otro

### Comportamiento degradado

| Escenario | Estado M3 | Detalle |
|-----------|-----------|---------|
| Ambas APIs disponibles | Ok | "Disponible" por integración |
| Una API caída, otra disponible | Critical (escalate-only) | Detalle técnico en la caída |
| Ambas APIs caídas | Critical | Detalle por integración |
| Sin datos aún (arranque) | secondary (gris) | Solo durante ~30s iniciales |

### Estados posibles

| Estado | Significado |
|--------|-------------|
| Ok | Ambas APIs responden correctamente |
| Warn | Al menos una API responde pero con advertencias |
| Critical | Al menos una API no responde |
| Sin datos (secondary) | Ningún checker de API ha corrido aún |

**Escalate-only:** una vez que M3 tiene datos de al menos una integración, nunca vuelve a estado secondary. El estado se calcula con los datos conocidos.

### Reglas de interpretación

- M3 Critical no implica que los pedidos estén bloqueados — puede ser temporal
- M3 Ok no garantiza que los pedidos estén fluyendo — para eso está M2 y Brand Monitor
- El detalle técnico (ej. "HTTP 401 — token inválido") se muestra solo en Critical

### Dependencias

- Salesforce: OAuth2 (Account Manager), endpoint OCAPI
- Multivende: API key, endpoint base configurable

### UX

- Dashboard: card "APIs Externas (M3)" con sub-tabla Salesforce/Multivende
- NOC: card con estado agregado + detalle por integración
- Detalle técnico solo en Critical (no contamina la vista cuando todo va bien)

### Tiempos

- Checkers: cada 5 min (prod) / 1 min (dev)
- UI Dashboard refresh: cada 30s (lee LastCheckStore)
- UI NOC refresh: cada 15s

---

## M4 — Estado de Base de Datos (BD Salud)

### Propósito

Medir la latencia y disponibilidad de la BD de aplicación (`AppDb`). Detecta problemas de conectividad, timeouts y degradación de rendimiento.

### Fuente de datos

`AppDb` (SQL Server) — `SELECT 1` como health check con medición de latencia.

### Comportamiento normal

- `DbHealthChecker` ejecuta `SELECT 1` y mide el tiempo de respuesta
- `Ok` si responde en < 1000 ms
- `Warn` si responde entre 1000 ms y 5000 ms (latencia alta)
- `Critical` si no responde o supera 5000 ms

### Comportamiento degradado

| Escenario | Estado M4 | Detalle |
|-----------|-----------|---------|
| BD responde rápido (< 1s) | Ok | "BD responde en N ms" |
| BD responde lento (1-5s) | Warn | "Latencia alta: N ms" |
| BD sin respuesta / timeout | Critical | "No hay conexión a la base de datos" |
| BD accesible pero con error | Critical | Mensaje limpio (sin SQL técnico) |

### Dependencias

- `AppDb` (SQL Server, lectura/escritura)

### UX

- M4 Critical generalmente implica que M2 también fallará
- Mensaje "No hay conexión a la base de datos" visible sin detalles técnicos en la card

### Tiempos

- Checker: cada 5 min (prod) / 30s (dev)
- UI refresh: igual que M2

---

## M11 — Procesos / Jobs

### Propósito

Verificar que el job de integración `OC_PATPRIMO` en Windows Task Scheduler esté activo y haya ejecutado exitosamente. Este job descarga pedidos de Salesforce al ERP.

### Fuente de datos

Windows Task Scheduler en `SR-SDEV02CO.patprimo.local` — consultado vía `schtasks.exe` o API equivalente.

### Comportamiento normal

- `JobsChecker` verifica que el job existe, está habilitado y su última ejecución fue exitosa
- `Ok` si el job está activo y ejecutó correctamente
- `Critical` si el job falló, está deshabilitado o no se puede conectar al servidor

### Comportamiento degradado

| Escenario | Estado M11 | Mensaje en UI |
|-----------|------------|---------------|
| Job activo y exitoso | Ok | "Enabled" (verde) |
| Servidor inaccesible / timeout red | Critical | `OC_PATPRIMO: Sin conexión a SR-SDEV02CO (timeout 10000ms)` |
| Job deshabilitado (status != Ready) | Critical | `OC_PATPRIMO: Job Disabled — no está en estado Ready` |
| Error de acceso al servidor | Critical | `OC_PATPRIMO: Error al consultar SR-SDEV02CO: ...` |

**Nota de diagnóstico**: el sistema distingue "no puedo llegar al servidor" (problema de red/VPN) de "llegué pero el job está apagado". Esto evita confundir un problema de red con un job realmente deshabilitado.

### Dependencias

- Servidor `SR-SDEV02CO.patprimo.local` (red interna)
- Windows Task Scheduler

### UX

- M11 Critical con M2 Critical = flujo completo de descarga interrumpido
- M11 Critical con M2 Ok = job falló pero pedidos siguen llegando (situación inusual)

### Tiempos

- Checker: cada 5 min (prod) / 30s (dev)

---

## Brand Monitor — Pedidos Pendientes por Descargar

### Propósito

Mostrar en tiempo real cuántos pedidos tienen status `ready` para exportar en Salesforce Commerce Cloud (pedidos pagados, listos para descarga al ERP, no cancelados). Este conteo refleja la "cola de trabajo" pendiente por marca.

### Fuente de datos

Salesforce OCAPI — POST `order_search` con filtros:
- `payment_status = paid`
- `export_status = ready`
- `c_orderStatus = 2`
- `status != cancelled`
- Fecha desde: inicio del año en curso (para alinearse con la query de negocio usada en Postman)

### Marcas monitoreadas

| Site ID | Marca |
|---------|-------|
| PatPrimo | Pat Primo Colombia |
| SevenSeven | Seven Seven Colombia |
| Ostu | Ostu Colombia |
| Atmos | Atmos Colombia |

### Comportamiento normal

Flujo de datos:

```
BrandMonitorChecker (cada 3 min prod / 1 min dev)
        │
        ├── Consulta Salesforce OCAPI → conteos por site
        │
        ├── Por cada site:
        │       ├── GetLatestAsync(site) → snapshot más reciente
        │       ├── GetSnapshotBeforeAsync(site, now - ventana) → snapshot para comparar
        │       ├── DetermineStatus(current, previous):
        │       │       Green  = bajaron los pendientes
        │       │       Yellow = igual
        │       │       Red    = subieron
        │       │       NoData = sin histórico para comparar
        │       └── InsertAsync(snapshot) → solo si cambió o pasó intervalo mínimo
        │
        └── Details = "PatPrimo:14|SevenSeven:3|Ostu:6|Atmos:1"
                    → guardado en LastCheckStore[BrandMonitor].Details
```

### Comportamiento degradado

| Escenario | Qué se muestra | Indicador visual |
|-----------|---------------|------------------|
| BD y Salesforce disponibles | Conteos + tendencia (↑ ↓ =) | Verde/Amarillo/Rojo |
| BD caída, checker corrió | Conteos desde `LastCheckStore` | Amarillo "Sin historial" por fila |
| BD caída, checker no corrió | Skeleton con 4 marcas y "—" | Gris |
| Salesforce caído | Warn en checker, datos anteriores en LastCheckStore | Último dato conocido |
| Botón manual "↻ Actualizar" con BD caída | Llama Salesforce directamente | Banner "⚠ Datos en tiempo real" |

### Estados posibles (por marca)

| Estado | Significado | Flecha |
|--------|-------------|--------|
| Green | Bajaron los pendientes | ↓ verde |
| Yellow | Igual que antes | = amarillo |
| Red | Subieron los pendientes | ↑ rojo |
| NoData | Sin snapshot anterior para comparar | — gris |

### Reglas de negocio

1. La fecha de corte es el inicio del año en curso (no últimas 24h) — alinea con la query de Postman usada por el equipo de negocio
2. Los conteos incluyen todas las marcas activas en Colombia
3. Un conteo alto (pendientes) con tendencia subiendo indica que el job de descarga puede estar atrasado o detenido
4. Un conteo 0 en todas las marcas durante horas de negocio puede indicar que no hay pedidos nuevos o que algo en el flujo de Salesforce falló

### Separación de responsabilidades

- `BrandMonitorChecker` (checker): única responsabilidad — consultar Salesforce y persistir
- Dashboard / NOC (UI): solo lee datos — nunca consulta Salesforce directamente en ciclo automático
- Botón "↻ Actualizar": única excepción — permite al usuario forzar una consulta Salesforce cuando la BD no está disponible

### Dependencias

- Salesforce OCAPI (OAuth2 — Account Manager)
- `AppDb` → tabla `brand_snapshots` (append-only, historial completo)
- `LastCheckStore[BrandMonitor]` (siempre disponible — en memoria)

### UX

**Dashboard:**
- Tabla con 4 filas (una por marca) + columnas: Marca / Pendientes / Tendencia / Hace X min
- Header: "Actualizado · [timestamp] · Próx. verificación · Checker Salesforce: cada 3 min"
- Timer countdown de 60s siempre visible
- Spinner inline "Actualizando..." durante refresh (tabla no desaparece)

**NOC:**
- Tabla compacta con flechas de tendencia grandes
- Sin botón manual (NOC es vista pasiva)
- Se actualiza automáticamente cada 15s

### Tiempos

| Acción | Frecuencia |
|--------|-----------|
| BrandMonitorChecker → Salesforce | cada 3 min (prod) / 1 min (dev) |
| Dashboard timer Brand Monitor | cada 60s |
| Dashboard timer cards principales | cada 30s |
| NOC refresh | cada 15s |

---

## Arquitectura de datos — flujo completo

```
Salesforce OCAPI
        │
        ▼ (cada 3 min, por BrandMonitorChecker)
AppDb (brand_snapshots) ←→ LastCheckStore[BrandMonitor].Details
        │                           │
        │                           │ (siempre disponible en memoria)
        ▼                           ▼
Dashboard.RefreshBrandAsync    Dashboard.GetBrandCountsFromLastCheckStore()
(cada 60s, lee BD primero)     (fallback si BD no disponible)
        │
        ▼
UI — BrandMonitorTable
```

---

## Decisiones de arquitectura clave

### 1. Separación checker / UI

Los checkers son los únicos que consultan fuentes externas (BD producción, APIs, jobs). La UI nunca consulta APIs directamente en ciclos automáticos. Esta separación garantiza:
- Control total del timing de llamadas externas
- Sin doble conteo de pedidos
- Posibilidad de cambiar frecuencias sin tocar la UI

### 2. LastCheckStore como fuente de verdad en tiempo real

`LastCheckStore` es un singleton en memoria que contiene el último `CheckResult` de cada módulo. Es siempre accesible aunque la BD no esté disponible. Contiene:
- Estado del módulo (Ok/Warn/Critical)
- Timestamp del último check
- Detalles en texto (incluyendo el formato pipe-separado para Brand Monitor)

### 3. Escalate-only para estados

El estado de M3 nunca baja de Critical a secondary una vez que tiene datos. Esto evita que un estado incorrecto (gris = "no sé") reemplace uno correcto (rojo = "sé que falló"). Solo en los primeros ~30s de arranque se muestra secondary.

### 4. Append-only para brand_snapshots

La tabla `brand_snapshots` solo tiene `INSERT` — nunca `UPDATE` ni `DELETE`. Esto permite:
- Historial real de tendencias
- Comparación con snapshot de hace X minutos (`GetSnapshotBeforeAsync`)
- Detección de cambios a lo largo del tiempo

---

## Graceful Degradation — resumen ejecutivo

| Componente falla | Qué se pierde | Qué se mantiene |
|-----------------|---------------|----------------|
| VPN / BD AppDb | Historial de incidentes, historial Brand Monitor | Estado actual de todos los módulos desde LastCheckStore, conteos Salesforce en tiempo real |
| BD de producción (oc_encabezado) | Conteo de pedidos M2 | M3, M4, M11, Brand Monitor sin cambio |
| Salesforce OCAPI | Brand Monitor en tiempo real | Último dato válido en LastCheckStore; M2, M4, M11 sin cambio |
| Windows Task Scheduler inaccesible | Estado M11 (checker no puede conectar) | LastCheckStore muestra "Sin acceso a jobs" en rojo; resto sin cambio |

En ningún caso la UI muestra "Error al cargar la página". Si algo falla, el módulo afectado muestra su estado degradado y los demás continúan operando normalmente.

---

## Reglas Críticas del Sistema

Las siguientes reglas son invariantes del sistema. Deben respetarse en toda implementación, refactoring o extensión futura.

### Regla 1 — Brand Monitor: el fallback live a Salesforce NO es automático

**La UI NUNCA llama Salesforce automáticamente cuando la BD falla.**

El único camino automático para datos de Brand Monitor es:

```
BrandMonitorChecker (background) → LastCheckStore → UI
```

El botón "↻ Actualizar" es la **única excepción permitida**: si la BD no está disponible, el usuario puede pulsar ese botón para que la UI llame `GetLiveCountsAsync` (Salesforce directo) en ese instante. Esto NO es automático — requiere acción explícita del usuario.

Cualquier implementación que coloque una llamada a Salesforce dentro de un timer, refresh periódico o ciclo automático de la UI **viola esta regla**.

---

### Regla 2 — Timestamp "Actualizado" = momento real de consulta del checker

El campo "Actualizado" en el header del Brand Monitor **refleja cuándo el checker background consultó Salesforce**, no cuándo la UI hizo refresh.

- En modo normal: viene del `CheckedAt` del snapshot más reciente en BD
- En modo fallback (sin BD): viene del `CheckedAt` en `LastCheckStore[BrandMonitor]`

Si el checker corrió hace 2 minutos y la UI hizo refresh hace 5 segundos, el campo muestra "hace 2 min". Esto es correcto e intencional. El negocio necesita saber cuándo fue la última consulta real a Salesforce, no cuándo se renderizó la página.

---

### Regla 3 — Estado global M3 = peor estado entre sus integraciones

**El card M3 (APIs Externas) siempre muestra el estado más grave entre Salesforce y Multivende.**

| Salesforce | Multivende | Estado M3 |
|------------|------------|-----------|
| Ok         | Ok         | **Ok (verde)** |
| Ok         | Critical   | **Critical (rojo)** |
| Critical   | Ok         | **Critical (rojo)** |
| Critical   | Critical   | **Critical (rojo)** |

M3 **nunca puede mostrar verde si alguna integración está en rojo**. Esta regla aplica en Dashboard y en NOC sin excepción. Está implementada en `ApplyApisWorstStatus`.

---

### Regla 4 — Refresh no-destructivo: los datos anteriores permanecen hasta que llegan datos nuevos

**La tabla del Brand Monitor nunca desaparece durante un refresh.**

- Al iniciar un refresh (automático o manual): se muestra un spinner inline "Actualizando...", pero los datos anteriores permanecen visibles
- El reemplazo de datos es atómico: ocurre en el bloque `finally` solo cuando hay datos confirmados
- Si el refresh falla (BD no disponible, Salesforce timeout, etc.): los datos anteriores permanecen intactos en pantalla — `_brandStale = true` activa solo un banner de advertencia, no borra los datos
- `_brandStale` solo se activa si ya existían datos previos; si no había datos, se mantiene el skeleton

---

### Regla 5 — Modo degradado: último dato conocido + indicación clara del tipo de dato

Cuando la BD no está disponible, el sistema siempre muestra el último dato conocido junto a un banner visible que indica el origen. El usuario nunca ve un estado ambiguo.

Los mensajes exactos según la condición:

| Condición | Banner visible |
|-----------|---------------|
| BD caída, hay snapshots anteriores en pantalla | `⚠ Datos desactualizados` |
| BD no disponible, dato viene de LastCheckStore (checker corrió antes) | `⚠ Datos en tiempo real · BD no disponible` |
| Botón manual "↻ Actualizar" con BD caída (Salesforce consultado ahora) | `⚠ Tiempo real · BD no disponible — datos consultados ahora en Salesforce` |
| Primer arranque, checker no ha corrido, sin datos | `Sin datos aún` (skeleton con "—" en conteos) |

**Garantía:** el usuario siempre sabe si está viendo datos históricos, datos del último check automático, datos consultados en el momento o un estado de "aún sin datos".
