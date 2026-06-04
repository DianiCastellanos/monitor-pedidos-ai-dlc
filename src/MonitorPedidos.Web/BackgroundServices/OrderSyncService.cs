using Dapper;
using Microsoft.Data.SqlClient;

namespace MonitorPedidos.Web.BackgroundServices;

/// <summary>
/// Sincroniza órdenes desde vtainternet_qa (ProductionDb) hacia MonitorPedidosDb.
/// Permite que M2 y otros módulos lean de la misma BD de la app sin depender de vtainternet_qa.
/// </summary>
public sealed class OrderSyncService : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<OrderSyncService> _logger;

    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    public OrderSyncService(IConfiguration config, ILogger<OrderSyncService> logger)
    {
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderSyncService iniciado — sincroniza oc_encabezado cada {Min} min", Interval.TotalMinutes);

        // Primera sincronización al arrancar
        await RunSyncAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }

            await RunSyncAsync(stoppingToken);
        }
    }

    private async Task RunSyncAsync(CancellationToken ct)
    {
        var sourceCs = _config.GetConnectionString("ProductionDb");
        var targetCs = _config.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(sourceCs) || string.IsNullOrWhiteSpace(targetCs))
        {
            _logger.LogWarning("[OrderSync] Cadenas de conexión no configuradas — saltando sincronización");
            return;
        }

        try
        {
            // Ventana de sincronización: últimas 48 horas (cubre lo que M2 necesita con margen)
            var windowHours = _config.GetValue("OrderSync:WindowHours", 48);
            var from        = DateTime.Now.AddHours(-windowHours);

            // 1. Leer desde vtainternet_qa
            IEnumerable<OrderRow> sourceRows;
            await using (var sourceConn = new SqlConnection(sourceCs))
            {
                sourceRows = await sourceConn.QueryAsync<OrderRow>(
                    @"SELECT IdOrder, Seller, ChannelName, CreationDate,
                             FechaGeneracion, EstadoFactura, EstadoActualOrden
                      FROM oc_encabezado WITH (NOLOCK)
                      WHERE FechaGeneracion >= @From",
                    new { From = from },
                    commandTimeout: 30);
            }

            var rows = sourceRows.AsList();
            if (rows.Count == 0)
            {
                _logger.LogInformation("[OrderSync] Sin registros nuevos desde {From:HH:mm} — nada que sincronizar", from);
                return;
            }

            // 2. Upsert en MonitorPedidosDb usando MERGE
            await using var targetConn = new SqlConnection(targetCs);
            await targetConn.OpenAsync(ct);

            const string mergeSql = @"
                MERGE [dbo].[oc_encabezado] AS target
                USING (SELECT @IdOrder, @Seller, @ChannelName, @CreationDate,
                              @FechaGeneracion, @EstadoFactura, @EstadoActualOrden)
                      AS source (IdOrder, Seller, ChannelName, CreationDate,
                                 FechaGeneracion, EstadoFactura, EstadoActualOrden)
                ON target.IdOrder = source.IdOrder
                WHEN MATCHED THEN
                    UPDATE SET Seller            = source.Seller,
                               ChannelName       = source.ChannelName,
                               CreationDate      = source.CreationDate,
                               FechaGeneracion   = source.FechaGeneracion,
                               EstadoFactura     = source.EstadoFactura,
                               EstadoActualOrden = source.EstadoActualOrden
                WHEN NOT MATCHED THEN
                    INSERT (IdOrder, Seller, ChannelName, CreationDate,
                            FechaGeneracion, EstadoFactura, EstadoActualOrden)
                    VALUES (source.IdOrder, source.Seller, source.ChannelName,
                            source.CreationDate, source.FechaGeneracion,
                            source.EstadoFactura, source.EstadoActualOrden);";

            var affected = await targetConn.ExecuteAsync(mergeSql, rows, commandTimeout: 60);

            _logger.LogInformation(
                "[OrderSync] Sincronizados {Count} registros desde vtainternet_qa → MonitorPedidosDb (ventana={H}h)",
                affected, windowHours);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Si vtainternet_qa no está disponible (sin VPN) el resto de la app sigue funcionando
            _logger.LogWarning(
                "[OrderSync] No se pudo sincronizar — vtainternet_qa inaccesible: {Msg}",
                ex.Message[..Math.Min(100, ex.Message.Length)]);
        }
    }

    // Clase con setters para compatibilidad con Dapper
    private sealed class OrderRow
    {
        public string    IdOrder           { get; set; } = "";
        public string?   Seller            { get; set; }
        public string    ChannelName       { get; set; } = "";
        public DateTime? CreationDate      { get; set; }
        public DateTime  FechaGeneracion   { get; set; }
        public string?   EstadoFactura     { get; set; }
        public string?   EstadoActualOrden { get; set; }
    }
}
