-- ============================================================
-- PASO 1: Registrar migraciones ya aplicadas en __EFMigrationsHistory
-- (Las tablas ya existen en la BD; solo falta el registro de historial)
-- ============================================================
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES
  ('20260527190352_InitialCreate',              '8.0.27'),
  ('20260527224023_AddRetryMetadataToIncidents','8.0.27'),
  ('20260527225620_AddRulesTables',             '8.0.27'),
  ('20260528003426_AddBrandSnapshots',          '8.0.27'),
  ('20260528010133_AddSimulatedTables',         '8.0.27')
ON CONFLICT DO NOTHING;

-- ============================================================
-- PASO 2: Aplicar migración BrandSnapshotsAppendOnly
-- Elimina el índice ÚNICO en site → permite múltiples filas por marca
-- ============================================================
DROP INDEX IF EXISTS "IX_brand_snapshots_Site";

CREATE INDEX IF NOT EXISTS "IX_brand_snapshots_Site_CheckedAt"
  ON brand_snapshots (site, checked_at);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260528162611_BrandSnapshotsAppendOnly', '8.0.27')
ON CONFLICT DO NOTHING;

-- ============================================================
-- PASO 3: Aplicar migración BrandSnapshotPreviousNullable
-- Hace pending_count_previous nullable → permite estado "Sin datos"
-- ============================================================
ALTER TABLE brand_snapshots
  ALTER COLUMN pending_count_previous DROP NOT NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260528171754_BrandSnapshotPreviousNullable', '8.0.27')
ON CONFLICT DO NOTHING;

-- ============================================================
-- VERIFICACIÓN
-- ============================================================
SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
SELECT indexname, indexdef FROM pg_indexes WHERE tablename = 'brand_snapshots';
SELECT column_name, is_nullable FROM information_schema.columns
  WHERE table_name = 'brand_snapshots' AND column_name = 'pending_count_previous';
