-- migrations/003_add_sessions.sql
--
-- Purpose: Add session domain tables for tracking user work sessions
-- Dependencies: 001_initial_schema
-- Author: acode-team
-- Date: 2024-01-03

-- ═══════════════════════════════════════════════════════════════════════
-- SESSION DOMAIN TABLES
-- ═══════════════════════════════════════════════════════════════════════

CREATE TABLE IF NOT EXISTS sess_sessions (
    id TEXT PRIMARY KEY,
    title TEXT NOT NULL,
    worktree_id TEXT,
    status TEXT NOT NULL DEFAULT 'active',
    started_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    ended_at TEXT,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    deleted_at TEXT,
    created_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    updated_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    sync_status TEXT NOT NULL DEFAULT 'pending',
    sync_at TEXT,
    remote_id TEXT,
    version INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS sess_checkpoints (
    id TEXT PRIMARY KEY,
    session_id TEXT NOT NULL,
    description TEXT,
    snapshot TEXT,  -- JSON
    created_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    CONSTRAINT fk_sess_checkpoints_session FOREIGN KEY (session_id)
        REFERENCES sess_sessions(id) ON DELETE CASCADE
);

-- Indexes for session domain
CREATE INDEX IF NOT EXISTS idx_sess_sessions_worktree ON sess_sessions(worktree_id);
CREATE INDEX IF NOT EXISTS idx_sess_sessions_status ON sess_sessions(status);
CREATE INDEX IF NOT EXISTS idx_sess_sessions_started ON sess_sessions(started_at DESC);
CREATE INDEX IF NOT EXISTS idx_sess_checkpoints_session ON sess_checkpoints(session_id);
