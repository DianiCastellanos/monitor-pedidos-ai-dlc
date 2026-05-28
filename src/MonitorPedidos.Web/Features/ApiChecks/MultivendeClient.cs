using System.Diagnostics;
using System.Net;
using MonitorPedidos.Domain.Monitoring;

namespace MonitorPedidos.Web.Features.ApiChecks;

public sealed class MultivendeClient : IMultivendeClient
{
    private readonly HttpClient _http;
    private readonly ILogger<MultivendeClient> _logger;

    public MultivendeClient(HttpClient http, ILogger<MultivendeClient> logger)
    {
        _http   = http;
        _logger = logger;
    }

    public async Task<ApiPingResult> PingOrdersAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await _http.GetAsync("v1/orders?limit=1", ct);
            sw.Stop();

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
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
            _logger.LogError(ex, "MultivendeClient unexpected error");
            return ApiPingResult.ServerError(0, (int)sw.ElapsedMilliseconds);
        }
    }
}
