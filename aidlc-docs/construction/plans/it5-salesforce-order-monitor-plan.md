# Plan IT5 — Salesforce Order Monitor (OCAPI + OAuth2)

**Fecha:** 2026-05-30  
**Estado:** ✅ Completado  
**Unidades afectadas:** U4 (External Integrations), U6 (Dashboard)

---

## Objetivo

Evolucionar M3-Salesforce de un check de conectividad genérico a un monitor real de pedidos pendientes en Salesforce Commerce Cloud, usando la API OCAPI con autenticación OAuth2 dinámica. M3 detecta pedidos que deberían haber sido descargados por el sistema de integración pero aún permanecen en estado `export_status=ready`.

---

## Alcance

Esta iteración tiene dos partes que se implementaron en secuencia:

| Paso | Descripción |
|------|-------------|
| **Paso 1** | Reemplazar ping genérico por `POST order_search` con query de negocio real |
| **Paso 2** | Reemplazar `ApiKey` estático por OAuth2 dinámico con cache de token y `DelegatingHandler` |

---

## Decisiones de diseño

### Paso 1 — OCAPI Order Search

| # | Decisión | Opción elegida | Razón |
|---|----------|----------------|-------|
| D1 | Método HTTP | `POST /order_search` | OCAPI requiere POST para queries — no GET |
| D2 | Retorno de `ISalesforceClient` | `SalesforceSearchOutcome` record | Captura errores de transporte (401, timeout) y datos de negocio en un solo tipo |
| D3 | Filtro de sites Colombia | Post-procesamiento del response en `SalesforceApiChecker` | La query OCAPI no filtra por `site_id`; el campo viene en cada hit del response |
| D4 | Formato `CheckResult.Details` | `"SITE:COUNT:STATUS"` pipe-delimited | Reutiliza `ParseChannelDetails` existente en Dashboard sin modificaciones |
| D5 | Threshold de alerta | `CriticalThreshold=50` configurable | 0 pedidos = OK; >0 = WARN (job no ha descargado); ≥ umbral = CRITICAL |
| D6 | Ventana de búsqueda | `SearchWindowHours=24` configurable | Limita el payload; captura pedidos recientes sin cargar histórico completo |
| D7 | Template de alerta WARN | `(Api, Warn)` con `ctx.CheckDetails` | Muestra desglose por site en el historial de incidentes abiertos |

### Paso 2 — OAuth2 Dinámico

| # | Decisión | Opción elegida | Razón |
|---|----------|----------------|-------|
| D8 | Gestión del token | `SalesforceTokenCache` Singleton | Un único token válido para toda la app; `SemaphoreSlim` evita refreshes concurrentes |
| D9 | Inyección del token | `SalesforceAuthHandler` `DelegatingHandler` | Transparente para `SalesforceClient` — separación de concerns; el cliente no gestiona auth |
| D10 | Buffer de expiración | 60 segundos antes de `expires_in` | Previene uso de token expirado en requests que llegan justo antes del límite |
| D11 | HttpClient separado para auth | Named client `"SalesforceAuth"` | URL base diferente (host raíz vs `/s/{site}/dw/shop/v23_2/`); timeout más corto (10s vs 15s) |
| D12 | Credenciales | `.env` gitignored — NUNCA en `appsettings.json` | Constraint de seguridad del proyecto: `ClientId` y `ClientPassword` nunca en repositorio |

---

## Arquitectura de la solución

```
SalesforceApiChecker
    └─► ISalesforceClient.SearchPendingOrdersAsync()
            └─► SalesforceClient (typed HttpClient)
                    ├─ POST /order_search (JSON query body)
                    └─ Auth inyectada por SalesforceAuthHandler (DelegatingHandler)
                            └─► SalesforceTokenCache.GetValidTokenAsync()
                                    └─► POST /dw/oauth2/access_token  (named "SalesforceAuth")
                                            Basic auth: base64(ClientId:ClientPassword)
                                            grant_type: urn:demandware:params:...dwsecuretoken
```

### Flujo OAuth2

```
1. Request llega a SalesforceAuthHandler
2. Llama a SalesforceTokenCache.GetValidTokenAsync()
3. Si token válido (no expirado - 60s buffer) → retorna token cacheado
4. Si token expirado o null:
   a. Adquiere SemaphoreSlim (evita refreshes paralelos)
   b. Double-check: si otro thread ya renovó, retorna ese
   c. POST a /dw/oauth2/access_token con Basic auth
   d. Guarda token + calcula _expiresAt = UtcNow + expires_in
   e. Retorna token nuevo
5. Handler agrega: Authorization: Bearer {token}
6. Si respuesta es 401 → llamador puede invalidar caché (SalesforceTokenCache.Invalidate())
```

---

## Query OCAPI — pedidos pendientes de descarga

```
POST {OcapiHost}/s/{OcapiSite}/dw/shop/v23_2/order_search
Authorization: Bearer {token}
Content-Type: application/json
```

**Condiciones de negocio (pedido pendiente):**

| Campo | Valor | Significado |
|-------|-------|-------------|
| `payment_status` | `paid` | Pago confirmado |
| `export_status` | `ready` | Listo para exportar — aún no descargado |
| `c_orderStatus` | `2` | Estado custom — pendiente de descarga |
| `status` | `!= cancelled` | No cancelado |
| `creation_date` | `>= now - SearchWindowHours` | Dentro de la ventana de búsqueda |

**Sites Colombia monitoreados:** `Patprimo`, `SevenSeven`, `Ostu`, `Atmos`

---

## Configuración

### `appsettings.json` (valores vacíos — sin credenciales)

```json
"Salesforce": {
  "OcapiHost":        "",
  "OcapiSite":        "",
  "ClientId":         "",
  "ClientPassword":   "",
  "SearchWindowHours": 24,
  "CriticalThreshold": 50
}
```

### `.env` (gitignored — credenciales reales aquí)

```
Salesforce__OcapiHost=www.atmosmovement.com        # host del storefront (sin https://)
Salesforce__OcapiSite=Atmos                        # site ID OCAPI
Salesforce__ClientId=<uuid>                        # client_id del Account Manager
Salesforce__ClientPassword=<password>              # client_secret del Account Manager
Salesforce__OAuthTokenUrl=https://account.demandware.com/dwsso/oauth2/access_token
Salesforce__OAuthGrantType=client_credentials
```

> **IMPORTANTE (verificado 2026-05-30):** El endpoint OAuth2 correcto para S2S es el **Account Manager** de Demandware (`account.demandware.com`), NO el storefront (`{OcapiHost}/dw/oauth2/access_token`). El storefront rechaza `client_credentials` con `unsupported_grant_type`. El AM devuelve el token con `expires_in=1800s`.

---

## Archivos creados / modificados

### Nuevos

| Archivo | Descripción |
|---------|-------------|
| `MonitorPedidos.Domain/Monitoring/SalesforceSearchOutcome.cs` | Record resultado de `SearchPendingOrdersAsync` — `IsSuccess`, `IsUnauthorized`, `IsTimeout`, `Items`, `ErrorDetails` |
| `MonitorPedidos.Web/Features/ApiChecks/SalesforceTokenCache.cs` | Singleton OAuth2 token cache — `SemaphoreSlim` double-check locking, buffer 60s, método `Invalidate()` |
| `MonitorPedidos.Web/Features/ApiChecks/SalesforceAuthHandler.cs` | `DelegatingHandler` que inyecta `Authorization: Bearer {token}` en cada request |
| `tests/.../SalesforceTokenCacheTests.cs` | 6 unit tests: token válido, cache reutilizado, post-invalidate, 401, sin credenciales, error de red |

### Modificados

| Archivo | Cambio |
|---------|--------|
| `MonitorPedidos.Domain/Monitoring/ISalesforceClient.cs` | `PingOrdersAsync` → `SearchPendingOrdersAsync` retornando `SalesforceSearchOutcome` |
| `MonitorPedidos.Web/Features/ApiChecks/SalesforceClient.cs` | POST a `order_search` con query JSON; deserializa response OCAPI; auth removida (delegada a handler) |
| `MonitorPedidos.Web/Features/ApiChecks/SalesforceApiChecker.cs` | Lógica por site Colombia; desglose `"SITE:COUNT:STATUS"`; thresholds configurables |
| `MonitorPedidos.Web/Features/Monitoring/AlertTemplateRenderer.cs` | Nuevo template `(Api, Warn)` que expone `ctx.CheckDetails` (desglose de sites) |
| `MonitorPedidos.Web/Components/Pages/Dashboard.razor` | `ApplyLastCheckResult` → case `"APIs Externas"` → `ModuleId.SalesforceApi` |
| `MonitorPedidos.Web/Program.cs` | Registro: `SalesforceTokenCache` (Singleton), `SalesforceAuthHandler` (Transient), named `"SalesforceAuth"` HttpClient, `AddHttpMessageHandler<SalesforceAuthHandler>()` |
| `MonitorPedidos.Web/appsettings.json` | `ApiKey` → `ClientId` + `ClientPassword`; añade `OcapiHost`, `OcapiSite`, `SearchWindowHours`, `CriticalThreshold` |
| `MonitorPedidos.Web/appsettings.Development.json` | Sección Salesforce actualizada con todas las claves nuevas (valores vacíos) |
| `MonitorPedidos.Web/.env` | Placeholders de credenciales OAuth2 con comentarios de endpoint |
| `tests/.../SalesforceClientTests.cs` | Reescrito para `SearchPendingOrdersAsync` (POST); auth estática removida del helper `BuildClient` |

---

## Cobertura de tests

| Suite | Tests | Estado |
|-------|-------|--------|
| `SalesforceTokenCacheTests` | 6/6 | ✅ |
| `SalesforceClientTests` | 6/6 | ✅ |
| **ApiChecks total** | **16/16** | ✅ |

**Casos cubiertos por `SalesforceTokenCacheTests`:**
- Token se obtiene correctamente con credenciales válidas
- Segunda llamada retorna el mismo token (no hace segunda request HTTP)
- Después de `Invalidate()`, obtiene token nuevo
- Endpoint devuelve 401 → retorna `null` sin lanzar excepción
- Credenciales no configuradas → retorna `null` sin llamada HTTP
- Error de red → retorna `null` sin lanzar excepción

---

## Restricciones de seguridad (invariantes del proyecto)

- `ClientId` y `ClientPassword` **NUNCA** en `appsettings.json` ni en código fuente
- Credenciales solo en `.env` (gitignored) o variables de entorno
- `ProductionDb` (192.168.20.91) es **READ-ONLY** — este módulo no accede a esa BD
- El token OAuth2 no se loguea (solo se loguea que fue renovado y su tiempo de expiración)

---

## Artefactos de diseño

- [functional-design/domain-entities.md](../iteraciones/it5-salesforce-order-monitor/functional-design/domain-entities.md)
- [functional-design/business-logic-model.md](../iteraciones/it5-salesforce-order-monitor/functional-design/business-logic-model.md)
