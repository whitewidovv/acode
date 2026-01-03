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

### Assumptions

- Git 2.20+ is installed on the system
- System Git credentials are configured for remotes
- Repository has at least one commit (not empty)
- Working directory is within repository bounds

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
│           └── ModeViolationException.cs
├── Acode.Infrastructure/
│   └── Git/
│       ├── GitService.cs
│       ├── GitOutputParser.cs
│       ├── GitCommandBuilder.cs
│       └── CredentialRedactor.cs
└── Acode.Cli/
    └── Commands/
        └── Git/
            ├── GitStatusCommand.cs
            ├── GitLogCommand.cs
            └── GitBranchCommand.cs
```

### Core Interface

```csharp
public interface IGitService
{
    Task<GitStatus> GetStatusAsync(string workingDir, CancellationToken ct = default);
    Task<string> GetDiffAsync(string workingDir, DiffOptions options, CancellationToken ct = default);
    Task<IReadOnlyList<GitCommit>> GetLogAsync(string workingDir, LogOptions options, CancellationToken ct = default);
    Task<GitBranch> CreateBranchAsync(string workingDir, string name, CancellationToken ct = default);
    Task CheckoutAsync(string workingDir, string branch, CancellationToken ct = default);
    Task StageAsync(string workingDir, IEnumerable<string> paths, CancellationToken ct = default);
    Task<GitCommit> CommitAsync(string workingDir, string message, CancellationToken ct = default);
    Task PushAsync(string workingDir, PushOptions options, CancellationToken ct = default);
    Task<string> GetCurrentBranchAsync(string workingDir, CancellationToken ct = default);
    Task<IReadOnlyList<GitRemote>> GetRemotesAsync(string workingDir, CancellationToken ct = default);
    Task<bool> IsRepositoryAsync(string path, CancellationToken ct = default);
}
```

### Domain Models

```csharp
public record GitStatus(
    string Branch,
    bool IsClean,
    IReadOnlyList<FileStatus> Files);

public record FileStatus(
    string Path,
    GitFileState State);

public enum GitFileState 
{ 
    Untracked, 
    Modified, 
    Added, 
    Deleted, 
    Renamed, 
    Copied 
}

public record GitCommit(
    string Sha,
    string ShortSha,
    string Author,
    string Email,
    DateTimeOffset Date,
    string Message);

public record GitBranch(
    string Name,
    string Sha,
    bool IsCurrent);

public record GitRemote(
    string Name,
    string FetchUrl,
    string PushUrl);
```

### Error Codes

| Code | Name | Description |
|------|------|-------------|
| GIT_001 | NotARepository | Working directory is not a git repository |
| GIT_002 | BranchNotFound | Specified branch does not exist |
| GIT_003 | AuthFailed | Git authentication failed |
| GIT_004 | NetworkError | Network operation failed |
| GIT_005 | PushRejected | Remote rejected push |
| GIT_006 | MergeConflict | Merge conflict detected |
| GIT_007 | Timeout | Operation timed out |
| GIT_008 | ModeViolation | Operation blocked by mode |

### Validation Checklist Before Merge

- [ ] IGitService interface complete
- [ ] All domain models defined
- [ ] All exception types defined
- [ ] Command execution integration works
- [ ] Mode validation works
- [ ] Credential redaction works
- [ ] Configuration loading works
- [ ] Unit tests pass with >90% coverage
- [ ] Integration tests pass
- [ ] E2E tests pass
- [ ] Performance benchmarks met

### Rollout Plan

1. Implement domain models
2. Implement IGitService interface
3. Implement GitService with command execution
4. Implement GitOutputParser
5. Implement exception handling
6. Add mode validation
7. Add credential redaction
8. Add CLI commands
9. Integration testing
10. Documentation

---

**End of Task 022 Specification**