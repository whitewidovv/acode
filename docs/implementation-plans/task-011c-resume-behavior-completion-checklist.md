# Task-011c Completion Checklist: Resume Behavior + Invariants

**Status:** ❌ 0% COMPLETE - FULLY BLOCKED

**Date:** 2026-01-15
**Created By:** Claude Code
**Purpose:** Comprehensive implementation checklist for session resume behavior (deferred until blocking dependencies complete)

---

## CRITICAL: READ THIS FIRST

### ⛔ THIS TASK IS 100% BLOCKED

**You cannot start ANY work on this task until all three dependencies are complete and merged to main:**

1. **Task-011a (Run Entities)** - Provides: Session, SessionId, Task, TaskId, Step, StepId, ToolCall, ToolCallId, Artifact, ArtifactId, all state enums
   - Status: NOT STARTED (0%, 42.75 hours needed)
   - Signal: PR merged with task-011a in commit message

2. **Task-050 (Workspace DB Foundation)** - Provides: Database infrastructure, transactions, connection pooling
   - Status: NOT STARTED (40-50 hours estimated)
   - Signal: PR merged with task-050 in commit message

3. **Task-049f (Sync Engine)** - Provides: SyncStatus, sync coordination, conflict resolution
   - Status: NOT STARTED (part of task-049 suite)
   - Signal: PR merged with task-049f in commit message

**BEFORE USING THIS CHECKLIST:**

```bash
# Verify all dependencies available on main
git log main --oneline | grep -E "task-011a|task-050|task-049f"

# If you don't see all three, STOP - dependencies not ready
# DO NOT proceed until all three are merged
```

---

## HOW TO USE THIS CHECKLIST (Once Dependencies Complete)

### For Fresh-Context Agent:

1. **Read Section 1** - Understand what's blocked and why
2. **Verify dependencies** - Run bash command above
3. **Read Section 2** - Understand file structure and imports needed
4. **Follow TDD Cycle** - For EACH gap:
   - RED: Write test that fails due to missing component
   - GREEN: Implement minimum code to pass test
   - REFACTOR: Clean up while keeping tests green
5. **Mark Progress** - Change `[ ]` to `[🔄]` when starting, `[✅]` when complete
6. **Commit** - After each gap complete (one commit per gap)
7. **Run Full Tests** - After each phase complete

### For Continuing Agent:

1. **Find last `[✅]` item** - That's where to resume
2. **Look at next `[🔄]` or `[ ]` item** - That's what to work on next
3. **Follow same TDD cycle** for that gap
4. **Update this checklist** with evidence after each gap
5. **Continue until all items are `[✅]`**

---

## SECTION 1: BLOCKING DEPENDENCIES & FILE STRUCTURE

### What Task-011a Provides

From `docs/tasks/refined-tasks/Epic 02/task-011a-run-session-state-machine.md`:

**Value Objects (ULID-based IDs):**
- `SessionId` - 26 chars, lexicographically sortable
- `TaskId` - Same format
- `StepId` - Same format
- `ToolCallId` - Same format
- `ArtifactId` - Same format

**Domain Entities:**
- `Session` - Aggregate root with `Id`, `State`, `CurrentTaskId`, `CurrentStepId`, `CreatedAt`, `UpdatedAt`, methods for state transitions
- `Task` - Child entity with `Id`, `ChatId`, `Status`, `CreatedAt`, `CompletedAt`
- `Step` - Child entity with `Id`, `TaskId`, `Status`, `Input`, `Output`
- `ToolCall` - Child entity with `Id`, `StepId`, `Name`, `Arguments`, `Result`, `Status`
- `Artifact` - Child entity with `Id`, `SessionId`, `Content`, `Type`, `CreatedAt`

**State Enums:**
- `SessionState` - CREATED, PLANNING, AWAITING_APPROVAL, EXECUTING, PAUSED, COMPLETED, FAILED, CANCELLED
- `TaskState` - QUEUED, IN_PROGRESS, COMPLETED, FAILED
- `StepState` - PENDING, IN_PROGRESS, COMPLETED, FAILED, SKIPPED
- `ToolCallState` - PENDING, IN_PROGRESS, COMPLETED, FAILED
- `ArtifactState` - CREATED, MODIFIED, DELETED, PRESERVED

**Event Entities:**
- `SessionEvent` - Base with `Id`, `SessionId`, `Timestamp`, `EventType`
- `SessionCreatedEvent`, `TaskCompletedEvent`, `StepCompletedEvent`, etc. (all event subtypes)

**What You'll Use:**
- Import: `using Acode.Domain.Sessions;`
- Checkpoint will serialize `Session` state to `JsonDocument`
- Replay will use `SessionEvent` entities to reconstruct state
- ContinuationPlanner will inspect `Task`, `Step`, `TaskState`, `StepState`

### What Task-050 Provides

From `docs/tasks/refined-tasks/Epic 02/task-050-workspace-db-foundation.md` (spec TBD but inferred):

**Database Infrastructure:**
- `IDbContext` - Base class for database contexts
- `IRepository<T>` - Generic repository interface
- `IUnitOfWork` - Transaction management
- Connection pooling configuration
- Migration framework (`IMigration`, `MigrationRunner`)
- Transaction support with `BeginTransactionAsync()`, `CommitAsync()`, `RollbackAsync()`

**What You'll Use:**
- Inject `IRunStateStore` (from 011b) for checkpoint persistence
- Use transactions when creating checkpoints (atomic)
- Query event log using persistence layer
- Lock management using database connections

### What Task-049f Provides

From `docs/tasks/refined-tasks/Epic 02/task-049f-sqlite-postgres-sync-engine.md`:

**Sync Infrastructure:**
- `SyncStatus` enum or record - Pending, Synced, Conflict, Failed
- `ISyncEngine` - Interface for sync operations
- `OutboxEvent` - Domain event for sync
- Conflict resolution patterns (last-write-wins assumed)

**What You'll Use:**
- `SyncStatus` in `ResumePreview` record
- Query sync status to indicate "degraded mode"
- Coordinate with sync engine during remote reconnection
- Prevent duplicate operations via sync idempotency keys

### File Structure

**This task creates these files** (spec line 1460-1481):

```
src/Acode.Application/
├── Sessions/
│   ├── Resume/
│   │   ├── IResumeService.cs              [GAP 1]
│   │   ├── ResumeService.cs               [GAP 2]
│   │   ├── CheckpointManager.cs           [GAP 3]
│   │   ├── ContinuationPlanner.cs         [GAP 4]
│   │   ├── EnvironmentValidator.cs        [GAP 5]
│   │   ├── InProgressHandler.cs           [GAP 6]
│   │   ├── SessionLockManager.cs          [GAP 11]
│   │   └── ResumeProgressDisplay.cs       [GAP 18]
│   │
│   ├── Idempotency/
│   │   ├── IdempotencyKeyGenerator.cs     [GAP 7]
│   │   └── CompletionDetector.cs          [GAP 8]
│   │
│   ├── Replay/
│   │   ├── ReplayOrderer.cs               [GAP 9]
│   │   └── DeterministicReplayer.cs       [GAP 10]
│   │
│   └── Security/
│       ├── CheckpointIntegrityValidator.cs[GAP 12]
│       ├── WorkspaceIntegrityChecker.cs   [GAP 13]
│       ├── SecretRedactionService.cs      [GAP 14]
│       └── AtomicFileValidator.cs         [GAP 15]

src/Acode.CLI/
└── Commands/
    └── ResumeCommand.cs                   [GAP 16]

tests/Acode.Application.Tests/Sessions/Resume/
├── ResumeServiceTests.cs                  [TEST 1]
├── CheckpointTests.cs                     [TEST 2]
├── IdempotencyTests.cs                    [TEST 3]
├── ReplayOrderTests.cs                    [TEST 4]
├── EnvironmentValidationTests.cs          [TEST 5]
├── ResumeIntegrationTests.cs              [TEST 6]
├── SyncResumeTests.cs                     [TEST 7]
└── InProgressHandlingTests.cs             [TEST 8]

tests/Acode.Application.Tests/E2E/
└── FullResumeTests.cs                     [TEST 9]
```

---

## SECTION 2: ACCEPTANCE CRITERIA MAPPING

**All 62 ACs from spec (lines 1198-1296) organized by implementation component:**

### Resume Initiation (AC-001 to AC-007)
- `acode resume` resumes recent session [AC-001]
- `acode resume <id>` resumes specific [AC-002]
- Only interruptible states resumable [AC-003]
- Terminal states rejected [AC-004]
- Lock acquired [AC-005]
- Lock conflict handled [AC-006]
- Session ID logged [AC-007]

**Maps to: IResumeService, ResumeService, SessionLockManager**

### State Recovery (AC-008 to AC-013)
- Full session loaded [AC-008]
- Last checkpoint identified [AC-009]
- Incomplete work identified [AC-010]
- Conversation context restored [AC-011]
- Model config restored [AC-012]
- Approval state restored [AC-013]

**Maps to: CheckpointManager, ResumeService**

### Checkpoints (AC-014 to AC-017)
- After each completed step [AC-014]
- Includes all state [AC-015]
- Atomically persisted [AC-016]
- Most recent identifiable [AC-017]

**Maps to: CheckpointManager (spec lines 1532-1551)**

### Continuation Planning (AC-018 to AC-022)
- Completed tasks skipped [AC-018]
- Completed steps skipped [AC-019]
- In-progress evaluated [AC-020]
- Pending queued [AC-021]
- Plan logged [AC-022]

**Maps to: ContinuationPlanner (spec lines 1554-1576)**

### In-Progress Handling (AC-023 to AC-027)
- Reads retryable [AC-023]
- Writes checked [AC-024]
- Partials rolled back [AC-025]
- Strategy configurable [AC-026]
- Default is rollback+retry [AC-027]

**Maps to: InProgressHandler**

### Idempotency (AC-028 to AC-032)
- Steps have idempotency keys [AC-028]
- Completed detectable [AC-029]
- Re-apply is no-op [AC-030]
- Works across restarts [AC-031]
- Keys persisted [AC-032]

**Maps to: IdempotencyKeyGenerator, CompletionDetector**

### Replay (AC-033 to AC-036)
- Ordered by timestamp [AC-033]
- Event_id tiebreaker [AC-034]
- Identical state produced [AC-035]
- Order logged [AC-036]

**Maps to: ReplayOrderer, DeterministicReplayer**

### Environment Validation (AC-037 to AC-043)
- Directory checked [AC-037]
- Files accessible [AC-038]
- Model available [AC-039]
- Config valid [AC-040]
- Failures reported [AC-041]
- Changed files warned [AC-042]
- Abort option works [AC-043]

**Maps to: EnvironmentValidator (spec lines 1579-1598)**

### Sync State (AC-044 to AC-048)
- Pending detected [AC-044]
- Degraded mode indicated [AC-045]
- Works without sync [AC-046]
- Background sync continues [AC-047]
- No network blocking [AC-048]

**Maps to: ResumeService (sync coordination)**

### Remote Reconnect (AC-049 to AC-053)
- Reconnect detected [AC-049]
- Events sync first [AC-050]
- No duplicate steps [AC-051]
- No double commits [AC-052]
- Lock coordination works [AC-053]

**Maps to: ResumeService (sync-aware resume), CompletionDetector**

### Progress Reporting (AC-054 to AC-058)
- Start announced [AC-054]
- Skip count shown [AC-055]
- Remaining shown [AC-056]
- Progress reported [AC-057]
- Completion announced [AC-058]

**Maps to: ResumeProgressDisplay**

### Error Handling (AC-059 to AC-062)
- Failure doesn't corrupt [AC-059]
- Logged with context [AC-060]
- Exit code indicates type [AC-061]
- Guidance provided [AC-062]

**Maps to: ResumeService (error handling), exit codes (spec lines 1625-1634)**

---

## SECTION 3: IMPLEMENTATION PHASES (TDD ORDER)

### PHASE 1: DOMAIN VALUE OBJECTS & ENUMS (0.5 hours)

**These are defined in spec but may need resume-specific additions:**

#### Gap 1.1: ResumeOptions Enum & Records [🔄 → ✅]

**File:** `src/Acode.Application/Sessions/Resume/IResumeService.cs`
**Status:** 🔄 PENDING (blocked by 011a + 050)
**Effort:** 0.5 hours
**Spec Reference:** Lines 1496-1513

**What to Implement:**

Enums and record types (NOT the interface yet, just types):

```csharp
namespace Acode.Application.Sessions.Resume;

public enum InProgressStrategy
{
    RollbackRetry,
    Retry,
    Prompt
}

public enum ChangedFilesAction
{
    Prompt,
    Continue,
    Abort
}

public sealed record ResumeOptions(
    InProgressStrategy Strategy = InProgressStrategy.RollbackRetry,
    bool ValidateEnvironment = true,
    ChangedFilesAction ChangedFilesAction = ChangedFilesAction.Prompt);

public sealed record ResumeResult(
    bool Success,
    SessionId SessionId,
    int SkippedSteps,
    int RemainingSteps,
    string? ErrorMessage);

public sealed record ResumePreview(
    SessionId SessionId,
    SessionState State,
    CheckpointInfo LastCheckpoint,
    int SkippedSteps,
    int RemainingSteps,
    IReadOnlyList<string> ChangedFiles,
    SyncStatus SyncStatus);

// From spec, define these record types as well
public sealed record Checkpoint(
    long Id,
    SessionId SessionId,
    TaskId? CurrentTaskId,
    StepId? CurrentStepId,
    SessionState State,
    JsonDocument ConversationState,
    DateTimeOffset CreatedAt);

public sealed record CheckpointInfo(
    long Id,
    DateTimeOffset CreatedAt,
    int SkippedTasks,
    int SkippedSteps);

public sealed record ContinuationPlan(
    IReadOnlyList<TaskId> SkippedTasks,
    IReadOnlyList<StepId> SkippedSteps,
    StepId? InProgressStep,
    IReadOnlyList<StepId> PendingSteps,
    InProgressAction InProgressAction);

public enum InProgressAction
{
    Skip,
    Retry,
    RollbackAndRetry
}

public sealed record EnvironmentValidationResult(
    bool IsValid,
    IReadOnlyList<ValidationError> Errors,
    IReadOnlyList<ValidationWarning> Warnings,
    IReadOnlyList<ChangedFile> ChangedFiles);

public sealed record ValidationError(string Code, string Message);
public sealed record ValidationWarning(string Code, string Message);
public sealed record ChangedFile(string Path, DateTimeOffset ModifiedAt, ChangeType Type);

public enum ChangeType { Modified, Deleted, Created }
```

**Tests:** 3 unit tests in ResumeServiceTests.cs
- Test ResumeOptions defaults
- Test ChangedFilesAction enum values
- Test record immutability

**Success Criteria:**
- [ ] All enums and records compile
- [ ] All defaults match spec (line 1497-1498)
- [ ] 3 unit tests passing
- [ ] No warnings

---

### PHASE 2: INTERFACES & ABSTRACTIONS (2 hours)

#### Gap 2.1: IResumeService Interface [🔄 → ✅]

**File:** `src/Acode.Application/Sessions/Resume/IResumeService.cs`
**Status:** 🔄 PENDING (blocked by 011a + 050)
**Effort:** 1 hour
**Spec Reference:** Lines 1484-1529

**What to Implement:**

From spec, create complete interface with all methods:

```csharp
namespace Acode.Application.Sessions.Resume;

public interface IResumeService
{
    /// <summary>
    /// Resume an interrupted session, optionally returning preview instead of executing
    /// </summary>
    Task<ResumeResult> ResumeAsync(SessionId? sessionId, ResumeOptions options, CancellationToken ct);

    /// <summary>
    /// Preview what would happen if resume executed (dry-run)
    /// </summary>
    Task<ResumePreview> PreviewAsync(SessionId sessionId, CancellationToken ct);

    /// <summary>
    /// List all sessions that can be resumed (PAUSED, EXECUTING states)
    /// </summary>
    Task<IReadOnlyList<Session>> GetResumableAsync(CancellationToken ct);
}
```

**Tests:** 5 unit tests in ResumeServiceTests.cs
- Test interface definition compiles
- Test all method signatures present
- Test return types correct

**Success Criteria:**
- [ ] Interface compiles
- [ ] All 3 methods present and correct per spec
- [ ] XML docs present for all methods
- [ ] 5 unit tests passing

---

#### Gap 2.2: Other Interfaces [🔄 → ✅]

**Files:**
- CheckpointManager (public methods, no interface needed)
- ContinuationPlanner (public methods, no interface needed)
- EnvironmentValidator (public methods, no interface needed)

These are application services, not interfaces, per spec.

**Status:** Define in implementation phase (not tests first)

---

### PHASE 3: CHECKPOINT MANAGER (3 hours)

#### Gap 3.1: CheckpointManager Class [🔄 → ✅]

**File:** `src/Acode.Application/Sessions/Resume/CheckpointManager.cs`
**Status:** 🔄 PENDING (blocked by 011a + 050)
**Effort:** 3 hours
**Spec Reference:** Lines 1532-1551

**What to Implement:**

Complete class with full implementation:

```csharp
namespace Acode.Application.Sessions.Resume;

public sealed class CheckpointManager
{
    private readonly IRunStateStore _store;  // From 011b
    private readonly IUnitOfWork _unitOfWork; // From 050

    public CheckpointManager(IRunStateStore store, IUnitOfWork unitOfWork)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Create checkpoint atomically after step completion
    /// Includes full session state: entities, conversation, model config
    /// </summary>
    public async Task<Checkpoint> CreateAsync(Session session, CancellationToken ct)
    {
        // Validate session is in resumable state
        // Serialize full session state to JsonDocument
        // Create checkpoint record
        // Persist atomically using transaction
        // Return checkpoint with ID
        throw new NotImplementedException();
    }

    /// <summary>
    /// Get most recent checkpoint for session
    /// Used to determine resume starting point
    /// </summary>
    public async Task<Checkpoint?> GetLatestAsync(SessionId sessionId, CancellationToken ct)
    {
        // Query checkpoint store, order by CreatedAt DESC, take 1
        // Return null if none found
        throw new NotImplementedException();
    }

    /// <summary>
    /// Get all checkpoints for session (for rollback scenarios)
    /// </summary>
    public async Task<IReadOnlyList<Checkpoint>> GetAllAsync(SessionId sessionId, CancellationToken ct)
    {
        // Query all checkpoints for session, ordered by CreatedAt DESC
        // Return read-only list
        throw new NotImplementedException();
    }
}
```

**Tests:** 8 unit tests in CheckpointTests.cs
- RED test: Create checkpoint after step (fails, no implementation)
- RED test: Checkpoint includes all state
- RED test: Checkpoint atomically persisted
- RED test: Get latest returns most recent
- RED test: Get latest returns null when none exist
- RED test: Get all returns ordered list
- RED test: Invalid state cannot be checkpointed
- RED test: Checkpoint immutable after creation

**Success Criteria:**
- [ ] Class compiles with constructor injection
- [ ] CreateAsync persists checkpoint atomically (AC-016)
- [ ] GetLatestAsync identifies most recent (AC-017)
- [ ] GetAllAsync returns full history
- [ ] 8 unit tests passing
- [ ] All state included in checkpoint (AC-015)

---

### PHASE 4: CONTINUATION PLANNER (2 hours)

#### Gap 4.1: ContinuationPlanner Class [🔄 → ✅]

**File:** `src/Acode.Application/Sessions/Resume/ContinuationPlanner.cs`
**Status:** 🔄 PENDING (blocked by 011a + 050)
**Effort:** 2 hours
**Spec Reference:** Lines 1554-1576

**What to Implement:**

```csharp
namespace Acode.Application.Sessions.Resume;

public sealed class ContinuationPlanner
{
    /// <summary>
    /// Create continuation plan from session + checkpoint
    /// Identifies what to skip vs what to retry vs what to execute
    /// </summary>
    public ContinuationPlan Create(Session session, Checkpoint checkpoint)
    {
        // Extract completed tasks from session state
        // Extract completed steps from checkpoint
        // Identify in-progress step (if any)
        // Determine all pending steps
        // Determine in-progress action (AC-027: default RollbackAndRetry)
        // Return plan with all info
        throw new NotImplementedException();
    }
}
```

**Tests:** 8 unit tests in ContinuationPlanner tests (or ResumeServiceTests.cs)
- RED: Skips completed tasks (AC-018)
- RED: Skips completed steps (AC-019)
- RED: Identifies in-progress step (AC-020)
- RED: Queues pending steps (AC-021)
- RED: Default action is RollbackAndRetry (AC-027)
- RED: Plan can be logged (AC-022)
- RED: All task IDs correct
- RED: All step IDs correct

**Success Criteria:**
- [ ] Class compiles
- [ ] Creates continuation plan from session + checkpoint
- [ ] Completed tasks skipped (AC-018)
- [ ] Completed steps skipped (AC-019)
- [ ] In-progress step evaluated (AC-020)
- [ ] Pending steps queued (AC-021)
- [ ] 8 unit tests passing

---

### PHASE 5: ENVIRONMENT VALIDATOR (3 hours)

#### Gap 5.1: EnvironmentValidator Class [🔄 → ✅]

**File:** `src/Acode.Application/Sessions/Resume/EnvironmentValidator.cs`
**Status:** 🔄 PENDING (blocked by 011a + 050)
**Effort:** 3 hours
**Spec Reference:** Lines 1579-1598

**What to Implement:**

```csharp
namespace Acode.Application.Sessions.Resume;

public sealed class EnvironmentValidator
{
    private readonly IFileSystemAbstraction _fileSystem;  // Injected
    private readonly IModelAvailabilityChecker _modelChecker;  // Injected
    private readonly IConfigurationValidator _configValidator;  // Injected

    public EnvironmentValidator(
        IFileSystemAbstraction fileSystem,
        IModelAvailabilityChecker modelChecker,
        IConfigurationValidator configValidator)
    {
        _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        _modelChecker = modelChecker ?? throw new ArgumentNullException(nameof(modelChecker));
        _configValidator = configValidator ?? throw new ArgumentNullException(nameof(configValidator));
    }

    /// <summary>
    /// Validate environment is safe for resume
    /// Checks: directory, files, model, config, file changes
    /// </summary>
    public async Task<EnvironmentValidationResult> ValidateAsync(Session session, CancellationToken ct)
    {
        var errors = new List<ValidationError>();
        var warnings = new List<ValidationWarning>();
        var changedFiles = new List<ChangedFile>();

        // AC-037: Check working directory exists
        // AC-038: Check required files accessible
        // AC-039: Check model available
        // AC-040: Check config valid
        // AC-042: Detect file changes
        // Return result with errors, warnings, changed files

        throw new NotImplementedException();
    }
}
```

**Tests:** 10 unit tests in EnvironmentValidationTests.cs
- RED: Directory check (AC-037)
- RED: File accessibility (AC-038)
- RED: Model availability (AC-039)
- RED: Config validation (AC-040)
- RED: Failures reported (AC-041)
- RED: Changed files detected (AC-042)
- RED: Abort option works (AC-043)
- RED: Returns all warnings
- RED: Returns all changed files
- RED: Validation succeeds when all pass

**Success Criteria:**
- [ ] Validates working directory exists (AC-037)
- [ ] Checks required files accessible (AC-038)
- [ ] Verifies model available (AC-039)
- [ ] Validates configuration (AC-040)
- [ ] Detects file changes with timestamps (AC-042)
- [ ] 10 unit tests passing

---

### PHASE 6: IDEMPOTENCY SYSTEM (2 hours)

#### Gap 6.1: IdempotencyKeyGenerator [🔄 → ✅]

**File:** `src/Acode.Application/Sessions/Idempotency/IdempotencyKeyGenerator.cs`
**Status:** 🔄 PENDING
**Effort:** 1 hour
**Spec Reference:** Lines 1601-1610

**What to Implement:**

```csharp
namespace Acode.Application.Sessions.Idempotency;

public sealed class IdempotencyKeyGenerator
{
    /// <summary>
    /// Generate deterministic idempotency key from session/step/attempt
    /// AC-028: Every step has idempotency key
    /// </summary>
    public string Generate(SessionId sessionId, StepId stepId, int attemptNumber)
    {
        // Create deterministic input: sessionId + stepId + attemptNumber
        // Hash with SHA256
        // Return base64-encoded hash
        throw new NotImplementedException();
    }

    /// <summary>
    /// Parse key back to components (for verification)
    /// </summary>
    public bool Parse(string key, out SessionId sessionId, out StepId stepId, out int attemptNumber)
    {
        // Try to parse key back to components
        // Return true if successful, false if invalid format
        throw new NotImplementedException();
    }
}
```

**Tests:** 6 unit tests in IdempotencyTests.cs
- RED: Generate produces unique keys (AC-028)
- RED: Generate is deterministic (same input = same key)
- RED: Parse recovers components
- RED: Parse fails on invalid key
- RED: Handles large attempt numbers
- RED: Keys are URL-safe

**Success Criteria:**
- [ ] Generate creates unique keys (AC-028)
- [ ] Generate is deterministic (AC-031)
- [ ] Parse recovers original components
- [ ] 6 unit tests passing

---

#### Gap 6.2: CompletionDetector [🔄 → ✅]

**File:** `src/Acode.Application/Sessions/Idempotency/CompletionDetector.cs`
**Status:** 🔄 PENDING
**Effort:** 1 hour

**What to Implement:**

```csharp
namespace Acode.Application.Sessions.Idempotency;

public sealed class CompletionDetector
{
    private readonly IRunStateStore _store;  // Query event log
    private readonly IdempotencyKeyGenerator _keyGenerator;

    public CompletionDetector(IRunStateStore store)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _keyGenerator = new IdempotencyKeyGenerator();
    }

    /// <summary>
    /// Detect if step already completed and applied
    /// AC-029: Completed steps detectable
    /// AC-030: Re-apply is no-op if already applied
    /// </summary>
    public async Task<bool> IsAlreadyCompletedAsync(
        SessionId sessionId,
        StepId stepId,
        CancellationToken ct)
    {
        // Generate idempotency key for step
        // Query event log for key
        // Return true if found (already applied)
        // Return false if not found (needs execution)
        throw new NotImplementedException();
    }
}
```

**Tests:** 5 unit tests in IdempotencyTests.cs
- RED: Detects completed steps (AC-029)
- RED: Returns false for new steps
- RED: Returns true after completion
- RED: Handles multiple attempts
- RED: Prevents duplicate application (AC-030)

**Success Criteria:**
- [ ] Detects completed steps (AC-029)
- [ ] Returns false for new steps
- [ ] Prevents duplicate execution (AC-030)
- [ ] 5 unit tests passing

---

### PHASE 7: REPLAY SYSTEM (3 hours)

#### Gap 7.1: ReplayOrderer [🔄 → ✅]

**File:** `src/Acode.Application/Sessions/Replay/ReplayOrderer.cs`
**Status:** 🔄 PENDING
**Effort:** 1.5 hours
**Spec Reference:** Line 534-538 (Functional Requirements for replay)

**What to Implement:**

```csharp
namespace Acode.Application.Sessions.Replay;

public sealed class ReplayOrderer
{
    /// <summary>
    /// Order events by timestamp, using event_id as tiebreaker
    /// AC-033: Events ordered by timestamp
    /// AC-034: Event_id used as tiebreaker
    /// AC-036: Replay order logged for audit
    /// </summary>
    public IReadOnlyList<SessionEvent> OrderForReplay(IEnumerable<SessionEvent> events)
    {
        // Sort by timestamp ascending
        // Use event_id as tiebreaker for same-timestamp events
        // Log replay order for audit trail
        // Return ordered list
        throw new NotImplementedException();
    }
}
```

**Tests:** 6 unit tests in ReplayOrderTests.cs
- RED: Orders by timestamp (AC-033)
- RED: Uses event_id tiebreaker (AC-034)
- RED: Detects clock skew
- RED: Logs replay order (AC-036)
- RED: Handles empty list
- RED: Handles single event

**Success Criteria:**
- [ ] Orders by timestamp (AC-033)
- [ ] Uses event_id tiebreaker (AC-034)
- [ ] Logs replay order (AC-036)
- [ ] 6 unit tests passing

---

#### Gap 7.2: DeterministicReplayer [🔄 → ✅]

**File:** `src/Acode.Application/Sessions/Replay/DeterministicReplayer.cs`
**Status:** 🔄 PENDING
**Effort:** 2 hours

**What to Implement:**

```csharp
namespace Acode.Application.Sessions.Replay;

public sealed class DeterministicReplayer
{
    private readonly ReplayOrderer _orderer;

    public DeterministicReplayer()
    {
        _orderer = new ReplayOrderer();
    }

    /// <summary>
    /// Replay events in deterministic order to reconstruct session state
    /// AC-035: Replay produces identical state
    /// AC-036: Order logged for audit
    /// </summary>
    public async Task<Session> ReplayAsync(
        Session baseSession,
        IEnumerable<SessionEvent> events,
        CancellationToken ct)
    {
        // Order events deterministically
        // Replay each event, applying to session state
        // Capture state after each replay
        // Detect non-determinism (should not change with replay)
        // Return final session state
        throw new NotImplementedException();
    }
}
```

**Tests:** 8 unit tests in ReplayOrderTests.cs or separate DeterministicReplayerTests.cs
- RED: Replays events in order (AC-035)
- RED: Produces identical state
- RED: Detects non-determinism
- RED: Handles partial replay
- RED: Validates event sequence
- RED: Logs replay operations
- RED: Preserves immutability
- RED: Handles empty event list

**Success Criteria:**
- [ ] Replays events deterministically (AC-035)
- [ ] Produces identical results on repeated replay
- [ ] 8 unit tests passing

---

### PHASE 8: RESUME SERVICE CORE (4 hours)

#### Gap 8.1: ResumeService Implementation [🔄 → ✅]

**File:** `src/Acode.Application/Sessions/Resume/ResumeService.cs`
**Status:** 🔄 PENDING
**Effort:** 4 hours

**What to Implement:**

```csharp
namespace Acode.Application.Sessions.Resume;

public sealed class ResumeService : IResumeService
{
    private readonly IRunStateStore _store;
    private readonly CheckpointManager _checkpointManager;
    private readonly ContinuationPlanner _continuationPlanner;
    private readonly EnvironmentValidator _environmentValidator;
    private readonly InProgressHandler _inProgressHandler;
    private readonly ReplayOrderer _replayOrderer;
    private readonly DeterministicReplayer _determinisitcReplayer;
    private readonly SessionLockManager _lockManager;
    private readonly CompletionDetector _completionDetector;
    private readonly ISyncEngine _syncEngine; // From 049f

    public ResumeService(
        IRunStateStore store,
        CheckpointManager checkpointManager,
        ContinuationPlanner continuationPlanner,
        EnvironmentValidator environmentValidator,
        InProgressHandler inProgressHandler,
        ReplayOrderer replayOrderer,
        DeterministicReplayer deterministicReplayer,
        SessionLockManager lockManager,
        CompletionDetector completionDetector,
        ISyncEngine syncEngine)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        // ... assign other dependencies
    }

    /// <summary>
    /// AC-001-007: Resume initiation logic
    /// AC-008-013: State recovery
    /// AC-023-027: In-progress handling
    /// AC-044-048: Sync state handling
    /// AC-049-053: Remote reconnection
    /// </summary>
    public async Task<ResumeResult> ResumeAsync(SessionId? sessionId, ResumeOptions options, CancellationToken ct)
    {
        try
        {
            // Step 1: Identify session to resume (AC-001-002)
            // Step 2: Validate state is resumable (AC-003-004)
            // Step 3: Acquire session lock (AC-005-006)
            // Step 4: Load full session (AC-008)
            // Step 5: Get latest checkpoint (AC-009)
            // Step 6: Validate environment (AC-037-043)
            // Step 7: Create continuation plan (AC-018-022)
            // Step 8: Handle in-progress step (AC-023-027)
            // Step 9: Check sync state (AC-044-048)
            // Step 10: Coordinate with remote if reconnected (AC-049-053)
            // Step 11: Execute remaining steps (replay + execute)
            // Step 12: Return result with skip/remaining counts
            // Step 13: Log session ID and metrics (AC-007, AC-060)
            throw new NotImplementedException();
        }
        catch (Exception ex)
        {
            // AC-059: Failure doesn't corrupt state
            // AC-060: Logged with context
            // AC-061: Exit code indicates type
            // AC-062: Guidance provided
            throw;
        }
    }

    public async Task<ResumePreview> PreviewAsync(SessionId sessionId, CancellationToken ct)
    {
        // Like ResumeAsync but return preview instead of executing
        // AC-055-057: Show skip count, remaining, progress
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<Session>> GetResumableAsync(CancellationToken ct)
    {
        // AC-001: Find resumable sessions
        // Query for sessions in PAUSED, EXECUTING, AWAITING_APPROVAL states
        // Return list
        throw new NotImplementedException();
    }
}
```

**Tests:** 15 unit tests in ResumeServiceTests.cs
- RED: Resume paused session (AC-001)
- RED: Resume specific session (AC-002)
- RED: Reject terminal states (AC-004)
- RED: Acquire lock (AC-005)
- RED: Handle lock conflict (AC-006)
- RED: Load full session (AC-008)
- RED: Identify last checkpoint (AC-009)
- RED: Build continuation plan (AC-022)
- RED: Handle environment validation (AC-037-043)
- RED: Handle sync pending (AC-044)
- RED: Prevent duplicates (AC-051)
- RED: Correct exit codes (AC-061)
- RED: Log session ID (AC-007)
- RED: Preview dry-run (AC-055-057)
- RED: Get resumable list

**Success Criteria:**
- [ ] Resume works for PAUSED state (AC-001)
- [ ] Resume works for EXECUTING state
- [ ] Terminal states rejected (AC-004)
- [ ] Lock acquired and released (AC-005-006)
- [ ] Full session loaded (AC-008)
- [ ] Last checkpoint identified (AC-009)
- [ ] 15 unit tests passing
- [ ] Error handling complete (AC-059-062)

---

### PHASE 9: SECURITY & LOCKING (3 hours)

#### Gap 9.1: SessionLockManager [🔄 → ✅]

**File:** `src/Acode.Application/Sessions/Resume/SessionLockManager.cs`
**Status:** 🔄 PENDING
**Effort:** 1.5 hours

**What to Implement:**

```csharp
namespace Acode.Application.Sessions.Resume;

public sealed class SessionLockManager
{
    private readonly IFileSystem _fileSystem;  // Or ILockService from 050

    /// <summary>
    /// AC-005-006: Acquire session lock
    /// AC-059: Lock-based isolation prevents concurrent resume
    /// </summary>
    public async Task<IDisposable> AcquireLockAsync(SessionId sessionId, TimeSpan timeout, CancellationToken ct)
    {
        // Create lock file: .acode/locks/session-{id}.lock
        // Write PID and timestamp
        // Retry with exponential backoff if conflict
        // Return disposable that releases lock on dispose
        throw new NotImplementedException();
    }

    /// <summary>
    /// Check if lock is stale (process dead)
    /// </summary>
    public async Task<bool> IsLockStaleAsync(SessionId sessionId, CancellationToken ct)
    {
        // Read lock file
        // Check if process PID still alive
        // Return true if process dead (stale lock)
        throw new NotImplementedException();
    }
}
```

**Tests:** 6 unit tests in ResumeServiceTests.cs
- RED: Acquires lock (AC-005)
- RED: Handles lock conflict (AC-006)
- RED: Detects stale locks
- RED: Releases lock on dispose
- RED: Timeout handling
- RED: Concurrent access prevention

**Success Criteria:**
- [ ] Acquires exclusive lock (AC-005)
- [ ] Handles conflict (AC-006)
- [ ] Detects stale locks
- [ ] 6 unit tests passing

---

#### Gap 9.2: Security Validators [🔄 → ✅]

**Files:**
- CheckpointIntegrityValidator.cs
- WorkspaceIntegrityChecker.cs
- SecretRedactionService.cs
- AtomicFileValidator.cs

**Status:** 🔄 PENDING
**Effort:** 1.5 hours total (spec lines 162-399)

These implement 5 security threats from spec (lines 162-443).

Implement minimal versions:
- CheckpointIntegrityValidator: HMAC signature on checkpoint
- WorkspaceIntegrityChecker: File hash verification
- SecretRedactionService: Pattern-based secret detection
- AtomicFileValidator: Exclusive lock during validation

**Tests:** 8 unit tests for security components

**Success Criteria:**
- [ ] All 5 security threats mitigated
- [ ] 8 security unit tests passing

---

### PHASE 10: IN-PROGRESS HANDLER & CONTEXT (2 hours)

#### Gap 10.1: InProgressHandler [🔄 → ✅]

**File:** `src/Acode.Application/Sessions/Resume/InProgressHandler.cs`
**Status:** 🔄 PENDING
**Effort:** 2 hours

**What to Implement (spec description lines 19-21):**

```csharp
namespace Acode.Application.Sessions.Resume;

public sealed class InProgressHandler
{
    private readonly CompletionDetector _completionDetector;

    /// <summary>
    /// AC-023-027: Determine how to handle in-progress step
    /// Read operations: retry
    /// Write operations: check if already applied, if so skip
    /// Partial writes: rollback first, then retry
    /// </summary>
    public InProgressAction DetermineAction(Step inProgressStep, ContinuationPlan plan)
    {
        // Determine step type (read/write/compute)
        // Check idempotency status
        // Return action: Skip, Retry, or RollbackAndRetry
        throw new NotImplementedException();
    }

    /// <summary>
    /// AC-025: Rollback partial writes
    /// </summary>
    public async Task RollbackAsync(Step partialStep, CancellationToken ct)
    {
        // Rollback partial writes
        // Restore file to checkpoint state if needed
        throw new NotImplementedException();
    }

    /// <summary>
    /// AC-024: Check if write already applied
    /// </summary>
    public async Task<bool> IsWriteAlreadyAppliedAsync(Step writeStep, CancellationToken ct)
    {
        // Check file content against expected final state
        // Return true if matches, false if needs re-execution
        throw new NotImplementedException();
    }
}
```

**Tests:** 8 unit tests in InProgressHandlingTests.cs (integration level)
- RED: Retries read operations (AC-023)
- RED: Checks write completion (AC-024)
- RED: Rolls back partials (AC-025)
- RED: Strategy configurable (AC-026)
- RED: Default is RollbackAndRetry (AC-027)
- RED: Handles all step types
- RED: Preserves immutability
- RED: Logs actions

**Success Criteria:**
- [ ] Reads retryable (AC-023)
- [ ] Writes checked (AC-024)
- [ ] Partials rolled back (AC-025)
- [ ] 8 unit tests passing

---

### PHASE 11: CLI INTEGRATION (2 hours)

#### Gap 11.1: ResumeCommand [🔄 → ✅]

**File:** `src/Acode.Cli/Commands/ResumeCommand.cs`
**Status:** 🔄 PENDING
**Effort:** 2 hours

**What to Implement:**

```csharp
namespace Acode.Cli.Commands;

[Command("resume")]
public sealed class ResumeCommand : ICommand
{
    private readonly IResumeService _resumeService;

    [Option("--session", "-s", Description = "Session ID to resume (default: most recent)")]
    public string? SessionId { get; set; }

    [Option("--dry-run", Description = "Preview without executing")]
    public bool DryRun { get; set; }

    [Option("--skip-validation", Description = "Skip environment validation")]
    public bool SkipValidation { get; set; }

    /// <summary>
    /// Execute resume command
    /// AC-054-058: Progress reporting
    /// AC-061: Exit codes
    /// </summary>
    public async Task<int> ExecuteAsync(CancellationToken ct)
    {
        try
        {
            // AC-054: Announce resume start
            // Call IResumeService.ResumeAsync() or PreviewAsync()
            // AC-055-057: Show skip count, remaining steps, progress
            // AC-058: Announce completion
            // Return exit code (AC-061)
            throw new NotImplementedException();
        }
        catch (Exception ex)
        {
            // AC-062: Provide guidance
            // Log error
            // Return appropriate exit code
            return 1; // General failure
        }
    }
}
```

**Tests:** 5 unit tests in CLI tests
- RED: Resume command created
- RED: Session ID option parsed
- RED: Dry-run option works
- RED: Exit codes correct (AC-061)
- RED: Progress displayed (AC-055-057)

**Success Criteria:**
- [ ] `acode resume` works (AC-001)
- [ ] `acode resume <id>` works (AC-002)
- [ ] Exit codes correct (AC-061)
- [ ] Progress shown (AC-055-058)
- [ ] 5 unit tests passing

---

### PHASE 12: COMPREHENSIVE TESTING (8 hours)

#### Test Suite 1: Unit Tests (4 hours)

**File:** `tests/Acode.Application.Tests/Sessions/Resume/ResumeServiceTests.cs`
**Count:** ~25 unit tests

Topics:
- Resume initiation (7 tests) [AC-001-007]
- State recovery (6 tests) [AC-008-013]
- Checkpoint operations (4 tests) [AC-014-017]
- Continuation planning (4 tests) [AC-018-022]

#### Test Suite 2: Integration Tests (3 hours)

**File:** `tests/Acode.Application.Tests/Sessions/Resume/ResumeIntegrationTests.cs`
**Count:** ~15 integration tests

Topics:
- Resume after simulated crash
- Skip completed work in multi-step session
- Handle workspace file modifications
- Sync coordination during resume
- Reconnect handling
- Lock management under concurrency
- Idempotency across multiple resumes
- Performance benchmarks

#### Test Suite 3: E2E Tests (1 hour)

**File:** `tests/Acode.Application.Tests/E2E/FullResumeTests.cs`
**Count:** ~8 E2E tests

Topics:
- Complete session from start to resume
- Produce identical result to non-interrupted run
- Multiple interrupts and resumes
- All 10 user verification scenarios from spec (lines 1382-1452)

---

### PHASE 13: AUDIT & DOCUMENTATION (1 hour)

#### Gap 13.1: Final Verification Checklist [🔄 → ✅]

**Verify all 62 ACs:**

**Resume Initiation (AC-001 to AC-007):**
- [ ] AC-001: `acode resume` without args resumes recent
- [ ] AC-002: `acode resume <id>` resumes specific session
- [ ] AC-003: Only interruptible states resumable (PAUSED, EXECUTING, AWAITING_APPROVAL)
- [ ] AC-004: Terminal states rejected (COMPLETED, FAILED, CANCELLED)
- [ ] AC-005: Lock acquired on resume
- [ ] AC-006: Lock conflict handled with error
- [ ] AC-007: Session ID logged

**State Recovery (AC-008 to AC-013):**
- [ ] AC-008: Full session loaded from persistence
- [ ] AC-009: Last checkpoint identified correctly
- [ ] AC-010: Incomplete work identified
- [ ] AC-011: Conversation context restored
- [ ] AC-012: Model configuration restored
- [ ] AC-013: Approval state restored

**Checkpoints (AC-014 to AC-017):**
- [ ] AC-014: Checkpoint created after each completed step
- [ ] AC-015: Checkpoint includes all state (entities, conversation, config)
- [ ] AC-016: Checkpoint atomically persisted (transaction)
- [ ] AC-017: Most recent checkpoint identifiable

**Continuation Planning (AC-018 to AC-022):**
- [ ] AC-018: Completed tasks skipped
- [ ] AC-019: Completed steps skipped
- [ ] AC-020: In-progress steps evaluated
- [ ] AC-021: Pending steps queued
- [ ] AC-022: Plan logged before execution

**In-Progress Handling (AC-023 to AC-027):**
- [ ] AC-023: Read operations retryable
- [ ] AC-024: Write operations checked for completion
- [ ] AC-025: Partial writes rolled back first
- [ ] AC-026: Strategy configurable via ResumeOptions
- [ ] AC-027: Default strategy is RollbackAndRetry

**Idempotency (AC-028 to AC-032):**
- [ ] AC-028: Every step has idempotency key
- [ ] AC-029: Completed steps detectable via idempotency key
- [ ] AC-030: Re-applying completed step is no-op
- [ ] AC-031: Idempotency works across restarts
- [ ] AC-032: Idempotency keys persisted

**Deterministic Replay (AC-033 to AC-036):**
- [ ] AC-033: Events ordered by timestamp
- [ ] AC-034: Event_id used as timestamp tiebreaker
- [ ] AC-035: Replay produces identical session state
- [ ] AC-036: Replay order logged for audit

**Environment Validation (AC-037 to AC-043):**
- [ ] AC-037: Working directory checked
- [ ] AC-038: Required files accessible
- [ ] AC-039: Model availability checked
- [ ] AC-040: Configuration validity checked
- [ ] AC-041: Validation failures reported
- [ ] AC-042: Changed files warned
- [ ] AC-043: Abort option available

**Sync State (AC-044 to AC-048):**
- [ ] AC-044: Pending sync detected
- [ ] AC-045: Degraded mode indicated
- [ ] AC-046: Resume works without sync
- [ ] AC-047: Background sync continues
- [ ] AC-048: No blocking on network

**Remote Reconnection (AC-049 to AC-053):**
- [ ] AC-049: Reconnect detected
- [ ] AC-050: Pending events synced first
- [ ] AC-051: Duplicate steps prevented
- [ ] AC-052: Double commits prevented
- [ ] AC-053: Lock coordination works

**Progress Reporting (AC-054 to AC-058):**
- [ ] AC-054: Resume start announced
- [ ] AC-055: Skip count shown
- [ ] AC-056: Remaining work shown
- [ ] AC-057: Progress reported during execution
- [ ] AC-058: Completion announced

**Error Handling (AC-059 to AC-062):**
- [ ] AC-059: Resume failure doesn't corrupt state
- [ ] AC-060: Errors logged with context
- [ ] AC-061: Exit codes correct (0/1/14/15/16/17)
- [ ] AC-062: Actionable guidance provided

---

## SECTION 4: GIT WORKFLOW & COMMIT STRATEGY

**Branch:** `feature/task-011-run-session-state-machine` (or new branch after blockers resolved)

**Commit After Each Gap Complete:**

```bash
# After Gap 1.1 (ResumeOptions enums)
git add src/Acode.Application/Sessions/Resume/IResumeService.cs
git commit -m "feat(resume): define ResumeOptions enums and record types

- Add InProgressStrategy enum (RollbackRetry, Retry, Prompt)
- Add ChangedFilesAction enum (Prompt, Continue, Abort)
- Add ResumeOptions, ResumeResult, ResumePreview records
- Add Checkpoint, ContinuationPlan, EnvironmentValidationResult types
- 3 unit tests for defaults and immutability
- Reference: spec lines 1496-1598"

# After Gap 2.1 (IResumeService interface)
git add src/Acode.Application/Sessions/Resume/IResumeService.cs
git commit -m "feat(resume): define IResumeService interface

- Add ResumeAsync method (primary entry point)
- Add PreviewAsync method (dry-run)
- Add GetResumableAsync method (list resumable sessions)
- XML docs for all methods
- 5 unit tests for signature verification
- Reference: spec lines 1484-1494"

# Continue for each gap...
# After each phase complete, run full test suite:
dotnet test tests/Acode.Application.Tests/Sessions/Resume/ --verbosity normal

# After all phases complete:
git add .
git commit -m "feat(task-011c): complete resume behavior and invariants

- Phase 1: Enums and record types (ResumeOptions, etc.)
- Phase 2: Interfaces (IResumeService)
- Phase 3: Checkpoint management (CheckpointManager)
- Phase 4: Continuation planning (ContinuationPlanner)
- Phase 5: Environment validation (EnvironmentValidator)
- Phase 6: Idempotency system (IdempotencyKeyGenerator, CompletionDetector)
- Phase 7: Replay system (ReplayOrderer, DeterministicReplayer)
- Phase 8: Resume service core (ResumeService implementation)
- Phase 9: Security and locking (SessionLockManager, security validators)
- Phase 10: In-progress handling (InProgressHandler)
- Phase 11: CLI integration (ResumeCommand)
- Phase 12: Comprehensive testing (48+ tests, all passing)

All 62 Acceptance Criteria verified complete
Build: GREEN (0 errors, 0 warnings)
Tests: 48+ passing (unit, integration, E2E)
Coverage: > 90% for all components
Audit: PASSED

Reference: spec lines 1456-1700
🤖 Generated with Claude Code"
```

---

## SECTION 5: SUCCESS CRITERIA CHECKLIST

**Before PR Creation, Verify:**

### Build Status
- [ ] `dotnet build src/Acode.Application/ src/Acode.Cli/` - GREEN (0 errors)
- [ ] No warnings (or approved warnings only)
- [ ] All projects build without issues

### Test Status
- [ ] `dotnet test tests/Acode.Application.Tests/Sessions/Resume/ --verbosity normal` - ALL PASSING
- [ ] Unit tests: 25+ passing
- [ ] Integration tests: 15+ passing
- [ ] E2E tests: 8+ passing
- [ ] Total: 48+ tests passing

### Code Quality
- [ ] No `NotImplementedException` anywhere
- [ ] No `TODO` comments (or all addressed)
- [ ] No public method without tests
- [ ] Clean Architecture boundaries respected
- [ ] No direct `DateTime.Now` (use injected clock)
- [ ] All async methods have proper `await`

### Functional Requirements
- [ ] All 62 Acceptance Criteria verified complete (Section 5)
- [ ] Resume works for PAUSED state
- [ ] Resume works for EXECUTING state
- [ ] Terminal states properly rejected
- [ ] Completed work properly skipped
- [ ] Incomplete work properly handled
- [ ] Environment validation works
- [ ] Changed files detected
- [ ] Idempotency prevents duplicates
- [ ] Deterministic replay verified
- [ ] Crash during resume doesn't corrupt state
- [ ] Exit codes correct
- [ ] Lock management functional
- [ ] Sync coordination working

### Non-Functional Requirements
- [ ] State recovery < 500ms (target 250ms)
- [ ] Environment validation < 2s (target 1s)
- [ ] Continuation planning < 100ms (target 50ms)
- [ ] Checkpoint creation < 100ms (target 50ms)
- [ ] No performance degradation vs fresh start

### Security
- [ ] All 5 security threats mitigated (spec lines 162-443)
- [ ] Lock hijacking prevented
- [ ] Approval bypass prevented
- [ ] Workspace file injection prevented
- [ ] Secrets not exposed in checkpoints
- [ ] TOCTOU attacks prevented

### Documentation
- [ ] Implementation Prompt followed exactly
- [ ] All code patterns from spec implemented
- [ ] Error codes defined (ACODE-RESUME-001 through 007)
- [ ] Exit codes defined (0, 1, 14, 15, 16, 17)
- [ ] Logging fields documented
- [ ] User manual verified against implementation

---

## NEXT STEPS

1. **Wait for Task-011a merge** - Verify commit with `grep`
2. **Wait for Task-050 merge** - Verify commit with `grep`
3. **Wait for Task-049f merge** - Verify commit with `grep`
4. **Create feature branch** - Off main, named `feature/task-011-run-session-state-machine`
5. **Follow Phase 1-13 in order** - Don't skip phases
6. **Mark progress as you go** - Update this file with `[✅]` and `[🔄]`
7. **Commit after each gap** - One gap per commit
8. **Run tests frequently** - After each phase
9. **Audit before PR** - Use Section 5 checklist
10. **Create PR** - Link to this checklist as reference

---

**Status:** BLOCKED - AWAITING TASK-011a + TASK-050 + TASK-049f
**When Ready:** This checklist provides 100% implementation guidance
**Completeness:** Ready for fresh-context agent to implement once dependencies available

