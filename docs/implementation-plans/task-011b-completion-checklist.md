# Task-011b Completion Checklist: Persistence Model (SQLite/Postgres)

**Status:** 0% COMPLETE - PARTIALLY BLOCKED

**Date:** 2026-01-15
**Created By:** Claude Code
**Purpose:** Track implementation of persistence layer (Phase 1 startable, Phases 2-7 deferred)

---

## BLOCKING DEPENDENCIES STATUS

### ✅ Unblocked: Phase 1 (Interfaces)
- Can start immediately: Yes
- Duration: 8 hours
- External dependencies: None
- Status: Ready to begin

### ❌ Blocked: Phases 2-7 (Implementation)
- Task-050 (Workspace DB Foundation): NOT STARTED
- Task-011a (Run Entities): NOT STARTED (0%, 42.75 hrs needed)
- Cannot proceed until both complete
- Estimated wait: 4-6 weeks

---

## PHASE 1: Interface Definitions & Abstractions (8 hours)

### This phase CAN START IMMEDIATELY

These interfaces define contracts without requiring database infrastructure or entity implementations.

### Gap 1.1: IRunStateStore Interface

**File:** `src/Acode.Application/Persistence/IRunStateStore.cs`
**Status:** 🔄 PENDING
**Effort:** 3 hours, 60 LOC

**What to Implement:**

```csharp
public interface IRunStateStore
{
    // Create/Read/Update/Delete
    Task<SessionId> CreateSessionAsync(string taskDescription, JsonDocument? metadata = null);
    Task<Session> GetSessionAsync(SessionId sessionId);
    Task UpdateSessionAsync(Session session);
    Task DeleteSessionAsync(SessionId sessionId);

    // Queries
    Task<IReadOnlyList<Session>> QuerySessionsAsync(SessionFilter filter, PaginationOptions? pagination = null);
    Task<int> CountSessionsAsync(SessionFilter filter);

    // Nested entities
    Task<SessionTask> GetTaskAsync(TaskId taskId);
    Task<Step> GetStepAsync(StepId stepId);
    Task<ToolCall> GetToolCallAsync(ToolCallId toolCallId);
    Task<Artifact> GetArtifactAsync(ArtifactId artifactId);

    // Transactions
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation);
}
```

**Tests:** 10 in-memory tests (fakes, no database)

### Gap 1.2: IOutbox Interface

**File:** `src/Acode.Application/Persistence/IOutbox.cs`
**Status:** 🔄 PENDING
**Effort:** 1 hour, 25 LOC

**What to Implement:**

```csharp
public interface IOutbox
{
    Task AppendAsync(OutboxEvent evt);
    Task<IReadOnlyList<OutboxEvent>> ReadUnprocessedAsync(int batchSize = 100);
    Task AcknowledgeAsync(OutboxEventId id);
    Task<OutboxEventStatus> GetStatusAsync(OutboxEventId id);
}

public record OutboxEvent(
    OutboxEventId Id,
    SessionId SessionId,
    string EventType,
    JsonDocument Payload,
    DateTimeOffset CreatedAt
);
```

**Tests:** 5 in-memory tests

### Gap 1.3: ISyncService Interface

**File:** `src/Acode.Application/Persistence/ISyncService.cs`
**Status:** 🔄 PENDING
**Effort:** 2 hours, 40 LOC

**What to Implement:**

```csharp
public interface ISyncService
{
    Task SyncAsync(CancellationToken cancellationToken);
    Task<SyncStatus> GetStatusAsync();
    Task CancelSyncAsync();
}

public record SyncStatus(
    bool IsSyncing,
    int PendingEvents,
    DateTimeOffset? LastSyncAt,
    DateTimeOffset? NextSyncScheduled
);
```

**Tests:** 5 in-memory tests

### Gap 1.4: Configuration Classes

**File:** `src/Acode.Application/Persistence/PersistenceConfiguration.cs`
**Status:** 🔄 PENDING
**Effort:** 1 hour, 30 LOC

**What to Implement:**

```csharp
public record SessionFilter(
    SessionState? State = null,
    DateTime? CreatedAfter = null,
    DateTime? CreatedBefore = null,
    string? TaskDescriptionFilter = null
);

public record PaginationOptions(int PageSize = 50, int PageNumber = 1);

public record SyncConfiguration(
    int RetryAttempts = 3,
    TimeSpan InitialRetryDelay = default,
    double BackoffMultiplier = 2.0,
    TimeSpan MaxRetryDelay = default
);
```

**Tests:** 3 tests (validation, defaults)

### Gap 1.5: Test Doubles & Fakes

**File:** `tests/Acode.Application.Tests/Persistence/Fakes/InMemoryRunStateStore.cs`
**Status:** 🔄 PENDING
**Effort:** 1 hour, 50 LOC

**What to Implement:**

```csharp
public class InMemoryRunStateStore : IRunStateStore
{
    private readonly Dictionary<SessionId, Session> _sessions = new();
    private readonly Dictionary<TaskId, SessionTask> _tasks = new();
    private readonly Dictionary<StepId, Step> _steps = new();
    private readonly Dictionary<ToolCallId, ToolCall> _toolCalls = new();
    private readonly Dictionary<ArtifactId, Artifact> _artifacts = new();

    public Task<SessionId> CreateSessionAsync(string taskDescription, JsonDocument? metadata = null)
    {
        var session = Session.Create(taskDescription);
        _sessions[session.Id] = session;
        return Task.FromResult(session.Id);
    }

    // ... implement rest of interface for testing
}
```

**Tests:** 12 tests (CRUD operations with fake store)

---

## PHASE 1 IMPLEMENTATION WORKFLOW

### Step 1: Create Interfaces File (30 min)
```bash
touch src/Acode.Application/Persistence/IRunStateStore.cs
touch src/Acode.Application/Persistence/IOutbox.cs
touch src/Acode.Application/Persistence/ISyncService.cs
```

### Step 2: TDD for Each Interface (6.5 hours)

For each interface:
1. **RED:** Write test using interface
2. **GREEN:** Write interface definition
3. **REFACTOR:** Add XML docs

**Test Command After Each Interface:**
```bash
dotnet test --filter "FullyQualifiedName~IRunStateStoreTests|IOutboxTests|ISyncServiceTests"
```

### Step 3: Create Configuration Classes (1 hour)

Define records for query parameters, options, status objects.

### Step 4: Create Test Doubles (1 hour)

Implement InMemoryRunStateStore for testing.

### Step 5: Run Full Phase 1 Tests

```bash
dotnet test tests/Acode.Application.Tests/Persistence/ --verbosity normal
# Expected: All 33 tests passing, 0 errors
```

---

## PHASE 1 SUCCESS CRITERIA

- [x] IRunStateStore interface created with all methods
- [x] IOutbox interface created with all methods
- [x] ISyncService interface created with all methods
- [x] Configuration classes defined
- [x] Test doubles (InMemoryRunStateStore) implemented
- [x] All 33 unit tests written and passing
- [x] Zero external dependencies
- [x] No references to Task-050 or 011a implementations
- [x] Can use interfaces immediately with mock implementations

---

## PHASE 2-7: DEFERRED (Blocked by Task-050 + 011a)

These phases require actual database implementations and cannot proceed until:
1. Task-050 provides database infrastructure
2. Task-011a provides entity implementations

**Deferred Phases:**
- Phase 2: SQLite Implementation (20 hours)
- Phase 3: PostgreSQL Implementation (13 hours)
- Phase 4: Sync Core (25 hours)
- Phase 5: CLI Integration (7.5 hours)
- Phase 6: Health & Monitoring (5.5 hours)
- Phase 7: Testing (20 hours)

**Total Deferred:** 91 hours

**When to Resume:** After Task-050 + 011a complete (estimated 4-6 weeks)

---

## DECISIONS & RECOMMENDATIONS

**Decision 1: Phase 1 Only for This Window**
- Proceed with Phase 1 immediately
- Stop after interfaces and test doubles
- Defer implementation until dependencies ready
- Rationale: Maximizes parallel work, unblocks other tasks

**Decision 2: Store Implementations After Task-050**
- Don't create IRunStateStore implementations until Task-050 provides infrastructure
- Don't write database migrations until both 050 and 011a ready
- Rationale: Prevents rework when foundations change

**Decision 3: Create Detailed Phase 2-7 Plan After 050**
- Update this checklist with Phase 2-7 details once Task-050 spec available
- Adapt implementations based on Task-050 abstractions
- Rationale: Don't over-plan for blocking work

---

## GIT WORKFLOW

**Branch:** `feature/task-010-validator-system` (continuing task-011 analysis)

**Phase 1 Commits:**
```
feat(persistence): define IRunStateStore interface
feat(persistence): define IOutbox interface
feat(persistence): define ISyncService interface
feat(persistence): define persistence configuration classes
feat(persistence): implement in-memory test doubles
test(persistence): add comprehensive tests for interfaces
docs(task-011b): update checklist phase 1 complete
```

**After Phase 1 Complete:**
- Push to feature branch
- Add Phase 1 to PR description
- Note: "Phases 2-7 deferred pending Task-050 & 011a completion"

---

## PHASE 1 COMMIT MESSAGE TEMPLATE

```
feat(persistence): implement run state persistence interfaces and test doubles

- Define IRunStateStore interface for session/task/step/toolcall/artifact access
- Define IOutbox interface for sync event queueing
- Define ISyncService interface for replication orchestration
- Create configuration objects (SessionFilter, PaginationOptions, SyncConfiguration)
- Implement InMemoryRunStateStore for unit testing
- Add 33 comprehensive unit tests for all interfaces
- Deferred: Database implementations pending Task-050 & Task-011a completion

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Haiku 4.5 <noreply@anthropic.com>
```

---

## BLOCKING DEPENDENCY RESOLUTION

**To Resume Phases 2-7:**

1. **Wait for Task-050 Implementation**
   - Provides: ConnectionFactory, DbContext base, Migration framework
   - Signals readiness: PR with Task-050 merged to main

2. **Wait for Task-011a Implementation**
   - Provides: Session, Task, Step, ToolCall, Artifact entity definitions
   - Signals readiness: PR with Task-011a merged to main

3. **Create Phase 2-7 Plan**
   - Read Task-050 implementation details
   - Review actual entity definitions from 011a
   - Adapt PersistenceModelSQLiteStore, etc. to match actual infrastructure
   - Create detailed Phase 2-7 checklist

4. **Execute Phases 2-7**
   - Follow TDD for each implementation
   - Reference Task-050 patterns
   - Commit after each component

---

## CURRENT STATUS

| Phase | Status | Hours | Blocker | Next |
|-------|--------|-------|---------|------|
| 1 | Ready ✅ | 8 | None | Start immediately |
| 2-7 | Blocked ❌ | 91 | Task-050 + 011a | Wait for completion |

**Next Action:** Begin Phase 1 interface definitions immediately. Expected completion: 8 hours. Then defer Phases 2-7 until dependencies ready (target: 4-6 weeks).

---

**Status:** PARTIALLY BLOCKED - Phase 1 can proceed immediately; Phases 2-7 deferred pending Task-050 + Task-011a
**Phase 1 Duration:** 8 hours (can start now)
**Phase 2-7 Duration:** 91 hours (deferred until dependencies complete)
