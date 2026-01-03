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

### Provisioner

```csharp
public class Ec2Provisioner
{
    private readonly IAmazonEC2 _ec2Client;
    
    public async Task<Ec2InstanceInfo> ProvisionAsync(
        Ec2ProvisionRequest request,
        IProgress<ProvisionProgress> progress = null,
        CancellationToken ct = default);
        
    public async Task<bool> WaitForReadyAsync(
        string instanceId,
        TimeSpan timeout,
        CancellationToken ct = default);
}

public record Ec2ProvisionRequest(
    string AmiId,
    string InstanceType,
    string SubnetId,
    IReadOnlyList<string> SecurityGroupIds,
    string KeyName,
    int EbsSizeGb = 20,
    string UserData = null,
    bool UseSpot = false,
    decimal? SpotMaxPrice = null,
    IReadOnlyDictionary<string, string> Tags = null);

public record ProvisionProgress(
    ProvisionPhase Phase,
    string Message,
    int PercentComplete);

public enum ProvisionPhase
{
    Validating,
    PreparingKeyPair,
    Launching,
    WaitingForRunning,
    WaitingForSsh,
    Ready
}
```

### Key Pair Manager

```csharp
public class Ec2KeyPairManager
{
    public async Task<string> EnsureKeyPairAsync(
        string keyName,
        string privateKeyPath,
        CancellationToken ct);
        
    public async Task DeleteKeyPairAsync(
        string keyName,
        CancellationToken ct);
}
```

---

**End of Task 031.a Specification**