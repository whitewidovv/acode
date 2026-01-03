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