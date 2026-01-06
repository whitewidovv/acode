-- migrations/002_add_conversations_down.sql
--
-- Rollback: Remove conversation domain

DROP INDEX IF EXISTS idx_conv_tool_calls_status;
DROP INDEX IF EXISTS idx_conv_tool_calls_message;
DROP INDEX IF EXISTS idx_conv_messages_role;
DROP INDEX IF EXISTS idx_conv_messages_run;
DROP INDEX IF EXISTS idx_conv_runs_status;
DROP INDEX IF EXISTS idx_conv_runs_chat;
DROP INDEX IF EXISTS idx_conv_chats_created;
DROP INDEX IF EXISTS idx_conv_chats_sync;
DROP INDEX IF EXISTS idx_conv_chats_worktree;
DROP TABLE IF EXISTS conv_tool_calls;
DROP TABLE IF EXISTS conv_messages;
DROP TABLE IF EXISTS conv_runs;
DROP TABLE IF EXISTS conv_chats;
