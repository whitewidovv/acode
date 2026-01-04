# Task 031.b: EC2 Instance Management

**Priority:** P1 – High  
**Tier:** L – Cloud Integration  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 031 (EC2 Target), Task 031.a (Provisioning)  

---

## Description

Task 031.b implements EC2 instance lifecycle management. Instances MUST be monitored. States MUST be tracked. Termination MUST be reliable.

Instance management covers the running phase. Health checks detect issues. Stop/start preserves instances. Terminate releases resources.

This task ensures no orphan instances. Cost control requires reliable cleanup.

### Business Value

Instance management provides:
- Resource visibility
- Cost control
- Health monitoring
- Clean shutdown

### Scope Boundaries

This task covers instance lifecycle. Provisioning is in 031.a. Cost tracking is in 031.c.

### Integration Points

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Task 031.a Provisioning | IEc2Provisioner | Instance IDs from provisioning | Lifecycle starts here |
| Task 029.d Teardown | ITeardown | Terminate request | Cleanup trigger |
| Task 033 Heuristics | IBurstHeuristics | Health status reports | Decision input |
| AWS SDK EC2 | IAmazonEC2 | All EC2 lifecycle APIs | Cloud provider |
| AWS CloudWatch | IAmazonCloudWatch | Instance metrics | Monitoring |
| SSH Connection | ISshConnection | SSH health checks | Secondary check |
| Event System | IEventBus | State change notifications | Observers |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Terminate fails | API error | Retry with force terminate | Delayed cleanup |
| Instance stuck in stopping | Timeout (2 min) | Force terminate | Resource stuck |
| API rate limit | ThrottlingException | Exponential backoff | Delayed response |
| Orphan detected | Tag scan + no session | Auto-cleanup | Cost leak prevented |
| SSH health check fails | Connection timeout | Mark impaired, notify | Degraded instance |
| Scheduled retirement | AWS event notification | Notify and prepare migration | Planned downtime |
| Stop fails | API error | Retry or force stop | Instance continues |
| Start fails after stop | API error | Re-provision option | Restart required |

---

## Assumptions

1. **AWS SDK**: Implementation uses AWS SDK for .NET v3
2. **IAM Permissions**: Caller has ec2:DescribeInstances, ec2:StopInstances, ec2:StartInstances, ec2:TerminateInstances
3. **Tagging**: All managed instances have acode=true and acode-session-id tags
4. **DeleteOnTermination**: EBS volumes configured to delete on termination
5. **No Termination Protection**: Instances created without API termination protection
6. **CloudWatch Access**: Permissions for cloudwatch:GetMetricData for monitoring
7. **SSH Access**: SSH connection available for health checks
8. **Event Visibility**: DescribeInstanceStatus returns scheduled events

---

## Security Considerations

1. **Orphan Cleanup Auth**: Only instances with matching session tags are cleaned
2. **Force Terminate Logging**: Force terminate operations MUST be audit logged
3. **Credential Isolation**: AWS credentials MUST NOT appear in logs
4. **State Change Authorization**: Only session owner can stop/start/terminate
5. **Health Check Isolation**: Health checks MUST NOT execute arbitrary commands
6. **Metric Filtering**: CloudWatch metrics scoped to managed instances only
7. **Scheduled Event Handling**: Retirement notifications trigger secure migration
8. **Cleanup Verification**: Termination verified before reporting complete

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| StopInstances | Pause instance (billed for storage) |
| StartInstances | Resume stopped instance |
| TerminateInstances | Destroy instance |
| RebootInstances | Restart instance |
| InstanceStatus | Health check result |
| SystemStatus | Hypervisor health |

---

## Out of Scope

- Instance hibernation
- Instance migration
- Instance snapshots
- Instance cloning
- Scheduled actions
- CloudWatch alarms

---

## Functional Requirements

### State Tracking

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031B-01 | Instance state MUST be tracked internally | P0 |
| FR-031B-02 | AWS states MUST map to internal states | P0 |
| FR-031B-03 | `pending` → `Preparing` state | P0 |
| FR-031B-04 | `running` → `Ready` state | P0 |
| FR-031B-05 | `stopping` → `Terminating` state | P0 |
| FR-031B-06 | `stopped` → `Suspended` state | P0 |
| FR-031B-07 | `shutting-down` → `Terminating` state | P0 |
| FR-031B-08 | `terminated` → `Terminated` state | P0 |
| FR-031B-09 | State polling MUST run continuously | P0 |
| FR-031B-10 | Poll interval MUST be 15 seconds | P0 |
| FR-031B-11 | Polling MUST use `DescribeInstances` API | P0 |
| FR-031B-12 | Batch polling MUST work for multiple instances | P0 |
| FR-031B-13 | Batch size MUST be up to 50 instances per call | P1 |
| FR-031B-14 | State changes MUST trigger notifications | P0 |
| FR-031B-15 | Notification via callback/event | P0 |
| FR-031B-16 | State history MUST be logged | P1 |
| FR-031B-17 | Current state MUST be queryable | P0 |
| FR-031B-18 | Last state update timestamp MUST be tracked | P1 |
| FR-031B-19 | Polling MUST stop on terminal state | P0 |
| FR-031B-20 | Terminal state is `terminated` | P0 |

### Health Monitoring

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031B-21 | Health check MUST run periodically | P0 |
| FR-031B-22 | `DescribeInstanceStatus` API MUST be used | P0 |
| FR-031B-23 | System status MUST be checked (ok/impaired) | P0 |
| FR-031B-24 | Instance status MUST be checked (ok/impaired) | P0 |
| FR-031B-25 | Impaired status MUST trigger alert callback | P0 |
| FR-031B-26 | Scheduled events MUST be checked | P1 |
| FR-031B-27 | Reboot event MUST notify with timestamp | P1 |
| FR-031B-28 | Stop event MUST notify with timestamp | P1 |
| FR-031B-29 | Retire event MUST notify with timestamp | P0 |
| FR-031B-30 | Health check interval MUST be 60 seconds | P0 |
| FR-031B-31 | SSH health check MUST complement AWS checks | P0 |
| FR-031B-32 | SSH check: execute `echo ok` test command | P0 |
| FR-031B-33 | Failed health MUST trigger configurable action | P0 |
| FR-031B-34 | Action: notify callback with details | P0 |
| FR-031B-35 | Action: optional auto-terminate on threshold | P2 |
| FR-031B-36 | Health status MUST be queryable | P0 |
| FR-031B-37 | Status values: `Healthy`, `Impaired`, `Unknown` | P0 |
| FR-031B-38 | Metrics MUST track health check results | P1 |
| FR-031B-39 | CloudWatch metrics MUST be fetchable | P2 |
| FR-031B-40 | Metrics: CPU, network, disk utilization | P2 |

### Stop/Start Operations

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031B-41 | Stop operation MUST be available | P0 |
| FR-031B-42 | `StopInstances` API MUST be called | P0 |
| FR-031B-43 | Stop MUST wait for `stopped` state | P0 |
| FR-031B-44 | Stop timeout MUST be 2 minutes | P0 |
| FR-031B-45 | Force stop MUST be available as option | P1 |
| FR-031B-46 | Force stop: `Force=true` parameter | P1 |
| FR-031B-47 | Start operation MUST be available | P0 |
| FR-031B-48 | `StartInstances` API MUST be called | P0 |
| FR-031B-49 | Start MUST wait for `running` state | P0 |
| FR-031B-50 | Start MUST update tracked public IP | P0 |
| FR-031B-51 | Public IP may change after start | P0 |
| FR-031B-52 | SSH connection MUST re-establish after start | P0 |
| FR-031B-53 | Workspace MUST be preserved across stop/start | P0 |
| FR-031B-54 | EBS data MUST persist across stop/start | P0 |
| FR-031B-55 | Instance store data MUST NOT persist | P1 |
| FR-031B-56 | Warning MUST be logged for instance store loss | P1 |
| FR-031B-57 | Reboot operation MUST be available | P1 |
| FR-031B-58 | `RebootInstances` API MUST be called | P1 |
| FR-031B-59 | Reboot MUST wait for SSH reconnection | P1 |
| FR-031B-60 | Reboot MUST preserve public IP | P1 |

### Termination and Cleanup

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031B-61 | Terminate operation MUST work | P0 |
| FR-031B-62 | `TerminateInstances` API MUST be called | P0 |
| FR-031B-63 | Terminate MUST wait for `terminated` state | P0 |
| FR-031B-64 | Terminate timeout MUST be 2 minutes | P0 |
| FR-031B-65 | Already terminated MUST NOT error (idempotent) | P0 |
| FR-031B-66 | Termination MUST be idempotent | P0 |
| FR-031B-67 | EBS volumes MUST be deleted (DeleteOnTermination) | P0 |
| FR-031B-68 | Temp security groups MUST be deleted | P1 |
| FR-031B-69 | Temp key pairs MUST be deleted | P0 |
| FR-031B-70 | Associated Elastic IP MUST be released | P1 |
| FR-031B-71 | Termination protection MUST be off | P0 |
| FR-031B-72 | `DisableApiTermination = false` MUST be set | P0 |
| FR-031B-73 | Force terminate MUST bypass stuck states | P0 |
| FR-031B-74 | Terminate MUST log with instance ID | P0 |
| FR-031B-75 | Terminate MUST emit metrics | P1 |
| FR-031B-76 | Terminate MUST notify callback on complete | P0 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-031B-01 | Terminate operation completion | <30 seconds | P0 |
| NFR-031B-02 | State poll response time | <500ms | P0 |
| NFR-031B-03 | Stop operation completion | <2 minutes | P0 |
| NFR-031B-04 | Start operation completion | <2 minutes | P0 |
| NFR-031B-05 | Health check execution time | <5 seconds | P0 |
| NFR-031B-06 | Batch polling efficiency | 50 instances/call | P1 |
| NFR-031B-07 | CloudWatch metrics retrieval | <10 seconds | P2 |
| NFR-031B-08 | State notification latency | <1 second | P1 |
| NFR-031B-09 | Force terminate effectiveness | 100% success | P0 |
| NFR-031B-10 | API call efficiency | Minimal calls | P1 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-031B-11 | No orphan instances after failure | 100% cleanup | P0 |
| NFR-031B-12 | Cleanup reliability | 100% resources freed | P0 |
| NFR-031B-13 | Graceful degradation on AWS issues | Continue with cached state | P0 |
| NFR-031B-14 | API retries with exponential backoff | 3 retries | P0 |
| NFR-031B-15 | Idempotent operations | Safe to retry | P0 |
| NFR-031B-16 | State consistency after restart | Recover from DB | P1 |
| NFR-031B-17 | Health check reliability | Zero false negatives | P0 |
| NFR-031B-18 | Scheduled event handling | 100% detected | P1 |
| NFR-031B-19 | EBS volume cleanup | All deleted | P0 |
| NFR-031B-20 | Key pair cleanup | All deleted | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-031B-21 | Structured logging for all operations | JSON with correlation ID | P0 |
| NFR-031B-22 | Metrics for lifecycle events | Counter per operation | P1 |
| NFR-031B-23 | Audit trail for terminate operations | Full history | P0 |
| NFR-031B-24 | Health status visibility | Real-time queryable | P0 |
| NFR-031B-25 | State change event streaming | Via IObservable | P1 |
| NFR-031B-26 | Orphan detection logging | Each scan logged | P1 |
| NFR-031B-27 | Cost attribution per instance | Duration tracked | P2 |
| NFR-031B-28 | Error categorization | Typed exception codes | P1 |
| NFR-031B-29 | AWS API call tracing | X-Ray compatible | P2 |
| NFR-031B-30 | Cleanup verification logging | Post-terminate check | P1 |

---

## User Manual Documentation

### Instance States

| AWS State | Internal State | Billable |
|-----------|----------------|----------|
| pending | Preparing | Yes (from launch) |
| running | Ready | Yes |
| stopping | Terminating | No |
| stopped | Suspended | No (storage only) |
| shutting-down | Terminating | No |
| terminated | Terminated | No |

### CLI Commands

```bash
# List instances
acode target ec2 list

# Stop instance (preserves)
acode target ec2 stop <session-id>

# Start instance
acode target ec2 start <session-id>

# Terminate instance
acode target ec2 terminate <session-id>

# Force terminate
acode target ec2 terminate <session-id> --force
```

### Health Status

| Status | Meaning |
|--------|---------|
| Healthy | All checks pass |
| Impaired | AWS or SSH check failed |
| Unknown | Cannot determine |

---

## Acceptance Criteria / Definition of Done

### State Tracking
- [ ] AC-001: Instance state tracked correctly
- [ ] AC-002: AWS states map to internal states
- [ ] AC-003: pending → Preparing works
- [ ] AC-004: running → Ready works
- [ ] AC-005: stopping/shutting-down → Terminating works
- [ ] AC-006: stopped → Suspended works
- [ ] AC-007: terminated → Terminated works
- [ ] AC-008: Polling runs every 15 seconds
- [ ] AC-009: DescribeInstances API used
- [ ] AC-010: Batch polling works (50 instances)
- [ ] AC-011: State changes trigger notifications
- [ ] AC-012: State history logged
- [ ] AC-013: Current state queryable
- [ ] AC-014: Polling stops on terminal state

### Health Monitoring
- [ ] AC-015: Health check runs every 60 seconds
- [ ] AC-016: DescribeInstanceStatus used
- [ ] AC-017: System status checked (ok/impaired)
- [ ] AC-018: Instance status checked
- [ ] AC-019: Impaired status triggers alert
- [ ] AC-020: Scheduled events detected
- [ ] AC-021: Reboot events notify
- [ ] AC-022: Retire events notify
- [ ] AC-023: SSH health check works
- [ ] AC-024: Health status queryable
- [ ] AC-025: Status values correct (Healthy/Impaired/Unknown)

### Stop/Start Operations
- [ ] AC-026: Stop operation works
- [ ] AC-027: StopInstances API called
- [ ] AC-028: Stop waits for stopped state
- [ ] AC-029: Stop timeout is 2 minutes
- [ ] AC-030: Force stop works
- [ ] AC-031: Start operation works
- [ ] AC-032: StartInstances API called
- [ ] AC-033: Start waits for running state
- [ ] AC-034: Public IP updated after start
- [ ] AC-035: SSH reconnects after start
- [ ] AC-036: Workspace preserved across stop/start
- [ ] AC-037: EBS data persists
- [ ] AC-038: Reboot operation works
- [ ] AC-039: Reboot waits for SSH
- [ ] AC-040: Reboot preserves IP

### Termination
- [ ] AC-041: Terminate operation works
- [ ] AC-042: TerminateInstances API called
- [ ] AC-043: Terminate waits for terminated state
- [ ] AC-044: Terminate timeout is 2 minutes
- [ ] AC-045: Already terminated doesn't error
- [ ] AC-046: Termination is idempotent
- [ ] AC-047: EBS volumes deleted
- [ ] AC-048: Temp security groups cleaned
- [ ] AC-049: Temp key pairs cleaned
- [ ] AC-050: Elastic IP released
- [ ] AC-051: Force terminate bypasses stuck states
- [ ] AC-052: Terminate logs with instance ID
- [ ] AC-053: Terminate emits metrics
- [ ] AC-054: Terminate notifies callback

### Reliability
- [ ] AC-055: No orphan instances after any failure
- [ ] AC-056: API retries work on transient errors
- [ ] AC-057: Graceful degradation on AWS issues
- [ ] AC-058: Idempotent operations verified
- [ ] AC-059: Audit trail complete
- [ ] AC-060: All cleanup resources verified

---

## User Verification Scenarios

### Scenario 1: Developer Stops Instance Overnight
**Persona:** Cost-conscious developer  
**Preconditions:** EC2 instance running with work in progress  
**Steps:**
1. Execute stop command
2. Verify instance reaches stopped state
3. Wait overnight
4. Execute start command
5. Verify workspace intact

**Verification Checklist:**
- [ ] Stop completes within 2 minutes
- [ ] State transitions: Ready → Terminating → Suspended
- [ ] No charges for compute during stopped
- [ ] Start completes within 2 minutes
- [ ] Public IP may change (verified)
- [ ] SSH reconnects successfully
- [ ] EBS data intact
- [ ] Workspace files preserved

### Scenario 2: Graceful Termination After Job
**Persona:** CI system cleaning up  
**Preconditions:** Job completed, instance no longer needed  
**Steps:**
1. Execute terminate command
2. Verify terminated state
3. Verify all resources cleaned

**Verification Checklist:**
- [ ] Terminate completes within 30 seconds
- [ ] State transitions: Ready → Terminating → Terminated
- [ ] EBS volumes deleted (DeleteOnTermination)
- [ ] Temp key pair deleted
- [ ] Elastic IP released (if any)
- [ ] Metrics emitted
- [ ] Callback notified

### Scenario 3: Force Terminate Stuck Instance
**Persona:** Admin dealing with stuck instance  
**Preconditions:** Instance stuck in stopping state  
**Steps:**
1. Attempt normal terminate (times out)
2. Execute force terminate
3. Verify termination completes

**Verification Checklist:**
- [ ] Normal terminate timeout detected
- [ ] Force terminate initiated
- [ ] Termination completes
- [ ] All resources cleaned
- [ ] Force flag logged for audit

### Scenario 4: Health Monitoring Detects Failure
**Persona:** SRE monitoring production  
**Preconditions:** Instance running, health checks active  
**Steps:**
1. Observe health status = Healthy
2. Simulate SSH failure
3. Observe health status change
4. Verify alert triggered

**Verification Checklist:**
- [ ] Health status shows Healthy initially
- [ ] SSH health check fails after simulation
- [ ] Status changes to Impaired
- [ ] Alert callback triggered
- [ ] Event logged with details

### Scenario 5: Scheduled Retirement Handling
**Persona:** DevOps handling AWS event  
**Preconditions:** AWS scheduled instance retirement  
**Steps:**
1. Health check detects scheduled event
2. Notification sent with event details
3. Migration initiated
4. Old instance terminated after migration

**Verification Checklist:**
- [ ] Retirement event detected via API
- [ ] Notification includes event timestamp
- [ ] Adequate warning time provided
- [ ] Migration option available
- [ ] Old instance cleaned up

### Scenario 6: Orphan Detection and Cleanup
**Persona:** Admin running cost audit  
**Preconditions:** Orphan instances exist (no matching session)  
**Steps:**
1. Run orphan scan
2. Identify instances with acode=true but no session
3. Verify cleanup executes
4. Confirm no orphans remain

**Verification Checklist:**
- [ ] Orphan scan finds tagged instances
- [ ] Missing session detected
- [ ] Cleanup terminates orphans
- [ ] All resources freed
- [ ] Audit log shows cleanup

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-031B-01 | AWS state to internal state mapping | FR-031B-02-08 |
| UT-031B-02 | Health status determination logic | FR-031B-21-37 |
| UT-031B-03 | Termination sequence with cleanup | FR-031B-61-76 |
| UT-031B-04 | Cleanup resource ordering | FR-031B-67-72 |
| UT-031B-05 | State polling interval logic | FR-031B-09-10 |
| UT-031B-06 | Batch polling construction | FR-031B-12-13 |
| UT-031B-07 | Stop/start state transitions | FR-031B-41-60 |
| UT-031B-08 | Force terminate logic | FR-031B-73 |
| UT-031B-09 | Scheduled event parsing | FR-031B-26-29 |
| UT-031B-10 | Idempotency detection | FR-031B-65-66 |
| UT-031B-11 | API retry policy | NFR-031B-14 |
| UT-031B-12 | Notification callback | FR-031B-14-15 |
| UT-031B-13 | Terminal state detection | FR-031B-19-20 |
| UT-031B-14 | Public IP update logic | FR-031B-50-51 |
| UT-031B-15 | Instance store warning | FR-031B-55-56 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-031B-01 | Real stop/start cycle | FR-031B-41-53 |
| IT-031B-02 | Real terminate operation | FR-031B-61-76 |
| IT-031B-03 | Force terminate stuck instance | FR-031B-73 |
| IT-031B-04 | Orphan detection and cleanup | NFR-031B-11 |
| IT-031B-05 | Health monitoring E2E | FR-031B-21-38 |
| IT-031B-06 | SSH health check | FR-031B-31-32 |
| IT-031B-07 | State polling accuracy | FR-031B-09-18 |
| IT-031B-08 | EBS volume cleanup verification | FR-031B-67 |
| IT-031B-09 | Key pair cleanup verification | FR-031B-69 |
| IT-031B-10 | Elastic IP release verification | FR-031B-70 |
| IT-031B-11 | Terminate idempotency | FR-031B-65-66 |
| IT-031B-12 | Reboot operation | FR-031B-57-60 |
| IT-031B-13 | CloudWatch metrics retrieval | FR-031B-39-40 |
| IT-031B-14 | Scheduled event detection | FR-031B-26-29 |
| IT-031B-15 | API retry on throttling | NFR-031B-14 |

---

## Implementation Prompt

### Part 1: File Structure and Domain Models

**Target Directory Structure:**
```
src/
├── Acode.Domain/
│   └── Compute/
│       └── Ec2/
│           └── Management/
│               ├── InternalInstanceState.cs
│               ├── Ec2HealthStatus.cs
│               ├── ScheduledEvent.cs
│               └── Events/
│                   ├── InstanceStateChangedEvent.cs
│                   ├── InstanceHealthChangedEvent.cs
│                   └── InstanceTerminatedEvent.cs
├── Acode.Application/
│   └── Compute/
│       └── Ec2/
│           └── Management/
│               ├── IEc2InstanceManager.cs
│               ├── IEc2StatePoller.cs
│               ├── IEc2HealthMonitor.cs
│               └── TerminateOptions.cs
└── Acode.Infrastructure/
    └── Compute/
        └── Ec2/
            └── Management/
                ├── Ec2InstanceManager.cs
                ├── Ec2StatePoller.cs
                ├── Ec2HealthMonitor.cs
                └── Ec2CleanupService.cs
```

**Domain Models:**

```csharp
// src/Acode.Domain/Compute/Ec2/Management/InternalInstanceState.cs
namespace Acode.Domain.Compute.Ec2.Management;

public enum InternalInstanceState
{
    Unknown,
    Preparing,    // pending
    Ready,        // running
    Suspended,    // stopped
    Terminating,  // stopping, shutting-down
    Terminated    // terminated
}

// src/Acode.Domain/Compute/Ec2/Management/Ec2HealthStatus.cs
namespace Acode.Domain.Compute.Ec2.Management;

public sealed record Ec2HealthStatus
{
    public required HealthState State { get; init; }
    public required string SystemStatus { get; init; }
    public required string InstanceStatus { get; init; }
    public bool SshReachable { get; init; }
    public IReadOnlyList<ScheduledEvent> ScheduledEvents { get; init; } = [];
    public DateTimeOffset CheckedAt { get; init; } = DateTimeOffset.UtcNow;
}

public enum HealthState { Healthy, Impaired, Unknown }

// src/Acode.Domain/Compute/Ec2/Management/ScheduledEvent.cs
namespace Acode.Domain.Compute.Ec2.Management;

public sealed record ScheduledEvent
{
    public required string EventType { get; init; }
    public required string Description { get; init; }
    public required DateTimeOffset NotBefore { get; init; }
    public DateTimeOffset? NotAfter { get; init; }
}

// src/Acode.Domain/Compute/Ec2/Management/Events/InstanceStateChangedEvent.cs
namespace Acode.Domain.Compute.Ec2.Management.Events;

public sealed record InstanceStateChangedEvent(
    string InstanceId,
    InternalInstanceState PreviousState,
    InternalInstanceState NewState,
    DateTimeOffset ChangedAt);

// src/Acode.Domain/Compute/Ec2/Management/Events/InstanceTerminatedEvent.cs
namespace Acode.Domain.Compute.Ec2.Management.Events;

public sealed record InstanceTerminatedEvent(
    string InstanceId,
    TimeSpan TotalRuntime,
    bool WasForced,
    bool CleanupComplete,
    DateTimeOffset TerminatedAt);
```

**End of Task 031.b Specification - Part 1/3**

### Part 2: Application Interfaces

```csharp
// src/Acode.Application/Compute/Ec2/Management/IEc2InstanceManager.cs
namespace Acode.Application.Compute.Ec2.Management;

public interface IEc2InstanceManager
{
    Task<Ec2InstanceInfo?> GetInstanceAsync(
        string instanceId,
        CancellationToken ct = default);
    
    Task<InternalInstanceState> GetStateAsync(
        string instanceId,
        CancellationToken ct = default);
    
    Task StopAsync(
        string instanceId,
        bool force = false,
        CancellationToken ct = default);
    
    Task StartAsync(
        string instanceId,
        CancellationToken ct = default);
    
    Task RebootAsync(
        string instanceId,
        CancellationToken ct = default);
    
    Task TerminateAsync(
        string instanceId,
        TerminateOptions? options = null,
        CancellationToken ct = default);
    
    Task<IReadOnlyList<Ec2InstanceInfo>> ListAcodeInstancesAsync(
        string region,
        CancellationToken ct = default);
}

// src/Acode.Application/Compute/Ec2/Management/TerminateOptions.cs
namespace Acode.Application.Compute.Ec2.Management;

public sealed record TerminateOptions
{
    public bool Force { get; init; } = false;
    public bool CleanupSecurityGroup { get; init; } = true;
    public bool CleanupKeyPair { get; init; } = true;
    public bool ReleaseElasticIp { get; init; } = true;
    public TimeSpan Timeout { get; init; } = TimeSpan.FromMinutes(2);
}

// src/Acode.Application/Compute/Ec2/Management/IEc2StatePoller.cs
namespace Acode.Application.Compute.Ec2.Management;

public interface IEc2StatePoller : IDisposable
{
    event Action<string, InternalInstanceState>? StateChanged;
    
    void StartPolling(string instanceId, TimeSpan interval);
    void StopPolling(string instanceId);
    void StopAll();
    
    InternalInstanceState? GetLastKnownState(string instanceId);
}

// src/Acode.Application/Compute/Ec2/Management/IEc2HealthMonitor.cs
namespace Acode.Application.Compute.Ec2.Management;

public interface IEc2HealthMonitor
{
    Task<Ec2HealthStatus> CheckHealthAsync(
        string instanceId,
        CancellationToken ct = default);
    
    void StartMonitoring(
        string instanceId,
        TimeSpan interval,
        Action<Ec2HealthStatus>? onHealthChange = null);
    
    void StopMonitoring(string instanceId);
    
    Ec2HealthStatus? GetLastKnownHealth(string instanceId);
}
```

**End of Task 031.b Specification - Part 2/3**

### Part 3: Infrastructure Implementation and Checklist

```csharp
// src/Acode.Infrastructure/Compute/Ec2/Management/Ec2InstanceManager.cs
namespace Acode.Infrastructure.Compute.Ec2.Management;

public sealed class Ec2InstanceManager : IEc2InstanceManager
{
    private readonly IAmazonEC2 _ec2Client;
    private readonly IEventPublisher _events;
    
    public async Task<InternalInstanceState> GetStateAsync(
        string instanceId,
        CancellationToken ct = default)
    {
        var response = await _ec2Client.DescribeInstancesAsync(
            new DescribeInstancesRequest { InstanceIds = [instanceId] }, ct);
        
        var instance = response.Reservations.SelectMany(r => r.Instances).FirstOrDefault();
        return MapState(instance?.State.Name);
    }
    
    public async Task TerminateAsync(
        string instanceId,
        TerminateOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new TerminateOptions();
        
        var currentState = await GetStateAsync(instanceId, ct);
        if (currentState == InternalInstanceState.Terminated) return;
        
        await _ec2Client.TerminateInstancesAsync(
            new TerminateInstancesRequest { InstanceIds = [instanceId] }, ct);
        
        await WaitForStateAsync(instanceId, InternalInstanceState.Terminated, options.Timeout, ct);
        
        await _events.PublishAsync(new InstanceTerminatedEvent(
            instanceId, TimeSpan.Zero, options.Force, true, DateTimeOffset.UtcNow));
    }
    
    private static InternalInstanceState MapState(InstanceStateName? awsState) => awsState?.Value switch
    {
        "pending" => InternalInstanceState.Preparing,
        "running" => InternalInstanceState.Ready,
        "stopping" or "shutting-down" => InternalInstanceState.Terminating,
        "stopped" => InternalInstanceState.Suspended,
        "terminated" => InternalInstanceState.Terminated,
        _ => InternalInstanceState.Unknown
    };
}

// src/Acode.Infrastructure/Compute/Ec2/Management/Ec2StatePoller.cs
namespace Acode.Infrastructure.Compute.Ec2.Management;

public sealed class Ec2StatePoller : IEc2StatePoller
{
    private readonly IEc2InstanceManager _manager;
    private readonly ConcurrentDictionary<string, Timer> _polling = new();
    
    public event Action<string, InternalInstanceState>? StateChanged;
    
    public void StartPolling(string instanceId, TimeSpan interval)
    {
        var timer = new Timer(async _ =>
        {
            var state = await _manager.GetStateAsync(instanceId);
            StateChanged?.Invoke(instanceId, state);
            if (state == InternalInstanceState.Terminated) StopPolling(instanceId);
        }, null, TimeSpan.Zero, interval);
        
        _polling[instanceId] = timer;
    }
    
    public void StopPolling(string instanceId)
    {
        if (_polling.TryRemove(instanceId, out var timer)) timer.Dispose();
    }
    
    public void Dispose()
    {
        foreach (var timer in _polling.Values) timer.Dispose();
        _polling.Clear();
    }
}
```

### Implementation Checklist

| # | Requirement | Test | Impl |
|---|-------------|------|------|
| 1 | State mapping from AWS to internal | ⬜ | ⬜ |
| 2 | State polling at configurable interval | ⬜ | ⬜ |
| 3 | StateChanged event fires on transitions | ⬜ | ⬜ |
| 4 | Polling stops on terminal state | ⬜ | ⬜ |
| 5 | Stop instance waits for stopped state | ⬜ | ⬜ |
| 6 | Start instance waits for running | ⬜ | ⬜ |
| 7 | Terminate is idempotent | ⬜ | ⬜ |
| 8 | Cleanup resources on terminate | ⬜ | ⬜ |
| 9 | InstanceTerminatedEvent published | ⬜ | ⬜ |
| 10 | Health monitoring works | ⬜ | ⬜ |

### Rollout Plan

1. **Tests first**: Unit tests for state mapping, polling logic
2. **Domain models**: Events, InternalInstanceState, Ec2HealthStatus
3. **Application interfaces**: IEc2InstanceManager, IEc2StatePoller, IEc2HealthMonitor
4. **Infrastructure impl**: Ec2InstanceManager, Ec2StatePoller
5. **Integration tests**: Real stop/start/terminate operations
6. **DI registration**: Register manager as scoped, poller as singleton

**End of Task 031.b Specification**