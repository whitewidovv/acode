# Task 031: AWS EC2 Target

**Priority:** P1 – High  
**Tier:** L – Cloud Integration  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 029 (Interface), Task 030 (SSH Target)  

---

## Description

Task 031 implements the AWS EC2 compute target. Instances MUST be provisioned on demand. SSH MUST be used for execution. Instances MUST be terminated after use.

EC2 enables elastic compute scaling. Burst workloads run on cloud instances. Cost is incurred only during use.

This task provides core EC2 integration. Subtasks cover provisioning, instance management, and cost controls.

### Business Value

EC2 targets enable:
- Elastic compute scaling
- Pay-per-use model
- Access to specialized instances
- Geographic distribution

### Scope Boundaries

This task covers EC2 target implementation. SSH execution is in Task 030. Other cloud providers are future work.

### Integration Points

- Task 029: Implements IComputeTarget
- Task 030: Uses SSH for execution
- Task 033: Heuristics trigger EC2

### Mode Compliance

| Mode | EC2 Behavior |
|------|--------------|
| local-only | BLOCKED |
| airgapped | BLOCKED |
| burst | ALLOWED |

MUST validate mode before provisioning. MUST NOT spend money in restricted modes.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| EC2 | Elastic Compute Cloud |
| AMI | Amazon Machine Image |
| Instance Type | Hardware specification |
| Spot | Discounted preemptible |
| On-Demand | Standard pricing |
| VPC | Virtual Private Cloud |
| Security Group | Firewall rules |

---

## Out of Scope

- Azure VM support
- GCP Compute Engine
- Lambda functions
- ECS/EKS containers
- EC2 Mac instances
- Dedicated hosts

---

## Functional Requirements

### FR-001 to FR-020: EC2 Target

- FR-001: `Ec2ComputeTarget` MUST implement interface
- FR-002: AWS credentials MUST be configurable
- FR-003: Credentials from env vars MUST work
- FR-004: Credentials from profile MUST work
- FR-005: Credentials from IAM role MUST work
- FR-006: Region MUST be configurable
- FR-007: Default region from env/profile
- FR-008: Instance type MUST be configurable
- FR-009: Default: t3.medium
- FR-010: AMI MUST be configurable
- FR-011: Default: latest Amazon Linux 2
- FR-012: VPC MUST be configurable
- FR-013: Subnet MUST be configurable
- FR-014: Security group MUST be configurable
- FR-015: Key pair MUST be configurable
- FR-016: Auto key pair MUST be optional
- FR-017: IAM instance profile MUST work
- FR-018: Tags MUST be settable
- FR-019: Default tag: acode=true
- FR-020: User data MUST be supported

### FR-021 to FR-040: Provisioning

- FR-021: `PrepareAsync` MUST provision instance
- FR-022: Instance MUST be created
- FR-023: Instance MUST be waited for running
- FR-024: Instance MUST be waited for SSH
- FR-025: SSH readiness check MUST retry
- FR-026: Max SSH retries: 30
- FR-027: Retry interval: 10 seconds
- FR-028: Public IP MUST be obtained
- FR-029: Elastic IP MUST be optional
- FR-030: Private IP MUST be option
- FR-031: Security group MUST allow SSH
- FR-032: Temp security group MUST be optional
- FR-033: Temp group MUST be cleaned up
- FR-034: Instance store MUST work
- FR-035: EBS volume MUST work
- FR-036: EBS size MUST be configurable
- FR-037: Default EBS: 20GB
- FR-038: EBS cleanup on terminate MUST work
- FR-039: Provisioning timeout MUST exist
- FR-040: Default timeout: 5 minutes

### FR-041 to FR-060: Instance Types

- FR-041: General purpose MUST work (t3, m5)
- FR-042: Compute optimized MUST work (c5)
- FR-043: Memory optimized MUST work (r5)
- FR-044: GPU instances MUST work (g4dn)
- FR-045: Instance family validation MUST exist
- FR-046: Invalid instance type MUST error
- FR-047: Instance type recommendations MUST work
- FR-048: Recommend based on task
- FR-049: Spot instances MUST be optional
- FR-050: Spot price limit MUST work
- FR-051: Spot interruption handler MUST work
- FR-052: Spot fallback to on-demand MUST work
- FR-053: On-demand MUST be default
- FR-054: Reserved instance check MUST work
- FR-055: Savings plan check MUST work
- FR-056: Cost estimate MUST be available
- FR-057: Cost MUST include all components
- FR-058: Hourly rate MUST be shown
- FR-059: Running cost MUST be tracked
- FR-060: Cost alerts MUST be optional

### FR-061 to FR-075: Lifecycle

- FR-061: Instance state MUST be tracked
- FR-062: States: pending, running, stopping, terminated
- FR-063: State polling MUST work
- FR-064: Poll interval: 5 seconds
- FR-065: State callbacks MUST work
- FR-066: Stop MUST work (preserve instance)
- FR-067: Start MUST work (resume stopped)
- FR-068: Terminate MUST work (destroy)
- FR-069: Teardown MUST terminate
- FR-070: Terminate MUST be idempotent
- FR-071: Already terminated MUST not error
- FR-072: Orphan detection MUST work
- FR-073: Orphan: running with acode tag + old
- FR-074: Orphan threshold: 2 hours
- FR-075: Orphan cleanup MUST be safe

---

## Non-Functional Requirements

- NFR-001: Provision in <5 minutes
- NFR-002: SSH ready in <2 minutes after running
- NFR-003: Terminate in <30 seconds
- NFR-004: No orphan instances
- NFR-005: Cost tracking accurate
- NFR-006: Spot handling graceful
- NFR-007: Structured logging
- NFR-008: Metrics on instance lifecycle
- NFR-009: IAM least privilege
- NFR-010: Secrets not logged

---

## User Manual Documentation

### Configuration

```yaml
ec2Target:
  region: us-west-2
  instanceType: t3.medium
  ami: ami-0c55b159cbfafe1f0  # Amazon Linux 2
  subnetId: subnet-12345678
  securityGroupIds:
    - sg-12345678
  keyPairName: acode-key
  instanceProfile: acode-instance-profile
  spotEnabled: false
  spotMaxPrice: "0.05"
  ebsSizeGb: 20
  tags:
    project: my-project
    environment: dev
```

### CLI Usage

```bash
# Add EC2 target
acode target add ec2 \
  --region us-west-2 \
  --instance-type t3.medium \
  --ami ami-0c55b159cbfafe1f0

# Test EC2 provisioning
acode target test ec2 --dry-run

# List running instances
acode target ec2 list

# Terminate orphans
acode target ec2 cleanup
```

### Cost Awareness

| Instance Type | Hourly Rate | Use Case |
|--------------|-------------|----------|
| t3.micro | $0.0104 | Light tasks |
| t3.medium | $0.0416 | Standard |
| c5.large | $0.085 | Compute |
| r5.large | $0.126 | Memory |
| g4dn.xlarge | $0.526 | GPU |

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: EC2 instance provisions
- [ ] AC-002: SSH connection works
- [ ] AC-003: Commands execute
- [ ] AC-004: Files transfer
- [ ] AC-005: Instance terminates
- [ ] AC-006: Spot instances work
- [ ] AC-007: Mode compliance enforced
- [ ] AC-008: Orphan detection works
- [ ] AC-009: Cost tracking works
- [ ] AC-010: Credentials work

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Config parsing
- [ ] UT-002: Mode validation
- [ ] UT-003: Instance type validation
- [ ] UT-004: Cost calculation

### Integration Tests

- [ ] IT-001: Real EC2 provisioning
- [ ] IT-002: Full lifecycle
- [ ] IT-003: Spot instance handling
- [ ] IT-004: Orphan cleanup

---

## Implementation Prompt

### Classes

```csharp
public class Ec2ComputeTarget : IComputeTarget
{
    private readonly IAmazonEC2 _ec2Client;
    private readonly SshComputeTarget _sshTarget;
    private readonly Ec2Configuration _config;
    private string _instanceId;
}

public record Ec2Configuration(
    string Region,
    string InstanceType = "t3.medium",
    string AmiId = null,  // null = latest Amazon Linux 2
    string SubnetId = null,
    IReadOnlyList<string> SecurityGroupIds = null,
    string KeyPairName = null,
    string InstanceProfileArn = null,
    bool SpotEnabled = false,
    decimal? SpotMaxPrice = null,
    int EbsSizeGb = 20,
    IReadOnlyDictionary<string, string> Tags = null);

public record Ec2InstanceInfo(
    string InstanceId,
    string InstanceType,
    string PublicIp,
    string PrivateIp,
    Ec2InstanceState State,
    DateTime LaunchTime,
    decimal HourlyRate);

public enum Ec2InstanceState
{
    Pending,
    Running,
    Stopping,
    Stopped,
    ShuttingDown,
    Terminated
}
```

### Factory

```csharp
public class Ec2ComputeTargetFactory : IComputeTargetFactory<Ec2Configuration>
{
    public string TargetType => "ec2";
    
    public Task<IComputeTarget> CreateAsync(
        Ec2Configuration config,
        CancellationToken ct);
}
```

---

**End of Task 031 Specification**