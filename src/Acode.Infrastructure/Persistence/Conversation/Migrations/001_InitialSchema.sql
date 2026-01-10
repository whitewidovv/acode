-- 001_InitialSchema.sql
-- Initial database schema for conversation data model
-- SQLite database for local-first storage with PostgreSQL sync capability

-- Chats table (aggregate root)
CREATE TABLE conv_chats (
    id TEXT PRIMARY KEY,
    title TEXT NOT NULL,
    tags TEXT,  -- JSON array
    worktree_id TEXT,
    is_deleted INTEGER DEFAULT 0,
    deleted_at TEXT,
    sync_status TEXT DEFAULT 'Pending',
    version INTEGER DEFAULT 1,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

-- Runs table (child of Chat)
CREATE TABLE conv_runs (
    id TEXT PRIMARY KEY,
    chat_id TEXT NOT NULL,
    model_id TEXT NOT NULL,
    status TEXT NOT NULL,
    started_at TEXT NOT NULL,
    completed_at TEXT,
    tokens_in INTEGER DEFAULT 0,
    tokens_out INTEGER DEFAULT 0,
    sequence_number INTEGER NOT NULL,
    error_message TEXT,
    sync_status TEXT DEFAULT 'Pending',
    FOREIGN KEY (chat_id) REFERENCES conv_chats(id) ON DELETE CASCADE,
    UNIQUE(chat_id, sequence_number)
);

-- Messages table (child of Run)
CREATE TABLE conv_messages (
    id TEXT PRIMARY KEY,
    run_id TEXT NOT NULL,
    role TEXT NOT NULL CHECK(role IN ('user', 'assistant', 'system', 'tool')),
    content TEXT NOT NULL,
    tool_calls TEXT,  -- JSON array of ToolCall objects
    created_at TEXT NOT NULL,
    sequence_number INTEGER NOT NULL,
    sync_status TEXT DEFAULT 'Pending',
    FOREIGN KEY (run_id) REFERENCES conv_runs(id) ON DELETE CASCADE,
    UNIQUE(run_id, sequence_number)
);

-- Indexes for query performance
CREATE INDEX idx_chats_worktree ON conv_chats(worktree_id) WHERE worktree_id IS NOT NULL;
CREATE INDEX idx_chats_is_deleted ON conv_chats(is_deleted);
CREATE INDEX idx_chats_updated_at ON conv_chats(updated_at DESC);
CREATE INDEX idx_chats_sync_status ON conv_chats(sync_status);

CREATE INDEX idx_runs_chat ON conv_runs(chat_id);
CREATE INDEX idx_runs_status ON conv_runs(status);
CREATE INDEX idx_runs_sync_status ON conv_runs(sync_status);

CREATE INDEX idx_messages_run ON conv_messages(run_id);
CREATE INDEX idx_messages_role ON conv_messages(role);
CREATE INDEX idx_messages_sync_status ON conv_messages(sync_status);
