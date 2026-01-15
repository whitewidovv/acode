# Task-011b Completion Checklist: Persistence Model (SQLite + PostgreSQL)

**Status:** Implementation Roadmap (8 Phases)
**Date:** 2026-01-15
**Total Estimated Effort:** 28-40 hours
**Methodology:** TDD (Red → Green → Refactor) with verification at each phase

---

## INSTRUCTIONS FOR IMPLEMENTATION AGENT

This checklist guides implementation from 0% (0/59 ACs) to 100% (59/59 ACs verified) completion.

**Critical Rules:**
1. **Work through phases SEQUENTIALLY** - Each phase depends on previous
2. **TDD mandatory** - Write tests FIRST (RED), then implementation (GREEN)
3. **Mark each item** when complete: [🔄] starting → [✅] completed
4. **Commit after each phase** with evidence (test output, line counts)
5. **Run verification checks** at end of each phase
6. **Never skip sections** - All code examples in spec must be implemented

**Success Criteria for Task Complete:**
- All 8 phases marked [✅]
- 59/59 ACs verified implemented
- 24+ test methods passing (100% pass rate)
- Build: 0 errors, 0 warnings
- All commits pushed to feature branch

---

# PHASE 1: APPLICATION LAYER INTERFACES (1-2 hours)

## Gap 1.1: Create IRunStateStore Interface

**Current State:** ❌ MISSING
**Spec Reference:** Implementation Prompt lines 2054-2066
**What Exists:** Nothing - interface must be created from scratch

**What's Missing:** Complete interface definition with all methods

**Implementation Details from Spec (lines 2054-2066):**
```csharp
namespace Acode.Application.Sessions;

public interface IRunStateStore
{
    Task<Session?> GetAsync(SessionId id, CancellationToken ct);
    Task SaveAsync(Session session, CancellationToken ct);
    Task<IReadOnlyList<Session>> ListAsync(SessionFilter filter, CancellationToken ct);
    Task<IReadOnlyList<SessionEvent>> GetEventsAsync(SessionId id, CancellationToken ct);
    Task<Session?> GetWithHierarchyAsync(SessionId id, CancellationToken ct);
}
```

**Acceptance Criteria Covered:** AC-001 through AC-056 (all persistence ACs depend on this)

**Test Requirements:**
- [ ] Should_Define_Required_Interface_Contract - Verify all 5 methods present
- [ ] Should_Support_Session_Retrieval - GetAsync returns correct Session
- [ ] Should_Support_Session_Persistence - SaveAsync persists without error
- [ ] Should_Support_Filtering - ListAsync respects filter criteria

**Success Criteria:**
- [ ] File created at `src/Acode.Application/Sessions/IRunStateStore.cs`
- [ ] All 5 methods defined with correct signatures
- [ ] No NotImplementedException
- [ ] Build succeeds (0 errors, 0 warnings)
- [ ] Tests passing (verify interface can be mocked)

**Gap Checklist Item:** [ ] 🔄 IRunStateStore interface complete with tests passing

---

## Gap 1.2: Create IOutbox Interface

**Current State:** ❌ MISSING
**Spec Reference:** Implementation Prompt lines 2069-2094
**What Exists:** IOutboxRepository exists but spec requires IOutbox interface

**What's Missing:** IOutbox interface and OutboxRecord record as defined in spec

**Implementation Details from Spec (lines 2069-2094):**
```csharp
namespace Acode.Application.Sessions;

public interface IOutbox
{
    Task EnqueueAsync(OutboxRecord record, CancellationToken ct);
    Task<IReadOnlyList<OutboxRecord>> GetPendingAsync(int limit, CancellationToken ct);
    Task MarkProcessedAsync(long id, CancellationToken ct);
    Task MarkFailedAsync(long id, string error, CancellationToken ct);
    Task<int> GetPendingCountAsync(CancellationToken ct);
}

public sealed record OutboxRecord(
    long Id,
    string IdempotencyKey,
    string EntityType,
    string EntityId,
    string Operation,
    JsonDocument Payload,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt,
    int Attempts,
    string? LastError);
```

**Acceptance Criteria Covered:** AC-029 through AC-047 (outbox and idempotency ACs)

**Test Requirements:**
- [ ] Should_Enqueue_Record - EnqueueAsync stores record
- [ ] Should_Get_Pending_Records - GetPendingAsync returns unprocessed
- [ ] Should_Mark_Processed - MarkProcessedAsync sets processed_at
- [ ] Should_Count_Pending - GetPendingCountAsync returns correct count

**Success Criteria:**
- [ ] File created at `src/Acode.Application/Sessions/IOutbox.cs`
- [ ] Interface has all 5 methods with correct signatures
- [ ] OutboxRecord record defined with all 10 properties
- [ ] All properties immutable (record type)
- [ ] Tests passing
- [ ] Build succeeds

**Gap Checklist Item:** [ ] 🔄 IOutbox interface and OutboxRecord complete with tests passing

---

## Phase 1 Verification Checklist

- [ ] IRunStateStore.cs exists (20 lines)
- [ ] IOutbox.cs exists (30 lines)
- [ ] OutboxRecord defined with all 10 properties
- [ ] No NotImplementedException in either file
- [ ] Build: 0 errors, 0 warnings
- [ ] Test file created: InterfaceContractTests.cs
- [ ] All interface tests passing (8+ methods)
- [ ] Commit pushed with message: "feat(task-011b-p1): create IRunStateStore and IOutbox interfaces"

**Phase 1 Status:** [ ] 🔄 IN PROGRESS → [ ] ✅ COMPLETE

---

# PHASE 2: SQLITE PERSISTENCE LAYER (6-8 hours)

## Gap 2.1: Create SQLiteRunStateStore Implementation

**Current State:** ❌ MISSING
**Spec Reference:** Implementation Prompt, Security Considerations (lines 792-829 show database usage patterns)
**What Exists:** Nothing - must implement IRunStateStore for SQLite

**What's Missing:** Full CRUD implementation for sessions using SQLite

**Implementation Details from Spec Pattern (Security section shows parameterized queries):**

Key implementation requirements from spec:
- Use Entity Framework Core or direct parameterized queries (never string concatenation)
- Implement all 5 methods from IRunStateStore interface
- Handle transactions (AC-005, AC-006)
- Respect WAL mode and STRICT tables (AC-002, AC-003)
- Support Session, Task, Step, ToolCall, Artifact hierarchies

**Example Implementation Structure:**
```csharp
namespace Acode.Infrastructure.Persistence.SQLite;

public sealed class SqliteRunStateStore : IRunStateStore
{
    private readonly AcodeDbContext _context;
    private readonly ILogger<SqliteRunStateStore> _logger;

    public SqliteRunStateStore(AcodeDbContext context, ILogger<SqliteRunStateStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Session?> GetAsync(SessionId id, CancellationToken ct)
    {
        _logger.LogInformation("Getting session {SessionId}", id.Value);
        var entity = await _context.Sessions
            .FirstOrDefaultAsync(s => s.Id == id.Value, ct);

        return entity?.MapToDomain();
    }

    public async Task SaveAsync(Session session, CancellationToken ct)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var entity = new SessionEntity
            {
                Id = session.Id.Value,
                TaskDescription = session.TaskDescription,
                State = session.State.ToString(),
                CreatedAt = session.CreatedAt,
                UpdatedAt = session.UpdatedAt
            };

            var existing = await _context.Sessions
                .FirstOrDefaultAsync(s => s.Id == entity.Id, ct);

            if (existing != null)
                _context.Sessions.Update(entity);
            else
                _context.Sessions.Add(entity);

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            _logger.LogInformation("Saved session {SessionId}", session.Id.Value);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to save session {SessionId}", session.Id.Value);
            throw;
        }
    }

    // ... other methods follow same pattern
}
```

**Acceptance Criteria Covered:** AC-001, AC-004, AC-005, AC-006, AC-007, AC-008

**Test Requirements (SessionRepositoryTests.cs):**
- [ ] Should_Create_Session - SaveAsync creates new session
- [ ] Should_Update_Session - SaveAsync updates existing session
- [ ] Should_Query_By_Id - GetAsync returns correct session
- [ ] Should_List_With_Filter - ListAsync respects filters

**Success Criteria:**
- [ ] File created at `src/Acode.Infrastructure/Persistence/SQLite/SqliteRunStateStore.cs` (~100 lines)
- [ ] Implements all 5 methods from IRunStateStore
- [ ] No NotImplementedException
- [ ] Uses parameterized queries (prevents SQL injection)
- [ ] Handles transactions properly (AC-005)
- [ ] Tests: 4/4 passing
- [ ] Build succeeds

**Gap Checklist Item:** [ ] 🔄 SqliteRunStateStore complete with CRUD operations and tests passing

---

## Gap 2.2: Create SQLiteOutbox Implementation

**Current State:** ⚠️ PARTIAL - SqliteOutboxRepository exists but not per spec for IOutbox
**Spec Reference:** Implementation Prompt, Outbox section, Security section (idempotency pattern)
**What Exists:** SqliteOutboxRepository (partial implementation)

**What's Missing:** Full IOutbox implementation with idempotency, retry logic, attempted counting

**Implementation Details from Spec:**

Key requirements:
- Implement all 5 IOutbox methods
- Generate unique idempotency keys: `{entity_type}:{entity_id}:{timestamp_utc}` (AC-044)
- Track attempts count (AC-035, AC-041)
- Support mark failed with error message (AC-035)
- Prevent duplicates via idempotency key (AC-045, AC-046)

**Example Implementation Structure:**
```csharp
namespace Acode.Infrastructure.Persistence.SQLite;

public sealed class SqliteOutbox : IOutbox
{
    private readonly AcodeDbContext _context;
    private readonly ILogger<SqliteOutbox> _logger;

    public async Task EnqueueAsync(OutboxRecord record, CancellationToken ct)
    {
        _logger.LogInformation("Enqueueing outbox record {IdempotencyKey}", record.IdempotencyKey);

        var entity = new OutboxEntity
        {
            IdempotencyKey = record.IdempotencyKey,
            EntityType = record.EntityType,
            EntityId = record.EntityId,
            Operation = record.Operation,
            Payload = record.Payload.RootElement.GetRawText(),
            CreatedAt = record.CreatedAt,
            ProcessedAt = record.ProcessedAt,
            Attempts = record.Attempts,
            LastError = record.LastError
        };

        _context.OutboxRecords.Add(entity);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<OutboxRecord>> GetPendingAsync(int limit, CancellationToken ct)
    {
        var records = await _context.OutboxRecords
            .Where(r => r.ProcessedAt == null)
            .OrderBy(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

        return records.Select(MapToDomain).ToList();
    }

    public async Task MarkProcessedAsync(long id, CancellationToken ct)
    {
        var record = await _context.OutboxRecords
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (record != null)
        {
            record.ProcessedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Marked outbox record {Id} as processed", id);
        }
    }

    // ... other methods follow similar pattern
}
```

**Acceptance Criteria Covered:** AC-029 through AC-035, AC-043 through AC-047

**Test Requirements (OutboxTests.cs):**
- [ ] Should_Add_Record - EnqueueAsync stores record
- [ ] Should_Generate_Idempotency_Key - Format matches spec
- [ ] Should_Mark_Processed - ProcessedAt timestamp set

**Success Criteria:**
- [ ] File created at `src/Acode.Infrastructure/Persistence/SQLite/SqliteOutbox.cs` (~80 lines)
- [ ] Implements all 5 IOutbox methods
- [ ] Idempotency key generation correct
- [ ] Attempts counter working
- [ ] Tests: 3/3 passing
- [ ] Build succeeds

**Gap Checklist Item:** [ ] 🔄 SqliteOutbox complete with idempotency and tests passing

---

## Phase 2 Verification Checklist

- [ ] SqliteRunStateStore.cs exists (~100 lines)
- [ ] SqliteOutbox.cs exists (~80 lines)
- [ ] Both implement required interfaces
- [ ] No NotImplementedException in either
- [ ] SessionRepositoryTests.cs created (4 test methods)
- [ ] OutboxTests.cs created (3 test methods)
- [ ] All 7 tests passing (100%)
- [ ] Parameterized queries used (no SQL injection)
- [ ] Transactions properly handled
- [ ] Build: 0 errors, 0 warnings
- [ ] Commit pushed with message: "feat(task-011b-p2): implement SQLite persistence layer with CRUD and outbox"

**Phase 2 Status:** [ ] 🔄 IN PROGRESS → [ ] ✅ COMPLETE

---

# PHASE 3: POSTGRESQL PERSISTENCE LAYER (5-6 hours)

## Gap 3.1: Create PostgresRunStateStore Implementation

**Current State:** ❌ MISSING
**Spec Reference:** Implementation Prompt lines 2037-2040, PostgreSQL section (lines 498-504)
**What Exists:** PostgresConnectionFactory exists (connection management)

**What's Missing:** PostgreSQL implementation of IRunStateStore

**Implementation Details:**

Key requirements from spec:
- Mirror SQLite schema structure (AC-026)
- Implement all 5 IRunStateStore methods
- Use Entity Framework Core or Npgsql with parameterized queries
- Optimize with indexes (AC-027)
- Support connection pooling (AC-028)

**Implementation Pattern:**
```csharp
namespace Acode.Infrastructure.Persistence.PostgreSQL;

public sealed class PostgresRunStateStore : IRunStateStore
{
    private readonly PostgresDbContext _context;
    private readonly ILogger<PostgresRunStateStore> _logger;

    public PostgresRunStateStore(PostgresDbContext context, ILogger<PostgresRunStateStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Session?> GetAsync(SessionId id, CancellationToken ct)
    {
        var entity = await _context.Sessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id.Value, ct);

        return entity?.MapToDomain();
    }

    public async Task SaveAsync(Session session, CancellationToken ct)
    {
        var entity = new SessionEntity
        {
            Id = session.Id.Value,
            TaskDescription = session.TaskDescription,
            State = session.State.ToString(),
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            SyncVersion = 0  // Track for conflict resolution
        };

        var existing = await _context.Sessions
            .FirstOrDefaultAsync(s => s.Id == entity.Id, ct);

        if (existing != null)
        {
            existing.State = entity.State;
            existing.UpdatedAt = entity.UpdatedAt;
            existing.SyncVersion++;
            _context.Sessions.Update(existing);
        }
        else
        {
            _context.Sessions.Add(entity);
        }

        await _context.SaveChangesAsync(ct);
    }

    // ... remaining methods follow same pattern
}
```

**Acceptance Criteria Covered:** AC-023 through AC-028, AC-048 through AC-052 (conflict resolution)

**Test Requirements (PostgreSQLIntegrationTests.cs):**
- [ ] Should_Sync_From_Outbox - PostgreSQL receives outbox data
- [ ] Should_Handle_Network_Failure - Retry logic works
- [ ] Should_Resolve_Conflicts - Latest timestamp wins

**Success Criteria:**
- [ ] File created at `src/Acode.Infrastructure/Persistence/PostgreSQL/PostgresRunStateStore.cs` (~110 lines)
- [ ] Implements all 5 IRunStateStore methods
- [ ] Tracks SyncVersion for conflict detection
- [ ] Uses parameterized queries
- [ ] Connection pooling configured
- [ ] Tests: 3/3 passing
- [ ] Build succeeds

**Gap Checklist Item:** [ ] 🔄 PostgresRunStateStore complete with sync support

---

## Gap 3.2: Create PostgresSyncTarget Implementation

**Current State:** ❌ MISSING
**Spec Reference:** Sync section lines 40-22
**What Exists:** None

**What's Missing:** Sync target that receives data from outbox

**Implementation Details:**

Minimal implementation needed to receive sync data:
```csharp
namespace Acode.Infrastructure.Persistence.PostgreSQL;

public sealed class PostgresSyncTarget
{
    private readonly PostgresDbContext _context;
    private readonly ILogger<PostgresSyncTarget> _logger;

    public async Task ApplySyncAsync(OutboxRecord record, CancellationToken ct)
    {
        // Convert record payload back to domain entity
        // Apply to PostgreSQL
        // Log success
    }

    public async Task HandleConflictAsync(Session existing, Session incoming, CancellationToken ct)
    {
        // Latest timestamp wins (AC-049)
        if (incoming.UpdatedAt > existing.UpdatedAt)
        {
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Conflict resolved: {SessionId} - incoming won", incoming.Id.Value);
        }
    }
}
```

**Acceptance Criteria Covered:** AC-048 through AC-052

**Test Requirements:** Covered by PostgreSQLIntegrationTests

**Success Criteria:**
- [ ] File created at `src/Acode.Infrastructure/Persistence/PostgreSQL/PostgresSyncTarget.cs` (~90 lines)
- [ ] ApplySyncAsync method implemented
- [ ] Conflict resolution implemented
- [ ] Tests passing via integration tests
- [ ] Build succeeds

**Gap Checklist Item:** [ ] 🔄 PostgresSyncTarget complete with conflict handling

---

## Phase 3 Verification Checklist

- [ ] PostgresRunStateStore.cs exists (~110 lines)
- [ ] PostgresSyncTarget.cs exists (~90 lines)
- [ ] Both implement expected interfaces/contracts
- [ ] SyncVersion tracking for conflicts
- [ ] PostgreSQLIntegrationTests.cs created (3 test methods)
- [ ] All tests passing (100%)
- [ ] Parameterized queries used
- [ ] Build: 0 errors, 0 warnings
- [ ] Commit pushed with message: "feat(task-011b-p3): implement PostgreSQL persistence layer with sync target"

**Phase 3 Status:** [ ] 🔄 IN PROGRESS → [ ] ✅ COMPLETE

---

# PHASE 4: MIGRATION INFRASTRUCTURE (3-4 hours)

## Gap 4.1: Create IMigration Interface

**Current State:** ❌ MISSING
**Spec Reference:** Implementation Prompt lines 2122-2127
**What Exists:** None

**What's Missing:** Interface for defining database migrations

**Implementation Details from Spec:**
```csharp
namespace Acode.Infrastructure.Persistence.SQLite.Migrations;

public interface IMigration
{
    string Version { get; }
    string Description { get; }
    Task UpAsync(SqliteConnection connection, CancellationToken ct);
}
```

**Acceptance Criteria Covered:** AC-009 through AC-014

**Test Requirements:** Covered by MigrationRunnerTests

**Success Criteria:**
- [ ] File created at `src/Acode.Infrastructure/Persistence/SQLite/Migrations/IMigration.cs` (~15 lines)
- [ ] All 3 properties defined correctly
- [ ] Build succeeds

**Gap Checklist Item:** [ ] 🔄 IMigration interface created

---

## Gap 4.2: Create MigrationRunner

**Current State:** ❌ MISSING
**Spec Reference:** Schema management section (lines 475-484)
**What Exists:** Existing MigrationRunner in codebase but may need enhancement

**What's Missing:** Migration orchestration system that:
- Runs migrations in order (AC-011)
- Is idempotent (AC-012)
- Tracks version (AC-009)
- Halts startup on failure (AC-013)

**Implementation Pattern:**
```csharp
namespace Acode.Infrastructure.Persistence.SQLite.Migrations;

public sealed class MigrationRunner
{
    private readonly SqliteConnection _connection;
    private readonly ILogger<MigrationRunner> _logger;
    private readonly IReadOnlyList<IMigration> _migrations;

    public async Task RunMigrationsAsync(CancellationToken ct)
    {
        await _connection.OpenAsync(ct);

        // Get current schema version
        var currentVersion = await GetCurrentVersionAsync(_connection, ct);
        _logger.LogInformation("Current schema version: {Version}", currentVersion);

        // Run migrations in order
        foreach (var migration in _migrations)
        {
            if (ComparableVersion(migration.Version) > ComparableVersion(currentVersion))
            {
                _logger.LogInformation("Running migration {Version}: {Description}",
                    migration.Version, migration.Description);

                try
                {
                    await migration.UpAsync(_connection, ct);
                    await UpdateVersionAsync(_connection, migration.Version, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Migration {Version} failed - halting startup", migration.Version);
                    throw;
                }
            }
        }

        await _connection.CloseAsync();
    }

    private async Task<string> GetCurrentVersionAsync(SqliteConnection conn, CancellationToken ct)
    {
        try
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT version FROM schema_version ORDER BY applied_at DESC LIMIT 1";
            var result = await cmd.ExecuteScalarAsync(ct);
            return result?.ToString() ?? "0.0.0";
        }
        catch
        {
            return "0.0.0";  // Table doesn't exist yet
        }
    }

    private async Task UpdateVersionAsync(SqliteConnection conn, string version, CancellationToken ct)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO schema_version (version, applied_at)
            VALUES (@version, @applied_at)
            """;
        cmd.Parameters.AddWithValue("@version", version);
        cmd.Parameters.AddWithValue("@applied_at", DateTimeOffset.UtcNow);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
```

**Acceptance Criteria Covered:** AC-009 through AC-014

**Test Requirements (MigrationRunnerTests.cs):**
- [ ] Should_Run_Migrations_In_Order - Migrations execute in sequence
- [ ] Should_Be_Idempotent - Re-running same migration safe
- [ ] Should_Halt_On_Failure - Exception on migration failure

**Success Criteria:**
- [ ] File created/updated at `src/Acode.Infrastructure/Persistence/SQLite/Migrations/MigrationRunner.cs` (~120 lines)
- [ ] All 3 test methods passing
- [ ] Migrations run in order
- [ ] Version tracking works
- [ ] Build succeeds

**Gap Checklist Item:** [ ] 🔄 MigrationRunner complete with version tracking

---

## Gap 4.3: Create Initial Schema Migration V1.0.0

**Current State:** ❌ MISSING
**Spec Reference:** User Manual Documentation section, database schema definitions (lines 1584-1699)
**What Exists:** None

**What's Missing:** Initial schema migration creating all required tables with proper structure

**Implementation Details:**

Must create tables per spec:
- sessions (AC-015)
- session_events (AC-016)
- session_tasks (AC-017)
- steps (AC-018)
- tool_calls (AC-019)
- artifacts (AC-020)
- outbox (AC-021)
- schema_version (tracking)

With:
- STRICT mode for SQLite (AC-003)
- Proper constraints and foreign keys
- Timestamps on all tables (AC-022)
- Indexes for performance (AC-053 through AC-056)

**Schema Structure from Spec (lines 1589-1699):**

```csharp
public sealed class V1_0_0_InitialSchema : IMigration
{
    public string Version => "1.0.0";
    public string Description => "Create initial database schema";

    public async Task UpAsync(SqliteConnection connection, CancellationToken ct)
    {
        using var cmd = connection.CreateCommand();

        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS sessions (
                id TEXT PRIMARY KEY,
                task_description TEXT NOT NULL,
                state TEXT NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                metadata TEXT,
                sync_version INTEGER DEFAULT 0
            ) STRICT;

            CREATE TABLE IF NOT EXISTS session_events (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                session_id TEXT NOT NULL REFERENCES sessions(id),
                from_state TEXT NOT NULL,
                to_state TEXT NOT NULL,
                reason TEXT NOT NULL,
                timestamp TEXT NOT NULL
            ) STRICT;

            CREATE TABLE IF NOT EXISTS session_tasks (
                id TEXT PRIMARY KEY,
                session_id TEXT NOT NULL REFERENCES sessions(id),
                title TEXT NOT NULL,
                description TEXT,
                state TEXT NOT NULL,
                "order" INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                metadata TEXT,
                sync_version INTEGER DEFAULT 0
            ) STRICT;

            CREATE TABLE IF NOT EXISTS steps (
                id TEXT PRIMARY KEY,
                task_id TEXT NOT NULL REFERENCES session_tasks(id),
                name TEXT NOT NULL,
                description TEXT,
                state TEXT NOT NULL,
                "order" INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                metadata TEXT,
                sync_version INTEGER DEFAULT 0
            ) STRICT;

            CREATE TABLE IF NOT EXISTS tool_calls (
                id TEXT PRIMARY KEY,
                step_id TEXT NOT NULL REFERENCES steps(id),
                tool_name TEXT NOT NULL,
                parameters TEXT NOT NULL,
                state TEXT NOT NULL,
                "order" INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                completed_at TEXT,
                result TEXT,
                error_message TEXT,
                sync_version INTEGER DEFAULT 0
            ) STRICT;

            CREATE TABLE IF NOT EXISTS artifacts (
                id TEXT PRIMARY KEY,
                tool_call_id TEXT NOT NULL REFERENCES tool_calls(id),
                type TEXT NOT NULL,
                name TEXT NOT NULL,
                content BLOB NOT NULL,
                content_hash TEXT NOT NULL,
                content_type TEXT NOT NULL,
                size INTEGER NOT NULL,
                created_at TEXT NOT NULL
            ) STRICT;

            CREATE TABLE IF NOT EXISTS outbox (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                idempotency_key TEXT UNIQUE NOT NULL,
                entity_type TEXT NOT NULL,
                entity_id TEXT NOT NULL,
                operation TEXT NOT NULL,
                payload TEXT NOT NULL,
                created_at TEXT NOT NULL,
                processed_at TEXT,
                attempts INTEGER DEFAULT 0,
                last_error TEXT
            ) STRICT;

            CREATE TABLE IF NOT EXISTS schema_version (
                version TEXT NOT NULL,
                applied_at TEXT NOT NULL
            ) STRICT;

            CREATE INDEX idx_sessions_state ON sessions(state);
            CREATE INDEX idx_sessions_created_at ON sessions(created_at);
            CREATE INDEX idx_outbox_pending ON outbox(processed_at) WHERE processed_at IS NULL;
            CREATE INDEX idx_session_tasks_session_id ON session_tasks(session_id);
            CREATE INDEX idx_steps_task_id ON steps(task_id);
            CREATE INDEX idx_tool_calls_step_id ON tool_calls(step_id);
            CREATE INDEX idx_artifacts_tool_call_id ON artifacts(tool_call_id);
            """;

        await cmd.ExecuteNonQueryAsync(ct);
    }
}
```

**Acceptance Criteria Covered:** AC-015 through AC-022, AC-053 through AC-056

**Test Requirements:** Covered by MigrationIntegrationTests

**Success Criteria:**
- [ ] File created at `src/Acode.Infrastructure/Persistence/SQLite/Migrations/V1_0_0_InitialSchema.cs` (~150 lines)
- [ ] All 7 tables created with STRICT mode
- [ ] All foreign keys present
- [ ] All indexes created
- [ ] Migration runs successfully
- [ ] Integration tests verify schema created
- [ ] Build succeeds

**Gap Checklist Item:** [ ] 🔄 V1_0_0_InitialSchema migration complete

---

## Gap 4.4: Create Artifacts Migration V1.1.0

**Current State:** ❌ MISSING
**Spec Reference:** Migration pattern (would be run if artifacts table schema changes)
**What Exists:** None - but included in spec for completeness

**What's Missing:** Example migration for future schema changes

**Implementation Pattern:**
```csharp
public sealed class V1_1_0_AddArtifacts : IMigration
{
    public string Version => "1.1.0";
    public string Description => "Add artifacts table enhancements";

    public async Task UpAsync(SqliteConnection connection, CancellationToken ct)
    {
        // Example migration - add column if not exists
        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            ALTER TABLE artifacts ADD COLUMN IF NOT EXISTS metadata TEXT;
            CREATE INDEX IF NOT EXISTS idx_artifacts_type ON artifacts(type);
            """;
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
```

**Acceptance Criteria Covered:** AC-010 through AC-012 (demonstrates idempotent migrations)

**Test Requirements:** Covered by MigrationIntegrationTests

**Success Criteria:**
- [ ] File created at `src/Acode.Infrastructure/Persistence/SQLite/Migrations/V1_1_0_AddArtifacts.cs` (~50 lines)
- [ ] Uses idempotent SQL (IF NOT EXISTS, IF NOT FOUND)
- [ ] Build succeeds

**Gap Checklist Item:** [ ] 🔄 V1_1_0_AddArtifacts migration complete

---

## Phase 4 Verification Checklist

- [ ] IMigration.cs interface created (~15 lines)
- [ ] MigrationRunner.cs updated/created (~120 lines)
- [ ] V1_0_0_InitialSchema.cs created (~150 lines)
- [ ] V1_1_0_AddArtifacts.cs created (~50 lines)
- [ ] MigrationRunnerTests.cs created (3 test methods)
- [ ] MigrationIntegrationTests.cs created (2 test methods)
- [ ] All 5 tests passing (100%)
- [ ] Migrations run in order
- [ ] Schema version tracking works
- [ ] All tables created with STRICT mode
- [ ] All indexes present
- [ ] Build: 0 errors, 0 warnings
- [ ] Commit pushed with message: "feat(task-011b-p4): implement migration system with initial and artifacts schemas"

**Phase 4 Status:** [ ] 🔄 IN PROGRESS → [ ] ✅ COMPLETE

---

# PHASE 5: SYNC SERVICE CORE (6-8 hours)

## Gap 5.1: Create ISyncService Interface

**Current State:** ❌ MISSING
**Spec Reference:** Implementation Prompt lines 2100-2114
**What Exists:** None

**What's Missing:** Interface for sync service orchestration

**Implementation Details from Spec:**
```csharp
namespace Acode.Infrastructure.Persistence.Sync;

public interface ISyncService
{
    Task StartAsync(CancellationToken ct);
    Task StopAsync(CancellationToken ct);
    Task SyncNowAsync(CancellationToken ct);
    SyncStatus GetStatus();
    bool IsRunning { get; }
}

public sealed record SyncStatus(
    int PendingCount,
    DateTimeOffset? LastSyncTime,
    DateTimeOffset? NextRetryTime,
    int FailedCount);
```

**Acceptance Criteria Covered:** AC-036 through AC-042

**Test Requirements:** Covered by SyncServiceTests

**Success Criteria:**
- [ ] File created at `src/Acode.Infrastructure/Persistence/Sync/ISyncService.cs` (~20 lines)
- [ ] SyncStatus record defined
- [ ] Build succeeds

**Gap Checklist Item:** [ ] 🔄 ISyncService interface created

---

## Gap 5.2: Create SyncService Implementation

**Current State:** ❌ MISSING
**Spec Reference:** Sync Process section (lines 519-528), Best Practices (BP-004 through BP-006)
**What Exists:** None

**What's Missing:** Background sync worker implementing outbox processing with exponential backoff

**Implementation Details:**

Must implement:
- Background worker (AC-036)
- Non-blocking operation (AC-037)
- Process oldest records first (AC-038)
- Retry logic with exponential backoff (AC-040, BP-006)
- Max 10 retry attempts (AC-041)
- Resumable after restart (AC-042)

**Implementation Pattern (~200 lines):**
```csharp
namespace Acode.Infrastructure.Persistence.Sync;

public sealed class SyncService : ISyncService, IDisposable
{
    private readonly IOutbox _outbox;
    private readonly PostgresSyncTarget _syncTarget;
    private readonly ILogger<SyncService> _logger;
    private readonly SyncConfiguration _config;
    private CancellationTokenSource? _cts;
    private Task? _syncTask;
    private DateTimeOffset? _lastSyncTime;
    private DateTimeOffset? _nextRetryTime;
    private int _failedCount;

    public bool IsRunning => _syncTask?.IsCompleted == false;

    public async Task StartAsync(CancellationToken ct)
    {
        _logger.LogInformation("Starting sync service");
        _cts = new CancellationTokenSource();
        _syncTask = BackgroundSyncWorker(_cts.Token);
        await Task.Yield();
    }

    public async Task StopAsync(CancellationToken ct)
    {
        _logger.LogInformation("Stopping sync service");
        _cts?.Cancel();
        if (_syncTask != null)
            await _syncTask;
    }

    public async Task SyncNowAsync(CancellationToken ct)
    {
        _logger.LogInformation("Triggering immediate sync");
        await ProcessOutboxAsync(ct);
    }

    public SyncStatus GetStatus()
    {
        return new SyncStatus(
            await _outbox.GetPendingCountAsync(default),
            _lastSyncTime,
            _nextRetryTime,
            _failedCount);
    }

    private async Task BackgroundSyncWorker(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_config.SyncIntervalSeconds * 1000, ct);
                await ProcessOutboxAsync(ct);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Sync worker cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Sync worker error");
            }
        }
    }

    private async Task ProcessOutboxAsync(CancellationToken ct)
    {
        var pendingRecords = await _outbox.GetPendingAsync(_config.MaxBatchSize, ct);

        if (pendingRecords.Count == 0)
        {
            _lastSyncTime = DateTimeOffset.UtcNow;
            return;
        }

        _logger.LogInformation("Processing {Count} outbox records", pendingRecords.Count);

        foreach (var record in pendingRecords)
        {
            try
            {
                await _syncTarget.ApplySyncAsync(record, ct);
                await _outbox.MarkProcessedAsync(record.Id, ct);
                _logger.LogInformation("Synced record {IdempotencyKey}", record.IdempotencyKey);
            }
            catch (Exception ex)
            {
                await HandleSyncFailureAsync(record, ex, ct);
            }
        }

        _lastSyncTime = DateTimeOffset.UtcNow;
    }

    private async Task HandleSyncFailureAsync(OutboxRecord record, Exception ex, CancellationToken ct)
    {
        if (record.Attempts >= _config.MaxRetryAttempts)
        {
            _logger.LogError(ex, "Record {IdempotencyKey} exhausted retries ({Attempts})",
                record.IdempotencyKey, record.Attempts);
            await _outbox.MarkFailedAsync(record.Id, ex.Message, ct);
            _failedCount++;
            return;
        }

        var backoff = CalculateBackoff(record.Attempts);
        _nextRetryTime = DateTimeOffset.UtcNow.AddSeconds(backoff);
        _logger.LogWarning(ex, "Record {IdempotencyKey} failed (attempt {Attempt}/{Max}), retrying in {Backoff}s",
            record.IdempotencyKey, record.Attempts + 1, _config.MaxRetryAttempts, backoff);
    }

    private double CalculateBackoff(int attempts)
    {
        // Exponential backoff: 1s, 2s, 4s, 8s, ... up to max
        var backoff = Math.Min(
            Math.Pow(2, attempts),
            _config.MaxBackoffSeconds);
        return backoff;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
```

**Acceptance Criteria Covered:** AC-036 through AC-042, AC-039 through AC-041

**Test Requirements (SyncServiceTests.cs):**
- [ ] Should_Process_Oldest_First - Prioritizes old records
- [ ] Should_Retry_With_Backoff - Exponential backoff working
- [ ] Should_Handle_Duplicates - Idempotency key prevents duplicates

**Success Criteria:**
- [ ] File created at `src/Acode.Infrastructure/Persistence/Sync/SyncService.cs` (~200 lines)
- [ ] Implements all ISyncService methods
- [ ] Exponential backoff algorithm correct
- [ ] Max 10 attempts enforced
- [ ] Tests: 3/3 passing
- [ ] Build succeeds

**Gap Checklist Item:** [ ] 🔄 SyncService complete with exponential backoff

---

## Gap 5.3: Create OutboxProcessor Helper

**Current State:** ❌ MISSING
**Spec Reference:** Sync section (optional helper - main logic in SyncService)
**What Exists:** None

**What's Missing:** Batch processor for outbox records (may be merged with SyncService)

**Note:** If SyncService fully implements batch processing, this may be combined into SyncService rather than separate file. Prioritize implementing SyncService fully.

**Gap Checklist Item:** [ ] 🔄 OutboxProcessor (if separate) or [✅] merged into SyncService

---

## Phase 5 Verification Checklist

- [ ] ISyncService.cs created (~20 lines)
- [ ] SyncService.cs created (~200 lines)
- [ ] SyncServiceTests.cs created (3 test methods)
- [ ] BackgroundSyncWorker loop implemented
- [ ] Exponential backoff algorithm correct (1s, 2s, 4s, 8s...)
- [ ] Max retry attempts = 10
- [ ] Oldest-first ordering verified
- [ ] All 3 tests passing (100%)
- [ ] Build: 0 errors, 0 warnings
- [ ] Commit pushed with message: "feat(task-011b-p5): implement SyncService with exponential backoff"

**Phase 5 Status:** [ ] 🔄 IN PROGRESS → [ ] ✅ COMPLETE

---

# PHASE 6: CONFLICT RESOLUTION (2-3 hours)

## Gap 6.1: Create ConflictResolver

**Current State:** ❌ MISSING
**Spec Reference:** Conflict Resolution section (lines 537-543), Use Case 3 (disaster recovery)
**What Exists:** None

**What's Missing:** Conflict detection and resolution logic (latest timestamp wins)

**Implementation Details:**

Per spec (lines 537-543):
- Detect conflicts when same entity modified on multiple machines
- Latest timestamp wins (AC-049)
- Log conflicts (AC-050)
- Count conflicts (AC-051)
- Preserve details (AC-052)

**Implementation Pattern (~80 lines):**
```csharp
namespace Acode.Infrastructure.Persistence.Sync;

public sealed class ConflictResolver
{
    private readonly ILogger<ConflictResolver> _logger;
    private int _conflictCount;

    public bool TryResolveConflict(
        Session existing,
        Session incoming,
        out Session resolved)
    {
        resolved = existing;

        // Check for conflict (different updates)
        if (existing.UpdatedAt == incoming.UpdatedAt &&
            existing.State == incoming.State)
        {
            return false; // No conflict
        }

        // Conflict exists - log it
        _logger.LogWarning(
            "Conflict detected for session {SessionId}: " +
            "existing updated {ExistingTime}, incoming updated {IncomingTime}",
            existing.Id.Value, existing.UpdatedAt, incoming.UpdatedAt);

        // Latest timestamp wins (AC-049)
        if (incoming.UpdatedAt > existing.UpdatedAt)
        {
            resolved = incoming;
            _logger.LogInformation(
                "Conflict resolved: Session {SessionId} - incoming won (newer by {Seconds}s)",
                incoming.Id.Value,
                (incoming.UpdatedAt - existing.UpdatedAt).TotalSeconds);
        }
        else
        {
            _logger.LogInformation(
                "Conflict resolved: Session {SessionId} - existing won (newer by {Seconds}s)",
                existing.Id.Value,
                (existing.UpdatedAt - incoming.UpdatedAt).TotalSeconds);
        }

        _conflictCount++;
        return true;
    }

    public int GetConflictCount() => _conflictCount;
}
```

**Acceptance Criteria Covered:** AC-048 through AC-052

**Test Requirements:** Covered by PostgreSQLIntegrationTests or dedicated ConflictResolverTests

**Success Criteria:**
- [ ] File created at `src/Acode.Infrastructure/Persistence/Sync/ConflictResolver.cs` (~80 lines)
- [ ] Detects conflicts correctly
- [ ] Latest timestamp wins implemented
- [ ] Conflicts logged
- [ ] Conflict count tracked
- [ ] Tests verify resolution logic
- [ ] Build succeeds

**Gap Checklist Item:** [ ] 🔄 ConflictResolver complete with conflict detection and logging

---

## Phase 6 Verification Checklist

- [ ] ConflictResolver.cs created (~80 lines)
- [ ] Conflict detection logic implemented
- [ ] Latest-timestamp-wins algorithm correct
- [ ] Conflict logging present
- [ ] Conflict counting working
- [ ] Tests pass (via integration tests)
- [ ] Build: 0 errors, 0 warnings
- [ ] Commit pushed with message: "feat(task-011b-p6): implement conflict resolution with latest-timestamp-wins strategy"

**Phase 6 Status:** [ ] 🔄 IN PROGRESS → [ ] ✅ COMPLETE

---

# PHASE 7: INTEGRATION & PERFORMANCE (4-5 hours)

## Gap 7.1: Write SQLiteIntegrationTests

**Current State:** ❌ MISSING (partial integration test coverage needed)
**Spec Reference:** Testing Requirements (lines 1905-1908)
**What Exists:** None dedicated to SQLite persistence layer

**Required Test Methods:**
- [ ] Should_Persist_Full_Hierarchy - Save session with tasks, steps, artifacts
- [ ] Should_Survive_Crash - Data recoverable after simulated crash
- [ ] Should_Handle_Concurrent_Access - Multiple concurrent writes

**Test Implementation:**
```csharp
[Collection("SQLite Integration")]
public sealed class SqliteIntegrationTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection;

    [Fact]
    public async Task Should_Persist_Full_Hierarchy()
    {
        // Arrange: Create complete session hierarchy
        var session = CreateTestSession();

        // Act: Persist to SQLite
        await _repository.SaveAsync(session, CancellationToken.None);

        // Assert: Retrieve and verify all entities present
        var retrieved = await _repository.GetWithHierarchyAsync(session.Id, CancellationToken.None);
        Assert.NotNull(retrieved);
        Assert.Equal(session.Id, retrieved.Id);
        // Verify tasks, steps, artifacts all present
    }

    [Fact]
    public async Task Should_Survive_Crash()
    {
        // Simulate database crash scenario
        // Verify data integrity after reconnection
    }

    [Fact]
    public async Task Should_Handle_Concurrent_Access()
    {
        // Run multiple parallel saves
        // Verify no data corruption
    }
}
```

**Success Criteria:**
- [ ] File created at `tests/Acode.Infrastructure.Tests/Persistence/SqliteIntegrationTests.cs` (~120 lines)
- [ ] 3 test methods implemented
- [ ] All tests passing
- [ ] Uses real SQLite (not mocked)

**Gap Checklist Item:** [ ] 🔄 SqliteIntegrationTests complete and passing

---

## Gap 7.2: Write OfflineOperationTests

**Current State:** ❌ MISSING
**Spec Reference:** Testing Requirements (lines 1923-1927), Use Case 1 (offline sync)
**What Exists:** None

**Required Test Methods:**
- [ ] Should_Work_Without_Postgres - SQLite works offline
- [ ] Should_Queue_For_Sync - Outbox records queued when sync fails
- [ ] Should_Sync_When_Available - Automatic sync when PostgreSQL available

**Test Implementation:**
```csharp
[Collection("E2E Offline")]
public sealed class OfflineOperationTests : IAsyncLifetime
{
    [Fact]
    public async Task Should_Work_Without_Postgres()
    {
        // Configure without PostgreSQL
        // Perform session operations
        // Verify all work normally via SQLite
    }

    [Fact]
    public async Task Should_Queue_For_Sync()
    {
        // Disconnect PostgreSQL
        // Create session
        // Verify outbox record created for later sync
    }

    [Fact]
    public async Task Should_Sync_When_Available()
    {
        // Queue records while offline
        // Reconnect PostgreSQL
        // Verify records auto-sync
    }
}
```

**Success Criteria:**
- [ ] File created at `tests/Acode.Infrastructure.Tests/Persistence/OfflineOperationTests.cs` (~100 lines)
- [ ] 3 test methods implemented
- [ ] All tests passing
- [ ] Tests full offline→online scenario

**Gap Checklist Item:** [ ] 🔄 OfflineOperationTests complete and passing

---

## Gap 7.3: Implement Performance Benchmarks

**Current State:** ❌ MISSING
**Spec Reference:** Testing Requirements (lines 1930-1938), Performance section (lines 573-609)
**What Exists:** None

**Required Benchmarks (5 scenarios):**

1. **SQLite Single Write** - Target: < 50ms (AC-053)
2. **SQLite Single Read** - Target: < 10ms (AC-054)
3. **Sync Throughput** - Target: 100+ records/sec (AC-055)
4. **Full Hierarchy Query** - Target: < 100ms (AC-056)
5. **Migration Execution** - Target: < 5s (from spec)

**Benchmark Implementation (~100 lines):**
```csharp
[MemoryDiagnoser]
public class PersistenceBenchmarks
{
    private IRunStateStore _store;
    private Session _testSession;

    [GlobalSetup]
    public void Setup()
    {
        // Initialize store and test data
    }

    [Benchmark]
    public async Task SQLiteWrite()
    {
        await _store.SaveAsync(_testSession, CancellationToken.None);
        // Measure: Should be < 50ms
    }

    [Benchmark]
    public async Task SQLiteRead()
    {
        await _store.GetAsync(_testSession.Id, CancellationToken.None);
        // Measure: Should be < 10ms
    }

    [Benchmark]
    public async Task SyncThroughput()
    {
        for (int i = 0; i < 100; i++)
        {
            await _outbox.EnqueueAsync(CreateRecord(), CancellationToken.None);
        }
        // Measure: Should handle 100 records/sec
    }

    [Benchmark]
    public async Task HierarchyQuery()
    {
        await _store.GetWithHierarchyAsync(_testSession.Id, CancellationToken.None);
        // Measure: Should be < 100ms
    }

    [Benchmark]
    public async Task MigrationExecution()
    {
        await _migrationRunner.RunMigrationsAsync(CancellationToken.None);
        // Measure: Should be < 5s
    }
}
```

**Success Criteria:**
- [ ] Benchmark file created at `benchmarks/Acode.Benchmarks/PersistenceBenchmarks.cs` (~100 lines)
- [ ] All 5 scenarios implemented
- [ ] All targets met (or documented if not)
- [ ] Benchmarks runnable via: `dotnet run -c Release`

**Gap Checklist Item:** [ ] 🔄 Performance benchmarks implemented and targets verified

---

## Phase 7 Verification Checklist

- [ ] SqliteIntegrationTests.cs created (~120 lines, 3 methods)
- [ ] OfflineOperationTests.cs created (~100 lines, 3 methods)
- [ ] PersistenceBenchmarks.cs created (~100 lines, 5 scenarios)
- [ ] SqliteIntegrationTests passing (3/3)
- [ ] OfflineOperationTests passing (3/3)
- [ ] Benchmarks show performance targets met
- [ ] Total tests passing: 24+ (including all previous phases)
- [ ] Build: 0 errors, 0 warnings
- [ ] Commit pushed with message: "feat(task-011b-p7): add integration tests and performance benchmarks"

**Phase 7 Status:** [ ] 🔄 IN PROGRESS → [ ] ✅ COMPLETE

---

# PHASE 8: CLI COMMANDS & FINAL POLISH (2-3 hours)

## Gap 8.1: Implement CLI Commands

**Current State:** ⚠️ Partial (may exist but need verification)
**Spec Reference:** User Manual Documentation (lines 1526-1582)
**What Exists:** Unknown - need to verify

**Required Commands:**
1. `acode db status` - Show database status
2. `acode db sync` - Manual sync trigger
3. `acode db migrate` - Run migrations

**Example Implementation:**
```csharp
// In CLI command handler
[Command("db status")]
public async Task DbStatus()
{
    var status = _syncService.GetStatus();
    AnsiConsole.MarkupLine($"[green]SQLite[/]: Connected");
    AnsiConsole.MarkupLine($"[cyan]Pending Outbox Records[/]: {status.PendingCount}");
    AnsiConsole.MarkupLine($"[cyan]Failed Records[/]: {status.FailedCount}");
    AnsiConsole.MarkupLine($"[cyan]Last Sync[/]: {status.LastSyncTime:O}");
}

[Command("db sync")]
public async Task DbSync()
{
    await _syncService.SyncNowAsync(CancellationToken.None);
    AnsiConsole.MarkupLine("[green]✓ Sync initiated[/]");
}

[Command("db migrate")]
public async Task DbMigrate()
{
    await _migrationRunner.RunMigrationsAsync(CancellationToken.None);
    AnsiConsole.MarkupLine("[green]✓ Migrations applied[/]");
}
```

**Success Criteria:**
- [ ] CLI commands implemented
- [ ] User Manual commands all work
- [ ] Output matches spec examples

**Gap Checklist Item:** [ ] 🔄 CLI commands implemented and tested

---

## Gap 8.2: Final Verification & Audit

**Before Marking Complete, Verify:**

### Build Verification
- [ ] `dotnet clean && dotnet build` → 0 errors, 0 warnings
- [ ] All dependencies resolved correctly

### Test Verification
```bash
[ ] dotnet test --filter "Acode.Infrastructure.Tests.Persistence" --verbosity normal
    Expected: 24+ tests passing

[ ] dotnet test --filter "Acode.Application.Tests" --verbosity normal
    Expected: Interface tests passing
```

### Semantic Verification
```bash
[ ] grep -r "NotImplementedException" src/Acode.*/Persistence/
    Expected: NO MATCHES

[ ] grep -r "TODO\|FIXME" src/Acode.*/Persistence/ | grep -v "Performance optimization"
    Expected: NO MATCHES (except minor TODOs)
```

### Acceptance Criteria Verification
- [ ] AC-001 through AC-059: All verified implemented
- [ ] Review spec Acceptance Criteria section
- [ ] Check each AC against implementation

### Test Coverage
- [ ] Unit tests: 5 test files (13 methods)
- [ ] Integration tests: 3 test files (8 methods)
- [ ] E2E tests: 1 test file (3 methods)
- [ ] Benchmarks: 5 scenarios
- [ ] Total: 24+ test methods + 5 benchmarks

**Gap Checklist Item:** [ ] 🔄 Final audit complete - all criteria passed

---

## Phase 8 Verification Checklist

- [ ] `acode db status` implemented
- [ ] `acode db sync` implemented
- [ ] `acode db migrate` implemented
- [ ] All 59 ACs verified implemented
- [ ] Build: 0 errors, 0 warnings
- [ ] All 24+ tests passing (100%)
- [ ] No NotImplementedException anywhere
- [ ] No incomplete TODO markers
- [ ] Performance benchmarks met
- [ ] Git history clean: 8 commits (one per phase)
- [ ] All commits pushed to feature branch

**Phase 8 Status:** [ ] 🔄 IN PROGRESS → [ ] ✅ COMPLETE

---

# FINAL SUMMARY TABLE

| Phase | Description | Status | Hours | ACs Covered |
|-------|-------------|--------|-------|-----------|
| 1 | Application Interfaces | [ ] ⏳ | 1-2 | 1-59 (all) |
| 2 | SQLite Persistence | [ ] ⏳ | 6-8 | 1-8, 29-35 |
| 3 | PostgreSQL Persistence | [ ] ⏳ | 5-6 | 23-28, 48-52 |
| 4 | Migration Infrastructure | [ ] ⏳ | 3-4 | 9-14 |
| 5 | Sync Service Core | [ ] ⏳ | 6-8 | 36-42, 39-41 |
| 6 | Conflict Resolution | [ ] ⏳ | 2-3 | 48-52 |
| 7 | Integration & Performance | [ ] ⏳ | 4-5 | 53-56 |
| 8 | CLI Commands & Final | [ ] ⏳ | 2-3 | ALL |
| **TOTAL** | **All Persistence** | **[ ] ⏳** | **28-40** | **59/59** |

---

## COMPLETION DEFINITION

Task-011b is COMPLETE when:
- [ ] All 8 phases marked [✅]
- [ ] All 59 ACs verified (grep: no NotImplementedException)
- [ ] All 24+ tests passing (100% pass rate)
- [ ] Build: 0 errors, 0 warnings
- [ ] Performance targets met (5/5 benchmarks)
- [ ] 8 commits pushed (one per phase)
- [ ] PR created and ready for review

**When you've finished all 8 phases successfully, the persistence layer is COMPLETE and ready for integration with other systems.**

---

**End of Task-011b Completion Checklist**
