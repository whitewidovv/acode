-- migrations/005_add_sync_down.sql
--
-- Rollback: Remove sync domain

DROP INDEX IF EXISTS idx_sync_conflicts_resolved;
DROP INDEX IF EXISTS idx_sync_conflicts_entity;
DROP INDEX IF EXISTS idx_sync_outbox_created;
DROP INDEX IF EXISTS idx_sync_outbox_entity;
DROP INDEX IF EXISTS idx_sync_outbox_status;
DROP TABLE IF EXISTS sync_conflicts;
DROP TABLE IF EXISTS sync_outbox;
