# Task 024: Safe Commit/Push Workflow

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 5 – Git Integration Layer  
**Dependencies:** Task 022.c (add/commit/push), Task 019 (Language Runners)  

---

## Description

Task 024 implements safe commit and push workflows. Commits MUST pass verification before creation. Pushes MUST be gated by quality checks. This prevents broken code from being committed or shared.

The pre-commit verification pipeline MUST run configurable checks. Build verification, test execution, and linting MUST be supported. Failed verification MUST block the commit with clear feedback.

Commit message rules MUST be enforced. Conventional commit format MAY be required. Maximum length, required prefixes, and issue references MAY be configured.

Push gating MUST evaluate additional criteria before push. All local checks MUST pass. Remote push MUST only proceed if gating succeeds. Push failures MUST be handled gracefully with retry support.

The workflow MUST be configurable per-repository. Some repos may require strict checks, others may be lenient. Configuration MUST come from Task 002's `.agent/config.yml`.

### Business Value

Safe workflows prevent broken code from polluting the repository. Automated verification catches issues before they become problems. Consistent commit messages enable automated changelog generation.

### Scope Boundaries

This task defines the workflow orchestration. Subtasks cover specific components: 024.a (verification pipeline), 024.b (message rules), 024.c (push gating).

### Integration Points

- Task 022.c: Underlying commit/push operations
- Task 019: Build and test runners
- Task 002: Configuration contract
- Task 001: Operating mode compliance

### Failure Modes

- Verification fails → Block commit, show failures
- Message validation fails → Block commit, show rules
- Push gate fails → Block push, show failures
- Network error on push → Retry with backoff

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Pre-commit | Verification before commit creation |
| Pipeline | Sequence of verification steps |
| Gate | Condition that must pass before proceeding |
| Conventional Commit | Standard commit message format |
| Retry | Automatic re-attempt after failure |
| Backoff | Increasing delay between retries |

---

## Out of Scope

- External CI/CD integration
- Code review requirements
- Branch protection rules
- Multi-repo workflows
- Commit signing

---

## Functional Requirements

### FR-001 to FR-020: Workflow Orchestration

- FR-001: `SafeCommitAsync` MUST orchestrate verification + commit
- FR-002: Verification MUST run before commit
- FR-003: Failed verification MUST block commit
- FR-004: Verification results MUST be returned
- FR-005: `SafePushAsync` MUST orchestrate gating + push
- FR-006: Gate evaluation MUST run before push
- FR-007: Failed gate MUST block push
- FR-008: Gate results MUST be returned
- FR-009: `--skip-verification` MUST bypass checks
- FR-010: Skip MUST require explicit confirmation
- FR-011: Skip MUST be logged as warning
- FR-012: Workflow MUST be cancellable
- FR-013: Partial failures MUST be recoverable
- FR-014: Workflow status MUST be queryable
- FR-015: Workflow MUST emit events
- FR-016: Events MUST include step completion
- FR-017: Events MUST include step failure
- FR-018: Timeout MUST be configurable
- FR-019: Default timeout MUST be 5 minutes
- FR-020: Timeout MUST abort gracefully

### FR-021 to FR-035: Configuration

- FR-021: Workflow config MUST come from Task 002
- FR-022: `workflow.preCommit.enabled` MUST control verification
- FR-023: Default `enabled` MUST be true
- FR-024: `workflow.preCommit.steps` MUST define checks
- FR-025: Default steps MUST include build and test
- FR-026: `workflow.preCommit.failFast` MUST control behavior
- FR-027: Default `failFast` MUST be true
- FR-028: `workflow.pushGate.enabled` MUST control gating
- FR-029: Default gate enabled MUST be true
- FR-030: `workflow.pushGate.checks` MUST define criteria
- FR-031: Invalid config MUST produce clear error
- FR-032: Missing config MUST use defaults
- FR-033: Config MUST be reloadable
- FR-034: Config changes MUST apply to next workflow
- FR-035: Config MUST be logged at workflow start

---

## Non-Functional Requirements

- NFR-001: Workflow start MUST be <100ms
- NFR-002: Verification step overhead MUST be <1s
- NFR-003: Total workflow MUST complete within timeout
- NFR-004: Memory MUST NOT exceed 100MB for workflow
- NFR-005: Concurrent workflows MUST be serialized
- NFR-006: Failed steps MUST NOT corrupt state
- NFR-007: Recovery MUST be possible from any point
- NFR-008: Audit log MUST record all workflow runs
- NFR-009: Metrics MUST track success/failure rates
- NFR-010: Secrets in output MUST be redacted

---

## User Manual Documentation

### Configuration

```yaml
workflow:
  preCommit:
    enabled: true
    failFast: true
    timeoutSeconds: 300
    steps:
      - name: build
        command: dotnet build
      - name: test
        command: dotnet test
      - name: lint
        command: dotnet format --verify-no-changes
        
  commitMessage:
    pattern: "^(feat|fix|docs|chore|refactor|test)\\(.*\\): .+"
    maxLength: 72
    requireIssueReference: false
    
  pushGate:
    enabled: true
    requireAllChecks: true
    checks:
      - preCommit
      - branchUpToDate
```

### Usage

```bash
# Safe commit with verification
acode commit "feat(api): add new endpoint"

# Skip verification (not recommended)
acode commit "fix: urgent hotfix" --skip-verification

# Safe push with gating
acode push

# Skip gate (not recommended)
acode push --skip-gate
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Pre-commit verification runs
- [ ] AC-002: Failed verification blocks commit
- [ ] AC-003: Message validation runs
- [ ] AC-004: Invalid message blocks commit
- [ ] AC-005: Push gate evaluates
- [ ] AC-006: Failed gate blocks push
- [ ] AC-007: Skip flags work
- [ ] AC-008: Configuration respected
- [ ] AC-009: Timeout enforced
- [ ] AC-010: Events emitted

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Test workflow state machine
- [ ] UT-002: Test step execution order
- [ ] UT-003: Test fail-fast behavior
- [ ] UT-004: Test timeout handling

### Integration Tests

- [ ] IT-001: Full commit workflow
- [ ] IT-002: Full push workflow
- [ ] IT-003: Verification failure handling
- [ ] IT-004: Gate failure handling

### End-to-End Tests

- [ ] E2E-001: CLI safe commit
- [ ] E2E-002: CLI safe push
- [ ] E2E-003: Skip flags

---

## Implementation Prompt

### Interface

```csharp
public interface ISafeWorkflowService
{
    Task<WorkflowResult> SafeCommitAsync(string workingDir, string message,
        SafeCommitOptions? options = null, CancellationToken ct = default);
    
    Task<WorkflowResult> SafePushAsync(string workingDir,
        SafePushOptions? options = null, CancellationToken ct = default);
}

public record WorkflowResult(
    bool Success,
    IReadOnlyList<StepResult> Steps,
    GitCommit? Commit,
    string? Error);

public record StepResult(
    string Name,
    bool Success,
    TimeSpan Duration,
    string? Output,
    string? Error);
```

---

**End of Task 024 Specification**