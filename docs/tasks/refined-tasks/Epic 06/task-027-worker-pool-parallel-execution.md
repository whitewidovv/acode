# Task 027: Worker Pool + Parallel Execution

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 026 (Queue), Task 005 (Git Worktrees), Task 001 (Modes)  

---

## Description

Task 027 implements the worker pool for parallel task execution. Workers claim tasks from the queue, execute them in isolation, and report results. Multiple workers MUST run concurrently.

The worker pool MUST be configurable. Worker count, isolation mode, and resource limits MUST be adjustable. Workers MUST support both local process and Docker container isolation.

Workers MUST be managed as a pool. Starting and stopping MUST be graceful. Workers MUST heartbeat to prove liveness. Failed workers MUST have tasks recovered.

### Business Value

Worker pools enable:
- Parallel task execution
- Faster completion times
- Resource utilization
- Isolation between tasks
- Scalable execution

### Scope Boundaries

This task covers pool management. Local workers are in Task 027.a. Docker workers are in Task 027.b. Log multiplexing is in Task 027.c.

### Integration Points

- Task 026: Queue provides tasks
- Task 005: Git worktrees for isolation
- Task 001: Mode affects worker types
- Task 026.c: Heartbeat and recovery

### Failure Modes

- Worker crash → Task recovered
- Resource exhaustion → Backpressure
- Docker unavailable → Fallback to local
- All workers busy → Queue backlog

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Worker | Process executing tasks |
| Pool | Collection of workers |
| Claim | Worker taking task ownership |
| Isolation | Separation between executions |
| Heartbeat | Liveness signal |
| Backpressure | Slowing on overload |
| Graceful | Clean shutdown |
| Supervisor | Worker manager |
| Executor | Task runner within worker |

---

## Out of Scope

- Remote workers
- Cloud worker scaling
- GPU workers
- Worker authentication
- Cross-machine pools
- Container orchestration

---

## Functional Requirements

### FR-001 to FR-030: Pool Management

- FR-001: `IWorkerPool` interface MUST be defined
- FR-002: `StartAsync` MUST launch workers
- FR-003: `StopAsync` MUST stop workers
- FR-004: Stop MUST be graceful by default
- FR-005: `--force` stop MUST kill immediately
- FR-006: Worker count MUST be configurable
- FR-007: Default count MUST be CPU count
- FR-008: Max count MUST be enforced
- FR-009: Min count MUST be 1
- FR-010: Workers MUST have unique IDs
- FR-011: Worker ID MUST be ULID
- FR-012: Workers MUST register on start
- FR-013: Workers MUST deregister on stop
- FR-014: Pool MUST track active workers
- FR-015: Pool MUST track worker status
- FR-016: Status: starting, idle, busy, stopping
- FR-017: Pool MUST support dynamic scaling
- FR-018: `ScaleAsync(count)` MUST adjust
- FR-019: Scale up MUST add workers
- FR-020: Scale down MUST stop idle first
- FR-021: Busy workers MUST finish current
- FR-022: Pool MUST emit events
- FR-023: Events: WorkerStarted, Stopped, Failed
- FR-024: Pool metrics MUST be available
- FR-025: Metrics: active, idle, busy counts
- FR-026: Pool MUST support isolation modes
- FR-027: Modes: process, docker
- FR-028: Default mode MUST be process
- FR-029: Mode MUST be configurable
- FR-030: Mixed modes MUST be supported

### FR-031 to FR-055: Worker Lifecycle

- FR-031: Worker MUST poll for tasks
- FR-032: Poll interval MUST be configurable
- FR-033: Default interval MUST be 1 second
- FR-034: Worker MUST claim task atomically
- FR-035: Claim MUST prevent double-claim
- FR-036: Worker MUST execute task
- FR-037: Execution MUST be isolated
- FR-038: Worker MUST report start
- FR-039: Worker MUST report completion
- FR-040: Worker MUST report failure
- FR-041: Worker MUST heartbeat during execution
- FR-042: Heartbeat MUST include task ID
- FR-043: Worker MUST respect timeout
- FR-044: Timeout MUST cancel execution
- FR-045: Worker MUST handle cancellation
- FR-046: Cancelled task MUST be marked
- FR-047: Worker MUST cleanup after task
- FR-048: Cleanup MUST release resources
- FR-049: Worker MUST loop for next task
- FR-050: Idle worker MUST reduce polling
- FR-051: Idle backoff MUST be exponential
- FR-052: Max idle backoff MUST be 30s
- FR-053: Task claim MUST reset backoff
- FR-054: Worker shutdown MUST drain queue
- FR-055: Drain timeout MUST be configurable

### FR-056 to FR-075: Resource Management

- FR-056: Worker MUST have resource limits
- FR-057: CPU limit MUST be supported
- FR-058: Memory limit MUST be supported
- FR-059: Disk limit MUST be supported
- FR-060: Limits MUST be configurable
- FR-061: Default limits MUST be reasonable
- FR-062: Resource usage MUST be tracked
- FR-063: Over-limit MUST trigger action
- FR-064: Actions: warn, throttle, kill
- FR-065: Default action MUST be warn
- FR-066: Resource metrics MUST be exported
- FR-067: Pool MUST respect system limits
- FR-068: Pool MUST not exhaust resources
- FR-069: Backpressure MUST apply when needed
- FR-070: Backpressure MUST pause new tasks
- FR-071: Backpressure MUST log
- FR-072: Recovery MUST resume tasks
- FR-073: Temp files MUST be cleaned
- FR-074: Worktrees MUST be cleaned
- FR-075: Cleanup MUST be configurable

---

## Non-Functional Requirements

- NFR-001: Worker start MUST complete in <1s
- NFR-002: Worker stop MUST complete in <10s
- NFR-003: Task claim MUST be <50ms
- NFR-004: Heartbeat MUST be <10ms
- NFR-005: Pool MUST handle 100 workers
- NFR-006: Pool MUST handle 10k tasks
- NFR-007: Memory per worker MUST be bounded
- NFR-008: CPU per worker MUST be bounded
- NFR-009: Graceful degradation required
- NFR-010: No resource leaks

---

## User Manual Documentation

### Configuration

```yaml
workers:
  count: 4
  mode: process  # process, docker
  pollIntervalMs: 1000
  idleBackoffMaxMs: 30000
  drainTimeoutSeconds: 60
  
  limits:
    cpuPercent: 25
    memoryMb: 512
    diskMb: 1024
    
  resources:
    overLimitAction: warn  # warn, throttle, kill
```

### CLI Commands

```bash
# Start worker pool
acode worker start

# Start with specific count
acode worker start --count 8

# Start with Docker isolation
acode worker start --mode docker

# Check pool status
acode worker status

# Scale workers
acode worker scale 6

# Stop workers gracefully
acode worker stop

# Force stop
acode worker stop --force

# List workers
acode worker list
```

### Worker Status

```
Worker Pool Status
==================
Mode: process
Active: 4
  - worker-abc123: busy (task xyz789)
  - worker-def456: idle
  - worker-ghi012: busy (task uvw345)
  - worker-jkl678: starting

Queue: 12 pending, 2 running
Resources: CPU 45%, Memory 1.2GB
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Pool starts workers
- [ ] AC-002: Pool stops workers
- [ ] AC-003: Worker claims tasks
- [ ] AC-004: Worker executes tasks
- [ ] AC-005: Worker reports results
- [ ] AC-006: Heartbeat works
- [ ] AC-007: Timeout enforced
- [ ] AC-008: Scaling works
- [ ] AC-009: Resource limits work
- [ ] AC-010: Backpressure works
- [ ] AC-011: Cleanup works
- [ ] AC-012: Events emitted
- [ ] AC-013: Metrics tracked
- [ ] AC-014: CLI commands work
- [ ] AC-015: Documentation complete

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Pool start/stop
- [ ] UT-002: Worker lifecycle
- [ ] UT-003: Task claiming
- [ ] UT-004: Timeout handling
- [ ] UT-005: Scaling logic

### Integration Tests

- [ ] IT-001: Full execution cycle
- [ ] IT-002: Parallel execution
- [ ] IT-003: Crash recovery
- [ ] IT-004: Resource limits

---

## Implementation Prompt

### Interface

```csharp
public interface IWorkerPool
{
    Task StartAsync(WorkerPoolOptions options, 
        CancellationToken ct = default);
        
    Task StopAsync(bool force = false, 
        CancellationToken ct = default);
        
    Task ScaleAsync(int targetCount, 
        CancellationToken ct = default);
        
    Task<IReadOnlyList<WorkerInfo>> GetWorkersAsync(
        CancellationToken ct = default);
        
    Task<PoolStatus> GetStatusAsync(
        CancellationToken ct = default);
}

public record WorkerPoolOptions(
    int WorkerCount,
    WorkerMode Mode,
    TimeSpan PollInterval,
    TimeSpan DrainTimeout,
    ResourceLimits Limits);

public enum WorkerMode { Process, Docker }

public record WorkerInfo(
    string Id,
    WorkerStatus Status,
    string? CurrentTaskId,
    DateTimeOffset StartedAt,
    DateTimeOffset? LastHeartbeat);

public enum WorkerStatus { Starting, Idle, Busy, Stopping }

public record PoolStatus(
    int ActiveCount,
    int IdleCount,
    int BusyCount,
    int PendingTasks,
    ResourceUsage Resources);
```

---

**End of Task 027 Specification**