# Task 011: Run Session State Machine + Persistence

**Priority:** P0 – Critical Path  
**Tier:** Core Infrastructure  
**Complexity:** 34 (Fibonacci points)  
**Phase:** Foundation  
**Dependencies:** Task 050 (Workspace DB), Task 010 (CLI Framework), Task 002 (.agent/config.yml), Task 018 (Undo/Checkpoint), Task 026 (Observability)  

---

## Description

Task 011 implements the Run Session State Machine, the central orchestration mechanism that governs agent execution from initiation through completion. The state machine manages session lifecycle, tracks execution progress, handles interruptions, and ensures recoverability through persistence. This is the backbone of Acode's reliability.

A Run Session represents a complete unit of agentic work. When a user invokes `acode run "Add validation"`, a session is created encompassing all planning, execution, and verification that follows. Sessions have defined states—Created, Planning, Executing, Paused, Completed, Failed, Cancelled. Transitions between states follow strict rules that maintain invariants.

The state machine enforces correctness constraints. Invalid transitions are rejected—a Completed session cannot move to Executing. State-specific behaviors are encapsulated—Planning state allows only planning operations. This rigor prevents subtle bugs where sessions end up in inconsistent states due to unexpected event sequences.

Persistence is fundamental, not optional. Every state transition is durably recorded before the transition completes. If the process crashes mid-execution, the session can be resumed from its last persisted state. This durability uses the Workspace DB Foundation (Task 050) abstractions, writing to local SQLite with optional sync to remote PostgreSQL.

The persistence model follows event sourcing principles. Rather than just storing current state, the system records every transition event. This history enables debugging (what happened?), auditing (who approved what?), and recovery (resume from any point). Events are immutable—corrections are new events, not edits.

Abstraction layers ensure database independence. Run session logic depends on `IRunStateStore` and `IWorkspaceDb` interfaces, not concrete SQLite or PostgreSQL implementations. This enables testing with in-memory stores, development with SQLite, and production with PostgreSQL, all without changing business logic.

Session hierarchy organizes work naturally. A Session contains one or more Tasks (high-level goals). Tasks contain Steps (discrete actions). Steps contain Tool Calls (atomic operations). This hierarchy mirrors how the agent thinks and enables granular progress tracking, targeted rollback, and efficient resumption.

Interruption handling is first-class. Users may Ctrl+C, systems may crash, models may timeout. The state machine handles all interruption modes gracefully. In-progress work is checkpointed. Resources are cleaned up. The session enters a resumable state that clearly indicates what completed and what remains.

Resume behavior reconstructs exactly where execution stopped. The state machine queries persisted state, identifies incomplete work, and restarts from that point. Completed steps are not re-executed. Partially completed steps are rolled back or retried based on configuration. This efficiency is critical for long-running sessions.

Concurrency control prevents corruption. Only one process may actively execute a session. Locks (advisory or table-level) prevent multiple CLI invocations from operating on the same session. Stale locks from crashed processes are detected and released. These mechanisms maintain single-writer semantics.

Integration points connect the state machine to all Acode subsystems. The agent loop (Task 012) requests state transitions. Approval gates (Task 013) pause execution for user decisions. Undo/checkpoint (Task 018) creates recovery points. Observability (Task 026) receives transition events. The state machine is the coordination hub.

Testing requires simulating complex scenarios. Unit tests verify individual transitions. Integration tests verify the persistence layer. Property-based tests explore edge cases like concurrent access and crash recovery. Test coverage must include all state combinations and transition paths.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Run Session | Complete unit of agentic work |
| Session State | Current position in lifecycle |
| State Transition | Movement from one state to another |
| State Machine | System governing valid transitions |
| Event Sourcing | Recording all transitions as events |
| Persistence | Durable storage of state |
| IRunStateStore | Abstraction for state persistence |
| IWorkspaceDb | Database abstraction layer |
| Session Hierarchy | Session → Task → Step → Tool Call |
| Checkpoint | Recovery point within execution |
| Resume | Restart from interrupted state |
| Lock | Concurrency control mechanism |
| Invariant | Condition that must always hold |
| Transition Guard | Condition for valid transition |
| Event | Immutable record of state change |

---

## Out of Scope

The following items are explicitly excluded from Task 011:

- **Agent decision logic** - Task 012
- **Approval workflow** - Task 013
- **Undo/rollback mechanics** - Task 018
- **Multi-agent coordination** - Post-MVP
- **Distributed sessions** - Single machine
- **Session migration** - Fixed location
- **Session archival** - Retention policy only
- **External monitoring integration** - Task 026
- **Real-time sync** - Eventual consistency
- **Session templates** - Hardcoded structure

---

## Assumptions

### Technical Assumptions

- ASM-001: UUID v7 generation is available for time-ordered unique identifiers
- ASM-002: State machine patterns can be implemented with finite state automaton semantics
- ASM-003: DateTimeOffset provides sufficient precision for timestamp ordering
- ASM-004: JSON serialization is available for event payloads and metadata
- ASM-005: Event sourcing patterns can be applied for state history
- ASM-006: Lock mechanisms are available for session exclusivity

### Environmental Assumptions

- ASM-007: Single-machine execution is the primary deployment model
- ASM-008: File system or database storage is available for persistence
- ASM-009: Process interruption (SIGINT, crash) can occur at any point
- ASM-010: System clock is reasonably accurate for timestamp ordering

### Dependency Assumptions

- ASM-011: Task 010 CLI provides command infrastructure for session commands
- ASM-012: Task 011.a entity model is defined before implementation
- ASM-013: Task 011.b persistence layer handles actual storage
- ASM-014: Task 011.c resume behavior builds on session state

### Behavioral Assumptions

- ASM-015: State transitions are atomic and observable
- ASM-016: Invalid state transitions are programming errors, not user errors
- ASM-017: Session state must be recoverable after process restart
- ASM-018: Event history provides complete audit trail

---

## Functional Requirements

### Session Lifecycle

- FR-001: Session MUST have unique ID (UUID v7)
- FR-002: Session MUST track creation timestamp
- FR-003: Session MUST track last update timestamp
- FR-004: Session MUST reference original task description
- FR-005: Session MUST track current state
- FR-006: Session MUST track previous states (history)
- FR-007: Session MUST be queryable by ID
- FR-008: Active sessions MUST be listable

### State Definitions

- FR-009: CREATED state for new sessions
- FR-010: PLANNING state during analysis
- FR-011: AWAITING_APPROVAL for approval gates
- FR-012: EXECUTING state during actions
- FR-013: PAUSED state for user interruption
- FR-014: COMPLETED state for success
- FR-015: FAILED state for error termination
- FR-016: CANCELLED state for user cancellation

### State Transitions

- FR-017: CREATED → PLANNING (on start)
- FR-018: PLANNING → AWAITING_APPROVAL (if required)
- FR-019: PLANNING → EXECUTING (if auto-approved)
- FR-020: AWAITING_APPROVAL → EXECUTING (on approve)
- FR-021: AWAITING_APPROVAL → CANCELLED (on reject)
- FR-022: EXECUTING → AWAITING_APPROVAL (for gates)
- FR-023: EXECUTING → COMPLETED (on success)
- FR-024: EXECUTING → FAILED (on error)
- FR-025: Any active → PAUSED (on interrupt)
- FR-026: PAUSED → previous active state (on resume)
- FR-027: Any → CANCELLED (on user cancel)

### Transition Guards

- FR-028: Transition MUST validate preconditions
- FR-029: Invalid transitions MUST be rejected
- FR-030: Guard failures MUST log reason
- FR-031: Guards MUST be synchronous
- FR-032: Guards MUST NOT have side effects

### Event Recording

- FR-033: Every transition MUST emit event
- FR-034: Events MUST be immutable
- FR-035: Events MUST include timestamp
- FR-036: Events MUST include previous state
- FR-037: Events MUST include new state
- FR-038: Events MUST include trigger reason
- FR-039: Events MUST be persistable

### Session Hierarchy

- FR-040: Session contains Tasks
- FR-041: Task contains Steps
- FR-042: Step contains ToolCalls
- FR-043: Each level has own state
- FR-044: Parent state derives from children
- FR-045: Hierarchy is queryable
- FR-046: Hierarchy supports partial completion

### Persistence

- FR-047: State MUST use IRunStateStore abstraction
- FR-048: MUST persist to local SQLite via Task 050
- FR-049: MAY sync to PostgreSQL when configured
- FR-050: Persist MUST occur before transition completes
- FR-051: Persist failure MUST abort transition
- FR-052: Persist MUST be transactional
- FR-053: Recovery MUST restore exact state

### Locking

- FR-054: Active session MUST acquire lock
- FR-055: Lock MUST prevent concurrent access
- FR-056: Lock MUST have timeout (60s default)
- FR-057: Stale locks MUST be detectable
- FR-058: Stale locks MUST be releasable
- FR-059: Lock release MUST occur on exit

### Resume Behavior

- FR-060: Resume MUST find last checkpoint
- FR-061: Resume MUST skip completed steps
- FR-062: Resume MUST retry incomplete steps
- FR-063: Resume MUST restore context
- FR-064: Resume MUST validate environment
- FR-065: Resume MUST log what's being resumed

### Cleanup

- FR-066: COMPLETED sessions MUST cleanup resources
- FR-067: FAILED sessions MUST cleanup partial work
- FR-068: CANCELLED sessions MUST cleanup all
- FR-069: Cleanup MUST respect retention policy
- FR-070: Cleanup MUST not affect other sessions

### Query Operations

- FR-071: Get session by ID
- FR-072: List sessions by state
- FR-073: List sessions by date range
- FR-074: Get session history
- FR-075: Get session hierarchy
- FR-076: Query MUST support pagination

---

## Non-Functional Requirements

### Performance

- NFR-001: State transition MUST complete < 50ms
- NFR-002: Persist MUST complete < 100ms
- NFR-003: Query by ID MUST complete < 10ms
- NFR-004: Resume MUST complete < 500ms
- NFR-005: Lock acquisition MUST complete < 100ms

### Reliability

- NFR-006: Crash recovery MUST preserve state
- NFR-007: Partial transitions MUST NOT corrupt state
- NFR-008: Lock conflicts MUST be handled gracefully
- NFR-009: Database errors MUST be recoverable

### Security

- NFR-010: Session data MUST NOT contain secrets
- NFR-011: Lock files MUST have appropriate permissions
- NFR-012: Queries MUST NOT leak other sessions

### Durability

- NFR-013: Persisted state MUST survive crash
- NFR-014: Events MUST NOT be lost
- NFR-015: Sync failures MUST NOT lose local data

### Observability

- NFR-016: All transitions MUST be logged
- NFR-017: All errors MUST be logged with context
- NFR-018: Metrics MUST track transition latency
- NFR-019: Metrics MUST track session counts by state

---

## User Manual Documentation

### Overview

The Run Session State Machine manages the lifecycle of agent runs. Understanding session states helps with debugging, resuming interrupted work, and monitoring progress.

### Session States

```
CREATED → PLANNING → AWAITING_APPROVAL → EXECUTING → COMPLETED
                   ↘                    ↗
                    → ─────────────────→
                    
Any State → PAUSED (interrupt)
Any State → CANCELLED (user cancel)
Any State → FAILED (error)
```

### State Descriptions

| State | Description | Duration |
|-------|-------------|----------|
| CREATED | Session initialized | < 1s |
| PLANNING | Agent analyzing task | Seconds-minutes |
| AWAITING_APPROVAL | Waiting for user | Until response |
| EXECUTING | Agent performing actions | Varies |
| PAUSED | User interrupted | Until resume |
| COMPLETED | Successfully finished | Final |
| FAILED | Error occurred | Final |
| CANCELLED | User cancelled | Final |

### Session Hierarchy

```
Session: "Add input validation"
├── Task: "Analyze existing code"
│   ├── Step: "Read login form"
│   │   └── ToolCall: read_file("src/login.ts")
│   └── Step: "Identify validation points"
│       └── ToolCall: semantic_search("validation")
├── Task: "Implement validation"
│   ├── Step: "Add email validator"
│   │   ├── ToolCall: read_file("src/validators.ts")
│   │   └── ToolCall: write_file("src/validators.ts")
│   └── Step: "Add to form"
│       └── ToolCall: write_file("src/login.ts")
└── Task: "Test changes"
    └── Step: "Run tests"
        └── ToolCall: run_command("npm test")
```

### CLI Commands

```bash
# View current session status
$ acode status
Session: abc123
State: EXECUTING
Task: 2/3 "Implement validation"
Step: 1/2 "Add email validator"
Progress: 45%

# List all sessions
$ acode session list
ID          STATE       CREATED              TASK
abc123      EXECUTING   2024-01-15 10:30    Add input validation
def456      COMPLETED   2024-01-14 15:45    Fix login bug
ghi789      FAILED      2024-01-14 09:00    Refactor module

# View session details
$ acode session show abc123
Session: abc123
State: EXECUTING
Created: 2024-01-15T10:30:00Z
Updated: 2024-01-15T10:35:00Z
Task: Add input validation

History:
  10:30:00 CREATED
  10:30:01 PLANNING
  10:30:15 AWAITING_APPROVAL
  10:30:45 EXECUTING

Tasks:
  ✓ Analyze existing code
  ● Implement validation (in progress)
  ○ Test changes

# Resume interrupted session
$ acode resume abc123
Resuming session abc123...
Last state: EXECUTING
Resuming from: Task 2, Step 1
```

### Interruption and Resume

#### Graceful Interruption

Press Ctrl+C during execution:

```
$ acode run "Add validation"
[Planning] Analyzing codebase...
[Executing] Step 1/5: Reading files...
^C
Interrupted. Saving state...
Session abc123 paused at Step 1.
Resume with: acode resume abc123
```

#### Crash Recovery

If the process crashes, state is preserved:

```
$ acode resume
Found interrupted session: abc123
Last state: EXECUTING at Step 2
Resume? [Y/n] y
Continuing from Step 2...
```

### Configuration

```yaml
# .agent/config.yml
session:
  # Lock timeout in seconds
  lock_timeout_seconds: 60
  
  # Session retention in days
  retention_days: 30
  
  # Maximum concurrent sessions
  max_concurrent: 1
  
  # Checkpoint interval (steps)
  checkpoint_interval: 5
  
  # Stale lock detection (seconds)
  stale_lock_seconds: 300
```

### Persistence

Sessions are stored in the workspace database:

```
.agent/
├── workspace.db          # SQLite database
└── config.yml
```

Database tables:
- `sessions` - Session metadata and current state
- `session_events` - Transition history
- `tasks` - Task records
- `steps` - Step records  
- `tool_calls` - Tool call records

### Troubleshooting

#### Stale Lock

**Problem:** "Session locked by another process"

**Solutions:**
1. Check for running Acode processes
2. Wait for lock timeout (60s)
3. Force release: `acode session unlock abc123`

#### Corrupted State

**Problem:** Session in inconsistent state

**Solutions:**
1. Check event history: `acode session history abc123`
2. Try repair: `acode session repair abc123`
3. Archive and start fresh: `acode session archive abc123`

#### Resume Failures

**Problem:** Cannot resume session

**Possible causes:**
1. Session already completed/failed
2. Environment changed since pause
3. Database corruption

**Solutions:**
1. Check state: `acode session show abc123`
2. Verify environment
3. Run database check: `acode db check`

---

## Acceptance Criteria

### Session Lifecycle

- [ ] AC-001: UUID v7 IDs generated
- [ ] AC-002: Creation timestamp recorded
- [ ] AC-003: Update timestamp maintained
- [ ] AC-004: Task description stored
- [ ] AC-005: Current state tracked
- [ ] AC-006: History maintained
- [ ] AC-007: Queryable by ID
- [ ] AC-008: Active sessions listable

### States

- [ ] AC-009: CREATED state works
- [ ] AC-010: PLANNING state works
- [ ] AC-011: AWAITING_APPROVAL state works
- [ ] AC-012: EXECUTING state works
- [ ] AC-013: PAUSED state works
- [ ] AC-014: COMPLETED state works
- [ ] AC-015: FAILED state works
- [ ] AC-016: CANCELLED state works

### Transitions

- [ ] AC-017: CREATED → PLANNING works
- [ ] AC-018: PLANNING → AWAITING_APPROVAL works
- [ ] AC-019: PLANNING → EXECUTING works
- [ ] AC-020: AWAITING_APPROVAL → EXECUTING works
- [ ] AC-021: AWAITING_APPROVAL → CANCELLED works
- [ ] AC-022: EXECUTING → AWAITING_APPROVAL works
- [ ] AC-023: EXECUTING → COMPLETED works
- [ ] AC-024: EXECUTING → FAILED works
- [ ] AC-025: Any → PAUSED works
- [ ] AC-026: PAUSED → resume works
- [ ] AC-027: Any → CANCELLED works
- [ ] AC-028: Invalid transitions rejected

### Guards

- [ ] AC-029: Preconditions validated
- [ ] AC-030: Invalid rejected
- [ ] AC-031: Failures logged
- [ ] AC-032: Guards synchronous
- [ ] AC-033: No side effects

### Events

- [ ] AC-034: Transitions emit events
- [ ] AC-035: Events immutable
- [ ] AC-036: Events timestamped
- [ ] AC-037: Previous state included
- [ ] AC-038: New state included
- [ ] AC-039: Reason included
- [ ] AC-040: Events persistable

### Hierarchy

- [ ] AC-041: Session contains Tasks
- [ ] AC-042: Task contains Steps
- [ ] AC-043: Step contains ToolCalls
- [ ] AC-044: Each level has state
- [ ] AC-045: Parent derives from children
- [ ] AC-046: Hierarchy queryable

### Persistence

- [ ] AC-047: Uses IRunStateStore
- [ ] AC-048: Persists to SQLite
- [ ] AC-049: Syncs to PostgreSQL if configured
- [ ] AC-050: Persist before transition completes
- [ ] AC-051: Persist failure aborts transition
- [ ] AC-052: Transactional persist
- [ ] AC-053: Recovery restores state

### Locking

- [ ] AC-054: Active sessions locked
- [ ] AC-055: Concurrent access prevented
- [ ] AC-056: Lock timeout configurable
- [ ] AC-057: Stale locks detected
- [ ] AC-058: Stale locks releasable
- [ ] AC-059: Locks released on exit

### Resume

- [ ] AC-060: Finds last checkpoint
- [ ] AC-061: Skips completed steps
- [ ] AC-062: Retries incomplete steps
- [ ] AC-063: Restores context
- [ ] AC-064: Validates environment
- [ ] AC-065: Logs what's resumed

### Performance

- [ ] AC-066: Transition < 50ms
- [ ] AC-067: Persist < 100ms
- [ ] AC-068: Query by ID < 10ms
- [ ] AC-069: Resume < 500ms
- [ ] AC-070: Lock acquisition < 100ms

---

## Testing Requirements

### Unit Tests

```
Tests/Unit/Session/
├── SessionTests.cs
│   ├── Should_Generate_UUID7_Id()
│   ├── Should_Track_Timestamps()
│   └── Should_Store_Task_Description()
│
├── StateMachineTests.cs
│   ├── Should_Start_In_Created_State()
│   ├── Should_Transition_Created_To_Planning()
│   ├── Should_Reject_Invalid_Transition()
│   ├── Should_Transition_To_Paused_From_Any_Active()
│   └── Should_Resume_To_Previous_State()
│
├── TransitionGuardTests.cs
│   ├── Should_Validate_Preconditions()
│   ├── Should_Reject_Without_Preconditions()
│   └── Should_Log_Guard_Failures()
│
├── EventTests.cs
│   ├── Should_Emit_Event_On_Transition()
│   ├── Should_Include_All_Fields()
│   └── Should_Be_Immutable()
│
└── HierarchyTests.cs
    ├── Should_Nest_Tasks_In_Session()
    ├── Should_Nest_Steps_In_Task()
    └── Should_Derive_Parent_State()
```

### Integration Tests

```
Tests/Integration/Session/
├── PersistenceTests.cs
│   ├── Should_Persist_Session_To_SQLite()
│   ├── Should_Recover_After_Crash()
│   └── Should_Sync_To_PostgreSQL()
│
├── LockingTests.cs
│   ├── Should_Acquire_Lock()
│   ├── Should_Block_Concurrent_Access()
│   └── Should_Detect_Stale_Lock()
│
└── ResumeTests.cs
    ├── Should_Resume_From_Checkpoint()
    ├── Should_Skip_Completed_Steps()
    └── Should_Restore_Context()
```

### E2E Tests

```
Tests/E2E/Session/
├── FullLifecycleTests.cs
│   ├── Should_Complete_Full_Session()
│   ├── Should_Handle_Interruption()
│   └── Should_Resume_Successfully()
```

### Performance Benchmarks

| Benchmark | Target | Maximum |
|-----------|--------|---------|
| State transition | 25ms | 50ms |
| Persist operation | 50ms | 100ms |
| Query by ID | 5ms | 10ms |
| Resume session | 250ms | 500ms |
| Lock acquisition | 50ms | 100ms |

### Regression Tests

- State machine after new state added
- Persistence after schema migration
- Resume after checkpoint format change

---

## User Verification Steps

### Scenario 1: Create Session

1. Run `acode run "task"`
2. Run `acode status`
3. Verify: Session ID shown
4. Verify: State is active

### Scenario 2: View Session

1. Run `acode session show <id>`
2. Verify: Details displayed
3. Verify: History shown

### Scenario 3: List Sessions

1. Run `acode session list`
2. Verify: All sessions listed
3. Verify: States shown

### Scenario 4: Interrupt and Resume

1. Start `acode run "task"`
2. Press Ctrl+C
3. Verify: Session paused
4. Run `acode resume`
5. Verify: Resumes from checkpoint

### Scenario 5: Crash Recovery

1. Start `acode run "task"`
2. Kill process (kill -9)
3. Run `acode resume`
4. Verify: Session resumed

### Scenario 6: Concurrent Access

1. Start `acode run "task"` in terminal 1
2. Try `acode status` in terminal 2
3. Verify: Status works (read-only)
4. Try `acode run "task2"` in terminal 2
5. Verify: Blocked or queued

### Scenario 7: Stale Lock

1. Start session, kill -9 process
2. Wait for lock timeout
3. Run `acode resume`
4. Verify: Stale lock detected and released

### Scenario 8: Session History

1. Complete a session
2. Run `acode session history <id>`
3. Verify: All transitions shown
4. Verify: Timestamps correct

### Scenario 9: Persistence

1. Complete a session
2. Run `acode db check`
3. Verify: Session in database
4. Verify: Events in database

### Scenario 10: Cancel Session

1. Start `acode run "task"`
2. Press Ctrl+C then confirm cancel
3. Verify: State is CANCELLED
4. Verify: Cleanup performed

---

## Implementation Prompt

### File Structure

```
src/AgenticCoder.Domain/
├── Sessions/
│   ├── Session.cs
│   ├── SessionId.cs
│   ├── SessionState.cs
│   ├── SessionTask.cs
│   ├── SessionStep.cs
│   ├── ToolCall.cs
│   └── SessionEvent.cs
│
src/AgenticCoder.Application/
├── Sessions/
│   ├── IRunStateStore.cs
│   ├── ISessionStateMachine.cs
│   ├── SessionStateMachine.cs
│   ├── TransitionGuard.cs
│   ├── SessionService.cs
│   └── ResumeService.cs
│
src/AgenticCoder.Infrastructure/
├── Persistence/
│   ├── SQLite/
│   │   └── SQLiteRunStateStore.cs
│   └── PostgreSQL/
│       └── PostgreSQLRunStateStore.cs
└── Locking/
    ├── ISessionLock.cs
    └── FileSessionLock.cs
```

### Session Entity

```csharp
namespace AgenticCoder.Domain.Sessions;

public sealed class Session
{
    public SessionId Id { get; }
    public string TaskDescription { get; }
    public SessionState State { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
    
    private readonly List<SessionTask> _tasks = new();
    private readonly List<SessionEvent> _events = new();
    
    public IReadOnlyList<SessionTask> Tasks => _tasks;
    public IReadOnlyList<SessionEvent> Events => _events;
    
    public void Transition(SessionState newState, string reason);
}
```

### SessionState Enum

```csharp
namespace AgenticCoder.Domain.Sessions;

public enum SessionState
{
    Created,
    Planning,
    AwaitingApproval,
    Executing,
    Paused,
    Completed,
    Failed,
    Cancelled
}
```

### ISessionStateMachine

```csharp
namespace AgenticCoder.Application.Sessions;

public interface ISessionStateMachine
{
    bool CanTransition(SessionState from, SessionState to);
    void Transition(Session session, SessionState to, string reason);
    IReadOnlyList<SessionState> GetValidTransitions(SessionState from);
}
```

### IRunStateStore

```csharp
namespace AgenticCoder.Application.Sessions;

public interface IRunStateStore
{
    Task<Session?> GetAsync(SessionId id, CancellationToken ct);
    Task SaveAsync(Session session, CancellationToken ct);
    Task<IReadOnlyList<Session>> ListAsync(SessionFilter filter, CancellationToken ct);
    Task<IReadOnlyList<SessionEvent>> GetEventsAsync(SessionId id, CancellationToken ct);
}

public sealed record SessionFilter(
    SessionState? State = null,
    DateTimeOffset? After = null,
    DateTimeOffset? Before = null,
    int? Limit = null,
    int? Offset = null);
```

### ISessionLock

```csharp
namespace AgenticCoder.Infrastructure.Locking;

public interface ISessionLock : IAsyncDisposable
{
    SessionId SessionId { get; }
    bool IsHeld { get; }
    DateTimeOffset AcquiredAt { get; }
    
    Task<bool> TryAcquireAsync(TimeSpan timeout, CancellationToken ct);
    Task ReleaseAsync(CancellationToken ct);
}
```

### Transition Matrix

```
From \ To       | Created | Planning | AwaitingApproval | Executing | Paused | Completed | Failed | Cancelled
----------------|---------|----------|------------------|-----------|--------|-----------|--------|----------
Created         |    -    |    ✓     |        -         |     -     |   ✓    |     -     |   ✓    |    ✓
Planning        |    -    |    -     |        ✓         |     ✓     |   ✓    |     -     |   ✓    |    ✓
AwaitingApproval|    -    |    -     |        -         |     ✓     |   ✓    |     -     |   ✓    |    ✓
Executing       |    -    |    -     |        ✓         |     -     |   ✓    |     ✓     |   ✓    |    ✓
Paused          |    -    |    ✓     |        ✓         |     ✓     |   -    |     -     |   -    |    ✓
Completed       |    -    |    -     |        -         |     -     |   -    |     -     |   -    |    -
Failed          |    -    |    -     |        -         |     -     |   -    |     -     |   -    |    -
Cancelled       |    -    |    -     |        -         |     -     |   -    |     -     |   -    |    -
```

### Error Codes

| Code | Meaning |
|------|---------|
| ACODE-SESSION-001 | Invalid state transition |
| ACODE-SESSION-002 | Session not found |
| ACODE-SESSION-003 | Session locked |
| ACODE-SESSION-004 | Persist failed |
| ACODE-SESSION-005 | Resume failed |
| ACODE-SESSION-006 | Lock acquisition failed |

### Logging Fields

```json
{
  "event": "session_transition",
  "session_id": "abc123",
  "from_state": "Planning",
  "to_state": "Executing",
  "reason": "Plan approved",
  "duration_ms": 15
}
```

### Database Schema

```sql
-- Sessions table
CREATE TABLE sessions (
    id TEXT PRIMARY KEY,
    task_description TEXT NOT NULL,
    state TEXT NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    metadata TEXT
);

-- Session events table
CREATE TABLE session_events (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id TEXT NOT NULL,
    from_state TEXT NOT NULL,
    to_state TEXT NOT NULL,
    reason TEXT NOT NULL,
    timestamp TEXT NOT NULL,
    FOREIGN KEY (session_id) REFERENCES sessions(id)
);

-- Session locks table
CREATE TABLE session_locks (
    session_id TEXT PRIMARY KEY,
    process_id INTEGER NOT NULL,
    acquired_at TEXT NOT NULL,
    expires_at TEXT NOT NULL
);

CREATE INDEX idx_sessions_state ON sessions(state);
CREATE INDEX idx_sessions_created ON sessions(created_at);
CREATE INDEX idx_events_session ON session_events(session_id);
```

### Implementation Checklist

1. [ ] Create Session domain entity
2. [ ] Create SessionState enum
3. [ ] Create SessionEvent record
4. [ ] Create session hierarchy (Task, Step, ToolCall)
5. [ ] Implement ISessionStateMachine
6. [ ] Implement transition guards
7. [ ] Create IRunStateStore interface
8. [ ] Implement SQLiteRunStateStore
9. [ ] Implement PostgreSQLRunStateStore
10. [ ] Create ISessionLock interface
11. [ ] Implement FileSessionLock
12. [ ] Implement ResumeService
13. [ ] Add CLI commands (session list, show, etc.)
14. [ ] Write state machine unit tests
15. [ ] Write persistence integration tests
16. [ ] Write locking tests
17. [ ] Add performance benchmarks

### Validation Checklist Before Merge

- [ ] All states implemented and tested
- [ ] All valid transitions work
- [ ] Invalid transitions rejected
- [ ] Events recorded for all transitions
- [ ] Persistence to SQLite works
- [ ] Resume from checkpoint works
- [ ] Locking prevents concurrent access
- [ ] Stale lock detection works
- [ ] Performance targets met
- [ ] Unit test coverage > 90%

### Rollout Plan

1. **Phase 1:** Session entity and state machine
2. **Phase 2:** Event recording
3. **Phase 3:** SQLite persistence
4. **Phase 4:** Locking mechanism
5. **Phase 5:** Resume capability
6. **Phase 6:** CLI integration
7. **Phase 7:** PostgreSQL sync

---

**End of Task 011 Specification**