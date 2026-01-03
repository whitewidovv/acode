# Task 029.d: Teardown

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 3 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 029 (Interface), Task 029.a-c  

---

## Description

Task 029.d implements compute target teardown. Resources MUST be released. State MUST be cleaned. Orphans MUST be detected.

Teardown is the final lifecycle phase. After teardown, the target MUST NOT hold resources. Cloud instances MUST be terminated. Containers MUST be removed.

Teardown MUST be idempotent. Multiple teardown calls MUST succeed. Teardown after failure MUST work.

### Business Value

Proper teardown:
- Prevents resource leaks
- Reduces cloud costs
- Ensures clean state
- Enables re-use

### Scope Boundaries

This task covers cleanup. Execution is in 029.b. Artifacts are in 029.c.

### Integration Points

- Task 029: Part of target interface
- Task 031: EC2 termination
- Task 027: Workers trigger teardown

### Failure Modes

- Teardown timeout → Force terminate
- Resource stuck → Log and continue
- Cloud API failure → Retry
- Orphan found → Clean up

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Teardown | Resource cleanup |
| Terminate | Stop cloud instance |
| Orphan | Resource without owner |
| Idempotent | Same result on repeat |
| Force | Skip graceful shutdown |
| Drain | Complete pending work |

---

## Out of Scope

- Hibernation support
- Instance snapshots
- Spot instance handling
- Cost allocation cleanup
- Billing reconciliation

---

## Functional Requirements

### FR-001 to FR-020: Teardown Operation

- FR-001: `TeardownAsync` MUST be defined
- FR-002: Teardown MUST release compute
- FR-003: Teardown MUST clean workspace
- FR-004: Teardown MUST remove temp files
- FR-005: Graceful shutdown MUST be first
- FR-006: Grace period MUST be configurable
- FR-007: Default grace: 30 seconds
- FR-008: Force MUST be available
- FR-009: Force skips graceful
- FR-010: Running processes MUST be killed
- FR-011: Open connections MUST close
- FR-012: Teardown MUST be idempotent
- FR-013: Second teardown succeeds
- FR-014: Concurrent teardown MUST serialize
- FR-015: State MUST update to Terminated
- FR-016: State MUST be final
- FR-017: No restart after teardown
- FR-018: Metrics MUST be captured first
- FR-019: Logs MUST be retrieved first
- FR-020: Artifacts MUST be collected first

### FR-021 to FR-035: Provider-Specific

- FR-021: Local: kill processes
- FR-022: Local: remove workspace
- FR-023: Docker: stop container
- FR-024: Docker: remove container
- FR-025: Docker: remove volumes (optional)
- FR-026: SSH: kill remote processes
- FR-027: SSH: remove remote workspace
- FR-028: SSH: close connection
- FR-029: EC2: terminate instance
- FR-030: EC2: wait for termination
- FR-031: EC2: release elastic IP (if any)
- FR-032: EC2: delete security group (if temp)
- FR-033: EC2: clean up key pair (if temp)
- FR-034: All providers MUST log actions
- FR-035: All providers MUST return status

### FR-036 to FR-050: Orphan Detection

- FR-036: Orphan detector MUST exist
- FR-037: Orphan: resource without owner
- FR-038: Owner: tracked by registry
- FR-039: Registry MUST be persistent
- FR-040: Startup MUST scan for orphans
- FR-041: Periodic scan MUST be optional
- FR-042: Default scan: every 15 minutes
- FR-043: Orphan age threshold MUST exist
- FR-044: Default threshold: 1 hour
- FR-045: Orphans MUST be cleaned
- FR-046: Cleanup MUST be logged
- FR-047: Cleanup MUST be auditable
- FR-048: Manual override MUST work
- FR-049: Dry-run MUST be available
- FR-050: Report MUST list all orphans

---

## Non-Functional Requirements

- NFR-001: Teardown MUST complete in <60s
- NFR-002: Force MUST complete in <10s
- NFR-003: No resource leaks
- NFR-004: No orphan accumulation
- NFR-005: Idempotent always
- NFR-006: Structured logging
- NFR-007: Metrics on duration
- NFR-008: Audit trail
- NFR-009: Cross-platform
- NFR-010: Graceful degradation

---

## User Manual Documentation

### Configuration

```yaml
teardown:
  gracePeriodSeconds: 30
  forceTimeoutSeconds: 10
  orphanScanIntervalMinutes: 15
  orphanAgeThresholdMinutes: 60
  retrieveLogsFirst: true
  retrieveArtifactsFirst: true
```

### Example Usage

```csharp
// Graceful teardown
await target.TeardownAsync();

// Force teardown
await target.TeardownAsync(force: true);

// With callback
await target.TeardownAsync(onPhase: phase => 
    Console.WriteLine($"Teardown: {phase}"));
```

### Teardown Phases

| Phase | Description |
|-------|-------------|
| Drain | Complete pending work |
| Retrieve | Get logs/artifacts |
| Terminate | Stop compute |
| Cleanup | Remove resources |
| Complete | Final state |

### CLI Commands

```bash
# Teardown specific target
acode target teardown <session-id>

# Force teardown
acode target teardown <session-id> --force

# List orphans
acode target orphans list

# Clean orphans
acode target orphans clean

# Dry-run cleanup
acode target orphans clean --dry-run
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Teardown releases resources
- [ ] AC-002: Force teardown works
- [ ] AC-003: Idempotent verified
- [ ] AC-004: Orphan detection works
- [ ] AC-005: Orphan cleanup works
- [ ] AC-006: State transitions correct
- [ ] AC-007: Logs/artifacts retrieved
- [ ] AC-008: EC2 terminates
- [ ] AC-009: Docker removes
- [ ] AC-010: No leaks in tests

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Teardown state machine
- [ ] UT-002: Idempotent behavior
- [ ] UT-003: Force vs graceful
- [ ] UT-004: Orphan detection logic

### Integration Tests

- [ ] IT-001: Local process cleanup
- [ ] IT-002: Docker container removal
- [ ] IT-003: Orphan scan and clean
- [ ] IT-004: Crash recovery cleanup

---

## Implementation Prompt

### Interface

```csharp
public record TeardownOptions(
    bool Force = false,
    bool RetrieveLogsFirst = true,
    bool RetrieveArtifactsFirst = true,
    TimeSpan? GracePeriod = null);

public record TeardownResult(
    bool Success,
    TeardownPhase FinalPhase,
    TimeSpan Duration,
    IReadOnlyList<string> ActionsPerformed,
    IReadOnlyList<string> Errors);

public enum TeardownPhase
{
    NotStarted,
    Draining,
    RetrievingLogs,
    RetrievingArtifacts,
    Terminating,
    CleaningUp,
    Complete,
    Failed
}

public interface IOrphanDetector
{
    Task<IReadOnlyList<OrphanResource>> ScanAsync();
    Task CleanAsync(IEnumerable<OrphanResource> orphans);
}

public record OrphanResource(
    string ResourceId,
    string ResourceType,
    DateTime CreatedAt,
    TimeSpan Age,
    string Provider);
```

### State Machine

```
Ready|Preparing|Executing|Completed|Failed → Terminating
Terminating → Complete
```

---

**End of Task 029.d Specification**