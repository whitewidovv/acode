# Task 026.a: SQLite Schema

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 026 (Queue Persistence)  

---

## Description

Task 026.a defines the SQLite schema for the task queue. Tables MUST store tasks, state history, and worker assignments. Indexes MUST optimize common queries.

The schema MUST support efficient queue operations. Dequeue MUST be fast even with many tasks. Filtering by status and priority MUST be indexed. History queries MUST be efficient.

Schema migrations MUST be versioned. Upgrades MUST be automatic and safe. Downgrades MUST NOT be supported (forward-only). Schema version MUST be tracked.

### Business Value

Proper schema design enables:
- Fast queue operations
- Efficient queries
- Data integrity
- Safe migrations
- Audit compliance

### Scope Boundaries

This task covers SQLite schema only. Queue logic is in Task 026. State transitions are in Task 026.b. Crash recovery is in Task 026.c.

### Integration Points

- Task 026: Uses schema
- Task 017: Similar patterns
- Task 018: Outbox reference

### Failure Modes

- Migration failure → Rollback transaction
- Schema mismatch → Require upgrade
- Index corruption → Rebuild

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| DDL | Data Definition Language |
| Index | Query optimization structure |
| Covering Index | Index with all query columns |
| Composite Key | Multi-column primary key |
| Foreign Key | Referential integrity constraint |
| Migration | Schema version change |
| Trigger | Automatic action on change |
| View | Virtual table from query |
| Constraint | Data validation rule |

---

## Out of Scope

- Stored procedures
- Full-text search
- Spatial indexes
- JSON columns
- Generated columns
- Virtual tables

---

## Functional Requirements

### FR-001 to FR-025: Tasks Table

- FR-001: `tasks` table MUST exist
- FR-002: `id` MUST be TEXT PRIMARY KEY
- FR-003: `title` MUST be TEXT NOT NULL
- FR-004: `description` MUST be TEXT NOT NULL
- FR-005: `status` MUST be TEXT NOT NULL
- FR-006: `priority` MUST be INTEGER NOT NULL DEFAULT 3
- FR-007: `dependencies` MUST be TEXT (JSON array)
- FR-008: `files` MUST be TEXT (JSON array)
- FR-009: `tags` MUST be TEXT (JSON array)
- FR-010: `metadata` MUST be TEXT (JSON object)
- FR-011: `timeout_seconds` MUST be INTEGER DEFAULT 3600
- FR-012: `retry_limit` MUST be INTEGER DEFAULT 3
- FR-013: `attempt_count` MUST be INTEGER DEFAULT 0
- FR-014: `worker_id` MUST be TEXT (nullable)
- FR-015: `parent_id` MUST be TEXT (nullable FK)
- FR-016: `created_at` MUST be TEXT NOT NULL (ISO8601)
- FR-017: `started_at` MUST be TEXT (nullable)
- FR-018: `completed_at` MUST be TEXT (nullable)
- FR-019: `last_error` MUST be TEXT (nullable)
- FR-020: `result` MUST be TEXT (nullable, JSON)
- FR-021: `spec_version` MUST be INTEGER DEFAULT 1
- FR-022: `updated_at` MUST be TEXT NOT NULL
- FR-023: Status CHECK constraint MUST exist
- FR-024: Priority CHECK constraint MUST exist
- FR-025: Parent FK MUST reference tasks(id)

### FR-026 to FR-040: History Table

- FR-026: `task_history` table MUST exist
- FR-027: `id` MUST be INTEGER PRIMARY KEY AUTOINCREMENT
- FR-028: `task_id` MUST be TEXT NOT NULL FK
- FR-029: `from_status` MUST be TEXT (nullable)
- FR-030: `to_status` MUST be TEXT NOT NULL
- FR-031: `actor` MUST be TEXT NOT NULL
- FR-032: `reason` MUST be TEXT (nullable)
- FR-033: `timestamp` MUST be TEXT NOT NULL (ISO8601)
- FR-034: FK MUST cascade on delete
- FR-035: Index on task_id MUST exist
- FR-036: Index on timestamp MUST exist
- FR-037: Actor values: worker, user, system
- FR-038: History MUST be append-only
- FR-039: No UPDATE on history MUST be enforced
- FR-040: No DELETE on history except cascade

### FR-041 to FR-055: Indexes

- FR-041: `idx_tasks_status_priority` composite index
- FR-042: `idx_tasks_status` single index
- FR-043: `idx_tasks_priority` single index
- FR-044: `idx_tasks_created_at` single index
- FR-045: `idx_tasks_worker_id` single index
- FR-046: `idx_tasks_parent_id` single index
- FR-047: `idx_history_task_id` single index
- FR-048: `idx_history_timestamp` single index
- FR-049: Dequeue query MUST use covering index
- FR-050: Status filter MUST use index
- FR-051: Priority sort MUST use index
- FR-052: Worker lookup MUST use index
- FR-053: EXPLAIN QUERY PLAN MUST show index use
- FR-054: Index stats MUST be available
- FR-055: ANALYZE MUST run periodically

### FR-056 to FR-070: Migrations

- FR-056: `schema_version` table MUST exist
- FR-057: Version MUST be single row
- FR-058: Current version MUST be queryable
- FR-059: Migration scripts MUST be numbered
- FR-060: Migration MUST run in transaction
- FR-061: Migration failure MUST rollback
- FR-062: Applied migrations MUST be logged
- FR-063: Missing migrations MUST auto-apply
- FR-064: Version mismatch MUST error
- FR-065: Schema hash MUST be stored
- FR-066: Hash mismatch MUST warn
- FR-067: Backup before migration MUST be optional
- FR-068: Migration dry-run MUST be supported
- FR-069: Migration timing MUST be logged
- FR-070: Post-migration validation MUST run

---

## Non-Functional Requirements

- NFR-001: Dequeue MUST use index
- NFR-002: Index size MUST be reasonable
- NFR-003: Migration MUST complete in <30s
- NFR-004: Schema MUST fit in memory
- NFR-005: No table scans for common queries
- NFR-006: History MUST be purgeable
- NFR-007: Purge MUST not block operations
- NFR-008: Schema MUST be self-documenting
- NFR-009: Comments MUST exist
- NFR-010: Constraints MUST be named

---

## User Manual Documentation

### Schema Diagram

```
┌─────────────────────────────────────────────────────────────┐
│ tasks                                                        │
├─────────────────────────────────────────────────────────────┤
│ id            TEXT PRIMARY KEY                               │
│ title         TEXT NOT NULL                                  │
│ description   TEXT NOT NULL                                  │
│ status        TEXT NOT NULL CHECK (...)                      │
│ priority      INTEGER NOT NULL DEFAULT 3 CHECK (1-5)         │
│ dependencies  TEXT (JSON)                                    │
│ files         TEXT (JSON)                                    │
│ tags          TEXT (JSON)                                    │
│ metadata      TEXT (JSON)                                    │
│ timeout_secs  INTEGER DEFAULT 3600                           │
│ retry_limit   INTEGER DEFAULT 3                              │
│ attempt_count INTEGER DEFAULT 0                              │
│ worker_id     TEXT                                           │
│ parent_id     TEXT → tasks(id)                               │
│ created_at    TEXT NOT NULL                                  │
│ started_at    TEXT                                           │
│ completed_at  TEXT                                           │
│ updated_at    TEXT NOT NULL                                  │
│ last_error    TEXT                                           │
│ result        TEXT (JSON)                                    │
│ spec_version  INTEGER DEFAULT 1                              │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ FK
                              ▼
┌─────────────────────────────────────────────────────────────┐
│ task_history                                                 │
├─────────────────────────────────────────────────────────────┤
│ id           INTEGER PRIMARY KEY AUTOINCREMENT               │
│ task_id      TEXT NOT NULL → tasks(id) ON DELETE CASCADE     │
│ from_status  TEXT                                            │
│ to_status    TEXT NOT NULL                                   │
│ actor        TEXT NOT NULL                                   │
│ reason       TEXT                                            │
│ timestamp    TEXT NOT NULL                                   │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│ schema_version                                               │
├─────────────────────────────────────────────────────────────┤
│ version      INTEGER NOT NULL                                │
│ applied_at   TEXT NOT NULL                                   │
│ hash         TEXT NOT NULL                                   │
└─────────────────────────────────────────────────────────────┘
```

### Query Optimization

**Dequeue (optimized):**
```sql
SELECT * FROM tasks
WHERE status = 'pending'
ORDER BY priority ASC, created_at ASC
LIMIT 1;
-- Uses: idx_tasks_status_priority
```

**List by status:**
```sql
SELECT * FROM tasks WHERE status = ?
-- Uses: idx_tasks_status
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Tasks table created
- [ ] AC-002: History table created
- [ ] AC-003: All columns defined
- [ ] AC-004: Constraints enforced
- [ ] AC-005: Indexes created
- [ ] AC-006: Foreign keys work
- [ ] AC-007: Migration versioned
- [ ] AC-008: Schema hash tracked
- [ ] AC-009: Query plans use indexes
- [ ] AC-010: Documentation complete

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Table creation
- [ ] UT-002: Constraint enforcement
- [ ] UT-003: Index usage
- [ ] UT-004: Migration application

### Integration Tests

- [ ] IT-001: Full schema creation
- [ ] IT-002: Data integrity
- [ ] IT-003: Query performance

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Infrastructure/
│   └── Persistence/
│       └── TaskQueue/
│           ├── Migrations/
│           │   ├── V001__InitialSchema.sql
│           │   ├── V002__AddIndexes.sql
│           │   └── V003__AddResultColumn.sql
│           ├── ISchemaMigrator.cs
│           ├── SchemaMigrator.cs
│           ├── SchemaVersion.cs
│           └── TaskQueueDbContext.cs

tests/
└── Acode.Infrastructure.Tests/
    └── Persistence/
        └── TaskQueue/
            ├── SchemaMigrationTests.cs
            └── IndexUsageTests.cs
```

### Complete DDL Script

```sql
-- ============================================================
-- Acode Task Queue SQLite Schema
-- Version: 1
-- ============================================================

-- ------------------------------------------------------------
-- Schema Version Tracking
-- ------------------------------------------------------------
-- Single-row table tracking current schema version.
-- Used for migration detection and validation.
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS schema_version (
    -- Current schema version number (monotonically increasing)
    version INTEGER NOT NULL,
    
    -- When this version was applied (ISO 8601)
    applied_at TEXT NOT NULL,
    
    -- SHA256 hash of schema DDL for integrity check
    hash TEXT NOT NULL,
    
    -- Description of changes in this version
    description TEXT,
    
    -- Enforce single row
    CONSTRAINT single_row CHECK (rowid = 1)
);

-- Insert initial version (will fail if exists)
INSERT OR IGNORE INTO schema_version (rowid, version, applied_at, hash, description)
VALUES (1, 1, datetime('now'), '', 'Initial schema');

-- ------------------------------------------------------------
-- Tasks Table
-- ------------------------------------------------------------
-- Primary table storing all task specs and execution state.
-- JSON columns store arrays/objects as TEXT.
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS tasks (
    -- ULID task identifier (26 chars, lexicographically sortable)
    id TEXT PRIMARY KEY 
        CONSTRAINT valid_ulid CHECK (length(id) = 26),
    
    -- Task title (1-200 chars)
    title TEXT NOT NULL
        CONSTRAINT title_length CHECK (length(title) BETWEEN 1 AND 200),
    
    -- Full task description
    description TEXT NOT NULL
        CONSTRAINT description_length CHECK (length(description) >= 1),
    
    -- Current task status (constrained enum)
    status TEXT NOT NULL DEFAULT 'pending'
        CONSTRAINT valid_status CHECK (status IN (
            'pending',    -- Awaiting execution
            'running',    -- Currently executing
            'completed',  -- Successfully finished
            'failed',     -- Failed after all retries
            'cancelled',  -- User/system cancelled
            'blocked'     -- Waiting on dependencies
        )),
    
    -- Priority level (1 = highest, 5 = lowest)
    priority INTEGER NOT NULL DEFAULT 3
        CONSTRAINT valid_priority CHECK (priority BETWEEN 1 AND 5),
    
    -- Array of task IDs this depends on (JSON array of strings)
    dependencies TEXT NOT NULL DEFAULT '[]'
        CONSTRAINT valid_dependencies_json CHECK (
            json_valid(dependencies) AND json_type(dependencies) = 'array'
        ),
    
    -- Array of file paths affected (JSON array of strings)
    files TEXT NOT NULL DEFAULT '[]'
        CONSTRAINT valid_files_json CHECK (
            json_valid(files) AND json_type(files) = 'array'
        ),
    
    -- Array of categorization tags (JSON array of strings)
    tags TEXT NOT NULL DEFAULT '[]'
        CONSTRAINT valid_tags_json CHECK (
            json_valid(tags) AND json_type(tags) = 'array'
        ),
    
    -- Extension metadata (JSON object)
    metadata TEXT NOT NULL DEFAULT '{}'
        CONSTRAINT valid_metadata_json CHECK (
            json_valid(metadata) AND json_type(metadata) = 'object'
        ),
    
    -- Maximum execution time in seconds
    timeout_seconds INTEGER NOT NULL DEFAULT 3600
        CONSTRAINT valid_timeout CHECK (timeout_seconds BETWEEN 1 AND 86400),
    
    -- Maximum retry attempts (0 = no retries)
    retry_limit INTEGER NOT NULL DEFAULT 3
        CONSTRAINT valid_retry_limit CHECK (retry_limit BETWEEN 0 AND 10),
    
    -- Current attempt count
    attempt_count INTEGER NOT NULL DEFAULT 0
        CONSTRAINT valid_attempt_count CHECK (attempt_count >= 0),
    
    -- Worker currently assigned (NULL if not running)
    worker_id TEXT,
    
    -- Parent task ID for subtasks (NULL if root)
    parent_id TEXT REFERENCES tasks(id) ON DELETE SET NULL,
    
    -- When task was created (ISO 8601)
    created_at TEXT NOT NULL,
    
    -- When task started executing (NULL if not started)
    started_at TEXT,
    
    -- When task completed/failed/cancelled (NULL if ongoing)
    completed_at TEXT,
    
    -- Last modification timestamp (ISO 8601)
    updated_at TEXT NOT NULL,
    
    -- Last error message (NULL if no error)
    last_error TEXT,
    
    -- Task result data (JSON, NULL if not completed)
    result TEXT
        CONSTRAINT valid_result_json CHECK (
            result IS NULL OR json_valid(result)
        ),
    
    -- Task spec format version
    spec_version INTEGER NOT NULL DEFAULT 1,
    
    -- Optimistic concurrency version
    version INTEGER NOT NULL DEFAULT 1
);

-- ------------------------------------------------------------
-- Task History Table
-- ------------------------------------------------------------
-- Append-only log of all state transitions.
-- Used for auditing, debugging, and recovery.
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS task_history (
    -- Auto-incrementing ID
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    
    -- Task this history entry belongs to
    task_id TEXT NOT NULL REFERENCES tasks(id) ON DELETE CASCADE,
    
    -- Previous status (NULL for initial creation)
    from_status TEXT
        CONSTRAINT valid_from_status CHECK (from_status IS NULL OR from_status IN (
            'pending', 'running', 'completed', 'failed', 'cancelled', 'blocked'
        )),
    
    -- New status
    to_status TEXT NOT NULL
        CONSTRAINT valid_to_status CHECK (to_status IN (
            'pending', 'running', 'completed', 'failed', 'cancelled', 'blocked'
        )),
    
    -- Who triggered this transition
    actor TEXT NOT NULL
        CONSTRAINT valid_actor CHECK (actor IN ('worker', 'user', 'system', 'scheduler')),
    
    -- Optional reason for transition
    reason TEXT,
    
    -- When this transition occurred (ISO 8601)
    timestamp TEXT NOT NULL,
    
    -- Worker ID if applicable
    worker_id TEXT,
    
    -- Additional context (JSON)
    context TEXT
        CONSTRAINT valid_context_json CHECK (
            context IS NULL OR json_valid(context)
        )
);

-- ------------------------------------------------------------
-- Indexes for Tasks Table
-- ------------------------------------------------------------

-- Primary index for dequeue operation:
-- Finds pending tasks ordered by priority, then creation time
CREATE INDEX IF NOT EXISTS idx_tasks_dequeue 
    ON tasks(status, priority ASC, created_at ASC)
    WHERE status = 'pending';

-- Status filter index
CREATE INDEX IF NOT EXISTS idx_tasks_status 
    ON tasks(status);

-- Priority sorting index
CREATE INDEX IF NOT EXISTS idx_tasks_priority 
    ON tasks(priority);

-- Created time sorting index
CREATE INDEX IF NOT EXISTS idx_tasks_created_at 
    ON tasks(created_at);

-- Worker lookup index (find tasks assigned to worker)
CREATE INDEX IF NOT EXISTS idx_tasks_worker_id 
    ON tasks(worker_id)
    WHERE worker_id IS NOT NULL;

-- Parent lookup index (find subtasks)
CREATE INDEX IF NOT EXISTS idx_tasks_parent_id 
    ON tasks(parent_id)
    WHERE parent_id IS NOT NULL;

-- Running tasks index (for recovery)
CREATE INDEX IF NOT EXISTS idx_tasks_running
    ON tasks(worker_id, started_at)
    WHERE status = 'running';

-- Blocked tasks index (for dependency resolution)
CREATE INDEX IF NOT EXISTS idx_tasks_blocked
    ON tasks(id)
    WHERE status = 'blocked';

-- ------------------------------------------------------------
-- Indexes for History Table
-- ------------------------------------------------------------

-- Task history lookup
CREATE INDEX IF NOT EXISTS idx_history_task_id 
    ON task_history(task_id);

-- Time-based queries (purging, auditing)
CREATE INDEX IF NOT EXISTS idx_history_timestamp 
    ON task_history(timestamp);

-- Actor-based queries
CREATE INDEX IF NOT EXISTS idx_history_actor 
    ON task_history(actor, timestamp);

-- ------------------------------------------------------------
-- Triggers
-- ------------------------------------------------------------

-- Auto-update updated_at on task modification
CREATE TRIGGER IF NOT EXISTS trg_tasks_updated_at
AFTER UPDATE ON tasks
BEGIN
    UPDATE tasks SET updated_at = datetime('now')
    WHERE id = NEW.id AND updated_at = OLD.updated_at;
END;

-- Prevent history modification (append-only)
CREATE TRIGGER IF NOT EXISTS trg_history_no_update
BEFORE UPDATE ON task_history
BEGIN
    SELECT RAISE(ABORT, 'Task history is append-only');
END;

-- Prevent history deletion (except via cascade)
CREATE TRIGGER IF NOT EXISTS trg_history_no_delete
BEFORE DELETE ON task_history
WHEN OLD.task_id IN (SELECT id FROM tasks)
BEGIN
    SELECT RAISE(ABORT, 'Cannot delete history for existing task');
END;

-- ------------------------------------------------------------
-- Views
-- ------------------------------------------------------------

-- Current queue status summary
CREATE VIEW IF NOT EXISTS v_queue_summary AS
SELECT 
    status,
    COUNT(*) as count,
    MIN(priority) as min_priority,
    MAX(priority) as max_priority,
    MIN(created_at) as oldest,
    MAX(created_at) as newest
FROM tasks
GROUP BY status;

-- Tasks ready to run (pending, no blocked dependencies)
CREATE VIEW IF NOT EXISTS v_ready_tasks AS
SELECT t.*
FROM tasks t
WHERE t.status = 'pending'
  AND NOT EXISTS (
    SELECT 1 
    FROM json_each(t.dependencies) AS dep
    JOIN tasks dt ON dt.id = dep.value
    WHERE dt.status NOT IN ('completed')
  )
ORDER BY t.priority ASC, t.created_at ASC;
```

### Schema Migrator

```csharp
// Acode.Infrastructure/Persistence/TaskQueue/ISchemaMigrator.cs
namespace Acode.Infrastructure.Persistence.TaskQueue;

/// <summary>
/// Manages database schema migrations.
/// </summary>
public interface ISchemaMigrator
{
    /// <summary>
    /// Gets the current schema version.
    /// </summary>
    Task<int> GetCurrentVersionAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Applies all pending migrations.
    /// </summary>
    Task<MigrationResult> MigrateAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Validates schema integrity.
    /// </summary>
    Task<SchemaValidationResult> ValidateAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Creates a backup before migration.
    /// </summary>
    Task<string> BackupAsync(CancellationToken ct = default);
}

public sealed record MigrationResult
{
    public bool Success { get; init; }
    public int FromVersion { get; init; }
    public int ToVersion { get; init; }
    public TimeSpan Duration { get; init; }
    public IReadOnlyList<string> AppliedMigrations { get; init; } = Array.Empty<string>();
    public string? Error { get; init; }
}

public sealed record SchemaValidationResult
{
    public bool IsValid { get; init; }
    public bool HashMatches { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

// Acode.Infrastructure/Persistence/TaskQueue/SchemaMigrator.cs
namespace Acode.Infrastructure.Persistence.TaskQueue;

public sealed class SchemaMigrator : ISchemaMigrator
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<SchemaMigrator> _logger;
    
    private static readonly Dictionary<int, string> Migrations = new()
    {
        [1] = "V001__InitialSchema.sql",
        [2] = "V002__AddIndexes.sql",
        [3] = "V003__AddResultColumn.sql"
    };
    
    public SchemaMigrator(IDbConnectionFactory connectionFactory, ILogger<SchemaMigrator> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }
    
    public async Task<int> GetCurrentVersionAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateAsync(ct);
        
        // Check if schema_version exists
        var tableExists = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='schema_version'");
        
        if (tableExists == 0)
            return 0;
        
        return await conn.ExecuteScalarAsync<int>(
            "SELECT version FROM schema_version WHERE rowid = 1");
    }
    
    public async Task<MigrationResult> MigrateAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var currentVersion = await GetCurrentVersionAsync(ct);
        var targetVersion = Migrations.Keys.Max();
        
        if (currentVersion >= targetVersion)
        {
            return new MigrationResult
            {
                Success = true,
                FromVersion = currentVersion,
                ToVersion = currentVersion,
                Duration = sw.Elapsed
            };
        }
        
        _logger.LogInformation("Migrating schema from v{From} to v{To}", currentVersion, targetVersion);
        
        var applied = new List<string>();
        
        using var conn = await _connectionFactory.CreateAsync(ct);
        using var tx = await conn.BeginTransactionAsync(ct);
        
        try
        {
            for (var v = currentVersion + 1; v <= targetVersion; v++)
            {
                if (!Migrations.TryGetValue(v, out var migrationFile))
                    continue;
                
                _logger.LogInformation("Applying migration: {Migration}", migrationFile);
                
                var sql = await LoadMigrationScriptAsync(migrationFile);
                await conn.ExecuteAsync(sql, transaction: tx);
                
                // Update version
                await conn.ExecuteAsync(
                    "UPDATE schema_version SET version = @version, applied_at = datetime('now')",
                    new { version = v },
                    transaction: tx);
                
                applied.Add(migrationFile);
            }
            
            await tx.CommitAsync(ct);
            
            return new MigrationResult
            {
                Success = true,
                FromVersion = currentVersion,
                ToVersion = targetVersion,
                Duration = sw.Elapsed,
                AppliedMigrations = applied
            };
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "Migration failed");
            
            return new MigrationResult
            {
                Success = false,
                FromVersion = currentVersion,
                ToVersion = currentVersion,
                Duration = sw.Elapsed,
                AppliedMigrations = applied,
                Error = ex.Message
            };
        }
    }
    
    public async Task<SchemaValidationResult> ValidateAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateAsync(ct);
        var warnings = new List<string>();
        
        // Check required tables exist
        var requiredTables = new[] { "tasks", "task_history", "schema_version" };
        foreach (var table in requiredTables)
        {
            var exists = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@table",
                new { table });
            
            if (exists == 0)
            {
                return new SchemaValidationResult
                {
                    IsValid = false,
                    Warnings = new[] { $"Missing table: {table}" }
                };
            }
        }
        
        // Check required indexes
        var requiredIndexes = new[] { "idx_tasks_dequeue", "idx_tasks_status", "idx_history_task_id" };
        foreach (var index in requiredIndexes)
        {
            var exists = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM sqlite_master WHERE type='index' AND name=@index",
                new { index });
            
            if (exists == 0)
                warnings.Add($"Missing index: {index}");
        }
        
        // Verify schema hash (optional)
        var storedHash = await conn.ExecuteScalarAsync<string>(
            "SELECT hash FROM schema_version WHERE rowid = 1");
        
        var currentHash = ComputeSchemaHash(conn);
        var hashMatches = storedHash == currentHash;
        
        if (!hashMatches)
            warnings.Add("Schema hash mismatch - schema may have been modified manually");
        
        return new SchemaValidationResult
        {
            IsValid = true,
            HashMatches = hashMatches,
            Warnings = warnings
        };
    }
    
    public async Task<string> BackupAsync(CancellationToken ct = default)
    {
        var dbPath = _connectionFactory.DatabasePath;
        var backupPath = $"{dbPath}.backup.{DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
        
        File.Copy(dbPath, backupPath);
        _logger.LogInformation("Created backup: {Path}", backupPath);
        
        return backupPath;
    }
    
    private static async Task<string> LoadMigrationScriptAsync(string filename)
    {
        var assembly = typeof(SchemaMigrator).Assembly;
        var resourceName = $"Acode.Infrastructure.Persistence.TaskQueue.Migrations.{filename}";
        
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new InvalidOperationException($"Migration not found: {resourceName}");
        
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
    
    private static string ComputeSchemaHash(IDbConnection conn)
    {
        // Get schema DDL and compute hash
        var tables = conn.Query<string>(
            "SELECT sql FROM sqlite_master WHERE type IN ('table', 'index', 'trigger', 'view') ORDER BY name");
        
        var combined = string.Join("\n", tables);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(hash);
    }
}
```

### Query Examples with EXPLAIN

```sql
-- Dequeue query (should use idx_tasks_dequeue)
EXPLAIN QUERY PLAN
SELECT * FROM tasks
WHERE status = 'pending'
ORDER BY priority ASC, created_at ASC
LIMIT 1;
-- Expected: SEARCH tasks USING INDEX idx_tasks_dequeue

-- Worker task lookup (should use idx_tasks_worker_id)
EXPLAIN QUERY PLAN
SELECT * FROM tasks WHERE worker_id = ?;
-- Expected: SEARCH tasks USING INDEX idx_tasks_worker_id

-- History for task (should use idx_history_task_id)
EXPLAIN QUERY PLAN
SELECT * FROM task_history WHERE task_id = ? ORDER BY timestamp;
-- Expected: SEARCH task_history USING INDEX idx_history_task_id
```

### Implementation Checklist

- [ ] Create V001__InitialSchema.sql with tasks table
- [ ] Add task_history table
- [ ] Add schema_version table
- [ ] Add all CHECK constraints
- [ ] Add all foreign key constraints
- [ ] Create idx_tasks_dequeue partial index
- [ ] Create idx_tasks_status index
- [ ] Create idx_tasks_worker_id partial index
- [ ] Create idx_history_task_id index
- [ ] Create idx_history_timestamp index
- [ ] Add updated_at trigger
- [ ] Add history append-only triggers
- [ ] Create v_queue_summary view
- [ ] Create v_ready_tasks view
- [ ] Define `ISchemaMigrator` interface
- [ ] Implement `SchemaMigrator`
- [ ] Add migration file loading
- [ ] Add schema hash validation
- [ ] Add backup functionality
- [ ] Write EXPLAIN QUERY PLAN tests
- [ ] Write constraint violation tests
- [ ] Document all columns

### Rollout Plan

1. **Phase 1: Core Tables** (Day 1)
   - tasks table with constraints
   - task_history table
   - schema_version table

2. **Phase 2: Indexes** (Day 1)
   - All primary indexes
   - Partial indexes for dequeue
   - Verify with EXPLAIN

3. **Phase 3: Triggers/Views** (Day 2)
   - updated_at trigger
   - Append-only triggers
   - Summary views

4. **Phase 4: Migrator** (Day 2)
   - Migration loading
   - Version tracking
   - Transaction handling

5. **Phase 5: Validation** (Day 3)
   - Query plan tests
   - Constraint tests
   - Documentation

---

**End of Task 026.a Specification**