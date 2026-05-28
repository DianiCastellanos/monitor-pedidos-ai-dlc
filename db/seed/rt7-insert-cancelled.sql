-- RT7: Inserta pedido cancelado para verificar que el checker lo ignora
INSERT INTO simulated_orders (source, status, created_at, is_failure, site)
VALUES ('Salesforce', 'Cancelled', NOW() AT TIME ZONE 'UTC', true, 'Patprimo');
