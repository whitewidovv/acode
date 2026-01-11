-- migrations/001_initial_schema_down.sql
--
-- Rollback: Remove base schema

DROP INDEX IF EXISTS idx_sys_health_checked;
DROP INDEX IF EXISTS idx_sys_health_check;
DROP TABLE IF EXISTS sys_health;
DROP TABLE IF EXISTS sys_locks;
DROP TABLE IF EXISTS sys_config;
DROP TABLE IF EXISTS __migrations;
