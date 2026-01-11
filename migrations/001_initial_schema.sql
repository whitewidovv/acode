-- migrations/001_initial_schema.sql
--
-- Purpose: Create base schema including version tracking and system tables
-- Dependencies: None (initial migration)
-- Author: acode-team
-- Date: 2024-01-01

-- ═══════════════════════════════════════════════════════════════════════
-- MIGRATION TRACKING TABLE
-- ═══════════════════════════════════════════════════════════════════════

CREATE TABLE IF NOT EXISTS __migrations (
    version TEXT PRIMARY KEY,
    applied_at TEXT NOT NULL,
    checksum TEXT NOT NULL,
    rollback_checksum TEXT
);

-- ═══════════════════════════════════════════════════════════════════════
-- SYSTEM DOMAIN TABLES
-- ═══════════════════════════════════════════════════════════════════════

CREATE TABLE IF NOT EXISTS sys_config (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL,
    is_internal INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    updated_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now'))
);

CREATE TABLE IF NOT EXISTS sys_locks (
    name TEXT PRIMARY KEY,
    holder TEXT NOT NULL,
    acquired_at TEXT NOT NULL,
    expires_at TEXT
);

CREATE TABLE IF NOT EXISTS sys_health (
    id TEXT PRIMARY KEY,
    check_name TEXT NOT NULL,
    status TEXT NOT NULL,
    details TEXT,
    checked_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now'))
);

CREATE INDEX IF NOT EXISTS idx_sys_health_check ON sys_health(check_name);
CREATE INDEX IF NOT EXISTS idx_sys_health_checked ON sys_health(checked_at);
