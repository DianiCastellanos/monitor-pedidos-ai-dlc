namespace MonitorPedidos.Domain.Monitoring;

public sealed record ApiPingResult(
    int    HttpStatusCode,
    int    LatencyMs,
    bool   IsSuccess,
    bool   IsUnauthorized,
    string Details)
{
    public static ApiPingResult Success(int latencyMs)
        => new(200, latencyMs, true, false, "OK");

    public static ApiPingResult Unauthorized()
        => new(401, 0, false, true, "HTTP 401 — token inválido o expirado");

    public static ApiPingResult ServerError(int httpStatus, int latencyMs)
        => new(httpStatus, latencyMs, false, false, $"HTTP {httpStatus}");

    public static ApiPingResult Timeout()
        => new(0, 0, false, false, "Timeout — API no respondió en el tiempo límite");
}
