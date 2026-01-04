# Task 027.a: Local Worker Pool

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 6 – Execution Layer  
**Dependencies:** Task 027 (Worker Pool), Task 005 (Worktrees)  

---

## Description

Task 027.a implements local process-based workers. Each worker runs as a separate process on the local machine. Workers MUST be isolated using git worktrees.

Local workers MUST execute .NET code directly. Each worker MUST have its own working directory. Workers MUST share the same database connection for queue access.

Process management MUST be robust. Workers MUST start and stop cleanly. Crashed workers MUST be detected and restarted. Orphaned processes MUST be cleaned up.

### Business Value

Local workers enable:
- Fast execution (no container overhead)
- Simple deployment
- Direct debugging
- Low resource usage
- Works everywhere

### Scope Boundaries

This task covers local workers only. Docker workers are in Task 027.b. Pool management is in Task 027. Log multiplexing is in Task 027.c.

### Integration Points

- Task 027: Pool provides lifecycle
- Task 005: Worktrees for isolation
- Task 026: Queue for task claim
- Task 026.c: Heartbeat integration

### Failure Modes

- Process crash → Auto-restart
- Resource exhaustion → Backpressure
- Worktree conflict → Retry with new tree
- Cleanup failure → Log and continue

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Process Worker | Worker as OS process |
| Worktree | Isolated git checkout |
| Process ID | OS process identifier |
| Exit Code | Process termination status |
| Spawn | Create new process |
| Kill | Terminate process |
| Orphan | Process without parent |

---

## Out of Scope

- Remote process execution
- SSH worker execution
- Service account isolation
- Process sandboxing
- AppDomain isolation

---

## Functional Requirements

### FR-001 to FR-030: Process Management

- FR-001: Worker MUST run as process
- FR-002: Process MUST be spawned by pool
- FR-003: Spawn MUST pass worker ID
- FR-004: Spawn MUST pass config path
- FR-005: Process MUST inherit environment
- FR-006: Sensitive env MUST be filtered
- FR-007: Process MUST have working dir
- FR-008: Working dir MUST be worktree
- FR-009: Worktree MUST be created on start
- FR-010: Worktree MUST be unique per worker
- FR-011: Process MUST exit cleanly on stop
- FR-012: Clean exit MUST return 0
- FR-013: Error exit MUST return non-zero
- FR-014: Exit codes MUST be documented
- FR-015: Supervisor MUST track processes
- FR-016: Supervisor MUST detect exits
- FR-017: Unexpected exit MUST be logged
- FR-018: Crash MUST trigger recovery
- FR-019: Recovery MUST restart worker
- FR-020: Restart MUST have delay
- FR-021: Delay MUST be exponential
- FR-022: Max delay MUST be 60 seconds
- FR-023: Restart count MUST be tracked
- FR-024: Max restarts MUST be enforced
- FR-025: Over-limit MUST fail worker
- FR-026: Failed worker MUST be replaced
- FR-027: Kill MUST use SIGTERM first
- FR-028: SIGTERM timeout MUST be 10s
- FR-029: SIGKILL MUST follow timeout
- FR-030: Kill MUST be logged

### FR-031 to FR-055: Execution Environment

- FR-031: Worker MUST have isolated worktree
- FR-032: Worktree MUST be task-specific
- FR-033: Task worktree MUST be created
- FR-034: Task worktree MUST be cleaned after
- FR-035: Cleanup MUST be optional (for debug)
- FR-036: `--keep-worktrees` MUST preserve
- FR-037: Worker MUST have temp directory
- FR-038: Temp MUST be worker-specific
- FR-039: Temp MUST be cleaned on stop
- FR-040: Worker MUST have log file
- FR-041: Log MUST be worker-specific
- FR-042: Log MUST be rotated
- FR-043: Log MUST be accessible
- FR-044: Worker MUST set task env vars
- FR-045: ACODE_TASK_ID MUST be set
- FR-046: ACODE_WORKER_ID MUST be set
- FR-047: ACODE_WORKTREE_PATH MUST be set
- FR-048: Worker MUST have access to tools
- FR-049: dotnet MUST be accessible
- FR-050: git MUST be accessible
- FR-051: PATH MUST include tools
- FR-052: Worker MUST validate tools
- FR-053: Missing tool MUST fail start
- FR-054: Tool version MUST be logged
- FR-055: Tool errors MUST be clear

### FR-056 to FR-070: Task Execution

- FR-056: Worker MUST execute task
- FR-057: Execution MUST be in worktree
- FR-058: Build MUST run if needed
- FR-059: Tests MUST run if configured
- FR-060: Timeout MUST be enforced
- FR-061: Timeout MUST kill process tree
- FR-062: Output MUST be captured
- FR-063: Stdout MUST be logged
- FR-064: Stderr MUST be logged
- FR-065: Exit code MUST be captured
- FR-066: Artifacts MUST be collected
- FR-067: Results MUST be reported
- FR-068: Success MUST complete task
- FR-069: Failure MUST fail task
- FR-070: Cleanup MUST always run

---

## Non-Functional Requirements

- NFR-001: Process spawn MUST be <500ms
- NFR-002: Process kill MUST be <15s
- NFR-003: Worktree create MUST be <5s
- NFR-004: Cleanup MUST be <5s
- NFR-005: Memory overhead MUST be <100MB
- NFR-006: No zombie processes
- NFR-007: No orphaned worktrees
- NFR-008: Graceful on system shutdown
- NFR-009: Works on Windows/Linux/macOS
- NFR-010: No elevated privileges needed

---

## User Manual Documentation

### Configuration

```yaml
workers:
  mode: process
  count: 4
  
  process:
    restartDelayMs: 1000
    maxRestartDelayMs: 60000
    maxRestarts: 10
    killTimeoutSeconds: 10
    keepWorktrees: false
    
  worktree:
    baseDir: ".agent/worktrees"
    cleanupOnExit: true
```

### Environment Variables

| Variable | Description |
|----------|-------------|
| ACODE_TASK_ID | Current task identifier |
| ACODE_WORKER_ID | Worker identifier |
| ACODE_WORKTREE_PATH | Path to task worktree |
| ACODE_CONFIG_PATH | Path to config file |

### Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Clean exit |
| 1 | Task failed |
| 2 | Configuration error |
| 3 | Tool not found |
| 137 | Killed (SIGKILL) |
| 143 | Terminated (SIGTERM) |

### Debugging

```bash
# Start with worktree preservation
acode worker start --keep-worktrees

# View worker logs
acode worker logs worker-abc123

# Attach to worker process
acode worker attach worker-abc123
```

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Process spawns correctly
- [ ] AC-002: Process stops cleanly
- [ ] AC-003: Crash restarts worker
- [ ] AC-004: Worktree created
- [ ] AC-005: Worktree cleaned
- [ ] AC-006: Environment vars set
- [ ] AC-007: Task executes
- [ ] AC-008: Output captured
- [ ] AC-009: Exit code captured
- [ ] AC-010: Timeout enforced
- [ ] AC-011: No zombies
- [ ] AC-012: Cross-platform works

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Process spawn
- [ ] UT-002: Process kill
- [ ] UT-003: Restart logic
- [ ] UT-004: Worktree management
- [ ] UT-005: Environment setup

### Integration Tests

- [ ] IT-001: Full execution cycle
- [ ] IT-002: Crash recovery
- [ ] IT-003: Timeout handling
- [ ] IT-004: Cleanup verification

---

## Implementation Prompt

### File Structure

```
src/
├── Acode.Application/
│   └── Services/
│       └── Workers/
│           ├── Local/
│           │   ├── ILocalWorker.cs           # Worker interface
│           │   ├── LocalWorker.cs            # Implementation
│           │   ├── IProcessSupervisor.cs     # Process management
│           │   ├── ProcessSupervisor.cs      # Implementation
│           │   ├── LocalWorkerFactory.cs     # Worker creation
│           │   └── WorkerEnvironment.cs      # Env var setup
│           └── Worktrees/
│               ├── IWorkerWorktreeManager.cs
│               └── WorkerWorktreeManager.cs

tests/
└── Acode.Application.Tests/
    └── Services/
        └── Workers/
            ├── LocalWorkerTests.cs
            └── ProcessSupervisorTests.cs
```

### Local Worker Interface

```csharp
// Acode.Application/Services/Workers/Local/ILocalWorker.cs
namespace Acode.Application.Services.Workers.Local;

/// <summary>
/// A worker that executes tasks as a local process.
/// </summary>
public interface ILocalWorker : IWorker
{
    /// <summary>
    /// OS process ID of the worker.
    /// </summary>
    int? ProcessId { get; }
    
    /// <summary>
    /// Path to the worker's worktree.
    /// </summary>
    string? WorktreePath { get; }
    
    /// <summary>
    /// Number of times this worker has been restarted.
    /// </summary>
    int RestartCount { get; }
}
```

### Local Worker Implementation

```csharp
// Acode.Application/Services/Workers/Local/LocalWorker.cs
namespace Acode.Application.Services.Workers.Local;

public sealed class LocalWorker : ILocalWorker, IDisposable
{
    private readonly IProcessSupervisor _supervisor;
    private readonly IWorkerWorktreeManager _worktreeManager;
    private readonly IWorkerHeartbeat _heartbeat;
    private readonly ITaskQueue _queue;
    private readonly IStateMachine _stateMachine;
    private readonly ILogger<LocalWorker> _logger;
    private readonly LocalWorkerOptions _options;
    
    private Process? _process;
    private CancellationTokenSource? _cts;
    private Task? _runLoop;
    private int _restartCount;
    private TimeSpan _currentRestartDelay;
    
    public WorkerId Id { get; }
    public int? ProcessId => _process?.Id;
    public string? WorktreePath { get; private set; }
    public WorkerStatus Status { get; private set; }
    public int RestartCount => _restartCount;
    
    public LocalWorker(
        WorkerId id,
        IProcessSupervisor supervisor,
        IWorkerWorktreeManager worktreeManager,
        IWorkerHeartbeat heartbeat,
        ITaskQueue queue,
        IStateMachine stateMachine,
        IOptions<LocalWorkerOptions> options,
        ILogger<LocalWorker> logger)
    {
        Id = id;
        _supervisor = supervisor;
        _worktreeManager = worktreeManager;
        _heartbeat = heartbeat;
        _queue = queue;
        _stateMachine = stateMachine;
        _options = options.Value;
        _logger = logger;
        Status = WorkerStatus.Idle;
        _currentRestartDelay = TimeSpan.FromMilliseconds(_options.RestartDelayMs);
    }
    
    public async Task StartAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting worker {WorkerId}", Id);
        
        // Create worker worktree
        WorktreePath = await _worktreeManager.CreateWorktreeAsync(Id.Value, ct: ct);
        _logger.LogDebug("Created worktree: {Path}", WorktreePath);
        
        // Validate tools
        await ValidateToolsAsync(ct);
        
        // Start the run loop
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _runLoop = RunLoopAsync(_cts.Token);
        
        Status = WorkerStatus.Idle;
    }
    
    public async Task StopAsync(bool force = false, CancellationToken ct = default)
    {
        _logger.LogInformation("Stopping worker {WorkerId} (force={Force})", Id, force);
        Status = WorkerStatus.Stopping;
        
        // Cancel the run loop
        _cts?.Cancel();
        
        if (_process != null && !_process.HasExited)
        {
            if (force)
            {
                _process.Kill(entireProcessTree: true);
            }
            else
            {
                await _supervisor.KillAsync(
                    _process.Id,
                    TimeSpan.FromSeconds(_options.KillTimeoutSeconds),
                    ct);
            }
        }
        
        // Wait for run loop to complete
        if (_runLoop != null)
        {
            try
            {
                await _runLoop.WaitAsync(TimeSpan.FromSeconds(5), ct);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Worker run loop did not complete in time");
            }
        }
        
        // Cleanup worktree
        if (!_options.KeepWorktrees && WorktreePath != null)
        {
            await _worktreeManager.RemoveWorktreeAsync(WorktreePath, ct);
            _logger.LogDebug("Removed worktree: {Path}", WorktreePath);
        }
        
        Status = WorkerStatus.Stopped;
    }
    
    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Send heartbeat
                await _heartbeat.SendHeartbeatAsync(Id, null, ct);
                
                // Try to claim a task
                var task = await _queue.DequeueAsync(ct);
                
                if (task == null)
                {
                    // No task available, wait and retry
                    await Task.Delay(_options.PollIntervalMs, ct);
                    continue;
                }
                
                // Execute task
                Status = WorkerStatus.Busy;
                _logger.LogInformation("Worker {WorkerId} claimed task {TaskId}", Id, task.Spec.Id);
                
                // Transition to running
                await _stateMachine.TransitionAsync(
                    task.Spec.Id,
                    TaskStatus.Running,
                    TransitionActor.Worker(Id),
                    "Worker claimed task",
                    new TransitionContext { WorkerId = Id },
                    ct);
                
                var result = await ExecuteTaskAsync(task, ct);
                
                // Report result
                if (result.Success)
                {
                    await _stateMachine.TransitionAsync(
                        task.Spec.Id,
                        TaskStatus.Completed,
                        TransitionActor.Worker(Id),
                        "Task completed successfully",
                        new TransitionContext { Result = result },
                        ct);
                }
                else
                {
                    await _stateMachine.TransitionAsync(
                        task.Spec.Id,
                        TaskStatus.Failed,
                        TransitionActor.Worker(Id),
                        "Task failed",
                        new TransitionContext { Error = result.ErrorMessage },
                        ct);
                }
                
                Status = WorkerStatus.Idle;
                _restartCount = 0; // Reset on successful execution
                _currentRestartDelay = TimeSpan.FromMilliseconds(_options.RestartDelayMs);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker {WorkerId} crashed", Id);
                await HandleCrashAsync(ct);
            }
        }
    }
    
    private async Task<TaskResult> ExecuteTaskAsync(QueuedTask task, CancellationToken ct)
    {
        // Create task-specific worktree
        var taskWorktree = await _worktreeManager.CreateWorktreeAsync(
            $"{Id.Value}-{task.Spec.Id.Value}", ct: ct);
        
        try
        {
            // Set up environment
            var env = new Dictionary<string, string>
            {
                ["ACODE_TASK_ID"] = task.Spec.Id.Value,
                ["ACODE_WORKER_ID"] = Id.Value,
                ["ACODE_WORKTREE_PATH"] = taskWorktree,
                ["ACODE_CONFIG_PATH"] = _options.ConfigPath
            };
            
            // Spawn execution process
            var pid = await _supervisor.SpawnAsync(
                _options.ExecutablePath,
                new[] { "execute", "--task", task.Spec.Id.Value },
                taskWorktree,
                env,
                ct);
            
            _process = Process.GetProcessById(pid);
            
            // Wait for completion with heartbeat
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(task.Spec.Timeout));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
            
            var heartbeatTask = HeartbeatLoopAsync(task.Spec.Id, linkedCts.Token);
            
            try
            {
                await _process.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                _logger.LogWarning("Task {TaskId} timed out", task.Spec.Id);
                _process.Kill(entireProcessTree: true);
                return TaskResult.Failed("Execution timed out");
            }
            
            linkedCts.Cancel(); // Stop heartbeat
            
            var exitCode = _process.ExitCode;
            _logger.LogInformation("Task {TaskId} exited with code {ExitCode}", task.Spec.Id, exitCode);
            
            if (exitCode == 0)
            {
                return TaskResult.Succeeded();
            }
            else
            {
                return TaskResult.Failed($"Process exited with code {exitCode}");
            }
        }
        finally
        {
            // Cleanup task worktree
            if (!_options.KeepWorktrees)
            {
                await _worktreeManager.RemoveWorktreeAsync(taskWorktree, ct);
            }
            
            _process = null;
        }
    }
    
    private async Task HeartbeatLoopAsync(TaskId taskId, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await _heartbeat.SendHeartbeatAsync(Id, taskId, ct);
                await Task.Delay(TimeSpan.FromSeconds(_options.HeartbeatIntervalSeconds), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
    
    private async Task HandleCrashAsync(CancellationToken ct)
    {
        _restartCount++;
        
        if (_restartCount > _options.MaxRestarts)
        {
            _logger.LogError("Worker {WorkerId} exceeded max restarts ({Max})", Id, _options.MaxRestarts);
            Status = WorkerStatus.Failed;
            return;
        }
        
        _logger.LogWarning(
            "Worker {WorkerId} restarting ({Count}/{Max}) after {Delay}",
            Id, _restartCount, _options.MaxRestarts, _currentRestartDelay);
        
        await Task.Delay(_currentRestartDelay, ct);
        
        // Exponential backoff
        _currentRestartDelay = TimeSpan.FromMilliseconds(
            Math.Min(_currentRestartDelay.TotalMilliseconds * 2, _options.MaxRestartDelayMs));
        
        Status = WorkerStatus.Idle;
    }
    
    private async Task ValidateToolsAsync(CancellationToken ct)
    {
        var requiredTools = new[] { "dotnet", "git" };
        
        foreach (var tool in requiredTools)
        {
            try
            {
                var startInfo = new ProcessStartInfo(tool, "--version")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                
                using var process = Process.Start(startInfo);
                var version = await process!.StandardOutput.ReadToEndAsync(ct);
                await process.WaitForExitAsync(ct);
                
                if (process.ExitCode != 0)
                    throw new InvalidOperationException($"{tool} returned exit code {process.ExitCode}");
                
                _logger.LogDebug("Found {Tool}: {Version}", tool, version.Trim());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Required tool '{tool}' not found or not working", ex);
            }
        }
    }
    
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _process?.Dispose();
    }
}

public sealed class LocalWorkerOptions
{
    public int RestartDelayMs { get; set; } = 1000;
    public int MaxRestartDelayMs { get; set; } = 60000;
    public int MaxRestarts { get; set; } = 10;
    public int KillTimeoutSeconds { get; set; } = 10;
    public bool KeepWorktrees { get; set; } = false;
    public int PollIntervalMs { get; set; } = 1000;
    public int HeartbeatIntervalSeconds { get; set; } = 30;
    public string ExecutablePath { get; set; } = "dotnet";
    public string ConfigPath { get; set; } = "agent-config.yml";
}
```

### Process Supervisor

```csharp
// Acode.Application/Services/Workers/Local/IProcessSupervisor.cs
namespace Acode.Application.Services.Workers.Local;

/// <summary>
/// Manages OS processes for workers.
/// </summary>
public interface IProcessSupervisor
{
    /// <summary>
    /// Spawns a new process.
    /// </summary>
    Task<int> SpawnAsync(
        string executable,
        string[] args,
        string workingDir,
        IDictionary<string, string> env,
        CancellationToken ct = default);
    
    /// <summary>
    /// Kills a process gracefully then forcefully.
    /// </summary>
    Task KillAsync(int pid, TimeSpan timeout, CancellationToken ct = default);
    
    /// <summary>
    /// Checks if a process is running.
    /// </summary>
    bool IsRunning(int pid);
    
    /// <summary>
    /// Gets process info.
    /// </summary>
    ProcessInfo? GetInfo(int pid);
}

public sealed record ProcessInfo(int Pid, string Name, TimeSpan RunTime, long MemoryBytes);

// Acode.Application/Services/Workers/Local/ProcessSupervisor.cs
namespace Acode.Application.Services.Workers.Local;

public sealed class ProcessSupervisor : IProcessSupervisor
{
    private readonly ILogger<ProcessSupervisor> _logger;
    
    public ProcessSupervisor(ILogger<ProcessSupervisor> logger)
    {
        _logger = logger;
    }
    
    public async Task<int> SpawnAsync(
        string executable,
        string[] args,
        string workingDir,
        IDictionary<string, string> env,
        CancellationToken ct = default)
    {
        var startInfo = new ProcessStartInfo(executable)
        {
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }
        
        // Filter sensitive environment variables
        var sensitivePatterns = new[] { "KEY", "SECRET", "PASSWORD", "TOKEN", "CREDENTIAL" };
        foreach (var kvp in env)
        {
            var isSensitive = sensitivePatterns.Any(p => 
                kvp.Key.ToUpperInvariant().Contains(p));
            
            if (!isSensitive)
            {
                startInfo.Environment[kvp.Key] = kvp.Value;
            }
        }
        
        var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException($"Failed to start process: {executable}");
        }
        
        _logger.LogInformation(
            "Spawned process {Pid}: {Executable} {Args}",
            process.Id, executable, string.Join(" ", args));
        
        return process.Id;
    }
    
    public async Task KillAsync(int pid, TimeSpan timeout, CancellationToken ct = default)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            
            if (process.HasExited)
            {
                return;
            }
            
            _logger.LogDebug("Sending SIGTERM to process {Pid}", pid);
            
            // Send SIGTERM (graceful shutdown request)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows: use CloseMainWindow or GenerateConsoleCtrlEvent
                process.CloseMainWindow();
            }
            else
            {
                // Unix: send SIGTERM
                Process.Start("kill", $"-TERM {pid}")?.WaitForExit();
            }
            
            // Wait for graceful exit
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
            
            try
            {
                await process.WaitForExitAsync(linkedCts.Token);
                _logger.LogDebug("Process {Pid} exited gracefully", pid);
                return;
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                // Timeout - force kill
                _logger.LogWarning("Process {Pid} did not exit gracefully, sending SIGKILL", pid);
            }
            
            // Force kill
            process.Kill(entireProcessTree: true);
            _logger.LogInformation("Force killed process {Pid}", pid);
        }
        catch (ArgumentException)
        {
            // Process already exited
            _logger.LogDebug("Process {Pid} already exited", pid);
        }
    }
    
    public bool IsRunning(int pid)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
    
    public ProcessInfo? GetInfo(int pid)
    {
        try
        {
            var process = Process.GetProcessById(pid);
            return new ProcessInfo(
                pid,
                process.ProcessName,
                DateTime.Now - process.StartTime,
                process.WorkingSet64);
        }
        catch
        {
            return null;
        }
    }
}
```

### Worker Worktree Manager

```csharp
// Acode.Application/Services/Workers/Worktrees/IWorkerWorktreeManager.cs
namespace Acode.Application.Services.Workers.Worktrees;

/// <summary>
/// Manages git worktrees for workers.
/// </summary>
public interface IWorkerWorktreeManager
{
    /// <summary>
    /// Creates a new worktree for a worker.
    /// </summary>
    Task<string> CreateWorktreeAsync(
        string name,
        string? branch = null,
        CancellationToken ct = default);
    
    /// <summary>
    /// Removes a worktree.
    /// </summary>
    Task RemoveWorktreeAsync(string path, CancellationToken ct = default);
    
    /// <summary>
    /// Cleans up orphaned worktrees.
    /// </summary>
    Task<int> CleanupOrphanedAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Lists all worker worktrees.
    /// </summary>
    Task<IReadOnlyList<WorkerWorktreeInfo>> ListAsync(CancellationToken ct = default);
}

public sealed record WorkerWorktreeInfo(string Name, string Path, string Branch, bool IsOrphaned);

// Acode.Application/Services/Workers/Worktrees/WorkerWorktreeManager.cs
namespace Acode.Application.Services.Workers.Worktrees;

public sealed class WorkerWorktreeManager : IWorkerWorktreeManager
{
    private readonly IGitService _git;
    private readonly ILogger<WorkerWorktreeManager> _logger;
    private readonly WorktreeOptions _options;
    
    public WorkerWorktreeManager(
        IGitService git,
        IOptions<WorktreeOptions> options,
        ILogger<WorkerWorktreeManager> logger)
    {
        _git = git;
        _options = options.Value;
        _logger = logger;
    }
    
    public async Task<string> CreateWorktreeAsync(
        string name,
        string? branch = null,
        CancellationToken ct = default)
    {
        var worktreePath = Path.Combine(_options.BaseDir, name);
        
        // Ensure base dir exists
        Directory.CreateDirectory(_options.BaseDir);
        
        // Remove if exists (orphaned from previous run)
        if (Directory.Exists(worktreePath))
        {
            _logger.LogDebug("Removing existing worktree: {Path}", worktreePath);
            await RemoveWorktreeAsync(worktreePath, ct);
        }
        
        // Create worktree
        branch ??= await _git.GetCurrentBranchAsync(ct);
        await _git.AddWorktreeAsync(worktreePath, branch, ct);
        
        _logger.LogInformation("Created worktree: {Path} on branch {Branch}", worktreePath, branch);
        return worktreePath;
    }
    
    public async Task RemoveWorktreeAsync(string path, CancellationToken ct = default)
    {
        try
        {
            await _git.RemoveWorktreeAsync(path, force: true, ct);
            _logger.LogDebug("Removed worktree: {Path}", path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove worktree {Path}, cleaning up manually", path);
            
            // Manual cleanup
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
    }
    
    public async Task<int> CleanupOrphanedAsync(CancellationToken ct = default)
    {
        var worktrees = await ListAsync(ct);
        var orphaned = worktrees.Where(w => w.IsOrphaned).ToList();
        
        foreach (var worktree in orphaned)
        {
            await RemoveWorktreeAsync(worktree.Path, ct);
        }
        
        _logger.LogInformation("Cleaned up {Count} orphaned worktrees", orphaned.Count);
        return orphaned.Count;
    }
    
    public async Task<IReadOnlyList<WorkerWorktreeInfo>> ListAsync(CancellationToken ct = default)
    {
        var worktrees = await _git.ListWorktreesAsync(ct);
        var results = new List<WorkerWorktreeInfo>();
        
        foreach (var wt in worktrees)
        {
            if (!wt.Path.StartsWith(_options.BaseDir))
                continue;
            
            var name = Path.GetFileName(wt.Path);
            var isOrphaned = !Directory.Exists(wt.Path) || wt.IsLocked;
            
            results.Add(new WorkerWorktreeInfo(name, wt.Path, wt.Branch, isOrphaned));
        }
        
        return results;
    }
}

public sealed class WorktreeOptions
{
    public string BaseDir { get; set; } = ".agent/worktrees";
    public bool CleanupOnExit { get; set; } = true;
}
```

### Worker Environment

```csharp
// Acode.Application/Services/Workers/Local/WorkerEnvironment.cs
namespace Acode.Application.Services.Workers.Local;

/// <summary>
/// Sets up worker execution environment.
/// </summary>
public static class WorkerEnvironment
{
    public const string TaskIdVar = "ACODE_TASK_ID";
    public const string WorkerIdVar = "ACODE_WORKER_ID";
    public const string WorktreePathVar = "ACODE_WORKTREE_PATH";
    public const string ConfigPathVar = "ACODE_CONFIG_PATH";
    
    /// <summary>
    /// Creates environment variables for task execution.
    /// </summary>
    public static Dictionary<string, string> CreateForTask(
        TaskId taskId,
        WorkerId workerId,
        string worktreePath,
        string configPath)
    {
        return new Dictionary<string, string>
        {
            [TaskIdVar] = taskId.Value,
            [WorkerIdVar] = workerId.Value,
            [WorktreePathVar] = worktreePath,
            [ConfigPathVar] = configPath
        };
    }
    
    /// <summary>
    /// Gets the current task ID from environment.
    /// </summary>
    public static TaskId? GetTaskId() =>
        Environment.GetEnvironmentVariable(TaskIdVar) is string id 
            ? new TaskId(id) 
            : null;
    
    /// <summary>
    /// Gets the current worker ID from environment.
    /// </summary>
    public static WorkerId? GetWorkerId() =>
        Environment.GetEnvironmentVariable(WorkerIdVar) is string id 
            ? new WorkerId(id) 
            : null;
}
```

### Exit Codes

```csharp
// Acode.Application/Services/Workers/Local/WorkerExitCodes.cs
namespace Acode.Application.Services.Workers.Local;

public static class WorkerExitCodes
{
    public const int Success = 0;
    public const int TaskFailed = 1;
    public const int ConfigurationError = 2;
    public const int ToolNotFound = 3;
    public const int Timeout = 4;
    
    // Unix signals (128 + signal number)
    public const int SigKill = 137; // 128 + 9
    public const int SigTerm = 143; // 128 + 15
    
    public static string GetDescription(int exitCode) => exitCode switch
    {
        Success => "Clean exit",
        TaskFailed => "Task failed",
        ConfigurationError => "Configuration error",
        ToolNotFound => "Required tool not found",
        Timeout => "Execution timed out",
        SigKill => "Killed (SIGKILL)",
        SigTerm => "Terminated (SIGTERM)",
        _ => $"Unknown exit code ({exitCode})"
    };
}
```

### Implementation Checklist

- [ ] Define `ILocalWorker` interface extending `IWorker`
- [ ] Implement `LocalWorker` with run loop
- [ ] Add task claiming from queue
- [ ] Add state machine integration
- [ ] Define `IProcessSupervisor` interface
- [ ] Implement `ProcessSupervisor`
- [ ] Add SIGTERM/SIGKILL handling
- [ ] Add cross-platform process kill
- [ ] Define `IWorkerWorktreeManager` interface
- [ ] Implement `WorkerWorktreeManager`
- [ ] Add worktree creation
- [ ] Add worktree cleanup
- [ ] Add orphan detection
- [ ] Create `WorkerEnvironment` helper
- [ ] Define exit codes
- [ ] Add tool validation
- [ ] Add heartbeat loop
- [ ] Add crash recovery with backoff
- [ ] Add restart limit enforcement
- [ ] Configure options
- [ ] Register in DI
- [ ] Write unit tests
- [ ] Write integration tests

### Rollout Plan

1. **Phase 1: Process Management** (Day 1)
   - ProcessSupervisor
   - Spawn/kill logic
   - Cross-platform support

2. **Phase 2: Worktree Management** (Day 1)
   - Create/remove worktrees
   - Orphan cleanup

3. **Phase 3: Local Worker** (Day 2)
   - Run loop
   - Task execution
   - Heartbeat integration

4. **Phase 4: Recovery** (Day 2)
   - Crash detection
   - Exponential backoff
   - Restart limits

5. **Phase 5: Integration** (Day 3)
   - DI registration
   - End-to-end testing
   - Performance validation

---

**End of Task 027.a Specification**