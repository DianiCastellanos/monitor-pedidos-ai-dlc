namespace MonitorPedidos.Domain.Monitoring;

public interface ISalesforceClient
{
    /// <summary>Busca pedidos pendientes de descarga en OCAPI. Solo lectura — POST a order_search.</summary>
    Task<SalesforceSearchOutcome> SearchPendingOrdersAsync(CancellationToken ct = default);
}
