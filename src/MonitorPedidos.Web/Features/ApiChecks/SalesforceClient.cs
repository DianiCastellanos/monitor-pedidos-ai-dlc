using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using MonitorPedidos.Domain.Monitoring;

namespace MonitorPedidos.Web.Features.ApiChecks;

/// <summary>
/// Cliente OCAPI para Salesforce Commerce Cloud.
/// La autenticación OAuth2 es gestionada por SalesforceAuthHandler — este cliente solo llama al endpoint.
/// </summary>
public sealed class SalesforceClient(
    HttpClient                http,
    IConfiguration            config,
    ILogger<SalesforceClient> logger) : ISalesforceClient
{
    public async Task<SalesforceSearchOutcome> SearchPendingOrdersAsync(CancellationToken ct = default)
    {
        var sites = config.GetSection("Salesforce:Sites").Get<List<SiteConfig>>() ?? [];

        if (sites.Count == 0)
        {
            logger.LogWarning("[SalesforceClient] No hay sites configurados en Salesforce:Sites");
            return SalesforceSearchOutcome.Failure("No hay sites configurados (Salesforce:Sites vacío)");
        }

        // Buscar desde inicio del año en curso — igual que la query de negocio en Postman
        var from = new DateTimeOffset(DateTimeOffset.UtcNow.Year, 1, 1, 0, 0, 0, TimeSpan.Zero).ToString("o");

        // Requests paralelos — uno por site configurado
        var results = await Task.WhenAll(sites.Select(s => SearchSiteAsync(s, from, ct)));

        // Si algún site devolvió 401 el token es inválido para toda la integración
        var unauthorized = Array.Find(results, r => r.IsUnauthorized);
        if (unauthorized is not null) return unauthorized;

        // Agregar items de todos los sites exitosos
        var allItems = results
            .Where(r => r.IsSuccess)
            .SelectMany(r => r.Items)
            .ToList();

        var total = results.Where(r => r.IsSuccess).Sum(r => r.Total);

        logger.LogInformation("[SalesforceClient] order_search multi-site OK — total={Total} hits={Hits}", total, allItems.Count);
        return SalesforceSearchOutcome.Success(total, allItems);
    }

    private async Task<SalesforceSearchOutcome> SearchSiteAsync(SiteConfig site, string from, CancellationToken ct)
    {
        var host = site.Host.TrimEnd('/');
        if (!host.StartsWith("http://") && !host.StartsWith("https://"))
            host = "https://" + host;
        var url = $"{host}/s/{site.SiteId}/dw/shop/v23_2/order_search";

        try
        {
            using var content  = new StringContent(BuildQueryBody(from), Encoding.UTF8, "application/json");
            using var response = await http.PostAsync(url, content, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                logger.LogWarning("[SalesforceClient] {SiteId} HTTP 401 — token rechazado", site.SiteId);
                return SalesforceSearchOutcome.Unauthorized();
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("[SalesforceClient] {SiteId} HTTP {Status} — {Body}",
                    site.SiteId, (int)response.StatusCode, body[..Math.Min(200, body.Length)]);
                return SalesforceSearchOutcome.Failure($"{site.SiteId} HTTP {(int)response.StatusCode}");
            }

            var parsed = await response.Content.ReadFromJsonAsync<OrderSearchResponse>(cancellationToken: ct);
            if (parsed is null)
                return SalesforceSearchOutcome.Failure($"{site.SiteId} respuesta vacía");

            var items = (parsed.Hits ?? [])
                .Where(h => h.Data is not null)
                .Select(h => new SalesforceOrderItem(
                    h.Data!.OrderNo    ?? "",
                    h.Data.SiteId      ?? site.SiteId,
                    h.Data.CreationDate))
                .ToList();

            logger.LogInformation("[SalesforceClient] {SiteId} — total={Total} hits={Hits}", site.SiteId, parsed.Total, items.Count);
            return SalesforceSearchOutcome.Success(parsed.Total, items);
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("[SalesforceClient] {SiteId} Timeout", site.SiteId);
            return SalesforceSearchOutcome.Timeout();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[SalesforceClient] {SiteId} Error inesperado", site.SiteId);
            return SalesforceSearchOutcome.Failure($"{site.SiteId}: {ex.Message}");
        }
    }

    private record SiteConfig
    {
        public string Host   { get; init; } = "";
        public string SiteId { get; init; } = "";
    }

    private static string BuildQueryBody(string from) => $$"""
        {
          "query": {
            "filtered_query": {
              "filter": { "range_filter": { "field": "creation_date", "from": "{{from}}" } },
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
        """;

    // DTOs privados para deserializar la respuesta OCAPI
    private sealed record OrderSearchResponse(
        [property: JsonPropertyName("count")] int Count,
        [property: JsonPropertyName("total")] int Total,
        [property: JsonPropertyName("hits")]  IReadOnlyList<OrderHit> Hits);

    private sealed record OrderHit(
        [property: JsonPropertyName("data")]  OrderHitData? Data);

    private sealed record OrderHitData(
        [property: JsonPropertyName("order_no")]      string?        OrderNo,
        [property: JsonPropertyName("site_id")]       string?        SiteId,
        [property: JsonPropertyName("status")]        string?        Status,
        [property: JsonPropertyName("export_status")] string?        ExportStatus,
        [property: JsonPropertyName("creation_date")] DateTimeOffset CreationDate);
}
