using System.Diagnostics;
using System.Net;
using MonitorPedidos.Domain.Monitoring;

namespace MonitorPedidos.Web.Features.ApiChecks;

public sealed class SalesforceClient : ISalesforceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<SalesforceClient> _logger;

    public SalesforceClient(HttpClient http, ILogger<SalesforceClient> logger)
    {
        _http   = http;
        _logger = logger;
    }

    public async Task<ApiPingResult> PingOrdersAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            // Solo GET — nunca POST/PUT/PATCH/DELETE (BR-API-03)
            var response = await _http.GetAsync("orders?$top=1", ct);
            sw.Stop();

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                return ApiPingResult.Unauthorized();

            return response.IsSuccessStatusCode
                ? ApiPingResult.Success((int)sw.ElapsedMilliseconds)
                : ApiPingResult.ServerError((int)response.StatusCode, (int)sw.ElapsedMilliseconds);
        }
        catch (TaskCanceledException)
        {
            return ApiPingResult.Timeout();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SalesforceClient unexpected error");
            return ApiPingResult.ServerError(0, (int)sw.ElapsedMilliseconds);
        }
    }
}
