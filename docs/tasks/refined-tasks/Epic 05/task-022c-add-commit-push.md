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

### Assumptions

- Git is installed and accessible
- For push, credentials are configured
- Repository has at least one commit

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

### Core Methods

```csharp
public interface IGitService
{
    Task StageAsync(string workingDir, IEnumerable<string> paths,
        StageOptions? options = null, CancellationToken ct = default);
    
    Task UnstageAsync(string workingDir, IEnumerable<string> paths,
        CancellationToken ct = default);
    
    Task<GitCommit> CommitAsync(string workingDir, string message,
        CommitOptions? options = null, CancellationToken ct = default);
    
    Task<PushResult> PushAsync(string workingDir,
        PushOptions? options = null, CancellationToken ct = default);
}
```

### Validation Checklist

- [ ] Mode validation implemented
- [ ] Retry logic with backoff
- [ ] Credential redaction works
- [ ] All exceptions defined

---

**End of Task 022.c Specification**