-- migrations/004_add_approvals.sql
--
-- Purpose: Add approval domain tables for tracking user approvals/denials
-- Dependencies: 001_initial_schema
-- Author: acode-team
-- Date: 2024-01-04

-- ═══════════════════════════════════════════════════════════════════════
-- APPROVAL DOMAIN TABLES
-- ═══════════════════════════════════════════════════════════════════════

CREATE TABLE IF NOT EXISTS appr_records (
    id TEXT PRIMARY KEY,
    action_type TEXT NOT NULL,
    action_payload TEXT,  -- JSON
    status TEXT NOT NULL DEFAULT 'pending',
    requested_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    approved_at TEXT,
    denied_at TEXT,
    reason TEXT,
    created_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    updated_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    sync_status TEXT NOT NULL DEFAULT 'pending',
    sync_at TEXT,
    remote_id TEXT,
    version INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS appr_policies (
    id TEXT PRIMARY KEY,
    action_type TEXT NOT NULL,
    policy_definition TEXT NOT NULL,  -- JSON
    is_active INTEGER NOT NULL DEFAULT 1,
    created_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    updated_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now'))
);

-- Indexes for approval domain
CREATE INDEX IF NOT EXISTS idx_appr_records_status ON appr_records(status);
CREATE INDEX IF NOT EXISTS idx_appr_records_type ON appr_records(action_type);
CREATE INDEX IF NOT EXISTS idx_appr_records_requested ON appr_records(requested_at DESC);
CREATE INDEX IF NOT EXISTS idx_appr_policies_type ON appr_policies(action_type);
CREATE INDEX IF NOT EXISTS idx_appr_policies_active ON appr_policies(is_active);
