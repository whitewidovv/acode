# Database Schema Conventions

## Table Naming

- Use snake_case: `conv_chats`, not `ConvChats` or `convChats`
- Prefix with domain: `conv_`, `sess_`, `appr_`, `sync_`, `sys_`, `__`
- Be descriptive: `conv_messages`, not `conv_msg`
- Maximum length: 63 characters (PostgreSQL limit)

### Domain Prefixes

| Prefix | Domain | Purpose |
|--------|--------|---------|
| `conv_` | Conversation | Chat conversations, messages, tool calls |
| `sess_` | Session | User work sessions and checkpoints |
| `appr_` | Approval | User approval/denial records and policies |
| `sync_` | Synchronization | Sync operations, outbox, conflicts |
| `sys_` | System | Configuration, locks, health checks |
| `__` | Internal | Migration tracking and system internals |

## Column Naming

- Use snake_case: `created_at`, not `createdAt`
- Primary key: always `id`
- Foreign keys: `{table}_id` (e.g., `chat_id` for reference to `conv_chats`)
- Timestamps: `{action}_at` (e.g., `created_at`, `updated_at`, `deleted_at`)
- Booleans: `is_{condition}` (e.g., `is_deleted`, `is_active`, `is_archived`)
- Maximum length: 63 characters (PostgreSQL limit)

### Standard Column Patterns

| Pattern | Data Type | Example | Purpose |
|---------|-----------|---------|---------|
| `id` | TEXT | `01ARZ3NDEKTSV4RRFFQ69G5FAV` | Primary key (ULID, 26 chars) |
| `{table}_id` | TEXT | `chat_id` | Foreign key reference |
| `created_at` | TEXT | `2024-01-15T14:30:00Z` | Creation timestamp |
| `updated_at` | TEXT | `2024-01-15T14:30:00Z` | Last update timestamp |
| `deleted_at` | TEXT | `2024-01-15T14:30:00Z` | Soft delete timestamp |
| `is_{condition}` | INTEGER | `0` or `1` | Boolean flag |
| `sync_status` | TEXT | `pending`, `synced`, `failed` | Sync state enum |
| `sync_at` | TEXT | `2024-01-15T14:30:00Z` | Last sync timestamp |
| `remote_id` | TEXT | External system identifier | Remote entity ID |
| `version` | INTEGER | `1`, `2`, `3` | Optimistic concurrency version |

## Data Types

### SQLite/PostgreSQL Compatible Types

| Purpose | SQLite Type | PostgreSQL Type | Format/Notes |
|---------|-------------|-----------------|--------------|
| IDs | TEXT | TEXT | ULID format (26 characters) |
| Timestamps | TEXT | TEXT | ISO 8601: `YYYY-MM-DDTHH:MM:SSZ` |
| Booleans | INTEGER | INTEGER | 0 (false) or 1 (true) |
| JSON | TEXT | JSONB | Serialized JSON objects/arrays |
| Enums | TEXT | TEXT | String values (e.g., `pending`, `active`) |
| Numbers | INTEGER | INTEGER | Whole numbers |
| Decimals | REAL | NUMERIC | Floating-point numbers |
| Text | TEXT | TEXT | Variable-length strings |

### ULID Format

- **Length**: 26 characters (Base32 encoded)
- **Sortable**: Lexicographically sortable by timestamp
- **Example**: `01ARZ3NDEKTSV4RRFFQ69G5FAV`
- **Advantages**: Timestamp-ordered, collision-resistant, URL-safe

### Timestamp Format

- **Format**: ISO 8601 with UTC timezone
- **Pattern**: `YYYY-MM-DDTHH:MM:SSZ`
- **Example**: `2024-01-15T14:30:00Z`
- **Default**: Use SQLite's `strftime('%Y-%m-%dT%H:%M:%SZ', 'now')` for current timestamp

## Indexes

### Naming Patterns

| Type | Pattern | Example |
|------|---------|---------|
| Standard Index | `idx_{table}_{columns}` | `idx_conv_chats_worktree` |
| Unique Index | `ux_{table}_{columns}` | `ux_sys_config_key` |
| Foreign Key Index | `fk_{table}_{ref}` | `fk_conv_runs_chat` |

### Indexing Guidelines

- **Always index**: Foreign keys, frequently queried columns
- **Index cardinality**: High cardinality columns first in composite indexes
- **Limit**: 5 indexes per table (soft limit) to avoid write overhead
- **Covering indexes**: Consider including commonly selected columns
- **Partial indexes**: Use `WHERE` clause for conditional indexes (e.g., `WHERE is_deleted = 0`)

### Common Index Patterns

```sql
-- Foreign key indexes (always create these)
CREATE INDEX idx_conv_runs_chat ON conv_runs(chat_id);

-- Composite indexes for filtering + sorting
CREATE INDEX idx_conv_chats_sync ON conv_chats(sync_status, sync_at);

-- Descending indexes for recent-first queries
CREATE INDEX idx_conv_chats_created ON conv_chats(created_at DESC);

-- Partial indexes for active records only
CREATE INDEX idx_sess_sessions_active ON sess_sessions(status) WHERE is_deleted = 0;
```

## Foreign Keys

### Naming and Definitions

- **Column naming**: `{referenced_table}_id` (without domain prefix)
  - Reference to `conv_chats` → column named `chat_id`
  - Reference to `sess_sessions` → column named `session_id`

- **Constraint naming**: `fk_{table}_{referenced_table}`
  - Foreign key from `conv_runs` to `conv_chats` → `fk_conv_runs_chat`

- **Delete behavior**: Use `ON DELETE CASCADE` for parent-child relationships, `ON DELETE SET NULL` for optional references

```sql
CONSTRAINT fk_conv_runs_chat FOREIGN KEY (chat_id)
    REFERENCES conv_chats(id) ON DELETE CASCADE
```

## Migrations

### File Naming

- **Pattern**: `NNN_description.sql` where NNN is zero-padded version number
- **Valid**: `001_initial_schema.sql`, `042_add_worktrees.sql`
- **Invalid**: `1_initial.sql` (not zero-padded), `initial-schema.sql` (no version)

### Rollback Scripts

- **Pattern**: `NNN_description_down.sql`
- **Required**: Every migration MUST have a matching rollback script
- **Order**: Drop objects in reverse dependency order (indexes first, then tables)

```sql
-- migrations/002_add_conversations_down.sql
DROP INDEX IF EXISTS idx_conv_tool_calls_status;
DROP INDEX IF EXISTS idx_conv_tool_calls_message;
-- ... more indexes
DROP TABLE IF EXISTS conv_tool_calls;
DROP TABLE IF EXISTS conv_messages;
DROP TABLE IF EXISTS conv_runs;
DROP TABLE IF EXISTS conv_chats;
```

### Migration File Structure

```sql
-- migrations/NNN_description.sql
--
-- Purpose: <Brief description of what this migration does>
-- Dependencies: <Migration versions this depends on, or "None">
-- Author: <Author or team name>
-- Date: <YYYY-MM-DD>

-- ═══════════════════════════════════════════════════════════════════════
-- SECTION NAME (e.g., CONVERSATION DOMAIN TABLES)
-- ═══════════════════════════════════════════════════════════════════════

CREATE TABLE IF NOT EXISTS table_name (
    id TEXT PRIMARY KEY,
    -- ...
);

CREATE INDEX IF NOT EXISTS idx_table_name_column ON table_name(column);
```

### Idempotency Requirements

- **Always use**: `IF NOT EXISTS` for CREATE statements
- **Always use**: `IF EXISTS` for DROP statements
- **Why**: Migrations must be safe to run multiple times without errors
- **Testing**: Run each migration twice to verify idempotency

```sql
-- Good (idempotent)
CREATE TABLE IF NOT EXISTS conv_chats (id TEXT PRIMARY KEY);
CREATE INDEX IF NOT EXISTS idx_conv_chats_created ON conv_chats(created_at);

-- Bad (not idempotent - will fail on second run)
CREATE TABLE conv_chats (id TEXT PRIMARY KEY);
CREATE INDEX idx_conv_chats_created ON conv_chats(created_at);
```

### Migration Sequence

- **Sequential versions**: No gaps allowed (001, 002, 003...)
- **Dependencies**: Later migrations can depend on earlier ones
- **Order matters**: Foreign keys require referenced tables to exist first
- **Validation**: Use `MigrationFileValidator.ValidateMigrationSequence()` to detect gaps

## Sync Metadata Columns

Tables that sync to external systems should include these standard columns:

```sql
sync_status TEXT NOT NULL DEFAULT 'pending',  -- 'pending', 'synced', 'failed'
sync_at TEXT,                                   -- Last sync attempt timestamp
remote_id TEXT,                                 -- External system identifier
version INTEGER NOT NULL DEFAULT 1              -- Optimistic concurrency version
```

## Audit Columns

All domain tables (except internal `__` tables) should include:

```sql
created_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
updated_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now'))
```

## Soft Delete Pattern

For tables requiring soft delete capability:

```sql
is_deleted INTEGER NOT NULL DEFAULT 0,  -- 0 = active, 1 = deleted
deleted_at TEXT                          -- Timestamp of deletion (NULL if active)
```

Queries should filter out deleted records by default:

```sql
SELECT * FROM conv_chats WHERE is_deleted = 0;
```

## Validation

Use the provided validators to ensure schema compliance:

```csharp
// Naming conventions
var namingValidator = new NamingConventionValidator();
namingValidator.ValidateTableName("conv_chats");  // Returns ValidationResult
namingValidator.ValidateColumnName("created_at");

// Data types
var typeValidator = new DataTypeValidator();
typeValidator.ValidateIdColumn(column);
typeValidator.ValidateBooleanColumn(column);

// Migration files
var migrationValidator = new MigrationFileValidator();
migrationValidator.ValidateFileName("001_initial_schema.sql");
migrationValidator.ValidateContent(sqlContent);
```

## Error Codes Reference

| Code | Description |
|------|-------------|
| ACODE-DB-LAY-001 | Invalid table name (wrong case or missing prefix) |
| ACODE-DB-LAY-002 | Invalid column name (wrong case or pattern) |
| ACODE-DB-LAY-003 | Missing rollback script (_down.sql) |
| ACODE-DB-LAY-004 | Invalid migration version number |
| ACODE-DB-LAY-005 | Foreign key dependency violation |
| ACODE-DB-LAY-006 | Data type mismatch (wrong type for column category) |
| ACODE-DB-LAY-007 | Invalid index name pattern |
| ACODE-DB-LAY-008 | Migration sequence gap detected |
| ACODE-DB-LAY-009 | Forbidden SQL pattern detected |
| ACODE-DB-LAY-010 | Checksum validation failed |

## Best Practices

### DO

- Use domain prefixes consistently
- Always include created_at/updated_at on domain tables
- Index all foreign keys
- Use IF NOT EXISTS/IF EXISTS for idempotency
- Write descriptive migration header comments
- Test migrations on both SQLite and PostgreSQL
- Use soft deletes for user-facing data
- Include sync metadata for syncable entities
- Use ULID format for all IDs
- Store timestamps in ISO 8601 format (UTC)

### DON'T

- Don't use AUTO_INCREMENT or SERIAL IDs (use ULIDs)
- Don't use native DATETIME/TIMESTAMP types (use TEXT with ISO 8601)
- Don't use native BOOLEAN type (use INTEGER 0/1)
- Don't create tables without domain prefixes (except `__` internal tables)
- Don't use camelCase or PascalCase naming
- Don't skip migration version numbers
- Don't create migrations without rollback scripts
- Don't use destructive commands in migrations (DROP DATABASE, TRUNCATE, GRANT, REVOKE)
- Don't exceed 63 characters for table/column names (PostgreSQL limit)
- Don't create more than 5 indexes per table without good reason

## Examples

### Complete Table Definition

```sql
CREATE TABLE IF NOT EXISTS conv_chats (
    -- Primary key (ULID)
    id TEXT PRIMARY KEY,

    -- Domain-specific columns
    title TEXT NOT NULL,
    tags TEXT,  -- JSON array
    worktree_id TEXT,

    -- Soft delete
    is_archived INTEGER NOT NULL DEFAULT 0,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    deleted_at TEXT,

    -- Audit timestamps
    created_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    updated_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),

    -- Sync metadata
    sync_status TEXT NOT NULL DEFAULT 'pending',
    sync_at TEXT,
    remote_id TEXT,
    version INTEGER NOT NULL DEFAULT 1
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_conv_chats_worktree ON conv_chats(worktree_id);
CREATE INDEX IF NOT EXISTS idx_conv_chats_sync ON conv_chats(sync_status, sync_at);
CREATE INDEX IF NOT EXISTS idx_conv_chats_created ON conv_chats(created_at DESC);
```

### Foreign Key Relationship

```sql
CREATE TABLE IF NOT EXISTS conv_runs (
    id TEXT PRIMARY KEY,
    chat_id TEXT NOT NULL,
    -- ... other columns
    CONSTRAINT fk_conv_runs_chat FOREIGN KEY (chat_id)
        REFERENCES conv_chats(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_conv_runs_chat ON conv_runs(chat_id);
```

---

**Version**: 1.0
**Last Updated**: 2024-01-06
**Maintained By**: acode-team
