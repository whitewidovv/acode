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

## Assumptions

### Technical Assumptions

1. **SQLite Availability**: Microsoft.Data.Sqlite is available for queue persistence
2. **WAL Mode Support**: SQLite WAL mode is enabled for concurrent read/write performance
3. **File System Reliability**: Local file system provides ACID guarantees with WAL mode
4. **Single Writer**: Only one process writes to queue at a time (worker pool is single-host)
5. **Memory Constraints**: Queue can hold metadata for 10,000+ tasks in memory
6. **Transaction Support**: SQLite transactions are used for atomic state changes

### State Machine Assumptions

7. **Finite States**: All possible task states are pre-defined and enumerable
8. **Valid Transitions**: State machine defines allowed transitions as directed graph
9. **Atomic Transitions**: State changes are atomic (no partial transitions)
10. **Transition Logging**: All state transitions are recorded in audit log
11. **Event Emission**: State changes trigger observable events for subscribers

### Integration Assumptions

12. **Task Spec Compatibility**: TaskSpec from task-025 is the unit of work in queue
13. **Worker Pool Integration**: Worker pool (task-027) dequeues and processes tasks
14. **CLI Access**: CLI commands (task-025b) can query and modify queue state
15. **Crash Recovery**: System can recover queue state after unexpected termination

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

## Best Practices

### Queue Design

1. **Interface-First**: Define ITaskQueue interface before implementation for testability
2. **Async All The Way**: All queue operations should be async to avoid blocking
3. **Bounded Queue**: Consider memory-bounded queue with backpressure for safety
4. **Priority Ordering**: Always dequeue highest priority tasks first (priority queue semantics)

### Persistence Strategy

5. **Write-Ahead Logging**: Use SQLite WAL mode for concurrent reads during writes
6. **Batch Writes**: Batch multiple enqueue operations in single transaction when possible
7. **Index Strategically**: Index status+priority for efficient dequeue queries
8. **Checkpoint Regularly**: Configure WAL checkpointing to prevent unbounded WAL growth

### State Integrity

9. **Validate Transitions**: Always validate state transitions against allowed graph
10. **Atomic Updates**: State change + history insert in single transaction
11. **Emit After Commit**: Only emit events after transaction commits successfully
12. **Log Everything**: Include before/after state in transition logs for debugging

---

## Troubleshooting

### Issue: Queue Operations Slow Under Load

**Symptoms:** Enqueue/dequeue taking >100ms with many tasks

**Possible Causes:**
- Missing indexes on frequently queried columns
- WAL file grown too large without checkpointing
- Lock contention from concurrent CLI access

**Solutions:**
1. Add composite index on (status, priority, createdAt)
2. Configure automatic WAL checkpointing (PRAGMA wal_checkpoint)
3. Use connection pooling with appropriate timeouts

### Issue: Tasks Stuck in Running State

**Symptoms:** Tasks show Running status but no worker is processing them

**Possible Causes:**
- Previous worker crashed without cleanup
- Heartbeat mechanism not detecting stale workers
- Recovery process not running on startup

**Solutions:**
1. Run manual recovery: `acode queue recover`
2. Check heartbeat timeout configuration (default 60s)
3. Verify recovery runs on application startup

### Issue: State Transition Rejected

**Symptoms:** Transition fails with "invalid transition" error

**Possible Causes:**
- Attempting transition not in allowed graph
- Guard condition not satisfied
- Task already in terminal state

**Solutions:**
1. Review state machine diagram for allowed transitions
2. Check guard condition requirements in logs
3. Use `acode task show <id>` to verify current state

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

### File Structure

```
src/
├── Acode.Core/
│   └── Domain/
│       └── Queue/
│           ├── QueuedTask.cs             # Task in queue
│           ├── TaskTransition.cs         # State transition record
│           ├── TransitionHistory.cs      # Audit trail entry
│           └── QueueExceptions.cs        # Queue exceptions
│
├── Acode.Application/
│   └── Services/
│       └── Queue/
│           ├── ITaskQueue.cs             # Queue interface
│           ├── TaskQueue.cs              # SQLite implementation
│           ├── IStateMachine.cs          # Transition validation
│           ├── TaskStateMachine.cs       # State machine impl
│           ├── QueueFilter.cs            # Query filters
│           └── QueueCounts.cs            # Aggregate counts
│
├── Acode.Infrastructure/
│   └── Persistence/
│       └── Queue/
│           ├── QueueDbContext.cs         # SQLite context
│           ├── QueueMigrations.cs        # Schema migrations
│           └── QueueRepository.cs        # Data access
│
└── Acode.Cli/
    └── Commands/
        └── Queue/
            ├── QueueStatusCommand.cs
            └── QueuePruneCommand.cs

tests/
├── Acode.Application.Tests/
│   └── Services/
│       └── Queue/
│           ├── TaskQueueTests.cs
│           └── TaskStateMachineTests.cs
│
└── Acode.Integration.Tests/
    └── Queue/
        └── QueuePersistenceTests.cs
```

### Domain Models

```csharp
// Acode.Core/Domain/Queue/QueuedTask.cs
namespace Acode.Core.Domain.Queue;

/// <summary>
/// A task stored in the persistent queue.
/// </summary>
public sealed record QueuedTask
{
    /// <summary>Unique task identifier.</summary>
    public required TaskId Id { get; init; }
    
    /// <summary>Original task specification.</summary>
    public required TaskSpec Spec { get; init; }
    
    /// <summary>Current status.</summary>
    public required TaskStatus Status { get; init; }
    
    /// <summary>Worker currently processing (if Running).</summary>
    public string? WorkerId { get; init; }
    
    /// <summary>When the task was enqueued.</summary>
    public required DateTimeOffset CreatedAt { get; init; }
    
    /// <summary>When execution started.</summary>
    public DateTimeOffset? StartedAt { get; init; }
    
    /// <summary>When execution completed.</summary>
    public DateTimeOffset? CompletedAt { get; init; }
    
    /// <summary>Number of execution attempts.</summary>
    public int AttemptCount { get; init; }
    
    /// <summary>Result if completed.</summary>
    public TaskResult? Result { get; init; }
    
    /// <summary>Error message if failed.</summary>
    public string? Error { get; init; }
    
    /// <summary>Last heartbeat from worker.</summary>
    public DateTimeOffset? LastHeartbeat { get; init; }
    
    /// <summary>Row version for concurrency.</summary>
    public long Version { get; init; }
}

/// <summary>
/// Result of task execution.
/// </summary>
public sealed record TaskResult
{
    public required bool Success { get; init; }
    public string? Output { get; init; }
    public IReadOnlyList<string>? AffectedFiles { get; init; }
    public TimeSpan Duration { get; init; }
    public IReadOnlyDictionary<string, object?>? Metadata { get; init; }
}

// Acode.Core/Domain/Queue/TaskTransition.cs
namespace Acode.Core.Domain.Queue;

/// <summary>
/// Represents a state transition request.
/// </summary>
public sealed record TaskTransition
{
    public required TaskId TaskId { get; init; }
    public required TaskStatus FromStatus { get; init; }
    public required TaskStatus ToStatus { get; init; }
    public required string Actor { get; init; }  // "worker:abc" or "user:cancel"
    public string? Reason { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

// Acode.Core/Domain/Queue/TransitionHistory.cs
namespace Acode.Core.Domain.Queue;

/// <summary>
/// Audit trail entry for state transitions.
/// </summary>
public sealed record TransitionHistory
{
    public required long Id { get; init; }
    public required TaskId TaskId { get; init; }
    public required TaskStatus FromStatus { get; init; }
    public required TaskStatus ToStatus { get; init; }
    public required string Actor { get; init; }
    public string? Reason { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

// Acode.Core/Domain/Queue/QueueExceptions.cs
namespace Acode.Core.Domain.Queue;

public abstract class QueueException : Exception
{
    protected QueueException(string message) : base(message) { }
    protected QueueException(string message, Exception inner) : base(message, inner) { }
}

public sealed class InvalidTransitionException : QueueException
{
    public TaskStatus FromStatus { get; }
    public TaskStatus ToStatus { get; }
    
    public InvalidTransitionException(TaskStatus from, TaskStatus to)
        : base($"Invalid transition: {from} → {to}")
    {
        FromStatus = from;
        ToStatus = to;
    }
}

public sealed class TaskNotFoundException : QueueException
{
    public TaskId TaskId { get; }
    
    public TaskNotFoundException(TaskId taskId)
        : base($"Task not found: {taskId}")
    {
        TaskId = taskId;
    }
}

public sealed class ConcurrencyException : QueueException
{
    public TaskId TaskId { get; }
    
    public ConcurrencyException(TaskId taskId)
        : base($"Concurrent modification detected for task: {taskId}")
    {
        TaskId = taskId;
    }
}

public sealed class QueueFullException : QueueException
{
    public QueueFullException() : base("Queue is full") { }
}
```

### State Machine

```csharp
// Acode.Application/Services/Queue/IStateMachine.cs
namespace Acode.Application.Services.Queue;

/// <summary>
/// Validates task state transitions.
/// </summary>
public interface IStateMachine
{
    /// <summary>
    /// Checks if a transition is valid.
    /// </summary>
    bool IsValidTransition(TaskStatus from, TaskStatus to);
    
    /// <summary>
    /// Gets all valid transitions from a status.
    /// </summary>
    IReadOnlyList<TaskStatus> GetValidTransitions(TaskStatus from);
    
    /// <summary>
    /// Checks if a status is terminal.
    /// </summary>
    bool IsTerminal(TaskStatus status);
}

// Acode.Application/Services/Queue/TaskStateMachine.cs
namespace Acode.Application.Services.Queue;

public sealed class TaskStateMachine : IStateMachine
{
    private static readonly Dictionary<TaskStatus, HashSet<TaskStatus>> ValidTransitions = new()
    {
        [TaskStatus.Pending] = new()
        {
            TaskStatus.Running,
            TaskStatus.Cancelled,
            TaskStatus.Blocked
        },
        [TaskStatus.Running] = new()
        {
            TaskStatus.Completed,
            TaskStatus.Failed,
            TaskStatus.Cancelled
        },
        [TaskStatus.Failed] = new()
        {
            TaskStatus.Pending,  // Retry
            TaskStatus.Cancelled
        },
        [TaskStatus.Blocked] = new()
        {
            TaskStatus.Pending,  // Dependencies met
            TaskStatus.Cancelled
        },
        [TaskStatus.Completed] = new(),  // Terminal
        [TaskStatus.Cancelled] = new()   // Terminal
    };
    
    private static readonly HashSet<TaskStatus> TerminalStates = new()
    {
        TaskStatus.Completed,
        TaskStatus.Cancelled
    };
    
    public bool IsValidTransition(TaskStatus from, TaskStatus to)
    {
        return ValidTransitions.TryGetValue(from, out var valid) && valid.Contains(to);
    }
    
    public IReadOnlyList<TaskStatus> GetValidTransitions(TaskStatus from)
    {
        return ValidTransitions.TryGetValue(from, out var valid)
            ? valid.ToList()
            : Array.Empty<TaskStatus>();
    }
    
    public bool IsTerminal(TaskStatus status)
    {
        return TerminalStates.Contains(status);
    }
}
```

### Queue Filter and Counts

```csharp
// Acode.Application/Services/Queue/QueueFilter.cs
namespace Acode.Application.Services.Queue;

/// <summary>
/// Filter options for queue queries.
/// </summary>
public sealed record QueueFilter
{
    /// <summary>Filter by status.</summary>
    public TaskStatus? Status { get; init; }
    
    /// <summary>Filter by priority.</summary>
    public int? Priority { get; init; }
    
    /// <summary>Filter by tag.</summary>
    public string? Tag { get; init; }
    
    /// <summary>Filter by worker.</summary>
    public string? WorkerId { get; init; }
    
    /// <summary>Created after this time.</summary>
    public DateTimeOffset? CreatedAfter { get; init; }
    
    /// <summary>Created before this time.</summary>
    public DateTimeOffset? CreatedBefore { get; init; }
    
    /// <summary>Maximum results.</summary>
    public int Limit { get; init; } = 100;
    
    /// <summary>Offset for pagination.</summary>
    public int Offset { get; init; }
    
    public static QueueFilter All => new();
    public static QueueFilter Pending => new() { Status = TaskStatus.Pending };
    public static QueueFilter Running => new() { Status = TaskStatus.Running };
}

// Acode.Application/Services/Queue/QueueCounts.cs
namespace Acode.Application.Services.Queue;

/// <summary>
/// Aggregate task counts by status.
/// </summary>
public sealed record QueueCounts
{
    public int Pending { get; init; }
    public int Running { get; init; }
    public int Completed { get; init; }
    public int Failed { get; init; }
    public int Cancelled { get; init; }
    public int Blocked { get; init; }
    
    public int Total => Pending + Running + Completed + Failed + Cancelled + Blocked;
    public int Active => Pending + Running + Blocked;
}
```

### Queue Interface

```csharp
// Acode.Application/Services/Queue/ITaskQueue.cs
namespace Acode.Application.Services.Queue;

/// <summary>
/// Persistent task queue with state machine semantics.
/// </summary>
public interface ITaskQueue
{
    /// <summary>
    /// Adds a task to the queue.
    /// </summary>
    /// <returns>The assigned task ID.</returns>
    Task<TaskId> EnqueueAsync(TaskSpec spec, CancellationToken ct = default);
    
    /// <summary>
    /// Claims the next available task for a worker.
    /// </summary>
    /// <returns>The claimed task, or null if queue empty.</returns>
    Task<QueuedTask?> DequeueAsync(string workerId, CancellationToken ct = default);
    
    /// <summary>
    /// Marks a task as completed.
    /// </summary>
    Task CompleteAsync(TaskId taskId, TaskResult result, CancellationToken ct = default);
    
    /// <summary>
    /// Marks a task as failed.
    /// </summary>
    Task FailAsync(TaskId taskId, string error, CancellationToken ct = default);
    
    /// <summary>
    /// Cancels a task.
    /// </summary>
    Task CancelAsync(TaskId taskId, CancellationToken ct = default);
    
    /// <summary>
    /// Retries a failed task.
    /// </summary>
    Task RetryAsync(TaskId taskId, CancellationToken ct = default);
    
    /// <summary>
    /// Gets a task by ID.
    /// </summary>
    Task<QueuedTask?> GetAsync(TaskId taskId, CancellationToken ct = default);
    
    /// <summary>
    /// Lists tasks matching filter.
    /// </summary>
    Task<IReadOnlyList<QueuedTask>> ListAsync(QueueFilter filter, CancellationToken ct = default);
    
    /// <summary>
    /// Gets aggregate counts.
    /// </summary>
    Task<QueueCounts> CountAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets transition history for a task.
    /// </summary>
    Task<IReadOnlyList<TransitionHistory>> GetHistoryAsync(TaskId taskId, CancellationToken ct = default);
    
    /// <summary>
    /// Updates worker heartbeat.
    /// </summary>
    Task HeartbeatAsync(string workerId, TaskId taskId, CancellationToken ct = default);
    
    /// <summary>
    /// Finds orphaned tasks (running with no heartbeat).
    /// </summary>
    Task<IReadOnlyList<QueuedTask>> FindOrphanedAsync(TimeSpan heartbeatTimeout, CancellationToken ct = default);
}
```

### Queue Implementation

```csharp
// Acode.Application/Services/Queue/TaskQueue.cs
namespace Acode.Application.Services.Queue;

public sealed class TaskQueue : ITaskQueue
{
    private readonly IStateMachine _stateMachine;
    private readonly IQueueRepository _repository;
    private readonly IOutbox _outbox;
    private readonly ILogger<TaskQueue> _logger;
    private readonly SemaphoreSlim _dequeueLock = new(1, 1);
    
    public TaskQueue(
        IStateMachine stateMachine,
        IQueueRepository repository,
        IOutbox outbox,
        ILogger<TaskQueue> logger)
    {
        _stateMachine = stateMachine;
        _repository = repository;
        _outbox = outbox;
        _logger = logger;
    }
    
    public async Task<TaskId> EnqueueAsync(TaskSpec spec, CancellationToken ct = default)
    {
        _logger.LogInformation("Enqueueing task: {Title}", spec.Title);
        
        var task = new QueuedTask
        {
            Id = spec.Id,
            Spec = spec,
            Status = TaskStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            AttemptCount = 0,
            Version = 1
        };
        
        await _repository.InsertAsync(task, ct);
        
        await RecordTransitionAsync(new TaskTransition
        {
            TaskId = task.Id,
            FromStatus = TaskStatus.Pending, // Initial
            ToStatus = TaskStatus.Pending,
            Actor = "system:enqueue",
            Reason = "Task created"
        }, ct);
        
        await _outbox.PublishAsync(new TaskEnqueuedEvent(task.Id), ct);
        
        _logger.LogInformation("Task enqueued: {TaskId}", task.Id);
        return task.Id;
    }
    
    public async Task<QueuedTask?> DequeueAsync(string workerId, CancellationToken ct = default)
    {
        await _dequeueLock.WaitAsync(ct);
        try
        {
            // Get next pending task by priority, then FIFO
            var task = await _repository.GetNextPendingAsync(ct);
            if (task == null)
            {
                return null;
            }
            
            // Transition to Running
            var updated = task with
            {
                Status = TaskStatus.Running,
                WorkerId = workerId,
                StartedAt = DateTimeOffset.UtcNow,
                LastHeartbeat = DateTimeOffset.UtcNow,
                AttemptCount = task.AttemptCount + 1,
                Version = task.Version + 1
            };
            
            await _repository.UpdateAsync(updated, task.Version, ct);
            
            await RecordTransitionAsync(new TaskTransition
            {
                TaskId = task.Id,
                FromStatus = TaskStatus.Pending,
                ToStatus = TaskStatus.Running,
                Actor = $"worker:{workerId}",
                Reason = $"Attempt {updated.AttemptCount}"
            }, ct);
            
            await _outbox.PublishAsync(new TaskStartedEvent(task.Id, workerId), ct);
            
            _logger.LogInformation(
                "Task {TaskId} dequeued by worker {WorkerId}",
                task.Id, workerId);
            
            return updated;
        }
        finally
        {
            _dequeueLock.Release();
        }
    }
    
    public async Task CompleteAsync(TaskId taskId, TaskResult result, CancellationToken ct = default)
    {
        var task = await GetRequiredAsync(taskId, ct);
        
        ValidateTransition(task.Status, TaskStatus.Completed);
        
        var updated = task with
        {
            Status = TaskStatus.Completed,
            Result = result,
            CompletedAt = DateTimeOffset.UtcNow,
            Version = task.Version + 1
        };
        
        await _repository.UpdateAsync(updated, task.Version, ct);
        
        await RecordTransitionAsync(new TaskTransition
        {
            TaskId = taskId,
            FromStatus = task.Status,
            ToStatus = TaskStatus.Completed,
            Actor = $"worker:{task.WorkerId}",
            Reason = result.Success ? "Completed successfully" : "Completed with errors"
        }, ct);
        
        await _outbox.PublishAsync(new TaskCompletedEvent(taskId, result), ct);
        
        _logger.LogInformation("Task {TaskId} completed", taskId);
    }
    
    public async Task FailAsync(TaskId taskId, string error, CancellationToken ct = default)
    {
        var task = await GetRequiredAsync(taskId, ct);
        
        ValidateTransition(task.Status, TaskStatus.Failed);
        
        var updated = task with
        {
            Status = TaskStatus.Failed,
            Error = error,
            CompletedAt = DateTimeOffset.UtcNow,
            Version = task.Version + 1
        };
        
        await _repository.UpdateAsync(updated, task.Version, ct);
        
        await RecordTransitionAsync(new TaskTransition
        {
            TaskId = taskId,
            FromStatus = task.Status,
            ToStatus = TaskStatus.Failed,
            Actor = $"worker:{task.WorkerId}",
            Reason = TruncateError(error)
        }, ct);
        
        await _outbox.PublishAsync(new TaskFailedEvent(taskId, error), ct);
        
        _logger.LogWarning("Task {TaskId} failed: {Error}", taskId, error);
    }
    
    public async Task CancelAsync(TaskId taskId, CancellationToken ct = default)
    {
        var task = await GetRequiredAsync(taskId, ct);
        
        if (_stateMachine.IsTerminal(task.Status))
        {
            _logger.LogDebug("Task {TaskId} already in terminal state", taskId);
            return; // Idempotent
        }
        
        ValidateTransition(task.Status, TaskStatus.Cancelled);
        
        var updated = task with
        {
            Status = TaskStatus.Cancelled,
            CompletedAt = DateTimeOffset.UtcNow,
            Version = task.Version + 1
        };
        
        await _repository.UpdateAsync(updated, task.Version, ct);
        
        await RecordTransitionAsync(new TaskTransition
        {
            TaskId = taskId,
            FromStatus = task.Status,
            ToStatus = TaskStatus.Cancelled,
            Actor = "user:cancel"
        }, ct);
        
        await _outbox.PublishAsync(new TaskCancelledEvent(taskId), ct);
        
        _logger.LogInformation("Task {TaskId} cancelled", taskId);
    }
    
    public async Task RetryAsync(TaskId taskId, CancellationToken ct = default)
    {
        var task = await GetRequiredAsync(taskId, ct);
        
        if (task.Status != TaskStatus.Failed)
        {
            throw new InvalidTransitionException(task.Status, TaskStatus.Pending);
        }
        
        if (task.AttemptCount >= task.Spec.RetryLimit)
        {
            throw new InvalidOperationException(
                $"Retry limit exceeded ({task.AttemptCount}/{task.Spec.RetryLimit})");
        }
        
        var updated = task with
        {
            Status = TaskStatus.Pending,
            Error = null,
            WorkerId = null,
            StartedAt = null,
            CompletedAt = null,
            Version = task.Version + 1
        };
        
        await _repository.UpdateAsync(updated, task.Version, ct);
        
        await RecordTransitionAsync(new TaskTransition
        {
            TaskId = taskId,
            FromStatus = TaskStatus.Failed,
            ToStatus = TaskStatus.Pending,
            Actor = "user:retry",
            Reason = $"Manual retry (attempt {task.AttemptCount + 1})"
        }, ct);
        
        _logger.LogInformation("Task {TaskId} queued for retry", taskId);
    }
    
    public Task<QueuedTask?> GetAsync(TaskId taskId, CancellationToken ct = default)
    {
        return _repository.GetByIdAsync(taskId, ct);
    }
    
    public Task<IReadOnlyList<QueuedTask>> ListAsync(QueueFilter filter, CancellationToken ct = default)
    {
        return _repository.QueryAsync(filter, ct);
    }
    
    public Task<QueueCounts> CountAsync(CancellationToken ct = default)
    {
        return _repository.CountByStatusAsync(ct);
    }
    
    public Task<IReadOnlyList<TransitionHistory>> GetHistoryAsync(TaskId taskId, CancellationToken ct = default)
    {
        return _repository.GetHistoryAsync(taskId, ct);
    }
    
    public async Task HeartbeatAsync(string workerId, TaskId taskId, CancellationToken ct = default)
    {
        await _repository.UpdateHeartbeatAsync(taskId, DateTimeOffset.UtcNow, ct);
    }
    
    public Task<IReadOnlyList<QueuedTask>> FindOrphanedAsync(TimeSpan heartbeatTimeout, CancellationToken ct = default)
    {
        var threshold = DateTimeOffset.UtcNow - heartbeatTimeout;
        return _repository.FindOrphanedAsync(threshold, ct);
    }
    
    private async Task<QueuedTask> GetRequiredAsync(TaskId taskId, CancellationToken ct)
    {
        var task = await _repository.GetByIdAsync(taskId, ct);
        if (task == null)
            throw new TaskNotFoundException(taskId);
        return task;
    }
    
    private void ValidateTransition(TaskStatus from, TaskStatus to)
    {
        if (!_stateMachine.IsValidTransition(from, to))
        {
            throw new InvalidTransitionException(from, to);
        }
    }
    
    private Task RecordTransitionAsync(TaskTransition transition, CancellationToken ct)
    {
        return _repository.InsertHistoryAsync(new TransitionHistory
        {
            Id = 0, // Auto-increment
            TaskId = transition.TaskId,
            FromStatus = transition.FromStatus,
            ToStatus = transition.ToStatus,
            Actor = transition.Actor,
            Reason = transition.Reason,
            Timestamp = transition.Timestamp
        }, ct);
    }
    
    private static string TruncateError(string error)
    {
        const int maxLength = 500;
        return error.Length <= maxLength 
            ? error 
            : error[..(maxLength - 3)] + "...";
    }
}
```

### Repository Interface

```csharp
// Acode.Infrastructure/Persistence/Queue/IQueueRepository.cs
namespace Acode.Infrastructure.Persistence.Queue;

public interface IQueueRepository
{
    Task InsertAsync(QueuedTask task, CancellationToken ct);
    Task UpdateAsync(QueuedTask task, long expectedVersion, CancellationToken ct);
    Task<QueuedTask?> GetByIdAsync(TaskId id, CancellationToken ct);
    Task<QueuedTask?> GetNextPendingAsync(CancellationToken ct);
    Task<IReadOnlyList<QueuedTask>> QueryAsync(QueueFilter filter, CancellationToken ct);
    Task<QueueCounts> CountByStatusAsync(CancellationToken ct);
    Task InsertHistoryAsync(TransitionHistory history, CancellationToken ct);
    Task<IReadOnlyList<TransitionHistory>> GetHistoryAsync(TaskId taskId, CancellationToken ct);
    Task UpdateHeartbeatAsync(TaskId taskId, DateTimeOffset timestamp, CancellationToken ct);
    Task<IReadOnlyList<QueuedTask>> FindOrphanedAsync(DateTimeOffset heartbeatThreshold, CancellationToken ct);
}
```

### CLI Commands

```csharp
// Acode.Cli/Commands/Queue/QueueStatusCommand.cs
namespace Acode.Cli.Commands.Queue;

[Command("queue status", Description = "Show queue status")]
public sealed class QueueStatusCommand : ICommand
{
    [CommandOption("--json", Description = "Output as JSON")]
    public bool Json { get; init; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var queue = GetQueue(); // DI
        
        var counts = await queue.CountAsync();
        
        if (Json)
        {
            console.Output.WriteLine(JsonSerializer.Serialize(counts));
            return;
        }
        
        console.Output.WriteLine("Queue Status");
        console.Output.WriteLine("============");
        console.Output.WriteLine($"  Pending:   {counts.Pending}");
        console.Output.WriteLine($"  Running:   {counts.Running}");
        console.Output.WriteLine($"  Blocked:   {counts.Blocked}");
        console.Output.WriteLine($"  Completed: {counts.Completed}");
        console.Output.WriteLine($"  Failed:    {counts.Failed}");
        console.Output.WriteLine($"  Cancelled: {counts.Cancelled}");
        console.Output.WriteLine($"  ─────────────────");
        console.Output.WriteLine($"  Total:     {counts.Total}");
        console.Output.WriteLine($"  Active:    {counts.Active}");
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| QUEUE-001 | Task not found |
| QUEUE-002 | Invalid transition |
| QUEUE-003 | Concurrency conflict |
| QUEUE-004 | Queue full |
| QUEUE-005 | Retry limit exceeded |
| QUEUE-006 | Database error |

### Implementation Checklist

- [ ] Create `QueuedTask` record
- [ ] Create `TaskResult` record
- [ ] Create `TaskTransition` record
- [ ] Create `TransitionHistory` record
- [ ] Create queue exception types
- [ ] Implement `IStateMachine` interface
- [ ] Implement `TaskStateMachine` with valid transitions
- [ ] Add terminal state detection
- [ ] Create `QueueFilter` record
- [ ] Create `QueueCounts` record
- [ ] Define `ITaskQueue` interface
- [ ] Implement `TaskQueue` with all operations
- [ ] Add dequeue locking with SemaphoreSlim
- [ ] Add priority+FIFO ordering
- [ ] Add optimistic concurrency with version
- [ ] Add transition validation
- [ ] Add transition history recording
- [ ] Add outbox event publishing
- [ ] Add heartbeat support
- [ ] Add orphan detection
- [ ] Define `IQueueRepository` interface
- [ ] Implement SQLite repository
- [ ] Create CLI status command
- [ ] Register in DI
- [ ] Write unit tests for state machine
- [ ] Write integration tests for queue

### Rollout Plan

1. **Phase 1: Domain Models** (Day 1)
   - Create all records and exceptions
   - Unit test models

2. **Phase 2: State Machine** (Day 1)
   - Implement transition validation
   - Unit test all transitions

3. **Phase 3: Queue Interface** (Day 2)
   - Define interface with all operations
   - Create filter and counts types

4. **Phase 4: Queue Implementation** (Day 2-3)
   - Implement all queue operations
   - Add concurrency handling
   - Add transition history

5. **Phase 5: Repository** (Day 3)
   - Implement SQLite persistence
   - Add schema migration

6. **Phase 6: CLI + Integration** (Day 4)
   - Add CLI commands
   - Integration tests
   - Documentation

---

**End of Task 026 Specification**