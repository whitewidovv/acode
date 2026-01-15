# Task-011b Completion Checklist: Persistence Model (SQLite + PostgreSQL)

**Status:** ❌ 0% COMPLETE - READY FOR IMPLEMENTATION

**Date:** 2026-01-15
**Created By:** Claude Code
**Methodology:** Semantic gap analysis per CLAUDE.md Section 3.2 (task-011b-semantic-gap-analysis.md)
**Reference Implementation:** task-049d-completion-checklist.md (gold standard)

---

## CRITICAL: READ THIS FIRST

### METHODOLOGY

This checklist is created FROM the gap analysis (task-011b-semantic-gap-analysis.md), not before it. Each gap listed below comes directly from AC verification findings.

**Before using this checklist:**

1. Read `task-011b-semantic-gap-analysis.md` completely (identifies all 59 missing ACs)
2. Understand that semantic completeness = 0/59 (0%) - NO production code exists for session persistence
3. Note: SQLite/PostgreSQL files in codebase are for CONVERSATION persistence (task-049), not SESSION persistence (task-011b)

### BLOCKING DEPENDENCIES: NONE ✅

All required domain entities from task-011a assumed available or already present.
No dependency on task-050 - can implement independently.

### HOW TO USE THIS CHECKLIST

#### For Fresh-Context Agent:

1. **Read task-011b-semantic-gap-analysis.md** (identifies all gaps)
2. **Read Section 2** - AC mapping shows what each gap implements
3. **Follow Phases 1-6 sequentially** in TDD order
4. **For Each Gap:**
   - Write test(s) that fail (RED)
   - Implement minimum code to pass (GREEN)
   - Clean up while keeping tests green (REFACTOR)
5. **Mark Progress:** `[ ]` = not started, `[🔄]` = in progress, `[✅]` = complete
6. **After Each Phase:** Run `dotnet test` and verify all tests pass
7. **After Each Gap:** Commit with `git commit -m "feat(task-011b): [gap description]"`

#### For Continuing Agent:

1. Find last `[✅]` item
2. Read next `[🔄]` or `[ ]` item
3. Follow same TDD cycle
4. Update checklist with test evidence

---

## SECTION 1: FILE STRUCTURE & WHAT EXISTS

### Files That EXIST (For Conversation, Not This Task):

- ✓ `src/Acode.Infrastructure/Persistence/Connections/SqliteConnectionFactory.cs`
- ✓ `src/Acode.Infrastructure/Persistence/Conversation/SqliteChatRepository.cs`
- ✓ `src/Acode.Infrastructure/Persistence/Migrations/MigrationBootstrapper.cs`
- ✓ `src/Acode.Application/Sync/IOutboxRepository.cs`
- ✓ `src/Acode.Domain/Sync/OutboxEntry.cs`

**Key Point:** These are for CONVERSATION persistence, not SESSION persistence. You can reference patterns but cannot reuse implementations.

### Files MUST CREATE (14 production files):

**Application Layer (2 files):**
- [ ] `src/Acode.Application/Sessions/IRunStateStore.cs` [GAP 1]
- [ ] `src/Acode.Application/Sessions/IOutbox.cs` [GAP 2]

**Infrastructure - SQLite (3 files):**
- [ ] `src/Acode.Infrastructure/Persistence/Sessions/SqliteConnectionFactory.cs` [GAP 3]
- [ ] `src/Acode.Infrastructure/Persistence/Sessions/SqliteRunStateStore.cs` [GAP 4]
- [ ] `src/Acode.Infrastructure/Persistence/Sessions/SqliteOutbox.cs` [GAP 5]

**Infrastructure - Migrations (3 files):**
- [ ] `src/Acode.Infrastructure/Persistence/Migrations/SessionMigrationRunner.cs` [GAP 6]
- [ ] `src/Acode.Infrastructure/Persistence/Migrations/Sessions/V1_0_0_InitialSchema.cs` [GAP 7]
- [ ] `src/Acode.Infrastructure/Persistence/Migrations/Sessions/V1_1_0_AddArtifacts.cs` [GAP 8]

**Infrastructure - PostgreSQL (2 files):**
- [ ] `src/Acode.Infrastructure/Persistence/Sessions/PostgresRunStateStore.cs` [GAP 9]
- [ ] `src/Acode.Infrastructure/Persistence/Sessions/PostgresSyncTarget.cs` [GAP 10]

**Infrastructure - Sync (4 files):**
- [ ] `src/Acode.Infrastructure/Persistence/Sync/SyncService.cs` [GAP 11]
- [ ] `src/Acode.Infrastructure/Persistence/Sync/OutboxProcessor.cs` [GAP 12]
- [ ] `src/Acode.Infrastructure/Persistence/Sync/ConflictResolver.cs` [GAP 13]
- [ ] `src/Acode.Infrastructure/Persistence/Sync/IdempotencyKeyGenerator.cs` [GAP 14]

---

## SECTION 2: ACCEPTANCE CRITERIA MAPPING

All 59 ACs mapped to implementation gaps:

| Gap | Component | ACs | Hours |
|-----|-----------|-----|-------|
| 1-2 | Interfaces | All | 1 |
| 3-5 | SQLite CRUD | AC-001-008 | 8 |
| 6-8 | Migrations | AC-009-022 | 5 |
| 9-10 | PostgreSQL | AC-023-028 | 8 |
| 11-14 | Sync | AC-029-052 | 16 |
| - | Perf/Security | AC-053-059 | Benchmarks |
| - | Tests | All ACs | 13 |

---

## SECTION 3: IMPLEMENTATION PHASES

### PHASE 1: DOMAIN INTERFACES (1 hour)

#### Gap 1: IRunStateStore Interface [ ]

**File:** `src/Acode.Application/Sessions/IRunStateStore.cs`
**ACs Covered:** AC-001-059 (all methods referenced)
**Effort:** 0.5 hours
**Spec Reference:** Lines 2025-2045
**Status:** [ ]

**What to Implement:**
```csharp
public interface IRunStateStore
{
    Task<Session?> GetAsync(SessionId id, CancellationToken ct);
    Task SaveAsync(Session session, CancellationToken ct);
    Task<IReadOnlyList<Session>> ListAsync(SessionFilter filter, CancellationToken ct);
    Task<IReadOnlyList<SessionEvent>> GetEventsAsync(SessionId id, CancellationToken ct);
    Task<Session?> GetWithHierarchyAsync(SessionId id, CancellationToken ct);
}

public sealed record SessionFilter(
    int Skip = 0,
    int Take = 50,
    SessionState? StateFilter = null,
    DateTimeOffset? CreatedAfter = null);
```

**Tests (3):**
- [ ] Interface compiles with all 5 methods
- [ ] Method signatures match spec
- [ ] XML docs present

**Success Criteria:**
- [ ] File exists at correct path
- [ ] 5 public methods present
- [ ] 3 tests passing

---

#### Gap 2: IOutbox Interface [ ]

**File:** `src/Acode.Application/Sessions/IOutbox.cs`
**ACs Covered:** AC-029-035 (outbox record structure)
**Effort:** 0.5 hours
**Spec Reference:** Lines 2046-2061
**Status:** [ ]

**What to Implement:**
```csharp
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
    DateTimeOffset? ProcessedAt = null,
    int Attempts = 0,
    string? LastError = null,
    DateTimeOffset? NextRetryAt = null);
```

**Tests (3):**
- [ ] Interface compiles
- [ ] OutboxRecord immutable
- [ ] Payload is JsonDocument

**Success Criteria:**
- [ ] 5 public methods
- [ ] OutboxRecord has all spec fields
- [ ] 3 tests passing

---

### PHASE 2: SQLITE CORE (8 hours)

#### Gap 3: SqliteConnectionFactory (Session-Specific) [🔄]

**File:** `src/Acode.Infrastructure/Persistence/Sessions/SqliteConnectionFactory.cs`
**ACs Covered:** AC-001-008, AC-057
**Effort:** 3 hours
**Spec Reference:** Lines 2062-2099
**Status:** [ ]

**Requirements from Spec:**
- [ ] AC-001: Database path = `.agent/workspace.db`
- [ ] AC-002: WAL mode enabled
- [ ] AC-003: STRICT tables enforced
- [ ] AC-004: Auto-create if not exists
- [ ] AC-005-006: Transaction setup (timeout = 30s)
- [ ] AC-057: File permissions = 0600

**Tests (4):**
- [ ] Connection creation fails without factory
- [ ] WAL mode not enabled
- [ ] STRICT mode not enabled
- [ ] Timeout = 30 seconds

**Evidence Needed:**
- [ ] PRAGMA journal_mode = WAL executed
- [ ] PRAGMA strict = ON executed
- [ ] DefaultTimeout = 30
- [ ] File created with 0600 permissions

---

#### Gap 4: SqliteRunStateStore (CRUD Operations) [🔄]

**File:** `src/Acode.Infrastructure/Persistence/Sessions/SqliteRunStateStore.cs`
**ACs Covered:** AC-005-008, AC-053-054
**Effort:** 3 hours
**Spec Reference:** Lines 2025-2045
**Status:** [ ]

**Methods Required:**
- [ ] GetAsync(SessionId id) - AC-054: < 10ms
- [ ] SaveAsync(Session session) - AC-053: < 50ms, transactional
- [ ] ListAsync(SessionFilter filter) - pagination, AC-056: < 100ms
- [ ] GetEventsAsync(SessionId id) - ordered by timestamp
- [ ] GetWithHierarchyAsync(SessionId id) - full load

**Tests (5):**
- [ ] SaveAsync fails (no impl)
- [ ] GetAsync returns null when not found
- [ ] ListAsync returns empty
- [ ] GetEventsAsync ordered by created_at
- [ ] All transactional writes

**Evidence:**
- [ ] All methods implemented (no stubs)
- [ ] BEGIN TRANSACTION in SaveAsync
- [ ] Parameterized queries (no SQL injection)
- [ ] 5 tests passing

---

#### Gap 5: SqliteOutbox [🔄]

**File:** `src/Acode.Infrastructure/Persistence/Sessions/SqliteOutbox.cs`
**ACs Covered:** AC-029-035, AC-038-042
**Effort:** 2 hours
**Spec Reference:** Lines 2287-2341
**Status:** [ ]

**Methods Required:**
- [ ] EnqueueAsync(OutboxRecord) - AC-029-035
- [ ] GetPendingAsync(limit) - AC-038: oldest first
- [ ] MarkProcessedAsync(id) - AC-034
- [ ] MarkFailedAsync(id, error) - AC-040-041: exponential backoff
- [ ] GetPendingCountAsync() - monitoring

**Tests (4):**
- [ ] EnqueueAsync fails
- [ ] GetPendingAsync oldest first
- [ ] MarkProcessedAsync updates
- [ ] Exponential backoff calculated

**Evidence:**
- [ ] ORDER BY created_at ASC in query
- [ ] Backoff formula: min(5 * 2^attempts, 3600)
- [ ] 4 tests passing

---

### PHASE 3: MIGRATIONS (5 hours)

#### Gap 6: SessionMigrationRunner [ ]

**File:** `src/Acode.Infrastructure/Persistence/Migrations/SessionMigrationRunner.cs`
**ACs Covered:** AC-009-013
**Effort:** 1 hour
**Status:** [ ]

**Requirements:**
- [ ] AC-009: schema_version table created
- [ ] AC-010: Migrations versioned (V1.0.0, V1.1.0, etc.)
- [ ] AC-011: Auto-run on startup
- [ ] AC-012: Idempotent (can run multiple times)
- [ ] AC-013: Failure halts startup

**Tests (2):**
- [ ] BootstrapAsync discovers migrations
- [ ] Applied versions tracked

---

#### Gap 7: V1_0_0_InitialSchema [ ]

**File:** `src/Acode.Infrastructure/Persistence/Migrations/Sessions/V1_0_0_InitialSchema.cs`
**ACs Covered:** AC-015-019, AC-022
**Effort:** 2 hours
**Status:** [ ]

**Tables Required (all STRICT):**
- [ ] sessions (id, state, task_id, step_id, created_at, updated_at)
- [ ] session_events (event_id, session_id, event_type, payload, created_at)
- [ ] session_tasks (task_id, session_id, status, created_at)
- [ ] steps (step_id, task_id, status, created_at)
- [ ] tool_calls (tool_call_id, step_id, name, status, created_at)

**Tests (1 integration):**
- [ ] All 5 tables created after migration
- [ ] All have timestamps
- [ ] Foreign keys configured

---

#### Gap 8: V1_1_0_AddArtifacts [ ]

**File:** `src/Acode.Infrastructure/Persistence/Migrations/Sessions/V1_1_0_AddArtifacts.cs`
**ACs Covered:** AC-020-021, AC-029-035, AC-022
**Effort:** 2 hours
**Status:** [ ]

**Tables Required:**
- [ ] artifacts (artifact_id, session_id, content, type, created_at)
- [ ] outbox (id, idempotency_key UNIQUE, entity_type, entity_id, payload, created_at, processed_at, attempts)

**Tests (1 integration):**
- [ ] Tables created
- [ ] Outbox indexes for query performance

---

### PHASE 4: POSTGRESQL & SYNC (24 hours)

[Gaps 9-14 follow same pattern as above]

#### Gap 9: PostgresRunStateStore [ ] (2 hours, AC-023-028)
#### Gap 10: PostgresSyncTarget [ ] (2 hours)
#### Gap 11: SyncService [ ] (8 hours, AC-036-042)
#### Gap 12: OutboxProcessor [ ] (6 hours, AC-038-042)
#### Gap 13: ConflictResolver [ ] (4 hours, AC-048-052)
#### Gap 14: IdempotencyKeyGenerator [ ] (2 hours, AC-043-047)

---

### PHASE 5: TESTING (13 hours)

- [ ] Unit tests: SQLite, Outbox, Sync (all gaps, 74+ tests)
- [ ] Integration tests: Real database operations
- [ ] E2E tests: Offline operation, sync recovery
- [ ] Performance benchmarks: AC-053-056

---

### PHASE 6: DOCUMENTATION & CLI (3 hours)

- [ ] CLI commands: `acode db status`, `sync`, `migrate`
- [ ] Configuration guide
- [ ] Troubleshooting
- [ ] Error codes: ACODE-DB-001 through ACODE-DB-007

---

## SECTION 4: VERIFICATION CHECKLIST

**After all gaps complete, verify:**

- [ ] All 14 production files created
- [ ] All 59 ACs verified implemented
- [ ] All 74+ tests passing
- [ ] Zero NotImplementedException
- [ ] Zero build errors/warnings
- [ ] Performance benchmarks passing (AC-053-056)
- [ ] Security verified (AC-057-059)
- [ ] Code coverage > 90%
- [ ] PR created and ready for review

---

**Next Action:** Begin Phase 1 (Gaps 1-2) - create interfaces in TDD order.
