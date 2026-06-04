-- ============================================================
-- Tabla oc_encabezado en MonitorPedidosDb
-- Réplica de la estructura original de vtainternet_qa
-- para M2 (DbOrderChecker) — permite despliegue sin acceso a vtainternet_qa
--
-- Servidor: 172.16.0.41
-- Base de datos: MonitorPedidosDb
-- Usuario: salesviewer (solo SELECT sobre esta tabla)
-- ============================================================

IF NOT EXISTS (
    SELECT * FROM sys.objects
    WHERE object_id = OBJECT_ID(N'[dbo].[oc_encabezado]') AND type = N'U'
)
BEGIN
    CREATE TABLE [dbo].[oc_encabezado] (
        [IdOrder]           NVARCHAR(100)  NOT NULL,   -- Identificador único del pedido
        [Seller]            NVARCHAR(200)  NULL,        -- Vendedor / tienda origen
        [ChannelName]       NVARCHAR(100)  NOT NULL,   -- Canal: 'SALESFORCE' o 'MULTIVENDE'
        [CreationDate]      DATETIME       NULL,        -- Fecha de creación en el canal
        [FechaGeneracion]   DATETIME       NOT NULL,   -- Fecha usada por M2 para la ventana de tiempo
        [EstadoFactura]     NVARCHAR(100)  NULL,        -- Estado de la factura
        [EstadoActualOrden] NVARCHAR(100)  NULL,        -- Estado actual del pedido
        CONSTRAINT [PK_oc_encabezado] PRIMARY KEY ([IdOrder])
    );

    -- Índice para la consulta de M2 (DbOrderChecker):
    -- WHERE FechaGeneracion >= @from AND FechaGeneracion <= @to
    CREATE INDEX [IX_oc_encabezado_FechaGeneracion_Channel]
        ON [dbo].[oc_encabezado] ([FechaGeneracion], [ChannelName]);

    PRINT 'Tabla oc_encabezado creada en MonitorPedidosDb';
END
ELSE
    PRINT 'Tabla oc_encabezado ya existe';

-- ============================================================
-- Cómo usa esta tabla el sistema (DbOrderChecker):
--
-- SELECT ChannelName, FechaGeneracion
-- FROM oc_encabezado WITH (NOLOCK)
-- WHERE FechaGeneracion >= @From AND FechaGeneracion <= @To
--
-- La app filtra en C# por ChannelName (SALESFORCE, MULTIVENDE)
-- y cuenta pedidos por canal en la ventana de tiempo configurada
-- (OrderDetectionWindowMinutes = 30 en producción)
-- ============================================================
