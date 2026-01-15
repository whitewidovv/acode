# Task-049f Completion Checklist: SQLite/PostgreSQL Sync Engine

**Status:** 🟡 15% COMPLETE - FOUNDATIONAL WORK DONE, CORE IMPLEMENTATION NEEDED

**Date:** 2026-01-15
**Created By:** Claude Code
**Methodology:** Gap analysis to checklist per CLAUDE.md Section 3.2
**Gap Analysis Source:** task-049f-semantic-gap-analysis.md (146 ACs documented)

---

## CRITICAL: READ THIS FIRST

### STRUCTURE

This checklist is built FROM the gap analysis (task-049f-semantic-gap-analysis.md) while fresh in context.

All 146 ACs organized into 5 implementation phases:
- **Phase 1: Sync Core** (40 ACs - connection management, status tracking, outbox) - 8 hours
- **Phase 2: PostgreSQL Repositories** (50 ACs - Chat, Run, Message CRUD) - 12 hours
- **Phase 3: Conflict Resolution** (30 ACs - detection, last-write-wins, logging) - 8 hours
- **Phase 4: Health & Reliability** (26 ACs - performance, health checks, error handling) - 12 hours
- **Phase 5: Testing & Integration** (remaining) - 15 hours

Total remaining: 50 hours (15% done = 22 ACs foundational)

### NO BLOCKING DEPENDENCIES ✅

Can implement immediately. Note: 049a/049c must be integrated first (uses their domain models/repos).

### HOW TO USE THIS CHECKLIST

#### For Fresh-Context Agent:

1. **Read task-049f-semantic-gap-analysis.md completely** (all 146 ACs listed)
2. **Understand what exists** (outbox infrastructure, sync status tracking, batching logic outlined)
3. **Follow Phases 1-5 sequentially** in TDD order
4. **For Each Gap:**
   - RED: Write test(s) that fail due to missing implementation
   - GREEN: Implement minimum code to pass tests
   - REFACTOR: Clean up while keeping tests green
5. **Mark Progress:** `[ ]` = not started, `[🔄]` = in progress, `[✅]` = complete
6. **Commit after each logical unit** per git workflow below

---

## SECTION 1: WHAT EXISTS (PARTIAL - 15%)

**Foundational (Already Exists):**
- ✅ IOutboxRepository interface skeleton
- ✅ OutboxEntry domain model
- ✅ SyncStatus enum (Pending, Syncing, Synced, Conflict, Failed)
- ✅ Sync infrastructure concepts (outbox pattern, batching, retry logic documented)
- ✅ Retry policy (exponential backoff formula)

**What's Missing (Core Implementation):**
- ❌ PostgreSQL repositories (Chat, Run, Message CRUD)
- ❌ Sync engine orchestration (background worker, coordination)
- ❌ Conflict detection and resolution
- ❌ Health checks and monitoring
- ❌ Comprehensive test coverage

---

## SECTION 2: PRODUCTION FILES NEEDED (20 files)

### PostgreSQL Layer (6 files)

```
src/Acode.Infrastructure/Persistence/PostgreSQL/
├── PostgresChatRepository.cs              [GAP 1] - IChatRepository implementation
├── PostgresRunRepository.cs               [GAP 2] - IRunRepository implementation
├── PostgresMessageRepository.cs           [GAP 3] - IMessageRepository implementation
├── PostgresConnectionPool.cs              [GAP 4] - Connection pooling & lifecycle
├── PostgresTransaction.cs                 [GAP 5] - Transaction handling
└── PostgresMigrations.sql                 [GAP 6] - Schema for PostgreSQL (mirrors SQLite)
```

### Sync Engine Layer (6 files)

```
src/Acode.Infrastructure/Sync/
├── SyncEngine.cs                          [GAP 7] - Main orchestrator
├── OutboxBatcher.cs                       [GAP 8] - Batch processing (partial, needs completion)
├── SyncWorker.cs                          [GAP 9] - Background worker thread
├── ConflictResolver.cs                    [GAP 10] - Last-write-wins strategy
├── HealthChecker.cs                       [GAP 11] - Sync health monitoring
└── SyncLogger.cs                          [GAP 12] - Comprehensive logging (error codes, metrics)
```

### Application Layer (3 files)

```
src/Acode.Application/Sync/
├── ISyncEngine.cs                         [GAP 13] - Interface (refine from existing)
├── ISyncStatus.cs                         [GAP 14] - Status queries
└── ConflictRecord.cs                      [GAP 15] - Domain model for conflicts
```

### CLI Layer (2 files)

```
src/Acode.Cli/Commands/
├── SyncCommand.cs                         [GAP 16] - acode db sync [now|status|pause|resume]
└── HealthCommand.cs                       [GAP 17] - acode db health
```

### Configuration (2 files)

```
src/Acode.Infrastructure/Configuration/
├── SyncOptions.cs                         [GAP 18] - Configuration model
└── PostgresOptions.cs                     [GAP 19] - PostgreSQL connection options
```

**Total: 19-20 production files**

---

## SECTION 3: ACCEPTANCE CRITERIA BY PHASE

### PHASE 1: SYNC CORE (8 hours, 40 ACs)

#### Gap 1: Connection Management [ ]

**Files:**
- `src/Acode.Infrastructure/Persistence/PostgreSQL/PostgresConnectionPool.cs` [GAP 4]
- `src/Acode.Infrastructure/Persistence/PostgreSQL/PostgresTransaction.cs` [GAP 5]

**ACs Covered:** AC-001-012
**Status:** [ ] PENDING
**Effort:** 2 hours

**What to Implement:**

Connection pool with validation:

```csharp
namespace Acode.Infrastructure.Persistence.PostgreSQL;

public sealed class PostgresConnectionPool : IAsyncDisposable
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly PostgresOptions _options;
    private readonly ILogger<PostgresConnectionPool> _logger;

    public PostgresConnectionPool(PostgresOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        // AC-003: Pool size = 10 (configurable)
        var connString = new NpgsqlConnectionStringBuilder(_options.ConnectionString)
        {
            MaxPoolSize = _options.PoolSize
        };

        _dataSource = new NpgsqlDataSourceBuilder(connString.ToString()).Build();
    }

    /// <summary>
    /// Get validated connection (AC-004, AC-005)
    /// </summary>
    public async Task<NpgsqlConnection> GetConnectionAsync(CancellationToken ct)
    {
        try
        {
            var conn = await _dataSource.OpenConnectionAsync(ct);

            // AC-004: Validate before use
            await ValidateConnectionAsync(conn, ct);

            return conn;
        }
        catch (Exception ex) when (IsTransientFailure(ex))
        {
            // AC-006: Retry exponential backoff
            _logger.LogWarning(ex, "Transient connection failure, will retry");
            throw new SyncException("ACODE-SYNC-001", "Connection failed (transient)", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection failed");
            throw new SyncException("ACODE-SYNC-001", "Connection failed", ex);
        }
    }

    private async Task ValidateConnectionAsync(NpgsqlConnection conn, CancellationToken ct)
    {
        // AC-004: Validate with simple query
        using var cmd = new NpgsqlCommand("SELECT 1", conn);
        await cmd.ExecuteScalarAsync(ct);

        // AC-005: Detect stale connections (SHOW statement_timeout)
        using var checkCmd = new NpgsqlCommand("SELECT 1", conn);
        checkCmd.CommandTimeout = (int)_options.ConnectionTimeout.TotalSeconds;
        await checkCmd.ExecuteScalarAsync(ct);
    }

    private bool IsTransientFailure(Exception ex)
    {
        // AC-006: Network/transient errors
        return ex is TimeoutException or 
               (NpgsqlException pex && pex.SqlState == "08P01"); // Admin shutdown
    }

    public async ValueTask DisposeAsync()
    {
        await _dataSource.DisposeAsync();
    }
}

public sealed record PostgresOptions(
    string ConnectionString,
    int PoolSize = 10,
    TimeSpan? ConnectionTimeout = null,
    bool RequireSsl = false)
{
    public PostgresOptions() : this("") { }
}
```

**Tests (4):**
- [ ] Pool initialized with size = 10 (AC-003)
- [ ] Connections validated before use (AC-004)
- [ ] Stale connections detected (AC-005)
- [ ] Transient errors trigger retry (AC-006)

---

#### Gap 2: Sync Status Tracking [ ]

**Files:**
- `src/Acode.Application/Sync/ISyncStatus.cs` [GAP 14]
- `src/Acode.Infrastructure/Sync/SyncStatusStore.cs` (new)

**ACs Covered:** AC-013-025
**Status:** [ ] PENDING
**Effort:** 2 hours

**What to Implement:**

Status tracking and querying (AC-013-025):

```csharp
namespace Acode.Application.Sync;

public enum SyncState
{
    Pending = 0,
    Syncing = 1,
    Synced = 2,
    Conflict = 3,
    Failed = 4
}

public sealed record SyncStatus(
    SyncState State,
    DateTimeOffset? LastSyncTime = null,
    DateTimeOffset? NextRetryTime = null,
    int PendingCount = 0,
    int FailedCount = 0,
    int ConflictCount = 0,
    TimeSpan? LastSyncDuration = null);

public interface ISyncStatusStore
{
    Task<SyncStatus> GetStatusAsync(CancellationToken ct);
    Task SetStateAsync(SyncState state, CancellationToken ct);
    Task UpdateMetricsAsync(int pending, int failed, int conflicts, CancellationToken ct);
    Task RecordSyncCompletionAsync(TimeSpan duration, CancellationToken ct);
}
```

**Spec Reference:** Lines 1506-1600 (Sync status requirements)

**Tests (5):**
- [ ] Status queryable (AC-020)
- [ ] Last sync time tracked (AC-015)
- [ ] Pending count tracked (AC-017)
- [ ] Failed count tracked (AC-018)
- [ ] Conflict count tracked (AC-019)

---

#### Gap 3: Outbox Processing [ ]

**File:** `src/Acode.Infrastructure/Sync/OutboxBatcher.cs` [GAP 8] (partial - needs completion)

**ACs Covered:** AC-026-040
**Status:** [ ] PENDING
**Effort:** 4 hours

**What to Implement:**

Complete outbox processor with batching, retry, and idempotency (AC-026-040):

```csharp
namespace Acode.Infrastructure.Sync;

public sealed class OutboxBatcher
{
    private readonly IOutboxRepository _outbox;
    private readonly ISyncTarget _target;
    private readonly ILogger<OutboxBatcher> _logger;
    private const int DefaultBatchSize = 100; // AC-027

    /// <summary>
    /// Process pending outbox records in batches (AC-027-028)
    /// </summary>
    public async Task<ProcessResult> ProcessPendingAsync(int? batchSize = null, CancellationToken ct = default)
    {
        var size = batchSize ?? DefaultBatchSize;
        var pending = await _outbox.GetPendingAsync(size, ct);

        if (pending.Count == 0)
            return new ProcessResult(0, 0, 0);

        var succeeded = 0;
        var failed = 0;

        foreach (var record in pending)
        {
            try
            {
                // AC-034: Check idempotency (don't retry duplicates)
                var isDuplicate = await CheckIdempotencyAsync(record.IdempotencyKey, ct);
                if (isDuplicate)
                {
                    await _outbox.MarkProcessedAsync(record.Id, ct);
                    succeeded++;
                    _logger.LogInformation("Outbox record {Id} already processed (idempotency - AC-034)", record.Id);
                    continue;
                }

                // AC-029: Process oldest first (ordering already ensured by query)
                await _target.SyncAsync(record, ct);

                // AC-035: Mark as processed
                await _outbox.MarkProcessedAsync(record.Id, ct);
                succeeded++;
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                // AC-029: Failed records marked for retry
                // AC-030: Calculate exponential backoff
                var attempts = record.Attempts;
                var backoffSeconds = Math.Min(5 * (int)Math.Pow(2, attempts), 3600); // AC-030
                var nextRetry = DateTimeOffset.UtcNow.AddSeconds(backoffSeconds);

                // AC-031-032: Max 10 attempts, then mark permanent failure
                if (attempts >= 10)
                {
                    await _outbox.MarkFailedAsync(record.Id, $"Max retry attempts exceeded: {ex.Message}", ct);
                    _logger.LogError("Record {Id} failed after 10 attempts (AC-032)", record.Id);
                }
                else
                {
                    // Retry later
                    _logger.LogWarning(ex, "Retry record {Id} in {Seconds}s (attempt {Attempt}/10 - AC-030/031)", 
                        record.Id, backoffSeconds, attempts + 1);
                }

                failed++;
            }
            catch (Exception ex)
            {
                // AC-038-039: Stop on authentication or schema errors
                if (IsAuthenticationError(ex) || IsSchemaError(ex))
                {
                    _logger.LogError(ex, "Fatal sync error - stopping processing (AC-038/039)");
                    return new ProcessResult(succeeded, failed, pending.Count - succeeded - failed);
                }

                // AC-033: Continue on batch failure
                failed++;
                _logger.LogError(ex, "Error processing outbox record {Id}, continuing", record.Id);
            }
        }

        // AC-037: Processing resumable after crash (no in-memory state)
        return new ProcessResult(succeeded, failed, pending.Count - succeeded - failed);
    }

    private bool IsTransient(Exception ex) =>
        ex is TimeoutException or
        (NpgsqlException pex && (pex.SqlState == "08P01" || pex.SqlState == "08006"));

    private bool IsAuthenticationError(Exception ex) =>
        ex is NpgsqlException pex && pex.SqlState == "28P01"; // Invalid password

    private bool IsSchemaError(Exception ex) =>
        ex is NpgsqlException pex && pex.SqlState == "42P01"; // Table not found

    private async Task<bool> CheckIdempotencyAsync(string key, CancellationToken ct)
    {
        // AC-034: Check if key already processed
        // Implementation would query sync log for this key
        return false; // Placeholder
    }
}

public sealed record ProcessResult(int Succeeded, int Failed, int Remaining);
```

**Tests (8):**
- [ ] Batch processing ordered oldest-first (AC-026-027)
- [ ] Sequential processing (AC-028)
- [ ] Failed records marked for retry (AC-029)
- [ ] Exponential backoff calculated (AC-030)
- [ ] Max 10 attempts (AC-031-032)
- [ ] Continues on batch failure (AC-033)
- [ ] Resumable after crash (AC-036-037)
- [ ] Stops on auth/schema errors (AC-038-039)

---

### PHASE 2: POSTGRESQL REPOSITORIES (12 hours, 50 ACs)

#### Gap 4: PostgresChatRepository [ ]

**File:** `src/Acode.Infrastructure/Persistence/PostgreSQL/PostgresChatRepository.cs` [GAP 1]

**ACs Covered:** AC-041-055
**Status:** [ ] PENDING
**Effort:** 4 hours

**What to Implement:**

Complete IChatRepository implementation with:
- CRUD operations (AC-042-045)
- Filtering and queries (AC-046-047)
- Sync status tracking (AC-048)
- Version tracking for conflicts (AC-049)
- Cascade operations (AC-050)
- ACID transaction handling (AC-051)
- Connection pooling (AC-052)
- Timeout handling (AC-053)
- Error codes (AC-054)
- Logging (AC-055)

**Tests (8):**
- [ ] Create returns ChatId (AC-042)
- [ ] Read returns Chat with all fields (AC-043)
- [ ] List with pagination (AC-044)
- [ ] Delete marks deleted_at (AC-045)
- [ ] Tag queries (AC-046)
- [ ] Worktree filter (AC-047)
- [ ] Cascade deletion (AC-050)
- [ ] Transactions atomic (AC-051)

---

#### Gap 5: PostgresRunRepository [ ]

**File:** `src/Acode.Infrastructure/Persistence/PostgreSQL/PostgresRunRepository.cs` [GAP 2]

**ACs Covered:** AC-056-070 (similar to ChatRepository)

**Status:** [ ] PENDING
**Effort:** 3 hours

---

#### Gap 6: PostgresMessageRepository [ ]

**File:** `src/Acode.Infrastructure/Persistence/PostgreSQL/PostgresMessageRepository.cs` [GAP 3]

**ACs Covered:** AC-071-090 (similar to ChatRepository)

**Status:** [ ] PENDING
**Effort:** 3 hours

---

#### Gap 7: PostgreSQL Migrations [ ]

**File:** `src/Acode.Infrastructure/Persistence/PostgreSQL/PostgresMigrations.sql` [GAP 6]

**ACs Covered:** AC-091 (schema mirrors SQLite)

**Status:** [ ] PENDING
**Effort:** 2 hours

**What to Implement:**

DDL statements that mirror SQLite schema but with PostgreSQL types/constraints:

```sql
-- PostgreSQL schema mirrors SQLite (AC-091)
CREATE TABLE IF NOT EXISTS chats (
    id TEXT PRIMARY KEY,
    title VARCHAR(500) NOT NULL,
    tags TEXT[], -- Array of tags
    state VARCHAR(50) NOT NULL,
    worktree_id TEXT,
    is_deleted BOOLEAN DEFAULT FALSE,
    deleted_at TIMESTAMP WITH TIME ZONE,
    sync_status VARCHAR(50) DEFAULT 'Pending',
    version INTEGER DEFAULT 1,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_chats_worktree ON chats(worktree_id);
CREATE INDEX IF NOT EXISTS idx_chats_created ON chats(created_at);
CREATE INDEX IF NOT EXISTS idx_chats_sync_status ON chats(sync_status);

-- Similar for runs, messages, with foreign keys
```

---

### PHASE 3: CONFLICT RESOLUTION (8 hours, 30 ACs)

#### Gap 8: ConflictResolver [ ]

**File:** `src/Acode.Infrastructure/Sync/ConflictResolver.cs` [GAP 10]

**ACs Covered:** AC-091-120
**Status:** [ ] PENDING
**Effort:** 8 hours

**What to Implement:**

Conflict detection, resolution, and logging:

```csharp
namespace Acode.Infrastructure.Sync;

public sealed class ConflictResolver
{
    private readonly ISyncLogger _logger;

    /// <summary>
    /// Detect conflicts when versions differ (AC-091-100)
    /// </summary>
    public async Task<ConflictRecord?> DetectConflictAsync(
        string entityType,
        string entityId,
        DateTimeOffset localUpdatedAt,
        DateTimeOffset remoteUpdatedAt,
        JsonDocument localState,
        JsonDocument remoteState,
        CancellationToken ct)
    {
        // AC-091: Conflict if versions differ
        // AC-092: Compare updated_at timestamps
        if (localUpdatedAt == remoteUpdatedAt)
            return null; // No conflict

        // AC-093: Load both versions
        var conflict = new ConflictRecord(
            EntityType: entityType,
            EntityId: entityId,
            LocalState: localState,
            LocalUpdatedAt: localUpdatedAt,
            RemoteState: remoteState,
            RemoteUpdatedAt: remoteUpdatedAt,
            DetectedAt: DateTimeOffset.UtcNow
        );

        // AC-094: Log conflict
        await _logger.LogConflictAsync(conflict, ct);

        // AC-096: Store for audit
        // AC-097: Increment conflict count

        return conflict;
    }

    /// <summary>
    /// Resolve conflict using last-write-wins (AC-101-120)
    /// </summary>
    public async Task<JsonDocument> ResolveAsync(ConflictRecord conflict, CancellationToken ct)
    {
        // AC-101: Last-write-wins strategy
        var winner = conflict.RemoteUpdatedAt > conflict.LocalUpdatedAt
            ? conflict.RemoteState
            : conflict.LocalState;

        // AC-102: Winner's version persisted
        // AC-103: Loser's version archived
        // AC-104: Resolution logged
        await _logger.LogConflictResolutionAsync(conflict, winner, ct);

        // AC-107: Deterministic ordering (tiebreaker by ID if same timestamp)
        if (conflict.LocalUpdatedAt == conflict.RemoteUpdatedAt)
        {
            // Use ID as tiebreaker (deterministic)
            winner = conflict.EntityId.CompareTo(conflict.EntityId) > 0
                ? conflict.RemoteState
                : conflict.LocalState;
        }

        return winner;
    }
}

public sealed record ConflictRecord(
    string EntityType,
    string EntityId,
    JsonDocument LocalState,
    DateTimeOffset LocalUpdatedAt,
    JsonDocument RemoteState,
    DateTimeOffset RemoteUpdatedAt,
    DateTimeOffset DetectedAt);
```

**Tests (8):**
- [ ] Conflicts detected when versions differ (AC-091-092)
- [ ] Both versions loaded (AC-093)
- [ ] Conflict logged (AC-094)
- [ ] Last-write-wins applied (AC-101)
- [ ] Winner persisted (AC-102)
- [ ] Loser archived (AC-103)
- [ ] Deterministic ordering (AC-107)
- [ ] Concurrent conflicts handled (AC-108)

---

### PHASE 4: HEALTH & RELIABILITY (12 hours, 26 ACs)

#### Gap 9: HealthChecker [ ]

**File:** `src/Acode.Infrastructure/Sync/HealthChecker.cs` [GAP 11]

**ACs Covered:** AC-121-146
**Status:** [ ] PENDING
**Effort:** 4 hours

**What to Implement:**

Health monitoring with performance tracking and circuit breaker (AC-121-146):

```csharp
public sealed class HealthChecker
{
    /// <summary>
    /// Verify sync is healthy (performance, reliability)
    /// </summary>
    public async Task<HealthReport> CheckAsync(CancellationToken ct)
    {
        var report = new HealthReport();

        // AC-121: Throughput > 100 records/sec
        // AC-122: Latency < 50ms per record
        // AC-123: Memory < 500MB for 10k pending
        // AC-124: CPU < 50% during sync
        // AC-125: No blocking of main operations (async)

        // AC-142: Alerts on sync failures
        // AC-143: Circuit breaker (stop on repeated failures)

        return report;
    }
}

public sealed record HealthReport(
    bool IsHealthy,
    string? Issues = null,
    PerformanceMetrics? Metrics = null);

public sealed record PerformanceMetrics(
    double ThroughputRecordsPerSecond,
    double AverageLatencyMs,
    long MemoryUsageBytes,
    double CpuPercentage);
```

**Tests (5):**
- [ ] Throughput measured (AC-121)
- [ ] Latency measured (AC-122)
- [ ] Memory bounded (AC-123)
- [ ] No deadlocks (AC-139)
- [ ] Circuit breaker engages on failures (AC-144)

---

#### Gap 10: SyncEngine Orchestrator [ ]

**File:** `src/Acode.Infrastructure/Sync/SyncEngine.cs` [GAP 7]

**ACs Covered:** AC-036-040, AC-121-146
**Status:** [ ] PENDING
**Effort:** 5 hours

**What to Implement:**

Main orchestrator that coordinates all sync operations:

```csharp
public sealed class SyncEngine : ISyncEngine
{
    private readonly OutboxBatcher _batcher;
    private readonly ConflictResolver _resolver;
    private readonly HealthChecker _health;
    private readonly SyncWorker _worker;

    /// <summary>
    /// Run full sync cycle (AC-036-040)
    /// </summary>
    public async Task SyncNowAsync(CancellationToken ct)
    {
        var status = await _health.CheckAsync(ct);
        if (!status.IsHealthy)
        {
            _logger.LogWarning("Sync skipped - health check failed");
            return;
        }

        try
        {
            // AC-036-037: Background processing resumable after crash
            var result = await _batcher.ProcessPendingAsync(cancellationToken: ct);

            await _statusStore.RecordSyncCompletionAsync(
                TimeSpan.FromMilliseconds(result.Duration),
                ct);
        }
        catch (Exception ex)
        {
            // AC-146: Comprehensive error handling
            _logger.LogError(ex, "Sync cycle failed");
            throw;
        }
    }

    /// <summary>
    /// Start background sync worker (AC-036)
    /// </summary>
    public async Task StartAsync(CancellationToken ct)
    {
        await _worker.StartAsync(ct);
    }

    /// <summary>
    /// Stop background worker gracefully (AC-145)
    /// </summary>
    public async Task StopAsync(CancellationToken ct)
    {
        await _worker.StopAsync(ct);
    }
}
```

**Tests (6):**
- [ ] Full sync cycle works (AC-036-037)
- [ ] Worker starts/stops (AC-036)
- [ ] No data loss on crash (AC-133)
- [ ] State recoverable after restart (AC-134)
- [ ] Audit trail complete (AC-137)
- [ ] Graceful shutdown (AC-145)

---

### PHASE 5: TESTING & CLI (15 hours, remaining ACs)

#### Gap 11: SyncCommand CLI [ ]

**File:** `src/Acode.Cli/Commands/SyncCommand.cs` [GAP 16]

**ACs Covered:** AC-024
**Status:** [ ] PENDING
**Effort:** 2 hours

**Commands:**
- `acode db sync now` - Manual trigger (AC-024)
- `acode db sync status` - Query status (AC-020)
- `acode db sync pause` - Pause sync (AC-023)
- `acode db sync resume` - Resume sync

---

#### Gap 12: Comprehensive Test Suite [ ]

**Test Files Needed:**
- PostgresChatRepositoryTests.cs (8 tests)
- PostgresRunRepositoryTests.cs (6 tests)
- PostgresMessageRepositoryTests.cs (6 tests)
- ConflictResolverTests.cs (8 tests)
- OutboxBatcherTests.cs (5 tests)
- SyncEngineTests.cs (6 tests)
- PostgresSyncIntegrationTests.cs (12 tests)
- ConflictResolutionIntegrationTests.cs (8 tests)
- SyncFailureRecoveryTests.cs (5 tests)
- FullSyncE2ETests.cs (6 tests)

**Total: 70+ test methods**

**Status:** [ ] PENDING
**Effort:** 15 hours

---

## SECTION 4: VERIFICATION CHECKLIST

**After all 5 phases complete, verify all 146 ACs:**

- [ ] All 20 PostgreSQL repositories working (AC-041-090)
- [ ] Sync core functioning (AC-001-040)
- [ ] Conflict resolution working (AC-091-120)
- [ ] Health checks passing (AC-121-146)
- [ ] All 70+ tests passing
- [ ] Zero NotImplementedException
- [ ] Zero build errors/warnings
- [ ] Performance benchmarks passing
- [ ] No data loss scenarios
- [ ] Comprehensive error handling

---

## GIT WORKFLOW

**Commit after each phase:**

```bash
# Phase 1
git commit -m "feat(task-049f): implement PostgreSQL connection pooling (AC-001-012)"
git commit -m "feat(task-049f): implement sync status tracking (AC-013-025)"
git commit -m "feat(task-049f): implement outbox batch processor (AC-026-040)"

# Phase 2
git commit -m "feat(task-049f): implement PostgreSQL repositories (AC-041-090)"

# Phase 3
git commit -m "feat(task-049f): implement conflict resolver (AC-091-120)"

# Phase 4
git commit -m "feat(task-049f): implement sync engine and health checker (AC-121-146)"

# Phase 5
git commit -m "feat(task-049f): complete sync engine with comprehensive tests

- PostgreSQL repositories (Chat, Run, Message)
- Sync engine orchestration (background worker, status tracking)
- Conflict resolution (last-write-wins strategy)
- Health checks and monitoring
- Comprehensive testing (70+ tests)

Total: 146 ACs, 20 production files, 70+ tests

🤖 Generated with Claude Code"
```

---

**Next Action:** Begin Phase 1 with fresh context from gap analysis.

