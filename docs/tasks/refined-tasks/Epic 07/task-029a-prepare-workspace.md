# Task 029.a: Prepare Workspace

**Priority:** P0 – Critical  
**Tier:** S – Core Infrastructure  
**Complexity:** 5 (Fibonacci points)  
**Phase:** Phase 7 – Cloud Integration  
**Dependencies:** Task 029 (Interface), Task 005 (Git)  

---

## Description

Task 029.a implements workspace preparation for compute targets. Before execution, the target MUST have the code and dependencies ready. Preparation MUST be idempotent.

Workspace preparation MUST sync the repository. The correct branch or commit MUST be checked out. Dependencies MUST be installed. Build tools MUST be available.

Preparation MUST be target-specific. Local targets use the filesystem. Remote targets use rsync or similar. Cloud targets bootstrap from scratch.

### Business Value

Workspace preparation enables:
- Consistent execution environment
- Correct code version
- Required dependencies
- Reproducible builds

### Scope Boundaries

This task covers preparation. Execution is in 029.b. Artifacts are in 029.c. Teardown is in 029.d.

### Integration Points

- Task 029: Uses this for lifecycle
- Task 005: Git operations
- Task 030-031: Override for specifics

### Failure Modes

- Sync failure → Retry or fail
- Dependency install failure → Report clearly
- Disk full → Cleanup and retry
- Timeout → Cancel and report

---

## Glossary / Terms

| Term | Definition |
|------|------------|
| Workspace | Directory for execution |
| Sync | Transfer code to target |
| Dependencies | Required packages |
| Bootstrap | Initial setup |
| Idempotent | Same result on repeat |

---

## Out of Scope

- Custom build systems
- Monorepo handling
- Submodule resolution
- LFS file handling
- Pre-built artifacts

---

## Functional Requirements

### FR-001 to FR-030: Preparation Steps

- FR-001: `PrepareWorkspaceAsync` MUST be called
- FR-002: Config MUST specify source
- FR-003: Source: repo path or URL
- FR-004: Config MUST specify ref
- FR-005: Ref: branch, tag, or commit
- FR-006: Config MUST specify worktree
- FR-007: Worktree: target path
- FR-008: Workspace MUST be created
- FR-009: Directory MUST be clean
- FR-010: Existing files MAY be cleaned
- FR-011: Clean option MUST be configurable
- FR-012: Default: clean before sync
- FR-013: Source MUST be synced
- FR-014: Local: copy or symlink
- FR-015: Remote: rsync or git clone
- FR-016: Ref MUST be checked out
- FR-017: Checkout MUST be detached HEAD
- FR-018: Submodules MUST be updated
- FR-019: Submodule depth MUST be configurable
- FR-020: Dependencies MUST be installed
- FR-021: .NET: `dotnet restore`
- FR-022: Node: `npm ci` or `yarn install`
- FR-023: Python: `pip install -r requirements.txt`
- FR-024: Detection MUST be automatic
- FR-025: Multiple ecosystems MUST work
- FR-026: Custom commands MUST be supported
- FR-027: prepareCommands config option
- FR-028: Commands MUST run in order
- FR-029: Failure MUST stop preparation
- FR-030: Success MUST update state to Ready

### FR-031 to FR-050: Optimization

- FR-031: Caching MUST be supported
- FR-032: Dependency cache MUST work
- FR-033: .NET NuGet cache
- FR-034: Node node_modules cache
- FR-035: Python venv cache
- FR-036: Cache location MUST be configurable
- FR-037: Cache invalidation MUST work
- FR-038: Invalidation on lockfile change
- FR-039: Incremental sync MUST work
- FR-040: Only changed files MUST transfer
- FR-041: rsync delta MUST be used
- FR-042: git fetch MUST be incremental
- FR-043: Parallel download MUST work
- FR-044: Large files MAY parallelize
- FR-045: Progress MUST be reported
- FR-046: Progress events MUST emit
- FR-047: ETA MUST be estimated
- FR-048: Cancellation MUST be respected
- FR-049: Partial state MUST cleanup
- FR-050: Retry MUST be configurable

---

## Non-Functional Requirements

- NFR-001: Preparation MUST complete in <5min
- NFR-002: Large repo (1GB) MUST handle
- NFR-003: Network interruption MUST retry
- NFR-004: Disk space MUST be checked
- NFR-005: Permission errors MUST report
- NFR-006: Idempotent preparation
- NFR-007: Parallel preparation MUST work
- NFR-008: Resource cleanup on failure
- NFR-009: Clear progress reporting
- NFR-010: Structured logging

---

## User Manual Documentation

### Configuration

```yaml
workspace:
  source: /path/to/repo  # or git URL
  ref: main
  worktree: /tmp/acode-work
  clean: true
  
  cache:
    enabled: true
    path: ~/.acode/cache
    
  dependencies:
    autoDetect: true
    commands:
      - dotnet restore
      - npm ci
      
  prepareCommands:
    - chmod +x scripts/*.sh
```

### Lifecycle

```
Target.PrepareWorkspaceAsync(config)
  ├── Create workspace directory
  ├── Clean existing files (if configured)
  ├── Sync source code
  ├── Checkout specified ref
  ├── Update submodules
  ├── Install dependencies
  ├── Run custom commands
  └── Mark target as Ready
```

### Progress Events

| Event | Data |
|-------|------|
| PreparationStarted | targetId, config |
| SyncProgress | bytesTransferred, total |
| DependencyProgress | ecosystem, status |
| PreparationCompleted | targetId, duration |
| PreparationFailed | targetId, error |

---

## Acceptance Criteria / Definition of Done

- [ ] AC-001: Workspace created
- [ ] AC-002: Source synced
- [ ] AC-003: Ref checked out
- [ ] AC-004: Dependencies installed
- [ ] AC-005: Custom commands run
- [ ] AC-006: Caching works
- [ ] AC-007: Incremental sync works
- [ ] AC-008: Progress reported
- [ ] AC-009: Cancellation works
- [ ] AC-010: Failure cleanup works

---

## Testing Requirements

### Unit Tests

- [ ] UT-001: Config validation
- [ ] UT-002: Step ordering
- [ ] UT-003: Ecosystem detection
- [ ] UT-004: Cache invalidation

### Integration Tests

- [ ] IT-001: Full preparation
- [ ] IT-002: Large repo sync
- [ ] IT-003: Multiple ecosystems

---

## Implementation Prompt

You are implementing workspace preparation for compute targets. This handles code sync, dependency installation, and environment setup. Follow Clean Architecture and TDD.

### Part 1: File Structure and Domain Models

#### File Structure

```
src/Acode.Domain/
├── Compute/
│   └── Workspace/
│       ├── PreparationPhase.cs
│       ├── PreparationProgress.cs
│       ├── WorkspaceConfig.cs
│       ├── EcosystemType.cs
│       └── Events/
│           ├── PreparationStartedEvent.cs
│           ├── SyncProgressEvent.cs
│           ├── DependencyInstalledEvent.cs
│           └── PreparationCompletedEvent.cs

src/Acode.Application/
├── Compute/
│   └── Workspace/
│       ├── IWorkspacePreparation.cs
│       ├── IEcosystemDetector.cs
│       ├── IDependencyInstaller.cs
│       ├── ISourceSyncer.cs
│       └── ICacheManager.cs

src/Acode.Infrastructure/
├── Compute/
│   └── Workspace/
│       ├── WorkspacePreparation.cs
│       ├── EcosystemDetector.cs
│       ├── SourceSyncer/
│       │   ├── LocalSourceSyncer.cs
│       │   ├── GitSourceSyncer.cs
│       │   └── RsyncSourceSyncer.cs
│       ├── DependencyInstaller/
│       │   ├── DotNetDependencyInstaller.cs
│       │   ├── NodeDependencyInstaller.cs
│       │   └── PythonDependencyInstaller.cs
│       └── Cache/
│           ├── DependencyCacheManager.cs
│           └── CacheInvalidator.cs

tests/Acode.Domain.Tests/
├── Compute/
│   └── Workspace/
│       ├── WorkspaceConfigTests.cs
│       └── PreparationProgressTests.cs

tests/Acode.Infrastructure.Tests/
├── Compute/
│   └── Workspace/
│       ├── WorkspacePreparationTests.cs
│       ├── EcosystemDetectorTests.cs
│       ├── SourceSyncer/
│       │   ├── LocalSourceSyncerTests.cs
│       │   └── GitSourceSyncerTests.cs
│       └── DependencyInstaller/
│           ├── DotNetDependencyInstallerTests.cs
│           ├── NodeDependencyInstallerTests.cs
│           └── PythonDependencyInstallerTests.cs

tests/Acode.Integration.Tests/
├── Compute/
│   └── Workspace/
│       ├── FullPreparationTests.cs
│       ├── LargeRepoSyncTests.cs
│       └── MultiEcosystemTests.cs
```

#### Domain Models

```csharp
// src/Acode.Domain/Compute/Workspace/PreparationPhase.cs
namespace Acode.Domain.Compute.Workspace;

public enum PreparationPhase
{
    NotStarted = 0,
    Creating = 1,
    Cleaning = 2,
    Syncing = 3,
    CheckingOut = 4,
    UpdatingSubmodules = 5,
    DetectingEcosystems = 6,
    InstallingDependencies = 7,
    RunningCommands = 8,
    Completed = 9,
    Failed = 10
}

// src/Acode.Domain/Compute/Workspace/PreparationProgress.cs
namespace Acode.Domain.Compute.Workspace;

public sealed record PreparationProgress
{
    public required PreparationPhase Phase { get; init; }
    public required double PercentComplete { get; init; }
    public required string Message { get; init; }
    public long? BytesTransferred { get; init; }
    public long? TotalBytes { get; init; }
    public TimeSpan? EstimatedRemaining { get; init; }
    public string? CurrentFile { get; init; }
}

// src/Acode.Domain/Compute/Workspace/EcosystemType.cs
namespace Acode.Domain.Compute.Workspace;

[Flags]
public enum EcosystemType
{
    None = 0,
    DotNet = 1,
    Node = 2,
    Python = 4,
    Go = 8,
    Rust = 16,
    Java = 32
}

// src/Acode.Domain/Compute/Workspace/Events/PreparationStartedEvent.cs
namespace Acode.Domain.Compute.Workspace.Events;

public sealed record PreparationStartedEvent(
    ComputeTargetId TargetId,
    WorkspaceConfig Config,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/Compute/Workspace/Events/SyncProgressEvent.cs
namespace Acode.Domain.Compute.Workspace.Events;

public sealed record SyncProgressEvent(
    ComputeTargetId TargetId,
    long BytesTransferred,
    long TotalBytes,
    int FilesTransferred,
    DateTimeOffset Timestamp) : IDomainEvent;

// src/Acode.Domain/Compute/Workspace/Events/PreparationCompletedEvent.cs
namespace Acode.Domain.Compute.Workspace.Events;

public sealed record PreparationCompletedEvent(
    ComputeTargetId TargetId,
    TimeSpan Duration,
    EcosystemType DetectedEcosystems,
    DateTimeOffset Timestamp) : IDomainEvent;
```

**End of Task 029.a Specification - Part 1/4**

### Part 2: Application Layer Interfaces

```csharp
// src/Acode.Application/Compute/Workspace/IWorkspacePreparation.cs
namespace Acode.Application.Compute.Workspace;

public interface IWorkspacePreparation
{
    Task PrepareAsync(
        IComputeTarget target,
        WorkspaceConfig config,
        IProgress<PreparationProgress>? progress = null,
        CancellationToken ct = default);
    
    Task<bool> ValidateWorkspaceAsync(
        string workspacePath,
        CancellationToken ct = default);
    
    Task CleanupAsync(
        string workspacePath,
        CancellationToken ct = default);
}

// src/Acode.Application/Compute/Workspace/IEcosystemDetector.cs
namespace Acode.Application.Compute.Workspace;

public interface IEcosystemDetector
{
    EcosystemType Detect(string workspacePath);
    IReadOnlyList<EcosystemInfo> GetDetailedInfo(string workspacePath);
}

public sealed record EcosystemInfo(
    EcosystemType Type,
    string RootPath,
    string? LockFile,
    string? ConfigFile);

// src/Acode.Application/Compute/Workspace/IDependencyInstaller.cs
namespace Acode.Application.Compute.Workspace;

public interface IDependencyInstaller
{
    EcosystemType SupportedEcosystem { get; }
    
    Task InstallAsync(
        string workspacePath,
        DependencyConfig? config,
        IProgress<DependencyProgress>? progress = null,
        CancellationToken ct = default);
    
    Task<bool> IsInstalledAsync(string workspacePath, CancellationToken ct = default);
}

public sealed record DependencyProgress(
    string Message,
    double PercentComplete,
    int PackagesInstalled,
    int TotalPackages);

// src/Acode.Application/Compute/Workspace/ISourceSyncer.cs
namespace Acode.Application.Compute.Workspace;

public interface ISourceSyncer
{
    bool CanHandle(string sourcePath);
    
    Task SyncAsync(
        string source,
        string destination,
        string? gitRef,
        SyncOptions options,
        IProgress<SyncProgress>? progress = null,
        CancellationToken ct = default);
}

public sealed record SyncOptions(
    bool Incremental = true,
    bool IncludeSubmodules = true,
    int SubmoduleDepth = 1,
    IReadOnlyList<string>? ExcludePatterns = null);

public sealed record SyncProgress(
    long BytesTransferred,
    long TotalBytes,
    int FilesTransferred,
    int TotalFiles,
    string? CurrentFile);

// src/Acode.Application/Compute/Workspace/ICacheManager.cs
namespace Acode.Application.Compute.Workspace;

public interface ICacheManager
{
    Task<string?> GetCachePathAsync(
        EcosystemType ecosystem,
        string lockFileHash,
        CancellationToken ct = default);
    
    Task StoreCacheAsync(
        EcosystemType ecosystem,
        string dependencyPath,
        string lockFileHash,
        CancellationToken ct = default);
    
    Task<bool> RestoreCacheAsync(
        string cachePath,
        string destinationPath,
        CancellationToken ct = default);
    
    Task InvalidateCacheAsync(
        EcosystemType ecosystem,
        CancellationToken ct = default);
    
    Task<CacheStats> GetStatsAsync(CancellationToken ct = default);
}

public sealed record CacheStats(
    long TotalSize,
    int EntryCount,
    DateTimeOffset OldestEntry,
    IReadOnlyDictionary<EcosystemType, long> SizeByEcosystem);
```

**End of Task 029.a Specification - Part 2/4**

### Part 3: Infrastructure - Source Syncers and Ecosystem Detection

```csharp
// src/Acode.Infrastructure/Compute/Workspace/WorkspacePreparation.cs
namespace Acode.Infrastructure.Compute.Workspace;

public sealed class WorkspacePreparation : IWorkspacePreparation
{
    private readonly IEnumerable<ISourceSyncer> _syncers;
    private readonly IEcosystemDetector _ecosystemDetector;
    private readonly IEnumerable<IDependencyInstaller> _installers;
    private readonly ICacheManager _cacheManager;
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly IEventPublisher _events;
    private readonly ILogger<WorkspacePreparation> _logger;
    
    public WorkspacePreparation(
        IEnumerable<ISourceSyncer> syncers,
        IEcosystemDetector ecosystemDetector,
        IEnumerable<IDependencyInstaller> installers,
        ICacheManager cacheManager,
        IProcessRunner processRunner,
        IFileSystem fileSystem,
        IEventPublisher events,
        ILogger<WorkspacePreparation> logger)
    {
        _syncers = syncers;
        _ecosystemDetector = ecosystemDetector;
        _installers = installers;
        _cacheManager = cacheManager;
        _processRunner = processRunner;
        _fileSystem = fileSystem;
        _events = events;
        _logger = logger;
    }
    
    public async Task PrepareAsync(
        IComputeTarget target,
        WorkspaceConfig config,
        IProgress<PreparationProgress>? progress = null,
        CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await _events.PublishAsync(new PreparationStartedEvent(
            target.Id, config, DateTimeOffset.UtcNow));
        
        try
        {
            // Step 1: Create workspace directory
            Report(progress, PreparationPhase.Creating, 5, "Creating workspace directory");
            _fileSystem.Directory.CreateDirectory(config.WorktreePath);
            
            // Step 2: Clean if configured
            if (config.CleanBeforeSync)
            {
                Report(progress, PreparationPhase.Cleaning, 10, "Cleaning existing files");
                await CleanupAsync(config.WorktreePath, ct);
            }
            
            // Step 3: Sync source code
            Report(progress, PreparationPhase.Syncing, 15, "Syncing source code");
            var syncer = _syncers.FirstOrDefault(s => s.CanHandle(config.SourcePath))
                ?? throw new InvalidOperationException($"No syncer for: {config.SourcePath}");
            
            var syncProgress = new Progress<SyncProgress>(p =>
                Report(progress, PreparationPhase.Syncing, 
                    15 + (p.BytesTransferred * 35 / Math.Max(p.TotalBytes, 1)),
                    $"Syncing: {p.CurrentFile}",
                    p.BytesTransferred, p.TotalBytes));
            
            await syncer.SyncAsync(
                config.SourcePath,
                config.WorktreePath,
                config.Ref,
                new SyncOptions(),
                syncProgress,
                ct);
            
            // Step 4: Detect ecosystems
            Report(progress, PreparationPhase.DetectingEcosystems, 55, "Detecting ecosystems");
            var ecosystems = _ecosystemDetector.Detect(config.WorktreePath);
            
            // Step 5: Install dependencies
            if (config.Dependencies?.AutoDetect ?? true)
            {
                Report(progress, PreparationPhase.InstallingDependencies, 60, "Installing dependencies");
                await InstallDependenciesAsync(
                    config.WorktreePath, ecosystems, config, progress, ct);
            }
            
            // Step 6: Run custom commands
            if (config.PrepareCommands?.Count > 0)
            {
                Report(progress, PreparationPhase.RunningCommands, 90, "Running prepare commands");
                await RunPrepareCommandsAsync(
                    config.WorktreePath, config.PrepareCommands, ct);
            }
            
            stopwatch.Stop();
            Report(progress, PreparationPhase.Completed, 100, "Workspace ready");
            
            await _events.PublishAsync(new PreparationCompletedEvent(
                target.Id, stopwatch.Elapsed, ecosystems, DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workspace preparation failed for {TargetId}", target.Id);
            Report(progress, PreparationPhase.Failed, 0, ex.Message);
            throw;
        }
    }
    
    private async Task InstallDependenciesAsync(
        string workspacePath,
        EcosystemType ecosystems,
        WorkspaceConfig config,
        IProgress<PreparationProgress>? progress,
        CancellationToken ct)
    {
        foreach (var installer in _installers)
        {
            if (!ecosystems.HasFlag(installer.SupportedEcosystem))
                continue;
            
            // Check cache first
            if (config.Cache?.Enabled ?? false)
            {
                var lockHash = ComputeLockFileHash(workspacePath, installer.SupportedEcosystem);
                var cachePath = await _cacheManager.GetCachePathAsync(
                    installer.SupportedEcosystem, lockHash, ct);
                
                if (cachePath != null)
                {
                    var depPath = GetDependencyPath(workspacePath, installer.SupportedEcosystem);
                    if (await _cacheManager.RestoreCacheAsync(cachePath, depPath, ct))
                    {
                        _logger.LogInformation("Restored {Ecosystem} from cache", 
                            installer.SupportedEcosystem);
                        continue;
                    }
                }
            }
            
            await installer.InstallAsync(workspacePath, config.Dependencies, null, ct);
            
            // Store in cache
            if (config.Cache?.Enabled ?? false)
            {
                var lockHash = ComputeLockFileHash(workspacePath, installer.SupportedEcosystem);
                var depPath = GetDependencyPath(workspacePath, installer.SupportedEcosystem);
                await _cacheManager.StoreCacheAsync(
                    installer.SupportedEcosystem, depPath, lockHash, ct);
            }
        }
    }
    
    private async Task RunPrepareCommandsAsync(
        string workspacePath,
        IReadOnlyList<string> commands,
        CancellationToken ct)
    {
        foreach (var cmd in commands)
        {
            var result = await _processRunner.RunAsync(
                cmd, [], workspacePath, null, TimeSpan.FromMinutes(5), ct);
            
            if (result.ExitCode != 0)
                throw new InvalidOperationException($"Prepare command failed: {cmd}\n{result.StdErr}");
        }
    }
    
    private static void Report(
        IProgress<PreparationProgress>? progress,
        PreparationPhase phase, double pct, string msg,
        long? bytesTransferred = null, long? totalBytes = null)
    {
        progress?.Report(new PreparationProgress
        {
            Phase = phase,
            PercentComplete = pct,
            Message = msg,
            BytesTransferred = bytesTransferred,
            TotalBytes = totalBytes
        });
    }
}

// src/Acode.Infrastructure/Compute/Workspace/EcosystemDetector.cs
namespace Acode.Infrastructure.Compute.Workspace;

public sealed class EcosystemDetector : IEcosystemDetector
{
    private readonly IFileSystem _fileSystem;
    
    private static readonly (string File, EcosystemType Type)[] Indicators =
    [
        ("*.csproj", EcosystemType.DotNet),
        ("*.fsproj", EcosystemType.DotNet),
        ("*.sln", EcosystemType.DotNet),
        ("package.json", EcosystemType.Node),
        ("requirements.txt", EcosystemType.Python),
        ("pyproject.toml", EcosystemType.Python),
        ("setup.py", EcosystemType.Python),
        ("go.mod", EcosystemType.Go),
        ("Cargo.toml", EcosystemType.Rust),
        ("pom.xml", EcosystemType.Java),
        ("build.gradle", EcosystemType.Java)
    ];
    
    public EcosystemDetector(IFileSystem fileSystem) => _fileSystem = fileSystem;
    
    public EcosystemType Detect(string workspacePath)
    {
        var result = EcosystemType.None;
        
        foreach (var (pattern, type) in Indicators)
        {
            var files = _fileSystem.Directory.GetFiles(workspacePath, pattern, SearchOption.AllDirectories);
            if (files.Length > 0)
                result |= type;
        }
        
        return result;
    }
    
    public IReadOnlyList<EcosystemInfo> GetDetailedInfo(string workspacePath)
    {
        var results = new List<EcosystemInfo>();
        
        foreach (var (pattern, type) in Indicators)
        {
            foreach (var file in _fileSystem.Directory.GetFiles(workspacePath, pattern, SearchOption.AllDirectories))
            {
                results.Add(new EcosystemInfo(
                    type,
                    Path.GetDirectoryName(file)!,
                    GetLockFile(Path.GetDirectoryName(file)!, type),
                    file));
            }
        }
        
        return results;
    }
    
    private string? GetLockFile(string dir, EcosystemType type) => type switch
    {
        EcosystemType.Node => FindFile(dir, "package-lock.json", "yarn.lock", "pnpm-lock.yaml"),
        EcosystemType.Python => FindFile(dir, "requirements.txt", "poetry.lock", "Pipfile.lock"),
        EcosystemType.DotNet => FindFile(dir, "packages.lock.json"),
        _ => null
    };
    
    private string? FindFile(string dir, params string[] names)
    {
        foreach (var name in names)
        {
            var path = Path.Combine(dir, name);
            if (_fileSystem.File.Exists(path))
                return path;
        }
        return null;
    }
}
```

**End of Task 029.a Specification - Part 3/4**

### Part 4: Dependency Installers, Cache Manager, and Rollout

```csharp
// src/Acode.Infrastructure/Compute/Workspace/DependencyInstaller/DotNetDependencyInstaller.cs
namespace Acode.Infrastructure.Compute.Workspace.DependencyInstaller;

public sealed class DotNetDependencyInstaller : IDependencyInstaller
{
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<DotNetDependencyInstaller> _logger;
    
    public EcosystemType SupportedEcosystem => EcosystemType.DotNet;
    
    public DotNetDependencyInstaller(
        IProcessRunner processRunner,
        ILogger<DotNetDependencyInstaller> logger)
    {
        _processRunner = processRunner;
        _logger = logger;
    }
    
    public async Task InstallAsync(
        string workspacePath,
        DependencyConfig? config,
        IProgress<DependencyProgress>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report(new DependencyProgress("Running dotnet restore", 0, 0, 0));
        
        var result = await _processRunner.RunAsync(
            "dotnet", ["restore", "--verbosity", "minimal"],
            workspacePath, null, TimeSpan.FromMinutes(10), ct);
        
        if (result.ExitCode != 0)
        {
            _logger.LogError("dotnet restore failed: {Error}", result.StdErr);
            throw new InvalidOperationException($"dotnet restore failed: {result.StdErr}");
        }
        
        progress?.Report(new DependencyProgress("Restore complete", 100, 0, 0));
    }
    
    public async Task<bool> IsInstalledAsync(string workspacePath, CancellationToken ct)
    {
        // Check if obj folders exist with project.assets.json
        var objDirs = Directory.GetDirectories(workspacePath, "obj", SearchOption.AllDirectories);
        return objDirs.Any(d => File.Exists(Path.Combine(d, "project.assets.json")));
    }
}

// src/Acode.Infrastructure/Compute/Workspace/DependencyInstaller/NodeDependencyInstaller.cs
namespace Acode.Infrastructure.Compute.Workspace.DependencyInstaller;

public sealed class NodeDependencyInstaller : IDependencyInstaller
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<NodeDependencyInstaller> _logger;
    
    public EcosystemType SupportedEcosystem => EcosystemType.Node;
    
    public NodeDependencyInstaller(
        IProcessRunner processRunner,
        IFileSystem fileSystem,
        ILogger<NodeDependencyInstaller> logger)
    {
        _processRunner = processRunner;
        _fileSystem = fileSystem;
        _logger = logger;
    }
    
    public async Task InstallAsync(
        string workspacePath,
        DependencyConfig? config,
        IProgress<DependencyProgress>? progress = null,
        CancellationToken ct = default)
    {
        var (cmd, args) = DeterminePackageManager(workspacePath);
        progress?.Report(new DependencyProgress($"Running {cmd} {string.Join(" ", args)}", 0, 0, 0));
        
        var result = await _processRunner.RunAsync(
            cmd, args, workspacePath, null, TimeSpan.FromMinutes(15), ct);
        
        if (result.ExitCode != 0)
        {
            _logger.LogError("{Cmd} failed: {Error}", cmd, result.StdErr);
            throw new InvalidOperationException($"{cmd} install failed: {result.StdErr}");
        }
        
        progress?.Report(new DependencyProgress("Install complete", 100, 0, 0));
    }
    
    private (string cmd, string[] args) DeterminePackageManager(string workspacePath)
    {
        if (_fileSystem.File.Exists(Path.Combine(workspacePath, "pnpm-lock.yaml")))
            return ("pnpm", ["install", "--frozen-lockfile"]);
        if (_fileSystem.File.Exists(Path.Combine(workspacePath, "yarn.lock")))
            return ("yarn", ["install", "--frozen-lockfile"]);
        return ("npm", ["ci"]);
    }
    
    public Task<bool> IsInstalledAsync(string workspacePath, CancellationToken ct)
    {
        var nodeModules = Path.Combine(workspacePath, "node_modules");
        return Task.FromResult(_fileSystem.Directory.Exists(nodeModules));
    }
}

// src/Acode.Infrastructure/Compute/Workspace/DependencyInstaller/PythonDependencyInstaller.cs
namespace Acode.Infrastructure.Compute.Workspace.DependencyInstaller;

public sealed class PythonDependencyInstaller : IDependencyInstaller
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<PythonDependencyInstaller> _logger;
    
    public EcosystemType SupportedEcosystem => EcosystemType.Python;
    
    public async Task InstallAsync(
        string workspacePath,
        DependencyConfig? config,
        IProgress<DependencyProgress>? progress = null,
        CancellationToken ct = default)
    {
        var (cmd, args) = DetermineInstallCommand(workspacePath);
        progress?.Report(new DependencyProgress($"Running {cmd}", 0, 0, 0));
        
        var result = await _processRunner.RunAsync(
            cmd, args, workspacePath, null, TimeSpan.FromMinutes(15), ct);
        
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"Python install failed: {result.StdErr}");
        
        progress?.Report(new DependencyProgress("Install complete", 100, 0, 0));
    }
    
    private (string cmd, string[] args) DetermineInstallCommand(string path)
    {
        if (_fileSystem.File.Exists(Path.Combine(path, "pyproject.toml")))
            return ("pip", ["install", "-e", "."]);
        if (_fileSystem.File.Exists(Path.Combine(path, "requirements.txt")))
            return ("pip", ["install", "-r", "requirements.txt"]);
        return ("pip", ["install", "."]);
    }
}

// src/Acode.Infrastructure/Compute/Workspace/Cache/DependencyCacheManager.cs
namespace Acode.Infrastructure.Compute.Workspace.Cache;

public sealed class DependencyCacheManager : ICacheManager
{
    private readonly string _cacheRoot;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<DependencyCacheManager> _logger;
    
    public DependencyCacheManager(
        IOptions<CacheOptions> options,
        IFileSystem fileSystem,
        ILogger<DependencyCacheManager> logger)
    {
        _cacheRoot = options.Value.CachePath;
        _fileSystem = fileSystem;
        _logger = logger;
    }
    
    public Task<string?> GetCachePathAsync(
        EcosystemType ecosystem,
        string lockFileHash,
        CancellationToken ct = default)
    {
        var path = GetCacheEntryPath(ecosystem, lockFileHash);
        return Task.FromResult(_fileSystem.Directory.Exists(path) ? path : null);
    }
    
    public async Task StoreCacheAsync(
        EcosystemType ecosystem,
        string dependencyPath,
        string lockFileHash,
        CancellationToken ct = default)
    {
        var cachePath = GetCacheEntryPath(ecosystem, lockFileHash);
        
        if (_fileSystem.Directory.Exists(cachePath))
            _fileSystem.Directory.Delete(cachePath, true);
        
        await CopyDirectoryAsync(dependencyPath, cachePath, ct);
        
        _logger.LogInformation(
            "Cached {Ecosystem} dependencies at {Path}", ecosystem, cachePath);
    }
    
    public async Task<bool> RestoreCacheAsync(
        string cachePath,
        string destinationPath,
        CancellationToken ct = default)
    {
        if (!_fileSystem.Directory.Exists(cachePath))
            return false;
        
        await CopyDirectoryAsync(cachePath, destinationPath, ct);
        return true;
    }
    
    public Task InvalidateCacheAsync(EcosystemType ecosystem, CancellationToken ct)
    {
        var ecosystemPath = Path.Combine(_cacheRoot, ecosystem.ToString().ToLowerInvariant());
        if (_fileSystem.Directory.Exists(ecosystemPath))
            _fileSystem.Directory.Delete(ecosystemPath, true);
        return Task.CompletedTask;
    }
    
    private string GetCacheEntryPath(EcosystemType ecosystem, string hash) =>
        Path.Combine(_cacheRoot, ecosystem.ToString().ToLowerInvariant(), hash[..16]);
    
    private async Task CopyDirectoryAsync(string source, string dest, CancellationToken ct)
    {
        _fileSystem.Directory.CreateDirectory(dest);
        await Task.Run(() =>
        {
            foreach (var file in _fileSystem.Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                ct.ThrowIfCancellationRequested();
                var relative = Path.GetRelativePath(source, file);
                var destFile = Path.Combine(dest, relative);
                _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                _fileSystem.File.Copy(file, destFile, true);
            }
        }, ct);
    }
}

// src/Acode.Infrastructure/Compute/Workspace/SourceSyncer/LocalSourceSyncer.cs
namespace Acode.Infrastructure.Compute.Workspace.SourceSyncer;

public sealed class LocalSourceSyncer : ISourceSyncer
{
    private readonly IFileSystem _fileSystem;
    
    public LocalSourceSyncer(IFileSystem fileSystem) => _fileSystem = fileSystem;
    
    public bool CanHandle(string sourcePath) =>
        _fileSystem.Directory.Exists(sourcePath) && !sourcePath.StartsWith("git://");
    
    public async Task SyncAsync(
        string source,
        string destination,
        string? gitRef,
        SyncOptions options,
        IProgress<SyncProgress>? progress = null,
        CancellationToken ct = default)
    {
        var files = _fileSystem.Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories)
            .Where(f => !ShouldExclude(f, source, options.ExcludePatterns))
            .ToList();
        
        long totalSize = files.Sum(f => new FileInfo(f).Length);
        long transferred = 0;
        int fileCount = 0;
        
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            
            var relative = Path.GetRelativePath(source, file);
            var destFile = Path.Combine(destination, relative);
            
            _fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            _fileSystem.File.Copy(file, destFile, true);
            
            var size = new FileInfo(file).Length;
            transferred += size;
            fileCount++;
            
            progress?.Report(new SyncProgress(transferred, totalSize, fileCount, files.Count, relative));
        }
    }
    
    private static bool ShouldExclude(string file, string root, IReadOnlyList<string>? patterns)
    {
        if (patterns is null) return false;
        var relative = Path.GetRelativePath(root, file);
        return patterns.Any(p => Glob.IsMatch(relative, p));
    }
}
```

---

## Implementation Checklist

- [ ] Create PreparationPhase and PreparationProgress records
- [ ] Define EcosystemType flags enum
- [ ] Create preparation events for audit trail
- [ ] Implement IWorkspacePreparation with full lifecycle
- [ ] Build EcosystemDetector with file-pattern matching
- [ ] Implement DotNetDependencyInstaller (dotnet restore)
- [ ] Implement NodeDependencyInstaller (npm/yarn/pnpm)
- [ ] Implement PythonDependencyInstaller (pip)
- [ ] Create DependencyCacheManager with hash-based lookup
- [ ] Build LocalSourceSyncer for filesystem copies
- [ ] Add progress reporting throughout pipeline
- [ ] Write unit tests for each component (TDD)
- [ ] Write integration tests for full preparation
- [ ] Test cache hit/miss scenarios
- [ ] Test multi-ecosystem projects
- [ ] Verify cleanup on failure

---

## Rollout Plan

1. **Phase 1**: Domain models (phases, progress, events)
2. **Phase 2**: Application interfaces
3. **Phase 3**: EcosystemDetector implementation
4. **Phase 4**: Source syncers (local, git, rsync)
5. **Phase 5**: Dependency installers per ecosystem
6. **Phase 6**: Cache manager
7. **Phase 7**: WorkspacePreparation orchestrator
8. **Phase 8**: Integration testing

---

**End of Task 029.a Specification**