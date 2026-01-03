# EPIC 6 — Task Queue + Parallel Worker System

**Priority:** P0 – Critical  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Epic 02 (Interfaces), Epic 03 (Planner), Epic 04 (Audit)  

---

## Epic Overview

Epic 6 implements the task queue and parallel worker system. Tasks flow from planner to queue to worker to completion. The queue provides persistence, state transitions, and crash recovery. Workers execute tasks in parallel with proper isolation.

This epic MUST provide durable task storage, parallel execution, and merge coordination. The queue MUST survive crashes. Workers MUST support local and Docker isolation. Parallel work MUST merge safely.

### Purpose

Acode executes tasks from a persistent queue. Multiple workers MAY process tasks concurrently. The system MUST handle failures gracefully and prevent conflicts when parallel tasks touch the same files.

### Boundaries

This epic covers:
- Task specification format (YAML/JSON schema)
- Queue persistence (SQLite-backed)
- State transitions and invariants
- Worker pools (local and Docker)
- Parallel execution with isolation
- Merge coordination for parallel work

This epic does NOT cover:
- Task decomposition (Epic 03)
- Git operations (Epic 05)
- File mutation safety (Epic 07)

### Dependencies

| Dependency | Purpose |
|------------|---------|
| Epic 02 | Core interfaces and DI |
| Epic 03 | Planner produces tasks |
| Epic 04 | Audit logging for state changes |
| Epic 05 | Git worktrees for isolation |

---

## Outcomes

1. Task specs MUST have validated YAML/JSON schema
2. CLI MUST support add/list/show/retry/cancel commands
3. Queue MUST persist to SQLite
4. State transitions MUST be atomic
5. Invalid transitions MUST be rejected
6. Crash recovery MUST restore pending tasks
7. Local worker pool MUST execute tasks
8. Docker worker pool MUST provide isolation
9. Workers MUST run in parallel
10. Logs MUST be multiplexed correctly
11. Dashboard MUST show worker status
12. Conflict detection MUST identify overlaps
13. Dependency hints MUST order execution
14. Merge MUST handle parallel changes
15. All state changes MUST be logged
16. Queue MUST handle backpressure
17. Task priority MUST affect ordering
18. Retry logic MUST respect limits
19. Cancellation MUST be graceful
20. Metrics MUST track throughput

---

## Non-Goals

1. Distributed queue across machines
2. Remote worker nodes
3. GPU scheduling
4. Real-time streaming of results
5. Task scheduling by time
6. Cron-style recurring tasks
7. Multi-tenant queue isolation
8. Cross-repository coordination
9. Cloud queue services integration
10. Task migration between queues
11. Hot-reloading worker code
12. Dynamic worker scaling
13. Task templating system
14. Visual task designer
15. Inter-process communication beyond files

---

## Architecture & Integration Points

### Core Interfaces

```
ITaskSpecParser
├── ParseAsync(content) → TaskSpec
├── ValidateAsync(spec) → ValidationResult
└── SerializeAsync(spec) → string

ITaskQueue
├── EnqueueAsync(spec) → TaskId
├── DequeueAsync() → QueuedTask?
├── GetStatusAsync(id) → TaskStatus
├── RetryAsync(id) → Result
├── CancelAsync(id) → Result
└── ListAsync(filter) → IEnumerable<QueuedTask>

IWorkerPool
├── StartAsync(config) → void
├── StopAsync() → void
├── GetWorkersAsync() → IEnumerable<WorkerInfo>
└── ExecuteAsync(task) → TaskResult

IMergeCoordinator
├── AnalyzeAsync(changes[]) → MergePlan
├── DetectConflictsAsync(a, b) → Conflicts
├── MergeAsync(plan) → MergeResult
└── ValidateMergeAsync(result) → bool
```

### Events

| Event | Trigger |
|-------|---------|
| TaskEnqueued | Task added to queue |
| TaskDequeued | Worker claimed task |
| TaskStarted | Execution began |
| TaskCompleted | Successful completion |
| TaskFailed | Execution failed |
| TaskRetried | Retry initiated |
| TaskCancelled | Cancellation completed |
| WorkerStarted | Worker came online |
| WorkerStopped | Worker went offline |
| MergeCompleted | Parallel merge done |
| ConflictDetected | Files overlap |

### Data Contracts

```
TaskSpec
├── id: string (ULID)
├── title: string
├── description: string
├── priority: int (1-5)
├── dependencies: string[]
├── files: string[]
├── tags: string[]
└── metadata: Dictionary

QueuedTask
├── taskId: string
├── spec: TaskSpec
├── status: TaskStatus
├── enqueuedAt: DateTime
├── startedAt: DateTime?
├── completedAt: DateTime?
├── workerId: string?
├── attemptCount: int
└── lastError: string?

TaskStatus
├── Pending
├── Running
├── Completed
├── Failed
├── Cancelled
└── Blocked
```

---

## Operational Considerations

### Mode Compliance

| Mode | Queue | Workers |
|------|-------|---------|
| local-only | Full | Local only |
| burst | Full | All |
| airgapped | Full | Local only |

### Safety

- Worker isolation MUST prevent cross-task interference
- Failed tasks MUST NOT corrupt queue state
- Parallel execution MUST detect conflicts before merge
- Docker workers MUST have resource limits

### Audit

- All queue operations MUST be logged
- State transitions MUST capture before/after
- Worker assignments MUST be tracked
- Merge decisions MUST be recorded

---

## Acceptance Criteria / Definition of Done

### Task Spec Format (Task 025)
- [ ] AC-001: YAML schema defined
- [ ] AC-002: JSON schema defined
- [ ] AC-003: Schema validation works
- [ ] AC-004: Required fields enforced
- [ ] AC-005: Optional fields handled
- [ ] AC-006: Defaults applied
- [ ] AC-007: CLI add command works
- [ ] AC-008: CLI list command works
- [ ] AC-009: CLI show command works
- [ ] AC-010: CLI retry command works
- [ ] AC-011: CLI cancel command works
- [ ] AC-012: Human-readable errors shown
- [ ] AC-013: Error codes documented
- [ ] AC-014: Examples provided

### Queue Persistence (Task 026)
- [ ] AC-015: SQLite schema created
- [ ] AC-016: Tasks persist across restarts
- [ ] AC-017: State transitions atomic
- [ ] AC-018: Invalid transitions rejected
- [ ] AC-019: Transition history logged
- [ ] AC-020: Crash recovery works
- [ ] AC-021: Orphan tasks detected
- [ ] AC-022: Pending tasks resumed
- [ ] AC-023: Failed tasks queryable
- [ ] AC-024: Queue metrics available
- [ ] AC-025: Backup/restore works

### Worker Pool (Task 027)
- [ ] AC-026: Local workers created
- [ ] AC-027: Worker count configurable
- [ ] AC-028: Workers claim tasks
- [ ] AC-029: Workers report status
- [ ] AC-030: Docker workers created
- [ ] AC-031: Docker isolation enforced
- [ ] AC-032: Resource limits applied
- [ ] AC-033: Logs multiplexed correctly
- [ ] AC-034: Dashboard shows status
- [ ] AC-035: Workers shut down gracefully

### Parallel Safety (Task 028)
- [ ] AC-036: Conflict detection works
- [ ] AC-037: File overlap detected
- [ ] AC-038: Dependencies ordered
- [ ] AC-039: Parallel execution safe
- [ ] AC-040: Merge plan generated
- [ ] AC-041: Merge executed correctly
- [ ] AC-042: Conflicts reported
- [ ] AC-043: Integration tests pass
- [ ] AC-044: Rollback available

### Cross-Cutting
- [ ] AC-045: All operations logged
- [ ] AC-046: Metrics exported
- [ ] AC-047: Error handling complete
- [ ] AC-048: Documentation complete
- [ ] AC-049: Unit test coverage >80%
- [ ] AC-050: Integration tests pass

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Queue corruption | High | WAL mode, checksums |
| Worker deadlock | High | Timeout + kill |
| Merge conflicts | Medium | Pre-execution analysis |
| Resource exhaustion | Medium | Limits + backpressure |
| Docker unavailable | Medium | Fallback to local |
| Lost tasks | High | Transaction + recovery |
| Slow workers | Medium | Timeout + reassign |
| Log interleaving | Low | Structured multiplexing |
| Priority inversion | Medium | Fair scheduling |
| State divergence | High | Single source of truth |
| Network partition | Medium | Local queue only |
| Disk full | High | Space monitoring |

---

## Milestone Plan

### Milestone 1: Task Spec Format (Week 1)
- Task 025: Schema definition
- Task 025.a: YAML/JSON schema
- Task 025.b: CLI commands
- Task 025.c: Error handling

### Milestone 2: Queue Persistence (Week 2)
- Task 026: Queue core
- Task 026.a: SQLite schema
- Task 026.b: State transitions
- Task 026.c: Crash recovery

### Milestone 3: Worker Pool (Week 3)
- Task 027: Pool core
- Task 027.a: Local workers
- Task 027.b: Docker workers
- Task 027.c: Log dashboard

### Milestone 4: Parallel Safety (Week 4)
- Task 028: Merge coordinator
- Task 028.a: Conflict heuristics
- Task 028.b: Dependency graph
- Task 028.c: Integration tests

---

## Definition of Epic Complete

- [ ] All task specs validated
- [ ] Queue persists reliably
- [ ] State transitions are atomic
- [ ] Crash recovery tested
- [ ] Local workers functional
- [ ] Docker workers functional
- [ ] Logs multiplexed correctly
- [ ] Dashboard operational
- [ ] Conflicts detected
- [ ] Dependencies ordered
- [ ] Merge works correctly
- [ ] All CLI commands work
- [ ] Error messages clear
- [ ] Documentation complete
- [ ] Unit tests pass (>80%)
- [ ] Integration tests pass
- [ ] Performance acceptable
- [ ] Mode compliance verified
- [ ] Audit logging complete
- [ ] Security review passed

---

**END OF EPIC 6**