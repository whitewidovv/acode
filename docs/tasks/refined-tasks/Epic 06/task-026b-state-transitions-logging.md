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

### File Structure

```
src/
├── Acode.Application/
│   └── Services/
│       └── TaskQueue/
│           ├── IStateMachine.cs           # State machine interface
│           ├── TaskStateMachine.cs        # Implementation
│           ├── TransitionMatrix.cs        # Valid transitions
│           ├── TransitionGuards.cs        # Guard conditions
│           ├── TransitionActions.cs       # Side effects
│           ├── ITransitionHistory.cs      # History interface
│           └── TransitionHistoryService.cs
│
├── Acode.Core/
│   └── Domain/
│       └── Tasks/
│           └── Events/
│               ├── TaskStatusChangedEvent.cs
│               ├── TaskStartedEvent.cs
│               ├── TaskCompletedEvent.cs
│               ├── TaskFailedEvent.cs
│               └── TaskCancelledEvent.cs

tests/
└── Acode.Application.Tests/
    └── Services/
        └── TaskQueue/
            ├── TaskStateMachineTests.cs
            └── TransitionHistoryTests.cs
```

### State Machine Interface

```csharp
// Acode.Application/Services/TaskQueue/IStateMachine.cs
namespace Acode.Application.Services.TaskQueue;

/// <summary>
/// Manages task state transitions with validation and logging.
/// </summary>
public interface IStateMachine
{
    /// <summary>
    /// Checks if a transition is valid.
    /// </summary>
    bool CanTransition(TaskStatus from, TaskStatus to);
    
    /// <summary>
    /// Executes a state transition atomically.
    /// </summary>
    Task<TransitionResult> TransitionAsync(
        TaskId taskId,
        TaskStatus to,
        TransitionActor actor,
        string? reason = null,
        TransitionContext? context = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets valid target states from a given state.
    /// </summary>
    IReadOnlyList<TaskStatus> GetValidTransitions(TaskStatus from);
    
    /// <summary>
    /// Gets the terminal states.
    /// </summary>
    IReadOnlySet<TaskStatus> TerminalStates { get; }
}

/// <summary>
/// Result of a transition attempt.
/// </summary>
public sealed record TransitionResult
{
    public bool Success { get; init; }
    public TaskStatus FromStatus { get; init; }
    public TaskStatus ToStatus { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string? Error { get; init; }
    public string? ErrorCode { get; init; }
    
    public static TransitionResult Succeeded(TaskStatus from, TaskStatus to) => new()
    {
        Success = true,
        FromStatus = from,
        ToStatus = to,
        Timestamp = DateTimeOffset.UtcNow
    };
    
    public static TransitionResult Failed(TaskStatus from, TaskStatus to, string error, string code) => new()
    {
        Success = false,
        FromStatus = from,
        ToStatus = to,
        Timestamp = DateTimeOffset.UtcNow,
        Error = error,
        ErrorCode = code
    };
}

/// <summary>
/// Actor performing the transition.
/// </summary>
public sealed record TransitionActor
{
    public required string Type { get; init; } // worker, user, system, scheduler
    public string? Id { get; init; }
    
    public override string ToString() => Id != null ? $"{Type}/{Id}" : Type;
    
    public static TransitionActor Worker(WorkerId id) => new() { Type = "worker", Id = id.Value };
    public static TransitionActor User(string name) => new() { Type = "user", Id = name };
    public static TransitionActor System => new() { Type = "system" };
    public static TransitionActor Scheduler => new() { Type = "scheduler" };
}

/// <summary>
/// Additional context for a transition.
/// </summary>
public sealed record TransitionContext
{
    public WorkerId? WorkerId { get; init; }
    public TaskResult? Result { get; init; }
    public string? Error { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
}
```

### Transition Matrix

```csharp
// Acode.Application/Services/TaskQueue/TransitionMatrix.cs
namespace Acode.Application.Services.TaskQueue;

/// <summary>
/// Defines valid state transitions.
/// </summary>
public static class TransitionMatrix
{
    private static readonly Dictionary<(TaskStatus From, TaskStatus To), TransitionSpec> _transitions = new()
    {
        // From Pending
        { (TaskStatus.Pending, TaskStatus.Running), new("Worker claims task", RequiresWorkerId: true) },
        { (TaskStatus.Pending, TaskStatus.Cancelled), new("User or system cancellation") },
        { (TaskStatus.Pending, TaskStatus.Blocked), new("Dependencies not satisfied") },
        
        // From Running
        { (TaskStatus.Running, TaskStatus.Completed), new("Task succeeded", RequiresResult: true) },
        { (TaskStatus.Running, TaskStatus.Failed), new("Task failed", RequiresError: true) },
        { (TaskStatus.Running, TaskStatus.Cancelled), new("Cancelled while running") },
        
        // From Failed
        { (TaskStatus.Failed, TaskStatus.Pending), new("Retry requested", IsRetry: true) },
        { (TaskStatus.Failed, TaskStatus.Cancelled), new("Cancellation after failure") },
        
        // From Blocked
        { (TaskStatus.Blocked, TaskStatus.Pending), new("Dependencies satisfied") },
        { (TaskStatus.Blocked, TaskStatus.Cancelled), new("Cancelled while blocked") },
    };
    
    private static readonly HashSet<TaskStatus> _terminalStates = new()
    {
        TaskStatus.Completed,
        TaskStatus.Cancelled
    };
    
    public static bool IsValid(TaskStatus from, TaskStatus to)
    {
        if (from == to) return false; // No self-transitions
        if (_terminalStates.Contains(from)) return false; // No transitions from terminal
        return _transitions.ContainsKey((from, to));
    }
    
    public static TransitionSpec? GetSpec(TaskStatus from, TaskStatus to) =>
        _transitions.TryGetValue((from, to), out var spec) ? spec : null;
    
    public static IReadOnlyList<TaskStatus> GetValidTargets(TaskStatus from)
    {
        if (_terminalStates.Contains(from))
            return Array.Empty<TaskStatus>();
        
        return _transitions.Keys
            .Where(k => k.From == from)
            .Select(k => k.To)
            .ToList();
    }
    
    public static IReadOnlySet<TaskStatus> TerminalStates => _terminalStates;
}

public sealed record TransitionSpec(
    string Description,
    bool RequiresWorkerId = false,
    bool RequiresResult = false,
    bool RequiresError = false,
    bool IsRetry = false);
```

### State Machine Implementation

```csharp
// Acode.Application/Services/TaskQueue/TaskStateMachine.cs
namespace Acode.Application.Services.TaskQueue;

public sealed class TaskStateMachine : IStateMachine
{
    private readonly ITaskRepository _repository;
    private readonly ITransitionHistoryService _history;
    private readonly IEventPublisher _events;
    private readonly ILogger<TaskStateMachine> _logger;
    
    public IReadOnlySet<TaskStatus> TerminalStates => TransitionMatrix.TerminalStates;
    
    public TaskStateMachine(
        ITaskRepository repository,
        ITransitionHistoryService history,
        IEventPublisher events,
        ILogger<TaskStateMachine> logger)
    {
        _repository = repository;
        _history = history;
        _events = events;
        _logger = logger;
    }
    
    public bool CanTransition(TaskStatus from, TaskStatus to)
    {
        return TransitionMatrix.IsValid(from, to);
    }
    
    public IReadOnlyList<TaskStatus> GetValidTransitions(TaskStatus from)
    {
        return TransitionMatrix.GetValidTargets(from);
    }
    
    public async Task<TransitionResult> TransitionAsync(
        TaskId taskId,
        TaskStatus to,
        TransitionActor actor,
        string? reason = null,
        TransitionContext? context = null,
        CancellationToken ct = default)
    {
        var task = await _repository.GetAsync(taskId, ct);
        if (task == null)
        {
            return TransitionResult.Failed(
                TaskStatus.Pending, to,
                $"Task not found: {taskId}",
                "QUEUE-001");
        }
        
        var from = task.Status;
        
        // Validate transition
        if (!CanTransition(from, to))
        {
            _logger.LogWarning(
                "Invalid transition attempted: {TaskId} {From} -> {To}",
                taskId, from, to);
            
            return TransitionResult.Failed(
                from, to,
                $"Invalid transition from {from} to {to}",
                "QUEUE-002");
        }
        
        // Check guards
        var spec = TransitionMatrix.GetSpec(from, to)!;
        var guardResult = await CheckGuardsAsync(task, to, spec, context, ct);
        if (!guardResult.Passed)
        {
            _logger.LogWarning(
                "Transition guard failed: {TaskId} {From} -> {To}: {Reason}",
                taskId, from, to, guardResult.Reason);
            
            return TransitionResult.Failed(from, to, guardResult.Reason!, "QUEUE-003");
        }
        
        // Execute transition in transaction
        var timestamp = DateTimeOffset.UtcNow;
        
        await _repository.ExecuteInTransactionAsync(async tx =>
        {
            // Apply transition actions
            ApplyTransitionActions(task, to, context, timestamp);
            
            // Update task
            task.Status = to;
            task.UpdatedAt = timestamp;
            await _repository.UpdateAsync(task, tx, ct);
            
            // Record history
            await _history.RecordAsync(new TransitionRecord
            {
                TaskId = taskId,
                FromStatus = from,
                ToStatus = to,
                Actor = actor.ToString(),
                Reason = reason,
                Timestamp = timestamp,
                WorkerId = context?.WorkerId?.Value,
                Context = context?.Metadata != null 
                    ? JsonSerializer.Serialize(context.Metadata)
                    : null
            }, tx, ct);
        }, ct);
        
        _logger.LogInformation(
            "Task {TaskId} transitioned {From} -> {To} by {Actor}",
            taskId, from, to, actor);
        
        // Emit event (async, non-blocking)
        _ = EmitEventAsync(taskId, from, to, actor, timestamp, context);
        
        return TransitionResult.Succeeded(from, to);
    }
    
    private async Task<GuardResult> CheckGuardsAsync(
        QueuedTask task,
        TaskStatus to,
        TransitionSpec spec,
        TransitionContext? context,
        CancellationToken ct)
    {
        // Check required context
        if (spec.RequiresWorkerId && context?.WorkerId == null)
        {
            return GuardResult.Fail("WorkerId required for this transition");
        }
        
        if (spec.RequiresResult && context?.Result == null)
        {
            return GuardResult.Fail("Result required for completion");
        }
        
        if (spec.RequiresError && string.IsNullOrEmpty(context?.Error))
        {
            return GuardResult.Fail("Error message required for failure");
        }
        
        // Check retry limit
        if (spec.IsRetry && task.AttemptCount >= task.Spec.RetryLimit)
        {
            return GuardResult.Fail($"Retry limit exceeded ({task.AttemptCount}/{task.Spec.RetryLimit})");
        }
        
        return GuardResult.Pass();
    }
    
    private void ApplyTransitionActions(
        QueuedTask task,
        TaskStatus to,
        TransitionContext? context,
        DateTimeOffset timestamp)
    {
        switch (to)
        {
            case TaskStatus.Running:
                task.StartedAt = timestamp;
                task.WorkerId = context?.WorkerId;
                break;
            
            case TaskStatus.Completed:
                task.CompletedAt = timestamp;
                task.Result = context?.Result;
                task.WorkerId = null;
                break;
            
            case TaskStatus.Failed:
                task.CompletedAt = timestamp;
                task.LastError = context?.Error;
                task.AttemptCount++;
                task.WorkerId = null;
                break;
            
            case TaskStatus.Pending when task.Status == TaskStatus.Failed:
                // Retry - optionally clear error
                task.StartedAt = null;
                task.CompletedAt = null;
                break;
            
            case TaskStatus.Cancelled:
                task.CompletedAt = timestamp;
                task.WorkerId = null;
                break;
        }
    }
    
    private async Task EmitEventAsync(
        TaskId taskId,
        TaskStatus from,
        TaskStatus to,
        TransitionActor actor,
        DateTimeOffset timestamp,
        TransitionContext? context)
    {
        try
        {
            var baseEvent = new TaskStatusChangedEvent
            {
                TaskId = taskId.Value,
                FromStatus = from.ToString(),
                ToStatus = to.ToString(),
                Actor = actor.ToString(),
                Timestamp = timestamp
            };
            
            await _events.PublishAsync(baseEvent);
            
            // Emit specific event
            IDomainEvent? specificEvent = (from, to) switch
            {
                (TaskStatus.Pending, TaskStatus.Running) => new TaskStartedEvent
                {
                    TaskId = taskId.Value,
                    WorkerId = context?.WorkerId?.Value,
                    Timestamp = timestamp
                },
                (TaskStatus.Running, TaskStatus.Completed) => new TaskCompletedEvent
                {
                    TaskId = taskId.Value,
                    Timestamp = timestamp
                },
                (TaskStatus.Running, TaskStatus.Failed) => new TaskFailedEvent
                {
                    TaskId = taskId.Value,
                    Error = context?.Error,
                    AttemptCount = context?.Metadata?.GetValueOrDefault("attemptCount") as int? ?? 0,
                    Timestamp = timestamp
                },
                (_, TaskStatus.Cancelled) => new TaskCancelledEvent
                {
                    TaskId = taskId.Value,
                    Timestamp = timestamp
                },
                _ => null
            };
            
            if (specificEvent != null)
            {
                await _events.PublishAsync(specificEvent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to emit event for task {TaskId}", taskId);
            // Event emission failure does not fail the transition
        }
    }
    
    private record struct GuardResult(bool Passed, string? Reason)
    {
        public static GuardResult Pass() => new(true, null);
        public static GuardResult Fail(string reason) => new(false, reason);
    }
}
```

### Transition History Interface

```csharp
// Acode.Application/Services/TaskQueue/ITransitionHistory.cs
namespace Acode.Application.Services.TaskQueue;

/// <summary>
/// Manages task state transition history.
/// </summary>
public interface ITransitionHistoryService
{
    /// <summary>
    /// Records a transition.
    /// </summary>
    Task RecordAsync(TransitionRecord record, IDbTransaction? tx = null, CancellationToken ct = default);
    
    /// <summary>
    /// Gets history for a task.
    /// </summary>
    Task<IReadOnlyList<TransitionRecord>> GetHistoryAsync(
        TaskId taskId,
        int? limit = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets recent transitions.
    /// </summary>
    Task<IReadOnlyList<TransitionRecord>> GetRecentAsync(
        DateTimeOffset since,
        int limit = 100,
        CancellationToken ct = default);
    
    /// <summary>
    /// Purges old history records.
    /// </summary>
    Task<PurgeResult> PurgeAsync(
        DateTimeOffset before,
        int batchSize = 1000,
        CancellationToken ct = default);
    
    /// <summary>
    /// Exports history.
    /// </summary>
    Task<Stream> ExportAsync(
        TaskId? taskId,
        DateTimeOffset? since,
        HistoryExportFormat format,
        CancellationToken ct = default);
}

public sealed record TransitionRecord
{
    public long Id { get; init; }
    public required TaskId TaskId { get; init; }
    public TaskStatus? FromStatus { get; init; }
    public required TaskStatus ToStatus { get; init; }
    public required string Actor { get; init; }
    public string? Reason { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public string? WorkerId { get; init; }
    public string? Context { get; init; }
}

public sealed record PurgeResult(int DeletedCount, TimeSpan Duration);

public enum HistoryExportFormat { Json, Csv }
```

### Transition History Service

```csharp
// Acode.Application/Services/TaskQueue/TransitionHistoryService.cs
namespace Acode.Application.Services.TaskQueue;

public sealed class TransitionHistoryService : ITransitionHistoryService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<TransitionHistoryService> _logger;
    
    public TransitionHistoryService(
        IDbConnectionFactory connectionFactory,
        ILogger<TransitionHistoryService> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }
    
    public async Task RecordAsync(
        TransitionRecord record,
        IDbTransaction? tx = null,
        CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO task_history (task_id, from_status, to_status, actor, reason, timestamp, worker_id, context)
            VALUES (@TaskId, @FromStatus, @ToStatus, @Actor, @Reason, @Timestamp, @WorkerId, @Context)";
        
        var conn = tx?.Connection ?? await _connectionFactory.CreateAsync(ct);
        
        await conn.ExecuteAsync(sql, new
        {
            TaskId = record.TaskId.Value,
            FromStatus = record.FromStatus?.ToString(),
            ToStatus = record.ToStatus.ToString(),
            record.Actor,
            record.Reason,
            Timestamp = record.Timestamp.ToString("O"),
            record.WorkerId,
            record.Context
        }, tx);
    }
    
    public async Task<IReadOnlyList<TransitionRecord>> GetHistoryAsync(
        TaskId taskId,
        int? limit = null,
        CancellationToken ct = default)
    {
        var sql = @"
            SELECT id, task_id, from_status, to_status, actor, reason, timestamp, worker_id, context
            FROM task_history
            WHERE task_id = @TaskId
            ORDER BY timestamp ASC";
        
        if (limit.HasValue)
            sql += " LIMIT @Limit";
        
        using var conn = await _connectionFactory.CreateAsync(ct);
        var rows = await conn.QueryAsync(sql, new { TaskId = taskId.Value, Limit = limit });
        
        return rows.Select(MapRecord).ToList();
    }
    
    public async Task<IReadOnlyList<TransitionRecord>> GetRecentAsync(
        DateTimeOffset since,
        int limit = 100,
        CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, task_id, from_status, to_status, actor, reason, timestamp, worker_id, context
            FROM task_history
            WHERE timestamp >= @Since
            ORDER BY timestamp DESC
            LIMIT @Limit";
        
        using var conn = await _connectionFactory.CreateAsync(ct);
        var rows = await conn.QueryAsync(sql, new { Since = since.ToString("O"), Limit = limit });
        
        return rows.Select(MapRecord).ToList();
    }
    
    public async Task<PurgeResult> PurgeAsync(
        DateTimeOffset before,
        int batchSize = 1000,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var totalDeleted = 0;
        
        using var conn = await _connectionFactory.CreateAsync(ct);
        
        while (!ct.IsCancellationRequested)
        {
            // Delete in batches to avoid long-running transactions
            var deleted = await conn.ExecuteAsync(@"
                DELETE FROM task_history
                WHERE id IN (
                    SELECT id FROM task_history
                    WHERE timestamp < @Before
                    LIMIT @BatchSize
                )", new { Before = before.ToString("O"), BatchSize = batchSize });
            
            totalDeleted += deleted;
            
            if (deleted < batchSize)
                break;
            
            // Yield to avoid blocking
            await Task.Delay(10, ct);
        }
        
        _logger.LogInformation("Purged {Count} history records older than {Before}", totalDeleted, before);
        
        return new PurgeResult(totalDeleted, sw.Elapsed);
    }
    
    public async Task<Stream> ExportAsync(
        TaskId? taskId,
        DateTimeOffset? since,
        HistoryExportFormat format,
        CancellationToken ct = default)
    {
        var sql = "SELECT * FROM task_history WHERE 1=1";
        var parameters = new DynamicParameters();
        
        if (taskId != null)
        {
            sql += " AND task_id = @TaskId";
            parameters.Add("TaskId", taskId.Value);
        }
        
        if (since != null)
        {
            sql += " AND timestamp >= @Since";
            parameters.Add("Since", since.Value.ToString("O"));
        }
        
        sql += " ORDER BY timestamp ASC";
        
        using var conn = await _connectionFactory.CreateAsync(ct);
        var rows = await conn.QueryAsync(sql, parameters);
        var records = rows.Select(MapRecord).ToList();
        
        var stream = new MemoryStream();
        
        if (format == HistoryExportFormat.Json)
        {
            await JsonSerializer.SerializeAsync(stream, records, cancellationToken: ct);
        }
        else
        {
            await using var writer = new StreamWriter(stream, leaveOpen: true);
            await writer.WriteLineAsync("id,task_id,from_status,to_status,actor,reason,timestamp");
            foreach (var r in records)
            {
                await writer.WriteLineAsync($"{r.Id},{r.TaskId},{r.FromStatus},{r.ToStatus},{r.Actor},{r.Reason},{r.Timestamp:O}");
            }
        }
        
        stream.Position = 0;
        return stream;
    }
    
    private static TransitionRecord MapRecord(dynamic row) => new()
    {
        Id = (long)row.id,
        TaskId = new TaskId((string)row.task_id),
        FromStatus = row.from_status != null ? Enum.Parse<TaskStatus>((string)row.from_status) : null,
        ToStatus = Enum.Parse<TaskStatus>((string)row.to_status),
        Actor = (string)row.actor,
        Reason = row.reason,
        Timestamp = DateTimeOffset.Parse((string)row.timestamp),
        WorkerId = row.worker_id,
        Context = row.context
    };
}
```

### Domain Events

```csharp
// Acode.Core/Domain/Tasks/Events/TaskStatusChangedEvent.cs
namespace Acode.Core.Domain.Tasks.Events;

public sealed record TaskStatusChangedEvent : IDomainEvent
{
    public required string TaskId { get; init; }
    public required string FromStatus { get; init; }
    public required string ToStatus { get; init; }
    public required string Actor { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed record TaskStartedEvent : IDomainEvent
{
    public required string TaskId { get; init; }
    public string? WorkerId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed record TaskCompletedEvent : IDomainEvent
{
    public required string TaskId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed record TaskFailedEvent : IDomainEvent
{
    public required string TaskId { get; init; }
    public string? Error { get; init; }
    public int AttemptCount { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}

public sealed record TaskCancelledEvent : IDomainEvent
{
    public required string TaskId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
```

### Implementation Checklist

- [ ] Create `TransitionMatrix` with all valid transitions
- [ ] Define `TransitionSpec` for each transition
- [ ] Define `IStateMachine` interface
- [ ] Implement `TaskStateMachine`
- [ ] Add guard validation logic
- [ ] Add transition action logic (startedAt, completedAt, etc.)
- [ ] Implement atomic transaction handling
- [ ] Define `ITransitionHistoryService` interface
- [ ] Implement `TransitionHistoryService`
- [ ] Add batch purge with non-blocking yields
- [ ] Add JSON export
- [ ] Add CSV export
- [ ] Define all domain events
- [ ] Implement event emission (async, non-blocking)
- [ ] Add structured logging
- [ ] Register in DI
- [ ] Write state machine unit tests
- [ ] Write history service tests
- [ ] Test concurrent transitions

### Rollout Plan

1. **Phase 1: Transition Matrix** (Day 1)
   - Define all valid transitions
   - Add transition specs
   - Unit tests

2. **Phase 2: State Machine** (Day 2)
   - Guard checking
   - Action application
   - Transaction handling

3. **Phase 3: History Service** (Day 2)
   - Recording
   - Querying
   - Purging

4. **Phase 4: Events** (Day 3)
   - Domain event definitions
   - Event emission
   - Async handling

5. **Phase 5: Integration** (Day 3)
   - DI registration
   - End-to-end testing
   - Performance validation

---

**End of Task 026.b Specification**