-- 001_InitialSchema_down.sql
-- Rollback script for initial database schema
-- Drops all tables and indexes created in 001_InitialSchema.sql

-- Drop indexes (messages)
DROP INDEX IF EXISTS idx_messages_sync_status;
DROP INDEX IF EXISTS idx_messages_role;
DROP INDEX IF EXISTS idx_messages_run;

-- Drop indexes (runs)
DROP INDEX IF EXISTS idx_runs_sync_status;
DROP INDEX IF EXISTS idx_runs_status;
DROP INDEX IF EXISTS idx_runs_chat;

-- Drop indexes (chats)
DROP INDEX IF EXISTS idx_chats_sync_status;
DROP INDEX IF EXISTS idx_chats_updated_at;
DROP INDEX IF EXISTS idx_chats_is_deleted;
DROP INDEX IF EXISTS idx_chats_worktree;

-- Drop tables (reverse order of creation - children first, then parents)
DROP TABLE IF EXISTS conv_messages;
DROP TABLE IF EXISTS conv_runs;
DROP TABLE IF EXISTS conv_chats;
