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