INSERT INTO rules (Id, Name, AppliesTo, Description, condition_json, is_active, Severity, created_at, updated_at)
VALUES
('B1C2D3E4-F5A6-7890-BCDE-F12345678901',
 'Brand Monitor - Pedidos Pendientes por Marca', 'BrandMonitor',
 'Ventana de comparacion de backlog por marca.',
 '{"pendingDropThreshold":20,"snapshotMinIntervalSeconds":60,"comparisonWindowSeconds":600}',
 1, 'Warn', '2026-05-30T00:00:00', NULL),
('A2B3C4D5-E6F7-8901-ABCD-EF1234567890',
 'APIs Externas - Umbral pendientes Salesforce', 'SalesforceApi',
 'Umbral de pedidos pendientes en Salesforce antes de alertar.',
 '{"pendingDropThreshold":50}',
 1, 'Critical', '2026-05-30T00:00:00', NULL),
('A3B4C5D6-E7F8-9012-ABCD-EF1234567890',
 'BD Salud - Latencia de conexion', 'DbHealthChecker',
 'Controla tiempos de respuesta de la BD.',
 '{"latencyWarnMs":1000,"latencyCriticalMs":5000}',
 1, 'Critical', '2026-05-30T00:00:00', NULL),
('A4B5C6D7-E8F9-0123-ABCD-EF1234567890',
 'Jobs - Estado del Task Scheduler', 'JobsMonitor',
 'Monitoreo de trabajos programados via Task Scheduler.',
 '{}',
 1, 'Warn', '2026-05-30T00:00:00', NULL);

SELECT Name, AppliesTo FROM rules ORDER BY AppliesTo;
