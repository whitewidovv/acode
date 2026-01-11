-- migrations/004_add_approvals_down.sql
--
-- Rollback: Remove approval domain

DROP INDEX IF EXISTS idx_appr_policies_active;
DROP INDEX IF EXISTS idx_appr_policies_type;
DROP INDEX IF EXISTS idx_appr_records_requested;
DROP INDEX IF EXISTS idx_appr_records_type;
DROP INDEX IF EXISTS idx_appr_records_status;
DROP TABLE IF EXISTS appr_policies;
DROP TABLE IF EXISTS appr_records;
