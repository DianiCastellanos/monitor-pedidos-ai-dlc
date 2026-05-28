namespace MonitorPedidos.Domain.Monitoring;

public interface IMultivendeClient
{
    /// <summary>Verifica conectividad en modo solo lectura. Nunca POST/PUT/PATCH/DELETE.</summary>
    Task<ApiPingResult> PingOrdersAsync(CancellationToken ct = default);
}
