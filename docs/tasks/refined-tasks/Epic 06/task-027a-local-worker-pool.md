# Task 027.a: Local Worker Pool

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 027 (Worker Pool), Task 005 (Worktrees)  

---

## Description

Task 027.a implements local process-based workers. Each worker runs as a separate process on the local machine. Workers MUST be isolated using git worktrees.

Local workers MUST execute .NET code directly. Each worker MUST have its own working directory. Workers MUST share the same database connection for queue access.

Process management MUST be robust. Workers MUST start and stop cleanly. Crashed workers MUST be detected and restarted. Orphaned processes MUST be cleaned up.

### Business Value

Local workers enable:
- Fast execution (no container overhead)
- Simple deployment
- Direct debugging
- Low resource usage
- Works everywhere

### Scope Boundaries

This task covers local workers only. Docker workers are in Task 027.b. Pool management is in Task 027. Log multiplexing is in Task 027.c.

### Integration Points

- Task 027: Pool provides lifecycle
- Task 005: Worktrees for isolation
- Task 026: Queue for task claim
- Task 026.c: Heartbeat integration

### Failure Modes

- Process crash → Auto-restart
- Resource exhaustion → Backpressure
- Worktree conflict → Retry with new tree
- Cleanup failure → Log and continue

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Process Worker | Worker as OS process |
| Worktree | Isolated git checkout |
| Process ID | OS process identifier |
| Exit Code | Process termination status |
| Spawn | Create new process |
| Kill | Terminate process |
| Orphan | Process without parent |

---

## Out of Scope

- Remote process execution
- SSH worker execution
- Service account isolation
- Process sandboxing
- AppDomain isolation

---

## Functional Requirements

### FR-001 to FR-030: Process Management

- FR-001: Worker MUST run as process
- FR-002: Process MUST be spawned by pool
- FR-003: Spawn MUST pass worker ID
- FR-004: Spawn MUST pass config path
- FR-005: Process MUST inherit environment
- FR-006: Sensitive env MUST be filtered
- FR-007: Process MUST have working dir
- FR-008: Working dir MUST be worktree
- FR-009: Worktree MUST be created on start
- FR-010: Worktree MUST be unique per worker
- FR-011: Process MUST exit cleanly on stop
- FR-012: Clean exit MUST return 0
- FR-013: Error exit MUST return non-zero
- FR-014: Exit codes MUST be documented
- FR-015: Supervisor MUST track processes
- FR-016: Supervisor MUST detect exits
- FR-017: Unexpected exit MUST be logged
- FR-018: Crash MUST trigger recovery
- FR-019: Recovery MUST restart worker
- FR-020: Restart MUST have delay
- FR-021: Delay MUST be exponential
- FR-022: Max delay MUST be 60 seconds
- FR-023: Restart count MUST be tracked
- FR-024: Max restarts MUST be enforced
- FR-025: Over-limit MUST fail worker
- FR-026: Failed worker MUST be replaced
- FR-027: Kill MUST use SIGTERM first
- FR-028: SIGTERM timeout MUST be 10s
- FR-029: SIGKILL MUST follow timeout
- FR-030: Kill MUST be logged

### FR-031 to FR-055: Execution Environment

- FR-031: Worker MUST have isolated worktree
- FR-032: Worktree MUST be task-specific
- FR-033: Task worktree MUST be created
- FR-034: Task worktree MUST be cleaned after
- FR-035: Cleanup MUST be optional (for debug)
- FR-036: `--keep-worktrees` MUST preserve
- FR-037: Worker MUST have temp directory
- FR-038: Temp MUST be worker-specific
- FR-039: Temp MUST be cleaned on stop
- FR-040: Worker MUST have log file
- FR-041: Log MUST be worker-specific
- FR-042: Log MUST be rotated
- FR-043: Log MUST be accessible
- FR-044: Worker MUST set task env vars
- FR-045: ACODE_TASK_ID MUST be set
- FR-046: ACODE_WORKER_ID MUST be set
- FR-047: ACODE_WORKTREE_PATH MUST be set
- FR-048: Worker MUST have access to tools
- FR-049: dotnet MUST be accessible
- FR-050: git MUST be accessible
- FR-051: PATH MUST include tools
- FR-052: Worker MUST validate tools
- FR-053: Missing tool MUST fail start
- FR-054: Tool version MUST be logged
- FR-055: Tool errors MUST be clear

### FR-056 to FR-070: Task Execution

- FR-056: Worker MUST execute task
- FR-057: Execution MUST be in worktree
- FR-058: Build MUST run if needed
- FR-059: Tests MUST run if configured
- FR-060: Timeout MUST be enforced
- FR-061: Timeout MUST kill process tree
- FR-062: Output MUST be captured
- FR-063: Stdout MUST be logged
- FR-064: Stderr MUST be logged
- FR-065: Exit code MUST be captured
- FR-066: Artifacts MUST be collected
- FR-067: Results MUST be reported
- FR-068: Success MUST complete task
- FR-069: Failure MUST fail task
- FR-070: Cleanup MUST always run

---

## Non-Functional Requirements

- NFR-001: Process spawn MUST be <500ms
- NFR-002: Process kill MUST be <15s
- NFR-003: Worktree create MUST be <5s
- NFR-004: Cleanup MUST be <5s
- NFR-005: Memory overhead MUST be <100MB
- NFR-006: No zombie processes
- NFR-007: No orphaned worktrees
- NFR-008: Graceful on system shutdown
- NFR-009: Works on Windows/Linux/macOS
- NFR-010: No elevated privileges needed

---

## User Manual Documentation

### Configuration

```yaml
workers:
  mode: process
  count: 4
  
  process:
    restartDelayMs: 1000
    maxRestartDelayMs: 60000
    maxRestarts: 10
    killTimeoutSeconds: 10
    keepWorktrees: false
    
  worktree:
    baseDir: ".agent/worktrees"
    cleanupOnExit: true
```

### Environment Variables

| Variable | Description |
|----------|-------------|
| ACODE_TASK_ID | Current task identifier |
| ACODE_WORKER_ID | Worker identifier |
| ACODE_WORKTREE_PATH | Path to task worktree |
| ACODE_CONFIG_PATH | Path to config file |

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Clean exit |
| 1 | Task failed |
| 2 | Configuration error |
| 3 | Tool not found |
| 137 | Killed (SIGKILL) |
| 143 | Terminated (SIGTERM) |

### Debugging

```bash
# Start with worktree preservation
acode worker start --keep-worktrees

# View worker logs
acode worker logs worker-abc123

# Attach to worker process
acode worker attach worker-abc123
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Process spawns correctly
- [ ] AC-002: Process stops cleanly
- [ ] AC-003: Crash restarts worker
- [ ] AC-004: Worktree created
- [ ] AC-005: Worktree cleaned
- [ ] AC-006: Environment vars set
- [ ] AC-007: Task executes
- [ ] AC-008: Output captured
- [ ] AC-009: Exit code captured
- [ ] AC-010: Timeout enforced
- [ ] AC-011: No zombies
- [ ] AC-012: Cross-platform works

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Process spawn
- [ ] UT-002: Process kill
- [ ] UT-003: Restart logic
- [ ] UT-004: Worktree management
- [ ] UT-005: Environment setup

### Integration Tests

- [ ] IT-001: Full execution cycle
- [ ] IT-002: Crash recovery
- [ ] IT-003: Timeout handling
- [ ] IT-004: Cleanup verification

---

## Implementation Prompt

### Interface

```csharp
public interface ILocalWorker
{
    string Id { get; }
    int? ProcessId { get; }
    WorkerStatus Status { get; }
    
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(bool force = false, 
        CancellationToken ct = default);
    Task<TaskResult> ExecuteAsync(QueuedTask task, 
        CancellationToken ct = default);
}

public interface IProcessSupervisor
{
    Task<int> SpawnAsync(string executable, 
        string[] args,
        string workingDir,
        IDictionary<string, string> env,
        CancellationToken ct = default);
        
    Task KillAsync(int pid, 
        TimeSpan timeout,
        CancellationToken ct = default);
        
    bool IsRunning(int pid);
}

public interface IWorkerWorktreeManager
{
    Task<string> CreateWorktreeAsync(string workerId, 
        string? branch = null,
        CancellationToken ct = default);
        
    Task RemoveWorktreeAsync(string path, 
        CancellationToken ct = default);
        
    Task CleanupOrphanedAsync(
        CancellationToken ct = default);
}
```

---

**End of Task 027.a Specification**