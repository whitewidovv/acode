# Task 027.b: Docker Worker Pool

**Priority:** P1 – High  
**Tier:** S – Core Infrastructure  
**Complexity:** 8 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 027 (Worker Pool), Task 027.a (Local), Task 001 (Modes)  

---

## Description

Task 027.b implements Docker container-based workers. Each worker runs in an isolated container. Containers provide stronger isolation than processes.

Docker workers MUST use a standard base image. The image MUST include .NET runtime and git. Custom images MUST be configurable. Images MUST be pulled on demand.

Container lifecycle MUST be managed. Containers MUST start and stop cleanly. Resources MUST be limited. Networking MUST be controlled. Volumes MUST mount work directories.

### Business Value

Docker workers enable:
- Strong isolation
- Reproducible environments
- Resource limits
- Network isolation
- Consistent tooling

### Scope Boundaries

This task covers Docker workers. Local workers are in Task 027.a. Pool management is in Task 027. Log multiplexing is in Task 027.c.

### Integration Points

- Task 027: Pool provides lifecycle
- Task 001: Mode affects availability
- Task 026: Queue for task claim
- Docker: Container runtime

### Failure Modes

- Docker unavailable → Fallback to local
- Image pull failure → Retry with backoff
- Container crash → Restart container
- Resource exhaustion → Kill container

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Container | Isolated runtime environment |
| Image | Container template |
| Registry | Image storage |
| Volume | Mounted directory |
| Network | Container connectivity |
| Dockerfile | Image build recipe |
| Healthcheck | Container liveness probe |

---

## Out of Scope

- Kubernetes deployment
- Docker Swarm
- Remote Docker hosts
- Custom registries auth
- GPU containers
- Windows containers

---

## Functional Requirements

### FR-001 to FR-030: Container Management

- FR-001: Docker client MUST be used
- FR-002: Docker availability MUST be checked
- FR-003: Unavailable MUST fallback gracefully
- FR-004: Container MUST use standard image
- FR-005: Default image MUST be configurable
- FR-006: Default: `mcr.microsoft.com/dotnet/sdk:8.0`
- FR-007: Custom image MUST be supported
- FR-008: Image MUST be pulled if missing
- FR-009: Pull MUST have timeout
- FR-010: Pull failure MUST retry
- FR-011: Max retries MUST be 3
- FR-012: Container MUST have unique name
- FR-013: Name format: `acode-worker-{id}`
- FR-014: Container MUST have labels
- FR-015: Labels MUST identify acode
- FR-016: Container MUST start
- FR-017: Start MUST have timeout
- FR-018: Start failure MUST be logged
- FR-019: Container MUST stop
- FR-020: Stop MUST be graceful
- FR-021: Stop timeout MUST be 10s
- FR-022: Force stop MUST kill
- FR-023: Container MUST be removed after
- FR-024: Removal MUST be optional
- FR-025: `--keep-containers` for debug
- FR-026: Container logs MUST be captured
- FR-027: Logs MUST stream during run
- FR-028: Healthcheck MUST be configured
- FR-029: Unhealthy MUST trigger restart
- FR-030: Restart policy MUST be on-failure

### FR-031 to FR-055: Resource Limits

- FR-031: CPU limit MUST be set
- FR-032: CPU default MUST be 1 core
- FR-033: Memory limit MUST be set
- FR-034: Memory default MUST be 512MB
- FR-035: Disk limit MUST be set
- FR-036: Disk via volume size
- FR-037: Network limit MUST be supported
- FR-038: PID limit MUST be set
- FR-039: PID default MUST be 100
- FR-040: OOM kill MUST be enabled
- FR-041: OOM event MUST be logged
- FR-042: Resource usage MUST be tracked
- FR-043: Stats MUST be polled
- FR-044: Stats interval MUST be 5s
- FR-045: High usage MUST warn
- FR-046: Limits MUST be configurable
- FR-047: Override per-task MAY exist
- FR-048: Task limits MUST not exceed pool
- FR-049: Resource events MUST emit
- FR-050: Metrics MUST be exported
- FR-051: Ulimits MUST be set
- FR-052: No privileged mode
- FR-053: No root user
- FR-054: Read-only root FS MAY be used
- FR-055: Security options MUST be set

### FR-056 to FR-075: Volumes and Network

- FR-056: Worktree MUST be mounted
- FR-057: Mount MUST be read-write
- FR-058: Output dir MUST be mounted
- FR-059: Temp dir MUST be mounted
- FR-060: Config MUST be mounted read-only
- FR-061: Source repo MUST be mounted
- FR-062: Mount paths MUST be configured
- FR-063: Network mode MUST be configurable
- FR-064: Default network MUST be bridge
- FR-065: Host network MUST be optional
- FR-066: No network MAY be used
- FR-067: Port mapping MUST be supported
- FR-068: DNS MUST be configurable
- FR-069: Extra hosts MUST be supported
- FR-070: Environment vars MUST be passed
- FR-071: Secrets MUST NOT be in env
- FR-072: Secrets MUST use files
- FR-073: Secret files MUST be mounted
- FR-074: Secret files MUST be read-only
- FR-075: Cleanup MUST remove volumes

---

## Non-Functional Requirements

- NFR-001: Container start MUST be <10s
- NFR-002: Container stop MUST be <15s
- NFR-003: Image pull MUST be <5min
- NFR-004: Stats poll MUST be <100ms
- NFR-005: Memory overhead MUST be <50MB
- NFR-006: No container leaks
- NFR-007: No volume leaks
- NFR-008: Graceful fallback
- NFR-009: Works with Docker/Podman
- NFR-010: No elevated host privileges

---

## User Manual Documentation

### Configuration

```yaml
workers:
  mode: docker
  count: 4
  
  docker:
    image: "mcr.microsoft.com/dotnet/sdk:8.0"
    pullPolicy: ifNotPresent  # always, never, ifNotPresent
    keepContainers: false
    network: bridge
    
    resources:
      cpus: 1.0
      memoryMb: 512
      pidsLimit: 100
      
    mounts:
      - source: /host/repo
        target: /workspace
        readOnly: false
```

### Docker Requirements

- Docker Engine 20.10+
- Docker CLI in PATH
- User in docker group (Linux)
- Sufficient disk space

### Fallback Behavior

If Docker is unavailable:
1. Warning logged
2. Fallback to local workers
3. Continue with process isolation

### Debugging

```bash
# Start with container preservation
acode worker start --mode docker --keep-containers

# View container logs
docker logs acode-worker-abc123

# Exec into running container
docker exec -it acode-worker-abc123 /bin/bash

# List acode containers
docker ps --filter "label=acode.worker=true"
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Container starts
- [ ] AC-002: Container stops
- [ ] AC-003: Image pulled
- [ ] AC-004: Volumes mounted
- [ ] AC-005: Resource limits work
- [ ] AC-006: Logs captured
- [ ] AC-007: Healthcheck works
- [ ] AC-008: Fallback works
- [ ] AC-009: No leaks
- [ ] AC-010: Security enforced
- [ ] AC-011: Metrics tracked
- [ ] AC-012: Cross-platform works

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Container config building
- [ ] UT-002: Resource limit calculation
- [ ] UT-003: Volume mount config
- [ ] UT-004: Fallback logic

### Integration Tests

- [ ] IT-001: Full container lifecycle
- [ ] IT-002: Image pull
- [ ] IT-003: Resource enforcement
- [ ] IT-004: Cleanup verification

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Domain/
│   └── Workers/
│       ├── IDockerWorker.cs
│       ├── DockerWorkerStatus.cs
│       └── ContainerInfo.cs
├── Acode.Application/
│   └── Workers/
│       ├── IDockerClient.cs
│       ├── IImageManager.cs
│       └── IContainerConfigBuilder.cs
├── Acode.Infrastructure/
│   └── Workers/
│       ├── Docker/
│       │   ├── DockerWorker.cs
│       │   ├── DockerClientWrapper.cs
│       │   ├── ImageManager.cs
│       │   ├── ContainerConfigBuilder.cs
│       │   ├── DockerAvailabilityChecker.cs
│       │   └── ContainerStatsCollector.cs
│       └── Fallback/
│           └── DockerFallbackHandler.cs
└── Acode.Cli/
    └── Commands/
        └── Worker/
            └── DockerWorkerCommand.cs
tests/
├── Acode.Infrastructure.Tests/
│   └── Workers/
│       └── Docker/
│           ├── DockerWorkerTests.cs
│           ├── ImageManagerTests.cs
│           └── ContainerConfigBuilderTests.cs
└── Acode.Integration.Tests/
    └── Workers/
        └── DockerWorkerIntegrationTests.cs
```

### Part 1: Domain Models

```csharp
// File: src/Acode.Domain/Workers/IDockerWorker.cs
namespace Acode.Domain.Workers;

/// <summary>
/// Docker container-based worker for isolated task execution.
/// Extends IWorker with container-specific capabilities.
/// </summary>
public interface IDockerWorker : IWorker
{
    /// <summary>
    /// Docker container ID when running, null otherwise.
    /// </summary>
    string? ContainerId { get; }
    
    /// <summary>
    /// Container image being used.
    /// </summary>
    string ImageName { get; }
    
    /// <summary>
    /// Whether container is currently healthy.
    /// </summary>
    bool IsHealthy { get; }
    
    /// <summary>
    /// Get current container resource statistics.
    /// </summary>
    Task<ContainerStats?> GetStatsAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Stream container logs in real-time.
    /// </summary>
    IAsyncEnumerable<LogEntry> StreamLogsAsync(CancellationToken ct = default);
}

// File: src/Acode.Domain/Workers/ContainerInfo.cs
namespace Acode.Domain.Workers;

/// <summary>
/// Information about a running container.
/// </summary>
public sealed record ContainerInfo
{
    public required string ContainerId { get; init; }
    public required string Name { get; init; }
    public required string Image { get; init; }
    public required ContainerState State { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? FinishedAt { get; init; }
    public int? ExitCode { get; init; }
    public string? Error { get; init; }
    public IReadOnlyDictionary<string, string> Labels { get; init; } = 
        new Dictionary<string, string>();
}

public enum ContainerState
{
    Created,
    Running,
    Paused,
    Restarting,
    Removing,
    Exited,
    Dead
}

/// <summary>
/// Container resource usage statistics.
/// </summary>
public sealed record ContainerStats
{
    public required string ContainerId { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    
    // CPU
    public required double CpuPercent { get; init; }
    public required long CpuUsageNanos { get; init; }
    
    // Memory
    public required long MemoryUsageBytes { get; init; }
    public required long MemoryLimitBytes { get; init; }
    public double MemoryPercent => MemoryLimitBytes > 0 
        ? (double)MemoryUsageBytes / MemoryLimitBytes * 100 
        : 0;
    
    // Network
    public required long NetworkRxBytes { get; init; }
    public required long NetworkTxBytes { get; init; }
    
    // Disk
    public required long BlockReadBytes { get; init; }
    public required long BlockWriteBytes { get; init; }
    
    // PIDs
    public required int PidsCurrent { get; init; }
    public required int PidsLimit { get; init; }
}

/// <summary>
/// Log entry from container output.
/// </summary>
public sealed record LogEntry
{
    public required DateTimeOffset Timestamp { get; init; }
    public required LogStream Stream { get; init; }
    public required string Message { get; init; }
}

public enum LogStream
{
    StdOut,
    StdErr
}
```

### Part 2: Configuration Models

```csharp
// File: src/Acode.Domain/Workers/DockerWorkerOptions.cs
namespace Acode.Domain.Workers;

/// <summary>
/// Configuration options for Docker workers.
/// </summary>
public sealed record DockerWorkerOptions
{
    /// <summary>
    /// Default container image. Default: mcr.microsoft.com/dotnet/sdk:8.0
    /// </summary>
    public string DefaultImage { get; init; } = "mcr.microsoft.com/dotnet/sdk:8.0";
    
    /// <summary>
    /// Image pull policy.
    /// </summary>
    public ImagePullPolicy PullPolicy { get; init; } = ImagePullPolicy.IfNotPresent;
    
    /// <summary>
    /// Keep containers after execution for debugging.
    /// </summary>
    public bool KeepContainers { get; init; } = false;
    
    /// <summary>
    /// Container network mode.
    /// </summary>
    public NetworkMode NetworkMode { get; init; } = NetworkMode.Bridge;
    
    /// <summary>
    /// Resource limits for containers.
    /// </summary>
    public ContainerResourceLimits Resources { get; init; } = new();
    
    /// <summary>
    /// Container start timeout.
    /// </summary>
    public TimeSpan StartTimeout { get; init; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Container stop timeout before force kill.
    /// </summary>
    public TimeSpan StopTimeout { get; init; } = TimeSpan.FromSeconds(10);
    
    /// <summary>
    /// Image pull timeout.
    /// </summary>
    public TimeSpan PullTimeout { get; init; } = TimeSpan.FromMinutes(5);
    
    /// <summary>
    /// Max retries for image pull.
    /// </summary>
    public int MaxPullRetries { get; init; } = 3;
    
    /// <summary>
    /// Stats polling interval.
    /// </summary>
    public TimeSpan StatsInterval { get; init; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Whether to fallback to local workers if Docker unavailable.
    /// </summary>
    public bool FallbackToLocal { get; init; } = true;
}

public enum ImagePullPolicy
{
    Always,
    Never,
    IfNotPresent
}

public enum NetworkMode
{
    Bridge,
    Host,
    None
}

/// <summary>
/// Resource limits for a container.
/// </summary>
public sealed record ContainerResourceLimits
{
    /// <summary>
    /// CPU limit in cores (e.g., 1.0 = 1 core, 0.5 = half core).
    /// </summary>
    public double CpuLimit { get; init; } = 1.0;
    
    /// <summary>
    /// Memory limit in megabytes.
    /// </summary>
    public int MemoryLimitMb { get; init; } = 512;
    
    /// <summary>
    /// Maximum number of PIDs.
    /// </summary>
    public int PidsLimit { get; init; } = 100;
    
    /// <summary>
    /// Enable OOM killer.
    /// </summary>
    public bool OomKillEnabled { get; init; } = true;
    
    /// <summary>
    /// Read-only root filesystem.
    /// </summary>
    public bool ReadOnlyRootFs { get; init; } = false;
}

/// <summary>
/// Volume mount configuration.
/// </summary>
public sealed record VolumeMount
{
    public required string HostPath { get; init; }
    public required string ContainerPath { get; init; }
    public bool ReadOnly { get; init; } = false;
}
```

### Part 3: Application Interfaces

```csharp
// File: src/Acode.Application/Workers/IDockerClient.cs
namespace Acode.Application.Workers;

/// <summary>
/// Docker API client abstraction.
/// </summary>
public interface IDockerClient
{
    /// <summary>
    /// Check if Docker daemon is available.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Get Docker version information.
    /// </summary>
    Task<DockerVersion> GetVersionAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Create a new container (does not start it).
    /// </summary>
    Task<string> CreateContainerAsync(
        ContainerCreateConfig config,
        CancellationToken ct = default);
    
    /// <summary>
    /// Start a created container.
    /// </summary>
    Task StartContainerAsync(
        string containerId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Stop a running container gracefully.
    /// </summary>
    Task StopContainerAsync(
        string containerId,
        TimeSpan timeout,
        CancellationToken ct = default);
    
    /// <summary>
    /// Force kill a container.
    /// </summary>
    Task KillContainerAsync(
        string containerId,
        string signal = "SIGKILL",
        CancellationToken ct = default);
    
    /// <summary>
    /// Remove a container.
    /// </summary>
    Task RemoveContainerAsync(
        string containerId,
        bool force = false,
        bool removeVolumes = true,
        CancellationToken ct = default);
    
    /// <summary>
    /// Get container information.
    /// </summary>
    Task<ContainerInfo> InspectContainerAsync(
        string containerId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Get container resource stats.
    /// </summary>
    Task<ContainerStats> GetStatsAsync(
        string containerId,
        CancellationToken ct = default);
    
    /// <summary>
    /// Stream container logs.
    /// </summary>
    IAsyncEnumerable<LogEntry> StreamLogsAsync(
        string containerId,
        bool stdout = true,
        bool stderr = true,
        bool follow = true,
        CancellationToken ct = default);
    
    /// <summary>
    /// Wait for container to exit.
    /// </summary>
    Task<ContainerWaitResult> WaitContainerAsync(
        string containerId,
        CancellationToken ct = default);
    
    /// <summary>
    /// List containers matching filter.
    /// </summary>
    Task<IReadOnlyList<ContainerInfo>> ListContainersAsync(
        ContainerListFilter? filter = null,
        CancellationToken ct = default);
}

public sealed record DockerVersion
{
    public required string Version { get; init; }
    public required string ApiVersion { get; init; }
    public required string Os { get; init; }
    public required string Arch { get; init; }
}

public sealed record ContainerWaitResult
{
    public required int ExitCode { get; init; }
    public string? Error { get; init; }
}

public sealed record ContainerListFilter
{
    public IReadOnlyDictionary<string, string>? Labels { get; init; }
    public IReadOnlyList<ContainerState>? States { get; init; }
}

// File: src/Acode.Application/Workers/IImageManager.cs
namespace Acode.Application.Workers;

/// <summary>
/// Manages Docker images.
/// </summary>
public interface IImageManager
{
    /// <summary>
    /// Check if image exists locally.
    /// </summary>
    Task<bool> ImageExistsAsync(
        string imageName,
        CancellationToken ct = default);
    
    /// <summary>
    /// Pull image from registry.
    /// </summary>
    Task PullImageAsync(
        string imageName,
        IProgress<ImagePullProgress>? progress = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Ensure image is available per pull policy.
    /// </summary>
    Task EnsureImageAsync(
        string imageName,
        ImagePullPolicy policy,
        CancellationToken ct = default);
    
    /// <summary>
    /// Get image information.
    /// </summary>
    Task<ImageInfo> InspectImageAsync(
        string imageName,
        CancellationToken ct = default);
}

public sealed record ImagePullProgress
{
    public required string Status { get; init; }
    public string? Id { get; init; }
    public long? Current { get; init; }
    public long? Total { get; init; }
}

public sealed record ImageInfo
{
    public required string Id { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public required long Size { get; init; }
    public required DateTimeOffset Created { get; init; }
}
```

*(continued in Part 4...)*

### Part 4: Container Config Builder

```csharp
// File: src/Acode.Application/Workers/IContainerConfigBuilder.cs
namespace Acode.Application.Workers;

/// <summary>
/// Builds container configuration from task and options.
/// </summary>
public interface IContainerConfigBuilder
{
    /// <summary>
    /// Build container config for a worker.
    /// </summary>
    ContainerCreateConfig BuildWorkerConfig(
        string workerId,
        string worktreePath,
        DockerWorkerOptions options);
    
    /// <summary>
    /// Build container config for a specific task.
    /// </summary>
    ContainerCreateConfig BuildTaskConfig(
        string workerId,
        QueuedTask task,
        string worktreePath,
        DockerWorkerOptions options);
}

/// <summary>
/// Full container creation configuration.
/// </summary>
public sealed record ContainerCreateConfig
{
    public required string Image { get; init; }
    public required string Name { get; init; }
    public IReadOnlyList<string> Cmd { get; init; } = [];
    public IReadOnlyList<string> Entrypoint { get; init; } = [];
    public string? WorkingDir { get; init; }
    public IReadOnlyDictionary<string, string> Env { get; init; } = 
        new Dictionary<string, string>();
    public IReadOnlyDictionary<string, string> Labels { get; init; } = 
        new Dictionary<string, string>();
    public IReadOnlyList<VolumeMount> Mounts { get; init; } = [];
    public required HostConfig HostConfig { get; init; }
    public HealthcheckConfig? Healthcheck { get; init; }
}

/// <summary>
/// Host-specific container configuration.
/// </summary>
public sealed record HostConfig
{
    // Resource limits
    public long? Memory { get; init; }
    public long? MemorySwap { get; init; }
    public long? NanoCpus { get; init; }
    public int? PidsLimit { get; init; }
    public bool OomKillDisable { get; init; }
    
    // Network
    public string NetworkMode { get; init; } = "bridge";
    public IReadOnlyList<PortBinding>? PortBindings { get; init; }
    public IReadOnlyList<string>? Dns { get; init; }
    public IReadOnlyList<string>? ExtraHosts { get; init; }
    
    // Security
    public bool Privileged { get; init; } = false;
    public bool ReadonlyRootfs { get; init; } = false;
    public string? User { get; init; }
    public IReadOnlyList<string>? SecurityOpt { get; init; }
    public IReadOnlyList<string>? CapDrop { get; init; }
    
    // Restart
    public RestartPolicy? RestartPolicy { get; init; }
    
    // Auto-remove
    public bool AutoRemove { get; init; } = false;
}

public sealed record PortBinding
{
    public required int ContainerPort { get; init; }
    public int? HostPort { get; init; }
    public string Protocol { get; init; } = "tcp";
}

public sealed record RestartPolicy
{
    public required string Name { get; init; } // "no", "on-failure", "always"
    public int MaximumRetryCount { get; init; }
}

public sealed record HealthcheckConfig
{
    public required IReadOnlyList<string> Test { get; init; }
    public TimeSpan? Interval { get; init; }
    public TimeSpan? Timeout { get; init; }
    public int? Retries { get; init; }
    public TimeSpan? StartPeriod { get; init; }
}
```

### Part 5: DockerWorker Implementation

```csharp
// File: src/Acode.Infrastructure/Workers/Docker/DockerWorker.cs
namespace Acode.Infrastructure.Workers.Docker;

/// <summary>
/// Docker container-based worker implementation.
/// </summary>
public sealed class DockerWorker : IDockerWorker, IAsyncDisposable
{
    private readonly IDockerClient _docker;
    private readonly IImageManager _images;
    private readonly IContainerConfigBuilder _configBuilder;
    private readonly ITaskQueue _queue;
    private readonly ILogger<DockerWorker> _logger;
    private readonly DockerWorkerOptions _options;
    private readonly string _worktreePath;
    
    private string? _containerId;
    private WorkerStatus _status = WorkerStatus.Idle;
    private CancellationTokenSource? _runCts;
    private Task? _runTask;
    private readonly object _lock = new();
    
    public string Id { get; }
    public string? ContainerId => _containerId;
    public string ImageName => _options.DefaultImage;
    public WorkerStatus Status => _status;
    public bool IsHealthy => _status == WorkerStatus.Running;
    
    public DockerWorker(
        string id,
        string worktreePath,
        IDockerClient docker,
        IImageManager images,
        IContainerConfigBuilder configBuilder,
        ITaskQueue queue,
        ILogger<DockerWorker> logger,
        DockerWorkerOptions options)
    {
        Id = id;
        _worktreePath = worktreePath;
        _docker = docker;
        _images = images;
        _configBuilder = configBuilder;
        _queue = queue;
        _logger = logger;
        _options = options;
    }
    
    public async Task StartAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_status != WorkerStatus.Idle)
                throw new InvalidOperationException(
                    $"Cannot start worker in state {_status}");
            _status = WorkerStatus.Starting;
        }
        
        try
        {
            // Ensure image is available
            await _images.EnsureImageAsync(
                _options.DefaultImage,
                _options.PullPolicy,
                ct);
            
            // Build container config
            var config = _configBuilder.BuildWorkerConfig(
                Id, _worktreePath, _options);
            
            // Create container
            _containerId = await _docker.CreateContainerAsync(config, ct);
            _logger.LogInformation(
                "Created container {ContainerId} for worker {WorkerId}",
                _containerId, Id);
            
            // Start container
            await _docker.StartContainerAsync(_containerId, ct);
            
            lock (_lock)
            {
                _status = WorkerStatus.Running;
            }
            
            // Start run loop
            _runCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _runTask = RunLoopAsync(_runCts.Token);
            
            _logger.LogInformation(
                "Docker worker {WorkerId} started with container {ContainerId}",
                Id, _containerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to start Docker worker {WorkerId}", Id);
            
            lock (_lock)
            {
                _status = WorkerStatus.Failed;
            }
            
            // Cleanup partial container
            if (_containerId != null)
            {
                try
                {
                    await _docker.RemoveContainerAsync(
                        _containerId, force: true);
                }
                catch { /* ignore cleanup errors */ }
            }
            
            throw;
        }
    }
    
    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Claim next task
                var task = await _queue.ClaimNextAsync(Id, ct);
                if (task == null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), ct);
                    continue;
                }
                
                _logger.LogInformation(
                    "Worker {WorkerId} claimed task {TaskId}",
                    Id, task.Id);
                
                // Execute task in container
                var result = await ExecuteTaskInContainerAsync(task, ct);
                
                // Report result
                await _queue.CompleteAsync(task.Id, result, ct);
                
                _logger.LogInformation(
                    "Worker {WorkerId} completed task {TaskId} with status {Status}",
                    Id, task.Id, result.Status);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in Docker worker {WorkerId} run loop", Id);
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }
    
    private async Task<TaskResult> ExecuteTaskInContainerAsync(
        QueuedTask task,
        CancellationToken ct)
    {
        // For container-per-task model, we exec into running container
        // or create task-specific container
        
        var startTime = DateTimeOffset.UtcNow;
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        
        try
        {
            // Stream logs during execution
            await foreach (var log in _docker.StreamLogsAsync(
                _containerId!, follow: true, ct: ct))
            {
                if (log.Stream == LogStream.StdOut)
                    stdout.AppendLine(log.Message);
                else
                    stderr.AppendLine(log.Message);
            }
            
            // Wait for container to finish task
            var waitResult = await _docker.WaitContainerAsync(_containerId!, ct);
            
            return new TaskResult
            {
                TaskId = task.Id,
                Status = waitResult.ExitCode == 0 
                    ? TaskResultStatus.Success 
                    : TaskResultStatus.Failed,
                ExitCode = waitResult.ExitCode,
                Stdout = stdout.ToString(),
                Stderr = stderr.ToString(),
                StartedAt = startTime,
                CompletedAt = DateTimeOffset.UtcNow,
                Error = waitResult.Error
            };
        }
        catch (Exception ex)
        {
            return new TaskResult
            {
                TaskId = task.Id,
                Status = TaskResultStatus.Failed,
                ExitCode = -1,
                Stdout = stdout.ToString(),
                Stderr = stderr.ToString(),
                StartedAt = startTime,
                CompletedAt = DateTimeOffset.UtcNow,
                Error = ex.Message
            };
        }
    }
    
    public async Task StopAsync(bool force = false, CancellationToken ct = default)
    {
        lock (_lock)
        {
            if (_status == WorkerStatus.Stopped)
                return;
            _status = WorkerStatus.Stopping;
        }
        
        // Cancel run loop
        _runCts?.Cancel();
        
        if (_runTask != null)
        {
            try
            {
                await _runTask.WaitAsync(TimeSpan.FromSeconds(5), ct);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning(
                    "Worker {WorkerId} run loop did not stop gracefully", Id);
            }
            catch (OperationCanceledException) { }
        }
        
        // Stop container
        if (_containerId != null)
        {
            try
            {
                if (force)
                {
                    await _docker.KillContainerAsync(_containerId, ct: ct);
                }
                else
                {
                    await _docker.StopContainerAsync(
                        _containerId, _options.StopTimeout, ct);
                }
                
                // Remove unless keeping for debug
                if (!_options.KeepContainers)
                {
                    await _docker.RemoveContainerAsync(
                        _containerId, force: true, ct: ct);
                    _logger.LogDebug(
                        "Removed container {ContainerId}", _containerId);
                }
                else
                {
                    _logger.LogInformation(
                        "Keeping container {ContainerId} for debugging",
                        _containerId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Error stopping container {ContainerId}", _containerId);
            }
        }
        
        lock (_lock)
        {
            _status = WorkerStatus.Stopped;
        }
        
        _logger.LogInformation("Docker worker {WorkerId} stopped", Id);
    }
    
    public async Task<TaskResult> ExecuteAsync(
        QueuedTask task,
        CancellationToken ct = default)
    {
        if (_status != WorkerStatus.Running)
            throw new InvalidOperationException(
                $"Worker is not running (status: {_status})");
        
        return await ExecuteTaskInContainerAsync(task, ct);
    }
    
    public async Task<ContainerStats?> GetStatsAsync(CancellationToken ct = default)
    {
        if (_containerId == null)
            return null;
        
        return await _docker.GetStatsAsync(_containerId, ct);
    }
    
    public async IAsyncEnumerable<LogEntry> StreamLogsAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (_containerId == null)
            yield break;
        
        await foreach (var entry in _docker.StreamLogsAsync(
            _containerId, follow: true, ct: ct))
        {
            yield return entry;
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        await StopAsync(force: true);
        _runCts?.Dispose();
    }
}
```

---

**End of Task 027.b Specification - Part 2/3**

### Part 6: Docker Client Wrapper

```csharp
// File: src/Acode.Infrastructure/Workers/Docker/DockerClientWrapper.cs
namespace Acode.Infrastructure.Workers.Docker;

/// <summary>
/// Wraps Docker CLI or Docker.DotNet SDK.
/// This implementation uses Docker CLI for simplicity.
/// </summary>
public sealed class DockerClientWrapper : IDockerClient
{
    private readonly ILogger<DockerClientWrapper> _logger;
    private readonly string _dockerPath;
    
    public DockerClientWrapper(ILogger<DockerClientWrapper> logger)
    {
        _logger = logger;
        _dockerPath = FindDockerExecutable();
    }
    
    private static string FindDockerExecutable()
    {
        // Check common locations
        var candidates = new[]
        {
            "docker",
            "/usr/bin/docker",
            "/usr/local/bin/docker",
            @"C:\Program Files\Docker\Docker\resources\bin\docker.exe"
        };
        
        foreach (var candidate in candidates)
        {
            try
            {
                var psi = new ProcessStartInfo(candidate, "version")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(psi);
                if (process != null)
                {
                    process.WaitForExit(5000);
                    if (process.ExitCode == 0)
                        return candidate;
                }
            }
            catch { /* try next */ }
        }
        
        return "docker"; // fallback, let it fail later
    }
    
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await RunDockerAsync(["info"], ct);
            return result.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task<DockerVersion> GetVersionAsync(CancellationToken ct = default)
    {
        var result = await RunDockerAsync(
            ["version", "--format", "{{json .}}"], ct);
        
        if (result.ExitCode != 0)
            throw new DockerException($"Failed to get version: {result.Stderr}");
        
        var json = JsonDocument.Parse(result.Stdout);
        var client = json.RootElement.GetProperty("Client");
        
        return new DockerVersion
        {
            Version = client.GetProperty("Version").GetString()!,
            ApiVersion = client.GetProperty("ApiVersion").GetString()!,
            Os = client.GetProperty("Os").GetString()!,
            Arch = client.GetProperty("Arch").GetString()!
        };
    }
    
    public async Task<string> CreateContainerAsync(
        ContainerCreateConfig config,
        CancellationToken ct = default)
    {
        var args = new List<string>
        {
            "create",
            "--name", config.Name
        };
        
        // Add labels
        foreach (var (key, value) in config.Labels)
        {
            args.Add("--label");
            args.Add($"{key}={value}");
        }
        
        // Add environment
        foreach (var (key, value) in config.Env)
        {
            args.Add("-e");
            args.Add($"{key}={value}");
        }
        
        // Add mounts
        foreach (var mount in config.Mounts)
        {
            args.Add("-v");
            var mountSpec = mount.ReadOnly
                ? $"{mount.HostPath}:{mount.ContainerPath}:ro"
                : $"{mount.HostPath}:{mount.ContainerPath}";
            args.Add(mountSpec);
        }
        
        // Add resource limits
        if (config.HostConfig.Memory.HasValue)
        {
            args.Add("--memory");
            args.Add($"{config.HostConfig.Memory}");
        }
        
        if (config.HostConfig.NanoCpus.HasValue)
        {
            args.Add("--cpus");
            args.Add($"{config.HostConfig.NanoCpus.Value / 1_000_000_000.0}");
        }
        
        if (config.HostConfig.PidsLimit.HasValue)
        {
            args.Add("--pids-limit");
            args.Add($"{config.HostConfig.PidsLimit}");
        }
        
        // Network mode
        args.Add("--network");
        args.Add(config.HostConfig.NetworkMode);
        
        // Security options
        if (!config.HostConfig.Privileged)
        {
            args.Add("--security-opt");
            args.Add("no-new-privileges:true");
        }
        
        if (config.HostConfig.ReadonlyRootfs)
        {
            args.Add("--read-only");
        }
        
        if (config.HostConfig.User != null)
        {
            args.Add("--user");
            args.Add(config.HostConfig.User);
        }
        
        // Working directory
        if (config.WorkingDir != null)
        {
            args.Add("-w");
            args.Add(config.WorkingDir);
        }
        
        // Image
        args.Add(config.Image);
        
        // Command
        args.AddRange(config.Cmd);
        
        var result = await RunDockerAsync(args, ct);
        
        if (result.ExitCode != 0)
            throw new DockerException($"Failed to create container: {result.Stderr}");
        
        return result.Stdout.Trim();
    }
    
    public async Task StartContainerAsync(
        string containerId,
        CancellationToken ct = default)
    {
        var result = await RunDockerAsync(["start", containerId], ct);
        
        if (result.ExitCode != 0)
            throw new DockerException($"Failed to start container: {result.Stderr}");
    }
    
    public async Task StopContainerAsync(
        string containerId,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        var result = await RunDockerAsync(
            ["stop", "-t", $"{(int)timeout.TotalSeconds}", containerId], ct);
        
        if (result.ExitCode != 0)
            throw new DockerException($"Failed to stop container: {result.Stderr}");
    }
    
    public async Task KillContainerAsync(
        string containerId,
        string signal = "SIGKILL",
        CancellationToken ct = default)
    {
        var result = await RunDockerAsync(
            ["kill", "-s", signal, containerId], ct);
        
        if (result.ExitCode != 0)
            throw new DockerException($"Failed to kill container: {result.Stderr}");
    }
    
    public async Task RemoveContainerAsync(
        string containerId,
        bool force = false,
        bool removeVolumes = true,
        CancellationToken ct = default)
    {
        var args = new List<string> { "rm" };
        if (force) args.Add("-f");
        if (removeVolumes) args.Add("-v");
        args.Add(containerId);
        
        var result = await RunDockerAsync(args, ct);
        
        if (result.ExitCode != 0)
            throw new DockerException($"Failed to remove container: {result.Stderr}");
    }
    
    public async Task<ContainerInfo> InspectContainerAsync(
        string containerId,
        CancellationToken ct = default)
    {
        var result = await RunDockerAsync(
            ["inspect", "--format", "{{json .}}", containerId], ct);
        
        if (result.ExitCode != 0)
            throw new DockerException($"Failed to inspect container: {result.Stderr}");
        
        var json = JsonDocument.Parse(result.Stdout);
        var root = json.RootElement;
        var state = root.GetProperty("State");
        
        return new ContainerInfo
        {
            ContainerId = root.GetProperty("Id").GetString()!,
            Name = root.GetProperty("Name").GetString()!.TrimStart('/'),
            Image = root.GetProperty("Config").GetProperty("Image").GetString()!,
            State = ParseContainerState(state.GetProperty("Status").GetString()!),
            CreatedAt = DateTimeOffset.Parse(root.GetProperty("Created").GetString()!),
            StartedAt = ParseNullableDateTime(state, "StartedAt"),
            FinishedAt = ParseNullableDateTime(state, "FinishedAt"),
            ExitCode = state.GetProperty("ExitCode").GetInt32(),
            Error = state.TryGetProperty("Error", out var err) ? err.GetString() : null
        };
    }
    
    public async Task<ContainerStats> GetStatsAsync(
        string containerId,
        CancellationToken ct = default)
    {
        var result = await RunDockerAsync(
            ["stats", "--no-stream", "--format", "{{json .}}", containerId], ct);
        
        if (result.ExitCode != 0)
            throw new DockerException($"Failed to get stats: {result.Stderr}");
        
        var json = JsonDocument.Parse(result.Stdout);
        var root = json.RootElement;
        
        return new ContainerStats
        {
            ContainerId = containerId,
            Timestamp = DateTimeOffset.UtcNow,
            CpuPercent = ParsePercentage(root.GetProperty("CPUPerc").GetString()!),
            CpuUsageNanos = 0, // Not available from docker stats CLI
            MemoryUsageBytes = ParseMemorySize(
                root.GetProperty("MemUsage").GetString()!.Split('/')[0].Trim()),
            MemoryLimitBytes = ParseMemorySize(
                root.GetProperty("MemUsage").GetString()!.Split('/')[1].Trim()),
            NetworkRxBytes = 0, // Parse from NetIO if needed
            NetworkTxBytes = 0,
            BlockReadBytes = 0, // Parse from BlockIO if needed
            BlockWriteBytes = 0,
            PidsCurrent = int.TryParse(root.GetProperty("PIDs").GetString(), out var p) ? p : 0,
            PidsLimit = 0 // Not available from docker stats
        };
    }
    
    public async IAsyncEnumerable<LogEntry> StreamLogsAsync(
        string containerId,
        bool stdout = true,
        bool stderr = true,
        bool follow = true,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var args = new List<string> { "logs", "--timestamps" };
        if (follow) args.Add("-f");
        args.Add(containerId);
        
        var psi = new ProcessStartInfo(_dockerPath)
        {
            RedirectStandardOutput = stdout,
            RedirectStandardError = stderr,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);
        
        using var process = Process.Start(psi)!;
        
        // Read stdout
        if (stdout && process.StandardOutput != null)
        {
            while (!ct.IsCancellationRequested)
            {
                var line = await process.StandardOutput.ReadLineAsync(ct);
                if (line == null) break;
                
                yield return ParseLogLine(line, LogStream.StdOut);
            }
        }
    }
    
    public async Task<ContainerWaitResult> WaitContainerAsync(
        string containerId,
        CancellationToken ct = default)
    {
        var result = await RunDockerAsync(["wait", containerId], ct);
        
        return new ContainerWaitResult
        {
            ExitCode = int.TryParse(result.Stdout.Trim(), out var code) ? code : -1,
            Error = result.ExitCode != 0 ? result.Stderr : null
        };
    }
    
    public async Task<IReadOnlyList<ContainerInfo>> ListContainersAsync(
        ContainerListFilter? filter = null,
        CancellationToken ct = default)
    {
        var args = new List<string> { "ps", "-a", "--format", "{{json .}}" };
        
        if (filter?.Labels != null)
        {
            foreach (var (key, value) in filter.Labels)
            {
                args.Add("--filter");
                args.Add($"label={key}={value}");
            }
        }
        
        var result = await RunDockerAsync(args, ct);
        
        if (result.ExitCode != 0)
            throw new DockerException($"Failed to list containers: {result.Stderr}");
        
        var containers = new List<ContainerInfo>();
        foreach (var line in result.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var json = JsonDocument.Parse(line);
            var root = json.RootElement;
            
            containers.Add(new ContainerInfo
            {
                ContainerId = root.GetProperty("ID").GetString()!,
                Name = root.GetProperty("Names").GetString()!,
                Image = root.GetProperty("Image").GetString()!,
                State = ParseContainerState(root.GetProperty("State").GetString()!),
                CreatedAt = DateTimeOffset.UtcNow // Simplified
            });
        }
        
        return containers;
    }
    
    private async Task<DockerResult> RunDockerAsync(
        IEnumerable<string> args,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo(_dockerPath)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        foreach (var arg in args)
            psi.ArgumentList.Add(arg);
        
        _logger.LogDebug("Running: docker {Args}", string.Join(" ", args));
        
        using var process = Process.Start(psi)!;
        
        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);
        
        await process.WaitForExitAsync(ct);
        
        return new DockerResult(
            process.ExitCode,
            await stdoutTask,
            await stderrTask);
    }
    
    private static ContainerState ParseContainerState(string state) => state.ToLower() switch
    {
        "created" => ContainerState.Created,
        "running" => ContainerState.Running,
        "paused" => ContainerState.Paused,
        "restarting" => ContainerState.Restarting,
        "removing" => ContainerState.Removing,
        "exited" => ContainerState.Exited,
        "dead" => ContainerState.Dead,
        _ => ContainerState.Exited
    };
    
    private static DateTimeOffset? ParseNullableDateTime(JsonElement element, string prop)
    {
        if (!element.TryGetProperty(prop, out var val))
            return null;
        
        var str = val.GetString();
        if (string.IsNullOrEmpty(str) || str.StartsWith("0001"))
            return null;
        
        return DateTimeOffset.Parse(str);
    }
    
    private static double ParsePercentage(string value) =>
        double.TryParse(value.TrimEnd('%'), out var d) ? d : 0;
    
    private static long ParseMemorySize(string value)
    {
        value = value.Trim().ToUpper();
        var multiplier = 1L;
        
        if (value.EndsWith("GIB")) { multiplier = 1024 * 1024 * 1024; value = value[..^3]; }
        else if (value.EndsWith("MIB")) { multiplier = 1024 * 1024; value = value[..^3]; }
        else if (value.EndsWith("KIB")) { multiplier = 1024; value = value[..^3]; }
        else if (value.EndsWith("GB")) { multiplier = 1000 * 1000 * 1000; value = value[..^2]; }
        else if (value.EndsWith("MB")) { multiplier = 1000 * 1000; value = value[..^2]; }
        else if (value.EndsWith("KB")) { multiplier = 1000; value = value[..^2]; }
        else if (value.EndsWith("B")) { value = value[..^1]; }
        
        return (long)(double.Parse(value.Trim()) * multiplier);
    }
    
    private static LogEntry ParseLogLine(string line, LogStream stream)
    {
        // Format: 2024-01-15T10:30:00.000000000Z message
        var spaceIndex = line.IndexOf(' ');
        if (spaceIndex > 0 && DateTimeOffset.TryParse(line[..spaceIndex], out var ts))
        {
            return new LogEntry
            {
                Timestamp = ts,
                Stream = stream,
                Message = line[(spaceIndex + 1)..]
            };
        }
        
        return new LogEntry
        {
            Timestamp = DateTimeOffset.UtcNow,
            Stream = stream,
            Message = line
        };
    }
    
    private sealed record DockerResult(int ExitCode, string Stdout, string Stderr);
}

public class DockerException : Exception
{
    public DockerException(string message) : base(message) { }
    public DockerException(string message, Exception inner) : base(message, inner) { }
}
```

### Part 7: Fallback Handler & Image Manager

```csharp
// File: src/Acode.Infrastructure/Workers/Docker/ImageManager.cs
namespace Acode.Infrastructure.Workers.Docker;

public sealed class ImageManager : IImageManager
{
    private readonly IDockerClient _docker;
    private readonly ILogger<ImageManager> _logger;
    private readonly DockerWorkerOptions _options;
    
    public ImageManager(
        IDockerClient docker,
        ILogger<ImageManager> logger,
        DockerWorkerOptions options)
    {
        _docker = docker;
        _logger = logger;
        _options = options;
    }
    
    public async Task<bool> ImageExistsAsync(
        string imageName,
        CancellationToken ct = default)
    {
        try
        {
            await InspectImageAsync(imageName, ct);
            return true;
        }
        catch
        {
            return false;
        }
    }
    
    public async Task PullImageAsync(
        string imageName,
        IProgress<ImagePullProgress>? progress = null,
        CancellationToken ct = default)
    {
        var attempts = 0;
        var maxAttempts = _options.MaxPullRetries;
        
        while (true)
        {
            attempts++;
            try
            {
                _logger.LogInformation(
                    "Pulling image {Image} (attempt {Attempt}/{Max})",
                    imageName, attempts, maxAttempts);
                
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(_options.PullTimeout);
                
                // Use docker pull CLI
                var psi = new ProcessStartInfo("docker", $"pull {imageName}")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(psi)!;
                
                // Report progress
                while (!process.StandardOutput.EndOfStream)
                {
                    var line = await process.StandardOutput.ReadLineAsync(cts.Token);
                    if (line != null)
                    {
                        progress?.Report(new ImagePullProgress { Status = line });
                    }
                }
                
                await process.WaitForExitAsync(cts.Token);
                
                if (process.ExitCode != 0)
                {
                    var error = await process.StandardError.ReadToEndAsync(cts.Token);
                    throw new DockerException($"Image pull failed: {error}");
                }
                
                _logger.LogInformation("Successfully pulled image {Image}", imageName);
                return;
            }
            catch (Exception ex) when (attempts < maxAttempts)
            {
                _logger.LogWarning(ex,
                    "Image pull attempt {Attempt} failed, retrying...", attempts);
                
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempts));
                await Task.Delay(delay, ct);
            }
        }
    }
    
    public async Task EnsureImageAsync(
        string imageName,
        ImagePullPolicy policy,
        CancellationToken ct = default)
    {
        var exists = await ImageExistsAsync(imageName, ct);
        
        switch (policy)
        {
            case ImagePullPolicy.Always:
                await PullImageAsync(imageName, ct: ct);
                break;
                
            case ImagePullPolicy.IfNotPresent:
                if (!exists)
                    await PullImageAsync(imageName, ct: ct);
                break;
                
            case ImagePullPolicy.Never:
                if (!exists)
                    throw new DockerException(
                        $"Image {imageName} not found and pull policy is Never");
                break;
        }
    }
    
    public async Task<ImageInfo> InspectImageAsync(
        string imageName,
        CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo("docker", $"inspect --format {{{{json .}}}} {imageName}")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using var process = Process.Start(psi)!;
        var output = await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        
        if (process.ExitCode != 0)
            throw new DockerException($"Image {imageName} not found");
        
        var json = JsonDocument.Parse(output);
        var root = json.RootElement[0];
        
        return new ImageInfo
        {
            Id = root.GetProperty("Id").GetString()!,
            Tags = root.GetProperty("RepoTags").EnumerateArray()
                .Select(t => t.GetString()!)
                .ToList(),
            Size = root.GetProperty("Size").GetInt64(),
            Created = DateTimeOffset.Parse(root.GetProperty("Created").GetString()!)
        };
    }
}

// File: src/Acode.Infrastructure/Workers/Fallback/DockerFallbackHandler.cs
namespace Acode.Infrastructure.Workers.Fallback;

/// <summary>
/// Handles fallback to local workers when Docker unavailable.
/// </summary>
public sealed class DockerFallbackHandler
{
    private readonly IDockerClient _docker;
    private readonly ILocalWorkerFactory _localWorkerFactory;
    private readonly ILogger<DockerFallbackHandler> _logger;
    private readonly DockerWorkerOptions _options;
    
    private bool? _dockerAvailable;
    
    public DockerFallbackHandler(
        IDockerClient docker,
        ILocalWorkerFactory localWorkerFactory,
        ILogger<DockerFallbackHandler> logger,
        DockerWorkerOptions options)
    {
        _docker = docker;
        _localWorkerFactory = localWorkerFactory;
        _logger = logger;
        _options = options;
    }
    
    /// <summary>
    /// Check Docker availability and determine worker type.
    /// </summary>
    public async Task<WorkerCreationResult> ResolveWorkerAsync(
        string workerId,
        string worktreePath,
        CancellationToken ct = default)
    {
        // Check Docker availability (cache result)
        _dockerAvailable ??= await _docker.IsAvailableAsync(ct);
        
        if (_dockerAvailable.Value)
        {
            return new WorkerCreationResult
            {
                UseDocker = true,
                FallbackReason = null
            };
        }
        
        if (!_options.FallbackToLocal)
        {
            throw new DockerException(
                "Docker is not available and fallback is disabled");
        }
        
        _logger.LogWarning(
            "Docker unavailable, falling back to local worker for {WorkerId}",
            workerId);
        
        return new WorkerCreationResult
        {
            UseDocker = false,
            FallbackReason = "Docker daemon not available",
            LocalWorker = _localWorkerFactory.Create(workerId, worktreePath)
        };
    }
}

public sealed record WorkerCreationResult
{
    public required bool UseDocker { get; init; }
    public string? FallbackReason { get; init; }
    public ILocalWorker? LocalWorker { get; init; }
}
```

### Implementation Checklist

- [ ] Create `IDockerWorker` interface extending `IWorker`
- [ ] Create `ContainerInfo`, `ContainerStats`, `LogEntry` domain models
- [ ] Create `DockerWorkerOptions` with all configuration
- [ ] Create `IDockerClient` interface with full container lifecycle
- [ ] Create `IImageManager` interface for image operations
- [ ] Create `IContainerConfigBuilder` for config generation
- [ ] Implement `DockerClientWrapper` using Docker CLI
- [ ] Implement `DockerWorker` with run loop and task execution
- [ ] Implement `ImageManager` with pull policy and retry logic
- [ ] Implement `ContainerConfigBuilder` with all settings
- [ ] Implement `DockerFallbackHandler` for graceful degradation
- [ ] Add standard acode labels to containers
- [ ] Implement resource limits (CPU, memory, PIDs)
- [ ] Implement volume mounts for worktree
- [ ] Implement log streaming
- [ ] Implement stats collection
- [ ] Add container cleanup on stop
- [ ] Add `--keep-containers` debug option
- [ ] Write unit tests for config builder
- [ ] Write integration tests for container lifecycle
- [ ] Test fallback behavior

### Rollout Plan

1. **Day 1**: Domain models and interfaces
2. **Day 2**: DockerClientWrapper implementation
3. **Day 3**: ImageManager with pull/retry
4. **Day 4**: ContainerConfigBuilder
5. **Day 5**: DockerWorker implementation
6. **Day 6**: Fallback handler
7. **Day 7**: Integration tests
8. **Day 8**: CLI commands and debugging tools

---

**End of Task 027.b Specification**