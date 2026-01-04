# Task 022.b: branch create/checkout

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 5 – Git Integration Layer  
**Dependencies:** Task 022 (Git Tool Layer), Task 022.a (Status)  

---

## Description

Task 022.b implements Git branch creation and checkout operations. The agent MUST be able to create feature branches and switch between branches programmatically.

Branch creation MUST support naming conventions. Branch names MUST be validated before creation. Invalid characters MUST be rejected with clear error messages. Duplicate branch names MUST be detected.

Checkout MUST handle working tree state. Uncommitted changes MUST be detected before checkout. Checkout with dirty working tree MUST be blocked by default. Force checkout MUST be available but require explicit opt-in.

Branch listing MUST enumerate all local branches. Remote tracking branches MUST also be listable. Current branch MUST be identified in listings.

Branch deletion MUST be supported with safety checks. Unmerged branches MUST require force delete. Current branch MUST NOT be deletable.

All branch operations MUST work in all operating modes. These are local operations that do NOT require network access.

### Business Value

Branch management enables isolated development workflows. The agent can create feature branches for tasks, switch between work streams, and clean up after completion. This supports the worktree-per-task pattern in Task 023.

### Scope Boundaries

This task covers branch create, checkout, list, and delete. Status and diff are in Task 022.a. Commit and push are in Task 022.c. Remote branch operations beyond listing are out of scope.

### Integration Points

- Task 022: IGitService interface
- Task 022.a: Status check before checkout
- Task 023: Worktree creation uses branches
- Task 018: Command execution

### Failure Modes

- Invalid branch name → Clear validation error
- Branch already exists → BranchExistsException
- Branch not found → BranchNotFoundException
- Dirty working tree → UncommittedChangesException
- Delete current branch → Operation blocked

### Assumptions

- Git is installed and accessible
- Repository has at least one commit
- User has write permissions

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Branch | Named pointer to a commit |
| HEAD | Reference to current branch or commit |
| Checkout | Switch working tree to different branch |
| Detached HEAD | HEAD points to commit, not branch |
| Remote Branch | Branch on remote repository |
| Tracking Branch | Local branch tracking remote |
| Upstream | Remote branch a local branch tracks |
| Fast-forward | Branch update without merge commit |

---

## Out of Scope

- Remote branch creation
- Branch renaming
- Branch protection rules
- Pull request integration
- Merge operations
- Rebase operations
- Cherry-pick operations

---

## Functional Requirements

### FR-001 to FR-020: Branch Creation

- FR-001: `CreateBranchAsync` MUST create new branch
- FR-002: Branch MUST be created at current HEAD by default
- FR-003: Custom start point MUST be supported
- FR-004: Branch name MUST be validated
- FR-005: Invalid characters MUST be rejected
- FR-006: Branch name MUST NOT start with `-`
- FR-007: Branch name MUST NOT contain `..`
- FR-008: Branch name MUST NOT contain whitespace
- FR-009: Branch name MUST NOT end with `.lock`
- FR-010: Duplicate name MUST throw BranchExistsException
- FR-011: `--force` MUST reset existing branch
- FR-012: Created branch MUST be returned
- FR-013: Creation MUST NOT switch to new branch
- FR-014: `--checkout` MUST switch to new branch
- FR-015: Branch MUST be created atomically
- FR-016: Branch ref MUST be visible after creation
- FR-017: Creation MUST be logged
- FR-018: Error MUST include suggested fix
- FR-019: Empty branch name MUST be rejected
- FR-020: Branch name length MUST be validated (<255 chars)

### FR-021 to FR-040: Checkout Operation

- FR-021: `CheckoutAsync` MUST switch to branch
- FR-022: Working tree MUST be updated
- FR-023: Index MUST be updated
- FR-024: HEAD MUST point to branch
- FR-025: Uncommitted changes MUST block checkout
- FR-026: `--force` MUST discard local changes
- FR-027: Untracked files MUST NOT block checkout
- FR-028: Conflicting untracked files MUST block
- FR-029: Non-existent branch MUST throw exception
- FR-030: Detached HEAD checkout MUST be supported
- FR-031: Checkout of commit SHA MUST work
- FR-032: Checkout of tag MUST work
- FR-033: Current branch MUST be updated after checkout
- FR-034: Checkout MUST be logged
- FR-035: Checkout progress MUST be reportable
- FR-036: `--quiet` MUST suppress output
- FR-037: Pathspec checkout MUST be supported
- FR-038: Partial checkout MUST NOT change HEAD
- FR-039: Remote branch checkout MUST create tracking
- FR-040: Checkout MUST handle submodules

### FR-041 to FR-060: Branch Listing

- FR-041: `ListBranchesAsync` MUST return all local branches
- FR-042: Each branch MUST include name
- FR-043: Each branch MUST include SHA
- FR-044: Current branch MUST be marked
- FR-045: `--remote` MUST list remote branches
- FR-046: `--all` MUST list local and remote
- FR-047: Upstream tracking MUST be included
- FR-048: Ahead/behind counts MUST be available
- FR-049: Last commit date MUST be available
- FR-050: Sorting MUST be configurable
- FR-051: Default sort MUST be by name
- FR-052: `--sort=-committerdate` MUST work
- FR-053: Pattern matching MUST filter results
- FR-054: Merged branches MUST be identifiable
- FR-055: Unmerged branches MUST be identifiable
- FR-056: Listing MUST complete in <1s for 1000 branches
- FR-057: Empty result MUST return empty list
- FR-058: Listing MUST work in detached HEAD
- FR-059: Symbolic refs MUST be handled
- FR-060: Listing MUST be cacheable

### FR-061 to FR-070: Branch Deletion

- FR-061: `DeleteBranchAsync` MUST delete local branch
- FR-062: Current branch MUST NOT be deletable
- FR-063: Merged branches MUST delete without force
- FR-064: Unmerged branches MUST require force
- FR-065: `--force` MUST delete unmerged branch
- FR-066: Deletion MUST be logged
- FR-067: Non-existent branch MUST return success
- FR-068: Multiple branches MUST be deletable
- FR-069: Remote tracking refs MUST be deletable
- FR-070: Deletion MUST be atomic

---

## Non-Functional Requirements

### NFR-001 to NFR-015: Performance and Reliability

- NFR-001: Branch creation MUST complete in <200ms
- NFR-002: Checkout MUST complete in <1s for clean repos
- NFR-003: Checkout MUST complete in <5s for large repos
- NFR-004: Listing MUST complete in <1s for 1000 branches
- NFR-005: Memory MUST NOT exceed 20MB for operations
- NFR-006: Concurrent branch operations MUST be safe
- NFR-007: Failed operations MUST NOT leave partial state
- NFR-008: Interrupted checkout MUST be recoverable
- NFR-009: File locks MUST timeout after 30s
- NFR-010: Operations MUST support cancellation
- NFR-011: Path sanitization MUST prevent injection
- NFR-012: Branch name MUST be escaped in commands
- NFR-013: Error messages MUST NOT leak paths
- NFR-014: Temporary refs MUST be cleaned up
- NFR-015: Atomic ref updates MUST be used

---

## User Manual Documentation

### Quick Start

```bash
# Create a new branch
acode git branch create feature/my-feature

# Switch to a branch
acode git checkout feature/my-feature

# List all branches
acode git branch list

# Delete a branch
acode git branch delete feature/my-feature
```

### Branch Create Command

```bash
acode git branch create <name> [options]

Options:
  --start-point REF     Start branch from this ref (default: HEAD)
  --checkout            Switch to new branch after creation
  --force               Reset branch if it exists
```

### Checkout Command

```bash
acode git checkout <branch> [options]

Options:
  --force               Discard local changes
  --quiet               Suppress output
```

### Branch List Command

```bash
acode git branch list [options]

Options:
  --remote              List remote branches
  --all                 List local and remote
  --sort FIELD          Sort by: name, committerdate
  --format FORMAT       Output: text, json
```

### Troubleshooting

**Q: "Branch already exists"**

Use `--force` to reset the branch, or choose a different name.

**Q: "Uncommitted changes would be lost"**

Commit or stash changes before checkout:
```bash
acode git stash  # or commit changes
acode git checkout feature/branch
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Branch creation works
- [ ] AC-002: Branch name validation works
- [ ] AC-003: Duplicate detection works
- [ ] AC-004: Checkout switches branches
- [ ] AC-005: Dirty tree blocks checkout
- [ ] AC-006: Force checkout works
- [ ] AC-007: Branch listing returns all branches
- [ ] AC-008: Current branch marked in list
- [ ] AC-009: Branch deletion works
- [ ] AC-010: Unmerged branch requires force
- [ ] AC-011: CLI commands work
- [ ] AC-012: Error messages are clear
- [ ] AC-013: Performance benchmarks met
- [ ] AC-014: Unit tests >90% coverage

---

## Best Practices

### Branch Naming

1. **Validate names early** - Check against git naming rules before creation
2. **Use consistent prefixes** - feature/, bugfix/, task/ for organization
3. **Include task ID** - Link branches to tasks: task-123-description
4. **Avoid special characters** - Alphanumeric, hyphen, slash only

### Checkout Operations

5. **Check for uncommitted changes** - Warn or fail if working tree dirty
6. **Handle detached HEAD** - Detect and warn about headless state
7. **Update submodules** - Optionally update submodules on checkout
8. **Preserve stash if needed** - Offer to stash changes before switching

### Safety

9. **Confirm force operations** - Force delete requires explicit confirmation
10. **List affected files** - Show what would change before checkout
11. **Backup current work** - Stash or commit before risky operations
12. **Track branch operations** - Log creates, deletes, renames

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Test branch name validation
- [ ] UT-002: Test invalid character detection
- [ ] UT-003: Test branch parsing
- [ ] UT-004: Test current branch detection
- [ ] UT-005: Test upstream parsing

### Integration Tests

- [ ] IT-001: Create branch on real repo
- [ ] IT-002: Checkout on real repo
- [ ] IT-003: List branches on real repo
- [ ] IT-004: Delete branch on real repo
- [ ] IT-005: Dirty tree handling

### End-to-End Tests

- [ ] E2E-001: CLI branch create
- [ ] E2E-002: CLI checkout
- [ ] E2E-003: CLI branch list
- [ ] E2E-004: CLI branch delete

### Performance/Benchmarks

- [ ] PB-001: Create in <200ms
- [ ] PB-002: Checkout clean repo in <1s
- [ ] PB-003: List 1000 branches in <1s

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Git/
│       └── GitBranch.cs
├── Acode.Application/
│   └── Git/
│       ├── Options/
│       │   ├── CreateBranchOptions.cs
│       │   ├── CheckoutOptions.cs
│       │   └── ListBranchesOptions.cs
│       └── Exceptions/
│           ├── BranchExistsException.cs
│           ├── BranchNotFoundException.cs
│           └── UncommittedChangesException.cs
├── Acode.Infrastructure/
│   └── Git/
│       ├── BranchNameValidator.cs
│       ├── BranchParser.cs
│       └── GitService.Branch.cs
└── Acode.Cli/
    └── Commands/
        └── Git/
            ├── GitBranchCreateCommand.cs
            ├── GitBranchListCommand.cs
            ├── GitBranchDeleteCommand.cs
            └── GitCheckoutCommand.cs
```

### Domain Models

```csharp
// GitBranch.cs - already defined in task-022
// Extended with additional properties

namespace Acode.Domain.Git;

public sealed record GitBranch
{
    public required string Name { get; init; }
    public required string FullName { get; init; }
    public required string Sha { get; init; }
    public required bool IsCurrent { get; init; }
    public required bool IsRemote { get; init; }
    public string? Upstream { get; init; }
    public int? AheadBy { get; init; }
    public int? BehindBy { get; init; }
    public DateTimeOffset? LastCommitDate { get; init; }
    public string? LastCommitSubject { get; init; }
}
```

### Options Classes

```csharp
// CreateBranchOptions.cs
namespace Acode.Application.Git.Options;

public sealed record CreateBranchOptions
{
    public string? StartPoint { get; init; }
    public bool Checkout { get; init; }
    public bool Force { get; init; }
}

// CheckoutOptions.cs
public sealed record CheckoutOptions
{
    public bool Force { get; init; }
    public bool Quiet { get; init; }
    public bool CreateBranch { get; init; }
}

// ListBranchesOptions.cs
public sealed record ListBranchesOptions
{
    public bool IncludeRemote { get; init; }
    public bool All { get; init; }
    public string? Pattern { get; init; }
    public BranchSortField SortBy { get; init; } = BranchSortField.Name;
    public bool Descending { get; init; }
}

public enum BranchSortField
{
    Name,
    CommitterDate,
    AuthorDate
}
```

### Branch Name Validator

```csharp
// BranchNameValidator.cs
namespace Acode.Infrastructure.Git;

public sealed class BranchNameValidator
{
    private static readonly char[] InvalidChars = { ' ', '~', '^', ':', '\\', '?', '*', '[' };
    
    public ValidationResult Validate(string name)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Branch name cannot be empty");
            return new ValidationResult(false, errors);
        }
        
        if (name.Length > 255)
        {
            errors.Add("Branch name exceeds 255 characters");
        }
        
        if (name.StartsWith("-"))
        {
            errors.Add("Branch name cannot start with '-'");
        }
        
        if (name.StartsWith("."))
        {
            errors.Add("Branch name cannot start with '.'");
        }
        
        if (name.EndsWith("."))
        {
            errors.Add("Branch name cannot end with '.'");
        }
        
        if (name.EndsWith(".lock"))
        {
            errors.Add("Branch name cannot end with '.lock'");
        }
        
        if (name.Contains(".."))
        {
            errors.Add("Branch name cannot contain '..'");
        }
        
        if (name.Contains("@{"))
        {
            errors.Add("Branch name cannot contain '@{'");
        }
        
        foreach (var c in InvalidChars)
        {
            if (name.Contains(c))
            {
                errors.Add($"Branch name cannot contain '{c}'");
                break;
            }
        }
        
        if (name.Any(c => char.IsControl(c)))
        {
            errors.Add("Branch name cannot contain control characters");
        }
        
        return new ValidationResult(errors.Count == 0, errors);
    }
}

public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Errors);
```

### Service Implementation

```csharp
// GitService.Branch.cs
namespace Acode.Infrastructure.Git;

public partial class GitService
{
    private readonly BranchNameValidator _branchValidator = new();
    private readonly BranchParser _branchParser = new();
    
    public async Task<GitBranch> CreateBranchAsync(
        string workingDir, 
        string name, 
        CreateBranchOptions? options = null, 
        CancellationToken ct = default)
    {
        options ??= new CreateBranchOptions();
        
        await EnsureRepositoryAsync(workingDir, ct);
        
        // Validate branch name
        var validation = _branchValidator.Validate(name);
        if (!validation.IsValid)
        {
            throw new InvalidBranchNameException(name, validation.Errors);
        }
        
        // Check if branch exists (unless force)
        if (!options.Force)
        {
            var exists = await BranchExistsAsync(workingDir, name, ct);
            if (exists)
            {
                throw new BranchExistsException(name);
            }
        }
        
        // Build command
        var args = new List<string> { "branch" };
        
        if (options.Force)
        {
            args.Add("--force");
        }
        
        args.Add(name);
        
        if (options.StartPoint is not null)
        {
            args.Add(options.StartPoint);
        }
        
        await ExecuteGitAsync(workingDir, args, ct);
        
        _logger.LogInformation("Created branch {Branch} in {Dir}", name, workingDir);
        
        // Checkout if requested
        if (options.Checkout)
        {
            await CheckoutAsync(workingDir, name, null, ct);
        }
        
        // Return branch info
        var sha = await GetBranchShaAsync(workingDir, name, ct);
        return new GitBranch
        {
            Name = name,
            FullName = $"refs/heads/{name}",
            Sha = sha,
            IsCurrent = options.Checkout,
            IsRemote = false
        };
    }
    
    public async Task CheckoutAsync(
        string workingDir, 
        string branchOrCommit, 
        CheckoutOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new CheckoutOptions();
        
        await EnsureRepositoryAsync(workingDir, ct);
        
        // Check for uncommitted changes (unless force)
        if (!options.Force)
        {
            var status = await GetStatusAsync(workingDir, ct);
            if (!status.IsClean && status.StagedFiles.Count + status.UnstagedFiles.Count > 0)
            {
                throw new UncommittedChangesException(
                    workingDir, 
                    status.StagedFiles.Count + status.UnstagedFiles.Count);
            }
        }
        
        var args = new List<string> { "checkout" };
        
        if (options.Force)
        {
            args.Add("--force");
        }
        
        if (options.Quiet)
        {
            args.Add("--quiet");
        }
        
        if (options.CreateBranch)
        {
            args.Add("-b");
        }
        
        args.Add(branchOrCommit);
        
        try
        {
            await ExecuteGitAsync(workingDir, args, ct);
            _logger.LogInformation("Checked out {Branch} in {Dir}", branchOrCommit, workingDir);
        }
        catch (GitException ex) when (ex.StdErr?.Contains("did not match") == true)
        {
            throw new BranchNotFoundException(branchOrCommit, workingDir, ex.StdErr);
        }
    }
    
    public async Task<IReadOnlyList<GitBranch>> ListBranchesAsync(
        string workingDir, 
        ListBranchesOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new ListBranchesOptions();
        
        await EnsureRepositoryAsync(workingDir, ct);
        
        var args = new List<string> 
        { 
            "branch", 
            "--format=%(HEAD)|%(refname:short)|%(objectname:short)|%(upstream:short)|%(upstream:track)|%(committerdate:iso-strict)|%(subject)"
        };
        
        if (options.All)
        {
            args.Add("--all");
        }
        else if (options.IncludeRemote)
        {
            args.Add("--remotes");
        }
        
        var sortField = options.SortBy switch
        {
            BranchSortField.CommitterDate => "committerdate",
            BranchSortField.AuthorDate => "authordate",
            _ => "refname"
        };
        
        args.Add($"--sort={(options.Descending ? "-" : "")}{sortField}");
        
        if (options.Pattern is not null)
        {
            args.Add(options.Pattern);
        }
        
        var result = await ExecuteGitAsync(workingDir, args, ct);
        return _branchParser.ParseList(result.StdOut);
    }
    
    public async Task DeleteBranchAsync(
        string workingDir, 
        string name, 
        bool force = false,
        CancellationToken ct = default)
    {
        await EnsureRepositoryAsync(workingDir, ct);
        
        // Check if it's the current branch
        var current = await GetCurrentBranchAsync(workingDir, ct);
        if (current == name)
        {
            throw new GitException(
                $"Cannot delete current branch '{name}'. Switch to another branch first.",
                1, null, workingDir, "GIT_022B_001");
        }
        
        var args = new List<string> { "branch", force ? "-D" : "-d", name };
        
        try
        {
            await ExecuteGitAsync(workingDir, args, ct);
            _logger.LogInformation("Deleted branch {Branch}", name);
        }
        catch (GitException ex) when (ex.StdErr?.Contains("not fully merged") == true)
        {
            throw new GitException(
                $"Branch '{name}' is not fully merged. Use --force to delete anyway.",
                ex.ExitCode, ex.StdErr, workingDir, "GIT_022B_002");
        }
    }
    
    private async Task<bool> BranchExistsAsync(string workingDir, string name, CancellationToken ct)
    {
        var result = await ExecuteGitAsync(
            workingDir, 
            new[] { "rev-parse", "--verify", $"refs/heads/{name}" },
            ct,
            throwOnError: false);
        
        return result.ExitCode == 0;
    }
    
    private async Task<string> GetBranchShaAsync(string workingDir, string name, CancellationToken ct)
    {
        var result = await ExecuteGitAsync(
            workingDir,
            new[] { "rev-parse", $"refs/heads/{name}" },
            ct);
        
        return result.StdOut.Trim();
    }
}

// BranchParser.cs
namespace Acode.Infrastructure.Git;

public sealed class BranchParser
{
    public IReadOnlyList<GitBranch> ParseList(string output)
    {
        var branches = new List<GitBranch>();
        
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split('|');
            if (parts.Length < 7) continue;
            
            var isCurrent = parts[0] == "*";
            var name = parts[1];
            var sha = parts[2];
            var upstream = string.IsNullOrEmpty(parts[3]) ? null : parts[3];
            var trackInfo = parts[4];
            var dateStr = parts[5];
            var subject = parts[6];
            
            int? ahead = null, behind = null;
            if (!string.IsNullOrEmpty(trackInfo))
            {
                var match = Regex.Match(trackInfo, @"ahead (\d+)");
                if (match.Success) ahead = int.Parse(match.Groups[1].Value);
                
                match = Regex.Match(trackInfo, @"behind (\d+)");
                if (match.Success) behind = int.Parse(match.Groups[1].Value);
            }
            
            branches.Add(new GitBranch
            {
                Name = name.StartsWith("origin/") ? name[7..] : name,
                FullName = name.Contains('/') ? $"refs/remotes/{name}" : $"refs/heads/{name}",
                Sha = sha,
                IsCurrent = isCurrent,
                IsRemote = name.Contains('/'),
                Upstream = upstream,
                AheadBy = ahead,
                BehindBy = behind,
                LastCommitDate = string.IsNullOrEmpty(dateStr) ? null : DateTimeOffset.Parse(dateStr),
                LastCommitSubject = subject
            });
        }
        
        return branches;
    }
}
```

### CLI Commands

```csharp
// GitBranchCreateCommand.cs
namespace Acode.Cli.Commands.Git;

[Command("git branch create", Description = "Create a new branch")]
public class GitBranchCreateCommand
{
    [Argument(0, Description = "Branch name")]
    public string Name { get; set; } = "";
    
    [Option("--start-point", Description = "Start branch from this ref")]
    public string? StartPoint { get; set; }
    
    [Option("--checkout", Description = "Switch to new branch")]
    public bool Checkout { get; set; }
    
    [Option("--force", Description = "Reset branch if exists")]
    public bool Force { get; set; }
    
    public async Task<int> ExecuteAsync(
        IGitService git,
        IConsole console,
        CancellationToken ct)
    {
        var cwd = Directory.GetCurrentDirectory();
        
        var branch = await git.CreateBranchAsync(cwd, Name, new CreateBranchOptions
        {
            StartPoint = StartPoint,
            Checkout = Checkout,
            Force = Force
        }, ct);
        
        console.WriteLine($"✓ Created branch '{branch.Name}' at {branch.Sha[..7]}");
        
        if (Checkout)
        {
            console.WriteLine($"  Switched to '{branch.Name}'");
        }
        
        return 0;
    }
}

// GitCheckoutCommand.cs
[Command("git checkout", Description = "Switch branches")]
public class GitCheckoutCommand
{
    [Argument(0, Description = "Branch name or commit")]
    public string Target { get; set; } = "";
    
    [Option("--force", Description = "Discard local changes")]
    public bool Force { get; set; }
    
    [Option("-b", Description = "Create and checkout new branch")]
    public bool CreateBranch { get; set; }
    
    public async Task<int> ExecuteAsync(
        IGitService git,
        IConsole console,
        CancellationToken ct)
    {
        var cwd = Directory.GetCurrentDirectory();
        
        await git.CheckoutAsync(cwd, Target, new CheckoutOptions
        {
            Force = Force,
            CreateBranch = CreateBranch
        }, ct);
        
        console.WriteLine($"✓ Switched to '{Target}'");
        return 0;
    }
}
```

### Error Codes

| Code | Name | Description | Recovery |
|------|------|-------------|----------|
| GIT-022B-001 | DeleteCurrentBranch | Cannot delete current branch | Checkout different branch first |
| GIT-022B-002 | BranchNotMerged | Branch not fully merged | Use --force or merge first |
| GIT-022B-003 | InvalidBranchName | Branch name invalid | Use valid name |
| GIT-022B-004 | BranchExists | Branch already exists | Use different name or --force |
| GIT-022B-005 | BranchNotFound | Branch does not exist | Check branch name |
| GIT-022B-006 | UncommittedChanges | Uncommitted changes block checkout | Commit/stash or use --force |

### Implementation Checklist

- [ ] Implement BranchNameValidator with all rules
- [ ] Implement BranchParser for list output
- [ ] Add CreateBranchAsync to GitService
- [ ] Add CheckoutAsync with dirty tree check
- [ ] Add ListBranchesAsync with formatting
- [ ] Add DeleteBranchAsync with safety checks
- [ ] Implement CLI branch create command
- [ ] Implement CLI checkout command
- [ ] Implement CLI branch list command
- [ ] Implement CLI branch delete command
- [ ] Add unit tests for validator
- [ ] Add integration tests with real repos
- [ ] Run performance benchmarks

### Rollout Plan

| Phase | Action | Validation |
|-------|--------|------------|
| 1 | Implement BranchNameValidator | Validation tests pass |
| 2 | Implement BranchParser | Parser tests pass |
| 3 | Add CreateBranchAsync | Create tests pass |
| 4 | Add CheckoutAsync | Checkout tests pass |
| 5 | Add ListBranchesAsync | List tests pass |
| 6 | Add DeleteBranchAsync | Delete tests pass |
| 7 | Add CLI commands | E2E tests pass |
| 8 | Performance testing | Benchmarks pass |

---

**End of Task 022.b Specification**