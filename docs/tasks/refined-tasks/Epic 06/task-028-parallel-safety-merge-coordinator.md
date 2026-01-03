# Task 028: Parallel Safety + Merge Coordinator

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 027 (Workers), Task 022 (Git), Task 023 (Worktrees)  

---

## Description

Task 028 implements parallel safety and merge coordination. When multiple workers modify files concurrently, changes MUST be merged safely. Conflicts MUST be detected before merge. Merge plans MUST be generated.

Parallel execution introduces the risk of conflicting changes. Two tasks editing the same file MUST be coordinated. The merge coordinator MUST analyze changes before combining them.

Conflict detection MUST be heuristic-based. Overlapping file edits MUST trigger review. Non-overlapping changes MUST merge automatically. Complex conflicts MUST halt for resolution.

### Business Value

Merge coordination enables:
- Safe parallel execution
- Higher throughput
- Conflict prevention
- Automatic merging
- Predictable outcomes

### Scope Boundaries

This task covers merge coordination. Conflict heuristics are in Task 028.a. Dependency graphs are in Task 028.b. Integration tests are in Task 028.c.

### Integration Points

- Task 027: Workers produce changes
- Task 022: Git operations for merge
- Task 023: Worktrees provide isolation
- Task 026: Queue for task ordering

### Failure Modes

- Conflict unresolved → Block merge
- Merge failure → Rollback changes
- Heuristic wrong → Manual review
- Corrupt merge → Restore from branch

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Merge | Combine parallel changes |
| Conflict | Overlapping modifications |
| Heuristic | Best-guess detection |
| Plan | Merge execution strategy |
| Coordinator | Merge orchestrator |
| Fast-forward | Simple linear merge |
| Three-way | Common ancestor merge |
| Rebase | Replay commits on base |

---

## Out of Scope

- Semantic merge (AST-aware)
- AI-assisted conflict resolution
- Visual merge tool integration
- Merge request workflows
- Branch protection rules

---

## Functional Requirements

### FR-001 to FR-030: Merge Coordinator

- FR-001: `IMergeCoordinator` interface MUST be defined
- FR-002: Coordinator MUST track pending merges
- FR-003: Coordinator MUST order merges
- FR-004: First-completed MUST merge first
- FR-005: Dependent tasks MUST wait
- FR-006: Coordinator MUST analyze changes
- FR-007: Analysis MUST identify files
- FR-008: Analysis MUST identify line ranges
- FR-009: Analysis MUST compute overlap
- FR-010: Overlap MUST trigger conflict check
- FR-011: No overlap MUST allow fast merge
- FR-012: Coordinator MUST generate plan
- FR-013: Plan MUST list merge steps
- FR-014: Plan MUST identify conflicts
- FR-015: Plan MUST suggest resolution
- FR-016: Plan MUST be reviewable
- FR-017: Plan approval MUST be optional
- FR-018: Auto-approve MUST be configurable
- FR-019: Coordinator MUST execute plan
- FR-020: Execution MUST be atomic
- FR-021: Failure MUST rollback
- FR-022: Success MUST update refs
- FR-023: Merge events MUST emit
- FR-024: Merge metrics MUST track
- FR-025: Coordinator MUST handle queuing
- FR-026: Queue MUST prevent races
- FR-027: Merge lock MUST be held
- FR-028: Lock timeout MUST be configurable
- FR-029: Lock contention MUST queue
- FR-030: Coordinator MUST cleanup

### FR-031 to FR-055: Conflict Detection

- FR-031: File-level conflict MUST detect
- FR-032: Line-level conflict MUST detect
- FR-033: Overlap within N lines MUST warn
- FR-034: Default N MUST be 5
- FR-035: Same function MUST warn
- FR-036: Function detection MUST be heuristic
- FR-037: Import/using changes MAY conflict
- FR-038: Config file changes MAY conflict
- FR-039: Lock file changes MUST merge special
- FR-040: Binary files MUST NOT merge
- FR-041: Binary conflict MUST block
- FR-042: Conflict severity MUST be rated
- FR-043: Severity: low, medium, high, critical
- FR-044: Low MUST auto-resolve
- FR-045: Medium MUST warn but merge
- FR-046: High MUST require review
- FR-047: Critical MUST block
- FR-048: Severity rules MUST be configurable
- FR-049: Custom rules MUST be supported
- FR-050: Rule matching MUST use patterns
- FR-051: Detection MUST be fast
- FR-052: Large diffs MUST timeout
- FR-053: Timeout MUST be configurable
- FR-054: Partial analysis MUST be possible
- FR-055: Analysis result MUST be cached

### FR-056 to FR-075: Merge Execution

- FR-056: Merge MUST use git
- FR-057: Three-way merge MUST be default
- FR-058: Rebase MAY be configured
- FR-059: Fast-forward MUST be used when possible
- FR-060: Merge commit MUST be created
- FR-061: Commit message MUST be generated
- FR-062: Message MUST list merged tasks
- FR-063: Message MUST link task IDs
- FR-064: Merge MUST preserve history
- FR-065: Squash MUST be optional
- FR-066: Merge MUST validate result
- FR-067: Build MUST run after merge
- FR-068: Tests MUST run after merge
- FR-069: Failure MUST rollback
- FR-070: Rollback MUST restore state
- FR-071: Rollback MUST be logged
- FR-072: Success MUST cleanup branches
- FR-073: Cleanup MUST be optional
- FR-074: Merged branches MUST be deletable
- FR-075: Main branch MUST be protected

---

## Non-Functional Requirements

- NFR-001: Conflict detection MUST be <1s
- NFR-002: Merge execution MUST be <30s
- NFR-003: Rollback MUST be <10s
- NFR-004: 10 parallel merges MUST work
- NFR-005: Lock fairness MUST be FIFO
- NFR-006: No data loss on merge failure
- NFR-007: Atomic merge operations
- NFR-008: Clear conflict messages
- NFR-009: Reproducible merges
- NFR-010: Audit trail complete

---

## User Manual Documentation

### Configuration

```yaml
merge:
  strategy: three-way  # three-way, rebase, ff-only
  autoApprove: true
  autoCleanup: true
  overlapWarningLines: 5
  
  conflict:
    lowAction: auto-resolve
    mediumAction: warn-and-merge
    highAction: require-review
    criticalAction: block
    
  validation:
    buildAfterMerge: true
    testAfterMerge: true
```

### CLI Commands

```bash
# Show pending merges
acode merge list

# Preview merge plan
acode merge plan task-abc123

# Execute merge
acode merge execute task-abc123

# Review conflicts
acode merge conflicts task-abc123

# Force merge (skip validation)
acode merge execute --force task-abc123

# Rollback merge
acode merge rollback task-abc123
```

### Merge Plan Example

```
Merge Plan for task-abc123
==========================
Strategy: three-way merge

Changes:
  - src/Auth/LoginHandler.cs (modified, 45 lines)
  - tests/Auth/LoginHandlerTests.cs (modified, 120 lines)

Conflicts:
  ⚠ MEDIUM: src/Auth/LoginHandler.cs
    Line 42-48 overlaps with task-def456 (lines 44-52)
    Suggestion: Review changes before merge

Dependencies:
  ✓ task-xyz789 (already merged)

Actions:
  1. Fast-forward from main to task-xyz789
  2. Three-way merge task-abc123
  3. Run validation pipeline
  4. Update main branch
  5. Cleanup task branch

Approve? [y/N]
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Coordinator tracks merges
- [ ] AC-002: Analysis identifies overlaps
- [ ] AC-003: Conflict severity rated
- [ ] AC-004: Plan generated
- [ ] AC-005: Auto-merge works
- [ ] AC-006: Conflict blocks work
- [ ] AC-007: Merge executes
- [ ] AC-008: Rollback works
- [ ] AC-009: Validation runs
- [ ] AC-010: Cleanup works
- [ ] AC-011: Events emitted
- [ ] AC-012: Metrics tracked

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Overlap detection
- [ ] UT-002: Severity rating
- [ ] UT-003: Plan generation
- [ ] UT-004: Lock management
- [ ] UT-005: Rollback logic

### Integration Tests

- [ ] IT-001: Full merge cycle
- [ ] IT-002: Conflict handling
- [ ] IT-003: Parallel merges
- [ ] IT-004: Validation pipeline

---

## Implementation Prompt

### Interface

```csharp
public interface IMergeCoordinator
{
    Task<MergePlan> AnalyzeAsync(string taskId,
        CancellationToken ct = default);
        
    Task<MergeResult> ExecuteAsync(MergePlan plan,
        CancellationToken ct = default);
        
    Task RollbackAsync(string mergeId,
        CancellationToken ct = default);
        
    Task<IReadOnlyList<PendingMerge>> GetPendingAsync(
        CancellationToken ct = default);
}

public record MergePlan(
    string TaskId,
    MergeStrategy Strategy,
    IReadOnlyList<FileChange> Changes,
    IReadOnlyList<Conflict> Conflicts,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<MergeStep> Steps,
    ConflictSeverity MaxSeverity);

public record Conflict(
    string File,
    LineRange LocalRange,
    LineRange RemoteRange,
    string RemoteTaskId,
    ConflictSeverity Severity,
    string Suggestion);

public enum ConflictSeverity { Low, Medium, High, Critical }

public record MergeResult(
    bool Success,
    string? MergeCommitId,
    string? Error,
    TimeSpan Duration);
```

---

**End of Task 028 Specification**