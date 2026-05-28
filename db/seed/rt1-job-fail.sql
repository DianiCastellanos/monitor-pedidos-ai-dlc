-- RT1: Simula job SalesforceDownload apagado
UPDATE simulated_job_statuses
SET    status        = 'Failed',
       last_run_at   = NOW() AT TIME ZONE 'UTC',
       error_message = 'Job SalesforceDownload detenido manualmente (RT1)'
WHERE  job_name = 'SalesforceDownload';
