-- ============================================================
-- oc_encabezado en MonitorPedidosDb — Schema y Queries
-- ============================================================
-- Servidor destino : 172.16.0.41 / MonitorPedidosDb
-- Servidor origen  : 192.168.20.91 / vtainternet_qa (solo lectura)
-- Sincronizado por : OrderSyncService (cada 5 min, ventana 30 días)
-- ============================================================

-- ============================================================
-- 1. CREAR TABLA (ejecutar una vez en MonitorPedidosDb)
-- ============================================================
IF OBJECT_ID('[dbo].[oc_encabezado]', 'U') IS NOT NULL
    DROP TABLE [dbo].[oc_encabezado];

CREATE TABLE [dbo].[oc_encabezado] (
    [IdAutOrder]        NUMERIC(18,0)  NOT NULL,   -- PK: auto-incremental de vtainternet_qa
    [IdOrder]           NVARCHAR(100)  NULL,        -- Número real del pedido (campo de negocio)
    [Seller]            NVARCHAR(200)  NULL,        -- Vendedor / tienda origen
    [ChannelName]       NVARCHAR(100)  NULL,        -- Canal: 'SALESFORCE' o 'MULTIVENDE'
    [CreationDate]      DATETIME       NULL,        -- Fecha de creación en el canal
    [FechaGeneracion]   DATETIME       NULL,        -- Fecha usada por M2 para la ventana de tiempo
    [EstadoFactura]     NVARCHAR(100)  NULL,        -- Estado de la factura
    [EstadoActualOrden] INT            NULL,        -- Estado numérico del pedido (INT en origen)
    CONSTRAINT [PK_oc_encabezado] PRIMARY KEY ([IdAutOrder])
);

CREATE INDEX [IX_oc_encabezado_FechaGeneracion_Channel]
    ON [dbo].[oc_encabezado] ([FechaGeneracion], [ChannelName]);

-- ============================================================
-- 2. QUERY DE SINCRONIZACIÓN (ejecuta OrderSyncService cada 5 min)
-- ============================================================
-- ORIGEN: vtainternet_qa.oc_encabezado
SELECT IdAutOrder, IdOrder, Seller, ChannelName,
       CreationDate, FechaGeneracion,
       EstadoFactura, EstadoActualOrden
FROM oc_encabezado WITH (NOLOCK)
WHERE FechaGeneracion >= @From    -- @From = DATEADD(DAY, -30, GETDATE())
ORDER BY IdAutOrder ASC;

-- DESTINO: MonitorPedidosDb.oc_encabezado — MERGE por IdAutOrder
MERGE [dbo].[oc_encabezado] AS target
USING (...) AS source (IdAutOrder, IdOrder, Seller, ChannelName,
                        CreationDate, FechaGeneracion,
                        EstadoFactura, EstadoActualOrden)
ON target.IdAutOrder = source.IdAutOrder
WHEN MATCHED THEN UPDATE SET ...
WHEN NOT MATCHED THEN INSERT ...;

-- ============================================================
-- 3. QUERY USADA POR M2 (DbOrderChecker)
-- ============================================================
-- Lee de MonitorPedidosDb (mismo servidor que la app)
SELECT ChannelName, FechaGeneracion
FROM oc_encabezado WITH (NOLOCK)
WHERE FechaGeneracion >= @From     -- últimos 30 min (configurable OrderDetectionWindowMinutes)
  AND FechaGeneracion <= @To;

-- La app agrupa por ChannelName y cuenta pedidos:
-- SALESFORCE: X pedidos | MULTIVENDE: Y pedidos
-- Si algún canal tiene 0 en la ventana → Critical

-- ============================================================
-- 4. VERIFICACIÓN MANUAL
-- ============================================================
-- Total registros sincronizados
SELECT COUNT(*) AS Total, MIN(FechaGeneracion) AS MasAntiguo, MAX(FechaGeneracion) AS MasReciente
FROM oc_encabezado;

-- Últimos 10 pedidos
SELECT TOP 10 IdAutOrder, IdOrder, ChannelName, FechaGeneracion, EstadoActualOrden
FROM oc_encabezado
ORDER BY IdAutOrder DESC;

-- Pedidos últimas 24h por canal (prueba para M2)
SELECT ChannelName, COUNT(*) AS Pedidos
FROM oc_encabezado
WHERE FechaGeneracion >= DATEADD(HOUR, -24, GETDATE())
GROUP BY ChannelName;

-- ============================================================
-- NOTAS
-- ============================================================
-- IdAutOrder : clave técnica (auto-incremental de vtainternet_qa)
-- IdOrder    : número de pedido del canal (campo de negocio)
-- EstadoActualOrden es INT en origen (no varchar)
-- Sincronización cada 5 min — si vtainternet_qa no está disponible,
--   el servicio registra un warning y continúa sin interrumpir la app
