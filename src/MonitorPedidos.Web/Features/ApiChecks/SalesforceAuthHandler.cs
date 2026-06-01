using System.Net.Http.Headers;

namespace MonitorPedidos.Web.Features.ApiChecks;

/// <summary>
/// DelegatingHandler que inyecta automáticamente el Bearer token OCAPI en cada request.
/// Obtiene el token desde SalesforceTokenCache (singleton) — sin lógica de auth en el cliente.
/// </summary>
public sealed class SalesforceAuthHandler(SalesforceTokenCache tokenCache) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        var token = await tokenCache.GetValidTokenAsync(ct);

        if (token is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, ct);
    }
}
