# Task 026.c: Crash Recovery Handling

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 026 (Queue), Task 026.a (Schema), Task 026.b (Transitions)  

---

## Description

Task 026.c implements crash recovery for the task queue. After a crash, the system MUST restore the queue to a consistent state. Running tasks MUST be detected and handled. Orphaned tasks MUST be recovered.

Recovery MUST run automatically on startup. The process MUST be idempotent. Multiple recovery attempts MUST produce the same result. Recovery MUST complete before normal operations.

The system MUST detect stale workers. Workers that stop heartbeating MUST have their tasks recovered. Tasks MUST be retried or failed based on configuration.

### Business Value

Crash recovery enables:
- No data loss after crashes
- Automatic system restoration
- Minimal manual intervention
- Reliable task execution
- Predictable failure handling

### Scope Boundaries

This task covers crash recovery. Normal queue operations are in Task 026. State transitions are in Task 026.b. Worker management is in Task 027.

### Integration Points

- Task 026: Queue state restoration
- Task 026.b: Transition to recovered state
- Task 027: Worker heartbeat detection
- Task 020: Recovery events logged

### Failure Modes

- Recovery fails → Manual intervention required
- DB corruption → WAL recovery attempted
- Partial recovery → System degraded mode

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Crash | Unexpected process termination |
| Recovery | Restoration after crash |
| Orphan | Task with dead worker |
| Heartbeat | Worker liveness signal |
| Stale | Worker not heartbeating |
| WAL | Write-Ahead Log for SQLite |
| Checkpoint | WAL flush to main DB |
| Idempotent | Same result on repeated run |
| Lease | Time-limited task ownership |

---

## Out of Scope

- Distributed crash recovery
- Point-in-time recovery
- Incremental backup restore
- Cross-node coordination
- Automatic failover

---

## Assumptions

### Technical Assumptions

1. **WAL Checkpointing**: SQLite WAL mode provides crash recovery for committed transactions
2. **Process Restart**: Application can restart and resume after unexpected termination
3. **File System Integrity**: Underlying file system doesn't corrupt SQLite files
4. **Heartbeat Mechanism**: Running workers emit periodic heartbeats to detect hangs
5. **Timeout Configuration**: Heartbeat timeout is configurable (default 60 seconds)
6. **Single Recovery**: Only one recovery process runs at startup (no concurrent recovery)

### Recovery Scenarios

7. **Orphaned Tasks**: Tasks left in Running state after crash are detected as orphaned
8. **Recovery Actions**: Orphaned tasks are transitioned to Retry or Failed based on policy
9. **Idempotent Recovery**: Running recovery multiple times produces same result
10. **Partial Work**: Partially completed work (uncommitted changes) is discarded on recovery
11. **Lock Release**: Any held locks are released after crash (SQLite handles this)

### Operational Assumptions

12. **Recovery Time**: Recovery completes within 30 seconds for typical queue sizes (<10K tasks)
13. **Logging During Recovery**: All recovery actions are logged with detail
14. **Event Notification**: Recovery start/complete events are emitted for monitoring
15. **Manual Override**: Administrator can force recovery actions via CLI if needed

---

## Functional Requirements

### FR-001 to FR-030: Startup Recovery

- FR-001: Recovery MUST run on startup
- FR-002: Recovery MUST complete before operations
- FR-003: Recovery MUST be logged
- FR-004: Recovery start event MUST emit
- FR-005: Recovery complete event MUST emit
- FR-006: Recovery MUST check DB integrity
- FR-007: Integrity failure MUST attempt repair
- FR-008: Repair failure MUST halt startup
- FR-009: WAL MUST be checkpointed
- FR-010: Orphaned transactions MUST rollback
- FR-011: Running tasks MUST be identified
- FR-012: Running tasks from dead workers MUST recover
- FR-013: Recovery action MUST be configurable
- FR-014: Actions: retry, fail, pending
- FR-015: Default action MUST be retry
- FR-016: Retry MUST check attempt limit
- FR-017: Over-limit MUST fail task
- FR-018: Recovery MUST update task status
- FR-019: Recovery MUST log transition
- FR-020: Recovery MUST emit event
- FR-021: Recovery actor MUST be "system/recovery"
- FR-022: Recovery reason MUST explain
- FR-023: Recovery count MUST be tracked
- FR-024: Recovery time MUST be tracked
- FR-025: Recovery report MUST be generated
- FR-026: Report MUST show recovered tasks
- FR-027: Report MUST show failed recoveries
- FR-028: Report MUST show duration
- FR-029: Recovery MUST be idempotent
- FR-030: Repeated recovery MUST be safe

### FR-031 to FR-055: Worker Heartbeat

- FR-031: Workers MUST send heartbeats
- FR-032: Heartbeat interval MUST be configurable
- FR-033: Default interval MUST be 30 seconds
- FR-034: Heartbeat MUST update timestamp
- FR-035: Heartbeat MUST include worker ID
- FR-036: Heartbeat MUST include task ID
- FR-037: Stale threshold MUST be configurable
- FR-038: Default threshold MUST be 3x interval
- FR-039: Stale detection MUST run periodically
- FR-040: Detection interval MUST be configurable
- FR-041: Default detection: every 60 seconds
- FR-042: Stale workers MUST be identified
- FR-043: Stale tasks MUST be recovered
- FR-044: Recovery MUST follow same rules
- FR-045: Worker MUST be marked dead
- FR-046: Dead worker MUST be logged
- FR-047: Dead worker event MUST emit
- FR-048: Worker cleanup MUST run
- FR-049: Cleanup MUST release resources
- FR-050: Cleanup MUST be logged
- FR-051: Heartbeat failure MUST NOT crash worker
- FR-052: Heartbeat retry MUST be attempted
- FR-053: Heartbeat timeout MUST be reasonable
- FR-054: Network issues MUST be tolerated
- FR-055: Heartbeat status MUST be queryable

### FR-056 to FR-070: Data Integrity

- FR-056: SQLite integrity_check MUST run
- FR-057: Check failure MUST log
- FR-058: Check failure MUST attempt repair
- FR-059: VACUUM MUST be attempted if needed
- FR-060: Reindex MUST be attempted if needed
- FR-061: WAL checkpoint MUST be forced
- FR-062: WAL size MUST be checked
- FR-063: Large WAL MUST checkpoint
- FR-064: Checkpoint failure MUST log
- FR-065: Journal mode MUST be verified
- FR-066: WAL mode MUST be restored if changed
- FR-067: Backup MUST be recommended if corrupt
- FR-068: Recovery point MUST be identified
- FR-069: Partial data MUST be flagged
- FR-070: Inconsistencies MUST be reported

---

## Non-Functional Requirements

- NFR-001: Recovery MUST complete in <30s
- NFR-002: Integrity check MUST complete in <10s
- NFR-003: Heartbeat overhead MUST be <1% CPU
- NFR-004: Stale detection MUST be <1s
- NFR-005: No data loss on clean crash
- NFR-006: Idempotent recovery
- NFR-007: Clear recovery logging
- NFR-008: Recovery metrics exported
- NFR-009: Degraded mode if needed
- NFR-010: Human-readable reports

---

## User Manual Documentation

### Recovery Configuration

```yaml
queue:
  recovery:
    action: retry  # retry, fail, pending
    maxRecoveryAttempts: 3
    integrityCheckOnStartup: true
    
  heartbeat:
    intervalSeconds: 30
    staleThresholdMultiplier: 3
    detectionIntervalSeconds: 60
```

### Recovery Process

1. **Startup Check**: System checks for previous unclean shutdown
2. **Integrity Verification**: SQLite integrity check runs
3. **WAL Checkpoint**: Write-ahead log flushed to main database
4. **Orphan Detection**: Tasks with status=Running from dead workers
5. **Recovery Action**: Tasks transitioned based on configuration
6. **Report Generation**: Summary of recovered tasks

### Recovery Report

```
=== Queue Recovery Report ===
Started: 2024-01-15T10:30:00Z
Duration: 1.2s

Integrity Check: PASSED
WAL Checkpointed: 145 frames

Orphaned Tasks Found: 3
  - abc123: retry (attempt 2/3)
  - def456: retry (attempt 3/3)
  - ghi789: failed (max attempts exceeded)

Dead Workers: 1
  - worker-001 (last heartbeat: 5m ago)

Recovery Complete: 2024-01-15T10:30:01Z
```

### Monitoring

```bash
# Check recovery status
acode queue recovery-status

# View last recovery report
acode queue recovery-report

# Check worker heartbeats
acode worker list --show-heartbeat
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Startup recovery runs
- [ ] AC-002: Orphaned tasks detected
- [ ] AC-003: Recovery action applied
- [ ] AC-004: Retry respects limits
- [ ] AC-005: Over-limit tasks failed
- [ ] AC-006: Heartbeat works
- [ ] AC-007: Stale detection works
- [ ] AC-008: Dead workers handled
- [ ] AC-009: Integrity check runs
- [ ] AC-010: WAL checkpointed
- [ ] AC-011: Recovery idempotent
- [ ] AC-012: Report generated
- [ ] AC-013: Events emitted
- [ ] AC-014: Logging complete
- [ ] AC-015: Metrics tracked

---

## Best Practices

### Recovery Design

1. **Fail-Safe Defaults**: When in doubt, mark orphaned tasks for retry rather than failure
2. **Idempotent Recovery**: Running recovery twice should produce same result
3. **Log Everything**: Recovery actions are critical audit events, log extensively
4. **Progress Reporting**: Long recovery should emit progress events for monitoring

### Heartbeat Strategy

5. **Reasonable Timeout**: Heartbeat timeout should be 2-3x expected heartbeat interval
6. **Grace Period**: Allow brief grace period after timeout before declaring orphaned
7. **Heartbeat Includes Context**: Include worker ID, task ID, and progress in heartbeat
8. **Separate Heartbeat Table**: Don't update main task record for heartbeats (reduces contention)

### Operational Safety

9. **Block Operations During Recovery**: Don't start new tasks until recovery completes
10. **Recovery Report**: Generate summary report of all recovery actions taken
11. **Manual Override**: Provide CLI to force specific recovery action on stuck tasks
12. **Alerting Integration**: Emit recoverable events for monitoring/alerting systems

---

## Troubleshooting

### Issue: Recovery Takes Too Long

**Symptoms:** Startup blocked for minutes during recovery phase

**Possible Causes:**
- Large number of orphaned tasks to process
- Slow database queries for orphan detection
- Recovery actions triggering slow side effects

**Solutions:**
1. Add index on (status, lastHeartbeat) for orphan detection query
2. Process orphaned tasks in batches with progress reporting
3. Defer non-critical recovery actions to background after startup

### Issue: Tasks Keep Getting Orphaned

**Symptoms:** Same tasks repeatedly appear as orphaned after each restart

**Possible Causes:**
- Heartbeat timeout too aggressive for task duration
- Worker not emitting heartbeats during long operations
- Clock skew between workers and database

**Solutions:**
1. Increase heartbeat timeout for long-running tasks
2. Ensure worker emits heartbeat during all phases, not just between tasks
3. Use database server time for heartbeat timestamp, not local clock

### Issue: Recovery Actions Not Applied

**Symptoms:** Orphaned tasks detected but not recovered

**Possible Causes:**
- Recovery policy returning "no action" for task type
- Transaction failing during recovery update
- Recovery disabled by configuration

**Solutions:**
1. Review recovery policy configuration for task type
2. Enable verbose logging to see recovery decision reasoning
3. Check ACODE_RECOVERY_ENABLED environment variable

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Orphan detection logic
- [ ] UT-002: Recovery action selection
- [ ] UT-003: Heartbeat timeout
- [ ] UT-004: Stale threshold calculation
- [ ] UT-005: Idempotency verification

### Integration Tests

- [ ] IT-001: Full crash recovery
- [ ] IT-002: Worker death handling
- [ ] IT-003: WAL recovery
- [ ] IT-004: Multiple crash cycles

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Application/
│   └── Services/
│       └── TaskQueue/
│           ├── IQueueRecovery.cs           # Recovery interface
│           ├── QueueRecoveryService.cs     # Implementation
│           ├── RecoveryReport.cs           # Report model
│           ├── IWorkerHeartbeat.cs         # Heartbeat interface
│           ├── WorkerHeartbeatService.cs   # Implementation
│           └── StaleWorkerDetector.cs      # Detection logic
│
├── Acode.Infrastructure/
│   └── Persistence/
│       └── TaskQueue/
│           └── SqliteIntegrityChecker.cs   # SQLite checks

tests/
└── Acode.Application.Tests/
    └── Services/
        └── TaskQueue/
            ├── QueueRecoveryServiceTests.cs
            └── StaleWorkerDetectorTests.cs
```

### Recovery Interface

```csharp
// Acode.Application/Services/TaskQueue/IQueueRecovery.cs
namespace Acode.Application.Services.TaskQueue;

/// <summary>
/// Handles crash recovery for the task queue.
/// </summary>
public interface IQueueRecovery
{
    /// <summary>
    /// Runs full recovery process.
    /// </summary>
    Task<RecoveryReport> RecoverAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Checks database integrity.
    /// </summary>
    Task<IntegrityResult> CheckIntegrityAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Forces WAL checkpoint.
    /// </summary>
    Task<int> CheckpointWalAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets last recovery report.
    /// </summary>
    RecoveryReport? LastReport { get; }
}

/// <summary>
/// Result of integrity check.
/// </summary>
public sealed record IntegrityResult
{
    public bool Passed { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
    public TimeSpan Duration { get; init; }
    public bool RepairAttempted { get; init; }
    public bool RepairSucceeded { get; init; }
}
```

### Recovery Report Model

```csharp
// Acode.Application/Services/TaskQueue/RecoveryReport.cs
namespace Acode.Application.Services.TaskQueue;

public sealed record RecoveryReport
{
    public required DateTimeOffset StartedAt { get; init; }
    public required DateTimeOffset CompletedAt { get; init; }
    public TimeSpan Duration => CompletedAt - StartedAt;
    
    public required bool IntegrityCheckPassed { get; init; }
    public int WalFramesCheckpointed { get; init; }
    
    public required IReadOnlyList<RecoveredTask> RecoveredTasks { get; init; }
    public required IReadOnlyList<DeadWorkerInfo> DeadWorkers { get; init; }
    
    public int OrphanedTasksFound => RecoveredTasks.Count;
    public int SuccessfulRecoveries => RecoveredTasks.Count(t => t.Success);
    public int FailedRecoveries => RecoveredTasks.Count(t => !t.Success);
    
    public string ToReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Queue Recovery Report ===");
        sb.AppendLine($"Started: {StartedAt:O}");
        sb.AppendLine($"Duration: {Duration.TotalSeconds:F1}s");
        sb.AppendLine();
        sb.AppendLine($"Integrity Check: {(IntegrityCheckPassed ? "PASSED" : "FAILED")}");
        sb.AppendLine($"WAL Checkpointed: {WalFramesCheckpointed} frames");
        sb.AppendLine();
        sb.AppendLine($"Orphaned Tasks Found: {OrphanedTasksFound}");
        foreach (var task in RecoveredTasks)
        {
            var status = task.Success ? task.Action.ToString().ToLower() : "FAILED";
            var attempt = task.Action == RecoveryAction.Fail 
                ? "(max attempts exceeded)" 
                : $"(attempt {task.AttemptCount}/{task.MaxAttempts})";
            sb.AppendLine($"  - {task.TaskId}: {status} {attempt}");
        }
        sb.AppendLine();
        sb.AppendLine($"Dead Workers: {DeadWorkers.Count}");
        foreach (var worker in DeadWorkers)
        {
            var ago = DateTimeOffset.UtcNow - worker.LastHeartbeat;
            sb.AppendLine($"  - {worker.WorkerId} (last heartbeat: {ago.TotalMinutes:F0}m ago)");
        }
        sb.AppendLine();
        sb.AppendLine($"Recovery Complete: {CompletedAt:O}");
        return sb.ToString();
    }
}

public sealed record RecoveredTask
{
    public required TaskId TaskId { get; init; }
    public required RecoveryAction Action { get; init; }
    public required int AttemptCount { get; init; }
    public required int MaxAttempts { get; init; }
    public required bool Success { get; init; }
    public string? Error { get; init; }
    public WorkerId? PreviousWorkerId { get; init; }
}

public enum RecoveryAction
{
    Retry,   // Reset to pending for retry
    Fail,    // Mark as failed (over limit)
    Pending  // Set to pending without incrementing attempt
}

public sealed record DeadWorkerInfo
{
    public required WorkerId WorkerId { get; init; }
    public required DateTimeOffset LastHeartbeat { get; init; }
    public required IReadOnlyList<TaskId> OrphanedTasks { get; init; }
}
```

### Queue Recovery Service

```csharp
// Acode.Application/Services/TaskQueue/QueueRecoveryService.cs
namespace Acode.Application.Services.TaskQueue;

public sealed class QueueRecoveryService : IQueueRecovery
{
    private readonly ITaskRepository _repository;
    private readonly IStateMachine _stateMachine;
    private readonly IWorkerHeartbeat _heartbeat;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<QueueRecoveryService> _logger;
    private readonly RecoveryOptions _options;
    
    private RecoveryReport? _lastReport;
    
    public RecoveryReport? LastReport => _lastReport;
    
    public QueueRecoveryService(
        ITaskRepository repository,
        IStateMachine stateMachine,
        IWorkerHeartbeat heartbeat,
        IDbConnectionFactory connectionFactory,
        IOptions<RecoveryOptions> options,
        ILogger<QueueRecoveryService> logger)
    {
        _repository = repository;
        _stateMachine = stateMachine;
        _heartbeat = heartbeat;
        _connectionFactory = connectionFactory;
        _options = options.Value;
        _logger = logger;
    }
    
    public async Task<RecoveryReport> RecoverAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting queue recovery");
        var startedAt = DateTimeOffset.UtcNow;
        
        var recoveredTasks = new List<RecoveredTask>();
        var deadWorkers = new List<DeadWorkerInfo>();
        
        // Step 1: Integrity check
        var integrity = await CheckIntegrityAsync(ct);
        if (!integrity.Passed && !integrity.RepairSucceeded)
        {
            _logger.LogCritical("Database integrity check failed and repair unsuccessful");
            throw new RecoveryFailedException("Database integrity check failed");
        }
        
        // Step 2: WAL checkpoint
        var walFrames = await CheckpointWalAsync(ct);
        _logger.LogInformation("Checkpointed {Frames} WAL frames", walFrames);
        
        // Step 3: Find orphaned tasks (Running status with no live worker)
        var orphanedTasks = await FindOrphanedTasksAsync(ct);
        _logger.LogInformation("Found {Count} orphaned tasks", orphanedTasks.Count);
        
        // Step 4: Recover each orphaned task
        foreach (var task in orphanedTasks)
        {
            var recovered = await RecoverTaskAsync(task, ct);
            recoveredTasks.Add(recovered);
        }
        
        // Step 5: Detect and handle stale workers
        var staleWorkers = await _heartbeat.DetectStaleWorkersAsync(ct);
        foreach (var staleWorker in staleWorkers)
        {
            var workerInfo = await HandleDeadWorkerAsync(staleWorker, ct);
            deadWorkers.Add(workerInfo);
        }
        
        var report = new RecoveryReport
        {
            StartedAt = startedAt,
            CompletedAt = DateTimeOffset.UtcNow,
            IntegrityCheckPassed = integrity.Passed,
            WalFramesCheckpointed = walFrames,
            RecoveredTasks = recoveredTasks,
            DeadWorkers = deadWorkers
        };
        
        _lastReport = report;
        
        _logger.LogInformation(
            "Recovery complete: {Orphans} orphaned tasks, {Recovered} recovered, {Dead} dead workers",
            report.OrphanedTasksFound, report.SuccessfulRecoveries, report.DeadWorkers.Count);
        
        return report;
    }
    
    public async Task<IntegrityResult> CheckIntegrityAsync(CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var errors = new List<string>();
        var warnings = new List<string>();
        var repairAttempted = false;
        var repairSucceeded = false;
        
        using var conn = await _connectionFactory.CreateAsync(ct);
        
        // Run SQLite integrity check
        var results = await conn.QueryAsync<string>("PRAGMA integrity_check");
        var resultList = results.ToList();
        
        if (resultList.Count == 1 && resultList[0] == "ok")
        {
            _logger.LogDebug("Integrity check passed");
        }
        else
        {
            errors.AddRange(resultList);
            _logger.LogWarning("Integrity check found issues: {Issues}", string.Join(", ", resultList));
            
            // Attempt repair
            repairAttempted = true;
            try
            {
                await conn.ExecuteAsync("REINDEX");
                _logger.LogInformation("REINDEX completed");
                
                // Re-check
                var recheck = await conn.QueryAsync<string>("PRAGMA integrity_check");
                repairSucceeded = recheck.Count() == 1 && recheck.First() == "ok";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Repair attempt failed");
            }
        }
        
        // Check WAL mode
        var journalMode = await conn.ExecuteScalarAsync<string>("PRAGMA journal_mode");
        if (journalMode != "wal")
        {
            warnings.Add($"Journal mode is {journalMode}, expected WAL");
        }
        
        return new IntegrityResult
        {
            Passed = errors.Count == 0 || repairSucceeded,
            Errors = errors,
            Warnings = warnings,
            Duration = sw.Elapsed,
            RepairAttempted = repairAttempted,
            RepairSucceeded = repairSucceeded
        };
    }
    
    public async Task<int> CheckpointWalAsync(CancellationToken ct = default)
    {
        using var conn = await _connectionFactory.CreateAsync(ct);
        
        // PRAGMA wal_checkpoint(TRUNCATE) returns: busy, log frames, checkpointed frames
        var result = await conn.QueryFirstAsync("PRAGMA wal_checkpoint(TRUNCATE)");
        var checkpointed = (int)result.checkpointed;
        
        _logger.LogDebug("WAL checkpoint: {Checkpointed} frames", checkpointed);
        return checkpointed;
    }
    
    private async Task<IReadOnlyList<QueuedTask>> FindOrphanedTasksAsync(CancellationToken ct)
    {
        // Find tasks that are Running but have no active heartbeat
        var runningTasks = await _repository.GetByStatusAsync(TaskStatus.Running, ct);
        var orphaned = new List<QueuedTask>();
        
        foreach (var task in runningTasks)
        {
            if (task.WorkerId == null)
            {
                // No worker assigned - definitely orphaned
                orphaned.Add(task);
            }
            else
            {
                // Check if worker is still alive
                var isAlive = await _heartbeat.IsWorkerAliveAsync(task.WorkerId.Value, ct);
                if (!isAlive)
                {
                    orphaned.Add(task);
                }
            }
        }
        
        return orphaned;
    }
    
    private async Task<RecoveredTask> RecoverTaskAsync(QueuedTask task, CancellationToken ct)
    {
        var action = DetermineRecoveryAction(task);
        var targetStatus = action switch
        {
            RecoveryAction.Retry => TaskStatus.Pending,
            RecoveryAction.Pending => TaskStatus.Pending,
            RecoveryAction.Fail => TaskStatus.Failed,
            _ => TaskStatus.Pending
        };
        
        var reason = action switch
        {
            RecoveryAction.Retry => "Recovered after crash - queued for retry",
            RecoveryAction.Pending => "Recovered after crash - reset to pending",
            RecoveryAction.Fail => "Recovered after crash - max attempts exceeded",
            _ => "Recovered after crash"
        };
        
        try
        {
            var result = await _stateMachine.TransitionAsync(
                task.Spec.Id,
                targetStatus,
                TransitionActor.System,
                reason,
                new TransitionContext
                {
                    Error = action == RecoveryAction.Fail ? "Worker crashed during execution" : null
                },
                ct);
            
            if (result.Success)
            {
                _logger.LogInformation(
                    "Task {TaskId} recovered: {Action} (attempt {Attempt}/{Max})",
                    task.Spec.Id, action, task.AttemptCount, task.Spec.RetryLimit);
            }
            
            return new RecoveredTask
            {
                TaskId = task.Spec.Id,
                Action = action,
                AttemptCount = task.AttemptCount,
                MaxAttempts = task.Spec.RetryLimit,
                Success = result.Success,
                Error = result.Error,
                PreviousWorkerId = task.WorkerId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recover task {TaskId}", task.Spec.Id);
            return new RecoveredTask
            {
                TaskId = task.Spec.Id,
                Action = action,
                AttemptCount = task.AttemptCount,
                MaxAttempts = task.Spec.RetryLimit,
                Success = false,
                Error = ex.Message,
                PreviousWorkerId = task.WorkerId
            };
        }
    }
    
    private RecoveryAction DetermineRecoveryAction(QueuedTask task)
    {
        // Check if over retry limit
        if (task.AttemptCount >= task.Spec.RetryLimit)
        {
            return RecoveryAction.Fail;
        }
        
        // Use configured default action
        return _options.DefaultAction;
    }
    
    private async Task<DeadWorkerInfo> HandleDeadWorkerAsync(StaleWorker staleWorker, CancellationToken ct)
    {
        _logger.LogWarning(
            "Worker {WorkerId} is dead (last heartbeat: {LastHeartbeat})",
            staleWorker.WorkerId, staleWorker.LastHeartbeat);
        
        // Recover all tasks assigned to this worker
        var orphanedTasks = new List<TaskId>();
        foreach (var taskId in staleWorker.TaskIds)
        {
            orphanedTasks.Add(taskId);
        }
        
        // Mark worker as dead
        await _heartbeat.MarkWorkerDeadAsync(staleWorker.WorkerId, ct);
        
        return new DeadWorkerInfo
        {
            WorkerId = staleWorker.WorkerId,
            LastHeartbeat = staleWorker.LastHeartbeat,
            OrphanedTasks = orphanedTasks
        };
    }
}

public sealed class RecoveryOptions
{
    public RecoveryAction DefaultAction { get; set; } = RecoveryAction.Retry;
    public int MaxRecoveryAttempts { get; set; } = 3;
    public bool IntegrityCheckOnStartup { get; set; } = true;
}

public class RecoveryFailedException : Exception
{
    public RecoveryFailedException(string message) : base(message) { }
}
```

### Worker Heartbeat Interface

```csharp
// Acode.Application/Services/TaskQueue/IWorkerHeartbeat.cs
namespace Acode.Application.Services.TaskQueue;

/// <summary>
/// Manages worker heartbeats and stale detection.
/// </summary>
public interface IWorkerHeartbeat
{
    /// <summary>
    /// Sends a heartbeat for a worker.
    /// </summary>
    Task SendHeartbeatAsync(WorkerId workerId, TaskId? currentTaskId, CancellationToken ct = default);
    
    /// <summary>
    /// Checks if a worker is alive.
    /// </summary>
    Task<bool> IsWorkerAliveAsync(WorkerId workerId, CancellationToken ct = default);
    
    /// <summary>
    /// Detects all stale workers.
    /// </summary>
    Task<IReadOnlyList<StaleWorker>> DetectStaleWorkersAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Marks a worker as dead.
    /// </summary>
    Task MarkWorkerDeadAsync(WorkerId workerId, CancellationToken ct = default);
    
    /// <summary>
    /// Gets heartbeat status for all workers.
    /// </summary>
    Task<IReadOnlyList<WorkerHeartbeatStatus>> GetAllStatusAsync(CancellationToken ct = default);
}

public sealed record StaleWorker
{
    public required WorkerId WorkerId { get; init; }
    public required DateTimeOffset LastHeartbeat { get; init; }
    public required IReadOnlyList<TaskId> TaskIds { get; init; }
}

public sealed record WorkerHeartbeatStatus
{
    public required WorkerId WorkerId { get; init; }
    public required DateTimeOffset LastHeartbeat { get; init; }
    public TaskId? CurrentTaskId { get; init; }
    public required bool IsAlive { get; init; }
    public TimeSpan Age => DateTimeOffset.UtcNow - LastHeartbeat;
}
```

### Worker Heartbeat Service

```csharp
// Acode.Application/Services/TaskQueue/WorkerHeartbeatService.cs
namespace Acode.Application.Services.TaskQueue;

public sealed class WorkerHeartbeatService : IWorkerHeartbeat
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<WorkerHeartbeatService> _logger;
    private readonly HeartbeatOptions _options;
    
    public WorkerHeartbeatService(
        IDbConnectionFactory connectionFactory,
        IOptions<HeartbeatOptions> options,
        ILogger<WorkerHeartbeatService> logger)
    {
        _connectionFactory = connectionFactory;
        _options = options.Value;
        _logger = logger;
    }
    
    public async Task SendHeartbeatAsync(WorkerId workerId, TaskId? currentTaskId, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO worker_heartbeats (worker_id, task_id, timestamp)
            VALUES (@WorkerId, @TaskId, @Timestamp)
            ON CONFLICT(worker_id) DO UPDATE SET
                task_id = @TaskId,
                timestamp = @Timestamp";
        
        using var conn = await _connectionFactory.CreateAsync(ct);
        await conn.ExecuteAsync(sql, new
        {
            WorkerId = workerId.Value,
            TaskId = currentTaskId?.Value,
            Timestamp = DateTimeOffset.UtcNow.ToString("O")
        });
    }
    
    public async Task<bool> IsWorkerAliveAsync(WorkerId workerId, CancellationToken ct = default)
    {
        var threshold = DateTimeOffset.UtcNow.AddSeconds(
            -_options.IntervalSeconds * _options.StaleThresholdMultiplier);
        
        const string sql = @"
            SELECT COUNT(*) FROM worker_heartbeats
            WHERE worker_id = @WorkerId AND timestamp >= @Threshold";
        
        using var conn = await _connectionFactory.CreateAsync(ct);
        var count = await conn.ExecuteScalarAsync<int>(sql, new
        {
            WorkerId = workerId.Value,
            Threshold = threshold.ToString("O")
        });
        
        return count > 0;
    }
    
    public async Task<IReadOnlyList<StaleWorker>> DetectStaleWorkersAsync(CancellationToken ct = default)
    {
        var threshold = DateTimeOffset.UtcNow.AddSeconds(
            -_options.IntervalSeconds * _options.StaleThresholdMultiplier);
        
        // Find workers with old heartbeats that still have running tasks
        const string sql = @"
            SELECT 
                h.worker_id,
                h.timestamp as last_heartbeat,
                GROUP_CONCAT(t.id) as task_ids
            FROM worker_heartbeats h
            LEFT JOIN tasks t ON t.worker_id = h.worker_id AND t.status = 'running'
            WHERE h.timestamp < @Threshold
            GROUP BY h.worker_id
            HAVING COUNT(t.id) > 0";
        
        using var conn = await _connectionFactory.CreateAsync(ct);
        var rows = await conn.QueryAsync(sql, new { Threshold = threshold.ToString("O") });
        
        return rows.Select(r => new StaleWorker
        {
            WorkerId = new WorkerId((string)r.worker_id),
            LastHeartbeat = DateTimeOffset.Parse((string)r.last_heartbeat),
            TaskIds = ((string?)r.task_ids)?.Split(',').Select(id => new TaskId(id)).ToList() 
                ?? new List<TaskId>()
        }).ToList();
    }
    
    public async Task MarkWorkerDeadAsync(WorkerId workerId, CancellationToken ct = default)
    {
        _logger.LogWarning("Marking worker {WorkerId} as dead", workerId);
        
        const string sql = "DELETE FROM worker_heartbeats WHERE worker_id = @WorkerId";
        
        using var conn = await _connectionFactory.CreateAsync(ct);
        await conn.ExecuteAsync(sql, new { WorkerId = workerId.Value });
    }
    
    public async Task<IReadOnlyList<WorkerHeartbeatStatus>> GetAllStatusAsync(CancellationToken ct = default)
    {
        var threshold = DateTimeOffset.UtcNow.AddSeconds(
            -_options.IntervalSeconds * _options.StaleThresholdMultiplier);
        
        const string sql = @"
            SELECT worker_id, task_id, timestamp
            FROM worker_heartbeats
            ORDER BY timestamp DESC";
        
        using var conn = await _connectionFactory.CreateAsync(ct);
        var rows = await conn.QueryAsync(sql);
        
        return rows.Select(r =>
        {
            var heartbeat = DateTimeOffset.Parse((string)r.timestamp);
            return new WorkerHeartbeatStatus
            {
                WorkerId = new WorkerId((string)r.worker_id),
                LastHeartbeat = heartbeat,
                CurrentTaskId = r.task_id != null ? new TaskId((string)r.task_id) : null,
                IsAlive = heartbeat >= threshold
            };
        }).ToList();
    }
}

public sealed class HeartbeatOptions
{
    public int IntervalSeconds { get; set; } = 30;
    public int StaleThresholdMultiplier { get; set; } = 3;
    public int DetectionIntervalSeconds { get; set; } = 60;
}
```

### Heartbeat Table DDL

```sql
-- Worker heartbeat tracking table
CREATE TABLE IF NOT EXISTS worker_heartbeats (
    worker_id TEXT PRIMARY KEY,
    task_id TEXT,
    timestamp TEXT NOT NULL,
    
    CONSTRAINT fk_task FOREIGN KEY (task_id) REFERENCES tasks(id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_heartbeats_timestamp 
    ON worker_heartbeats(timestamp);
```

### Implementation Checklist

- [ ] Define `IQueueRecovery` interface
- [ ] Implement `QueueRecoveryService`
- [ ] Add SQLite integrity check
- [ ] Add WAL checkpoint logic
- [ ] Implement orphaned task detection
- [ ] Implement task recovery with state machine
- [ ] Add recovery action determination
- [ ] Create `RecoveryReport` model
- [ ] Add report formatting
- [ ] Define `IWorkerHeartbeat` interface
- [ ] Implement `WorkerHeartbeatService`
- [ ] Add heartbeat upsert logic
- [ ] Implement stale worker detection
- [ ] Add worker death handling
- [ ] Create `worker_heartbeats` table
- [ ] Configure recovery options
- [ ] Configure heartbeat options
- [ ] Register in DI
- [ ] Add startup recovery hook
- [ ] Write unit tests
- [ ] Write integration tests

### Rollout Plan

1. **Phase 1: Integrity** (Day 1)
   - SQLite integrity check
   - WAL checkpoint
   - Repair logic

2. **Phase 2: Recovery** (Day 2)
   - Orphan detection
   - Recovery actions
   - State transitions

3. **Phase 3: Heartbeat** (Day 2)
   - Heartbeat table
   - Send/receive logic
   - Stale detection

4. **Phase 4: Integration** (Day 3)
   - Startup hook
   - DI registration
   - Report generation

5. **Phase 5: Testing** (Day 3)
   - Crash simulation
   - Recovery verification
   - Performance testing

---

**End of Task 026.c Specification**