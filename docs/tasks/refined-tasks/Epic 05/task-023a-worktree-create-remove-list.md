# Task 023.a: worktree create/remove/list

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 5 – Git Integration Layer  
**Dependencies:** Task 023 (Worktree-per-Task), Task 022 (Git Tool Layer)  

---

## Description

Task 023.a implements the core worktree operations: create, remove, and list. These operations wrap Git's worktree commands with structured output and error handling.

Worktree creation MUST create both the worktree directory and a new branch. The path MUST be within the configured base directory. The branch name MUST follow naming conventions.

Worktree removal MUST delete the worktree directory and optionally the associated branch. Uncommitted changes MUST be detected before removal. Force removal MUST be available for abandoned work.

Worktree listing MUST enumerate all linked worktrees. Each entry MUST include path, branch, commit SHA, and status. Pruning stale entries MUST be supported.

### Business Value

These operations enable the worktree-per-task pattern. The agent can create isolated environments, work in them, and clean up afterward.

### Scope Boundaries

This task covers the Git worktree operations. Task mapping is in 023.b. Cleanup policies are in 023.c.

### Integration Points

- Task 023: IWorktreeService interface
- Task 022: Git branch operations
- Task 018: Command execution

### Failure Modes

- Path already exists → PathExistsException
- Branch in use → BranchInUseException
- Uncommitted changes → UncommittedChangesException
- Stale worktree → Prune or error

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Create | Add a new linked worktree |
| Remove | Delete a linked worktree |
| List | Enumerate all worktrees |
| Prune | Clean up stale worktree entries |
| Stale | Worktree with missing directory |
| Locked | Worktree protected from removal |

---

## Out of Scope

- Task mapping persistence (Task 023.b)
- Automatic cleanup policies (Task 023.c)
- Cross-worktree operations
- Worktree locking management

---

## Assumptions

### Technical Assumptions

1. **Git 2.20+** - Git version supports worktree features used
2. **Filesystem support** - Filesystem supports git worktree operations
3. **Sufficient disk space** - Space available for worktree creation
4. **Write permissions** - Agent has write access to worktree directory
5. **Single repository** - Worktrees for one git repository at a time

### Operational Assumptions

6. **Branch per worktree** - Each worktree has its own branch
7. **Unique paths** - No path collisions between worktrees
8. **Git index available** - Main repo .git directory accessible
9. **No conflicting locks** - Git index not locked during operations
10. **Worktree directory writable** - .acode/worktrees/ exists and is writable

### Integration Assumptions

11. **Task IDs unique** - Task IDs can be used for path generation
12. **Mapping service available** - Task 023.b provides persistence
13. **CLI integration** - Commands integrate with acode CLI framework
14. **Event emission** - Worktree events can be published

---

## Functional Requirements

### FR-001 to FR-025: Create Operation

- FR-001: `CreateAsync` MUST create linked worktree
- FR-002: Path MUST be specified
- FR-003: Branch MUST be specified or generated
- FR-004: New branch MUST be created if not exists
- FR-005: Existing branch MUST be checked out
- FR-006: `--detach` MUST create detached HEAD
- FR-007: `--force` MUST override safety checks
- FR-008: Path MUST be validated
- FR-009: Path MUST NOT exist before creation
- FR-010: Path MUST be within base directory
- FR-011: Branch name MUST be validated
- FR-012: Creation MUST be atomic
- FR-013: Failed creation MUST cleanup
- FR-014: Created worktree MUST be returned
- FR-015: Creation MUST be logged
- FR-016: Progress MUST be reportable
- FR-017: Submodule init MUST be optional
- FR-018: Lock MUST be settable on creation
- FR-019: Lock reason MUST be stored
- FR-020: Worktree MUST be immediately usable
- FR-021: Index MUST be initialized
- FR-022: Working tree MUST match branch HEAD
- FR-023: `.git` file MUST point to main repo
- FR-024: Concurrent creates MUST be safe
- FR-025: Create in progress MUST be detected

### FR-026 to FR-045: Remove Operation

- FR-026: `RemoveAsync` MUST remove worktree
- FR-027: Directory MUST be deleted
- FR-028: Git metadata MUST be cleaned
- FR-029: Uncommitted changes MUST block removal
- FR-030: `--force` MUST override safety check
- FR-031: Branch deletion MUST be optional
- FR-032: `--delete-branch` MUST remove branch
- FR-033: Merged branch MUST delete without force
- FR-034: Unmerged branch MUST require force
- FR-035: Locked worktree MUST block removal
- FR-036: `--unlock` MUST remove lock first
- FR-037: Non-existent worktree MUST return success
- FR-038: Stale worktree MUST be prunable
- FR-039: Removal MUST be logged
- FR-040: Removal MUST update internal state
- FR-041: Concurrent removals MUST be safe
- FR-042: Partial removal MUST be recoverable
- FR-043: Related refs MUST be cleaned
- FR-044: Admin directory MUST be cleaned
- FR-045: Parent directories MUST NOT be deleted

### FR-046 to FR-060: List Operation

- FR-046: `ListAsync` MUST return all worktrees
- FR-047: Main worktree MUST be included
- FR-048: Linked worktrees MUST be included
- FR-049: Each entry MUST include path
- FR-050: Each entry MUST include branch
- FR-051: Each entry MUST include commit SHA
- FR-052: Each entry MUST include locked status
- FR-053: Each entry MUST include prunable status
- FR-054: `--prune` MUST remove stale entries
- FR-055: Stale detection MUST check path exists
- FR-056: Listing MUST be fast (<500ms)
- FR-057: Large worktree counts MUST handle
- FR-058: Broken worktrees MUST be flagged
- FR-059: Listing MUST NOT modify state
- FR-060: Porcelain output MUST be parsed

---

## Non-Functional Requirements

- NFR-001: Create MUST complete in <5s
- NFR-002: Remove MUST complete in <2s
- NFR-003: List MUST complete in <500ms
- NFR-004: Concurrent operations MUST be safe
- NFR-005: Path sanitization MUST prevent injection
- NFR-006: Symlinks MUST NOT escape containment
- NFR-007: File permissions MUST be respected
- NFR-008: Large file handling MUST work
- NFR-009: Network paths MUST be blocked
- NFR-010: Temp files MUST be cleaned up

---

## User Manual Documentation

### Commands

```bash
# Create worktree
acode worktree create <path> --branch <name>

# Remove worktree
acode worktree remove <path> [--force] [--delete-branch]

# List worktrees
acode worktree list [--prune]
```

### Examples

```bash
# Create isolated worktree
acode worktree create .acode/worktrees/task-123 --branch feature/task-123

# List all worktrees
acode worktree list

# Remove with branch cleanup
acode worktree remove .acode/worktrees/task-123 --delete-branch
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Create worktree works
- [ ] AC-002: Branch created automatically
- [ ] AC-003: Path validation works
- [ ] AC-004: Remove worktree works
- [ ] AC-005: Uncommitted check works
- [ ] AC-006: Force removal works
- [ ] AC-007: List shows all worktrees
- [ ] AC-008: Stale detection works
- [ ] AC-009: Prune removes stale entries
- [ ] AC-010: CLI commands work

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Test path validation
- [ ] UT-002: Test branch name generation
- [ ] UT-003: Test list parsing
- [ ] UT-004: Test stale detection

### Integration Tests

- [ ] IT-001: Create on real repo
- [ ] IT-002: Remove on real repo
- [ ] IT-003: List on real repo
- [ ] IT-004: Prune stale entries

### End-to-End Tests

- [ ] E2E-001: CLI create
- [ ] E2E-002: CLI remove
- [ ] E2E-003: CLI list

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Core/
│   └── Domain/
│       └── Git/
│           ├── Worktree.cs              # Worktree entity
│           ├── WorktreeId.cs            # Strongly-typed ID
│           ├── WorktreeState.cs         # Worktree state enum
│           └── WorktreeException.cs     # Worktree-specific exceptions
│
├── Acode.Application/
│   └── Services/
│       └── Git/
│           ├── IWorktreeService.cs      # Service interface
│           ├── WorktreeService.cs       # Core implementation
│           ├── WorktreePathGenerator.cs # Path generation logic
│           └── WorktreeParser.cs        # Parse worktree list output
│
├── Acode.Infrastructure/
│   └── Git/
│       └── WorktreeCommands.cs          # Git command execution
│
└── Acode.Cli/
    └── Commands/
        └── Worktree/
            ├── WorktreeCreateCommand.cs
            ├── WorktreeRemoveCommand.cs
            ├── WorktreeListCommand.cs
            └── WorktreePruneCommand.cs

tests/
├── Acode.Core.Tests/
│   └── Domain/
│       └── Git/
│           ├── WorktreeTests.cs
│           └── WorktreeIdTests.cs
│
├── Acode.Application.Tests/
│   └── Services/
│       └── Git/
│           ├── WorktreeServiceTests.cs
│           ├── WorktreePathGeneratorTests.cs
│           └── WorktreeParserTests.cs
│
└── Acode.Integration.Tests/
    └── Git/
        └── WorktreeIntegrationTests.cs
```

### Domain Models

```csharp
// Acode.Core/Domain/Git/WorktreeId.cs
namespace Acode.Core.Domain.Git;

/// <summary>
/// Strongly-typed worktree identifier based on normalized path.
/// </summary>
public readonly record struct WorktreeId
{
    public string Value { get; }
    
    public WorktreeId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = NormalizePath(value);
    }
    
    private static string NormalizePath(string path)
    {
        // Normalize to forward slashes, lowercase for comparison
        return path.Replace('\\', '/').TrimEnd('/');
    }
    
    public override string ToString() => Value;
    
    public static implicit operator string(WorktreeId id) => id.Value;
}

// Acode.Core/Domain/Git/WorktreeState.cs
namespace Acode.Core.Domain.Git;

/// <summary>
/// Represents the state of a worktree.
/// </summary>
public enum WorktreeState
{
    /// <summary>Worktree is valid and usable.</summary>
    Valid,
    
    /// <summary>Worktree is locked (protected from removal).</summary>
    Locked,
    
    /// <summary>Worktree directory is missing (stale entry).</summary>
    Prunable,
    
    /// <summary>Worktree has detached HEAD.</summary>
    Detached
}

// Acode.Core/Domain/Git/Worktree.cs
namespace Acode.Core.Domain.Git;

/// <summary>
/// Represents a Git worktree.
/// </summary>
public sealed class Worktree
{
    public WorktreeId Id { get; }
    
    /// <summary>Absolute path to the worktree directory.</summary>
    public string Path { get; }
    
    /// <summary>HEAD commit SHA.</summary>
    public string CommitSha { get; }
    
    /// <summary>Branch name (null if detached HEAD).</summary>
    public string? Branch { get; }
    
    /// <summary>Whether this is the main (bare) worktree.</summary>
    public bool IsMain { get; }
    
    /// <summary>Current state of the worktree.</summary>
    public WorktreeState State { get; }
    
    /// <summary>Lock reason if locked.</summary>
    public string? LockReason { get; }
    
    public Worktree(
        string path,
        string commitSha,
        string? branch,
        bool isMain,
        WorktreeState state,
        string? lockReason = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(commitSha);
        
        Path = path;
        Id = new WorktreeId(path);
        CommitSha = commitSha;
        Branch = branch;
        IsMain = isMain;
        State = state;
        LockReason = lockReason;
    }
    
    public bool IsPrunable => State == WorktreeState.Prunable;
    public bool IsLocked => State == WorktreeState.Locked;
    public bool IsDetached => Branch is null;
}

// Acode.Core/Domain/Git/WorktreeException.cs
namespace Acode.Core.Domain.Git;

/// <summary>
/// Base exception for worktree operations.
/// </summary>
public class WorktreeException : Exception
{
    public string WorktreePath { get; }
    
    public WorktreeException(string path, string message) 
        : base(message)
    {
        WorktreePath = path;
    }
    
    public WorktreeException(string path, string message, Exception inner) 
        : base(message, inner)
    {
        WorktreePath = path;
    }
}

public sealed class PathExistsException : WorktreeException
{
    public PathExistsException(string path) 
        : base(path, $"Path already exists: {path}") { }
}

public sealed class PathOutsideBaseException : WorktreeException
{
    public string BaseDirectory { get; }
    
    public PathOutsideBaseException(string path, string baseDir) 
        : base(path, $"Path '{path}' is outside base directory '{baseDir}'")
    {
        BaseDirectory = baseDir;
    }
}

public sealed class BranchInUseException : WorktreeException
{
    public string BranchName { get; }
    public string UsingWorktreePath { get; }
    
    public BranchInUseException(string path, string branch, string usingPath) 
        : base(path, $"Branch '{branch}' is already checked out in '{usingPath}'")
    {
        BranchName = branch;
        UsingWorktreePath = usingPath;
    }
}

public sealed class UncommittedChangesException : WorktreeException
{
    public IReadOnlyList<string> ChangedFiles { get; }
    
    public UncommittedChangesException(string path, IReadOnlyList<string> files) 
        : base(path, $"Worktree has {files.Count} uncommitted changes")
    {
        ChangedFiles = files;
    }
}

public sealed class WorktreeLockedException : WorktreeException
{
    public string? LockReason { get; }
    
    public WorktreeLockedException(string path, string? reason) 
        : base(path, $"Worktree is locked{(reason != null ? $": {reason}" : "")}")
    {
        LockReason = reason;
    }
}

public sealed class WorktreeNotFoundException : WorktreeException
{
    public WorktreeNotFoundException(string path) 
        : base(path, $"Worktree not found: {path}") { }
}
```

### Create Options

```csharp
// Acode.Application/Services/Git/CreateWorktreeOptions.cs
namespace Acode.Application.Services.Git;

/// <summary>
/// Options for creating a worktree.
/// </summary>
public sealed record CreateWorktreeOptions
{
    /// <summary>Absolute path for the new worktree.</summary>
    public required string Path { get; init; }
    
    /// <summary>Branch name. If null, generated from path.</summary>
    public string? Branch { get; init; }
    
    /// <summary>Create new branch vs checkout existing.</summary>
    public bool CreateBranch { get; init; } = true;
    
    /// <summary>Base commit/branch for new branch. Defaults to HEAD.</summary>
    public string? StartPoint { get; init; }
    
    /// <summary>Create with detached HEAD instead of branch.</summary>
    public bool Detach { get; init; }
    
    /// <summary>Force creation even if branch exists elsewhere.</summary>
    public bool Force { get; init; }
    
    /// <summary>Lock worktree immediately after creation.</summary>
    public bool Lock { get; init; }
    
    /// <summary>Reason for locking.</summary>
    public string? LockReason { get; init; }
    
    /// <summary>Initialize submodules.</summary>
    public bool InitializeSubmodules { get; init; }
}

/// <summary>
/// Options for removing a worktree.
/// </summary>
public sealed record RemoveWorktreeOptions
{
    /// <summary>Worktree path to remove.</summary>
    public required string Path { get; init; }
    
    /// <summary>Force removal even with uncommitted changes.</summary>
    public bool Force { get; init; }
    
    /// <summary>Also delete the associated branch.</summary>
    public bool DeleteBranch { get; init; }
    
    /// <summary>Force branch deletion even if not merged.</summary>
    public bool ForceBranchDelete { get; init; }
}
```

### Service Interface

```csharp
// Acode.Application/Services/Git/IWorktreeService.cs
namespace Acode.Application.Services.Git;

/// <summary>
/// Service for managing Git worktrees.
/// </summary>
public interface IWorktreeService
{
    /// <summary>
    /// Creates a new worktree.
    /// </summary>
    /// <param name="options">Creation options.</param>
    /// <param name="progress">Optional progress reporter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created worktree.</returns>
    /// <exception cref="PathExistsException">Path already exists.</exception>
    /// <exception cref="PathOutsideBaseException">Path outside base directory.</exception>
    /// <exception cref="BranchInUseException">Branch checked out elsewhere.</exception>
    Task<Worktree> CreateAsync(
        CreateWorktreeOptions options,
        IProgress<string>? progress = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Removes a worktree.
    /// </summary>
    /// <param name="options">Removal options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="UncommittedChangesException">Has uncommitted changes without force.</exception>
    /// <exception cref="WorktreeLockedException">Worktree is locked.</exception>
    Task RemoveAsync(
        RemoveWorktreeOptions options,
        CancellationToken ct = default);
    
    /// <summary>
    /// Lists all worktrees in the repository.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All worktrees including main.</returns>
    Task<IReadOnlyList<Worktree>> ListAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets a specific worktree by path.
    /// </summary>
    /// <param name="path">Worktree path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Worktree if found, null otherwise.</returns>
    Task<Worktree?> GetAsync(string path, CancellationToken ct = default);
    
    /// <summary>
    /// Prunes stale worktree entries.
    /// </summary>
    /// <param name="dryRun">If true, return what would be pruned without pruning.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Pruned worktree paths.</returns>
    Task<IReadOnlyList<string>> PruneAsync(
        bool dryRun = false,
        CancellationToken ct = default);
    
    /// <summary>
    /// Locks a worktree to prevent removal.
    /// </summary>
    /// <param name="path">Worktree path.</param>
    /// <param name="reason">Lock reason.</param>
    /// <param name="ct">Cancellation token.</param>
    Task LockAsync(string path, string? reason = null, CancellationToken ct = default);
    
    /// <summary>
    /// Unlocks a worktree.
    /// </summary>
    /// <param name="path">Worktree path.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UnlockAsync(string path, CancellationToken ct = default);
}
```

### Worktree Parser

```csharp
// Acode.Application/Services/Git/WorktreeParser.cs
namespace Acode.Application.Services.Git;

/// <summary>
/// Parses git worktree list --porcelain output.
/// </summary>
public static class WorktreeParser
{
    /// <summary>
    /// Parses porcelain output from git worktree list.
    /// </summary>
    /// <remarks>
    /// Porcelain format (blocks separated by empty lines):
    /// worktree /path/to/worktree
    /// HEAD abc123def456
    /// branch refs/heads/main
    /// [locked [reason]]
    /// [prunable [reason]]
    /// 
    /// For detached HEAD, "detached" instead of "branch".
    /// For bare, "bare" instead of branch/detached.
    /// </remarks>
    public static IReadOnlyList<Worktree> Parse(string porcelainOutput)
    {
        if (string.IsNullOrWhiteSpace(porcelainOutput))
            return [];
        
        var worktrees = new List<Worktree>();
        var blocks = porcelainOutput.Split(
            ["\n\n", "\r\n\r\n"], 
            StringSplitOptions.RemoveEmptyEntries);
        
        bool isFirst = true;
        foreach (var block in blocks)
        {
            var worktree = ParseBlock(block.Trim(), isFirst);
            if (worktree != null)
            {
                worktrees.Add(worktree);
            }
            isFirst = false;
        }
        
        return worktrees;
    }
    
    private static Worktree? ParseBlock(string block, bool isMain)
    {
        if (string.IsNullOrWhiteSpace(block))
            return null;
        
        var lines = block.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        
        string? path = null;
        string? commitSha = null;
        string? branch = null;
        var state = WorktreeState.Valid;
        string? lockReason = null;
        
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            
            if (line.StartsWith("worktree "))
            {
                path = line[9..];
            }
            else if (line.StartsWith("HEAD "))
            {
                commitSha = line[5..];
            }
            else if (line.StartsWith("branch "))
            {
                // refs/heads/main -> main
                var fullRef = line[7..];
                branch = fullRef.StartsWith("refs/heads/")
                    ? fullRef[11..]
                    : fullRef;
            }
            else if (line == "detached")
            {
                branch = null; // Detached HEAD
                state = WorktreeState.Detached;
            }
            else if (line.StartsWith("locked"))
            {
                state = WorktreeState.Locked;
                lockReason = line.Length > 7 ? line[7..] : null;
            }
            else if (line.StartsWith("prunable"))
            {
                state = WorktreeState.Prunable;
            }
            else if (line == "bare")
            {
                // Bare repository main worktree
                branch = null;
            }
        }
        
        if (path == null || commitSha == null)
            return null;
        
        return new Worktree(
            path,
            commitSha,
            branch,
            isMain,
            state,
            lockReason);
    }
}
```

### Path Generator

```csharp
// Acode.Application/Services/Git/WorktreePathGenerator.cs
namespace Acode.Application.Services.Git;

/// <summary>
/// Generates and validates worktree paths.
/// </summary>
public sealed class WorktreePathGenerator
{
    private readonly string _baseDirectory;
    private readonly int _maxPathLength;
    
    public WorktreePathGenerator(string baseDirectory, int maxPathLength = 260)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
        
        _baseDirectory = System.IO.Path.GetFullPath(baseDirectory);
        _maxPathLength = maxPathLength;
    }
    
    /// <summary>
    /// Generates a worktree path for a task.
    /// </summary>
    public string GenerateForTask(string taskId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        
        // Sanitize task ID for filesystem
        var sanitized = SanitizeForPath(taskId);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
        var dirName = $"{sanitized}-{timestamp}";
        
        return System.IO.Path.Combine(_baseDirectory, dirName);
    }
    
    /// <summary>
    /// Validates that a path is within the base directory.
    /// </summary>
    /// <exception cref="PathOutsideBaseException">Path is outside base directory.</exception>
    public void ValidatePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        
        var fullPath = System.IO.Path.GetFullPath(path);
        
        // Check containment
        if (!IsWithinBase(fullPath))
        {
            throw new PathOutsideBaseException(path, _baseDirectory);
        }
        
        // Check for symlink escape attempts
        if (ContainsSymlinkEscape(path))
        {
            throw new PathOutsideBaseException(path, _baseDirectory);
        }
        
        // Check path length
        if (fullPath.Length > _maxPathLength)
        {
            throw new ArgumentException(
                $"Path exceeds maximum length of {_maxPathLength}: {fullPath.Length}",
                nameof(path));
        }
        
        // Check for invalid characters
        var invalidChars = System.IO.Path.GetInvalidPathChars();
        if (path.Any(c => invalidChars.Contains(c)))
        {
            throw new ArgumentException("Path contains invalid characters", nameof(path));
        }
        
        // Block network paths
        if (path.StartsWith(@"\\") || path.StartsWith("//"))
        {
            throw new ArgumentException("Network paths are not allowed", nameof(path));
        }
    }
    
    /// <summary>
    /// Generates a branch name from a path or task ID.
    /// </summary>
    public static string GenerateBranchName(string source, string? prefix = "task")
    {
        var sanitized = SanitizeForBranch(source);
        return prefix != null ? $"{prefix}/{sanitized}" : sanitized;
    }
    
    private bool IsWithinBase(string fullPath)
    {
        var basePath = _baseDirectory.TrimEnd(
            System.IO.Path.DirectorySeparatorChar,
            System.IO.Path.AltDirectorySeparatorChar) 
            + System.IO.Path.DirectorySeparatorChar;
        
        return fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
    }
    
    private static bool ContainsSymlinkEscape(string path)
    {
        // Check for path traversal attempts
        var segments = path.Split(
            [System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);
        
        return segments.Any(s => s == "..");
    }
    
    private static string SanitizeForPath(string input)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var result = new StringBuilder(input.Length);
        
        foreach (var c in input)
        {
            result.Append(invalid.Contains(c) ? '_' : c);
        }
        
        return result.ToString().Trim('_').ToLowerInvariant();
    }
    
    private static string SanitizeForBranch(string input)
    {
        // Git branch name restrictions
        var result = new StringBuilder(input.Length);
        
        foreach (var c in input)
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '/')
            {
                result.Append(c);
            }
            else if (c == ' ' || c == '.')
            {
                result.Append('-');
            }
        }
        
        // Remove consecutive dashes, leading/trailing dashes
        var str = result.ToString();
        while (str.Contains("--"))
        {
            str = str.Replace("--", "-");
        }
        
        return str.Trim('-', '/').ToLowerInvariant();
    }
}
```

### Service Implementation

```csharp
// Acode.Application/Services/Git/WorktreeService.cs
namespace Acode.Application.Services.Git;

/// <summary>
/// Implements worktree operations using Git CLI.
/// </summary>
public sealed class WorktreeService : IWorktreeService
{
    private readonly IGitService _git;
    private readonly WorktreePathGenerator _pathGenerator;
    private readonly ILogger<WorktreeService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    
    public WorktreeService(
        IGitService git,
        WorktreePathGenerator pathGenerator,
        ILogger<WorktreeService> logger)
    {
        _git = git ?? throw new ArgumentNullException(nameof(git));
        _pathGenerator = pathGenerator ?? throw new ArgumentNullException(nameof(pathGenerator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    public async Task<Worktree> CreateAsync(
        CreateWorktreeOptions options,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        
        // Validate path
        _pathGenerator.ValidatePath(options.Path);
        
        var fullPath = Path.GetFullPath(options.Path);
        
        // Check if path exists
        if (Directory.Exists(fullPath) || File.Exists(fullPath))
        {
            throw new PathExistsException(fullPath);
        }
        
        // Generate branch name if not provided
        var branch = options.Branch ?? 
            WorktreePathGenerator.GenerateBranchName(Path.GetFileName(fullPath));
        
        await _lock.WaitAsync(ct);
        try
        {
            progress?.Report($"Creating worktree at {fullPath}...");
            _logger.LogInformation("Creating worktree: {Path} with branch {Branch}",
                fullPath, branch);
            
            // Build git worktree add command
            var args = new List<string> { "worktree", "add" };
            
            if (options.Detach)
            {
                args.Add("--detach");
            }
            else if (options.CreateBranch)
            {
                args.Add("-b");
                args.Add(branch);
            }
            
            if (options.Force)
            {
                args.Add("--force");
            }
            
            if (options.Lock)
            {
                args.Add("--lock");
                if (!string.IsNullOrEmpty(options.LockReason))
                {
                    args.Add($"--reason={options.LockReason}");
                }
            }
            
            args.Add(fullPath);
            
            if (!options.Detach && !options.CreateBranch && options.StartPoint != null)
            {
                args.Add(options.StartPoint);
            }
            else if (options.CreateBranch && options.StartPoint != null)
            {
                // For new branch, add start point after path
                args.Add(options.StartPoint);
            }
            
            try
            {
                await _git.ExecuteAsync(args, ct);
            }
            catch (Exception ex)
            {
                // Cleanup on failure
                CleanupFailedCreation(fullPath);
                
                if (ex.Message.Contains("already checked out"))
                {
                    // Parse which worktree has the branch
                    var match = System.Text.RegularExpressions.Regex.Match(
                        ex.Message, @"'([^']+)'");
                    var usingPath = match.Success ? match.Groups[1].Value : "unknown";
                    throw new BranchInUseException(fullPath, branch, usingPath);
                }
                
                throw;
            }
            
            // Initialize submodules if requested
            if (options.InitializeSubmodules)
            {
                progress?.Report("Initializing submodules...");
                await _git.ExecuteAsync(
                    ["submodule", "update", "--init", "--recursive"],
                    ct,
                    workingDirectory: fullPath);
            }
            
            progress?.Report("Worktree created successfully.");
            
            // Return the created worktree
            var worktree = await GetAsync(fullPath, ct);
            return worktree ?? throw new InvalidOperationException(
                $"Failed to verify created worktree at {fullPath}");
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public async Task RemoveAsync(
        RemoveWorktreeOptions options,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        
        var fullPath = Path.GetFullPath(options.Path);
        
        _logger.LogInformation("Removing worktree: {Path}", fullPath);
        
        await _lock.WaitAsync(ct);
        try
        {
            // Get worktree info to check status
            var worktree = await GetAsync(fullPath, ct);
            
            if (worktree == null)
            {
                // Worktree doesn't exist - treat as success
                _logger.LogWarning("Worktree not found, nothing to remove: {Path}", fullPath);
                return;
            }
            
            // Check for lock
            if (worktree.IsLocked && !options.Force)
            {
                throw new WorktreeLockedException(fullPath, worktree.LockReason);
            }
            
            // Check for uncommitted changes (unless force)
            if (!options.Force && !worktree.IsPrunable)
            {
                var status = await _git.StatusAsync(fullPath, ct);
                if (status.HasChanges)
                {
                    var changedFiles = status.Entries
                        .Select(e => e.Path)
                        .Take(10)
                        .ToList();
                    throw new UncommittedChangesException(fullPath, changedFiles);
                }
            }
            
            var branch = worktree.Branch;
            
            // Remove worktree
            var args = new List<string> { "worktree", "remove" };
            if (options.Force)
            {
                args.Add("--force");
            }
            args.Add(fullPath);
            
            await _git.ExecuteAsync(args, ct);
            
            // Optionally delete branch
            if (options.DeleteBranch && !string.IsNullOrEmpty(branch))
            {
                try
                {
                    var branchArgs = new List<string> { "branch" };
                    branchArgs.Add(options.ForceBranchDelete ? "-D" : "-d");
                    branchArgs.Add(branch);
                    
                    await _git.ExecuteAsync(branchArgs, ct);
                    _logger.LogInformation("Deleted branch: {Branch}", branch);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, 
                        "Failed to delete branch {Branch} after worktree removal",
                        branch);
                    // Don't rethrow - worktree removal succeeded
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public async Task<IReadOnlyList<Worktree>> ListAsync(CancellationToken ct = default)
    {
        var output = await _git.ExecuteAsync(
            ["worktree", "list", "--porcelain"], ct);
        
        return WorktreeParser.Parse(output);
    }
    
    public async Task<Worktree?> GetAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.GetFullPath(path);
        var worktrees = await ListAsync(ct);
        
        return worktrees.FirstOrDefault(w => 
            string.Equals(w.Path, fullPath, StringComparison.OrdinalIgnoreCase));
    }
    
    public async Task<IReadOnlyList<string>> PruneAsync(
        bool dryRun = false,
        CancellationToken ct = default)
    {
        // First, get list of prunable worktrees
        var worktrees = await ListAsync(ct);
        var prunableList = worktrees
            .Where(w => w.IsPrunable)
            .Select(w => w.Path)
            .ToList();
        
        if (dryRun || prunableList.Count == 0)
        {
            return prunableList;
        }
        
        // Execute prune
        await _git.ExecuteAsync(["worktree", "prune"], ct);
        _logger.LogInformation("Pruned {Count} stale worktree entries", prunableList.Count);
        
        return prunableList;
    }
    
    public async Task LockAsync(string path, string? reason = null, CancellationToken ct = default)
    {
        var fullPath = Path.GetFullPath(path);
        
        var args = new List<string> { "worktree", "lock" };
        if (!string.IsNullOrEmpty(reason))
        {
            args.Add($"--reason={reason}");
        }
        args.Add(fullPath);
        
        await _git.ExecuteAsync(args, ct);
        _logger.LogInformation("Locked worktree: {Path}", fullPath);
    }
    
    public async Task UnlockAsync(string path, CancellationToken ct = default)
    {
        var fullPath = Path.GetFullPath(path);
        await _git.ExecuteAsync(["worktree", "unlock", fullPath], ct);
        _logger.LogInformation("Unlocked worktree: {Path}", fullPath);
    }
    
    private void CleanupFailedCreation(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup after failed worktree creation: {Path}", path);
        }
    }
}
```

### CLI Commands

```csharp
// Acode.Cli/Commands/Worktree/WorktreeCreateCommand.cs
namespace Acode.Cli.Commands.Worktree;

[Command("worktree create", Description = "Create a new worktree")]
public sealed class WorktreeCreateCommand : ICommand
{
    [CommandParameter(0, Description = "Path for the new worktree")]
    public required string Path { get; init; }
    
    [CommandOption("branch|b", Description = "Branch name (auto-generated if not specified)")]
    public string? Branch { get; init; }
    
    [CommandOption("start-point|s", Description = "Starting point for new branch")]
    public string? StartPoint { get; init; }
    
    [CommandOption("detach|d", Description = "Create with detached HEAD")]
    public bool Detach { get; init; }
    
    [CommandOption("force|f", Description = "Force creation")]
    public bool Force { get; init; }
    
    [CommandOption("lock", Description = "Lock worktree after creation")]
    public bool Lock { get; init; }
    
    [CommandOption("lock-reason", Description = "Reason for locking")]
    public string? LockReason { get; init; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var service = GetWorktreeService(); // DI
        
        var options = new CreateWorktreeOptions
        {
            Path = Path,
            Branch = Branch,
            StartPoint = StartPoint,
            Detach = Detach,
            Force = Force,
            Lock = Lock,
            LockReason = LockReason
        };
        
        var progress = new Progress<string>(msg => console.Output.WriteLine(msg));
        
        try
        {
            var worktree = await service.CreateAsync(options, progress);
            
            console.Output.WriteLine();
            console.Output.WriteLine($"Created worktree:");
            console.Output.WriteLine($"  Path:   {worktree.Path}");
            console.Output.WriteLine($"  Branch: {worktree.Branch ?? "(detached)"}");
            console.Output.WriteLine($"  Commit: {worktree.CommitSha[..8]}");
        }
        catch (PathExistsException ex)
        {
            console.Error.WriteLine($"Error: Path already exists: {ex.WorktreePath}");
            Environment.ExitCode = ExitCodes.PathExists;
        }
        catch (PathOutsideBaseException ex)
        {
            console.Error.WriteLine($"Error: Path outside allowed directory");
            console.Error.WriteLine($"  Path: {ex.WorktreePath}");
            console.Error.WriteLine($"  Base: {ex.BaseDirectory}");
            Environment.ExitCode = ExitCodes.InvalidPath;
        }
        catch (BranchInUseException ex)
        {
            console.Error.WriteLine($"Error: Branch already checked out elsewhere");
            console.Error.WriteLine($"  Branch: {ex.BranchName}");
            console.Error.WriteLine($"  In use by: {ex.UsingWorktreePath}");
            Environment.ExitCode = ExitCodes.BranchInUse;
        }
    }
}

// Acode.Cli/Commands/Worktree/WorktreeRemoveCommand.cs
[Command("worktree remove", Description = "Remove a worktree")]
public sealed class WorktreeRemoveCommand : ICommand
{
    [CommandParameter(0, Description = "Worktree path to remove")]
    public required string Path { get; init; }
    
    [CommandOption("force|f", Description = "Force removal even with uncommitted changes")]
    public bool Force { get; init; }
    
    [CommandOption("delete-branch", Description = "Also delete the associated branch")]
    public bool DeleteBranch { get; init; }
    
    [CommandOption("force-branch-delete|D", Description = "Force branch deletion even if not merged")]
    public bool ForceBranchDelete { get; init; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var service = GetWorktreeService();
        
        var options = new RemoveWorktreeOptions
        {
            Path = Path,
            Force = Force,
            DeleteBranch = DeleteBranch,
            ForceBranchDelete = ForceBranchDelete
        };
        
        try
        {
            await service.RemoveAsync(options);
            console.Output.WriteLine($"Removed worktree: {Path}");
            
            if (DeleteBranch)
            {
                console.Output.WriteLine("Associated branch deleted.");
            }
        }
        catch (UncommittedChangesException ex)
        {
            console.Error.WriteLine($"Error: Worktree has uncommitted changes");
            console.Error.WriteLine($"  Changed files ({ex.ChangedFiles.Count}):");
            foreach (var file in ex.ChangedFiles.Take(5))
            {
                console.Error.WriteLine($"    - {file}");
            }
            if (ex.ChangedFiles.Count > 5)
            {
                console.Error.WriteLine($"    ... and {ex.ChangedFiles.Count - 5} more");
            }
            console.Error.WriteLine();
            console.Error.WriteLine("Use --force to remove anyway.");
            Environment.ExitCode = ExitCodes.UncommittedChanges;
        }
        catch (WorktreeLockedException ex)
        {
            console.Error.WriteLine($"Error: Worktree is locked");
            if (ex.LockReason != null)
            {
                console.Error.WriteLine($"  Reason: {ex.LockReason}");
            }
            console.Error.WriteLine();
            console.Error.WriteLine("Use 'acode worktree unlock' first, or --force to remove anyway.");
            Environment.ExitCode = ExitCodes.WorktreeLocked;
        }
    }
}

// Acode.Cli/Commands/Worktree/WorktreeListCommand.cs
[Command("worktree list", Description = "List all worktrees")]
public sealed class WorktreeListCommand : ICommand
{
    [CommandOption("json", Description = "Output as JSON")]
    public bool Json { get; init; }
    
    [CommandOption("prune", Description = "Prune stale entries")]
    public bool Prune { get; init; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var service = GetWorktreeService();
        
        if (Prune)
        {
            var pruned = await service.PruneAsync(dryRun: false);
            if (pruned.Count > 0)
            {
                console.Output.WriteLine($"Pruned {pruned.Count} stale entries.");
            }
        }
        
        var worktrees = await service.ListAsync();
        
        if (Json)
        {
            var json = JsonSerializer.Serialize(worktrees, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            console.Output.WriteLine(json);
            return;
        }
        
        if (worktrees.Count == 0)
        {
            console.Output.WriteLine("No worktrees found.");
            return;
        }
        
        foreach (var wt in worktrees)
        {
            var status = wt.IsMain ? "[main]" :
                         wt.IsLocked ? "[locked]" :
                         wt.IsPrunable ? "[prunable]" :
                         wt.IsDetached ? "[detached]" : "";
            
            console.Output.WriteLine($"{wt.Path}");
            console.Output.WriteLine($"  Branch: {wt.Branch ?? "(detached)"}");
            console.Output.WriteLine($"  Commit: {wt.CommitSha[..8]} {status}");
            console.Output.WriteLine();
        }
        
        console.Output.WriteLine($"Total: {worktrees.Count} worktrees");
    }
}

// Acode.Cli/Commands/Worktree/WorktreePruneCommand.cs
[Command("worktree prune", Description = "Prune stale worktree entries")]
public sealed class WorktreePruneCommand : ICommand
{
    [CommandOption("dry-run|n", Description = "Show what would be pruned without pruning")]
    public bool DryRun { get; init; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var service = GetWorktreeService();
        
        var pruned = await service.PruneAsync(dryRun: DryRun);
        
        if (pruned.Count == 0)
        {
            console.Output.WriteLine("No stale worktree entries to prune.");
            return;
        }
        
        var action = DryRun ? "Would prune" : "Pruned";
        console.Output.WriteLine($"{action} {pruned.Count} stale entries:");
        
        foreach (var path in pruned)
        {
            console.Output.WriteLine($"  - {path}");
        }
    }
}
```

### Error Codes

```csharp
// Acode.Cli/ExitCodes.cs (additions)
public static partial class ExitCodes
{
    // Worktree errors: 60-69
    public const int PathExists = 60;
    public const int InvalidPath = 61;
    public const int BranchInUse = 62;
    public const int UncommittedChanges = 63;
    public const int WorktreeLocked = 64;
    public const int WorktreeNotFound = 65;
}
```

### Git Commands Reference

```bash
# Create worktree with new branch
git worktree add <path> -b <branch>

# Create worktree with existing branch
git worktree add <path> <branch>

# Create with detached HEAD
git worktree add --detach <path> <commit>

# Create and lock
git worktree add --lock --reason="working on task" <path> -b <branch>

# List in porcelain format
git worktree list --porcelain

# Remove worktree
git worktree remove <path>

# Force remove (with uncommitted changes)
git worktree remove --force <path>

# Prune stale entries
git worktree prune

# Lock/unlock
git worktree lock <path>
git worktree lock --reason="reason" <path>
git worktree unlock <path>
```

### Implementation Checklist

- [ ] Create `WorktreeId` value object with normalization
- [ ] Create `WorktreeState` enum with all states
- [ ] Create `Worktree` entity with full properties
- [ ] Create all exception types (Path, Branch, Lock, Uncommitted)
- [ ] Create `CreateWorktreeOptions` with all fields
- [ ] Create `RemoveWorktreeOptions` with all fields
- [ ] Define `IWorktreeService` interface
- [ ] Implement `WorktreeParser` for porcelain output
- [ ] Implement `WorktreePathGenerator` with validation
- [ ] Implement `WorktreeService.CreateAsync` with all options
- [ ] Implement `WorktreeService.RemoveAsync` with safety checks
- [ ] Implement `WorktreeService.ListAsync`
- [ ] Implement `WorktreeService.GetAsync`
- [ ] Implement `WorktreeService.PruneAsync` with dry-run
- [ ] Implement `WorktreeService.LockAsync`
- [ ] Implement `WorktreeService.UnlockAsync`
- [ ] Add concurrency protection with `SemaphoreSlim`
- [ ] Add cleanup on failed creation
- [ ] Create `WorktreeCreateCommand` CLI
- [ ] Create `WorktreeRemoveCommand` CLI
- [ ] Create `WorktreeListCommand` CLI with JSON
- [ ] Create `WorktreePruneCommand` CLI with dry-run
- [ ] Add error codes for all exception types
- [ ] Write unit tests for `WorktreeParser`
- [ ] Write unit tests for `WorktreePathGenerator`
- [ ] Write integration tests with real Git repo

### Rollout Plan

1. **Phase 1: Domain Models** (Day 1)
   - Implement all domain models and exceptions
   - Unit test value objects

2. **Phase 2: Parser** (Day 1)
   - Implement `WorktreeParser`
   - Test with various porcelain outputs

3. **Phase 3: Path Generator** (Day 2)
   - Implement `WorktreePathGenerator`
   - Test security (path traversal, symlinks)

4. **Phase 4: Service** (Days 2-3)
   - Implement `WorktreeService` operations
   - Add concurrency protection
   - Integration test with real repo

5. **Phase 5: CLI** (Day 3)
   - Implement all CLI commands
   - Manual testing with real scenarios

6. **Phase 6: Polish** (Day 4)
   - Error message refinement
   - Documentation updates
   - Edge case handling

---

**End of Task 023.a Specification**