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

### File Structure

```
src/
├── Acode.Core/
│   └── Domain/
│       └── Git/
│           ├── CleanupPolicy.cs         # Policy configuration
│           └── CleanupResult.cs         # Cleanup operation result
│
├── Acode.Application/
│   └── Services/
│       └── Git/
│           ├── IWorktreeCleanupService.cs   # Service interface
│           ├── WorktreeCleanupService.cs    # Implementation
│           ├── CleanupCandidateEvaluator.cs # Candidate selection logic
│           └── CleanupScheduler.cs          # Background scheduling
│
├── Acode.Infrastructure/
│   └── Services/
│       └── DiskSpaceChecker.cs          # Disk usage monitoring
│
└── Acode.Cli/
    └── Commands/
        └── Worktree/
            └── WorktreeCleanupCommand.cs

tests/
├── Acode.Application.Tests/
│   └── Services/
│       └── Git/
│           ├── WorktreeCleanupServiceTests.cs
│           └── CleanupCandidateEvaluatorTests.cs
│
└── Acode.Integration.Tests/
    └── Git/
        └── WorktreeCleanupIntegrationTests.cs
```

### Domain Models

```csharp
// Acode.Core/Domain/Git/CleanupPolicy.cs
namespace Acode.Core.Domain.Git;

/// <summary>
/// Configuration for automatic worktree cleanup.
/// </summary>
public sealed record CleanupPolicy
{
    /// <summary>
    /// Whether automatic cleanup is enabled.
    /// </summary>
    public bool Enabled { get; init; } = true;
    
    /// <summary>
    /// Maximum age in days before a worktree becomes a cleanup candidate.
    /// </summary>
    public int MaxAgeDays { get; init; } = 7;
    
    /// <summary>
    /// Maximum number of worktrees before oldest become cleanup candidates.
    /// </summary>
    public int MaxWorktrees { get; init; } = 10;
    
    /// <summary>
    /// Whether to clean up worktree immediately when task completes.
    /// </summary>
    public bool CleanupOnComplete { get; init; }
    
    /// <summary>
    /// Whether to protect worktrees with uncommitted changes from cleanup.
    /// </summary>
    public bool ProtectUncommitted { get; init; } = true;
    
    /// <summary>
    /// Whether to protect worktrees with active tasks from cleanup.
    /// </summary>
    public bool ProtectActive { get; init; } = true;
    
    /// <summary>
    /// Minimum number of worktrees to always keep.
    /// </summary>
    public int MinKeep { get; init; } = 2;
    
    /// <summary>
    /// Interval in minutes between automatic cleanup runs.
    /// </summary>
    public int ScheduleMinutes { get; init; } = 60;
    
    /// <summary>
    /// Disk usage percentage that triggers emergency cleanup.
    /// </summary>
    public int DiskThresholdPercent { get; init; } = 90;
    
    /// <summary>
    /// Default policy with safe defaults.
    /// </summary>
    public static CleanupPolicy Default => new();
    
    /// <summary>
    /// Validates the policy configuration.
    /// </summary>
    /// <returns>List of validation errors, empty if valid.</returns>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();
        
        if (MaxAgeDays < 1)
            errors.Add("MaxAgeDays must be at least 1");
        
        if (MaxWorktrees < 1)
            errors.Add("MaxWorktrees must be at least 1");
        
        if (MinKeep < 0)
            errors.Add("MinKeep cannot be negative");
        
        if (MinKeep >= MaxWorktrees)
            errors.Add("MinKeep must be less than MaxWorktrees");
        
        if (ScheduleMinutes < 1)
            errors.Add("ScheduleMinutes must be at least 1");
        
        if (DiskThresholdPercent < 50 || DiskThresholdPercent > 99)
            errors.Add("DiskThresholdPercent must be between 50 and 99");
        
        return errors;
    }
}

// Acode.Core/Domain/Git/CleanupResult.cs
namespace Acode.Core.Domain.Git;

/// <summary>
/// Result of a cleanup operation.
/// </summary>
public sealed record CleanupResult
{
    /// <summary>Number of worktrees successfully removed.</summary>
    public int RemovedCount { get; init; }
    
    /// <summary>Number of worktrees skipped (protected or failed evaluation).</summary>
    public int SkippedCount { get; init; }
    
    /// <summary>Number of worktrees that failed to remove.</summary>
    public int ErrorCount { get; init; }
    
    /// <summary>Paths of successfully removed worktrees.</summary>
    public required IReadOnlyList<string> RemovedPaths { get; init; }
    
    /// <summary>Details of skipped worktrees.</summary>
    public required IReadOnlyList<SkippedWorktree> SkippedWorktrees { get; init; }
    
    /// <summary>Errors encountered during cleanup.</summary>
    public required IReadOnlyList<CleanupError> Errors { get; init; }
    
    /// <summary>Total duration of cleanup operation.</summary>
    public TimeSpan Duration { get; init; }
    
    /// <summary>Whether this was a dry-run (no actual removals).</summary>
    public bool IsDryRun { get; init; }
    
    /// <summary>Reason the cleanup was triggered.</summary>
    public CleanupTrigger Trigger { get; init; }
    
    /// <summary>Disk usage percentage before cleanup.</summary>
    public int? DiskUsageBefore { get; init; }
    
    /// <summary>Disk usage percentage after cleanup.</summary>
    public int? DiskUsageAfter { get; init; }
    
    public bool Success => ErrorCount == 0;
    
    public static CleanupResult Empty(bool isDryRun = false) => new()
    {
        RemovedCount = 0,
        SkippedCount = 0,
        ErrorCount = 0,
        RemovedPaths = [],
        SkippedWorktrees = [],
        Errors = [],
        IsDryRun = isDryRun,
        Trigger = CleanupTrigger.Manual
    };
}

/// <summary>
/// Details of a worktree that was skipped during cleanup.
/// </summary>
public sealed record SkippedWorktree(
    string Path,
    SkipReason Reason,
    string? Details = null);

/// <summary>
/// Reason a worktree was skipped during cleanup.
/// </summary>
public enum SkipReason
{
    /// <summary>Worktree has an active task.</summary>
    ActiveTask,
    
    /// <summary>Worktree has uncommitted changes.</summary>
    UncommittedChanges,
    
    /// <summary>Worktree is within MinKeep threshold.</summary>
    MinKeepProtected,
    
    /// <summary>Worktree is not old enough.</summary>
    NotOldEnough,
    
    /// <summary>Worktree is locked.</summary>
    Locked,
    
    /// <summary>Main worktree cannot be removed.</summary>
    MainWorktree
}

/// <summary>
/// Error during worktree cleanup.
/// </summary>
public sealed record CleanupError(
    string Path,
    string Message,
    Exception? Exception = null);

/// <summary>
/// What triggered the cleanup.
/// </summary>
public enum CleanupTrigger
{
    /// <summary>Manual user request.</summary>
    Manual,
    
    /// <summary>Scheduled automatic cleanup.</summary>
    Scheduled,
    
    /// <summary>Disk threshold exceeded.</summary>
    DiskThreshold,
    
    /// <summary>Task completion with CleanupOnComplete enabled.</summary>
    TaskComplete
}

/// <summary>
/// Candidate worktree for cleanup with evaluation details.
/// </summary>
public sealed record CleanupCandidate
{
    public required Worktree Worktree { get; init; }
    public required WorktreeMapping? Mapping { get; init; }
    public required DateTimeOffset LastAccessed { get; init; }
    public required int AgeDays { get; init; }
    public required bool HasUncommittedChanges { get; init; }
    public required bool HasActiveTask { get; init; }
    public required bool IsProtected { get; init; }
    public required SkipReason? ProtectionReason { get; init; }
}
```

### Options

```csharp
// Acode.Application/Services/Git/CleanupOptions.cs
namespace Acode.Application.Services.Git;

/// <summary>
/// Options for a cleanup operation.
/// </summary>
public sealed record CleanupOptions
{
    /// <summary>
    /// If true, report what would be cleaned without removing.
    /// </summary>
    public bool DryRun { get; init; }
    
    /// <summary>
    /// Force cleanup even for protected worktrees.
    /// </summary>
    public bool Force { get; init; }
    
    /// <summary>
    /// Maximum number of worktrees to remove in one run.
    /// </summary>
    public int? MaxRemovals { get; init; }
    
    /// <summary>
    /// Trigger reason for logging/auditing.
    /// </summary>
    public CleanupTrigger Trigger { get; init; } = CleanupTrigger.Manual;
    
    /// <summary>
    /// Override policy for this run.
    /// </summary>
    public CleanupPolicy? PolicyOverride { get; init; }
}
```

### Service Interface

```csharp
// Acode.Application/Services/Git/IWorktreeCleanupService.cs
namespace Acode.Application.Services.Git;

/// <summary>
/// Service for automatic worktree cleanup.
/// </summary>
public interface IWorktreeCleanupService
{
    /// <summary>
    /// Runs cleanup according to configured policy.
    /// </summary>
    /// <param name="options">Optional cleanup options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the cleanup operation.</returns>
    Task<CleanupResult> RunCleanupAsync(
        CleanupOptions? options = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets worktrees that are candidates for cleanup.
    /// </summary>
    /// <param name="includeProtected">Whether to include protected worktrees.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of cleanup candidates with evaluation details.</returns>
    Task<IReadOnlyList<CleanupCandidate>> GetCandidatesAsync(
        bool includeProtected = true,
        CancellationToken ct = default);
    
    /// <summary>
    /// Gets the currently active cleanup policy.
    /// </summary>
    CleanupPolicy GetCurrentPolicy();
    
    /// <summary>
    /// Checks if disk threshold is exceeded.
    /// </summary>
    Task<bool> IsDiskThresholdExceededAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Cleans up worktree for a specific completed task.
    /// </summary>
    /// <param name="taskId">The completed task ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if worktree was removed, false if not found or protected.</returns>
    Task<bool> CleanupForTaskAsync(string taskId, CancellationToken ct = default);
}
```

### Candidate Evaluator

```csharp
// Acode.Application/Services/Git/CleanupCandidateEvaluator.cs
namespace Acode.Application.Services.Git;

/// <summary>
/// Evaluates worktrees as cleanup candidates according to policy.
/// </summary>
public sealed class CleanupCandidateEvaluator
{
    private readonly CleanupPolicy _policy;
    private readonly HashSet<string> _activeTaskIds;
    
    public CleanupCandidateEvaluator(
        CleanupPolicy policy,
        IEnumerable<string>? activeTaskIds = null)
    {
        _policy = policy;
        _activeTaskIds = new HashSet<string>(
            activeTaskIds ?? [],
            StringComparer.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Evaluates a worktree as a cleanup candidate.
    /// </summary>
    public CleanupCandidate Evaluate(
        Worktree worktree,
        WorktreeMapping? mapping,
        bool hasUncommittedChanges)
    {
        // Main worktree is always protected
        if (worktree.IsMain)
        {
            return CreateCandidate(worktree, mapping, hasUncommittedChanges,
                isProtected: true, reason: SkipReason.MainWorktree);
        }
        
        // Locked worktree is protected
        if (worktree.IsLocked)
        {
            return CreateCandidate(worktree, mapping, hasUncommittedChanges,
                isProtected: true, reason: SkipReason.Locked);
        }
        
        // Active task protection
        var hasActiveTask = mapping?.TaskId != null && 
                           _activeTaskIds.Contains(mapping.TaskId);
        
        if (_policy.ProtectActive && hasActiveTask)
        {
            return CreateCandidate(worktree, mapping, hasUncommittedChanges,
                isProtected: true, reason: SkipReason.ActiveTask);
        }
        
        // Uncommitted changes protection
        if (_policy.ProtectUncommitted && hasUncommittedChanges)
        {
            return CreateCandidate(worktree, mapping, hasUncommittedChanges,
                isProtected: true, reason: SkipReason.UncommittedChanges);
        }
        
        // Age check
        var lastAccessed = mapping?.LastAccessedAt ?? DateTimeOffset.UtcNow;
        var ageDays = (int)(DateTimeOffset.UtcNow - lastAccessed).TotalDays;
        
        if (ageDays < _policy.MaxAgeDays)
        {
            return CreateCandidate(worktree, mapping, hasUncommittedChanges,
                isProtected: true, reason: SkipReason.NotOldEnough,
                ageDays: ageDays, lastAccessed: lastAccessed,
                hasActiveTask: hasActiveTask);
        }
        
        // Not protected - eligible for cleanup
        return CreateCandidate(worktree, mapping, hasUncommittedChanges,
            isProtected: false, reason: null,
            ageDays: ageDays, lastAccessed: lastAccessed,
            hasActiveTask: hasActiveTask);
    }
    
    /// <summary>
    /// Orders candidates for cleanup (oldest first, respecting MinKeep).
    /// </summary>
    public IReadOnlyList<CleanupCandidate> OrderForCleanup(
        IEnumerable<CleanupCandidate> candidates,
        int totalWorktreeCount)
    {
        // Get eligible candidates (not protected)
        var eligible = candidates
            .Where(c => !c.IsProtected)
            .OrderByDescending(c => c.AgeDays) // Oldest first
            .ThenBy(c => c.LastAccessed)
            .ToList();
        
        // Calculate how many can be removed while respecting MinKeep
        var protectedCount = candidates.Count(c => c.IsProtected);
        var maxRemovable = Math.Max(0, totalWorktreeCount - protectedCount - _policy.MinKeep);
        
        // Return only the ones we can actually remove
        return eligible.Take(maxRemovable).ToList();
    }
    
    private CleanupCandidate CreateCandidate(
        Worktree worktree,
        WorktreeMapping? mapping,
        bool hasUncommittedChanges,
        bool isProtected,
        SkipReason? reason,
        int? ageDays = null,
        DateTimeOffset? lastAccessed = null,
        bool hasActiveTask = false)
    {
        var accessed = lastAccessed ?? mapping?.LastAccessedAt ?? DateTimeOffset.UtcNow;
        var days = ageDays ?? (int)(DateTimeOffset.UtcNow - accessed).TotalDays;
        
        return new CleanupCandidate
        {
            Worktree = worktree,
            Mapping = mapping,
            LastAccessed = accessed,
            AgeDays = days,
            HasUncommittedChanges = hasUncommittedChanges,
            HasActiveTask = hasActiveTask,
            IsProtected = isProtected,
            ProtectionReason = reason
        };
    }
}
```

### Cleanup Service Implementation

```csharp
// Acode.Application/Services/Git/WorktreeCleanupService.cs
namespace Acode.Application.Services.Git;

/// <summary>
/// Implements worktree cleanup with policy evaluation.
/// </summary>
public sealed class WorktreeCleanupService : IWorktreeCleanupService
{
    private readonly IWorktreeService _worktreeService;
    private readonly IWorktreeMappingRepository _mappingRepository;
    private readonly IGitService _gitService;
    private readonly IDiskSpaceChecker _diskChecker;
    private readonly IActiveTaskProvider _taskProvider;
    private readonly IOptions<CleanupPolicy> _policyOptions;
    private readonly ILogger<WorktreeCleanupService> _logger;
    private readonly SemaphoreSlim _cleanupLock = new(1, 1);
    
    public WorktreeCleanupService(
        IWorktreeService worktreeService,
        IWorktreeMappingRepository mappingRepository,
        IGitService gitService,
        IDiskSpaceChecker diskChecker,
        IActiveTaskProvider taskProvider,
        IOptions<CleanupPolicy> policyOptions,
        ILogger<WorktreeCleanupService> logger)
    {
        _worktreeService = worktreeService;
        _mappingRepository = mappingRepository;
        _gitService = gitService;
        _diskChecker = diskChecker;
        _taskProvider = taskProvider;
        _policyOptions = policyOptions;
        _logger = logger;
    }
    
    public CleanupPolicy GetCurrentPolicy() => _policyOptions.Value;
    
    public async Task<CleanupResult> RunCleanupAsync(
        CleanupOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new CleanupOptions();
        var policy = options.PolicyOverride ?? GetCurrentPolicy();
        
        // Prevent concurrent cleanups
        if (!await _cleanupLock.WaitAsync(TimeSpan.Zero, ct))
        {
            _logger.LogWarning("Cleanup already in progress, skipping");
            return CleanupResult.Empty(options.DryRun);
        }
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation(
                "Starting worktree cleanup (trigger: {Trigger}, dryRun: {DryRun})",
                options.Trigger, options.DryRun);
            
            var diskBefore = await _diskChecker.GetUsagePercentAsync(ct);
            
            // Get all candidates
            var candidates = await GetCandidatesAsync(includeProtected: true, ct);
            
            // Get non-main worktree count
            var worktreeCount = candidates.Count(c => !c.Worktree.IsMain);
            
            // Evaluate and order for cleanup
            var evaluator = new CleanupCandidateEvaluator(
                policy,
                await _taskProvider.GetActiveTaskIdsAsync(ct));
            
            var toRemove = evaluator.OrderForCleanup(candidates, worktreeCount);
            
            // Apply max removals limit
            if (options.MaxRemovals.HasValue)
            {
                toRemove = toRemove.Take(options.MaxRemovals.Value).ToList();
            }
            
            // Track results
            var removedPaths = new List<string>();
            var skipped = new List<SkippedWorktree>();
            var errors = new List<CleanupError>();
            
            // Add all protected to skipped
            foreach (var candidate in candidates.Where(c => c.IsProtected))
            {
                skipped.Add(new SkippedWorktree(
                    candidate.Worktree.Path,
                    candidate.ProtectionReason!.Value,
                    candidate.Mapping?.TaskId));
            }
            
            // Perform cleanup
            foreach (var candidate in toRemove)
            {
                ct.ThrowIfCancellationRequested();
                
                if (options.DryRun)
                {
                    removedPaths.Add(candidate.Worktree.Path);
                    _logger.LogInformation(
                        "[DRY-RUN] Would remove worktree: {Path}",
                        candidate.Worktree.Path);
                    continue;
                }
                
                try
                {
                    await _worktreeService.RemoveAsync(
                        new RemoveWorktreeOptions
                        {
                            Path = candidate.Worktree.Path,
                            Force = options.Force,
                            DeleteBranch = true,
                            ForceBranchDelete = options.Force
                        },
                        ct);
                    
                    // Remove mapping
                    await _mappingRepository.DeleteByPathAsync(candidate.Worktree.Path, ct);
                    
                    removedPaths.Add(candidate.Worktree.Path);
                    
                    _logger.LogInformation(
                        "Removed worktree: {Path} (age: {Age}d)",
                        candidate.Worktree.Path, candidate.AgeDays);
                }
                catch (Exception ex)
                {
                    errors.Add(new CleanupError(
                        candidate.Worktree.Path,
                        ex.Message,
                        ex));
                    
                    _logger.LogWarning(ex,
                        "Failed to remove worktree: {Path}",
                        candidate.Worktree.Path);
                }
            }
            
            stopwatch.Stop();
            
            var diskAfter = options.DryRun 
                ? diskBefore 
                : await _diskChecker.GetUsagePercentAsync(ct);
            
            var result = new CleanupResult
            {
                RemovedCount = removedPaths.Count,
                SkippedCount = skipped.Count,
                ErrorCount = errors.Count,
                RemovedPaths = removedPaths,
                SkippedWorktrees = skipped,
                Errors = errors,
                Duration = stopwatch.Elapsed,
                IsDryRun = options.DryRun,
                Trigger = options.Trigger,
                DiskUsageBefore = diskBefore,
                DiskUsageAfter = diskAfter
            };
            
            _logger.LogInformation(
                "Cleanup complete: removed={Removed}, skipped={Skipped}, errors={Errors}, duration={Duration}ms",
                result.RemovedCount, result.SkippedCount, result.ErrorCount,
                stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        finally
        {
            _cleanupLock.Release();
        }
    }
    
    public async Task<IReadOnlyList<CleanupCandidate>> GetCandidatesAsync(
        bool includeProtected = true,
        CancellationToken ct = default)
    {
        var worktrees = await _worktreeService.ListAsync(ct);
        var mappings = await _mappingRepository.ListAsync(ct);
        var activeTaskIds = await _taskProvider.GetActiveTaskIdsAsync(ct);
        
        var policy = GetCurrentPolicy();
        var evaluator = new CleanupCandidateEvaluator(policy, activeTaskIds);
        
        var candidates = new List<CleanupCandidate>();
        
        foreach (var worktree in worktrees)
        {
            // Find mapping for this worktree
            var mapping = mappings.FirstOrDefault(m => 
                string.Equals(m.WorktreePath, worktree.Path, StringComparison.OrdinalIgnoreCase));
            
            // Check for uncommitted changes
            var hasUncommitted = false;
            if (!worktree.IsPrunable && !worktree.IsMain)
            {
                try
                {
                    var status = await _gitService.StatusAsync(worktree.Path, ct);
                    hasUncommitted = status.HasChanges;
                }
                catch
                {
                    // Can't determine status - treat as having changes for safety
                    hasUncommitted = true;
                }
            }
            
            var candidate = evaluator.Evaluate(worktree, mapping, hasUncommitted);
            
            if (includeProtected || !candidate.IsProtected)
            {
                candidates.Add(candidate);
            }
        }
        
        return candidates;
    }
    
    public async Task<bool> IsDiskThresholdExceededAsync(CancellationToken ct = default)
    {
        var usage = await _diskChecker.GetUsagePercentAsync(ct);
        var threshold = GetCurrentPolicy().DiskThresholdPercent;
        return usage >= threshold;
    }
    
    public async Task<bool> CleanupForTaskAsync(string taskId, CancellationToken ct = default)
    {
        var mapping = await _mappingRepository.GetByTaskAsync(taskId, ct);
        if (mapping == null)
        {
            _logger.LogDebug("No worktree mapping found for task: {TaskId}", taskId);
            return false;
        }
        
        var policy = GetCurrentPolicy();
        if (!policy.CleanupOnComplete)
        {
            _logger.LogDebug(
                "CleanupOnComplete disabled, not cleaning up worktree for task: {TaskId}",
                taskId);
            return false;
        }
        
        try
        {
            await _worktreeService.RemoveAsync(
                new RemoveWorktreeOptions
                {
                    Path = mapping.WorktreePath,
                    Force = false, // Respect uncommitted changes
                    DeleteBranch = true
                },
                ct);
            
            await _mappingRepository.DeleteByPathAsync(mapping.WorktreePath, ct);
            
            _logger.LogInformation(
                "Cleaned up worktree for completed task: {TaskId} at {Path}",
                taskId, mapping.WorktreePath);
            
            return true;
        }
        catch (UncommittedChangesException)
        {
            _logger.LogWarning(
                "Cannot cleanup worktree for task {TaskId}: uncommitted changes",
                taskId);
            return false;
        }
    }
}
```

### Disk Space Checker

```csharp
// Acode.Infrastructure/Services/DiskSpaceChecker.cs
namespace Acode.Infrastructure.Services;

/// <summary>
/// Checks disk space usage.
/// </summary>
public interface IDiskSpaceChecker
{
    /// <summary>
    /// Gets the disk usage percentage for the workspace drive.
    /// </summary>
    Task<int> GetUsagePercentAsync(CancellationToken ct = default);
}

public sealed class DiskSpaceChecker : IDiskSpaceChecker
{
    private readonly string _workspacePath;
    private readonly ILogger<DiskSpaceChecker> _logger;
    
    public DiskSpaceChecker(
        IOptions<WorkspaceOptions> options,
        ILogger<DiskSpaceChecker> logger)
    {
        _workspacePath = options.Value.RootPath;
        _logger = logger;
    }
    
    public Task<int> GetUsagePercentAsync(CancellationToken ct = default)
    {
        try
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(_workspacePath)!);
            var usedBytes = driveInfo.TotalSize - driveInfo.AvailableFreeSpace;
            var usagePercent = (int)((usedBytes * 100) / driveInfo.TotalSize);
            
            return Task.FromResult(usagePercent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check disk space");
            return Task.FromResult(0); // Assume OK if can't check
        }
    }
}
```

### Cleanup Scheduler

```csharp
// Acode.Application/Services/Git/CleanupScheduler.cs
namespace Acode.Application.Services.Git;

/// <summary>
/// Schedules automatic worktree cleanup.
/// </summary>
public sealed class CleanupScheduler : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<CleanupPolicy> _policyMonitor;
    private readonly ILogger<CleanupScheduler> _logger;
    
    public CleanupScheduler(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<CleanupPolicy> policyMonitor,
        ILogger<CleanupScheduler> logger)
    {
        _scopeFactory = scopeFactory;
        _policyMonitor = policyMonitor;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cleanup scheduler started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var policy = _policyMonitor.CurrentValue;
                
                if (!policy.Enabled)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                    continue;
                }
                
                await Task.Delay(TimeSpan.FromMinutes(policy.ScheduleMinutes), stoppingToken);
                
                await RunScheduledCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in cleanup scheduler");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
        
        _logger.LogInformation("Cleanup scheduler stopped");
    }
    
    private async Task RunScheduledCleanupAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var cleanupService = scope.ServiceProvider
            .GetRequiredService<IWorktreeCleanupService>();
        
        // Check if disk threshold cleanup is needed
        var trigger = await cleanupService.IsDiskThresholdExceededAsync(ct)
            ? CleanupTrigger.DiskThreshold
            : CleanupTrigger.Scheduled;
        
        if (trigger == CleanupTrigger.DiskThreshold)
        {
            _logger.LogWarning("Disk threshold exceeded, running emergency cleanup");
        }
        
        var result = await cleanupService.RunCleanupAsync(
            new CleanupOptions { Trigger = trigger },
            ct);
        
        if (result.RemovedCount > 0)
        {
            _logger.LogInformation(
                "Scheduled cleanup removed {Count} worktrees",
                result.RemovedCount);
        }
    }
}
```

### CLI Command

```csharp
// Acode.Cli/Commands/Worktree/WorktreeCleanupCommand.cs
namespace Acode.Cli.Commands.Worktree;

[Command("worktree cleanup", Description = "Clean up old worktrees")]
public sealed class WorktreeCleanupCommand : ICommand
{
    [CommandOption("dry-run|n", Description = "Show what would be cleaned without removing")]
    public bool DryRun { get; init; }
    
    [CommandOption("force|f", Description = "Force cleanup including protected worktrees")]
    public bool Force { get; init; }
    
    [CommandOption("max", Description = "Maximum number of worktrees to remove")]
    public int? Max { get; init; }
    
    [CommandOption("json", Description = "Output as JSON")]
    public bool Json { get; init; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var service = GetCleanupService(); // DI
        
        var options = new CleanupOptions
        {
            DryRun = DryRun,
            Force = Force,
            MaxRemovals = Max,
            Trigger = CleanupTrigger.Manual
        };
        
        var result = await service.RunCleanupAsync(options);
        
        if (Json)
        {
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            console.Output.WriteLine(json);
            return;
        }
        
        var action = DryRun ? "Would remove" : "Removed";
        
        if (result.RemovedCount == 0 && result.ErrorCount == 0)
        {
            console.Output.WriteLine("No worktrees to clean up.");
            return;
        }
        
        if (result.RemovedPaths.Count > 0)
        {
            console.Output.WriteLine($"{action} {result.RemovedCount} worktrees:");
            foreach (var path in result.RemovedPaths)
            {
                console.Output.WriteLine($"  ✓ {path}");
            }
            console.Output.WriteLine();
        }
        
        if (result.SkippedWorktrees.Count > 0 && !DryRun)
        {
            console.Output.WriteLine($"Skipped {result.SkippedCount} protected worktrees:");
            foreach (var skip in result.SkippedWorktrees.Take(5))
            {
                console.Output.WriteLine($"  - {skip.Path} ({skip.Reason})");
            }
            if (result.SkippedWorktrees.Count > 5)
            {
                console.Output.WriteLine($"  ... and {result.SkippedWorktrees.Count - 5} more");
            }
            console.Output.WriteLine();
        }
        
        if (result.Errors.Count > 0)
        {
            console.Error.WriteLine($"Errors ({result.ErrorCount}):");
            foreach (var error in result.Errors)
            {
                console.Error.WriteLine($"  ✗ {error.Path}: {error.Message}");
            }
            Environment.ExitCode = ExitCodes.PartialFailure;
        }
        
        if (result.DiskUsageBefore.HasValue && result.DiskUsageAfter.HasValue)
        {
            console.Output.WriteLine(
                $"Disk usage: {result.DiskUsageBefore}% → {result.DiskUsageAfter}%");
        }
        
        console.Output.WriteLine($"Duration: {result.Duration.TotalMilliseconds:F0}ms");
    }
}

[Command("worktree candidates", Description = "Show cleanup candidates")]
public sealed class WorktreeCandidatesCommand : ICommand
{
    [CommandOption("all|a", Description = "Include protected worktrees")]
    public bool All { get; init; }
    
    [CommandOption("json", Description = "Output as JSON")]
    public bool Json { get; init; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var service = GetCleanupService();
        
        var candidates = await service.GetCandidatesAsync(includeProtected: All);
        
        if (Json)
        {
            var json = JsonSerializer.Serialize(candidates, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            console.Output.WriteLine(json);
            return;
        }
        
        var eligible = candidates.Where(c => !c.IsProtected).ToList();
        var protected_ = candidates.Where(c => c.IsProtected).ToList();
        
        if (eligible.Count > 0)
        {
            console.Output.WriteLine($"Eligible for cleanup ({eligible.Count}):");
            foreach (var c in eligible)
            {
                console.Output.WriteLine($"  {c.Worktree.Path}");
                console.Output.WriteLine($"    Age: {c.AgeDays}d, Branch: {c.Worktree.Branch}");
            }
            console.Output.WriteLine();
        }
        
        if (All && protected_.Count > 0)
        {
            console.Output.WriteLine($"Protected ({protected_.Count}):");
            foreach (var c in protected_)
            {
                console.Output.WriteLine($"  {c.Worktree.Path}");
                console.Output.WriteLine($"    Reason: {c.ProtectionReason}");
            }
        }
        
        if (candidates.Count == 0)
        {
            console.Output.WriteLine("No worktree candidates found.");
        }
    }
}
```

### Configuration Binding

```csharp
// Acode.Infrastructure/Configuration/CleanupPolicyConfiguration.cs
namespace Acode.Infrastructure.Configuration;

public static class CleanupPolicyConfiguration
{
    public static IServiceCollection AddCleanupPolicy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CleanupPolicy>(
            configuration.GetSection("worktree:cleanup"));
        
        services.AddSingleton<IDiskSpaceChecker, DiskSpaceChecker>();
        services.AddScoped<IWorktreeCleanupService, WorktreeCleanupService>();
        services.AddHostedService<CleanupScheduler>();
        
        return services;
    }
}
```

### Implementation Checklist

- [ ] Create `CleanupPolicy` record with validation
- [ ] Create `CleanupResult` with all fields
- [ ] Create `SkippedWorktree`, `CleanupError` records
- [ ] Create `CleanupTrigger` enum
- [ ] Create `CleanupCandidate` record
- [ ] Create `CleanupOptions` record
- [ ] Define `IWorktreeCleanupService` interface
- [ ] Implement `CleanupCandidateEvaluator.Evaluate`
- [ ] Implement `CleanupCandidateEvaluator.OrderForCleanup` with MinKeep
- [ ] Implement `IDiskSpaceChecker` interface
- [ ] Implement `DiskSpaceChecker` for Windows/Linux
- [ ] Implement `WorktreeCleanupService.RunCleanupAsync`
- [ ] Implement `WorktreeCleanupService.GetCandidatesAsync`
- [ ] Implement `WorktreeCleanupService.CleanupForTaskAsync`
- [ ] Add concurrent cleanup prevention with `SemaphoreSlim`
- [ ] Implement `CleanupScheduler` background service
- [ ] Add disk threshold emergency cleanup
- [ ] Create `WorktreeCleanupCommand` CLI
- [ ] Create `WorktreeCandidatesCommand` CLI
- [ ] Add configuration binding for policy
- [ ] Register services in DI
- [ ] Write unit tests for `CleanupCandidateEvaluator`
- [ ] Write unit tests for policy validation
- [ ] Write integration tests for cleanup service

### Rollout Plan

1. **Phase 1: Domain Models** (Day 1)
   - Create all records and enums
   - Unit test policy validation

2. **Phase 2: Candidate Evaluator** (Day 1)
   - Implement evaluation logic
   - Unit test all protection scenarios

3. **Phase 3: Cleanup Service** (Day 2)
   - Implement cleanup operations
   - Add concurrency protection
   - Integration test with mock services

4. **Phase 4: Scheduler** (Day 2)
   - Implement background service
   - Test scheduled execution

5. **Phase 5: CLI** (Day 3)
   - Implement CLI commands
   - Manual testing with real worktrees

6. **Phase 6: Integration** (Day 3)
   - End-to-end testing
   - Disk threshold testing
   - Performance validation

---

**End of Task 023.c Specification**