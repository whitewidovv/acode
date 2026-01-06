# Task 022: Git Tool Layer

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 5 – Git Integration Layer  
**Dependencies:** Task 018 (Command Execution), Task 011 (Workspace DB)  

---

## Description

Task 022 implements the Git tool layer for Acode. All Git operations MUST be abstracted through a unified service interface. This enables consistent error handling, logging, and testing across all Git functionality.

The `IGitService` interface MUST provide programmatic access to Git operations. Status, diff, log, branch, checkout, add, commit, and push operations MUST all be available. Each operation MUST be implemented using the command execution layer (Task 018).

Git operations MUST parse and structure output. Raw git command output MUST NOT be exposed directly to callers. Structured data types MUST represent repository state, file changes, commit history, and branch information.

Error handling MUST be comprehensive. Git failures MUST produce clear, actionable error messages. Exit codes MUST be captured and translated to typed exceptions. stderr output MUST be included in error details.

The Git layer MUST respect operating modes from Task 001. Push operations MUST be blocked in local-only and airgapped modes. All network operations MUST validate mode before execution.

Configuration MUST integrate with Task 002's `.agent/config.yml` contract. Default branch names, commit message patterns, and push settings MUST be configurable. Configuration MUST support repository-specific overrides.

Authentication handling MUST be transparent. The Git layer MUST use system Git's configured credentials. Credential prompts MUST NOT block automated operations. Auth failures MUST produce clear error messages.

### Business Value

Abstracted Git operations enable safe, automated source control management. The agent can create branches, commit changes, and push to remotes without manual intervention. Structured interfaces ensure reliable integration with planning and execution workflows.

### Scope Boundaries

This task defines the core Git service interface and shared infrastructure. Specific operations are implemented in subtasks: 022.a (status/diff/log), 022.b (branch/checkout), 022.c (add/commit/push).

### Integration Points

- Task 018: Command execution for running git commands
- Task 001: Operating mode validation for network operations
- Task 002: Configuration contract for git settings
- Task 011: Workspace DB for operation logging
- Task 021: Artifact collection for git output capture

### Failure Modes

- Git not installed → Clear error with installation guidance
- Not a git repository → Detect and report clearly
- Auth failure → Capture and report, suggest credential setup
- Network timeout → Configurable timeout, retry with backoff
- Merge conflict → Detect and report, block commit

---

## Assumptions

### Technical Assumptions

1. **Git availability** - Git 2.20+ is installed and accessible via PATH on all target systems
2. **Repository initialized** - Working directory is within a valid Git repository with at least one commit
3. **Commit history exists** - Repository is not empty (has at least initial commit for log operations)
4. **Command execution layer available** - Task 018 command execution infrastructure is functional and tested
5. **Porcelain output stability** - Git porcelain output format (--porcelain=v2) remains stable across patch versions
6. **UTF-8 encoding** - All file paths and commit messages use UTF-8 encoding
7. **Shell escaping works** - Command execution layer properly escapes shell metacharacters
8. **Process spawning reliable** - System can reliably spawn git child processes without resource exhaustion
9. **Working directory writable** - User has write permissions to .git/ directory for local operations
10. **Index lock handling** - Git index.lock file contention is rare and transient (retry once is sufficient)

### Operational Assumptions

11. **Credentials pre-configured** - System Git credentials are already configured via git-credential-store, SSH keys, or credential helper
12. **Network available for remotes** - When in burst mode, network connectivity to git remotes is available and stable
13. **No credential prompts** - Git credential helper is configured to never prompt interactively (breaks automation)
14. **Reasonable repository size** - Repositories are <1GB, with <100k files, <50k commits for performance targets
15. **Single user per repository** - Concurrent git operations from different Acode instances on same repository are rare
16. **Clean working state** - Most operations assume no merge conflicts or rebase in progress
17. **Standard branch names** - Default branch is "main" or "master" (configurable via .agent/config.yml)
18. **Shallow clones acceptable** - Operations work with shallow clones (no --depth=1 assumptions)

### Integration Assumptions

19. **Mode resolver available** - Task 001 operating mode system is functional and mode transitions are rare mid-operation
20. **Configuration accessible** - Task 002 .agent/config.yml is readable and parsed correctly
21. **Workspace DB writable** - Task 011 workspace database is available for logging git operations
22. **Artifact directory available** - Task 021 artifact collection can store git command output for debugging
23. **Command timeout enforcement** - Task 018 command execution enforces timeouts and properly kills hung processes
24. **Logging infrastructure ready** - Structured logging (ILogger) is available and properly configured
25. **Exception serialization** - All exceptions can be serialized for cross-process logging

---

## Use Cases

### Use Case 1: DevBot Automated Feature Development with Git Operations

**Persona:** DevBot is an autonomous coding agent working on a new feature branch for adding user authentication to a web application.

**Before:**
- Human developer must manually create feature branch: `git checkout -b feature/auth` (30 seconds)
- Human developer must manually stage changes after each file: `git add file.cs` (15 seconds per file × 8 files = 2 minutes)
- Human developer must manually commit: `git commit -m "..."` and craft message (3 minutes)
- Human developer must manually push: `git push --set-upstream origin feature/auth` (30 seconds)
- Human developer must manually check status: `git status` between each step (10 seconds × 4 = 40 seconds)
- **Total time:** 6 minutes 40 seconds per feature workflow
- **Annual cost for developer doing 5 feature branches/week:** 5 × 52 × 6.67 min = 1,734 minutes = 28.9 hours/year × $100/hour = **$2,890/year**

**After:**
- DevBot calls `IGitService.CreateBranchAsync("feature/auth")` automatically (200ms)
- DevBot calls `IGitService.StageAllAsync()` for all changes automatically (500ms)
- DevBot calls `IGitService.CommitAsync("feat: add authentication")` automatically (1s)
- DevBot calls `IGitService.PushAsync()` with auto-upstream setup (3s with network)
- DevBot calls `IGitService.GetStatusAsync()` automatically between steps (200ms)
- **Total time:** 5 seconds per feature workflow (automated, no human intervention)
- **Time savings:** 6 min 35 sec per workflow = 99.1% reduction
- **Annual savings:** $2,890 × 0.991 = **$2,864/year per developer**
- **10-developer team:** $28,640/year
- **ROI:** Immediate (no upfront cost, pure automation benefit)

**Metrics:**
- Feature branch creation: 30s → 200ms (99.3% faster)
- Staging changes: 2 min → 500ms (99.6% faster)
- Commit creation: 3 min → 1s (99.4% faster)
- Push with upstream: 30s → 3s (90% faster)
- Developer interruptions: 5 per workflow → 0 (100% reduction)

### Use Case 2: Jordan Investigating Production Incident with Git History

**Persona:** Jordan is a DevOps engineer investigating a production incident that occurred after a recent deployment. They need to find which commits introduced the breaking change.

**Before:**
- Jordan manually runs: `git log --oneline --since="2 days ago"` (5 seconds)
- Jordan copies 50 commit SHAs to a text file manually (2 minutes)
- Jordan manually runs: `git show <sha>` for each commit to inspect changes (30 seconds × 10 commits = 5 minutes)
- Jordan manually runs: `git diff <sha1> <sha2>` to compare versions (1 minute × 5 comparisons = 5 minutes)
- Jordan manually runs: `git log --grep="bug"` to search for related fixes (30 seconds)
- Jordan manually correlates timestamps with deployment logs (10 minutes)
- **Total investigation time:** 23 minutes per incident
- **Frequency:** 12 incidents/year requiring git investigation
- **Annual time:** 23 min × 12 = 276 minutes = 4.6 hours/year
- **Annual cost:** 4.6 hours × $120/hour (DevOps rate) = **$552/year**

**After:**
- Acode agent calls `IGitService.GetLogAsync(new LogOptions { Since = "2 days ago", Limit = 50 })` (800ms)
- Agent automatically parses 50 commits into structured `GitCommit` objects (50ms)
- Agent calls `IGitService.GetDiffAsync()` for suspicious commits automatically (1.5s × 10 = 15s)
- Agent automatically correlates commit timestamps with deployment logs (2s)
- Agent searches commit messages with `LogOptions { Grep = "bug" }` automatically (500ms)
- Agent presents findings in structured report (instant)
- **Total investigation time:** 18.5 seconds (automated analysis)
- **Time savings:** 22 min 41.5 sec per incident = 98.7% reduction
- **Annual savings:** $552 × 0.987 = **$545/year per engineer**
- **5-engineer DevOps team:** $2,725/year
- **ROI:** Immediate (structured Git API enables automated root cause analysis)

**Metrics:**
- Log retrieval: Manual copy-paste → structured API (0 human time)
- Commit inspection: 5 min manual → 15s automated (98% faster)
- Diff comparison: 5 min manual → 15s automated (95% faster)
- Correlation: 10 min manual → 2s automated (99.7% faster)
- Mean Time To Identify (MTTI): 23 min → 19 sec (98.7% reduction)

### Use Case 3: Alex Compliance Audit of Code Changes

**Persona:** Alex is a security auditor reviewing all commits made to production branches over the past quarter to ensure compliance with security policies (no hardcoded secrets, proper review process, commit message standards).

**Before:**
- Alex manually runs: `git log --all --since="3 months ago" --format="%H %an %ae %s"` (10 seconds)
- Alex manually copies output to spreadsheet (15 minutes for 200 commits)
- Alex manually runs: `git show <sha>` for each commit to inspect content (2 minutes × 200 commits = 6.67 hours)
- Alex manually searches for patterns like "password", "api_key", "secret" in diffs (30 minutes across all commits)
- Alex manually verifies commit message format compliance (1 minute × 200 = 3.33 hours)
- Alex manually generates compliance report (1 hour)
- **Total audit time:** 11.5 hours per quarter
- **Annual time:** 11.5 hours × 4 quarters = 46 hours/year
- **Annual cost:** 46 hours × $150/hour (auditor rate) = **$6,900/year**

**After:**
- Acode compliance agent calls `IGitService.GetLogAsync(new LogOptions { Since = "3 months ago" })` (1.5s for 200 commits)
- Agent receives structured `GitCommit[]` with all metadata parsed (instant)
- Agent automatically inspects commit diffs using `IGitService.GetDiffAsync()` for all 200 commits (100ms × 200 = 20s)
- Agent automatically scans diffs for secret patterns using regex (5s total across all commits)
- Agent automatically validates commit message format against policy (instant with structured data)
- Agent automatically generates compliance report with violations flagged (2s)
- **Total audit time:** 28.5 seconds (automated analysis)
- **Time savings:** 11.5 hours - 28.5s = 11.49 hours per quarter (99.9% reduction)
- **Annual savings:** $6,900 × 0.999 = **$6,893/year per auditor**
- **3-auditor team:** $20,679/year
- **ROI:** Immediate (structured Git API enables automated compliance checks)

**Metrics:**
- Log collection: 10s + 15 min manual → 1.5s automated (99% faster)
- Commit content inspection: 6.67 hours → 20s (99.9% faster)
- Secret pattern scanning: 30 min → 5s (99.7% faster)
- Message format validation: 3.33 hours → instant (100% faster)
- Report generation: 1 hour → 2s (99.9% faster)
- Compliance cycle time: 11.5 hours → 28.5s (99.9% reduction)

**Combined Suite 022 ROI:**
- DevBot automation: $28,640/year (10 developers)
- Jordan incident investigation: $2,725/year (5 engineers)
- Alex compliance audit: $20,679/year (3 auditors)
- **Total annual value:** $52,044/year for typical 10-person engineering team
- **Payback period:** Immediate (zero infrastructure cost, pure productivity gain)

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Working Directory | The directory containing the Git checkout |
| Staging Area | Files added for the next commit (index) |
| HEAD | Reference to the current commit |
| Branch | Named pointer to a commit |
| Remote | Server-hosted copy of the repository |
| Origin | Default name for the primary remote |
| Commit | Snapshot of repository state with metadata |
| Diff | Difference between two states (commits, index, working) |
| Worktree | Additional working directory linked to repository |
| Checkout | Switch to a different branch or commit |
| Push | Upload local commits to remote |
| Pull | Download and integrate remote commits |
| Merge | Combine two branches |
| Conflict | Incompatible changes requiring resolution |
| Stash | Temporarily saved working changes |

---

## Out of Scope

- GitHub/GitLab/Azure DevOps API integration
- Pull request creation or management
- Merge conflict resolution
- Git LFS operations
- Submodule management
- Git hooks management
- Interactive rebase
- Cherry-pick operations
- Stash management
- Repository cloning
- SSH key management
- GPG signing

---

## Functional Requirements

### FR-001 to FR-020: Core Interface

- FR-001: `IGitService` interface MUST be defined in Application layer
- FR-002: All Git operations MUST accept working directory parameter
- FR-003: All operations MUST return structured result types
- FR-004: All operations MUST throw typed exceptions on failure
- FR-005: Interface MUST include `GetStatusAsync` method
- FR-006: Interface MUST include `GetDiffAsync` method
- FR-007: Interface MUST include `GetLogAsync` method
- FR-008: Interface MUST include `CreateBranchAsync` method
- FR-009: Interface MUST include `CheckoutAsync` method
- FR-010: Interface MUST include `StageAsync` method
- FR-011: Interface MUST include `CommitAsync` method
- FR-012: Interface MUST include `PushAsync` method
- FR-013: Interface MUST include `GetCurrentBranchAsync` method
- FR-014: Interface MUST include `GetRemotesAsync` method
- FR-015: Interface MUST include `IsRepositoryAsync` method
- FR-016: All methods MUST support cancellation tokens
- FR-017: All methods MUST be async/await compatible
- FR-018: Implementation MUST use Task 018 command execution
- FR-019: Implementation MUST parse git output to structured types
- FR-020: Implementation MUST capture both stdout and stderr

### FR-021 to FR-040: Error Handling

- FR-021: `GitException` base class MUST be defined
- FR-022: `NotARepositoryException` MUST be defined
- FR-023: `BranchNotFoundException` MUST be defined
- FR-024: `MergeConflictException` MUST be defined
- FR-025: `AuthenticationException` MUST be defined
- FR-026: `NetworkException` MUST be defined
- FR-027: `PushRejectedException` MUST be defined
- FR-028: All exceptions MUST include git stderr output
- FR-029: All exceptions MUST include exit code
- FR-030: All exceptions MUST include working directory
- FR-031: Exception messages MUST be user-friendly
- FR-032: Exception messages MUST suggest remediation
- FR-033: Exit code 128 MUST map to repository/permission errors
- FR-034: Exit code 1 with conflict markers MUST trigger conflict exception
- FR-035: Network errors MUST be detected from stderr patterns
- FR-036: Auth errors MUST be detected from stderr patterns
- FR-037: Timeout MUST throw `GitTimeoutException`
- FR-038: Git not found MUST throw `GitNotFoundException`
- FR-039: All exceptions MUST be serializable for logging
- FR-040: Inner exceptions MUST preserve original stack trace

### FR-041 to FR-060: Configuration

- FR-041: Git settings MUST be read from Task 002 config
- FR-042: `git.defaultBranch` MUST be configurable
- FR-043: `git.defaultRemote` MUST be configurable (default: origin)
- FR-044: `git.timeoutSeconds` MUST be configurable (default: 60)
- FR-045: `git.retryCount` MUST be configurable (default: 3)
- FR-046: `git.retryDelayMs` MUST be configurable (default: 1000)
- FR-047: Configuration MUST support environment variable override
- FR-048: Configuration MUST support repository-level override
- FR-049: Missing config MUST use sensible defaults
- FR-050: Invalid config MUST produce clear validation error
- FR-051: Git executable path MUST be configurable
- FR-052: Default git path MUST be "git" (from PATH)
- FR-053: Custom git path MUST be validated on startup
- FR-054: Git version MUST be detected and logged
- FR-055: Minimum git version MUST be 2.20
- FR-056: Version below minimum MUST produce warning
- FR-057: Version check MUST cache result
- FR-058: Environment variables MUST be passable to git commands
- FR-059: GIT_DIR override MUST be supported
- FR-060: GIT_WORK_TREE override MUST be supported

### FR-061 to FR-080: Mode Compliance

- FR-061: Operating mode MUST be checked before network operations
- FR-062: Push MUST be blocked in local-only mode
- FR-063: Push MUST be blocked in airgapped mode
- FR-064: Fetch MUST be blocked in airgapped mode
- FR-065: Pull MUST be blocked in airgapped mode
- FR-066: Mode violation MUST throw `ModeViolationException`
- FR-067: Mode violation MUST log security event
- FR-068: Local operations MUST work in all modes
- FR-069: Status MUST work in all modes
- FR-070: Diff MUST work in all modes
- FR-071: Log MUST work in all modes
- FR-072: Branch create MUST work in all modes
- FR-073: Checkout MUST work in all modes
- FR-074: Add MUST work in all modes
- FR-075: Commit MUST work in all modes
- FR-076: Mode MUST be resolved per-operation
- FR-077: Mode override MUST NOT be possible via config
- FR-078: Mode source MUST be logged for audit
- FR-079: Burst mode MUST enable all operations
- FR-080: Mode transition MUST NOT affect in-flight operations

---

## Non-Functional Requirements

### NFR-001 to NFR-010: Performance

- NFR-001: Status check MUST complete in <500ms for repos <10000 files
- NFR-002: Log retrieval MUST complete in <1s for 1000 commits
- NFR-003: Diff generation MUST complete in <2s for diffs <10MB
- NFR-004: Branch operations MUST complete in <200ms
- NFR-005: Commit MUST complete in <1s (excluding hooks)
- NFR-006: Git command spawning MUST complete in <50ms
- NFR-007: Output parsing MUST be streaming for large outputs
- NFR-008: Memory MUST NOT exceed 50MB for normal operations
- NFR-009: Concurrent operations MUST NOT deadlock
- NFR-010: Command queue MUST support 10 concurrent operations

### NFR-011 to NFR-020: Reliability

- NFR-011: Partial command output MUST be captured on timeout
- NFR-012: Killed processes MUST be detected and reported
- NFR-013: File locks MUST be detected and reported
- NFR-014: Stale lock files MUST suggest removal
- NFR-015: Interrupted operations MUST NOT corrupt repository
- NFR-016: Retry logic MUST use exponential backoff
- NFR-017: Network retry MUST respect timeout budget
- NFR-018: Operation cancel MUST terminate git process
- NFR-019: Zombie processes MUST be prevented
- NFR-020: Resource cleanup MUST be guaranteed via using/finally

### NFR-021 to NFR-030: Security

- NFR-021: Credentials MUST NOT appear in logs
- NFR-022: URLs with embedded credentials MUST be redacted
- NFR-023: SSH keys MUST NOT be logged
- NFR-024: Token values MUST be masked in error messages
- NFR-025: Command arguments MUST be sanitized
- NFR-026: Path injection MUST be prevented
- NFR-027: Shell escaping MUST be applied
- NFR-028: Environment variables MUST be filtered for secrets
- NFR-029: Temporary files MUST be created securely
- NFR-030: Temporary files MUST be cleaned up on completion

---

## User Manual Documentation

### Quick Start

The Git tool layer provides programmatic access to Git operations. It is used internally by Acode and exposed through CLI commands.

```bash
# Check repository status
acode git status

# View recent commits
acode git log --limit 10

# Create a new branch
acode git branch create feature/my-feature
```

### Configuration

Git settings in `.agent/config.yml`:

```yaml
git:
  # Default branch for new repositories
  defaultBranch: main
  
  # Default remote name
  defaultRemote: origin
  
  # Timeout for git operations (seconds)
  timeoutSeconds: 60
  
  # Retry configuration for network operations
  retryCount: 3
  retryDelayMs: 1000
  
  # Custom git executable path (optional)
  executable: git
```

### Programmatic Usage

```csharp
// Inject IGitService
public class MyHandler
{
    private readonly IGitService _git;
    
    public MyHandler(IGitService git)
    {
        _git = git;
    }
    
    public async Task DoWorkAsync(string repoPath)
    {
        // Get status
        var status = await _git.GetStatusAsync(repoPath);
        
        // Create branch
        await _git.CreateBranchAsync(repoPath, "feature/work");
        
        // Stage files
        await _git.StageAsync(repoPath, new[] { "file.cs" });
        
        // Commit
        await _git.CommitAsync(repoPath, "feat: add feature");
    }
}
```

### Error Handling

```csharp
try
{
    await _git.PushAsync(repoPath, new PushOptions());
}
catch (ModeViolationException ex)
{
    // Push blocked by operating mode
    logger.LogWarning("Push blocked: {Mode}", ex.CurrentMode);
}
catch (AuthenticationException ex)
{
    // Credentials not configured
    logger.LogError("Auth failed: {Message}", ex.Message);
}
catch (NetworkException ex)
{
    // Network unreachable
    logger.LogError("Network error: {Message}", ex.Message);
}
```

### Exit Codes (CLI)

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Git operation failed |
| 2 | Invalid arguments |
| 3 | Mode violation |
| 4 | Authentication failure |
| 5 | Network error |

### Troubleshooting

**Q: "Git executable not found"**

Ensure git is installed and in PATH:
```bash
git --version
```

**Q: "Not a git repository"**

Verify you're in a git repository:
```bash
git rev-parse --git-dir
```

**Q: "Push blocked by operating mode"**

Push requires burst mode:
```bash
acode config set mode burst
```

**Q: "Authentication failed"**

Configure git credentials:
```bash
git config credential.helper store
git push  # Enter credentials once
```

---

## Security Considerations

### Threat 1: Credential Exposure in Logs and Error Messages

**Risk:** Git URLs often contain embedded credentials (e.g., `https://user:password@github.com/repo.git`). If these URLs appear in logs, error messages, or exception stack traces, credentials are exposed to anyone with log access.

**Attack Scenario:**
1. Developer configures remote with embedded credentials: `git remote add origin https://token:ghp_abc123@github.com/repo.git`
2. Push operation fails due to network error
3. GitService logs error message including full URL with token
4. Attacker with log access (e.g., compromised log aggregator, insider threat) extracts token
5. Attacker uses token to access private repository and steal source code

**Mitigation:**

```csharp
// CredentialRedactor.cs - Complete implementation
namespace Acode.Infrastructure.Git;

public sealed class CredentialRedactor : ICredentialRedactor
{
    // Regex patterns for detecting credentials in various formats
    private static readonly Regex UrlWithEmbeddedCredentials = new(
        @"(https?://)([^:]+):([^@]+)@",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex SshUrlWithPassword = new(
        @"(ssh://[^:]+):([^@]+)@",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex GitCredentialLine = new(
        @"(password|token|secret|key|credential|auth|bearer|api_key|apikey)\s*[:=]\s*\S+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly string[] SensitiveEnvironmentVariables =
    {
        "GIT_ASKPASS", "GIT_PASSWORD", "GIT_TOKEN", "GITHUB_TOKEN",
        "GITLAB_TOKEN", "GIT_CREDENTIALS"
    };

    public string Redact(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Step 1: Redact URLs with embedded credentials
        var result = UrlWithEmbeddedCredentials.Replace(input, "$1[REDACTED]@");
        result = SshUrlWithPassword.Replace(result, "$1[REDACTED]@");

        // Step 2: Redact lines containing credential keywords
        var lines = result.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (GitCredentialLine.IsMatch(lines[i]))
            {
                var match = GitCredentialLine.Match(lines[i]);
                var keyPart = match.Value[..match.Value.IndexOfAny(new[] { ':', '=' }) + 1];
                lines[i] = GitCredentialLine.Replace(lines[i], $"{keyPart}[REDACTED]");
            }
        }

        // Step 3: Redact sensitive environment variables
        result = string.Join('\n', lines);
        foreach (var envVar in SensitiveEnvironmentVariables)
        {
            var pattern = $@"{envVar}\s*=\s*\S+";
            result = Regex.Replace(result, pattern, $"{envVar}=[REDACTED]", RegexOptions.IgnoreCase);
        }

        return result;
    }

    public string RedactUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return url;

        return UrlWithEmbeddedCredentials.Replace(url, "$1[REDACTED]@");
    }
}

// Usage in GitService.cs
private GitException MapToException(CommandResult result, string workingDir)
{
    var stderr = result.StdErr ?? "";
    var redactedStderr = _redactor.Redact(stderr);
    var redactedWorkingDir = workingDir; // Path is safe

    _logger.LogError(
        "Git command failed: Exit={ExitCode}, StdErr={StdErr}, WorkingDir={WorkingDir}",
        result.ExitCode,
        redactedStderr, // Always redacted
        redactedWorkingDir);

    return result.ExitCode switch
    {
        128 when stderr.Contains("not a git repository") =>
            new NotARepositoryException(workingDir),
        // ... other exception mappings with redacted stderr
        _ => new GitException($"Git command failed: {redactedStderr}",
            result.ExitCode, redactedStderr, workingDir, "GIT_000")
    };
}
```

---

### Threat 2: Command Injection via Unsanitized Paths

**Risk:** If file paths or branch names from user input are passed to git commands without proper escaping, an attacker can inject shell commands that execute with Acode's privileges.

**Attack Scenario:**
1. Attacker crafts malicious branch name: `feature/test; rm -rf /tmp/important; #`
2. Agent calls `IGitService.CheckoutAsync(repoPath, maliciousBranchName)`
3. GitService constructs command: `git checkout feature/test; rm -rf /tmp/important; #`
4. Command execution layer executes as shell command (if not properly escaped)
5. Malicious `rm` command executes, deleting critical files

**Mitigation:**

```csharp
// PathSanitizer.cs - Complete implementation
namespace Acode.Infrastructure.Git;

public sealed class PathSanitizer : IPathSanitizer
{
    private static readonly char[] ShellMetacharacters =
    {
        ';', '&', '|', '<', '>', '`', '$', '(', ')', '{', '}', '[', ']',
        '\\', '"', '\'', '\n', '\r', '\t'
    };

    private static readonly Regex BranchNamePattern = new(
        @"^[a-zA-Z0-9/_\-\.]+$",
        RegexOptions.Compiled);

    private static readonly Regex PathTraversalPattern = new(
        @"\.\./|\.\.\\",
        RegexOptions.Compiled);

    public string SanitizeBranchName(string branchName)
    {
        if (string.IsNullOrWhiteSpace(branchName))
            throw new ArgumentException("Branch name cannot be empty", nameof(branchName));

        // Check for shell metacharacters
        if (branchName.IndexOfAny(ShellMetacharacters) >= 0)
            throw new GitException(
                $"Branch name contains invalid characters: {branchName}",
                0, null, null, "GIT_INVALID_INPUT");

        // Validate against allowed pattern
        if (!BranchNamePattern.IsMatch(branchName))
            throw new GitException(
                $"Branch name format invalid: {branchName}. Use only alphanumeric, /, -, _, .",
                0, null, null, "GIT_INVALID_INPUT");

        // Check for path traversal
        if (PathTraversalPattern.IsMatch(branchName))
            throw new GitException(
                $"Branch name contains path traversal: {branchName}",
                0, null, null, "GIT_INVALID_INPUT");

        return branchName;
    }

    public string SanitizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be empty", nameof(path));

        // Normalize path separators
        var normalized = path.Replace('\\', '/');

        // Check for path traversal
        if (PathTraversalPattern.IsMatch(normalized))
            throw new GitException(
                $"Path contains traversal sequences: {path}",
                0, null, null, "GIT_INVALID_INPUT");

        // Check for absolute paths outside repository
        if (Path.IsPathRooted(path) && !path.StartsWith("/repo/", StringComparison.Ordinal))
        {
            throw new GitException(
                $"Absolute path not allowed: {path}",
                0, null, null, "GIT_INVALID_INPUT");
        }

        // Shell metacharacter check (especially for file operations)
        if (path.IndexOfAny(ShellMetacharacters) >= 0)
        {
            // Log warning but allow (git handles quoting)
            // Command execution layer will properly escape
        }

        return normalized;
    }
}

// Usage in GitService.cs
public async Task CheckoutAsync(string workingDir, string branchOrCommit, CancellationToken ct = default)
{
    // Sanitize inputs before constructing command
    var sanitizedBranch = _sanitizer.SanitizeBranchName(branchOrCommit);
    var sanitizedWorkingDir = _sanitizer.SanitizePath(workingDir);

    await EnsureRepositoryAsync(sanitizedWorkingDir, ct);

    // Command execution layer will properly quote arguments
    var args = new[] { "checkout", sanitizedBranch };
    await ExecuteGitAsync(sanitizedWorkingDir, args, ct);

    _logger.LogInformation("Checked out {Branch} in {Dir}", sanitizedBranch, sanitizedWorkingDir);
}
```

---

### Threat 3: Repository Escape via Relative Paths

**Risk:** If working directory parameter is not validated, an attacker can use relative paths (e.g., `../../../../etc`) to execute git commands outside the intended repository, potentially accessing sensitive system files.

**Attack Scenario:**
1. Attacker provides malicious working directory: `../../../../etc`
2. Agent calls `IGitService.GetStatusAsync(maliciousPath)`
3. GitService executes `git -C ../../../../etc status`
4. Git attempts to access /etc/.git/ (doesn't exist, but info leak if it did)
5. Error messages reveal filesystem structure and permissions

**Mitigation:**

```csharp
// RepositoryValidator.cs - Complete implementation
namespace Acode.Infrastructure.Git;

public sealed class RepositoryValidator : IRepositoryValidator
{
    private readonly ICommandExecutor _executor;
    private readonly ILogger<RepositoryValidator> _logger;

    // Cache validated repositories to avoid repeated checks
    private readonly ConcurrentDictionary<string, (bool IsValid, DateTime ValidatedAt)> _cache = new();
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public async Task<string> ValidateAndNormalizeAsync(string workingDir, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(workingDir))
            throw new ArgumentException("Working directory cannot be empty", nameof(workingDir));

        // Step 1: Resolve to absolute path
        var absolutePath = Path.GetFullPath(workingDir);

        // Step 2: Check cache
        if (_cache.TryGetValue(absolutePath, out var cached) &&
            DateTime.UtcNow - cached.ValidatedAt < CacheExpiration &&
            cached.IsValid)
        {
            return absolutePath;
        }

        // Step 3: Verify directory exists
        if (!Directory.Exists(absolutePath))
            throw new NotARepositoryException(absolutePath);

        // Step 4: Verify it's a git repository (use rev-parse --git-dir)
        try
        {
            var command = new CommandSpec
            {
                Executable = "git",
                Arguments = new[] { "-C", absolutePath, "rev-parse", "--git-dir" },
                Timeout = TimeSpan.FromSeconds(5),
                RedactSecrets = false
            };

            var result = await _executor.ExecuteAsync(command, ct);

            if (result.ExitCode != 0)
            {
                _logger.LogWarning(
                    "Not a git repository: {Path}, Exit={Exit}, StdErr={StdErr}",
                    absolutePath, result.ExitCode, result.StdErr);
                throw new NotARepositoryException(absolutePath);
            }

            // Step 5: Verify git-dir is within working directory (prevent escape)
            var gitDir = result.StdOut.Trim();
            var gitDirAbsolute = Path.IsPathRooted(gitDir)
                ? gitDir
                : Path.GetFullPath(Path.Combine(absolutePath, gitDir));

            if (!gitDirAbsolute.StartsWith(absolutePath, StringComparison.Ordinal) &&
                !gitDirAbsolute.StartsWith(Path.Combine(absolutePath, ".git"), StringComparison.Ordinal))
            {
                _logger.LogWarning(
                    "Git directory outside working directory: WorkingDir={WorkingDir}, GitDir={GitDir}",
                    absolutePath, gitDirAbsolute);
                throw new GitException(
                    $"Git directory outside repository bounds: {gitDirAbsolute}",
                    0, null, absolutePath, "GIT_SECURITY_VIOLATION");
            }

            // Step 6: Cache successful validation
            _cache[absolutePath] = (true, DateTime.UtcNow);

            return absolutePath;
        }
        catch (CommandExecutionException ex)
        {
            _logger.LogError(ex, "Failed to validate repository: {Path}", absolutePath);
            throw new NotARepositoryException(absolutePath);
        }
    }

    public void InvalidateCache(string workingDir)
    {
        var absolutePath = Path.GetFullPath(workingDir);
        _cache.TryRemove(absolutePath, out _);
    }
}

// Usage in GitService.cs - Always validate first
private async Task EnsureRepositoryAsync(string workingDir, CancellationToken ct)
{
    var validatedPath = await _validator.ValidateAndNormalizeAsync(workingDir, ct);
    // All subsequent operations use validatedPath
}
```

---

### Threat 4: Sensitive Data Exposure via Unredacted Diff Output

**Risk:** Git diff output may contain sensitive data (API keys, passwords, tokens) that was accidentally committed. If diff output is logged or displayed without redaction, these secrets are exposed.

**Attack Scenario:**
1. Developer accidentally commits file containing `API_KEY=sk_live_abc123`
2. Agent calls `IGitService.GetDiffAsync()` to review changes
3. Diff output includes full API key in plain text
4. Diff output is logged to artifact collection (Task 021)
5. Attacker with artifact access extracts API key from logs
6. Attacker uses key to access production API

**Mitigation:**

```csharp
// DiffRedactor.cs - Complete implementation
namespace Acode.Infrastructure.Git;

public sealed class DiffRedactor : IDiffRedactor
{
    private static readonly Regex[] SensitivePatterns =
    {
        // API keys (various formats)
        new Regex(@"(?i)api[_-]?key\s*[:=]\s*['\""]*([a-z0-9_\-]{20,})['\""]*", RegexOptions.Compiled),
        new Regex(@"(?i)(sk|pk)_live_[a-zA-Z0-9]{24,}", RegexOptions.Compiled),

        // Passwords
        new Regex(@"(?i)password\s*[:=]\s*['\""]*([^\s'""]+)['\""]*", RegexOptions.Compiled),

        // Tokens (GitHub, GitLab, etc.)
        new Regex(@"(?i)(ghp|gho|ghu|ghs|ghr|glpat)_[a-zA-Z0-9]{36,}", RegexOptions.Compiled),

        // AWS credentials
        new Regex(@"AKIA[0-9A-Z]{16}", RegexOptions.Compiled),
        new Regex(@"(?i)aws_secret_access_key\s*[:=]\s*['\""]*([a-zA-Z0-9/+]{40})['\""]*", RegexOptions.Compiled),

        // Private keys (PEM format)
        new Regex(@"-----BEGIN (RSA |DSA |EC )?PRIVATE KEY-----[^-]+-----END (RSA |DSA |EC )?PRIVATE KEY-----",
            RegexOptions.Compiled | RegexOptions.Singleline),

        // Connection strings
        new Regex(@"(?i)(Server|Host|Data Source)\s*=\s*[^;]+;\s*(User Id|UID)\s*=\s*[^;]+;\s*Password\s*=\s*[^;]+",
            RegexOptions.Compiled),

        // JWT tokens
        new Regex(@"eyJ[a-zA-Z0-9_-]*\.eyJ[a-zA-Z0-9_-]*\.[a-zA-Z0-9_-]*", RegexOptions.Compiled),

        // Generic secrets (base64-looking strings >20 chars after "secret")
        new Regex(@"(?i)secret\s*[:=]\s*['\""]*([a-zA-Z0-9+/]{20,}={0,2})['\""]*", RegexOptions.Compiled)
    };

    private readonly ILogger<DiffRedactor> _logger;

    public string RedactDiff(string diffOutput)
    {
        if (string.IsNullOrEmpty(diffOutput))
            return diffOutput;

        var redactedCount = 0;
        var result = diffOutput;

        foreach (var pattern in SensitivePatterns)
        {
            var matches = pattern.Matches(result);
            if (matches.Count > 0)
            {
                redactedCount += matches.Count;
                result = pattern.Replace(result, match =>
                {
                    // Keep pattern prefix, redact value
                    var prefix = match.Value[..Math.Min(match.Value.IndexOf('=') + 1, 20)];
                    return $"{prefix}[REDACTED]";
                });
            }
        }

        if (redactedCount > 0)
        {
            _logger.LogWarning(
                "Redacted {Count} sensitive patterns from diff output",
                redactedCount);
        }

        return result;
    }

    public bool ContainsSensitiveData(string content)
    {
        return SensitivePatterns.Any(pattern => pattern.IsMatch(content));
    }
}

// Usage in GitService.cs
public async Task<string> GetDiffAsync(string workingDir, DiffOptions options, CancellationToken ct = default)
{
    await EnsureRepositoryAsync(workingDir, ct);

    var args = new List<string> { "diff" };
    // ... build diff args ...

    var result = await ExecuteGitAsync(workingDir, args, ct);

    // ALWAYS redact diff output before returning
    var redactedDiff = _diffRedactor.RedactDiff(result.StdOut);

    return redactedDiff;
}
```

---

### Threat 5: Mode Bypass via Configuration Tampering

**Risk:** If git configuration file (.agent/config.yml) is writable by untrusted processes, an attacker can modify operating mode settings to bypass network operation restrictions.

**Attack Scenario:**
1. System is configured in local-only mode (no network operations allowed)
2. Attacker gains write access to .agent/config.yml (e.g., via misconfigured permissions)
3. Attacker modifies: `operatingMode: burst` in config file
4. Agent reloads configuration and switches to burst mode
5. Attacker calls `IGitService.PushAsync()` to exfiltrate code to external repository
6. Source code leaked to attacker-controlled remote

**Mitigation:**

```csharp
// ModeEnforcementService.cs - Complete implementation
namespace Acode.Infrastructure.Git;

public sealed class ModeEnforcementService : IModeEnforcementService
{
    private readonly IModeResolver _modeResolver;
    private readonly IConfigurationValidator _configValidator;
    private readonly ILogger<ModeEnforcementService> _logger;

    // Cached mode to detect unauthorized changes
    private OperatingMode? _lastValidatedMode;
    private DateTime _lastValidationTime;
    private static readonly TimeSpan ValidationExpiration = TimeSpan.FromMinutes(1);

    public async Task<OperatingMode> GetAndValidateModeAsync(CancellationToken ct = default)
    {
        // Step 1: Get current mode from resolver
        var currentMode = await _modeResolver.GetCurrentModeAsync(ct);

        // Step 2: Validate configuration file integrity
        var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".agent", "config.yml");
        if (File.Exists(configPath))
        {
            var configHash = await ComputeFileHashAsync(configPath, ct);
            var (isValid, reason) = await _configValidator.ValidateIntegrityAsync(configPath, configHash, ct);

            if (!isValid)
            {
                _logger.LogCritical(
                    "Configuration file integrity check failed: {Reason}. Falling back to most restrictive mode (LocalOnly).",
                    reason);

                // Fallback to most restrictive mode
                return OperatingMode.LocalOnly;
            }
        }

        // Step 3: Detect suspicious mode transitions
        if (_lastValidatedMode.HasValue &&
            DateTime.UtcNow - _lastValidationTime < ValidationExpiration &&
            _lastValidatedMode.Value != currentMode)
        {
            _logger.LogWarning(
                "Operating mode changed unexpectedly: {OldMode} -> {NewMode}. Validating transition...",
                _lastValidatedMode.Value, currentMode);

            // Audit log the transition
            await LogModeTransitionAsync(_lastValidatedMode.Value, currentMode, ct);
        }

        // Step 4: Update cache
        _lastValidatedMode = currentMode;
        _lastValidationTime = DateTime.UtcNow;

        return currentMode;
    }

    public async Task ValidateNetworkOperationAsync(string operationName, CancellationToken ct = default)
    {
        var mode = await GetAndValidateModeAsync(ct);

        if (mode is OperatingMode.LocalOnly or OperatingMode.Airgapped)
        {
            _logger.LogCritical(
                "Security violation: Network operation '{Operation}' blocked in {Mode} mode",
                operationName, mode);

            // Audit log security event
            await LogSecurityViolationAsync(operationName, mode, ct);

            throw new ModeViolationException(mode.ToString(), operationName);
        }

        _logger.LogInformation(
            "Network operation '{Operation}' permitted in {Mode} mode",
            operationName, mode);
    }

    private async Task<string> ComputeFileHashAsync(string path, CancellationToken ct)
    {
        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(path);
        var hash = await sha256.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash);
    }

    private async Task LogModeTransitionAsync(OperatingMode oldMode, OperatingMode newMode, CancellationToken ct)
    {
        // Log to audit trail (Task 011 workspace DB)
        _logger.LogInformation(
            "Mode transition: {OldMode} -> {NewMode}, Timestamp={Timestamp}",
            oldMode, newMode, DateTime.UtcNow);
    }

    private async Task LogSecurityViolationAsync(string operation, OperatingMode mode, CancellationToken ct)
    {
        // Log to security audit trail
        _logger.LogCritical(
            "SECURITY VIOLATION: Operation={Operation}, Mode={Mode}, Timestamp={Timestamp}, User={User}",
            operation, mode, DateTime.UtcNow, Environment.UserName);
    }
}

// Usage in GitService.cs - ALWAYS check mode before network operations
public async Task PushAsync(string workingDir, PushOptions options, CancellationToken ct = default)
{
    // MANDATORY mode enforcement before any network operation
    await _modeEnforcement.ValidateNetworkOperationAsync("push", ct);

    await EnsureRepositoryAsync(workingDir, ct);

    // ... proceed with push operation ...
}

public async Task FetchAsync(string workingDir, FetchOptions? options = null, CancellationToken ct = default)
{
    // MANDATORY mode enforcement
    await _modeEnforcement.ValidateNetworkOperationAsync("fetch", ct);

    // ... proceed with fetch operation ...
}
```

---

## Acceptance Criteria / Definition of Done

### Functionality

- [ ] AC-001: `IGitService` interface defined in Application layer
- [ ] AC-002: `GetStatusAsync` returns structured status
- [ ] AC-003: `GetDiffAsync` returns diff content
- [ ] AC-004: `GetLogAsync` returns commit list
- [ ] AC-005: `CreateBranchAsync` creates new branch
- [ ] AC-006: `CheckoutAsync` switches branches
- [ ] AC-007: `StageAsync` adds files to index
- [ ] AC-008: `CommitAsync` creates commit
- [ ] AC-009: `PushAsync` uploads to remote
- [ ] AC-010: `GetCurrentBranchAsync` returns current branch
- [ ] AC-011: `GetRemotesAsync` returns remote list
- [ ] AC-012: `IsRepositoryAsync` detects git repos
- [ ] AC-013: All methods support cancellation
- [ ] AC-014: All methods use command execution layer

### Error Handling

- [ ] AC-015: `GitException` base class exists
- [ ] AC-016: `NotARepositoryException` thrown appropriately
- [ ] AC-017: `BranchNotFoundException` thrown appropriately
- [ ] AC-018: `AuthenticationException` thrown appropriately
- [ ] AC-019: `NetworkException` thrown appropriately
- [ ] AC-020: All exceptions include stderr
- [ ] AC-021: All exceptions include exit code
- [ ] AC-022: Error messages are user-friendly

### Mode Compliance

- [ ] AC-023: Push blocked in local-only mode
- [ ] AC-024: Push blocked in airgapped mode
- [ ] AC-025: Push works in burst mode
- [ ] AC-026: Local operations work in all modes
- [ ] AC-027: Mode violation logged as security event

### Configuration

- [ ] AC-028: Default branch configurable
- [ ] AC-029: Timeout configurable
- [ ] AC-030: Retry count configurable
- [ ] AC-031: Git path configurable
- [ ] AC-032: Invalid config produces clear error

### Security

- [ ] AC-033: Credentials not logged
- [ ] AC-034: URLs with credentials redacted
- [ ] AC-035: Path injection prevented
- [ ] AC-036: Shell escaping applied

### Performance

- [ ] AC-037: Status completes in <500ms
- [ ] AC-038: Log retrieval completes in <1s
- [ ] AC-039: Memory stays under 50MB

### Tests

- [ ] AC-040: Unit test coverage >90%
- [ ] AC-041: Integration tests with real git repo
- [ ] AC-042: E2E tests for CLI commands

---

## Best Practices

### Git Integration

1. **Use porcelain output** - Parse --porcelain or --format for reliable parsing
2. **Handle all git versions** - Test with git 2.20+ for compatibility
3. **Never parse human output** - Human-readable output changes between versions
4. **Escape paths correctly** - Handle spaces, quotes, unicode in file paths

### Error Handling

5. **Capture stderr** - Git errors go to stderr; always capture both streams
6. **Parse exit codes** - Non-zero exit means failure; code indicates type
7. **Provide context** - Include file path and operation in error messages
8. **Retry on lock** - Git index.lock may be temporary; retry once

### Security

9. **Sanitize paths** - Validate paths don't escape repository
10. **No credentials in logs** - Never log authentication tokens
11. **Respect gitignore** - Honor ignore rules in all operations
12. **Audit sensitive operations** - Log commits, pushes for audit trail

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Test GitStatus parsing from git output
- [ ] UT-002: Test FileStatus enum mapping
- [ ] UT-003: Test GitCommit parsing from log output
- [ ] UT-004: Test branch name parsing
- [ ] UT-005: Test remote URL parsing
- [ ] UT-006: Test exit code to exception mapping
- [ ] UT-007: Test stderr pattern matching for auth errors
- [ ] UT-008: Test stderr pattern matching for network errors
- [ ] UT-009: Test configuration loading
- [ ] UT-010: Test configuration defaults
- [ ] UT-011: Test mode validation logic
- [ ] UT-012: Test credential redaction
- [ ] UT-013: Test path sanitization
- [ ] UT-014: Test timeout handling
- [ ] UT-015: Test retry logic

### Integration Tests

- [ ] IT-001: Test status on real repository
- [ ] IT-002: Test diff generation on real changes
- [ ] IT-003: Test log retrieval on real commits
- [ ] IT-004: Test branch creation on real repo
- [ ] IT-005: Test checkout on real repo
- [ ] IT-006: Test staging on real files
- [ ] IT-007: Test commit on real changes
- [ ] IT-008: Test concurrent operations
- [ ] IT-009: Test operation cancellation
- [ ] IT-010: Test with various git versions

### End-to-End Tests

- [ ] E2E-001: CLI status command works
- [ ] E2E-002: CLI log command works
- [ ] E2E-003: CLI branch create works
- [ ] E2E-004: CLI checkout works
- [ ] E2E-005: CLI add/commit workflow works
- [ ] E2E-006: Mode blocking prevents push
- [ ] E2E-007: Error messages are clear
- [ ] E2E-008: Exit codes are correct

### Performance/Benchmarks

- [ ] PB-001: Status in <500ms for 10000 files
- [ ] PB-002: Log 1000 commits in <1s
- [ ] PB-003: Diff 10MB in <2s
- [ ] PB-004: Branch create in <200ms
- [ ] PB-005: Memory under 50MB for all operations

### Regression

- [ ] RG-001: Verify Task 018 command execution compatibility
- [ ] RG-002: Verify Task 001 mode integration
- [ ] RG-003: Verify Task 002 config integration

---

## User Verification Steps

1. **Verify status works:**
   ```bash
   acode git status
   ```
   Verify: Shows branch name, clean/dirty state, file list

2. **Verify log works:**
   ```bash
   acode git log --limit 5
   ```
   Verify: Shows 5 most recent commits with hash and message

3. **Verify branch create works:**
   ```bash
   acode git branch create test-branch
   ```
   Verify: Branch created successfully

4. **Verify checkout works:**
   ```bash
   acode git checkout test-branch
   ```
   Verify: Switched to test-branch

5. **Verify not-a-repo error:**
   ```bash
   cd /tmp && acode git status
   ```
   Verify: Error "Not a git repository"

6. **Verify mode blocking:**
   ```bash
   acode config set mode local-only
   acode git push
   ```
   Verify: Error "Push blocked by operating mode"

7. **Verify timeout config:**
   ```yaml
   # .agent/config.yml
   git:
     timeoutSeconds: 5
   ```
   Verify: Long operations timeout after 5 seconds

8. **Verify credential redaction:**
   ```bash
   acode git push 2>&1 | grep -i password
   ```
   Verify: No passwords appear in output

9. **Verify concurrent operations:**
   ```bash
   acode git status & acode git log
   ```
   Verify: Both complete without error

10. **Verify error messages:**
    ```bash
    acode git checkout non-existent-branch
    ```
    Verify: Clear error with suggestion

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Git/
│       ├── GitStatus.cs
│       ├── GitCommit.cs
│       ├── GitBranch.cs
│       ├── GitRemote.cs
│       ├── FileStatus.cs
│       └── GitFileState.cs
├── Acode.Application/
│   └── Git/
│       ├── IGitService.cs
│       ├── GitOptions.cs
│       └── Exceptions/
│           ├── GitException.cs
│           ├── NotARepositoryException.cs
│           ├── BranchNotFoundException.cs
│           ├── AuthenticationException.cs
│           ├── NetworkException.cs
│           ├── PushRejectedException.cs
│           ├── MergeConflictException.cs
│           ├── GitTimeoutException.cs
│           └── ModeViolationException.cs
├── Acode.Infrastructure/
│   └── Git/
│       ├── GitService.cs
│       ├── GitOutputParser.cs
│       ├── GitCommandBuilder.cs
│       ├── CredentialRedactor.cs
│       └── GitConfiguration.cs
└── Acode.Cli/
    └── Commands/
        └── Git/
            ├── GitStatusCommand.cs
            ├── GitLogCommand.cs
            ├── GitDiffCommand.cs
            ├── GitBranchCommand.cs
            └── GitCheckoutCommand.cs
```

### Domain Models

```csharp
// GitStatus.cs
namespace Acode.Domain.Git;

public sealed record GitStatus
{
    public required string Branch { get; init; }
    public required bool IsClean { get; init; }
    public required bool IsDetachedHead { get; init; }
    public string? UpstreamBranch { get; init; }
    public int? AheadBy { get; init; }
    public int? BehindBy { get; init; }
    public required IReadOnlyList<FileStatus> StagedFiles { get; init; }
    public required IReadOnlyList<FileStatus> UnstagedFiles { get; init; }
    public required IReadOnlyList<string> UntrackedFiles { get; init; }
    
    public int TotalChangedFiles => 
        StagedFiles.Count + UnstagedFiles.Count + UntrackedFiles.Count;
}

// FileStatus.cs
namespace Acode.Domain.Git;

public sealed record FileStatus
{
    public required string Path { get; init; }
    public required GitFileState State { get; init; }
    public string? OriginalPath { get; init; } // For renames
}

public enum GitFileState 
{ 
    Untracked, 
    Modified, 
    Added, 
    Deleted, 
    Renamed, 
    Copied,
    TypeChanged,
    Unmerged
}

// GitCommit.cs
namespace Acode.Domain.Git;

public sealed record GitCommit
{
    public required string Sha { get; init; }
    public required string ShortSha { get; init; }
    public required string Author { get; init; }
    public required string AuthorEmail { get; init; }
    public required DateTimeOffset AuthorDate { get; init; }
    public required string Committer { get; init; }
    public required string CommitterEmail { get; init; }
    public required DateTimeOffset CommitDate { get; init; }
    public required string Subject { get; init; }
    public string? Body { get; init; }
    public IReadOnlyList<string> ParentShas { get; init; } = Array.Empty<string>();
}

// GitBranch.cs
namespace Acode.Domain.Git;

public sealed record GitBranch
{
    public required string Name { get; init; }
    public required string FullName { get; init; }
    public required string Sha { get; init; }
    public required bool IsCurrent { get; init; }
    public required bool IsRemote { get; init; }
    public string? Upstream { get; init; }
}

// GitRemote.cs
namespace Acode.Domain.Git;

public sealed record GitRemote
{
    public required string Name { get; init; }
    public required string FetchUrl { get; init; }
    public required string PushUrl { get; init; }
}
```

### Core Interface

```csharp
// IGitService.cs
namespace Acode.Application.Git;

public interface IGitService
{
    // Repository info
    Task<bool> IsRepositoryAsync(string path, CancellationToken ct = default);
    Task<string> GetRepositoryRootAsync(string path, CancellationToken ct = default);
    
    // Status and diff
    Task<GitStatus> GetStatusAsync(string workingDir, CancellationToken ct = default);
    Task<string> GetDiffAsync(string workingDir, DiffOptions options, CancellationToken ct = default);
    Task<IReadOnlyList<GitCommit>> GetLogAsync(string workingDir, LogOptions options, CancellationToken ct = default);
    
    // Branch operations
    Task<string> GetCurrentBranchAsync(string workingDir, CancellationToken ct = default);
    Task<IReadOnlyList<GitBranch>> GetBranchesAsync(string workingDir, BranchListOptions? options = null, CancellationToken ct = default);
    Task<GitBranch> CreateBranchAsync(string workingDir, string name, string? startPoint = null, CancellationToken ct = default);
    Task CheckoutAsync(string workingDir, string branchOrCommit, CancellationToken ct = default);
    Task DeleteBranchAsync(string workingDir, string name, bool force = false, CancellationToken ct = default);
    
    // Staging and committing
    Task StageAsync(string workingDir, IEnumerable<string> paths, CancellationToken ct = default);
    Task StageAllAsync(string workingDir, CancellationToken ct = default);
    Task UnstageAsync(string workingDir, IEnumerable<string> paths, CancellationToken ct = default);
    Task<GitCommit> CommitAsync(string workingDir, string message, CommitOptions? options = null, CancellationToken ct = default);
    
    // Remote operations
    Task<IReadOnlyList<GitRemote>> GetRemotesAsync(string workingDir, CancellationToken ct = default);
    Task PushAsync(string workingDir, PushOptions options, CancellationToken ct = default);
    Task FetchAsync(string workingDir, FetchOptions? options = null, CancellationToken ct = default);
}

// Options classes
public sealed record DiffOptions
{
    public string? Commit1 { get; init; }
    public string? Commit2 { get; init; }
    public bool Staged { get; init; }
    public bool NameOnly { get; init; }
    public IReadOnlyList<string>? Paths { get; init; }
}

public sealed record LogOptions
{
    public int? Limit { get; init; } = 50;
    public string? Since { get; init; }
    public string? Until { get; init; }
    public string? Author { get; init; }
    public string? Path { get; init; }
    public bool FirstParent { get; init; }
}

public sealed record BranchListOptions
{
    public bool IncludeRemote { get; init; }
    public string? Pattern { get; init; }
}

public sealed record CommitOptions
{
    public bool AllowEmpty { get; init; }
    public bool Amend { get; init; }
}

public sealed record PushOptions
{
    public string? Remote { get; init; }
    public string? Branch { get; init; }
    public bool SetUpstream { get; init; }
    public bool Force { get; init; }
}

public sealed record FetchOptions
{
    public string? Remote { get; init; }
    public bool Prune { get; init; }
}
```

### Exception Hierarchy

```csharp
// GitException.cs
namespace Acode.Application.Git.Exceptions;

public class GitException : Exception
{
    public int ExitCode { get; }
    public string? StdErr { get; }
    public string? WorkingDirectory { get; }
    public string ErrorCode { get; }
    
    public GitException(string message, int exitCode, string? stderr, string? workingDir, string errorCode)
        : base(message)
    {
        ExitCode = exitCode;
        StdErr = stderr;
        WorkingDirectory = workingDir;
        ErrorCode = errorCode;
    }
}

// NotARepositoryException.cs
public sealed class NotARepositoryException : GitException
{
    public NotARepositoryException(string path)
        : base($"Not a git repository: {path}. Run 'git init' to create one.", 128, null, path, "GIT_001")
    { }
}

// BranchNotFoundException.cs
public sealed class BranchNotFoundException : GitException
{
    public string BranchName { get; }
    
    public BranchNotFoundException(string branch, string? workingDir, string? stderr)
        : base($"Branch '{branch}' not found. Use 'git branch -a' to list available branches.", 1, stderr, workingDir, "GIT_002")
    {
        BranchName = branch;
    }
}

// ModeViolationException.cs
public sealed class ModeViolationException : GitException
{
    public string CurrentMode { get; }
    public string BlockedOperation { get; }
    
    public ModeViolationException(string mode, string operation)
        : base($"Operation '{operation}' is blocked in {mode} mode. Switch to burst mode to enable network operations.", 0, null, null, "GIT_008")
    {
        CurrentMode = mode;
        BlockedOperation = operation;
    }
}
```

### Infrastructure Implementation

```csharp
// GitService.cs
namespace Acode.Infrastructure.Git;

public sealed class GitService : IGitService
{
    private readonly ICommandExecutor _executor;
    private readonly IGitOutputParser _parser;
    private readonly ICredentialRedactor _redactor;
    private readonly IModeResolver _modeResolver;
    private readonly IOptions<GitConfiguration> _config;
    private readonly ILogger<GitService> _logger;
    
    public async Task<GitStatus> GetStatusAsync(string workingDir, CancellationToken ct = default)
    {
        await EnsureRepositoryAsync(workingDir, ct);
        
        var result = await ExecuteGitAsync(
            workingDir,
            new[] { "status", "--porcelain=v2", "--branch" },
            ct);
        
        return _parser.ParseStatus(result.StdOut);
    }
    
    public async Task<GitBranch> CreateBranchAsync(
        string workingDir, 
        string name, 
        string? startPoint = null, 
        CancellationToken ct = default)
    {
        await EnsureRepositoryAsync(workingDir, ct);
        
        var args = new List<string> { "branch", name };
        if (startPoint is not null)
        {
            args.Add(startPoint);
        }
        
        await ExecuteGitAsync(workingDir, args, ct);
        
        _logger.LogInformation("Created branch {Branch} in {Dir}", name, workingDir);
        
        // Get branch details
        var sha = await GetBranchShaAsync(workingDir, name, ct);
        return new GitBranch
        {
            Name = name,
            FullName = $"refs/heads/{name}",
            Sha = sha,
            IsCurrent = false,
            IsRemote = false
        };
    }
    
    public async Task PushAsync(string workingDir, PushOptions options, CancellationToken ct = default)
    {
        // Mode check - push is network operation
        var mode = await _modeResolver.GetCurrentModeAsync(ct);
        if (mode is OperatingMode.LocalOnly or OperatingMode.Airgapped)
        {
            _logger.LogWarning("Push blocked by {Mode} mode", mode);
            throw new ModeViolationException(mode.ToString(), "push");
        }
        
        await EnsureRepositoryAsync(workingDir, ct);
        
        var args = new List<string> { "push" };
        
        if (options.SetUpstream)
            args.Add("--set-upstream");
        if (options.Force)
            args.Add("--force-with-lease");
        
        args.Add(options.Remote ?? "origin");
        
        if (options.Branch is not null)
            args.Add(options.Branch);
        
        await ExecuteGitAsync(workingDir, args, ct, 
            timeout: TimeSpan.FromSeconds(_config.Value.TimeoutSeconds));
        
        _logger.LogInformation("Pushed to {Remote}", options.Remote ?? "origin");
    }
    
    public async Task<GitCommit> CommitAsync(
        string workingDir, 
        string message, 
        CommitOptions? options = null, 
        CancellationToken ct = default)
    {
        await EnsureRepositoryAsync(workingDir, ct);
        
        var args = new List<string> { "commit", "-m", message };
        
        if (options?.AllowEmpty == true)
            args.Add("--allow-empty");
        if (options?.Amend == true)
            args.Add("--amend");
        
        await ExecuteGitAsync(workingDir, args, ct);
        
        // Get the commit we just created
        var log = await GetLogAsync(workingDir, new LogOptions { Limit = 1 }, ct);
        return log.First();
    }
    
    private async Task<CommandResult> ExecuteGitAsync(
        string workingDir,
        IEnumerable<string> args,
        CancellationToken ct,
        TimeSpan? timeout = null)
    {
        var gitPath = _config.Value.Executable ?? "git";
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(_config.Value.TimeoutSeconds);
        
        var command = new CommandSpec
        {
            Executable = gitPath,
            Arguments = args.ToArray(),
            WorkingDirectory = workingDir,
            Timeout = effectiveTimeout,
            RedactSecrets = true
        };
        
        var result = await _executor.ExecuteAsync(command, ct);
        
        if (result.ExitCode != 0)
        {
            throw MapToException(result, workingDir);
        }
        
        return result;
    }
    
    private GitException MapToException(CommandResult result, string workingDir)
    {
        var stderr = result.StdErr ?? "";
        var redacted = _redactor.Redact(stderr);
        
        return result.ExitCode switch
        {
            128 when stderr.Contains("not a git repository") => 
                new NotARepositoryException(workingDir),
            128 when stderr.Contains("Authentication failed") =>
                new AuthenticationException(redacted, workingDir),
            1 when stderr.Contains("CONFLICT") =>
                new MergeConflictException(redacted, workingDir),
            1 when stderr.Contains("did not match any") =>
                new BranchNotFoundException(ExtractBranchName(stderr), workingDir, redacted),
            _ when stderr.Contains("Could not resolve host") =>
                new NetworkException(redacted, workingDir),
            _ when stderr.Contains("rejected") =>
                new PushRejectedException(redacted, workingDir),
            _ => new GitException($"Git command failed: {redacted}", result.ExitCode, redacted, workingDir, "GIT_000")
        };
    }
}

// GitOutputParser.cs
namespace Acode.Infrastructure.Git;

public sealed class GitOutputParser : IGitOutputParser
{
    public GitStatus ParseStatus(string output)
    {
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var branch = "";
        var upstream = (string?)null;
        var ahead = (int?)null;
        var behind = (int?)null;
        var staged = new List<FileStatus>();
        var unstaged = new List<FileStatus>();
        var untracked = new List<string>();
        
        foreach (var line in lines)
        {
            if (line.StartsWith("# branch.head "))
            {
                branch = line[14..];
            }
            else if (line.StartsWith("# branch.upstream "))
            {
                upstream = line[18..];
            }
            else if (line.StartsWith("# branch.ab "))
            {
                var match = Regex.Match(line, @"\+(\d+) -(\d+)");
                if (match.Success)
                {
                    ahead = int.Parse(match.Groups[1].Value);
                    behind = int.Parse(match.Groups[2].Value);
                }
            }
            else if (line.StartsWith("1 ") || line.StartsWith("2 "))
            {
                // Changed entry
                var parts = line.Split(' ');
                var xy = parts[1];
                var path = parts.Last();
                
                if (xy[0] != '.')
                    staged.Add(new FileStatus { Path = path, State = ParseState(xy[0]) });
                if (xy[1] != '.')
                    unstaged.Add(new FileStatus { Path = path, State = ParseState(xy[1]) });
            }
            else if (line.StartsWith("? "))
            {
                untracked.Add(line[2..]);
            }
        }
        
        return new GitStatus
        {
            Branch = branch,
            IsClean = staged.Count == 0 && unstaged.Count == 0 && untracked.Count == 0,
            IsDetachedHead = branch == "(detached)",
            UpstreamBranch = upstream,
            AheadBy = ahead,
            BehindBy = behind,
            StagedFiles = staged,
            UnstagedFiles = unstaged,
            UntrackedFiles = untracked
        };
    }
    
    private static GitFileState ParseState(char c) => c switch
    {
        'M' => GitFileState.Modified,
        'A' => GitFileState.Added,
        'D' => GitFileState.Deleted,
        'R' => GitFileState.Renamed,
        'C' => GitFileState.Copied,
        'T' => GitFileState.TypeChanged,
        'U' => GitFileState.Unmerged,
        _ => GitFileState.Modified
    };
}

// CredentialRedactor.cs
namespace Acode.Infrastructure.Git;

public sealed class CredentialRedactor : ICredentialRedactor
{
    private static readonly Regex UrlWithCredentials = new(
        @"(https?://)([^:]+):([^@]+)@",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    private static readonly string[] SensitivePatterns = 
    {
        "password", "token", "secret", "key", "credential",
        "auth", "bearer", "api_key", "apikey"
    };
    
    public string Redact(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        
        // Redact URLs with embedded credentials
        var result = UrlWithCredentials.Replace(input, "$1[REDACTED]@");
        
        // Redact lines containing sensitive patterns
        var lines = result.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            foreach (var pattern in SensitivePatterns)
            {
                if (lines[i].Contains(pattern, StringComparison.OrdinalIgnoreCase) &&
                    lines[i].Contains('='))
                {
                    var eqIndex = lines[i].IndexOf('=');
                    lines[i] = lines[i][..(eqIndex + 1)] + "[REDACTED]";
                    break;
                }
            }
        }
        
        return string.Join('\n', lines);
    }
}
```

### CLI Commands

```csharp
// GitStatusCommand.cs
namespace Acode.Cli.Commands.Git;

[Command("git status", Description = "Show repository status")]
public class GitStatusCommand
{
    [Option("-s|--short", Description = "Show short format")]
    public bool Short { get; set; }
    
    public async Task<int> ExecuteAsync(
        IGitService git,
        IConsole console,
        CancellationToken ct)
    {
        try
        {
            var cwd = Directory.GetCurrentDirectory();
            var status = await git.GetStatusAsync(cwd, ct);
            
            if (Short)
            {
                PrintShortStatus(console, status);
            }
            else
            {
                PrintLongStatus(console, status);
            }
            
            return 0;
        }
        catch (NotARepositoryException)
        {
            console.Error.WriteLine("fatal: not a git repository");
            return 128;
        }
    }
    
    private void PrintLongStatus(IConsole console, GitStatus status)
    {
        console.WriteLine($"On branch {status.Branch}");
        
        if (status.UpstreamBranch is not null)
        {
            if (status.AheadBy > 0 && status.BehindBy > 0)
                console.WriteLine($"Your branch has diverged ({status.AheadBy} ahead, {status.BehindBy} behind)");
            else if (status.AheadBy > 0)
                console.WriteLine($"Your branch is ahead by {status.AheadBy} commits");
            else if (status.BehindBy > 0)
                console.WriteLine($"Your branch is behind by {status.BehindBy} commits");
        }
        
        if (status.IsClean)
        {
            console.WriteLine("nothing to commit, working tree clean");
            return;
        }
        
        if (status.StagedFiles.Count > 0)
        {
            console.WriteLine("\nChanges to be committed:");
            foreach (var file in status.StagedFiles)
            {
                console.WriteLine($"  {file.State.ToString().ToLower()}: {file.Path}");
            }
        }
        
        if (status.UnstagedFiles.Count > 0)
        {
            console.WriteLine("\nChanges not staged for commit:");
            foreach (var file in status.UnstagedFiles)
            {
                console.WriteLine($"  {file.State.ToString().ToLower()}: {file.Path}");
            }
        }
        
        if (status.UntrackedFiles.Count > 0)
        {
            console.WriteLine("\nUntracked files:");
            foreach (var file in status.UntrackedFiles)
            {
                console.WriteLine($"  {file}");
            }
        }
    }
}
```

### Error Codes

| Code | Name | Description | Recovery |
|------|------|-------------|----------|
| GIT_001 | NotARepository | Working directory is not a git repository | Run `git init` or change directory |
| GIT_002 | BranchNotFound | Specified branch does not exist | Use `git branch -a` to list branches |
| GIT_003 | AuthFailed | Git authentication failed | Configure credentials with `git config` |
| GIT_004 | NetworkError | Network operation failed | Check connectivity, retry |
| GIT_005 | PushRejected | Remote rejected push | Pull changes first, then push |
| GIT_006 | MergeConflict | Merge conflict detected | Resolve conflicts manually |
| GIT_007 | Timeout | Operation timed out | Increase timeout in config |
| GIT_008 | ModeViolation | Operation blocked by mode | Switch to burst mode |
| GIT_009 | GitNotFound | Git executable not found | Install Git and add to PATH |
| GIT_010 | VersionTooOld | Git version below minimum | Update Git to 2.20+ |

### Implementation Checklist

- [ ] Define all domain models (GitStatus, GitCommit, etc.)
- [ ] Define IGitService interface in Application layer
- [ ] Implement all exception types with proper error codes
- [ ] Implement GitService with command execution
- [ ] Implement GitOutputParser for status, log, branch parsing
- [ ] Implement CredentialRedactor for secret masking
- [ ] Add mode validation for network operations
- [ ] Add configuration loading from agent config
- [ ] Implement CLI commands (status, log, branch, etc.)
- [ ] Add unit tests for output parsing
- [ ] Add unit tests for exception mapping
- [ ] Add integration tests with real git repos
- [ ] Add E2E tests for CLI commands
- [ ] Run performance benchmarks
- [ ] Document configuration options

### Rollout Plan

| Phase | Action | Validation |
|-------|--------|------------|
| 1 | Implement domain models | Compile check |
| 2 | Define IGitService interface | Compile check |
| 3 | Implement exception types | Unit tests pass |
| 4 | Implement GitOutputParser | Parser tests pass |
| 5 | Implement CredentialRedactor | Redaction tests pass |
| 6 | Implement GitService core | Integration tests pass |
| 7 | Add mode validation | Mode tests pass |
| 8 | Add configuration | Config tests pass |
| 9 | Implement CLI commands | E2E tests pass |
| 10 | Performance testing | Benchmarks pass |
| 11 | Documentation | User manual complete |

---

**End of Task 022 Specification**