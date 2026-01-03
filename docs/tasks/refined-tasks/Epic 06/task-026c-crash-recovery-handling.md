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

### Interface

```csharp
public interface IQueueRecovery
{
    Task<RecoveryReport> RecoverAsync(
        CancellationToken ct = default);
        
    Task<IntegrityResult> CheckIntegrityAsync(
        CancellationToken ct = default);
}

public interface IWorkerHeartbeat
{
    Task SendHeartbeatAsync(
        string workerId,
        string? taskId,
        CancellationToken ct = default);
        
    Task<IReadOnlyList<StaleWorker>> DetectStaleWorkersAsync(
        CancellationToken ct = default);
}

public record RecoveryReport(
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    TimeSpan Duration,
    bool IntegrityCheckPassed,
    int WalFramesCheckpointed,
    IReadOnlyList<RecoveredTask> RecoveredTasks,
    IReadOnlyList<string> DeadWorkers);

public record RecoveredTask(
    string TaskId,
    RecoveryAction Action,
    int AttemptCount,
    bool Success,
    string? Error);

public enum RecoveryAction { Retry, Fail, Pending }

public record StaleWorker(
    string WorkerId,
    DateTimeOffset LastHeartbeat,
    IReadOnlyList<string> TaskIds);
```

### Recovery Algorithm

```csharp
public async Task<RecoveryReport> RecoverAsync(CancellationToken ct)
{
    var report = new RecoveryReportBuilder();
    report.StartedAt = DateTimeOffset.UtcNow;
    
    // 1. Integrity check
    var integrity = await CheckIntegrityAsync(ct);
    report.IntegrityCheckPassed = integrity.Passed;
    
    // 2. WAL checkpoint
    var walFrames = await CheckpointWalAsync(ct);
    report.WalFramesCheckpointed = walFrames;
    
    // 3. Find orphaned tasks
    var orphans = await FindOrphanedTasksAsync(ct);
    
    // 4. Recover each task
    foreach (var task in orphans)
    {
        var recovered = await RecoverTaskAsync(task, ct);
        report.AddRecoveredTask(recovered);
    }
    
    // 5. Find dead workers
    var staleWorkers = await DetectStaleWorkersAsync(ct);
    foreach (var worker in staleWorkers)
    {
        await HandleDeadWorkerAsync(worker, ct);
        report.AddDeadWorker(worker.WorkerId);
    }
    
    report.CompletedAt = DateTimeOffset.UtcNow;
    return report.Build();
}
```

---

**End of Task 026.c Specification**