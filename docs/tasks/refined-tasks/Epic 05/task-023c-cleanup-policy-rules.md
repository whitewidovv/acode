# Task 023.c: cleanup policy rules

**Priority:** P2 – Medium  
**Tier:** S – Core Infrastructure  
**Complexity:** 3 (Fibonacci points)  
**Phase:** Phase 5 – Git Integration Layer  
**Dependencies:** Task 023 (Worktree-per-Task), Task 023.a, Task 023.b  

---

## Description

Task 023.c implements cleanup policy rules for worktrees. Automatic cleanup MUST prevent disk exhaustion. Policies MUST be configurable to match team workflows.

Age-based cleanup MUST remove worktrees older than a configured threshold. Default MUST be 7 days. Completed task worktrees MAY be cleaned immediately.

Count-based cleanup MUST limit total worktrees. When the limit is reached, oldest worktrees MUST be candidates for removal. Active task worktrees MUST be protected.

Uncommitted changes MUST be handled appropriately. By default, worktrees with uncommitted changes MUST NOT be auto-deleted. Force cleanup MUST be available with explicit acknowledgment.

### Business Value

Automatic cleanup prevents disk exhaustion. Teams don't need to manually manage worktree proliferation. Configurable policies match different workflows.

### Scope Boundaries

This task covers policy definition and execution. Worktree operations are in 023.a. Mapping is in 023.b.

### Integration Points

- Task 023: Worktree service
- Task 023.a: Removal operations
- Task 023.b: Mapping queries
- Task 002: Configuration

### Failure Modes

- All worktrees protected → Warning, no cleanup
- Cleanup during task execution → Skip active worktrees
- Disk check fails → Log warning, continue

---

## Functional Requirements

### FR-001 to FR-025: Policy Definition

- FR-001: `maxAgeDays` MUST define age threshold
- FR-002: Default `maxAgeDays` MUST be 7
- FR-003: `maxWorktrees` MUST define count limit
- FR-004: Default `maxWorktrees` MUST be 10
- FR-005: `cleanupOnComplete` MUST control immediate cleanup
- FR-006: Default `cleanupOnComplete` MUST be false
- FR-007: `protectUncommitted` MUST control uncommitted handling
- FR-008: Default `protectUncommitted` MUST be true
- FR-009: `protectActive` MUST control active task handling
- FR-010: Default `protectActive` MUST be true
- FR-011: `minKeep` MUST define minimum retained count
- FR-012: Default `minKeep` MUST be 2
- FR-013: Policy MUST be loaded from config
- FR-014: Policy MUST support override per-repo
- FR-015: Policy changes MUST apply immediately
- FR-016: Invalid policy MUST produce clear error
- FR-017: Policy MUST be queryable
- FR-018: `enabled` MUST control auto-cleanup
- FR-019: Default `enabled` MUST be true
- FR-020: `scheduleMinutes` MUST define cleanup interval
- FR-021: Default `scheduleMinutes` MUST be 60
- FR-022: Disk threshold MUST trigger emergency cleanup
- FR-023: Disk threshold MUST be configurable
- FR-024: Default disk threshold MUST be 90%
- FR-025: Policy MUST be serializable for logging

### FR-026 to FR-045: Cleanup Execution

- FR-026: `RunCleanupAsync` MUST execute policy
- FR-027: Cleanup MUST identify candidates by age
- FR-028: Cleanup MUST identify candidates by count
- FR-029: Protected worktrees MUST be excluded
- FR-030: Active task worktrees MUST be excluded
- FR-031: Uncommitted worktrees MUST be excluded (if protected)
- FR-032: Candidates MUST be sorted by last access
- FR-033: Oldest MUST be removed first
- FR-034: Removal MUST use 023.a operations
- FR-035: Removal MUST update 023.b mappings
- FR-036: Cleanup MUST be logged
- FR-037: Each removal MUST be logged individually
- FR-038: Cleanup summary MUST be returned
- FR-039: Summary MUST include removed count
- FR-040: Summary MUST include skipped count
- FR-041: Summary MUST include error count
- FR-042: Errors MUST NOT stop cleanup
- FR-043: Cleanup MUST be cancellable
- FR-044: Cleanup MUST support dry-run mode
- FR-045: Dry-run MUST return what would be removed

---

## Non-Functional Requirements

- NFR-001: Cleanup MUST complete in <30s for 100 worktrees
- NFR-002: Policy evaluation MUST complete in <100ms
- NFR-003: Disk check MUST complete in <1s
- NFR-004: Cleanup MUST NOT block other operations
- NFR-005: Cleanup MUST be idempotent
- NFR-006: Concurrent cleanups MUST be prevented
- NFR-007: Cleanup state MUST be recoverable
- NFR-008: Audit log MUST record all removals
- NFR-009: Metrics MUST track cleanup effectiveness
- NFR-010: Alerts MUST fire on cleanup failures

---

## User Manual Documentation

### Configuration

```yaml
worktree:
  cleanup:
    enabled: true
    maxAgeDays: 7
    maxWorktrees: 10
    cleanupOnComplete: false
    protectUncommitted: true
    protectActive: true
    minKeep: 2
    scheduleMinutes: 60
    diskThresholdPercent: 90
```

### Manual Cleanup

```bash
# Run cleanup now
acode worktree cleanup

# Dry run
acode worktree cleanup --dry-run

# Force cleanup including uncommitted
acode worktree cleanup --force
```

### Automatic Cleanup

Cleanup runs automatically every 60 minutes (configurable). It removes:

1. Worktrees older than `maxAgeDays`
2. Excess worktrees beyond `maxWorktrees` (oldest first)
3. Orphaned worktrees (no task mapping)

Protected worktrees are never auto-removed:
- Active task worktrees
- Worktrees with uncommitted changes (unless forced)
- Worktrees within `minKeep` count

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Age policy works
- [ ] AC-002: Count policy works
- [ ] AC-003: Uncommitted protection works
- [ ] AC-004: Active task protection works
- [ ] AC-005: Manual cleanup works
- [ ] AC-006: Scheduled cleanup works
- [ ] AC-007: Dry-run shows candidates
- [ ] AC-008: Force removes uncommitted
- [ ] AC-009: Configuration respected
- [ ] AC-010: Cleanup logged

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Test age evaluation
- [ ] UT-002: Test count evaluation
- [ ] UT-003: Test protection logic
- [ ] UT-004: Test sorting

### Integration Tests

- [ ] IT-001: Full cleanup cycle
- [ ] IT-002: Protection enforcement
- [ ] IT-003: Scheduled execution
- [ ] IT-004: Concurrent access

### End-to-End Tests

- [ ] E2E-001: CLI cleanup
- [ ] E2E-002: CLI dry-run
- [ ] E2E-003: Automatic scheduling

---

## Implementation Prompt

### Interface

```csharp
public interface IWorktreeCleanupService
{
    Task<CleanupResult> RunCleanupAsync(CleanupOptions? options = null, 
        CancellationToken ct = default);
    Task<IReadOnlyList<Worktree>> GetCandidatesAsync(CancellationToken ct = default);
    CleanupPolicy GetCurrentPolicy();
}

public record CleanupPolicy(
    bool Enabled,
    int MaxAgeDays,
    int MaxWorktrees,
    bool CleanupOnComplete,
    bool ProtectUncommitted,
    bool ProtectActive,
    int MinKeep,
    int ScheduleMinutes,
    int DiskThresholdPercent);

public record CleanupResult(
    int RemovedCount,
    int SkippedCount,
    int ErrorCount,
    IReadOnlyList<string> RemovedPaths,
    IReadOnlyList<CleanupError> Errors);
```

---

**End of Task 023.c Specification**