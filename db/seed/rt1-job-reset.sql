-- RT1 Reset: Restaura job SalesforceDownload a estado normal
UPDATE simulated_job_statuses
SET    status        = 'Completed',
       last_run_at   = GETUTCDATE(),
       error_message = NULL
WHERE  job_name = 'SalesforceDownload';
