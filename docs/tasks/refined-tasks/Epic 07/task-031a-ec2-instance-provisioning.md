# Task 031.a: EC2 Instance Provisioning

**Priority:** P1 – High  
**Tier:** L – Cloud Integration  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 031 (EC2 Target)  

---

## Description

Task 031.a implements EC2 instance provisioning. Instances MUST be launched via AWS API. Configuration MUST be validated. Readiness MUST be verified.

Provisioning is the prepare phase for EC2. After provisioning, the instance MUST be running and accessible via SSH.

This task covers the AWS API interactions. SSH setup follows in Task 030. Instance management is in Task 031.b.

### Business Value

Automated provisioning enables:
- On-demand compute
- Consistent configuration
- Reproducible environments
- Infrastructure as code

### Scope Boundaries

This task covers EC2 instance creation. SSH is delegated to Task 030. Termination is in 031.b.

### Integration Points

- Task 031: Part of EC2 target
- Task 030: SSH connection after provision
- Task 029.a: Workspace preparation

### Failure Modes

- Quota exceeded → Error with guidance
- AMI not found → Error with suggestion
- Subnet full → Try different AZ
- Key pair missing → Create temp or error

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| RunInstances | EC2 launch API |
| DescribeInstances | Status check API |
| UserData | Bootstrap script |
| IMDSv2 | Instance metadata v2 |
| Launch Template | Reusable config |
| Capacity | Available instance slots |

---

## Out of Scope

- Launch templates (future)
- Placement groups
- Dedicated hosts
- Fleet provisioning
- Auto Scaling groups
- Hibernation

---

## Functional Requirements

### FR-001 to FR-020: RunInstances

- FR-001: `RunInstances` MUST be called
- FR-002: ImageId MUST be specified
- FR-003: InstanceType MUST be specified
- FR-004: MinCount = MaxCount = 1
- FR-005: KeyName MUST be specified
- FR-006: SubnetId MUST be specified
- FR-007: SecurityGroupIds MUST be specified
- FR-008: IamInstanceProfile MUST be optional
- FR-009: UserData MUST be optional
- FR-010: UserData MUST be base64 encoded
- FR-011: TagSpecifications MUST be set
- FR-012: Required tag: acode=true
- FR-013: Required tag: acode-session-id
- FR-014: Required tag: acode-timestamp
- FR-015: Custom tags MUST merge
- FR-016: BlockDeviceMappings MUST be set
- FR-017: EBS volume size configurable
- FR-018: EBS volume type: gp3
- FR-019: DeleteOnTermination = true
- FR-020: IMDSv2 MUST be enforced

### FR-021 to FR-040: Spot Instances

- FR-021: Spot request MUST be optional
- FR-022: InstanceMarketOptions MUST be set
- FR-023: SpotInstanceType = one-time
- FR-024: MaxPrice MUST be configurable
- FR-025: Default: on-demand price
- FR-026: Spot request timeout MUST exist
- FR-027: Default timeout: 5 minutes
- FR-028: Spot fulfillment MUST be waited
- FR-029: Spot failure MUST fallback
- FR-030: Fallback to on-demand MUST be optional
- FR-031: Default: fallback enabled
- FR-032: Spot interruption notice MUST be handled
- FR-033: 2-minute warning via metadata
- FR-034: Interruption callback MUST exist
- FR-035: Graceful shutdown on interruption
- FR-036: Spot savings MUST be logged
- FR-037: Spot availability MUST be checked
- FR-038: SpotPlacementScores API MUST be used
- FR-039: Best AZ MUST be selected
- FR-040: Spot history MUST inform decisions

### FR-041 to FR-060: Instance Readiness

- FR-041: Instance state MUST be polled
- FR-042: Wait for running state
- FR-043: Poll interval: 5 seconds
- FR-044: Max poll time: 5 minutes
- FR-045: DescribeInstances MUST be used
- FR-046: InstanceStatuses MUST be checked
- FR-047: System status MUST be ok
- FR-048: Instance status MUST be ok
- FR-049: Public IP MUST be obtained
- FR-050: SSH port MUST be reachable
- FR-051: SSH check MUST retry
- FR-052: Max SSH retries: 30
- FR-053: Retry interval: 10 seconds
- FR-054: SSH fingerprint MUST be verified
- FR-055: Fingerprint from console output
- FR-056: GetConsoleOutput API MUST be used
- FR-057: Readiness callback MUST exist
- FR-058: Readiness timeout MUST exist
- FR-059: Timeout MUST cleanup instance
- FR-060: Cleanup MUST terminate instance

### FR-061 to FR-075: Key Pair Management

- FR-061: Existing key pair MUST work
- FR-062: Key pair name from config
- FR-063: Auto key pair MUST be optional
- FR-064: Auto: create temp key pair
- FR-065: CreateKeyPair API MUST be used
- FR-066: Key material MUST be stored securely
- FR-067: Key MUST be written to temp file
- FR-068: File permissions: 600
- FR-069: Key path MUST be tracked
- FR-070: Temp key MUST be deleted on teardown
- FR-071: DeleteKeyPair API MUST be used
- FR-072: Key naming: acode-{session-id}
- FR-073: Import existing key MUST work
- FR-074: ImportKeyPair API MUST be used
- FR-075: ED25519 and RSA MUST be supported

---

## Non-Functional Requirements

- NFR-001: Provision in <3 minutes
- NFR-002: SSH ready in <2 minutes after running
- NFR-003: API retries with backoff
- NFR-004: No orphan instances on failure
- NFR-005: Secrets not logged
- NFR-006: Key material secured
- NFR-007: Structured logging
- NFR-008: Metrics on provision time
- NFR-009: IAM least privilege
- NFR-010: Idempotent on retry

---

## User Manual Documentation

### Provisioning Phases

| Phase | Description |
|-------|-------------|
| Validate | Check config and quotas |
| KeyPair | Setup or create key |
| Launch | Call RunInstances |
| Wait | Poll for running state |
| Ready | Verify SSH connectivity |

### UserData Example

```yaml
ec2Target:
  userData: |
    #!/bin/bash
    yum update -y
    yum install -y docker
    systemctl start docker
```

### AMI Selection

| OS | AMI Pattern |
|----|-------------|
| Amazon Linux 2 | amzn2-ami-hvm-* |
| Ubuntu 22.04 | ubuntu/images/hvm-ssd/ubuntu-jammy-22.04-* |
| Debian 11 | debian-11-amd64-* |

### Error Messages

| Error | Resolution |
|-------|------------|
| InsufficientInstanceCapacity | Try different AZ |
| InstanceLimitExceeded | Request quota increase |
| InvalidKeyPair.NotFound | Create or import key |
| InvalidGroup.NotFound | Check security group |

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: RunInstances works
- [ ] AC-002: Tags are set
- [ ] AC-003: EBS configured
- [ ] AC-004: Instance reaches running
- [ ] AC-005: SSH is reachable
- [ ] AC-006: Spot instances work
- [ ] AC-007: Spot fallback works
- [ ] AC-008: Auto key pair works
- [ ] AC-009: Timeout cleanup works
- [ ] AC-010: UserData works

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Request building
- [ ] UT-002: Tag generation
- [ ] UT-003: Spot config
- [ ] UT-004: Readiness logic

### Integration Tests

- [ ] IT-001: Real EC2 launch
- [ ] IT-002: Spot instance launch
- [ ] IT-003: Auto key pair
- [ ] IT-004: UserData execution

---

## Implementation Prompt

### Part 1: File Structure and Domain Models

**Target Directory Structure:**
```
src/
├── Acode.Domain/
│   └── Compute/
│       └── Ec2/
│           └── Provisioning/
│               ├── ProvisionPhase.cs
│               ├── ProvisionProgress.cs
│               ├── SpotRequestStatus.cs
│               └── Events/
│                   ├── ProvisionStartedEvent.cs
│                   ├── ProvisionPhaseChangedEvent.cs
│                   ├── SpotFallbackEvent.cs
│                   └── ProvisionCompletedEvent.cs
├── Acode.Application/
│   └── Compute/
│       └── Ec2/
│           └── Provisioning/
│               ├── Ec2ProvisionRequest.cs
│               ├── IEc2Provisioner.cs
│               ├── IEc2KeyPairManager.cs
│               ├── IEc2AmiResolver.cs
│               └── ISshReadinessChecker.cs
└── Acode.Infrastructure/
    └── Compute/
        └── Ec2/
            └── Provisioning/
                ├── Ec2Provisioner.cs
                ├── Ec2KeyPairManager.cs
                ├── Ec2AmiResolver.cs
                ├── SshReadinessChecker.cs
                ├── SpotInstanceHandler.cs
                └── ConsoleOutputParser.cs
```

**Domain Models:**

```csharp
// src/Acode.Domain/Compute/Ec2/Provisioning/ProvisionPhase.cs
namespace Acode.Domain.Compute.Ec2.Provisioning;

public enum ProvisionPhase
{
    Validating,
    PreparingKeyPair,
    RequestingSpot,
    Launching,
    WaitingForRunning,
    WaitingForSsh,
    Ready,
    Failed
}

// src/Acode.Domain/Compute/Ec2/Provisioning/ProvisionProgress.cs
namespace Acode.Domain.Compute.Ec2.Provisioning;

public sealed record ProvisionProgress
{
    public required ProvisionPhase Phase { get; init; }
    public required string Message { get; init; }
    public int PercentComplete { get; init; }
    public string? InstanceId { get; init; }
    public TimeSpan Elapsed { get; init; }
}

// src/Acode.Domain/Compute/Ec2/Provisioning/SpotRequestStatus.cs
namespace Acode.Domain.Compute.Ec2.Provisioning;

public sealed record SpotRequestStatus
{
    public required string RequestId { get; init; }
    public required string State { get; init; }
    public string? InstanceId { get; init; }
    public string? StatusCode { get; init; }
    public string? StatusMessage { get; init; }
    public decimal? SpotPrice { get; init; }
    public bool IsFulfilled => State == "active" && InstanceId != null;
}

// src/Acode.Domain/Compute/Ec2/Provisioning/Events/ProvisionStartedEvent.cs
namespace Acode.Domain.Compute.Ec2.Provisioning.Events;

public sealed record ProvisionStartedEvent(
    string SessionId,
    string InstanceType,
    string Region,
    bool UseSpot,
    DateTimeOffset StartedAt);

// src/Acode.Domain/Compute/Ec2/Provisioning/Events/SpotFallbackEvent.cs
namespace Acode.Domain.Compute.Ec2.Provisioning.Events;

public sealed record SpotFallbackEvent(
    string SessionId,
    string SpotRequestId,
    string FailureReason,
    DateTimeOffset FallbackAt);

// src/Acode.Domain/Compute/Ec2/Provisioning/Events/ProvisionCompletedEvent.cs
namespace Acode.Domain.Compute.Ec2.Provisioning.Events;

public sealed record ProvisionCompletedEvent(
    string SessionId,
    string InstanceId,
    string PublicIp,
    TimeSpan TotalDuration,
    bool WasSpot,
    DateTimeOffset CompletedAt);
```

**End of Task 031.a Specification - Part 1/3**

### Part 2: Application Interfaces

```csharp
// src/Acode.Application/Compute/Ec2/Provisioning/Ec2ProvisionRequest.cs
namespace Acode.Application.Compute.Ec2.Provisioning;

public sealed record Ec2ProvisionRequest
{
    public required string AmiId { get; init; }
    public required string InstanceType { get; init; }
    public required string SubnetId { get; init; }
    public required IReadOnlyList<string> SecurityGroupIds { get; init; }
    public required string KeyName { get; init; }
    public int EbsSizeGb { get; init; } = 20;
    public string? UserData { get; init; }
    public bool UseSpot { get; init; } = false;
    public decimal? SpotMaxPrice { get; init; }
    public bool SpotFallbackToOnDemand { get; init; } = true;
    public TimeSpan SpotRequestTimeout { get; init; } = TimeSpan.FromMinutes(5);
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
    public bool EnforceImdsV2 { get; init; } = true;
    public string? IamInstanceProfileArn { get; init; }
}

// src/Acode.Application/Compute/Ec2/Provisioning/IEc2Provisioner.cs
namespace Acode.Application.Compute.Ec2.Provisioning;

public interface IEc2Provisioner
{
    Task<Ec2InstanceInfo> ProvisionAsync(
        Ec2ProvisionRequest request,
        IProgress<ProvisionProgress>? progress = null,
        CancellationToken ct = default);
    
    Task<bool> WaitForReadyAsync(
        string instanceId,
        string host,
        TimeSpan timeout,
        CancellationToken ct = default);
    
    Task CleanupFailedProvisionAsync(
        string? instanceId,
        string? keyPairName,
        CancellationToken ct = default);
}

// src/Acode.Application/Compute/Ec2/Provisioning/IEc2KeyPairManager.cs
namespace Acode.Application.Compute.Ec2.Provisioning;

public interface IEc2KeyPairManager
{
    Task<KeyPairInfo> EnsureKeyPairAsync(
        string keyName,
        string privateKeyPath,
        CancellationToken ct = default);
    
    Task<KeyPairInfo> CreateTempKeyPairAsync(
        string sessionId,
        CancellationToken ct = default);
    
    Task DeleteKeyPairAsync(
        string keyName,
        CancellationToken ct = default);
    
    Task<string> ImportKeyPairAsync(
        string keyName,
        string publicKeyMaterial,
        CancellationToken ct = default);
}

public sealed record KeyPairInfo(
    string KeyName,
    string KeyFingerprint,
    string PrivateKeyPath,
    bool IsTemporary);

// src/Acode.Application/Compute/Ec2/Provisioning/IEc2AmiResolver.cs
namespace Acode.Application.Compute.Ec2.Provisioning;

public interface IEc2AmiResolver
{
    Task<string> ResolveAmiAsync(
        string? amiId,
        string region,
        CancellationToken ct = default);
    
    Task<string> GetLatestAmazonLinux2Async(
        string region,
        CancellationToken ct = default);
    
    Task<string> GetLatestUbuntuAsync(
        string region,
        string version = "22.04",
        CancellationToken ct = default);
    
    Task<bool> ValidateAmiAsync(
        string amiId,
        string region,
        CancellationToken ct = default);
}

// src/Acode.Application/Compute/Ec2/Provisioning/ISshReadinessChecker.cs
namespace Acode.Application.Compute.Ec2.Provisioning;

public interface ISshReadinessChecker
{
    Task<SshReadinessResult> CheckAsync(
        string host,
        int port,
        TimeSpan timeout,
        CancellationToken ct = default);
    
    Task<SshReadinessResult> WaitForReadyAsync(
        string host,
        int port,
        int maxRetries,
        TimeSpan retryInterval,
        CancellationToken ct = default);
    
    Task<string?> GetHostFingerprintAsync(
        string instanceId,
        CancellationToken ct = default);
}

public sealed record SshReadinessResult(
    bool IsReady,
    int AttemptsUsed,
    TimeSpan TotalWaitTime,
    string? ErrorMessage);
```

**End of Task 031.a Specification - Part 2/3**

### Part 3: Infrastructure Implementation and Checklist

```csharp
// src/Acode.Infrastructure/Compute/Ec2/Provisioning/Ec2Provisioner.cs
namespace Acode.Infrastructure.Compute.Ec2.Provisioning;

public sealed class Ec2Provisioner : IEc2Provisioner
{
    private readonly IAmazonEC2 _ec2Client;
    private readonly IEc2KeyPairManager _keyPairManager;
    private readonly ISshReadinessChecker _sshChecker;
    private readonly IEventPublisher _events;
    private readonly ILogger<Ec2Provisioner> _logger;
    
    public async Task<Ec2InstanceInfo> ProvisionAsync(
        Ec2ProvisionRequest request,
        IProgress<ProvisionProgress>? progress = null,
        CancellationToken ct = default)
    {
        var sessionId = Ulid.NewUlid().ToString();
        
        progress?.Report(new ProvisionProgress
        {
            Phase = ProvisionPhase.Launching,
            Message = "Launching instance",
            PercentComplete = 30
        });
        
        var runRequest = BuildRunInstancesRequest(request, sessionId);
        var response = await _ec2Client.RunInstancesAsync(runRequest, ct);
        var instanceId = response.Reservation.Instances[0].InstanceId;
        
        progress?.Report(new ProvisionProgress
        {
            Phase = ProvisionPhase.WaitingForRunning,
            Message = $"Waiting for {instanceId} to be running",
            PercentComplete = 50,
            InstanceId = instanceId
        });
        
        await WaitForInstanceStateAsync(instanceId, "running", TimeSpan.FromMinutes(5), ct);
        
        var instance = await GetInstanceAsync(instanceId, ct);
        var host = instance.PublicIpAddress ?? instance.PrivateIpAddress;
        
        await _sshChecker.WaitForReadyAsync(host, 22, 30, TimeSpan.FromSeconds(10), ct);
        
        return MapToInstanceInfo(instance, request.UseSpot);
    }
}

// src/Acode.Infrastructure/Compute/Ec2/Provisioning/Ec2KeyPairManager.cs
namespace Acode.Infrastructure.Compute.Ec2.Provisioning;

public sealed class Ec2KeyPairManager : IEc2KeyPairManager
{
    private readonly IAmazonEC2 _ec2Client;
    
    public async Task<KeyPairInfo> CreateTempKeyPairAsync(string sessionId, CancellationToken ct)
    {
        var keyName = $"acode-{sessionId}";
        var response = await _ec2Client.CreateKeyPairAsync(
            new CreateKeyPairRequest { KeyName = keyName }, ct);
        
        var tempPath = Path.Combine(Path.GetTempPath(), $"{keyName}.pem");
        await File.WriteAllTextAsync(tempPath, response.KeyMaterial, ct);
        
        return new KeyPairInfo(keyName, response.KeyFingerprint, tempPath, IsTemporary: true);
    }
    
    public async Task DeleteKeyPairAsync(string keyName, CancellationToken ct)
    {
        await _ec2Client.DeleteKeyPairAsync(new DeleteKeyPairRequest { KeyName = keyName }, ct);
    }
}
```

### Implementation Checklist

| # | Requirement | Test | Impl |
|---|-------------|------|------|
| 1 | RunInstances called with correct params | ⬜ | ⬜ |
| 2 | Required tags applied (acode, session-id) | ⬜ | ⬜ |
| 3 | EBS volume configured (gp3) | ⬜ | ⬜ |
| 4 | IMDSv2 enforced | ⬜ | ⬜ |
| 5 | UserData base64 encoded | ⬜ | ⬜ |
| 6 | Spot request with fallback works | ⬜ | ⬜ |
| 7 | Wait for running state | ⬜ | ⬜ |
| 8 | SSH readiness check (30 retries) | ⬜ | ⬜ |
| 9 | Public IP obtained | ⬜ | ⬜ |
| 10 | Temp key pair created | ⬜ | ⬜ |
| 11 | Failed provision cleaned up | ⬜ | ⬜ |
| 12 | Progress events published | ⬜ | ⬜ |

### Rollout Plan

1. **Tests first**: Unit tests for request building, tag generation
2. **Domain models**: Events, ProvisionPhase, ProvisionProgress
3. **Application interfaces**: IEc2Provisioner, IEc2KeyPairManager, ISshReadinessChecker
4. **Infrastructure impl**: Ec2Provisioner, Ec2KeyPairManager, SshReadinessChecker
5. **Integration tests**: Real EC2 provisioning
6. **DI registration**: Register provisioner as scoped

**End of Task 031.a Specification**