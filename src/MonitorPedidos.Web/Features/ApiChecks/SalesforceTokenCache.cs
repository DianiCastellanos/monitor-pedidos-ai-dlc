using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace MonitorPedidos.Web.Features.ApiChecks;

/// <summary>
/// Singleton que gestiona el ciclo de vida del access_token OCAPI OAuth2.
/// Cachea el token y lo renueva automáticamente cuando está por vencer.
/// </summary>
public sealed class SalesforceTokenCache(
    IHttpClientFactory             httpFactory,
    IConfiguration                 config,
    ILogger<SalesforceTokenCache>  logger)
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string?        _cachedToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    // Renueva el token 60s antes de que expire para evitar race conditions
    private const int ExpiryBufferSeconds = 60;

    /// <summary>Devuelve un token válido, renovándolo si expiró o está por vencer.</summary>
    public async Task<string?> GetValidTokenAsync(CancellationToken ct = default)
    {
        if (IsTokenValid()) return _cachedToken;

        await _lock.WaitAsync(ct);
        try
        {
            // Double-check después de adquirir el lock
            if (IsTokenValid()) return _cachedToken;
            return await FetchTokenAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>Invalida el token cacheado forzando renovación en el próximo uso.</summary>
    public void Invalidate()
    {
        _cachedToken = null;
        _expiresAt   = DateTimeOffset.MinValue;
    }

    private bool IsTokenValid()
        => _cachedToken is not null
        && DateTimeOffset.UtcNow.AddSeconds(ExpiryBufferSeconds) < _expiresAt;

    private async Task<string?> FetchTokenAsync(CancellationToken ct)
    {
        var clientId = config["Salesforce:ClientId"];
        var password = config["Salesforce:ClientPassword"];

        // TokenUrl configurable: por defecto Account Manager SFCC (S2S)
        // Puede sobrescribirse con Salesforce:OAuthTokenUrl en .env si el AM está en otro host
        var tokenUrl = config["Salesforce:OAuthTokenUrl"];
        if (string.IsNullOrEmpty(tokenUrl))
        {
            // Fallback: derivar del OcapiHost si se configuró una URL custom de token
            var host = config["Salesforce:OcapiHost"]?.TrimEnd('/');
            if (!string.IsNullOrEmpty(host))
            {
                if (!host.StartsWith("http://") && !host.StartsWith("https://"))
                    host = "https://" + host;
                tokenUrl = $"{host}/dw/oauth2/access_token";
            }
        }

        if (string.IsNullOrEmpty(tokenUrl) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(password))
        {
            logger.LogWarning("[SalesforceTokenCache] Credenciales no configuradas — OAuthTokenUrl/ClientId/ClientPassword requeridos");
            return null;
        }

        try
        {
            var http = httpFactory.CreateClient("SalesforceAuth");

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{password}"));

            // Añadir client_id como query param solo si la URL no lo incluye ya
            // (requerido por el storefront SFCC; no necesario para Account Manager)
            var addClientIdParam = config.GetValue("Salesforce:OAuthAddClientIdParam", false);
            var finalUrl = addClientIdParam
                ? $"{tokenUrl}{(tokenUrl.Contains('?') ? "&" : "?")}client_id={Uri.EscapeDataString(clientId)}"
                : tokenUrl;

            using var request = new HttpRequestMessage(HttpMethod.Post, finalUrl);
            request.Headers.Authorization = new("Basic", credentials);
            // grantType configurable: "client_credentials" (S2S) o el grant dwsid (browser-based)
            var grantType = config["Salesforce:OAuthGrantType"]
                ?? "urn:demandware:params:oauth:grant-type:client-id:dwsid:dwsecuretoken";

            request.Content = new FormUrlEncodedContent(
            [
                new KeyValuePair<string, string>("grant_type", grantType)
            ]);

            using var response = await http.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning("[SalesforceTokenCache] Token endpoint HTTP {Status} — body: {Body}",
                    (int)response.StatusCode,
                    errorBody.Length > 500 ? errorBody[..500] : errorBody);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct);
            if (json?.AccessToken is null)
            {
                logger.LogWarning("[SalesforceTokenCache] Respuesta sin access_token");
                return null;
            }

            _cachedToken = json.AccessToken;
            _expiresAt   = DateTimeOffset.UtcNow.AddSeconds(json.ExpiresIn);

            logger.LogInformation(
                "[SalesforceTokenCache] Token renovado — expira en {ExpiresIn}s ({ExpiresAt:HH:mm:ss} UTC)",
                json.ExpiresIn, _expiresAt.UtcDateTime);

            return _cachedToken;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[SalesforceTokenCache] Error al obtener token OAuth2");
            return null;
        }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("expires_in")]   int     ExpiresIn,
        [property: JsonPropertyName("token_type")]   string? TokenType);
}
