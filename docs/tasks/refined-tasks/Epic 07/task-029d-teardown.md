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

| Component | Integration Type | Description |
|-----------|-----------------|-------------|
| Task 029 IComputeTarget | Parent | DisposeAsync is part of target interface |
| Task 031 EC2 Target | Override | EC2-specific termination logic |
| Task 027 Workers | Consumer | Workers trigger teardown after task completion |
| Task 030 SSH Target | Override | SSH-specific cleanup logic |
| ITargetRegistry | Dependency | Tracks active targets for orphan detection |
| IOrphanDetector | Component | Scans for and cleans orphaned resources |
| ITeardownService | Interface | Main contract for teardown logic |

### Failure Modes

| Failure Type | Detection | Recovery | User Impact |
|--------------|-----------|----------|-------------|
| Teardown timeout | Timer expiration | Force terminate | Delayed cleanup |
| Resource stuck | API call hangs | Skip and log, continue others | Manual cleanup may be needed |
| Cloud API failure | Exception | Retry with backoff | Automatic retry |
| Orphan found | Registry mismatch | Automatic cleanup | No user impact |
| Process won't die | SIGTERM timeout | SIGKILL | Forced termination |
| Network unreachable | Connection timeout | Retry then mark for later | Background retry |
| Permission denied | API error | Log and escalate | Admin intervention |
| Partial teardown | Some resources remain | Log state, retry on restart | May need manual cleanup |

---

## Assumptions

1. **Resource Trackability**: All provisioned resources are tracked in a persistent registry
2. **API Availability**: Cloud APIs (EC2, etc.) are reachable for teardown operations
3. **Permission Sufficiency**: Teardown has same or greater permissions as provisioning
4. **Idempotent APIs**: Underlying APIs handle duplicate termination requests gracefully
5. **Registry Durability**: Target registry survives application restarts
6. **Time Synchronization**: Clocks synchronized for orphan age calculation
7. **Network Access**: For remote targets, network is available for cleanup
8. **Graceful Shutdown**: Processes respond to SIGTERM within grace period

---

## Security Considerations

1. **Authorization Check**: Teardown verifies caller has permission to terminate target
2. **Audit Logging**: All teardown operations logged with user, target, timestamp
3. **Credential Cleanup**: Temporary credentials/keys removed during teardown
4. **Secret Purging**: Secrets in environment cleared before workspace deletion
5. **Data Sanitization**: Sensitive workspace data securely deleted (not just unlinked)
6. **Cross-Account Safety**: Teardown cannot affect resources in other accounts
7. **Registry Integrity**: Registry protected from unauthorized modification
8. **Orphan Cleanup Authorization**: Orphan cleanup requires elevated permission

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

### Teardown Operation (FR-029D-01 to FR-029D-20)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-029D-01 | `TeardownAsync(CancellationToken)` or `DisposeAsync()` MUST be defined | Must Have |
| FR-029D-02 | Teardown MUST release compute resources (processes, containers, instances) | Must Have |
| FR-029D-03 | Teardown MUST clean workspace directory | Must Have |
| FR-029D-04 | Teardown MUST remove temporary files created during execution | Must Have |
| FR-029D-05 | Graceful shutdown MUST be attempted first | Must Have |
| FR-029D-06 | Grace period MUST be configurable (default: 30 seconds) | Should Have |
| FR-029D-07 | Force teardown option MUST skip graceful shutdown | Should Have |
| FR-029D-08 | Running processes MUST be killed (SIGTERM then SIGKILL) | Must Have |
| FR-029D-09 | Open network connections MUST be closed | Must Have |
| FR-029D-10 | Open file handles MUST be closed | Must Have |
| FR-029D-11 | Teardown MUST be idempotent (safe to call multiple times) | Must Have |
| FR-029D-12 | Second teardown call MUST succeed without error | Must Have |
| FR-029D-13 | Concurrent teardown calls MUST serialize safely | Must Have |
| FR-029D-14 | State MUST update to TearingDown during teardown | Must Have |
| FR-029D-15 | State MUST update to Terminated after completion | Must Have |
| FR-029D-16 | Terminated state MUST be final (no transitions out) | Must Have |
| FR-029D-17 | Restart after teardown MUST throw InvalidOperationException | Must Have |
| FR-029D-18 | Metrics MUST be captured before resource cleanup | Should Have |
| FR-029D-19 | Logs MUST be retrieved before workspace deletion | Should Have |
| FR-029D-20 | Artifacts MUST be collected before workspace deletion | Should Have |

### Provider-Specific Teardown (FR-029D-21 to FR-029D-35)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-029D-21 | Local: kill all spawned processes | Must Have |
| FR-029D-22 | Local: remove workspace directory | Must Have |
| FR-029D-23 | Local: remove temporary files from system temp | Should Have |
| FR-029D-24 | Docker: stop container gracefully | Must Have |
| FR-029D-25 | Docker: remove container after stop | Must Have |
| FR-029D-26 | Docker: optionally remove volumes (configurable) | Should Have |
| FR-029D-27 | SSH: kill remote processes via SSH | Must Have |
| FR-029D-28 | SSH: remove remote workspace directory | Should Have |
| FR-029D-29 | SSH: close SSH connection | Must Have |
| FR-029D-30 | EC2: terminate instance via API | Must Have |
| FR-029D-31 | EC2: wait for instance termination confirmation | Should Have |
| FR-029D-32 | EC2: release elastic IP if dynamically allocated | Should Have |
| FR-029D-33 | EC2: delete temporary security group if created | Should Have |
| FR-029D-34 | EC2: delete temporary key pair if created | Should Have |
| FR-029D-35 | All providers MUST log all teardown actions | Must Have |

### Orphan Detection (FR-029D-36 to FR-029D-50)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-029D-36 | `IOrphanDetector` interface MUST be defined | Should Have |
| FR-029D-37 | Orphan defined as: resource in cloud but not in registry | Must Have |
| FR-029D-38 | Registry MUST track all active targets with unique ID | Must Have |
| FR-029D-39 | Registry MUST persist to survive application restart | Must Have |
| FR-029D-40 | Application startup MUST scan for orphans | Should Have |
| FR-029D-41 | Periodic orphan scan MUST be configurable (default: every 15 min) | Should Have |
| FR-029D-42 | Orphan age threshold MUST be configurable (default: 1 hour) | Should Have |
| FR-029D-43 | Orphans older than threshold MUST be cleaned up | Should Have |
| FR-029D-44 | Orphan cleanup MUST be logged with resource details | Must Have |
| FR-029D-45 | Orphan cleanup MUST be auditable (who, when, what) | Should Have |
| FR-029D-46 | Manual orphan cleanup command MUST exist | Should Have |
| FR-029D-47 | Dry-run mode MUST be available (list without cleanup) | Should Have |
| FR-029D-48 | Orphan report MUST list all detected orphans | Should Have |
| FR-029D-49 | Orphan report MUST include resource type, ID, age | Should Have |
| FR-029D-50 | Orphan cleanup MUST respect resource tags (only clean our resources) | Must Have |

---

## Non-Functional Requirements

### Performance (NFR-029D-01 to NFR-029D-10)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-029D-01 | Graceful teardown time | <60 seconds | Must Have |
| NFR-029D-02 | Force teardown time | <10 seconds | Must Have |
| NFR-029D-03 | Local workspace deletion (10GB) | <30 seconds | Should Have |
| NFR-029D-04 | EC2 termination confirmation | <2 minutes | Should Have |
| NFR-029D-05 | SSH teardown time | <30 seconds | Should Have |
| NFR-029D-06 | Orphan scan time (100 instances) | <60 seconds | Should Have |
| NFR-029D-07 | Registry query time | <10ms | Should Have |
| NFR-029D-08 | Concurrent teardown support | 50 targets | Should Have |
| NFR-029D-09 | Memory usage during teardown | <10MB | Should Have |
| NFR-029D-10 | Cleanup parallelism | 10 concurrent cleanups | Should Have |

### Reliability (NFR-029D-11 to NFR-029D-20)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-029D-11 | No resource leaks | 100% cleanup | Must Have |
| NFR-029D-12 | No orphan accumulation | <1% orphan rate | Must Have |
| NFR-029D-13 | Idempotent teardown | Always safe to retry | Must Have |
| NFR-029D-14 | Partial failure handling | Continue other cleanups | Must Have |
| NFR-029D-15 | Registry consistency | Always accurate | Must Have |
| NFR-029D-16 | Graceful degradation | Best effort on API failure | Should Have |
| NFR-029D-17 | Retry on transient failure | 3 retries with backoff | Should Have |
| NFR-029D-18 | Timeout handling | Proceed to force after timeout | Must Have |
| NFR-029D-19 | Cross-platform support | Windows, macOS, Linux | Must Have |
| NFR-029D-20 | Application crash recovery | Orphan cleanup on restart | Should Have |

### Observability (NFR-029D-21 to NFR-029D-30)

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-029D-21 | Teardown start log | Info level with target ID | Must Have |
| NFR-029D-22 | Teardown complete log | Info level with duration | Must Have |
| NFR-029D-23 | Force teardown log | Warning level | Should Have |
| NFR-029D-24 | Orphan detection log | Info level with count | Should Have |
| NFR-029D-25 | Orphan cleanup log | Warning level with resource ID | Must Have |
| NFR-029D-26 | Cleanup failure log | Error level with reason | Must Have |
| NFR-029D-27 | Structured logging format | JSON-compatible | Should Have |
| NFR-029D-28 | TargetId in all logs | Correlation | Must Have |
| NFR-029D-29 | Metric: teardown_duration_seconds | Histogram | Should Have |
| NFR-029D-30 | Metric: orphans_cleaned_total | Counter | Should Have |

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

### Core Teardown (AC-029D-01 to AC-029D-15)

- [ ] AC-029D-01: `ITeardownService` interface defined in Application layer
- [ ] AC-029D-02: `TeardownAsync` method accepts optional force flag and CancellationToken
- [ ] AC-029D-03: Graceful teardown waits up to grace period for processes to exit
- [ ] AC-029D-04: Force teardown skips graceful wait and immediately kills
- [ ] AC-029D-05: Running processes killed with SIGTERM then SIGKILL
- [ ] AC-029D-06: All open connections closed
- [ ] AC-029D-07: All file handles released
- [ ] AC-029D-08: Workspace directory removed
- [ ] AC-029D-09: Temporary files removed from system temp
- [ ] AC-029D-10: State transitions to TearingDown during teardown
- [ ] AC-029D-11: State transitions to Terminated on completion
- [ ] AC-029D-12: Terminated is final state (no further transitions)
- [ ] AC-029D-13: Attempting operation on terminated target throws
- [ ] AC-029D-14: Teardown is idempotent (second call succeeds)
- [ ] AC-029D-15: Concurrent teardown calls serialize correctly

### Pre-Teardown Data Capture (AC-029D-16 to AC-029D-25)

- [ ] AC-029D-16: Metrics captured before resource cleanup
- [ ] AC-029D-17: Logs retrieved before workspace deletion
- [ ] AC-029D-18: Artifacts collected before workspace deletion
- [ ] AC-029D-19: Order: metrics → logs → artifacts → teardown
- [ ] AC-029D-20: Failure to capture doesn't block teardown
- [ ] AC-029D-21: Capture failures logged as warnings
- [ ] AC-029D-22: Captured data stored in TeardownResult
- [ ] AC-029D-23: Configurable: retrieveLogsFirst (default true)
- [ ] AC-029D-24: Configurable: retrieveArtifactsFirst (default true)
- [ ] AC-029D-25: Skip capture on force teardown (configurable)

### Provider-Specific Local (AC-029D-26 to AC-029D-35)

- [ ] AC-029D-26: Local teardown kills all spawned processes
- [ ] AC-029D-27: Local teardown removes workspace directory recursively
- [ ] AC-029D-28: Local teardown handles read-only files
- [ ] AC-029D-29: Local teardown handles files in use (retry)
- [ ] AC-029D-30: Docker teardown stops container gracefully
- [ ] AC-029D-31: Docker teardown removes container after stop
- [ ] AC-029D-32: Docker teardown removes volumes if configured
- [ ] AC-029D-33: Docker teardown handles container not found
- [ ] AC-029D-34: Docker teardown handles container already stopped
- [ ] AC-029D-35: All actions logged with resource IDs

### Provider-Specific Remote (AC-029D-36 to AC-029D-45)

- [ ] AC-029D-36: SSH teardown kills remote processes via SSH
- [ ] AC-029D-37: SSH teardown removes remote workspace
- [ ] AC-029D-38: SSH teardown closes SSH connection
- [ ] AC-029D-39: SSH teardown handles connection lost gracefully
- [ ] AC-029D-40: EC2 teardown terminates instance via API
- [ ] AC-029D-41: EC2 teardown waits for termination confirmation
- [ ] AC-029D-42: EC2 teardown releases elastic IP if dynamically allocated
- [ ] AC-029D-43: EC2 teardown deletes temporary security group
- [ ] AC-029D-44: EC2 teardown deletes temporary key pair
- [ ] AC-029D-45: EC2 teardown handles instance already terminated

### Orphan Detection (AC-029D-46 to AC-029D-60)

- [ ] AC-029D-46: `IOrphanDetector` interface defined
- [ ] AC-029D-47: Registry tracks all active targets persistently
- [ ] AC-029D-48: Application startup scans for orphans
- [ ] AC-029D-49: Periodic scan runs at configured interval
- [ ] AC-029D-50: Orphan defined as: resource in cloud but not in registry
- [ ] AC-029D-51: Orphan age threshold configurable (default 1 hour)
- [ ] AC-029D-52: Only orphans older than threshold cleaned
- [ ] AC-029D-53: Resource tags used to identify our resources
- [ ] AC-029D-54: Orphan cleanup logged with full details
- [ ] AC-029D-55: Orphan cleanup auditable (user, time, resources)
- [ ] AC-029D-56: Manual `acode target orphans clean` command works
- [ ] AC-029D-57: Dry-run mode lists without cleaning
- [ ] AC-029D-58: Orphan report includes resource type, ID, age
- [ ] AC-029D-59: Report available in JSON format
- [ ] AC-029D-60: Orphan count exposed as metric

---

## User Verification Scenarios

### Scenario 1: Graceful Local Target Teardown

**Persona:** Developer finishing work session

**Steps:**
1. Create local target and execute some commands
2. Call `target.TeardownAsync()`
3. Observe: State changes to TearingDown
4. Observe: Running processes given chance to exit
5. Observe: Workspace directory removed
6. Observe: State changes to Terminated
7. Verify no orphan processes remain

**Verification:**
- [ ] Graceful shutdown sequence followed
- [ ] Workspace completely removed
- [ ] No orphan processes
- [ ] State is Terminated

### Scenario 2: Force Teardown of Hung Target

**Persona:** Developer with stuck process

**Steps:**
1. Target has process ignoring SIGTERM
2. Call `target.TeardownAsync(force: true)`
3. Observe: Immediate SIGKILL (no 30s wait)
4. Observe: Teardown completes in <10 seconds
5. Verify process actually killed

**Verification:**
- [ ] Force bypasses graceful wait
- [ ] Completes within 10 seconds
- [ ] Stuck process killed

### Scenario 3: Idempotent Teardown

**Persona:** Developer calling teardown multiple times

**Steps:**
1. Call `target.TeardownAsync()` - succeeds
2. Call `target.TeardownAsync()` again - also succeeds
3. No error thrown
4. State remains Terminated

**Verification:**
- [ ] Second call doesn't throw
- [ ] No side effects from second call
- [ ] Correct final state

### Scenario 4: Pre-Teardown Data Capture

**Persona:** Developer needing logs from failed execution

**Steps:**
1. Execute command that generates logs
2. Call teardown
3. Observe: Logs retrieved before workspace deletion
4. Observe: Artifacts collected
5. TeardownResult contains captured data

**Verification:**
- [ ] Logs captured successfully
- [ ] Artifacts captured
- [ ] Data available in result
- [ ] Workspace then deleted

### Scenario 5: Orphan Detection on Restart

**Persona:** Application crashed, restarting

**Steps:**
1. Simulate crash (kill application with active EC2 target)
2. Restart application
3. Observe: Orphan scan runs on startup
4. Observe: "Detected 1 orphan resource: i-abc123"
5. After threshold, observe cleanup

**Verification:**
- [ ] Orphan detected on startup
- [ ] Orphan logged with details
- [ ] Cleanup happens after threshold
- [ ] Resource actually terminated

### Scenario 6: Orphan Dry-Run

**Persona:** Administrator reviewing orphans

**Steps:**
1. Run `acode target orphans list`
2. See list of potential orphans with age
3. Run `acode target orphans clean --dry-run`
4. See what would be cleaned (no actual cleanup)
5. Run `acode target orphans clean` for real

**Verification:**
- [ ] List shows orphans accurately
- [ ] Dry-run doesn't cleanup
- [ ] Real cleanup removes resources
- [ ] Audit log updated

---

## Testing Requirements

### Unit Tests (UT-029D-01 to UT-029D-20)

- [ ] UT-029D-01: TeardownPhase enum has all required values
- [ ] UT-029D-02: State machine allows TearingDown → Terminated
- [ ] UT-029D-03: State machine rejects Terminated → anything
- [ ] UT-029D-04: Idempotent teardown returns success
- [ ] UT-029D-05: Force flag skips graceful wait
- [ ] UT-029D-06: Grace period configurable
- [ ] UT-029D-07: Concurrent teardown serializes
- [ ] UT-029D-08: Pre-teardown capture runs in order
- [ ] UT-029D-09: Capture failure doesn't block teardown
- [ ] UT-029D-10: TeardownResult includes captured data
- [ ] UT-029D-11: Orphan age calculation correct
- [ ] UT-029D-12: Orphan threshold filtering works
- [ ] UT-029D-13: Registry tracks targets correctly
- [ ] UT-029D-14: Registry persists across restarts
- [ ] UT-029D-15: Resource tagging for identification
- [ ] UT-029D-16: Events emitted for each phase
- [ ] UT-029D-17: Metrics recorded correctly
- [ ] UT-029D-18: Error handling doesn't leak resources
- [ ] UT-029D-19: Timeout handling triggers force
- [ ] UT-029D-20: Dry-run mode doesn't cleanup

### Integration Tests (IT-029D-01 to IT-029D-15)

- [ ] IT-029D-01: Local process cleanup end-to-end
- [ ] IT-029D-02: Local workspace deletion
- [ ] IT-029D-03: Docker container stop and remove
- [ ] IT-029D-04: SSH remote cleanup
- [ ] IT-029D-05: EC2 instance termination
- [ ] IT-029D-06: Force teardown of stuck process
- [ ] IT-029D-07: Idempotent teardown
- [ ] IT-029D-08: Orphan detection on startup
- [ ] IT-029D-09: Orphan periodic scan
- [ ] IT-029D-10: Orphan cleanup
- [ ] IT-029D-11: Pre-teardown log capture
- [ ] IT-029D-12: Pre-teardown artifact capture
- [ ] IT-029D-13: Cross-platform teardown
- [ ] IT-029D-14: No resource leaks after 100 teardowns
- [ ] IT-029D-15: Crash recovery cleanup

---

## Implementation Prompt

You are implementing compute target teardown. This handles graceful shutdown, resource cleanup, and orphan detection. Follow Clean Architecture and TDD.

### Part 1: File Structure and Domain Models

#### File Structure

```
src/Acode.Domain/
├── Compute/
│   └── Teardown/
│       ├── TeardownPhase.cs
│       ├── TeardownOptions.cs
│       ├── TeardownResult.cs
│       ├── OrphanResource.cs
│       └── Events/
│           ├── TeardownStartedEvent.cs
│           ├── TeardownPhaseChangedEvent.cs
│           ├── TeardownCompletedEvent.cs
│           └── OrphanDetectedEvent.cs

src/Acode.Application/
├── Compute/
│   └── Teardown/
│       ├── ITeardownService.cs
│       ├── IOrphanDetector.cs
│       ├── IOrphanCleaner.cs
│       └── IResourceTracker.cs

src/Acode.Infrastructure/
├── Compute/
│   └── Teardown/
│       ├── TeardownService.cs
│       ├── OrphanDetector.cs
│       ├── OrphanCleaner.cs
│       ├── ResourceTracker.cs
│       ├── Providers/
│       │   ├── LocalTeardownProvider.cs
│       │   ├── DockerTeardownProvider.cs
│       │   ├── SshTeardownProvider.cs
│       │   └── Ec2TeardownProvider.cs
│       └── Scheduler/
│           └── OrphanScanScheduler.cs

tests/Acode.Infrastructure.Tests/
├── Compute/
│   └── Teardown/
│       ├── TeardownServiceTests.cs
│       ├── OrphanDetectorTests.cs
│       ├── OrphanCleanerTests.cs
│       └── Providers/
│           ├── LocalTeardownProviderTests.cs
│           └── DockerTeardownProviderTests.cs
```

#### Domain Models

```csharp
// src/Acode.Domain/Compute/Teardown/TeardownPhase.cs
namespace Acode.Domain.Compute.Teardown;

public enum TeardownPhase
{
    NotStarted = 0,
    Draining = 1,
    RetrievingLogs = 2,
    RetrievingArtifacts = 3,
    TerminatingProcesses = 4,
    CleaningWorkspace = 5,
    ReleasingResources = 6,
    Complete = 7,
    Failed = 8
}

// src/Acode.Domain/Compute/Teardown/TeardownOptions.cs
namespace Acode.Domain.Compute.Teardown;

public sealed record TeardownOptions
{
    public bool Force { get; init; } = false;
    public bool RetrieveLogsFirst { get; init; } = true;
    public bool RetrieveArtifactsFirst { get; init; } = true;
    public TimeSpan GracePeriod { get; init; } = TimeSpan.FromSeconds(30);
    public TimeSpan ForceTimeout { get; init; } = TimeSpan.FromSeconds(10);
    public bool CleanWorkspace { get; init; } = true;
}

// src/Acode.Domain/Compute/Teardown/TeardownResult.cs
namespace Acode.Domain.Compute.Teardown;

public sealed record TeardownResult
{
    public required bool Success { get; init; }
    public required TeardownPhase FinalPhase { get; init; }
    public required TimeSpan Duration { get; init; }
    public IReadOnlyList<string> ActionsPerformed { get; init; } = [];
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
    
    public static TeardownResult Succeeded(TimeSpan duration, IReadOnlyList<string> actions) =>
        new()
        {
            Success = true,
            FinalPhase = TeardownPhase.Complete,
            Duration = duration,
            ActionsPerformed = actions
        };
    
    public static TeardownResult Failed(TeardownPhase phase, string error, TimeSpan duration) =>
        new()
        {
            Success = false,
            FinalPhase = phase,
            Duration = duration,
            Errors = [error]
        };
}

// src/Acode.Domain/Compute/Teardown/OrphanResource.cs
namespace Acode.Domain.Compute.Teardown;

public sealed record OrphanResource
{
    public required string ResourceId { get; init; }
    public required string ResourceType { get; init; }
    public required string Provider { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public TimeSpan Age => DateTimeOffset.UtcNow - CreatedAt;
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
    public string? LastKnownOwner { get; init; }
}

// src/Acode.Domain/Compute/Teardown/Events/TeardownStartedEvent.cs
namespace Acode.Domain.Compute.Teardown.Events;

public sealed record TeardownStartedEvent(
    ComputeTargetId TargetId,
    bool Force,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/Compute/Teardown/Events/TeardownCompletedEvent.cs
namespace Acode.Domain.Compute.Teardown.Events;

public sealed record TeardownCompletedEvent(
    ComputeTargetId TargetId,
    bool Success,
    TimeSpan Duration,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/Compute/Teardown/Events/OrphanDetectedEvent.cs
namespace Acode.Domain.Compute.Teardown.Events;

public sealed record OrphanDetectedEvent(
    OrphanResource Resource,
    DateTimeOffset Timestamp) : IDomainEvent;
```

**End of Task 029.d Specification - Part 1/3**

### Part 2: Application Interfaces and Infrastructure Implementation

```csharp
// src/Acode.Application/Compute/Teardown/ITeardownService.cs
namespace Acode.Application.Compute.Teardown;

public interface ITeardownService
{
    Task<TeardownResult> TeardownAsync(
        IComputeTarget target,
        TeardownOptions? options = null,
        Action<TeardownPhase>? onPhaseChange = null,
        CancellationToken ct = default);
    
    Task<TeardownResult> ForceTeardownAsync(
        IComputeTarget target,
        CancellationToken ct = default);
}

// src/Acode.Application/Compute/Teardown/IOrphanDetector.cs
namespace Acode.Application.Compute.Teardown;

public interface IOrphanDetector
{
    Task<IReadOnlyList<OrphanResource>> ScanAsync(CancellationToken ct = default);
    Task<IReadOnlyList<OrphanResource>> ScanProviderAsync(
        string provider,
        CancellationToken ct = default);
}

// src/Acode.Application/Compute/Teardown/IOrphanCleaner.cs
namespace Acode.Application.Compute.Teardown;

public interface IOrphanCleaner
{
    Task<CleanupResult> CleanAsync(
        IEnumerable<OrphanResource> orphans,
        bool dryRun = false,
        CancellationToken ct = default);
}

public sealed record CleanupResult(
    int CleanedCount,
    int FailedCount,
    IReadOnlyList<OrphanResource> Cleaned,
    IReadOnlyList<(OrphanResource Resource, string Error)> Failed);

// src/Acode.Application/Compute/Teardown/IResourceTracker.cs
namespace Acode.Application.Compute.Teardown;

public interface IResourceTracker
{
    Task TrackAsync(string resourceId, string resourceType, string provider, CancellationToken ct = default);
    Task UntrackAsync(string resourceId, CancellationToken ct = default);
    Task<bool> IsTrackedAsync(string resourceId, CancellationToken ct = default);
    Task<IReadOnlyList<TrackedResource>> GetAllAsync(CancellationToken ct = default);
}

public sealed record TrackedResource(
    string ResourceId,
    string ResourceType,
    string Provider,
    string? OwnerId,
    DateTimeOffset TrackedAt);

// src/Acode.Infrastructure/Compute/Teardown/TeardownService.cs
namespace Acode.Infrastructure.Compute.Teardown;

public sealed class TeardownService : ITeardownService
{
    private readonly IEnumerable<ITeardownProvider> _providers;
    private readonly IResourceTracker _resourceTracker;
    private readonly IArtifactTransfer _artifactTransfer;
    private readonly IEventPublisher _events;
    private readonly TeardownOptions _defaultOptions;
    private readonly ILogger<TeardownService> _logger;
    
    public TeardownService(
        IEnumerable<ITeardownProvider> providers,
        IResourceTracker resourceTracker,
        IArtifactTransfer artifactTransfer,
        IEventPublisher events,
        IOptions<TeardownOptions> defaultOptions,
        ILogger<TeardownService> logger)
    {
        _providers = providers;
        _resourceTracker = resourceTracker;
        _artifactTransfer = artifactTransfer;
        _events = events;
        _defaultOptions = defaultOptions.Value;
        _logger = logger;
    }
    
    public async Task<TeardownResult> TeardownAsync(
        IComputeTarget target,
        TeardownOptions? options = null,
        Action<TeardownPhase>? onPhaseChange = null,
        CancellationToken ct = default)
    {
        options ??= _defaultOptions;
        var stopwatch = Stopwatch.StartNew();
        var actions = new List<string>();
        var currentPhase = TeardownPhase.NotStarted;
        
        void SetPhase(TeardownPhase phase)
        {
            currentPhase = phase;
            onPhaseChange?.Invoke(phase);
            _logger.LogInformation("Teardown phase: {Phase} for {TargetId}", phase, target.Id);
        }
        
        await _events.PublishAsync(new TeardownStartedEvent(
            target.Id, options.Force, DateTimeOffset.UtcNow));
        
        try
        {
            // Phase 1: Drain pending work
            if (!options.Force)
            {
                SetPhase(TeardownPhase.Draining);
                await DrainAsync(target, options.GracePeriod, ct);
                actions.Add("Drained pending work");
            }
            
            // Phase 2: Retrieve logs
            if (options.RetrieveLogsFirst)
            {
                SetPhase(TeardownPhase.RetrievingLogs);
                await RetrieveLogsAsync(target, ct);
                actions.Add("Retrieved logs");
            }
            
            // Phase 3: Retrieve artifacts
            if (options.RetrieveArtifactsFirst)
            {
                SetPhase(TeardownPhase.RetrievingArtifacts);
                await RetrieveArtifactsAsync(target, ct);
                actions.Add("Retrieved artifacts");
            }
            
            // Phase 4: Terminate processes
            SetPhase(TeardownPhase.TerminatingProcesses);
            var provider = GetProvider(target.Type);
            await provider.TerminateProcessesAsync(target, options.ForceTimeout, ct);
            actions.Add("Terminated processes");
            
            // Phase 5: Clean workspace
            if (options.CleanWorkspace)
            {
                SetPhase(TeardownPhase.CleaningWorkspace);
                await provider.CleanWorkspaceAsync(target, ct);
                actions.Add("Cleaned workspace");
            }
            
            // Phase 6: Release resources
            SetPhase(TeardownPhase.ReleasingResources);
            await provider.ReleaseResourcesAsync(target, ct);
            await _resourceTracker.UntrackAsync(target.Id.Value, ct);
            actions.Add("Released resources");
            
            SetPhase(TeardownPhase.Complete);
            stopwatch.Stop();
            
            await _events.PublishAsync(new TeardownCompletedEvent(
                target.Id, true, stopwatch.Elapsed, DateTimeOffset.UtcNow));
            
            return TeardownResult.Succeeded(stopwatch.Elapsed, actions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Teardown failed at {Phase} for {TargetId}", currentPhase, target.Id);
            stopwatch.Stop();
            
            await _events.PublishAsync(new TeardownCompletedEvent(
                target.Id, false, stopwatch.Elapsed, DateTimeOffset.UtcNow));
            
            return TeardownResult.Failed(currentPhase, ex.Message, stopwatch.Elapsed);
        }
    }
    
    public Task<TeardownResult> ForceTeardownAsync(
        IComputeTarget target,
        CancellationToken ct = default)
    {
        return TeardownAsync(target, new TeardownOptions
        {
            Force = true,
            RetrieveLogsFirst = false,
            RetrieveArtifactsFirst = false,
            GracePeriod = TimeSpan.Zero
        }, null, ct);
    }
    
    private ITeardownProvider GetProvider(ComputeTargetType type) =>
        _providers.FirstOrDefault(p => p.Supports(type))
            ?? throw new InvalidOperationException($"No teardown provider for {type}");
    
    private async Task DrainAsync(IComputeTarget target, TimeSpan gracePeriod, CancellationToken ct)
    {
        // Wait for target to become non-busy or timeout
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(gracePeriod);
        
        while (target.State == ComputeTargetState.Busy && !cts.IsCancellationRequested)
        {
            await Task.Delay(500, cts.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }
}

// src/Acode.Infrastructure/Compute/Teardown/Providers/ITeardownProvider.cs
namespace Acode.Infrastructure.Compute.Teardown.Providers;

public interface ITeardownProvider
{
    bool Supports(ComputeTargetType type);
    Task TerminateProcessesAsync(IComputeTarget target, TimeSpan timeout, CancellationToken ct);
    Task CleanWorkspaceAsync(IComputeTarget target, CancellationToken ct);
    Task ReleaseResourcesAsync(IComputeTarget target, CancellationToken ct);
}
```

**End of Task 029.d Specification - Part 2/3**

### Part 3: Orphan Detection, Provider Implementations, and Rollout

```csharp
// src/Acode.Infrastructure/Compute/Teardown/OrphanDetector.cs
namespace Acode.Infrastructure.Compute.Teardown;

public sealed class OrphanDetector : IOrphanDetector
{
    private readonly IEnumerable<IOrphanScanner> _scanners;
    private readonly IResourceTracker _tracker;
    private readonly OrphanDetectionOptions _options;
    private readonly ILogger<OrphanDetector> _logger;
    
    public OrphanDetector(
        IEnumerable<IOrphanScanner> scanners,
        IResourceTracker tracker,
        IOptions<OrphanDetectionOptions> options,
        ILogger<OrphanDetector> logger)
    {
        _scanners = scanners;
        _tracker = tracker;
        _options = options.Value;
        _logger = logger;
    }
    
    public async Task<IReadOnlyList<OrphanResource>> ScanAsync(CancellationToken ct = default)
    {
        var orphans = new List<OrphanResource>();
        var trackedResources = await _tracker.GetAllAsync(ct);
        var trackedIds = trackedResources.Select(r => r.ResourceId).ToHashSet();
        
        foreach (var scanner in _scanners)
        {
            var resources = await scanner.ScanAsync(ct);
            
            foreach (var resource in resources)
            {
                if (!trackedIds.Contains(resource.ResourceId) &&
                    resource.Age > _options.OrphanAgeThreshold)
                {
                    orphans.Add(resource);
                    _logger.LogWarning(
                        "Orphan detected: {ResourceId} ({Type}) aged {Age}",
                        resource.ResourceId, resource.ResourceType, resource.Age);
                }
            }
        }
        
        return orphans;
    }
    
    public async Task<IReadOnlyList<OrphanResource>> ScanProviderAsync(
        string provider,
        CancellationToken ct = default)
    {
        var scanner = _scanners.FirstOrDefault(s => s.Provider == provider);
        if (scanner == null)
            return [];
        
        var trackedResources = await _tracker.GetAllAsync(ct);
        var trackedIds = trackedResources.Select(r => r.ResourceId).ToHashSet();
        
        var resources = await scanner.ScanAsync(ct);
        return resources
            .Where(r => !trackedIds.Contains(r.ResourceId) && r.Age > _options.OrphanAgeThreshold)
            .ToList();
    }
}

public interface IOrphanScanner
{
    string Provider { get; }
    Task<IReadOnlyList<OrphanResource>> ScanAsync(CancellationToken ct = default);
}

// src/Acode.Infrastructure/Compute/Teardown/OrphanCleaner.cs
namespace Acode.Infrastructure.Compute.Teardown;

public sealed class OrphanCleaner : IOrphanCleaner
{
    private readonly IEnumerable<ITeardownProvider> _providers;
    private readonly IEventPublisher _events;
    private readonly ILogger<OrphanCleaner> _logger;
    
    public OrphanCleaner(
        IEnumerable<ITeardownProvider> providers,
        IEventPublisher events,
        ILogger<OrphanCleaner> logger)
    {
        _providers = providers;
        _events = events;
        _logger = logger;
    }
    
    public async Task<CleanupResult> CleanAsync(
        IEnumerable<OrphanResource> orphans,
        bool dryRun = false,
        CancellationToken ct = default)
    {
        var cleaned = new List<OrphanResource>();
        var failed = new List<(OrphanResource, string)>();
        
        foreach (var orphan in orphans)
        {
            if (dryRun)
            {
                _logger.LogInformation(
                    "[DRY-RUN] Would clean orphan: {ResourceId} ({Type})",
                    orphan.ResourceId, orphan.ResourceType);
                cleaned.Add(orphan);
                continue;
            }
            
            try
            {
                await CleanOrphanAsync(orphan, ct);
                cleaned.Add(orphan);
                _logger.LogInformation(
                    "Cleaned orphan: {ResourceId} ({Type})",
                    orphan.ResourceId, orphan.ResourceType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean orphan: {ResourceId}", orphan.ResourceId);
                failed.Add((orphan, ex.Message));
            }
        }
        
        return new CleanupResult(cleaned.Count, failed.Count, cleaned, failed);
    }
    
    private Task CleanOrphanAsync(OrphanResource orphan, CancellationToken ct)
    {
        // Delegate to appropriate provider based on resource type
        return orphan.ResourceType switch
        {
            "ec2-instance" => CleanEc2InstanceAsync(orphan, ct),
            "docker-container" => CleanDockerContainerAsync(orphan, ct),
            "ssh-session" => CleanSshSessionAsync(orphan, ct),
            "workspace" => CleanWorkspaceAsync(orphan, ct),
            _ => Task.CompletedTask
        };
    }
}

// src/Acode.Infrastructure/Compute/Teardown/Providers/LocalTeardownProvider.cs
namespace Acode.Infrastructure.Compute.Teardown.Providers;

public sealed class LocalTeardownProvider : ITeardownProvider
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<LocalTeardownProvider> _logger;
    
    public bool Supports(ComputeTargetType type) => type == ComputeTargetType.Local;
    
    public async Task TerminateProcessesAsync(
        IComputeTarget target,
        TimeSpan timeout,
        CancellationToken ct)
    {
        // Local targets: find and kill child processes
        _logger.LogInformation("Terminating local processes for {TargetId}", target.Id);
        // Implementation would track spawned PIDs and terminate them
    }
    
    public Task CleanWorkspaceAsync(IComputeTarget target, CancellationToken ct)
    {
        var workspacePath = target.Metadata.Get<string>("WorkspacePath");
        if (workspacePath != null && _fileSystem.Directory.Exists(workspacePath))
        {
            _fileSystem.Directory.Delete(workspacePath, recursive: true);
            _logger.LogInformation("Cleaned workspace: {Path}", workspacePath);
        }
        return Task.CompletedTask;
    }
    
    public Task ReleaseResourcesAsync(IComputeTarget target, CancellationToken ct)
    {
        // Local targets have minimal resources to release
        return Task.CompletedTask;
    }
}

// src/Acode.Infrastructure/Compute/Teardown/Scheduler/OrphanScanScheduler.cs
namespace Acode.Infrastructure.Compute.Teardown.Scheduler;

public sealed class OrphanScanScheduler : BackgroundService
{
    private readonly IOrphanDetector _detector;
    private readonly IOrphanCleaner _cleaner;
    private readonly OrphanDetectionOptions _options;
    private readonly ILogger<OrphanScanScheduler> _logger;
    
    public OrphanScanScheduler(
        IOrphanDetector detector,
        IOrphanCleaner cleaner,
        IOptions<OrphanDetectionOptions> options,
        ILogger<OrphanScanScheduler> logger)
    {
        _detector = detector;
        _cleaner = cleaner;
        _options = options.Value;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.ScanInterval, stoppingToken);
                
                _logger.LogDebug("Starting orphan scan");
                var orphans = await _detector.ScanAsync(stoppingToken);
                
                if (orphans.Count > 0)
                {
                    _logger.LogWarning("Found {Count} orphan resources", orphans.Count);
                    
                    if (_options.AutoClean)
                    {
                        await _cleaner.CleanAsync(orphans, dryRun: false, stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Orphan scan failed");
            }
        }
    }
}

public sealed class OrphanDetectionOptions
{
    public TimeSpan ScanInterval { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan OrphanAgeThreshold { get; set; } = TimeSpan.FromHours(1);
    public bool AutoClean { get; set; } = false;
}
```

---

## Implementation Checklist

- [ ] Create TeardownPhase enum and TeardownResult record
- [ ] Define TeardownOptions with configurable timeouts
- [ ] Implement OrphanResource record with age calculation
- [ ] Create domain events for teardown lifecycle
- [ ] Implement ITeardownService with phased shutdown
- [ ] Build IOrphanDetector with multi-provider scanning
- [ ] Create IOrphanCleaner with dry-run support
- [ ] Implement IResourceTracker for ownership tracking
- [ ] Build LocalTeardownProvider as baseline
- [ ] Create OrphanScanScheduler background service
- [ ] Write unit tests for all components (TDD)
- [ ] Test idempotent teardown behavior
- [ ] Test force vs graceful teardown
- [ ] Verify orphan detection accuracy
- [ ] Test cleanup with various resource types
- [ ] Integration test full teardown lifecycle

---

## Rollout Plan

1. **Phase 1**: Domain models (phases, options, results)
2. **Phase 2**: Application interfaces
3. **Phase 3**: TeardownService orchestrator
4. **Phase 4**: ResourceTracker for ownership
5. **Phase 5**: LocalTeardownProvider baseline
6. **Phase 6**: OrphanDetector and OrphanCleaner
7. **Phase 7**: OrphanScanScheduler background service
8. **Phase 8**: Integration testing

---

**End of Task 029.d Specification**