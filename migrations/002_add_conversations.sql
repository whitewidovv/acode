-- migrations/002_add_conversations.sql
--
-- Purpose: Add conversation domain tables (chats, runs, messages, tool calls)
-- Dependencies: 001_initial_schema
-- Author: acode-team
-- Date: 2024-01-02

-- ═══════════════════════════════════════════════════════════════════════
-- CONVERSATION DOMAIN TABLES
-- ═══════════════════════════════════════════════════════════════════════

CREATE TABLE IF NOT EXISTS conv_chats (
    id TEXT PRIMARY KEY,
    title TEXT NOT NULL,
    tags TEXT,  -- JSON array
    worktree_id TEXT,
    is_archived INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    deleted_at TEXT,
    created_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    updated_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    -- Sync metadata
    sync_status TEXT NOT NULL DEFAULT 'pending',
    sync_at TEXT,
    remote_id TEXT,
    version INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS conv_runs (
    id TEXT PRIMARY KEY,
    chat_id TEXT NOT NULL,
    started_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    ended_at TEXT,
    status TEXT NOT NULL DEFAULT 'running',
    created_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    updated_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    sync_status TEXT NOT NULL DEFAULT 'pending',
    sync_at TEXT,
    remote_id TEXT,
    version INTEGER NOT NULL DEFAULT 1,
    CONSTRAINT fk_conv_runs_chat FOREIGN KEY (chat_id)
        REFERENCES conv_chats(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS conv_messages (
    id TEXT PRIMARY KEY,
    run_id TEXT NOT NULL,
    role TEXT NOT NULL,  -- 'user', 'assistant', 'system', 'tool'
    content TEXT,
    metadata TEXT,  -- JSON
    created_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    updated_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    sync_status TEXT NOT NULL DEFAULT 'pending',
    sync_at TEXT,
    remote_id TEXT,
    version INTEGER NOT NULL DEFAULT 1,
    CONSTRAINT fk_conv_messages_run FOREIGN KEY (run_id)
        REFERENCES conv_runs(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS conv_tool_calls (
    id TEXT PRIMARY KEY,
    message_id TEXT NOT NULL,
    tool_name TEXT NOT NULL,
    arguments TEXT,  -- JSON
    result TEXT,
    status TEXT NOT NULL DEFAULT 'pending',
    started_at TEXT,
    completed_at TEXT,
    created_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    CONSTRAINT fk_conv_tool_calls_message FOREIGN KEY (message_id)
        REFERENCES conv_messages(id) ON DELETE CASCADE
);

-- Indexes for conversation domain
CREATE INDEX IF NOT EXISTS idx_conv_chats_worktree ON conv_chats(worktree_id);
CREATE INDEX IF NOT EXISTS idx_conv_chats_sync ON conv_chats(sync_status, sync_at);
CREATE INDEX IF NOT EXISTS idx_conv_chats_created ON conv_chats(created_at DESC);
CREATE INDEX IF NOT EXISTS idx_conv_runs_chat ON conv_runs(chat_id);
CREATE INDEX IF NOT EXISTS idx_conv_runs_status ON conv_runs(status);
CREATE INDEX IF NOT EXISTS idx_conv_messages_run ON conv_messages(run_id);
CREATE INDEX IF NOT EXISTS idx_conv_messages_role ON conv_messages(role);
CREATE INDEX IF NOT EXISTS idx_conv_tool_calls_message ON conv_tool_calls(message_id);
CREATE INDEX IF NOT EXISTS idx_conv_tool_calls_status ON conv_tool_calls(status);
