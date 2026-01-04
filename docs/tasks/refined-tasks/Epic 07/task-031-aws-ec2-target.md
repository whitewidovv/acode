# Task 031: AWS EC2 Target

**Priority:** P1 – High  
**Tier:** L – Cloud Integration  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 029 (IComputeTarget Interface), Task 030 (SSH Target)  

---

## Description

### Overview

Task 031 implements the AWS EC2 compute target, enabling the Agentic Coding Bot to dynamically provision and utilize cloud compute resources for workload execution. When burst mode is active and local resources are insufficient, the system MUST provision EC2 instances on-demand, execute commands via SSH (leveraging Task 030's SSH target), and MUST terminate instances after use to avoid ongoing costs. This provides elastic scaling that lets the agent handle computationally intensive tasks without being constrained by local hardware limitations.

The EC2 target implements the `IComputeTarget` interface from Task 029, providing seamless interoperability with the placement engine. All execution is delegated to an underlying SSH target—EC2 provisioning handles the infrastructure lifecycle while SSH handles the actual command execution, file transfers, and workspace management. This separation of concerns enables clean architecture and reuse of existing SSH infrastructure.

### Business Value

1. **Elastic Compute Scaling**: Burst to cloud when local resources are saturated—handle peak workloads without over-provisioning permanent infrastructure
2. **Pay-Per-Use Economics**: EC2 instances are billed by the second (minimum 60 seconds)—only pay for actual compute time used during task execution
3. **Access to Specialized Hardware**: GPU instances (g4dn, p4d), high-memory instances (r5, x2i), and compute-optimized instances (c5, c6i) enable workloads impossible on typical development machines
4. **Geographic Distribution**: Deploy compute close to dependent services (databases, APIs) to reduce latency for integration-heavy workloads
5. **Isolation and Security**: Each task can run in a fresh, isolated instance—no state leakage between runs, reduced attack surface
6. **Reproducibility**: AMI-based provisioning ensures consistent environments across runs, eliminating "works on my machine" issues

### Scope

This task delivers:

1. **Ec2ComputeTarget Class**: Full implementation of `IComputeTarget` for EC2-backed compute
2. **Instance Provisioning**: Automated EC2 instance launch with configurable instance types, AMIs, VPCs, and security groups
3. **SSH Integration**: Delegation to `SshComputeTarget` for actual command execution after instance is running
4. **Lifecycle Management**: Full instance lifecycle from provisioning through teardown with proper resource cleanup
5. **Cost Tracking**: Real-time cost accumulation based on instance type pricing and run duration
6. **Spot Instance Support**: Optional use of spot instances for cost savings with automatic fallback to on-demand
7. **Orphan Detection**: Detection and cleanup of abandoned EC2 instances tagged with acode markers
8. **Mode Compliance**: Strict enforcement of operating mode—EC2 BLOCKED in local-only and airgapped modes

### Integration Points

| Component | Integration Type | Purpose |
|-----------|------------------|---------|
| Task 029 (IComputeTarget) | Implements | EC2 target provides cloud compute implementation |
| Task 030 (SSH Target) | Composes | SSH target handles actual command execution on provisioned instance |
| Task 032 (Placement Engine) | Consumer | Placement engine selects EC2 when burst needed |
| Task 033 (Burst Heuristics) | Trigger | Heuristics determine when to burst to EC2 |
| Task 001 (Operating Modes) | Enforces | Mode validation before any AWS API calls |
| Task 002a (Config) | Configuration | EC2 settings from agent-config.yml |
| AWS SDK for .NET | Dependency | EC2 API operations via AWSSDK.EC2 |

### Failure Modes

| Failure | Detection | Recovery |
|---------|-----------|----------|
| AWS credentials missing | SDK credential resolution fails | Return clear error with credential setup instructions |
| Insufficient IAM permissions | EC2 API returns UnauthorizedOperation | Return error listing required IAM permissions |
| Instance launch fails | RunInstances returns error | Log error, no resources to cleanup, propagate exception |
| Instance stuck in pending | State polling timeout (5 min) | Terminate instance, return error with instance ID for AWS console investigation |
| SSH never becomes ready | Port 22 connection failures after 30 retries | Terminate instance, suggest checking security group and AMI SSH configuration |
| Spot interruption | Spot termination notice received | Publish event, attempt graceful workload migration if time allows |
| Instance terminated externally | API returns instance not found | Treat as already cleaned up, log warning |
| Cost threshold exceeded | Accumulated cost > configured threshold | Publish alert event, optionally terminate to prevent overspend |
| Network connectivity lost | SSH connection drops mid-execution | Return partial results, instance may need manual cleanup |
| AMI not found | RunInstances returns InvalidAMIID.NotFound | Return error suggesting valid AMI IDs for region |

### Assumptions

1. AWS credentials are available via environment variables, ~/.aws/credentials, or IAM instance profile
2. IAM permissions include ec2:RunInstances, ec2:DescribeInstances, ec2:TerminateInstances, ec2:DescribeImages
3. Target VPC/subnet has internet connectivity for SSH access (or VPN/Direct Connect for private)
4. Security group allows inbound SSH (port 22) from the agent's IP
5. SSH key pair exists in AWS and private key is accessible locally
6. AMI has SSH daemon running and accessible on port 22 after boot
7. Agent has outbound internet connectivity to reach AWS API endpoints

### Security Considerations

1. **Credential Management**: AWS credentials MUST NOT be logged—use SDK credential chain, never inline secrets
2. **Least Privilege IAM**: Document minimum required IAM permissions, encourage use of scoped IAM roles
3. **Security Group Hygiene**: Encourage use of IP-restricted security groups, warn about 0.0.0.0/0 SSH rules
4. **Key Pair Security**: SSH private key path is sensitive—never log, ensure file permissions are restrictive
5. **Instance Isolation**: Each task gets fresh instance—no state persists, reduces lateral movement risk
6. **Tag-Based Access Control**: All instances tagged with acode=true enables IAM policy scoping
7. **Encryption**: Recommend EBS encryption at rest, SSH provides encryption in transit
8. **Audit Trail**: All EC2 API calls are logged to CloudTrail—enables forensic analysis

### Mode Compliance

| Mode | EC2 Behavior | Rationale |
|------|--------------|-----------|
| local-only | **BLOCKED** | No cloud resources permitted—hard constraint |
| airgapped | **BLOCKED** | No network calls to AWS APIs permitted |
| burst | **ALLOWED** | Cloud resources explicitly permitted when local insufficient |

CRITICAL: Mode validation MUST occur before ANY AWS API call. A single API call to AWS in local-only mode is a constraint violation. The system MUST NOT spend money in restricted modes.

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| EC2 | Elastic Compute Cloud—AWS's core virtual server service |
| AMI | Amazon Machine Image—template containing OS and software for instance launch |
| Instance Type | Hardware specification (vCPU, memory, network)—e.g., t3.medium, c5.xlarge |
| Spot Instance | Discounted EC2 capacity that can be interrupted with 2-minute warning |
| On-Demand Instance | Standard EC2 pricing with guaranteed availability |
| VPC | Virtual Private Cloud—isolated network environment in AWS |
| Subnet | Subdivision of VPC with specific IP range and availability zone |
| Security Group | Virtual firewall controlling inbound/outbound traffic to instances |
| Key Pair | SSH key pair for secure instance access—public key stored in AWS |
| EBS | Elastic Block Store—persistent block storage attached to instances |
| IAM | Identity and Access Management—AWS permission system |
| Instance Profile | IAM role attached to EC2 instance for AWS API access |
| User Data | Script executed on first boot of instance for initialization |
| Availability Zone | Isolated data center within a region |
| Elastic IP | Static public IP address that can be associated with instances |
| CloudTrail | AWS audit logging service for API call tracking |

---

## Out of Scope

The following items are explicitly excluded from Task 031:

- **Azure VM support** — Future epic for multi-cloud
- **GCP Compute Engine** — Future epic for multi-cloud
- **AWS Lambda functions** — Serverless is different execution model
- **ECS/EKS containers** — Container orchestration is separate concern
- **EC2 Mac instances** — Specialized use case, different provisioning model
- **Dedicated hosts** — Enterprise feature for compliance, not typical use
- **Placement groups** — Advanced networking optimization
- **Instance store volumes** — Ephemeral storage, EBS preferred for persistence
- **Hibernate support** — Complex state management, terminate simpler
- **Auto Scaling groups** — Overkill for single-task execution

---

## Functional Requirements

### EC2 Target Core (FR-031-01 to FR-031-25)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031-01 | `Ec2ComputeTarget` class MUST implement `IComputeTarget` interface | Must Have |
| FR-031-02 | Target MUST have unique `TargetId` property (ULID format) | Must Have |
| FR-031-03 | Target MUST expose `TargetType` as "ec2" | Must Have |
| FR-031-04 | Target MUST track `State` (NotProvisioned, Provisioning, Ready, TearingDown, Terminated, Failed) | Must Have |
| FR-031-05 | Target MUST expose `IsReady` property (true only when Ready and SSH connected) | Must Have |
| FR-031-06 | Target MUST implement `IAsyncDisposable` for proper cleanup | Must Have |
| FR-031-07 | `DisposeAsync` MUST call `TeardownAsync` if not already terminated | Must Have |
| FR-031-08 | Target MUST store `Ec2InstanceInfo` after successful provisioning | Must Have |
| FR-031-09 | Target MUST track provisioning timestamp for cost calculation | Must Have |
| FR-031-10 | Target MUST compose `SshComputeTarget` for command execution | Must Have |
| FR-031-11 | All public methods MUST throw `InvalidOperationException` if called when not ready | Must Have |
| FR-031-12 | Target MUST publish domain events for lifecycle transitions | Should Have |
| FR-031-13 | Target MUST log all significant operations at appropriate levels | Must Have |
| FR-031-14 | Target MUST NOT log AWS credentials or SSH private keys | Must Have |
| FR-031-15 | Target MUST validate operating mode before any AWS API call | Must Have |
| FR-031-16 | Target MUST throw `ModeViolationException` in local-only or airgapped mode | Must Have |
| FR-031-17 | Target MUST support configuration via `Ec2Configuration` record | Must Have |
| FR-031-18 | Target MUST be created via `Ec2ComputeTargetFactory` | Must Have |
| FR-031-19 | Factory MUST validate mode before creating target | Must Have |
| FR-031-20 | Factory MUST create appropriate AWS SDK client with region | Must Have |
| FR-031-21 | Factory MUST resolve configuration from agent-config.yml | Should Have |
| FR-031-22 | Target MUST support cancellation via CancellationToken on all async methods | Must Have |
| FR-031-23 | Target MUST cleanup resources on cancellation | Must Have |
| FR-031-24 | Target MUST be thread-safe for concurrent method calls | Should Have |
| FR-031-25 | Target MUST expose `InstanceId` property when provisioned | Should Have |

### AWS Credential Resolution (FR-031-26 to FR-031-40)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031-26 | Credentials MUST be resolvable from environment variables | Must Have |
| FR-031-27 | Credentials MUST be resolvable from AWS_ACCESS_KEY_ID and AWS_SECRET_ACCESS_KEY | Must Have |
| FR-031-28 | Credentials MUST be resolvable from AWS_SESSION_TOKEN for temporary credentials | Must Have |
| FR-031-29 | Credentials MUST be resolvable from ~/.aws/credentials file | Must Have |
| FR-031-30 | Credentials MUST support profile selection via AWS_PROFILE | Must Have |
| FR-031-31 | Credentials MUST be resolvable from IAM instance profile (when on EC2) | Should Have |
| FR-031-32 | Credentials MUST be resolvable from ECS task role | Should Have |
| FR-031-33 | Credential resolution MUST use AWS SDK default chain | Must Have |
| FR-031-34 | Missing credentials MUST result in clear error message | Must Have |
| FR-031-35 | Credential error MUST suggest available resolution methods | Should Have |
| FR-031-36 | Credentials MUST NOT be logged in any form | Must Have |
| FR-031-37 | Credentials MUST NOT appear in exception messages | Must Have |
| FR-031-38 | Region MUST be configurable via Ec2Configuration | Must Have |
| FR-031-39 | Region MUST have fallback to AWS_REGION environment variable | Should Have |
| FR-031-40 | Region MUST have fallback to AWS_DEFAULT_REGION | Should Have |

### Instance Type Configuration (FR-031-41 to FR-031-60)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031-41 | Instance type MUST be configurable via Ec2Configuration | Must Have |
| FR-031-42 | Default instance type MUST be t3.medium | Must Have |
| FR-031-43 | General purpose instances MUST be supported (t3, t3a, m5, m5a, m6i) | Must Have |
| FR-031-44 | Compute optimized instances MUST be supported (c5, c5a, c6i) | Must Have |
| FR-031-45 | Memory optimized instances MUST be supported (r5, r5a, r6i) | Should Have |
| FR-031-46 | GPU instances MUST be supported (g4dn, g5, p4d) | Should Have |
| FR-031-47 | Invalid instance type MUST result in validation error before launch | Must Have |
| FR-031-48 | Instance type validation MUST check against known instance families | Should Have |
| FR-031-49 | Instance type MUST be logged at info level during provisioning | Must Have |
| FR-031-50 | Burst credits MUST be documented for t-series instances | Should Have |
| FR-031-51 | vCPU and memory MUST be retrievable for selected instance type | Should Have |
| FR-031-52 | Instance type recommendations MUST be available based on workload hints | Could Have |
| FR-031-53 | Workload hint "cpu" SHOULD suggest c5/c6i instances | Could Have |
| FR-031-54 | Workload hint "memory" SHOULD suggest r5/r6i instances | Could Have |
| FR-031-55 | Workload hint "gpu" SHOULD suggest g4dn/g5 instances | Could Have |
| FR-031-56 | Workload hint "balanced" SHOULD suggest m5/m6i instances | Could Have |
| FR-031-57 | Instance type change MUST require new provisioning | Must Have |
| FR-031-58 | ARM instances (t4g, m6g, c6g) SHOULD be supported | Could Have |
| FR-031-59 | ARM instances MUST warn about architecture compatibility | Could Have |
| FR-031-60 | Maximum instance type limits SHOULD be configurable for cost control | Should Have |

### AMI Configuration (FR-031-61 to FR-031-80)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031-61 | AMI ID MUST be configurable via Ec2Configuration | Must Have |
| FR-031-62 | Default AMI MUST be latest Amazon Linux 2023 for region | Must Have |
| FR-031-63 | AMI resolution MUST query EC2 DescribeImages API | Must Have |
| FR-031-64 | AMI resolution MUST filter by owner (amazon for default) | Must Have |
| FR-031-65 | AMI resolution MUST filter by architecture (x86_64 by default) | Must Have |
| FR-031-66 | AMI resolution MUST sort by creation date descending | Must Have |
| FR-031-67 | Resolved AMI ID MUST be logged at debug level | Should Have |
| FR-031-68 | Custom AMI IDs MUST be validated before launch | Must Have |
| FR-031-69 | Invalid AMI MUST result in clear error with suggested valid AMIs | Should Have |
| FR-031-70 | AMI MUST be region-specific (no cross-region AMI usage) | Must Have |
| FR-031-71 | Ubuntu AMIs SHOULD be supported as alternative | Should Have |
| FR-031-72 | Debian AMIs SHOULD be supported as alternative | Could Have |
| FR-031-73 | Windows AMIs MUST NOT be supported (SSH requirement) | Must Have |
| FR-031-74 | AMI MUST have SSH daemon pre-configured and enabled | Must Have |
| FR-031-75 | AMI default user MUST be determinable (ec2-user, ubuntu, admin) | Must Have |
| FR-031-76 | User override MUST be configurable for custom AMIs | Should Have |
| FR-031-77 | AMI architecture MUST match instance type architecture | Must Have |
| FR-031-78 | ARM AMIs MUST be used for ARM instances (Graviton) | Should Have |
| FR-031-79 | AMI caching SHOULD reduce API calls for repeated provisions | Could Have |
| FR-031-80 | AMI age warnings SHOULD be emitted for old AMIs | Could Have |

### Network Configuration (FR-031-81 to FR-031-100)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031-81 | VPC ID SHOULD be configurable via Ec2Configuration | Should Have |
| FR-031-82 | Subnet ID MUST be configurable via Ec2Configuration | Must Have |
| FR-031-83 | Default VPC MUST be used when subnet not specified | Must Have |
| FR-031-84 | Security group IDs MUST be configurable as list | Must Have |
| FR-031-85 | At least one security group MUST allow SSH (port 22) inbound | Must Have |
| FR-031-86 | Security group validation SHOULD verify SSH access before launch | Should Have |
| FR-031-87 | Public IP association MUST be configurable (default true) | Must Have |
| FR-031-88 | Private IP only mode MUST be supported for VPN scenarios | Should Have |
| FR-031-89 | Elastic IP association SHOULD be supported | Could Have |
| FR-031-90 | Elastic IP MUST be released on teardown if temporarily allocated | Should Have |
| FR-031-91 | Temporary security group creation SHOULD be supported | Could Have |
| FR-031-92 | Temporary security group MUST be deleted on teardown | Should Have |
| FR-031-93 | Temporary security group MUST restrict SSH to agent's public IP | Should Have |
| FR-031-94 | Agent public IP detection MUST use external service (ifconfig.me) | Should Have |
| FR-031-95 | Private subnet support MUST work with NAT gateway | Should Have |
| FR-031-96 | DNS hostname MUST be enabled for public instances | Should Have |
| FR-031-97 | Network interface configuration SHOULD be logged | Should Have |
| FR-031-98 | IPv6 support SHOULD be available when VPC supports it | Could Have |
| FR-031-99 | Source/dest check disable SHOULD be configurable | Could Have |
| FR-031-100 | Enhanced networking SHOULD be enabled for supported types | Could Have |

### SSH Key Pair Configuration (FR-031-101 to FR-031-115)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031-101 | Key pair name MUST be configurable via Ec2Configuration | Must Have |
| FR-031-102 | Private key path MUST be resolvable for SSH connection | Must Have |
| FR-031-103 | Private key path SHOULD default to ~/.ssh/{keyPairName}.pem | Should Have |
| FR-031-104 | Private key path MUST be configurable explicitly | Should Have |
| FR-031-105 | Key pair existence MUST be validated before launch | Must Have |
| FR-031-106 | Invalid key pair MUST result in clear error | Must Have |
| FR-031-107 | Private key file existence MUST be validated | Must Have |
| FR-031-108 | Private key file permissions SHOULD be validated (Unix) | Should Have |
| FR-031-109 | Auto key pair generation SHOULD be supported | Could Have |
| FR-031-110 | Auto-generated key pair MUST be stored securely | Should Have |
| FR-031-111 | Auto-generated key pair MUST be deleted on teardown | Should Have |
| FR-031-112 | Key pair name MUST NOT be logged at info level | Should Have |
| FR-031-113 | Private key content MUST NOT be logged at any level | Must Have |
| FR-031-114 | ED25519 keys SHOULD be supported | Could Have |
| FR-031-115 | RSA keys MUST be supported (AWS default) | Must Have |

### Instance Provisioning (FR-031-116 to FR-031-140)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031-116 | `PrepareAsync` MUST provision EC2 instance | Must Have |
| FR-031-117 | Instance MUST be created via RunInstances API | Must Have |
| FR-031-118 | RunInstances MUST specify MinCount=1, MaxCount=1 | Must Have |
| FR-031-119 | Instance MUST be tagged with acode=true | Must Have |
| FR-031-120 | Instance MUST be tagged with target-id={TargetId} | Should Have |
| FR-031-121 | Instance MUST be tagged with provisioned-at={timestamp} | Should Have |
| FR-031-122 | Custom tags MUST be merged with required tags | Should Have |
| FR-031-123 | Tag specification MUST use ResourceType.Instance | Must Have |
| FR-031-124 | Instance state MUST be polled until Running | Must Have |
| FR-031-125 | State polling interval MUST be 5 seconds | Should Have |
| FR-031-126 | State polling MUST have configurable timeout (default 5 min) | Must Have |
| FR-031-127 | Pending state timeout MUST terminate instance and throw | Must Have |
| FR-031-128 | Public IP MUST be captured once Running | Must Have |
| FR-031-129 | Private IP MUST be captured as fallback | Must Have |
| FR-031-130 | SSH readiness MUST be verified after Running state | Must Have |
| FR-031-131 | SSH readiness MUST retry up to 30 times | Must Have |
| FR-031-132 | SSH retry interval MUST be 10 seconds | Should Have |
| FR-031-133 | SSH readiness MUST attempt TCP connection to port 22 | Must Have |
| FR-031-134 | SSH connection failure after retries MUST terminate instance | Must Have |
| FR-031-135 | SSH target MUST be created after SSH readiness confirmed | Must Have |
| FR-031-136 | SSH target MUST be configured with instance IP and key | Must Have |
| FR-031-137 | Workspace preparation MUST be delegated to SSH target | Must Have |
| FR-031-138 | Provisioning events MUST be published (Launching, Running) | Should Have |
| FR-031-139 | Provisioning duration MUST be tracked and logged | Should Have |
| FR-031-140 | Failed provisioning MUST cleanup any created resources | Must Have |

### Instance Lifecycle Management (FR-031-141 to FR-031-160)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031-141 | Instance state MUST be trackable via IEc2InstanceManager | Must Have |
| FR-031-142 | GetStateAsync MUST return current instance state | Must Have |
| FR-031-143 | GetInstanceAsync MUST return full Ec2InstanceInfo | Should Have |
| FR-031-144 | Stop MUST be supported for instance preservation | Could Have |
| FR-031-145 | Start MUST be supported for stopped instances | Could Have |
| FR-031-146 | Terminate MUST be the primary cleanup method | Must Have |
| FR-031-147 | Terminate MUST be idempotent | Must Have |
| FR-031-148 | Terminate on already-terminated MUST NOT error | Must Have |
| FR-031-149 | TeardownAsync MUST terminate the instance | Must Have |
| FR-031-150 | TeardownAsync MUST disconnect SSH first | Must Have |
| FR-031-151 | TeardownAsync MUST calculate run duration | Should Have |
| FR-031-152 | TeardownAsync MUST calculate final cost | Should Have |
| FR-031-153 | TeardownAsync MUST publish termination event | Should Have |
| FR-031-154 | TeardownAsync MUST log instance ID and run duration | Must Have |
| FR-031-155 | State transitions MUST be logged at info level | Should Have |
| FR-031-156 | Unexpected state MUST be logged at warning level | Should Have |
| FR-031-157 | ListAcodeInstancesAsync MUST find all tagged instances | Should Have |
| FR-031-158 | List MUST filter by acode=true tag | Should Have |
| FR-031-159 | List MUST filter by region | Should Have |
| FR-031-160 | List MUST return running and pending instances | Should Have |

### Spot Instance Support (FR-031-161 to FR-031-180)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031-161 | Spot instance mode MUST be configurable (default false) | Should Have |
| FR-031-162 | Spot request MUST use RunInstances with InstanceMarketOptions | Should Have |
| FR-031-163 | Spot max price MUST be configurable | Should Have |
| FR-031-164 | Spot max price SHOULD default to on-demand price | Should Have |
| FR-031-165 | Spot request type MUST be one-time (not persistent) | Should Have |
| FR-031-166 | Spot capacity unavailable MUST fallback to on-demand | Should Have |
| FR-031-167 | Spot fallback MUST be configurable (default true) | Should Have |
| FR-031-168 | Spot interruption MUST be detectable via instance metadata | Could Have |
| FR-031-169 | Spot interruption MUST publish event with 2-min warning | Could Have |
| FR-031-170 | Spot interruption event MUST include interruption reason | Could Have |
| FR-031-171 | Spot savings MUST be logged (spot price vs on-demand) | Should Have |
| FR-031-172 | IsSpotInstance MUST be tracked in Ec2InstanceInfo | Should Have |
| FR-031-173 | Spot price history SHOULD be queryable | Could Have |
| FR-031-174 | Spot availability SHOULD be checked before request | Could Have |
| FR-031-175 | Spot-only mode SHOULD be available (no fallback) | Could Have |
| FR-031-176 | On-demand fallback MUST log that fallback occurred | Should Have |
| FR-031-177 | Spot interruption behavior type MUST be terminate | Should Have |
| FR-031-178 | Spot pricing MUST use current spot price for cost tracking | Should Have |
| FR-031-179 | Spot capacity pools SHOULD be configurable | Could Have |
| FR-031-180 | Spot hibernation MUST NOT be used (complexity) | Must Have |

### Orphan Detection and Cleanup (FR-031-181 to FR-031-200)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031-181 | Orphan detection MUST find running instances with acode=true tag | Must Have |
| FR-031-182 | Orphan threshold MUST be configurable (default 2 hours) | Should Have |
| FR-031-183 | Orphan MUST be defined as running beyond threshold with no recent activity | Should Have |
| FR-031-184 | Orphan detection MUST query DescribeInstances with tag filter | Must Have |
| FR-031-185 | Orphan detection MUST compare launch time to current time | Must Have |
| FR-031-186 | DetectOrphansAsync MUST return list of orphan Ec2InstanceInfo | Must Have |
| FR-031-187 | CleanupOrphansAsync MUST terminate specified instance IDs | Should Have |
| FR-031-188 | Cleanup MUST be safe (verify tag before terminate) | Must Have |
| FR-031-189 | Cleanup MUST log each terminated instance | Must Have |
| FR-031-190 | CleanupAllOrphansAsync MUST support dry-run mode | Should Have |
| FR-031-191 | Dry-run MUST list orphans without terminating | Should Have |
| FR-031-192 | Cleanup MUST return count of terminated instances | Should Have |
| FR-031-193 | CLI command MUST expose orphan detection | Should Have |
| FR-031-194 | CLI command MUST expose orphan cleanup | Should Have |
| FR-031-195 | CLI cleanup MUST require confirmation (--force to skip) | Should Have |
| FR-031-196 | Orphan detection SHOULD run periodically in background | Could Have |
| FR-031-197 | Orphan detection SHOULD publish alert event | Could Have |
| FR-031-198 | Orphan cost accumulation SHOULD be reported | Could Have |
| FR-031-199 | Cross-region orphan detection SHOULD be supported | Could Have |
| FR-031-200 | Orphan detection MUST NOT affect non-acode instances | Must Have |

### EBS Volume Configuration (FR-031-201 to FR-031-215)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031-201 | Root EBS volume size MUST be configurable | Should Have |
| FR-031-202 | Default EBS size MUST be 20GB | Should Have |
| FR-031-203 | EBS volume type SHOULD be configurable (default gp3) | Should Have |
| FR-031-204 | EBS IOPS SHOULD be configurable for io1/io2/gp3 | Could Have |
| FR-031-205 | EBS throughput SHOULD be configurable for gp3 | Could Have |
| FR-031-206 | EBS encryption SHOULD be configurable (default false) | Should Have |
| FR-031-207 | EBS encryption key SHOULD be configurable (KMS) | Could Have |
| FR-031-208 | DeleteOnTermination MUST be true for root volume | Must Have |
| FR-031-209 | Additional EBS volumes SHOULD be configurable | Could Have |
| FR-031-210 | Volume tagging MUST match instance tags | Should Have |
| FR-031-211 | EBS optimization MUST be enabled for supported types | Should Have |
| FR-031-212 | EBS snapshot support is OUT OF SCOPE | Must Have |
| FR-031-213 | Instance store volumes are OUT OF SCOPE | Must Have |
| FR-031-214 | EBS volume configuration MUST be logged | Should Have |
| FR-031-215 | Insufficient EBS quota MUST result in clear error | Should Have |

### User Data and Initialization (FR-031-216 to FR-031-225)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-031-216 | User data script MUST be configurable | Should Have |
| FR-031-217 | User data MUST be base64 encoded before API call | Must Have |
| FR-031-218 | User data size MUST be validated (<16KB) | Should Have |
| FR-031-219 | User data execution MUST be waited for if specified | Could Have |
| FR-031-220 | Cloud-init completion SHOULD be detectable | Could Have |
| FR-031-221 | User data errors SHOULD be detectable via console output | Could Have |
| FR-031-222 | Instance profile ARN MUST be configurable | Should Have |
| FR-031-223 | Instance profile MUST grant instance AWS API access | Should Have |
| FR-031-224 | Instance metadata service v2 SHOULD be required | Should Have |
| FR-031-225 | IMDSv2 hop limit SHOULD be configurable | Could Have |

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

### Part 1: File Structure and Domain Models

**Target Directory Structure:**
```
src/
├── Acode.Domain/
│   └── Compute/
│       └── Ec2/
│           ├── Ec2InstanceState.cs
│           ├── Ec2InstanceInfo.cs
│           ├── Ec2PricingInfo.cs
│           └── Events/
│               ├── Ec2InstanceLaunchingEvent.cs
│               ├── Ec2InstanceRunningEvent.cs
│               ├── Ec2InstanceTerminatingEvent.cs
│               ├── Ec2SpotInterruptionEvent.cs
│               └── Ec2CostAlertEvent.cs
├── Acode.Application/
│   └── Compute/
│       └── Ec2/
│           ├── Ec2Configuration.cs
│           ├── IEc2InstanceProvisioner.cs
│           ├── IEc2InstanceManager.cs
│           ├── IEc2CostTracker.cs
│           └── IEc2OrphanDetector.cs
└── Acode.Infrastructure/
    └── Compute/
        └── Ec2/
            ├── Ec2ComputeTarget.cs
            ├── Ec2ComputeTargetFactory.cs
            ├── Ec2InstanceProvisioner.cs
            ├── Ec2InstanceManager.cs
            ├── Ec2CostTracker.cs
            ├── Ec2OrphanDetector.cs
            ├── Ec2AmiResolver.cs
            ├── Ec2SecurityGroupManager.cs
            └── Ec2CredentialResolver.cs
```

**Domain Models:**

```csharp
// src/Acode.Domain/Compute/Ec2/Ec2InstanceState.cs
namespace Acode.Domain.Compute.Ec2;

public enum Ec2InstanceState
{
    Pending,
    Running,
    Stopping,
    Stopped,
    ShuttingDown,
    Terminated,
    Unknown
}

// src/Acode.Domain/Compute/Ec2/Ec2InstanceInfo.cs
namespace Acode.Domain.Compute.Ec2;

public sealed record Ec2InstanceInfo
{
    public required string InstanceId { get; init; }
    public required string InstanceType { get; init; }
    public string? PublicIp { get; init; }
    public string? PrivateIp { get; init; }
    public required Ec2InstanceState State { get; init; }
    public required DateTimeOffset LaunchTime { get; init; }
    public string? AmiId { get; init; }
    public string? SubnetId { get; init; }
    public string? VpcId { get; init; }
    public bool IsSpotInstance { get; init; }
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
}

// src/Acode.Domain/Compute/Ec2/Ec2PricingInfo.cs
namespace Acode.Domain.Compute.Ec2;

public sealed record Ec2PricingInfo
{
    public required string InstanceType { get; init; }
    public required string Region { get; init; }
    public decimal OnDemandHourlyRate { get; init; }
    public decimal? SpotHourlyRate { get; init; }
    public decimal EstimatedMonthlyCost => OnDemandHourlyRate * 24 * 30;
}

// src/Acode.Domain/Compute/Ec2/Events/Ec2InstanceLaunchingEvent.cs
namespace Acode.Domain.Compute.Ec2.Events;

public sealed record Ec2InstanceLaunchingEvent(
    string InstanceId,
    string InstanceType,
    string Region,
    bool IsSpot,
    DateTimeOffset LaunchedAt);

// src/Acode.Domain/Compute/Ec2/Events/Ec2InstanceRunningEvent.cs
namespace Acode.Domain.Compute.Ec2.Events;

public sealed record Ec2InstanceRunningEvent(
    string InstanceId,
    string PublicIp,
    TimeSpan ProvisionDuration,
    DateTimeOffset ReadyAt);

// src/Acode.Domain/Compute/Ec2/Events/Ec2InstanceTerminatingEvent.cs
namespace Acode.Domain.Compute.Ec2.Events;

public sealed record Ec2InstanceTerminatingEvent(
    string InstanceId,
    TimeSpan RunDuration,
    decimal EstimatedCost,
    DateTimeOffset TerminatedAt);

// src/Acode.Domain/Compute/Ec2/Events/Ec2SpotInterruptionEvent.cs
namespace Acode.Domain.Compute.Ec2.Events;

public sealed record Ec2SpotInterruptionEvent(
    string InstanceId,
    string InterruptionReason,
    DateTimeOffset InterruptionTime,
    bool WillFallbackToOnDemand);

// src/Acode.Domain/Compute/Ec2/Events/Ec2CostAlertEvent.cs
namespace Acode.Domain.Compute.Ec2.Events;

public sealed record Ec2CostAlertEvent(
    string InstanceId,
    decimal CurrentCost,
    decimal Threshold,
    TimeSpan RunDuration,
    DateTimeOffset AlertedAt);
```

**End of Task 031 Specification - Part 1/4**

### Part 2: Application Interfaces

```csharp
// src/Acode.Application/Compute/Ec2/Ec2Configuration.cs
namespace Acode.Application.Compute.Ec2;

public sealed record Ec2Configuration
{
    public required string Region { get; init; }
    public string InstanceType { get; init; } = "t3.medium";
    public string? AmiId { get; init; } // null = latest Amazon Linux 2
    public string? SubnetId { get; init; }
    public IReadOnlyList<string> SecurityGroupIds { get; init; } = [];
    public string? KeyPairName { get; init; }
    public string? InstanceProfileArn { get; init; }
    public bool SpotEnabled { get; init; } = false;
    public decimal? SpotMaxPrice { get; init; }
    public bool SpotFallbackToOnDemand { get; init; } = true;
    public int EbsSizeGb { get; init; } = 20;
    public IReadOnlyDictionary<string, string> Tags { get; init; } = new Dictionary<string, string>();
    public bool CreateTempSecurityGroup { get; init; } = false;
    public bool AssociatePublicIp { get; init; } = true;
    public string? UserData { get; init; }
    public TimeSpan ProvisionTimeout { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan SshReadyTimeout { get; init; } = TimeSpan.FromMinutes(2);
    public int SshRetryCount { get; init; } = 30;
    public TimeSpan SshRetryInterval { get; init; } = TimeSpan.FromSeconds(10);
}

// src/Acode.Application/Compute/Ec2/IEc2InstanceProvisioner.cs
namespace Acode.Application.Compute.Ec2;

public interface IEc2InstanceProvisioner
{
    Task<Ec2InstanceInfo> ProvisionAsync(
        Ec2Configuration config,
        CancellationToken ct = default);
    
    Task<string> ResolveAmiAsync(
        string? amiId,
        string region,
        CancellationToken ct = default);
    
    Task<bool> WaitForRunningAsync(
        string instanceId,
        TimeSpan timeout,
        CancellationToken ct = default);
    
    Task<bool> WaitForSshReadyAsync(
        string instanceId,
        string host,
        int port,
        TimeSpan timeout,
        CancellationToken ct = default);
}

// src/Acode.Application/Compute/Ec2/IEc2InstanceManager.cs
namespace Acode.Application.Compute.Ec2;

public interface IEc2InstanceManager
{
    Task<Ec2InstanceInfo?> GetInstanceAsync(
        string instanceId,
        CancellationToken ct = default);
    
    Task<Ec2InstanceState> GetStateAsync(
        string instanceId,
        CancellationToken ct = default);
    
    Task StartAsync(string instanceId, CancellationToken ct = default);
    Task StopAsync(string instanceId, CancellationToken ct = default);
    Task TerminateAsync(string instanceId, CancellationToken ct = default);
    
    Task<IReadOnlyList<Ec2InstanceInfo>> ListAcodeInstancesAsync(
        string region,
        CancellationToken ct = default);
}

// src/Acode.Application/Compute/Ec2/IEc2CostTracker.cs
namespace Acode.Application.Compute.Ec2;

public interface IEc2CostTracker
{
    Task<Ec2PricingInfo> GetPricingAsync(
        string instanceType,
        string region,
        CancellationToken ct = default);
    
    decimal CalculateRunningCost(
        string instanceType,
        TimeSpan runDuration,
        bool isSpot);
    
    Task<decimal> GetAccumulatedCostAsync(
        string instanceId,
        CancellationToken ct = default);
    
    void SetCostAlertThreshold(string instanceId, decimal threshold);
}

// src/Acode.Application/Compute/Ec2/IEc2OrphanDetector.cs
namespace Acode.Application.Compute.Ec2;

public interface IEc2OrphanDetector
{
    Task<IReadOnlyList<Ec2InstanceInfo>> DetectOrphansAsync(
        string region,
        TimeSpan orphanThreshold,
        CancellationToken ct = default);
    
    Task CleanupOrphansAsync(
        IEnumerable<string> instanceIds,
        CancellationToken ct = default);
    
    Task<int> CleanupAllOrphansAsync(
        string region,
        TimeSpan orphanThreshold,
        bool dryRun = true,
        CancellationToken ct = default);
}
```

**End of Task 031 Specification - Part 2/4**

### Part 3: Infrastructure Implementation - EC2 Target and Factory

```csharp
// src/Acode.Infrastructure/Compute/Ec2/Ec2ComputeTarget.cs
namespace Acode.Infrastructure.Compute.Ec2;

public sealed class Ec2ComputeTarget : IComputeTarget, IAsyncDisposable
{
    private readonly IAmazonEC2 _ec2Client;
    private readonly IEc2InstanceProvisioner _provisioner;
    private readonly IEc2InstanceManager _instanceManager;
    private readonly IEc2CostTracker _costTracker;
    private readonly IModeValidator _modeValidator;
    private readonly IEventPublisher _events;
    private readonly ILogger<Ec2ComputeTarget> _logger;
    
    private readonly Ec2Configuration _config;
    private SshComputeTarget? _sshTarget;
    private Ec2InstanceInfo? _instance;
    private DateTimeOffset? _provisionedAt;
    
    public string TargetId { get; } = Ulid.NewUlid().ToString();
    public string TargetType => "ec2";
    public ComputeTargetState State { get; private set; } = ComputeTargetState.NotProvisioned;
    public bool IsReady => State == ComputeTargetState.Ready && _sshTarget?.IsReady == true;
    
    public Ec2ComputeTarget(
        IAmazonEC2 ec2Client,
        IEc2InstanceProvisioner provisioner,
        IEc2InstanceManager instanceManager,
        IEc2CostTracker costTracker,
        IModeValidator modeValidator,
        SshComputeTargetFactory sshFactory,
        IEventPublisher events,
        ILogger<Ec2ComputeTarget> logger,
        Ec2Configuration config)
    {
        _ec2Client = ec2Client;
        _provisioner = provisioner;
        _instanceManager = instanceManager;
        _costTracker = costTracker;
        _modeValidator = modeValidator;
        _events = events;
        _logger = logger;
        _config = config;
    }
    
    public async Task<WorkspacePrepareResult> PrepareAsync(
        WorkspaceContext context,
        CancellationToken ct = default)
    {
        // Mode validation - EC2 blocked in local-only and airgapped
        if (!_modeValidator.IsCloudAllowed())
        {
            throw new ModeViolationException(
                $"EC2 targets blocked in {_modeValidator.CurrentMode} mode");
        }
        
        State = ComputeTargetState.Provisioning;
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Provision EC2 instance
            _instance = await _provisioner.ProvisionAsync(_config, ct);
            _provisionedAt = DateTimeOffset.UtcNow;
            
            await _events.PublishAsync(new Ec2InstanceLaunchingEvent(
                _instance.InstanceId,
                _instance.InstanceType,
                _config.Region,
                _instance.IsSpotInstance,
                _provisionedAt.Value));
            
            // Wait for SSH ready
            var host = _instance.PublicIp ?? _instance.PrivateIp 
                ?? throw new InvalidOperationException("No IP address available");
            
            await _provisioner.WaitForSshReadyAsync(
                _instance.InstanceId, host, 22, _config.SshReadyTimeout, ct);
            
            stopwatch.Stop();
            
            await _events.PublishAsync(new Ec2InstanceRunningEvent(
                _instance.InstanceId, host, stopwatch.Elapsed, DateTimeOffset.UtcNow));
            
            // Create SSH target for execution
            _sshTarget = await CreateSshTargetAsync(host, ct);
            
            // Delegate workspace preparation to SSH target
            var result = await _sshTarget.PrepareAsync(context, ct);
            
            State = ComputeTargetState.Ready;
            _logger.LogInformation(
                "EC2 target {InstanceId} ready in {Duration}",
                _instance.InstanceId, stopwatch.Elapsed);
            
            return result;
        }
        catch (Exception ex)
        {
            State = ComputeTargetState.Failed;
            _logger.LogError(ex, "Failed to provision EC2 instance");
            
            // Cleanup on failure
            if (_instance != null)
            {
                await TeardownAsync(ct);
            }
            
            throw;
        }
    }
    
    public async Task<ExecuteResult> ExecuteAsync(
        string command,
        ExecuteOptions? options = null,
        CancellationToken ct = default)
    {
        EnsureReady();
        return await _sshTarget!.ExecuteAsync(command, options, ct);
    }
    
    public async Task<TransferResult> UploadAsync(
        string localPath,
        string remotePath,
        TransferOptions? options = null,
        CancellationToken ct = default)
    {
        EnsureReady();
        return await _sshTarget!.UploadAsync(localPath, remotePath, options, ct);
    }
    
    public async Task<TransferResult> DownloadAsync(
        string remotePath,
        string localPath,
        TransferOptions? options = null,
        CancellationToken ct = default)
    {
        EnsureReady();
        return await _sshTarget!.DownloadAsync(remotePath, localPath, options, ct);
    }
    
    public async Task TeardownAsync(CancellationToken ct = default)
    {
        State = ComputeTargetState.TearingDown;
        
        try
        {
            // Cleanup SSH target first
            if (_sshTarget != null)
            {
                await _sshTarget.TeardownAsync(ct);
                await _sshTarget.DisposeAsync();
                _sshTarget = null;
            }
            
            // Terminate EC2 instance
            if (_instance != null)
            {
                var runDuration = _provisionedAt.HasValue 
                    ? DateTimeOffset.UtcNow - _provisionedAt.Value 
                    : TimeSpan.Zero;
                
                var cost = _costTracker.CalculateRunningCost(
                    _instance.InstanceType, runDuration, _instance.IsSpotInstance);
                
                await _instanceManager.TerminateAsync(_instance.InstanceId, ct);
                
                await _events.PublishAsync(new Ec2InstanceTerminatingEvent(
                    _instance.InstanceId, runDuration, cost, DateTimeOffset.UtcNow));
                
                _logger.LogInformation(
                    "EC2 instance {InstanceId} terminated after {Duration}, cost: ${Cost:F4}",
                    _instance.InstanceId, runDuration, cost);
                
                _instance = null;
            }
            
            State = ComputeTargetState.Terminated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during EC2 teardown");
            State = ComputeTargetState.Failed;
            throw;
        }
    }
    
    private void EnsureReady()
    {
        if (!IsReady)
            throw new InvalidOperationException("EC2 target not ready");
    }
    
    public async ValueTask DisposeAsync()
    {
        if (State != ComputeTargetState.Terminated)
        {
            await TeardownAsync();
        }
    }
}

// src/Acode.Infrastructure/Compute/Ec2/Ec2ComputeTargetFactory.cs
namespace Acode.Infrastructure.Compute.Ec2;

public sealed class Ec2ComputeTargetFactory : IComputeTargetFactory
{
    private readonly IServiceProvider _services;
    private readonly IModeValidator _modeValidator;
    
    public string TargetType => "ec2";
    
    public async Task<IComputeTarget> CreateAsync(
        ComputeTargetConfiguration config,
        CancellationToken ct = default)
    {
        if (!_modeValidator.IsCloudAllowed())
        {
            throw new ModeViolationException(
                $"Cannot create EC2 target in {_modeValidator.CurrentMode} mode");
        }
        
        var ec2Config = ParseConfiguration(config);
        var ec2Client = CreateEc2Client(ec2Config);
        
        return new Ec2ComputeTarget(
            ec2Client,
            _services.GetRequiredService<IEc2InstanceProvisioner>(),
            _services.GetRequiredService<IEc2InstanceManager>(),
            _services.GetRequiredService<IEc2CostTracker>(),
            _modeValidator,
            _services.GetRequiredService<SshComputeTargetFactory>(),
            _services.GetRequiredService<IEventPublisher>(),
            _services.GetRequiredService<ILogger<Ec2ComputeTarget>>(),
            ec2Config);
    }
    
    private IAmazonEC2 CreateEc2Client(Ec2Configuration config)
    {
        var awsConfig = new AmazonEC2Config
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(config.Region)
        };
        return new AmazonEC2Client(awsConfig);
    }
}
```

**End of Task 031 Specification - Part 3/4**

### Part 4: Provisioner, Orphan Detection, and Implementation Checklist

```csharp
// src/Acode.Infrastructure/Compute/Ec2/Ec2InstanceProvisioner.cs
namespace Acode.Infrastructure.Compute.Ec2;

public sealed class Ec2InstanceProvisioner : IEc2InstanceProvisioner
{
    private readonly IAmazonEC2 _ec2Client;
    private readonly IEc2AmiResolver _amiResolver;
    private readonly ILogger<Ec2InstanceProvisioner> _logger;
    
    public async Task<Ec2InstanceInfo> ProvisionAsync(
        Ec2Configuration config,
        CancellationToken ct = default)
    {
        var amiId = await ResolveAmiAsync(config.AmiId, config.Region, ct);
        
        var request = new RunInstancesRequest
        {
            ImageId = amiId,
            InstanceType = InstanceType.FindValue(config.InstanceType),
            MinCount = 1,
            MaxCount = 1,
            SubnetId = config.SubnetId,
            SecurityGroupIds = config.SecurityGroupIds?.ToList(),
            KeyName = config.KeyPairName,
            TagSpecifications =
            [
                new TagSpecification
                {
                    ResourceType = ResourceType.Instance,
                    Tags = BuildTags(config.Tags)
                }
            ]
        };
        
        var response = await _ec2Client.RunInstancesAsync(request, ct);
        var instance = response.Reservation.Instances.First();
        
        await WaitForRunningAsync(instance.InstanceId, config.ProvisionTimeout, ct);
        
        return MapToInstanceInfo(instance, config.SpotEnabled);
    }
    
    public async Task<bool> WaitForSshReadyAsync(
        string instanceId,
        string host,
        int port,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        
        for (var i = 0; i < 30 && !cts.Token.IsCancellationRequested; i++)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(host, port, cts.Token);
                return true;
            }
            catch
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
            }
        }
        return false;
    }
}

// src/Acode.Infrastructure/Compute/Ec2/Ec2OrphanDetector.cs
namespace Acode.Infrastructure.Compute.Ec2;

public sealed class Ec2OrphanDetector : IEc2OrphanDetector
{
    private readonly IAmazonEC2 _ec2Client;
    
    public async Task<IReadOnlyList<Ec2InstanceInfo>> DetectOrphansAsync(
        string region,
        TimeSpan orphanThreshold,
        CancellationToken ct = default)
    {
        var request = new DescribeInstancesRequest
        {
            Filters =
            [
                new Filter("tag:acode", ["true"]),
                new Filter("instance-state-name", ["running", "pending"])
            ]
        };
        
        var response = await _ec2Client.DescribeInstancesAsync(request, ct);
        var cutoff = DateTimeOffset.UtcNow - orphanThreshold;
        
        return response.Reservations
            .SelectMany(r => r.Instances)
            .Where(i => i.LaunchTime < cutoff.UtcDateTime)
            .Select(MapToInstanceInfo)
            .ToList();
    }
}
```

### Implementation Checklist

| # | Requirement | Test | Impl |
|---|-------------|------|------|
| 1 | Ec2ComputeTarget implements IComputeTarget | ⬜ | ⬜ |
| 2 | AWS credentials from env/profile/IAM | ⬜ | ⬜ |
| 3 | Region configurable | ⬜ | ⬜ |
| 4 | Instance type configurable (default t3.medium) | ⬜ | ⬜ |
| 5 | AMI configurable (default Amazon Linux 2) | ⬜ | ⬜ |
| 6 | PrepareAsync provisions instance | ⬜ | ⬜ |
| 7 | Wait for running state | ⬜ | ⬜ |
| 8 | Wait for SSH ready (30 retries, 10s interval) | ⬜ | ⬜ |
| 9 | SSH target created for execution | ⬜ | ⬜ |
| 10 | Spot instances supported | ⬜ | ⬜ |
| 11 | Spot fallback to on-demand | ⬜ | ⬜ |
| 12 | Mode validation blocks local-only/airgapped | ⬜ | ⬜ |
| 13 | Tags applied (acode=true) | ⬜ | ⬜ |
| 14 | TeardownAsync terminates instance | ⬜ | ⬜ |
| 15 | Orphan detection finds old acode instances | ⬜ | ⬜ |
| 16 | Orphan cleanup terminates safely | ⬜ | ⬜ |
| 17 | Cost tracking accurate | ⬜ | ⬜ |
| 18 | Events published for lifecycle | ⬜ | ⬜ |
| 19 | Provisioning timeout enforced (5 min) | ⬜ | ⬜ |
| 20 | Secrets not logged | ⬜ | ⬜ |

### Rollout Plan

1. **Tests first**: Unit tests for config parsing, mode validation, instance type validation
2. **Domain models**: Events, Ec2InstanceInfo, Ec2PricingInfo, Ec2InstanceState
3. **Application interfaces**: IEc2InstanceProvisioner, IEc2InstanceManager, IEc2CostTracker, IEc2OrphanDetector
4. **Infrastructure impl**: Ec2ComputeTarget, Ec2ComputeTargetFactory, Ec2InstanceProvisioner
5. **AWS SDK integration**: Ec2InstanceManager, Ec2AmiResolver, Ec2SecurityGroupManager
6. **Cost tracking**: Ec2CostTracker with pricing API integration
7. **Orphan detection**: Ec2OrphanDetector with safe cleanup
8. **Integration tests**: Real EC2 provisioning (requires AWS creds)
9. **DI registration**: Register factory, provisioner, manager as scoped

**End of Task 031 Specification**