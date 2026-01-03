# EPIC 5 — Git Automation + Worktrees

**Priority:** P1 – High  
**Phase:** Phase 5 – Git Integration Layer  
**Dependencies:** Epic 04 (Execution), Epic 03 (Repo Intelligence)  

---

## Epic Overview

Epic 5 implements Git automation capabilities for Acode. The agent MUST be able to interact with Git repositories programmatically. This enables automated branching, committing, and pushing changes.

Git operations MUST be abstracted through a tool layer. Direct git command execution MUST NOT be scattered throughout the codebase. All Git interactions MUST go through defined interfaces.

Worktree-per-task isolation MUST be supported. Each task MAY execute in its own Git worktree. This prevents work-in-progress from interfering with the main worktree. Cleanup policies MUST manage worktree lifecycle.

Safe commit and push workflows MUST be enforced. Pre-commit verification MUST run before commits. Commit message rules MUST be configurable. Push gating MUST prevent broken code from reaching remotes.

### Scope Boundaries

This epic covers Git automation within the local repository. Remote operations (push) are included but external service integrations (GitHub API, PR creation) are NOT in scope.

### Dependencies

- Epic 04: Command execution infrastructure for running git commands
- Epic 03: Repository structure understanding for intelligent operations
- Task 001: Operating mode constraints for network operations
- Task 002: Config contract for Git-related settings

---

## Outcomes

1. Git operations abstracted through `IGitService` interface
2. Status, diff, and log operations available programmatically
3. Branch creation and checkout automated
4. Add, commit, and push operations automated
5. Worktree creation isolated per task
6. Worktree-to-task mapping persisted in DB
7. Worktree cleanup policies enforced automatically
8. Pre-commit verification pipeline executes before commits
9. Commit message rules validated and enforced
10. Push gating prevents broken code from reaching remotes
11. All Git operations logged for audit
12. Git errors handled gracefully with clear messages
13. Network operations respect Task 001 mode constraints
14. Git configuration respects Task 002 contract
15. Concurrent worktree operations handled safely

---

## Non-Goals

1. GitHub/GitLab/Azure DevOps API integration (future epic)
2. Pull request creation or management
3. Code review automation
4. Merge conflict resolution (beyond detection)
5. Git LFS operations
6. Submodule management
7. Git hooks installation or management
8. Repository cloning from remote
9. Multi-remote configuration
10. SSH key management
11. GPG signing of commits
12. Interactive rebase operations
13. Cherry-pick operations
14. Git bisect automation
15. Stash management

---

## Architecture & Integration Points

### Core Interfaces

```csharp
public interface IGitService
{
    Task<GitStatus> GetStatusAsync(string workingDir);
    Task<string> GetDiffAsync(string workingDir, DiffOptions options);
    Task<IReadOnlyList<GitCommit>> GetLogAsync(string workingDir, LogOptions options);
    Task<GitBranch> CreateBranchAsync(string workingDir, string branchName);
    Task CheckoutAsync(string workingDir, string branchName);
    Task StageAsync(string workingDir, IEnumerable<string> paths);
    Task<GitCommit> CommitAsync(string workingDir, string message);
    Task PushAsync(string workingDir, PushOptions options);
}

public interface IWorktreeService
{
    Task<Worktree> CreateAsync(string repoPath, string worktreePath, string branch);
    Task RemoveAsync(string worktreePath);
    Task<IReadOnlyList<Worktree>> ListAsync(string repoPath);
    Task<Worktree?> GetForTaskAsync(TaskId taskId);
}

public interface IPreCommitPipeline
{
    Task<VerificationResult> VerifyAsync(string workingDir);
}

public interface ICommitMessageValidator
{
    ValidationResult Validate(string message);
}

public interface IPushGate
{
    Task<GateResult> EvaluateAsync(string workingDir);
}
```

### Data Contracts

```csharp
public record GitStatus(
    string Branch,
    bool IsClean,
    IReadOnlyList<FileStatus> Files);

public record FileStatus(
    string Path,
    GitFileState State);

public enum GitFileState { Untracked, Modified, Added, Deleted, Renamed }

public record Worktree(
    string Path,
    string Branch,
    string CommitSha,
    TaskId? AssociatedTaskId);
```

### Integration Points

- Task 018: Command execution for git commands
- Task 011: Workspace DB for worktree-task mapping
- Task 039: Session context for operation tracking
- Task 001: Operating mode validation for push operations

---

## Operational Considerations

### Mode Constraints

- **Local-Only Mode:** All Git operations permitted except push
- **Burst Mode:** All Git operations permitted including push
- **Airgapped Mode:** All Git operations permitted except push; no network

### Safety Rules

- Commits MUST NOT be made without pre-commit verification
- Push MUST NOT proceed if verification fails
- Branch deletion MUST require explicit confirmation
- Force push MUST be disabled by default
- Worktree removal MUST NOT delete uncommitted changes without warning

### Audit Requirements

- All Git operations MUST be logged with timestamp
- Commit operations MUST log commit SHA
- Push operations MUST log remote and branch
- Failed operations MUST log error details
- Worktree lifecycle MUST be tracked

### Configuration

```yaml
git:
  defaultBranch: main
  pushEnabled: true
  preCommit:
    enabled: true
    failFast: true
  commitMessage:
    pattern: "^(feat|fix|docs|chore)\\(.*\\): .+"
    maxLength: 72
  worktree:
    basePath: .acode/worktrees
    cleanupAfterDays: 7
```

---

## Acceptance Criteria / Definition of Done

### Git Tool Layer (Task 022)

- [ ] AC-001: `IGitService` interface defined in Application layer
- [ ] AC-002: Git status retrieval works correctly
- [ ] AC-003: Git diff generation works correctly
- [ ] AC-004: Git log retrieval works correctly
- [ ] AC-005: Branch creation works correctly
- [ ] AC-006: Branch checkout works correctly
- [ ] AC-007: File staging works correctly
- [ ] AC-008: Commit creation works correctly
- [ ] AC-009: Push operation works correctly
- [ ] AC-010: All operations use command execution layer

### Worktree Management (Task 023)

- [ ] AC-011: `IWorktreeService` interface defined
- [ ] AC-012: Worktree creation works correctly
- [ ] AC-013: Worktree removal works correctly
- [ ] AC-014: Worktree listing works correctly
- [ ] AC-015: Task-to-worktree mapping persisted
- [ ] AC-016: Worktree lookup by task works
- [ ] AC-017: Cleanup policy executes automatically
- [ ] AC-018: Stale worktrees removed per policy
- [ ] AC-019: Uncommitted changes protected
- [ ] AC-020: Concurrent operations handled safely

### Safe Commit/Push (Task 024)

- [ ] AC-021: Pre-commit pipeline executes before commit
- [ ] AC-022: Failed verification blocks commit
- [ ] AC-023: Commit message validated
- [ ] AC-024: Invalid message rejected with guidance
- [ ] AC-025: Push gate evaluates before push
- [ ] AC-026: Failed gate blocks push
- [ ] AC-027: Push respects operating mode
- [ ] AC-028: Push failure handled gracefully
- [ ] AC-029: Retry mechanism available
- [ ] AC-030: All operations logged

### Cross-Cutting

- [ ] AC-031: All operations respect Task 001 modes
- [ ] AC-032: Configuration from Task 002 honored
- [ ] AC-033: Errors produce clear messages
- [ ] AC-034: Performance acceptable for large repos
- [ ] AC-035: Unit test coverage >90%
- [ ] AC-036: Integration tests pass
- [ ] AC-037: E2E tests verify workflows
- [ ] AC-038: Documentation complete

---

## Risks & Mitigations

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Git command fails silently | Medium | High | Parse exit codes and stderr, verify expected state |
| Worktree corruption | Low | High | Validate worktree state before operations |
| Push to wrong branch | Medium | High | Require explicit branch confirmation |
| Uncommitted work lost | Medium | High | Check for dirty state before destructive ops |
| Merge conflicts undetected | Medium | Medium | Detect conflicts before commit |
| Auth credentials exposed in logs | Medium | High | Redact URLs with credentials |
| Network timeout on push | Medium | Medium | Configurable timeout, retry mechanism |
| Disk space exhaustion from worktrees | Medium | Medium | Cleanup policy with space monitoring |
| Concurrent git operations conflict | Medium | Medium | Lock mechanism per repository |
| Pre-commit hooks interfere | Low | Medium | Option to skip external hooks |
| Large repo performance | Medium | Medium | Streaming operations, progress feedback |
| Git version incompatibility | Low | Medium | Detect git version, warn on unsupported |

---

## Milestone Plan

### Milestone 1: Git Tool Layer (Task 022)

- Implement IGitService interface
- Task 022.a: status/diff/log operations
- Task 022.b: branch create/checkout operations
- Task 022.c: add/commit/push operations
- Unit and integration tests

### Milestone 2: Worktree Management (Task 023)

- Implement IWorktreeService interface
- Task 023.a: worktree create/remove/list
- Task 023.b: persist worktree-task mapping
- Task 023.c: cleanup policy rules
- Integration with Task 011 DB

### Milestone 3: Safe Commit/Push (Task 024)

- Implement pre-commit pipeline
- Task 024.a: verification pipeline
- Task 024.b: commit message rules
- Task 024.c: push gating and failure handling
- E2E workflow tests

### Milestone 4: Integration & Polish

- Cross-task integration testing
- Performance optimization
- Documentation
- Final acceptance testing

---

## Definition of Epic Complete

- [ ] All 12 tasks implemented and tested
- [ ] IGitService interface complete with all operations
- [ ] IWorktreeService interface complete with all operations
- [ ] Pre-commit pipeline operational
- [ ] Commit message validation enforced
- [ ] Push gating operational
- [ ] All operations respect Task 001 modes
- [ ] Configuration honors Task 002 contract
- [ ] Unit test coverage >90% for all tasks
- [ ] Integration tests pass for all Git operations
- [ ] E2E tests verify complete workflows
- [ ] Performance acceptable for repos with 10000+ commits
- [ ] All errors produce clear, actionable messages
- [ ] Audit logging complete for all operations
- [ ] Documentation complete for all interfaces
- [ ] No known critical or high-severity bugs
- [ ] Code reviewed and approved
- [ ] Security review completed for credential handling

---

**End of Epic 05 Specification**