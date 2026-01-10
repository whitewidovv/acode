-- migrations/006_add_search_index_down.sql
--
-- Purpose: Rollback FTS5 search index
-- Dependencies: None
-- Author: acode-team
-- Date: 2026-01-10

DROP TRIGGER IF EXISTS conversation_search_after_chat_tags_update;
DROP TRIGGER IF EXISTS conversation_search_after_chat_update;
DROP TRIGGER IF EXISTS conversation_search_after_delete;
DROP TRIGGER IF EXISTS conversation_search_after_update;
DROP TRIGGER IF EXISTS conversation_search_after_insert;
DROP TABLE IF EXISTS conversation_search;
