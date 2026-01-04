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