# Task 024.c: push gating + failure handling

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 5 – Git Integration Layer  
**Dependencies:** Task 024 (Safe Workflow), Task 022.c (push), Task 001 (Modes)  

---

## Description

Task 024.c implements push gating and failure handling. Before pushing, configurable gates MUST be evaluated. Failed gates MUST block the push. Push failures MUST be handled with retries.

Push gates MUST verify local state before network operations. This includes ensuring pre-commit verification passed, branch is up-to-date, and no conflicting changes exist.

Failure handling MUST support automatic retries. Network errors MUST trigger retry with exponential backoff. Authentication failures MUST NOT retry. Rejection (non-fast-forward) MUST suggest remediation.

Operating mode compliance MUST be enforced. Push MUST be blocked in local-only and airgapped modes. Mode violations MUST produce clear errors.

### Business Value

Push gating prevents broken code from reaching remotes. Automatic retries handle transient network issues. Clear failure messages enable quick remediation.

### Scope Boundaries

This task covers push gating and error handling. Pre-commit verification is in 024.a. Message validation is in 024.b.

### Integration Points

- Task 024: Workflow orchestration
- Task 022.c: Push operations
- Task 001: Operating mode validation
- Task 002: Configuration

### Failure Modes

- Gate fails → Block push, show failures
- Network timeout → Retry with backoff
- Auth failure → Report, no retry
- Non-fast-forward → Suggest pull/rebase
- Mode violation → Clear error

---

## Functional Requirements

### FR-001 to FR-025: Gate Evaluation

- FR-001: `IPushGate` interface MUST be defined
- FR-002: `EvaluateAsync` MUST check all gates
- FR-003: Result MUST indicate pass/fail
- FR-004: Result MUST include failed gates
- FR-005: `preCommitPassed` gate MUST verify verification ran
- FR-006: `branchUpToDate` gate MUST check remote tracking
- FR-007: `noConflicts` gate MUST check for conflicts
- FR-008: `modeAllowed` gate MUST check operating mode
- FR-009: Gates MUST be configurable
- FR-010: `requireAllChecks` MUST require all gates pass
- FR-011: Gates MUST be independently disableable
- FR-012: Gate order MUST be deterministic
- FR-013: Fast gates MUST run first
- FR-014: Gate timeout MUST be configurable
- FR-015: Default gate timeout MUST be 30 seconds
- FR-016: Gate results MUST be logged
- FR-017: Gate results MUST include duration
- FR-018: Failed gate MUST block push
- FR-019: All gates MUST run unless fail-fast
- FR-020: Gate events MUST be emitted
- FR-021: Custom gates MUST be registrable
- FR-022: Custom gate MUST implement interface
- FR-023: Gate dependencies MAY be specified
- FR-024: Dependent gates MUST wait
- FR-025: Gate caching MAY optimize repeated checks

### FR-026 to FR-045: Failure Handling

- FR-026: Network errors MUST trigger retry
- FR-027: Retry count MUST be configurable
- FR-028: Default retry count MUST be 3
- FR-029: Retry delay MUST use exponential backoff
- FR-030: Initial delay MUST be 1 second
- FR-031: Max delay MUST be 30 seconds
- FR-032: Total timeout MUST be respected
- FR-033: Auth failure MUST NOT retry
- FR-034: Auth failure MUST suggest credential setup
- FR-035: Non-fast-forward MUST NOT retry
- FR-036: Non-fast-forward MUST suggest pull
- FR-037: Permission denied MUST NOT retry
- FR-038: Permission denied MUST explain
- FR-039: Unknown errors MUST retry
- FR-040: Final failure MUST include all attempts
- FR-041: Failure MUST be logged
- FR-042: Failure metrics MUST be tracked
- FR-043: Recovery suggestions MUST be provided
- FR-044: Manual retry MUST be possible
- FR-045: Retry state MUST be clearable

---

## Non-Functional Requirements

- NFR-001: Gate evaluation MUST complete in <30s
- NFR-002: Mode check MUST be <10ms
- NFR-003: Remote check MUST respect timeout
- NFR-004: Retry MUST NOT block indefinitely
- NFR-005: Backoff MUST cap at max delay
- NFR-006: Concurrent pushes MUST be serialized
- NFR-007: Partial push MUST be detectable
- NFR-008: Credentials MUST NOT be logged
- NFR-009: Remote URLs MUST be redacted
- NFR-010: Error messages MUST NOT leak secrets

---

## User Manual Documentation

### Configuration

```yaml
workflow:
  pushGate:
    enabled: true
    requireAllChecks: true
    checks:
      - name: preCommitPassed
        enabled: true
      - name: branchUpToDate
        enabled: true
      - name: modeAllowed
        enabled: true
    
  pushRetry:
    maxAttempts: 3
    initialDelayMs: 1000
    maxDelayMs: 30000
    timeoutSeconds: 120
```

### Gate Types

| Gate | Description |
|------|-------------|
| preCommitPassed | Pre-commit verification completed successfully |
| branchUpToDate | Local branch not behind remote |
| noConflicts | No merge conflicts detected |
| modeAllowed | Operating mode permits push |

### Error Recovery

**Non-fast-forward rejection:**
```bash
$ acode push
Error: Push rejected (non-fast-forward)

Remote has commits not in your local branch.

Suggested fix:
  acode git pull --rebase
  acode push
```

**Authentication failure:**
```bash
$ acode push
Error: Authentication failed

Suggested fix:
  Configure git credentials:
    git config credential.helper store
    git push  # Enter credentials
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Gates evaluated before push
- [ ] AC-002: Failed gate blocks push
- [ ] AC-003: Mode gate enforced
- [ ] AC-004: Network retry works
- [ ] AC-005: Backoff increases delay
- [ ] AC-006: Auth failure no retry
- [ ] AC-007: Non-FF no retry
- [ ] AC-008: Recovery suggestions shown
- [ ] AC-009: Configuration respected
- [ ] AC-010: Credentials not logged

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Test gate evaluation
- [ ] UT-002: Test retry logic
- [ ] UT-003: Test backoff calculation
- [ ] UT-004: Test error classification

### Integration Tests

- [ ] IT-001: Full push workflow
- [ ] IT-002: Gate failure handling
- [ ] IT-003: Network retry
- [ ] IT-004: Mode enforcement

---

## Implementation Prompt

### Interface

```csharp
public interface IPushGate
{
    Task<GateResult> EvaluateAsync(string workingDir, 
        CancellationToken ct = default);
}

public record GateResult(
    bool Passed,
    IReadOnlyList<GateCheck> Checks);

public record GateCheck(
    string Name,
    bool Passed,
    string? Error,
    TimeSpan Duration);

public interface IPushRetryPolicy
{
    bool ShouldRetry(Exception ex, int attempt);
    TimeSpan GetDelay(int attempt);
}
```

---

**End of Task 024.c Specification**