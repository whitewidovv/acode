# Task 011.c: Resume Behavior + Invariants

**Priority:** P0 – Critical Path  
**Tier:** Core Infrastructure  
**Complexity:** 21 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 011 (State Machine), Task 011.b (Persistence), Task 050 (Workspace DB), Task 049.f (Sync)  

---

## Description

Task 011.c implements the resume behavior and invariants that enable interrupted sessions to continue from exactly where they stopped. Resume is not just a convenience feature—it's a reliability fundamental. Users must trust that crashes, interruptions, and restarts don't lose work or cause incorrect behavior.

Resume accounts for the complexity of a two-tier persistence model. Local SQLite may have events not yet synced to PostgreSQL. The system may reconnect mid-run. These edge cases require careful handling to prevent duplicate steps, double commits, or lost work. Resume invariants guarantee correctness in all scenarios.

Idempotent replay is the core principle. Every operation that modifies session state must be designed so that replaying it produces the same result. If a step was half-complete when crash occurred, replaying from the last checkpoint produces correct state. This requires careful operation design and checkpoint placement.

Deterministic replay order uses a combination of timestamp and event_id. Events are replayed in timestamp order, with event_id as tiebreaker. This ensures that regardless of when resume occurs—immediately after crash or days later—the replay produces identical results.

Resume begins with loading the persisted session state. The system identifies the last completed checkpoint, determines what work remains, and constructs a continuation plan. Completed tasks and steps are skipped. Partially completed steps are handled according to their nature—some are retried, others are rolled back first.

Environment validation ensures resume is safe. The workspace may have changed since the session was interrupted. Files may have been modified externally. Configuration may have changed. Resume verifies critical invariants before proceeding and warns or aborts if conditions have changed significantly.

Sync state affects resume behavior. If events are pending sync to PostgreSQL, the system operates in "degraded mode" where some features may be limited. The CLI clearly indicates this state. Resume continues locally regardless of sync state—users are never blocked by network issues.

When remote reconnects mid-run, coordination prevents conflicts. The session lock mechanism ensures only one process executes a session. Pending syncs complete before remote execution begins. No duplicate steps or double commits occur because idempotency keys prevent double-application.

The resume command is user-friendly. `acode resume` with no arguments resumes the most recent interrupted session. `acode resume <session-id>` resumes a specific session. Clear output shows what's being resumed and progress as execution continues.

Failure handling during resume is comprehensive. If resume itself fails, the session remains in its interrupted state—no partial corruption. Errors are logged with full context. Users receive actionable guidance on how to proceed.

Testing resume requires simulating many failure modes. Crashes at various points in execution. Network disconnects during sync. External modifications to workspace. Each scenario must produce correct behavior. Property-based testing explores edge cases systematically.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Resume | Continue interrupted session |
| Checkpoint | Saved state for recovery |
| Invariant | Condition that must always hold |
| Idempotent | Same result on repeat execution |
| Replay | Re-executing from checkpoint |
| Deterministic | Produces same output given same input |
| Degraded Mode | Operating with reduced capability |
| Sync Pending | Events awaiting remote sync |
| Environment Validation | Checking workspace state |
| Continuation Plan | Strategy for resuming work |
| Session Lock | Exclusive execution rights |
| Double Commit | Incorrectly applying change twice |
| Rollback | Undoing partial work |
| Event Order | Sequence for replay |
| Recovery Point | Position to resume from |

---

## Out of Scope

The following items are explicitly excluded from Task 011.c:

- **Undo functionality** - Task 018
- **Multi-device resume** - Single machine
- **Conflict merge** - Latest timestamp wins
- **Resume different session version** - Same schema only
- **Resume archived sessions** - Active only
- **Resume cancelled sessions** - Terminal states excluded
- **Automatic resume on startup** - Explicit command only
- **Resume with different model** - Same config only
- **Background resume** - Interactive only
- **Partial resume** - Full continuation only

---

## Assumptions

### Technical Assumptions

- ASM-001: Session state is fully serialized and persisted before interruption
- ASM-002: Event history provides sufficient context for state reconstruction
- ASM-003: File system state at resume time may differ from interruption time
- ASM-004: Model availability at resume may differ from original run
- ASM-005: Process memory state is NOT preserved across restart

### Behavioral Assumptions

- ASM-006: Users expect resume to continue from last completed step
- ASM-007: In-progress steps should be re-executed, not continued mid-step
- ASM-008: Resume must validate current state matches expected state
- ASM-009: Resume failures should provide clear remediation guidance
- ASM-010: Lock acquisition prevents concurrent resume attempts

### Dependency Assumptions

- ASM-011: Task 011 main provides state machine and session model
- ASM-012: Task 011.a provides entity structures for session data
- ASM-013: Task 011.b provides persistence layer for state loading
- ASM-014: Task 010 CLI provides resume command infrastructure

### Invariant Assumptions

- ASM-015: Session ID is immutable across resume
- ASM-016: Completed steps are not re-executed
- ASM-017: Event history is append-only during resume
- ASM-018: Configuration changes between runs are detected and handled
- ASM-019: Resume preserves original task description

---

## Functional Requirements

### Resume Initiation

- FR-001: `acode resume` MUST resume most recent interrupted session
- FR-002: `acode resume <id>` MUST resume specific session
- FR-003: Resume MUST only work for interruptible states
- FR-004: Resume MUST fail for terminal states (Completed, Failed, Cancelled)
- FR-005: Resume MUST acquire session lock
- FR-006: Resume MUST fail if lock unavailable
- FR-007: Resume MUST log session ID being resumed

### State Recovery

- FR-008: Resume MUST load full session from persistence
- FR-009: Resume MUST identify last checkpoint
- FR-010: Resume MUST identify incomplete work
- FR-011: Resume MUST reconstruct conversation context
- FR-012: Resume MUST restore model configuration
- FR-013: Resume MUST restore approval state

### Checkpoint Identification

- FR-014: Checkpoint MUST be after each completed step
- FR-015: Checkpoint MUST include task/step/toolcall state
- FR-016: Checkpoint MUST include conversation state
- FR-017: Checkpoint MUST be atomically persisted
- FR-018: Most recent checkpoint MUST be identifiable

### Continuation Planning

- FR-019: Completed tasks MUST be skipped
- FR-020: Completed steps MUST be skipped
- FR-021: In-progress steps MUST be evaluated
- FR-022: Pending steps MUST be queued
- FR-023: Plan MUST be logged before execution

### In-Progress Step Handling

- FR-024: Read operations MUST be retryable
- FR-025: Write operations MUST check if already applied
- FR-026: Partial writes MUST be rolled back first
- FR-027: Step strategy MUST be configurable (retry/rollback)
- FR-028: Default strategy: rollback and retry

### Idempotency

- FR-029: Every step MUST have idempotency key
- FR-030: Completed steps MUST be detectable
- FR-031: Re-applying completed step MUST be no-op
- FR-032: Idempotency MUST work across restarts
- FR-033: Idempotency keys MUST be persisted

### Deterministic Replay

- FR-034: Events MUST be ordered by timestamp
- FR-035: Event_id MUST be tiebreaker
- FR-036: Replay MUST produce identical state
- FR-037: Replay order MUST be logged
- FR-038: Non-determinism MUST be flagged as error

### Environment Validation

- FR-039: Working directory MUST exist
- FR-040: Required files MUST be accessible
- FR-041: Model MUST be available
- FR-042: Config MUST be valid
- FR-043: Validation failures MUST be reported
- FR-044: Warnings for changed files
- FR-045: Abort option for significant changes

### Sync State Handling

- FR-046: Pending sync MUST be detected
- FR-047: Degraded mode MUST be indicated
- FR-048: Resume MUST work regardless of sync state
- FR-049: Sync MUST continue in background
- FR-050: No blocking on network operations

### Remote Reconnection

- FR-051: Reconnect MUST be detected
- FR-052: Pending events MUST sync before remote ops
- FR-053: Duplicate steps MUST be prevented
- FR-054: Double commits MUST be prevented
- FR-055: Lock coordination MUST prevent conflicts

### Progress Reporting

- FR-056: Resume start MUST be announced
- FR-057: Skip count MUST be shown
- FR-058: Remaining work MUST be shown
- FR-059: Progress MUST be reported during execution
- FR-060: Resume completion MUST be announced

### Error Handling

- FR-061: Resume failure MUST NOT corrupt state
- FR-062: Errors MUST be logged with context
- FR-063: Exit code MUST indicate failure type
- FR-064: User guidance MUST be provided
- FR-065: Retry option MUST be available

---

## Non-Functional Requirements

### Performance

- NFR-001: State recovery MUST complete < 500ms
- NFR-002: Environment validation MUST complete < 2s
- NFR-003: Continuation planning MUST complete < 100ms
- NFR-004: No performance degradation vs fresh start

### Reliability

- NFR-005: Crash during resume MUST NOT corrupt
- NFR-006: Multiple resume attempts MUST be safe
- NFR-007: Partial recovery MUST be completable

### Correctness

- NFR-008: Completed work MUST NOT be re-executed
- NFR-009: Incomplete work MUST be completed
- NFR-010: Final state MUST match non-interrupted run

### Security

- NFR-011: Session lock MUST prevent hijacking
- NFR-012: Resume MUST not bypass approvals
- NFR-013: Secrets MUST not be logged

### Observability

- NFR-014: All resume attempts MUST be logged
- NFR-015: Skip/retry decisions MUST be logged
- NFR-016: Environment changes MUST be logged

---

## User Manual Documentation

### Overview

The resume feature enables Acode to continue interrupted sessions from exactly where they stopped. Whether interrupted by Ctrl+C, crash, or system shutdown, resume picks up seamlessly.

### Quick Start

```bash
# Resume most recent interrupted session
$ acode resume

# Resume specific session
$ acode resume abc123

# View resumable sessions
$ acode session list --resumable
```

### Resume Behavior

#### What Gets Preserved

When you resume, the following is restored:

- Session state and progress
- Completed tasks and steps
- Conversation context
- Model configuration
- Approval state

#### What Gets Skipped

Already-completed work is skipped:

```
$ acode resume abc123
Resuming session abc123...
  Loaded state: PAUSED at Task 2, Step 3
  Skipping: Task 1 (complete)
  Skipping: Task 2, Steps 1-2 (complete)
  Starting from: Task 2, Step 3
```

### Resumable States

Resume works for these states:

| State | Resumable | Notes |
|-------|-----------|-------|
| CREATED | ✓ | Starts fresh |
| PLANNING | ✓ | Resumes planning |
| AWAITING_APPROVAL | ✓ | Re-prompts for approval |
| EXECUTING | ✓ | Continues execution |
| PAUSED | ✓ | Most common case |
| COMPLETED | ✗ | Already finished |
| FAILED | ✗ | Must start new session |
| CANCELLED | ✗ | User terminated |

### CLI Commands

#### Resume Recent

```bash
$ acode resume
Found 1 resumable session:
  abc123 - PAUSED - "Add input validation"
  Paused at: Task 2/3, Step 3/5
  Paused: 10 minutes ago

Resume this session? [Y/n] y

Resuming session abc123...
  ✓ Task 1: Analyze existing code (skipped)
  ✓ Task 2, Steps 1-2 (skipped)
  ● Task 2, Step 3: Add email validator
```

#### Resume Specific

```bash
$ acode resume abc123
Resuming session abc123...
```

#### List Resumable

```bash
$ acode session list --resumable
ID          STATE       PAUSED              TASK
abc123      PAUSED      10m ago             Add input validation
def456      EXECUTING   5m ago              Fix login bug
```

#### Resume Status

```bash
$ acode resume --dry-run abc123
Would resume session abc123:
  Current state: PAUSED
  Last checkpoint: Task 2, Step 3
  Skipping: 5 steps (complete)
  Remaining: 8 steps
  Sync status: 2 events pending
```

### Environment Validation

Resume checks the environment before continuing:

```
$ acode resume abc123
Validating environment...
  ✓ Working directory exists
  ✓ Required files accessible
  ✓ Model llama3.2:7b available
  ✓ Configuration valid

Environment ready. Resuming...
```

#### Changed Files

If files changed since pause:

```
$ acode resume abc123
Validating environment...
  ⚠ Changed files detected:
    - src/login.ts (modified 5 min ago)
    - src/validators.ts (modified 3 min ago)

The session was working on these files. Changes may conflict.

Options:
  [C]ontinue anyway
  [A]bort and start fresh
  [V]iew changes

Choice: c

Continuing with changed files...
```

### Sync State

#### Normal Mode

```
$ acode status
Session: abc123
State: EXECUTING
Sync: Up to date ✓
```

#### Degraded Mode

```
$ acode status
Session: abc123
State: EXECUTING
Sync: DEGRADED - 5 events pending
  Last sync attempt: 10 min ago
  Next retry: 30 sec
  Reason: Connection refused

Note: Session continues locally. Sync will resume when remote is available.
```

### In-Progress Step Handling

When resuming from a step that was in progress:

#### Read Operations

Reads are simply re-executed:
```
Step: Read src/login.ts
Status: Was in progress
Action: Re-reading file
```

#### Write Operations

Writes check if already applied:
```
Step: Write src/validators.ts
Status: Was in progress
Checking: File matches expected state
Action: Already applied, skipping
```

#### Partial Writes

Partial writes are rolled back first:
```
Step: Write src/config.ts
Status: Partially written
Action: Rolling back to checkpoint
Action: Re-executing step
```

### Configuration

```yaml
# .agent/config.yml
resume:
  # Strategy for in-progress steps
  in_progress_strategy: rollback  # rollback | retry | prompt
  
  # Validation behavior
  validate_environment: true
  changed_files_action: prompt    # prompt | continue | abort
  
  # Timeout for environment validation
  validation_timeout_seconds: 10
```

### Troubleshooting

#### Cannot Resume - Session Locked

**Problem:** "Session is locked by another process"

**Solutions:**
1. Check for other Acode processes
2. Wait for lock timeout (60s default)
3. Force unlock: `acode session unlock abc123`

#### Cannot Resume - Terminal State

**Problem:** "Cannot resume FAILED session"

**Solution:**
Terminal states (COMPLETED, FAILED, CANCELLED) cannot be resumed.
Start a new session: `acode run "same task"`

#### Resume Shows Wrong State

**Problem:** Resume skips steps that weren't completed

**Possible cause:** Checkpoint wasn't persisted before crash

**Solution:**
1. Check event history: `acode session history abc123`
2. If work was lost, start fresh
3. Report as bug if checkpoints should have been saved

#### Environment Validation Fails

**Problem:** Resume aborts due to environment changes

**Solutions:**
1. View changes: `acode resume --dry-run abc123`
2. Resolve conflicts manually
3. Continue with --force (careful!)
4. Start fresh session

---

## Acceptance Criteria

### Resume Initiation

- [ ] AC-001: `acode resume` resumes recent
- [ ] AC-002: `acode resume <id>` resumes specific
- [ ] AC-003: Only interruptible states resumable
- [ ] AC-004: Terminal states rejected
- [ ] AC-005: Lock acquired
- [ ] AC-006: Lock conflict handled
- [ ] AC-007: Session ID logged

### State Recovery

- [ ] AC-008: Full session loaded
- [ ] AC-009: Last checkpoint identified
- [ ] AC-010: Incomplete work identified
- [ ] AC-011: Conversation context restored
- [ ] AC-012: Model config restored
- [ ] AC-013: Approval state restored

### Checkpoints

- [ ] AC-014: After each completed step
- [ ] AC-015: Includes all state
- [ ] AC-016: Atomically persisted
- [ ] AC-017: Most recent identifiable

### Continuation

- [ ] AC-018: Completed tasks skipped
- [ ] AC-019: Completed steps skipped
- [ ] AC-020: In-progress evaluated
- [ ] AC-021: Pending queued
- [ ] AC-022: Plan logged

### In-Progress Handling

- [ ] AC-023: Reads retryable
- [ ] AC-024: Writes checked
- [ ] AC-025: Partials rolled back
- [ ] AC-026: Strategy configurable
- [ ] AC-027: Default is rollback+retry

### Idempotency

- [ ] AC-028: Steps have idempotency keys
- [ ] AC-029: Completed detectable
- [ ] AC-030: Re-apply is no-op
- [ ] AC-031: Works across restarts
- [ ] AC-032: Keys persisted

### Replay

- [ ] AC-033: Ordered by timestamp
- [ ] AC-034: Event_id tiebreaker
- [ ] AC-035: Identical state produced
- [ ] AC-036: Order logged

### Environment

- [ ] AC-037: Directory checked
- [ ] AC-038: Files accessible
- [ ] AC-039: Model available
- [ ] AC-040: Config valid
- [ ] AC-041: Failures reported
- [ ] AC-042: Changed files warned
- [ ] AC-043: Abort option works

### Sync State

- [ ] AC-044: Pending detected
- [ ] AC-045: Degraded mode indicated
- [ ] AC-046: Works without sync
- [ ] AC-047: Background sync continues
- [ ] AC-048: No network blocking

### Remote Reconnect

- [ ] AC-049: Reconnect detected
- [ ] AC-050: Events sync first
- [ ] AC-051: No duplicate steps
- [ ] AC-052: No double commits
- [ ] AC-053: Lock coordination works

### Progress

- [ ] AC-054: Start announced
- [ ] AC-055: Skip count shown
- [ ] AC-056: Remaining shown
- [ ] AC-057: Progress reported
- [ ] AC-058: Completion announced

### Errors

- [ ] AC-059: Failure doesn't corrupt
- [ ] AC-060: Logged with context
- [ ] AC-061: Exit code indicates type
- [ ] AC-062: Guidance provided

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Sessions/Resume/
├── ResumeServiceTests.cs
│   ├── Should_Resume_Paused_Session()
│   ├── Should_Reject_Terminal_States()
│   ├── Should_Identify_Last_Checkpoint()
│   └── Should_Build_Continuation_Plan()
│
├── CheckpointTests.cs
│   ├── Should_Create_Checkpoint_After_Step()
│   ├── Should_Be_Atomic()
│   └── Should_Include_All_State()
│
├── IdempotencyTests.cs
│   ├── Should_Detect_Completed_Steps()
│   ├── Should_Skip_Completed()
│   └── Should_Generate_Unique_Keys()
│
├── ReplayOrderTests.cs
│   ├── Should_Order_By_Timestamp()
│   ├── Should_Use_EventId_Tiebreaker()
│   └── Should_Produce_Deterministic_State()
│
└── EnvironmentValidationTests.cs
    ├── Should_Check_Directory()
    ├── Should_Check_Files()
    ├── Should_Check_Model()
    └── Should_Detect_Changes()
```

### Integration Tests

```
Tests/Integration/Sessions/Resume/
├── ResumeIntegrationTests.cs
│   ├── Should_Resume_After_Crash()
│   ├── Should_Skip_Completed_Work()
│   └── Should_Handle_Changed_Files()
│
├── SyncResumeTests.cs
│   ├── Should_Resume_With_Pending_Sync()
│   ├── Should_Handle_Reconnect()
│   └── Should_Prevent_Duplicates()
│
└── InProgressHandlingTests.cs
    ├── Should_Retry_Reads()
    ├── Should_Rollback_Partials()
    └── Should_Detect_Applied_Writes()
```

### E2E Tests

```
Tests/E2E/Sessions/Resume/
├── FullResumeTests.cs
│   ├── Should_Complete_After_Interrupt()
│   ├── Should_Produce_Same_Result()
│   └── Should_Handle_Multiple_Resumes()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| State recovery | 250ms | 500ms |
| Environment validation | 1s | 2s |
| Continuation planning | 50ms | 100ms |
| Checkpoint creation | 50ms | 100ms |

### Regression Tests

- Resume after session schema change
- Checkpoint format changes
- Idempotency key format changes

---

## User Verification Steps

### Scenario 1: Basic Resume

1. Run `acode run "task"`
2. Press Ctrl+C mid-run
3. Run `acode resume`
4. Verify: Continues from pause point

### Scenario 2: Resume Specific

1. Start and pause session abc123
2. Start and pause session def456
3. Run `acode resume abc123`
4. Verify: abc123 resumed, not def456

### Scenario 3: Skip Completed

1. Complete 2 of 3 tasks
2. Interrupt session
3. Resume
4. Verify: Only task 3 executes

### Scenario 4: Terminal State Rejected

1. Complete a session
2. Run `acode resume <completed-session>`
3. Verify: Error message
4. Verify: Exit code non-zero

### Scenario 5: Lock Conflict

1. Start session in terminal 1
2. Try `acode resume` in terminal 2
3. Verify: Lock conflict message

### Scenario 6: Environment Changed

1. Pause session working on file X
2. Modify file X externally
3. Resume
4. Verify: Warning about changes

### Scenario 7: Sync Pending

1. Disconnect network
2. Run session
3. Pause
4. Resume
5. Verify: "Degraded mode" shown
6. Verify: Works locally

### Scenario 8: Crash Recovery

1. Start session
2. Kill process (kill -9)
3. Run `acode resume`
4. Verify: Resumes from checkpoint

### Scenario 9: Idempotency

1. Pause after file write
2. Resume
3. Verify: File not written again
4. Verify: Step marked complete

### Scenario 10: Multiple Resumes

1. Resume session
2. Interrupt again
3. Resume again
4. Verify: Works correctly
5. Verify: No duplicate work

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Application/
├── Sessions/
│   ├── Resume/
│   │   ├── IResumeService.cs
│   │   ├── ResumeService.cs
│   │   ├── CheckpointManager.cs
│   │   ├── ContinuationPlanner.cs
│   │   ├── EnvironmentValidator.cs
│   │   └── InProgressHandler.cs
│   │
│   ├── Idempotency/
│   │   ├── IdempotencyKeyGenerator.cs
│   │   └── CompletionDetector.cs
│   │
│   └── Replay/
│       ├── ReplayOrderer.cs
│       └── DeterministicReplayer.cs
│
src/AgenticCoder.CLI/
└── Commands/
    └── ResumeCommand.cs
```

### IResumeService Interface

```csharp
namespace AgenticCoder.Application.Sessions.Resume;

public interface IResumeService
{
    Task<ResumeResult> ResumeAsync(SessionId? sessionId, ResumeOptions options, CancellationToken ct);
    Task<ResumePreview> PreviewAsync(SessionId sessionId, CancellationToken ct);
    Task<IReadOnlyList<Session>> GetResumableAsync(CancellationToken ct);
}

public sealed record ResumeOptions(
    InProgressStrategy Strategy = InProgressStrategy.RollbackRetry,
    bool ValidateEnvironment = true,
    ChangedFilesAction ChangedFilesAction = ChangedFilesAction.Prompt);

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
```

### CheckpointManager

```csharp
namespace AgenticCoder.Application.Sessions.Resume;

public sealed class CheckpointManager
{
    public Task<Checkpoint> CreateAsync(Session session, CancellationToken ct);
    public Task<Checkpoint?> GetLatestAsync(SessionId sessionId, CancellationToken ct);
    public Task<IReadOnlyList<Checkpoint>> GetAllAsync(SessionId sessionId, CancellationToken ct);
}

public sealed record Checkpoint(
    long Id,
    SessionId SessionId,
    TaskId? CurrentTaskId,
    StepId? CurrentStepId,
    SessionState State,
    JsonDocument ConversationState,
    DateTimeOffset CreatedAt);
```

### ContinuationPlanner

```csharp
namespace AgenticCoder.Application.Sessions.Resume;

public sealed class ContinuationPlanner
{
    public ContinuationPlan Create(Session session, Checkpoint checkpoint);
}

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
```

### EnvironmentValidator

```csharp
namespace AgenticCoder.Application.Sessions.Resume;

public sealed class EnvironmentValidator
{
    public Task<EnvironmentValidationResult> ValidateAsync(Session session, CancellationToken ct);
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

### IdempotencyKeyGenerator

```csharp
namespace AgenticCoder.Application.Sessions.Idempotency;

public sealed class IdempotencyKeyGenerator
{
    public string Generate(SessionId sessionId, StepId stepId, int attemptNumber);
    public bool Parse(string key, out SessionId sessionId, out StepId stepId, out int attemptNumber);
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-RESUME-001 | No resumable session found |
| ACODE-RESUME-002 | Session in terminal state |
| ACODE-RESUME-003 | Session locked |
| ACODE-RESUME-004 | Environment validation failed |
| ACODE-RESUME-005 | Checkpoint not found |
| ACODE-RESUME-006 | Continuation planning failed |
| ACODE-RESUME-007 | Idempotency conflict |

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Resume completed successfully |
| 1 | Resume failed (general) |
| 14 | No resumable session |
| 15 | Terminal state |
| 16 | Session locked |
| 17 | Environment validation failed |

### Logging Fields

```json
{
  "event": "session_resume",
  "session_id": "abc123",
  "checkpoint_id": 42,
  "skipped_tasks": 1,
  "skipped_steps": 5,
  "remaining_steps": 3,
  "in_progress_action": "rollback_retry",
  "sync_status": "pending",
  "environment_valid": true
}
```

### Implementation Checklist

1. [ ] Create IResumeService interface
2. [ ] Implement ResumeService
3. [ ] Create CheckpointManager
4. [ ] Implement checkpoint creation after steps
5. [ ] Create ContinuationPlanner
6. [ ] Implement continuation logic
7. [ ] Create EnvironmentValidator
8. [ ] Implement file change detection
9. [ ] Create InProgressHandler
10. [ ] Implement rollback/retry strategies
11. [ ] Create IdempotencyKeyGenerator
12. [ ] Implement completion detection
13. [ ] Create ReplayOrderer
14. [ ] Implement deterministic replay
15. [ ] Create ResumeCommand for CLI
16. [ ] Write unit tests
17. [ ] Write integration tests
18. [ ] Write E2E tests

### Validation Checklist Before Merge

- [ ] Resume works for PAUSED state
- [ ] Resume works for EXECUTING state
- [ ] Terminal states rejected
- [ ] Completed work skipped
- [ ] Incomplete work handled correctly
- [ ] Environment validation works
- [ ] Changed files detected
- [ ] Idempotency prevents duplicates
- [ ] Deterministic replay verified
- [ ] Crash during resume doesn't corrupt
- [ ] Exit codes correct
- [ ] Unit test coverage > 90%

### Rollout Plan

1. **Phase 1:** Checkpoint creation
2. **Phase 2:** State recovery
3. **Phase 3:** Continuation planning
4. **Phase 4:** Environment validation
5. **Phase 5:** In-progress handling
6. **Phase 6:** Idempotency
7. **Phase 7:** CLI command
8. **Phase 8:** Sync state handling

---

**End of Task 011.c Specification**