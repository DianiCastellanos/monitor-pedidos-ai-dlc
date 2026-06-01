# IT5 — Salesforce Order Monitor · Domain Entities

**Fecha:** 2026-05-30  
**Iteración:** IT5 — Salesforce Commerce Cloud Order Monitor  
**Stories relacionadas:** US-10 (APIs externas), RF-03, RF-06, RF-07  
**Estado:** ✅ Completado

---

## 1. SalesforceSearchOutcome

Resultado discriminado de `SearchPendingOrdersAsync`. Reemplaza `ApiPingResult` para el cliente Salesforce.

```csharp
public sealed record SalesforceSearchOutcome
{
    public bool   IsSuccess      { get; init; }
    public bool   IsUnauthorized { get; init; }
    public bool   IsTimeout      { get; init; }
    public int    Total          { get; init; }
    public IReadOnlyList<SalesforceOrderItem> Items { get; init; } = [];
    public string ErrorDetails   { get; init; } = "";

    public static SalesforceSearchOutcome Success(int total, IReadOnlyList<SalesforceOrderItem> items);
    public static SalesforceSearchOutcome Unauthorized();
    public static SalesforceSearchOutcome Timeout();
    public static SalesforceSearchOutcome Failure(string details);
}
```

## 2. SalesforceOrderItem

Proyección mínima de un pedido Salesforce Commerce Cloud (OCAPI).

```csharp
public record SalesforceOrderItem(
    string         OrderNo,
    string         SiteId,
    DateTimeOffset CreationDate
);
```

Los campos `SiteId` y `OrderNo` provienen del `select` de la query OCAPI:
`(count,total,hits.(data.(order_no,export_status,status,creation_date,order_total,payment_status,site_id)))`

## 3. ISalesforceClient (actualizado)

```csharp
public interface ISalesforceClient
{
    // Reemplaza PingOrdersAsync — consulta business-level, no solo ping
    Task<SalesforceSearchOutcome> SearchPendingOrdersAsync(CancellationToken ct = default);
}
```

`PingOrdersAsync` (conectividad simple) fue eliminado. M3 ya no es un check de conectividad.

---

## 4. DTOs internos de deserialización (privados en SalesforceClient)

```csharp
private record OrderSearchResponse(int Count, int Total, IReadOnlyList<OrderHit> Hits);
private record OrderHit(OrderHitData Data);
private record OrderHitData(string OrderNo, string SiteId, string Status, string ExportStatus, DateTimeOffset CreationDate);
```

Usan `[JsonPropertyName(...)]` para mapear los nombres snake_case de OCAPI.
