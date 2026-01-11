-- migrations/003_add_sessions_down.sql
--
-- Rollback: Remove session domain

DROP INDEX IF EXISTS idx_sess_checkpoints_session;
DROP INDEX IF EXISTS idx_sess_sessions_started;
DROP INDEX IF EXISTS idx_sess_sessions_status;
DROP INDEX IF EXISTS idx_sess_sessions_worktree;
DROP TABLE IF EXISTS sess_checkpoints;
DROP TABLE IF EXISTS sess_sessions;
