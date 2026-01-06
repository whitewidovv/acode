# Task 050.a: Workspace DB Layout + Migration Strategy

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 2 – Persistence + Reliability Core  
**Dependencies:** Task 050 (Database Foundation)  

---

## Description

### Business Value and ROI

Task 050.a defines the workspace database layout and migration strategy, establishing the foundational schema organization that all persistence features depend upon. This task represents a critical investment in data architecture that yields substantial returns across the entire application lifecycle.

**Quantified ROI Analysis:**

| Investment Area | Annual Value | Calculation Basis |
|----------------|--------------|-------------------|
| **Development Velocity** | $96,000 | Consistent naming conventions save 15 min/day × 4 developers × $50/hr × 240 days |
| **Debugging Efficiency** | $48,000 | Domain prefixes reduce table lookup time by 60%, saving 30 min/incident × 200 incidents/year × $80/hr |
| **Schema Evolution Safety** | $36,000 | Reversible migrations prevent 3 production rollback incidents/year × $12,000/incident |
| **Onboarding Time** | $16,000 | Clear conventions reduce new developer ramp-up by 2 weeks × 4 hires/year × $2,000/week |
| **Cross-Database Compatibility** | $24,000 | Dual-provider SQL eliminates 80% of migration issues × 50 migrations × 6 hours saved × $100/hr |
| **Total Annual ROI** | **$220,000** | |

The database layout serves as the single source of truth for data organization. Without consistent conventions, each feature implements schemas differently, leading to confusion, bugs, and maintenance burden. With a well-defined layout, every table follows predictable patterns, making the entire codebase more navigable and maintainable.

### Technical Approach

The workspace database layout employs a domain-driven schema organization combined with a linear migration versioning strategy:

```
┌─────────────────────────────────────────────────────────────────────┐
│                     DATABASE SCHEMA ORGANIZATION                     │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐     │
│  │  CONVERSATION   │  │    SESSION      │  │    APPROVAL     │     │
│  │    DOMAIN       │  │    DOMAIN       │  │    DOMAIN       │     │
│  │  (conv_*)       │  │  (sess_*)       │  │  (appr_*)       │     │
│  ├─────────────────┤  ├─────────────────┤  ├─────────────────┤     │
│  │ conv_chats      │  │ sess_sessions   │  │ appr_records    │     │
│  │ conv_runs       │  │ sess_checkpoints│  │ appr_rules      │     │
│  │ conv_messages   │  │ sess_events     │  │ appr_templates  │     │
│  │ conv_tool_calls │  │ sess_states     │  └─────────────────┘     │
│  │ conv_attachments│  └─────────────────┘                          │
│  └─────────────────┘                                               │
│                                                                      │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐     │
│  │      SYNC       │  │     SYSTEM      │  │    RESERVED     │     │
│  │    DOMAIN       │  │    DOMAIN       │  │    DOMAIN       │     │
│  │  (sync_*)       │  │  (sys_*)        │  │  (__*)          │     │
│  ├─────────────────┤  ├─────────────────┤  ├─────────────────┤     │
│  │ sync_outbox     │  │ sys_config      │  │ __migrations    │     │
│  │ sync_inbox      │  │ sys_locks       │  │ __schema_cache  │     │
│  │ sync_conflicts  │  │ sys_health      │  └─────────────────┘     │
│  │ sync_metadata   │  │ sys_audit       │                          │
│  └─────────────────┘  └─────────────────┘                          │
│                                                                      │
└─────────────────────────────────────────────────────────────────────┘
```

**Domain Prefix Strategy:**

Each domain is assigned a unique prefix that groups related tables together. This provides several benefits:

1. **Alphabetical Organization**: Tables sort by domain when listed alphabetically
2. **Clear Ownership**: Prefix immediately identifies which component owns the table
3. **Query Autocomplete**: IDE autocomplete narrows options based on domain prefix
4. **Access Control**: Database-level permissions can be granted per prefix pattern
5. **Documentation**: Domain documentation can reference all `prefix_*` tables

**Migration Versioning Strategy:**

```
┌────────────────────────────────────────────────────────────────────────┐
│                    MIGRATION LIFECYCLE                                  │
├────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│   DEVELOPMENT              STAGING                 PRODUCTION           │
│   ───────────              ───────                 ──────────          │
│                                                                         │
│   001_initial.sql ────────► 001_initial.sql ─────► 001_initial.sql    │
│          │                        │                      │             │
│          ▼                        ▼                      ▼             │
│   002_add_conv.sql ──────► 002_add_conv.sql ────► 002_add_conv.sql   │
│          │                        │                      │             │
│          ▼                        ▼                      ▼             │
│   003_add_sess.sql ──────► 003_add_sess.sql ────► 003_add_sess.sql   │
│          │                        │                      │             │
│          ▼                        ▼                      ▼             │
│   [NEW: 004_...]                 ...                    ...            │
│                                                                         │
│   __migrations table:                                                   │
│   ┌─────────┬────────────────────┬───────────────────────┐            │
│   │ version │ applied_at         │ checksum              │            │
│   ├─────────┼────────────────────┼───────────────────────┤            │
│   │ 001     │ 2024-01-01T00:00Z  │ a1b2c3d4e5f6...       │            │
│   │ 002     │ 2024-01-02T10:30Z  │ g7h8i9j0k1l2...       │            │
│   │ 003     │ 2024-01-05T14:15Z  │ m3n4o5p6q7r8...       │            │
│   └─────────┴────────────────────┴───────────────────────┘            │
│                                                                         │
└────────────────────────────────────────────────────────────────────────┘
```

The migration strategy enforces linear versioning without branching. Each migration receives a sequential three-digit number (001, 002, 003...). This simplicity ensures:

- No merge conflicts between developer branches
- Clear application order
- Easy identification of missing migrations
- Straightforward rollback targets

**Cross-Database Compatibility:**

The layout must support both SQLite (local development, offline operation) and PostgreSQL (team environments, cloud deployment). This requires careful consideration of SQL dialect differences:

```
┌────────────────────────────────────────────────────────────────────────┐
│                  DATA TYPE MAPPING                                      │
├─────────────────┬─────────────────┬────────────────────────────────────┤
│ Concept         │ SQLite          │ PostgreSQL                         │
├─────────────────┼─────────────────┼────────────────────────────────────┤
│ Primary Key     │ TEXT            │ VARCHAR(26) or TEXT                │
│ Timestamps      │ TEXT (ISO 8601) │ TIMESTAMPTZ                        │
│ Booleans        │ INTEGER (0/1)   │ BOOLEAN                            │
│ JSON Data       │ TEXT            │ JSONB                              │
│ Auto-increment  │ ROWID (implicit)│ SERIAL or IDENTITY                 │
│ Full-text       │ FTS5            │ tsvector + GIN index               │
└─────────────────┴─────────────────┴────────────────────────────────────┘
```

Migrations handle these differences using conditional SQL or separate dialect-specific files:

```sql
-- Option 1: Conditional within single file
CREATE TABLE conv_chats (
    id TEXT PRIMARY KEY,
    created_at TEXT NOT NULL,  -- ISO 8601 for both
    data TEXT  -- JSON stored as TEXT
);

-- Option 2: Dialect-specific files
-- 001_initial_sqlite.sql
-- 001_initial_postgres.sql
```

**Standard Column Patterns:**

Every table follows consistent column patterns to enable generic operations:

```sql
-- Primary Key (required on all tables)
id TEXT PRIMARY KEY  -- ULID format for sortability and uniqueness

-- Audit Columns (standard on domain tables)
created_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now'))
updated_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now'))
deleted_at TEXT  -- NULL = not deleted, timestamp = soft deleted

-- Sync Columns (on tables that sync to remote)
sync_status TEXT DEFAULT 'pending'  -- pending, synced, conflict
sync_at TEXT  -- Last sync timestamp
remote_id TEXT  -- ID on remote system
version INTEGER DEFAULT 1  -- Optimistic locking version
```

**Index Strategy:**

Indexes follow a consistent naming convention and are created based on query patterns:

```sql
-- Naming: idx_{table}_{column(s)}
CREATE INDEX idx_conv_chats_worktree ON conv_chats(worktree_id);
CREATE INDEX idx_conv_messages_chat_created ON conv_messages(chat_id, created_at);

-- Unique indexes for constraints
CREATE UNIQUE INDEX idx_conv_chats_remote ON conv_chats(remote_id) WHERE remote_id IS NOT NULL;

-- Composite indexes for common queries
CREATE INDEX idx_conv_messages_sync ON conv_messages(sync_status, sync_at);
```

### Integration Points

Task 050.a integrates with multiple system components:

**Task 050 (Database Foundation):**
- Connection factories use this layout to create tables
- Health checks validate schema integrity
- Backup procedures include all defined tables

**Task 050.c (Migration Runner):**
- Migration runner executes SQL files defined here
- Runner tracks versions in __migrations table
- Runner validates migration file structure

**Application Layer:**
- Repositories map to tables defined in layout
- Entity classes mirror column definitions
- Query builders use index-aware patterns

**Configuration System:**
- sys_config table stores persistent settings
- Configuration reader queries this table
- Settings cache refreshes from database

### Constraints and Limitations

**Constraint 1: No Branching Migrations**
The linear versioning strategy does not support branching. If two developers create migration 004, one must renumber. This trade-off prioritizes simplicity over flexibility.

**Constraint 2: SQLite Type Limitations**
SQLite's type affinity system means any value can be stored in any column. The layout relies on application-layer validation, not database-level type enforcement.

**Constraint 3: No Stored Procedures**
To maintain portability between SQLite and PostgreSQL, stored procedures and functions are not used. All logic resides in application code.

**Constraint 4: No Partitioning**
Table partitioning is not supported in this initial layout. Large tables are managed through archiving and cleanup procedures.

**Constraint 5: Foreign Key Performance**
SQLite foreign key checks add overhead. For bulk operations, foreign keys may be temporarily disabled with PRAGMA foreign_keys=OFF.

### Trade-offs and Alternatives

**Trade-off 1: TEXT vs Native Types**

*Decision:* Use TEXT for IDs and timestamps instead of native UUID/TIMESTAMP types.

*Rationale:* TEXT provides maximum portability between SQLite and PostgreSQL. ULIDs stored as TEXT maintain sortability while being human-readable in queries.

*Alternative Considered:* Native TIMESTAMP in PostgreSQL with TEXT in SQLite using separate migrations. Rejected due to complexity of maintaining two SQL dialects for every migration.

**Trade-off 2: Domain Prefixes vs Schemas**

*Decision:* Use table prefixes (conv_, sess_) instead of PostgreSQL schemas.

*Rationale:* SQLite does not support schemas. Prefixes work identically on both databases.

*Alternative Considered:* PostgreSQL schemas with schema-less SQLite. Rejected because it requires different query syntax per database.

**Trade-off 3: Soft Delete vs Hard Delete**

*Decision:* Implement soft delete (deleted_at timestamp) by default.

*Rationale:* Soft delete enables recovery, audit trails, and sync conflict resolution. Hard delete data cannot be recovered.

*Alternative Considered:* Hard delete with separate audit tables. Rejected due to complexity of maintaining sync between live and audit tables.

**Trade-off 4: ULID vs UUID**

*Decision:* Use ULID (Universally Unique Lexicographically Sortable Identifier) for primary keys.

*Rationale:* ULIDs are time-ordered, enabling efficient index scans for recent records. ULIDs are also shorter (26 chars vs 36 chars) and avoid the performance issues of UUID random distribution.

*Alternative Considered:* Auto-increment integers. Rejected because integers complicate sync between local and remote databases.

---

## Use Cases

### Use Case 1: DevBot Explores Database Schema

**Persona:** DevBot, an AI developer assistant helping debug a user's issue with message storage.

**Context:** A user reports that messages are not appearing in their conversation history. DevBot needs to understand the database schema to diagnose the issue.

**Before (Without Consistent Layout):**
```
DevBot thinks: "I need to find the messages table. Let me search..."
> SELECT name FROM sqlite_master WHERE type='table';
Results: user_messages, chats, MessageStore, msg_data, _messages, Messages

DevBot thinks: "Which one stores messages? Let me check each..."
> .schema user_messages
> .schema MessageStore
> .schema msg_data
... 15 minutes of exploration ...
```

**After (With Task 050.a Layout):**
```
DevBot thinks: "Messages are in the conversation domain, prefix conv_"
> SELECT name FROM sqlite_master WHERE type='table' AND name LIKE 'conv_%';
Results: conv_chats, conv_runs, conv_messages, conv_tool_calls

DevBot thinks: "conv_messages is clearly the messages table"
> .schema conv_messages
Column    Type   Description
id        TEXT   ULID primary key
chat_id   TEXT   FK to conv_chats
role      TEXT   'user', 'assistant', 'system'
content   TEXT   Message content
created_at TEXT  ISO 8601 timestamp

DevBot immediately identifies: "The chat_id foreign key tells me to check conv_chats for the conversation."
```

**Metrics:**
- Schema exploration time: 15 minutes → 2 minutes (87% reduction)
- Queries executed to understand structure: 12 → 3 (75% reduction)
- Confidence in diagnosis: Low → High

---

### Use Case 2: Jordan Creates a New Feature Migration

**Persona:** Jordan, a backend developer adding a feature flag system to the agent.

**Context:** Jordan needs to create migrations for a new feature_flags table with proper conventions.

**Before (Without Conventions):**
```
Jordan thinks: "How should I name this table? What about columns?"
Jordan creates: FeatureFlags table with columns:
  - ID (int, auto-increment)
  - Name (varchar)
  - Enabled (bit)
  - ModifiedDate (datetime)
  
PR Review: "This doesn't match our other tables. Please use snake_case. 
Why isn't this using ULID? Where's the sync metadata?"
Jordan rewrites migration...
PR Review: "The rollback script is missing"
Jordan adds rollback...
Total time: 4 hours across 3 PR iterations
```

**After (With Task 050.a Conventions):**
```
Jordan references conventions document:
- Table: sys_feature_flags (system domain prefix)
- ID: TEXT PRIMARY KEY (ULID)
- Columns: snake_case
- Required: created_at, updated_at
- Sync: Not needed for feature flags (system-only)

Jordan creates:
-- 007_add_feature_flags.sql
CREATE TABLE sys_feature_flags (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL UNIQUE,
    is_enabled INTEGER DEFAULT 0,
    description TEXT,
    created_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    updated_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now'))
);

-- 007_add_feature_flags_down.sql
DROP TABLE IF EXISTS sys_feature_flags;

PR Review: "LGTM! ✓"
Total time: 30 minutes, single iteration
```

**Metrics:**
- Time to create compliant migration: 4 hours → 30 minutes (88% reduction)
- PR review iterations: 3 → 1 (67% reduction)
- Developer frustration: High → Low

---

### Use Case 3: Alex Performs Production Rollback

**Persona:** Alex, a DevOps engineer responding to an incident caused by a bad migration.

**Context:** Migration 045 introduced a column that's causing query performance issues in production. Alex needs to roll back quickly.

**Before (Without Reversible Migrations):**
```
Alex: "We need to roll back migration 045"
Check migrations folder: Only 045_add_search_column.sql exists
Alex: "There's no rollback script. I'll have to write one manually..."

Alex writes: DROP COLUMN search_data...
SQLite error: "ALTER TABLE DROP COLUMN not supported in older SQLite"
Alex: "I need to recreate the entire table without the column"
... 45 minutes of manual SQL work under pressure ...
```

**After (With Task 050.a Rollback Strategy):**
```
Alex: "We need to roll back migration 045"
Check migrations folder: 
  045_add_search_column.sql
  045_add_search_column_down.sql ✓

Alex runs: agent db rollback --to 044
Output: 
  Rolling back 045_add_search_column...
  Executing 045_add_search_column_down.sql...
  Verifying checksum...
  Updating __migrations table...
  Rollback complete. Database at version 044.

Production restored in 2 minutes.
```

**Metrics:**
- Rollback time: 45+ minutes → 2 minutes (96% reduction)
- Manual SQL errors: Possible → None
- Incident duration: Extended → Minimal
- Post-incident review findings: "Rollback procedure worked as designed"

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

## Assumptions

### Technical Assumptions

1. **Embedded Resources** - Migration SQL files can be embedded as assembly resources for deployment
2. **File-Based Migrations** - Alternatively, migrations can be loaded from .agent/migrations/ directory
3. **Linear Versioning** - Migrations follow sequential numeric versioning (001, 002, 003...)
4. **No Branching** - Migration history is linear; no branch/merge scenarios supported
5. **Forward-Only Default** - Rollback migrations are optional; forward migrations always required
6. **Idempotent Checks** - Migrations check existence before CREATE to allow safe re-runs
7. **Transaction Per Migration** - Each migration runs in its own transaction for atomicity

### Schema Assumptions

8. **Domain Prefixes** - Tables use prefixes: conv_ (conversations), sess_ (sessions), appr_ (approvals), sync_ (synchronization), sys_ (system)
9. **Primary Keys** - All tables use id column as primary key; UUID strings for portability
10. **Audit Columns** - Standard tables include created_at, updated_at, deleted_at columns
11. **Soft Deletes** - deleted_at enables soft delete; queries filter WHERE deleted_at IS NULL
12. **Index Naming** - Indexes named idx_{table}_{columns} for consistency
13. **Foreign Keys** - Constraints named fk_{table}_{referenced} with explicit ON DELETE actions

### Operational Assumptions

14. **Version Tracking** - sys_migrations table tracks applied migrations with timestamps
15. **Hash Verification** - Migration content hashes prevent tampering with applied migrations
16. **CLI Execution** - Migrations run via `agent db migrate` command or startup flag
17. **Rollback Explicit** - Rollbacks require explicit `agent db rollback` with version target
18. **No Data Migration** - This task covers schema only; data transformations are separate concern

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

- NFR-001: All tables MUST follow snake_case naming convention without exception
- NFR-002: All domain tables MUST use the assigned domain prefix (conv_, sess_, appr_, sync_, sys_)
- NFR-003: All migrations MUST be reversible with corresponding _down.sql scripts
- NFR-004: All SQL MUST work on both SQLite 3.35+ and PostgreSQL 13+ without modification
- NFR-005: All timestamp columns MUST store ISO 8601 format strings for portability

### Maintainability

- NFR-006: Migration files MUST include header comments explaining purpose and dependencies
- NFR-007: Each table MUST have accompanying documentation in schema.md
- NFR-008: Index names MUST follow idx_{table}_{columns} pattern for discoverability
- NFR-009: Foreign key constraints MUST be named fk_{table}_{referenced} explicitly
- NFR-010: All column purposes MUST be documented in migration comments

### Performance

- NFR-011: Index count per table SHOULD NOT exceed 5 to limit write overhead
- NFR-012: Composite indexes MUST order columns by selectivity (most selective first)
- NFR-013: Covering indexes SHOULD be used for frequent query patterns
- NFR-014: TEXT columns SHOULD use appropriate length limits via CHECK constraints
- NFR-015: ULID primary keys MUST be used instead of UUIDs for better index locality

### Reliability

- NFR-016: Every migration MUST run within a transaction for atomicity
- NFR-017: Migration checksums MUST be SHA-256 hashes stored in __migrations
- NFR-018: Foreign key dependencies MUST be respected in migration ordering
- NFR-019: Rollback scripts MUST be tested before marking migration as complete
- NFR-020: Migration validation MUST detect circular dependencies at deploy time

### Security

- NFR-021: __migrations table MUST be protected from direct modification
- NFR-022: Soft delete MUST be used to preserve audit trails
- NFR-023: sys_config table MUST NOT store sensitive values in plaintext
- NFR-024: Database permissions MUST follow principle of least privilege
- NFR-025: Migration SQL MUST be validated for injection patterns before execution

### Scalability

- NFR-026: Schema MUST support tables up to 10 million rows without redesign
- NFR-027: Archive strategy MUST be defined for tables exceeding retention limits
- NFR-028: Sync metadata columns MUST enable efficient incremental sync queries
- NFR-029: Index strategy MUST consider read/write ratio of each table
- NFR-030: Version column MUST enable optimistic locking for concurrent updates

---

## Security Considerations

### Threat 1: Migration SQL Injection

**Risk:** Maliciously crafted migration files could execute unintended SQL commands, dropping tables, exfiltrating data, or escalating privileges.

**Attack Scenario:** An attacker gains commit access to the repository and modifies a migration file to include: `DROP TABLE conv_messages; --` or `INSERT INTO sys_config SELECT * FROM (SELECT load_extension('/tmp/malware.so'))`.

**Mitigation Code:**

```csharp
// Infrastructure/Migrations/MigrationSqlValidator.cs
namespace Acode.Infrastructure.Migrations;

public sealed class MigrationSqlValidator
{
    private static readonly string[] ForbiddenPatterns = new[]
    {
        @"\bDROP\s+DATABASE\b",           // Database deletion
        @"\bTRUNCATE\s+TABLE\b",           // Mass data deletion
        @"\bGRANT\b",                       // Permission escalation
        @"\bREVOKE\b",                      // Permission modification
        @"\bCREATE\s+USER\b",              // User creation
        @"\bALTER\s+USER\b",               // User modification
        @"\bload_extension\b",             // SQLite extension loading
        @"\bCOPY\s+.*\s+TO\b",            // PostgreSQL file export
        @"\bCOPY\s+.*\s+FROM\b",          // PostgreSQL file import
        @"\bpg_read_file\b",              // PostgreSQL file read
        @"\bpg_write_file\b",             // PostgreSQL file write
        @";\s*--",                          // Comment-based injection
    };
    
    private static readonly string[] AllowedStatementPrefixes = new[]
    {
        "CREATE TABLE",
        "CREATE INDEX",
        "CREATE UNIQUE INDEX",
        "ALTER TABLE",
        "DROP TABLE",
        "DROP INDEX",
        "INSERT INTO __migrations",
        "INSERT INTO sys_",
        "UPDATE __migrations",
        "DELETE FROM __migrations",
        "PRAGMA",  // SQLite specific
        "--",      // Comments
    };
    
    public ValidationResult ValidateMigration(string migrationContent, string fileName)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        
        // Check for forbidden patterns
        foreach (var pattern in ForbiddenPatterns)
        {
            if (Regex.IsMatch(migrationContent, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline))
            {
                errors.Add($"Forbidden SQL pattern detected: {pattern}");
            }
        }
        
        // Validate each statement
        var statements = SplitStatements(migrationContent);
        foreach (var statement in statements)
        {
            var trimmed = statement.Trim();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;
            
            var isAllowed = AllowedStatementPrefixes.Any(prefix => 
                trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            
            if (!isAllowed)
            {
                warnings.Add($"Unrecognized statement type: {trimmed.Substring(0, Math.Min(50, trimmed.Length))}...");
            }
        }
        
        // Check for required header comment
        if (!migrationContent.TrimStart().StartsWith("--"))
        {
            warnings.Add("Migration should start with a header comment explaining purpose");
        }
        
        return new ValidationResult(errors.Count == 0, errors, warnings);
    }
    
    private static IEnumerable<string> SplitStatements(string sql)
    {
        // Simple statement splitting (production would need proper SQL parsing)
        return sql.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s));
    }
}

public record ValidationResult(bool IsValid, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings);
```

---

### Threat 2: Schema Tampering via __migrations Manipulation

**Risk:** Direct modification of the __migrations table could allow attackers to mark malicious migrations as already applied, or reset migration state to force re-execution.

**Attack Scenario:** An attacker with database access executes: `DELETE FROM __migrations WHERE version = '005'` then modifies migration 005 to include malicious code. The migration runner, seeing version 005 as not applied, executes the modified migration.

**Mitigation Code:**

```csharp
// Infrastructure/Migrations/MigrationTableProtector.cs
namespace Acode.Infrastructure.Migrations;

public sealed class MigrationTableProtector
{
    private readonly ILogger<MigrationTableProtector> _logger;
    
    public async Task<bool> ValidateMigrationTableIntegrityAsync(
        IDbConnection connection,
        CancellationToken ct)
    {
        // Check row count hasn't decreased
        var currentCount = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM __migrations", ct);
        
        var expectedCount = await GetExpectedMigrationCountAsync(connection, ct);
        
        if (currentCount < expectedCount)
        {
            _logger.LogCritical(
                "SECURITY ALERT: __migrations row count decreased from {Expected} to {Actual}. Possible tampering!",
                expectedCount, currentCount);
            return false;
        }
        
        // Verify checksums of all applied migrations
        var appliedMigrations = await connection.QueryAsync<AppliedMigration>(
            "SELECT version, checksum FROM __migrations ORDER BY version", ct);
        
        foreach (var applied in appliedMigrations)
        {
            var embeddedContent = await GetEmbeddedMigrationContentAsync(applied.Version);
            if (embeddedContent == null)
            {
                _logger.LogWarning("Migration {Version} recorded but file not found", applied.Version);
                continue;
            }
            
            var computedChecksum = ComputeSha256(embeddedContent);
            if (computedChecksum != applied.Checksum)
            {
                _logger.LogCritical(
                    "SECURITY ALERT: Migration {Version} checksum mismatch! Stored: {Stored}, Computed: {Computed}",
                    applied.Version, applied.Checksum, computedChecksum);
                return false;
            }
        }
        
        return true;
    }
    
    public async Task SetupProtectedTriggersAsync(IDbConnection connection, CancellationToken ct)
    {
        // PostgreSQL: Create trigger to prevent direct deletes
        if (connection is NpgsqlConnection)
        {
            await connection.ExecuteAsync(@"
                CREATE OR REPLACE FUNCTION protect_migrations()
                RETURNS TRIGGER AS $$
                BEGIN
                    IF TG_OP = 'DELETE' THEN
                        RAISE EXCEPTION 'Direct deletion from __migrations is prohibited';
                    END IF;
                    IF TG_OP = 'UPDATE' AND OLD.checksum != NEW.checksum THEN
                        RAISE EXCEPTION 'Modification of migration checksum is prohibited';
                    END IF;
                    RETURN NEW;
                END;
                $$ LANGUAGE plpgsql;
                
                DROP TRIGGER IF EXISTS protect_migrations_trigger ON __migrations;
                CREATE TRIGGER protect_migrations_trigger
                    BEFORE DELETE OR UPDATE ON __migrations
                    FOR EACH ROW EXECUTE FUNCTION protect_migrations();
            ", ct);
        }
        // Note: SQLite doesn't support this level of trigger protection
    }
    
    private static string ComputeSha256(string content)
    {
        var normalized = content.Replace("\r\n", "\n");
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
```

---

### Threat 3: Data Exposure via Verbose Error Messages

**Risk:** SQL errors might expose sensitive schema information, table names, column names, or data values in error messages logged or returned to users.

**Attack Scenario:** An attacker triggers intentional SQL errors to learn database structure: `INSERT INTO ??? VALUES (...)` produces error "table conv_chats has no column named ???" revealing table and column names.

**Mitigation Code:**

```csharp
// Infrastructure/Database/SafeErrorHandler.cs
namespace Acode.Infrastructure.Database;

public sealed class SafeErrorHandler
{
    private readonly ILogger<SafeErrorHandler> _logger;
    
    // Internal details logged for debugging
    private static readonly Regex[] SensitivePatterns = new[]
    {
        new Regex(@"column\s+['`""]?(\w+)['`""]?", RegexOptions.IgnoreCase),
        new Regex(@"table\s+['`""]?(\w+)['`""]?", RegexOptions.IgnoreCase),
        new Regex(@"constraint\s+['`""]?(\w+)['`""]?", RegexOptions.IgnoreCase),
        new Regex(@"value\s+'([^']{0,20})'", RegexOptions.IgnoreCase),
    };
    
    public DatabaseException SanitizeAndWrap(Exception ex, string operationContext)
    {
        // Log full details internally
        var correlationId = Guid.NewGuid().ToString("N")[..8];
        _logger.LogError(ex, "Database error [{CorrelationId}] during {Operation}", correlationId, operationContext);
        
        // Return sanitized error to caller
        var safeMessage = ex switch
        {
            SqliteException sqe => MapSqliteError(sqe.SqliteErrorCode),
            NpgsqlException npg => MapPostgresError(npg.SqlState),
            _ => "A database error occurred"
        };
        
        return new DatabaseException(
            $"ACODE-DB-ERR",
            $"{safeMessage}. Reference: {correlationId}",
            ex);
    }
    
    private static string MapSqliteError(int errorCode) => errorCode switch
    {
        1 => "Invalid query syntax",
        5 => "Database is busy, please retry",
        6 => "Resource is locked",
        19 => "Constraint violation",
        _ => "Database operation failed"
    };
    
    private static string MapPostgresError(string sqlState) => sqlState switch
    {
        "23505" => "Duplicate value exists",
        "23503" => "Referenced record not found",
        "23502" => "Required value missing",
        "42P01" => "Invalid table reference",
        _ => "Database operation failed"
    };
}
```

---

### Threat 4: Sync Conflict Data Leakage

**Risk:** Sync conflict records in sync_conflicts table might expose sensitive data from multiple users if conflict resolution logic is not properly secured.

**Attack Scenario:** User A and User B both have access to same workspace. Sync conflict occurs. User A queries sync_conflicts table and sees User B's unapplied changes including sensitive content.

**Mitigation Code:**

```csharp
// Domain/Sync/SyncConflictAccessControl.cs
namespace Acode.Domain.Sync;

public sealed class SyncConflictAccessControl
{
    public async Task<IReadOnlyList<SyncConflict>> GetAccessibleConflictsAsync(
        IDbConnection connection,
        string currentUserId,
        string workspaceId,
        CancellationToken ct)
    {
        // Only return conflicts where current user is involved
        var conflicts = await connection.QueryAsync<SyncConflict>(@"
            SELECT c.*
            FROM sync_conflicts c
            WHERE c.workspace_id = @WorkspaceId
              AND (c.local_user_id = @UserId OR c.remote_user_id = @UserId)
              AND c.resolved_at IS NULL
            ORDER BY c.detected_at DESC",
            new { WorkspaceId = workspaceId, UserId = currentUserId }, ct);
        
        // Redact other user's content if necessary
        return conflicts.Select(c => RedactIfNeeded(c, currentUserId)).ToList();
    }
    
    private static SyncConflict RedactIfNeeded(SyncConflict conflict, string currentUserId)
    {
        if (conflict.LocalUserId != currentUserId)
        {
            // Redact local content we don't own
            conflict = conflict with { LocalContent = "[Content from another user]" };
        }
        if (conflict.RemoteUserId != currentUserId)
        {
            // Redact remote content we don't own
            conflict = conflict with { RemoteContent = "[Content from another user]" };
        }
        return conflict;
    }
}
```

---

### Threat 5: Schema Information Disclosure via sys_config

**Risk:** The sys_config table might store configuration values that reveal internal system details, API endpoints, feature flags, or other sensitive operational information.

**Attack Scenario:** A user with database query access runs `SELECT * FROM sys_config` and discovers internal API endpoints, feature flags, debugging settings, or other operational details that could assist in further attacks.

**Mitigation Code:**

```csharp
// Infrastructure/Configuration/SecureConfigStore.cs
namespace Acode.Infrastructure.Configuration;

public sealed class SecureConfigStore
{
    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "api_key", "api_secret", "token", "password", "connection_string",
        "encryption_key", "signing_key", "webhook_secret"
    };
    
    private static readonly HashSet<string> InternalKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "debug_mode", "internal_endpoint", "admin_bypass", "feature_"
    };
    
    public async Task SetConfigAsync(
        IDbConnection connection,
        string key,
        string value,
        CancellationToken ct)
    {
        // Never store sensitive values in plaintext
        if (SensitiveKeys.Any(s => key.Contains(s, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"Sensitive configuration '{key}' must use encrypted storage, not sys_config");
        }
        
        // Mark internal keys as non-queryable
        var isInternal = InternalKeys.Any(i => key.StartsWith(i, StringComparison.OrdinalIgnoreCase));
        
        await connection.ExecuteAsync(@"
            INSERT INTO sys_config (key, value, is_internal, updated_at)
            VALUES (@Key, @Value, @IsInternal, @UpdatedAt)
            ON CONFLICT (key) DO UPDATE SET
                value = @Value,
                is_internal = @IsInternal,
                updated_at = @UpdatedAt",
            new { Key = key, Value = value, IsInternal = isInternal ? 1 : 0, 
                  UpdatedAt = DateTime.UtcNow.ToString("O") }, ct);
    }
    
    public async Task<IReadOnlyDictionary<string, string>> GetPublicConfigAsync(
        IDbConnection connection,
        CancellationToken ct)
    {
        // Only return non-internal configuration
        var configs = await connection.QueryAsync<(string Key, string Value)>(
            "SELECT key, value FROM sys_config WHERE is_internal = 0", ct);
        
        return configs.ToDictionary(c => c.Key, c => c.Value);
    }
}
```

---

## User Manual Documentation

### Overview

The workspace database layout defines how Acode organizes persistent data for conversations, sessions, approvals, and synchronization. This manual provides comprehensive guidance for understanding the schema structure, navigating tables, authoring migrations, and performing common database operations.

Understanding the database layout is essential for:
- **Debugging issues** - Locating relevant tables and understanding relationships
- **Custom queries** - Writing efficient queries that leverage indexes
- **Data export** - Extracting data in consistent formats
- **Feature development** - Adding new tables that follow conventions
- **Troubleshooting** - Diagnosing sync, performance, and integrity issues

### Quick Reference Card

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    ACODE DATABASE QUICK REFERENCE                        │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  DOMAIN PREFIXES:                    DATA TYPES:                        │
│  ─────────────────                   ───────────                        │
│  conv_  Conversations                TEXT     IDs (ULID), Timestamps    │
│  sess_  Sessions                     INTEGER  Booleans (0/1)            │
│  appr_  Approvals                    TEXT     JSON (as string)          │
│  sync_  Synchronization                                                 │
│  sys_   System                       STANDARD COLUMNS:                  │
│  __     Reserved/Internal            ────────────────                   │
│                                      id          Primary key            │
│  NAMING CONVENTIONS:                 created_at  Row creation           │
│  ────────────────────                updated_at  Last modification      │
│  Tables:  snake_case with prefix     deleted_at  Soft delete marker     │
│  Columns: snake_case                 version     Optimistic locking     │
│  Indexes: idx_{table}_{columns}      sync_status Sync state            │
│  FKs:     fk_{table}_{referenced}    remote_id   Remote system ID       │
│                                                                          │
│  COMMON COMMANDS:                                                        │
│  ────────────────                                                        │
│  acode db status              Show migration status                     │
│  acode db migrate             Apply pending migrations                  │
│  acode db rollback --to N     Rollback to version N                     │
│  acode db schema --tables     List all tables                           │
│  acode db schema --table X    Show table X details                      │
│  acode db migration create X  Create new migration named X              │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### Table Organization by Domain

The database is organized into six domains, each with a unique prefix. This organization ensures tables sort alphabetically by domain and makes ownership immediately clear.

```
Domain Prefixes:
  conv_  = Conversation (chats, runs, messages, tool calls, attachments)
  sess_  = Session (sessions, checkpoints, events, state)
  appr_  = Approvals (records, rules, templates)
  sync_  = Sync (outbox, inbox, conflicts, metadata)
  sys_   = System (config, locks, health, audit)
  __     = Reserved (migrations, schema cache)
```

**Why Prefixes Matter:**
1. **Autocomplete** - Type `conv_` and IDE shows all conversation tables
2. **Grouping** - Tables appear together when sorted alphabetically
3. **Permissions** - Database roles can be granted per prefix pattern
4. **Documentation** - Easy to reference "all sync_* tables"

### Core Tables Reference

```sql
-- ═══════════════════════════════════════════════════════════════════════
-- CONVERSATION DOMAIN (conv_*)
-- Stores chat threads, execution runs, messages, and tool interactions
-- ═══════════════════════════════════════════════════════════════════════

conv_chats              -- Chat threads / conversation containers
                        -- Fields: id, title, tags, worktree_id, is_archived
                        -- FK: None (root entity)

conv_runs               -- Execution runs within a chat
                        -- Fields: id, chat_id, started_at, ended_at, status
                        -- FK: chat_id → conv_chats(id) ON DELETE CASCADE

conv_messages           -- Individual messages (user, assistant, system)
                        -- Fields: id, run_id, role, content, created_at
                        -- FK: run_id → conv_runs(id) ON DELETE CASCADE

conv_tool_calls         -- Tool invocations within messages
                        -- Fields: id, message_id, tool_name, arguments, result
                        -- FK: message_id → conv_messages(id) ON DELETE CASCADE

conv_attachments        -- File attachments to messages
                        -- Fields: id, message_id, filename, mime_type, content
                        -- FK: message_id → conv_messages(id) ON DELETE CASCADE

-- ═══════════════════════════════════════════════════════════════════════
-- SESSION DOMAIN (sess_*)
-- Stores active sessions, checkpoints, and session lifecycle events
-- ═══════════════════════════════════════════════════════════════════════

sess_sessions           -- Active agent sessions
                        -- Fields: id, worktree_id, started_at, status
                        -- FK: None (root entity)

sess_checkpoints        -- State checkpoints for recovery
                        -- Fields: id, session_id, state_json, created_at
                        -- FK: session_id → sess_sessions(id) ON DELETE CASCADE

sess_events             -- Session lifecycle events (start, pause, resume, end)
                        -- Fields: id, session_id, event_type, details, created_at
                        -- FK: session_id → sess_sessions(id) ON DELETE CASCADE

-- ═══════════════════════════════════════════════════════════════════════
-- APPROVAL DOMAIN (appr_*)
-- Stores approval decisions, rules, and templates
-- ═══════════════════════════════════════════════════════════════════════

appr_records            -- Individual approval decisions
                        -- Fields: id, session_id, category, decision, reason
                        -- FK: session_id → sess_sessions(id) ON DELETE CASCADE

appr_rules              -- User-defined approval rules
                        -- Fields: id, name, pattern, action, priority
                        -- FK: None (configuration entity)

appr_templates          -- Predefined approval templates
                        -- Fields: id, name, rules_json, description
                        -- FK: None (configuration entity)

-- ═══════════════════════════════════════════════════════════════════════
-- SYNC DOMAIN (sync_*)
-- Stores synchronization queues, conflicts, and metadata
-- ═══════════════════════════════════════════════════════════════════════

sync_outbox             -- Pending changes to upload to remote
                        -- Fields: id, entity_type, entity_id, operation, payload
                        -- FK: None (queue entity)

sync_inbox              -- Pending changes to apply from remote
                        -- Fields: id, entity_type, entity_id, operation, payload
                        -- FK: None (queue entity)

sync_conflicts          -- Detected sync conflicts requiring resolution
                        -- Fields: id, entity_type, local_version, remote_version
                        -- FK: None (conflict entity)

sync_metadata           -- Sync state per entity type
                        -- Fields: entity_type, last_sync_at, sync_cursor
                        -- FK: None (metadata entity)

-- ═══════════════════════════════════════════════════════════════════════
-- SYSTEM DOMAIN (sys_*)
-- Stores configuration, locks, health, and audit information
-- ═══════════════════════════════════════════════════════════════════════

sys_config              -- Key-value configuration settings
                        -- Fields: key, value, is_internal, updated_at
                        -- FK: None (configuration entity)

sys_locks               -- Distributed lock coordination
                        -- Fields: name, holder, acquired_at, expires_at
                        -- FK: None (coordination entity)

sys_health              -- Health check status records
                        -- Fields: id, check_name, status, details, checked_at
                        -- FK: None (monitoring entity)

sys_audit               -- Audit log for sensitive operations
                        -- Fields: id, user_id, action, details, created_at
                        -- FK: None (audit entity)
```

### Schema Exploration Commands

#### Listing All Tables

```bash
# Show all tables grouped by domain
$ acode db schema --tables

CONVERSATION DOMAIN (conv_*):
  conv_chats          5,423 rows    Chat threads
  conv_runs          12,891 rows    Execution runs
  conv_messages      89,234 rows    Individual messages
  conv_tool_calls    34,112 rows    Tool invocations
  conv_attachments    2,341 rows    File attachments

SESSION DOMAIN (sess_*):
  sess_sessions       1,234 rows    Active sessions
  sess_checkpoints    8,912 rows    State checkpoints
  sess_events        23,456 rows    Lifecycle events

APPROVAL DOMAIN (appr_*):
  appr_records        4,567 rows    Approval decisions
  appr_rules            45 rows    User rules
  appr_templates        12 rows    Templates

SYNC DOMAIN (sync_*):
  sync_outbox           23 rows    Pending uploads
  sync_inbox            17 rows    Pending downloads
  sync_conflicts         3 rows    Conflicts

SYSTEM DOMAIN (sys_*):
  sys_config            89 rows    Configuration
  sys_locks              2 rows    Active locks
  sys_health           156 rows    Health records
  sys_audit          1,234 rows    Audit log

RESERVED (__*):
  __migrations          12 rows    Applied migrations
```

#### Viewing Table Details

```bash
# Show detailed schema for a specific table
$ acode db schema --table conv_chats

Table: conv_chats
Description: Stores conversation threads (chats)
────────────────────────────────────────────────────────────────────────

COLUMNS:
  Column          Type       Nullable  Default                    Description
  ──────────────  ─────────  ────────  ─────────────────────────  ────────────────────
  id              TEXT       NO        -                          ULID primary key
  title           TEXT       NO        -                          Chat display title
  tags            TEXT       YES       NULL                       JSON array of tags
  worktree_id     TEXT       YES       NULL                       Associated worktree
  is_archived     INTEGER    NO        0                          Archive flag (0/1)
  is_deleted      INTEGER    NO        0                          Soft delete flag
  deleted_at      TEXT       YES       NULL                       Deletion timestamp
  created_at      TEXT       NO        current_timestamp          Creation time
  updated_at      TEXT       NO        current_timestamp          Last update time
  sync_status     TEXT       NO        'pending'                  Sync state
  sync_at         TEXT       YES       NULL                       Last sync time
  remote_id       TEXT       YES       NULL                       Remote system ID
  version         INTEGER    NO        1                          Optimistic lock

INDEXES:
  Name                          Columns                    Unique
  ────────────────────────────  ─────────────────────────  ──────
  PRIMARY KEY                   (id)                       YES
  idx_conv_chats_worktree       (worktree_id)              NO
  idx_conv_chats_sync           (sync_status, sync_at)     NO
  idx_conv_chats_created        (created_at DESC)          NO

FOREIGN KEYS:
  None (root entity)

REFERENCED BY:
  conv_runs.chat_id → conv_chats.id (ON DELETE CASCADE)

ROW COUNT: 5,423
AVG ROW SIZE: 847 bytes
TOTAL SIZE: 4.6 MB
```

#### Viewing Relationships

```bash
# Show entity relationship diagram for a domain
$ acode db schema --erd conv_

conv_chats
    │
    ├──< conv_runs (chat_id)
    │        │
    │        ├──< conv_messages (run_id)
    │        │        │
    │        │        ├──< conv_tool_calls (message_id)
    │        │        │
    │        │        └──< conv_attachments (message_id)

Legend: ──< = one-to-many relationship
```

### Migration Management

#### Checking Migration Status

```bash
# Show current migration status
$ acode db status

Database: .agent/data/workspace.db
Provider: SQLite 3.45.1
────────────────────────────────────────────────────────────────────────

APPLIED MIGRATIONS:
  Version  Applied At            Checksum (first 8)  Description
  ───────  ────────────────────  ─────────────────  ────────────────────
  001      2024-01-01T00:00:00Z  a1b2c3d4           Initial schema
  002      2024-01-02T10:30:00Z  e5f6g7h8           Add conversations
  003      2024-01-05T14:15:00Z  i9j0k1l2           Add sessions
  004      2024-01-10T09:00:00Z  m3n4o5p6           Add approvals
  005      2024-01-15T16:45:00Z  q7r8s9t0           Add sync

PENDING MIGRATIONS:
  006_add_audit_log.sql

Status: 1 pending migration
Run 'acode db migrate' to apply
```

#### Applying Migrations

```bash
# Apply all pending migrations
$ acode db migrate

Applying migrations...
  [1/1] 006_add_audit_log.sql
        ├─ Validating SQL... ✓
        ├─ Computing checksum... ✓
        ├─ Executing UP script... ✓
        ├─ Recording in __migrations... ✓
        └─ Duration: 23ms

All migrations applied successfully.
Database now at version 006.

# Apply with verbose output
$ acode db migrate --verbose

# Dry run (show what would happen)
$ acode db migrate --dry-run
```

#### Rolling Back Migrations

```bash
# Rollback to specific version
$ acode db rollback --to 004

Rolling back migrations...
  [1/2] 006_add_audit_log.sql
        ├─ Loading DOWN script... ✓
        ├─ Executing rollback... ✓
        ├─ Removing from __migrations... ✓
        └─ Duration: 12ms
  [2/2] 005_add_sync.sql
        ├─ Loading DOWN script... ✓
        ├─ Executing rollback... ✓
        ├─ Removing from __migrations... ✓
        └─ Duration: 45ms

Rollback complete. Database now at version 004.

# Rollback single migration
$ acode db rollback --steps 1
```

### Migration Authoring Guide

#### Creating a New Migration

```bash
# Create new migration with descriptive name
$ acode db migration create add_feature_flags

Created migration files:
  migrations/007_add_feature_flags.sql
  migrations/007_add_feature_flags_down.sql

Next steps:
  1. Edit the UP script: migrations/007_add_feature_flags.sql
  2. Edit the DOWN script: migrations/007_add_feature_flags_down.sql
  3. Run: acode db migrate
```

#### Migration File Template

```sql
-- migrations/007_add_feature_flags.sql
--
-- Purpose: Add feature flag system for controlled rollouts
-- Dependencies: 001_initial_schema (sys_config exists)
-- Author: your-name
-- Date: 2024-01-20
-- Ticket: ACODE-1234

-- ═══════════════════════════════════════════════════════════════════════
-- UP MIGRATION
-- ═══════════════════════════════════════════════════════════════════════

-- Create feature flags table with proper naming conventions
CREATE TABLE IF NOT EXISTS sys_feature_flags (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT,
    is_enabled INTEGER NOT NULL DEFAULT 0,
    rollout_percentage INTEGER DEFAULT 100,
    allowed_users TEXT,  -- JSON array of user IDs
    created_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    updated_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
    
    -- Unique constraint on name
    CONSTRAINT ux_feature_flags_name UNIQUE (name)
);

-- Index for enabled flags lookup
CREATE INDEX IF NOT EXISTS idx_sys_feature_flags_enabled 
    ON sys_feature_flags(is_enabled) 
    WHERE is_enabled = 1;

-- Index for name lookup
CREATE INDEX IF NOT EXISTS idx_sys_feature_flags_name 
    ON sys_feature_flags(name);
```

```sql
-- migrations/007_add_feature_flags_down.sql
--
-- Rollback: Remove feature flag system
-- NOTE: This will delete all feature flag data!

-- ═══════════════════════════════════════════════════════════════════════
-- DOWN MIGRATION (reverse order of UP)
-- ═══════════════════════════════════════════════════════════════════════

DROP INDEX IF EXISTS idx_sys_feature_flags_name;
DROP INDEX IF EXISTS idx_sys_feature_flags_enabled;
DROP TABLE IF EXISTS sys_feature_flags;
```

#### Migration Validation

```bash
# Validate migration before applying
$ acode db migration validate 007

Validating migration 007_add_feature_flags.sql...

NAMING CONVENTIONS:
  ✓ Table name 'sys_feature_flags' uses correct prefix
  ✓ Table name uses snake_case
  ✓ Column names use snake_case
  ✓ Index names follow idx_{table}_{columns} pattern

DATA TYPES:
  ✓ Primary key uses TEXT type
  ✓ Timestamps use TEXT type
  ✓ Booleans use INTEGER type

STRUCTURE:
  ✓ Has header comment with purpose
  ✓ Has header comment with dependencies
  ✓ Uses IF NOT EXISTS for CREATE
  ✓ DOWN script exists and is valid

SECURITY:
  ✓ No forbidden patterns detected
  ✓ No direct data manipulation

Validation passed. Ready to apply.
```

### Common Query Patterns

#### Finding Recent Conversations

```sql
-- Get 10 most recent non-deleted chats
SELECT id, title, created_at
FROM conv_chats
WHERE is_deleted = 0
ORDER BY created_at DESC
LIMIT 10;
```

#### Counting Messages by Role

```sql
-- Count messages grouped by role
SELECT role, COUNT(*) as count
FROM conv_messages
WHERE run_id = '01HX1234ABCD'
GROUP BY role;
```

#### Finding Sync Conflicts

```sql
-- Get unresolved sync conflicts
SELECT 
    c.id,
    c.entity_type,
    c.detected_at,
    c.local_version,
    c.remote_version
FROM sync_conflicts c
WHERE c.resolved_at IS NULL
ORDER BY c.detected_at DESC;
```

#### Checking Migration History

```sql
-- View all applied migrations
SELECT 
    version,
    applied_at,
    SUBSTR(checksum, 1, 8) as checksum_short
FROM __migrations
ORDER BY version;
```

### Version Table Reference

The `__migrations` table tracks all applied migrations and is managed automatically by the migration runner.

```sql
-- __migrations table structure
CREATE TABLE __migrations (
    version TEXT PRIMARY KEY,          -- Migration version (e.g., '007')
    applied_at TEXT NOT NULL,          -- ISO 8601 timestamp when applied
    checksum TEXT NOT NULL,            -- SHA-256 hash of migration content
    rollback_checksum TEXT             -- SHA-256 hash of DOWN script (if exists)
);

-- Example content
SELECT * FROM __migrations;
-- version | applied_at               | checksum                         | rollback_checksum
-- 001     | 2024-01-01T00:00:00Z    | a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6 | q7r8s9t0u1v2w3x4...
-- 002     | 2024-01-02T10:30:00Z    | e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0 | y5z6a7b8c9d0e1f2...
```

**Important:** Never manually modify the `__migrations` table. Use CLI commands to apply or rollback migrations.

---

## Acceptance Criteria

### Table Naming Conventions

- [ ] AC-001: All table names use snake_case (no camelCase or PascalCase)
- [ ] AC-002: All domain tables have appropriate prefix (conv_, sess_, appr_, sync_, sys_)
- [ ] AC-003: System tables use sys_ prefix
- [ ] AC-004: Reserved/internal tables use __ prefix (double underscore)
- [ ] AC-005: Join tables use {table1}_{table2} naming pattern
- [ ] AC-006: Table names are descriptive and indicate content (not abbreviations)
- [ ] AC-007: No table name exceeds 63 characters (PostgreSQL limit)
- [ ] AC-008: Table naming convention validator passes on all migrations

### Column Naming Conventions

- [ ] AC-009: All column names use snake_case
- [ ] AC-010: Primary key column is named "id" on all tables
- [ ] AC-011: Foreign key columns are named {referenced_table}_id
- [ ] AC-012: Timestamp columns follow {action}_at pattern (created_at, updated_at, deleted_at)
- [ ] AC-013: Boolean columns follow is_{condition} pattern (is_deleted, is_active)
- [ ] AC-014: No column name exceeds 63 characters
- [ ] AC-015: JSON columns are named descriptively (not just "data" or "json")

### Data Type Standards

- [ ] AC-016: Primary key IDs are TEXT type (ULID format)
- [ ] AC-017: Timestamps are TEXT type storing ISO 8601 format
- [ ] AC-018: Booleans are INTEGER type in SQLite (0/1)
- [ ] AC-019: JSON data is TEXT type in SQLite
- [ ] AC-020: JSON data is JSONB type in PostgreSQL
- [ ] AC-021: Enum values are TEXT type (not integer codes)
- [ ] AC-022: All TEXT columns have appropriate CHECK constraints for length

### Primary Key Standards

- [ ] AC-023: Every table has a primary key
- [ ] AC-024: Primary key is the "id" column
- [ ] AC-025: No composite primary keys are used
- [ ] AC-026: Primary keys use ULID format for sortability
- [ ] AC-027: Primary key values are generated by application (not database)

### Foreign Key Standards

- [ ] AC-028: All relationships are enforced with foreign key constraints
- [ ] AC-029: Every foreign key has ON DELETE action specified
- [ ] AC-030: ON DELETE CASCADE is used for dependent children
- [ ] AC-031: ON DELETE SET NULL is used for optional references
- [ ] AC-032: ON DELETE RESTRICT is used for protected references
- [ ] AC-033: Foreign key constraints are named fk_{table}_{referenced}
- [ ] AC-034: All foreign key columns have indexes

### Migration File Structure

- [ ] AC-035: Migration filename follows NNN_description.sql pattern
- [ ] AC-036: Version number is 3-digit zero-padded (001, 002, 003)
- [ ] AC-037: Description is snake_case
- [ ] AC-038: Every up migration has corresponding _down.sql file
- [ ] AC-039: Migration files are in migrations/ directory
- [ ] AC-040: Migration files are embedded as assembly resources

### Migration Content Standards

- [ ] AC-041: Every migration has header comment with purpose
- [ ] AC-042: Header includes dependencies on other migrations
- [ ] AC-043: Header includes author and date
- [ ] AC-044: DDL uses IF NOT EXISTS for CREATE statements
- [ ] AC-045: DDL uses IF EXISTS for DROP statements
- [ ] AC-046: Each migration is idempotent (safe to re-run)
- [ ] AC-047: Migration runs in single transaction
- [ ] AC-048: SQL is formatted consistently (indentation, capitalization)

### Cross-Database Compatibility

- [ ] AC-049: All SQL works on SQLite 3.35+ without modification
- [ ] AC-050: All SQL works on PostgreSQL 13+ without modification
- [ ] AC-051: SQLite-specific SQL is marked with -- @dialect: sqlite
- [ ] AC-052: PostgreSQL-specific SQL is marked with -- @dialect: postgres
- [ ] AC-053: No database-specific functions used in common SQL
- [ ] AC-054: Date/time functions use portable approach

### Version Tracking Table

- [ ] AC-055: __migrations table is created by initial migration
- [ ] AC-056: __migrations has version TEXT PRIMARY KEY column
- [ ] AC-057: __migrations has applied_at TEXT NOT NULL column
- [ ] AC-058: __migrations has checksum TEXT NOT NULL column
- [ ] AC-059: __migrations has rollback_checksum TEXT column
- [ ] AC-060: Checksum is SHA-256 of migration content
- [ ] AC-061: Version tracking prevents duplicate application

### Sync Metadata Columns

- [ ] AC-062: sync_status column exists on syncable tables
- [ ] AC-063: sync_status defaults to 'pending'
- [ ] AC-064: sync_at column exists on syncable tables
- [ ] AC-065: remote_id column exists on syncable tables
- [ ] AC-066: version INTEGER column exists for optimistic locking
- [ ] AC-067: version defaults to 1 and increments on update

### Audit Columns

- [ ] AC-068: created_at column exists on all domain tables
- [ ] AC-069: created_at has DEFAULT for current timestamp
- [ ] AC-070: updated_at column exists on all domain tables
- [ ] AC-071: deleted_at column exists for soft delete support
- [ ] AC-072: Soft delete uses NULL = not deleted convention

### System Tables

- [ ] AC-073: sys_config table exists for key-value settings
- [ ] AC-074: sys_locks table exists for coordination locks
- [ ] AC-075: sys_health table exists for status tracking
- [ ] AC-076: System tables have appropriate indexes

### Index Standards

- [ ] AC-077: All primary keys have implicit index
- [ ] AC-078: All foreign key columns are indexed
- [ ] AC-079: Index names follow idx_{table}_{columns} pattern
- [ ] AC-080: Unique constraint indexes follow ux_{table}_{columns} pattern
- [ ] AC-081: Composite indexes order columns by selectivity
- [ ] AC-082: No table has more than 5 indexes (default limit)
- [ ] AC-083: Query pattern indexes are documented with purpose

### Domain Tables - Conversation

- [ ] AC-084: conv_chats table exists with required columns
- [ ] AC-085: conv_runs table exists with FK to conv_chats
- [ ] AC-086: conv_messages table exists with FK to conv_runs
- [ ] AC-087: conv_tool_calls table exists with FK to conv_messages
- [ ] AC-088: All conversation tables have sync metadata columns

### Domain Tables - Session

- [ ] AC-089: sess_sessions table exists with required columns
- [ ] AC-090: sess_checkpoints table exists with FK to sess_sessions
- [ ] AC-091: sess_events table exists with FK to sess_sessions
- [ ] AC-092: All session tables have audit columns

### Domain Tables - Approval

- [ ] AC-093: appr_records table exists with required columns
- [ ] AC-094: appr_rules table exists with required columns
- [ ] AC-095: Approval tables reference session via FK

### Domain Tables - Sync

- [ ] AC-096: sync_outbox table exists for pending uploads
- [ ] AC-097: sync_inbox table exists for pending downloads
- [ ] AC-098: sync_conflicts table exists for conflict tracking

### Documentation

- [ ] AC-099: Schema documentation exists in docs/database/schema.md
- [ ] AC-100: Conventions document exists in docs/database/conventions.md
- [ ] AC-101: Every table has documented purpose
- [ ] AC-102: Every column has documented type and constraints
- [ ] AC-103: Relationships are documented with cardinality
- [ ] AC-104: Index purposes are documented

### Validation

- [ ] AC-105: Convention validator exists and runs on CI
- [ ] AC-106: All migrations pass convention validation
- [ ] AC-107: Migration dependency order is validated
- [ ] AC-108: No circular dependencies between tables

---

## Best Practices

### Schema Design

1. **Use consistent naming** - snake_case for tables/columns, domain prefixes for tables
2. **Document every table** - Add SQL comments explaining purpose and relationships
3. **Plan indexes early** - Design indexes based on expected query patterns, not afterthought
4. **Normalize appropriately** - Avoid over-normalization that complicates queries

### Migration Strategy

5. **One change per migration** - Small, focused migrations are easier to debug and rollback
6. **Write idempotent DDL** - Use IF NOT EXISTS, IF EXISTS for safe re-runs
7. **Include rollback SQL** - Every UP migration should have corresponding DOWN
8. **Test migrations on copy** - Apply migrations to database copy before production

### Version Control

9. **Never edit applied migrations** - Create new migration to fix issues in applied migrations
10. **Sequential numbering only** - Avoid gaps in migration numbers; use 001, 002, 003...
11. **Descriptive names** - Migration names should clearly indicate the change
12. **Commit migrations atomically** - Migration and code changes in same commit

---

## Troubleshooting

### Issue 1: Migration Order Conflict

**Symptoms:**
- Error: "Migration XXX out of order"
- Error: "Gap detected in migration sequence"
- Migration runner refuses to apply new migration
- Version number conflict between developers

**Causes:**
- Two developers independently created migrations with the same version number
- Migration file added to source after later migrations were already applied
- Manual manipulation of __migrations table
- Merge conflict resolved incorrectly in migrations folder
- Migration file renamed after being applied

**Solutions:**

1. **Identify the conflict:**
   ```powershell
   # List applied migrations
   acode db status --verbose
   
   # List migration files
   Get-ChildItem migrations/*.sql | Sort-Object Name
   ```

2. **Renumber the conflicting migration:**
   ```bash
   # Find next available number
   acode db migration next-version
   # Output: 007
   
   # Rename the file
   mv migrations/005_add_feature.sql migrations/007_add_feature.sql
   mv migrations/005_add_feature_down.sql migrations/007_add_feature_down.sql
   ```

3. **If local-only, reset database:**
   ```bash
   rm .agent/data/workspace.db
   acode db migrate
   ```

---

### Issue 2: Schema Drift Between Environments

**Symptoms:**
- Queries fail in production but work locally
- "Column not found" or "Table not found" errors
- Different row counts between environments
- Index missing in one environment

**Causes:**
- Migrations not applied consistently across environments
- Manual DDL changes made directly in production
- Different database provider behavior (SQLite vs PostgreSQL)
- Migration applied but not committed to source control
- Environment-specific migration skipped

**Solutions:**

1. **Compare migration versions:**
   ```bash
   # Local
   acode db status
   # Output: Applied migrations: 001, 002, 003, 004, 005
   
   # Production (via SSH or admin panel)
   acode db status
   # Output: Applied migrations: 001, 002, 003, 005  # Missing 004!
   ```

2. **Generate schema diff:**
   ```bash
   # Export local schema
   sqlite3 .agent/data/workspace.db ".schema" > local_schema.sql
   
   # Export production schema
   pg_dump --schema-only -h prod-host agentdb > prod_schema.sql
   
   # Diff
   diff local_schema.sql prod_schema.sql
   ```

3. **Create corrective migration:**
   ```sql
   -- migrations/006_fix_missing_column.sql
   -- Purpose: Add column missing from production
   -- This corrects drift from manual change
   
   ALTER TABLE conv_messages ADD COLUMN IF NOT EXISTS 
       metadata TEXT DEFAULT '{}';
   ```

---

### Issue 3: Migration Performance on Large Tables

**Symptoms:**
- ALTER TABLE takes minutes or hours
- Database appears frozen during migration
- Timeout errors during migration
- Other queries blocked during migration

**Causes:**
- PostgreSQL acquires exclusive table lock during ALTER
- Adding NOT NULL column requires full table scan
- Index creation on large table
- Table has millions of rows
- Insufficient disk space for table rewrite

**Solutions:**

1. **Use CONCURRENTLY for indexes (PostgreSQL):**
   ```sql
   -- WRONG - blocks table
   CREATE INDEX idx_messages_content ON conv_messages(content);
   
   -- RIGHT - allows concurrent access
   CREATE INDEX CONCURRENTLY idx_messages_content ON conv_messages(content);
   ```

2. **Add nullable column, then backfill:**
   ```sql
   -- Step 1: Add nullable column (instant)
   ALTER TABLE conv_messages ADD COLUMN category TEXT;
   
   -- Step 2: Backfill in batches (non-blocking)
   UPDATE conv_messages SET category = 'default' 
   WHERE id IN (SELECT id FROM conv_messages WHERE category IS NULL LIMIT 10000);
   
   -- Step 3: Add NOT NULL after data populated
   ALTER TABLE conv_messages ALTER COLUMN category SET NOT NULL;
   ```

3. **Increase migration timeout:**
   ```yaml
   # acode.yml
   database:
     migrations:
       timeoutSeconds: 1800  # 30 minutes for large migrations
   ```

---

### Issue 4: Foreign Key Constraint Violation During Migration

**Symptoms:**
- Error: "FOREIGN KEY constraint failed"
- Error: "insert or update violates foreign key constraint"
- Migration fails during INSERT or UPDATE
- Cannot add foreign key to existing table

**Causes:**
- Orphaned data in child table (parent record deleted)
- Migration order incorrect (child table created before parent)
- Data inserted without corresponding parent record
- Circular foreign key dependencies

**Solutions:**

1. **Find orphaned records:**
   ```sql
   -- Find messages referencing non-existent chats
   SELECT m.id, m.chat_id 
   FROM conv_messages m
   LEFT JOIN conv_chats c ON m.chat_id = c.id
   WHERE c.id IS NULL;
   ```

2. **Clean up orphans before adding constraint:**
   ```sql
   -- Delete orphaned records
   DELETE FROM conv_messages 
   WHERE chat_id NOT IN (SELECT id FROM conv_chats);
   
   -- Then add constraint
   ALTER TABLE conv_messages ADD CONSTRAINT fk_messages_chat
       FOREIGN KEY (chat_id) REFERENCES conv_chats(id);
   ```

3. **Temporarily disable constraints (SQLite):**
   ```sql
   PRAGMA foreign_keys = OFF;
   -- Run migration
   PRAGMA foreign_keys = ON;
   PRAGMA foreign_key_check;  -- Verify no violations
   ```

---

### Issue 5: Naming Convention Violation Detected

**Symptoms:**
- Convention validator rejects migration
- Error: "Table name 'UserMessages' does not follow snake_case"
- Error: "Missing domain prefix for table 'chats'"
- PR fails automated checks

**Causes:**
- Developer unfamiliar with conventions
- Copy-paste from external source with different conventions
- Auto-generated schema from ORM not conforming
- Legacy table not yet renamed

**Solutions:**

1. **Check convention rules:**
   ```bash
   # View conventions document
   cat docs/database/conventions.md
   ```

2. **Fix table naming:**
   ```sql
   -- WRONG
   CREATE TABLE UserMessages (...);
   CREATE TABLE chats (...);
   
   -- RIGHT
   CREATE TABLE conv_messages (...);
   CREATE TABLE conv_chats (...);
   ```

3. **Rename existing table:**
   ```sql
   -- Rename table (PostgreSQL)
   ALTER TABLE "UserMessages" RENAME TO conv_messages;
   
   -- Rename table (SQLite - requires recreation)
   ALTER TABLE chats RENAME TO conv_chats;
   ```

---

### Issue 6: Checksum Validation Failure

**Symptoms:**
- Error: "Migration checksum mismatch for version XXX"
- Error: "SECURITY ALERT: Migration file modified"
- Migration refuses to run
- Integrity check fails

**Causes:**
- Migration file was edited after being applied
- Line ending differences (CRLF vs LF) between platforms
- Encoding differences (UTF-8 BOM vs no BOM)
- Whitespace changes from IDE auto-formatting
- Merge conflict markers left in file

**Solutions:**

1. **Compare checksums:**
   ```powershell
   # Get stored checksum
   acode db status --verbose | Select-String "003"
   # Output: 003_add_sessions: a1b2c3d4...
   
   # Compute current file checksum
   $content = (Get-Content migrations/003_add_sessions.sql -Raw) -replace "`r`n", "`n"
   $hash = [System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($content))
   [Convert]::ToHexString($hash).ToLowerInvariant()
   ```

2. **Normalize line endings:**
   ```bash
   # Convert to LF
   sed -i 's/\r$//' migrations/003_add_sessions.sql
   
   # Or use git
   git config core.autocrlf false
   git add migrations/
   ```

3. **Force re-record (development only):**
   ```bash
   # WARNING: Only for development databases
   acode db migrate --force-checksum --version 003
   ```

---

### Issue 7: Down Script Missing or Incomplete

**Symptoms:**
- Rollback fails with "No rollback script found"
- Partial rollback leaves database in inconsistent state
- Error: "Cannot rollback migration XXX"
- Down script has syntax errors

**Causes:**
- Developer forgot to create _down.sql file
- Down script doesn't reverse all changes from up script
- Down script has different SQL than what's needed
- Complex migration cannot be cleanly reversed

**Solutions:**

1. **Create missing down script:**
   ```bash
   # Check what's missing
   ls migrations/*_down.sql
   
   # Create the missing file
   touch migrations/005_add_feature_down.sql
   ```

2. **Ensure down reverses up completely:**
   ```sql
   -- 005_add_feature.sql (UP)
   CREATE TABLE sys_feature_flags (...);
   CREATE INDEX idx_feature_flags_name ON sys_feature_flags(name);
   
   -- 005_add_feature_down.sql (DOWN) - reverse order!
   DROP INDEX IF EXISTS idx_feature_flags_name;
   DROP TABLE IF EXISTS sys_feature_flags;
   ```

3. **For complex migrations, document manual steps:**
   ```sql
   -- 005_migrate_data_down.sql
   -- MANUAL ROLLBACK REQUIRED
   -- This migration transforms data and cannot be automatically reversed.
   -- Steps:
   -- 1. Restore from backup: acode db restore --before 005
   -- 2. Or manually: DELETE FROM new_table; INSERT INTO old_table SELECT ...
   
   SELECT 'Manual rollback required - see comments' AS error;
   ```

---

### Issue 8: SQLite vs PostgreSQL Syntax Incompatibility

**Symptoms:**
- Migration works on SQLite but fails on PostgreSQL (or vice versa)
- Error: "syntax error at or near..."
- Error: "no such function..."
- Different behavior for same SQL

**Causes:**
- Using SQLite-specific syntax (AUTOINCREMENT, GLOB)
- Using PostgreSQL-specific syntax (SERIAL, RETURNING)
- Different date/time functions
- Different JSON handling
- Case sensitivity differences

**Solutions:**

1. **Use portable SQL subset:**
   ```sql
   -- WRONG - SQLite specific
   CREATE TABLE test (id INTEGER PRIMARY KEY AUTOINCREMENT);
   
   -- WRONG - PostgreSQL specific  
   CREATE TABLE test (id SERIAL PRIMARY KEY);
   
   -- RIGHT - works on both
   CREATE TABLE test (id TEXT PRIMARY KEY);  -- Use ULID
   ```

2. **Create dialect-specific migrations:**
   ```
   migrations/
     005_add_fulltext_sqlite.sql      # SQLite FTS5
     005_add_fulltext_postgres.sql    # PostgreSQL tsvector
   ```

3. **Use conditional execution:**
   ```sql
   -- Detected at runtime by migration runner
   -- @dialect: sqlite
   CREATE VIRTUAL TABLE conv_messages_fts USING fts5(content);
   
   -- @dialect: postgres
   ALTER TABLE conv_messages ADD COLUMN search_vector tsvector;
   CREATE INDEX idx_messages_search ON conv_messages USING GIN(search_vector);
   ```

---

## Testing Requirements

### Unit Tests

```csharp
// Tests/Acode.Infrastructure.Tests/Database/Layout/NamingConventionValidatorTests.cs
using Acode.Infrastructure.Database.Layout;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Database.Layout;

/// <summary>
/// Tests for database naming convention validation.
/// Verifies that table names, column names, indexes, and foreign keys
/// follow the established conventions.
/// </summary>
public sealed class NamingConventionValidatorTests
{
    private readonly NamingConventionValidator _sut = new();
    
    [Theory]
    [InlineData("conv_chats", true)]
    [InlineData("sess_sessions", true)]
    [InlineData("appr_records", true)]
    [InlineData("sync_outbox", true)]
    [InlineData("sys_config", true)]
    [InlineData("__migrations", true)]
    [InlineData("UserMessages", false)]         // PascalCase not allowed
    [InlineData("user_messages", false)]        // Missing domain prefix
    [InlineData("convChats", false)]            // camelCase not allowed
    [InlineData("CONV_CHATS", false)]           // UPPERCASE not allowed
    public void ValidateTableName_ShouldReturnExpectedResult(string tableName, bool expected)
    {
        // Act
        var result = _sut.ValidateTableName(tableName);
        
        // Assert
        result.IsValid.Should().Be(expected);
    }
    
    [Theory]
    [InlineData("id", true)]
    [InlineData("created_at", true)]
    [InlineData("updated_at", true)]
    [InlineData("chat_id", true)]
    [InlineData("is_deleted", true)]
    [InlineData("sync_status", true)]
    [InlineData("CreatedAt", false)]            // PascalCase
    [InlineData("chatId", false)]               // camelCase
    [InlineData("ID", false)]                   // UPPERCASE
    [InlineData("isDeleted", false)]            // camelCase
    public void ValidateColumnName_ShouldReturnExpectedResult(string columnName, bool expected)
    {
        // Act
        var result = _sut.ValidateColumnName(columnName);
        
        // Assert
        result.IsValid.Should().Be(expected);
    }
    
    [Theory]
    [InlineData("idx_conv_chats_worktree", true)]
    [InlineData("idx_conv_messages_chat_created", true)]
    [InlineData("ux_sys_config_key", true)]
    [InlineData("fk_conv_runs_chat", true)]
    [InlineData("PK_ConvChats", false)]         // PascalCase
    [InlineData("ix_chats", false)]             // Wrong prefix format
    [InlineData("conv_chats_idx", false)]       // Wrong order
    public void ValidateIndexName_ShouldReturnExpectedResult(string indexName, bool expected)
    {
        // Act
        var result = _sut.ValidateIndexName(indexName);
        
        // Assert
        result.IsValid.Should().Be(expected);
    }
    
    [Fact]
    public void ValidateTableName_WithInvalidPrefix_ShouldReturnError()
    {
        // Act
        var result = _sut.ValidateTableName("msg_content");
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("prefix"));
    }
    
    [Fact]
    public void ValidatePrimaryKeyColumn_ShouldRequireIdColumn()
    {
        // Arrange
        var tableSchema = new TableSchema("conv_chats", new[]
        {
            new ColumnSchema("chat_id", "TEXT", isPrimaryKey: true),
            new ColumnSchema("title", "TEXT", isPrimaryKey: false)
        });
        
        // Act
        var result = _sut.ValidatePrimaryKey(tableSchema);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("'id'"));
    }
    
    [Fact]
    public void ValidateForeignKeyColumn_ShouldFollowNamingPattern()
    {
        // Act
        var valid = _sut.ValidateForeignKeyColumn("chat_id", "conv_chats");
        var invalid = _sut.ValidateForeignKeyColumn("parentChat", "conv_chats");
        
        // Assert
        valid.IsValid.Should().BeTrue();
        invalid.IsValid.Should().BeFalse();
    }
    
    [Theory]
    [InlineData("created_at", true)]
    [InlineData("updated_at", true)]
    [InlineData("deleted_at", true)]
    [InlineData("sync_at", true)]
    [InlineData("applied_at", true)]
    [InlineData("createdDate", false)]          // Wrong pattern
    [InlineData("last_modified", false)]        // Should be modified_at
    public void ValidateTimestampColumn_ShouldFollowAtPattern(string columnName, bool expected)
    {
        // Act
        var result = _sut.ValidateTimestampColumn(columnName);
        
        // Assert
        result.IsValid.Should().Be(expected);
    }
    
    [Theory]
    [InlineData("is_deleted", true)]
    [InlineData("is_active", true)]
    [InlineData("is_enabled", true)]
    [InlineData("is_internal", true)]
    [InlineData("deleted", false)]              // Missing is_ prefix
    [InlineData("active", false)]               // Missing is_ prefix
    [InlineData("isActive", false)]             // camelCase
    public void ValidateBooleanColumn_ShouldFollowIsPattern(string columnName, bool expected)
    {
        // Act
        var result = _sut.ValidateBooleanColumn(columnName);
        
        // Assert
        result.IsValid.Should().Be(expected);
    }
}
```

```csharp
// Tests/Acode.Infrastructure.Tests/Database/Layout/DataTypeValidatorTests.cs
using Acode.Infrastructure.Database.Layout;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Database.Layout;

/// <summary>
/// Tests for database data type validation.
/// Verifies that columns use correct types for IDs, timestamps, booleans, etc.
/// </summary>
public sealed class DataTypeValidatorTests
{
    private readonly DataTypeValidator _sut = new();
    
    [Fact]
    public void ValidateIdColumn_ShouldRequireTextType()
    {
        // Arrange
        var textColumn = new ColumnSchema("id", "TEXT", isPrimaryKey: true);
        var intColumn = new ColumnSchema("id", "INTEGER", isPrimaryKey: true);
        
        // Act
        var textResult = _sut.ValidateIdColumn(textColumn);
        var intResult = _sut.ValidateIdColumn(intColumn);
        
        // Assert
        textResult.IsValid.Should().BeTrue();
        intResult.IsValid.Should().BeFalse();
        intResult.Errors.Should().Contain(e => e.Contains("TEXT"));
    }
    
    [Fact]
    public void ValidateTimestampColumn_ShouldRequireTextType()
    {
        // Arrange
        var textColumn = new ColumnSchema("created_at", "TEXT");
        var datetimeColumn = new ColumnSchema("created_at", "DATETIME");
        
        // Act
        var textResult = _sut.ValidateTimestampColumn(textColumn);
        var datetimeResult = _sut.ValidateTimestampColumn(datetimeColumn);
        
        // Assert
        textResult.IsValid.Should().BeTrue();
        datetimeResult.IsValid.Should().BeFalse();
        datetimeResult.Warnings.Should().Contain(w => w.Contains("ISO 8601"));
    }
    
    [Fact]
    public void ValidateBooleanColumn_ShouldRequireIntegerType()
    {
        // Arrange
        var intColumn = new ColumnSchema("is_deleted", "INTEGER");
        var boolColumn = new ColumnSchema("is_deleted", "BOOLEAN");
        
        // Act
        var intResult = _sut.ValidateBooleanColumn(intColumn);
        var boolResult = _sut.ValidateBooleanColumn(boolColumn);
        
        // Assert
        intResult.IsValid.Should().BeTrue();
        boolResult.IsValid.Should().BeFalse();
        boolResult.Errors.Should().Contain(e => e.Contains("INTEGER"));
    }
    
    [Fact]
    public void ValidateJsonColumn_ShouldRequireTextType()
    {
        // Arrange
        var textColumn = new ColumnSchema("metadata", "TEXT");
        var jsonColumn = new ColumnSchema("metadata", "JSON");
        
        // Act
        var textResult = _sut.ValidateJsonColumn(textColumn);
        var jsonResult = _sut.ValidateJsonColumn(jsonColumn);
        
        // Assert
        textResult.IsValid.Should().BeTrue();
        jsonResult.IsValid.Should().BeFalse();
    }
    
    [Fact]
    public void ValidateForeignKeyColumn_ShouldRequireTextType()
    {
        // Arrange
        var textFk = new ColumnSchema("chat_id", "TEXT");
        var intFk = new ColumnSchema("chat_id", "INTEGER");
        
        // Act
        var textResult = _sut.ValidateForeignKeyColumn(textFk);
        var intResult = _sut.ValidateForeignKeyColumn(intFk);
        
        // Assert
        textResult.IsValid.Should().BeTrue();
        intResult.IsValid.Should().BeFalse();
    }
    
    [Fact]
    public void ValidateEnumColumn_ShouldRequireTextType()
    {
        // Arrange
        var textColumn = new ColumnSchema("sync_status", "TEXT");
        var intColumn = new ColumnSchema("sync_status", "INTEGER");
        
        // Act
        var textResult = _sut.ValidateEnumColumn(textColumn);
        var intResult = _sut.ValidateEnumColumn(intColumn);
        
        // Assert
        textResult.IsValid.Should().BeTrue();
        intResult.Warnings.Should().Contain(w => w.Contains("TEXT for enum"));
    }
}
```

```csharp
// Tests/Acode.Infrastructure.Tests/Database/Layout/MigrationFileValidatorTests.cs
using System.IO;
using Acode.Infrastructure.Database.Layout;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Database.Layout;

/// <summary>
/// Tests for migration file structure and content validation.
/// </summary>
public sealed class MigrationFileValidatorTests
{
    private readonly MigrationFileValidator _sut = new();
    
    [Theory]
    [InlineData("001_initial_schema.sql", true)]
    [InlineData("002_add_conversations.sql", true)]
    [InlineData("099_final_cleanup.sql", true)]
    [InlineData("1_initial.sql", false)]                // Not zero-padded
    [InlineData("initial_schema.sql", false)]           // No version number
    [InlineData("001-initial-schema.sql", false)]       // Hyphens instead of underscore
    [InlineData("001_InitialSchema.sql", false)]        // PascalCase
    public void ValidateFileName_ShouldReturnExpectedResult(string fileName, bool expected)
    {
        // Act
        var result = _sut.ValidateFileName(fileName);
        
        // Assert
        result.IsValid.Should().Be(expected);
    }
    
    [Fact]
    public void ValidateDownScriptExists_ShouldRequireMatchingDownFile()
    {
        // Arrange
        var upFile = "migrations/005_add_sync.sql";
        var downFile = "migrations/005_add_sync_down.sql";
        
        // Act
        var withDown = _sut.ValidateDownScriptExists(upFile, downFileExists: true);
        var withoutDown = _sut.ValidateDownScriptExists(upFile, downFileExists: false);
        
        // Assert
        withDown.IsValid.Should().BeTrue();
        withoutDown.IsValid.Should().BeFalse();
        withoutDown.Errors.Should().Contain(e => e.Contains("_down.sql"));
    }
    
    [Fact]
    public void ValidateMigrationContent_ShouldRequireHeaderComment()
    {
        // Arrange
        var withHeader = @"-- migrations/001_initial.sql
-- Purpose: Create initial schema
-- Dependencies: None
CREATE TABLE sys_config (id TEXT PRIMARY KEY);";

        var withoutHeader = @"CREATE TABLE sys_config (id TEXT PRIMARY KEY);";
        
        // Act
        var withResult = _sut.ValidateContent(withHeader);
        var withoutResult = _sut.ValidateContent(withoutHeader);
        
        // Assert
        withResult.IsValid.Should().BeTrue();
        withoutResult.Warnings.Should().Contain(w => w.Contains("header comment"));
    }
    
    [Fact]
    public void ValidateMigrationContent_ShouldDetectForbiddenPatterns()
    {
        // Arrange
        var dangerous = @"-- Purpose: Backdoor
DROP DATABASE workspace;
GRANT ALL TO public;";
        
        // Act
        var result = _sut.ValidateContent(dangerous);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("DROP DATABASE"));
        result.Errors.Should().Contain(e => e.Contains("GRANT"));
    }
    
    [Fact]
    public void ValidateMigrationContent_ShouldRequireIfNotExists()
    {
        // Arrange
        var idempotent = "CREATE TABLE IF NOT EXISTS sys_config (id TEXT);";
        var notIdempotent = "CREATE TABLE sys_config (id TEXT);";
        
        // Act
        var idemResult = _sut.ValidateContent(idempotent);
        var notIdemResult = _sut.ValidateContent(notIdempotent);
        
        // Assert
        idemResult.IsValid.Should().BeTrue();
        notIdemResult.Warnings.Should().Contain(w => w.Contains("IF NOT EXISTS"));
    }
    
    [Fact]
    public void ExtractVersion_ShouldParseVersionFromFileName()
    {
        // Act
        var v1 = _sut.ExtractVersion("001_initial.sql");
        var v2 = _sut.ExtractVersion("042_add_feature.sql");
        var v3 = _sut.ExtractVersion("invalid.sql");
        
        // Assert
        v1.Should().Be(1);
        v2.Should().Be(42);
        v3.Should().BeNull();
    }
    
    [Fact]
    public void ValidateMigrationSequence_ShouldDetectGaps()
    {
        // Arrange
        var files = new[] { "001_a.sql", "002_b.sql", "004_d.sql" };  // Missing 003
        
        // Act
        var result = _sut.ValidateMigrationSequence(files);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("003"));
    }
}
```

### Integration Tests

```csharp
// Tests/Acode.Integration.Tests/Database/Layout/SchemaValidationIntegrationTests.cs
using System.Data;
using System.Threading.Tasks;
using Acode.Infrastructure.Database;
using Dapper;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Acode.Integration.Tests.Database.Layout;

/// <summary>
/// Integration tests that validate the actual schema against conventions.
/// These tests run against a real SQLite database with all migrations applied.
/// </summary>
public sealed class SchemaValidationIntegrationTests : IAsyncLifetime
{
    private SqliteConnection _connection = null!;
    private readonly MigrationRunner _migrationRunner = new();
    
    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        await _migrationRunner.ApplyAllAsync(_connection, CancellationToken.None);
    }
    
    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
    
    [Fact]
    public async Task AllTables_ShouldFollowNamingConventions()
    {
        // Arrange
        var validPrefixes = new[] { "conv_", "sess_", "appr_", "sync_", "sys_", "__" };
        
        // Act
        var tables = await _connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'");
        
        // Assert
        foreach (var table in tables)
        {
            var hasValidPrefix = validPrefixes.Any(p => table.StartsWith(p));
            hasValidPrefix.Should().BeTrue($"Table '{table}' should have a valid domain prefix");
            table.Should().MatchRegex(@"^[a-z][a-z0-9_]*$", 
                $"Table '{table}' should use snake_case");
        }
    }
    
    [Fact]
    public async Task AllPrimaryKeys_ShouldBeNamedId()
    {
        // Act
        var tables = await _connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'");
        
        foreach (var table in tables)
        {
            var columns = await _connection.QueryAsync<(string name, bool pk)>(
                $"PRAGMA table_info({table})");
            
            var pkColumn = columns.FirstOrDefault(c => c.pk);
            
            // Assert
            pkColumn.name.Should().Be("id", 
                $"Table '{table}' primary key should be named 'id'");
        }
    }
    
    [Fact]
    public async Task AllForeignKeys_ShouldHaveIndexes()
    {
        // Act
        var tables = await _connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__%'");
        
        foreach (var table in tables)
        {
            var foreignKeys = await _connection.QueryAsync<(string table, string from, string to)>(
                $"PRAGMA foreign_key_list({table})");
            
            foreach (var fk in foreignKeys)
            {
                var indexes = await _connection.QueryAsync<string>(
                    $"SELECT name FROM sqlite_master WHERE type='index' AND tbl_name='{table}'");
                
                // Assert
                var hasIndex = indexes.Any(idx => idx.Contains(fk.from) || 
                    idx.Contains($"{fk.from}"));
                hasIndex.Should().BeTrue(
                    $"Table '{table}' should have index for FK column '{fk.from}'");
            }
        }
    }
    
    [Fact]
    public async Task ConversationDomain_ShouldHaveAllRequiredTables()
    {
        // Arrange
        var requiredTables = new[] { "conv_chats", "conv_runs", "conv_messages", "conv_tool_calls" };
        
        // Act
        var tables = await _connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name LIKE 'conv_%'");
        
        // Assert
        tables.Should().Contain(requiredTables);
    }
    
    [Fact]
    public async Task MigrationsTable_ShouldTrackAllAppliedMigrations()
    {
        // Act
        var migrations = await _connection.QueryAsync<(string version, string applied_at, string checksum)>(
            "SELECT version, applied_at, checksum FROM __migrations ORDER BY version");
        
        // Assert
        migrations.Should().NotBeEmpty("At least one migration should be applied");
        migrations.All(m => m.checksum.Length == 64).Should().BeTrue("Checksums should be SHA-256 (64 hex chars)");
        migrations.All(m => DateTime.TryParse(m.applied_at, out _)).Should().BeTrue("Applied timestamps should be valid");
    }
    
    [Fact]
    public async Task SyncMetadataColumns_ShouldExistOnSyncableTables()
    {
        // Arrange
        var syncableTables = new[] { "conv_chats", "conv_messages", "sess_sessions" };
        var requiredColumns = new[] { "sync_status", "sync_at", "remote_id", "version" };
        
        foreach (var table in syncableTables)
        {
            // Act
            var columns = await _connection.QueryAsync<string>(
                $"SELECT name FROM pragma_table_info('{table}')");
            
            // Assert
            columns.Should().Contain(requiredColumns, 
                $"Table '{table}' should have sync metadata columns");
        }
    }
    
    [Fact]
    public async Task AuditColumns_ShouldExistOnDomainTables()
    {
        // Arrange
        var domainTables = await _connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name LIKE 'conv_%'");
        
        foreach (var table in domainTables)
        {
            // Act
            var columns = await _connection.QueryAsync<string>(
                $"SELECT name FROM pragma_table_info('{table}')");
            
            // Assert
            columns.Should().Contain("created_at", $"Table '{table}' should have created_at");
            columns.Should().Contain("updated_at", $"Table '{table}' should have updated_at");
        }
    }
}
```

```csharp
// Tests/Acode.Integration.Tests/Database/Layout/MigrationOrderIntegrationTests.cs
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Acode.Infrastructure.Database;
using FluentAssertions;
using Xunit;

namespace Acode.Integration.Tests.Database.Layout;

/// <summary>
/// Integration tests for migration ordering and dependency validation.
/// </summary>
public sealed class MigrationOrderIntegrationTests
{
    private readonly MigrationLoader _loader = new();
    
    [Fact]
    public void AllMigrations_ShouldHaveSequentialVersions()
    {
        // Act
        var migrations = _loader.LoadAllMigrations();
        var versions = migrations.Select(m => m.Version).OrderBy(v => v).ToList();
        
        // Assert
        for (int i = 0; i < versions.Count; i++)
        {
            versions[i].Should().Be(i + 1, 
                $"Migration versions should be sequential starting from 1");
        }
    }
    
    [Fact]
    public void AllMigrations_ShouldHaveMatchingDownScripts()
    {
        // Act
        var migrations = _loader.LoadAllMigrations();
        
        // Assert
        foreach (var migration in migrations)
        {
            migration.HasDownScript.Should().BeTrue(
                $"Migration {migration.Version} should have a corresponding _down.sql file");
        }
    }
    
    [Fact]
    public void MigrationDependencies_ShouldRespectOrder()
    {
        // Arrange - conv_runs depends on conv_chats
        var migrations = _loader.LoadAllMigrations();
        
        var chatsVersion = migrations.First(m => m.Name.Contains("conversation")).Version;
        var runsVersion = migrations.FirstOrDefault(m => m.Name.Contains("runs"))?.Version;
        
        // Assert
        if (runsVersion.HasValue)
        {
            chatsVersion.Should().BeLessThan(runsVersion.Value,
                "conv_chats migration should come before conv_runs");
        }
    }
    
    [Fact]
    public void AllMigrations_ShouldBeIdempotent()
    {
        // Act
        var migrations = _loader.LoadAllMigrations();
        
        // Assert
        foreach (var migration in migrations)
        {
            migration.Content.Should().Contain("IF NOT EXISTS", 
                $"Migration {migration.Version} should use IF NOT EXISTS for idempotency");
        }
    }
}
```

### End-to-End Tests

```csharp
// Tests/Acode.E2E.Tests/Database/LayoutE2ETests.cs
using System.IO;
using System.Threading.Tasks;
using Acode.Cli;
using Acode.Infrastructure.Database;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Acode.E2E.Tests.Database;

/// <summary>
/// End-to-end tests for the complete database layout workflow.
/// Tests the full lifecycle from fresh database to fully migrated state.
/// </summary>
public sealed class LayoutE2ETests : IAsyncLifetime
{
    private string _dbPath = null!;
    private SqliteConnection _connection = null!;
    
    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"acode_test_{Guid.NewGuid():N}.db");
        _connection = new SqliteConnection($"Data Source={_dbPath}");
        await _connection.OpenAsync();
    }
    
    public async Task DisposeAsync()
    {
        await _connection.DisposeAsync();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
    
    [Fact]
    public async Task FullMigration_ShouldCreateCompleteSchema()
    {
        // Arrange
        var runner = new MigrationRunner();
        
        // Act
        var result = await runner.ApplyAllAsync(_connection, CancellationToken.None);
        
        // Assert
        result.Success.Should().BeTrue();
        result.AppliedCount.Should().BeGreaterThan(0);
        
        // Verify core tables exist
        var tables = await _connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table'");
        
        tables.Should().Contain("conv_chats");
        tables.Should().Contain("sess_sessions");
        tables.Should().Contain("sys_config");
        tables.Should().Contain("__migrations");
    }
    
    [Fact]
    public async Task RollbackAndReapply_ShouldBeIdempotent()
    {
        // Arrange
        var runner = new MigrationRunner();
        await runner.ApplyAllAsync(_connection, CancellationToken.None);
        var initialVersion = await runner.GetCurrentVersionAsync(_connection, CancellationToken.None);
        
        // Act - Rollback to version 2
        await runner.RollbackToAsync(_connection, 2, CancellationToken.None);
        var afterRollback = await runner.GetCurrentVersionAsync(_connection, CancellationToken.None);
        
        // Re-apply all
        await runner.ApplyAllAsync(_connection, CancellationToken.None);
        var finalVersion = await runner.GetCurrentVersionAsync(_connection, CancellationToken.None);
        
        // Assert
        afterRollback.Should().Be(2);
        finalVersion.Should().Be(initialVersion);
    }
    
    [Fact]
    public async Task ChecksumValidation_ShouldDetectTampering()
    {
        // Arrange
        var runner = new MigrationRunner();
        await runner.ApplyAllAsync(_connection, CancellationToken.None);
        
        // Tamper with checksum
        await _connection.ExecuteAsync(
            "UPDATE __migrations SET checksum = 'tampered' WHERE version = '001'");
        
        // Act
        var result = await runner.ValidateChecksumAsync(_connection, "001", CancellationToken.None);
        
        // Assert
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("mismatch");
    }
}
```

---

## User Verification Steps

### Scenario 1: Verify Table Naming Conventions

**Objective:** Confirm all tables follow snake_case naming with domain prefixes.

**Prerequisites:**
- Acode installed with database initialized
- All migrations applied

**Steps:**
1. Open terminal in workspace directory
2. Run: `acode db schema --tables`
3. Review list of all tables
4. For each table, verify:
   - Name uses only lowercase letters, numbers, and underscores
   - Name starts with valid prefix: `conv_`, `sess_`, `appr_`, `sync_`, `sys_`, or `__`
   - Name is descriptive (e.g., `conv_messages` not `conv_msg`)

**Expected Outcome:**
- All tables listed with domain prefix grouping
- No PascalCase, camelCase, or UPPERCASE names
- Tables sort alphabetically by domain

**Verification Command:**
```bash
acode db schema --tables --validate
# Output: ✓ All 15 tables follow naming conventions
```

---

### Scenario 2: Verify Column Data Types

**Objective:** Confirm columns use correct data types per convention.

**Prerequisites:**
- Database initialized with migrations applied

**Steps:**
1. Run: `acode db schema --table conv_chats`
2. Check the `id` column:
   - Type should be `TEXT`
   - Should be PRIMARY KEY
   - Should not be INTEGER or UUID
3. Check timestamp columns (`created_at`, `updated_at`):
   - Type should be `TEXT`
   - Should store ISO 8601 format strings
4. Check boolean columns (`is_deleted`, `is_archived`):
   - Type should be `INTEGER`
   - Default should be 0 or 1
5. Repeat for other domain tables

**Expected Outcome:**
- All `id` columns are TEXT (ULID format)
- All `*_at` columns are TEXT
- All `is_*` columns are INTEGER
- No native DATETIME, BOOLEAN, or UUID types

**Sample Output:**
```
Column          Type       Notes
id              TEXT       ✓ Primary key, ULID format
created_at      TEXT       ✓ ISO 8601 timestamp
is_deleted      INTEGER    ✓ Boolean (0/1)
```

---

### Scenario 3: Verify Index Existence on Foreign Keys

**Objective:** Confirm all foreign key columns have supporting indexes.

**Prerequisites:**
- Database with conv_runs table (has FK to conv_chats)

**Steps:**
1. Run: `acode db schema --table conv_runs`
2. Identify foreign key column: `chat_id`
3. Check indexes section for index on `chat_id`
4. Run: `PRAGMA index_list(conv_runs)` in sqlite3
5. Verify index named like `idx_conv_runs_chat` exists
6. Repeat for all tables with foreign keys

**Expected Outcome:**
- Every FK column (`*_id` referencing another table) has an index
- Index names follow `idx_{table}_{column}` pattern
- No orphan FK columns without indexes

**Verification Query:**
```sql
-- Check for FK columns without indexes
SELECT t.name as table_name, fk."from" as fk_column
FROM sqlite_master t
JOIN pragma_foreign_key_list(t.name) fk
WHERE NOT EXISTS (
    SELECT 1 FROM pragma_index_list(t.name) il
    JOIN pragma_index_info(il.name) ii ON ii.name LIKE '%' || fk."from" || '%'
);
-- Expected: Empty result (no unindexed FKs)
```

---

### Scenario 4: Verify Migration File Structure

**Objective:** Confirm migration files follow naming and structure conventions.

**Prerequisites:**
- Access to migrations folder

**Steps:**
1. Navigate to `migrations/` directory
2. List all `.sql` files
3. For each file, verify:
   - Filename matches pattern `NNN_description.sql`
   - NNN is 3-digit zero-padded (001, 002, etc.)
   - Description uses snake_case (no hyphens or capitals)
4. Check that every `NNN_xxx.sql` has matching `NNN_xxx_down.sql`
5. Open a migration file and verify header comment exists

**Expected Outcome:**
- All files follow `NNN_description.sql` pattern
- Every UP script has matching DOWN script
- No gaps in version sequence (001, 002, 003... not 001, 003)

**Sample Directory Listing:**
```
migrations/
├── 001_initial_schema.sql        ✓
├── 001_initial_schema_down.sql   ✓
├── 002_add_conversations.sql     ✓
├── 002_add_conversations_down.sql ✓
├── 003_add_sessions.sql          ✓
├── 003_add_sessions_down.sql     ✓
```

---

### Scenario 5: Verify Sync Metadata Columns

**Objective:** Confirm syncable tables have required sync columns.

**Prerequisites:**
- Conversation tables exist

**Steps:**
1. Run: `acode db schema --table conv_chats`
2. Verify these columns exist:
   - `sync_status` (TEXT, default 'pending')
   - `sync_at` (TEXT, nullable)
   - `remote_id` (TEXT, nullable)
   - `version` (INTEGER, default 1)
3. Check same columns exist on `conv_messages`
4. Check same columns exist on `sess_sessions`

**Expected Outcome:**
- All tables that sync to remote have full sync metadata
- `sync_status` has appropriate CHECK constraint or default
- `version` starts at 1 for optimistic locking

**Verification Query:**
```sql
SELECT name FROM pragma_table_info('conv_chats') 
WHERE name IN ('sync_status', 'sync_at', 'remote_id', 'version');
-- Expected: 4 rows returned
```

---

### Scenario 6: Verify Soft Delete Implementation

**Objective:** Confirm soft delete pattern is properly implemented.

**Prerequisites:**
- conv_chats table exists

**Steps:**
1. Run: `acode db schema --table conv_chats`
2. Verify `deleted_at` column exists (TEXT, nullable)
3. Verify `is_deleted` column exists (INTEGER, default 0)
4. Insert a test record
5. Soft delete: `UPDATE conv_chats SET is_deleted=1, deleted_at=datetime('now') WHERE id='test'`
6. Query without filter: `SELECT * FROM conv_chats WHERE id='test'`
7. Query with soft delete filter: `SELECT * FROM conv_chats WHERE id='test' AND is_deleted=0`

**Expected Outcome:**
- Record remains in table after soft delete
- Unfiltered query returns the record
- Filtered query (normal usage) excludes the record
- `deleted_at` contains timestamp when deleted

---

### Scenario 7: Verify Rollback Capability

**Objective:** Confirm migrations can be safely rolled back.

**Prerequisites:**
- Database at version 005 or higher

**Steps:**
1. Check current version: `acode db status`
2. Note current version (e.g., 005)
3. Rollback one version: `acode db rollback --steps 1`
4. Verify version decreased: `acode db status`
5. Verify removed table/columns no longer exist
6. Re-apply: `acode db migrate`
7. Verify version restored

**Expected Outcome:**
- Rollback completes without errors
- Schema state matches expected for that version
- Re-apply restores full schema
- No data corruption in unaffected tables

**Sample Output:**
```bash
$ acode db rollback --steps 1
Rolling back 005_add_sync.sql... done
Database now at version 004

$ acode db migrate
Applying 005_add_sync.sql... done
Database now at version 005
```

---

### Scenario 8: Verify Cross-Database Compatibility

**Objective:** Confirm SQL works on both SQLite and PostgreSQL.

**Prerequisites:**
- SQLite database available locally
- PostgreSQL test database available

**Steps:**
1. Apply all migrations to SQLite: `acode db migrate --provider sqlite`
2. Apply all migrations to PostgreSQL: `acode db migrate --provider postgres`
3. Compare table lists between databases
4. Compare column types (accounting for expected differences)
5. Run sample queries on both databases

**Expected Outcome:**
- All migrations apply successfully on both databases
- Same tables exist on both (by name)
- Column types are equivalent (TEXT in both for IDs/timestamps)
- Sample queries return same structure

**Dialect-Specific Notes:**
```
SQLite                 PostgreSQL
TEXT                   TEXT or VARCHAR
INTEGER (boolean)      BOOLEAN (auto-converted)
TEXT (JSON)            JSONB
```

---

### Scenario 9: Verify Migration Checksum Integrity

**Objective:** Confirm checksums prevent migration tampering.

**Prerequisites:**
- Database with migrations applied

**Steps:**
1. Run: `acode db status --verbose`
2. Note checksum for migration 001
3. View stored checksum: `SELECT checksum FROM __migrations WHERE version='001'`
4. Manually compute checksum:
   ```powershell
   $content = Get-Content migrations/001_initial_schema.sql -Raw
   $normalized = $content -replace "`r`n", "`n"
   $hash = [System.Security.Cryptography.SHA256]::HashData([System.Text.Encoding]::UTF8.GetBytes($normalized))
   [Convert]::ToHexString($hash).ToLowerInvariant()
   ```
5. Compare computed hash with stored checksum

**Expected Outcome:**
- Computed hash matches stored checksum exactly
- Modifying migration file would change checksum
- Migration runner detects tampered files

---

### Scenario 10: Verify System Tables Configuration

**Objective:** Confirm system tables are properly configured.

**Prerequisites:**
- Database initialized

**Steps:**
1. Verify `sys_config` table exists: `acode db schema --table sys_config`
2. Insert config value: `INSERT INTO sys_config (key, value, updated_at) VALUES ('test_key', 'test_value', datetime('now'))`
3. Query config: `SELECT * FROM sys_config WHERE key='test_key'`
4. Verify `sys_locks` table exists with proper columns
5. Verify `sys_health` table exists for health checks
6. Verify `__migrations` table is protected (attempt DELETE should be audited)

**Expected Outcome:**
- All system tables exist with proper columns
- Config storage works correctly
- Lock table has holder, acquired_at, expires_at columns
- Health table can store check results
- __migrations table tracks all applied versions

**Verification Query:**
```sql
-- Check all system tables exist
SELECT name FROM sqlite_master 
WHERE type='table' AND name LIKE 'sys_%'
ORDER BY name;
-- Expected: sys_config, sys_health, sys_locks
```

---

## Implementation Prompt

### Overview

This section provides complete, production-ready C# code for implementing the workspace database layout and migration strategy. The implementation follows Clean Architecture principles with clear separation between domain models, infrastructure services, and validation utilities.

### File Structure

```
src/
├── Acode.Domain/
│   └── Database/
│       ├── ColumnSchema.cs
│       ├── TableSchema.cs
│       └── ValidationResult.cs
│
├── Acode.Infrastructure/
│   └── Database/
│       ├── Layout/
│       │   ├── NamingConventionValidator.cs
│       │   ├── DataTypeValidator.cs
│       │   └── SchemaConventions.cs
│       ├── Migrations/
│       │   ├── MigrationFile.cs
│       │   ├── MigrationFileValidator.cs
│       │   ├── MigrationLoader.cs
│       │   └── MigrationSqlValidator.cs
│       └── Schema/
│           └── SchemaInspector.cs
│
└── migrations/
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
```

### Domain Models

```csharp
// src/Acode.Domain/Database/ColumnSchema.cs
namespace Acode.Domain.Database;

/// <summary>
/// Represents a database column's schema definition.
/// </summary>
public sealed record ColumnSchema
{
    public string Name { get; init; }
    public string DataType { get; init; }
    public bool IsNullable { get; init; }
    public bool IsPrimaryKey { get; init; }
    public bool IsForeignKey { get; init; }
    public string? ForeignKeyTable { get; init; }
    public string? DefaultValue { get; init; }
    
    public ColumnSchema(
        string name,
        string dataType,
        bool isNullable = true,
        bool isPrimaryKey = false,
        bool isForeignKey = false,
        string? foreignKeyTable = null,
        string? defaultValue = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
        IsNullable = isNullable;
        IsPrimaryKey = isPrimaryKey;
        IsForeignKey = isForeignKey;
        ForeignKeyTable = foreignKeyTable;
        DefaultValue = defaultValue;
    }
    
    public bool IsTimestamp => Name.EndsWith("_at", StringComparison.Ordinal);
    public bool IsBoolean => Name.StartsWith("is_", StringComparison.Ordinal);
    public bool IsId => Name == "id" || Name.EndsWith("_id", StringComparison.Ordinal);
}
```

```csharp
// src/Acode.Domain/Database/TableSchema.cs
namespace Acode.Domain.Database;

/// <summary>
/// Represents a database table's schema definition.
/// </summary>
public sealed record TableSchema
{
    public string Name { get; init; }
    public IReadOnlyList<ColumnSchema> Columns { get; init; }
    public IReadOnlyList<string> Indexes { get; init; }
    
    public TableSchema(
        string name,
        IEnumerable<ColumnSchema> columns,
        IEnumerable<string>? indexes = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Columns = columns?.ToList() ?? throw new ArgumentNullException(nameof(columns));
        Indexes = indexes?.ToList() ?? Array.Empty<string>();
    }
    
    public ColumnSchema? PrimaryKey => Columns.FirstOrDefault(c => c.IsPrimaryKey);
    public IEnumerable<ColumnSchema> ForeignKeys => Columns.Where(c => c.IsForeignKey);
    
    public string DomainPrefix => Name.Split('_')[0] + "_";
}
```

```csharp
// src/Acode.Domain/Database/ValidationResult.cs
namespace Acode.Domain.Database;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public sealed record ValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; }
    public IReadOnlyList<string> Warnings { get; init; }
    
    public static ValidationResult Success() => new()
    {
        IsValid = true,
        Errors = Array.Empty<string>(),
        Warnings = Array.Empty<string>()
    };
    
    public static ValidationResult Failure(params string[] errors) => new()
    {
        IsValid = false,
        Errors = errors,
        Warnings = Array.Empty<string>()
    };
    
    public static ValidationResult WithWarnings(params string[] warnings) => new()
    {
        IsValid = true,
        Errors = Array.Empty<string>(),
        Warnings = warnings
    };
    
    public ValidationResult Combine(ValidationResult other) => new()
    {
        IsValid = IsValid && other.IsValid,
        Errors = Errors.Concat(other.Errors).ToList(),
        Warnings = Warnings.Concat(other.Warnings).ToList()
    };
}
```

### Naming Convention Validator

```csharp
// src/Acode.Infrastructure/Database/Layout/NamingConventionValidator.cs
using System.Text.RegularExpressions;
using Acode.Domain.Database;

namespace Acode.Infrastructure.Database.Layout;

/// <summary>
/// Validates database naming conventions.
/// Ensures tables, columns, indexes follow established patterns.
/// </summary>
public sealed partial class NamingConventionValidator
{
    private static readonly HashSet<string> ValidPrefixes = new(StringComparer.Ordinal)
    {
        "conv_",   // Conversation domain
        "sess_",   // Session domain
        "appr_",   // Approval domain
        "sync_",   // Synchronization domain
        "sys_",    // System domain
        "__"       // Reserved/internal
    };
    
    [GeneratedRegex(@"^[a-z][a-z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex SnakeCasePattern();
    
    [GeneratedRegex(@"^idx_[a-z][a-z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex IndexPattern();
    
    [GeneratedRegex(@"^ux_[a-z][a-z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex UniqueIndexPattern();
    
    [GeneratedRegex(@"^fk_[a-z][a-z0-9_]*$", RegexOptions.Compiled)]
    private static partial Regex ForeignKeyPattern();
    
    public ValidationResult ValidateTableName(string tableName)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(tableName))
        {
            return ValidationResult.Failure("Table name cannot be empty");
        }
        
        // Check snake_case
        if (!SnakeCasePattern().IsMatch(tableName))
        {
            errors.Add($"Table name '{tableName}' must use snake_case (lowercase letters, numbers, underscores)");
        }
        
        // Check prefix
        var hasValidPrefix = ValidPrefixes.Any(p => tableName.StartsWith(p, StringComparison.Ordinal));
        if (!hasValidPrefix)
        {
            errors.Add($"Table name '{tableName}' must have a valid domain prefix: {string.Join(", ", ValidPrefixes)}");
        }
        
        // Check length (PostgreSQL limit)
        if (tableName.Length > 63)
        {
            errors.Add($"Table name '{tableName}' exceeds 63 character PostgreSQL limit");
        }
        
        return errors.Count == 0 
            ? ValidationResult.Success() 
            : ValidationResult.Failure(errors.ToArray());
    }
    
    public ValidationResult ValidateColumnName(string columnName)
    {
        if (string.IsNullOrWhiteSpace(columnName))
        {
            return ValidationResult.Failure("Column name cannot be empty");
        }
        
        if (!SnakeCasePattern().IsMatch(columnName))
        {
            return ValidationResult.Failure(
                $"Column name '{columnName}' must use snake_case (lowercase letters, numbers, underscores)");
        }
        
        if (columnName.Length > 63)
        {
            return ValidationResult.Failure(
                $"Column name '{columnName}' exceeds 63 character PostgreSQL limit");
        }
        
        return ValidationResult.Success();
    }
    
    public ValidationResult ValidateIndexName(string indexName)
    {
        if (string.IsNullOrWhiteSpace(indexName))
        {
            return ValidationResult.Failure("Index name cannot be empty");
        }
        
        var isValidIndex = IndexPattern().IsMatch(indexName);
        var isValidUnique = UniqueIndexPattern().IsMatch(indexName);
        var isValidFk = ForeignKeyPattern().IsMatch(indexName);
        
        if (!isValidIndex && !isValidUnique && !isValidFk)
        {
            return ValidationResult.Failure(
                $"Index name '{indexName}' must follow pattern: idx_{{table}}_{{columns}}, ux_{{table}}_{{columns}}, or fk_{{table}}_{{ref}}");
        }
        
        return ValidationResult.Success();
    }
    
    public ValidationResult ValidatePrimaryKey(TableSchema table)
    {
        var pk = table.Columns.FirstOrDefault(c => c.IsPrimaryKey);
        
        if (pk == null)
        {
            return ValidationResult.Failure($"Table '{table.Name}' must have a primary key");
        }
        
        if (pk.Name != "id")
        {
            return ValidationResult.Failure(
                $"Table '{table.Name}' primary key must be named 'id', found '{pk.Name}'");
        }
        
        return ValidationResult.Success();
    }
    
    public ValidationResult ValidateForeignKeyColumn(string columnName, string referencedTable)
    {
        // FK column should be named {table}_id (without prefix)
        var expectedPattern = referencedTable.Split('_').Last() + "_id";
        var tableWithoutPrefix = referencedTable.Substring(referencedTable.IndexOf('_') + 1);
        var expectedName = tableWithoutPrefix + "_id";
        
        // Also allow just removing the 's' for plurals (chats -> chat_id)
        var singularName = tableWithoutPrefix.TrimEnd('s') + "_id";
        
        if (columnName != expectedName && columnName != singularName && !columnName.EndsWith("_id"))
        {
            return ValidationResult.Failure(
                $"Foreign key column '{columnName}' referencing '{referencedTable}' should follow {{table}}_id pattern");
        }
        
        return ValidationResult.Success();
    }
    
    public ValidationResult ValidateTimestampColumn(string columnName)
    {
        if (!columnName.EndsWith("_at", StringComparison.Ordinal))
        {
            return ValidationResult.Failure(
                $"Timestamp column '{columnName}' should follow {{action}}_at pattern (e.g., created_at, updated_at)");
        }
        
        return ValidationResult.Success();
    }
    
    public ValidationResult ValidateBooleanColumn(string columnName)
    {
        if (!columnName.StartsWith("is_", StringComparison.Ordinal))
        {
            return ValidationResult.Failure(
                $"Boolean column '{columnName}' should follow is_{{condition}} pattern (e.g., is_deleted, is_active)");
        }
        
        return ValidationResult.Success();
    }
}
```

### Data Type Validator

```csharp
// src/Acode.Infrastructure/Database/Layout/DataTypeValidator.cs
using Acode.Domain.Database;

namespace Acode.Infrastructure.Database.Layout;

/// <summary>
/// Validates database column data types.
/// Ensures correct types for IDs, timestamps, booleans, etc.
/// </summary>
public sealed class DataTypeValidator
{
    private static readonly HashSet<string> ValidIdTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "TEXT", "VARCHAR", "VARCHAR(26)"
    };
    
    private static readonly HashSet<string> ValidTimestampTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "TEXT"
    };
    
    private static readonly HashSet<string> ValidBooleanTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "INTEGER", "INT"
    };
    
    private static readonly HashSet<string> ValidJsonTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "TEXT", "JSONB"
    };
    
    public ValidationResult ValidateIdColumn(ColumnSchema column)
    {
        if (!ValidIdTypes.Contains(column.DataType))
        {
            return ValidationResult.Failure(
                $"ID column '{column.Name}' must use TEXT type for ULID format, found '{column.DataType}'");
        }
        
        return ValidationResult.Success();
    }
    
    public ValidationResult ValidateTimestampColumn(ColumnSchema column)
    {
        if (!ValidTimestampTypes.Contains(column.DataType))
        {
            var result = ValidationResult.WithWarnings(
                $"Timestamp column '{column.Name}' should use TEXT type for ISO 8601 format, found '{column.DataType}'");
            
            return result;
        }
        
        return ValidationResult.Success();
    }
    
    public ValidationResult ValidateBooleanColumn(ColumnSchema column)
    {
        if (!ValidBooleanTypes.Contains(column.DataType))
        {
            return ValidationResult.Failure(
                $"Boolean column '{column.Name}' must use INTEGER type (0/1) for SQLite compatibility, found '{column.DataType}'");
        }
        
        return ValidationResult.Success();
    }
    
    public ValidationResult ValidateJsonColumn(ColumnSchema column)
    {
        if (!ValidJsonTypes.Contains(column.DataType))
        {
            return ValidationResult.Failure(
                $"JSON column '{column.Name}' must use TEXT (SQLite) or JSONB (PostgreSQL) type, found '{column.DataType}'");
        }
        
        return ValidationResult.Success();
    }
    
    public ValidationResult ValidateForeignKeyColumn(ColumnSchema column)
    {
        // FK must match PK type (TEXT for ULID)
        if (!ValidIdTypes.Contains(column.DataType))
        {
            return ValidationResult.Failure(
                $"Foreign key column '{column.Name}' must use TEXT type to match ULID primary keys, found '{column.DataType}'");
        }
        
        return ValidationResult.Success();
    }
    
    public ValidationResult ValidateEnumColumn(ColumnSchema column)
    {
        if (!column.DataType.Equals("TEXT", StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.WithWarnings(
                $"Enum column '{column.Name}' should use TEXT for portability, found '{column.DataType}'");
        }
        
        return ValidationResult.Success();
    }
}
```

### Migration File Validator

```csharp
// src/Acode.Infrastructure/Database/Migrations/MigrationFileValidator.cs
using System.Text.RegularExpressions;
using Acode.Domain.Database;

namespace Acode.Infrastructure.Database.Migrations;

/// <summary>
/// Validates migration file structure and content.
/// </summary>
public sealed partial class MigrationFileValidator
{
    [GeneratedRegex(@"^(\d{3})_([a-z][a-z0-9_]*)\.sql$", RegexOptions.Compiled)]
    private static partial Regex FileNamePattern();
    
    [GeneratedRegex(@"^--\s*.*$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex HeaderCommentPattern();
    
    private static readonly string[] ForbiddenPatterns = new[]
    {
        @"\bDROP\s+DATABASE\b",
        @"\bTRUNCATE\s+TABLE\b",
        @"\bGRANT\b",
        @"\bREVOKE\b",
        @"\bCREATE\s+USER\b",
        @"\bALTER\s+USER\b",
        @"\bload_extension\b"
    };
    
    public ValidationResult ValidateFileName(string fileName)
    {
        var match = FileNamePattern().Match(Path.GetFileName(fileName));
        
        if (!match.Success)
        {
            return ValidationResult.Failure(
                $"Migration filename '{fileName}' must match pattern NNN_description.sql (e.g., 001_initial_schema.sql)");
        }
        
        return ValidationResult.Success();
    }
    
    public ValidationResult ValidateDownScriptExists(string upFilePath, bool downFileExists)
    {
        if (!downFileExists)
        {
            var expectedDown = upFilePath.Replace(".sql", "_down.sql");
            return ValidationResult.Failure(
                $"Migration '{Path.GetFileName(upFilePath)}' is missing rollback script: {Path.GetFileName(expectedDown)}");
        }
        
        return ValidationResult.Success();
    }
    
    public ValidationResult ValidateContent(string content)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        
        // Check for header comment
        if (!content.TrimStart().StartsWith("--"))
        {
            warnings.Add("Migration should start with a header comment explaining purpose and dependencies");
        }
        
        // Check for forbidden patterns
        foreach (var pattern in ForbiddenPatterns)
        {
            if (Regex.IsMatch(content, pattern, RegexOptions.IgnoreCase))
            {
                errors.Add($"Forbidden SQL pattern detected: {pattern.Replace(@"\b", "").Replace(@"\s+", " ")}");
            }
        }
        
        // Check for IF NOT EXISTS / IF EXISTS
        if (content.Contains("CREATE TABLE", StringComparison.OrdinalIgnoreCase) &&
            !content.Contains("IF NOT EXISTS", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("CREATE TABLE statements should use IF NOT EXISTS for idempotency");
        }
        
        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }
    
    public int? ExtractVersion(string fileName)
    {
        var match = FileNamePattern().Match(Path.GetFileName(fileName));
        
        if (match.Success && int.TryParse(match.Groups[1].Value, out var version))
        {
            return version;
        }
        
        return null;
    }
    
    public ValidationResult ValidateMigrationSequence(IEnumerable<string> migrationFiles)
    {
        var versions = migrationFiles
            .Select(f => ExtractVersion(f))
            .Where(v => v.HasValue)
            .Select(v => v!.Value)
            .OrderBy(v => v)
            .ToList();
        
        var errors = new List<string>();
        
        for (int i = 0; i < versions.Count; i++)
        {
            var expected = i + 1;
            if (versions[i] != expected)
            {
                errors.Add($"Migration sequence gap: expected version {expected:D3}, found {versions[i]:D3}");
            }
        }
        
        return errors.Count == 0
            ? ValidationResult.Success()
            : ValidationResult.Failure(errors.ToArray());
    }
}
```

### Migration SQL Files

```sql
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
```

```sql
-- migrations/001_initial_schema_down.sql
--
-- Rollback: Remove base schema

DROP INDEX IF EXISTS idx_sys_health_checked;
DROP INDEX IF EXISTS idx_sys_health_check;
DROP TABLE IF EXISTS sys_health;
DROP TABLE IF EXISTS sys_locks;
DROP TABLE IF EXISTS sys_config;
DROP TABLE IF EXISTS __migrations;
```

```sql
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
```

```sql
-- migrations/002_add_conversations_down.sql
--
-- Rollback: Remove conversation domain

DROP INDEX IF EXISTS idx_conv_tool_calls_status;
DROP INDEX IF EXISTS idx_conv_tool_calls_message;
DROP INDEX IF EXISTS idx_conv_messages_role;
DROP INDEX IF EXISTS idx_conv_messages_run;
DROP INDEX IF EXISTS idx_conv_runs_status;
DROP INDEX IF EXISTS idx_conv_runs_chat;
DROP INDEX IF EXISTS idx_conv_chats_created;
DROP INDEX IF EXISTS idx_conv_chats_sync;
DROP INDEX IF EXISTS idx_conv_chats_worktree;
DROP TABLE IF EXISTS conv_tool_calls;
DROP TABLE IF EXISTS conv_messages;
DROP TABLE IF EXISTS conv_runs;
DROP TABLE IF EXISTS conv_chats;
```

### Schema Conventions Documentation

```markdown
<!-- docs/database/conventions.md -->
# Database Schema Conventions

## Table Naming
- Use snake_case: `conv_chats`, not `ConvChats` or `convChats`
- Prefix with domain: `conv_`, `sess_`, `appr_`, `sync_`, `sys_`, `__`
- Be descriptive: `conv_messages`, not `conv_msg`

## Column Naming
- Use snake_case: `created_at`, not `createdAt`
- Primary key: always `id`
- Foreign keys: `{table}_id` (e.g., `chat_id`)
- Timestamps: `{action}_at` (e.g., `created_at`, `updated_at`)
- Booleans: `is_{condition}` (e.g., `is_deleted`, `is_active`)

## Data Types
- IDs: TEXT (ULID format, 26 characters)
- Timestamps: TEXT (ISO 8601 format)
- Booleans: INTEGER (0 or 1)
- JSON: TEXT (SQLite) or JSONB (PostgreSQL)
- Enums: TEXT (string values)

## Indexes
- Name pattern: `idx_{table}_{columns}`
- Unique: `ux_{table}_{columns}`
- Foreign keys: always indexed
- Limit: 5 indexes per table (soft limit)

## Migrations
- Filename: `NNN_description.sql`
- Rollback: `NNN_description_down.sql`
- Idempotent: use IF NOT EXISTS / IF EXISTS
- Header: purpose, dependencies, author, date
```

### Error Codes Reference

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

### Implementation Checklist

1. [ ] Create domain model classes (ColumnSchema, TableSchema, ValidationResult)
2. [ ] Implement NamingConventionValidator with all validation methods
3. [ ] Implement DataTypeValidator for column type checking
4. [ ] Implement MigrationFileValidator for file structure validation
5. [ ] Create 001_initial_schema.sql with system tables
6. [ ] Create 002_add_conversations.sql with conversation domain
7. [ ] Create 003_add_sessions.sql with session domain
8. [ ] Create 004_add_approvals.sql with approval domain
9. [ ] Create 005_add_sync.sql with sync domain
10. [ ] Write all corresponding _down.sql rollback scripts
11. [ ] Create conventions.md documentation
12. [ ] Write unit tests for all validators
13. [ ] Write integration tests for schema validation
14. [ ] Run full migration cycle on SQLite and PostgreSQL

### Rollout Plan

1. **Phase 1:** Domain models and validation infrastructure
2. **Phase 2:** Initial migration (001) and system tables
3. **Phase 3:** Conversation domain migration (002)
4. **Phase 4:** Session and approval domain migrations (003, 004)
5. **Phase 5:** Sync domain migration (005)
6. **Phase 6:** Documentation and cross-database testing

---

**End of Task 050.a Specification**