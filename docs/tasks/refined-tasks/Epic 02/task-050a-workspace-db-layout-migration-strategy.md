# Task 050.a: Workspace DB Layout + Migration Strategy

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 2 – Persistence + Reliability Core  
**Dependencies:** Task 050 (Database Foundation)  

---

## Description

Task 050.a defines the workspace database layout and migration strategy. This includes the schema organization, table naming conventions, migration file structure, and versioning approach for both SQLite (local) and PostgreSQL (remote).

The database layout establishes consistency across all persistence features. Tables follow naming conventions. Columns use standardized types. Indexes follow patterns. This consistency simplifies development and maintenance.

Migration strategy ensures safe schema evolution. Migrations are versioned and ordered. Each migration is reversible. Failed migrations roll back cleanly. The strategy handles both local SQLite and remote PostgreSQL.

Schema design respects storage differences. SQLite and PostgreSQL have different capabilities. The layout uses a common subset where possible. Database-specific features are isolated to specific migrations.

The layout organizes tables by domain. Conversation tables (chats, runs, messages) are prefixed. Session tables (sessions, checkpoints) are prefixed. Approval tables are prefixed. This organization aids navigation.

Migration files follow a strict structure. Naming includes version number and description. Each migration has up and down scripts. SQL is written for both SQLite and PostgreSQL when they differ.

Version tracking uses a dedicated table. The __migrations table records applied versions. Timestamps track when migrations ran. Checksums detect tampering. This enables safe upgrade detection.

Dependency ordering ensures migrations apply correctly. Some tables depend on others (foreign keys). The migration order respects dependencies. Circular dependencies are prevented by design.

Index strategy balances performance and storage. Primary keys are always indexed. Foreign keys are indexed. Common query patterns get indexes. Unused indexes are avoided.

The layout supports the offline-first model. Sync metadata columns exist on relevant tables. Outbox and inbox tables support sync. Conflict tracking columns exist.

Documentation accompanies the schema. Each table has purpose documentation. Each column has type and constraint notes. Relationships are documented. This documentation lives with the migrations.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Layout | Schema organization |
| Migration | Schema change script |
| Up Script | Apply change |
| Down Script | Reverse change |
| Versioning | Sequential numbering |
| Convention | Naming standards |
| Prefix | Table name grouping |
| Dependency | Table relationship |
| Checksum | Integrity verification |
| Common Subset | Cross-database features |
| Index | Query optimization |
| Foreign Key | Relationship constraint |
| Sync Metadata | Columns for sync |
| Outbox | Pending uploads |
| Inbox | Pending downloads |

---

## Out of Scope

The following items are explicitly excluded from Task 050.a:

- **Connection code** - Task 050.b
- **Migration runner** - Task 050.c
- **Health checks** - Task 050.d
- **Backup/export** - Task 050.e
- **Specific feature schemas** - Other tasks
- **Query optimization** - Per-feature
- **Performance tuning** - Runtime
- **Sharding** - Not supported
- **Partitioning** - Not supported
- **Stored procedures** - Not used

---

## Functional Requirements

### Table Naming

- FR-001: Tables MUST use snake_case
- FR-002: Tables MUST have domain prefix
- FR-003: Prefixes: conv_, sess_, appr_, sync_, sys_
- FR-004: Join tables: {table1}_{table2}
- FR-005: Reserved prefix: __

### Column Naming

- FR-006: Columns MUST use snake_case
- FR-007: Primary key MUST be id
- FR-008: Foreign key MUST be {table}_id
- FR-009: Timestamps MUST be {action}_at
- FR-010: Booleans MUST be is_{condition}

### Data Types

- FR-011: IDs MUST be TEXT (ULID)
- FR-012: Timestamps MUST be TEXT (ISO 8601)
- FR-013: Booleans MUST be INTEGER (SQLite)
- FR-014: JSON MUST be TEXT (SQLite), JSONB (Postgres)
- FR-015: Enums MUST be TEXT

### Primary Keys

- FR-016: Every table MUST have primary key
- FR-017: Primary key MUST be id column
- FR-018: Composite keys MUST NOT be used

### Foreign Keys

- FR-019: Relationships MUST use foreign keys
- FR-020: ON DELETE MUST be specified
- FR-021: Cascade MUST be explicit
- FR-022: Foreign key indexes MUST exist

### Migration Files

- FR-023: Filename: NNN_description.sql
- FR-024: NNN MUST be 3-digit zero-padded
- FR-025: Description MUST be snake_case
- FR-026: Up script: NNN_description.sql
- FR-027: Down script: NNN_description_down.sql

### Migration Content

- FR-028: Migration MUST be idempotent
- FR-029: Migration MUST have comment header
- FR-030: Comment MUST include purpose
- FR-031: Comment MUST include dependencies

### Database-Specific

- FR-032: SQLite-specific MUST be marked
- FR-033: Postgres-specific MUST be marked
- FR-034: Common MUST work on both
- FR-035: Dialect flag MUST exist

### Version Table

- FR-036: Table MUST be __migrations
- FR-037: Columns: version, applied_at, checksum
- FR-038: Version MUST be unique
- FR-039: Checksum MUST be SHA-256

### Sync Columns

- FR-040: sync_status MUST exist where needed
- FR-041: sync_at MUST exist where needed
- FR-042: remote_id MUST exist where needed
- FR-043: version MUST exist for conflict

### System Tables

- FR-044: sys_config for settings
- FR-045: sys_locks for coordination
- FR-046: sys_health for status

### Indexes

- FR-047: Primary key MUST be indexed
- FR-048: Foreign keys MUST be indexed
- FR-049: Unique constraints MUST be indexed
- FR-050: Query patterns MUST be indexed

---

## Non-Functional Requirements

### Consistency

- NFR-001: All tables follow conventions
- NFR-002: All migrations are reversible
- NFR-003: Cross-database compatibility

### Maintainability

- NFR-004: Clear naming
- NFR-005: Documentation in migrations
- NFR-006: Logical grouping

### Performance

- NFR-007: Minimal index overhead
- NFR-008: Efficient data types
- NFR-009: No redundant constraints

### Reliability

- NFR-010: Safe migrations
- NFR-011: Checksum verification
- NFR-012: Dependency ordering

---

## User Manual Documentation

### Overview

The workspace database layout defines how data is organized. Understanding the layout helps with debugging, custom queries, and data export.

### Table Organization

```
Domain Prefixes:
  conv_  = Conversation (chats, runs, messages)
  sess_  = Session (sessions, checkpoints, state)
  appr_  = Approvals (records, rules)
  sync_  = Sync (outbox, inbox, conflicts)
  sys_   = System (config, locks, health)
  __     = Reserved (migrations)
```

### Core Tables

```sql
-- Conversation domain
conv_chats         -- Chat threads
conv_runs          -- Execution runs within chats
conv_messages      -- Individual messages
conv_tool_calls    -- Tool invocations

-- Session domain
sess_sessions      -- Active sessions
sess_checkpoints   -- State checkpoints
sess_events        -- Session events

-- Approval domain
appr_records       -- Approval decisions
appr_rules         -- Custom approval rules

-- Sync domain
sync_outbox        -- Pending uploads
sync_inbox         -- Pending downloads
sync_conflicts     -- Conflict records

-- System domain
sys_config         -- Key-value settings
sys_locks          -- Coordination locks
sys_health         -- Health status
```

### Schema Example

```sql
-- conv_chats table
CREATE TABLE conv_chats (
    id TEXT PRIMARY KEY,
    title TEXT NOT NULL,
    tags TEXT,  -- JSON array
    worktree_id TEXT,
    is_deleted INTEGER DEFAULT 0,
    deleted_at TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    
    -- Sync metadata
    sync_status TEXT DEFAULT 'pending',
    sync_at TEXT,
    remote_id TEXT,
    version INTEGER DEFAULT 1
);

-- Indexes
CREATE INDEX idx_conv_chats_worktree ON conv_chats(worktree_id);
CREATE INDEX idx_conv_chats_sync_status ON conv_chats(sync_status);
```

### Migration File Example

```sql
-- migrations/003_add_approval_records.sql
-- 
-- Purpose: Add approval record storage
-- Dependencies: 001_initial_schema
-- Author: acode-team
-- Date: 2024-01-15

-- SQLite and PostgreSQL compatible

CREATE TABLE appr_records (
    id TEXT PRIMARY KEY,
    session_id TEXT NOT NULL,
    operation_category TEXT NOT NULL,
    operation_details TEXT,  -- JSON
    decision TEXT NOT NULL,
    matched_rule TEXT,
    user_reason TEXT,
    created_at TEXT NOT NULL,
    
    -- Sync metadata
    sync_status TEXT DEFAULT 'pending',
    sync_at TEXT,
    
    CONSTRAINT fk_session 
        FOREIGN KEY (session_id) 
        REFERENCES sess_sessions(id)
        ON DELETE CASCADE
);

CREATE INDEX idx_appr_records_session 
    ON appr_records(session_id);
CREATE INDEX idx_appr_records_created 
    ON appr_records(created_at);
```

```sql
-- migrations/003_add_approval_records_down.sql
-- 
-- Rollback: Remove approval record storage

DROP INDEX IF EXISTS idx_appr_records_created;
DROP INDEX IF EXISTS idx_appr_records_session;
DROP TABLE IF EXISTS appr_records;
```

### Version Table

```sql
-- __migrations table (auto-created)
CREATE TABLE __migrations (
    version TEXT PRIMARY KEY,
    applied_at TEXT NOT NULL,
    checksum TEXT NOT NULL,
    rollback_checksum TEXT
);
```

### Viewing Schema

```bash
# Show all tables
$ acode db schema --tables
conv_chats
conv_runs
conv_messages
sess_sessions
...

# Show table details
$ acode db schema --table conv_chats
Table: conv_chats
────────────────────────────────────
Column           Type     Nullable  Default
id               TEXT     NO        -
title            TEXT     NO        -
tags             TEXT     YES       -
...

Indexes:
  idx_conv_chats_worktree (worktree_id)
  idx_conv_chats_sync_status (sync_status)
```

### Migration Authoring

```bash
# Create new migration
$ acode db migration create add_feature_flags

Created:
  migrations/006_add_feature_flags.sql
  migrations/006_add_feature_flags_down.sql

Edit these files, then run:
  acode db migrate
```

---

## Acceptance Criteria

### Naming

- [ ] AC-001: Tables use snake_case
- [ ] AC-002: Tables have prefixes
- [ ] AC-003: Columns use snake_case
- [ ] AC-004: IDs named correctly
- [ ] AC-005: Foreign keys named correctly

### Data Types

- [ ] AC-006: IDs are TEXT
- [ ] AC-007: Timestamps are TEXT
- [ ] AC-008: Booleans are INTEGER
- [ ] AC-009: JSON is TEXT/JSONB

### Migrations

- [ ] AC-010: Versioned naming
- [ ] AC-011: Up scripts exist
- [ ] AC-012: Down scripts exist
- [ ] AC-013: Comments present
- [ ] AC-014: Idempotent

### Version Table

- [ ] AC-015: __migrations exists
- [ ] AC-016: Checksum stored
- [ ] AC-017: Applied_at stored

### Indexes

- [ ] AC-018: PKs indexed
- [ ] AC-019: FKs indexed
- [ ] AC-020: Query patterns indexed

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Database/Layout/
├── NamingConventionTests.cs
│   ├── Should_Use_Snake_Case()
│   ├── Should_Have_Prefix()
│   └── Should_Follow_Column_Rules()
│
├── DataTypeTests.cs
│   ├── Should_Use_Text_For_Ids()
│   └── Should_Use_Text_For_Timestamps()
│
└── MigrationStructureTests.cs
    ├── Should_Have_Version()
    ├── Should_Have_Down_Script()
    └── Should_Be_Idempotent()
```

### Integration Tests

```
Tests/Integration/Database/Layout/
├── SchemaValidationTests.cs
│   ├── Should_Create_All_Tables()
│   └── Should_Create_All_Indexes()
│
└── MigrationOrderTests.cs
    └── Should_Respect_Dependencies()
```

### E2E Tests

```
Tests/E2E/Database/
├── LayoutE2ETests.cs
│   └── Should_Apply_Full_Schema()
```

---

## User Verification Steps

### Scenario 1: Table Naming

1. Apply all migrations
2. List tables
3. Verify: All use prefixes

### Scenario 2: Column Types

1. Check conv_chats schema
2. Verify: id is TEXT
3. Verify: created_at is TEXT

### Scenario 3: Indexes

1. Check index list
2. Verify: Foreign keys indexed

### Scenario 4: Migration Files

1. List migration files
2. Verify: Versioned naming
3. Verify: Down scripts exist

---

## Implementation Prompt

### File Structure

```
migrations/
├── 001_initial_schema.sql
├── 001_initial_schema_down.sql
├── 002_add_conversations.sql
├── 002_add_conversations_down.sql
├── 003_add_sessions.sql
├── 003_add_sessions_down.sql
├── 004_add_approvals.sql
├── 004_add_approvals_down.sql
├── 005_add_sync.sql
└── 005_add_sync_down.sql

docs/
└── database/
    ├── schema.md
    └── conventions.md
```

### 001_initial_schema.sql

```sql
-- migrations/001_initial_schema.sql
--
-- Purpose: Create base schema and version tracking
-- Dependencies: None
-- Date: 2024-01-01

-- Version tracking table
CREATE TABLE IF NOT EXISTS __migrations (
    version TEXT PRIMARY KEY,
    applied_at TEXT NOT NULL,
    checksum TEXT NOT NULL,
    rollback_checksum TEXT
);

-- System config table
CREATE TABLE IF NOT EXISTS sys_config (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

-- System locks table
CREATE TABLE IF NOT EXISTS sys_locks (
    name TEXT PRIMARY KEY,
    holder TEXT NOT NULL,
    acquired_at TEXT NOT NULL,
    expires_at TEXT
);
```

### Documentation Template

```markdown
# Table: conv_chats

## Purpose
Stores conversation threads (chats).

## Columns
| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| id | TEXT | NO | ULID primary key |
| title | TEXT | NO | Chat title |
| ... | ... | ... | ... |

## Indexes
| Name | Columns | Purpose |
|------|---------|---------|
| idx_conv_chats_worktree | worktree_id | Worktree lookup |

## Relationships
- One-to-many: conv_runs
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-DB-LAY-001 | Invalid table name |
| ACODE-DB-LAY-002 | Invalid column name |
| ACODE-DB-LAY-003 | Missing down script |
| ACODE-DB-LAY-004 | Invalid migration version |
| ACODE-DB-LAY-005 | Dependency violation |

### Implementation Checklist

1. [ ] Define naming conventions
2. [ ] Document data types
3. [ ] Create initial migration
4. [ ] Create conversation migrations
5. [ ] Create session migrations
6. [ ] Create approval migrations
7. [ ] Create sync migrations
8. [ ] Document all schemas
9. [ ] Validate conventions
10. [ ] Write tests

### Rollout Plan

1. **Phase 1:** Conventions document
2. **Phase 2:** Initial migration
3. **Phase 3:** Domain migrations
4. **Phase 4:** Schema documentation
5. **Phase 5:** Validation tests

---

**End of Task 050.a Specification**