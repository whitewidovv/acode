# Task 026.b: State Transitions + Logging

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 026 (Queue), Task 026.a (Schema), Task 018 (Outbox)  

---

## Description

Task 026.b implements state transition logic and comprehensive logging. Each state change MUST be validated, executed atomically, and logged. Transition history MUST be preserved for audit.

The state machine MUST enforce valid transitions. Invalid transitions MUST be rejected with clear errors. Each transition MUST record the actor, reason, and timestamp.

Logging MUST capture all state changes. Logs MUST be structured for querying. Events MUST be emitted for subscribers. Metrics MUST track transition rates.

### Business Value

State transition logging enables:
- Complete audit trail
- Debugging failed tasks
- Performance analysis
- Compliance reporting
- Incident investigation

### Scope Boundaries

This task covers transitions and logging. Schema is in Task 026.a. Crash recovery is in Task 026.c. Queue operations are in Task 026.

### Integration Points

- Task 026: Queue operations trigger transitions
- Task 026.a: History table stores records
- Task 018: Events emitted to outbox
- Task 020: Audit log integration

### Failure Modes

- Transition rejected → Clear error message
- Log write failure → Transaction rollback
- Event emit failure → Retry with backoff

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Transition | State change operation |
| Guard | Condition check before transition |
| Action | Side effect during transition |
| Actor | Entity causing transition |
| Reason | Explanation for transition |
| History | Record of past transitions |
| Event | Notification of transition |
| Metric | Quantitative measurement |

---

## Out of Scope

- Transition visualization
- State machine DSL
- Undo/redo operations
- Transition approval workflow
- External state sync

---

## Functional Requirements

### FR-001 to FR-030: State Machine

- FR-001: `IStateMachine` interface MUST be defined
- FR-002: `CanTransition(from, to)` MUST validate
- FR-003: `Transition(task, to, actor, reason)` MUST execute
- FR-004: Transition MUST be atomic
- FR-005: Transition MUST update task status
- FR-006: Transition MUST update updated_at
- FR-007: Transition MUST insert history record
- FR-008: Transition MUST emit event
- FR-009: Invalid transition MUST throw
- FR-010: Exception MUST include from/to states
- FR-011: Exception MUST include task ID
- FR-012: Guards MUST run before transition
- FR-013: Guard failure MUST prevent transition
- FR-014: Guard failure MUST log reason
- FR-015: Actions MUST run during transition
- FR-016: Action failure MUST rollback
- FR-017: Pending→Running MUST set startedAt
- FR-018: Pending→Running MUST set workerId
- FR-019: Running→Completed MUST set completedAt
- FR-020: Running→Completed MUST set result
- FR-021: Running→Failed MUST set lastError
- FR-022: Running→Failed MUST increment attemptCount
- FR-023: Failed→Pending MUST check retryLimit
- FR-024: Failed→Pending MUST clear lastError
- FR-025: *→Cancelled MUST be allowed from non-terminal
- FR-026: Completed→* MUST always fail
- FR-027: Cancelled→* MUST always fail
- FR-028: Self-transition MUST be rejected
- FR-029: Transition metadata MAY be stored
- FR-030: Bulk transitions MUST be supported

### FR-031 to FR-055: History Logging

- FR-031: Every transition MUST create history record
- FR-032: Record MUST include task_id
- FR-033: Record MUST include from_status
- FR-034: Record MUST include to_status
- FR-035: Record MUST include actor
- FR-036: Record MUST include timestamp
- FR-037: Record MAY include reason
- FR-038: Timestamp MUST be ISO8601 UTC
- FR-039: Actor values: worker/{id}, user/{name}, system
- FR-040: History MUST be queryable by task
- FR-041: History MUST be queryable by time
- FR-042: History MUST be ordered by timestamp
- FR-043: History count MUST be available
- FR-044: History MUST support pagination
- FR-045: History MUST be exportable
- FR-046: Export formats: JSON, CSV
- FR-047: History retention MUST be configurable
- FR-048: Default retention: 90 days
- FR-049: Purge MUST delete old records
- FR-050: Purge MUST be batched
- FR-051: Purge MUST not block operations
- FR-052: Purge MUST log count deleted
- FR-053: History size MUST be tracked
- FR-054: Size alerts MAY be configured
- FR-055: History MUST NOT be modified

### FR-056 to FR-075: Event Emission

- FR-056: Transition MUST emit domain event
- FR-057: Event MUST include task ID
- FR-058: Event MUST include from/to status
- FR-059: Event MUST include actor
- FR-060: Event MUST include timestamp
- FR-061: Event MUST include correlation ID
- FR-062: Events: TaskStatusChanged
- FR-063: Events: TaskStarted (Pending→Running)
- FR-064: Events: TaskCompleted (Running→Completed)
- FR-065: Events: TaskFailed (Running→Failed)
- FR-066: Events: TaskCancelled (*→Cancelled)
- FR-067: Events: TaskRetried (Failed→Pending)
- FR-068: Events: TaskBlocked (Pending→Blocked)
- FR-069: Events: TaskUnblocked (Blocked→Pending)
- FR-070: Event emission MUST be async
- FR-071: Event failure MUST NOT fail transition
- FR-072: Failed events MUST retry
- FR-073: Retry MUST use exponential backoff
- FR-074: Max retry MUST be configurable
- FR-075: Dead-letter MUST capture failed events

---

## Non-Functional Requirements

- NFR-001: Transition MUST complete in <50ms
- NFR-002: History write MUST be included
- NFR-003: Event emit MUST NOT block
- NFR-004: 1000 transitions/sec MUST be supported
- NFR-005: History query MUST be <100ms
- NFR-006: Purge MUST be <5s per batch
- NFR-007: No locks held during event emit
- NFR-008: Thread-safe operations
- NFR-009: Correlation ID propagation
- NFR-010: Structured logging throughout

---

## User Manual Documentation

### State Diagram

```
Pending ──────────────────────────────────────────────┐
   │                                                   │
   │ worker claims                                     │ cancel
   ▼                                                   ▼
Running ──────────────────────────────────────────► Cancelled
   │                                                   ▲
   ├── success ──► Completed                           │
   │                                                   │
   └── error ───► Failed ──── retry ──► Pending        │
                    │                                  │
                    └────────── cancel ────────────────┘
```

### Transition Events

| Transition | Event |
|------------|-------|
| Pending→Running | TaskStarted |
| Running→Completed | TaskCompleted |
| Running→Failed | TaskFailed |
| Failed→Pending | TaskRetried |
| *→Cancelled | TaskCancelled |
| Pending→Blocked | TaskBlocked |
| Blocked→Pending | TaskUnblocked |

### Query History

```bash
# Show task history
acode task history abc123

# Show recent transitions
acode task history --since 1h

# Export history
acode task history --format json > history.json
```

### Configuration

```yaml
queue:
  history:
    retentionDays: 90
    purgeBatchSize: 1000
    purgeIntervalMinutes: 60
  events:
    maxRetries: 3
    retryDelayMs: 1000
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Valid transitions succeed
- [ ] AC-002: Invalid transitions fail
- [ ] AC-003: History records created
- [ ] AC-004: Actor captured
- [ ] AC-005: Timestamp accurate
- [ ] AC-006: Events emitted
- [ ] AC-007: Event retry works
- [ ] AC-008: History queryable
- [ ] AC-009: Purge works
- [ ] AC-010: Performance targets met
- [ ] AC-011: Logging complete
- [ ] AC-012: Metrics tracked

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Valid transition paths
- [ ] UT-002: Invalid transition rejection
- [ ] UT-003: Guard execution
- [ ] UT-004: Action execution
- [ ] UT-005: History creation
- [ ] UT-006: Event emission

### Integration Tests

- [ ] IT-001: Full transition cycle
- [ ] IT-002: Concurrent transitions
- [ ] IT-003: Event delivery
- [ ] IT-004: History purge

---

## Implementation Prompt

### Interface

```csharp
public interface IStateMachine
{
    bool CanTransition(TaskStatus from, TaskStatus to);
    
    Task<TransitionResult> TransitionAsync(
        string taskId,
        TaskStatus to,
        string actor,
        string? reason = null,
        CancellationToken ct = default);
        
    IReadOnlyList<TaskStatus> GetValidTransitions(TaskStatus from);
}

public record TransitionResult(
    bool Success,
    TaskStatus FromStatus,
    TaskStatus ToStatus,
    DateTimeOffset Timestamp,
    string? Error);

public interface ITransitionHistory
{
    Task<IReadOnlyList<TransitionRecord>> GetHistoryAsync(
        string taskId,
        CancellationToken ct = default);
        
    Task<int> PurgeAsync(
        DateTimeOffset before,
        int batchSize,
        CancellationToken ct = default);
}

public record TransitionRecord(
    long Id,
    string TaskId,
    TaskStatus? FromStatus,
    TaskStatus ToStatus,
    string Actor,
    string? Reason,
    DateTimeOffset Timestamp);
```

### Transition Matrix

```csharp
private static readonly Dictionary<(TaskStatus, TaskStatus), bool> ValidTransitions = new()
{
    { (TaskStatus.Pending, TaskStatus.Running), true },
    { (TaskStatus.Pending, TaskStatus.Cancelled), true },
    { (TaskStatus.Pending, TaskStatus.Blocked), true },
    { (TaskStatus.Running, TaskStatus.Completed), true },
    { (TaskStatus.Running, TaskStatus.Failed), true },
    { (TaskStatus.Running, TaskStatus.Cancelled), true },
    { (TaskStatus.Failed, TaskStatus.Pending), true },
    { (TaskStatus.Failed, TaskStatus.Cancelled), true },
    { (TaskStatus.Blocked, TaskStatus.Pending), true },
    { (TaskStatus.Blocked, TaskStatus.Cancelled), true },
};
```

---

**End of Task 026.b Specification**