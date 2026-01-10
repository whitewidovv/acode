-- migrations/006_add_search_index.sql
--
-- Purpose: Add FTS5 full-text search index for conversation history
-- Dependencies: 002_add_conversations
-- Author: acode-team
-- Date: 2026-01-10

-- ═══════════════════════════════════════════════════════════════════════
-- FTS5 FULL-TEXT SEARCH INDEX
-- ═══════════════════════════════════════════════════════════════════════

CREATE VIRTUAL TABLE IF NOT EXISTS conversation_search USING fts5(
    message_id UNINDEXED,
    chat_id UNINDEXED,
    run_id UNINDEXED,
    created_at UNINDEXED,
    role UNINDEXED,
    content,
    chat_title,
    tags,
    tokenize='porter unicode61'
);

-- ═══════════════════════════════════════════════════════════════════════
-- AUTOMATIC INDEXING TRIGGERS
-- ═══════════════════════════════════════════════════════════════════════

-- Index new messages automatically
CREATE TRIGGER IF NOT EXISTS conversation_search_after_insert
AFTER INSERT ON conv_messages
BEGIN
    INSERT INTO conversation_search (
        message_id,
        chat_id,
        run_id,
        created_at,
        role,
        content,
        chat_title,
        tags
    )
    SELECT
        NEW.id,
        r.chat_id,
        NEW.run_id,
        NEW.created_at,
        NEW.role,
        NEW.content,
        c.title,
        c.tags
    FROM conv_runs r
    INNER JOIN conv_chats c ON r.chat_id = c.id
    WHERE r.id = NEW.run_id;
END;

-- Update index when message updated
CREATE TRIGGER IF NOT EXISTS conversation_search_after_update
AFTER UPDATE ON conv_messages
BEGIN
    DELETE FROM conversation_search WHERE message_id = OLD.id;
    INSERT INTO conversation_search (
        message_id,
        chat_id,
        run_id,
        created_at,
        role,
        content,
        chat_title,
        tags
    )
    SELECT
        NEW.id,
        r.chat_id,
        NEW.run_id,
        NEW.created_at,
        NEW.role,
        NEW.content,
        c.title,
        c.tags
    FROM conv_runs r
    INNER JOIN conv_chats c ON r.chat_id = c.id
    WHERE r.id = NEW.run_id;
END;

-- Remove from index when message deleted
CREATE TRIGGER IF NOT EXISTS conversation_search_after_delete
AFTER DELETE ON conv_messages
BEGIN
    DELETE FROM conversation_search WHERE message_id = OLD.id;
END;

-- Update chat_title in index when chat title changes
CREATE TRIGGER IF NOT EXISTS conversation_search_after_chat_update
AFTER UPDATE OF title ON conv_chats
BEGIN
    UPDATE conversation_search
    SET chat_title = NEW.title
    WHERE chat_id = NEW.id;
END;

-- Update tags in index when chat tags change
CREATE TRIGGER IF NOT EXISTS conversation_search_after_chat_tags_update
AFTER UPDATE OF tags ON conv_chats
BEGIN
    UPDATE conversation_search
    SET tags = NEW.tags
    WHERE chat_id = NEW.id;
END;
