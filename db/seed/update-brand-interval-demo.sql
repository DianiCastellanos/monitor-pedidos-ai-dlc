UPDATE rules
SET condition_json = '{"pendingDropThreshold":20,"pollIntervalMinutes":1,"snapshotMinIntervalSeconds":30,"comparisonWindowSeconds":600}'
WHERE AppliesTo = 'BrandMonitor';

SELECT AppliesTo, condition_json FROM rules WHERE AppliesTo = 'BrandMonitor';
