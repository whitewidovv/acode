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

## Assumptions

### Technical Assumptions

1. **Git Integration**: Git command-line tools are available for merge operations
2. **Worktree Isolation**: Each task operates in isolated worktree (task-023)
3. **Branch Strategy**: Each task creates changes on dedicated branch
4. **Merge Destination**: All tasks merge to same target branch (configurable)
5. **Sequential Merging**: Only one merge operation executes at a time
6. **Conflict Detection**: Git's built-in conflict detection is sufficient

### Coordination Assumptions

7. **First-Complete First-Merge**: Task that completes first gets merge priority
8. **Blocking on Conflict**: Conflicting merges block until resolved or aborted
9. **Retry After Rebase**: Failed merges can retry after rebase to current head
10. **Rollback Capability**: Failed merge is automatically rolled back
11. **Lock Mechanism**: Merge lock prevents concurrent merge attempts

### Integration Assumptions

12. **Worker Pool Integration**: Workers notify coordinator on task completion
13. **Event Emission**: Merge events are emitted for monitoring (task-013)
14. **State Persistence**: Pending merge queue persists across restarts
15. **CLI Control**: Merge operations are controllable via CLI commands

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

### File Structure

```
src/
├── Acode.Core/
│   └── Domain/
│       └── Merge/
│           ├── MergePlan.cs              # Merge plan record
│           ├── Conflict.cs               # Conflict detection
│           ├── MergeResult.cs            # Merge outcome
│           └── MergeExceptions.cs        # Merge exceptions
│
├── Acode.Application/
│   └── Services/
│       └── Merge/
│           ├── IMergeCoordinator.cs      # Coordinator interface
│           ├── MergeCoordinator.cs       # Coordinator impl
│           ├── IConflictDetector.cs      # Detection interface
│           ├── ConflictDetector.cs       # Overlap detection
│           ├── IMergePlanner.cs          # Plan generation
│           ├── MergePlanner.cs           # Plan impl
│           ├── IMergeExecutor.cs         # Execution interface
│           ├── MergeExecutor.cs          # Git merge execution
│           └── MergeLock.cs              # Merge serialization
│
└── Acode.Cli/
    └── Commands/
        └── Merge/
            ├── MergeListCommand.cs
            ├── MergePlanCommand.cs
            ├── MergeExecuteCommand.cs
            └── MergeRollbackCommand.cs

tests/
├── Acode.Application.Tests/
│   └── Services/
│       └── Merge/
│           ├── ConflictDetectorTests.cs
│           ├── MergePlannerTests.cs
│           └── MergeExecutorTests.cs
│
└── Acode.Integration.Tests/
    └── Merge/
        └── ParallelMergeTests.cs
```

### Domain Models

```csharp
// Acode.Core/Domain/Merge/Conflict.cs
namespace Acode.Core.Domain.Merge;

/// <summary>
/// Severity of a detected conflict.
/// </summary>
public enum ConflictSeverity
{
    /// <summary>Auto-resolvable, no action needed.</summary>
    Low = 0,
    
    /// <summary>Warn but proceed with merge.</summary>
    Medium = 1,
    
    /// <summary>Requires review before merge.</summary>
    High = 2,
    
    /// <summary>Blocks merge entirely.</summary>
    Critical = 3
}

/// <summary>
/// A detected conflict between parallel changes.
/// </summary>
public sealed record Conflict
{
    /// <summary>File with conflict.</summary>
    public required string FilePath { get; init; }
    
    /// <summary>Lines affected in local changes.</summary>
    public required LineRange LocalRange { get; init; }
    
    /// <summary>Lines affected in remote changes.</summary>
    public required LineRange RemoteRange { get; init; }
    
    /// <summary>Task ID that made the remote change.</summary>
    public required TaskId RemoteTaskId { get; init; }
    
    /// <summary>Conflict severity.</summary>
    public required ConflictSeverity Severity { get; init; }
    
    /// <summary>Suggested resolution.</summary>
    public string? Suggestion { get; init; }
    
    /// <summary>Type of conflict.</summary>
    public ConflictType Type { get; init; } = ConflictType.Overlap;
}

/// <summary>
/// Type of conflict detected.
/// </summary>
public enum ConflictType
{
    /// <summary>Line ranges overlap.</summary>
    Overlap,
    
    /// <summary>Same function modified.</summary>
    SameFunction,
    
    /// <summary>Nearby changes (within N lines).</summary>
    Proximity,
    
    /// <summary>Binary file conflict.</summary>
    Binary,
    
    /// <summary>Lock file (package.json, etc.).</summary>
    LockFile
}

/// <summary>
/// A range of lines in a file.
/// </summary>
public readonly record struct LineRange(int Start, int End)
{
    public int Length => End - Start + 1;
    
    public bool Overlaps(LineRange other) =>
        Start <= other.End && End >= other.Start;
    
    public bool IsProximateTo(LineRange other, int threshold) =>
        Math.Abs(Start - other.End) <= threshold ||
        Math.Abs(End - other.Start) <= threshold;
    
    public override string ToString() => $"L{Start}-L{End}";
}

// Acode.Core/Domain/Merge/MergePlan.cs
namespace Acode.Core.Domain.Merge;

/// <summary>
/// A plan for merging task changes.
/// </summary>
public sealed record MergePlan
{
    /// <summary>Unique plan identifier.</summary>
    public required string PlanId { get; init; }
    
    /// <summary>Task being merged.</summary>
    public required TaskId TaskId { get; init; }
    
    /// <summary>Source branch.</summary>
    public required string SourceBranch { get; init; }
    
    /// <summary>Target branch.</summary>
    public required string TargetBranch { get; init; }
    
    /// <summary>Merge strategy.</summary>
    public required MergeStrategy Strategy { get; init; }
    
    /// <summary>Files changed by this task.</summary>
    public required IReadOnlyList<FileChange> Changes { get; init; }
    
    /// <summary>Detected conflicts.</summary>
    public required IReadOnlyList<Conflict> Conflicts { get; init; }
    
    /// <summary>Tasks that must merge first.</summary>
    public required IReadOnlyList<TaskId> Dependencies { get; init; }
    
    /// <summary>Steps to execute the merge.</summary>
    public required IReadOnlyList<MergeStep> Steps { get; init; }
    
    /// <summary>Maximum conflict severity.</summary>
    public ConflictSeverity MaxSeverity => Conflicts.Count > 0
        ? Conflicts.Max(c => c.Severity)
        : ConflictSeverity.Low;
    
    /// <summary>Whether merge can proceed automatically.</summary>
    public bool CanAutoMerge => MaxSeverity < ConflictSeverity.High;
    
    /// <summary>Whether merge is blocked.</summary>
    public bool IsBlocked => Conflicts.Any(c => c.Severity == ConflictSeverity.Critical);
    
    /// <summary>When the plan was created.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Merge strategy to use.
/// </summary>
public enum MergeStrategy
{
    /// <summary>Three-way merge with merge commit.</summary>
    ThreeWay,
    
    /// <summary>Rebase commits onto target.</summary>
    Rebase,
    
    /// <summary>Fast-forward only (no merge commit).</summary>
    FastForward
}

/// <summary>
/// A file change in the merge.
/// </summary>
public sealed record FileChange
{
    public required string Path { get; init; }
    public required FileChangeType ChangeType { get; init; }
    public int LinesAdded { get; init; }
    public int LinesRemoved { get; init; }
}

public enum FileChangeType { Added, Modified, Deleted, Renamed }

/// <summary>
/// A step in the merge execution.
/// </summary>
public sealed record MergeStep
{
    public required int Order { get; init; }
    public required MergeStepType Type { get; init; }
    public required string Description { get; init; }
    public string? Command { get; init; }
}

public enum MergeStepType
{
    Fetch,
    Checkout,
    Merge,
    Rebase,
    Validate,
    Commit,
    Push,
    Cleanup
}

// Acode.Core/Domain/Merge/MergeResult.cs
namespace Acode.Core.Domain.Merge;

/// <summary>
/// Result of a merge execution.
/// </summary>
public sealed record MergeResult
{
    /// <summary>Whether merge succeeded.</summary>
    public required bool Success { get; init; }
    
    /// <summary>Plan that was executed.</summary>
    public required string PlanId { get; init; }
    
    /// <summary>Merge commit SHA (if created).</summary>
    public string? MergeCommitId { get; init; }
    
    /// <summary>Error message if failed.</summary>
    public string? Error { get; init; }
    
    /// <summary>Execution duration.</summary>
    public TimeSpan Duration { get; init; }
    
    /// <summary>Whether rollback was performed.</summary>
    public bool RolledBack { get; init; }
    
    /// <summary>Validation results.</summary>
    public ValidationSummary? Validation { get; init; }
    
    public static MergeResult Succeeded(string planId, string commitId, TimeSpan duration) => new()
    {
        Success = true,
        PlanId = planId,
        MergeCommitId = commitId,
        Duration = duration
    };
    
    public static MergeResult Failed(string planId, string error, TimeSpan duration, bool rolledBack = false) => new()
    {
        Success = false,
        PlanId = planId,
        Error = error,
        Duration = duration,
        RolledBack = rolledBack
    };
}

/// <summary>
/// Summary of post-merge validation.
/// </summary>
public sealed record ValidationSummary
{
    public bool BuildPassed { get; init; }
    public bool TestsPassed { get; init; }
    public int TestsRun { get; init; }
    public int TestsFailed { get; init; }
}

// Acode.Core/Domain/Merge/MergeExceptions.cs
namespace Acode.Core.Domain.Merge;

public abstract class MergeException : Exception
{
    protected MergeException(string message) : base(message) { }
    protected MergeException(string message, Exception inner) : base(message, inner) { }
}

public sealed class ConflictBlockedException : MergeException
{
    public IReadOnlyList<Conflict> Conflicts { get; }
    
    public ConflictBlockedException(IReadOnlyList<Conflict> conflicts)
        : base($"Merge blocked by {conflicts.Count} critical conflict(s)")
    {
        Conflicts = conflicts;
    }
}

public sealed class MergeInProgressException : MergeException
{
    public string PlanId { get; }
    
    public MergeInProgressException(string planId)
        : base($"Another merge is in progress: {planId}")
    {
        PlanId = planId;
    }
}

public sealed class MergeRollbackException : MergeException
{
    public MergeRollbackException(string message, Exception inner) 
        : base($"Rollback failed: {message}", inner) { }
}
```

### Conflict Detector

```csharp
// Acode.Application/Services/Merge/IConflictDetector.cs
namespace Acode.Application.Services.Merge;

/// <summary>
/// Detects conflicts between parallel changes.
/// </summary>
public interface IConflictDetector
{
    /// <summary>
    /// Analyzes changes for conflicts.
    /// </summary>
    Task<IReadOnlyList<Conflict>> DetectAsync(
        TaskId taskId,
        IReadOnlyList<FileChange> changes,
        CancellationToken ct = default);
}

// Acode.Application/Services/Merge/ConflictDetector.cs
namespace Acode.Application.Services.Merge;

public sealed class ConflictDetector : IConflictDetector
{
    private readonly IGitService _git;
    private readonly ITaskQueue _queue;
    private readonly ConflictOptions _options;
    private readonly ILogger<ConflictDetector> _logger;
    
    public ConflictDetector(
        IGitService git,
        ITaskQueue queue,
        IOptions<ConflictOptions> options,
        ILogger<ConflictDetector> logger)
    {
        _git = git;
        _queue = queue;
        _options = options.Value;
        _logger = logger;
    }
    
    public async Task<IReadOnlyList<Conflict>> DetectAsync(
        TaskId taskId,
        IReadOnlyList<FileChange> changes,
        CancellationToken ct = default)
    {
        var conflicts = new List<Conflict>();
        
        // Get other running/pending tasks
        var otherTasks = await _queue.ListAsync(
            new QueueFilter { Status = TaskStatus.Completed }, ct);
        
        // Get recent completed tasks that might conflict
        var recentTasks = otherTasks
            .Where(t => t.Id != taskId)
            .Where(t => t.CompletedAt > DateTimeOffset.UtcNow.AddHours(-1))
            .ToList();
        
        foreach (var change in changes)
        {
            ct.ThrowIfCancellationRequested();
            
            // Binary file check
            if (IsBinaryFile(change.Path))
            {
                var binaryConflicts = await CheckBinaryConflictsAsync(change, recentTasks, ct);
                conflicts.AddRange(binaryConflicts);
                continue;
            }
            
            // Lock file check
            if (IsLockFile(change.Path))
            {
                var lockConflicts = await CheckLockFileConflictsAsync(change, recentTasks, ct);
                conflicts.AddRange(lockConflicts);
                continue;
            }
            
            // Get line ranges for this change
            var localRange = await GetChangedLinesAsync(taskId, change.Path, ct);
            if (localRange == null) continue;
            
            // Check against other tasks' changes
            foreach (var other in recentTasks)
            {
                var otherChanges = other.Result?.AffectedFiles ?? Array.Empty<string>();
                if (!otherChanges.Contains(change.Path)) continue;
                
                var remoteRange = await GetChangedLinesAsync(other.Id, change.Path, ct);
                if (remoteRange == null) continue;
                
                // Check for overlap
                if (localRange.Value.Overlaps(remoteRange.Value))
                {
                    conflicts.Add(new Conflict
                    {
                        FilePath = change.Path,
                        LocalRange = localRange.Value,
                        RemoteRange = remoteRange.Value,
                        RemoteTaskId = other.Id,
                        Severity = ConflictSeverity.High,
                        Type = ConflictType.Overlap,
                        Suggestion = "Review both changes before merging"
                    });
                }
                // Check for proximity
                else if (localRange.Value.IsProximateTo(remoteRange.Value, _options.ProximityLines))
                {
                    conflicts.Add(new Conflict
                    {
                        FilePath = change.Path,
                        LocalRange = localRange.Value,
                        RemoteRange = remoteRange.Value,
                        RemoteTaskId = other.Id,
                        Severity = ConflictSeverity.Medium,
                        Type = ConflictType.Proximity,
                        Suggestion = "Changes are close together, review recommended"
                    });
                }
            }
        }
        
        _logger.LogInformation(
            "Detected {Count} conflicts for task {TaskId}",
            conflicts.Count, taskId);
        
        return conflicts;
    }
    
    private async Task<LineRange?> GetChangedLinesAsync(
        TaskId taskId, 
        string filePath, 
        CancellationToken ct)
    {
        try
        {
            var diff = await _git.GetDiffAsync(taskId.Value, filePath, ct);
            if (string.IsNullOrEmpty(diff)) return null;
            
            // Parse diff for line ranges
            var lines = ParseDiffLineRange(diff);
            return lines;
        }
        catch
        {
            return null;
        }
    }
    
    private static LineRange? ParseDiffLineRange(string diff)
    {
        // Parse unified diff format @@ -start,count +start,count @@
        var match = Regex.Match(diff, @"@@ -\d+,?\d* \+(\d+),?(\d*) @@");
        if (!match.Success) return null;
        
        var start = int.Parse(match.Groups[1].Value);
        var count = match.Groups[2].Success && match.Groups[2].Value.Length > 0
            ? int.Parse(match.Groups[2].Value)
            : 1;
        
        return new LineRange(start, start + count - 1);
    }
    
    private async Task<IReadOnlyList<Conflict>> CheckBinaryConflictsAsync(
        FileChange change,
        IReadOnlyList<QueuedTask> otherTasks,
        CancellationToken ct)
    {
        var conflicts = new List<Conflict>();
        
        foreach (var other in otherTasks)
        {
            var otherChanges = other.Result?.AffectedFiles ?? Array.Empty<string>();
            if (otherChanges.Contains(change.Path))
            {
                conflicts.Add(new Conflict
                {
                    FilePath = change.Path,
                    LocalRange = new LineRange(0, 0),
                    RemoteRange = new LineRange(0, 0),
                    RemoteTaskId = other.Id,
                    Severity = ConflictSeverity.Critical,
                    Type = ConflictType.Binary,
                    Suggestion = "Binary files cannot be merged automatically"
                });
            }
        }
        
        return conflicts;
    }
    
    private async Task<IReadOnlyList<Conflict>> CheckLockFileConflictsAsync(
        FileChange change,
        IReadOnlyList<QueuedTask> otherTasks,
        CancellationToken ct)
    {
        var conflicts = new List<Conflict>();
        
        foreach (var other in otherTasks)
        {
            var otherChanges = other.Result?.AffectedFiles ?? Array.Empty<string>();
            if (otherChanges.Contains(change.Path))
            {
                conflicts.Add(new Conflict
                {
                    FilePath = change.Path,
                    LocalRange = new LineRange(0, 0),
                    RemoteRange = new LineRange(0, 0),
                    RemoteTaskId = other.Id,
                    Severity = ConflictSeverity.High,
                    Type = ConflictType.LockFile,
                    Suggestion = "Regenerate lock file after merge"
                });
            }
        }
        
        return conflicts;
    }
    
    private static bool IsBinaryFile(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".ico" => true,
            ".dll" or ".exe" or ".so" or ".dylib" => true,
            ".zip" or ".tar" or ".gz" => true,
            ".pdf" or ".doc" or ".docx" => true,
            _ => false
        };
    }
    
    private static bool IsLockFile(string path)
    {
        var name = Path.GetFileName(path).ToLowerInvariant();
        return name switch
        {
            "package-lock.json" or "yarn.lock" or "pnpm-lock.yaml" => true,
            "packages.lock.json" or "paket.lock" => true,
            "composer.lock" or "gemfile.lock" or "poetry.lock" => true,
            _ => false
        };
    }
}

public sealed record ConflictOptions
{
    public int ProximityLines { get; init; } = 5;
    public TimeSpan LookbackWindow { get; init; } = TimeSpan.FromHours(1);
}
```

### Merge Coordinator

```csharp
// Acode.Application/Services/Merge/IMergeCoordinator.cs
namespace Acode.Application.Services.Merge;

/// <summary>
/// Coordinates merge operations for parallel tasks.
/// </summary>
public interface IMergeCoordinator
{
    /// <summary>
    /// Analyzes and creates a merge plan.
    /// </summary>
    Task<MergePlan> AnalyzeAsync(TaskId taskId, CancellationToken ct = default);
    
    /// <summary>
    /// Executes a merge plan.
    /// </summary>
    Task<MergeResult> ExecuteAsync(MergePlan plan, CancellationToken ct = default);
    
    /// <summary>
    /// Rolls back a failed merge.
    /// </summary>
    Task RollbackAsync(string planId, CancellationToken ct = default);
    
    /// <summary>
    /// Gets pending merges.
    /// </summary>
    Task<IReadOnlyList<MergePlan>> GetPendingAsync(CancellationToken ct = default);
}

// Acode.Application/Services/Merge/MergeCoordinator.cs
namespace Acode.Application.Services.Merge;

public sealed class MergeCoordinator : IMergeCoordinator
{
    private readonly IMergePlanner _planner;
    private readonly IMergeExecutor _executor;
    private readonly IConflictDetector _detector;
    private readonly MergeLock _lock;
    private readonly ILogger<MergeCoordinator> _logger;
    
    private readonly ConcurrentDictionary<string, MergePlan> _pendingPlans = new();
    
    public MergeCoordinator(
        IMergePlanner planner,
        IMergeExecutor executor,
        IConflictDetector detector,
        MergeLock mergeLock,
        ILogger<MergeCoordinator> logger)
    {
        _planner = planner;
        _executor = executor;
        _detector = detector;
        _lock = mergeLock;
        _logger = logger;
    }
    
    public async Task<MergePlan> AnalyzeAsync(TaskId taskId, CancellationToken ct = default)
    {
        _logger.LogInformation("Analyzing merge for task {TaskId}", taskId);
        
        // Get task changes
        var changes = await _planner.GetChangesAsync(taskId, ct);
        
        // Detect conflicts
        var conflicts = await _detector.DetectAsync(taskId, changes, ct);
        
        // Get dependencies
        var dependencies = await _planner.GetDependenciesAsync(taskId, ct);
        
        // Generate plan
        var plan = await _planner.CreatePlanAsync(taskId, changes, conflicts, dependencies, ct);
        
        _pendingPlans[plan.PlanId] = plan;
        
        _logger.LogInformation(
            "Merge plan created: {PlanId}, conflicts={Count}, canAuto={CanAuto}",
            plan.PlanId, plan.Conflicts.Count, plan.CanAutoMerge);
        
        return plan;
    }
    
    public async Task<MergeResult> ExecuteAsync(MergePlan plan, CancellationToken ct = default)
    {
        if (plan.IsBlocked)
        {
            var criticalConflicts = plan.Conflicts
                .Where(c => c.Severity == ConflictSeverity.Critical)
                .ToList();
            throw new ConflictBlockedException(criticalConflicts);
        }
        
        var stopwatch = Stopwatch.StartNew();
        
        // Acquire merge lock
        if (!await _lock.TryAcquireAsync(plan.PlanId, TimeSpan.FromMinutes(5), ct))
        {
            throw new MergeInProgressException(plan.PlanId);
        }
        
        try
        {
            _logger.LogInformation("Executing merge plan {PlanId}", plan.PlanId);
            
            // Execute merge steps
            foreach (var step in plan.Steps.OrderBy(s => s.Order))
            {
                ct.ThrowIfCancellationRequested();
                
                _logger.LogDebug("Executing step {Order}: {Type}", step.Order, step.Type);
                
                await _executor.ExecuteStepAsync(step, ct);
            }
            
            // Get merge commit
            var commitId = await _executor.GetMergeCommitAsync(ct);
            
            // Run validation if configured
            var validation = await _executor.ValidateAsync(ct);
            
            if (validation != null && !validation.BuildPassed)
            {
                _logger.LogWarning("Post-merge validation failed, rolling back");
                await RollbackAsync(plan.PlanId, ct);
                
                return MergeResult.Failed(
                    plan.PlanId,
                    "Post-merge validation failed",
                    stopwatch.Elapsed,
                    rolledBack: true);
            }
            
            _pendingPlans.TryRemove(plan.PlanId, out _);
            
            return MergeResult.Succeeded(plan.PlanId, commitId, stopwatch.Elapsed) with
            {
                Validation = validation
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Merge execution failed for plan {PlanId}", plan.PlanId);
            
            try
            {
                await RollbackAsync(plan.PlanId, ct);
                return MergeResult.Failed(plan.PlanId, ex.Message, stopwatch.Elapsed, rolledBack: true);
            }
            catch (Exception rollbackEx)
            {
                throw new MergeRollbackException(rollbackEx.Message, rollbackEx);
            }
        }
        finally
        {
            await _lock.ReleaseAsync(plan.PlanId, ct);
        }
    }
    
    public async Task RollbackAsync(string planId, CancellationToken ct = default)
    {
        _logger.LogWarning("Rolling back merge {PlanId}", planId);
        
        await _executor.RollbackAsync(ct);
        
        _pendingPlans.TryRemove(planId, out _);
    }
    
    public Task<IReadOnlyList<MergePlan>> GetPendingAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<MergePlan>>(_pendingPlans.Values.ToList());
    }
}
```

### Merge Lock

```csharp
// Acode.Application/Services/Merge/MergeLock.cs
namespace Acode.Application.Services.Merge;

/// <summary>
/// Serializes merge operations to prevent concurrent merges.
/// </summary>
public sealed class MergeLock
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private string? _currentPlanId;
    
    public async Task<bool> TryAcquireAsync(
        string planId, 
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        if (!await _semaphore.WaitAsync(timeout, ct))
            return false;
        
        _currentPlanId = planId;
        return true;
    }
    
    public Task ReleaseAsync(string planId, CancellationToken ct = default)
    {
        if (_currentPlanId == planId)
        {
            _currentPlanId = null;
            _semaphore.Release();
        }
        return Task.CompletedTask;
    }
    
    public bool IsLocked => _semaphore.CurrentCount == 0;
    public string? CurrentPlanId => _currentPlanId;
}
```

### CLI Commands

```csharp
// Acode.Cli/Commands/Merge/MergePlanCommand.cs
namespace Acode.Cli.Commands.Merge;

[Command("merge plan", Description = "Preview merge plan for a task")]
public sealed class MergePlanCommand : ICommand
{
    [CommandArgument(0, "<task-id>", Description = "Task ID to merge")]
    public string TaskId { get; init; } = string.Empty;
    
    [CommandOption("--json", Description = "Output as JSON")]
    public bool Json { get; init; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var coordinator = GetCoordinator(); // DI
        var taskId = new TaskId(TaskId);
        
        var plan = await coordinator.AnalyzeAsync(taskId);
        
        if (Json)
        {
            console.Output.WriteLine(JsonSerializer.Serialize(plan));
            return;
        }
        
        console.Output.WriteLine($"Merge Plan for {taskId}");
        console.Output.WriteLine(new string('=', 40));
        console.Output.WriteLine($"Strategy: {plan.Strategy}");
        console.Output.WriteLine();
        
        console.Output.WriteLine("Changes:");
        foreach (var change in plan.Changes)
        {
            console.Output.WriteLine($"  - {change.Path} ({change.ChangeType}, +{change.LinesAdded}/-{change.LinesRemoved})");
        }
        
        if (plan.Conflicts.Count > 0)
        {
            console.Output.WriteLine();
            console.Output.WriteLine("Conflicts:");
            foreach (var conflict in plan.Conflicts)
            {
                var icon = conflict.Severity switch
                {
                    ConflictSeverity.Critical => "✗",
                    ConflictSeverity.High => "⚠",
                    ConflictSeverity.Medium => "△",
                    _ => "○"
                };
                console.Output.WriteLine($"  {icon} {conflict.Severity}: {conflict.FilePath}");
                console.Output.WriteLine($"    {conflict.LocalRange} overlaps with task {conflict.RemoteTaskId} ({conflict.RemoteRange})");
                if (conflict.Suggestion != null)
                    console.Output.WriteLine($"    Suggestion: {conflict.Suggestion}");
            }
        }
        
        console.Output.WriteLine();
        console.Output.WriteLine("Steps:");
        foreach (var step in plan.Steps)
        {
            console.Output.WriteLine($"  {step.Order}. {step.Description}");
        }
        
        console.Output.WriteLine();
        if (plan.IsBlocked)
        {
            console.Error.WriteLine("✗ Merge is BLOCKED by critical conflicts");
            Environment.ExitCode = ExitCodes.MergeBlocked;
        }
        else if (plan.CanAutoMerge)
        {
            console.Output.WriteLine("✓ Ready for automatic merge");
        }
        else
        {
            console.Output.WriteLine("⚠ Review required before merge");
        }
    }
}

// Acode.Cli/Commands/Merge/MergeExecuteCommand.cs
namespace Acode.Cli.Commands.Merge;

[Command("merge execute", Description = "Execute a merge")]
public sealed class MergeExecuteCommand : ICommand
{
    [CommandArgument(0, "<task-id>", Description = "Task ID to merge")]
    public string TaskId { get; init; } = string.Empty;
    
    [CommandOption("--force", Description = "Skip confirmation")]
    public bool Force { get; init; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var coordinator = GetCoordinator();
        var taskId = new TaskId(TaskId);
        
        var plan = await coordinator.AnalyzeAsync(taskId);
        
        if (plan.IsBlocked)
        {
            console.Error.WriteLine("✗ Cannot merge: blocked by critical conflicts");
            Environment.ExitCode = ExitCodes.MergeBlocked;
            return;
        }
        
        if (!plan.CanAutoMerge && !Force)
        {
            console.Output.WriteLine($"⚠ {plan.Conflicts.Count} conflict(s) detected");
            console.Output.Write("Continue anyway? [y/N] ");
            var response = Console.ReadLine();
            if (response?.ToLowerInvariant() != "y")
            {
                console.Output.WriteLine("Merge cancelled");
                return;
            }
        }
        
        console.Output.WriteLine("Executing merge...");
        
        var result = await coordinator.ExecuteAsync(plan);
        
        if (result.Success)
        {
            console.Output.WriteLine($"✓ Merge successful");
            console.Output.WriteLine($"  Commit: {result.MergeCommitId}");
            console.Output.WriteLine($"  Duration: {result.Duration.TotalSeconds:F1}s");
        }
        else
        {
            console.Error.WriteLine($"✗ Merge failed: {result.Error}");
            if (result.RolledBack)
            {
                console.Output.WriteLine("  Changes were rolled back");
            }
            Environment.ExitCode = ExitCodes.MergeFailed;
        }
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| MERGE-001 | Conflict blocked merge |
| MERGE-002 | Merge in progress |
| MERGE-003 | Merge failed |
| MERGE-004 | Rollback failed |
| MERGE-005 | Validation failed |

### Implementation Checklist

- [ ] Create `ConflictSeverity` enum
- [ ] Create `Conflict` record with line ranges
- [ ] Create `LineRange` struct with overlap detection
- [ ] Create `MergePlan` record
- [ ] Create `MergeStrategy` enum
- [ ] Create `FileChange` and `MergeStep` records
- [ ] Create `MergeResult` record
- [ ] Create merge exception types
- [ ] Define `IConflictDetector` interface
- [ ] Implement `ConflictDetector` with overlap detection
- [ ] Add proximity detection
- [ ] Add binary file detection
- [ ] Add lock file detection
- [ ] Define `IMergeCoordinator` interface
- [ ] Implement `MergeCoordinator`
- [ ] Implement `MergeLock` with SemaphoreSlim
- [ ] Define `IMergePlanner` interface
- [ ] Define `IMergeExecutor` interface
- [ ] Implement merge execution with git
- [ ] Add rollback support
- [ ] Add post-merge validation
- [ ] Create `MergePlanCommand` CLI
- [ ] Create `MergeExecuteCommand` CLI
- [ ] Create `MergeRollbackCommand` CLI
- [ ] Register in DI
- [ ] Write unit tests for conflict detection
- [ ] Write integration tests for merge

### Rollout Plan

1. **Phase 1: Domain Models** (Day 1)
   - Create all records and enums
   - Create LineRange with overlap logic
   - Unit test models

2. **Phase 2: Conflict Detection** (Day 2)
   - Implement detector
   - Add overlap/proximity logic
   - Add binary/lock file handling

3. **Phase 3: Coordinator** (Day 2-3)
   - Implement coordinator
   - Add merge lock
   - Add pending plan tracking

4. **Phase 4: Execution** (Day 3)
   - Implement planner
   - Implement executor
   - Add rollback

5. **Phase 5: CLI** (Day 3)
   - Plan command
   - Execute command
   - Manual testing

6. **Phase 6: Integration** (Day 4)
   - Integration tests
   - Parallel merge tests
   - Documentation

---

**End of Task 028 Specification**