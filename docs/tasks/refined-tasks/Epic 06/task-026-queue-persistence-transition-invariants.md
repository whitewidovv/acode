# Task 026: Queue Persistence + Transition Invariants

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 025 (Task Spec), Task 018 (Outbox), Task 004 (DI)  

---

## Description

Task 026 implements persistent task queue storage with state transition invariants. The queue MUST survive process restarts. State transitions MUST be atomic and logged. Invalid transitions MUST be rejected.

The queue MUST use SQLite for persistence. All state changes MUST be transactional. Crash recovery MUST restore the queue to a consistent state. Orphaned tasks MUST be detected and handled.

State transitions MUST follow a defined state machine. Each transition MUST be validated before execution. Transition history MUST be preserved for audit. Invalid transitions MUST produce clear errors.

### Business Value

Persistent queue enables:
- Survival across restarts
- Crash recovery without data loss
- Audit trail of all state changes
- Consistent task lifecycle
- Reliable task execution

### Scope Boundaries

This task covers queue persistence and transitions. SQLite schema is in Task 026.a. Transition logging is in Task 026.b. Crash recovery is in Task 026.c.

### Integration Points

- Task 025: Task specs stored
- Task 018: State changes emit to outbox
- Task 027: Workers dequeue tasks
- Task 004: DI for queue service

### Failure Modes

- DB corruption → Restore from WAL
- Transaction failure → Rollback
- Invalid transition → Reject with error
- Disk full → Queue read-only
- Lock contention → Timeout and retry

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Queue | Ordered collection of tasks |
| State Machine | Valid state transitions |
| Transition | State change operation |
| Invariant | Condition that must hold |
| WAL | Write-Ahead Logging mode |
| Transaction | Atomic operation unit |
| Checkpoint | WAL flush to main DB |
| Orphan | Task with no worker |
| Heartbeat | Worker liveness signal |
| Lease | Worker task ownership |

---

## Out of Scope

- Distributed queue
- Message queue integration
- Priority queue variants
- Queue sharding
- Multi-database support
- Cloud queue migration

---

## Functional Requirements

### FR-001 to FR-030: Queue Operations

- FR-001: `ITaskQueue` interface MUST be defined
- FR-002: `EnqueueAsync` MUST add task
- FR-003: Enqueue MUST return task ID
- FR-004: Enqueue MUST validate spec
- FR-005: Enqueue MUST set Pending status
- FR-006: Enqueue MUST set createdAt
- FR-007: `DequeueAsync` MUST claim task
- FR-008: Dequeue MUST use priority order
- FR-009: Dequeue MUST use FIFO within priority
- FR-010: Dequeue MUST set Running status
- FR-011: Dequeue MUST set startedAt
- FR-012: Dequeue MUST record workerId
- FR-013: Dequeue MUST be atomic
- FR-014: `CompleteAsync` MUST mark done
- FR-015: Complete MUST set Completed status
- FR-016: Complete MUST set completedAt
- FR-017: Complete MUST store result
- FR-018: `FailAsync` MUST mark failed
- FR-019: Fail MUST set Failed status
- FR-020: Fail MUST store error
- FR-021: Fail MUST increment attemptCount
- FR-022: `CancelAsync` MUST mark cancelled
- FR-023: Cancel MUST set Cancelled status
- FR-024: Cancel MUST be idempotent
- FR-025: `GetAsync` MUST retrieve by ID
- FR-026: `ListAsync` MUST support filters
- FR-027: `CountAsync` MUST return counts
- FR-028: All operations MUST be logged
- FR-029: All operations MUST emit events
- FR-030: All operations MUST be transactional

### FR-031 to FR-055: State Machine

- FR-031: States: Pending, Running, Completed, Failed, Cancelled, Blocked
- FR-032: Initial state MUST be Pending
- FR-033: Terminal states: Completed, Cancelled
- FR-034: Pending → Running MUST be valid
- FR-035: Pending → Cancelled MUST be valid
- FR-036: Pending → Blocked MUST be valid
- FR-037: Running → Completed MUST be valid
- FR-038: Running → Failed MUST be valid
- FR-039: Running → Cancelled MUST be valid
- FR-040: Failed → Pending MUST be valid (retry)
- FR-041: Failed → Cancelled MUST be valid
- FR-042: Blocked → Pending MUST be valid
- FR-043: Blocked → Cancelled MUST be valid
- FR-044: Completed → * MUST be invalid
- FR-045: Cancelled → * MUST be invalid
- FR-046: Invalid transitions MUST throw
- FR-047: Transition MUST be atomic
- FR-048: Transition MUST log before/after
- FR-049: Transition MUST check invariants
- FR-050: Transition MUST emit event
- FR-051: Transition history MUST be stored
- FR-052: History MUST include timestamp
- FR-053: History MUST include actor
- FR-054: History MUST include reason
- FR-055: History MUST be queryable

### FR-056 to FR-075: Persistence

- FR-056: SQLite MUST be storage engine
- FR-057: WAL mode MUST be enabled
- FR-058: Journal MUST be persistent
- FR-059: Checkpoint MUST be automatic
- FR-060: Connection pool MUST be used
- FR-061: Pool size MUST be configurable
- FR-062: Default pool size MUST be 5
- FR-063: Timeout MUST be configurable
- FR-064: Default timeout MUST be 30s
- FR-065: Retry on busy MUST be enabled
- FR-066: Busy timeout MUST be 5s
- FR-067: Foreign keys MUST be enabled
- FR-068: Indexes MUST exist for filters
- FR-069: Vacuum MUST be periodic
- FR-070: Integrity check MUST be available
- FR-071: Backup MUST be supported
- FR-072: Backup MUST be online
- FR-073: Restore MUST be documented
- FR-074: Migration MUST be versioned
- FR-075: Schema version MUST be tracked

---

## Non-Functional Requirements

- NFR-001: Enqueue MUST complete in <50ms
- NFR-002: Dequeue MUST complete in <50ms
- NFR-003: List MUST complete in <100ms
- NFR-004: 100k tasks MUST be supported
- NFR-005: DB size MUST be bounded
- NFR-006: Old completed tasks MUST be archived
- NFR-007: Archive age MUST be configurable
- NFR-008: Default archive age: 30 days
- NFR-009: Concurrent access MUST work
- NFR-010: No data loss on crash
- NFR-011: Recovery MUST be <5s
- NFR-012: Transition audit MUST be complete

---

## User Manual Documentation

### Queue Configuration

```yaml
queue:
  database: ".agent/queue.db"
  poolSize: 5
  timeoutSeconds: 30
  busyTimeoutMs: 5000
  archiveAfterDays: 30
  checkpointIntervalSeconds: 60
```

### State Diagram

```
         ┌──────────────────┐
         │                  │
         ▼                  │
    ┌─────────┐        ┌────┴────┐
    │ Pending │───────▶│ Running │
    └────┬────┘        └────┬────┘
         │                  │
         │    ┌─────────────┼─────────────┐
         │    │             │             │
         ▼    ▼             ▼             ▼
    ┌─────────┐       ┌───────────┐  ┌─────────┐
    │ Blocked │       │ Completed │  │ Failed  │
    └────┬────┘       └───────────┘  └────┬────┘
         │                                │
         │      ┌─────────────────────────┘
         │      │
         ▼      ▼
    ┌───────────┐
    │ Cancelled │
    └───────────┘
```

### Valid Transitions

| From | To | Trigger |
|------|----|---------|
| Pending | Running | Worker claims |
| Pending | Cancelled | User cancels |
| Pending | Blocked | Dependencies pending |
| Running | Completed | Success |
| Running | Failed | Error |
| Running | Cancelled | User cancels |
| Failed | Pending | Retry |
| Failed | Cancelled | User cancels |
| Blocked | Pending | Dependencies met |
| Blocked | Cancelled | User cancels |

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Enqueue works
- [ ] AC-002: Dequeue works
- [ ] AC-003: Complete works
- [ ] AC-004: Fail works
- [ ] AC-005: Cancel works
- [ ] AC-006: Priority ordering works
- [ ] AC-007: State transitions validated
- [ ] AC-008: Invalid transitions rejected
- [ ] AC-009: Transition history stored
- [ ] AC-010: SQLite persists data
- [ ] AC-011: WAL mode enabled
- [ ] AC-012: Crash recovery works
- [ ] AC-013: Events emitted
- [ ] AC-014: Logging complete
- [ ] AC-015: Performance targets met

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Enqueue validation
- [ ] UT-002: Dequeue priority
- [ ] UT-003: State transitions
- [ ] UT-004: Invalid transition rejection
- [ ] UT-005: History recording

### Integration Tests

- [ ] IT-001: Full lifecycle
- [ ] IT-002: Concurrent access
- [ ] IT-003: Crash recovery
- [ ] IT-004: Large queue handling

---

## Implementation Prompt

### Interface

```csharp
public interface ITaskQueue
{
    Task<string> EnqueueAsync(TaskSpec spec, 
        CancellationToken ct = default);
        
    Task<QueuedTask?> DequeueAsync(string workerId, 
        CancellationToken ct = default);
        
    Task CompleteAsync(string taskId, TaskResult result, 
        CancellationToken ct = default);
        
    Task FailAsync(string taskId, string error, 
        CancellationToken ct = default);
        
    Task CancelAsync(string taskId, 
        CancellationToken ct = default);
        
    Task<QueuedTask?> GetAsync(string taskId, 
        CancellationToken ct = default);
        
    Task<IReadOnlyList<QueuedTask>> ListAsync(QueueFilter filter, 
        CancellationToken ct = default);
        
    Task<QueueCounts> CountAsync(
        CancellationToken ct = default);
}

public record QueueCounts(
    int Pending,
    int Running,
    int Completed,
    int Failed,
    int Cancelled,
    int Blocked);
```

---

**End of Task 026 Specification**