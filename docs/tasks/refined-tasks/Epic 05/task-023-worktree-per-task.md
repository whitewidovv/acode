# Task 023: Worktree-per-Task

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 5 – Git Integration Layer  
**Dependencies:** Task 022 (Git Tool Layer), Task 011 (Workspace DB)  

---

## Description

Task 023 implements the worktree-per-task isolation pattern. Each task MAY execute in its own Git worktree. This prevents work-in-progress from interfering with the main worktree or other tasks.

Git worktrees allow multiple working directories linked to the same repository. Each worktree can be on a different branch. Changes in one worktree do NOT affect others. This enables parallel development.

The worktree-per-task pattern creates a dedicated worktree for each agent task. The task operates in isolation. Commits can be made without affecting the main branch. Failed tasks can be discarded without cleanup complexity.

Worktree lifecycle MUST be managed automatically. Creation MUST happen when a task requests isolation. Removal MUST happen after task completion or abandonment. Orphaned worktrees MUST be detected and cleaned.

The mapping between worktrees and tasks MUST be persisted. This enables recovery after restart. The Workspace DB (Task 011) MUST store the mapping. Querying worktree by task ID MUST be efficient.

Cleanup policies MUST prevent disk exhaustion. Old worktrees MUST be removed based on age or count limits. Uncommitted changes MUST be handled appropriately during cleanup.

### Business Value

Isolation prevents interference between concurrent tasks. Failed experiments can be discarded cleanly. The main worktree remains pristine. Multiple features can be developed in parallel.

### Scope Boundaries

This task defines the worktree management service. Subtasks cover specific operations: 023.a (create/remove/list), 023.b (task mapping), 023.c (cleanup policies).

### Integration Points

- Task 022: Git operations for branch management
- Task 011: Workspace DB for mapping persistence
- Task 039: Session management for task context
- Task 018: Command execution for git worktree commands

### Failure Modes

- Worktree path in use → Clear error
- Branch already checked out → Create new branch or error
- Disk full → Cleanup old worktrees, then error
- Orphaned worktree → Detected and cleaned
- Mapping out of sync → Reconciliation on startup

### Assumptions

- Git version 2.20+ with worktree support
- Sufficient disk space for multiple worktrees
- Filesystem supports required operations

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Worktree | Additional working directory linked to repository |
| Main Worktree | The original repository checkout |
| Linked Worktree | Additional worktree created via git worktree add |
| Task | Unit of work assigned to the agent |
| Isolation | Separation of changes between tasks |
| Orphaned Worktree | Worktree without associated task |
| Cleanup Policy | Rules for automatic worktree removal |
| Mapping | Association between worktree and task |

---

## Out of Scope

- Git submodule handling in worktrees
- Cross-worktree file sharing
- Worktree synchronization
- Network-mounted worktrees
- Worktree templates
- Custom worktree layouts

---

## Functional Requirements

### FR-001 to FR-020: Core Interface

- FR-001: `IWorktreeService` interface MUST be defined
- FR-002: Interface MUST include `CreateAsync` method
- FR-003: Interface MUST include `RemoveAsync` method
- FR-004: Interface MUST include `ListAsync` method
- FR-005: Interface MUST include `GetForTaskAsync` method
- FR-006: Interface MUST include `GetAsync` by path
- FR-007: Interface MUST include `ExistsAsync` method
- FR-008: All methods MUST support cancellation
- FR-009: All methods MUST be async
- FR-010: Worktree paths MUST be validated
- FR-011: Worktree paths MUST be absolute
- FR-012: Worktree paths MUST be within allowed base
- FR-013: Base path MUST be configurable
- FR-014: Default base MUST be `.acode/worktrees`
- FR-015: Worktree MUST include path property
- FR-016: Worktree MUST include branch property
- FR-017: Worktree MUST include commit SHA property
- FR-018: Worktree MUST include associated task ID
- FR-019: Worktree MUST include creation timestamp
- FR-020: Worktree MUST include last access timestamp

### FR-021 to FR-040: Lifecycle Management

- FR-021: Task requesting isolation MUST get worktree
- FR-022: Worktree MUST be created with unique path
- FR-023: Path naming MUST include task identifier
- FR-024: Path naming MUST include timestamp
- FR-025: Branch MUST be created for worktree
- FR-026: Branch naming MUST follow convention
- FR-027: Branch name MUST include task ID
- FR-028: Worktree MUST be ready before returning
- FR-029: Creation failure MUST cleanup partial state
- FR-030: Task completion MUST trigger cleanup consideration
- FR-031: Cleanup MUST respect retention policy
- FR-032: Immediate cleanup MUST be optional
- FR-033: Cleanup MUST NOT lose uncommitted changes
- FR-034: Uncommitted changes MUST trigger warning
- FR-035: Force cleanup MUST be available
- FR-036: Cleanup MUST remove worktree and branch
- FR-037: Cleanup MUST update mapping
- FR-038: Orphan detection MUST run periodically
- FR-039: Orphans MUST be logged
- FR-040: Orphan cleanup MUST respect age threshold

---

## Non-Functional Requirements

### NFR-001 to NFR-015

- NFR-001: Worktree creation MUST complete in <5s
- NFR-002: Worktree removal MUST complete in <2s
- NFR-003: Listing MUST complete in <500ms
- NFR-004: Mapping lookup MUST complete in <50ms
- NFR-005: Memory overhead MUST be <10MB per worktree
- NFR-006: Disk usage MUST be monitored
- NFR-007: Concurrent operations MUST be safe
- NFR-008: Locking MUST prevent race conditions
- NFR-009: Failed operations MUST cleanup
- NFR-010: Paths MUST be sanitized
- NFR-011: Path traversal MUST be prevented
- NFR-012: Symlinks MUST NOT escape base
- NFR-013: Operations MUST be logged
- NFR-014: Cleanup MUST be logged
- NFR-015: Errors MUST include worktree path

---

## User Manual Documentation

### Quick Start

```bash
# Create worktree for a task
acode worktree create --task TASK-123

# List worktrees
acode worktree list

# Remove worktree
acode worktree remove --task TASK-123
```

### Configuration

```yaml
worktree:
  basePath: .acode/worktrees
  branchPrefix: acode-task-
  maxWorktrees: 10
  maxAgeDays: 7
  cleanupOnComplete: false
```

### Automatic Isolation

When a task requests isolated execution:

```csharp
var worktree = await _worktreeService.GetOrCreateForTaskAsync(taskId);
// Task executes in worktree.Path
// Changes are isolated from main worktree
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: IWorktreeService interface defined
- [ ] AC-002: Worktree creation works
- [ ] AC-003: Worktree removal works
- [ ] AC-004: Worktree listing works
- [ ] AC-005: Task mapping persisted
- [ ] AC-006: Task lookup works
- [ ] AC-007: Orphan detection works
- [ ] AC-008: Cleanup policy enforced
- [ ] AC-009: Configuration respected
- [ ] AC-010: CLI commands work
- [ ] AC-011: Errors are clear
- [ ] AC-012: Unit tests >90%

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Test path generation
- [ ] UT-002: Test branch naming
- [ ] UT-003: Test mapping operations
- [ ] UT-004: Test orphan detection logic
- [ ] UT-005: Test cleanup policy rules

### Integration Tests

- [ ] IT-001: Create worktree on real repo
- [ ] IT-002: Remove worktree on real repo
- [ ] IT-003: List worktrees
- [ ] IT-004: Task mapping persistence
- [ ] IT-005: Concurrent operations

### End-to-End Tests

- [ ] E2E-001: CLI create command
- [ ] E2E-002: CLI list command
- [ ] E2E-003: CLI remove command
- [ ] E2E-004: Task isolation workflow

---

## Implementation Prompt

### Core Interface

```csharp
public interface IWorktreeService
{
    Task<Worktree> CreateAsync(string repoPath, CreateWorktreeOptions options, 
        CancellationToken ct = default);
    Task RemoveAsync(string worktreePath, RemoveWorktreeOptions? options = null,
        CancellationToken ct = default);
    Task<IReadOnlyList<Worktree>> ListAsync(string repoPath, 
        CancellationToken ct = default);
    Task<Worktree?> GetForTaskAsync(TaskId taskId, 
        CancellationToken ct = default);
    Task<Worktree?> GetAsync(string path, CancellationToken ct = default);
    Task<bool> ExistsAsync(string path, CancellationToken ct = default);
}

public record Worktree(
    string Path,
    string Branch,
    string CommitSha,
    TaskId? AssociatedTaskId,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastAccessedAt);
```

### Validation Checklist

- [ ] Path sanitization complete
- [ ] Concurrent access safe
- [ ] Orphan detection works
- [ ] Cleanup policy enforced

---

**End of Task 023 Specification**