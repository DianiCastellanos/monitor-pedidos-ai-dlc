UPDATE rules
SET
    Name           = 'Brand Monitor — Frecuencia de consulta a Salesforce',
    Description    = 'pollIntervalMinutes: cada cuantos minutos consulta Salesforce y graba en BD (subir = menos consultas, info mas lenta). comparisonWindowSeconds: segundos de historico para comparar backlog (600=10 min). snapshotMinIntervalSeconds: minimo entre snapshots guardados.',
    condition_json = '{"pendingDropThreshold":20,"pollIntervalMinutes":3,"snapshotMinIntervalSeconds":60,"comparisonWindowSeconds":600}'
WHERE AppliesTo = 'BrandMonitor';

SELECT Name, condition_json, Description FROM rules WHERE AppliesTo = 'BrandMonitor';
