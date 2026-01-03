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

### NFR-016 to NFR-025

- NFR-016: Orphan detection MUST run at most every 5 minutes
- NFR-017: Cleanup MUST NOT block user operations
- NFR-018: Disk space warnings MUST trigger at 90% capacity
- NFR-019: Worktree count MUST be bounded per configuration
- NFR-020: Branch names MUST be unique across worktrees
- NFR-021: Creation failure MUST NOT leave partial state
- NFR-022: Removal MUST be idempotent
- NFR-023: Recovery MUST work after crash
- NFR-024: Mapping MUST be transactional
- NFR-025: Audit log MUST record all lifecycle events

---

## User Manual Documentation

### Quick Start

```bash
# Create worktree for a task
acode worktree create --task TASK-123

# Create worktree with custom branch
acode worktree create --task TASK-123 --branch feature/my-feature

# List worktrees
acode worktree list

# Show worktree details
acode worktree show --task TASK-123

# Remove worktree
acode worktree remove --task TASK-123

# Remove with force (discards uncommitted changes)
acode worktree remove --task TASK-123 --force

# Prune orphaned worktrees
acode worktree prune
```

### Configuration

```yaml
worktree:
  # Base directory for worktrees (relative to repo root)
  basePath: .acode/worktrees
  
  # Prefix for auto-generated branch names
  branchPrefix: acode-task-
  
  # Maximum number of worktrees
  maxWorktrees: 10
  
  # Maximum age before cleanup eligible (days)
  maxAgeDays: 7
  
  # Cleanup worktree when task completes
  cleanupOnComplete: false
  
  # Warn about uncommitted changes before cleanup
  warnUncommitted: true
  
  # Orphan detection interval (minutes)
  orphanCheckIntervalMinutes: 5
```

### Programmatic Usage

```csharp
public class TaskExecutor
{
    private readonly IWorktreeService _worktrees;
    
    public async Task ExecuteInIsolationAsync(TaskId taskId)
    {
        // Get or create worktree for task
        var worktree = await _worktrees.GetForTaskAsync(taskId);
        
        if (worktree is null)
        {
            worktree = await _worktrees.CreateAsync(repoPath, new CreateWorktreeOptions
            {
                TaskId = taskId,
                BranchName = $"acode-task-{taskId.Value}"
            });
        }
        
        // Execute task in worktree
        await ExecuteAsync(taskId, worktree.Path);
        
        // Optionally cleanup
        if (cleanupOnComplete)
        {
            await _worktrees.RemoveAsync(worktree.Path);
        }
    }
}
```

### Isolation Benefits

| Benefit | Description |
|---------|-------------|
| Parallel Development | Multiple tasks work on different branches simultaneously |
| Clean Rollback | Failed tasks discarded without affecting main worktree |
| Experiment Freely | Try risky changes without polluting main checkout |
| Conflict Avoidance | Each task has its own files, no merge conflicts during work |
| Easy Review | Complete task branch ready for PR |

### Troubleshooting

**Q: "Worktree path already exists"**

Remove the existing worktree or use a different path:
```bash
acode worktree remove --path /path/to/worktree
```

**Q: "Branch already checked out in another worktree"**

Each branch can only be in one worktree. Use a different branch:
```bash
acode worktree create --task TASK-123 --branch feature/task-123-v2
```

**Q: "Maximum worktrees exceeded"**

Increase limit or prune old worktrees:
```bash
acode worktree prune --older-than 3d
```

**Q: "Uncommitted changes would be lost"**

Commit or stash changes before removal:
```bash
cd /path/to/worktree
git add . && git commit -m "WIP"
acode worktree remove --task TASK-123
```

---

## Acceptance Criteria / Definition of Done

### Core Functionality

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

### Path Management

- [ ] AC-013: Paths are sanitized
- [ ] AC-014: Path traversal blocked
- [ ] AC-015: Base path enforced
- [ ] AC-016: Unique paths generated

### Concurrency

- [ ] AC-017: Concurrent create operations safe
- [ ] AC-018: Concurrent remove operations safe
- [ ] AC-019: No deadlocks under load
- [ ] AC-020: Race conditions prevented

### Cleanup

- [ ] AC-021: Age-based cleanup works
- [ ] AC-022: Count-based cleanup works
- [ ] AC-023: Uncommitted changes detected
- [ ] AC-024: Force cleanup works
- [ ] AC-025: Orphan cleanup works

---

## Testing Requirements

### Unit Tests

| Test ID | Method | Scenario |
|---------|--------|----------|
| UT-001 | `GenerateWorktreePath_WithTaskId_ReturnsValidPath` | Path generation |
| UT-002 | `GenerateBranchName_WithTaskId_ReturnsConventionalName` | Branch naming |
| UT-003 | `SaveMapping_WithValidData_PersistsToDb` | Mapping persistence |
| UT-004 | `LoadMapping_WithExistingTask_ReturnsWorktree` | Mapping retrieval |
| UT-005 | `DetectOrphans_WithStaleWorktrees_ReturnsOrphanList` | Orphan detection |
| UT-006 | `EvaluatePolicy_WithOldWorktrees_SelectsForCleanup` | Policy evaluation |
| UT-007 | `SanitizePath_WithTraversal_ThrowsException` | Path security |
| UT-008 | `ValidateBranchName_WithInvalidChars_ThrowsException` | Branch validation |

### Integration Tests

| Test ID | Scenario |
|---------|----------|
| IT-001 | Create worktree on real repo with new branch |
| IT-002 | Create worktree from existing branch |
| IT-003 | Remove worktree cleans up branch |
| IT-004 | Remove worktree preserves branch if configured |
| IT-005 | List worktrees matches git worktree list |
| IT-006 | Task mapping persists across restarts |
| IT-007 | Concurrent create operations don't conflict |
| IT-008 | Orphan detection finds stale worktrees |

### End-to-End Tests

| Test ID | Scenario |
|---------|----------|
| E2E-001 | `acode worktree create --task T1` creates worktree |
| E2E-002 | `acode worktree list` shows all worktrees |
| E2E-003 | `acode worktree remove --task T1` removes worktree |
| E2E-004 | Task isolation prevents interference |
| E2E-005 | Cleanup policy removes old worktrees |

### Performance/Benchmarks

| Benchmark | Target | Threshold |
|-----------|--------|-----------|
| Create worktree | <5s | <10s |
| Remove worktree | <2s | <5s |
| List 20 worktrees | <500ms | <1s |
| Task lookup | <50ms | <100ms |
| Orphan scan (20 worktrees) | <1s | <2s |

---

## User Verification Steps

1. **Verify worktree creation:**
   ```bash
   acode worktree create --task TEST-001
   ```
   Verify: Worktree created at `.acode/worktrees/TEST-001-YYYYMMDD-HHMMSS`

2. **Verify branch created:**
   ```bash
   git branch -a | grep TEST-001
   ```
   Verify: Branch `acode-task-TEST-001` exists

3. **Verify listing:**
   ```bash
   acode worktree list
   ```
   Verify: Shows worktree with task ID, path, branch

4. **Verify task lookup:**
   ```bash
   acode worktree show --task TEST-001
   ```
   Verify: Shows details for TEST-001 worktree

5. **Verify isolation:**
   ```bash
   cd <worktree-path>
   touch test-file.txt
   cd <main-repo>
   ls test-file.txt
   ```
   Verify: File exists in worktree but not main repo

6. **Verify removal:**
   ```bash
   acode worktree remove --task TEST-001
   ```
   Verify: Worktree directory deleted, mapping removed

7. **Verify orphan detection:**
   ```bash
   # Manually delete worktree directory
   rm -rf .acode/worktrees/TEST-002
   acode worktree prune --dry-run
   ```
   Verify: Orphan detected and reported

8. **Verify max worktree limit:**
   ```yaml
   # Set maxWorktrees: 2
   ```
   ```bash
   acode worktree create --task T1
   acode worktree create --task T2
   acode worktree create --task T3
   ```
   Verify: Third creation fails with clear error

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Worktree/
│       ├── Worktree.cs
│       ├── WorktreeId.cs
│       ├── WorktreeMapping.cs
│       └── CleanupPolicy.cs
├── Acode.Application/
│   └── Worktree/
│       ├── IWorktreeService.cs
│       ├── WorktreeOptions.cs
│       └── Exceptions/
│           ├── WorktreeException.cs
│           ├── WorktreeExistsException.cs
│           ├── BranchInUseException.cs
│           ├── MaxWorktreesExceededException.cs
│           └── UncommittedChangesException.cs
├── Acode.Infrastructure/
│   └── Worktree/
│       ├── WorktreeService.cs
│       ├── WorktreeMappingRepository.cs
│       ├── WorktreePathGenerator.cs
│       ├── OrphanDetector.cs
│       └── CleanupPolicyEnforcer.cs
└── Acode.Cli/
    └── Commands/
        └── Worktree/
            ├── WorktreeCreateCommand.cs
            ├── WorktreeListCommand.cs
            ├── WorktreeRemoveCommand.cs
            ├── WorktreeShowCommand.cs
            └── WorktreePruneCommand.cs
```

### Domain Models

```csharp
// Worktree.cs
namespace Acode.Domain.Worktree;

public sealed record Worktree
{
    public required WorktreeId Id { get; init; }
    public required string Path { get; init; }
    public required string Branch { get; init; }
    public required string CommitSha { get; init; }
    public TaskId? AssociatedTaskId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset LastAccessedAt { get; init; }
    public required bool IsMain { get; init; }
    
    public bool IsOrphan => AssociatedTaskId is null && !IsMain;
    public TimeSpan Age => DateTimeOffset.UtcNow - CreatedAt;
}

// WorktreeId.cs
public readonly record struct WorktreeId(string Value)
{
    public static WorktreeId Generate() => new(Guid.NewGuid().ToString("N")[..8]);
    public override string ToString() => Value;
}

// WorktreeMapping.cs
public sealed record WorktreeMapping
{
    public required TaskId TaskId { get; init; }
    public required WorktreeId WorktreeId { get; init; }
    public required string WorktreePath { get; init; }
    public required string BranchName { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

// CleanupPolicy.cs
public sealed record CleanupPolicy
{
    public int MaxWorktrees { get; init; } = 10;
    public TimeSpan MaxAge { get; init; } = TimeSpan.FromDays(7);
    public bool CleanupOnTaskComplete { get; init; }
    public bool WarnUncommitted { get; init; } = true;
    public bool PreserveBranch { get; init; }
}
```

### Core Interface

```csharp
// IWorktreeService.cs
namespace Acode.Application.Worktree;

public interface IWorktreeService
{
    Task<Worktree> CreateAsync(
        string repoPath, 
        CreateWorktreeOptions options, 
        CancellationToken ct = default);
    
    Task RemoveAsync(
        string worktreePath, 
        RemoveWorktreeOptions? options = null,
        CancellationToken ct = default);
    
    Task<IReadOnlyList<Worktree>> ListAsync(
        string repoPath, 
        CancellationToken ct = default);
    
    Task<Worktree?> GetForTaskAsync(
        TaskId taskId, 
        CancellationToken ct = default);
    
    Task<Worktree?> GetAsync(
        string path, 
        CancellationToken ct = default);
    
    Task<bool> ExistsAsync(
        string path, 
        CancellationToken ct = default);
    
    Task<IReadOnlyList<Worktree>> DetectOrphansAsync(
        string repoPath,
        CancellationToken ct = default);
    
    Task<int> PruneAsync(
        string repoPath,
        PruneOptions options,
        CancellationToken ct = default);
}

// CreateWorktreeOptions.cs
public sealed record CreateWorktreeOptions
{
    public TaskId? TaskId { get; init; }
    public string? BranchName { get; init; }
    public string? StartPoint { get; init; }
    public bool CreateNewBranch { get; init; } = true;
}

// RemoveWorktreeOptions.cs
public sealed record RemoveWorktreeOptions
{
    public bool Force { get; init; }
    public bool PreserveBranch { get; init; }
}

// PruneOptions.cs
public sealed record PruneOptions
{
    public TimeSpan? OlderThan { get; init; }
    public bool DryRun { get; init; }
    public bool Force { get; init; }
}
```

### Infrastructure Implementation

```csharp
// WorktreeService.cs
namespace Acode.Infrastructure.Worktree;

public sealed class WorktreeService : IWorktreeService
{
    private readonly IGitService _git;
    private readonly IWorktreeMappingRepository _mappings;
    private readonly IWorktreePathGenerator _pathGenerator;
    private readonly ICleanupPolicyEnforcer _cleanup;
    private readonly IOptions<WorktreeConfiguration> _config;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<WorktreeService> _logger;
    
    public async Task<Worktree> CreateAsync(
        string repoPath, 
        CreateWorktreeOptions options, 
        CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            // Check worktree limit
            var existing = await ListAsync(repoPath, ct);
            if (existing.Count >= _config.Value.MaxWorktrees)
            {
                throw new MaxWorktreesExceededException(_config.Value.MaxWorktrees);
            }
            
            // Generate path and branch
            var worktreePath = _pathGenerator.Generate(repoPath, options.TaskId);
            var branchName = options.BranchName ?? 
                $"{_config.Value.BranchPrefix}{options.TaskId?.Value ?? WorktreeId.Generate().Value}";
            
            // Validate branch not in use
            if (await IsBranchCheckedOutAsync(repoPath, branchName, ct))
            {
                throw new BranchInUseException(branchName);
            }
            
            // Create via git
            var args = new List<string> { "worktree", "add" };
            
            if (options.CreateNewBranch)
            {
                args.AddRange(new[] { "-b", branchName });
            }
            
            args.Add(worktreePath);
            
            if (!options.CreateNewBranch)
            {
                args.Add(branchName);
            }
            else if (options.StartPoint is not null)
            {
                args.Add(options.StartPoint);
            }
            
            await ExecuteGitAsync(repoPath, args, ct);
            
            // Get commit SHA
            var sha = await GetHeadShaAsync(worktreePath, ct);
            
            // Create worktree record
            var worktree = new Worktree
            {
                Id = WorktreeId.Generate(),
                Path = worktreePath,
                Branch = branchName,
                CommitSha = sha,
                AssociatedTaskId = options.TaskId,
                CreatedAt = DateTimeOffset.UtcNow,
                LastAccessedAt = DateTimeOffset.UtcNow,
                IsMain = false
            };
            
            // Persist mapping if task associated
            if (options.TaskId is not null)
            {
                await _mappings.SaveAsync(new WorktreeMapping
                {
                    TaskId = options.TaskId.Value,
                    WorktreeId = worktree.Id,
                    WorktreePath = worktreePath,
                    BranchName = branchName,
                    CreatedAt = worktree.CreatedAt
                }, ct);
            }
            
            _logger.LogInformation(
                "Created worktree {Path} on branch {Branch} for task {Task}",
                worktreePath, branchName, options.TaskId);
            
            return worktree;
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public async Task RemoveAsync(
        string worktreePath, 
        RemoveWorktreeOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new RemoveWorktreeOptions();
        
        await _lock.WaitAsync(ct);
        try
        {
            // Check for uncommitted changes
            if (!options.Force)
            {
                var status = await _git.GetStatusAsync(worktreePath, ct);
                if (!status.IsClean)
                {
                    throw new UncommittedChangesException(worktreePath, status.TotalChangedFiles);
                }
            }
            
            // Get branch name before removal
            var branch = await _git.GetCurrentBranchAsync(worktreePath, ct);
            
            // Remove worktree
            var args = new List<string> { "worktree", "remove" };
            if (options.Force)
            {
                args.Add("--force");
            }
            args.Add(worktreePath);
            
            var repoPath = await FindMainRepoAsync(worktreePath, ct);
            await ExecuteGitAsync(repoPath, args, ct);
            
            // Delete branch unless preserved
            if (!options.PreserveBranch && branch is not null)
            {
                try
                {
                    await _git.DeleteBranchAsync(repoPath, branch, force: true, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete branch {Branch}", branch);
                }
            }
            
            // Remove mapping
            await _mappings.DeleteByPathAsync(worktreePath, ct);
            
            _logger.LogInformation("Removed worktree {Path}", worktreePath);
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public async Task<Worktree?> GetForTaskAsync(TaskId taskId, CancellationToken ct = default)
    {
        var mapping = await _mappings.GetByTaskIdAsync(taskId, ct);
        if (mapping is null)
            return null;
        
        return await GetAsync(mapping.WorktreePath, ct);
    }
    
    public async Task<IReadOnlyList<Worktree>> DetectOrphansAsync(
        string repoPath,
        CancellationToken ct = default)
    {
        var worktrees = await ListAsync(repoPath, ct);
        var mappings = await _mappings.GetAllAsync(ct);
        var mappedPaths = mappings.Select(m => m.WorktreePath).ToHashSet();
        
        return worktrees
            .Where(w => !w.IsMain && !mappedPaths.Contains(w.Path))
            .ToList();
    }
}

// WorktreePathGenerator.cs
namespace Acode.Infrastructure.Worktree;

public sealed class WorktreePathGenerator : IWorktreePathGenerator
{
    private readonly IOptions<WorktreeConfiguration> _config;
    
    public string Generate(string repoPath, TaskId? taskId)
    {
        var basePath = Path.Combine(repoPath, _config.Value.BasePath);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var name = taskId?.Value ?? WorktreeId.Generate().Value;
        
        var worktreePath = Path.Combine(basePath, $"{name}-{timestamp}");
        
        // Validate no path traversal
        var fullPath = Path.GetFullPath(worktreePath);
        var fullBase = Path.GetFullPath(basePath);
        
        if (!fullPath.StartsWith(fullBase))
        {
            throw new InvalidOperationException("Path traversal detected");
        }
        
        return fullPath;
    }
}
```

### CLI Commands

```csharp
// WorktreeCreateCommand.cs
namespace Acode.Cli.Commands.Worktree;

[Command("worktree create", Description = "Create a new worktree")]
public class WorktreeCreateCommand
{
    [Option("--task", Description = "Task ID to associate")]
    public string? TaskId { get; set; }
    
    [Option("--branch", Description = "Branch name (default: auto-generated)")]
    public string? Branch { get; set; }
    
    [Option("--from", Description = "Starting point (branch, tag, or commit)")]
    public string? From { get; set; }
    
    public async Task<int> ExecuteAsync(
        IWorktreeService worktrees,
        IConsole console,
        CancellationToken ct)
    {
        var repoPath = Directory.GetCurrentDirectory();
        
        var options = new CreateWorktreeOptions
        {
            TaskId = TaskId is not null ? new TaskId(TaskId) : null,
            BranchName = Branch,
            StartPoint = From,
            CreateNewBranch = true
        };
        
        var worktree = await worktrees.CreateAsync(repoPath, options, ct);
        
        console.WriteLine($"✓ Created worktree at {worktree.Path}");
        console.WriteLine($"  Branch: {worktree.Branch}");
        console.WriteLine($"  Commit: {worktree.CommitSha[..7]}");
        
        return 0;
    }
}

// WorktreeListCommand.cs
[Command("worktree list", Description = "List worktrees")]
public class WorktreeListCommand
{
    [Option("--json", Description = "Output as JSON")]
    public bool Json { get; set; }
    
    public async Task<int> ExecuteAsync(
        IWorktreeService worktrees,
        IConsole console,
        CancellationToken ct)
    {
        var repoPath = Directory.GetCurrentDirectory();
        var list = await worktrees.ListAsync(repoPath, ct);
        
        if (Json)
        {
            console.WriteLine(JsonSerializer.Serialize(list, JsonOptions.Pretty));
            return 0;
        }
        
        if (list.Count == 0)
        {
            console.WriteLine("No worktrees found.");
            return 0;
        }
        
        console.WriteLine("TASK       BRANCH                    PATH");
        console.WriteLine("─".PadRight(70, '─'));
        
        foreach (var wt in list)
        {
            var task = wt.AssociatedTaskId?.Value ?? "(none)";
            console.WriteLine($"{task,-10} {wt.Branch,-25} {wt.Path}");
        }
        
        return 0;
    }
}
```

### Error Codes

| Code | Name | Description | Recovery |
|------|------|-------------|----------|
| WT_001 | PathExists | Worktree path already exists | Use different path or remove existing |
| WT_002 | BranchInUse | Branch checked out elsewhere | Use different branch name |
| WT_003 | MaxExceeded | Maximum worktrees reached | Prune old worktrees |
| WT_004 | UncommittedChanges | Worktree has uncommitted changes | Commit or use --force |
| WT_005 | NotFound | Worktree not found | Verify path exists |
| WT_006 | PathTraversal | Invalid path detected | Use valid path |
| WT_007 | MappingNotFound | Task mapping not found | Verify task ID |
| WT_008 | CleanupFailed | Failed to cleanup worktree | Check permissions, use --force |

### Implementation Checklist

- [ ] Define domain models (Worktree, WorktreeId, WorktreeMapping)
- [ ] Define IWorktreeService interface
- [ ] Implement WorktreeService with git worktree commands
- [ ] Implement WorktreeMappingRepository for persistence
- [ ] Implement WorktreePathGenerator with security checks
- [ ] Implement OrphanDetector for stale worktree detection
- [ ] Implement CleanupPolicyEnforcer for age/count limits
- [ ] Add exception types for all error cases
- [ ] Implement CLI commands (create, list, remove, show, prune)
- [ ] Add concurrency protection with SemaphoreSlim
- [ ] Add unit tests for path generation and policy
- [ ] Add integration tests with real git repos
- [ ] Add E2E tests for CLI workflow
- [ ] Document configuration options

### Rollout Plan

| Phase | Action | Validation |
|-------|--------|------------|
| 1 | Implement domain models | Compile check |
| 2 | Define IWorktreeService | Compile check |
| 3 | Implement path generator | Path tests pass |
| 4 | Implement WorktreeService | Create/remove works |
| 5 | Implement mapping repository | Persistence works |
| 6 | Implement orphan detector | Detection tests pass |
| 7 | Implement cleanup policy | Policy tests pass |
| 8 | Add CLI commands | E2E tests pass |
| 9 | Add concurrency protection | Concurrent tests pass |
| 10 | Performance testing | Benchmarks pass |
| 11 | Documentation | User manual complete |

---

**End of Task 023 Specification**