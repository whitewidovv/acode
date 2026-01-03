# Task 023.b: persist worktree ↔ task mapping

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 3 (Fibonacci points)  
**Phase:** Phase 5 – Git Integration Layer  
**Dependencies:** Task 023 (Worktree-per-Task), Task 011 (Workspace DB)  

---

## Description

Task 023.b implements persistence for the worktree-to-task mapping. When a worktree is created for a task, the association MUST be stored in the Workspace DB. This enables recovery and lookup.

The mapping MUST survive process restarts. After a crash or restart, the agent MUST be able to resume work in the correct worktree for each task.

Lookup by task ID MUST be efficient. The mapping MUST use indexed queries. Orphan detection MUST identify worktrees without active tasks.

Mapping updates MUST be atomic with worktree operations. If worktree creation fails, no mapping MUST exist. If removal succeeds, the mapping MUST be deleted.

### Business Value

Persistent mapping enables reliable task isolation. The agent can recover context after restarts. Orphaned worktrees can be detected and cleaned.

### Scope Boundaries

This task covers database persistence only. Worktree operations are in 023.a. Cleanup policies are in 023.c.

### Integration Points

- Task 023: Worktree management
- Task 011: Workspace DB storage
- Task 039: Task/session context

### Failure Modes

- DB write fails → Rollback worktree creation
- Mapping out of sync → Reconciliation on startup
- Task not found → Return null, not error

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Mapping | Association between worktree path and task ID |
| Orphan | Worktree with no associated active task |
| Reconciliation | Syncing DB state with filesystem state |

---

## Out of Scope

- Worktree creation/removal (Task 023.a)
- Cleanup policy execution (Task 023.c)
- Cross-repo mappings

---

## Functional Requirements

### FR-001 to FR-020: Core Operations

- FR-001: `CreateMappingAsync` MUST store worktree-task association
- FR-002: `GetMappingByTaskAsync` MUST return worktree for task
- FR-003: `GetMappingByPathAsync` MUST return task for worktree
- FR-004: `DeleteMappingAsync` MUST remove association
- FR-005: `ListMappingsAsync` MUST return all mappings
- FR-006: Mapping MUST include worktree path
- FR-007: Mapping MUST include task ID
- FR-008: Mapping MUST include creation timestamp
- FR-009: Mapping MUST include last access timestamp
- FR-010: Mapping MUST include branch name
- FR-011: Mapping MUST include commit SHA at creation
- FR-012: Duplicate task mapping MUST be rejected
- FR-013: Duplicate path mapping MUST be rejected
- FR-014: Null task ID MUST be allowed (manual worktrees)
- FR-015: Lookup by task MUST use index
- FR-016: Lookup by path MUST use index
- FR-017: All operations MUST be async
- FR-018: All operations MUST support cancellation
- FR-019: Operations MUST be transactional
- FR-020: Failed operations MUST NOT leave partial state

### FR-021 to FR-035: Synchronization

- FR-021: Startup MUST reconcile DB with filesystem
- FR-022: Missing worktrees MUST be flagged
- FR-023: Missing mappings MUST be created for found worktrees
- FR-024: Reconciliation MUST be logged
- FR-025: `ReconcileAsync` MUST be callable manually
- FR-026: Reconciliation MUST NOT delete mappings automatically
- FR-027: Orphan query MUST return unmapped worktrees
- FR-028: Stale query MUST return mappings for missing worktrees
- FR-029: Access timestamp MUST update on worktree use
- FR-030: Access update MUST be debounced
- FR-031: Bulk operations MUST be efficient
- FR-032: Large mapping counts MUST be handled
- FR-033: Mapping schema MUST be versioned
- FR-034: Schema migration MUST be automatic
- FR-035: Backup MUST be possible

---

## Non-Functional Requirements

- NFR-001: Lookup MUST complete in <50ms
- NFR-002: Insert MUST complete in <100ms
- NFR-003: List all MUST complete in <200ms
- NFR-004: Reconciliation MUST complete in <5s for 100 worktrees
- NFR-005: Concurrent access MUST be safe
- NFR-006: DB lock timeout MUST be 5s
- NFR-007: Transaction isolation MUST prevent dirty reads
- NFR-008: Path storage MUST be normalized
- NFR-009: Case sensitivity MUST match filesystem
- NFR-010: Maximum path length MUST be 4096

---

## User Manual Documentation

### Database Schema

```sql
CREATE TABLE worktree_mappings (
    id INTEGER PRIMARY KEY,
    worktree_path TEXT NOT NULL UNIQUE,
    task_id TEXT UNIQUE,
    branch_name TEXT NOT NULL,
    commit_sha TEXT NOT NULL,
    created_at TEXT NOT NULL,
    last_accessed_at TEXT NOT NULL
);

CREATE INDEX idx_worktree_task ON worktree_mappings(task_id);
CREATE INDEX idx_worktree_path ON worktree_mappings(worktree_path);
```

### API Usage

```csharp
// Create mapping when worktree created
await _mappingService.CreateMappingAsync(new WorktreeMapping
{
    WorktreePath = "/path/to/worktree",
    TaskId = "TASK-123",
    BranchName = "feature/task-123",
    CommitSha = "abc123"
});

// Lookup by task
var mapping = await _mappingService.GetMappingByTaskAsync("TASK-123");

// Lookup by path
var mapping = await _mappingService.GetMappingByPathAsync("/path/to/worktree");
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Create mapping works
- [ ] AC-002: Get by task works
- [ ] AC-003: Get by path works
- [ ] AC-004: Delete mapping works
- [ ] AC-005: List all works
- [ ] AC-006: Duplicate rejection works
- [ ] AC-007: Indexes used for queries
- [ ] AC-008: Reconciliation works
- [ ] AC-009: Orphan detection works
- [ ] AC-010: Performance benchmarks met

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Test mapping creation
- [ ] UT-002: Test lookup by task
- [ ] UT-003: Test lookup by path
- [ ] UT-004: Test duplicate rejection
- [ ] UT-005: Test reconciliation logic

### Integration Tests

- [ ] IT-001: Full CRUD cycle
- [ ] IT-002: Reconciliation with real DB
- [ ] IT-003: Concurrent access
- [ ] IT-004: Large dataset handling

### Performance Tests

- [ ] PB-001: Lookup in <50ms
- [ ] PB-002: Insert in <100ms
- [ ] PB-003: List 1000 in <500ms

---

## Implementation Prompt

### Interface

```csharp
public interface IWorktreeMappingRepository
{
    Task CreateAsync(WorktreeMapping mapping, CancellationToken ct = default);
    Task<WorktreeMapping?> GetByTaskAsync(TaskId taskId, CancellationToken ct = default);
    Task<WorktreeMapping?> GetByPathAsync(string path, CancellationToken ct = default);
    Task DeleteAsync(string path, CancellationToken ct = default);
    Task<IReadOnlyList<WorktreeMapping>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<WorktreeMapping>> GetOrphansAsync(CancellationToken ct = default);
    Task ReconcileAsync(CancellationToken ct = default);
}
```

---

**End of Task 023.b Specification**