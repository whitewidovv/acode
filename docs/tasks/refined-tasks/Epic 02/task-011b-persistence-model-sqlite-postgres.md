# Task 011.b: Persistence Model (SQLite Workspace Cache + Postgres Source-of-Truth)

**Priority:** P0 – Critical Path  
**Tier:** Infrastructure  
**Complexity:** 21 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 050 (Workspace DB), Task 011.a (Run Entities), Task 026 (Observability)  

---

## Description

Task 011.b implements the two-tier persistence model for run session state. SQLite serves as the workspace cache providing fast, offline-capable, crash-safe storage. PostgreSQL optionally serves as the canonical system of record when remote connectivity is available. This architecture ensures Acode works reliably in all environments.

The two-tier design addresses competing requirements. Users need immediate response and offline capability—SQLite provides this with zero network latency and no dependency on external services. Organizations need centralized visibility and backup—PostgreSQL provides this with proper multi-user access and backup infrastructure. Both needs are satisfied without compromise.

SQLite is the primary storage for all local operations. Every session state change writes to SQLite first. This write is synchronous and must complete before the operation proceeds. SQLite's transactional guarantees ensure crash safety—incomplete writes are rolled back, and the database remains consistent.

PostgreSQL sync is asynchronous and non-blocking. After writing to SQLite, changes are queued for PostgreSQL sync. The agent continues working immediately—it never waits for network operations. Sync happens in the background when the network is available. This design ensures that network issues never slow down the user.

The outbox pattern ensures reliable sync. Each SQLite write also inserts a record into an outbox table. A background process reads the outbox and syncs to PostgreSQL. On success, the outbox record is marked processed. On failure, records are retried with exponential backoff. This pattern guarantees eventual consistency.

Idempotency keys enable safe replay. Every sync record includes a unique idempotency key. PostgreSQL rejects duplicate keys, making replays safe. If sync fails partway through and retries, already-synced records are skipped. This eliminates the risk of duplicate data from retry storms.

Conflict resolution favors the latest timestamp. If the same entity is modified on multiple machines before sync, the latest modification wins. Conflicts are logged for audit but resolved automatically. This simple policy avoids complex merge logic while maintaining practical correctness.

The agent behaves identically whether PostgreSQL is available or not. All functionality works with SQLite alone. PostgreSQL adds centralized backup and multi-machine visibility but is not required for any feature. This ensures Acode works in air-gapped environments, on airplanes, or wherever network is unavailable.

Schema migrations are versioned and automatic. On startup, the persistence layer checks schema version and applies pending migrations. Migrations are forward-only—rollback is not supported (use database restore if needed). Schema changes are tested extensively before deployment.

Connection pooling and lifecycle management are handled by the infrastructure layer. SQLite connections are managed per-thread with proper disposal. PostgreSQL connections use a pool with configurable size. All connections are properly closed on application shutdown.

Error handling distinguishes transient from permanent failures. Network timeouts are transient and trigger retry. Authentication failures are permanent and trigger alert. Schema version mismatches are permanent and require migration. Each error type has appropriate handling and logging.

Testing verifies both tiers independently and together. Unit tests mock database connections. Integration tests use real SQLite. E2E tests use real PostgreSQL. Sync tests verify outbox processing and conflict resolution. This layered testing ensures reliability.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| SQLite | Embedded relational database |
| PostgreSQL | Server-based relational database |
| Workspace Cache | Local SQLite database |
| Source of Truth | Canonical PostgreSQL database |
| Outbox Pattern | Queue for reliable async sync |
| Inbox Pattern | Queue for receiving sync |
| Idempotency Key | Unique identifier for safe replay |
| Eventual Consistency | Data syncs over time |
| Conflict Resolution | Handling concurrent modifications |
| Schema Migration | Database structure updates |
| Connection Pool | Reusable database connections |
| WAL Mode | Write-Ahead Logging for SQLite |
| Transaction | Atomic database operation |
| Retry Backoff | Increasing delays between retries |
| Transient Failure | Temporary recoverable error |

---

## Out of Scope

The following items are explicitly excluded from Task 011.b:

- **Multi-master sync** - Single source of truth
- **Real-time sync** - Eventual consistency only
- **Conflict merge logic** - Latest timestamp wins
- **Cross-database joins** - Single-tier queries only
- **Database sharding** - Single PostgreSQL instance
- **Read replicas** - Primary only
- **Custom SQL** - ORM/abstraction only
- **Database encryption at rest** - OS-level only
- **Point-in-time recovery** - Backup-based only
- **Foreign database support** - SQLite + PostgreSQL only

---

## Functional Requirements

### SQLite Storage

- FR-001: SQLite file MUST be in .agent/workspace.db
- FR-002: SQLite MUST use WAL mode
- FR-003: SQLite MUST use STRICT tables
- FR-004: Database MUST be created on first access
- FR-005: All writes MUST be transactional
- FR-006: Transactions MUST have timeout (30s)
- FR-007: Concurrent reads MUST be supported
- FR-008: Write locks MUST be exclusive

### Schema Management

- FR-009: Schema version MUST be tracked
- FR-010: Migrations MUST be versioned
- FR-011: Migrations MUST run automatically on startup
- FR-012: Migrations MUST be idempotent
- FR-013: Migration failures MUST halt startup
- FR-014: Current schema version MUST be queryable
- FR-015: Migration history MUST be persisted

### Session Tables

- FR-016: sessions table for Session entities
- FR-017: session_events table for events
- FR-018: session_tasks table for Tasks
- FR-019: steps table for Steps
- FR-020: tool_calls table for ToolCalls
- FR-021: artifacts table for Artifacts
- FR-022: All tables MUST have created_at
- FR-023: All tables MUST have updated_at
- FR-024: Primary keys MUST be UUID strings

### PostgreSQL Storage (Optional)

- FR-025: PostgreSQL MUST be configurable
- FR-026: Connection string via config or env
- FR-027: Missing PostgreSQL MUST NOT fail startup
- FR-028: Schema MUST mirror SQLite structure
- FR-029: Indexes MUST optimize common queries
- FR-030: Connection pool size MUST be configurable

### Outbox Pattern

- FR-031: outbox table for pending syncs
- FR-032: Outbox records MUST have idempotency_key
- FR-033: Outbox records MUST have entity_type
- FR-034: Outbox records MUST have entity_id
- FR-035: Outbox records MUST have payload (JSON)
- FR-036: Outbox records MUST have created_at
- FR-037: Outbox records MUST have processed_at (nullable)
- FR-038: Outbox records MUST have attempts count
- FR-039: Outbox records MUST have last_error

### Sync Process

- FR-040: Sync MUST run in background
- FR-041: Sync MUST be non-blocking
- FR-042: Sync MUST process oldest first
- FR-043: Sync MUST retry failed records
- FR-044: Retry MUST use exponential backoff
- FR-045: Max retry attempts: 10
- FR-046: Max backoff: 1 hour
- FR-047: Sync MUST be resumable after restart

### Idempotency

- FR-048: Each sync MUST have unique key
- FR-049: Key format: {entity_type}:{entity_id}:{timestamp}
- FR-050: PostgreSQL MUST reject duplicate keys
- FR-051: Duplicate rejection MUST mark outbox processed
- FR-052: Idempotency MUST be logged

### Conflict Resolution

- FR-053: Conflicts MUST be detected
- FR-054: Latest timestamp MUST win
- FR-055: Conflicts MUST be logged
- FR-056: Conflict count MUST be tracked
- FR-057: Conflict details MUST be preserved

### Query Operations

- FR-058: Get session by ID
- FR-059: List sessions with filter
- FR-060: Get session hierarchy
- FR-061: Query events by session
- FR-062: Pagination MUST be supported
- FR-063: Queries MUST use indexes

### Write Operations

- FR-064: Create session atomically
- FR-065: Update session state
- FR-066: Add task to session
- FR-067: Add step to task
- FR-068: Add tool call to step
- FR-069: Add artifact to tool call
- FR-070: All writes to outbox too

### Abstraction Layer

- FR-071: IRunStateStore interface for queries
- FR-072: ISyncService interface for sync
- FR-073: No direct database access from domain
- FR-074: Database choice via dependency injection

---

## Non-Functional Requirements

### Performance

- NFR-001: SQLite write MUST complete < 50ms
- NFR-002: SQLite read MUST complete < 10ms
- NFR-003: Sync batch MUST process 100 records/sec
- NFR-004: Connection pool MUST handle 10 connections
- NFR-005: Query with pagination MUST be < 100ms

### Reliability

- NFR-006: Crash MUST NOT corrupt database
- NFR-007: Partial sync MUST be recoverable
- NFR-008: Connection loss MUST queue for retry
- NFR-009: Schema mismatch MUST halt startup

### Security

- NFR-010: Database file MUST have 600 permissions
- NFR-011: Connection string MUST NOT be logged
- NFR-012: Passwords MUST be from env var or secret
- NFR-013: SQL injection MUST be prevented

### Durability

- NFR-014: All writes MUST be fsync'd
- NFR-015: Outbox MUST survive crash
- NFR-016: Processed records MUST NOT be lost

### Observability

- NFR-017: All queries MUST be logged with duration
- NFR-018: Sync status MUST be exposed
- NFR-019: Outbox depth MUST be tracked
- NFR-020: Connection health MUST be monitored

---

## User Manual Documentation

### Overview

Acode uses a two-tier persistence model. SQLite stores data locally for fast, offline operation. PostgreSQL optionally provides centralized backup and visibility.

### Configuration

#### SQLite (Default)

SQLite requires no configuration. The database is created automatically:

```
.agent/
└── workspace.db    # SQLite database
```

#### PostgreSQL (Optional)

Configure PostgreSQL in `.agent/config.yml`:

```yaml
persistence:
  postgres:
    enabled: true
    connection_string_env: ACODE_POSTGRES_URL
    # OR explicit connection (not recommended)
    # host: localhost
    # port: 5432
    # database: acode
    # username: acode
    # password_env: ACODE_POSTGRES_PASSWORD
```

Environment variable:

```bash
export ACODE_POSTGRES_URL="postgresql://user:pass@host:5432/acode"
```

### CLI Commands

```bash
# Check database status
$ acode db status
SQLite: .agent/workspace.db (12.5 MB)
  Version: 1.0.0
  Sessions: 45
  Last modified: 2024-01-15T10:30:00Z

PostgreSQL: connected
  Host: db.example.com:5432
  Database: acode
  Sync status: up to date
  Outbox depth: 0

# View sync status
$ acode db sync status
Outbox records: 3 pending
  - Session abc123 (created 5m ago)
  - Task def456 (created 3m ago)
  - Step ghi789 (created 1m ago)

Last sync: 2024-01-15T10:25:00Z
Next retry: 2024-01-15T10:30:00Z

# Force sync
$ acode db sync now
Syncing 3 records...
  ✓ Session abc123
  ✓ Task def456
  ✓ Step ghi789
Sync complete.

# Run migrations
$ acode db migrate
Current version: 1.0.0
Target version: 1.1.0
Running migration 1.0.0 → 1.1.0...
  ✓ Add artifacts table
  ✓ Add index on session_id
Migration complete.

# Check database integrity
$ acode db check
Checking SQLite integrity...
  ✓ No corruption detected
  ✓ Foreign keys valid
  ✓ Indexes valid

# Vacuum database
$ acode db vacuum
Vacuuming .agent/workspace.db...
  Before: 12.5 MB
  After: 10.2 MB
  Saved: 2.3 MB
```

### Database Schema

#### sessions

```sql
CREATE TABLE sessions (
    id TEXT PRIMARY KEY,
    task_description TEXT NOT NULL,
    state TEXT NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    metadata TEXT,
    sync_version INTEGER DEFAULT 0
) STRICT;
```

#### session_events

```sql
CREATE TABLE session_events (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id TEXT NOT NULL REFERENCES sessions(id),
    from_state TEXT NOT NULL,
    to_state TEXT NOT NULL,
    reason TEXT NOT NULL,
    timestamp TEXT NOT NULL
) STRICT;
```

#### session_tasks

```sql
CREATE TABLE session_tasks (
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
```

#### steps

```sql
CREATE TABLE steps (
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
```

#### tool_calls

```sql
CREATE TABLE tool_calls (
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
```

#### artifacts

```sql
CREATE TABLE artifacts (
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
```

#### outbox

```sql
CREATE TABLE outbox (
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

CREATE INDEX idx_outbox_pending ON outbox(processed_at) 
    WHERE processed_at IS NULL;
```

### Sync Behavior

#### Normal Operation

1. User performs action
2. SQLite updated in transaction
3. Outbox record created
4. Operation returns to user (immediate)
5. Background sync processes outbox
6. PostgreSQL updated
7. Outbox marked processed

#### Network Unavailable

1. User performs action
2. SQLite updated (works offline)
3. Outbox record created
4. Background sync fails
5. Retry with backoff
6. When network returns, sync resumes

#### Conflict Resolution

When same entity modified on multiple machines:

1. Sync detects version mismatch
2. Compare timestamps
3. Latest timestamp wins
4. Conflict logged for audit
5. Loser's changes overwritten

### Troubleshooting

#### Database Locked

**Problem:** "database is locked" error

**Solutions:**
1. Close other Acode processes
2. Check for stale lock files
3. Wait and retry (temporary)
4. Increase timeout in config

#### Sync Failing

**Problem:** Outbox depth growing

**Solutions:**
1. Check PostgreSQL connectivity
2. Check credentials
3. View sync errors: `acode db sync status --verbose`
4. Force retry: `acode db sync now`

#### Schema Mismatch

**Problem:** "schema version mismatch" on startup

**Solutions:**
1. Run migrations: `acode db migrate`
2. Check for incompatible versions
3. Backup and recreate if needed

#### Corruption

**Problem:** Database integrity check fails

**Solutions:**
1. Restore from backup
2. Export valid data: `acode db export`
3. Create new database
4. Import: `acode db import`

---

## Acceptance Criteria

### SQLite Storage

- [ ] AC-001: Database in .agent/workspace.db
- [ ] AC-002: WAL mode enabled
- [ ] AC-003: STRICT tables used
- [ ] AC-004: Auto-created on first access
- [ ] AC-005: Writes transactional
- [ ] AC-006: Transaction timeout 30s
- [ ] AC-007: Concurrent reads work
- [ ] AC-008: Write locks exclusive

### Schema

- [ ] AC-009: Schema version tracked
- [ ] AC-010: Migrations versioned
- [ ] AC-011: Auto-migrate on startup
- [ ] AC-012: Migrations idempotent
- [ ] AC-013: Migration failure halts startup
- [ ] AC-014: Version queryable

### Tables

- [ ] AC-015: sessions table exists
- [ ] AC-016: session_events table exists
- [ ] AC-017: session_tasks table exists
- [ ] AC-018: steps table exists
- [ ] AC-019: tool_calls table exists
- [ ] AC-020: artifacts table exists
- [ ] AC-021: outbox table exists
- [ ] AC-022: All have timestamps

### PostgreSQL

- [ ] AC-023: Configurable via config
- [ ] AC-024: Connection string from env
- [ ] AC-025: Missing doesn't fail startup
- [ ] AC-026: Schema mirrors SQLite
- [ ] AC-027: Indexes optimized
- [ ] AC-028: Pool size configurable

### Outbox

- [ ] AC-029: Records have idempotency_key
- [ ] AC-030: Records have entity_type
- [ ] AC-031: Records have entity_id
- [ ] AC-032: Records have payload
- [ ] AC-033: Records have created_at
- [ ] AC-034: Records track processed_at
- [ ] AC-035: Records track attempts

### Sync

- [ ] AC-036: Runs in background
- [ ] AC-037: Non-blocking
- [ ] AC-038: Oldest first
- [ ] AC-039: Retries failed
- [ ] AC-040: Exponential backoff
- [ ] AC-041: Max 10 attempts
- [ ] AC-042: Resumable after restart

### Idempotency

- [ ] AC-043: Unique keys generated
- [ ] AC-044: Key format correct
- [ ] AC-045: Duplicates rejected
- [ ] AC-046: Rejection marks processed
- [ ] AC-047: Logged

### Conflicts

- [ ] AC-048: Detected
- [ ] AC-049: Latest wins
- [ ] AC-050: Logged
- [ ] AC-051: Counted
- [ ] AC-052: Details preserved

### Performance

- [ ] AC-053: SQLite write < 50ms
- [ ] AC-054: SQLite read < 10ms
- [ ] AC-055: Sync 100 records/sec
- [ ] AC-056: Query with pagination < 100ms

### Security

- [ ] AC-057: File permissions 600
- [ ] AC-058: Connection string not logged
- [ ] AC-059: SQL injection prevented

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Infrastructure/Persistence/
├── SQLiteConnectionTests.cs
│   ├── Should_Create_Database()
│   ├── Should_Enable_WAL_Mode()
│   └── Should_Use_Strict_Tables()
│
├── MigrationRunnerTests.cs
│   ├── Should_Run_Migrations_In_Order()
│   ├── Should_Be_Idempotent()
│   └── Should_Halt_On_Failure()
│
├── SessionRepositoryTests.cs
│   ├── Should_Create_Session()
│   ├── Should_Update_Session()
│   ├── Should_Query_By_Id()
│   └── Should_List_With_Filter()
│
├── OutboxTests.cs
│   ├── Should_Add_Record()
│   ├── Should_Generate_Idempotency_Key()
│   └── Should_Mark_Processed()
│
└── SyncServiceTests.cs
    ├── Should_Process_Oldest_First()
    ├── Should_Retry_With_Backoff()
    └── Should_Handle_Duplicates()
```

### Integration Tests

```
Tests/Integration/Persistence/
├── SQLiteIntegrationTests.cs
│   ├── Should_Persist_Full_Hierarchy()
│   ├── Should_Survive_Crash()
│   └── Should_Handle_Concurrent_Access()
│
├── PostgreSQLIntegrationTests.cs
│   ├── Should_Sync_From_Outbox()
│   ├── Should_Handle_Network_Failure()
│   └── Should_Resolve_Conflicts()
│
└── MigrationIntegrationTests.cs
    ├── Should_Migrate_From_Empty()
    └── Should_Migrate_Incrementally()
```

### E2E Tests

```
Tests/E2E/Persistence/
├── OfflineOperationTests.cs
│   ├── Should_Work_Without_Postgres()
│   ├── Should_Queue_For_Sync()
│   └── Should_Sync_When_Available()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| SQLite single write | 25ms | 50ms |
| SQLite single read | 5ms | 10ms |
| Sync throughput | 200/sec | 100/sec |
| Full hierarchy query | 50ms | 100ms |
| Migration execution | 1s | 5s |

### Regression Tests

- Schema after migration
- Sync after format change
- Query performance after data growth

---

## User Verification Steps

### Scenario 1: Auto-Create Database

1. Delete .agent/workspace.db
2. Run `acode status`
3. Verify: Database created
4. Verify: Schema correct

### Scenario 2: Persist Session

1. Run `acode run "task"`
2. Kill process mid-run
3. Run `acode resume`
4. Verify: Session state preserved

### Scenario 3: View Database Status

1. Run `acode db status`
2. Verify: SQLite info shown
3. Verify: Session count correct

### Scenario 4: PostgreSQL Sync

1. Configure PostgreSQL
2. Run `acode run "task"`
3. Run `acode db sync status`
4. Verify: Sync occurred

### Scenario 5: Offline Operation

1. Disconnect network
2. Run `acode run "task"`
3. Verify: Works normally
4. Verify: Outbox populated

### Scenario 6: Sync Recovery

1. Run while offline
2. Reconnect network
3. Wait or `acode db sync now`
4. Verify: Data synced

### Scenario 7: Run Migrations

1. Run `acode db migrate`
2. Verify: Migrations applied
3. Verify: Version updated

### Scenario 8: Database Check

1. Run `acode db check`
2. Verify: No errors
3. Verify: Indexes valid

### Scenario 9: Vacuum

1. Run `acode db vacuum`
2. Verify: Size reduced
3. Verify: Data intact

### Scenario 10: Conflict Resolution

1. Modify session on machine A
2. Modify same session on machine B (offline)
3. Sync machine B
4. Verify: Latest wins
5. Verify: Conflict logged

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Infrastructure/
├── Persistence/
│   ├── SQLite/
│   │   ├── SQLiteConnectionFactory.cs
│   │   ├── SQLiteRunStateStore.cs
│   │   ├── SQLiteOutbox.cs
│   │   └── Migrations/
│   │       ├── IMigration.cs
│   │       ├── MigrationRunner.cs
│   │       └── Migrations/
│   │           ├── V1_0_0_InitialSchema.cs
│   │           └── V1_1_0_AddArtifacts.cs
│   │
│   ├── PostgreSQL/
│   │   ├── PostgreSQLConnectionFactory.cs
│   │   ├── PostgreSQLRunStateStore.cs
│   │   └── PostgreSQLSyncTarget.cs
│   │
│   └── Sync/
│       ├── ISyncService.cs
│       ├── SyncService.cs
│       ├── OutboxProcessor.cs
│       └── ConflictResolver.cs
│
src/AgenticCoder.Application/
└── Sessions/
    ├── IRunStateStore.cs
    └── IOutbox.cs
```

### IRunStateStore Interface

```csharp
namespace AgenticCoder.Application.Sessions;

public interface IRunStateStore
{
    Task<Session?> GetAsync(SessionId id, CancellationToken ct);
    Task SaveAsync(Session session, CancellationToken ct);
    Task<IReadOnlyList<Session>> ListAsync(SessionFilter filter, CancellationToken ct);
    Task<IReadOnlyList<SessionEvent>> GetEventsAsync(SessionId id, CancellationToken ct);
    Task<Session?> GetWithHierarchyAsync(SessionId id, CancellationToken ct);
}
```

### IOutbox Interface

```csharp
namespace AgenticCoder.Application.Sessions;

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

### ISyncService Interface

```csharp
namespace AgenticCoder.Infrastructure.Persistence.Sync;

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

### Migration Interface

```csharp
namespace AgenticCoder.Infrastructure.Persistence.SQLite.Migrations;

public interface IMigration
{
    string Version { get; }
    string Description { get; }
    Task UpAsync(SqliteConnection connection, CancellationToken ct);
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-DB-001 | Database connection failed |
| ACODE-DB-002 | Transaction failed |
| ACODE-DB-003 | Migration failed |
| ACODE-DB-004 | Schema version mismatch |
| ACODE-DB-005 | Sync failed |
| ACODE-DB-006 | Conflict detected |
| ACODE-DB-007 | Integrity check failed |

### Logging Fields

```json
{
  "event": "database_write",
  "database": "sqlite",
  "table": "sessions",
  "operation": "insert",
  "entity_id": "abc123",
  "duration_ms": 12,
  "outbox_enqueued": true
}
```

### Configuration Schema

```yaml
persistence:
  sqlite:
    path: ".agent/workspace.db"
    wal_mode: true
    timeout_seconds: 30
    
  postgres:
    enabled: false
    connection_string_env: "ACODE_POSTGRES_URL"
    pool_size: 10
    
  sync:
    enabled: true
    interval_seconds: 30
    max_batch_size: 100
    max_retry_attempts: 10
    initial_backoff_seconds: 5
    max_backoff_seconds: 3600
```

### Implementation Checklist

1. [ ] Create SQLiteConnectionFactory
2. [ ] Implement WAL mode and STRICT tables
3. [ ] Create migration system
4. [ ] Create initial schema migration
5. [ ] Implement SQLiteRunStateStore
6. [ ] Implement SQLiteOutbox
7. [ ] Create PostgreSQLConnectionFactory
8. [ ] Implement PostgreSQLRunStateStore
9. [ ] Create SyncService
10. [ ] Create OutboxProcessor
11. [ ] Implement exponential backoff
12. [ ] Implement ConflictResolver
13. [ ] Add CLI commands (db status, sync, migrate)
14. [ ] Write unit tests
15. [ ] Write integration tests
16. [ ] Add performance benchmarks

### Validation Checklist Before Merge

- [ ] SQLite auto-creates on first use
- [ ] WAL mode verified
- [ ] Migrations run automatically
- [ ] Session CRUD works
- [ ] Outbox populated on writes
- [ ] Sync processes outbox
- [ ] Idempotency prevents duplicates
- [ ] Conflicts resolved correctly
- [ ] Offline operation works
- [ ] Performance targets met
- [ ] Unit test coverage > 90%

### Rollout Plan

1. **Phase 1:** SQLite core
2. **Phase 2:** Schema and migrations
3. **Phase 3:** Session repository
4. **Phase 4:** Outbox pattern
5. **Phase 5:** PostgreSQL connection
6. **Phase 6:** Sync service
7. **Phase 7:** CLI commands
8. **Phase 8:** Performance tuning

---

**End of Task 011.b Specification**