-- 001_WorktreeBindings.sql
-- Worktree-to-Chat binding table for multi-chat concurrency management
-- Enforces one-to-one relationship between worktrees and chats

-- Worktree bindings table
CREATE TABLE worktree_bindings (
    worktree_id TEXT PRIMARY KEY,
    chat_id TEXT NOT NULL UNIQUE,
    created_at TEXT NOT NULL,
    FOREIGN KEY (chat_id) REFERENCES chats(id) ON DELETE CASCADE
);

-- Index for reverse lookup (chat -> worktree)
CREATE INDEX idx_worktree_bindings_chat ON worktree_bindings(chat_id);

-- Comments for clarity
-- worktree_id PRIMARY KEY enforces: one worktree can only bind to one chat
-- chat_id UNIQUE enforces: one chat can only bind to one worktree
-- ON DELETE CASCADE: purging chat automatically unbinds worktree
