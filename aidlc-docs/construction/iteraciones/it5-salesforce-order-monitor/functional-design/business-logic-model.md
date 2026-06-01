# IT5 — Salesforce Order Monitor · Business Logic Model

**Fecha:** 2026-05-30  
**Iteración:** IT5 — Salesforce Commerce Cloud Order Monitor  
**Estado:** ✅ Completado

---

## 1. Objetivo

Reemplazar el check de conectividad genérico de M3-Salesforce por un monitor real de pedidos pendientes en Salesforce Commerce Cloud (OCAPI), detectando pedidos que deberían ser descargados por el sistema de integración pero aún no lo fueron.

---

## 2. Endpoint OCAPI

```
POST https://{OcapiHost}/s/{OcapiSite}/dw/shop/v23_2/order_search
Authorization: Bearer {ApiKey}
Content-Type: application/json
```

**URL base configurada en Program.cs:**
```
https://{OcapiHost}/s/{OcapiSite}/dw/shop/v23_2/
```
Relativa desde el cliente: `order_search`

---

## 3. Query OCAPI — pedidos pendientes de descarga

```json
{
  "query": {
    "filtered_query": {
      "filter": {
        "range_filter": {
          "field": "creation_date",
          "from": "<now - SearchWindowHours>"
        }
      },
      "query": {
        "bool_query": {
          "must": [
            { "term_query": { "fields": ["payment_status"], "operator": "is", "values": ["paid"] } },
            { "term_query": { "fields": ["export_status"],  "operator": "is", "values": ["ready"] } },
            { "term_query": { "fields": ["c_orderStatus"],  "operator": "is", "values": ["2"] } }
          ],
          "must_not": [
            { "term_query": { "fields": ["status"], "operator": "is", "values": ["cancelled"] } }
          ]
        }
      }
    }
  },
  "select": "(count,total,hits.(data.(order_no,export_status,status,creation_date,order_total,payment_status,site_id)))",
  "count": 200,
  "sorts": [{ "field": "creation_date", "sort_order": "asc" }]
}
```

**Condiciones de negocio (pedido pendiente de descarga):**

| Campo | Valor | Significado |
|-------|-------|-------------|
| `payment_status` | `paid` | Pago confirmado |
| `export_status` | `ready` | Listo para exportar al sistema de integración |
| `c_orderStatus` | `2` | Estado personalizado — pendiente de descarga |
| `status` | `!= cancelled` | No cancelado |

---

## 4. Lógica de evaluación por site (Colombia)

```
Sites relevantes = ["Patprimo", "SevenSeven", "Ostu", "Atmos"]

1. Obtener outcome = SearchPendingOrdersAsync()

2. Si !IsSuccess:
   └─ IsUnauthorized → Critical("HTTP 401 — token inválido")
   └─ IsTimeout      → Critical("Timeout")
   └─ Failure        → Critical(outcome.ErrorDetails)

3. Si IsSuccess:
   └─ Por cada site en Colombia:
       count = outcome.Items.Count(i => SiteId == site)
       status = count == 0 ? "OK" : "WARN"
   
   total = sum(site counts)
   
   details = "Patprimo:8:WARN|SevenSeven:0:OK|Ostu:3:WARN|Atmos:0:OK"
   
   └─ total == 0           → CheckResult.Ok(details)
   └─ total >= CritThresh  → CheckResult.Critical(details)
   └─ else                 → CheckResult.Warning(details)
```

**Configuración:**

| Clave | Default | Descripción |
|-------|---------|-------------|
| `Salesforce:OcapiHost` | `""` | Host OCAPI, e.g. `https://xxxx.dx.commercecloud.salesforce.com` |
| `Salesforce:OcapiSite` | `""` | Site ID OCAPI, e.g. `PatPrimo-COL` |
| `Salesforce:ClientId` | `""` | OAuth2 client ID — solo en `.env` (gitignored) |
| `Salesforce:ClientPassword` | `""` | OAuth2 client password — solo en `.env` (gitignored) |
| `Salesforce:SearchWindowHours` | `24` | Ventana de búsqueda (últimas N horas) |
| `Salesforce:CriticalThreshold` | `50` | Total pedidos ≥ N → Critical |

---

## 5. Formato de details — compatible con Dashboard `ParseChannelDetails`

El `CheckResult.Details` siempre usa el formato `"SITE:COUNT:STATUS"` para que el Dashboard pueda mostrar el desglose por site en la card "APIs Externas":

```
"Patprimo:8:WARN|SevenSeven:0:OK|Ostu:3:WARN|Atmos:0:OK"
```

- `STATUS = "OK"` → `IsOk = true` (verde) en la card
- `STATUS != "OK"` → `IsOk = false` (rojo) en la card

Cuando el API no responde (timeout/error), `Details` contiene el mensaje de error y no es parseable como channels — la card muestra solo el indicador de color sin sub-rows.

---

## 6. Visualización en Dashboard

La card "APIs Externas (M3)" muestra sub-rows por site Colombia idénticos al patrón de M2:

```
┌─────────────────────────────┐
│ M3                          │
│ APIs Externas               │
│    ●  (rojo / verde)        │
│ Patprimo     8 pedidos ❌   │
│ SevenSeven   0 pedidos ✅   │
│ Ostu         3 pedidos ❌   │
│ Atmos        0 pedidos ✅   │
│ HH:mm:ss                    │
└─────────────────────────────┘
```

La card obtiene los datos desde `LastCheckStore.Results[SalesforceApi].Details` mediante `ApplyLastCheckResult`.

---

## 7. Archivos modificados

| Archivo | Cambio |
|---------|--------|
| `MonitorPedidos.Domain/Monitoring/SalesforceSearchOutcome.cs` | **Nuevo** — resultado de order search |
| `MonitorPedidos.Domain/Monitoring/ISalesforceClient.cs` | `PingOrdersAsync` → `SearchPendingOrdersAsync` |
| `MonitorPedidos.Web/Features/ApiChecks/SalesforceClient.cs` | POST a `order_search`; deserializa respuesta OCAPI |
| `MonitorPedidos.Web/Features/ApiChecks/SalesforceApiChecker.cs` | Lógica por site; detalles con formato channel |
| `MonitorPedidos.Web/Features/Monitoring/AlertTemplateRenderer.cs` | `+` template `(Api, Warn)` con `ctx.CheckDetails` |
| `MonitorPedidos.Web/Components/Pages/Dashboard.razor` | `ApplyLastCheckResult` → "APIs Externas" → `SalesforceApi` |
| `MonitorPedidos.Web/Program.cs` | `OcapiHost`+`OcapiSite` en HttpClient config |
| `MonitorPedidos.Web/appsettings.json` | `OcapiHost`, `OcapiSite`, `SearchWindowHours`, `CriticalThreshold` |
| `tests/.../SalesforceClientTests.cs` | Tests para `SearchPendingOrdersAsync` (POST) |
