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

| Component | Interface | Data Flow | Notes |
|-----------|-----------|-----------|-------|
| Task 031 EC2 Target | IEc2Target | Provisioning request in, instance ID out | Parent container |
| Task 030 SSH | ISshConnection | SSH access after provision | Connection layer |
| Task 029.a Workspace | IPrepareWorkspace | Workspace setup after SSH | Preparation |
| AWS SDK EC2 | IAmazonEC2 | All EC2 API calls | Cloud provider |
| AWS SDK STS | IAmazonSTS | Credential validation | Auth |
| Key Pair Manager | IEc2KeyPairManager | Key creation/import | Security |
| AMI Resolver | IEc2AmiResolver | AMI lookup by pattern | Image selection |

### Failure Modes

| Failure | Detection | Recovery | User Impact |
|---------|-----------|----------|-------------|
| Quota exceeded | InsufficientInstanceCapacity error | Report with quota increase link | Cannot provision |
| AMI not found | InvalidAMIID error | Suggest valid AMIs | Configuration issue |
| Subnet full | Capacity error | Try different AZ | AZ failover |
| Key pair missing | InvalidKeyPair.NotFound | Create temp key pair | Auto-recovery |
| Spot not fulfilled | Timeout or insufficient capacity | Fall back to on-demand | Higher cost |
| Instance fails readiness | SSH unreachable after timeout | Terminate and retry | Delayed start |
| UserData script fails | Instance not ready | Report console output | Debugging needed |
| Security group invalid | InvalidGroup.NotFound | Report with details | Config fix needed |

---

## Assumptions

1. **AWS SDK**: Implementation uses AWS SDK for .NET v3
2. **IAM Permissions**: Caller has ec2:RunInstances, ec2:DescribeInstances, ec2:CreateKeyPair permissions
3. **VPC Setup**: Subnets and security groups pre-configured
4. **AMI Availability**: Target AMIs exist and are accessible
5. **SSH Port**: Security group allows port 22 from source
6. **IMDSv2**: All instances use IMDSv2 (token required)
7. **Default VPC**: Falls back to default VPC if not specified
8. **Region Config**: AWS region is configured in environment or config

---

## Security Considerations

1. **Key Material Handling**: Private keys MUST be stored with 600 permissions, deleted after use
2. **IMDSv2 Enforcement**: All instances MUST require IMDSv2 (no fallback to v1)
3. **Tagging Security**: Tags MUST NOT contain sensitive data
4. **UserData Secrets**: UserData MUST NOT contain plaintext secrets (use SSM/Secrets Manager)
5. **Security Group Scope**: Security groups MUST use least-privilege ingress rules
6. **IAM Role Scope**: Instance profiles MUST have minimum required permissions
7. **API Credentials**: AWS credentials MUST NOT be logged
8. **Console Output**: SSH fingerprint verification MUST use GetConsoleOutput for security

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

### RunInstances API

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031A-01 | `RunInstances` API MUST be called via AWS SDK | P0 |
| FR-031A-02 | `ImageId` MUST be specified (resolved or explicit) | P0 |
| FR-031A-03 | `InstanceType` MUST be specified and validated | P0 |
| FR-031A-04 | `MinCount = MaxCount = 1` for single instance launch | P0 |
| FR-031A-05 | `KeyName` MUST be specified for SSH access | P0 |
| FR-031A-06 | `SubnetId` MUST be specified for VPC placement | P0 |
| FR-031A-07 | `SecurityGroupIds` MUST be specified (array) | P0 |
| FR-031A-08 | `IamInstanceProfile` MUST be optional (ARN or name) | P1 |
| FR-031A-09 | `UserData` MUST be optional (bootstrap script) | P1 |
| FR-031A-10 | UserData MUST be base64 encoded before submission | P1 |
| FR-031A-11 | `TagSpecifications` MUST be set for instance and volumes | P0 |
| FR-031A-12 | Required tag: `acode=true` for identification | P0 |
| FR-031A-13 | Required tag: `acode-session-id={sessionId}` | P0 |
| FR-031A-14 | Required tag: `acode-timestamp={ISO8601}` | P0 |
| FR-031A-15 | Custom tags MUST merge with required tags | P1 |
| FR-031A-16 | `BlockDeviceMappings` MUST be set for root volume | P0 |
| FR-031A-17 | EBS volume size MUST be configurable (default 30GB) | P1 |
| FR-031A-18 | EBS volume type MUST be gp3 | P0 |
| FR-031A-19 | `DeleteOnTermination = true` MUST be set | P0 |
| FR-031A-20 | IMDSv2 MUST be enforced via `MetadataOptions.HttpTokens=required` | P0 |

### Spot Instances

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031A-21 | Spot instance request MUST be optional via config | P1 |
| FR-031A-22 | `InstanceMarketOptions` MUST be set for spot | P1 |
| FR-031A-23 | `SpotInstanceType = one-time` for immediate launch | P1 |
| FR-031A-24 | `MaxPrice` MUST be configurable ($/hour) | P1 |
| FR-031A-25 | Default MaxPrice = on-demand price (no cap) | P1 |
| FR-031A-26 | Spot request timeout MUST exist (default 5 min) | P1 |
| FR-031A-27 | Spot fulfillment MUST be waited with polling | P1 |
| FR-031A-28 | Spot failure MUST trigger fallback evaluation | P1 |
| FR-031A-29 | Fallback to on-demand MUST be optional (default true) | P1 |
| FR-031A-30 | Spot interruption 2-minute warning MUST be handled | P2 |
| FR-031A-31 | Interruption callback MUST trigger graceful shutdown | P2 |
| FR-031A-32 | Spot savings MUST be logged (vs on-demand price) | P2 |
| FR-031A-33 | `SpotPlacementScores` API MUST inform AZ selection | P2 |
| FR-031A-34 | Best AZ MUST be selected based on spot availability | P2 |
| FR-031A-35 | Spot price history MUST be queryable for decisions | P2 |

### Instance Readiness

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031A-36 | Instance state MUST be polled via `DescribeInstances` | P0 |
| FR-031A-37 | Wait for `running` state before SSH check | P0 |
| FR-031A-38 | Poll interval MUST be 5 seconds | P0 |
| FR-031A-39 | Max poll time MUST be 5 minutes for state check | P0 |
| FR-031A-40 | `DescribeInstanceStatus` MUST verify health | P0 |
| FR-031A-41 | System status MUST be `ok` before ready | P0 |
| FR-031A-42 | Instance status MUST be `ok` before ready | P0 |
| FR-031A-43 | Public IP MUST be obtained from instance metadata | P0 |
| FR-031A-44 | SSH port 22 MUST be reachable before ready | P0 |
| FR-031A-45 | SSH check MUST retry with backoff | P0 |
| FR-031A-46 | Max SSH retries MUST be 30 | P0 |
| FR-031A-47 | SSH retry interval MUST be 10 seconds | P0 |
| FR-031A-48 | SSH fingerprint MUST be verified from console output | P1 |
| FR-031A-49 | `GetConsoleOutput` API MUST be used for fingerprint | P1 |
| FR-031A-50 | Readiness callback MUST be invoked on success | P1 |
| FR-031A-51 | Readiness timeout MUST trigger cleanup | P0 |
| FR-031A-52 | Cleanup MUST terminate instance on timeout | P0 |

### Key Pair Management

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031A-53 | Existing key pair MUST work via `KeyName` config | P0 |
| FR-031A-54 | Key pair name MUST be resolved from config | P0 |
| FR-031A-55 | Auto key pair creation MUST be optional | P1 |
| FR-031A-56 | Auto mode: create temp key pair | P1 |
| FR-031A-57 | `CreateKeyPair` API MUST be used for temp keys | P1 |
| FR-031A-58 | Key material MUST be stored securely (memory only) | P0 |
| FR-031A-59 | Key MUST be written to temp file with 600 permissions | P0 |
| FR-031A-60 | Key file path MUST be tracked for cleanup | P0 |
| FR-031A-61 | Temp key MUST be deleted on teardown | P0 |
| FR-031A-62 | `DeleteKeyPair` API MUST be used for cleanup | P1 |
| FR-031A-63 | Key naming: `acode-{session-id}` format | P1 |
| FR-031A-64 | Import existing public key MUST work | P2 |
| FR-031A-65 | `ImportKeyPair` API MUST be used for import | P2 |
| FR-031A-66 | ED25519 and RSA key types MUST be supported | P1 |

---

## Non-Functional Requirements

### Performance Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-031A-01 | Total provisioning time (launch to SSH ready) | <3 minutes | P0 |
| NFR-031A-02 | SSH readiness after running state | <2 minutes | P0 |
| NFR-031A-03 | RunInstances API call latency | <5 seconds | P1 |
| NFR-031A-04 | State polling efficiency | Minimal API calls | P1 |
| NFR-031A-05 | Spot request fulfillment timeout | 5 minutes default | P1 |
| NFR-031A-06 | Key pair creation time | <2 seconds | P1 |
| NFR-031A-07 | Console output retrieval time | <10 seconds | P2 |
| NFR-031A-08 | AZ selection decision time | <5 seconds | P2 |
| NFR-031A-09 | Cleanup on failure | <30 seconds | P0 |
| NFR-031A-10 | Parallel provisioning support | 5 instances | P2 |

### Reliability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-031A-11 | API retries with exponential backoff | 3 retries | P0 |
| NFR-031A-12 | No orphan instances on failure | 100% cleanup | P0 |
| NFR-031A-13 | Idempotent on retry (same session ID) | Detect existing | P1 |
| NFR-031A-14 | Spot fallback success rate | 100% when enabled | P0 |
| NFR-031A-15 | SSH fingerprint verification accuracy | 100% match | P1 |
| NFR-031A-16 | Key pair cleanup on teardown | 100% deleted | P0 |
| NFR-031A-17 | Graceful handling of quota errors | Clear error message | P0 |
| NFR-031A-18 | Recovery from transient AWS errors | Auto-retry | P0 |
| NFR-031A-19 | Instance tagging consistency | All required tags | P0 |
| NFR-031A-20 | EBS volume configuration accuracy | Exact match to config | P0 |

### Observability Requirements

| ID | Requirement | Target | Priority |
|----|-------------|--------|----------|
| NFR-031A-21 | Structured logging for all phases | JSON with correlation ID | P0 |
| NFR-031A-22 | Secrets not logged (keys, credentials) | Zero exposure | P0 |
| NFR-031A-23 | Key material secured in memory | Encrypted or zeroed | P0 |
| NFR-031A-24 | Metrics for provisioning duration | Histogram | P1 |
| NFR-031A-25 | Metrics for spot vs on-demand usage | Counter | P1 |
| NFR-031A-26 | Metrics for failure reasons | Counter by type | P1 |
| NFR-031A-27 | IAM least privilege documentation | Documented permissions | P0 |
| NFR-031A-28 | Cost tracking per instance | Logged on termination | P2 |
| NFR-031A-29 | Phase progress reporting | Event-based | P1 |
| NFR-031A-30 | AWS API call tracing | X-Ray compatible | P2 |

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

### RunInstances API
- [ ] AC-001: `RunInstances` successfully launches instance
- [ ] AC-002: ImageId is correctly specified
- [ ] AC-003: InstanceType matches configuration
- [ ] AC-004: MinCount = MaxCount = 1
- [ ] AC-005: KeyName is attached to instance
- [ ] AC-006: SubnetId places instance correctly
- [ ] AC-007: SecurityGroupIds are applied
- [ ] AC-008: IamInstanceProfile attached when specified
- [ ] AC-009: UserData executed on boot
- [ ] AC-010: UserData is base64 encoded

### Tagging and Configuration
- [ ] AC-011: `acode=true` tag present
- [ ] AC-012: `acode-session-id` tag present
- [ ] AC-013: `acode-timestamp` tag present
- [ ] AC-014: Custom tags merged correctly
- [ ] AC-015: EBS root volume created with gp3
- [ ] AC-016: EBS volume size matches config
- [ ] AC-017: DeleteOnTermination = true
- [ ] AC-018: IMDSv2 enforced (HttpTokens=required)

### Spot Instances
- [ ] AC-019: Spot instance launches when configured
- [ ] AC-020: MaxPrice respected in spot request
- [ ] AC-021: Spot timeout triggers fallback
- [ ] AC-022: Fallback to on-demand works
- [ ] AC-023: Spot interruption warning handled
- [ ] AC-024: Spot savings logged
- [ ] AC-025: SpotPlacementScores informs AZ selection

### Instance Readiness
- [ ] AC-026: Instance state polled correctly
- [ ] AC-027: Running state detected
- [ ] AC-028: System status verified as ok
- [ ] AC-029: Instance status verified as ok
- [ ] AC-030: Public IP obtained
- [ ] AC-031: SSH port 22 reachable
- [ ] AC-032: SSH retries with backoff
- [ ] AC-033: SSH fingerprint verified from console
- [ ] AC-034: Readiness callback invoked
- [ ] AC-035: Timeout cleanup terminates instance

### Key Pair Management
- [ ] AC-036: Existing key pair works
- [ ] AC-037: Auto key pair created
- [ ] AC-038: CreateKeyPair API used correctly
- [ ] AC-039: Key material stored with 600 permissions
- [ ] AC-040: Key file path tracked
- [ ] AC-041: Temp key deleted on teardown
- [ ] AC-042: DeleteKeyPair API called
- [ ] AC-043: Key naming follows pattern
- [ ] AC-044: ImportKeyPair works
- [ ] AC-045: ED25519 keys supported
- [ ] AC-046: RSA keys supported

### Error Handling and Cleanup
- [ ] AC-047: Quota exceeded error handled gracefully
- [ ] AC-048: AMI not found error reported
- [ ] AC-049: Invalid security group error reported
- [ ] AC-050: No orphan instances on any failure
- [ ] AC-051: API retries on transient errors
- [ ] AC-052: Idempotent on retry (same session)
- [ ] AC-053: All credentials excluded from logs
- [ ] AC-054: Phase progress reported via events

---

## User Verification Scenarios

### Scenario 1: Developer Provisions EC2 for Build
**Persona:** Developer needing cloud compute  
**Preconditions:** AWS credentials configured, VPC/subnet exist  
**Steps:**
1. Configure EC2 target with t3.medium
2. Trigger provisioning
3. Wait for SSH ready
4. Execute build commands

**Verification Checklist:**
- [ ] Instance launches within 30 seconds
- [ ] Running state achieved
- [ ] SSH accessible within 3 minutes
- [ ] All required tags present
- [ ] UserData executed if specified

### Scenario 2: Spot Instance with Fallback
**Persona:** Cost-conscious developer  
**Preconditions:** Spot capacity limited in region  
**Steps:**
1. Configure spot instance with $0.01 max
2. Observe spot request timeout
3. Verify fallback to on-demand
4. Check savings logging

**Verification Checklist:**
- [ ] Spot request submitted
- [ ] Timeout detected after 5 minutes
- [ ] Fallback to on-demand triggered
- [ ] Instance launches successfully
- [ ] Fallback event logged

### Scenario 3: Auto Key Pair Creation
**Persona:** New user without existing keys  
**Preconditions:** No key pairs in AWS account  
**Steps:**
1. Enable auto key pair creation
2. Provision instance
3. Verify SSH works
4. Teardown and verify cleanup

**Verification Checklist:**
- [ ] Key pair created with acode-{session} name
- [ ] Key file written with 600 permissions
- [ ] SSH connects successfully
- [ ] Key pair deleted on teardown
- [ ] Key file deleted locally

### Scenario 4: Provisioning Failure Cleanup
**Persona:** Developer hitting quota  
**Preconditions:** Instance quota exceeded  
**Steps:**
1. Attempt provisioning
2. Observe quota error
3. Verify no orphan resources
4. Check error message quality

**Verification Checklist:**
- [ ] InsufficientInstanceCapacity error detected
- [ ] No instance left running
- [ ] No key pairs orphaned
- [ ] Error message includes quota increase link
- [ ] Cleanup completed within 30 seconds

### Scenario 5: UserData Bootstrap Script
**Persona:** DevOps setting up custom image  
**Preconditions:** Instance with Docker required  
**Steps:**
1. Configure UserData with docker install
2. Provision instance
3. Wait for SSH ready
4. Verify docker running

**Verification Checklist:**
- [ ] UserData base64 encoded correctly
- [ ] Instance boots with UserData
- [ ] Script executes on first boot
- [ ] Docker installed and running
- [ ] Console output shows script

### Scenario 6: SSH Fingerprint Verification
**Persona:** Security-conscious developer  
**Preconditions:** First connection to new instance  
**Steps:**
1. Provision instance
2. Retrieve console output
3. Extract SSH fingerprint
4. Verify fingerprint on connect

**Verification Checklist:**
- [ ] GetConsoleOutput API called
- [ ] SSH fingerprint extracted
- [ ] Fingerprint matches on connect
- [ ] Warning if fingerprint mismatch
- [ ] Connection established securely

---

## Testing Requirements

### Unit Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| UT-031A-01 | RunInstances request building | FR-031A-01 |
| UT-031A-02 | Tag generation with required tags | FR-031A-11-15 |
| UT-031A-03 | Spot instance options configuration | FR-031A-21-24 |
| UT-031A-04 | Readiness polling logic | FR-031A-36-42 |
| UT-031A-05 | SSH retry backoff calculation | FR-031A-45-47 |
| UT-031A-06 | Console output fingerprint extraction | FR-031A-48-49 |
| UT-031A-07 | UserData base64 encoding | FR-031A-10 |
| UT-031A-08 | Key pair naming pattern | FR-031A-63 |
| UT-031A-09 | EBS volume configuration | FR-031A-16-19 |
| UT-031A-10 | IMDSv2 metadata options | FR-031A-20 |
| UT-031A-11 | Spot fallback decision logic | FR-031A-28-29 |
| UT-031A-12 | Timeout cleanup trigger | FR-031A-51-52 |
| UT-031A-13 | Error message formatting | NFR-031A-17 |
| UT-031A-14 | API retry policy | NFR-031A-11 |
| UT-031A-15 | Idempotency detection | NFR-031A-13 |

### Integration Tests

| ID | Test Case | Validates |
|----|-----------|-----------|
| IT-031A-01 | Real EC2 instance launch | E2E provisioning |
| IT-031A-02 | Spot instance launch | FR-031A-21 |
| IT-031A-03 | Spot fallback to on-demand | FR-031A-29 |
| IT-031A-04 | Auto key pair creation | FR-031A-55-62 |
| IT-031A-05 | UserData execution | FR-031A-09-10 |
| IT-031A-06 | SSH fingerprint verification | FR-031A-48-49 |
| IT-031A-07 | Provisioning timeout cleanup | FR-031A-51-52 |
| IT-031A-08 | Quota exceeded handling | NFR-031A-17 |
| IT-031A-09 | Total provisioning time | NFR-031A-01 |
| IT-031A-10 | Orphan prevention on failure | NFR-031A-12 |
| IT-031A-11 | Multiple AZ failover | FR-031A-33-34 |
| IT-031A-12 | Key pair cleanup on teardown | FR-031A-61-62 |
| IT-031A-13 | All required tags present | FR-031A-11-14 |
| IT-031A-14 | IAM instance profile attachment | FR-031A-08 |
| IT-031A-15 | Security group application | FR-031A-07 |

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