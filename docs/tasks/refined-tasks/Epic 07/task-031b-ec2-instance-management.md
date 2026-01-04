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

- Task 031.a: Manages provisioned instances
- Task 029.d: Teardown terminates
- Task 033: Heuristics monitor instances

### Failure Modes

- Terminate fails → Retry with force
- Instance stuck → Force terminate
- API rate limit → Exponential backoff
- Orphan detected → Auto-cleanup

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

### FR-001 to FR-020: State Tracking

- FR-001: Instance state MUST be tracked
- FR-002: States from AWS MUST map
- FR-003: pending → Preparing
- FR-004: running → Ready
- FR-005: stopping → Terminating
- FR-006: stopped → Suspended
- FR-007: shutting-down → Terminating
- FR-008: terminated → Terminated
- FR-009: State polling MUST work
- FR-010: Poll interval: 15 seconds
- FR-011: Poll MUST use DescribeInstances
- FR-012: Batch polling MUST work
- FR-013: Poll up to 50 instances per call
- FR-014: State changes MUST notify
- FR-015: Notification via callback
- FR-016: State history MUST be logged
- FR-017: State MUST be queryable
- FR-018: Last state update MUST be tracked
- FR-019: State polling MUST stop on terminal
- FR-020: Terminal states: terminated

### FR-021 to FR-040: Health Monitoring

- FR-021: Health check MUST run
- FR-022: DescribeInstanceStatus MUST be used
- FR-023: System status MUST be checked
- FR-024: Instance status MUST be checked
- FR-025: Impaired status MUST alert
- FR-026: Scheduled events MUST be checked
- FR-027: Reboot event MUST notify
- FR-028: Stop event MUST notify
- FR-029: Retire event MUST notify
- FR-030: Health check interval: 60 seconds
- FR-031: SSH health check MUST complement
- FR-032: SSH check: run test command
- FR-033: Failed health MUST trigger action
- FR-034: Action: notify callback
- FR-035: Action: optional auto-terminate
- FR-036: Health status MUST be queryable
- FR-037: Status: healthy, impaired, unknown
- FR-038: Metrics MUST track health
- FR-039: CloudWatch metrics MUST be fetched
- FR-040: CPU, network, disk metrics

### FR-041 to FR-060: Stop/Start

- FR-041: Stop MUST be available
- FR-042: StopInstances API MUST be called
- FR-043: Stop MUST wait for stopped state
- FR-044: Stop timeout: 2 minutes
- FR-045: Force stop MUST be available
- FR-046: ForceStop parameter MUST be used
- FR-047: Start MUST be available
- FR-048: StartInstances API MUST be called
- FR-049: Start MUST wait for running
- FR-050: Start MUST update public IP
- FR-051: IP may change after start
- FR-052: SSH connection MUST re-establish
- FR-053: Workspace MUST be preserved
- FR-054: EBS data MUST persist
- FR-055: Instance store data MUST NOT persist
- FR-056: Warning for instance store
- FR-057: Reboot MUST be available
- FR-058: RebootInstances API MUST be called
- FR-059: Reboot MUST wait for SSH
- FR-060: Reboot preserves public IP

### FR-061 to FR-080: Termination

- FR-061: Terminate MUST work
- FR-062: TerminateInstances API MUST be called
- FR-063: Wait for terminated state
- FR-064: Terminate timeout: 2 minutes
- FR-065: Already terminated MUST not error
- FR-066: Idempotent termination
- FR-067: EBS volumes MUST be deleted
- FR-068: DeleteOnTermination MUST be true
- FR-069: Security group cleanup MUST work
- FR-070: Temp security group MUST delete
- FR-071: Key pair cleanup MUST work
- FR-072: Temp key pair MUST delete
- FR-073: Elastic IP release MUST work
- FR-074: Associated EIP MUST release
- FR-075: Termination protection MUST be off
- FR-076: DisableApiTermination = false
- FR-077: Force terminate MUST bypass
- FR-078: Terminate MUST log
- FR-079: Terminate MUST emit metrics
- FR-080: Terminate MUST notify callback

---

## Non-Functional Requirements

- NFR-001: Terminate in <30 seconds
- NFR-002: State poll <500ms
- NFR-003: No orphan instances
- NFR-004: 100% cleanup reliability
- NFR-005: Graceful degradation
- NFR-006: API retries with backoff
- NFR-007: Structured logging
- NFR-008: Metrics on lifecycle
- NFR-009: Audit trail
- NFR-010: Idempotent operations

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

- [ ] AC-001: State tracking works
- [ ] AC-002: Health monitoring works
- [ ] AC-003: Stop instance works
- [ ] AC-004: Start instance works
- [ ] AC-005: Terminate works
- [ ] AC-006: Force terminate works
- [ ] AC-007: Cleanup complete
- [ ] AC-008: No orphans after tests
- [ ] AC-009: Metrics emitted
- [ ] AC-010: Idempotent verified

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: State mapping
- [ ] UT-002: Health logic
- [ ] UT-003: Termination sequence
- [ ] UT-004: Cleanup order

### Integration Tests

- [ ] IT-001: Real stop/start
- [ ] IT-002: Real terminate
- [ ] IT-003: Force terminate
- [ ] IT-004: Orphan cleanup

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