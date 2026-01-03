# Task 027: Worker Pool + Parallel Execution

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 13 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 026 (Queue), Task 005 (Git Worktrees), Task 001 (Modes)  

---

## Description

Task 027 implements the worker pool for parallel task execution. Workers claim tasks from the queue, execute them in isolation, and report results. Multiple workers MUST run concurrently.

The worker pool MUST be configurable. Worker count, isolation mode, and resource limits MUST be adjustable. Workers MUST support both local process and Docker container isolation.

Workers MUST be managed as a pool. Starting and stopping MUST be graceful. Workers MUST heartbeat to prove liveness. Failed workers MUST have tasks recovered.

### Business Value

Worker pools enable:
- Parallel task execution
- Faster completion times
- Resource utilization
- Isolation between tasks
- Scalable execution

### Scope Boundaries

This task covers pool management. Local workers are in Task 027.a. Docker workers are in Task 027.b. Log multiplexing is in Task 027.c.

### Integration Points

- Task 026: Queue provides tasks
- Task 005: Git worktrees for isolation
- Task 001: Mode affects worker types
- Task 026.c: Heartbeat and recovery

### Failure Modes

- Worker crash → Task recovered
- Resource exhaustion → Backpressure
- Docker unavailable → Fallback to local
- All workers busy → Queue backlog

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Worker | Process executing tasks |
| Pool | Collection of workers |
| Claim | Worker taking task ownership |
| Isolation | Separation between executions |
| Heartbeat | Liveness signal |
| Backpressure | Slowing on overload |
| Graceful | Clean shutdown |
| Supervisor | Worker manager |
| Executor | Task runner within worker |

---

## Out of Scope

- Remote workers
- Cloud worker scaling
- GPU workers
- Worker authentication
- Cross-machine pools
- Container orchestration

---

## Functional Requirements

### FR-001 to FR-030: Pool Management

- FR-001: `IWorkerPool` interface MUST be defined
- FR-002: `StartAsync` MUST launch workers
- FR-003: `StopAsync` MUST stop workers
- FR-004: Stop MUST be graceful by default
- FR-005: `--force` stop MUST kill immediately
- FR-006: Worker count MUST be configurable
- FR-007: Default count MUST be CPU count
- FR-008: Max count MUST be enforced
- FR-009: Min count MUST be 1
- FR-010: Workers MUST have unique IDs
- FR-011: Worker ID MUST be ULID
- FR-012: Workers MUST register on start
- FR-013: Workers MUST deregister on stop
- FR-014: Pool MUST track active workers
- FR-015: Pool MUST track worker status
- FR-016: Status: starting, idle, busy, stopping
- FR-017: Pool MUST support dynamic scaling
- FR-018: `ScaleAsync(count)` MUST adjust
- FR-019: Scale up MUST add workers
- FR-020: Scale down MUST stop idle first
- FR-021: Busy workers MUST finish current
- FR-022: Pool MUST emit events
- FR-023: Events: WorkerStarted, Stopped, Failed
- FR-024: Pool metrics MUST be available
- FR-025: Metrics: active, idle, busy counts
- FR-026: Pool MUST support isolation modes
- FR-027: Modes: process, docker
- FR-028: Default mode MUST be process
- FR-029: Mode MUST be configurable
- FR-030: Mixed modes MUST be supported

### FR-031 to FR-055: Worker Lifecycle

- FR-031: Worker MUST poll for tasks
- FR-032: Poll interval MUST be configurable
- FR-033: Default interval MUST be 1 second
- FR-034: Worker MUST claim task atomically
- FR-035: Claim MUST prevent double-claim
- FR-036: Worker MUST execute task
- FR-037: Execution MUST be isolated
- FR-038: Worker MUST report start
- FR-039: Worker MUST report completion
- FR-040: Worker MUST report failure
- FR-041: Worker MUST heartbeat during execution
- FR-042: Heartbeat MUST include task ID
- FR-043: Worker MUST respect timeout
- FR-044: Timeout MUST cancel execution
- FR-045: Worker MUST handle cancellation
- FR-046: Cancelled task MUST be marked
- FR-047: Worker MUST cleanup after task
- FR-048: Cleanup MUST release resources
- FR-049: Worker MUST loop for next task
- FR-050: Idle worker MUST reduce polling
- FR-051: Idle backoff MUST be exponential
- FR-052: Max idle backoff MUST be 30s
- FR-053: Task claim MUST reset backoff
- FR-054: Worker shutdown MUST drain queue
- FR-055: Drain timeout MUST be configurable

### FR-056 to FR-075: Resource Management

- FR-056: Worker MUST have resource limits
- FR-057: CPU limit MUST be supported
- FR-058: Memory limit MUST be supported
- FR-059: Disk limit MUST be supported
- FR-060: Limits MUST be configurable
- FR-061: Default limits MUST be reasonable
- FR-062: Resource usage MUST be tracked
- FR-063: Over-limit MUST trigger action
- FR-064: Actions: warn, throttle, kill
- FR-065: Default action MUST be warn
- FR-066: Resource metrics MUST be exported
- FR-067: Pool MUST respect system limits
- FR-068: Pool MUST not exhaust resources
- FR-069: Backpressure MUST apply when needed
- FR-070: Backpressure MUST pause new tasks
- FR-071: Backpressure MUST log
- FR-072: Recovery MUST resume tasks
- FR-073: Temp files MUST be cleaned
- FR-074: Worktrees MUST be cleaned
- FR-075: Cleanup MUST be configurable

---

## Non-Functional Requirements

- NFR-001: Worker start MUST complete in <1s
- NFR-002: Worker stop MUST complete in <10s
- NFR-003: Task claim MUST be <50ms
- NFR-004: Heartbeat MUST be <10ms
- NFR-005: Pool MUST handle 100 workers
- NFR-006: Pool MUST handle 10k tasks
- NFR-007: Memory per worker MUST be bounded
- NFR-008: CPU per worker MUST be bounded
- NFR-009: Graceful degradation required
- NFR-010: No resource leaks

---

## User Manual Documentation

### Configuration

```yaml
workers:
  count: 4
  mode: process  # process, docker
  pollIntervalMs: 1000
  idleBackoffMaxMs: 30000
  drainTimeoutSeconds: 60
  
  limits:
    cpuPercent: 25
    memoryMb: 512
    diskMb: 1024
    
  resources:
    overLimitAction: warn  # warn, throttle, kill
```

### CLI Commands

```bash
# Start worker pool
acode worker start

# Start with specific count
acode worker start --count 8

# Start with Docker isolation
acode worker start --mode docker

# Check pool status
acode worker status

# Scale workers
acode worker scale 6

# Stop workers gracefully
acode worker stop

# Force stop
acode worker stop --force

# List workers
acode worker list
```

### Worker Status

```
Worker Pool Status
==================
Mode: process
Active: 4
  - worker-abc123: busy (task xyz789)
  - worker-def456: idle
  - worker-ghi012: busy (task uvw345)
  - worker-jkl678: starting

Queue: 12 pending, 2 running
Resources: CPU 45%, Memory 1.2GB
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Pool starts workers
- [ ] AC-002: Pool stops workers
- [ ] AC-003: Worker claims tasks
- [ ] AC-004: Worker executes tasks
- [ ] AC-005: Worker reports results
- [ ] AC-006: Heartbeat works
- [ ] AC-007: Timeout enforced
- [ ] AC-008: Scaling works
- [ ] AC-009: Resource limits work
- [ ] AC-010: Backpressure works
- [ ] AC-011: Cleanup works
- [ ] AC-012: Events emitted
- [ ] AC-013: Metrics tracked
- [ ] AC-014: CLI commands work
- [ ] AC-015: Documentation complete

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Pool start/stop
- [ ] UT-002: Worker lifecycle
- [ ] UT-003: Task claiming
- [ ] UT-004: Timeout handling
- [ ] UT-005: Scaling logic

### Integration Tests

- [ ] IT-001: Full execution cycle
- [ ] IT-002: Parallel execution
- [ ] IT-003: Crash recovery
- [ ] IT-004: Resource limits

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Core/
│   └── Domain/
│       └── Workers/
│           ├── WorkerInfo.cs             # Worker state
│           ├── PoolStatus.cs             # Pool metrics
│           ├── ResourceLimits.cs         # Resource constraints
│           └── WorkerExceptions.cs       # Worker exceptions
│
├── Acode.Application/
│   └── Services/
│       └── Workers/
│           ├── IWorkerPool.cs            # Pool interface
│           ├── WorkerPool.cs             # Pool manager
│           ├── IWorker.cs                # Worker interface
│           ├── Worker.cs                 # Base worker
│           ├── ITaskExecutor.cs          # Execution interface
│           ├── TaskExecutor.cs           # Task runner
│           ├── WorkerSupervisor.cs       # Lifecycle manager
│           └── ResourceMonitor.cs        # Resource tracking
│
├── Acode.Infrastructure/
│   └── Workers/
│       ├── LocalProcessWorker.cs         # Process isolation
│       └── DockerContainerWorker.cs      # Docker isolation
│
└── Acode.Cli/
    └── Commands/
        └── Worker/
            ├── WorkerStartCommand.cs
            ├── WorkerStopCommand.cs
            ├── WorkerStatusCommand.cs
            ├── WorkerScaleCommand.cs
            └── WorkerListCommand.cs

tests/
├── Acode.Application.Tests/
│   └── Services/
│       └── Workers/
│           ├── WorkerPoolTests.cs
│           ├── WorkerSupervisorTests.cs
│           └── ResourceMonitorTests.cs
│
└── Acode.Integration.Tests/
    └── Workers/
        └── ParallelExecutionTests.cs
```

### Domain Models

```csharp
// Acode.Core/Domain/Workers/WorkerInfo.cs
namespace Acode.Core.Domain.Workers;

/// <summary>
/// Unique worker identifier.
/// </summary>
public readonly record struct WorkerId(string Value)
{
    public static WorkerId NewId() => new(Ulid.NewUlid().ToString());
    public override string ToString() => Value;
}

/// <summary>
/// Current state of a worker.
/// </summary>
public enum WorkerStatus
{
    /// <summary>Worker is starting up.</summary>
    Starting,
    
    /// <summary>Worker is waiting for tasks.</summary>
    Idle,
    
    /// <summary>Worker is executing a task.</summary>
    Busy,
    
    /// <summary>Worker is shutting down.</summary>
    Stopping,
    
    /// <summary>Worker has stopped.</summary>
    Stopped,
    
    /// <summary>Worker crashed or failed.</summary>
    Failed
}

/// <summary>
/// Information about a worker.
/// </summary>
public sealed record WorkerInfo
{
    /// <summary>Unique worker identifier.</summary>
    public required WorkerId Id { get; init; }
    
    /// <summary>Current status.</summary>
    public required WorkerStatus Status { get; init; }
    
    /// <summary>Task being executed (if Busy).</summary>
    public TaskId? CurrentTaskId { get; init; }
    
    /// <summary>When the worker started.</summary>
    public required DateTimeOffset StartedAt { get; init; }
    
    /// <summary>Last heartbeat time.</summary>
    public DateTimeOffset? LastHeartbeat { get; init; }
    
    /// <summary>Number of tasks completed.</summary>
    public int TasksCompleted { get; init; }
    
    /// <summary>Number of tasks failed.</summary>
    public int TasksFailed { get; init; }
    
    /// <summary>Isolation mode.</summary>
    public required WorkerMode Mode { get; init; }
    
    /// <summary>Current resource usage.</summary>
    public ResourceUsage? Resources { get; init; }
}

/// <summary>
/// Worker isolation mode.
/// </summary>
public enum WorkerMode
{
    /// <summary>Execute in local process.</summary>
    Process,
    
    /// <summary>Execute in Docker container.</summary>
    Docker
}

// Acode.Core/Domain/Workers/PoolStatus.cs
namespace Acode.Core.Domain.Workers;

/// <summary>
/// Overall pool status and metrics.
/// </summary>
public sealed record PoolStatus
{
    /// <summary>Whether the pool is running.</summary>
    public required bool IsRunning { get; init; }
    
    /// <summary>Total active workers.</summary>
    public required int ActiveCount { get; init; }
    
    /// <summary>Workers waiting for tasks.</summary>
    public required int IdleCount { get; init; }
    
    /// <summary>Workers executing tasks.</summary>
    public required int BusyCount { get; init; }
    
    /// <summary>Workers starting or stopping.</summary>
    public required int TransitioningCount { get; init; }
    
    /// <summary>Tasks waiting in queue.</summary>
    public required int PendingTasks { get; init; }
    
    /// <summary>Tasks currently running.</summary>
    public required int RunningTasks { get; init; }
    
    /// <summary>Aggregate resource usage.</summary>
    public required ResourceUsage Resources { get; init; }
    
    /// <summary>Pool uptime.</summary>
    public TimeSpan Uptime { get; init; }
}

/// <summary>
/// Resource usage snapshot.
/// </summary>
public sealed record ResourceUsage
{
    public double CpuPercent { get; init; }
    public long MemoryBytes { get; init; }
    public long DiskBytes { get; init; }
    
    public string MemoryFormatted => FormatBytes(MemoryBytes);
    public string DiskFormatted => FormatBytes(DiskBytes);
    
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}

// Acode.Core/Domain/Workers/ResourceLimits.cs
namespace Acode.Core.Domain.Workers;

/// <summary>
/// Resource limits for workers.
/// </summary>
public sealed record ResourceLimits
{
    /// <summary>Maximum CPU percentage per worker.</summary>
    public int CpuPercent { get; init; } = 25;
    
    /// <summary>Maximum memory in MB.</summary>
    public int MemoryMb { get; init; } = 512;
    
    /// <summary>Maximum disk in MB.</summary>
    public int DiskMb { get; init; } = 1024;
    
    /// <summary>Action when limit exceeded.</summary>
    public OverLimitAction Action { get; init; } = OverLimitAction.Warn;
    
    public static ResourceLimits Default => new();
}

public enum OverLimitAction
{
    Warn,
    Throttle,
    Kill
}

// Acode.Core/Domain/Workers/WorkerExceptions.cs
namespace Acode.Core.Domain.Workers;

public abstract class WorkerException : Exception
{
    protected WorkerException(string message) : base(message) { }
    protected WorkerException(string message, Exception inner) : base(message, inner) { }
}

public sealed class WorkerNotFoundException : WorkerException
{
    public WorkerId WorkerId { get; }
    public WorkerNotFoundException(WorkerId id) : base($"Worker not found: {id}") 
    {
        WorkerId = id;
    }
}

public sealed class PoolNotRunningException : WorkerException
{
    public PoolNotRunningException() : base("Worker pool is not running") { }
}

public sealed class WorkerStartFailedException : WorkerException
{
    public WorkerStartFailedException(string reason) : base($"Worker failed to start: {reason}") { }
    public WorkerStartFailedException(string reason, Exception inner) : base($"Worker failed to start: {reason}", inner) { }
}
```

### Worker Pool Options

```csharp
// Acode.Application/Services/Workers/WorkerPoolOptions.cs
namespace Acode.Application.Services.Workers;

/// <summary>
/// Configuration for the worker pool.
/// </summary>
public sealed record WorkerPoolOptions
{
    /// <summary>Number of workers to start.</summary>
    public int WorkerCount { get; init; } = Environment.ProcessorCount;
    
    /// <summary>Worker isolation mode.</summary>
    public WorkerMode Mode { get; init; } = WorkerMode.Process;
    
    /// <summary>Task poll interval.</summary>
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(1);
    
    /// <summary>Maximum idle backoff.</summary>
    public TimeSpan MaxIdleBackoff { get; init; } = TimeSpan.FromSeconds(30);
    
    /// <summary>Graceful shutdown timeout.</summary>
    public TimeSpan DrainTimeout { get; init; } = TimeSpan.FromMinutes(1);
    
    /// <summary>Resource limits per worker.</summary>
    public ResourceLimits Limits { get; init; } = ResourceLimits.Default;
    
    /// <summary>Heartbeat interval.</summary>
    public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(10);
    
    /// <summary>Maximum workers allowed.</summary>
    public int MaxWorkers { get; init; } = 32;
    
    /// <summary>Minimum workers.</summary>
    public int MinWorkers { get; init; } = 1;
}
```

### Worker Pool Interface

```csharp
// Acode.Application/Services/Workers/IWorkerPool.cs
namespace Acode.Application.Services.Workers;

/// <summary>
/// Manages a pool of workers for parallel task execution.
/// </summary>
public interface IWorkerPool
{
    /// <summary>
    /// Starts the worker pool.
    /// </summary>
    Task StartAsync(WorkerPoolOptions options, CancellationToken ct = default);
    
    /// <summary>
    /// Stops all workers.
    /// </summary>
    /// <param name="force">If true, kill immediately without draining.</param>
    Task StopAsync(bool force = false, CancellationToken ct = default);
    
    /// <summary>
    /// Adjusts the number of workers.
    /// </summary>
    Task ScaleAsync(int targetCount, CancellationToken ct = default);
    
    /// <summary>
    /// Gets information about all workers.
    /// </summary>
    Task<IReadOnlyList<WorkerInfo>> GetWorkersAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Gets overall pool status.
    /// </summary>
    Task<PoolStatus> GetStatusAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Whether the pool is currently running.
    /// </summary>
    bool IsRunning { get; }
}
```

### Worker Interface

```csharp
// Acode.Application/Services/Workers/IWorker.cs
namespace Acode.Application.Services.Workers;

/// <summary>
/// A worker that executes tasks.
/// </summary>
public interface IWorker : IAsyncDisposable
{
    /// <summary>Worker identifier.</summary>
    WorkerId Id { get; }
    
    /// <summary>Current status.</summary>
    WorkerStatus Status { get; }
    
    /// <summary>Current task being executed.</summary>
    TaskId? CurrentTaskId { get; }
    
    /// <summary>Starts the worker loop.</summary>
    Task StartAsync(CancellationToken ct = default);
    
    /// <summary>Signals the worker to stop.</summary>
    Task StopAsync(bool force = false, CancellationToken ct = default);
    
    /// <summary>Gets worker information.</summary>
    WorkerInfo GetInfo();
}
```

### Worker Pool Implementation

```csharp
// Acode.Application/Services/Workers/WorkerPool.cs
namespace Acode.Application.Services.Workers;

public sealed class WorkerPool : IWorkerPool
{
    private readonly IWorkerFactory _workerFactory;
    private readonly ITaskQueue _queue;
    private readonly ILogger<WorkerPool> _logger;
    
    private readonly ConcurrentDictionary<WorkerId, IWorker> _workers = new();
    private readonly SemaphoreSlim _scaleLock = new(1, 1);
    
    private WorkerPoolOptions? _options;
    private CancellationTokenSource? _poolCts;
    private DateTimeOffset _startedAt;
    
    public bool IsRunning => _poolCts != null && !_poolCts.IsCancellationRequested;
    
    public WorkerPool(
        IWorkerFactory workerFactory,
        ITaskQueue queue,
        ILogger<WorkerPool> logger)
    {
        _workerFactory = workerFactory;
        _queue = queue;
        _logger = logger;
    }
    
    public async Task StartAsync(WorkerPoolOptions options, CancellationToken ct = default)
    {
        if (IsRunning)
            throw new InvalidOperationException("Pool is already running");
        
        _options = options;
        _poolCts = new CancellationTokenSource();
        _startedAt = DateTimeOffset.UtcNow;
        
        _logger.LogInformation(
            "Starting worker pool with {Count} workers in {Mode} mode",
            options.WorkerCount, options.Mode);
        
        // Start workers in parallel
        var tasks = Enumerable.Range(0, options.WorkerCount)
            .Select(_ => StartWorkerAsync(ct))
            .ToList();
        
        await Task.WhenAll(tasks);
        
        _logger.LogInformation("Worker pool started with {Count} workers", _workers.Count);
    }
    
    public async Task StopAsync(bool force = false, CancellationToken ct = default)
    {
        if (!IsRunning)
            return;
        
        _logger.LogInformation(
            "Stopping worker pool ({Mode})",
            force ? "force" : "graceful");
        
        _poolCts!.Cancel();
        
        // Stop all workers
        var stopTasks = _workers.Values
            .Select(w => StopWorkerAsync(w, force, ct))
            .ToList();
        
        if (!force && _options != null)
        {
            using var drainCts = new CancellationTokenSource(_options.DrainTimeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, drainCts.Token);
            
            try
            {
                await Task.WhenAll(stopTasks);
            }
            catch (OperationCanceledException) when (drainCts.IsCancellationRequested)
            {
                _logger.LogWarning("Drain timeout exceeded, forcing stop");
                await ForceStopAllAsync();
            }
        }
        else
        {
            await Task.WhenAll(stopTasks);
        }
        
        _workers.Clear();
        _poolCts?.Dispose();
        _poolCts = null;
        
        _logger.LogInformation("Worker pool stopped");
    }
    
    public async Task ScaleAsync(int targetCount, CancellationToken ct = default)
    {
        if (!IsRunning)
            throw new PoolNotRunningException();
        
        await _scaleLock.WaitAsync(ct);
        try
        {
            var currentCount = _workers.Count;
            
            targetCount = Math.Clamp(targetCount, _options!.MinWorkers, _options.MaxWorkers);
            
            _logger.LogInformation("Scaling workers: {Current} → {Target}", currentCount, targetCount);
            
            if (targetCount > currentCount)
            {
                // Scale up
                var toAdd = targetCount - currentCount;
                var tasks = Enumerable.Range(0, toAdd)
                    .Select(_ => StartWorkerAsync(ct))
                    .ToList();
                await Task.WhenAll(tasks);
            }
            else if (targetCount < currentCount)
            {
                // Scale down - stop idle workers first
                var toRemove = currentCount - targetCount;
                var workersToStop = _workers.Values
                    .Where(w => w.Status == WorkerStatus.Idle)
                    .Take(toRemove)
                    .ToList();
                
                // If not enough idle, wait for busy to complete
                if (workersToStop.Count < toRemove)
                {
                    var remaining = toRemove - workersToStop.Count;
                    workersToStop.AddRange(
                        _workers.Values
                            .Where(w => w.Status == WorkerStatus.Busy)
                            .Take(remaining));
                }
                
                foreach (var worker in workersToStop)
                {
                    await StopWorkerAsync(worker, force: false, ct);
                    _workers.TryRemove(worker.Id, out _);
                }
            }
            
            _logger.LogInformation("Scaling complete: {Count} workers active", _workers.Count);
        }
        finally
        {
            _scaleLock.Release();
        }
    }
    
    public Task<IReadOnlyList<WorkerInfo>> GetWorkersAsync(CancellationToken ct = default)
    {
        var infos = _workers.Values.Select(w => w.GetInfo()).ToList();
        return Task.FromResult<IReadOnlyList<WorkerInfo>>(infos);
    }
    
    public async Task<PoolStatus> GetStatusAsync(CancellationToken ct = default)
    {
        var workers = _workers.Values.Select(w => w.GetInfo()).ToList();
        var queueCounts = await _queue.CountAsync(ct);
        
        return new PoolStatus
        {
            IsRunning = IsRunning,
            ActiveCount = workers.Count(w => w.Status != WorkerStatus.Stopped && w.Status != WorkerStatus.Failed),
            IdleCount = workers.Count(w => w.Status == WorkerStatus.Idle),
            BusyCount = workers.Count(w => w.Status == WorkerStatus.Busy),
            TransitioningCount = workers.Count(w => w.Status == WorkerStatus.Starting || w.Status == WorkerStatus.Stopping),
            PendingTasks = queueCounts.Pending,
            RunningTasks = queueCounts.Running,
            Resources = AggregateResources(workers),
            Uptime = IsRunning ? DateTimeOffset.UtcNow - _startedAt : TimeSpan.Zero
        };
    }
    
    private async Task StartWorkerAsync(CancellationToken ct)
    {
        var worker = _workerFactory.Create(_options!.Mode);
        _workers[worker.Id] = worker;
        
        try
        {
            await worker.StartAsync(ct);
            _logger.LogDebug("Worker {WorkerId} started", worker.Id);
        }
        catch (Exception ex)
        {
            _workers.TryRemove(worker.Id, out _);
            _logger.LogError(ex, "Failed to start worker {WorkerId}", worker.Id);
            throw new WorkerStartFailedException("Startup failed", ex);
        }
    }
    
    private async Task StopWorkerAsync(IWorker worker, bool force, CancellationToken ct)
    {
        try
        {
            await worker.StopAsync(force, ct);
            await worker.DisposeAsync();
            _logger.LogDebug("Worker {WorkerId} stopped", worker.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping worker {WorkerId}", worker.Id);
        }
    }
    
    private async Task ForceStopAllAsync()
    {
        foreach (var worker in _workers.Values)
        {
            try
            {
                await worker.StopAsync(force: true);
                await worker.DisposeAsync();
            }
            catch { /* ignore */ }
        }
    }
    
    private static ResourceUsage AggregateResources(IReadOnlyList<WorkerInfo> workers)
    {
        return new ResourceUsage
        {
            CpuPercent = workers.Sum(w => w.Resources?.CpuPercent ?? 0),
            MemoryBytes = workers.Sum(w => w.Resources?.MemoryBytes ?? 0),
            DiskBytes = workers.Sum(w => w.Resources?.DiskBytes ?? 0)
        };
    }
}
```

### Worker Implementation

```csharp
// Acode.Application/Services/Workers/Worker.cs
namespace Acode.Application.Services.Workers;

public class Worker : IWorker
{
    private readonly ITaskQueue _queue;
    private readonly ITaskExecutor _executor;
    private readonly WorkerPoolOptions _options;
    private readonly ILogger _logger;
    
    private CancellationTokenSource? _workerCts;
    private Task? _workerLoop;
    private int _tasksCompleted;
    private int _tasksFailed;
    private DateTimeOffset _startedAt;
    private ResourceUsage? _resources;
    
    public WorkerId Id { get; } = WorkerId.NewId();
    public WorkerStatus Status { get; private set; } = WorkerStatus.Starting;
    public TaskId? CurrentTaskId { get; private set; }
    
    public Worker(
        ITaskQueue queue,
        ITaskExecutor executor,
        WorkerPoolOptions options,
        ILogger<Worker> logger)
    {
        _queue = queue;
        _executor = executor;
        _options = options;
        _logger = logger;
    }
    
    public async Task StartAsync(CancellationToken ct = default)
    {
        _workerCts = new CancellationTokenSource();
        _startedAt = DateTimeOffset.UtcNow;
        Status = WorkerStatus.Idle;
        
        _workerLoop = RunLoopAsync(_workerCts.Token);
        
        _logger.LogInformation("Worker {WorkerId} started", Id);
    }
    
    public async Task StopAsync(bool force = false, CancellationToken ct = default)
    {
        Status = WorkerStatus.Stopping;
        _workerCts?.Cancel();
        
        if (_workerLoop != null)
        {
            if (force)
            {
                // Don't wait for graceful completion
                return;
            }
            
            try
            {
                await _workerLoop;
            }
            catch (OperationCanceledException) { }
        }
        
        Status = WorkerStatus.Stopped;
        _logger.LogInformation("Worker {WorkerId} stopped", Id);
    }
    
    public WorkerInfo GetInfo() => new()
    {
        Id = Id,
        Status = Status,
        CurrentTaskId = CurrentTaskId,
        StartedAt = _startedAt,
        LastHeartbeat = DateTimeOffset.UtcNow,
        TasksCompleted = _tasksCompleted,
        TasksFailed = _tasksFailed,
        Mode = _options.Mode,
        Resources = _resources
    };
    
    public async ValueTask DisposeAsync()
    {
        _workerCts?.Cancel();
        _workerCts?.Dispose();
        
        if (_workerLoop != null)
        {
            try { await _workerLoop; } catch { }
        }
    }
    
    private async Task RunLoopAsync(CancellationToken ct)
    {
        var backoff = _options.PollInterval;
        var heartbeatTimer = new PeriodicTimer(_options.HeartbeatInterval);
        
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Try to claim a task
                var task = await _queue.DequeueAsync(Id.Value, ct);
                
                if (task == null)
                {
                    // No task available, back off
                    Status = WorkerStatus.Idle;
                    await Task.Delay(backoff, ct);
                    
                    // Exponential backoff
                    backoff = TimeSpan.FromMilliseconds(
                        Math.Min(backoff.TotalMilliseconds * 1.5, _options.MaxIdleBackoff.TotalMilliseconds));
                    
                    continue;
                }
                
                // Reset backoff
                backoff = _options.PollInterval;
                
                // Execute task
                Status = WorkerStatus.Busy;
                CurrentTaskId = task.Id;
                
                try
                {
                    // Start heartbeat in background
                    using var heartbeatCts = new CancellationTokenSource();
                    _ = HeartbeatLoopAsync(task.Id, heartbeatCts.Token);
                    
                    var result = await _executor.ExecuteAsync(task, ct);
                    
                    heartbeatCts.Cancel();
                    
                    if (result.Success)
                    {
                        await _queue.CompleteAsync(task.Id, result, ct);
                        Interlocked.Increment(ref _tasksCompleted);
                    }
                    else
                    {
                        await _queue.FailAsync(task.Id, result.Output ?? "Unknown error", ct);
                        Interlocked.Increment(ref _tasksFailed);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Task {TaskId} execution failed", task.Id);
                    await _queue.FailAsync(task.Id, ex.Message, ct);
                    Interlocked.Increment(ref _tasksFailed);
                }
                finally
                {
                    CurrentTaskId = null;
                    Status = WorkerStatus.Idle;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker {WorkerId} loop error", Id);
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }
    
    private async Task HeartbeatLoopAsync(TaskId taskId, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_options.HeartbeatInterval, ct);
                await _queue.HeartbeatAsync(Id.Value, taskId, ct);
            }
            catch (OperationCanceledException) { break; }
            catch { /* ignore heartbeat errors */ }
        }
    }
}
```

### CLI Commands

```csharp
// Acode.Cli/Commands/Worker/WorkerStartCommand.cs
namespace Acode.Cli.Commands.Worker;

[Command("worker start", Description = "Start the worker pool")]
public sealed class WorkerStartCommand : ICommand
{
    [CommandOption("--count|c", Description = "Number of workers")]
    public int? Count { get; init; }
    
    [CommandOption("--mode|m", Description = "Isolation mode (process, docker)")]
    public string Mode { get; init; } = "process";
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var pool = GetPool(); // DI
        
        var options = new WorkerPoolOptions
        {
            WorkerCount = Count ?? Environment.ProcessorCount,
            Mode = Mode.ToLowerInvariant() switch
            {
                "docker" => WorkerMode.Docker,
                _ => WorkerMode.Process
            }
        };
        
        console.Output.WriteLine($"Starting worker pool with {options.WorkerCount} workers...");
        
        await pool.StartAsync(options);
        
        console.Output.WriteLine($"✓ Worker pool started");
        console.Output.WriteLine($"  Workers: {options.WorkerCount}");
        console.Output.WriteLine($"  Mode: {options.Mode}");
    }
}

// Acode.Cli/Commands/Worker/WorkerStatusCommand.cs
namespace Acode.Cli.Commands.Worker;

[Command("worker status", Description = "Show worker pool status")]
public sealed class WorkerStatusCommand : ICommand
{
    [CommandOption("--json", Description = "Output as JSON")]
    public bool Json { get; init; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        var pool = GetPool();
        
        var status = await pool.GetStatusAsync();
        
        if (Json)
        {
            console.Output.WriteLine(JsonSerializer.Serialize(status));
            return;
        }
        
        console.Output.WriteLine("Worker Pool Status");
        console.Output.WriteLine("==================");
        console.Output.WriteLine($"  Running: {(status.IsRunning ? "yes" : "no")}");
        console.Output.WriteLine($"  Active:  {status.ActiveCount}");
        console.Output.WriteLine($"    Idle:  {status.IdleCount}");
        console.Output.WriteLine($"    Busy:  {status.BusyCount}");
        console.Output.WriteLine();
        console.Output.WriteLine($"  Queue:");
        console.Output.WriteLine($"    Pending: {status.PendingTasks}");
        console.Output.WriteLine($"    Running: {status.RunningTasks}");
        console.Output.WriteLine();
        console.Output.WriteLine($"  Resources:");
        console.Output.WriteLine($"    CPU:    {status.Resources.CpuPercent:F1}%");
        console.Output.WriteLine($"    Memory: {status.Resources.MemoryFormatted}");
    }
}
```

### Error Codes

| Code | Meaning |
|------|---------|
| WORKER-001 | Worker not found |
| WORKER-002 | Pool not running |
| WORKER-003 | Worker start failed |
| WORKER-004 | Worker timeout |
| WORKER-005 | Resource limit exceeded |
| WORKER-006 | Docker unavailable |

### Implementation Checklist

- [ ] Create `WorkerId` value object
- [ ] Create `WorkerStatus` enum
- [ ] Create `WorkerInfo` record
- [ ] Create `WorkerMode` enum
- [ ] Create `PoolStatus` record
- [ ] Create `ResourceUsage` and `ResourceLimits` records
- [ ] Create worker exception types
- [ ] Create `WorkerPoolOptions` record
- [ ] Define `IWorkerPool` interface
- [ ] Define `IWorker` interface
- [ ] Implement `WorkerPool` with ConcurrentDictionary
- [ ] Implement `StartAsync` with parallel worker creation
- [ ] Implement `StopAsync` with graceful drain
- [ ] Implement `ScaleAsync` with idle-first stopping
- [ ] Implement `Worker` base class
- [ ] Add poll loop with exponential backoff
- [ ] Add heartbeat loop
- [ ] Add task execution and result handling
- [ ] Create `IWorkerFactory` interface
- [ ] Implement `LocalProcessWorker` (subtask 027a)
- [ ] Implement `DockerContainerWorker` (subtask 027b)
- [ ] Create CLI start command
- [ ] Create CLI stop command
- [ ] Create CLI status command
- [ ] Create CLI scale command
- [ ] Create CLI list command
- [ ] Register in DI
- [ ] Write unit tests for pool
- [ ] Write integration tests for parallel execution

### Rollout Plan

1. **Phase 1: Domain Models** (Day 1)
   - Create all records and enums
   - Create exception types
   - Unit test models

2. **Phase 2: Worker Pool** (Day 2)
   - Implement pool interface
   - Add start/stop logic
   - Add scaling logic

3. **Phase 3: Worker** (Day 2-3)
   - Implement worker loop
   - Add task claiming
   - Add heartbeat

4. **Phase 4: Process Worker** (Day 3)
   - Implement local process isolation
   - Add resource monitoring

5. **Phase 5: CLI** (Day 3)
   - All worker commands
   - Manual testing

6. **Phase 6: Integration** (Day 4)
   - Integration tests
   - Parallel execution tests
   - Documentation

---

**End of Task 027 Specification**