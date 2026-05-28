-- Limpia datos de simulación post-demo
DELETE FROM simulated_orders WHERE created_at < NOW() AT TIME ZONE 'UTC' - INTERVAL '48 hours';

-- Restaura jobs a estado normal si fueron modificados
UPDATE simulated_job_statuses SET status = 'Completed', error_message = NULL;
